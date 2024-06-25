using GatherBuddy.Sync.Models;
using GatherBuddy.Sync.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using static Grpc.Core.Metadata;

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

        //[Function("Process")]
        //public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        //{
        //    await ProcessTable(CatchLogContainerName);
        //}

        private async Task ProcessTable(string table)
        {
            var biteTimes = new Dictionary<string, BiteTimeTableEntity>();
            var processedEntities = new List<FishRecordTableEntity>();
            var entitiesToReprocess = new List<FishRecordTableEntity>();
            var entities = (await _dataService.QueryAllAsync<FishRecordTableEntity>(table)).ToList();
            var cachePopulationTasks = entities.GroupBy(x => GetBiteTimeDictKey(GetBiteTimePartitionKey(x), GetBiteTimeRowKey(x)))
                .Select(g => g.First())
                .Select(x => Task.Run(async () => {
                    var biteTime = await _dataService.ReadAsync<BiteTimeTableEntity>(BiteTimeTableEntity.BiteTimeTableName, GetBiteTimePartitionKey(x), GetBiteTimeRowKey(x));
                    if (biteTime != null) // else create a new one based on the current entity
                    {
                        biteTimes[GetBiteTimeDictKey(GetBiteTimePartitionKey(x), GetBiteTimeRowKey(x))] = biteTime;
                    }
                }));

            await Task.WhenAll(cachePopulationTasks);
            foreach (var entity in entities)
            {
                var biteTimePartitionKey = GetBiteTimePartitionKey(entity);
                var biteTimeRowKey = GetBiteTimeRowKey(entity);
                var biteTimeDictKey = GetBiteTimeDictKey(biteTimePartitionKey, biteTimeRowKey);
                var biteTime = GetBiteTime(biteTimes, entity, biteTimePartitionKey, biteTimeRowKey, biteTimeDictKey);

                biteTime.Update((ushort)entity.BiteTime);
                processedEntities.Add(entity);
            }


            var dirtyBiteTimes = biteTimes.Values.Where(x => x.Dirty);
            await _dataService.UpsertBatchAsync(BiteTimeTableEntity.BiteTimeTableName, dirtyBiteTimes);
            await _dataService.DeleteBatchAsync(table, processedEntities);
        }

        private static string GetBiteTimeDictKey(string biteTimePartitionKey, string biteTimeRowKey)
        {
            return string.Join(',', biteTimePartitionKey, biteTimeRowKey);
        }

        private static string GetBiteTimeRowKey(FishRecordTableEntity entity)
        {
            return string.Join('_', entity.BaitItemId, entity.CatchItemId, entity.Chum);
        }

        private static string GetBiteTimePartitionKey(FishRecordTableEntity entity)
        {
            return entity.FishingSpotId.ToString();
        }

        private BiteTimeTableEntity GetBiteTime(Dictionary<string, BiteTimeTableEntity> biteTimes, FishRecordTableEntity entity, string biteTimePartitionKey, string biteTimeRowKey, string biteTimeDictKey, bool returnOnCacheMiss = true)
        {
            if (biteTimes.ContainsKey(biteTimeDictKey))
            {
                return biteTimes[biteTimeDictKey];
            }

            biteTimes[biteTimeDictKey] = new BiteTimeTableEntity(entity, biteTimePartitionKey, biteTimeRowKey);
            return biteTimes[biteTimeDictKey];
        }
    }
}
