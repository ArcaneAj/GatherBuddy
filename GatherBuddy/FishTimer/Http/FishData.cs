using System.Collections.Generic;
using static GatherBuddy.FishTimer.FishRecordTimes;

namespace GatherBuddy.FishTimer.Http
{
    public class FishData
    {
        public FishData() {
            CatchHistory = [];
        }

        public ushort Max { get; set; }
        public ushort Min { get; set; }
        public bool Chum { get; set; }
        public long BaitItemId { get; set; }
        public long CatchItemId { get; set; }
        public long FishingSpotId { get; set; }
        public Dictionary<long, long> CatchHistory { get; set; }

        public Times ToTimes()
        {
            if (this.Chum)
            {
                return new Times
                {
                    Max = 0,
                    Min = ushort.MaxValue,
                    MaxChum = this.Max,
                    MinChum = this.Min
                };
            }

            return new Times
            {
                Max = this.Max,
                Min = this.Min,
                MaxChum = 0,
                MinChum = ushort.MaxValue
            };
        }
    }
}
