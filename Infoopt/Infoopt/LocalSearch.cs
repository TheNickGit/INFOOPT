using System;
using System.Collections.Generic;
using System.Text;

namespace Infoopt
{
    class LocalSearch
    {
        Order[] orders;
        bool[] takenOrders; // false = order is not used yet; true = order is used
        public Order emptyingOrder = new Order(-1, "Maarheze", 0, 0, 0, 30, 287, 56343016, 513026712); // The 'stortplaats'
        public Schedule
            truck1Schedule,
            truck2Schedule;
        public int maxDayTime = 720;

        // Constructor
        public LocalSearch(Order[] orders)
        {
            this.orders = orders;
            takenOrders = new bool[orders.Length];
            truck1Schedule = new Schedule(orders);
            truck2Schedule = new Schedule(orders);

            for (int i = 0; i < 5 ; i++)
            {
                truck1Schedule.weekSchedule[i].extendAtHead(emptyingOrder);
                truck2Schedule.weekSchedule[i].extendAtHead(emptyingOrder);
            }
        }

        // TODO
        public void Iteration()
        {
            
        }

        // Try to add an order into the current schedules of the trucks.
        public void TryAddOrder()
        {
            Random r = new Random();

            // Generate a random order to add
            int orderNum = r.Next(orders.Length);
            if (takenOrders[orderNum]) // If the order is already used, don't add it
                return;
            Order order = orders[orderNum];
            if (order.freq != 1)    // TODO: Huidige code werkt alleen met freq = 1
                return;

            // Generate a random day to add the order into
            // TODO: voeg logica toe voor orders met andere frequenties dan 1
            int truck = r.Next(2);
            DoublyList<Order>[] weekSchedule;
            Schedule schedule;
            if (truck == 0)
            {
                schedule = truck1Schedule;
                weekSchedule = truck1Schedule.weekSchedule;
            }
            else
            {
                schedule = truck2Schedule;
                weekSchedule = truck2Schedule.weekSchedule;
            } 
            int day = r.Next(5);

            // Return early if the order can never fit into this schedule.
            if (schedule.scheduleTimes[day] >= maxDayTime || schedule.scheduleTimes[day] + order.emptyDur > maxDayTime)
                return;

            // Generate a random spot to add the order at in the DLL
            int spot = r.Next(weekSchedule[day].Length);

            // Get the previous and next orders at this spot, compare them and calculate the cost/gain of adding the new order
            DoublyNode<Order> prev = null, next;
            if (spot == 0)
            {
                next = weekSchedule[day].head;
            }
            else
            {
                next = weekSchedule[day].head;
                for (int i = 0; i < spot; i++)
                {
                    next = next.next;
                }
                prev = next.prev;
            }

            Order prevValue = null, nextValue = null;
            if (prev != null)
                prevValue = prev.value;
            if (next != null)
                nextValue = next.value;

            // Calculate change in time and see if the order can fit in the schedule.
            float timeChange = CalcTimeChangeAdd(prevValue, nextValue, order);
            Console.WriteLine("Time change: " + timeChange + ", old time: " + truck1Schedule.scheduleTimes[day] + ", new time: " + (truck1Schedule.scheduleTimes[day] + timeChange));
            if (schedule.scheduleTimes[day] + timeChange > maxDayTime)
                return;
            
            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = CalcCostChangeAdd(prevValue, nextValue, order);
            Console.WriteLine("Cost change: " + costChange);

            if (costChange < 0) // If adding the order would result in a negative cost, add it always
            {
                weekSchedule[day].insertBeforeNode(order, next);
                schedule.scheduleTimes[day] += timeChange;
                Console.WriteLine("Order added! Truck: " + (truck+1) + ", Day: " + day + ", Between " + prevValue + " and " + nextValue);
                takenOrders[orderNum] = true;
            }  
            else    // TODO: If worse, add node with a chance based on 'a' and 'T'
            {
               
            }

        }

        // Calculates the total cost of a solution by checking all its content.
        // This method is very slow! Use one of the methods for cost changes instead for small changes.
        public float CalcTotalCost()
        {
            float cost = 0;

            // Add cost for missed orders
            for (int i = 0; i < orders.Length; i++)
                if (!takenOrders[i])
                    cost += 3 * orders[i].freq * orders[i].emptyDur;

            // Add cost for time on the road
            for (int i = 0; i < 5; i++)
            {
                cost += truck1Schedule.scheduleTimes[i];
                cost += truck2Schedule.scheduleTimes[i];
            }

            return cost;
        }

        // Calculate the cost change for adding an order between prev and next.
        public float CalcCostChangeAdd(Order prev, Order next, Order order)
        {
            // Gains
            float currentDistanceGain;
            if (prev == null || next == null)
                currentDistanceGain = 0;
            else
                currentDistanceGain = prev.distanceTo(next).travelDur / 60;
            float pickupCostGain = order.emptyDur * 3;

            // Costs
            float newDistanceCost;
            if (prev != null && next != null)
                newDistanceCost = (prev.distanceTo(order).travelDur + order.distanceTo(next).travelDur) / 60;
            else
                newDistanceCost = order.distanceTo(next).travelDur / 60;
            float pickupTimeCost = order.emptyDur;

            // Calculate change: costs - gains (so a negative result is good!)
            float costChange = newDistanceCost + pickupTimeCost - (currentDistanceGain + pickupCostGain);
            return costChange;
        }

        // Calculate the time change for adding an order between prev and next.
        public float CalcTimeChangeAdd(Order prev, Order next, Order order)
        {
            // Time decreases
            float currentDistanceGain;
            if (prev == null || next == null)
                currentDistanceGain = 0;
            else
                currentDistanceGain = prev.distanceTo(next).travelDur / 60;

            // Time increases
            float newDistanceCost;
            if (prev != null && next != null)
                newDistanceCost = (prev.distanceTo(order).travelDur + order.distanceTo(next).travelDur) / 60;
            else
                newDistanceCost = order.distanceTo(next).travelDur / 60;
            float pickupTimeCost = order.emptyDur;

            // Calculate change: This number means how much additional time is spend if the order is added
            float timeChange = newDistanceCost + pickupTimeCost - currentDistanceGain;
            return timeChange;
        }
    }

}
