using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infoopt
{
    
    // Truck is responsible for holding a schedule of weekly routes for orders to pick up
    class Truck {
        
        public Schedule schedule;

        public static float unloadTime = 1800.0f;       // seconds
        public static int volumeCapacity =  100_000;    // liters (includes compression)


        public Truck() {
            this.schedule = new Schedule();
        }


    }


    // The schedule is responsible for holding the different routes of the week,
    // and is meant to be used by the garbage trucks. Also holds static methods,
    // meant for calculating time- and cost changes of route mutations
    class Schedule {

        public DayRoute[] weekRoutes = new DayRoute[5];


        // init each route of the weekschedule by setting its start and stop order, and unloading time
        public Schedule() {
            foreach(int day in Enum.GetValues(typeof(WorkDay))) {
                this.weekRoutes[day] = new DayRoute();
            }
        }

        // Display the orders of all weekroutes
        public string Display() 
            => String.Join("\n", this.weekRoutes.Select((dayRoute, day) => {
                    float ttcMinutes = (float)Math.Round(dayRoute.timeToComplete / 60.0f, 1);
                    return $"------ {(WorkDay)(day++)}  ({ttcMinutes} min.) ------\n{dayRoute.Display()}";
                }));

        // Describe the trips of all weekroutes
        public string Describe()
            => String.Join("\n", this.weekRoutes.Select((dayRoute, day) => {
                    float ttcMinutes = (float)Math.Round(dayRoute.timeToComplete / 60.0f, 1);
                    return $"------ {(WorkDay)(day++)}  ({ttcMinutes} min.) ------\n{dayRoute.Describe()}";
                }));


        // loop through the week routes and accumulate their time to complete
        public float timeCost() {
            float cost = 0.0f;
            foreach(DayRoute dayRoute in this.weekRoutes) {
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



    public enum WorkDay {
        Mon, Tue, Wed, Thu, Fri
    }

    public static class WorkDayHelpers {
        public static WorkDay randomDay()
            => (WorkDay)Program.random.Next(Enum.GetValues(typeof(WorkDay)).Length);

        public static (WorkDay, WorkDay) randomDay2()
        {
            WorkDay day1 = Program.random.Next(2) == 1 ? WorkDay.Mon : WorkDay.Tue; // Mon or Tue
            WorkDay day2 = day1 == WorkDay.Mon 
                ? WorkDay.Thu   // Mon + Thu combo
                : WorkDay.Fri;  // Tue + Fri combo
            return (day1, day2);
        }

    }

    class DayRoute {

        public static float maxDayTime = 43200f;

        public static Order
            startOrder = new Order(0, "MAARHEEZE-start", 0, 0, 0, 0, 287, 56343016, 513026712), // The startlocation of each day.
            emptyingOrder = new Order(0, "MAARHEEZE-stort", 0, 0, 0, 30, 287, 56343016, 513026712); // The 'stortplaats'

        public List<RouteTrip> trips;

        public float timeToComplete { get { return this.trips.Select(trip => trip.timeToComplete).Sum(); } }
        public bool canAddTimeChange(float dt) => this.timeToComplete + dt <= DayRoute.maxDayTime;


        // get a random route trip in this dayroute
        public RouteTrip getRandomRouteTrip() {
            //sConsole.WriteLine(this.trips.Count.ToString());
            return this.trips[Program.random.Next(this.trips.Count)];
        }

        // get a random order node from one of this dayroute's trips (also returned)
        public (RouteTrip trip, DoublyNode<Order> tripOrder) getRandRouteTripOrderNode() {
            RouteTrip trip = getRandomRouteTrip();
            return (trip, trip.getRandOrderNode());

        }


        public DayRoute() {
            this.trips = new List<RouteTrip>();
            this.AddTrip();     // a dayroute always consist of at least one trip
        }


        // Display all route trip orders
        public string Display() 
            => String.Join("\n", this.trips.Select(
                (trip, i) => $"--- Route trip {++i}  ({trip.volumePickedUp} L) ---\n{trip.Display()}"
            ));

        // Display all route trips themselves only
        public string Describe() 
            => String.Join("\n", this.trips.Select(
                (trip, i) => $"Route trip {++i}:\t\t {trip.volumePickedUp} L\t {Math.Round(trip.timeToComplete/60.0f,1)} min."
            ));


        
        // add a route trip at the end of the dayroute
        public RouteTrip AddTrip() {
            RouteTrip trip = new RouteTrip(DayRoute.startOrder, DayRoute.emptyingOrder);
            trip.timeToComplete += Truck.unloadTime; // each trip ends with the truck unloading at the emptying-order
            this.trips.Add(trip);
            return trip;
        }

        // add a route trip at the end of the dayroute with the new order (if it fits the time wise)
        public void AddTripWithNewOrder(Order order) {
            RouteTrip newTrip = this.AddTrip();
            float newTimeChange = newTrip.orders.head.value.distanceTo(order).travelDur + order.emptyDur + order.distanceTo(newTrip.orders.tail.value).travelDur;

            Program.Assert(newTrip.canAddVolumeChange(order.garbageVolume()));  // a new order should always fit volume-wise into a new trip
            if(!this.canAddTimeChange(newTimeChange))
                return;

            newTrip.putOrderBefore(order, newTrip.orders.tail, newTimeChange);
        }


        // try whether you can squeeze the order in any of the dayroute trips,
        // by trying to place it before a random chosen trip order.
        public bool tryPutOrderInAnyTrip(Order order) {
            bool orderAdded = false;
            foreach(RouteTrip altTrip in this.trips) {
                if (!altTrip.canAddVolumeChange(order.garbageVolume()))
                    continue;
                DoublyNode<Order> altTripOrder = altTrip.getRandOrderNode();
                float altTimeChange = Schedule.timeChangePutBeforeOrder(order, altTripOrder);
                if (!this.canAddTimeChange(altTimeChange)) continue;
                altTrip.putOrderBefore(order, altTripOrder, altTimeChange);
                orderAdded = true;
                break;
            }
            return orderAdded;
        }


        // tries to add an order to the trip before the specified trip order.
        // if it does not fit, it will try to fit before one random trip order 
        // of each of the trips contained in this day-route. Only if all those don't fit, 
        // it is assumed all the trips are full and a new trip will be tried
        // to be created with the new order put in it (reduces trip amount blowup).
        public void putOrderBeforeInTrip(Order order, float timeChange, RouteTrip trip, DoublyNode<Order> tripOrder) {
            int orderVolume = order.garbageVolume();

            // only put it before trip order if it fits truck-capacity wise, else try other trips in this day-route
            if (trip.canAddVolumeChange(orderVolume))
                trip.putOrderBefore(order, tripOrder, timeChange);

            else {
                // alternatively; try add order before random order in each trip in this dayroute.
                // this makes it possible to add new trips without trip amount being blown up,
                // by first checking (more like guessing) whether all trips in dayroute are full
                // and only then adding a new trip (for the new order)
                if (this.tryPutOrderInAnyTrip(order)) { 
                    // if cannot be added to any existing trip then try adding a new trip
                    this.AddTripWithNewOrder(order);
                }
            }
        }

        // remove order from the trip, removing the trip itself 
        // when no orders in it left (reduces trip amount blowup)
        public void removeOrderInTrip(float timeChange, RouteTrip trip, DoublyNode<Order> tripOrder) {

            // no checks for pickup volume, because order removal decreases picked up volume

            trip.removeOrder(tripOrder, timeChange);

            // if only start and emptying orders left, also remove trip itself 
            if (trip.orders.Length == 2) {  
                
                // never remove if last trip left, even when it is empty
                if (this.trips.Count == 1) return;

                this.trips.Remove(trip);
            };

        }

        // shift two orders in the same trip
        public void shiftOrdersInTrip(float timeChange, RouteTrip trip, DoublyNode<Order> tripOrder, DoublyNode<Order> tripOrder2) { 

            // no checks for pickup volume, because within trip order shifts have no effect on picked up volume

            trip.shiftOrders(tripOrder, tripOrder2, timeChange);
        }


        // swaps two orders in two different trips if it fits volume wise in those trips
        public static void swapOrdersInTrips(
            (float timeChange, RouteTrip routeTrip, DoublyNode<Order> tripOrder) o, 
            (float timeChange, RouteTrip routeTrip, DoublyNode<Order> tripOrder) o2
        ) {

            int vol = o.tripOrder.value.garbageVolume(),
                vol2 = o2.tripOrder.value.garbageVolume();

            int volChange = vol2 - vol,
                volChange2 = vol - vol2;
            
            // Check if swap fits with volume capacity of truck in trips
            if (!o.routeTrip.canAddVolumeChange(volChange)
                || !o2.routeTrip.canAddVolumeChange(volChange2))
                return;

            RouteTrip.swapOrders(o, o2);
            
        }


    }



    // The route trip is repsonsible for holding the sequential list of orders
    // between the startin and the emptying order, to the extent of a day-route 
    // being able to hold multiple route trips. it keeps track of the total time 
    // of the trip to complete, and how much volume of garbage was picked up.
    class RouteTrip {

        public DoublyList<Order> orders;

        public float timeToComplete = 0.0f;
        public int volumePickedUp = 0;

        public bool canAddVolumeChange(float dv) => this.volumePickedUp + dv <= Truck.volumeCapacity;


        public DoublyNode<Order> getRandOrderNode() {
            int i = Program.random.Next(1, this.orders.Length); // dont take the starting order
            return this.orders.head.skipForward(i);
        }


        public RouteTrip(Order startOrder, Order stopOrder) {
            this.orders = new DoublyList<Order>(
                new DoublyNode<Order>(startOrder),
                new DoublyNode<Order>(stopOrder)
            );
        }

        // Display all trip orders
        public string Display() 
            => String.Join('\n', this.orders.toEnumerable().Select(order => order.value.Display()));



        // puts an order before another order in this trip, modifying the time and volume change
        public void putOrderBefore(Order order, DoublyNode<Order> tripOrder, float dt) {
            this.orders.insertBeforeNode(order, tripOrder);    // put order before a order already in the route
            this.volumePickedUp += order.garbageVolume();       // modify volume picked up in route
            this.timeToComplete += dt;                          // modify total route time to complete 
            order.decreaseFrequency();                          
        }

         // removes an order in this trip, modifying the time and volume change
        public void removeOrder(DoublyNode<Order> tripOrder, float dt) {
            this.orders.ejectAfterNode(tripOrder.prev);                // remove order from route
            this.volumePickedUp -= tripOrder.value.garbageVolume();    // modify volume picked up in route
            this.timeToComplete += dt;                                  // modify total route time to complete
            tripOrder.value.increaseFrequency();                                       
        }

        // swaps two orders not within same trip, modifying the time and volume change
        public static void swapOrders(
                (float dt, RouteTrip trip, DoublyNode<Order> tripOrder) o,
                (float dt, RouteTrip trip, DoublyNode<Order> tripOrder) o2
            ) 
        {
            DoublyList<Order>.swapNodes(o.tripOrder, o2.tripOrder);   // swap values of nodes (orders)
            o.trip.timeToComplete += o.dt;                             // modify total route time to complete
            o2.trip.timeToComplete += o2.dt;                           // modify total route2 time to complete

            // modify volume picked up in routes
            int vol = o.tripOrder.value.garbageVolume(),
                vol2 = o2.tripOrder.value.garbageVolume();

            // NOTE: in my head, these 'vol' and 'vol1' volumes need to be changed for both trips
            // however, after debugging seemed to work correctly this way (??)
            o.trip.volumePickedUp += (-vol2 + vol);
            o2.trip.volumePickedUp += (-vol + vol2);
            
        }

        // shifts two orders within same trip, modifying the time change (no effect on volume picked up)
        public void shiftOrders(DoublyNode<Order> tripOrder, DoublyNode<Order> tripOrder2, float dt) 
        {
            DoublyList<Order>.swapNodes(tripOrder, tripOrder2);     // swap values of nodes (orders)
            this.timeToComplete += dt;                              // modify total route time to complete
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
            distId;                     // id of distance of order in distance-matrix (NOTE, DIFFERENT ORDERS MIGHT HAVE SAME DISTANCE ID ENTRY)
            //spot;                       // TODO: Deze is er alleen even als tussentijdse oplossing om orders te koppelen aan hun plek in de orders array
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
            

        public int garbageVolume()
            => this.binAmt * this.binVol; 


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
