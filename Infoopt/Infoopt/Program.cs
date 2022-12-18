using System;
using System.Diagnostics;

namespace Infoopt {

    class Program {

        // Config:
        static int totalIterations = 5_000_000;

        public static Order
            startOrder = new Order(0, "MAARHEEZE-start", 0, 0, 0, 0, 287, 56343016, 513026712), // The startlocation of each day.
            emptyingOrder = new Order(0, "MAARHEEZE-stort", 0, 0, 0, 30, 287, 56343016, 513026712); // The 'stortplaats'

        static void Main(string[] args)
        {
            // parse orders and display them
            string orderFilePath = "./data/Orderbestand.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            string distancesFilePath = "./data/AfstandenMatrix.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            Order[] orders = fetchOrders(orderFilePath, distancesFilePath);


            LocalSearch LS = new LocalSearch(orders, startOrder, emptyingOrder);
            
            LS.Run(nIterations: totalIterations);
            printLSCheckerOutput(LS);
            printLSDisplay(LS);
            //Tests.testLSRouteTimes(LS);
            
            
    	    //LocalSearch LS = new LocalSearch(orders, startOrder, emptyingOrder);
            //Tests.TestTimeToCompleteAccumulation(LS);

        }

        // print the LS with checker output
        public static void printLSCheckerOutput(LocalSearch LS) {           
            int n = 1;
            foreach(Truck truck in LS.trucks) {
                foreach(int day in Enum.GetValues(typeof(WorkDay))) {
                    int j = 0;
                    foreach(DoublyNode<Order> node in truck.schedule.weekRoutes[day].orders) {
                        int orderId = node.value.nr;
                        if (j>0) Console.WriteLine($"{n}; {day+1}; {j}; {orderId}");
                        j++;
                    }
                }
                n++;
            }
        }

        // print the LS with custom output
        public static void printLSDisplay(LocalSearch LS) {
            Console.WriteLine("NEW TOTAL COST:\t" + LS.CalcTotalCost());
            int i = 0;
            foreach(Truck truck in LS.trucks) {
                string msg = $"========== TRUCK {++i} ==========\n{truck.schedule.Display()}";
                Console.Write(msg);
            }
        }

        // re-calculated the time that a specific route takes to complete
        public static float routeTime(Route route) {
            float t = 0;
            foreach(DoublyNode<Order> node in route.orders) {
                if (route.orders.isHead(node)) {
                    t += node.value.distanceTo(node.next.value).travelDur;
                }
                else if (route.orders.isTail(node)) {
                    t += Truck.unloadTime;
                }
                else {
                    t += node.value.distanceTo(node.next.value).travelDur;
                    t += node.value.emptyDur;
                }
            }
            return t;
        }




        // get all orders and bind all distance-data to respective orders
        public static Order[] fetchOrders(string orderFilePath, string distancesFilePath) {
            // parse orders and distances
            Order[] orders = Parser.parseOrders(orderFilePath, nOrders:1177);
            (int, int)[][] distances = Parser.parseOrderDistances(distancesFilePath, nDistances:1099);

            // link distances to orders
            startOrder.distancesToOthers = distances[startOrder.distId];
            emptyingOrder.distancesToOthers = distances[emptyingOrder.distId];
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
            Route resetRoute = truck.schedule.weekRoutes[(int)day];
            Route route = truck.schedule.weekRoutes[(int)day] = new Route(resetRoute.orders.head.value, resetRoute.orders.tail.value);
            route.timeToComplete += Truck.unloadTime;

            Order randOrder = LS.randomOrder(),
                randOrder2 = LS.randomOrder();
            while(!(randOrder.freq > 0)) randOrder = LS.randomOrder();
            while(!(randOrder2.freq > 0)) randOrder2 = LS.randomOrder();

            LS.ForceAddOrder(randOrder, truck, day, route.orders.tail);
            LS.ForceAddOrder(randOrder2, truck, day, route.orders.tail);
            
            float durToOrder1 = route.orders.head.value.distanceTo(randOrder).travelDur,
                  durFromOrder1To2 = randOrder.distanceTo(randOrder2).travelDur,
                  durFromOrder2 = randOrder2.distanceTo(route.orders.tail.value).travelDur;

            float rt = durToOrder1 + randOrder.emptyDur + durFromOrder1To2 + randOrder2.emptyDur + durFromOrder2 + Truck.unloadTime;
            float rtt = Program.routeTime(route);

            //Console.WriteLine("\n##### TIME TEST: ADD ORDERS ######");
            //Console.WriteLine("accum\treal\troute recalc");
            //Console.WriteLine("----------------------------");
            //Console.WriteLine($"{route.timeToComplete} {rt} {routetime}");
            bool passed =Math.Round(route.timeToComplete,2) == Math.Round(rtt,2) || Math.Round(route.timeToComplete,2) == Math.Round(rt,2);
            Console.WriteLine($"PASSED?\t{passed}");

            return passed;
        }


