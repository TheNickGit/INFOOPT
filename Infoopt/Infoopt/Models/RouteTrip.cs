using System.Linq;
using System;

class RouteTrip
{
    public DaySchedule parent;
    public DoublyList<Order> orders;
    public float timeToComplete = 0f;
    public float totalTime { get { return parent.timeToComplete; } }
    public int volumePickedUp = 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    public RouteTrip(DaySchedule parent)
    {
        orders = new DoublyList<Order>(new DoublyNode<Order>(Program.startOrder), new DoublyNode<Order>(Program.stopOrder));
        this.parent = parent;
    }

    /// <summary>
    /// Display all trip orders
    /// </summary>
    public string Display()
        => String.Join('\n', this.orders.ToEnumerable().Select(order => order.value.Display()));

    /// <summary>
    /// puts an order before another order in this route (with time change dt)
    /// </summary>
    public void AddOrder(Order order, DoublyNode<Order> routeOrder, float dt)
    {
        if (CanAddVolume(order.volume))
        {
            orders.InsertBeforeNode(order, routeOrder);    // put order before a order already in the route
            timeToComplete += dt;                          // modify total route time to complete 
            volumePickedUp += order.volume;                // add order's garbage volume
            order.available = false;                       // signal that order has been placed into a route
        }
        else
        {
            parent.AddTrip().AddNewTripOrder(order, dt);
        }
    }

    /// <summary>
    /// Add an order to a just newly created trip.Call this when creating a new trip instead of the usual AddOrder().
    /// </summary>
    public void AddNewTripOrder(Order order, float dt)
    {
        AddOrder(order, orders.tail, dt);
    }

    /// <summary>
    /// removes an order in this route (with time change dt)
    /// </summary>
    public void RemoveOrder(DoublyNode<Order> routeOrder, float dt)
    {
        orders.EjectAfterNode(routeOrder.prev);        // remove order from route
        timeToComplete += dt;                          // modify total route time to complete
        volumePickedUp -= routeOrder.value.volume;     // remove order's garvage volume
        routeOrder.value.available = true;             // signal that order has been removed from a route

        // If emptied, ask to be removed.
        if (orders.Length <= 2)
            parent.RemoveTrip(this);
    }

    /// <summary>
    /// Return whether it's posible to fit this amount of additional garbage in the truck.
    /// </summary>
    public bool CanAddVolume(float dv) => this.volumePickedUp + dv <= Truck.volumeCapacity;

    /// <summary>
    /// Get a random order node from this trip.
    /// </summary>
    public DoublyNode<Order> getRandomOrderNode()
    {
        int i = Program.random.Next(1, this.orders.Length); // dont take the starting order
        return orders.head.SkipForward(i);
    }

    ///// <summary>
    ///// With randomPointer.
    ///// </summary>
    ///// <returns></returns>
    //public DoublyNode<Order> getRandomOrderNode2()
    //{
    //    int distance = Program.random.Next(randomWalkLength);
    //    int direction = Program.random.Next(2);
    //    DoublyNode<Order> node = randomPointer;
    //    if (direction == 0)
    //    {
    //        for (int i = 0; i <= distance; i++)
    //        {
    //            node = node.prev;
    //            if (node.prev == null)
    //                return node.next;
    //        }
    //    }
    //    else
    //    {
    //        for (int i = 0; i <= distance; i++)
    //        {
    //            if (node.next == null)
    //                return node;
    //            node = node.next;
    //        }
    //    }
    //    return node;
    //}

    /// <summary>
    /// shifts two orders within same route
    /// </summary>
    public void ShiftOrders(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2, float timeChange)
    {
        DoublyList<Order>.SwapNodes(routeOrder, routeOrder2);    // swap values of nodes (orders)
        timeToComplete += timeChange;                             // modify total route time to complete
    }

    // shift 'routeOrder' before 'routeOrder2' within the trip
    public void PureShiftOrderBeforeOther(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2, float timeChange) {
        this.orders.EjectAfterNode(routeOrder.prev);                    // remove routeOrder1 from its position
        this.orders.InsertBeforeNode(routeOrder.value, routeOrder2);    // insert routeOrder before routeOrder2
        this.timeToComplete += timeChange;
    }
}
