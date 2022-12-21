using System.Collections.Generic;
using System;

class LocalSearch
{
    // Config:
    public static double
        chanceAdd = 0.10,
        chanceRemove = 0.10,
        chanceShift = 0.30,
        chanceSwap = 0.50,
        alpha = 0.99,
        startT = 0.05,
        T = startT,
        coolDownIts = 5000,
        reheat = 10_000_000;

    // Properties
    public Order[] orders;
    public Truck[] trucks;
    public static int maxDayTime = 43200;
    public RandomGen random = new RandomGen();
    Order RandomOrder() => this.orders[random.Next(orders.Length)];
    Truck RandomTruck() => this.trucks[random.Next(trucks.Length)];

    /// <summary>
    /// Given a truck, return one of its routes.
    /// </summary>
    Route RandomRoute(Truck truck) => truck.schedule.weekRoutes[(int)random.RandomDay()];

    /// <summary>
    /// Given a route, return a random order node (excluding the start and end).
    /// </summary>
    DoublyNode<Order> RandomRouteOrder(Route route)
    {
        int randSpot = random.Next(1, route.orders.Length - 1); // dont take first or last(dont mutate)
        return route.orders.head.SkipForward(randSpot);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public LocalSearch(Order[] orders)
    {
        this.orders = orders;
        trucks = new Truck[2] { new Truck(), new Truck() };
    }

    /// <summary>
    /// Run the Local Search algorithm.
    /// </summary>
    public void Run(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            Iteration();
            if (i % coolDownIts == 0)
                T = alpha * T;
            if (i % reheat == 0)
                T = startT;
        }
    }

    /// <summary>
    /// Perform an iteration of the Local Search algorithm.
    /// </summary>
    void Iteration()
    {
        double choice = random.NextDouble();

        if (choice < chanceAdd) // Chance Add
            TryAddOrder();
        else if (choice < chanceAdd + chanceRemove) // Chance Remove
            TryRemoveOrder();
        else if (choice < chanceAdd + chanceRemove + chanceShift) // Chance Shift
            TryShiftOrders();
        else if (choice < chanceAdd + chanceRemove + chanceShift + chanceSwap) // Chance Swap
            TrySwapOrders();
    }

    /// <summary>
    /// Try to add an order into any of the routes.
    /// </summary>
    void TryAddOrder()
    {
        Order order = RandomOrder();
        if (!order.available)
            return;

        // Generate the days the order can be placed on, depending on frequency.
        Day[] days = random.RandomDays(order.freq);

        // Generate random spots on these days to potentially add the order to.
        (DoublyNode<Order>, Route)[] targetRouteList = new (DoublyNode<Order>, Route)[order.freq];
        for (int i = 0; i < order.freq; i++)
        {
            Truck truck = RandomTruck();
            Day day = days[i];
            Route route = truck.schedule.weekRoutes[(int)day];
            DoublyNode<Order> target = RandomRouteOrder(route);
            targetRouteList[i] = (target, route);
        }

        // Calculate change in time and see if the order can fit in the schedule.
        float[] timeChanges = new float[order.freq];
        for (int i = 0; i < order.freq; i++)
        {
            timeChanges[i] = Schedule.TimeChangePutBeforeOrder(order, targetRouteList[i].Item1);
            if (targetRouteList[i].Item2.timeToComplete + timeChanges[i] > maxDayTime)
                return;
        }

        // Calculate cost change and see if adding this order here is an improvement.
        float totalCostChange = 0;
        double p;
        for (int i = 0; i < order.freq; i++)
            totalCostChange += Schedule.CostChangePutBeforeOrder(order, targetRouteList[i].Item1);
        p = Math.Exp(-totalCostChange / T);

        // Add the order if cost is negative or with a certain chance
        if (totalCostChange < 0 || random.NextDouble() < T)
        {
            for (int i = 0; i < order.freq; i++)
                targetRouteList[i].Item2.PutOrderBefore(order, targetRouteList[i].Item1, timeChanges[i]);
        }
    }

