// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Caching.Memory.Tests
{
    public class EnumerationTests
    {
        private const int KeyCount = 10;

        [Fact]
        public void KeysContainsAllKeys()
        {
            MemoryCache cache = new(new MemoryCacheOptions());
            for (int i = 0; i < KeyCount; i++)
            {
                cache.Set(i.ToString(), i);
            }

            var keys = cache.Keys;

            Assert.Equal(KeyCount, keys.Count);
            for (int i = 0; i < KeyCount; i++)
            {
                Assert.Contains(i.ToString(), keys);
            }
        }

        [Fact]
        public void KeysReturnSnapshot()
        {
            MemoryCache cache = new(new MemoryCacheOptions());
            for (int i = 0; i < KeyCount; i++)
            {
                cache.Set(i.ToString(), i);
            }
            var keys = cache.Keys;

            Assert.Equal(KeyCount, keys.Count);

            cache.Clear();

            Assert.Equal(0, cache.Count);
            Assert.Equal(KeyCount, keys.Count); // the snapshot remains unaffected
        }

        [Fact]
        public void ModificationOfCacheDoesNotInterruptEnumerationOverKeys()
        {
            MemoryCache cache = new(new MemoryCacheOptions());
            for (int i = 0; i < KeyCount; i++)
            {
                cache.Set(i.ToString(), i);
            }
            var keys = cache.Keys;

            foreach(var key in keys)
            {
                cache.Remove(key);
            }
            Assert.Equal(0, cache.Count);
        }
    }
}
