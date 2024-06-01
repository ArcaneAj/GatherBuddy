using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using GatherBuddy.Sync.Utilities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GatherBuddy.Sync.Services
{
    internal partial class TableService : ITableService
    {
        public async Task<Response> UpsertAsync<T>(string tableName, T entity) where T : ITableEntity
        {
            var tableClient = await GetTableAsync(tableName);
            return await tableClient.UpsertEntityAsync(entity);
        }

        public async Task<Response<IReadOnlyList<Response>>?> UpsertBatchAsync<T>(string tableName, IEnumerable<T> entities) where T : ITableEntity
        {
            if (!entities.Any())
            {
                return null;
            }

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
            var timer = Stopwatch.StartNew();
            var tableClient = await GetTableAsync(tableName);
            var result = new List<T>();
            await foreach (var page in tableClient.QueryAsync<T>(x => x.PartitionKey == partitionKey).AsPages())
            {
                result.AddRange(page.Values);
            }

            _telemetry.FinishTimerAndLog(timer);
            return result;
        }

        public async Task<Response<IReadOnlyList<Response>>?> DeleteBatchAsync<T>(string tableName, IEnumerable<T> entities) where T : ITableEntity
        {
            if (!entities.Any())
            {
                return null;
            }

            var tableClient = await GetTableAsync(tableName);
            var transaction = entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e));

            return await tableClient.SubmitTransactionAsync(transaction);
        }

        public async Task<IEnumerable<TableItem>> ListTablesAsync()
        {
            var tables = new List<TableItem>();
            await foreach (var page in _serviceClient.QueryAsync().AsPages())
            {
                tables.AddRange(page.Values);
            }

            return tables;
        }

        public async IAsyncEnumerable<T> QueryAllAsync<T>(string tableName) where T : class, ITableEntity
        {
            var tableClient = await GetTableAsync(tableName);
            await foreach (var page in tableClient.QueryAsync<T>().AsPages())
            {
                foreach (var item in page.Values)
                {
                    yield return item;
                }
            }
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
