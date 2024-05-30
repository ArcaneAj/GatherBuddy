using Azure;
using Azure.Data.Tables;

namespace GatherBuddy.Sync.Services
{
    public interface ITableService
    {
        Response Upsert<T>(string tableName, T entity) where T : ITableEntity;
        Response<IReadOnlyList<Response>> UpsertBatch<T>(string tableName, IEnumerable<T> entities) where T : ITableEntity;
        T? Read<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity;
        IEnumerable<T> Read<T>(string tableName, string partitionKey) where T : class, ITableEntity;
        Task<Response> UpsertAsync<T>(string tableName, T entity) where T : ITableEntity;
        Task<Response<IReadOnlyList<Response>>> UpsertBatchAsync<T>(string tableName, IEnumerable<T> entities) where T : ITableEntity;
        Task<T?> ReadAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity;
        Task<IEnumerable<T>> ReadAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity;
    }
}
