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

            for (int i = 0; i < KeyCount; i++)
            {
                Assert.Contains(i.ToString(), cache.Keys);
            }
        }

        [Fact]
        public void ModificationOfCacheDoesNotInterruptEnumerationOverKeys()
        {
            MemoryCache cache = new(new MemoryCacheOptions());
            for (int i = 0; i < KeyCount; i++)
            {
                cache.Set(i.ToString(), i);
            }

            foreach (object key in cache.Keys)
            {
                cache.Remove(key);
            }
            Assert.Equal(0, cache.Count);
        }
    }
}
