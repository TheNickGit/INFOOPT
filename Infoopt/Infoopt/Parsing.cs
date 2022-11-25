using System;
using System.IO;
using System.Linq;

namespace Infoopt
{

    class Parser
    {

        // PARSE 'nOrders' AMOUNT OF ORDERS FROM CSV-FORMATTED FILE AT 'filePath'
        public static Order[] parseOrders(string filePath, int nOrders)
        {
            Order[] orders = new Order[nOrders];
            using (StreamReader sr = new StreamReader(filePath))
            {

                // skip csv header
                sr.ReadLine();

                // fill order array trivially backwards
                while (nOrders > 0)
                    orders[--nOrders] = parseOrder(sr.ReadLine());

            }
            return orders;
        }

        // PARSE A SINGLE ORDER FROM CSV-FORMATTED FILE LINE
        private static Order parseOrder(string line)
        {
            string[] args = line.Split(';');
            return new Order(
                int.Parse(args[0]), args[1].Trim(), (int)char.GetNumericValue(args[2][0]),
                int.Parse(args[3]), int.Parse(args[4]), float.Parse(args[5]),
                int.Parse(args[6]), int.Parse(args[7]), int.Parse(args[8])
            );
        }


        // PARSE 'nDistances^2' AMOUNT OF ORDER DISTANCES FROM CSV FORMATTED FILE AT 'filePath'
        public static (int dist, int travelDur)[][] parseOrderDistances(string filePath, int nDistances)
        {
            (int dist, int travelDur)[][] distances = new (int dist, int travelDur)[nDistances][];
            using (StreamReader sr = new StreamReader(filePath))
            {

                // skip csv header
                sr.ReadLine();

                // fill 2D distances matrix based on distance-ids as indexes (fromId x toId)
                int nDistPerms = nDistances * nDistances;
                while (nDistPerms-- > 0)
                {
                    (int fromId, int toId, (int, int) data) = parseDistance(sr.ReadLine());

                    // create new nested array in 2D matrix if not present yet
                    if (Object.ReferenceEquals(distances[fromId], null))
                        distances[fromId] = new (int dist, int travelDur)[nDistances];
                    distances[fromId][toId] = data;
                }
            }
            return distances;
        }

        // PARSE A SINGLE ORDER DISTANCE FROM CSV FORMATTED FILE LINE
        private static (int fromId, int toId, (int, int) data) parseDistance(string line)
        {
            int[] args = line.Split(';').Select(a => int.Parse(a)).ToArray();
            return (args[0], args[1], (args[2], args[3]));
        }

    }

}
