// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoIsPrefixTests
    {
        private static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        private static CompareInfo s_germanCompare = new CultureInfo("de-DE").CompareInfo;
        private static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        private static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;
        private static CompareInfo s_frenchCompare = new CultureInfo("fr-FR").CompareInfo;

        public static IEnumerable<object[]> IsPrefix_TestData()
        {
            // Empty strings
            yield return new object[] { s_invariantCompare, "foo", "", CompareOptions.None, true, 0 };
            yield return new object[] { s_invariantCompare, "", "", CompareOptions.None, true, 0 };

            // Long strings
            yield return new object[] { s_invariantCompare, new string('a', 5555), "aaaaaaaaaaaaaaa", CompareOptions.None, true, 15 };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000), CompareOptions.None, true, 5000 };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000) + "b", CompareOptions.None, false, 0 };

            // Hungarian
            yield return new object[] { s_invariantCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_hungarianCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "dz", "d", CompareOptions.None, true, 1 };
            yield return new object[] { s_hungarianCompare, "dz", "d", CompareOptions.None, false, 0 };
            yield return new object[] { s_hungarianCompare, "dz", "d", CompareOptions.Ordinal, true, 1 };

            // Turkish
            yield return new object[] { s_turkishCompare, "interesting", "I", CompareOptions.None, false, 0 };
            // Android has its own ICU, which doesn't work well with tr
            if (!PlatformDetection.IsAndroid)
            {
                yield return new object[] { s_turkishCompare, "interesting", "I", CompareOptions.IgnoreCase, false, 0 };
                yield return new object[] { s_turkishCompare, "interesting", "\u0130", CompareOptions.IgnoreCase, true, 1 };
            }
            yield return new object[] { s_turkishCompare, "interesting", "\u0130", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "interesting", "I", CompareOptions.IgnoreCase, true, 1 };
            yield return new object[] { s_invariantCompare, "interesting", "I", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "interesting", "\u0130", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "interesting", "\u0130", CompareOptions.IgnoreCase, false, 0 };

            // Unicode
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "A\u0300", CompareOptions.None, true, 1 };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "A\u0300", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "a\u0300", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "a\u0300", CompareOptions.IgnoreCase, true, 1 };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "a\u0300", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "\u00C0nimal", "a\u0300", CompareOptions.OrdinalIgnoreCase, false, 0 };
            yield return new object[] { s_invariantCompare, "FooBar", "Foo\u0400Bar", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "FooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace, true, 7 };
            yield return new object[] { s_invariantCompare, "o\u0308", "o", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "o\u0308", "o", CompareOptions.Ordinal, true, 1 };
            yield return new object[] { s_invariantCompare, "o\u0000\u0308", "o", CompareOptions.None, true, 1 };

            // Weightless comparisons
            yield return new object[] { s_invariantCompare, "", "\u200d", CompareOptions.None, true, 0 };
            yield return new object[] { s_invariantCompare, "\u200dxy", "x", CompareOptions.None, true, 2 };

            // Surrogates
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800\uDC00", CompareOptions.None, true, 2 };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800\uDC00", CompareOptions.IgnoreCase, true, 2 };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.Ordinal, true, 1 };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.OrdinalIgnoreCase, true, 1 };

            // Malformed Unicode - Invalid Surrogates (there is nothing special about them, they don't have a special treatment)
            yield return new object[] { s_invariantCompare, "\uD800\uD800", "\uD800", CompareOptions.None, true, 1 };
            yield return new object[] { s_invariantCompare, "\uD800\uD800", "\uD800\uD800", CompareOptions.None, true, 2 };

            // Ignore symbols
            yield return new object[] { s_invariantCompare, "Test's can be interesting", "Tests", CompareOptions.IgnoreSymbols, true, 6 };
            yield return new object[] { s_invariantCompare, "Test's can be interesting", "Tests", CompareOptions.None, false, 0 };

            // Platform differences
            bool useNls = PlatformDetection.IsNlsGlobalization;
            if (useNls)
            {
                yield return new object[] { s_hungarianCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.None, true, 7 };
                yield return new object[] { s_invariantCompare, "''Tests", "Tests", CompareOptions.IgnoreSymbols, true, 7 };
                yield return new object[] { s_frenchCompare, "\u0153", "oe", CompareOptions.None, true, 1 };
                yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.None, true, 1 };
                yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.IgnoreCase, true, 1 };
            }
            else
            {
                yield return new object[] { s_hungarianCompare, "dzsdzsfoobar", "ddzsf", CompareOptions.None, false, 0 };
                yield return new object[] { s_invariantCompare, "''Tests", "Tests", CompareOptions.IgnoreSymbols, false, 0 };
                yield return new object[] { s_frenchCompare, "\u0153", "oe", CompareOptions.None, false, 0 };
                yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.None, false, 0 };
                yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800", CompareOptions.IgnoreCase, false, 0 };
            }

            // ICU bugs
            // UInt16 overflow: https://unicode-org.atlassian.net/browse/ICU-20832 fixed in https://github.com/unicode-org/icu/pull/840 (ICU 65)
            if (useNls || PlatformDetection.ICUVersion.Major >= 65)
            {
                yield return new object[] { s_frenchCompare, "b", new string('a', UInt16.MaxValue + 1), CompareOptions.None, false, 0 };
            }

            // Prefixes where matched length does not equal value string length
            yield return new object[] { s_invariantCompare, "dzxyz", "\u01F3", CompareOptions.IgnoreNonSpace, true, 2 };
            yield return new object[] { s_invariantCompare, "\u01F3xyz", "dz", CompareOptions.IgnoreNonSpace, true, 1 };
            yield return new object[] { s_germanCompare, "Strasse xyz", "stra\u00DFe", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, true, 7 };
            yield return new object[] { s_germanCompare, "Strasse xyz", "xtra\u00DFe", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, false, 0 };
            yield return new object[] { s_germanCompare, "stra\u00DFe xyz", "Strasse", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, true, 6 };
            yield return new object[] { s_germanCompare, "stra\u00DFe xyz", "Xtrasse", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, false, 0 };
        }

        [Theory]
        [MemberData(nameof(IsPrefix_TestData))]
        public void IsPrefix(CompareInfo compareInfo, string source, string value, CompareOptions options, bool expected, int expectedMatchLength)
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

            // Now test the span version - use BoundedMemory to detect buffer overruns

            using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(source);
            sourceBoundedMemory.MakeReadonly();

            using BoundedMemory<char> valueBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(value);
            valueBoundedMemory.MakeReadonly();

            Assert.Equal(expected, compareInfo.IsPrefix(sourceBoundedMemory.Span, valueBoundedMemory.Span, options));
            Assert.Equal(expected, compareInfo.IsPrefix(sourceBoundedMemory.Span, valueBoundedMemory.Span, options, out int actualMatchLength));
            Assert.Equal(expectedMatchLength, actualMatchLength);
        }

        [Fact]
        public void IsPrefix_UnassignedUnicode()
        {
            bool result = PlatformDetection.IsNlsGlobalization ? true : false;
            int expectedMatchLength = (result) ? 6 : 0;
            IsPrefix(s_invariantCompare, "FooBar", "Foo\uFFFFBar", CompareOptions.None, result, expectedMatchLength);
            IsPrefix(s_invariantCompare, "FooBar", "Foo\uFFFFBar", CompareOptions.IgnoreNonSpace, result, expectedMatchLength);
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

        [Fact]
        public void IsPrefix_WithEmptyPrefix_DoesNotValidateOptions()
        {
            IsPrefix(s_invariantCompare, "Hello", "", (CompareOptions)(-1), true, 0);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        public void IsPrefixWithAsciiAndIgnoredCharacters()
        {
            Assert.StartsWith("A", "A\0");
            Assert.StartsWith("A\0", "A");
            Assert.StartsWith("a", "A\0", StringComparison.CurrentCultureIgnoreCase);
            Assert.StartsWith("a\0", "A", StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
