// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoReadOnly
    {
        public static IEnumerable<object[]> ReadOnly_TestData()
        {
            yield return new object[] { new NumberFormatInfo(), false };
            yield return new object[] { new CultureInfo("en-US").NumberFormat, false };
            yield return new object[] { NumberFormatInfo.InvariantInfo, true };
            yield return new object[] { NumberFormatInfo.ReadOnly(new CultureInfo("en-US").NumberFormat), true };
        }

        [Theory]
        [MemberData(nameof(ReadOnly_TestData))]
        public void ReadOnly(NumberFormatInfo format, bool expected)
        {
            Assert.Equal(expected, format.IsReadOnly);

            NumberFormatInfo readOnlyFormat = NumberFormatInfo.ReadOnly(format);
            Assert.True(readOnlyFormat.IsReadOnly);
        }

        [Fact]
        public void ReadOnly_ReadOnlyFormat()
        {
            NumberFormatInfo readOnlyFormat = NumberFormatInfo.ReadOnly(new NumberFormatInfo());
            Assert.Same(readOnlyFormat, NumberFormatInfo.ReadOnly(readOnlyFormat));
        }

        [Fact]
        public void ReadOnly_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("nfi", () => NumberFormatInfo.ReadOnly(null));
        }
    }
}
