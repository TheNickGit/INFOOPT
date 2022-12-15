using System;
using System.Net.Http.Headers;

namespace Infoopt {

    class Program {

        // Config:
        static int totalIterations = 10000000;

        public static Order
            startOrder = new Order(-1, "MAARHEEZE-start", 0, 0, 0, 0, 287, 56343016, 513026712), // The startlocation of each day.
            emptyingOrder = new Order(-2, "MAARHEEZE-stort", 0, 0, 0, 30, 287, 56343016, 513026712); // The 'stortplaats'

        static void Main(string[] args) {

            // parse orders and display them
            string orderFilePath = "./data/Orderbestand.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            string distancesFilePath = "./data/AfstandenMatrix.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            Order[] orders = fetchOrders(orderFilePath, distancesFilePath);

            /// TODO Dit is een noodoplossing om de Remove goed te laten werken! Aangezien orderID en de plek in de array niet hetzelfde zijn, is de plek in de array niet te vinden.
            /// Een efficientere datastructuur kan chiller zijn.
            for (int i = 0; i < orders.Length; i++)
                orders[i].spot = i;

            printLSCheckerOutput(orders);
            //testLS(orders);
        }

        public static void printLSCheckerOutput(Order[] orders) {
            LocalSearch LS = new LocalSearch(orders, startOrder, emptyingOrder);
            while (LS.counter < totalIterations) LS.Iteration();

            for(int i = 0; i<5; i++) {
                int j = 1;
                foreach(DoublyNode<Order> node in LS.truck1Schedule.weekSchedule[i]) {
                    int orderId = node.value.nr;
                    if (orderId >= 0) Console.WriteLine($"1; {i+1}; {j++}; {orderId}");
                    else if (orderId == -2) Console.WriteLine($"1; {i+1}; {j++}; 0"); // last order (dump) should have order id 0
                }
            }
            for(int i = 0; i<5; i++) {
                int j = 1;
                foreach(DoublyNode<Order> node in LS.truck2Schedule.weekSchedule[i]) {
                    int orderId = node.value.nr;
                    if (orderId >= 0) Console.WriteLine($"2; {i+1}; {j++}; {orderId}");
                    else if (orderId == -2) Console.WriteLine($"2; {i+1}; {j++}; 0"); // last order (dump) should have order id 0
                }
            }

        }

        public static void testLS(Order[] orders) {
            LocalSearch LS = new LocalSearch(orders, startOrder, emptyingOrder);
            Console.WriteLine("TOTAL COST: " + LS.CalcTotalCost());

            // Perform iterations
            while (LS.counter < totalIterations)
            {
                LS.Iteration();
            }

            Console.WriteLine("TOTAL COST: " + LS.CalcTotalCost());
            for (int i = 0; i < 5; i++)
                Console.WriteLine("Truck1 day " + i + ", time: " + LS.truck1Schedule.scheduleTimes[i]);
            for (int i = 0; i < 5; i++)
                Console.WriteLine("Truck2 day " + i + ", time: " + LS.truck2Schedule.scheduleTimes[i]);

            Console.WriteLine("-- Schema van truck 1 dag 1 -- lengte: " + LS.truck1Schedule.weekSchedule[0].Length);
            foreach (DoublyNode<Order> node in LS.truck1Schedule.weekSchedule[0])
                Console.WriteLine($"{node.value}");


        }

        public static void testLS2(Order[] orders) {
            LS2 LS = new LS2(orders, startOrder, emptyingOrder);
            Console.WriteLine("TOTAL COST: " + LS.CalcTotalCost());


            // TEST: Do 100.000 iterations.
            LS.Run(nIterations: 1_000);

            Console.WriteLine("TOTAL COST: " + LS.CalcTotalCost());
            foreach(Truck truck in LS.trucks) {
                int i = 0;
                foreach(int day in Enum.GetValues(typeof(WorkDay))) {
                    Console.WriteLine($"Truck{++i} day {day}, time: {truck.schedule.weekRoutes[day].timeToComplete}");
                }
            }

            Route firstTruckFirstDayRoute = LS.trucks[0].schedule.weekRoutes[0];
            Console.WriteLine("-- Schema van truck 1 dag 1 -- lengte: " + firstTruckFirstDayRoute.orders.Length);
            foreach (DoublyNode<Order> node in firstTruckFirstDayRoute.orders)
                Console.Write($"{node.value} \n");
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