        public static bool testRemoveOrders(LocalSearch LS) {
            Truck truck = LS.trucks[0];
            WorkDay day = 0;
            Route resetRoute = truck.schedule.weekRoutes[(int)day];
            Route route = truck.schedule.weekRoutes[(int)day] = new Route(resetRoute.orders.head.value, resetRoute.orders.tail.value);
            route.timeToComplete += Truck.unloadTime;

            Order randOrder = LS.randomOrder(),
                randOrder2 = LS.randomOrder();
            while(!(randOrder.freq > 0)) randOrder = LS.randomOrder();
            while(!(randOrder2.freq > 0)) randOrder2 = LS.randomOrder();


            LS.ForceAddOrder(randOrder, truck, day, route.orders.tail);
            LS.ForceAddOrder(randOrder2, truck, day, route.orders.tail);

            float rt;
            bool removeSecond = LS.random.NextDouble() <= 0.5;
            if (removeSecond) {
                LS.TryRemoveOrder(truck, day, route.orders.tail.prev);
                Order mid = route.orders.head.next.value;
                rt = route.orders.head.value.distanceTo(mid).travelDur + mid.emptyDur + mid.distanceTo(route.orders.tail.value).travelDur + Truck.unloadTime;;
            }
            else {
                LS.TryRemoveOrder(truck, day, route.orders.head.next);
                Order mid = route.orders.tail.prev.value;
                rt = route.orders.head.value.distanceTo(mid).travelDur + mid.emptyDur + mid.distanceTo(route.orders.tail.value).travelDur + Truck.unloadTime;
            }
            

            float rtt = Program.routeTime(route);

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
            Route resetRoute = truck.schedule.weekRoutes[(int)day];
            Route route = truck.schedule.weekRoutes[(int)day] = new Route(resetRoute.orders.head.value, resetRoute.orders.tail.value);
            route.timeToComplete += Truck.unloadTime;

            Order randOrder = LS.randomOrder(),
                randOrder2 = LS.randomOrder();
            while(!(randOrder.freq > 0)) randOrder = LS.randomOrder();
            while(!(randOrder2.freq > 0)) randOrder2 = LS.randomOrder();
            
            LS.ForceAddOrder(randOrder, truck, day, route.orders.tail);
            LS.ForceAddOrder(randOrder2, truck, day, route.orders.tail);


            LS.TryShiftOrders(truck, day, route.orders.head.next, route.orders.tail.prev);


            float durToOrder2 = route.orders.head.value.distanceTo(randOrder2).travelDur,
                durFromOrder2To1 = randOrder2.distanceTo(randOrder).travelDur,
                durFromOrder1 = randOrder.distanceTo(route.orders.tail.value).travelDur;


            float rt = durToOrder2 + randOrder2.emptyDur + durFromOrder2To1 + randOrder.emptyDur + durFromOrder1 + Truck.unloadTime;
            float rtt = Program.routeTime(route);

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
                truck2 = LS.trucks[0];
            WorkDay day = WorkDay.Mon,
                day2 = WorkDay.Tue;
            Route resetRoute = truck.schedule.weekRoutes[(int)day];
            Route route = truck.schedule.weekRoutes[(int)day] = new Route(resetRoute.orders.head.value, resetRoute.orders.tail.value),
                route2 = truck2.schedule.weekRoutes[(int)day2] = new Route(resetRoute.orders.head.value, resetRoute.orders.tail.value);
            route.timeToComplete += Truck.unloadTime;
            route2.timeToComplete += Truck.unloadTime;

            Order randOrder = LS.randomOrder(),
                randOrder2 = LS.randomOrder();
            while(!(randOrder.freq > 0)) randOrder = LS.randomOrder();
            while(!(randOrder2.freq > 0)) randOrder2 = LS.randomOrder();

            LS.ForceAddOrder(randOrder, truck, day, route.orders.tail);
            LS.ForceAddOrder(randOrder2, truck2, day2, route2.orders.tail);


            LS.TrySwapOrders(truck, day, route.orders.head.next, truck2, day2, route2.orders.tail.prev);

            float route1Dur = route.orders.head.value.distanceTo(randOrder).travelDur + randOrder.emptyDur + randOrder.distanceTo(route.orders.tail.value).travelDur + Truck.unloadTime,
                route2Dur = route2.orders.head.value.distanceTo(randOrder2).travelDur + randOrder2.emptyDur + randOrder2.distanceTo(route2.orders.tail.value).travelDur + Truck.unloadTime;

            float rt = route1Dur + route2Dur;
            float rtt = Program.routeTime(route) + Program.routeTime(route2);

            float accum = route.timeToComplete+route2.timeToComplete;

            //Console.WriteLine("\n##### TIME TEST: SWAP ORDERS ######");
            //Console.WriteLine("accum\treal\troute recalc");
            //Console.WriteLine("----------------------------");
            //Console.WriteLine($"{route.timeToComplete}\t{rt}\t{rtt}");
            bool passed = Math.Round(accum,2) == Math.Round(rt,2) || Math.Round(accum,2) == Math.Round(rtt,2);
            Console.WriteLine($"PASSED?\t{passed}");

            return passed;
        }
        
        // test each of the made routes to test for the accumulated route-time vs. recalculated routetime
        public static void testLSRouteTimes(LocalSearch LS) {
            // check
            foreach(Truck truck in LS.trucks) {
                foreach(Route dayRoute in truck.schedule.weekRoutes) {
                    Console.WriteLine($"Route time:\t{Math.Round(dayRoute.timeToComplete, 2)} (accum.)\t{Math.Round(Program.routeTime(dayRoute), 2)} (really)");
                }
            }
        }


    }

}




