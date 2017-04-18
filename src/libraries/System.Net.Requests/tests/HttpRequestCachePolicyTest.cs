// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Cache;

using Xunit;

namespace System.Net.Tests
{
    public class HttpRequestCachePolicyTest
    {
        public static IEnumerable<object[]> Ctor_ExpectedPropertyValues_MemberData()
        {
            yield return new object[] { new HttpRequestCachePolicy(), HttpRequestCacheLevel.Default, TimeSpan.MaxValue, TimeSpan.MinValue, TimeSpan.MinValue, DateTime.MinValue };
            foreach (HttpRequestCacheLevel level in Enum.GetValues(typeof(HttpRequestCacheLevel)))
            {
                yield return new object[] { new HttpRequestCachePolicy(level), level, TimeSpan.MaxValue, TimeSpan.MinValue, TimeSpan.MinValue, DateTime.MinValue };
            }
            yield return new object[] { new HttpRequestCachePolicy(new DateTime(504000000000)), HttpRequestCacheLevel.Default, TimeSpan.MaxValue, TimeSpan.MinValue, TimeSpan.MinValue, new DateTime(504000000000) };
            yield return new object[] { new HttpRequestCachePolicy(HttpCacheAgeControl.MaxAge, TimeSpan.FromSeconds(1)), HttpRequestCacheLevel.Default, TimeSpan.FromSeconds(1), TimeSpan.MinValue, TimeSpan.MinValue, DateTime.MinValue };
            yield return new object[] { new HttpRequestCachePolicy(HttpCacheAgeControl.MaxStale, TimeSpan.FromSeconds(1)), HttpRequestCacheLevel.Default, TimeSpan.MaxValue, TimeSpan.FromSeconds(1), TimeSpan.MinValue, DateTime.MinValue };
            yield return new object[] { new HttpRequestCachePolicy(HttpCacheAgeControl.MinFresh, TimeSpan.FromSeconds(1)), HttpRequestCacheLevel.Default, TimeSpan.MaxValue, TimeSpan.MinValue, TimeSpan.FromSeconds(1), DateTime.MinValue };
            yield return new object[] { new HttpRequestCachePolicy(HttpCacheAgeControl.MaxAge, TimeSpan.FromSeconds(1), TimeSpan.MaxValue), HttpRequestCacheLevel.Default, TimeSpan.FromSeconds(1), TimeSpan.MinValue, TimeSpan.MinValue, DateTime.MinValue };
            yield return new object[] { new HttpRequestCachePolicy(HttpCacheAgeControl.MaxStale, TimeSpan.MaxValue, TimeSpan.FromSeconds(1)), HttpRequestCacheLevel.Default, TimeSpan.MaxValue, TimeSpan.FromSeconds(1), TimeSpan.MinValue, DateTime.MinValue };
            yield return new object[] { new HttpRequestCachePolicy(HttpCacheAgeControl.MinFresh, TimeSpan.MaxValue, TimeSpan.FromSeconds(1)), HttpRequestCacheLevel.Default, TimeSpan.MaxValue, TimeSpan.MinValue, TimeSpan.FromSeconds(1), DateTime.MinValue };
            yield return new object[] { new HttpRequestCachePolicy(HttpCacheAgeControl.MaxAgeAndMaxStale, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)), HttpRequestCacheLevel.Default, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.MinValue, DateTime.MinValue };
            yield return new object[] { new HttpRequestCachePolicy(HttpCacheAgeControl.MaxAgeAndMinFresh, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)), HttpRequestCacheLevel.Default, TimeSpan.FromSeconds(1), TimeSpan.MinValue, TimeSpan.FromSeconds(2), DateTime.MinValue };
            yield return new object[] { new HttpRequestCachePolicy(HttpCacheAgeControl.MaxAgeAndMinFresh, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), new DateTime(1, 2, 3)), HttpRequestCacheLevel.Default, TimeSpan.FromSeconds(1), TimeSpan.MinValue, TimeSpan.FromSeconds(2), new DateTime(1, 2, 3) };
        }

        [Theory]
        [MemberData(nameof(Ctor_ExpectedPropertyValues_MemberData))]
        public void Ctor_ExpectedPropertyValues(
            HttpRequestCachePolicy p, HttpRequestCacheLevel level, TimeSpan maxAge, TimeSpan maxStale, TimeSpan minFresh, DateTime cacheSyncDate)
        {
            Assert.Equal(level, p.Level);
            Assert.Equal(maxAge, p.MaxAge);
            Assert.Equal(maxStale, p.MaxStale);
            Assert.Equal(minFresh, p.MinFresh);
            Assert.Equal(cacheSyncDate, p.CacheSyncDate);
            Assert.StartsWith("Level:", p.ToString());
        }

        [Fact]
        public void Ctor_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("level", () => new HttpRequestCachePolicy((HttpRequestCacheLevel)42));
            AssertExtensions.Throws<ArgumentException>("cacheAgeControl", () => new HttpRequestCachePolicy(HttpCacheAgeControl.MaxAgeAndMinFresh, TimeSpan.FromSeconds(1)));
            AssertExtensions.Throws<ArgumentException>("cacheAgeControl", () => new HttpRequestCachePolicy(HttpCacheAgeControl.None, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
        }
    }
}
