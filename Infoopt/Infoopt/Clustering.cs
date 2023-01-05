using System;
using System.Linq;


class Cluster2DKMeans
{

    public (float, float)[] centroids;
    public int[] assignments;


    public static float randFloatBetween(float min, float max)
        => (float)(min + ((max - min) * Program.random.NextDouble()));

    public static (float, float) rand2DFloatBetween(float xMin, float xMax, float yMin, float yMax)
        => (randFloatBetween(xMin, xMax), randFloatBetween(yMin, yMax));


    public static Cluster2DKMeans fromRandomCentroids(int nCentroids, float xMin = 0f, float xMax = 100f, float yMin = 0f, float yMax = 100f)
    {
        (float, float)[] centroids = new (float, float)[nCentroids];
        float dX = xMax - xMin, dY = yMax - yMin;
        float qdX = dX / 4, qdY = dY / 4;
        for (int i = 0; i < nCentroids; i++)
            centroids[i] = rand2DFloatBetween(xMin + qdX, xMax - qdX, yMin + qdY, yMax - qdY);
        return new Cluster2DKMeans(centroids);
    }


    public Cluster2DKMeans((float, float)[] centroids)
    {
        this.centroids = centroids;
    }


    public static float distanceToCentroid(float x, float y, (float, float) centroid)
        => (float)(Math.Pow(centroid.Item1 - x, 2) + Math.Pow(centroid.Item2 - y, 2));

    public int getClosestCentroid(float x, float y)
    {
        int centroid = -1;
        float distance, shortest = float.MaxValue;
        for (int j = 0; j < this.centroids.Length; j++)
        {
            distance = distanceToCentroid(x, y, this.centroids[j]);
            if (distance < shortest)
            {
                shortest = distance;
                centroid = j;
            }
        }
        return centroid;
    }


    public int Run((float, float)[] samples, int maxIterations, bool verbose = false)
    {
        // Run the clustering algorithm by assigning samples to a specific centroid, 
        // and consequentially recalculating the new locations of the centroids
        this.assignments = new int[samples.Length];
        this.AssignSamples(samples);
        int it = 0;
        for (; it < maxIterations; it++)
        {
            bool isFinished = this.ReCentre(samples);
            if (isFinished) break;
            this.AssignSamples(samples);
        }
        if (verbose)
            this.DescribeOutcome(samples.Length, this.centroids.Length, it);
        return it;
    }


    public void AssignSamples((float, float)[] samples)
    {
        // Assign each sample to their closest centroid
        for (int i = 0; i < samples.Length; i++)
        {
            (float x, float y) = samples[i];
            this.assignments[i] = this.getClosestCentroid(x, y);
        }
    }


    public bool ReCentre((float, float)[] samples)
    {
        // (Re)calculate new centroids as the mean 
        // of all sample points assigned to that specific centroid
        int nCentroids = this.centroids.Length;

        // Init amount of sample points assigned per centroid
        int[] nCentroidAssignments = new int[nCentroids];
        for (int j = 0; j < nCentroids; j++)
            nCentroidAssignments[j] = 0;

        // Init new centroids; will be accumulated by the locations of
        // the sample points assigned per centroid
        (float, float)[] newCentroids = new (float, float)[nCentroids];
        for (int j = 0; j < nCentroids; j++)
            newCentroids[j] = (0.0f, 0.0f);

        for (int i = 0; i < this.assignments.Length; i++)
        {
            int j = this.assignments[i];        // get centroid assigned to sample
            nCentroidAssignments[j] += 1;       // increment centroid assignment amount

            (float x, float y) = samples[i];    // get location of sample
            newCentroids[j].Item1 += x;             // accumulate centroid x
            newCentroids[j].Item2 += y;             // accumulate centroid y
        }

        // Check if centroids have changed (termination condition) and set new centroids
        bool haventChanged = true;
        for (int j = 0; j < nCentroids; j++)
        {

            // divide accumulated locations of sample points per centroid
            // by the amount of samples assigned to that centroid
            // to get the new centroid locations (mean of sample points per centroid)
            newCentroids[j].Item1 /= nCentroidAssignments[j];
            newCentroids[j].Item2 /= nCentroidAssignments[j];

            // check if centroid has changed
            haventChanged &= (newCentroids[j] == this.centroids[j]);
        }
        this.centroids = newCentroids;

        return haventChanged;
    }

    public void DescribeOutcome(int nSamples, int nClusters, int nIterations)
    {
        Console.Error.WriteLine($"# Clustered {nSamples} data-points for {nClusters} clusters within {nIterations} iterations");
        int centroid = 0;
        foreach ((float x, float y) c in this.centroids)
        {
            int nClusterAssignments = this.assignments.Where(v => v == centroid).Count();
            Console.Error.WriteLine($"\t- Cluster {centroid++} at ({c.x}, {c.y}) containing {nClusterAssignments} data-points");
        }
    }


}

