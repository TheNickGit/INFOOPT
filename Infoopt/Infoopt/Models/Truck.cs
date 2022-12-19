class Truck
{
    public Schedule schedule;
    public static float unloadTime = 1800f;

    /// <summary>
    /// Constructor.
    /// </summary>
    public Truck()
    {
        this.schedule = new Schedule(unloadTime);
    }
}
