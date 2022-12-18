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
            chanceAdd = 0.280,
            chanceRemove = 0.220,
            chanceShift = 0.50,
            alpha = 0.005; // Chance to accept a worse solution


        public Random random = new Random();
        
        public Order randomOrder() => this.orders[random.Next(orders.Length)];
        public Truck randomTruck() => this.trucks[random.Next(trucks.Length)];
        public WorkDay randomDay() {
            WorkDay[] days = (WorkDay[]) Enum.GetValues(typeof(WorkDay));
            return days[random.Next(days.Length)];
        }

        public (WorkDay, WorkDay) randomDay2()
        {
            WorkDay[] days = (WorkDay[])Enum.GetValues(typeof(WorkDay));
            WorkDay day1 = days[random.Next(2)]; // Mon or Tue
            WorkDay day2;
            if (day1 == WorkDay.Mon)
                day2 = WorkDay.Thu; // Mon + Thu combo
            else
                day2 = WorkDay.Fri; // Tue + Fri combo
            return (day1, day2);
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
            if (choice < chanceAdd)
                TryAddOrder(randomOrder(), truck, day, routeOrder);
            else if (choice >= chanceAdd && choice < chanceAdd + chanceRemove)
                TryRemoveOrder(truck, day, routeOrder);
            else if (choice >= chanceAdd + chanceRemove && choice < chanceAdd + chanceRemove + chanceShift) {
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

        // Try to add an order into the current dayRoute before a certain routeOrder
        public void TryAddOrder(Order order, Truck truck, WorkDay day,  DoublyNode<Order> routeOrder) {
            
            Order order_ = randomOrder(); // order to try add
            if (!order_.available)
                return;
            if (order_.freq == 1)
                TryAddOrder1(order_);
            else if (order.freq == 2)
                TryAddOrder2(order_);
        }

        /// <summary>
        /// Try to add an order of frequency 1 to a route.
        /// </summary>
        public void TryAddOrder1(Order order)
        {
            Truck truck = randomTruck();
            WorkDay day = randomDay();
            Route dayRoute = truck.schedule.weekRoutes[(int)day];
            DoublyNode<Order> target = randomTruckDayRouteOrder(truck, day);  
            
            // Calculate change in time and see if the order can fit in the schedule.
            float timeChange = Schedule.timeChangePutBeforeOrder(order, target);
            if (dayRoute.timeToComplete + timeChange > maxDayTime)
                return;

        
            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = Schedule.costChangePutBeforeOrder(order, target);
            if (costChange < 0) // If adding the order would result in a negative cost, add it always.
                dayRoute.putOrderBefore(order, target, timeChange);
            else if (random.NextDouble() < alpha)   // If worse, add node with a chance based on the simulated annealing variables.
                dayRoute.putOrderBefore(order, target, timeChange);
        }

        /// <summary>
        /// Try to add an order of frequency 2 to a route.
        /// </summary>
        public void TryAddOrder2(Order order)
        {
            Truck truck1 = randomTruck();
            Truck truck2 = randomTruck();
            (WorkDay, WorkDay) days = randomDay2();
            WorkDay day1 = days.Item1;
            WorkDay day2 = days.Item2;
            Route dayRoute1 = truck1.schedule.weekRoutes[(int)day1];
            DoublyNode<Order> target1 = randomTruckDayRouteOrder(truck1, day1);
            Route dayRoute2 = truck2.schedule.weekRoutes[(int)day2];
            DoublyNode<Order> target2 = randomTruckDayRouteOrder(truck2, day2);

            // Calculate change in time and see if the order can fit in the schedule.
            float timeChange1 = Schedule.timeChangePutBeforeOrder(order, target1);
            float timeChange2 = Schedule.timeChangePutBeforeOrder(order, target2);
            if (dayRoute1.timeToComplete + timeChange1 > maxDayTime || dayRoute2.timeToComplete + timeChange2 > maxDayTime)
                return;

            // Calculate cost change and see if adding this order here is an improvement.
            float costChange1 = Schedule.costChangePutBeforeOrder(order, target1);
            float costChange2 = Schedule.costChangePutBeforeOrder(order, target2);
            if (costChange1 < 0 && costChange2 < 0) // If adding the order would result in a negative cost, add it always.
            {
                dayRoute1.putOrderBefore(order, target1, timeChange1);
                dayRoute2.putOrderBefore(order, target2, timeChange2);
            }
            else if (random.NextDouble() < alpha)   // If worse, add node with a chance based on the simulated annealing variables.
            {
                dayRoute1.putOrderBefore(order, target1, timeChange1);
                dayRoute2.putOrderBefore(order, target2, timeChange2);
            }
        }


        // Try to remove an order from the current schedules.
        public void TryRemoveOrder(Truck truck, WorkDay day, DoublyNode<Order> routeOrder)
        {
            // TODO: Werkt alleen met freq = 1
            if (routeOrder.value.freq > 1)
                return;

            Route dayRoute = truck.schedule.weekRoutes[(int)day];

            if (dayRoute.orders.Length <= 3)
                return; // can only remove if three orders present (including start and stop order)
    
            float timeChange = Schedule.timeChangeRemoveOrder(routeOrder);
            if (dayRoute.timeToComplete + timeChange > maxDayTime)
                return; // Calculate change in time and see if the order can fit in the schedule.

            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = Schedule.costChangeRemoveOrder(routeOrder);
            if (costChange < 0) // If removing the order would result in a negative cost, always choose to remove it.
            {
                dayRoute.removeOrder(routeOrder, timeChange);
                //Console.WriteLine("NORMAL Order removed! Truck: " + ((Array.IndexOf(this.trucks, truck))+1) + ", Day: " + day);
            }
            else if (random.NextDouble() <= alpha)  // TODO: If worse, add node with a chance based on 'a' and 'T'
            {
                dayRoute.removeOrder(routeOrder, timeChange);
                //Console.WriteLine("ALPHA  Order removed! Truck: " + ((Array.IndexOf(this.trucks, truck))+1) + ", Day: " + day);
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
