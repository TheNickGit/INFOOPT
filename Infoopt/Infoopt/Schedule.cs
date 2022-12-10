using System;
using System.Collections.Generic;
using System.Text;

namespace Infoopt
{
    class Schedule
    {
        public DoublyList<Order>[] weekSchedule;
        public Order[] orders;
        public float[] scheduleTimes = new float[5];

        // Constructor
        public Schedule(Order[] orders)
        {
            weekSchedule = new DoublyList<Order>[5];
            for (int i = 0; i < 5; i++)
            {
                DoublyList<Order> daySchedule = new DoublyList<Order>();
                weekSchedule[i] = daySchedule;
                scheduleTimes[i] = 30; // 30 min is lost every day for emptying the truck at the end
            }

            this.orders = orders;
        }
    }
}
