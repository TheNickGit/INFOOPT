using System;

internal class SmartArray<T>
{
    public int length = 0;
    public T[] array;
    //public List<T> array = new List<T>();


    public SmartArray(int length)
    {
        array = new T[length];
    }

    public void Add(T item)
    {
        //if (array.Count == length)
        //    array.Add(item);
        //else
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
        }
        //Console.WriteLine("Successful remove at: " + index);
    }

    public (T, int) GetRandom()
    {
        if (length == 0)
            return (default(T), -1);

        int index = Program.random.Next(length);
        return (array[index], index);
    }
}