    /// <summary>
    /// Try to remove an order from the current schedules.
    /// </summary>
    void TryRemoveOrder()
    {
        Route route = RandomRoute(RandomTruck());
        DoublyNode<Order> orderNode = RandomRouteOrder(route);
        Order order = orderNode.value;
        if (order.freq == 0)
            return;

        List<(DoublyNode<Order>, Route)> targetRouteList = new List<(DoublyNode<Order>, Route)>();
        // TODO optimalisatie: Super dirty: dit werkt maar is traag voor freq > 1! Specifieker kunnen zoeken is een stuk sneller.
        if (order.freq == 1)
            targetRouteList.Add((orderNode, route));
        if (order.freq > 1)
            for (int i = 0; i <= 1; i++)
                for (int j = 0; j < 5; j++)
                    if (trucks[i].schedule.weekRoutes[j].orders.Contains(order))
                        targetRouteList.Add((trucks[i].schedule.weekRoutes[j].orders.Find(order), trucks[i].schedule.weekRoutes[j]));

        // Calculate change in time and see if removing the order can time-wise.
        float[] timeChanges = new float[order.freq];
        for (int i = 0; i < order.freq; i++)
        {
            timeChanges[i] = Schedule.TimeChangeRemoveOrder(targetRouteList[i].Item1);
            if (targetRouteList[i].Item2.timeToComplete + timeChanges[i] > maxDayTime)
                return;
        }

        // Calculate cost change and see if removing this order is an improvement.
        float totalCostChange = 0;
        for (int i = 0; i < order.freq; i++)
            totalCostChange += Schedule.CostChangeRemoveOrder(targetRouteList[i].Item1);

        // Remove order.
        if (totalCostChange < 0 || random.NextDouble() <= T)
        {
            for (int i = 0; i < order.freq; i++)
                targetRouteList[i].Item2.RemoveOrder(targetRouteList[i].Item1, timeChanges[i]);
        }
    }

    /// <summary>
    /// Try to shift two orders in the same route.
    /// </summary>
    void TryShiftOrders()
    {
        // Get a random route.
        Route route = RandomRoute(RandomTruck());
        if (route.orders.Length < 4) // With 3 or less orders (1 or less without start and end), a swap is not possible.
            return;

        // Get 2 random order nodes from this route.
        DoublyNode<Order> orderNode1 = RandomRouteOrder(route);
        DoublyNode<Order> orderNode2 = RandomRouteOrder(route);
        if (orderNode1.Equals(orderNode2))  // No sense swapping a node with itself.
            return;

        // Calculate change in time and see if the shift can fit in the schedule.
        float timeChange = Schedule.TimeChangeShiftOrders(orderNode1, orderNode2);
        if (route.timeToComplete + timeChange > maxDayTime)
            return; // Check if shift fits time-wise

        // Calculate cost change and see if shifting this order is an improvement.
        float costChange = Schedule.CostChangeShiftOrders(orderNode1, orderNode2);

        // Shift order.
        if (costChange < 0 || random.NextDouble() < T)
        {
            route.ShiftOrders(orderNode1, orderNode2, timeChange);
        }
    }

    /// <summary>
    /// Try to swap two orders in the current schedules (shift between different routes).
    /// </summary>
    void TrySwapOrders()
    {
        // Get 2 random routes.
        Route route1 = RandomRoute(RandomTruck());
        Route route2 = RandomRoute(RandomTruck());
        if (route1.Equals(route2)) // If the routes are the same, it's not a swap but a shift.
            return;

        // get 2 random order nodes from these routes.
        DoublyNode<Order> orderNode1 = RandomRouteOrder(route1);
        DoublyNode<Order> orderNode2 = RandomRouteOrder(route2);
        if (orderNode1.value.freq != 1 || orderNode2.value.freq != 1) // Swapping nodes of freq > 1 is asking for trouble!
            return;

        // Calculate the time change and see if swapping these orders can fit into the time schedules.
        float timeChange = Schedule.TimeChangeSwapOrders(orderNode1, orderNode2),
            timeChange2 = Schedule.TimeChangeSwapOrders(orderNode2, orderNode1);
        if (route1.timeToComplete + timeChange > maxDayTime || route2.timeToComplete + timeChange2 > maxDayTime)
            return; // Check if swap fits time-wise

        // Calculate cost change and see if swapping these orders is an improvement.
        float costChange = Schedule.CostChangeSwapOrders(orderNode1, orderNode2),
            costChange2 = Schedule.CostChangeSwapOrders(orderNode2, orderNode1);

        // Swap order nodes.
        if (costChange + costChange2 < 0 || random.NextDouble() <= T)
        {
            Route.SwapOrders(
                (route1, orderNode1, timeChange),
                (route2, orderNode2, timeChange2)
            );
        }
    }

    /// <summary>
    /// Calculates the total cost of a solution by checking all its content.
    /// This method is very slow! Use one of the methods for cost changes instead for small changes.
    /// </summary>
    public float CalcTotalCost()
    {
        float cost = 0.0f;

        // Add cost for missed orders
        foreach (Order order in this.orders)
        {
            if (order.available)
                cost += 3 * order.freq * order.emptyDur;
        }

        // Add cost for time on the road
        foreach (Truck truck in this.trucks)
        {
            cost += truck.schedule.TimeCost();
        }

        return cost;
    }
}
