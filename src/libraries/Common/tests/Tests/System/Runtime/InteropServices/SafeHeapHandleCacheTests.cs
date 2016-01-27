// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Tests.System.Runtime.InteropServices
{
    public class SafeHeapHandleCacheTests
    {
        [Theory,
            InlineData(5, 5, 5),
            InlineData(5, 7, 5),
            InlineData(5, 3, 3),
            InlineData(0, 1, 1),
            InlineData(-1, 1, 1),
            ]
        public void CachedCountTest(int maxCount, int itemsToCache, int expected)
        {
            using (SafeHeapHandleCache cache = new SafeHeapHandleCache(minSize: 5, maxSize: 100, maxHandles: maxCount))
            {
                for (int i = 0; i < itemsToCache; i++)
                {
                    cache.Release(new SafeHeapHandle(10));
                }

                int count = cache._handleCache.Count(h => h != null);
                Assert.Equal(expected, count);
            }
        }

        [Fact]
        public void GrabHandleFromEmptyCache()
        {
            using (SafeHeapHandleCache cache = new SafeHeapHandleCache(minSize: 5, maxSize: 100, maxHandles: 2))
            {
                var handle = cache.Acquire();
                Assert.NotNull(handle);
                Assert.Equal((ulong)5, handle.ByteLength);
            }
        }

        [Fact]
        public void OldestPushedOff()
        {
            using (SafeHeapHandleCache cache = new SafeHeapHandleCache(minSize: 5, maxSize: 100, maxHandles: 2))
            {
                var first = cache.Acquire();
                var second = cache.Acquire();
                var third = cache.Acquire();
                cache.Release(first);
                cache.Release(second);
                cache.Release(third);
                Assert.True(first.IsClosed);
                Assert.False(second.IsClosed);
                Assert.False(third.IsClosed);
            }
        }

        [Fact]
        public void LatestPopped()
        {
            using (SafeHeapHandleCache cache = new SafeHeapHandleCache(minSize: 5, maxSize: 100, maxHandles: 2))
            {
                var first = cache.Acquire();
                var second = cache.Acquire();
                cache.Release(first);
                cache.Release(second);
                Assert.Same(second, cache.Acquire());
                Assert.Same(first, cache.Acquire());
            }
        }
    }
}
