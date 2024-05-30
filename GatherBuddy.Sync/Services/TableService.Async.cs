using Azure;
using Azure.Data.Tables;
using GatherBuddy.Sync.Utilities;
using Microsoft.Extensions.Logging;

namespace GatherBuddy.Sync.Services
{
    internal partial class TableService : ITableService
    {
        public async Task<Response> UpsertAsync<T>(string tableName, T entity) where T : ITableEntity
        {
            var tableClient = await GetTableAsync(tableName);
            return await tableClient.UpsertEntityAsync(entity);
        }

        public async Task<Response<IReadOnlyList<Response>>> UpsertBatchAsync<T>(string tableName, IEnumerable<T> entities) where T : ITableEntity
        {
            var tableClient = await GetTableAsync(tableName);
            var transaction = entities.Select(e => new TableTransactionAction(TableTransactionActionType.UpsertReplace, e));

            return await tableClient.SubmitTransactionAsync(transaction);
        }

        public async Task<T?> ReadAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity
        {
            var tableClient = await GetTableAsync(tableName);
            return await tableClient.QueryAsync<T>(x => x.PartitionKey == partitionKey && x.RowKey == rowKey).FirstOrDefault();
        }

        public async Task<IEnumerable<T>> ReadAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity
        {
            var tableClient = await GetTableAsync(tableName);
            var result = new List<T>();
            await foreach (var page in tableClient.QueryAsync<T>(x => x.PartitionKey == partitionKey).AsPages())
            {
                result.AddRange(page.Values);
            }

            return result;
        }

        private async Task<TableClient> GetTableAsync(string tableName)
        {
            if (_tableItems.ContainsKey(tableName)) { return _tableItems[tableName]; }

            var tableClient = _serviceClient.GetTableClient(tableName);
            _tableItems[tableName] = tableClient;
            try
            {
                // Faster to ask for forgiveness than to ask permission
                await tableClient.CreateAsync();
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status != 409) // If it exists already (409) we can ignore the exception
                {
                    throw;
                }
            }

            _logger.LogInformation($"The created table's name is {tableClient.Name}.");
            return tableClient;
        }
    }
}
