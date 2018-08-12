namespace ICacheChallenge.Policies
{
    /// <inheritdoc />
    /// <summary>
    /// Eviction policy that evicts / removes members when the cache reaches a certain maximum size.
    /// </summary>
    public class MaxCacheSizeEvictionPolicy : ICacheEvictionPolicy
    {
        private readonly int _maxCacheSize;

        /// <inheritdoc />
        public int CacheSize { get; set; }

        public MaxCacheSizeEvictionPolicy(int maxCacheSize)
        {
            CacheSize = 0;
            _maxCacheSize = maxCacheSize;
        }

        /// <inheritdoc />
        public bool MemberRequiresEviction()
        { 
            return CacheSize >= _maxCacheSize;
        }
    }
}