using System.Linq;
using System;

class RouteTrip
{
    public DaySchedule parent;
    public DoublyList<Order> orders;
    public SmartArray<DoublyNode<Order>> orderArray;
    public float timeToComplete = Truck.unloadTime;
    public float totalTime { get { return parent.timeToComplete; } }
    public int volumePickedUp = 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    public RouteTrip(DaySchedule parent)
    {
        orders = new DoublyList<Order>(new DoublyNode<Order>(Program.startOrder), new DoublyNode<Order>(Program.stopOrder));
        orderArray = new SmartArray<DoublyNode<Order>>();
        orderArray.Add(orders.head);
        orderArray.Add(orders.tail);
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
        if (!CanAddVolume(order.volume))
            return;

        orders.InsertBeforeNode(order, routeOrder);    // put order before a order already in the route
        timeToComplete += dt;                          // modify total route time to complete 
        volumePickedUp += order.volume;                // add order's garbage volume
        orderArray.Add(routeOrder.prev);               // add order into the smart array in O(1) time
    }

    /// <summary>
    /// removes an order in this route (with time change dt)
    /// </summary>
    public void RemoveOrder(DoublyNode<Order> routeOrder, int index, float dt)
    {
        orders.EjectAfterNode(routeOrder.prev);        // remove order from route
        timeToComplete += dt;                          // modify total route time to complete
        volumePickedUp -= routeOrder.value.volume;     // remove order's garvage volume
        orderArray.Remove(index);                      // remove order from the smart array in O(1) time
    }

    /// <summary>
    /// Return whether it's posible to fit this amount of additional garbage in the truck.
    /// </summary>
    public bool CanAddVolume(float dv) => this.volumePickedUp + dv <= Truck.volumeCapacity;

    /// <summary>
    /// Get a random order node from this trip.
    /// </summary>
    public (DoublyNode<Order>, int) getRandomOrderNode(int start = 1)
    {
        return orderArray.GetRandom(start);
    }

    /// <summary>
    /// shifts two orders within same route
    /// </summary>
    public void ShiftOrders(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2, float timeChange)
    {
        DoublyList<Order>.SwapNodes(routeOrder, routeOrder2);    // swap values of nodes (orders)
        timeToComplete += timeChange;                             // modify total route time to complete
    }

    // shift 'routeOrder' before 'routeOrder2' within the trip
    public DoublyNode<Order> PureShiftOrderBeforeOther(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2, float timeChange) {
        this.orders.EjectAfterNode(routeOrder.prev);                    // remove routeOrder1 from its position
        this.orders.InsertBeforeNode(routeOrder.value, routeOrder2);    // insert routeOrder before routeOrder2
        this.timeToComplete += timeChange;

        return routeOrder2.prev;
    }
}
