using System;

namespace Infoopt {

    class Program {

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
            ///

            //foreach (Order order in orders) Console.WriteLine(order);


            //// make doubly-list and display each value
            //DoublyList<int> dll = DoublyList<int>.fromArray(new int[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            //foreach (DoublyNode<int> node in dll)
            //    Console.Write($"{node.value} ");

            LocalSearch LS = new LocalSearch(orders, startOrder, emptyingOrder);
            Console.WriteLine("TOTAL COST: " + LS.CalcTotalCost());

            // TEST: Do 100.000 iterations.
            while (LS.counter < 1000000)
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
                Console.Write($"{node.value} ");

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




