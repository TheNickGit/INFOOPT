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


        // Config:
        public static double
            chanceAdd = 0.40,
            chanceRemove = 0.10,
            chanceShift = 0.50,
            alpha = 0.005; // Chance to accept a worse solution


        
        public Order randomOrder() => this.orders[Program.random.Next(orders.Length)];
        public Truck randomTruck() => this.trucks[Program.random.Next(trucks.Length)];
     


        public LocalSearch(Order[] orders) {
            this.orders = orders;
            this.trucks = new Truck[2] { new Truck(), new Truck() };
        }




        // run the model for 'nIterations' cycles
        public void Run(int nIterations) {
            int i = 0;
            while (i <= nIterations) {
                MutateRandomTruckDayRoutes(i++);
            }
        }

        // Generate random variables needed for all iterations (add, remove, shift).
        // returns all intermediate values, not all have to be / will be used
        public ((Truck, WorkDay), DayRoute, RouteTrip, DoublyNode<Order>) PrecomputeMutationValues() {
            Truck truck = randomTruck();
            WorkDay day = WorkDayHelpers.randomDay();
            DayRoute dayRoute = truck.schedule.weekRoutes[(int)day];
            (RouteTrip trip, DoublyNode<Order> tripOrder) = dayRoute.getRandRouteTripOrderNode();
            return ((truck, day), dayRoute, trip, tripOrder);
        }

        // perform one random mutation in a truckschedule route
        public void MutateRandomTruckDayRoutes(int i) {

            ((Truck truck, WorkDay day), DayRoute dayRoute, RouteTrip trip, DoublyNode<Order> tripOrder) = PrecomputeMutationValues(); 

            // TODO: Simulated Annealing toepassen door de hiervoor benodigde variabelen en functionaliteit toe te voegen
           
            // Make a random choice for add, remove or shift depending on the chances given in the config.

            double choice = Program.random.NextDouble();
            if (choice < chanceAdd) {
                TryAddOrder(randomOrder(), dayRoute, trip, tripOrder);
            }
            else if (choice >= chanceAdd && choice < chanceAdd + chanceRemove) {
                TryRemoveOrder(dayRoute, trip, tripOrder);
            }

            
                
            else if (choice >= chanceAdd + chanceRemove && choice < chanceAdd + chanceRemove + chanceShift) {
                ((Truck truck2, WorkDay day2), DayRoute dayRoute2, RouteTrip trip2, DoublyNode<Order> tripOrder2) = PrecomputeMutationValues();
                bool withinTrip = trip == trip2;

                if (withinTrip) 
                    TryShiftOrders(dayRoute, trip, tripOrder, tripOrder2);
                else 
                    TrySwapOrders(
                        (dayRoute, trip, tripOrder), 
                        (dayRoute2, trip2, tripOrder2)
                    );
            }
                
        }



        // Try to add an order into the current dayRoute before a certain routeOrder
        public void TryAddOrder(Order order, DayRoute dayRoute, RouteTrip trip, DoublyNode<Order> tripOrder) {

            if (!order.available)
                return;
            if (order.freq == 1)
                TryAddOrder1(order, dayRoute, trip, tripOrder);
            
            // DEPRECATED FOR THE NEW IMPLEMENTATION OF ROUTE-TRIPS
            /*else if (order.freq == 2)
                TryAddOrder2(order);
            */
        }



        /// <summary>
        /// Try to add an order of frequency 1 to a route.
        /// </summary>
        public void TryAddOrder1(Order order, DayRoute dayRoute, RouteTrip routeTrip, DoublyNode<Order> tripOrder)
        {
            
            // Calculate change in time and see if the order can fit in the schedule.
            float timeChange = Schedule.timeChangePutBeforeOrder(order, tripOrder);
            if (!dayRoute.canAddTimeChange(timeChange))
                return;

            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = Schedule.costChangePutBeforeOrder(order, tripOrder);
            if (costChange < 0) // If adding the order would result in a negative cost, add it always.
                dayRoute.putOrderBeforeInTrip(order, timeChange, routeTrip, tripOrder);
            else if (Program.random.NextDouble() < alpha)   // If worse, add node with a chance based on the simulated annealing variables.
                dayRoute.putOrderBeforeInTrip(order, timeChange, routeTrip, tripOrder);
        }


