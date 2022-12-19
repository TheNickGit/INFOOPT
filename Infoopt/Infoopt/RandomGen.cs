using System;

class RandomGen : Random
{
    /// <summary>
    /// Returns a random day.
    /// </summary>
    public Day RandomDay()
    {
        Day[] days = (Day[])Enum.GetValues(typeof(Day));
        return days[Next(days.Length)];
    }

    /// <summary>
    /// Gives an array of days fitting for an order of the given frequency.
    /// </summary>
    public Day[] RandomDays(int freq)
    {
        if (freq == 1)
        {
            Day[] days = new Day[1];
            days[0] = RandomDay();
            return days;
        }

        else if (freq == 2)
        {
            Day[] allDays = (Day[])Enum.GetValues(typeof(Day));
            Day[] days = new Day[2];
            days[0] = allDays[Next(2)]; // Mon or Tue
            if (days[0] == Day.Mon)
                days[1] = Day.Thu;  // Mon + Thu combo
            else
                days[1] = Day.Fri; // Tue + Fri combo
            return days;
        }

        else if (freq == 3)
        {
            Day[] days = new Day[3];
            days[0] = Day.Mon;
            days[1] = Day.Wed;
            days[2] = Day.Fri;
            return days;
        }

        else if (freq == 4)
        {
            Day[] allDays = (Day[])Enum.GetValues(typeof(Day));
            Day[] days = new Day[4];
            Day excludedDay = allDays[Next(5)];
            int j = 0;
            for (int i = 0; i < 4; i++)
            {
                if (allDays[j] == excludedDay)
                    j++;
                days[i] = allDays[j];
                j++;
            }
            return days;
        }

        else
            return null;
    }
}