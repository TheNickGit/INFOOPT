using System;
using System.Collections.Generic;
using System.Text;

/// TODOs:
/// - Afvalvolumes tellen nog niet mee
/// - Aandachtspuntje shifts: Kan een order om te storten geshift worden en zo ja, hoe?

namespace Infoopt
{
    
    class LocalSearch {

        public Order[] orders;
        public Truck[] trucks;

        public static int maxDayTime = 43200;


        // Config:
        public static double
            chanceAdd = 0.30,
            chanceRemove = 0.10,
            chanceShift = 0.60,
            alpha = 0.005; // Chance to accept a worse solution


        public Random random = new Random();
        
        public Order randomOrder() => this.orders[random.Next(orders.Length)];
        public Truck randomTruck() => this.trucks[random.Next(trucks.Length)];
        public WorkDay randomDay() {
            WorkDay[] days = (WorkDay[]) Enum.GetValues(typeof(WorkDay));
            return days[random.Next(days.Length)];
        }
        public WorkDay[] randomDay2()
        {
            WorkDay[] allDays = (WorkDay[])Enum.GetValues(typeof(WorkDay));
            WorkDay[] days = new WorkDay[2];
            days[0] = allDays[random.Next(2)]; // Mon or Tue
            if (days[0] == WorkDay.Mon)
                days[1] = WorkDay.Thu;  // Mon + Thu combo
            else
                days[1] = WorkDay.Fri; // Tue + Fri combo
            return days;
        }
        public WorkDay[] randomDay3()
        {
            WorkDay[] days = new WorkDay[3];
            days[0] = WorkDay.Mon;
            days[1] = WorkDay.Wed;
            days[2] = WorkDay.Fri;
            return days;
        }
        public WorkDay[] randomDay4()
        {
            WorkDay[] allDays = (WorkDay[])Enum.GetValues(typeof(WorkDay));
            WorkDay[] days = new WorkDay[4];
            WorkDay excludedDay = allDays[random.Next(5)];
            int j = 0;
            for(int i = 0; i < 4; i++)
            {
                if (allDays[j] == excludedDay)
                    j++;
                days[i] = allDays[j];
                j++;
            }
            return days;
        }

        public Route randomTruckDayRoute(Truck truck) => truck.schedule.weekRoutes[(int)randomDay()];
        public DoublyNode<Order> randomTruckDayRouteOrder(Truck truck, WorkDay day) {
            Route route = truck.schedule.weekRoutes[(int)day];
            int randSpot = random.Next(1, route.orders.Length-1); // dont take first or last(dont mutate)
            return route.orders.head.skipForward(randSpot);
        }


        public LocalSearch(Order[] orders, Order startOrder, Order emptyingOrder) {
            this.orders = orders;
            this.trucks = new Truck[2] { 
                new Truck(startOrder, emptyingOrder), 
                new Truck(startOrder, emptyingOrder) };
        }


        // run the model for 'nIterations' cycles
        public void Run(int nIterations) {
            int i = 0;
            while (i <= nIterations) {
                MutateRandomTruckDayRoutes(i++);
            }
        }

        // perform one random mutation in a truckschedule route
        public void MutateRandomTruckDayRoutes(int i) {

            (Truck truck, WorkDay day, DoublyNode<Order> routeOrder) = PrecomputeMutationValues(); 

            // TODO: Simulated Annealing toepassen door de hiervoor benodigde variabelen en functionaliteit toe te voegen
           
            // Make a random choice for add, remove or shift depending on the chances given in the config.
            double choice = random.NextDouble();
            if (choice < chanceAdd) // Chance Add
            {
                // Take a random order and check if it's available to be added.
                Order order = randomOrder();
                if (!order.available)
                    return;
                TryAddOrder(order);
            }  
            else if (choice >= chanceAdd && choice < chanceAdd + chanceRemove) // Chance Remove
                TryRemoveOrder(truck, day, routeOrder);
            else if (choice >= chanceAdd + chanceRemove && choice < chanceAdd + chanceRemove + chanceShift) // Chance Shift
            {
                (Truck truck2, WorkDay day2, DoublyNode<Order> routeOrder2) = PrecomputeMutationValues();
                bool withinRoute = truck == truck2 && day == day2;

                if (withinRoute) 
                    TryShiftOrders(truck, day, routeOrder, routeOrder2);
                else 
                    TrySwapOrders(truck, day, routeOrder, truck2, day2, routeOrder2);
            }
                
        }

        // Generate random variables and do the other calculations needed for all iterations (add, remove, shift).
        public (Truck, WorkDay, DoublyNode<Order>) PrecomputeMutationValues() {
            Truck truck = randomTruck();
            WorkDay day = randomDay();
            return (truck, day, randomTruckDayRouteOrder(truck, day));
        }

