using System;
using System.Collections.Generic;
using System.Text;

namespace Infoopt
{
    class LocalSearch
    {
        Order[] orders;
        public Schedule currentSolution;

        // Constructor
        public LocalSearch(Order[] orders)
        {
            this.orders = orders;
            currentSolution = new Schedule();
        }

        // TODO
        public void Iteration()
        {
            
        }

        // Calculates the cost of a solution by checking all its content.
        // This method is very slow! Use the method from the Schedule class instead for small changes.
        public float CalcTotalCost(Schedule schedule)
        {
            float cost = 0;

            // Add cost for missed orders
            foreach (Order order in orders)
            {
                int foundFreq = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (schedule.weekSchedule[i].Contains(order))
                        foundFreq++;
                }
                if (foundFreq != order.freq)
                    cost += 3 * order.freq * order.emptyDur;
            }

            // Add cost for time on the road
            for (int i = 0; i < 5; i++)
                cost += schedule.scheduleTimes[i];

            return cost;
        }
    }

}
