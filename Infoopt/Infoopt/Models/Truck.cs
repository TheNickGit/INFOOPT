class Truck
{
    public Schedule schedule;
    public static float unloadTime = 1800f;     // in seconds
    public static int volumeCapacity = 100_000; // in liters (includes compression)

    /// <summary>
    /// Constructor.
    /// </summary>
    public Truck()
    {
        this.schedule = new Schedule();
    }
}
