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
        RouteTrip trip = AddTrip();
        trip.timeToComplete = Truck.unloadTime;
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
        //Console.WriteLine("New Trip created--------------------------------------------------");
        return trip;
    }

    /// <summary>
    /// Remove a trip if it's completely emptied by a remove operation and it's not the only trip left.
    /// </summary>
    public void RemoveTrip(RouteTrip trip)
    {
        if (trip.orders.Length <= 2 && trips.Count > 1)
        {
            trips.Remove(trip);
            //Console.WriteLine("------------------------------------------------------Trip removed");
        }
    }

    /// <summary>
    /// Get a random route trip in this dayroute
    /// </summary>
    public RouteTrip getRandomRouteTrip()
    {
        return trips[Program.random.Next(trips.Count)];
    }

    /// <summary>
    /// Get a random order node from one of this dayschedule's trips.
    /// </summary>
    public DoublyNode<Order> getRandomRouteOrder()
    {
        RouteTrip trip = getRandomRouteTrip();
        return trip.getRandomOrderNode();
    }

    ///// <summary>
    ///// swaps two orders not within same route; otherwise shiftOrders
    ///// </summary>
    //public static void SwapOrders(
    //        (RouteTrip trip, DoublyNode<Order> routeOrder, float dt, int vt) o,
    //        (RouteTrip trip, DoublyNode<Order> routeOrder, float dt, int vt) o2
    //    )
    //{
    //    DoublyList<Order>.SwapNodes(o.routeOrder, o2.routeOrder);   // swap values of nodes (orders)
    //    o.trip.timeToComplete += o.dt;                             // modify total route time to complete
    //    o.trip.volumePickedUp += o.vt;                             // modify route garbage volume
    //    o2.trip.timeToComplete += o2.dt;                           // modify total route2 time to complete
    //    o2.trip.timeToComplete += o2.vt;                           // modify route garbage volume
    //}
}

