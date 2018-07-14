﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

[JsonObject(MemberSerialization.OptIn)]
public class Flux
{
    public static float FrameDelayBetweenTrucks = 50;

    [JsonProperty]
    public IFluxSource Source { get; private set; }

    [JsonProperty]
    public IFluxTarget Target { get; private set; }

    public bool IsWaitingForInput { get; set; } = false;
    public bool IsWaitingForDelivery { get; set; } = false;
    public bool IsWaitingForPath { get; set; } = false;

    private static readonly float defaultSpeed = 0.1f;

    private readonly float speed;

    [JsonProperty]
    public int AvailableTrucks { get; private set; }
    [JsonProperty]
    public int TotalCargoMoved { get; private set; }

    private List<RoadVehicule> trucks;
    private RoadVehicule waitingTruck;
    private float currentDelay;

    public Path<Cell> Path { get; private set; }

    public enum Direction
    {
        incoming,
        outgoing
    }

    public static List<Flux> AllFlux = new List<Flux>();

    [JsonConstructor]
    public Flux(IFluxSource source, IFluxTarget target, int truckQuantity)
    {
        Source = source;
        Target = target;
        speed = defaultSpeed;
        TotalCargoMoved = 0;
        AvailableTrucks = truckQuantity;
        trucks = new List<RoadVehicule>(truckQuantity);
        currentDelay = FrameDelayBetweenTrucks;

        GetPath();

        if (Path != null)
        {
            Source.ReferenceFlux(this);
            Target.ReferenceFlux(this);
            AllFlux.Add(this);
        }
    }

    public Flux(Flux dummyFlux)
    {
        var trueSource = World.Instance.Constructions[dummyFlux.Source._Cell.X, dummyFlux.Source._Cell.Y] as IFluxSource;
        var trueTarget = World.Instance.Constructions[dummyFlux.Target._Cell.X, dummyFlux.Target._Cell.Y] as IFluxTarget;
        Source = trueSource;
        Target = trueTarget;
        speed = defaultSpeed;
        TotalCargoMoved = dummyFlux.TotalCargoMoved;
        AvailableTrucks = dummyFlux.AvailableTrucks;
        trucks = new List<RoadVehicule>(AvailableTrucks);
        currentDelay = FrameDelayBetweenTrucks;

        GetPath();
        if (Path != null)
        {
            Source.ReferenceFlux(this);
            Target.ReferenceFlux(this);
            AllFlux.Add(this);
        }
    }

    public void AddTrucks(int quantity)
    {
        AvailableTrucks += quantity;
    }

    public void UpdateTruckPath()
    {
        foreach (RoadVehicule truck in trucks)
            truck.UpdatePath();
    }

    private Path<Cell> GetPath()
    {
        var pf = new Pathfinder<Cell>(speed, 0, new List<Type>() { typeof(Road), typeof(City), typeof(Industry) });
        pf.FindPath(Target._Cell, Source._Cell);
        Path = pf.Path;
        return pf.Path;
    }

    private bool Consume()
    {
        if (Source.ProvideCargo(1))
        {
            currentDelay = 0;
            AvailableTrucks--;
            var truck = new RoadVehicule(World.Instance.TruckPrefab, speed, GetPath(), Source, Target, this);
            trucks.Add(truck);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Distribute(double ticks, double actualDistance, RoadVehicule truck)
    {
        var delivered = true;
        if (delivered)
        {
            truck.HasArrived = true;
            AvailableTrucks++;
            var walkingDistance = Source.ManhattanDistance(Target) * Pathfinder<Cell>.WalkingSpeed;
            var obtainedGain = World.LocalEconomy.GetGain("flux_deliver_percell");
            var gain = (int)Math.Round((walkingDistance - actualDistance) * obtainedGain);
            World.LocalEconomy.Credit(gain);
            TotalCargoMoved++;
        }
        return delivered;
    }

    public void Move()
    {
        int cost;
        currentDelay++;
        World.LocalEconomy.ForcedCost("flux_running", out cost);
        IsWaitingForInput = false;
        IsWaitingForDelivery = false;
        IsWaitingForPath = false;


        foreach (RoadVehicule truck in trucks)
        {
            truck.Tick();
            truck.CheckArrived();
        }

        trucks.RemoveAll(r => r.HasArrived);

        if (AvailableTrucks > 0 && currentDelay >= FrameDelayBetweenTrucks)
        {
            if (!Consume())
            {
                IsWaitingForInput = true;
            }
            else
            {
            }
        }

        foreach (RoadVehicule truck in trucks)
            truck.Move();
    }

    public static void RemoveFlux(Flux f)
    {
        AllFlux.Remove(f);
    }

    public override string ToString()
    {
        return $"[{Source} => {Target}]";
    }
}

