using System;
using System.Linq;
using LUSIDLRUCache;
using Xunit;
using Xunit.Abstractions;

namespace LUSIDTechnicalTest
{
    public class LRUCacheTests
    {
        private readonly ITestOutputHelper output;
        private LRUCache<int, string> lru = new LRUCache<int, string>("MyCache", Environment.ProcessorCount, 5);

        public LRUCacheTests(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
            
            //Register a callback to know the value we removed and print it to the test output
            lru.removedCallBack += (v) => output.WriteLine("This is the value that was removed {0}", v.ToString());
        }


        [Fact]
        public void WhenOneItemAddedCountShouldBeOneWithArg()
        {

            lru.GetOrAdd(1, (k, v) => v, "1");
            Assert.Equal(1, lru.Count);

        }

        [Fact]
        public void WhenOneItemAddedCountShouldBeOne()
        {

            lru.GetOrAdd(1, (k) => k.ToString());
            Assert.Equal(1, lru.Count);

        }

        [Fact]
        public void WhenSixItemsAddedCountShouldBeFive()
        {

            foreach (int val in Enumerable.Range(0, 6))
            {
                lru.GetOrAdd(val, (k) => k.ToString());
            }
            
            Assert.Equal(5, lru.Count);

        }

        [Fact]
        public void WhenItemsAddedGreaterThanCapacityLeastUsedItemPushedOutOfCache()
        {

            foreach (int key in Enumerable.Range(0, 5))
            {
                lru.GetOrAdd(key, (k) => k.ToString());
            }

            //Add 5, this should kick out the oldest key/val so (0, "0")
            lru.GetOrAdd(6, (k) => k.ToString());

            bool res = lru.TryGet(0, out var value);

            Assert.False(res);

        }


        [Fact]
        public void  WhenItemsAddedCountShouldBeZeroAfterClear()
        {

            foreach (int val in Enumerable.Range(0, 5))
            {
                lru.GetOrAdd(val, (k) => k.ToString());
            }

            lru.Clear();

            Assert.Equal(0, lru.Count);

        }

    }
}
