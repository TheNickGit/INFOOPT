using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.ComponentModel;
using System.Reflection;

class LocalSearch
{
    public static Stopwatch sw = new Stopwatch();
    public static bool showProgress = true;
    public static int showProgressPerIterations = 99_000;

    // Config:
    public static double
        totalIterations = 300_000_000,
        chanceAdd = 0.02,           // Chances are cumulative up to 1.00
        chanceRemove = 0.01,
        chancePureShiftWithinTrip = 0.30,
        chancePureShiftBetweenTrips = 0.67,
        alpha = 0.99,               // Rate at which T declines
        startT = 0.50,              // Starting chance to accept worse outcomes
        T = startT,
        reheatT = startT / 100,           // When reheating, T will be set to this
        coolDownIts = 100_000,        // Amount of iterations after which T*alpha happens
        reheat = 50_000_000,        // Reset T after this amount of iterations

        pCorrectionAdd = 8000,     // For adds - Increase for more accepted adds
        pCorrectionRemove = 2000,   // For removes - Increase for more accepted removes
        pCorrectionPureShiftWithinTrip = 100,
        pCorrectionPureShiftBetweenTrips = 1500;



    // Counters (for debugging)
    public int
        adds = 0,
        removes = 0,
        shifts = 0,
        swaps = 0,
        pureShiftsWithinTrip = 0,
        pureShiftsBetweenTrips = 0;

    // Properties
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
        this.orders = new SmartArray<Order>();
        foreach (Order order in orders)
        {
            this.orders.Add(order);
        }

        trucks = new Truck[2] { new Truck(), new Truck() };

