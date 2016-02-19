// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoIsPrefixTests
    {
        private static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        private static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        private static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;
        
        public static IEnumerable<object[]> IsPrefix_TestData()
        {
            // Empty strings
            yield return new object[] { s_invariantCompare, "foo", "", CompareOptions.None, true };
            yield return new object[] { s_invariantCompare, "", "", CompareOptions.None, true };

            // Long strings
            yield return new object[] { s_invariantCompare, new string('a', 5555), "aaaaaaaaaaaaaaa", CompareOptions.None, true };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000), CompareOptions.None, true };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000) + "b", CompareOptions.None, false };

            // Hungarian
            yield return new object[] { s_invariantCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.None, false };
            yield return new object[] { s_invariantCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.Ordinal, false };

            // Turkish
            yield return new object[] { s_turkishCompare, "interesting", "I", CompareOptions.None, false };
            yield return new object[] { s_turkishCompare, "interesting", "I", CompareOptions.IgnoreCase, false };
            yield return new object[] { s_turkishCompare, "interesting", "\u0130", CompareOptions.None, false };
            yield return new object[] { s_turkishCompare, "interesting", "\u0130", CompareOptions.IgnoreCase, true };
            yield return new object[] { s_invariantCompare, "interesting", "I", CompareOptions.None, false };
            yield return new object[] { s_invariantCompare, "interesting", "I", CompareOptions.IgnoreCase, true };
            yield return new object[] { s_invariantCompare, "interesting", "\u0130", CompareOptions.None, false };
            yield return new object[] { s_invariantCompare, "interesting", "\u0130", CompareOptions.IgnoreCase, false };

            // Unicode
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "A\u0300", CompareOptions.None, true };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "A\u0300", CompareOptions.Ordinal, false };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "a\u0300", CompareOptions.None, false };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "a\u0300", CompareOptions.IgnoreCase, true };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "a\u0300", CompareOptions.Ordinal, false };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "a\u0300", CompareOptions.OrdinalIgnoreCase, false };
            yield return new object[] { s_invariantCompare, "FooBar", "Foo\u0400Bar", CompareOptions.Ordinal, false };
            yield return new object[] { s_invariantCompare, "FooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace, true };

            // Ignore symbols
            yield return new object[] { s_invariantCompare, "Test's can be interesting", "Tests", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { s_invariantCompare, "Test's can be interesting", "Tests", CompareOptions.None, false };
        }

        [Theory]
        [MemberData("IsPrefix_TestData")]
        public void IsPrefix(CompareInfo compareInfo, string source, string value, CompareOptions options, bool expected)
        {
            if (options == CompareOptions.None)
            {
                Assert.Equal(expected, compareInfo.IsPrefix(source, value));
            }
            Assert.Equal(expected, compareInfo.IsPrefix(source, value, options));
        }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void IsPrefix_Hungarian()
        {
            // TODO: Remove this function, and combine into IsPrefix_TestData once 5463 is fixed
            IsPrefix(s_hungarianCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.None, true);
            IsPrefix(s_hungarianCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.Ordinal, false);
        }
        
        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void IsPrefix_UnassignedUnicode()
        {
            IsPrefix(s_invariantCompare, "FooBar", "Foo" + UnassignedUnicodeCharacter() + "Bar", CompareOptions.None, true);
            IsPrefix(s_invariantCompare, "FooBar", "Foo" + UnassignedUnicodeCharacter() + "Bar", CompareOptions.IgnoreNonSpace, true);
        }

        [Fact]
        public void IsPrefix_Invalid()
        {
            // Source is null
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IsPrefix(null, ""));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IsPrefix(null, "", CompareOptions.None));

            // Value is null
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IsPrefix("", null));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IsPrefix("", null, CompareOptions.None));

            // Source and prefix are null
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IsPrefix(null, null));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IsPrefix(null, null, CompareOptions.None));

            // Options are invalid
            Assert.Throws<ArgumentException>(() => s_invariantCompare.IsPrefix("Test's", "Tests", CompareOptions.StringSort));
            Assert.Throws<ArgumentException>(() => s_invariantCompare.IsPrefix("Test's", "Tests", (CompareOptions)(-1)));
            Assert.Throws<ArgumentException>(() => s_invariantCompare.IsPrefix("Test's", "Tests", (CompareOptions)0x11111111));
        }
        
        private static char UnassignedUnicodeCharacter()
        {
            for (char ch = '\uFFFF'; ch > '\u0000'; ch++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.OtherNotAssigned)
                {
                    return ch;
                }
            }
            return char.MinValue; // There are no unassigned unicode characters from \u0000 - \uFFFF
        }
    }
}
