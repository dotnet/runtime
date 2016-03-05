// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoReadOnly
    {
        public static IEnumerable<object[]> ReadOnly_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, true };
            yield return new object[] { new DateTimeFormatInfo(), false };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, false };
        }

        [Theory]
        [MemberData(nameof(ReadOnly_TestData))]
        public void ReadOnly(DateTimeFormatInfo format, bool expected)
        {
            Assert.Equal(expected, format.IsReadOnly);

            DateTimeFormatInfo readOnlyFormat = DateTimeFormatInfo.ReadOnly(format);
            Assert.True(readOnlyFormat.IsReadOnly);
        }

        [Fact]
        public void ReadOnly_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("dtfi", () => DateTimeFormatInfo.ReadOnly(null)); // Dtfi is null
        }
    }
}
