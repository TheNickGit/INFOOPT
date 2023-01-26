﻿using System.Linq;
using System;

class Schedule
{
    public DaySchedule[] weekSchedule = new DaySchedule[5];

    /// <summary>
    /// Constructor.
    /// </summary>
    public Schedule()
    {
        foreach (int day in Enum.GetValues(typeof(Day)))
        {
            weekSchedule[day] = new DaySchedule();
        }
    }

    /// <summary>
    /// Display all weekroutes (custom print)
    /// </summary>
    public string Display()
        => String.Join("\n", this.weekSchedule.Select((dayRoute, day) => {
            float ttcMinutes = (float)Math.Round(dayRoute.timeToComplete / 60.0f, 1);
            return $"------ {(Day)(day++)}  ({ttcMinutes} min.) ------\n{dayRoute.Display()}";
        }));

    /// <summary>
    /// loop through the week routes and accumulate their time to complete
    /// </summary>
    public float TimeCost()
    {
        float cost = 0.0f;
        foreach (DaySchedule dayRoute in weekSchedule)
        {
            cost += dayRoute.timeToComplete;
        }
        return cost;
    }

    /// <summary>
    /// Calculate the time change for adding an order between prev and next.
    /// </summary>
    public static float TimeChangeAdd(Order newOrder, DoublyNode<Order> routeOrder)
    {
        // Get the orders located at the current node and the one before it.
        Order prev, current;
        prev = routeOrder.prev.value;
        current = routeOrder.value;

        // Time decreases
        float currentDistanceGain = prev.DistanceTo(current);

        // Time increases
        float newDistanceCost = prev.DistanceTo(newOrder) + newOrder.DistanceTo(current);
        float pickupTimeCost = newOrder.emptyDur;
        float unloadTimeCost = 0;

        // Calculate change: This number means how much additional time is spend if the order is added
        return (newDistanceCost + pickupTimeCost + unloadTimeCost) - currentDistanceGain;
    }

    /// <summary>
    /// Calculate the cost change for adding an order between prev and next.
    /// </summary>
    public static float CostChangeAdd(Order newOrder, DoublyNode<Order> routeOrder)
    {
        // Get the orders located at the current node and the one before it.
        Order prev, current;
        prev = routeOrder.prev.value;
        current = routeOrder.value;


        // Gains
        float currentDistanceGain = prev.DistanceTo(current);
        float pickupCostGain = newOrder.emptyDur * 3;

        // Costs
        float newDistanceCost = prev.DistanceTo(newOrder) + newOrder.DistanceTo(current);
        float pickupTimeCost = newOrder.emptyDur;
        float unloadTimeCost = 0;

        // Calculate change: costs - gains (so a negative result is good!)
        return (newDistanceCost + pickupTimeCost + unloadTimeCost) - (currentDistanceGain + pickupCostGain);
    }

    /// <summary>
    /// Calculate the time change when removing an order between prev and next.
    /// </summary>
    public static float TimeChangeRemove(DoublyNode<Order> routeOrder)
    {
        Order prev = routeOrder.prev.value;
        Order current = routeOrder.value;
        Order next = routeOrder.next.value;

        // Time decreases
        float pickupTimeGain = current.emptyDur;
        float currentDistanceGain = prev.DistanceTo(current) + current.DistanceTo(next);
        float unloadTimeGain = 0;

        // time increases
        float newDistanceCost = prev.DistanceTo(next);

        // Calcuate change: This number means how much time is spend more/less when this order is removed.
        return newDistanceCost - (pickupTimeGain + currentDistanceGain + unloadTimeGain);
    }

    /// <summary>
    /// Calculate the cost change for removing an order between prev and next.
    /// </summary>
    public static float CostChangeRemove(DoublyNode<Order> routeOrder)
    {
        Order prev = routeOrder.prev.value,
            current = routeOrder.value,
            next = routeOrder.next.value;

        // Gains
        float currentDistanceGain = prev.DistanceTo(current) + current.DistanceTo(next);
        float pickupTimeGain = current.emptyDur;

        // Costs
        float newDistanceCost = prev.DistanceTo(next);
        float pickupCost = current.emptyDur * 3;

        // Calculate change: costs - gains (so a negative result is good!)
        return (newDistanceCost + pickupCost) - (currentDistanceGain + pickupTimeGain);
    }

    /// <summary>
    /// Calculate the time change when swapping orders.
    /// </summary>
    public static float TimeChangeSwap(DoublyNode<Order> oldRouteOrder, DoublyNode<Order> newRouteOrder)
    {
        Order prev = oldRouteOrder.prev.value,
            oldOrder = oldRouteOrder.value,
            next = oldRouteOrder.next.value,
            newOrder = newRouteOrder.value;

        // Time decreases
        float pickupTimeGain = oldOrder.emptyDur;
        float oldDistanceGain = prev.DistanceTo(oldOrder) + oldOrder.DistanceTo(next);

        // Time increases
        float pickupTimeCost = newOrder.emptyDur;
        float newDistanceCost = prev.DistanceTo(newOrder) + newOrder.DistanceTo(next);

        // Calculate and return the total cost of the swap for the route of the oldRouteOrder.
        return (pickupTimeCost + newDistanceCost) - (pickupTimeGain + oldDistanceGain);
    }

