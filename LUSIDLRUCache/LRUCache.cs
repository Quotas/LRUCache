using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace LUSIDLRUCache
{

    /// <summary>Class <c>LRUCache</c> Caches values up to the given capacity, once reaching capacity
    /// it will dispose of the first value.  If a value is accessed it will remove that value 
    /// and add it to the end. </summary>
    public class LRUCache<TKey, TValue>
    {


        /// <summary>Instance variable <c>capacity</c> is the max number of values the cache can hold. </summary>
        private readonly int capacity; //Once we set the size of the cache dont set it again
        private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> dict;
        private readonly LinkedList<CacheItem> data = new LinkedList<CacheItem>();
        private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();


        public LRUCache(int concurrencyLevel, int capacity = 100)
        {

            this.capacity = capacity;
            this.dict = new ConcurrentDictionary<TKey, LinkedListNode<CacheItem>>(concurrencyLevel, this.capacity + 1);

        }

        public int Count
        {
            get
            {
                rwLock.EnterWriteLock();
                try
                {
                    return data.Count;
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }

        }

        public void Clear()
        {
            rwLock.EnterWriteLock();
            try
            {
                data.Clear();

            }
            finally
            {
                rwLock.ExitWriteLock();
            }


        }


        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {


            if (this.TryGet(key, out var value))
            {
                return value;
            }

            var node = new LinkedListNode<CacheItem>(new CacheItem(key, valueFactory(key)));

            if (this.dict.TryAdd(key, node))
            {
                LinkedListNode<CacheItem> first = null;

                lock (this.data)
                {

                    if (data.Count >= capacity)
                    {
                        first = data.First;
                        data.RemoveFirst();

                    }

                    data.AddLast(node);
                }


                if (first != null)
                {
                    dict.TryRemove(first.Value.Key, out var removed);


                }

                return node.Value.Value;

            }
            return this.GetOrAdd(key, valueFactory);
        }

        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {


            if (this.TryGet(key, out var value))
            {
                return value;
            }

            var node = new LinkedListNode<CacheItem>(new CacheItem(key, valueFactory(key, factoryArgument)));

            if (this.dict.TryAdd(key, node))
            {
                LinkedListNode<CacheItem> first = null;

                lock (this.data)
                {

                    if (data.Count >= capacity)
                    {
                        first = data.First;
                        data.RemoveFirst();

                    }

                    data.AddLast(node);
                }


                if (first != null)
                {
                    dict.TryRemove(first.Value.Key, out var removed);

                }

                return node.Value.Value;

            }
            return this.GetOrAdd(key, valueFactory, factoryArgument);
        }
        
        public bool TryGet(TKey key, out TValue value)
        {
            LinkedListNode<CacheItem> node;

            if (dict.TryGetValue(key, out node))
            {
                value = node.Value.Value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public bool TryRemove(TKey key)
        {
            if (dict.TryRemove(key, out var node))
            {
                if (node.List != null)
                {
                    lock (this.data)
                    {
                        if (node.List != null)
                        {
                            data.Remove(node);
                        }
                    }

                }

                return true;

            }

            return false;
        }

        private class CacheItem
        {

            public CacheItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;

            }

            public TKey Key { get; }
            public TValue Value { get; }


        }

    }

}
