using GatherBuddy.Sync.Models;
using GatherBuddy.Sync.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GatherBuddy.Sync
{
    public class Process
    {
        private readonly ILogger _logger;
        private readonly IDataService _dataService;
        private const string CatchLogContainerName = "catchlog";

        public Process(ILoggerFactory loggerFactory, IDataService dataService)
        {
            _logger = loggerFactory.CreateLogger<Process>();
            _dataService = dataService;
        }

        [Function("ProcessManual")]
        public async Task<IActionResult> RunManual([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            await ProcessTable(CatchLogContainerName);
            return new OkResult();
        }

        [Function("Process")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            await ProcessTable(CatchLogContainerName);
        }

        private async Task ProcessTable(string table)
        {
            var biteTimes = new Dictionary<string, BiteTimeTableEntity>();
            var processedEntities = new List<FishRecordTableEntity>();
            await foreach (var entity in _dataService.QueryAllAsync<FishRecordTableEntity>(table))
            {
                var biteTimePartitionKey = entity.FishingSpotId.ToString();
                var biteTimeRowKey = entity.PartitionKey;
                var biteTimeDictKey = string.Join(',', biteTimePartitionKey, biteTimeRowKey);
                var biteTime = await GetBiteTime(biteTimes, entity, biteTimePartitionKey, biteTimeRowKey, biteTimeDictKey);

                biteTime.Update(entity.BiteTime, entity.Chum);
                processedEntities.Add(entity);
            }

            var dirtyBiteTimes = biteTimes.Values.Where(x => x.Dirty);
            await _dataService.UpsertBatchAsync(BiteTimeTableEntity.BiteTimeTableName, dirtyBiteTimes);
            await _dataService.DeleteBatchAsync(table, processedEntities);
        }

        private async Task<BiteTimeTableEntity> GetBiteTime(Dictionary<string, BiteTimeTableEntity> biteTimes, FishRecordTableEntity entity, string biteTimePartitionKey, string biteTimeRowKey, string biteTimeDictKey)
        {
            if (biteTimes.ContainsKey(biteTimeDictKey))
            {
                return biteTimes[biteTimeDictKey];
            }

            // try fetch the bite time key
            var biteTime = await _dataService.ReadAsync<BiteTimeTableEntity>(BiteTimeTableEntity.BiteTimeTableName, biteTimePartitionKey, biteTimeRowKey);
            if (biteTime == null) // else create a new one based on the current entity
            {
                biteTime = new BiteTimeTableEntity(entity, biteTimePartitionKey, biteTimeRowKey);
            }

            biteTimes[biteTimeDictKey] = biteTime;
            return biteTime;
        }
    }
}
