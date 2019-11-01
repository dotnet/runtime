// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Text.Unicode.Tests
{
    public static partial class UnicodeRangesTests
    {
        [Fact]
        public static void Range_None()
        {
            UnicodeRange range = UnicodeRanges.None;
            Assert.NotNull(range);

            // Test 1: the range should be empty
            Assert.Equal(0, range.FirstCodePoint);
            Assert.Equal(0, range.Length);
        }

        [Fact]
        public static void Range_All()
        {
            Range_Unicode('\u0000', '\uFFFF', "All");
        }

        [Theory]
        [MemberData(nameof(UnicodeRanges_GeneratedData))]
        public static void Range_Unicode(ushort first, ushort last, string blockName)
        {
            Assert.Equal(0x0, first & 0xF); // first char in any block should be U+nnn0
            Assert.Equal(0xF, last & 0xF); // last char in any block should be U+nnnF
            Assert.True(first < last); // code point ranges should be ordered

            var propInfo = typeof(UnicodeRanges).GetRuntimeProperty(blockName);
            Assert.NotNull(propInfo);

            UnicodeRange range = (UnicodeRange)propInfo.GetValue(null);
            Assert.NotNull(range);

            // Test 1: the range should span the range first..last
            Assert.Equal(first, range.FirstCodePoint);
            Assert.Equal(last, range.FirstCodePoint + range.Length - 1);
        }
    }
}
