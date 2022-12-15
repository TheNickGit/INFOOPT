﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Infoopt
{
    class Schedule
    {
        public DoublyList<Order>[] weekSchedule;
        public Order[] orders;
        public float[] scheduleTimes = new float[5];

        // Constructor
        public Schedule(Order[] orders)
        {
            weekSchedule = new DoublyList<Order>[5];
            for (int i = 0; i < 5; i++)
            {
                DoublyList<Order> daySchedule = new DoublyList<Order>();
                weekSchedule[i] = daySchedule;
                scheduleTimes[i] = 30; // 30 min is lost every day for emptying the truck at the end
            }

            this.orders = orders;
        }
    }



    //---------------------------------------------------------------------------------------------------------
    // REFACTORING
    //---------------------------------------------------------------------------------------------------------

    class Truck {
        
        public Schedule2 schedule;
        public static float unloadTime = 30.0f;

        public Truck(Order startOrder, Order stopOrder) {
            this.schedule = new Schedule2(startOrder, stopOrder, Truck.unloadTime);
        }

    }


    enum WorkDay {
        Mon, Tue, Wed, Thu, Fri
    }


    class Schedule2 {
        public Route[] weekRoutes = new Route[5];

        public Schedule2(Order startOrder, Order stopOrder, float truckUnloadTime) {
            foreach(int day in Enum.GetValues(typeof(WorkDay))) {
                this.weekRoutes[day] = new Route(startOrder, stopOrder);
                this.weekRoutes[day].timeToComplete += truckUnloadTime;
            }
        }

        public float timeCost() {
            float cost = 0.0f;
            foreach(Route dayRoute in this.weekRoutes) {
                cost += dayRoute.timeToComplete;
            }
            return cost;
        }
    }


    class Route {

        public DoublyList<Order> orders;
        public float timeToComplete = 0;

        public Route(Order startOrder, Order stopOrder) {
            this.orders = new DoublyList<Order>(
                new DoublyNode<Order>(startOrder),
                new DoublyNode<Order>(stopOrder)
            );
        }

        public void putOrderBefore(Order order, DoublyNode<Order> routeOrder, float dt) {
            this.orders.insertBeforeNode(order, routeOrder);    // put order before a order already in the route
            this.timeToComplete += dt;                          // modify total route time to complete 
            order.decreaseFrequency();                          // signal that order has been placed into a route; one less pickup to do
        }

        public void removeOrder(DoublyNode<Order> routeOrder, float dt) {
            this.orders.ejectAfterNode(routeOrder.prev);        // remove order from route
            this.timeToComplete += dt;                          // modify total route time to complete
            routeOrder.value.increaseFrequency();               // signal that order has been removed from a route; one more pickup to do again
        }


        // Calculate the time change for adding an order between prev and next.
        public static float timeChangeCalcPutOrderBefore(Order newOrder, DoublyNode<Order> routeOrder)
        {
            Order prev = routeOrder.prev.value;
            Order next = routeOrder.value; 

            // Time decreases
            float currentDistanceGain = prev.distanceTo(next).travelDur / 60;

            // Time increases
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur) / 60;
            float pickupTimeCost = newOrder.emptyDur;

            // Calculate change: This number means how much additional time is spend if the order is added
            return (newDistanceCost + pickupTimeCost) - currentDistanceGain;
        }
                
        // Calculate the cost change for adding an order between prev and next.
        public static float costChangeCalcPutOrderBefore(Order newOrder, DoublyNode<Order> routeOrder)
        {
            Order prev = routeOrder.prev.value,
                next = routeOrder.value; 

            // Gains
            float currentDistanceGain = prev.distanceTo(next).travelDur / 60;
            float pickupCostGain = newOrder.emptyDur * 3;

            // Costs
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur) / 60;
            float pickupTimeCost = newOrder.emptyDur;

            // Calculate change: costs - gains (so a negative result is good!)
            return (newDistanceCost + pickupTimeCost) - (currentDistanceGain + pickupCostGain);
        }

        // Calculate the time change when removing an order between prev and next.
        public static float timeChangeCalcRemoveOrder(DoublyNode<Order> routeOrder)
        {
            Order prev = routeOrder.prev.value;
            Order current = routeOrder.value;
            Order next = routeOrder.next.value;

            // Time decreases
            float pickupTimeGain = current.emptyDur;
            float currentDistanceGain = (prev.distanceTo(current).travelDur + current.distanceTo(next).travelDur) / 60;

            // time increases
            float newDistanceCost = prev.distanceTo(next).travelDur / 60;

            // Calcuate change: This number means how much time is spend more/less when this order is removed.
            return newDistanceCost - (pickupTimeGain + currentDistanceGain);
        }

        // Calculate the cost change for removing an order between prev and next.
        public static float costChangeCalcRemoveOrder(DoublyNode<Order> routeOrder)
        {
            Order prev = routeOrder.prev.value,
                current = routeOrder.value,
                next = routeOrder.next.value;
            
            // Gains
            float currentDistanceGain = (prev.distanceTo(current).travelDur + current.distanceTo(next).travelDur) / 60;
            float pickupTimeGain = current.emptyDur;

            // Costs
            float newDistanceCost =  prev.distanceTo(next).travelDur / 60;
            float pickupCost = current.emptyDur * 3;

            // Calculate change: costs - gains (so a negative result is good!)
            return newDistanceCost + pickupCost - (currentDistanceGain + pickupTimeGain);
        }
    }

    class LS2 {

        public Order[] orders;
        public Truck[] trucks;

        public static int maxDayTime = 720;


                // Config:
        public static double
            chanceAdd = 0.20,
            chanceRemove = 0.20,
            chanceShift = 0.60, // Doet nog niets
            // chance 'storten'?
            alpha = 0.005; // Chance to accept a worse solution


        Random random = new Random();
        
        public Order randomOrder() => this.orders[random.Next(orders.Length)];
        public Truck randomTruck() => this.trucks[random.Next(trucks.Length)];
        public WorkDay randomDay() {
            WorkDay[] days = (WorkDay[]) Enum.GetValues(typeof(WorkDay));
            return days[random.Next(days.Length)];
        }
        public Route randomTruckDayRoute(Truck truck) => truck.schedule.weekRoutes[(int)randomDay()];
        public DoublyNode<Order> randomTruckDayRouteOrder(Truck truck, WorkDay day) {
            Route route = truck.schedule.weekRoutes[(int)day];
            int randSpot = random.Next(1, route.orders.Length-1); // dont take first or last(dont mutate)
            //Console.WriteLine($"{randSpot} | {route.orders.Length} | {route.orders.head.skipForward(randSpot)} | {route.orders.head.next}");
            return route.orders.head.skipForward(randSpot);
        }


        public LS2(Order[] orders, Order startOrder, Order emtpyingOrder) {
            this.orders = orders;
            this.trucks = new Truck[2] { 
                new Truck(startOrder, emtpyingOrder), 
                new Truck(startOrder, emtpyingOrder) };
        }


        public void Run(int nIterations) {
            int i = 0;
            while (i <= nIterations) {
                MutateRandomTruckDayRoutes(i++);
            }
        }



        public void MutateRandomTruckDayRoutes(int i) {

            (Order order, Truck truck, WorkDay day, DoublyNode<Order> routeOrder) mutation = PrecomputeMutationValues(); 

            // TODO: Simulated Annealing toepassen door de hiervoor benodigde variabelen en functionaliteit toe te voegen
           
            // Make a random choice for add, remove or shift depending on the chances given in the config.
            //Console.WriteLine(mutation.ToString());

            double choice = random.NextDouble();
            if (choice < chanceAdd) 
                TryAddOrder(mutation.order, mutation.truck, mutation.day, mutation.routeOrder);
            else if (choice >= chanceAdd && choice < chanceAdd + chanceRemove)
                TryRemoveOrder(mutation.truck, mutation.day, mutation.routeOrder);
            else
                TryShiftOrder();

        }

        // Generate random variables and do the other calculations needed for all iterations (add, remove, shift).
        public (Order, Truck, WorkDay, DoublyNode<Order>) PrecomputeMutationValues() {
            Truck truck = randomTruck();
            WorkDay day = randomDay();
            return (
                randomOrder(),
                truck, 
                day,
                randomTruckDayRouteOrder(truck, day)
            );
        }

        // Try to add an order into the current dayRoute before a certain routeOrder
        public void TryAddOrder(Order order, Truck truck, WorkDay day, DoublyNode<Order> routeOrder) {
            
            Route dayRoute = truck.schedule.weekRoutes[(int)day];
            Console.WriteLine("ADD");

            if (!(order.freq > 0))  
                return;     // can only add order to route if it still has pickups left this week

            if (dayRoute.timeToComplete >= maxDayTime || dayRoute.timeToComplete + order.emptyDur > maxDayTime)
                return;     // return early if the order can never fit into this schedule.

            float timeChange = Route.timeChangeCalcPutOrderBefore(order, routeOrder);
            if (dayRoute.timeToComplete + timeChange > maxDayTime)
                return;     // Calculate change in time and see if the order can fit in the schedule.


            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = Route.costChangeCalcPutOrderBefore(order, routeOrder);
            if (costChange < 0) {   // If adding the order would result in a negative cost, add it always
                dayRoute.putOrderBefore(order, routeOrder, timeChange);
                Console.WriteLine("NORMAL Order added! Truck: " + ((Array.IndexOf(this.trucks, truck))+1) + ", Day: " + day);
            }
            else if (random.NextDouble() < alpha) { // If worse, add node with a chance based on 'a' and 'T'
                dayRoute.putOrderBefore(order, routeOrder, timeChange);
                Console.WriteLine("ALPHA  Order added! Truck: " + ((Array.IndexOf(this.trucks, truck))+1) + ", Day: " + day);
            }
        }


        // Try to remove an order from the current schedules.
        public void TryRemoveOrder(Truck truck, WorkDay day, DoublyNode<Order> routeOrder)
        {
            Route dayRoute = truck.schedule.weekRoutes[(int)day];
            Console.WriteLine("REMOVE");
            if (dayRoute.orders.Length < 3)
                return; // can only remove if three orders present (including start and stop order)
            
            
            
            float timeChange = Route.timeChangeCalcRemoveOrder(routeOrder);
            if (dayRoute.timeToComplete + timeChange > maxDayTime)
                return; // Calculate change in time and see if the order can fit in the schedule.


            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = Route.costChangeCalcRemoveOrder(routeOrder);
            if (costChange < 0) // If removing the order would result in a negative cost, always choose to remove it.
            {
                dayRoute.removeOrder(routeOrder, timeChange);
                Console.WriteLine("NORMAL Order removed! Truck: " + ((Array.IndexOf(this.trucks, truck))+1) + ", Day: " + day);
            }
            else if (random.NextDouble() < alpha)  // TODO: If worse, add node with a chance based on 'a' and 'T'
            {
                dayRoute.removeOrder(routeOrder, timeChange);
                Console.WriteLine("ALPHA  Order removed! Truck: " + ((Array.IndexOf(this.trucks, truck))+1) + ", Day: " + day);
            }
        }

        // Try to shift two orders in the current schedules.
        public void TryShiftOrder()
        {
            Console.WriteLine("SHIFT");
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

// TODOs:
// - Afvalvolumes tellen nog niet mee