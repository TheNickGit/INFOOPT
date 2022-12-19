﻿using System;
using System.Diagnostics;
using System.Linq;

namespace Infoopt {

    class Program {

        // Config:
        static int totalIterations = 5_000_000;

        public static Random random = new Random();


        static void Main(string[] args)
        {
            // parse orders and display them
            string orderFilePath = "./data/Orderbestand.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            string distancesFilePath = "./data/AfstandenMatrix.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            Order[] orders = fetchOrders(orderFilePath, distancesFilePath);


            LocalSearch LS = new LocalSearch(orders);
            
            LS.Run(nIterations: totalIterations);
            printLSCheckerOutput(LS);
            printTruckScheduleDisplays(LS);
            //printTruckScheduleDescriptions(LS);
            //Tests.testLSrouteTripTimes(LS);
            //Tests.testLSrouteTripVolumes(LS);
            
            
            //Tests.TestTimeToCompleteAccumulation(new LocalSearch(orders));

        }

        // print the LS with checker output
        public static void printLSCheckerOutput(LocalSearch LS) {           
            int truckNr = 1;
            foreach(Truck truck in LS.trucks) {
                int day = 1;
                foreach(DayRoute route in truck.schedule.weekRoutes) {
                    int routeNr = 1;
                    foreach(RouteTrip trip in route.trips) {
                        foreach(DoublyNode<Order> node in trip.orders) {
                            if (!trip.orders.isHead(node)) 
                                Console.WriteLine($"{truckNr}; {day}; {routeNr++}; {node.value.nr}");
                        }
                    }
                    day += 1;
                }
                truckNr++;
            }
        }

        // print the display of all orders in the LS truck routes
        public static void printTruckScheduleDisplays(LocalSearch LS)  {
            string msg = String.Join('\n', LS.trucks.Select(
                (truck, i) => $"=========== TRUCK {++i} ===========\n{truck.schedule.Display()}"
            ));
            Console.WriteLine(msg);
        }


        // print the descriptions of all trips in the LS truck routes
        public static void printTruckScheduleDescriptions(LocalSearch LS) {
            string msg = String.Join('\n', LS.trucks.Select(
                (truck, i) => $"=========== TRUCK {++i} ===========\n{truck.schedule.Describe()}"
            ));
            Console.WriteLine(msg);
        }


        // re-calculated the time that a specific route trip takes to complete
        public static float routeTripTime(RouteTrip trip) {
            float t = 0;
            foreach(DoublyNode<Order> node in trip.orders) {
                if (trip.orders.isHead(node))
                    t += node.value.distanceTo(node.next.value).travelDur;
                else if (trip.orders.isTail(node))
                    t += Truck.unloadTime;
                else {
                    t += node.value.distanceTo(node.next.value).travelDur;
                    t += node.value.emptyDur;
                }
            }         
            return t;
        }

        // re-calculate the volume of garbage that is picked up during a route trip
        public static float routeTripVolume(RouteTrip trip) {
            float v = 0;
            foreach(DoublyNode<Order> node in trip.orders) {
                if (trip.orders.isHeadOrTail(node)) continue;
                v += node.value.garbageVolume();
            }         
            return v;
        }




        // get all orders and bind all distance-data to respective orders
        public static Order[] fetchOrders(string orderFilePath, string distancesFilePath) {
            // parse orders and distances
            Order[] orders = Parser.parseOrders(orderFilePath, nOrders:1177);
            (int, int)[][] distances = Parser.parseOrderDistances(distancesFilePath, nDistances:1099);

            // link distances to orders
            DayRoute.startOrder.distancesToOthers = distances[DayRoute.startOrder.distId];
            DayRoute.emptyingOrder.distancesToOthers = distances[DayRoute.emptyingOrder.distId];
            foreach (Order order in orders) 
                order.distancesToOthers = distances[order.distId];

            return orders;
        }


        // For debugging purposes (remove later)
        public static void Assert(bool pred) {
            if (!pred) throw new Exception("ASSERTION FAIL");
        }


    }
    


    class Tests {
        
        public static void TestTimeToCompleteAccumulation(LocalSearch LS) {

            LocalSearch.alpha = 1.0;

            bool allPassed = true;
            for(int i=0; i<100; i++) {
                allPassed &= testAddOrders(LS);
            }
            
            for(int i=0; i<100; i++) {
                allPassed &= testRemoveOrders(LS);
            }
            
            for(int i=0; i<100; i++) {
                allPassed &= testShiftOrders(LS);
            }

            for(int i=0; i<100; i++) {
                
                allPassed &= testSwapOrders(LS);
            }      

            Console.WriteLine($"ALL PASSED: {allPassed}");
            LocalSearch.alpha = 0.05;

        }


