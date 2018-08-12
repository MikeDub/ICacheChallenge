using System.Collections.Generic;
using System.Threading;
using ICacheChallenge.Domain;
using ICacheChallenge.Policies;

namespace ICacheChallenge.Cache
{
    // Requirements for this Cache / Challenge addressed in this class.

    //The cache must implement ICache<TKey, TValue>(below).
    //The cache must implement an eviction policy(below).
    //All operations, including cache eviction, must have O(1) time complexity.
    //The cache must be thread-safe.Your consumers will be using the cache from a variety of threads simultaneously.
    //You are writing this for other developers, so please consider their feelings and include an appropriate level of documentation.

    public class SlickCache<TKey, TValue> : ICache<TKey, TValue>
    {
        private readonly Dictionary<TKey, Node<TKey, TValue>> _cache;
        private Node<TKey, TValue> _leastRecentlyUsed;
        private Node<TKey, TValue> _mostRecentlyUsed;
        private readonly ICacheEvictionPolicy _evictionPolicy;

        /// <summary>
        /// Instantiate a new Cache based on the ICache(TKey,TValue) interface, can be instantiated with either parameter
        /// </summary>
        /// <param name="maxSize">Instantiate the cache using a max size eviction policy.</param>
        /// /// <param name="evictionPolicy">Instantiate the cache using your chosen eviction policy.</param>
        public SlickCache(int? maxSize = null, ICacheEvictionPolicy evictionPolicy = null)
        {
            //If maxSize has been specified and no eviction policy has been specified, then default to the max size eviction policy
            _evictionPolicy = maxSize.HasValue && evictionPolicy == null ? new MaxCacheSizeEvictionPolicy(maxSize.Value) : evictionPolicy;
            _leastRecentlyUsed = new Node<TKey, TValue>(null, null, default(TKey), default(TValue));
            _mostRecentlyUsed = _leastRecentlyUsed;

            //When the cache is constructed, it should take as an argument the maximum number of elements stored in the cache.
            _cache = new Dictionary<TKey, Node<TKey, TValue>>();
        }

        /// <inheritdoc />
        public void AddOrUpdate(TKey key, TValue value)
        {
            SpinLock sLock = new SpinLock();

            bool lockTaken = false;
            try
            {
                sLock.Enter(ref lockTaken);

                //Support updating the cache (find by key and update the value)
                _cache.TryGetValue(key, out var valueToUpdate);
                if (valueToUpdate != null)
                {
                    valueToUpdate.Value = value;
                    _mostRecentlyUsed = valueToUpdate;
                    //Eviction policy shouldn't be required when just performing an update.
                    return;
                }

                // Insert the new node at the right-most end of the linked-list (recently used)
                Node<TKey, TValue> myNode = new Node<TKey, TValue>(_mostRecentlyUsed, null, key, value);

                //Support Adding to the cache
                _mostRecentlyUsed.Next = myNode;
                _cache.Add(key, myNode);
                _mostRecentlyUsed = myNode;

                //When an item is added to the cache, a check should be run to see if the cache size exceeds the maximum number of elements permitted. 
                // Delete the left-most entry and update the least recently used pointer
                if (_evictionPolicy.MemberRequiresEviction())
                {
                    //If this is the case, then the least recently added/updated/retrieved item should be evicted from the cache.
                    _cache.Remove(_leastRecentlyUsed.Key);
                    _leastRecentlyUsed = _leastRecentlyUsed.Next;
                    _leastRecentlyUsed.Previous = null;
                }
                //Update cache size
                else 
                {
                    // If this is the only node in the list, also set it as the least recently used.
                    if (_evictionPolicy.CacheSize == 0)
                    {
                        _leastRecentlyUsed = myNode;
                    }
                    _evictionPolicy.CacheSize++;
                }
            }
            finally
            {
                if (lockTaken) sLock.Exit(false);
            }
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            SpinLock sLock = new SpinLock();

            bool lockTaken = false;
            try
            {
                sLock.Enter(ref lockTaken);
                _cache.TryGetValue(key, out var tempNode);
                if (tempNode == null)
                {
                    value = default(TValue);
                    return false;
                }
                // If it is already the most recently used, don't modify the list
                if (tempNode.Key.Equals(_mostRecentlyUsed.Key))
                {
                    value = _mostRecentlyUsed.Value;
                }

                // Get the next and previous nodes
                Node<TKey, TValue> nextNode = tempNode.Next;
                Node<TKey, TValue> previousNode = tempNode.Previous;

                // If at the left most (oldest), we update the least recently used
                if (tempNode.Key.Equals(_leastRecentlyUsed.Key))
                {
                    nextNode.Previous = null;
                    _leastRecentlyUsed = nextNode;
                }

                //If we are in the middle, we need to update the member before and after our member
                // PREVIOUS -> THIS <- NEXTNODE
                else if (!tempNode.Key.Equals(_mostRecentlyUsed.Key))
                {
                    previousNode.Next = nextNode;
                    nextNode.Previous = previousNode;
                }
                //Finally move out item to the most recently used
                tempNode.Previous = _mostRecentlyUsed;
                _mostRecentlyUsed.Next = tempNode;
                _mostRecentlyUsed = tempNode;
                _mostRecentlyUsed.Next = null;

                value = tempNode.Value;
                return true;
            }
            finally
            {
                if (lockTaken)
                    sLock.Exit(false);
            }
        }
    }
}