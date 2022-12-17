using System;
using System.Collections.Generic;
using System.Text;

// TODOs:
// - Afvalvolumes tellen nog niet mee

namespace Infoopt
{

    // The schedule is responsible for holding the different routes of the week,
    // and is meant to be used by the garbage trucks. Also holds static methods,
    // meant for calculating time- and cost changes of route mutations
    class Schedule {

        public Route[] weekRoutes = new Route[5];


        // init each route of the weekschedule by setting its start and stop order, and unloading time
        public Schedule(Order startOrder, Order stopOrder, float truckUnloadTime) {
            foreach(int day in Enum.GetValues(typeof(WorkDay))) {
                this.weekRoutes[day] = new Route(startOrder, stopOrder);
                this.weekRoutes[day].timeToComplete += truckUnloadTime;
            }
        }

        // Display all weekroutes (custom print)
        public string Display() {
            string msg = "";
            int day = 0;
            foreach(Route dayRoute in this.weekRoutes) {
                msg += $"----- {(WorkDay)(day++)}  ({Math.Round(dayRoute.timeToComplete, 1)} sec. | {Math.Round(dayRoute.timeToComplete / 60, 1)} min.) ------\n{dayRoute.Display()}";
            }
            return msg;
        }


        // loop through the week routes and accumulate their time to complete
        public float timeCost() {
            float cost = 0.0f;
            foreach(Route dayRoute in this.weekRoutes) {
                cost += dayRoute.timeToComplete;
            }
            return cost;
        }

        
        // Calculate the time change for adding an order between prev and next.
        public static float timeChangePutBeforeOrder(Order newOrder, DoublyNode<Order> routeOrder)
        {
            Order prev = routeOrder.prev.value,
                current = routeOrder.value; 

            // Time decreases
            float currentDistanceGain = prev.distanceTo(current).travelDur  ;

            // Time increases
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(current).travelDur)  ;
            float pickupTimeCost = newOrder.emptyDur;

            // Calculate change: This number means how much additional time is spend if the order is added
            return (newDistanceCost + pickupTimeCost) - currentDistanceGain;
        }
                
        // Calculate the cost change for adding an order between prev and next.
        public static float costChangePutBeforeOrder(Order newOrder, DoublyNode<Order> routeOrder)
        {
            Order prev = routeOrder.prev.value,
                current = routeOrder.value; 

            // Gains
            float currentDistanceGain = prev.distanceTo(current).travelDur  ;
            float pickupCostGain = newOrder.emptyDur * 3;

            // Costs
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(current).travelDur)  ;
            float pickupTimeCost = newOrder.emptyDur;

            // Calculate change: costs - gains (so a negative result is good!)
            return (newDistanceCost + pickupTimeCost) - (currentDistanceGain + pickupCostGain);
        }

        // Calculate the time change when removing an order between prev and next.
        public static float timeChangeRemoveOrder(DoublyNode<Order> routeOrder)
        {
            Order prev = routeOrder.prev.value;
            Order current = routeOrder.value;
            Order next = routeOrder.next.value;

            // Time decreases
            float pickupTimeGain = current.emptyDur;
            float currentDistanceGain = (prev.distanceTo(current).travelDur + current.distanceTo(next).travelDur)  ;

            // time increases
            float newDistanceCost = prev.distanceTo(next).travelDur  ;

            // Calcuate change: This number means how much time is spend more/less when this order is removed.
            return newDistanceCost - (pickupTimeGain + currentDistanceGain);
        }
        
        // Calculate the cost change for removing an order between prev and next.
        public static float costChangeRemoveOrder(DoublyNode<Order> routeOrder)
        {
            Order prev = routeOrder.prev.value,
                current = routeOrder.value,
                next = routeOrder.next.value;
            
            // Gains
            float currentDistanceGain = (prev.distanceTo(current).travelDur + current.distanceTo(next).travelDur)  ;
            float pickupTimeGain = current.emptyDur;

            // Costs
            float newDistanceCost =  prev.distanceTo(next).travelDur  ;
            float pickupCost = current.emptyDur * 3;

            // Calculate change: costs - gains (so a negative result is good!)
            return newDistanceCost + pickupCost - (currentDistanceGain + pickupTimeGain);
        }

        // Calculate the time change when shifting orders.
        public static float timeChangeSwapOrders(DoublyNode<Order> oldRouteOrder, DoublyNode<Order> newRouteOrder, bool withinRoute)
        {
            Order prev = oldRouteOrder.prev.value,
                oldOrder = oldRouteOrder.value,
                next = oldRouteOrder.next.value,
                newOrder = newRouteOrder.value;

            // Time decreases
            float pickupTimeGain = oldOrder.emptyDur;
            float oldDistanceGain = (prev.distanceTo(oldOrder).travelDur + oldOrder.distanceTo(next).travelDur)  ;

            // Time increases
            float pickupTimeCost = newOrder.emptyDur;
            float newDistanceCost;
            if (newOrder == next)
                newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(oldOrder).travelDur)  ;
            else if (newOrder == prev)
                newDistanceCost = (oldOrder.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur)  ;
            else
                newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur)  ;

            if (withinRoute) // no change in pickup time when swapping orders within the same route
                return newDistanceCost - oldDistanceGain; 
            else 
                return (pickupTimeCost + newDistanceCost) - (pickupTimeGain + oldDistanceGain);
        }

        public static float costChangeSwapOrders(DoublyNode<Order> oldRouteOrder, DoublyNode<Order> newRouteOrder)
        {
            Order prev = oldRouteOrder.prev.value,
                oldOrder = oldRouteOrder.value,
                next = oldRouteOrder.next.value,
                newOrder = newRouteOrder.value;
            
            // Gains
            float oldDistanceGain = (prev.distanceTo(oldOrder).travelDur + oldOrder.distanceTo(next).travelDur)  ;

            // Costs
            float newDistanceCost;
            if (newOrder == next)
                newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(oldOrder).travelDur)  ;
            else if (newOrder == prev)
                newDistanceCost = (oldOrder.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur)  ;
            else
                newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur)  ;

            // Calculate change: costs - gains (so a negative result is good!)
            return newDistanceCost - oldDistanceGain;
        }

    }

}