        public static bool testAddOrders(LocalSearch LS) {
            Truck truck = LS.trucks[0];
            WorkDay day = 0;
            DayRoute route = truck.schedule.weekRoutes[(int)day] = new DayRoute();
            RouteTrip trip = route.trips[0];

            Order randOrder = LS.randomOrder(),
                randOrder2 = LS.randomOrder();
            while(!(randOrder.freq > 0)) randOrder = LS.randomOrder();
            while(!(randOrder2.freq > 0)) randOrder2 = LS.randomOrder();

            LS.ForceAddOrder(randOrder, route, trip, trip.orders.tail);
            LS.ForceAddOrder(randOrder2, route, trip, trip.orders.tail);
            
            float durToOrder1 = trip.orders.head.value.distanceTo(randOrder).travelDur,
                  durFromOrder1To2 = randOrder.distanceTo(randOrder2).travelDur,
                  durFromOrder2 = randOrder2.distanceTo(trip.orders.tail.value).travelDur;

            float rt = durToOrder1 + randOrder.emptyDur + durFromOrder1To2 + randOrder2.emptyDur + durFromOrder2 + Truck.unloadTime;
            float rtt = Program.routeTripTime(trip);

            //Console.WriteLine("\n##### TIME TEST: ADD ORDERS ######");
            //Console.WriteLine("accum\treal\troute recalc");
            //Console.WriteLine("----------------------------");
            //Console.WriteLine($"{route.timeToComplete} {rt} {routeTripTime}");
            bool passed =Math.Round(route.timeToComplete,2) == Math.Round(rtt,2) || Math.Round(route.timeToComplete,2) == Math.Round(rt,2);
            Console.WriteLine($"PASSED?\t{passed}");

            return passed;
        }


        public static bool testRemoveOrders(LocalSearch LS) {
            Truck truck = LS.trucks[0];
            WorkDay day = 0;
            DayRoute route = truck.schedule.weekRoutes[(int)day] = new DayRoute();
            RouteTrip trip = route.trips[0];

            Order randOrder = LS.randomOrder(),
                randOrder2 = LS.randomOrder();
            while(!(randOrder.freq > 0)) randOrder = LS.randomOrder();
            while(!(randOrder2.freq > 0)) randOrder2 = LS.randomOrder();

            LS.ForceAddOrder(randOrder, route, trip, trip.orders.tail);
            LS.ForceAddOrder(randOrder2, route, trip, trip.orders.tail);

            float rt;
            bool removeSecond = Program.random.NextDouble() <= 0.5;
            if (removeSecond) {
                LS.TryRemoveOrder(route, trip, trip.orders.tail.prev);
                Order mid = trip.orders.head.next.value;
                rt = trip.orders.head.value.distanceTo(mid).travelDur + mid.emptyDur + mid.distanceTo(trip.orders.tail.value).travelDur + Truck.unloadTime;;
            }
            else {
                LS.TryRemoveOrder(route, trip, trip.orders.tail.prev);
                Order mid = trip.orders.tail.prev.value;
                rt = trip.orders.head.value.distanceTo(mid).travelDur + mid.emptyDur + mid.distanceTo(trip.orders.tail.value).travelDur + Truck.unloadTime;
            }
            

            float rtt = Program.routeTripTime(trip);

            //Console.WriteLine("\n##### TIME TEST: REMOVE ORDER ######");
            //Console.WriteLine("accum\treal\troute recalc");
            //Console.WriteLine("----------------------------");
            //Console.WriteLine($"{route.timeToComplete} {rt} {rtt}");
            bool passed =Math.Round(route.timeToComplete,2) == Math.Round(rt,2) || Math.Round(route.timeToComplete,2) == Math.Round(rtt,2);
            Console.WriteLine($"PASSED?\t{passed}");

            return passed;
        }



