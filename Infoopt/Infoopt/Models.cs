using System;


namespace Infoopt
{

    class Order
    {

        public string place;            // place name of order
        public int nr,                  // trivial order id
            freq,                       // weekly frequency of order
            binAmt,                     // amount of bins
            binVol,                     // volume of bins
            distId,                     // id of distance of order in distance-matrix (NOTE, DIFFERENT ORDERS MIGHT HAVE SAME DISTANCE ID ENTRY)
            spot;                       // TODO: Deze is er alleen even als tussentijdse oplossing om orders te koppelen aan hun plek in de orders array
        public float emptyDur;          // time it takes to empty the bins of this order
        public (int X, int Y) coord;    // coordinates of order


        // distances to all other orders (index = distId of other order)
        public (int dist, int travelDur)[] distancesToOthers;

        // instead use this helper method to get distance-data from one order to another order
        public (int dist, int travelDur) distanceTo(Order other)
            => this.distancesToOthers[other.distId];


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
            this.emptyDur = emptyDur;
            this.distId = distId;
            this.coord = (xCoord, yCoord);
        }

        // TODO: replace by console-input line format for route checker?
        public override string ToString()
        {
            return String.Format("{0} | {1} [freq:{2}, amt:{3}, vol:{4}]",
                this.distId.ToString().PadLeft(4),
                this.place.PadRight(24),
                this.freq.ToString().PadLeft(2),
                this.binAmt.ToString().PadLeft(2),
                this.binVol.ToString().PadLeft(4)
            );
        }

    }
}
