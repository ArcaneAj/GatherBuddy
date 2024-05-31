using GatherBuddy.FishTimer;
using GatherBuddy.Sync.Models;
using GatherBuddy.Sync.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GatherBuddy.Sync
{
    public class Sync
    {
        private readonly ILogger<Sync> _logger;
        private readonly ITableService _tableService;

        public Sync(ILogger<Sync> logger, ITableService tableService)
        {
            _logger = logger;
            _tableService = tableService;
        }

        [Function("SyncWrite")]
        public async Task<IActionResult> Post([HttpTrigger(AuthorizationLevel.Function, "post", Route = "SyncWrite/{name}")] HttpRequest req, string name)
        {
            var data = await new StreamReader(req.Body).ReadToEndAsync();
            var records = JsonConvert.DeserializeObject<List<FishRecord.JsonStruct>>(data);
            if (records == null)
            {
                return new BadRequestObjectResult("Could not parse json. Aborting import.");
            }

            var entities = records.Where(x => x.CatchItemId != 0).Select(x => FishRecordTableEntity.FromFishRecord(x, name!)).ToList();

            foreach (var entityGroup in entities.GroupBy(e => e.GetTableId()))
            {
                try
                {
                    await _tableService.UpsertBatchAsync(entityGroup.Key, entityGroup);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.ToString());
                }
            }

            return new OkObjectResult(entities.Select(x => x.CatchTimestamp));
        }

        [Function("SyncRead")]
        public async Task<IActionResult> Get([HttpTrigger(AuthorizationLevel.Function, "get", Route = "SyncRead/{spotId:int}")] HttpRequest req, int spotId)
        {
            //TODO: Cache this?
            var partitionKey = spotId.ToString();
            return new OkObjectResult(await _tableService.ReadAsync<BiteTimeTableEntity>(BiteTimeTableEntity.BiteTimeTableName, partitionKey));
        }
    }
}
