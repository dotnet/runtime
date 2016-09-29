// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoMiscTests
    {
        public static IEnumerable<object[]> StringInfo_TestData()
        {
            yield return new object[] { "Simple Text", 7, "Text", 4, "Text" };
            yield return new object[] { "Simple Text", 0, "Simple Text", 6, "Simple" };
            yield return new object[] { "\uD800\uDC00\uD801\uDC01Left", 2, "Left", 2, "Le" };
            yield return new object[] { "\uD800\uDC00\uD801\uDC01Left", 1, "\uD801\uDC01Left", 2, "\uD801\uDC01L" };
            yield return new object[] { "Start\uD800\uDC00\uD801\uDC01Left", 5, "\uD800\uDC00\uD801\uDC01Left", 1, "\uD800\uDC00" };
        }

        [Theory]
        [MemberData(nameof(StringInfo_TestData))]
        public void SubstringTest(string source, int index, string expected, int length, string expectedWithLength)
        {
            StringInfo si = new StringInfo(source);
            Assert.Equal(expected, si.SubstringByTextElements(index));
            Assert.Equal(expectedWithLength, si.SubstringByTextElements(index, length));
        }

        [Fact]
        public void NegativeTest()
        {
            string s = "Some String";
            StringInfo si = new StringInfo(s);
            StringInfo siEmpty = new StringInfo("");

            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(s.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => siEmpty.SubstringByTextElements(0));

            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(s.Length + 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => siEmpty.SubstringByTextElements(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(0, s.Length + 1));
        }
    }
}
