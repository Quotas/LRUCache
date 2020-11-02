using System.Linq;
using LUSIDLRUCache;
using Xunit;

namespace LUSIDTechnicalTest
{
    public class LRUCacheTests
    {

        private LRUCache<int, string> lru = new LRUCache<int, string>(1, 5);

        /// <summary>Class <c>ValueFactory</c> produces a value given a key and a value, simply
        /// returns the given strin v </summary>
        static partial class ValueFactory { public static string Create(int k, string v) => v; }
        /// <summary>Class <c>ValueFactory</c> produces a value given a key, simply
        /// returns a string of the given key</summary>
        static partial class ValueFactory { public static string Create(int k) => k.ToString(); }

        [Fact]
        public void WhenOneItemAddedCountShouldBeOneWithArg()
        {

            lru.GetOrAdd(1, ValueFactory.Create, "1");
            Assert.Equal(1, lru.Count);

        }

        [Fact]
        public void WhenOneItemAddedCountShouldBeOne()
        {

            lru.GetOrAdd(1, ValueFactory.Create);
            Assert.Equal(1, lru.Count);

        }

        [Fact]
        public void WhenSixItemsAddedCountShouldBeFive()
        {

            foreach (int val in Enumerable.Range(0, 5))
            {
                lru.GetOrAdd(val, ValueFactory.Create, val.ToString());
            }
            
            Assert.Equal(5, lru.Count);

        }

    }
}
