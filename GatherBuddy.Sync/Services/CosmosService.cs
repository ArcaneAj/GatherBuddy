using Azure;
using Azure.Identity;
using GatherBuddy.Sync.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GatherBuddy.Sync.Services
{
    public class CosmosService : IDataService
    {
        private readonly ILogger<CosmosService> _logger;
        private readonly Telemetry _telemetry;
        private readonly CosmosClient _client;

        private readonly ConcurrentDictionary<string, Container> _containers = [];
        public CosmosService(ILogger<CosmosService> logger, Telemetry telemetry, IConfiguration configuration)
        {
            var connectionString = configuration["AzureWebJobsStorage"];
            _logger = logger;
            _telemetry = telemetry;
            _client = new CosmosClientBuilder(configuration["Cosmos"])
                .WithConnectionModeDirect()
                .WithBulkExecution(true)
                .Build();
        }

        private async Task<Container> GetContainerAsync(string containerName)
        {
            if (_containers.ContainsKey(containerName)) { return _containers[containerName]; }

            var database = _client.GetDatabase("gatherbuddy");
            var container = database.GetContainer(containerName);
            _containers[containerName] = container;
            try
            {
                // Faster to ask for forgiveness than to ask permission
                await database.CreateContainerAsync(new ContainerProperties(containerName, "/PartitionKey"));
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("409")) // If it exists already (409) we can ignore the exception
                {
                    throw;
                }
            }

            _logger.LogInformation($"The created container's name is {container.Id}.");
            return container;
        }

        public async Task DeleteBatchAsync<T>(string tableName, IEnumerable<T> entities) where T : IEntity
        {
            var concurrentTasks = new List<Task>();
            var container = await GetContainerAsync(tableName);

            foreach (var item in entities)
            {
                concurrentTasks.Add(container.DeleteItemStreamAsync(item.Id.ToString(), new PartitionKey(item.PartitionKey.ToString())));
            }

            await Task.WhenAll(concurrentTasks);
        }

        public async Task UpsertAsync<T>(string tableName, T entity)
        {
            var container = await GetContainerAsync(tableName);
            await container.UpsertItemAsync(entity);
        }

        public async Task UpsertBatchAsync<T>(string tableName, IEnumerable<T> entities) where T : IEntity
        {
            var concurrentTasks = new List<Task>();
            var container = await GetContainerAsync(tableName);

            foreach (var item in entities)
            {
                await container.UpsertItemAsync(item);
                //concurrentTasks.Add(container.UpsertItemAsync(item));
            }

            await Task.WhenAll(concurrentTasks);
        }

        public async IAsyncEnumerable<T> QueryAllAsync<T>(string tableName)
        {
            var container = await GetContainerAsync(tableName);
            var query = new QueryDefinition(query: "SELECT * FROM c");
            using var feed = container.GetItemQueryIterator<T>(
                queryDefinition: query
            );

            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                foreach (var item in response)
                {
                    yield return item;
                }
            }
        }

        public async Task<T?> ReadAsync<T>(string tableName, string partitionKey, string rowKey)
        {
            var container = await GetContainerAsync(tableName);
            try
            {
                var response = await container.ReadItemAsync<T>(
                    id: rowKey,
                    partitionKey: new PartitionKey(partitionKey)
                );
                return response.Resource;
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("404"))
                {
                    throw;
                }
            }
            return default;
        }

        public async Task<IEnumerable<T>> ReadAsync<T>(string tableName, string partitionKey)
        {
            var container = await GetContainerAsync(tableName);
            var queryDefinition = new QueryDefinition(query: "SELECT * FROM c");
            var feed = container.GetItemQueryIterator<T>(queryDefinition,
                requestOptions: new QueryRequestOptions()
                {
                    PartitionKey = new PartitionKey(partitionKey)
                });

            var results = new List<T>();

            while (feed.HasMoreResults)
            {
                var result = await feed.ReadNextAsync();
                results.AddRange(result.Resource);
            }

            return results;
        }
    }
}
