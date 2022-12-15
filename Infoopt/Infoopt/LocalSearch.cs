using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Infoopt
{
    class LocalSearch
    {
        // Config:
        double
            chanceAdd = 0.20,
            chanceRemove = 0.20,
            chanceShift = 0.60,
            // chance 'storten'?
            alpha = 0.005; // Chance to accept a worse solution

        Order[] orders;
        bool[] takenOrders; // false = order is not used yet; true = order is used
        public Order
            startOrder,
            emptyingOrder;
        public Schedule
            truck1Schedule,
            truck2Schedule;
        public int
            maxDayTime = 720,
            counter = 0;
        Random random = new Random();

        // Variables used in the precomputation of each iteration
        int orderNum, truck, day, spot;
        DoublyList<Order>[] weekSchedule;
        Schedule schedule;
        DoublyNode<Order> prev, next;
        Order prevValue, nextValue;


        // Constructor
        public LocalSearch(Order[] orders, Order startOrder, Order emtpyingOrder)
        {
            this.orders = orders;
            this.startOrder = startOrder;
            this.emptyingOrder = emtpyingOrder;
            takenOrders = new bool[orders.Length];
            truck1Schedule = new Schedule(orders);
            truck2Schedule = new Schedule(orders);

            // In the schedules of each day, at the special startOrder at the start and emptyingOrder at the end, resulting in DLLs of length 2.
            for (int i = 0; i < 5 ; i++)
            {
                truck1Schedule.weekSchedule[i].extendAtHead(startOrder);
                truck1Schedule.weekSchedule[i].extendAtTail(emptyingOrder);
                truck2Schedule.weekSchedule[i].extendAtHead(startOrder);
                truck2Schedule.weekSchedule[i].extendAtTail(emptyingOrder);
            }

        }

        // An iteration of the Simulated Annealing algorithm to potentially find a better solution.
        public void Iteration()
        {
            counter++;
            IterationPrecomputation();

            // TODO: Simulated Annealing toepassen door de hiervoor benodigde variabelen en functionaliteit toe te voegen
           
            // Make a random choice for add, remove or shift depending on the chances given in the config.
            double choice = random.NextDouble();
            if (choice < chanceAdd)
                TryAddOrder();
            else if (choice >= chanceAdd && choice < chanceAdd + chanceRemove)
                TryRemoveOrder();
            else if (choice >= chanceAdd + chanceRemove && choice < chanceAdd + chanceRemove + chanceShift)
                TryShiftOrder();
        }

        // Generate random variables and do the other calculations needed for all iterations (add, remove, shift).
        protected void IterationPrecomputation()
        {
            // Generate random variables to decide where to alter the schedules
            orderNum = random.Next(orders.Length);
            truck = random.Next(2);
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
            day = random.Next(5);
            spot = random.Next(1, weekSchedule[day].Length);

            // Get the previous and next orders at this spot, compare them and calculate the cost/gain of adding the new order
            next = weekSchedule[day].head.skipForward(spot);
            prev = next.prev;
            prevValue = prev.value;
            nextValue = next.value;
        }

        // Try to add an order into the current schedules.
        public void TryAddOrder()
        {
            // TODO: Deze check kan sneller wanneer je gewoon een aparte datastructuur hebt met alleen nog de ontbrekende orders erin
            if (takenOrders[orderNum]) // If the order is already used, don't add it
                return;
            Order order = orders[orderNum];
            if (order.freq != 1)    // TODO: Huidige code werkt alleen met freq = 1
                return;

            // Return early if the order can never fit into this schedule.
            if (schedule.scheduleTimes[day] >= maxDayTime || schedule.scheduleTimes[day] + order.emptyDur > maxDayTime)
                return;

            // Calculate change in time and see if the order can fit in the schedule.
            float timeChange = CalcTimeChangeAdd(prevValue, nextValue, order);
            //Console.WriteLine("Time change: " + timeChange + ", old time: " + truck1Schedule.scheduleTimes[day] + ", new time: " + (truck1Schedule.scheduleTimes[day] + timeChange));
            if (schedule.scheduleTimes[day] + timeChange > maxDayTime)
                return;
            
            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = CalcCostChangeAdd(prevValue, nextValue, order);
            //Console.WriteLine("Cost change: " + costChange);

            if (costChange < 0) // If adding the order would result in a negative cost, add it always
            {
                weekSchedule[day].insertBeforeNode(order, next);
                schedule.scheduleTimes[day] += timeChange;
                //Console.WriteLine("Order added! Truck: " + (truck+1) + ", Day: " + day + ", Between " + prevValue + " and " + nextValue);
                //Console.WriteLine("NORMAL Order added! Truck: " + (truck+1) + ", Day: " + day);
                takenOrders[orderNum] = true;
            }  
            else if (random.NextDouble() < alpha)    // If worse, add node with a chance based on 'a' and 'T'
            {
                weekSchedule[day].insertBeforeNode(order, next);
                schedule.scheduleTimes[day] += timeChange;
                //Console.WriteLine("Order added! Truck: " + (truck+1) + ", Day: " + day + ", Between " + prevValue + " and " + nextValue);
                //Console.WriteLine("ALPHA  Order added! Truck: " + (truck + 1) + ", Day: " + day);
                // TODO: Save best solution
                takenOrders[orderNum] = true;
            }
        }

        // Try to remove an order from the current schedules.
        public void TryRemoveOrder()
        {
            // Return if the first position is chosen as you don't want to remove the startOrder
            if (spot <= 1)
                return;

            DoublyNode<Order> current = prev;
            prev = current.prev;
            prevValue = prev.value;

            // Calculate change in time and see if the order can fit in the schedule.
            float timeChange = CalcTimeChangeRemove(prevValue, nextValue, current.value);
            //Console.WriteLine("Time change: " + timeChange + ", old time: " + truck1Schedule.scheduleTimes[day] + ", new time: " + (truck1Schedule.scheduleTimes[day] + timeChange));
            if (schedule.scheduleTimes[day] + timeChange > maxDayTime)
                return;

            // Calculate cost change and see if adding this order here is an improvement.
            float costChange = CalcCostChangeRemove(prevValue, nextValue, current.value);
            //Console.WriteLine("Cost change remove: " + costChange);

            if (costChange < 0) // If removing the order would result in a negative cost, always choose to remove it.
            {
                weekSchedule[day].ejectAfterNode(prev);
                schedule.scheduleTimes[day] += timeChange;
                //Console.WriteLine("NORMAL Order removed! Truck: " + (truck + 1) + ", Day: " + day);
                //Console.WriteLine("ORDERNUM: " + current.value.nr);
                takenOrders[current.value.spot] = false;
            }
            else if (random.NextDouble() < alpha)    // If worse, add node with a chance based on 'a' and 'T'
            {
                weekSchedule[day].ejectAfterNode(prev);
                schedule.scheduleTimes[day] += timeChange;
                //Console.WriteLine("ALPHA  Order removed! Truck: " + (truck + 1) + ", Day: " + day);
                // TODO: Save best solution
                takenOrders[current.value.spot] = false;
            }
        }

        // Try to shift two orders in the current schedules.
        public void TryShiftOrder()
        {
            // TODO: shift tussen schema's en tussen trucks; huidige implementatie is tussen zelfde schema.
            int spot2 = random.Next(1, weekSchedule[day].Length);
            if (spot <= 1 || spot2 <= 1 || spot == spot2)  // No use in swapping with itself
                return;

            DoublyNode<Order> current = prev;
            prev = current.prev;
            prevValue = prev.value;

            DoublyNode<Order> shiftTarget = weekSchedule[day].head.skipForward(spot2 - 1);

            // TODO: Houd rekening met de verschillende shifts (in schema, tussen schema's etc.), want die kunnen een verschillende manier van berekenen nodig hebben!
            float timeChange = CalcTimeChangeShift(prevValue, nextValue, current.value, shiftTarget.value);
            float timeChange2 = CalcTimeChangeShift(shiftTarget.prev.value, shiftTarget.next.value, shiftTarget.value, current.value);
            if (schedule.scheduleTimes[day] + timeChange + timeChange2 > 720) // Check if shift fits time-wise
                return;
            //Console.WriteLine("timeChange: " + timeChange + ", timechange2: " + timeChange2);

            // Calculate cost change and see if shifting these orders is an improvement.
            float costChange = CalcCostChangeShift(prevValue, nextValue, current.value, shiftTarget.value);
            float costChange2 = CalcCostChangeShift(shiftTarget.prev.value, shiftTarget.next.value, shiftTarget.value, current.value);

            if (costChange + costChange2 < 0) // If the shift would result in a negative cost, perform it always.
            {
                weekSchedule[day].swapNodes(current, shiftTarget);
                schedule.scheduleTimes[day] += timeChange + timeChange2;
                //Console.WriteLine("NORMAL Order shifted! Truck: " + (truck + 1) + ", Day: " + day);
                //Console.WriteLine(current.value + "" + shiftTarget.value);
            }
            else if (random.NextDouble() < alpha)   // If worse, perform the shift with a chance based on 'a' and 'T'
            {
                weekSchedule[day].swapNodes(current, shiftTarget);
                schedule.scheduleTimes[day] += timeChange + timeChange2;
                //Console.WriteLine("ALPHA  Order shifted! Truck: " + (truck + 1) + ", Day: " + day);
                //Console.WriteLine(current.value + "" + shiftTarget.value);
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
        public float CalcCostChangeAdd(Order prev, Order next, Order newOrder)
        {
            // Gains
            float currentDistanceGain = prev.distanceTo(next).travelDur / 60;
            float pickupCostGain = newOrder.emptyDur * 3;

            // Costs
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur) / 60;
            float pickupTimeCost = newOrder.emptyDur;

            // Calculate change: costs - gains (so a negative result is good!)
            return newDistanceCost + pickupTimeCost - (currentDistanceGain + pickupCostGain);
        }

        // Calculate the cost change for removing an order between prev and next.
        public float CalcCostChangeRemove(Order prev, Order next, Order current)
        {
            // Gains
            float currentDistanceGain = (prev.distanceTo(current).travelDur + current.distanceTo(next).travelDur) / 60;
            float pickupTimeGain = current.emptyDur;

            // Costs
            float newDistanceCost =  prev.distanceTo(next).travelDur / 60;
            float pickupCost = current.emptyDur * 3;

            // Calculate change: costs - gains (so a negative result is good!)
            return newDistanceCost + pickupCost - (currentDistanceGain + pickupTimeGain);
        }

        public float CalcCostChangeShift(Order prev, Order next, Order oldOrder, Order newOrder)
        {
            // Gains
            float oldDistanceGain = (prev.distanceTo(oldOrder).travelDur + oldOrder.distanceTo(next).travelDur) / 60;

            // Costs
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur) / 60;

            // Calculate change: costs - gains (so a negative result is good!)
            return newDistanceCost - oldDistanceGain;
        }

        // Calculate the time change for adding an order between prev and next.
        public float CalcTimeChangeAdd(Order prev, Order next, Order newOrder)
        {
            // Time decreases
            float currentDistanceGain = prev.distanceTo(next).travelDur / 60;

            // Time increases
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur) / 60;
            float pickupTimeCost = newOrder.emptyDur;

            // Calculate change: This number means how much additional time is spend if the order is added
            return newDistanceCost + pickupTimeCost - currentDistanceGain;
        }

        // Calculate the time change when removing an order between prev and next.
        public float CalcTimeChangeRemove(Order prev, Order next, Order current)
        {
            // Time decreases
            float pickupTimeGain = current.emptyDur;
            float currentDistanceGain = (prev.distanceTo(current).travelDur + current.distanceTo(next).travelDur) / 60;

            // time increases
            float newDistanceCost = prev.distanceTo(next).travelDur / 60;

            // Calcuate change: This number means how much time is spend more/less when this order is removed.
            return newDistanceCost - (pickupTimeGain + currentDistanceGain);
        }

        // Calculate the time change when shifting orders.
        public float CalcTimeChangeShift(Order prev, Order next, Order oldOrder, Order newOrder)
        {
            // Time decreases
            float pickupTimeGain = oldOrder.emptyDur;
            float oldDistanceGain = (prev.distanceTo(oldOrder).travelDur + oldOrder.distanceTo(next).travelDur) / 60;

            // Time increases
            float pickupTimeCost = newOrder.emptyDur;
            float newDistanceCost = (prev.distanceTo(newOrder).travelDur + newOrder.distanceTo(next).travelDur) / 60;

            return pickupTimeCost + newDistanceCost - (pickupTimeGain + oldDistanceGain);
        }
    }

}

/// TODOs:
/// - Afvalvolumes tellen nog niet mee
/// - Aandachtspuntje shifts: Kan een order om te storten geshift worden en zo ja, hoe?
