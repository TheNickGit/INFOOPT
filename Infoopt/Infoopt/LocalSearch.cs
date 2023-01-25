using System.Collections.Generic;
using System;

class LocalSearch
{
    // Config:
    public static double
        totalIterations = 20_000_000,
        chanceAdd = 0.02,           // Chances are cumulative up to 1.00
        chanceRemove = 0.01,
        chanceShift = 0.00,
        chanceSwap = 0.00,
        chancePureShiftWithinTrip = 0.10,
        chancePureShiftBetweenTrips = 0.87,
        alpha = 0.99,               // Rate at which T declines
        startT = 0.50,              // Starting chance to accept worse outcomes
        T = startT,
        reheatT = startT / 100,           // When reheating, T will be set to this
        coolDownIts = 50_000,        // Amount of iterations after which T*alpha happens
        reheat = 30_000_000,        // Reset T after this amount of iterations

        pCorrectionAdd = 8000,     // For adds - Increase for more accepted adds
        pCorrectionRemove = 1000,   // For removes - Increase for more accepted removes
        //pCorrectionShift = 100,     // For shifts - Increase for more accepted shifts
        //pCorrectionSwap = 1000,      // For swaps - Increase for more accepted swaps
        pCorrectionPureShiftWithinTrip = 100,
        pCorrectionPureShiftBetweenTrips = 1000;



    // Counters (for debugging)
    public int
        adds = 0,
        removes = 0,
        shifts = 0,
        swaps = 0,
        pureShiftsWithinTrip = 0,
        pureShiftsBetweenTrips = 0;

    // Properties
    //public Order[] orders;
    public SmartArray<Order> orders;
    public Truck[] trucks;
    public static int maxDayTime = 43200;

    /// <summary>
    /// Get a random order.
    /// </summary>
    //Order RandomOrder() => this.orders[Program.random.Next(orders.Length)];
    (Order, int) RandomOrder() => orders.GetRandom();

    /// <summary>
    /// Get a random truck.
    /// </summary>
    /// <returns></returns>
    Truck RandomTruck() => this.trucks[Program.random.Next(trucks.Length)];

    /// <summary>
    /// Given a truck, return one of its routes.
    /// </summary>
    DaySchedule RandomDaySchedule(Truck truck) => truck.schedule.weekSchedule[(int)Program.random.RandomDay()];

