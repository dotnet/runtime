// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Runtime.Caching;

namespace System.Runtime.Caching.Tests
{
    // These are the tests to fill in some of the coverage in ported Mono caching tests
    public class AdditionalCacheTests
    {
        [Fact]
        public void DisposedCacheTest()
        {
            var mc = new MemoryCache("my disposed cache 1");
            mc.Add("aa", "bb", new CacheItemPolicy());
            mc.Dispose();

            Assert.Null(mc["aa"]);

            mc = new MemoryCache("my disposed cache 2");
            CacheEntryRemovedReason reason = (CacheEntryRemovedReason)1111;
            var cip = new CacheItemPolicy();
            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                reason = args.RemovedReason;
            };

            mc.Set("key", "value", cip);
            mc.Dispose();
            Assert.Equal(reason, CacheEntryRemovedReason.CacheSpecificEviction);
        }
    }
}