// COMMENTED OUT FOR THE TIME BEING (CURRENTLY NOT IMPLEMENTED FOR NEW ROUTE TRIPS)
/*
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
*/

        // Try to remove an order from the current schedules.
        public void TryRemoveOrder(DayRoute dayRoute, RouteTrip routeTrip, DoublyNode<Order> tripOrder)
        {
            
            if (tripOrder.value.freq > 1     // TODO: Werkt alleen met freq = 1
                || routeTrip.orders.isHeadOrTail(tripOrder))           
                return;

            // Calculate change in time and see if the order can fit in the schedule.
            float timeChange = Schedule.timeChangeRemoveOrder(tripOrder);
            if (!dayRoute.canAddTimeChange(timeChange))
                return; 

            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = Schedule.costChangeRemoveOrder(tripOrder);
            if (costChange < 0) { // If removing the order would result in a negative cost, always choose to remove it.
                dayRoute.removeOrderInTrip(timeChange, routeTrip, tripOrder);
            }
            else if (Program.random.NextDouble() <= alpha) {  // TODO: If worse, add node with a chance based on 'a' and 'T'
                dayRoute.removeOrderInTrip(timeChange, routeTrip, tripOrder);
            }
        }

        

        // Try to swap two orders in the current schedules (shift between different routes).
        public void TrySwapOrders((DayRoute, RouteTrip, DoublyNode<Order>) o1, (DayRoute, RouteTrip, DoublyNode<Order>) o2)
        {
            (DayRoute dayRoute, RouteTrip routeTrip, DoublyNode<Order> tripOrder) = o1;
            (DayRoute dayRoute2, RouteTrip routeTrip2, DoublyNode<Order> tripOrder2) = o2;
            
            // TODO: Werkt alleen met freq = 1;
            if (tripOrder.value.freq > 1 || tripOrder2.value.freq > 1)
                return;

            if (routeTrip.orders.isHeadOrTail(tripOrder) 
                    || routeTrip2.orders.isHeadOrTail(tripOrder2)
                    || tripOrder.value == tripOrder2.value) 
                return; // trip orders should not be 'start' or 'stort', and no use in swapping with itself

            float timeChange = Schedule.timeChangeSwapOrders(tripOrder, tripOrder2),
                timeChange2 = Schedule.timeChangeSwapOrders(tripOrder2, tripOrder);
            if (!dayRoute.canAddTimeChange(timeChange) || !dayRoute2.canAddTimeChange(timeChange2))
                return; // Check if swap fits time-wise

            // Calculate cost change and see if swapping these orders is an improvement.
            float costChange = Schedule.costChangeSwapOrders(tripOrder, tripOrder2),
                costChange2 = Schedule.costChangeSwapOrders(tripOrder2, tripOrder);

            if (costChange + costChange2 < 0) {   // If the swap would result in a negative cost, perform it always.
                DayRoute.swapOrdersInTrips(
                    (timeChange, routeTrip, tripOrder),
                    (timeChange2, routeTrip2, tripOrder2)
                );
            }
            else if (Program.random.NextDouble() <= alpha) {  // If worse, perform the swap with a chance based on 'a' and 'T'
                DayRoute.swapOrdersInTrips(
                    (timeChange, routeTrip, tripOrder),
                    (timeChange2, routeTrip2, tripOrder2)
                );
            }
        }


        // Try to shift orders in their current schedules (swap within the same route!)
        public void TryShiftOrders(DayRoute dayRoute, RouteTrip routeTrip, DoublyNode<Order> tripOrder, DoublyNode<Order> tripOrder2) {

            if (routeTrip.orders.isHeadOrTail(tripOrder) 
                    || routeTrip.orders.isHeadOrTail(tripOrder2) 
                    || tripOrder.value == tripOrder2.value) 
                return; // trip orders should not be 'start' or 'stort', and no use in shifting with itself


            float timeChange = Schedule.timeChangeShiftOrders(tripOrder, tripOrder2);
            if (!dayRoute.canAddTimeChange(timeChange))
                return; // Check if shift fits time-wise
            
            float costChange = Schedule.costChangeShiftOrders(tripOrder, tripOrder2);
            if (costChange < 0) {       // If the shift would result in a negative cost, perform it always.
                dayRoute.shiftOrdersInTrip(timeChange, routeTrip, tripOrder, tripOrder2);
            }
            else if (Program.random.NextDouble() < alpha) {     // If worse, perform the shift with a chance based on 'a' and 'T'
                dayRoute.shiftOrdersInTrip(timeChange, routeTrip, tripOrder, tripOrder2);
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
