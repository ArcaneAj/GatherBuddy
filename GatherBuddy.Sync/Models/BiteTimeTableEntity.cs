using Azure;
using GatherBuddy.Sync.Services;
using Newtonsoft.Json;

namespace GatherBuddy.Sync.Models
{
    public class BiteTimeTableEntity : IEntity
    {
        [JsonProperty("id")]
        public string Id { get { return RowKey; } }
        public BiteTimeTableEntity()
        {
            PartitionKey = string.Empty;
            RowKey = string.Empty;
            Dirty = false;
            Max = 0;
            Min = ushort.MaxValue;
            CatchHistory = [];
        }

        public BiteTimeTableEntity(FishRecordTableEntity fishRecord, string biteTimePartitionKey, string biteTimeRowKey) {
            PartitionKey = biteTimePartitionKey;
            RowKey = biteTimeRowKey;
            Max = 0;
            Min = ushort.MaxValue;
            Chum = fishRecord.Chum;
            BaitItemId = fishRecord.BaitItemId;
            CatchItemId = fishRecord.CatchItemId;
            Dirty = true;
            CatchHistory = [];
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public ushort Max { get; set; }
        public ushort Min { get; set; }
        public bool Chum { get; set; }
        public long BaitItemId { get; set; }
        public long CatchItemId { get; set; }
        public long FishingSpotId => long.Parse(PartitionKey);

        public Dictionary<long, long> CatchHistory { get; set; }
        internal bool Dirty { get; set; }
        internal const string BiteTimeTableName = "bitetimes";

        public void Update(ushort biteTime)
        {
            // Implicitly rounds down, so the 0 bucket is everything from 0 to 999ms
            var bucket = biteTime / 1000;
            if (CatchHistory.ContainsKey(bucket))
            {
                CatchHistory[bucket]++;
            }
            else
            {
                CatchHistory[bucket] = 1;
            }

            UpdateMinMax(biteTime);
        }

        public void UpdateMinMax(ushort biteTime)
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
}