        /// <summary>
        /// Try to add an order to a route, keeping its frequency in mind.
        /// </summary>
        public void TryAddOrder(Order order)
        {
            // Generate the days the order can be placed on, depending on frequency.
            WorkDay[] days;
            switch (order.freq)
            {
                case 1:
                    days = new WorkDay[1];
                    days[0] = randomDay();
                    break;
                case 2:
                    days = randomDay2();
                    break;
                case 3:
                    days = randomDay3();
                    break;
                case 4:
                    days = randomDay4();
                    break;
                default:
                    return;
            }

            // Generate random spots on these days to potentially add the order to.
            (DoublyNode<Order>, Route)[] targetRouteList = new (DoublyNode<Order>, Route)[order.freq];
            for (int i = 0; i < order.freq; i++)
            {
                Truck truck = randomTruck();
                WorkDay day = days[i];
                Route dayRoute = truck.schedule.weekRoutes[(int)day];
                DoublyNode<Order> target = randomTruckDayRouteOrder(truck, day);
                targetRouteList[i] = (target, dayRoute);
            }

            // Calculate change in time and see if the order can fit in the schedule.
            float[] timeChanges = new float[order.freq];
            for (int i = 0; i < order.freq; i++)
            {
                timeChanges[i] = Schedule.timeChangePutBeforeOrder(order, targetRouteList[i].Item1);
                if (targetRouteList[i].Item2.timeToComplete + timeChanges[i] > maxDayTime)
                    return;
            }

            // Calculate cost change and see if adding this order here is an improvement.
            float totalCostChange = 0;
            for (int i = 0; i < order.freq; i++)
                totalCostChange += Schedule.costChangePutBeforeOrder(order, targetRouteList[i].Item1);
            
            // Add the order if cost is negative or with a certain chance
            if (totalCostChange < 0 || random.NextDouble() < alpha) 
            {
                for(int i = 0; i < order.freq; i++)
                    targetRouteList[i].Item2.putOrderBefore(order, targetRouteList[i].Item1, timeChanges[i]);
            }
        }

        /// <summary>
        /// Force add an order into a route - mainly for testing purposes. This method will break the solution when uses for normal iterations!
        /// </summary>
        public void ForceAddOrder(Order order, Truck truck, WorkDay day, DoublyNode<Order> node)
        {
            float timeChange = Schedule.timeChangePutBeforeOrder(order, node);
            float costChange = Schedule.costChangePutBeforeOrder(order, node);
            if (costChange < 0 || random.NextDouble() < alpha)
            {
                Route route = truck.schedule.weekRoutes[(int)day];
                route.putOrderBefore(order, node, timeChange);
            }
        }

        /// <summary>
        /// Try to remove an order from the current schedules.
        /// </summary>
        public void TryRemoveOrder(Truck truck, WorkDay day, DoublyNode<Order> orderNode)
        {
            Order order = orderNode.value;
            if (order.freq == 0)
                return;

            Route dayRoute = truck.schedule.weekRoutes[(int)day];

            List<(DoublyNode<Order>, Route)> targetRouteList = new List<(DoublyNode<Order>, Route)>();
            //targetRouteList.Add((orderNode, dayRoute));
            //// If freq > 1, find order in other route schedules.
            //if (order.freq == 2)
            //{
            //    switch (day)
            //    {
            //        case WorkDay.Mon:

            //    }
            //    // if Mon -> find Thu order
            //    // if Thu -> find Mon order
            //    // if Tue -> find Fri order
            //    // if Fri -> find Tue order
            //}
            //if (order.freq == 3)
            //{
            //    // if Mon -> find Wed & Fri order
            //    // if Wed -> find Mon & Fri order
            //    // if Fri -> find Mon & Wed order
            //}
            //if (order.freq == 4)
            //{
            //    // find order from all other days
            //}

            // TODO: Super dirty: dit werkt maar is traag! Bovenstaande is beter
            if (order.freq == 1)
                targetRouteList.Add((orderNode, dayRoute));
            if (order.freq > 1)
                for (int i = 0; i <= 1; i++)
                    for (int j = 0; j < 5; j++)
                        if (trucks[i].schedule.weekRoutes[j].orders.Contains(order))
                            targetRouteList.Add((trucks[i].schedule.weekRoutes[j].orders.Find(order), trucks[i].schedule.weekRoutes[j]));

            // Calculate change in time and see if the order can fit in the schedule.
            float[] timeChanges = new float[order.freq];
            for (int i = 0; i < order.freq; i++)
            {
                timeChanges[i] = Schedule.timeChangeRemoveOrder(targetRouteList[i].Item1);
                if (targetRouteList[i].Item2.timeToComplete + timeChanges[i] > maxDayTime)
                    return;
            }

            // Calculate cost change and see if removing this order is an improvement.
            float totalCostChange = 0;
            for (int i = 0; i < order.freq; i++)
                totalCostChange += Schedule.costChangeRemoveOrder(targetRouteList[i].Item1);

            // Remove order
            if (totalCostChange < 0 || random.NextDouble() <= alpha) 
            {
                for (int i = 0; i < order.freq; i++)
                    targetRouteList[i].Item2.removeOrder(targetRouteList[i].Item1, timeChanges[i]);
            }
        }

