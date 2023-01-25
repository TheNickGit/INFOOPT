using System.Globalization;
using System.IO;
using System.Linq;

internal class Parser
{
    /// <summary>
    /// Parse an nOrders-amount of orders from the CSV file at filePath.
    /// </summary>
    public static Order[] ParseOrders(string filePath, int nOrders)
    {
        Order[] orders = new Order[nOrders];
        using (StreamReader sr = new StreamReader(filePath))
        {
            // skip csv header
            sr.ReadLine();

            // fill order array trivially backwards
            while (nOrders > 0)
                orders[--nOrders] = ParseOrder(sr.ReadLine());
        }
        return orders;
    }

    /// <summary>
    /// Parse a single order from the CSV file.
    /// </summary>
    private static Order ParseOrder(string line)
    {
        string[] args = line.Split(';');
        return new Order(
            int.Parse(args[0]), args[1].Trim(), (int)char.GetNumericValue(args[2][0]),
            int.Parse(args[3]), int.Parse(args[4]), float.Parse(args[5], CultureInfo.InvariantCulture),
            int.Parse(args[6]), int.Parse(args[7]), int.Parse(args[8])
        );
    }


    /// <summary>
    /// PARSE 'nDistances^2' AMOUNT OF ORDER DISTANCES FROM CSV FORMATTED FILE AT 'filePath'
    /// </summary>
    public static int[][] ParseOrderDistances(string filePath, int nDistances)
    {
        int[][] distances = new int[nDistances][];
        using (StreamReader sr = new StreamReader(filePath))
        {
            // skip csv header
            sr.ReadLine();

            // fill 2D distances matrix based on distance-ids as indexes (fromId x toId)
            int nDistPerms = nDistances * nDistances;
            while (nDistPerms-- > 0)
            {
                (int fromId, int toId, int travelDur) = ParseDistance(sr.ReadLine());

                // create new nested array in 2D matrix if not present yet
                if (distances[fromId] is null)
                    distances[fromId] = new int[nDistances];
                distances[fromId][toId] = travelDur;
            }
        }
        return distances;
    }

    /// <summary>
    /// PARSE A SINGLE ORDER DISTANCE FROM CSV FORMATTED FILE LINE
    /// </summary>
    private static (int fromId, int toId, int travelDur) ParseDistance(string line)
    {
        int[] args = line.Split(';').Select(a => int.Parse(a)).ToArray();
        return (args[0], args[1], args[3]);
    }
}

