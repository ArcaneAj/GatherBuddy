namespace GatherBuddy.Sync.Utilities
{
    internal static class IAsyncEnumerableExtensions
    {
        public static async Task<T?> FirstOrDefault<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            await foreach (var item in asyncEnumerable)
            {
                return item;
            }
            return default;
        }
    }
}
