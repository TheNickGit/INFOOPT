using System;

class Program
{
    // Config:
    static readonly int totalIterations = 5_000_000;

    public static Order
        startOrder = new Order(0, "MAARHEEZE-start", 0, 0, 0, 0, 287, 56343016, 513026712), // The startlocation of each day.
        stopOrder = new Order(0, "MAARHEEZE-stort", 0, 0, 0, 30, 287, 56343016, 513026712); // The 'stortplaats'

    static void Main()
    {
        // parse orders and display them
        string orderFilePath = "./data/Orderbestand.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
        string distancesFilePath = "./data/AfstandenMatrix.csv"; // CHANGE TO ABSOLUTE PATH IF RUNNING IN DEBUG MODE
        Order[] orders = FetchOrders(orderFilePath, distancesFilePath);

        LocalSearch LS = new LocalSearch(orders);
        LS.Run(totalIterations);

        PrintLSCheckerOutput(LS);
        PrintLSDisplay(LS);
    }

    /// <summary>
    /// print the LS with checker output
    /// </summary>
    public static void PrintLSCheckerOutput(LocalSearch LS)
    {
        int n = 1;
        foreach (Truck truck in LS.trucks)
        {
            foreach (int day in Enum.GetValues(typeof(Day)))
            {
                int j = 0;
                foreach (DoublyNode<Order> node in truck.schedule.weekRoutes[day].orders)
                {
                    int orderId = node.value.nr;
                    if (j > 0) Console.WriteLine($"{n}; {day + 1}; {j}; {orderId}");
                    j++;
                }
            }
            n++;
        }
    }

    /// <summary>
    /// print the LS with custom output
    /// </summary>
    public static void PrintLSDisplay(LocalSearch LS)
    {
        int i = 0;
        foreach (Truck truck in LS.trucks)
        {
            string msg = $"========== TRUCK {++i} ==========\n{truck.schedule.Display()}";
            Console.Write(msg);
        }
        Console.WriteLine("\nNEW TOTAL COST:\t" + LS.CalcTotalCost() / 60);
    }

    /// <summary>
    /// Get all orders and bind all distance-data to respective orders.
    /// </summary>
    public static Order[] FetchOrders(string orderFilePath, string distancesFilePath)
    {
        // parse orders and distances
        Order[] orders = Parser.ParseOrders(orderFilePath, nOrders: 1177);
        int[][] distances = Parser.ParseOrderDistances(distancesFilePath, nDistances: 1099);

        // link distances to orders
        startOrder.distancesToOthers = distances[startOrder.distId];
        stopOrder.distancesToOthers = distances[stopOrder.distId];
        foreach (Order order in orders)
            order.distancesToOthers = distances[order.distId];

        return orders;
    }
}