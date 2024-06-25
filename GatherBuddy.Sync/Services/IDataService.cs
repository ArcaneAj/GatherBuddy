namespace GatherBuddy.Sync.Services
{
    public interface IDataService
    {
        Task UpsertAsync<T>(string tableName, T entity);
        Task UpsertBatchAsync<T>(string tableName, IEnumerable<T> entities) where T : IEntity;
        Task<T?> ReadAsync<T>(string tableName, string partitionKey, string rowKey);
        Task<IEnumerable<T>> ReadAsync<T>(string tableName, string partitionKey);
        Task DeleteBatchAsync<T>(string tableName, IEnumerable<T> entities) where T : IEntity;
        IAsyncEnumerable<T> QueryAllIterableAsync<T>(string tableName);
        Task<IEnumerable<T>> QueryAllAsync<T>(string tableName);
    }

    public interface IEntity
    {
        string PartitionKey { get; }

        string Id { get; }
    }
}
