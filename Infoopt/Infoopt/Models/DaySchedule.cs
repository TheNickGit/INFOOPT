using System.Collections.Generic;
using System;
using System.Linq;

class DaySchedule
{
    public List<RouteTrip> trips;
    public float timeToComplete { get { return trips.Select(trip => trip.timeToComplete).Sum(); } }

    /// <summary>
    /// Constructor.
    /// </summary>
    public DaySchedule()
    {
        trips = new List<RouteTrip>();
        AddTrip();
    }

    /// <summary>
    /// Display all route trip orders
    /// </summary>
    public string Display()
        => String.Join("\n", this.trips.Select(
            (trip, i) => $"--- Route trip {++i}  ({trip.volumePickedUp} L) ---\n{trip.Display()}"
        ));

    /// <summary>
    /// Add a route trip at the end of the dayroute
    /// </summary>
    public RouteTrip AddTrip()
    {
        RouteTrip trip = new RouteTrip(this);
        trips.Add(trip);
        return trip;
    }

    /// <summary>
    /// Get a random route trip in this dayroute
    /// </summary>
    public RouteTrip getRandomRouteTrip()
    {
        return trips[Program.random.Next(trips.Count)];
    }
}

