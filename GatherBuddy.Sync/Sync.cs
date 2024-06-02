using GatherBuddy.FishTimer;
using GatherBuddy.Sync.Models;
using GatherBuddy.Sync.Services;
using GatherBuddy.Sync.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace GatherBuddy.Sync
{
    public class Sync
    {
        private readonly ILogger<Sync> _logger;
        private readonly ITableService _tableService;
        private readonly Telemetry _telemetry;

        public Sync(ILogger<Sync> logger, Telemetry telemetry, ITableService tableService)
        {
            _logger = logger;
            _tableService = tableService;
            _telemetry = telemetry;
        }

        [Function("SyncWrite")]
        public async Task<IActionResult> Post([HttpTrigger(AuthorizationLevel.Function, "post", Route = "SyncWrite")] HttpRequest req)
        {
            var data = await new StreamReader(req.Body).ReadToEndAsync();
            var records = JsonConvert.DeserializeObject<List<FishRecord.JsonStruct>>(data);
            if (records == null)
            {
                return new BadRequestObjectResult("Could not parse json. Aborting import.");
            }


            var identifier = req.Headers["Identifier"].FirstOrDefault();
            if (string.IsNullOrEmpty(identifier))
            {
                return new StatusCodeResult(403);
            }

            var entities = records.Where(x => x.CatchItemId != 0).Select(x => FishRecordTableEntity.FromFishRecord(x, identifier)).ToList();

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
            var timer = Stopwatch.StartNew();
            var partitionKey = spotId.ToString();
            var baitTimes = await _tableService.ReadAsync<BiteTimeTableEntity>(BiteTimeTableEntity.BiteTimeTableName, partitionKey);
            var response = baitTimes.GroupBy(x => x.CatchItemId).ToDictionary(x => x.Key, x => x.ToDictionary(x => x.BaitItemId, x => x.MapTo()));
            _telemetry.FinishTimerAndLog(timer);
            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(response),
                ContentType = "application/json",
                StatusCode = 200
            };
        }
    }
}