    // time chang of shifting 'routeOrder' before 'routeOrder2' within same trips (pickup times are unchanged)
    public static float TimeChangePureShiftWithinTrip(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2) {
        Order order = routeOrder.value,
            order2 = routeOrder2.value;

        bool secondFollowsFirst = routeOrder.next.value == order2;
        bool firstFollowsSecond = routeOrder.prev.value == order2;

        float oldTime, newTime;
        if (secondFollowsFirst) 
            return 0f;                      // 'routeOrder' is already placed in front of 'routeorder2'
        else if (firstFollowsSecond)
        {
            oldTime = routeOrder2.prev.value.DistanceTo(order2) + order2.DistanceTo(order) + order.DistanceTo(routeOrder.next.value);   // old time of order2 before order1
            newTime = routeOrder2.prev.value.DistanceTo(order) + order.DistanceTo(order2) + order2.DistanceTo(routeOrder.next.value);   // new time of order1 before order2
            return (newTime - oldTime);     // pickup times are unchanged
        }
        else {
            oldTime = (routeOrder.prev.value.DistanceTo(order) + order.DistanceTo(routeOrder.next.value))       // old time to+from order1
                        + routeOrder2.prev.value.DistanceTo(order2);                                            // old time to order2
            newTime = (routeOrder2.prev.value.DistanceTo(order) + order.DistanceTo(order2))                     // new time to+from order1 shifted before order2
                        + routeOrder.prev.value.DistanceTo(routeOrder.next.value);                              // new time stiching prev and next of order1 together
            return (newTime - oldTime);     // pickup times are unchanged
        }
    }

    // time chang of shifting 'routeOrder' before 'routeOrder2' between different trips (pickup times are unchanged)
    public static (float, float) TimeChangePureShiftBetweenTrips(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2) {
        Order order = routeOrder.value,
            order2 = routeOrder2.value;

        float timeChangeRemove, timeChangeInsert;
        timeChangeRemove = routeOrder.prev.value.DistanceTo(routeOrder.next.value)                              // new time stiching prev and next of order1 together
                    - (routeOrder.prev.value.DistanceTo(order) + order.DistanceTo(routeOrder.next.value))       // old time to+from order1
                    - order.emptyDur;                                                                           // old time of picking up order1
        timeChangeInsert = (routeOrder2.prev.value.DistanceTo(order) + order.DistanceTo(order2))                // new time to+from order1 shifted before order2
                    + order.emptyDur                                                                            // new time of picking up order1
                    - routeOrder2.prev.value.DistanceTo(order2);                                                // old time to order2
      
        return (timeChangeRemove, timeChangeInsert);
    }

    /// <summary>
    /// Calculate the time change when shifting orders.
    /// </summary>
    public static float TimeChangeShift(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2)
    {
        Order order = routeOrder.value,
            order2 = routeOrder2.value;

        bool secondFollowsFirst = routeOrder.next.value == order2;
        bool firstFollowsSecond = routeOrder.prev.value == order2;
        bool areFollowUps = secondFollowsFirst || firstFollowsSecond;

        if (!areFollowUps) // if not following each other up in same route, time change of shift is equal to time change of swap for both
            return Schedule.TimeChangeSwap(routeOrder, routeOrder2) + Schedule.TimeChangeSwap(routeOrder2, routeOrder);

        // in same route order shift, order empty durations are disregarded due to having no effect in time change
        float oldTime, newTime;
        if (secondFollowsFirst)
        {
            oldTime = routeOrder.prev.value.DistanceTo(order) + order.DistanceTo(order2) + order2.DistanceTo(routeOrder2.next.value);
            newTime = routeOrder.prev.value.DistanceTo(order2) + order2.DistanceTo(order) + order.DistanceTo(routeOrder2.next.value);
            return (newTime - oldTime);
        }
        else
        {
            oldTime = routeOrder2.prev.value.DistanceTo(order2) + order2.DistanceTo(order) + order.DistanceTo(routeOrder.next.value);
            newTime = routeOrder2.prev.value.DistanceTo(order) + order.DistanceTo(order2) + order2.DistanceTo(routeOrder.next.value);
            return (newTime - oldTime);
        }

    }

    /// <summary>
    /// Calculate the cost change when swapping orders.
    /// </summary>
    public static float CostChangeSwap(DoublyNode<Order> oldRouteOrder, DoublyNode<Order> newRouteOrder)
    {
        Order prev = oldRouteOrder.prev.value,
            oldOrder = oldRouteOrder.value,
            next = oldRouteOrder.next.value,
            newOrder = newRouteOrder.value;

        // Gains
        float oldDistanceGain = prev.DistanceTo(oldOrder) + oldOrder.DistanceTo(next);
        float pickuptTimeGain = oldOrder.emptyDur;

        // Costs
        float newDistanceCost = prev.DistanceTo(newOrder) + newOrder.DistanceTo(next);
        float pickupTimeCost = newOrder.emptyDur;

        // Calculate change: costs - gains (so a negative result is good!)
        return (newDistanceCost + pickupTimeCost) - (oldDistanceGain + pickuptTimeGain);
    }

    /// <summary>
    /// cost change of shifting orders equals time change, because emptying-duration is indifferent in same route order shift 
    /// </summary>
    public static float CostChangeShift(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2)
    {
        return TimeChangeShift(routeOrder, routeOrder2);
    }


}
