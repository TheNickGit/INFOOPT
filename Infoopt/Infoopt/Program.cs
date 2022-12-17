using System;

namespace Infoopt {

    class Program {

        // Config:
        static int totalIterations = 10_000_000;

        public static Order
            startOrder = new Order(-1, "MAARHEEZE-start", 0, 0, 0, 0, 287, 56343016, 513026712), // The startlocation of each day.
            emptyingOrder = new Order(-2, "MAARHEEZE-stort", 0, 0, 0, 30, 287, 56343016, 513026712); // The 'stortplaats'

        static void Main(string[] args)
        {
            // parse orders and display them
            string orderFilePath = "./data/Orderbestand.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            string distancesFilePath = "./data/AfstandenMatrix.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            Order[] orders = fetchOrders(orderFilePath, distancesFilePath);

            printLSCheckerOutput(orders);

            //testLS(orders);
        }


        // test the LS with checker output
        public static void printLSCheckerOutput(Order[] orders) {
            LocalSearch LS = new LocalSearch(orders, startOrder, emptyingOrder);
            LS.Run(nIterations: totalIterations);
            
            int n = 1;
            foreach(Truck truck in LS.trucks) {
                foreach(int day in Enum.GetValues(typeof(WorkDay))) {
                    int j = 0;
                    foreach(DoublyNode<Order> node in truck.schedule.weekRoutes[day].orders) {
                        int orderId = node.value.nr == -2 ? 0 : node.value.nr;
                        if (orderId >= 0) Console.WriteLine($"{n}; {day+1}; {++j}; {orderId}");
                    }
                }
                n++;
            }

            // DEBUG
            int i = 0;
            foreach (Truck truck in LS.trucks)
            {
                string msg = $"========== TRUCK {++i} ==========\n{truck.schedule.Display()}";
                Console.Write(msg);
            }
        }

        // test the LS with custom output
        public static void testLS(Order[] orders) {
            LocalSearch LS = new LocalSearch(orders, startOrder, emptyingOrder);
            float oldCost = LS.CalcTotalCost();
            
            // TEST: Do 1.000.000 iterations.
            LS.Run(nIterations: totalIterations);

            Console.WriteLine("OLD TOTAL COST:\t" + oldCost);
            Console.WriteLine("NEW TOTAL COST:\t" + LS.CalcTotalCost());

            int i = 0;
            foreach(Truck truck in LS.trucks) {
                string msg = $"========== TRUCK {++i} ==========\n{truck.schedule.Display()}";
                Console.Write(msg);
            }

            testLSRouteTimes(LS);
        }

        public static void testLSRouteTimes(LocalSearch LS) {
            // check
            foreach(Truck truck in LS.trucks) {
                foreach(Route dayRoute in truck.schedule.weekRoutes) {
                    Console.WriteLine($"Route time:\t{Math.Round(dayRoute.timeToComplete, 2)} (accum.)\t{Math.Round(routeTime(dayRoute), 2)} (really)");
                }
            }
        }

        public static float routeTime(Route route) {
            float t = 0;
            foreach(DoublyNode<Order> node in route.orders) {
                
                if (route.orders.isHead(node)) {
                    t += node.value.distanceTo(node.next.value).travelDur;
                }
                else if (route.orders.isTail(node)) {
                    t += 30;    // truckUnloadTIme
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
    

}




