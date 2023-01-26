using System;
using System.Collections.Generic;

internal class SmartArray<T>
{
    public int length = 0;
    public List<T> array = new List<T>();

    public void Add(T item)
    {
        if (array.Count == length)
            array.Add(item);
        else
            array[length] = item;
        length++;
        //Console.WriteLine("Successful add at: " + (length - 1));
    }

    public void Remove(int index)
    {
        if (length > 0)
        {
            array[index] = array[length - 1];
            length--;
            //Console.WriteLine("Successful remove at: " + index);
        }
    }

    public (T, int) GetRandom(int start = 0)
    {
        if (length == 0)
            return (default(T), -1);

        int index = Program.random.Next(start, length);
        return (array[index], index);
    }

    public int FindIndex(T item)
    {
        for(int i = 0; i < length; i++)
            if (array[i].Equals(item))
                return i;
        return -1;
    }

    public void Update (T item, int index)
    {
        array[index] = item;
    }
}
