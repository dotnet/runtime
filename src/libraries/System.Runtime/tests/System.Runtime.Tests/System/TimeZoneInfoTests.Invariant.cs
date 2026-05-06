// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public static partial class TimeZoneInfoTests
    {
        [Fact]
        public static void IsInvariant()
        {
            Assert.True(GetInvariant(null));

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_Invariant")]
            static extern bool GetInvariant(TimeZoneInfo t);
        }

        [Fact]
        public static void JustUtcInvariant()
        {
            Assert.Equal(TimeZoneInfo.Local, TimeZoneInfo.Local);

            var tzs = TimeZoneInfo.GetSystemTimeZones();
            Assert.Equal(1, tzs.Count);
            Assert.Equal(TimeZoneInfo.Utc, tzs[0]);
        }

        [Fact]
        public static void OnlyUtcWhenInvariant()
        {
            Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(s_strPacific));
            Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(s_strSydney));
            Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(s_strGMT));
        }

        [Fact]
        public static void NoAlternateUTCIdsInvariant()
        {
            foreach (string alias in s_UtcAliases)
            {
                if (alias == s_strUtc)
                {
                    continue;
                }
                Assert.Throws<TimeZoneNotFoundException>(() =>
                {
                    TimeZoneInfo.FindSystemTimeZoneById(alias);
                });
            }
        }
    }
}
