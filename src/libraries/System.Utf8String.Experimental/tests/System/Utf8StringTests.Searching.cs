// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public unsafe partial class Utf8StringTests
    {
        [Theory]
        [MemberData(nameof(IndexOfTestData))]
        public static void Contains_And_IndexOf_CharRune_Ordinal(Utf8String utf8String, Rune searchValue, int expectedIndex)
        {
            // Contains

            if (searchValue.IsBmp)
            {
                Assert.Equal(expectedIndex >= 0, utf8String.Contains((char)searchValue.Value));
            }
            Assert.Equal(expectedIndex >= 0, utf8String.Contains(searchValue));

            // IndexOf

            if (searchValue.IsBmp)
            {
                Assert.Equal(expectedIndex, utf8String.IndexOf((char)searchValue.Value));
            }
            Assert.Equal(expectedIndex, utf8String.IndexOf(searchValue));
        }

        [Theory]
        [MemberData(nameof(IndexOfTestData))]
        public static void StartsWith_And_EndsWith_CharRune_Ordinal(Utf8String utf8String, Rune searchValue, int expectedIndex)
        {
            // StartsWith

            if (searchValue.IsBmp)
            {
                Assert.Equal(expectedIndex == 0, utf8String.StartsWith((char)searchValue.Value));
            }
            Assert.Equal(expectedIndex == 0, utf8String.StartsWith(searchValue));

            // EndsWith

            bool endsWithExpectedValue = (expectedIndex >= 0) && (expectedIndex + searchValue.Utf8SequenceLength) == utf8String.Length;

            if (searchValue.IsBmp)
            {
                Assert.Equal(endsWithExpectedValue, utf8String.EndsWith((char)searchValue.Value));
            }
            Assert.Equal(endsWithExpectedValue, utf8String.EndsWith(searchValue));
        }

        [Fact]
        public static void Searching_StandaloneSurrogate_Fails()
        {
            Utf8String utf8String = u8("\ud800\udfff");

            Assert.False(utf8String.Contains('\ud800'));
            Assert.False(utf8String.Contains('\udfff'));

            Assert.Equal(-1, utf8String.IndexOf('\ud800'));
            Assert.Equal(-1, utf8String.IndexOf('\udfff'));

            Assert.False(utf8String.StartsWith('\ud800'));
            Assert.False(utf8String.StartsWith('\udfff'));

            Assert.False(utf8String.EndsWith('\ud800'));
            Assert.False(utf8String.EndsWith('\udfff'));
        }

        public static IEnumerable<object[]> IndexOfTestData
        {
            get
            {
                yield return new object[] { Utf8String.Empty, default(Rune), -1 };
                yield return new object[] { u8("Hello"), (Rune)'H', 0 };
                yield return new object[] { u8("Hello"), (Rune)'h', -1 };
                yield return new object[] { u8("Hello"), (Rune)'O', -1 };
                yield return new object[] { u8("Hello"), (Rune)'o', 4 };
                yield return new object[] { u8("Hello"), (Rune)'L', -1 };
                yield return new object[] { u8("Hello"), (Rune)'l', 2 };
                yield return new object[] { u8("\U00012345\U0010ABCD"), (Rune)0x00012345, 0 };
                yield return new object[] { u8("\U00012345\U0010ABCD"), (Rune)0x0010ABCD, 4 };
                yield return new object[] { u8("abc\ufffdef"), (Rune)'c', 2 };
                yield return new object[] { u8("abc\ufffdef"), (Rune)'\ufffd', 3 };
                yield return new object[] { u8("abc\ufffdef"), (Rune)'d', -1 };
                yield return new object[] { u8("abc\ufffdef"), (Rune)'e', 6 };
            }
        }
    }
}
