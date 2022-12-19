class Route
{
    public DoublyList<Order> orders;
    public float timeToComplete = 0.0f;

    /// <summary>
    /// Constructor.
    /// </summary>
    public Route(Order startOrder, Order stopOrder)
    {
        this.orders = new DoublyList<Order>(
            new DoublyNode<Order>(startOrder),
            new DoublyNode<Order>(stopOrder)
        );
    }

    /// <summary>
    /// Display all route orders (custom print)
    /// </summary>
    public string Display()
    {
        string msg = "";
        foreach (DoublyNode<Order> order in this.orders)
        {
            msg += $"{order.value.Display()}\n";
        }
        return msg;
    }

    /// <summary>
    /// puts an order before another order in this route (with time change dt)
    /// </summary>
    public void PutOrderBefore(Order order, DoublyNode<Order> routeOrder, float dt)
    {
        this.orders.InsertBeforeNode(order, routeOrder);    // put order before a order already in the route
        this.timeToComplete += dt;                          // modify total route time to complete 
        order.available = false;                          // signal that order has been placed into a route
    }

    /// <summary>
    /// removes an order in this route (with time change dt)
    /// </summary>
    public void RemoveOrder(DoublyNode<Order> routeOrder, float dt)
    {
        this.orders.EjectAfterNode(routeOrder.prev);        // remove order from route
        this.timeToComplete += dt;                          // modify total route time to complete
        routeOrder.value.available = true;               // signal that order has been removed from a route
    }

    /// <summary>
    /// swaps two orders not within same route; otherwise shiftOrders
    /// </summary>
    public static void SwapOrders(
            (Route route, DoublyNode<Order> routeOrder, float dt) o,
            (Route route, DoublyNode<Order> routeOrder, float dt) o2
        )
    {
        DoublyList<Order>.SwapNodes(o.routeOrder, o2.routeOrder);   // swap values of nodes (orders)
        o.route.timeToComplete += o.dt;                             // modify total route time to complete
        o2.route.timeToComplete += o2.dt;                           // modify total route2 time to complete
    }

    /// <summary>
    /// shifts two orders within same route
    /// </summary>
    public void ShiftOrders(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2, float timeChange)
    {
        DoublyList<Order>.SwapNodes(routeOrder, routeOrder2);    // swap values of nodes (orders)
        timeToComplete += timeChange;                             // modify total route time to complete
    }
}

