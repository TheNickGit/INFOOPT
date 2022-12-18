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
            return (newDistanceCost + pickupCost) - (currentDistanceGain + pickupTimeGain);
        }

        // Calculate the time change when swapping orders.
        public static float timeChangeSwapOrders(DoublyNode<Order> oldRouteOrder, DoublyNode<Order> newRouteOrder)
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
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur)  ;


            return (pickupTimeCost + newDistanceCost) - (pickupTimeGain + oldDistanceGain);
        }

        public static float timeChangeShiftOrders(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2) {
            Order order = routeOrder.value,
                order2 = routeOrder2.value;

            bool secondFollowsFirst = routeOrder.next.value == order2;
            bool firstFollowsSecond = routeOrder.prev.value == order2;
            bool areFollowUps = secondFollowsFirst || firstFollowsSecond;

            if (!areFollowUps) // if not following each other up in same route, time change of shift is equal to time change of swap for both
                return Schedule.timeChangeSwapOrders(routeOrder, routeOrder2) + Schedule.timeChangeSwapOrders(routeOrder2, routeOrder);

            // in same route order shift, order empty durations are disregarded due to having no effect in time change
            float oldTime, newTime; 
            if (secondFollowsFirst) { 
                oldTime = routeOrder.prev.value.distanceTo(order).travelDur + order.distanceTo(order2).travelDur + order2.distanceTo(routeOrder2.next.value).travelDur;
                newTime = routeOrder.prev.value.distanceTo(order2).travelDur + order2.distanceTo(order).travelDur + order.distanceTo(routeOrder2.next.value).travelDur;
                return (newTime - oldTime); 
            }
            else {
                oldTime = routeOrder2.prev.value.distanceTo(order2).travelDur + order2.distanceTo(order).travelDur + order.distanceTo(routeOrder.next.value).travelDur;
                newTime = routeOrder2.prev.value.distanceTo(order).travelDur + order.distanceTo(order2).travelDur + order2.distanceTo(routeOrder.next.value).travelDur;
                return (newTime - oldTime); 
            }
            
        }

        public static float costChangeSwapOrders(DoublyNode<Order> oldRouteOrder, DoublyNode<Order> newRouteOrder)
        {
            Order prev = oldRouteOrder.prev.value,
                oldOrder = oldRouteOrder.value,
                next = oldRouteOrder.next.value,
                newOrder = newRouteOrder.value;
            
            // Gains
            float oldDistanceGain = (prev.distanceTo(oldOrder).travelDur + oldOrder.distanceTo(next).travelDur)  ;
            float pickuptTimeGain = oldOrder.emptyDur;

            // Costs
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur)  ;
            float pickupTimeCost = newOrder.emptyDur;

            // Calculate change: costs - gains (so a negative result is good!)
            return (newDistanceCost + pickupTimeCost) - (oldDistanceGain + pickuptTimeGain);
        }

        // cost change of shifting orders equals time change, because emptying-duration is indifferent in same route order shift 
        public static float costChangeShiftOrders(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2) 
        {
            return timeChangeShiftOrders(routeOrder, routeOrder2);
        }


    }

}