        // Config for extra trips.
        trucks[0].schedule.weekSchedule[0].AddTrip();
        //trucks[0].schedule.weekSchedule[1].AddTrip();
        //trucks[0].schedule.weekSchedule[2].AddTrip();
        //trucks[0].schedule.weekSchedule[3].AddTrip();
        trucks[0].schedule.weekSchedule[4].AddTrip();
        trucks[1].schedule.weekSchedule[0].AddTrip();
        //trucks[1].schedule.weekSchedule[1].AddTrip();
        //trucks[1].schedule.weekSchedule[2].AddTrip();
        trucks[1].schedule.weekSchedule[3].AddTrip();
        trucks[1].schedule.weekSchedule[4].AddTrip();
    }


    public void DisplayProgress(int it)
    {
        if (it % showProgressPerIterations == 0) {
            double progress = Math.Round(100 * (((float)it) / ((float)totalIterations)), 1);
            double duration = Math.Round(sw.ElapsedMilliseconds / 1000f, 1);
            double controlParam = Math.Round(T, 3);
            string[] args = new string[3] {
                progress.ToString().PadLeft(4),
                duration.ToString().PadLeft(6),
                controlParam.ToString().PadLeft(6)
            };
            Console.Error.Write(String.Format("LS progress: {0} % ({1} s; {2} T)\r", args));
        }
    }

    /// <summary>
    /// Run the Local Search algorithm.
    /// </summary>
    public void Run()
    {
        sw.Start();
        for (int i = 1; i < totalIterations; i++)
        {
            Iteration();
            if (i % coolDownIts == 0)
                T *= alpha;
            if (i % reheat == 0)
                T = reheatT;
            if (showProgress)
                this.DisplayProgress(i);
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
            DoublyNode<Order> target = trip.getRandomOrderNode().Item1;
            targetRouteList[i] = (target, trip);
        }

        // Calculate changes in cost, time and volume and see if this fits.
        float[] timeChanges = new float[order.freq];
        float totalCostChange = 0;
        for (int i = 0; i < order.freq; i++)
        {
            if (!targetRouteList[i].Item2.CanAddVolume(order.volume))
                return;

            // Time change
            timeChanges[i] = Schedule.TimeChangeAdd(order, targetRouteList[i].Item1);
            if (targetRouteList[i].Item2.totalTime + timeChanges[i] > maxDayTime)
                return;

            // Cost change
            totalCostChange += Schedule.CostChangeAdd(order, targetRouteList[i].Item1);
        }
        double p = Math.Exp(-totalCostChange / T / pCorrectionAdd);

        // Add the order if cost is negative or with a certain chance
        if (totalCostChange < 0 || Program.random.NextDouble() < p)
        {
            for (int i = 0; i < order.freq; i++)
                targetRouteList[i].Item2.AddOrder(order, targetRouteList[i].Item1, timeChanges[i]);
            // If freq > 1, update links so that this order can easily reference itself in other trips.
            if (order.freq > 1)
            {
                order.links = new List<(DoublyNode<Order>, RouteTrip)>();
                for (int i = 0; i < order.freq; i++)
                    order.links.Add((targetRouteList[i].Item1.prev, targetRouteList[i].Item2));
            }
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
        (DoublyNode<Order>, int) orderT = trip.getRandomOrderNode();
        DoublyNode<Order> orderNode = orderT.Item1;
        Order order = orderNode.value;
        if (order.freq == 0)
            return;

        List<(DoublyNode<Order>, RouteTrip)> targetRouteList = new List<(DoublyNode<Order>, RouteTrip)>();
        // Dit is nu met referenties en snel :)
        if (order.freq == 1)
            targetRouteList.Add((orderNode, trip));
        if (order.freq > 1)
            foreach ((DoublyNode<Order>, RouteTrip) link in order.links)
                targetRouteList.Add(link);

        // Calculate change in time and cost.
        float[] timeChanges = new float[order.freq];
        float totalCostChange = 0;
        for (int i = 0; i < order.freq; i++)
        {
            // Time change
            timeChanges[i] = Schedule.TimeChangeRemove(targetRouteList[i].Item1);
            if (targetRouteList[i].Item2.totalTime + timeChanges[i] > maxDayTime)
                return;

            // Cost change
            totalCostChange += Schedule.CostChangeRemove(targetRouteList[i].Item1);
        }
        double p = Math.Exp(-totalCostChange / T / pCorrectionRemove);

        // Remove order.
        if (totalCostChange < 0 || Program.random.NextDouble() < p)
        {
            for (int i = 0; i < order.freq; i++)
            {
                int index = targetRouteList[i].Item2.orderArray.FindIndex(targetRouteList[i].Item1);
                targetRouteList[i].Item2.RemoveOrder(targetRouteList[i].Item1, index, timeChanges[i]);
            }
                
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
        (DoublyNode<Order>, int) orderT1 = trip.getRandomOrderNode(2);
        DoublyNode<Order> orderNode1 = orderT1.Item1;
        DoublyNode<Order> orderNode2 = trip.getRandomOrderNode(2).Item1;
        if (orderNode2.Equals(orderNode1) || orderNode1.value.freq == 0 || orderNode2.value.freq == 0)
            return; // cannot shift with start/stortplaatsen or itself

        // Calculate change in time and see if the shift can fit in the schedule.
        float timeChange = Schedule.TimeChangePureShiftWithinTrip(orderNode1, orderNode2);
        if (trip.totalTime + timeChange > maxDayTime)
            return; 

        // Calculate cost change and see if shifting this order is an improvement.
        float costChange = timeChange;  // equal because cost penalty wont be applied in within trip shift
        double p = Math.Exp(-costChange / T / pCorrectionPureShiftWithinTrip);

        if (costChange < 0 || Program.random.NextDouble() < p)
        {
            DoublyNode<Order> newNode1 = trip.PureShiftOrderBeforeOther(orderNode1, orderNode2, timeChange);
            trip.orderArray.Update(newNode1, orderT1.Item2);    // Update node in the smart array (previous node now has null pointers, new node points correctly)
            // If freq > 1, update links to remove the old orderNode and add the new one
            orderNode1.value.links.Remove((orderNode1, trip));  
            orderNode1.value.links.Add((newNode1, trip));

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
        (DoublyNode<Order>, int) orderT1 = trip.getRandomOrderNode();
        (DoublyNode<Order>, int) orderT2 = trip2.getRandomOrderNode();
        DoublyNode<Order> orderNode1 = orderT1.Item1;
        DoublyNode<Order> orderNode2 = orderT2.Item1;

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
            trip2.AddOrder(orderNode1.value, orderNode2, timeChangeInsert);
            trip.RemoveOrder(orderNode1, orderT1.Item2, timeChangeRemove);

            pureShiftsBetweenTrips++;
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
        for (int i = 0; i < orders.length; i++)
        {
                cost += 3 * orders.array[i].freq * orders.array[i].emptyDur;
        }

        // Add cost for time on the road
        foreach (Truck truck in this.trucks)
        {
            cost += truck.schedule.TimeCost();
        }

        return cost / 60f; // in minutes
    }
}
