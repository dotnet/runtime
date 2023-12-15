// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoRFC1123Pattern
    {
        public static IEnumerable<object[]> RFC1123Pattern_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
        }

        [Theory]
        [MemberData(nameof(RFC1123Pattern_TestData))]
        public void RFC1123Pattern_Get_ReturnsExpected(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.RFC1123Pattern);
        }
    }
}
