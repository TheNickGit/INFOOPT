using System;
using System.Collections.Generic;
using System.Text;

namespace Infoopt
{
    class Schedule
    {
        public DoublyList<Order>[] weekSchedule;
        public float cost = 0;
        public float[] scheduleTimes = new float[5];
        int maxDayTime = 720; // Trucks can be used for 720 min each day

        // Constructor
        public Schedule()
        {
            weekSchedule = new DoublyList<Order>[5];
            for (int i = 0; i < 5; i++)
            {
                DoublyList<Order> daySchedule = new DoublyList<Order>();
                weekSchedule[i] = daySchedule;
                scheduleTimes[i] = 30; // 30 min is lost every day for emptying the truck at the end
            }
        }

        // TODO
        public float CalcCostChange()
        {
            float costChange = 0;
            return costChange;
        }

    }
}