        public static bool testShiftOrders(LocalSearch LS) {
            Truck truck = LS.trucks[0];
            WorkDay day = 0;
            DayRoute route = truck.schedule.weekRoutes[(int)day] = new DayRoute();
            RouteTrip trip = route.trips[0];

            Order randOrder = LS.randomOrder(),
                randOrder2 = LS.randomOrder();
            while(!(randOrder.freq > 0)) randOrder = LS.randomOrder();
            while(!(randOrder2.freq > 0)) randOrder2 = LS.randomOrder();
            
            LS.ForceAddOrder(randOrder, route, trip, trip.orders.tail);
            LS.ForceAddOrder(randOrder2, route, trip, trip.orders.tail);


            LS.TryShiftOrders(route, trip, trip.orders.head.next, trip.orders.tail.prev);


            float durToOrder2 = trip.orders.head.value.distanceTo(randOrder2).travelDur,
                durFromOrder2To1 = randOrder2.distanceTo(randOrder).travelDur,
                durFromOrder1 = randOrder.distanceTo(trip.orders.tail.value).travelDur;


            float rt = durToOrder2 + randOrder2.emptyDur + durFromOrder2To1 + randOrder.emptyDur + durFromOrder1 + Truck.unloadTime;
            float rtt = Program.routeTripTime(trip);

            //Console.WriteLine("\n##### TIME TEST: SHIFT ORDERS ######");
            //Console.WriteLine("accum\treal\troute recalc");
            //Console.WriteLine("----------------------------");
            //Console.WriteLine($"{route.timeToComplete}\t{rt}\t{rtt}");
            bool passed =Math.Round(route.timeToComplete,2) == Math.Round(rt,2) || Math.Round(route.timeToComplete,2) == Math.Round(rtt,2);
            Console.WriteLine($"PASSED?\t{passed}");

            return passed;
        }

        public static bool testSwapOrders(LocalSearch LS) {
            Truck truck = LS.trucks[0],
                truck2 = LS.trucks[1];
            WorkDay day = WorkDay.Mon,
                day2 = WorkDay.Tue;
            DayRoute route = truck.schedule.weekRoutes[(int)day] = new DayRoute(),
                route2 = truck2.schedule.weekRoutes[(int)day2] = new DayRoute();
            RouteTrip trip = route.trips[0],
                trip2 = route2.trips[0];

            Order randOrder = LS.randomOrder(),
                randOrder2 = LS.randomOrder();
            while(!(randOrder.freq > 0)) randOrder = LS.randomOrder();
            while(!(randOrder2.freq > 0)) randOrder2 = LS.randomOrder();
            
            LS.ForceAddOrder(randOrder, route, trip, trip.orders.tail);
            LS.ForceAddOrder(randOrder2, route2, trip2, trip2.orders.tail);

            LS.TrySwapOrders((route, trip, trip.orders.head.next), (route2, trip2, trip2.orders.tail.prev));

            float routeDur = trip.orders.head.value.distanceTo(randOrder).travelDur + randOrder.emptyDur + randOrder.distanceTo(trip.orders.tail.value).travelDur + Truck.unloadTime,
                route2Dur = trip2.orders.head.value.distanceTo(randOrder2).travelDur + randOrder2.emptyDur + randOrder2.distanceTo(trip2.orders.tail.value).travelDur + Truck.unloadTime;

            float rt = routeDur + route2Dur;
            float rtt = Program.routeTripTime(trip) + Program.routeTripTime(trip2);

            float accum = route.timeToComplete + route2.timeToComplete;

            //Console.WriteLine("\n##### TIME TEST: SWAP ORDERS ######");
            //Console.WriteLine("accum\treal\troute recalc");
            //Console.WriteLine("----------------------------");
            //Console.WriteLine($"{route.timeToComplete}\t{rt}\t{rtt}");
            bool passed = Math.Round(accum,2) == Math.Round(rt,2) || Math.Round(accum,2) == Math.Round(rtt,2);
            Console.WriteLine($"PASSED?\t{passed}");

            return passed;
        }
        
        // test each of the route trips to test for the accumulated trip time vs. recalculated trip time
        public static void testLSrouteTripTimes(LocalSearch LS) {
            foreach(Truck truck in LS.trucks) {
                foreach(DayRoute dayRoute in truck.schedule.weekRoutes) {
                    foreach(RouteTrip trip in dayRoute.trips) {
                        float ttc = trip.timeToComplete,
                            rt = Program.routeTripTime(trip);
                        Console.WriteLine($"Route trip time:\t{Math.Round(ttc,0)} (accum.)\t{Math.Round(rt,0)} (really)\t\t{Math.Round(ttc,0)==Math.Round(rt,0)} (isEqual)");
                    }
                }
            }
        }

        // test each of the route trips to test for the accumulated trip volume vs. recalculated trip volume
        public static void testLSrouteTripVolumes(LocalSearch LS) {
            foreach(Truck truck in LS.trucks) {
                foreach(DayRoute dayRoute in truck.schedule.weekRoutes) { 
                    foreach(RouteTrip trip in dayRoute.trips) {
                        float vpu = trip.volumePickedUp,
                            rv = Program.routeTripVolume(trip);
                        Console.WriteLine($"Route trip volume:\t{Math.Round(vpu,0)} (accum.)\t{Math.Round(rv,0)} (really)\t\t{Math.Round(vpu,0)==Math.Round(rv,0)} (isEqual)");
                    }
                }
            }
        }


    }

}




