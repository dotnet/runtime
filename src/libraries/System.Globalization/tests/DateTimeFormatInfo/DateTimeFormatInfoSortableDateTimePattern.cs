// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoSortableDateTimePattern
    {
        public static IEnumerable<object[]> SortableDateTimePattern_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat };
        }

        [Theory]
        [MemberData(nameof(SortableDateTimePattern_TestData))]
        public void SortableDateTimePattern_Get_ReturnsExpected(DateTimeFormatInfo format)
        {
            Assert.Equal("yyyy'-'MM'-'dd'T'HH':'mm':'ss", format.SortableDateTimePattern);
        }
    }
}