    /// <summary>
    /// Get a random RouteTrip.
    /// </summary>
    /// <returns></returns>
    RouteTrip RandomRouteTrip()
    {
        return RandomDaySchedule(RandomTruck()).getRandomRouteTrip();
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public LocalSearch(Order[] orders)
    {
        this.orders = new SmartArray<Order>(orders.Length);
        foreach (Order order in orders)
        {
            this.orders.Add(order);
        }

        trucks = new Truck[2] { new Truck(), new Truck() };
    }

    /// <summary>
    /// Run the Local Search algorithm.
    /// </summary>
    public void Run()
    {
        for (int i = 1; i <= totalIterations; i++)
        {
            Iteration();
            if (i % coolDownIts == 0)
                T *= alpha;
            if (i % reheat == 0)
                T = reheatT;
        }
    }

    /// <summary>
    /// Perform an iteration of the Local Search algorithm.
    /// </summary>
    void Iteration()
    {
        double choice = Program.random.NextDouble();

        if (choice < chanceAdd) // Chance Add
            TryAddOrder();
        else if (choice < chanceAdd + chanceRemove) // Chance Remove
            TryRemoveOrder();

        else if (choice < chanceAdd + chanceRemove + chancePureShiftWithinTrip)
            TryPureShiftWithinTrip();

        else if (choice < chanceAdd + chanceRemove + chancePureShiftWithinTrip + chancePureShiftBetweenTrips)
            TryPureShiftBetweenTrips();

        /*
        else if (choice < chanceAdd + chanceRemove + chanceShift) // Chance Shift
            TryShiftOrders();
        else if (choice < chanceAdd + chanceRemove + chanceShift + chanceSwap) // Chance Swap
            TrySwapOrders();
        */
    }

    /// <summary>
    /// Try to add an order into any of the routes.
    /// </summary>
    void TryAddOrder()
    {
        (Order, int) orderT = RandomOrder();
        Order order = orderT.Item1;
        if (order == null)
            return;

        // Generate the days the order can be placed on, depending on frequency.
        Day[] days = Program.random.RandomDays(order.freq);

        // Generate random spots on these days to potentially add the order to.
        (DoublyNode<Order>, RouteTrip)[] targetRouteList = new (DoublyNode<Order>, RouteTrip)[order.freq];
        for (int i = 0; i < order.freq; i++)
        {
            Day day = days[i];
            DaySchedule route = RandomTruck().schedule.weekSchedule[(int)day];
            RouteTrip trip = route.getRandomRouteTrip();
            DoublyNode<Order> target = trip.getRandomOrderNode();
            targetRouteList[i] = (target, trip);
        }

        // Calculate change in time and see if the order can fit in the schedule.
        float[] timeChanges = new float[order.freq];
        for (int i = 0; i < order.freq; i++)
        {
            foreach (RouteTrip trip in targetRouteList[i].Item2.parent.trips)
                if (trip.CanAddVolume(order.volume))
                {
                    targetRouteList[i].Item1 = trip.getRandomOrderNode();
                    targetRouteList[i].Item2 = trip;
                }

            bool newTrip = !targetRouteList[i].Item2.CanAddVolume(order.volume);
            timeChanges[i] = Schedule.TimeChangeAdd(order, targetRouteList[i].Item1, newTrip);
            if (targetRouteList[i].Item2.totalTime + timeChanges[i] > maxDayTime)
                return;
        }

        // Calculate cost change and see if adding this order here is an improvement.
        float totalCostChange = 0;
        for (int i = 0; i < order.freq; i++)
        {
            bool newTrip = !targetRouteList[i].Item2.CanAddVolume(targetRouteList[i].Item1.value.volume);
            totalCostChange += Schedule.CostChangeAdd(order, targetRouteList[i].Item1, newTrip);
        }
        double p = Math.Exp(-totalCostChange / T / pCorrectionAdd);

        // Add the order if cost is negative or with a certain chance
        if (totalCostChange < 0 || Program.random.NextDouble() < p)
        {
            for (int i = 0; i < order.freq; i++)
                targetRouteList[i].Item2.AddOrder(order, targetRouteList[i].Item1, timeChanges[i]);
            orders.Remove(orderT.Item2);
            adds++;
        }
    }

    /// <summary>
    /// Try to remove an order from the current schedules.
    /// </summary>
    void TryRemoveOrder()
    {
        RouteTrip trip = RandomDaySchedule(RandomTruck()).getRandomRouteTrip();
        DoublyNode<Order> orderNode = trip.getRandomOrderNode();
        Order order = orderNode.value;
        if (order.freq == 0)
            return;

        List<(DoublyNode<Order>, RouteTrip)> targetRouteList = new List<(DoublyNode<Order>, RouteTrip)>();
        // TODO optimalisatie: Super dirty: dit werkt maar is traag voor freq > 1! Specifieker kunnen zoeken is een stuk sneller.
        if (order.freq == 1)
            targetRouteList.Add((orderNode, trip));
        if (order.freq > 1)
            foreach (Truck truck in trucks)
                foreach (DaySchedule daySchedule in truck.schedule.weekSchedule)
                    foreach (RouteTrip routeTrip in daySchedule.trips)
                        if (routeTrip.orders.Contains(order))
                            targetRouteList.Add((routeTrip.orders.Find(order), routeTrip));

        // Calculate change in time and see if removing the order can time-wise.
        float[] timeChanges = new float[order.freq];
        for (int i = 0; i < order.freq; i++)
        {
            bool onlyTrip = true;
            if (targetRouteList[i].Item2.parent.trips.Count > 1)
                onlyTrip = false;
            timeChanges[i] = Schedule.TimeChangeRemove(targetRouteList[i].Item1, onlyTrip);
            if (targetRouteList[i].Item2.totalTime + timeChanges[i] > maxDayTime)
                return;
        }

        // Calculate cost change and see if removing this order is an improvement.
        float totalCostChange = 0;
        for (int i = 0; i < order.freq; i++)
            totalCostChange += Schedule.CostChangeRemove(targetRouteList[i].Item1);
        double p = Math.Exp(-totalCostChange / T / pCorrectionRemove);

        // Remove order.
        if (totalCostChange < 0 || Program.random.NextDouble() < p)
        {
            for (int i = 0; i < order.freq; i++)
                targetRouteList[i].Item2.RemoveOrder(targetRouteList[i].Item1, timeChanges[i]);
            orders.Add(order);
            removes++;
        }
    }

    // Try shift an order of a trip before another order of that trip (regardless of order pickup frequency)
    void TryPureShiftWithinTrip() {

        // Get a valid shiftable route trip
        RouteTrip trip = RandomRouteTrip();
        if (trip.orders.Length <= 3) 
            return; // For 3 or less orders, shifting an order within a trip is not possible

        // Get valid order nodes for shift
        DoublyNode<Order> orderNode1 = trip.getRandomOrderNode();
        DoublyNode<Order> orderNode2 = trip.getRandomOrderNode();
        if (orderNode2.Equals(orderNode1) || orderNode1.value.freq == 0 || orderNode2.value.freq == 0)
            return; // cannot shift with start/stortplaatsen or itself

        // Calculate change in time and see if the shift can fit in the schedule.
        float timeChange = Schedule.TimeChangePureShiftWithinTrip(orderNode1, orderNode2);
        if (trip.totalTime + timeChange > maxDayTime)
            return; 

        // Calculate cost change and see if shifting this order is an improvement.
        float costChange = timeChange;  // equal because cost penalty wont be applied in within trip shift
        double p = Math.Exp(-costChange / T / pCorrectionPureShiftWithinTrip);

        if (costChange < 0 || Program.random.NextDouble() < p) {
            trip.PureShiftOrderBeforeOther(orderNode1, orderNode2, timeChange);
            pureShiftsWithinTrip++;
        }
        
    }

    // Try shift an order of a trip before an order of another trip
    void TryPureShiftBetweenTrips() {

        // Get valid route trips
        RouteTrip trip = RandomRouteTrip(),
            trip2 = RandomRouteTrip();
        while (trip2.Equals(trip))
            trip2 = RandomRouteTrip();
        if (trip.orders.Length < 3 || trip2.orders.Length < 3)
            return; // Either trips should have at least 1 order apart from start/stort in order to be shiftable
        
        // Get valid order nodes for shift
        DoublyNode<Order> orderNode1 = trip.getRandomOrderNode();
        DoublyNode<Order> orderNode2 = trip2.getRandomOrderNode();

        if (orderNode1.value.freq == 0 || orderNode2.value.freq == 0 )
            return; // cannot shift with start/stortplaatsen

        if (orderNode1.value.freq != 1)
            return; // cannot just shift an order of freq>1 to another random trip

        if (!trip2.CanAddVolume(orderNode1.value.volume))
            return; // ordernode1 does not fit capacity-wise when shifted to trip2


        // Calculate change in time and see if the shift can fit in the schedule.
        (float timeChangeRemove, float timeChangeInsert) = Schedule.TimeChangePureShiftBetweenTrips(orderNode1, orderNode2);
        if (trip2.totalTime + timeChangeInsert > maxDayTime)
            return; // ordernode1 does not fit time-wise when shifted to trip2


        // Calculate cost change and see if shifting this order is an improvement.
        float costChange = (timeChangeRemove + timeChangeInsert);  // equal to full time change because cost penalty wont be applied in between trips shift
        double p = Math.Exp(-costChange / T / pCorrectionPureShiftBetweenTrips);

        if (costChange < 0 || Program.random.NextDouble() < p) {
            // place order1 before order2
            trip.orders.EjectAfterNode(orderNode1.prev);
            trip2.orders.InsertBeforeNode(orderNode1.value, orderNode2);

            // modify trip times to complete
            trip.timeToComplete += timeChangeRemove;
            trip2.timeToComplete += timeChangeInsert;

            // modify trip volumes
            trip.volumePickedUp -= orderNode1.value.volume;
            trip2.volumePickedUp += orderNode1.value.volume;

            pureShiftsBetweenTrips++;
        }
    }


    ///// <summary>
    ///// Try to shift two orders in the same route.
    ///// </summary>
    //void TryShiftOrders()
    //{
    //    // Get a random route.
    //    RouteTrip trip = RandomRouteTrip();
    //    if (trip.orders.Length < 4) // With 3 or less orders (1 or less without start and end), a swap is not possible.
    //        return;

    //    // Get 2 random order nodes from this route.
    //    DoublyNode<Order> orderNode1 = trip.getRandomOrderNode();
    //    DoublyNode<Order> orderNode2 = trip.getRandomOrderNode();
    //    if (orderNode1.Equals(orderNode2) || orderNode1.value.freq == 0 || orderNode2.value.freq == 0)  // No sense swapping a node with itself.
    //        return;

    //    // Calculate change in time and see if the shift can fit in the schedule.
    //    float timeChange = Schedule.TimeChangeShift(orderNode1, orderNode2);
    //    if (trip.totalTime + timeChange > maxDayTime)
    //        return; // Check if shift fits time-wise

    //    // Calculate cost change and see if shifting this order is an improvement.
    //    float costChange = Schedule.CostChangeShift(orderNode1, orderNode2);
    //    double p = Math.Exp(-costChange / T / pCorrectionShift);

    //    // Shift order.
    //    if (costChange < 0 || Program.random.NextDouble() < p)
    //    {
    //        trip.ShiftOrders(orderNode1, orderNode2, timeChange);
    //        shifts++;
    //    }
    //}

    ///// <summary>
    ///// Try to swap two orders in the current schedules (shift between different routes).
    ///// </summary>
    //void TrySwapOrders()
    //{
    //    // Get 2 random routes.
    //    RouteTrip trip1 = RandomRouteTrip();
    //    RouteTrip trip2 = RandomRouteTrip();
    //    if (trip1.Equals(trip2)) // If the routes are the same, it's not a swap but a shift.
    //        return;

    //    // get 2 random order nodes from these routes.
    //    DoublyNode<Order> orderNode1 = trip1.getRandomOrderNode();
    //    DoublyNode<Order> orderNode2 = trip2.getRandomOrderNode();

    //    if (trip1.Equals(trip2)) // If the routes are the same, it's not a swap but a shift.
    //        return;

    //    if (orderNode1.value.freq != 1 || orderNode2.value.freq != 1) // Swapping nodes of freq > 1 is asking for trouble!
    //        return;

    //    // Calculate the time change and see if swapping these orders can fit into the time schedules.
    //    float timeChange1 = Schedule.TimeChangeSwap(orderNode1, orderNode2),
    //        timeChange2 = Schedule.TimeChangeSwap(orderNode2, orderNode1);
    //    if (trip1.totalTime + timeChange1 > maxDayTime || trip2.totalTime + timeChange2 > maxDayTime)
    //        return; // Check if swap fits time-wise

    //    // Calculate the volume change and see if this can fit.
    //    int volumeChange1 = orderNode2.value.volume - orderNode1.value.volume,
    //        volumeChange2 = orderNode1.value.volume - orderNode2.value.volume;
    //    if (trip1.volumePickedUp + volumeChange1 > Truck.volumeCapacity || trip2.volumePickedUp + volumeChange2 > Truck.volumeCapacity)
    //        return;

    //    // Calculate cost change and see if swapping these orders is an improvement.
    //    float costChange = Schedule.CostChangeSwap(orderNode1, orderNode2),
    //        costChange2 = Schedule.CostChangeSwap(orderNode2, orderNode1);
    //    double p = Math.Exp(-(costChange + costChange2) / T / pCorrectionSwap);

    //    // Swap order nodes.
    //    if (costChange + costChange2 < 0 || Program.random.NextDouble() <= p)
    //    {
    //        trip1.timeToComplete += timeChange1;
    //        trip1.volumePickedUp += volumeChange1;
    //        trip2.timeToComplete += timeChange2;
    //        trip2.volumePickedUp += volumeChange2;
    //        DoublyList<Order>.SwapNodes(orderNode1, orderNode2);
    //        swaps++;
    //    }
    //}

    /// <summary>
    /// Calculates the total cost of a solution by checking all its content.
    /// This method is very slow! Use one of the methods for cost changes instead for small changes.
    /// </summary>
    public float CalcTotalCost()
    {
        float cost = 0.0f;

        // Add cost for missed orders
        foreach (Order order in this.orders.array)
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
