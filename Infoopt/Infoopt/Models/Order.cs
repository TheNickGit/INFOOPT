using System;
using System.Collections.Generic;

class Order
{
    public string place;            // place name of order
    public int nr,                  // trivial order id
        freq,                       // weekly frequency of order
        binAmt,                     // amount of bins
        binVol,                     // volume of bins
        distId,                     // id of distance of order in distance-matrix (NOTE, DIFFERENT ORDERS MIGHT HAVE SAME DISTANCE ID ENTRY)
        spot;
    public float emptyDur;          // time it takes to empty the bins of this order
    public (int X, int Y) coord;    // coordinates of order
    public bool available;          // signals whether the order is currently available to be taken

    public int[] distancesToOthers; // distances to all other orders (index = distId of other order)
    public int volume { get { return binAmt * binVol; } }

    /// <summary>
    /// Constructor.
    /// </summary>
    public Order(
                int nr, string place, int freq,
                int binAmt, int binVol, float emptyDur,
                int distId, int xCoord, int yCoord
            )
    {
        this.nr = nr;
        this.place = place;
        this.freq = freq;
        this.binAmt = binAmt;
        this.binVol = binVol;
        this.emptyDur = emptyDur * 60;
        this.distId = distId;
        this.coord = (xCoord, yCoord);
        this.available = true;
    }


    /// <summary>
    /// Display order specification (custom print)
    /// </summary>
    public string Display()
    {
        return String.Format("{0} | {1} [freq:{2}, amt:{3}, vol:{4}]",
            this.distId.ToString().PadLeft(4),
            this.place.PadRight(24),
            this.freq.ToString().PadLeft(2),
            this.binAmt.ToString().PadLeft(2),
            this.binVol.ToString().PadLeft(4)
        );
    }

    /// <summary>
    /// instead use this helper method to get distance-data from one order to another order
    /// </summary>
    public int DistanceTo(Order other)
    {
        if (this.distId == other.distId) return 0;
        return this.distancesToOthers[other.distId];
    }
}

