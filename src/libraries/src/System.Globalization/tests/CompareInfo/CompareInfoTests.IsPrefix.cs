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
        private static CompareInfo s_frenchCompare = new CultureInfo("fr-FR").CompareInfo;

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
            yield return new object[] { s_invariantCompare, "dz", "d", CompareOptions.None, true };
            yield return new object[] { s_hungarianCompare, "dz", "d", CompareOptions.None, false };
            yield return new object[] { s_hungarianCompare, "dz", "d", CompareOptions.Ordinal, true };

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
            yield return new object[] { s_invariantCompare, "o\u0308", "o", CompareOptions.None, false };
            yield return new object[] { s_invariantCompare, "o\u0308", "o", CompareOptions.Ordinal, true };
            yield return new object[] { s_invariantCompare, "o\u0000\u0308", "o", CompareOptions.None, true };

            // Surrogates
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800\uDC00", CompareOptions.None, true };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800\uDC00", CompareOptions.IgnoreCase, true };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.Ordinal, true };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.OrdinalIgnoreCase, true };

            // Malformed Unicode - Invalid Surrogates (there is nothing special about them, they don't have a special treatment)
            yield return new object[] { s_invariantCompare, "\uD800\uD800", "\uD800", CompareOptions.None, true };
            yield return new object[] { s_invariantCompare, "\uD800\uD800", "\uD800\uD800", CompareOptions.None, true };

            // Ignore symbols
            yield return new object[] { s_invariantCompare, "Test's can be interesting", "Tests", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { s_invariantCompare, "Test's can be interesting", "Tests", CompareOptions.None, false };

            // Platform differences
            yield return new object[] { s_hungarianCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.None, PlatformDetection.IsWindows ? true : false };
            yield return new object[] { s_invariantCompare, "''Tests", "Tests", CompareOptions.IgnoreSymbols, PlatformDetection.IsWindows ? true : false };
            yield return new object[] { s_frenchCompare, "\u0153", "oe", CompareOptions.None, PlatformDetection.IsWindows ? true : false };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.None, PlatformDetection.IsWindows ? true : false };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.IgnoreCase, PlatformDetection.IsWindows ? true : false };

            // ICU bugs
            // UInt16 overflow: https://unicode-org.atlassian.net/browse/ICU-20832 fixed in https://github.com/unicode-org/icu/pull/840 (ICU 65)
            if (PlatformDetection.IsWindows || PlatformDetection.ICUVersion.Major >= 65)
            {
                yield return new object[] { s_frenchCompare, "b", new string('a', UInt16.MaxValue + 1), CompareOptions.None, false };
            }
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

            if ((compareInfo == s_invariantCompare) && ((options == CompareOptions.None) || (options == CompareOptions.IgnoreCase)))
            {
                StringComparison stringComparison = (options == CompareOptions.IgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
                Assert.Equal(expected, source.StartsWith(value, stringComparison));
                Assert.Equal(expected, source.AsSpan().StartsWith(value.AsSpan(), stringComparison));
            }
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
