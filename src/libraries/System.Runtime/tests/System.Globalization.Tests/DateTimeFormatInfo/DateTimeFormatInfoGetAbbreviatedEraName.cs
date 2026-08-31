// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetAbbreviatedEraName
    {
        public static IEnumerable<object[]> GetAbbreviatedEraName_TestData()
        {
            yield return new object[] { "en-US", 0, DateTimeFormatInfoData.EnUSAbbreviatedEraName() };
            yield return new object[] { "en-US", 1, DateTimeFormatInfoData.EnUSAbbreviatedEraName() };
            yield return new object[] { "invariant", 0, "AD" };
            yield return new object[] { "invariant", 1, "AD" };
            yield return new object[] { "ja-JP", 1, DateTimeFormatInfoData.JaJPAbbreviatedEraName() };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnApplePlatform))]
        [MemberData(nameof(GetAbbreviatedEraName_TestData))]
        public void GetAbbreviatedEraName_Invoke_ReturnsExpected(string cultureName, int era, string expected)
        {
            var format = cultureName == "invariant" ? new DateTimeFormatInfo() : new CultureInfo(cultureName).DateTimeFormat;
            var eraName = format.GetAbbreviatedEraName(era);
            Assert.True(expected == eraName, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {eraName}");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void GetAbbreviatedEraName_Invalid(int era)
        {
            var format = new CultureInfo("en-US").DateTimeFormat;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("era", () => format.GetAbbreviatedEraName(era));
        }
    }
}
