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
            chanceAdd = 0.30,
            chanceRemove = 0.10,
            chanceShift = 0.60,
            alpha = 0.99,
            T = 0.05; //chance parameter
           

        
        public Order randomOrder() => this.orders[Program.random.Next(orders.Length)];
        public Truck randomTruck() => this.trucks[Program.random.Next(trucks.Length)];
     


        public LocalSearch(Order[] orders) {
            this.orders = orders;
            this.trucks = new Truck[2] { new Truck(), new Truck() };
        }




        // run the model for 'nIterations' cycles
        public void Run(int nIterations) {
            int i = 0;
            int n = 1;
            while (i <= nIterations)
            {
                MutateRandomTruckDayRoutes(i++);
                if (i == n * 10000)
                {
                    T = alpha * T;
                    n = n + 1;
                }
            }          
        }

        // Generate random variables needed for all iterations (add, remove, shift).
        // returns all intermediate values, not all have to be / will be used
        public ((Truck, WorkDay), DayRoute, RouteTrip, DoublyNode<Order>) PrecomputeMutationValues(WorkDay? onDay=null) {
            Truck truck = randomTruck();
            WorkDay day = onDay ?? WorkDayHelpers.randomDay();
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

            if (choice < chanceAdd) // Chance Add
            {
                // Take a random order and check if it's available to be added.
                Order order = randomOrder();
                if (!order.available)
                    return;
                TryAddOrder(randomOrder(), dayRoute, trip, tripOrder);
            }  

            else if (choice >= chanceAdd && choice < chanceAdd + chanceRemove) 
            {
                TryRemoveOrder(dayRoute, trip, tripOrder);
            }

            else if (choice >= chanceAdd + chanceRemove && choice < chanceAdd + chanceRemove + chanceShift) 
            {
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
                TryAddOrderSingleFreq(order, dayRoute, trip, tripOrder);
            else 
                TryAddOrderMultipleFreq(order);

        }

        public void TryRemoveOrder(DayRoute dayRoute, RouteTrip routeTrip, DoublyNode<Order> tripOrder) {
            if (tripOrder.value.freq == 1)
                TryRemoveOrderSingleFreq(dayRoute, routeTrip, tripOrder);
            else 
                TryRemoveOrderSingleFreq(dayRoute, routeTrip, tripOrder);
        }


        public void TryAddOrderSingleFreq(Order order, DayRoute dayRoute, RouteTrip routeTrip, DoublyNode<Order> tripOrder)
        {
            // Calculate change in time and see if the order can fit in the schedule.
            float timeChange = Schedule.timeChangePutBeforeOrder(order, tripOrder);
            if (!dayRoute.canAddTimeChange(timeChange))
                return;

            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = Schedule.costChangePutBeforeOrder(order, tripOrder);
            double p = Math.Exp(costChange / T);

            if (costChange < 0 || Program.random.NextDouble() < T)          // NOTE: HOORT HIER GEEN 'p' TE STAAN IPV 'T' ? 
                dayRoute.putOrderBeforeInTrip(order, timeChange, routeTrip, tripOrder);
        }

        /// <summary>
        /// Try to add an order to a route, keeping its frequency in mind.
        /// </summary>
        public void TryAddOrderMultipleFreq(Order order)
        {

            // Generate the days the order can be placed on, depending on frequency.
            WorkDay[] days;
            if (order.freq == 2) days = WorkDayHelpers.randomDay2();
            if (order.freq == 3) days = WorkDayHelpers.randomDay3();
            if (order.freq == 4) days = WorkDayHelpers.randomDay4();


            // Generate random spots on these days to potentially add the order to.
            (DoublyNode<Order>, DayRoute, RouteTrip)[] targetList = new (DoublyNode<Order>, DayRoute, RouteTrip)[order.freq];
            for (int i = 0; i < order.freq; i++)
            {
                ((Truck, WorkDay), DayRoute, RouteTrip, DoublyNode<Order>) mut = PrecomputeMutationValues();
                targetList[i] = (mut.Item4, mut.Item2, mut.Item3);
            }

            // Calculate change in time and see if the order can fit in the schedule.
            float[] timeChanges = new float[order.freq];
            for (int i = 0; i < order.freq; i++)
            {
                timeChanges[i] = Schedule.timeChangePutBeforeOrder(order, targetList[i].Item1);
                if (!targetList[i].Item2.canAddTimeChange(timeChanges[i]))
                    return;
            }

            // Calculate cost change and see if adding this order here is an improvement.
            float totalCostChange = 0;
            double p;
            for (int i = 0; i < order.freq; i++)
                totalCostChange += Schedule.costChangePutBeforeOrder(order, targetList[i].Item1);
            p = Math.Exp(totalCostChange / T);

            // Add the order if cost is negative or with a certain chance
            if (totalCostChange < 0 || Program.random.NextDouble() < T)     // NOTE: HOORT HIER GEEN 'p' TE STAAN IPV 'T' ?
            {
                for(int i = 0; i < order.freq; i++) {
                    // TODO: 'putORderBeforeInTrip' voert nog een volume-capacity check uit en kan daardoor rejecten om order toe te voegen
                    // waardoor orders niet meer gesynchroniseerd geadd worden
                    targetList[i].Item2.putOrderBeforeInTrip(order, timeChanges[i], targetList[i].Item3, targetList[i].Item1);
                }
            }
        }


        // Try to remove an order from the current schedules.
        public void TryRemoveOrderSingleFreq(DayRoute dayRoute, RouteTrip routeTrip, DoublyNode<Order> tripOrder)
        {
            if (routeTrip.orders.isHeadOrTail(tripOrder))           
                return; // dont remove start op emptying order

            // Calculate change in time and see if the order can fit in the schedule.
            float timeChange = Schedule.timeChangeRemoveOrder(tripOrder);
            if (!dayRoute.canAddTimeChange(timeChange))
                return; 

            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = Schedule.costChangeRemoveOrder(tripOrder);
            if (costChange < 0 || Program.random.NextDouble() <= T ) {     // NOTE: HOORT HIER GEEN 'p' TE STAAN IPV 'T' ?
                dayRoute.removeOrderInTrip(timeChange, routeTrip, tripOrder);
            }
        }

        /// <summary>
        /// Force add an order into a route - mainly for testing purposes. This method will break the solution when uses for normal iterations!
        /// </summary>
        public void ForceAddOrder(Order order, DayRoute dayRoute, RouteTrip routeTrip, DoublyNode<Order> tripOrder)
        {
            float timeChange = Schedule.timeChangePutBeforeOrder(order, tripOrder);
            float costChange = Schedule.costChangePutBeforeOrder(order, tripOrder);
            if (costChange < 0 || Program.random.NextDouble() < T)  // NOTE: HOORT HIER GEEN 'p' TE STAAN IPV 'T' ?
            {
                // TODO: 'putORderBeforeInTrip' voert nog een volume-capacity check uit en kan daardoor rejecten om order toe te voegen
                // waardoor orders niet geforceed worden met toevoegen bij een te volle trip garbage volume
                dayRoute.putOrderBeforeInTrip(order, timeChange, routeTrip, tripOrder);
            }
        }

        /// <summary>
        /// Try to remove an order from the current schedules.
        /// </summary>
        public void TryRemoveOrderMultipleFreq(DayRoute dayRoute, RouteTrip routeTrip, DoublyNode<Order> tripOrder)
        {

            List<(DoublyNode<Order>, DayRoute, RouteTrip)> targetList = new List<(DoublyNode<Order>, DayRoute, RouteTrip)>();
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
            foreach(Truck truck in trucks) {
                foreach(DayRoute route in truck.schedule.weekRoutes) {
                    foreach(RouteTrip trip in route.trips) {
                        DoublyNode<Order> otherTripOrder = trip.orders.Find(tripOrder.value);
                        targetList.Add((otherTripOrder, route, trip));
                    }
                }
            }


            // Calculate change in time and see if the order can fit in the schedule.
            float[] timeChanges = new float[tripOrder.value.freq];
            for (int i = 0; i < tripOrder.value.freq; i++)
            {
                timeChanges[i] = Schedule.timeChangeRemoveOrder(targetList[i].Item1);
                if(!targetList[i].Item2.canAddTimeChange(timeChanges[i]))
                    return;
            }

            // Calculate cost change and see if removing this order is an improvement.
            float totalCostChange = 0;
            for (int i = 0; i < tripOrder.value.freq; i++)
                totalCostChange += Schedule.costChangeRemoveOrder(targetList[i].Item1);

            // Remove order
            if (totalCostChange < 0 || Program.random.NextDouble() < T) 
            {
                for (int i = 0; i < tripOrder.value.freq; i++) {
                    targetList[i].Item2.removeOrderInTrip(timeChanges[i], targetList[i].Item3, targetList[i].Item1);
                }
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
            if (costChange + costChange2 < 0 || Program.random.NextDouble() < T) { 
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
            if (costChange < 0 || Program.random.NextDouble() < T) {    
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
