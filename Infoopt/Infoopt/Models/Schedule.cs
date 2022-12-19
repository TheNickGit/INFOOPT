using System;

class Schedule
{
    public Route[] weekRoutes = new Route[5];

    /// <summary>
    /// init each route of the weekschedule by setting its start and stop order, and unloading time
    /// </summary>
    public Schedule(float truckUnloadTime)
    {
        foreach (int day in Enum.GetValues(typeof(Day)))
        {
            this.weekRoutes[day] = new Route(Program.startOrder, Program.stopOrder);
            this.weekRoutes[day].timeToComplete += truckUnloadTime;
        }
    }

    /// <summary>
    /// Display all weekroutes (custom print)
    /// </summary>
    public string Display()
    {
        string msg = "";
        int day = 0;
        foreach (Route dayRoute in this.weekRoutes)
        {
            msg += $"----- {(Day)(day++)}  ({Math.Round(dayRoute.timeToComplete, 1)} sec. | {Math.Round(dayRoute.timeToComplete / 60, 1)} min.) ------\n{dayRoute.Display()}";
        }
        return msg;
    }

    /// <summary>
    /// loop through the week routes and accumulate their time to complete
    /// </summary>
    public float TimeCost()
    {
        float cost = 0.0f;
        foreach (Route dayRoute in this.weekRoutes)
        {
            cost += dayRoute.timeToComplete;
        }
        return cost;
    }

    /// <summary>
    /// Calculate the time change for adding an order between prev and next.
    /// </summary>
    public static float TimeChangePutBeforeOrder(Order newOrder, DoublyNode<Order> routeOrder)
    {
        Order prev = routeOrder.prev.value,
            current = routeOrder.value;

        // Time decreases
        float currentDistanceGain = prev.DistanceTo(current);

        // Time increases
        float newDistanceCost = (prev.DistanceTo(newOrder) + newOrder.DistanceTo(current));
        float pickupTimeCost = newOrder.emptyDur;

        // Calculate change: This number means how much additional time is spend if the order is added
        return (newDistanceCost + pickupTimeCost) - currentDistanceGain;
    }

    /// <summary>
    /// Calculate the cost change for adding an order between prev and next.
    /// </summary>
    public static float CostChangePutBeforeOrder(Order newOrder, DoublyNode<Order> routeOrder)
    {
        Order prev = routeOrder.prev.value,
            current = routeOrder.value;

        // Gains
        float currentDistanceGain = prev.DistanceTo(current);
        float pickupCostGain = newOrder.emptyDur * 3;

        // Costs
        float newDistanceCost = (prev.DistanceTo(newOrder) + newOrder.DistanceTo(current));
        float pickupTimeCost = newOrder.emptyDur;

        // Calculate change: costs - gains (so a negative result is good!)
        return (newDistanceCost + pickupTimeCost) - (currentDistanceGain + pickupCostGain);
    }

    /// <summary>
    /// Calculate the time change when removing an order between prev and next.
    /// </summary>
    public static float TimeChangeRemoveOrder(DoublyNode<Order> routeOrder)
    {
        Order prev = routeOrder.prev.value;
        Order current = routeOrder.value;
        Order next = routeOrder.next.value;

        // Time decreases
        float pickupTimeGain = current.emptyDur;
        float currentDistanceGain = (prev.DistanceTo(current) + current.DistanceTo(next));

        // time increases
        float newDistanceCost = prev.DistanceTo(next);

        // Calcuate change: This number means how much time is spend more/less when this order is removed.
        return newDistanceCost - (pickupTimeGain + currentDistanceGain);
    }

    /// <summary>
    /// Calculate the cost change for removing an order between prev and next.
    /// </summary>
    public static float CostChangeRemoveOrder(DoublyNode<Order> routeOrder)
    {
        Order prev = routeOrder.prev.value,
            current = routeOrder.value,
            next = routeOrder.next.value;

        // Gains
        float currentDistanceGain = (prev.DistanceTo(current) + current.DistanceTo(next));
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
    public static float TimeChangeSwapOrders(DoublyNode<Order> oldRouteOrder, DoublyNode<Order> newRouteOrder)
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

    /// <summary>
    /// Calculate the time change when shifting orders.
    /// </summary>
    public static float TimeChangeShiftOrders(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2)
    {
        Order order = routeOrder.value,
            order2 = routeOrder2.value;

        bool secondFollowsFirst = routeOrder.next.value == order2;
        bool firstFollowsSecond = routeOrder.prev.value == order2;
        bool areFollowUps = secondFollowsFirst || firstFollowsSecond;

        if (!areFollowUps) // if not following each other up in same route, time change of shift is equal to time change of swap for both
            return Schedule.TimeChangeSwapOrders(routeOrder, routeOrder2) + Schedule.TimeChangeSwapOrders(routeOrder2, routeOrder);

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
    public static float CostChangeSwapOrders(DoublyNode<Order> oldRouteOrder, DoublyNode<Order> newRouteOrder)
    {
        Order prev = oldRouteOrder.prev.value,
            oldOrder = oldRouteOrder.value,
            next = oldRouteOrder.next.value,
            newOrder = newRouteOrder.value;

        // Gains
        float oldDistanceGain = (prev.DistanceTo(oldOrder) + oldOrder.DistanceTo(next));
        float pickuptTimeGain = oldOrder.emptyDur;

        // Costs
        float newDistanceCost = (prev.DistanceTo(newOrder) + newOrder.DistanceTo(next));
        float pickupTimeCost = newOrder.emptyDur;

        // Calculate change: costs - gains (so a negative result is good!)
        return (newDistanceCost + pickupTimeCost) - (oldDistanceGain + pickuptTimeGain);
    }

    /// <summary>
    /// cost change of shifting orders equals time change, because emptying-duration is indifferent in same route order shift 
    /// </summary>
    public static float CostChangeShiftOrders(DoublyNode<Order> routeOrder, DoublyNode<Order> routeOrder2)
    {
        return TimeChangeShiftOrders(routeOrder, routeOrder2);
    }


}
