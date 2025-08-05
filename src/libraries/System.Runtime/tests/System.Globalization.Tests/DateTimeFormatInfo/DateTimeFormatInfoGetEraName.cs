// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetEraName
    {
        public static IEnumerable<object[]> GetEraName_TestData()
        {
            yield return new object[] { "invariant", 1, "A.D." };
            yield return new object[] { "invariant", 0, "A.D." };
            yield return new object[] { "en-US", 1, DateTimeFormatInfoData.EnUSEraName() };
            yield return new object[] { "en-US", 0, DateTimeFormatInfoData.EnUSEraName() };
            yield return new object[] { "fr-FR", 1, "ap. J.-C." };
            yield return new object[] { "fr-FR", 0, "ap. J.-C." };
        }

        [Theory]
        [MemberData(nameof(GetEraName_TestData))]
        public void GetEraName_Invoke_ReturnsExpected(string cultureName, int era, string expected)
        {
            var format = cultureName == "invariant" ? DateTimeFormatInfo.InvariantInfo : new CultureInfo(cultureName).DateTimeFormat;
            var eraName = format.GetEraName(era);
            Assert.True(expected == eraName, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {eraName}");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void GetEraName_InvalidEra_ThrowsArgumentOutOfRangeException(int era)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("era", () => format.GetEraName(era));
        }
    }
}
