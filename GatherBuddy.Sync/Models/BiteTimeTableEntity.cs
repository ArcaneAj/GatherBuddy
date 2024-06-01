using Azure;
using Azure.Data.Tables;
using GatherBuddy.FishTimer;

namespace GatherBuddy.Sync.Models
{
    public class BiteTimeTableEntity : ITableEntity
    {
        public BiteTimeTableEntity()
        {
            PartitionKey = string.Empty;
            RowKey = string.Empty;
            Dirty = false;
        }

        public BiteTimeTableEntity(FishRecordTableEntity fishRecord, string biteTimePartitionKey, string biteTimeRowKey) {
            PartitionKey = biteTimePartitionKey;
            RowKey = biteTimeRowKey;
            Max = 0;
            Min = ushort.MaxValue;
            MaxChum = 0;
            MinChum = ushort.MaxValue;
            BaitItemId = fishRecord.BaitItemId;
            CatchItemId = fishRecord.CatchItemId;
            Dirty = true;
            Update(fishRecord.BiteTime, fishRecord.Chum);
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public long Max { get; set; }
        public long Min { get; set; }
        public long MaxChum { get; set; }
        public long MinChum { get; set; }
        public long BaitItemId { get; set; }
        public long CatchItemId { get; set; }
        internal bool Dirty { get; set; }
        internal const string BiteTimeTableName = "bitetimes";

        public void Update(long biteTime, bool chum)
        {
            if (chum)
            {
                if (biteTime < MinChum)
                {
                    MinChum = biteTime;
                    Dirty = true;
                }

                if (biteTime > MaxChum)
                {
                    MaxChum = biteTime;
                    Dirty = true;
                }
            }
            else
            {
                if (biteTime < Min)
                {
                    Min = biteTime;
                    Dirty = true;
                }

                if (biteTime > Max)
                {
                    Max = biteTime;
                    Dirty = true;
                }
            }
        }

        public FishRecordTimes.Times MapTo()
        {
            return new FishRecordTimes.Times
            {
                Min = (ushort)Min,
                Max = (ushort)Max,
                MinChum = (ushort)MinChum,
                MaxChum = (ushort)MaxChum,
            };
        }
    }
}
