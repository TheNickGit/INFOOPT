using System;

namespace Infoopt {

    class Program {

        static void Main(string[] args) {

            // parse orders and display them
            string orderFilePath = "./data/Orderbestand.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            string distancesFilePath = "./data/AfstandenMatrix.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
            Order[] orders = fetchOrders(orderFilePath, distancesFilePath);
            //foreach (Order order in orders) Console.WriteLine(order);


            //// make doubly-list and display each value
            //DoublyList<int> dll = DoublyList<int>.fromArray(new int[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            //foreach (DoublyNode<int> node in dll)
            //    Console.Write($"{node.value} ");

            LocalSearch LS = new LocalSearch(orders);
            Console.WriteLine("TOTAL COST: " + LS.CalcTotalCost());

            // TEST: Do 10000 attempts to add an order into the schedules.
            for (int i = 0; i < 10000; i++)
            {
                LS.TryAddOrder();
            }
            Console.WriteLine("LENGTH: " + LS.truck1Schedule.weekSchedule[0].Length);
            Console.WriteLine("TOTAL COST: " + LS.CalcTotalCost());
            for (int i = 0; i < 5; i++)
                Console.WriteLine("Truck1 day " + i + ", time: " + LS.truck1Schedule.scheduleTimes[i]);
            for (int i = 0; i < 5; i++)
                Console.WriteLine("Truck2 day " + i + ", time: " + LS.truck2Schedule.scheduleTimes[i]);

        }

        // get all orders and bind all distance-data to respective orders
        public static Order[] fetchOrders(string orderFilePath, string distancesFilePath) {
            // parse orders and distances
            Order[] orders = Parser.parseOrders(orderFilePath, nOrders:1177);
            (int, int)[][] distances = Parser.parseOrderDistances(distancesFilePath, nDistances:1099);

            // link distances to orders
            foreach(Order order in orders) 
                order.distancesToOthers = distances[order.distId];
            return orders;
        }


        // For debugging purposes (remove later)
        public static void Assert(bool pred) {
            if (!pred) throw new Exception("ASSERTION FAIL");
        }


    }
    




}




