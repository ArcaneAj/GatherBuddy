using Azure;
using Azure.Data.Tables;

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
        public BiteTimeTableEntity(FishRecordTableEntity fishRecord) {
            PartitionKey = fishRecord.FishingSpotId.ToString();
            RowKey = fishRecord.PartitionKey;
            Max = fishRecord.BiteTime;
            Min = fishRecord.BiteTime;
            Dirty = true;
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public long Max { get; set; }
        public long Min { get; set; }
        internal bool Dirty { get; set; }

        internal void Update(long biteTime)
        {
            if (biteTime < Min)
            {
                Min = biteTime;
                Dirty = true;
                return;
            }

            if (biteTime > Max)
            {
                Max = biteTime;
                Dirty = true;
                return; 
            }
        }
    }
}
