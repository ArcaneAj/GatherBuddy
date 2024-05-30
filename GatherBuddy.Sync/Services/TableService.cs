﻿using Azure;
using Azure.Data.Tables;
using GatherBuddy.Sync.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GatherBuddy.Sync.Services
{
    internal partial class TableService : ITableService
    {
        private readonly ILogger<TableService> _logger;
        private readonly TableServiceClient _serviceClient;

        private readonly ConcurrentDictionary<string, TableClient> _tableItems = [];

        public TableService(ILogger<TableService> logger, IConfiguration configuration)
        {
            _logger = logger;
            var connectionString = configuration["AzureWebJobsStorage"];
            _serviceClient = new TableServiceClient(connectionString);
        }

        public Response Upsert<T>(string tableName, T entity) where T : ITableEntity
        {
            var tableClient = GetTable(tableName);
            return tableClient.UpsertEntity(entity);
        }

        public Response<IReadOnlyList<Response>> UpsertBatch<T>(string tableName, IEnumerable<T> entities) where T : ITableEntity
        {
            var tableClient = GetTable(tableName);
            var transaction = entities.Select(e => new TableTransactionAction(TableTransactionActionType.UpsertReplace, e));

            return tableClient.SubmitTransaction(transaction);
        }

        public T? Read<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity
        {
            var tableClient = GetTable(tableName);
            return tableClient.Query<T>(x => x.PartitionKey == partitionKey && x.RowKey == rowKey).FirstOrDefault();
        }

        public IEnumerable<T> Read<T>(string tableName, string partitionKey) where T : class, ITableEntity
        {
            var tableClient = GetTable(tableName);
            return tableClient.Query<T>(x => x.PartitionKey == partitionKey);
        }

        private TableClient GetTable(string tableName)
        {
            if (_tableItems.ContainsKey(tableName)) { return _tableItems[tableName]; }

            var tableClient = _serviceClient.GetTableClient(tableName);
            _tableItems[tableName] = tableClient;
            try
            {
                // Faster to ask for forgiveness than to ask permission
                tableClient.Create();
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
