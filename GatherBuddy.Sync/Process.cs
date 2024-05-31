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
        private readonly ITableService _tableService;

        public Process(ILoggerFactory loggerFactory, ITableService tableService)
        {
            _logger = loggerFactory.CreateLogger<Process>();
            _tableService = tableService;
        }

        [Function("ProcessManual")]
        public async Task<IActionResult> RunManual([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            await ProcessTables();
            return new OkResult();
        }

        [Function("Process")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            await ProcessTables();
        }

        private async Task ProcessTables()
        {
            var tables = await _tableService.ListTablesAsync();
            var tableNames = tables.Where(x => x.Name.StartsWith(FishRecordTableEntity.TablePrefix)).Select(x => x.Name);
            foreach (var table in tableNames)
            {
                await ProcessTable(table);
            }
        }

        private async Task ProcessTable(string table)
        {
            var biteTimes = new Dictionary<string, BiteTimeTableEntity>();
            var processedEntities = new List<FishRecordTableEntity>();
            await foreach (var entity in _tableService.QueryAllAsync<FishRecordTableEntity>(table))
            {
                var biteTimePartitionKey = string.Join('_', entity.FishingSpotId.ToString(), entity.BaitItemId);
                var biteTimeRowKey = entity.CatchItemId.ToString();
                var biteTimeDictKey = string.Join(',', biteTimePartitionKey, biteTimeRowKey);
                var biteTime = await GetBiteTime(biteTimes, entity, biteTimePartitionKey, biteTimeRowKey, biteTimeDictKey);

                biteTime.Update(entity.BiteTime);
                processedEntities.Add(entity);
            }

            var dirtyBiteTimes = biteTimes.Values.Where(x => x.Dirty);
            await _tableService.UpsertBatchAsync(BiteTimeTableEntity.BiteTimeTableName, dirtyBiteTimes);
            await _tableService.DeleteBatchAsync(table, processedEntities);
        }

        private async Task<BiteTimeTableEntity> GetBiteTime(Dictionary<string, BiteTimeTableEntity> biteTimes, FishRecordTableEntity entity, string biteTimePartitionKey, string biteTimeRowKey, string biteTimeDictKey)
        {
            if (biteTimes.ContainsKey(biteTimeDictKey))
            {
                return biteTimes[biteTimeDictKey];
            }

            // try fetch the bite time key
            var biteTime = await _tableService.ReadAsync<BiteTimeTableEntity>(BiteTimeTableEntity.BiteTimeTableName, biteTimePartitionKey, biteTimeRowKey);
            if (biteTime == null) // else create a new one based on the current entity
            {
                biteTime = new BiteTimeTableEntity(entity, biteTimePartitionKey, biteTimeRowKey);
            }

            biteTimes[biteTimeDictKey] = biteTime;
            return biteTime;
        }
    }
}
