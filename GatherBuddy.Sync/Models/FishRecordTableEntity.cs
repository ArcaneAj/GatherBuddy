using Azure;
using Azure.Data.Tables;
using GatherBuddy.Enums;
using GatherBuddy.FishTimer;
using GatherBuddy.Sync.Utilities;

namespace GatherBuddy.Sync.Models
{
    public class FishRecordTableEntity : ITableEntity
    {
        public string PartitionKey
        {
            get
            {
                return string.Join('_', BaitItemId, CatchItemId);
            }
            set
            {
                var keys    = value.Split('_');
                BaitItemId  = uint.Parse(keys[0]);
                CatchItemId = uint.Parse(keys[1]);
            }
        }

        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        internal static string TablePrefix => "fishingspotid";

        public static FishRecordTableEntity FromFishRecord(FishRecord.JsonStruct record, string name)
        {
            return new()
            {
                RowKey         = string.Join('_', Base64Url.Encode(name), record.TimeStamp),
                ContentIdHash  = record.ContentIdHash,
                Gathering      = record.Gathering,
                Perception     = record.Perception,
                Valid          = record.Valid,
                CatchTimestamp = record.TimeStamp,
                BaitItemId     = record.BaitItemId,
                FishingSpotId  = record.FishingSpotId,
                Snagging       = record.Snagging,
                Chum           = record.Chum,
                Intuition      = record.Intuition,
                FishEyes       = record.FishEyes,
                IdenticalCast  = record.IdenticalCast,
                SurfaceSlap    = record.SurfaceSlap,
                PrizeCatch     = record.PrizeCatch,
                Patience       = record.Patience,
                Patience2      = record.Patience2,
                BiteTime       = record.BiteTime,
                Tug            = (int)record.Tug,
                HookSet        = (int)record.HookSet,
                CatchItemId    = record.CatchItemId,
                Amount         = record.Amount,
                Size           = record.Size,
                Collectible    = record.Collectible,
                Large          = record.Large,
            };
        }

        public static FishRecord.JsonStruct ToFishRecord(FishRecordTableEntity entity)
        {
            return new()
            {
                ContentIdHash = (uint)entity.ContentIdHash,
                Gathering     = (ushort)entity.Gathering,
                Perception    = (ushort)entity.Perception,
                Valid         = entity.Valid,
                TimeStamp     = entity.CatchTimestamp,
                BaitItemId    = (uint)entity.BaitItemId,
                FishingSpotId = (ushort)entity.FishingSpotId,
                Snagging      = entity.Snagging,
                Chum          = entity.Chum,
                Intuition     = entity.Intuition,
                FishEyes      = entity.FishEyes,
                IdenticalCast = entity.IdenticalCast,
                SurfaceSlap   = entity.SurfaceSlap,
                PrizeCatch    = entity.PrizeCatch,
                Patience      = entity.Patience,
                Patience2     = entity.Patience2,
                BiteTime      = (ushort)entity.BiteTime,
                Tug           = (BiteType)entity.Tug,
                HookSet       = (HookSet)entity.HookSet,
                CatchItemId   = (uint)entity.CatchItemId,
                Amount        = (byte)entity.Amount,
                Size          = (float)entity.Size,
                Collectible   = entity.Collectible,
                Large         = entity.Large,
            };
        }

        public string GetTableId()
        {
            return $"{TablePrefix}{FishingSpotId}";
        }

        public long ContentIdHash;
        public long Gathering;
        public long Perception;
        public long BaitItemId;
        public long FishingSpotId;
        public long BiteTime;
        public long CatchItemId;
        public int CatchTimestamp;
        public int Tug;
        public int HookSet;
        public int Amount;
        public bool Valid;
        public bool Snagging;
        public bool Chum;
        public bool Intuition;
        public bool FishEyes;
        public bool IdenticalCast;
        public bool SurfaceSlap;
        public bool PrizeCatch;
        public bool Patience;
        public bool Patience2;
        public bool Collectible;
        public bool Large;
        public double Size;
    }
}
