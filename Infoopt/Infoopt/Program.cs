using System;
using System.Diagnostics;
using System.Linq;
using System.IO;

class Program
{

    /*
     * A configuration of the variables used in the algorithm can be found at the top of the Local Search class.
     * These variables can be changed to influence the flow of the algorithm.
     */

    // Static variables that are accessed by multiple classes during the LS process.
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
        LS.Run();
        sw.Stop();
        double seconds = Math.Round(sw.ElapsedMilliseconds / 1000f, 1);
        PrintLSCheckerOutput(LS);


        SaveLSCheckerOutput(LS);    // saves output to ./solution/<score>.csv


        PrintLSDisplay(LS);

        Console.WriteLine("Adds:    " + LS.adds);
        Console.WriteLine("Removes: " + LS.removes);
        Console.WriteLine("Shifts between:  " + LS.pureShiftsBetweenTrips);
        Console.WriteLine("Shifts witin:   " + LS.pureShiftsWithinTrip);
        Console.WriteLine("Total time spent iterating: " + seconds + " sec");


    }

    public static void SaveLSCheckerOutput(LocalSearch LS) {
        using (StreamWriter sw = new StreamWriter($"./solutions/{LS.CalcTotalCost()}.csv")) 
            PrintLSCheckerOutput(LS, cout: sw);
    }

    /// <summary>
    /// print the LS with checker output
    /// </summary>
    public static void PrintLSCheckerOutput(LocalSearch LS, TextWriter cout=null)
    {
        cout ??= Console.Out;
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
                            cout.WriteLine($"{truckNr}; {day}; {routeNr++}; {node.value.nr}");
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
        Console.WriteLine("\nNEW TOTAL COST:\t" + LS.CalcTotalCost());
    }

    /// <summary>
    /// Get all orders and bind all distance-data to respective orders.
    /// </summary>
    public static Order[] FetchOrders(string orderFilePath, string distancesFilePath)
    {
        // parse orders and distances, and make the distances available to be used by the orders
        Order[] orders = Parser.ParseOrders(orderFilePath, nOrders: 1177);
        Order.travelDistances = Parser.ParseOrderDistances(distancesFilePath, nDistances: 1099);

        for (int i = 0; i < orders.Length; i++) {
            orders[i].spot = i;
        }

        return orders;
    }
}