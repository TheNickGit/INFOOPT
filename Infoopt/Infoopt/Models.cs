using System;
using System.Collections.Generic;
using System.Text;

namespace Infoopt
{
    
    // Truck is responsible for holding a schedule of weekly routes for orders to pick up
    class Truck {
        
        public Schedule schedule;
        public static float unloadTime = 1800f;

        public Truck(Order startOrder, Order stopOrder) {
            this.schedule = new Schedule(startOrder, stopOrder, Truck.unloadTime);
        }


    }


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



    enum WorkDay {
        Mon, Tue, Wed, Thu, Fri
    }



    // The route is repsonsible for holding the sequential list of orders,
    // and keepint track of the total time for the route to complete
    class Route {

        public DoublyList<Order> orders;
        public float timeToComplete = 0.0f;

        public Route(Order startOrder, Order stopOrder) {
            this.orders = new DoublyList<Order>(
                new DoublyNode<Order>(startOrder),
                new DoublyNode<Order>(stopOrder)
            );
        }

        // Display all route orders (custom print)
        public string Display() {
            string msg = "";
            foreach(DoublyNode<Order> order in this.orders) {
                msg += $"{order.value.Display()}\n";
            }
            return msg;
        }


        // puts an order before another order in this route (with time change dt)
        public void putOrderBefore(Order order, DoublyNode<Order> routeOrder, float dt) {
            this.orders.insertBeforeNode(order, routeOrder);    // put order before a order already in the route
            this.timeToComplete += dt;                          // modify total route time to complete 
            order.decreaseFrequency();                          // signal that order has been placed into a route; one less pickup to do
        }

         // removes an order in this route (with time change dt)
        public void removeOrder(DoublyNode<Order> routeOrder, float dt) {
            this.orders.ejectAfterNode(routeOrder.prev);        // remove order from route
            this.timeToComplete += dt;                          // modify total route time to complete
            routeOrder.value.increaseFrequency();               // signal that order has been removed from a route; one more pickup to do again
        }

        // swaps two orders not within same route; otherwise shiftOrders
        public static void swapOrders(
                (Route route, DoublyNode<Order> routeOrder, float dt) o,
                (Route route, DoublyNode<Order> routeOrder, float dt) o2
            ) 
        {
            DoublyList<Order>.swapNodes(o.routeOrder, o2.routeOrder);   // swap values of nodes (orders)
            o.route.timeToComplete += o.dt;                             // modify total route time to complete
            o2.route.timeToComplete += o2.dt;                           // modify total route2 time to complete
        }

        // shifts two orders within same route
        public void shiftOrders(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2, Route route, float dt) 
        {
            DoublyList<Order>.swapNodes(routeOrder, routeOrder2);    // swap values of nodes (orders)
            route.timeToComplete += dt;                             // modify total route time to complete
        }
    }


    // Describes a placed order for garbage pickup (as defined in ./data/Orderbestand.csv)
    // and manages/contains the distance between orders (as defined in ./data/AfstandenMatrix.csv)
    class Order
    {

        public string place;            // place name of order
        public int nr,                  // trivial order id
            freq,                       // weekly frequency of order
            binAmt,                     // amount of bins
            binVol,                     // volume of bins
            distId,                     // id of distance of order in distance-matrix (NOTE, DIFFERENT ORDERS MIGHT HAVE SAME DISTANCE ID ENTRY)
            spot;                       // TODO: Deze is er alleen even als tussentijdse oplossing om orders te koppelen aan hun plek in de orders array
        public float emptyDur;          // time it takes to empty the bins of this order
        public (int X, int Y) coord;    // coordinates of order
        public bool available;          // signals whether the order is currently available to be taken


        // distances to all other orders (index = distId of other order)
        public (int dist, int travelDur)[] distancesToOthers;

        // instead use this helper method to get distance-data from one order to another order
        public (int dist, int travelDur) distanceTo(Order other) {
            if (this.distId == other.distId) return (0, 0);
            return this.distancesToOthers[other.distId];
        }
            


        public Order(
                int nr, string place, int freq,
                int binAmt, int binVol, float emptyDur,
                int distId, int xCoord, int yCoord
            )
        {
            this.nr = nr;
            this.place = place;
            this.freq = freq;
            this.binAmt = binAmt;
            this.binVol = binVol;
            this.emptyDur = emptyDur * 60;
            this.distId = distId;
            this.coord = (xCoord, yCoord);
            this.available = true;

        }

        // Display order specification (custom print)
        public string Display()
        {
            return String.Format("{0} | {1} [freq:{2}, amt:{3}, vol:{4}]",
                this.distId.ToString().PadLeft(4),
                this.place.PadRight(24),
                this.freq.ToString().PadLeft(2),
                this.binAmt.ToString().PadLeft(2),
                this.binVol.ToString().PadLeft(4)
            );
        }

        // decrease the order frequency by one (due to the order having been placed into a route)
        public void decreaseFrequency() {
            //this.freq -= 1;
            this.available = false;
        }

        // increase the order frequency by one (due to the order having been removed from a route)
        public void increaseFrequency() {
            //this.freq += 1;
            this.available = true;
        }
    }

}
