using System;
using System.Collections.Generic;
using System.Text;

namespace Infoopt
{
    
    // Truck is responsible for holding a schedule of weekly routes for orders to pick up
    class Truck {
        
        public Schedule schedule;
        public static float unloadTime = 30.0f;

        public Truck(Order startOrder, Order stopOrder) {
            this.schedule = new Schedule(startOrder, stopOrder, Truck.unloadTime);
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

        // swaps two orders, can be within route or between routes (with time change dt for each route)
        public static void shiftOrders(
                (Route route, DoublyNode<Order> routeOrder, float dt) o,
                (Route route, DoublyNode<Order> routeOrder, float dt) o2
            ) 
        {
            DoublyList<Order>.swapNodes(o.routeOrder, o2.routeOrder);   // swap values of nodes (orders)
            o.route.timeToComplete += o.dt;                             // modify total route time to complete
            o2.route.timeToComplete += o2.dt;                           // modify total route2 time to complete
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
