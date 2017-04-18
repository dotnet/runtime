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
            yield return new object[] { s_hungarianCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.Ordinal, false };            

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
            
            // Platform differences
            yield return new object[] { s_hungarianCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.None, PlatformDetection.IsWindows ? true : false };
        }

        [Theory]
        [MemberData(nameof(IsPrefix_TestData))]
        public void IsPrefix(CompareInfo compareInfo, string source, string value, CompareOptions options, bool expected)
        {
            if (options == CompareOptions.None)
            {
                Assert.Equal(expected, compareInfo.IsPrefix(source, value));
            }
            Assert.Equal(expected, compareInfo.IsPrefix(source, value, options));
        }

        [Fact]
        public void IsPrefix_UnassignedUnicode()
        {
            bool result = PlatformDetection.IsWindows ? true : false;
            IsPrefix(s_invariantCompare, "FooBar", "Foo\uFFFFBar", CompareOptions.None, result);
            IsPrefix(s_invariantCompare, "FooBar", "Foo\uFFFFBar", CompareOptions.IgnoreNonSpace, result);
        }

        [Fact]
        public void IsPrefix_Invalid()
        {
            // Source is null
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IsPrefix(null, ""));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IsPrefix(null, "", CompareOptions.None));

            // Value is null
            AssertExtensions.Throws<ArgumentNullException>("prefix", () => s_invariantCompare.IsPrefix("", null));
            AssertExtensions.Throws<ArgumentNullException>("prefix", () => s_invariantCompare.IsPrefix("", null, CompareOptions.None));

            // Source and prefix are null
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IsPrefix(null, null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IsPrefix(null, null, CompareOptions.None));

            // Options are invalid
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsPrefix("Test's", "Tests", CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsPrefix("Test's", "Tests", CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsPrefix("Test's", "Tests", CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsPrefix("Test's", "Tests", (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsPrefix("Test's", "Tests", (CompareOptions)0x11111111));
        }
    }
}
