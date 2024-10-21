// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoReadOnly
    {
        public static IEnumerable<object[]> ReadOnly_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, true };
            yield return new object[] { DateTimeFormatInfo.ReadOnly(new DateTimeFormatInfo()), true };
            yield return new object[] { new DateTimeFormatInfo(), false };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, false };
        }

        [Theory]
        [MemberData(nameof(ReadOnly_TestData))]
        public void ReadOnly_Invoke_ReturnsExpected(DateTimeFormatInfo format, bool originalFormatIsReadOnly)
        {
            Assert.Equal(originalFormatIsReadOnly, format.IsReadOnly);

            DateTimeFormatInfo readOnlyFormat = DateTimeFormatInfo.ReadOnly(format);
            if (originalFormatIsReadOnly)
            {
                Assert.Same(format, readOnlyFormat);
            }
            else
            {
                Assert.NotSame(format, readOnlyFormat);
            }
            Assert.True(readOnlyFormat.IsReadOnly);
        }

        [Fact]
        public void ReadOnly_NullDtfi_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("dtfi", () => DateTimeFormatInfo.ReadOnly(null)); // Dtfi is null
        }
    }
}
