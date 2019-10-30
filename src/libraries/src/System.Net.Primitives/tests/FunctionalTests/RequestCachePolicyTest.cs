// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Cache;

using Xunit;

namespace System.Net.Tests
{
    public class RequestCachePolicyTest
    {
        [Fact]
        public void Ctor_ExpectedPropertyValues()
        {
            Assert.Equal(RequestCacheLevel.Default, new RequestCachePolicy().Level);
            Assert.Equal(RequestCacheLevel.NoCacheNoStore, new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore).Level);
        }

        [Fact]
        public void Ctor_InvalidArg_Throws()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("level", () => new RequestCachePolicy((RequestCacheLevel)42));
        }

        [Fact]
        public void ToString_ExpectedValue()
        {
            Assert.Equal("Level:Default", new RequestCachePolicy().ToString());
        }
    }
}
