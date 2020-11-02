using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace LUSIDLRUCache
{

    /// <summary>Class <c>LRUCache</c> Caches values up to the given 
    /// capacity, once reaching capacity it will dispose of the first 
    /// value.  If a value is accessed it will remove that value and 
    /// add it to the end. </summary>
    public class LRUCache<TKey, TValue>
    {

        /// <summary>Instance variable <c>capacity</c> is the max number of 
        /// values the cache can hold. </summary>
        private readonly int capacity; //Once we set the size of the cache dont set it again
        
        /// <summary>Instance variable <c>dict</c> thread safe dict that holds our 
        /// key, node pair</summary>
        private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> dict;
        
        /// <summary>Instance variable <c>name</c> name of the cache</summary>
        private readonly string name;

        /// <summary>Instance variable <c>data</c> linked list of cache item to 
        /// preserve ordering to determine oldest/newest</summary>
        private readonly LinkedList<CacheItem> data = new LinkedList<CacheItem>();

        /// <summary>Action <c>removedCallBack</c> is a callback that is called when
        /// a value is removed from the cache</summary>
        public Action<TValue> removedCallBack;

        /// <summary>Constructor for <c>LRUCache</c>
        ///    (<paramref name="name"/>,<paramref name="concurrencyLevel"/>,
        ///    <paramref name="capacity"/>).</summary>
        /// <param><c>name</c> The name of the cache.</param>
        /// <param><c>concurrencyLevel</c> The level concurrent operations that the cache can handle .</param>
        /// <param><c>capacity</c> The max amount of items the cache can handle</param>
        public LRUCache(string name, int concurrencyLevel, int capacity = 100)
        {
            this.name = name;
            this.capacity = capacity;

            this.dict = new ConcurrentDictionary<TKey, LinkedListNode<CacheItem>>(concurrencyLevel, this.capacity);

        }

        /// <value>Property <c>Name</c> the name of the cache.</value>
        public string Name { get { return this.name; } }

        /// <value>Property <c>Count</c> the current amount of values in the cache, always >= capacity</value>
        public int Count { get { return data.Count; } }

        /// <summary>This method clears all of the contents in the cache</summary>
        public void Clear()
        {
            lock (this.data)
            {
                data.Clear();
            }
        }

        /// <summary>This method attempts to get a value given a key,
        /// if nothing found then adds the value to the cache</summary>
        /// <param><c>key</c> the key for the value</param>
        /// <param><c>valueFactory</c> valueFactory to produce the value</param>
        /// <returns>The value stored in the cache</returns>
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
                    removedCallBack(removed.Value.Value);
                }

                return node.Value.Value;

            }
            return this.GetOrAdd(key, valueFactory);
        }

        /// <summary>This method attempts to get a value given a key,
        /// if nothing found then adds the value to the cache</summary>
        /// <param><c>key</c> the key for the value</param>
        /// <param><c>valueFactory</c> valueFactory to produce the value </param>
        /// <param><c>factoryArgument</c> arg for the valueFactory </param>
        /// <returns>The value stored in the cache.</returns>
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
                    dict.TryRemove(first.Value.Key, out _);

                }

                return node.Value.Value;

            }
            return this.GetOrAdd(key, valueFactory, factoryArgument);
        }

        /// <summary>Attempts to get a value given a key</summary>
        /// <param><c>key</c> the key for the value</param>
        /// <param><c>value</c> out param for the value</param>
        /// <returns>A boolean, true if the value exists, false if not.</returns>
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

        /// <summary>Attempts to remove a value given a key</summary>
        /// <param><c>key</c> the key for the value</param>
        /// <returns>A boolean, true if the value exists, false if not.</returns>
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
