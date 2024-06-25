using GatherBuddy.Sync.Enums;

namespace GatherBuddy.Sync.Models
{

    public struct FishRecord
    {
        public uint ContentIdHash;
        public ushort Gathering;
        public ushort Perception;
        public bool Valid;
        public int TimeStamp;
        public uint BaitItemId;
        public ushort FishingSpotId;
        public bool Snagging;
        public bool Chum;
        public bool Intuition;
        public bool FishEyes;
        public bool IdenticalCast;
        public bool SurfaceSlap;
        public bool PrizeCatch;
        public bool Patience;
        public bool Patience2;
        public ushort BiteTime;
        public BiteType Tug;
        public HookSet HookSet;
        public uint CatchItemId;
        public byte Amount;
        public float Size;
        public bool Collectible;
        public bool Large;
    }
}