        // Try to swap two orders in the current schedules (shift between different routes).
        public void TrySwapOrders(Truck truck, WorkDay day, DoublyNode<Order> routeOrder, Truck truck2, WorkDay day2, DoublyNode<Order> routeOrder2)
        {
            //(Truck truck2, WorkDay day2, DoublyNode<Order> routeOrder2) = PrecomputeMutationValues(); 
            Route dayRoute = truck.schedule.weekRoutes[(int)day],
                dayRoute2 = truck2.schedule.weekRoutes[(int)day2];

            // TODO: Werkt alleen met freq = 1;
            if (routeOrder.value.freq > 1 || routeOrder2.value.freq > 1)
                return;

            if (dayRoute.orders.isHeadOrTail(routeOrder) 
                    || dayRoute2.orders.isHeadOrTail(routeOrder2) 
                    || routeOrder.value == routeOrder2.value) 
                return; // route orders should not be 'start' or 'stort', and no use in swapping with itself

            float timeChange = Schedule.timeChangeSwapOrders(routeOrder, routeOrder2),
                timeChange2 = Schedule.timeChangeSwapOrders(routeOrder2, routeOrder);
            
            if (dayRoute.timeToComplete + timeChange > maxDayTime 
                    || dayRoute2.timeToComplete + timeChange2 > maxDayTime)
                return; // Check if swap fits time-wise

            // Calculate cost change and see if swapping these orders is an improvement.
            float costChange = Schedule.costChangeSwapOrders(routeOrder, routeOrder2),
                costChange2 = Schedule.costChangeSwapOrders(routeOrder2, routeOrder);

            if (costChange + costChange2 < 0) {// If the swap would result in a negative cost, perform it always.
                Route.swapOrders(
                    (dayRoute, routeOrder, timeChange),
                    (dayRoute2, routeOrder2, timeChange2)
                );
                //Console.WriteLine("NORMAL Order swapped! Truck: " + (truck + 1) + ", Day: " + day);
            }
            else if (random.NextDouble() <= alpha)   // If worse, perform the swap with a chance based on 'a' and 'T'
            {
                Route.swapOrders(
                    (dayRoute2, routeOrder2, timeChange2),
                    (dayRoute, routeOrder, timeChange)
                );
                //Console.WriteLine("ALPHA  Order swapped! Truck: " + (truck + 1) + ", Day: " + day);
            }
        }

        // Try to shift orders in their current schedules (swap within the same route!)
        public void TryShiftOrders(Truck truck, WorkDay day, DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2) {
            Route dayRoute = truck.schedule.weekRoutes[(int)day];

            if (dayRoute.orders.isHeadOrTail(routeOrder) 
                    || dayRoute.orders.isHeadOrTail(routeOrder2) 
                    || routeOrder.value == routeOrder2.value) 
                return; // route orders should not be 'start' or 'stort', and no use in shifting with itself


            float timeChange = Schedule.timeChangeShiftOrders(routeOrder, routeOrder2);

            if (dayRoute.timeToComplete + timeChange > maxDayTime)
                return; // Check if shift fits time-wise

            
            float costChange = Schedule.costChangeShiftOrders(routeOrder, routeOrder2);

            if (costChange < 0) {// If the shift would result in a negative cost, perform it always.
                dayRoute.shiftOrders(routeOrder, routeOrder2, dayRoute, timeChange);
                //Console.WriteLine("NORMAL Order shifted! Truck: " + (truck + 1) + ", Day: " + day);
            }
            else if (random.NextDouble() < alpha)   // If worse, perform the shift with a chance based on 'a' and 'T'
            {
                dayRoute.shiftOrders(routeOrder, routeOrder2, dayRoute, timeChange);
                //Console.WriteLine("ALPHA  Order shifted! Truck: " + (truck + 1) + ", Day: " + day);
            }
        }

        
        // Calculates the total cost of a solution by checking all its content.
        // This method is very slow! Use one of the methods for cost changes instead for small changes.
        public float CalcTotalCost()
        {
            float cost = 0.0f;

            // Add cost for missed orders
            foreach (Order order in this.orders) {
                // the amount of pickups missed for a company is determined by the order's freq:
                //      freq == if all pickups done then 0 else >0
                cost += 3 * order.freq * order.emptyDur;
            }

            // Add cost for time on the road
            foreach (Truck truck in this.trucks) {
                cost += truck.schedule.timeCost();
            }

            return cost;
        }
    
    }



}
