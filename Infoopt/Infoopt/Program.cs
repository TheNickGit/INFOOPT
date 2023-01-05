using System;
using System.Diagnostics;
using System.Linq;

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


        // Cluster orders, and save cluster-assignments by binding them to order.cluster
        Cluster2DKMeans model = clusterOrders(orders);

        // Create a basis- solution by pre-filling the truck their day-trips 
        // with orders based on their clustering
        LocalSearch LS = new LocalSearch(orders);
        PreProcessOrders(LS);
        double seconds = MethodRunningTime(() => LS.Run());


        PrintLSCheckerOutput(LS);
        
        //PrintLSDisplay(LS);
        //PrintLSIterationStats(LS, seconds);
    }

    /// <summary>
    /// Pre-process the orders by filling each truck's day-trips with orders 
    /// partaining to the same cluster, before running the model
    /// </summary>
    public static void PreProcessOrders(LocalSearch LS)
    {
        int clusterIndex = 0;
        foreach(Truck truck in LS.trucks)
        {
            foreach(DaySchedule sched in truck.schedule.weekSchedule)
            {
                Order[] clusterOrders = LS.orders.Where(o => o.cluster == clusterIndex).ToArray();
                RouteTrip dayTrip = sched.trips[0];
                for(int i=0; i<clusterOrders.Length;i++)
                {
                    Order order = clusterOrders[i];
                    if (order.freq > 1) continue;                               // only add orders with freq of 1
                    bool newTrip = !dayTrip.CanAddVolume(order.volume);         // check if can add volume to trip, make new trip if not
                    dayTrip = newTrip ? sched.AddTrip() : dayTrip;
                    float dt = Schedule.TimeChangeAdd(order, dayTrip.orders.tail, newTrip: newTrip);
                    if (sched.timeToComplete + dt > LocalSearch.maxDayTime)     // do next day-schedule if this day-schedule is full time-wise
                        break;
                    dayTrip.AddOrder(order, dayTrip.orders.tail, dt);
                }
                clusterIndex++;
            }
        }
    }


    /// <summary>
    /// Assign orders to different clusters as means of pre-processing the orders
    /// </summary>
    public static Cluster2DKMeans clusterOrders(Order[] orders) {
        Cluster2DKMeans clusterModel = Cluster2DKMeans.fromRandomCentroids(
            nCentroids: 10,
            xMin: 55297404f, xMax: 59217110f,       // max and min values of order coordinates' x- and y-values
            yMin: 512345518f, yMax: 515539852f
        );
        clusterModel.Run(
            orders.Select(o => ((float)o.coord.X, (float)o.coord.Y)).ToArray(), 
            maxIterations: 50, 
            verbose: false
        );
        for(int i=0; i<orders.Length; i++)          // bind the assigned cluster to the specific order
            orders[i].cluster = clusterModel.assignments[i];
        return clusterModel;
    }

    /// <summary>
    /// Print clustering output to visualise compartimentalization (using "__clusvis.py")
    /// </summary>
    public static void printClusterOutput(Cluster2DKMeans model, Order[] orders) {
        for(int i=0; i< orders.Length; i++) {
            int cluster = model.assignments[i];
            Console.WriteLine($"{cluster}\t{orders[i].coord.X}\t{orders[i].coord.Y}");
        }
    }




    /// <summary>
    /// retrieves the running time of a called method through calling the supplied function
    /// </summary>
    public static double MethodRunningTime(Action action) {
        Stopwatch sw = Stopwatch.StartNew();
        sw.Start();
        action();
        sw.Stop();
        return Math.Round(sw.ElapsedMilliseconds / 1000f, 1);
    }

    /// <summary>
    /// print the LS with mutation stats output
    /// </summary>
    public static void PrintLSIterationStats(LocalSearch LS, double runningTime) {
        Console.WriteLine("Adds:    " + LS.adds);
        Console.WriteLine("Removes: " + LS.removes);
        Console.WriteLine("Shifts:  " + LS.shifts);
        Console.WriteLine("Swaps:   " + LS.swaps);
        Console.WriteLine("Total time spent iterating: " + runningTime + " sec");
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

        for (int i = 0; i < orders.Length; i++)
        {
            orders[i].spot = i;
        }

        // link distances to orders
        startOrder.distancesToOthers = distances[startOrder.distId];
        stopOrder.distancesToOthers = distances[stopOrder.distId];
        foreach (Order order in orders)
            order.distancesToOthers = distances[order.distId];

        return orders;
    }
}