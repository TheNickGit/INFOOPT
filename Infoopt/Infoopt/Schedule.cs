using System;
using System.Collections.Generic;
using System.Text;

namespace Infoopt
{
    class Schedule
    {
        public DoublyList<Order>[] weekSchedule;
        public Order[] orders;
        public bool[] takenOrders;
        public float cost;
        public float[] scheduleTimes = new float[5];
        float maxDayTime = 720; // Trucks can be used for 720 min each day

        // Constructor
        public Schedule(Order[] orders, float cost = 0)
        {
            weekSchedule = new DoublyList<Order>[5];
            for (int i = 0; i < 5; i++)
            {
                DoublyList<Order> daySchedule = new DoublyList<Order>();
                weekSchedule[i] = daySchedule;
                //scheduleTimes[i] = 30; // 30 min is lost every day for emptying the truck at the end
            }

            this.orders = orders;
            takenOrders = new bool[orders.Length];
            this.cost = cost;
        }
    }
}
