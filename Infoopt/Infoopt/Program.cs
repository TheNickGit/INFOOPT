using System;
using System.Diagnostics;
using System.Linq;

class Program
{
    // Config:
    static readonly int totalIterations = 50_000_000;

    public static RandomGen random = new RandomGen();
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
        Stopwatch sw = Stopwatch.StartNew();
        sw.Start();
        LS.Run(totalIterations);
        sw.Stop();
        double seconds = Math.Round(sw.ElapsedMilliseconds / 1000f, 1);
        PrintLSCheckerOutput(LS);
        PrintLSDisplay(LS);

        Console.WriteLine("Adds:    " + LS.adds);
        Console.WriteLine("Removes: " + LS.removes);
        Console.WriteLine("Shifts:  " + LS.shifts);
        Console.WriteLine("Swaps:   " + LS.swaps);
        Console.WriteLine("Total time spent iterating: " + seconds + " sec");
    }

    /// <summary>
    /// print the LS with checker output
    /// </summary>
    public static void PrintLSCheckerOutput(LocalSearch LS)
    {
        int truckNr = 1;
        foreach (Truck truck in LS.trucks)
        {
            int day = 1;
            foreach (DaySchedule route in truck.schedule.weekSchedule)
            {
                int routeNr = 1;
                foreach (RouteTrip trip in route.trips)
                {
                    foreach (DoublyNode<Order> node in trip.orders)
                    {
                        if (!trip.orders.IsHead(node))
                            Console.WriteLine($"{truckNr}; {day}; {routeNr++}; {node.value.nr}");
                    }
                }
                day += 1;
            }
            truckNr++;
        }
    }

    /// <summary>
    /// print the LS with custom output
    /// </summary>
    public static void PrintLSDisplay(LocalSearch LS)
    {
        string msg = String.Join('\n', LS.trucks.Select(
                (truck, i) => $"=========== TRUCK {++i} ===========\n{truck.schedule.Display()}"
            ));
        Console.WriteLine(msg);
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