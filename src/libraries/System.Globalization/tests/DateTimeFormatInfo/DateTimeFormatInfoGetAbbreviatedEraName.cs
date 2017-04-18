// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetAbbreviatedEraName
    {
        public static IEnumerable<object[]> GetAbbreviatedEraName_TestData()
        {
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, 0, DateTimeFormatInfoData.EnUSAbbreviatedEraName() };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, 1, DateTimeFormatInfoData.EnUSAbbreviatedEraName() };
            yield return new object[] { new DateTimeFormatInfo(), 0, "AD" };
            yield return new object[] { new DateTimeFormatInfo(), 1, "AD" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, 1, DateTimeFormatInfoData.JaJPAbbreviatedEraName() };
        }

        [Theory]
        [MemberData(nameof(GetAbbreviatedEraName_TestData))]
        public void GetAbbreviatedEraName(DateTimeFormatInfo format, int era, string expected)
        {
            Assert.Equal(expected, format.GetAbbreviatedEraName(era));
        }

        [Fact]
        public void GetAbbreviatedEraName_Invalid()
        {
            var format = new CultureInfo("en-US").DateTimeFormat;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("era", () => format.GetAbbreviatedEraName(-1)); // Era < 0

            const int EnUSMaxEra = 1;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("era", () => format.GetAbbreviatedEraName(EnUSMaxEra + 1)); // Era > max era for the culture
        }
    }
}
