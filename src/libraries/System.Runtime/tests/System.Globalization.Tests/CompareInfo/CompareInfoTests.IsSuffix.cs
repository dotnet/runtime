// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoIsSuffixTests : CompareInfoTestsBase
    {
        public static IEnumerable<object[]> IsSuffix_TestData()
        {
            // Empty strings
            yield return new object[] { s_invariantCompare, "foo", "", CompareOptions.None, true, 0 };
            yield return new object[] { s_invariantCompare, "", "", CompareOptions.None, true, 0 };

            // Long strings
            yield return new object[] { s_invariantCompare, new string('a', 5555), "aaaaaaaaaaaaaaa", CompareOptions.None, true, 15 };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000), CompareOptions.None, true, 5000 };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000) + "b", CompareOptions.None, false, 0 };

            // Hungarian
            yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "dz", "z", CompareOptions.None, true, 1 };
            if (!PlatformDetection.IsHybridGlobalizationOnBrowser && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                yield return new object[] { s_hungarianCompare, "dz", "z", CompareOptions.None, false, 0 };
            yield return new object[] { s_hungarianCompare, "dz", "z", CompareOptions.Ordinal, true, 1 };

            // Slovak
            if (!PlatformDetection.IsHybridGlobalizationOnBrowser && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { s_slovakCompare, "ch", "h", CompareOptions.None, false, 0 };
                yield return new object[] { s_slovakCompare, "velmi chora", "hora", CompareOptions.None, false, 0 };
            }
            yield return new object[] { s_slovakCompare, "chh", "H", CompareOptions.IgnoreCase, true, 1 };

            // Turkish
            yield return new object[] { s_turkishCompare, "Hi", "I", CompareOptions.None, false, 0 };
            // Android has its own ICU, which doesn't work well with tr
            if (!PlatformDetection.IsAndroid && !PlatformDetection.IsLinuxBionic)
            {
                yield return new object[] { s_turkishCompare, "Hi", "I", CompareOptions.IgnoreCase, false, 0 };
                yield return new object[] { s_turkishCompare, "Hi", "\u0130", CompareOptions.IgnoreCase, true, 1 };
            }
            yield return new object[] { s_turkishCompare, "Hi", "\u0130", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "Hi", "I", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "Hi", "I", CompareOptions.IgnoreCase, true, 1 };
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", CompareOptions.IgnoreCase, false, 0 };

            // Unicode
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", CompareOptions.None, true, 1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", CompareOptions.IgnoreCase, true, 1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", CompareOptions.OrdinalIgnoreCase, false, 0 };
            yield return new object[] { s_invariantCompare, "FooBar", "Foo\u0400Bar", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "FooBA\u0300R", "FooB\u00C0R", supportedIgnoreNonSpaceOption, true, 7 };
            yield return new object[] { s_invariantCompare, "o\u0308", "o", CompareOptions.None, false, 0 };
            yield return new object[] { s_invariantCompare, "o\u0308", "o", CompareOptions.Ordinal, false, 0 };
            yield return new object[] { s_invariantCompare, "o\u0308o", "o", CompareOptions.None, true, 1 };
            yield return new object[] { s_invariantCompare, "o\u0308o", "o", CompareOptions.Ordinal, true, 1 };

            // Weightless comparisons
            yield return new object[] { s_invariantCompare, "", "\u200d", CompareOptions.None, true, 0 };
            yield return new object[] { s_invariantCompare, "xy\u200d", "y", CompareOptions.None, true, 2 };

            // Surrogates
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800\uDC00", CompareOptions.None, true, 2 };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uD800\uDC00", CompareOptions.IgnoreCase, true, 2 };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uDC00", CompareOptions.Ordinal, true, 1 };
            yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uDC00", CompareOptions.OrdinalIgnoreCase, true, 1 };

            // Malformed Unicode - Invalid Surrogates (there is nothing special about them, they don't have a special treatment)
            yield return new object[] { s_invariantCompare, "\uD800\uD800", "\uD800", CompareOptions.None, true, 1 };
            yield return new object[] { s_invariantCompare, "\uD800\uD800", "\uD800\uD800", CompareOptions.None, true, 2 };

            // Ignore symbols
            if (!PlatformDetection.IsHybridGlobalizationOnBrowser && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { s_invariantCompare, "More Test's", "Tests", CompareOptions.IgnoreSymbols, true, 6 };
                yield return new object[] { s_invariantCompare, "More Test's", "Tests", CompareOptions.None, false, 0 };
            }

            // NULL character
            yield return new object[] { s_invariantCompare, "a\u0000b", "a\u0000b", CompareOptions.None, true, 3 };
            yield return new object[] { s_invariantCompare, "a\u0000b", "b\u0000b", CompareOptions.None, false, 0 };

            // Platform differences
            // in HybridGlobalization on Browser we use TextEncoder that is not supported for v8 and the manual decoding works like NLS
            bool behavesLikeNls = PlatformDetection.IsNlsGlobalization || 
                (PlatformDetection.IsHybridGlobalizationOnBrowser && !PlatformDetection.IsBrowserDomSupportedOrNodeJS);
            if (behavesLikeNls)
            {
                if (!PlatformDetection.IsHybridGlobalizationOnBrowser)
                {
                    yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", CompareOptions.None, true, 7 };
                    yield return new object[] { s_frenchCompare, "\u0153", "oe", CompareOptions.None, true, 1 };
                }
                yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uDC00", CompareOptions.None, true, 1 };
                yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uDC00", CompareOptions.IgnoreCase, true, 1 };
            } else
            {
                yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", CompareOptions.None, false, 0 };
                yield return new object[] { s_frenchCompare, "\u0153", "oe", CompareOptions.None, false, 0 };
                if (PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                {
                    yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uDC00", CompareOptions.None, false, 0 };
                    yield return new object[] { s_invariantCompare, "\uD800\uDC00", "\uDC00", CompareOptions.IgnoreCase, false, 0 };
                }
            }

            // Suffixes where matched length does not equal value string length
            yield return new object[] { s_germanCompare, "xyz Strasse", "xtra\u00DFe", supportedIgnoreCaseIgnoreNonSpaceOptions, false, 0 };
            if (!PlatformDetection.IsHybridGlobalizationOnBrowser && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { s_invariantCompare, "xyzdz", "\u01F3", supportedIgnoreNonSpaceOption, true, 2 };
                yield return new object[] { s_invariantCompare, "xyz\u01F3", "dz", supportedIgnoreNonSpaceOption, true, 1 };
                yield return new object[] { s_germanCompare, "xyz stra\u00DFe", "Strasse", supportedIgnoreCaseIgnoreNonSpaceOptions, true, 6 };
                yield return new object[] { s_germanCompare, "xyz Strasse", "stra\u00DFe", supportedIgnoreCaseIgnoreNonSpaceOptions, true, 7 };
            }
            yield return new object[] { s_germanCompare, "xyz stra\u00DFe", "Xtrasse", supportedIgnoreCaseIgnoreNonSpaceOptions, false, 0 };
        }

        [Theory]
        [MemberData(nameof(IsSuffix_TestData))]
        public void IsSuffix(CompareInfo compareInfo, string source, string value, CompareOptions options, bool expected, int expectedMatchLength)
        {
            if (options == CompareOptions.None)
            {
                Assert.Equal(expected, compareInfo.IsSuffix(source, value));
            }
            Assert.Equal(expected, compareInfo.IsSuffix(source, value, options));

            if ((compareInfo == s_invariantCompare) && ((options == CompareOptions.None) || (options == CompareOptions.IgnoreCase)))
            {
                StringComparison stringComparison = (options == CompareOptions.IgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
                Assert.Equal(expected, source.EndsWith(value, stringComparison));
                Assert.Equal(expected, source.AsSpan().EndsWith(value.AsSpan(), stringComparison));
            }

            // Now test the span version - use BoundedMemory to detect buffer overruns

            using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(source);
            sourceBoundedMemory.MakeReadonly();

            using BoundedMemory<char> valueBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(value);
            valueBoundedMemory.MakeReadonly();

            Assert.Equal(expected, compareInfo.IsSuffix(sourceBoundedMemory.Span, valueBoundedMemory.Span, options));
            if (!PlatformDetection.IsHybridGlobalizationOnBrowser && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                Assert.Equal(expected, compareInfo.IsSuffix(sourceBoundedMemory.Span, valueBoundedMemory.Span, options, out int actualMatchLength));
                Assert.Equal(expectedMatchLength, actualMatchLength);
            }
        }

        [Fact]
        public void IsSuffix_UnassignedUnicode()
        {
            bool result = PlatformDetection.IsIcuGlobalization || PlatformDetection.IsHybridGlobalizationOnApplePlatform ? false : true;
            int expectedMatchLength = (result) ? 6 : 0;

            IsSuffix(s_invariantCompare, "FooBar", "Foo\uFFFFBar", CompareOptions.None, result, expectedMatchLength);
            IsSuffix(s_invariantCompare, "FooBar", "Foo\uFFFFBar", supportedIgnoreNonSpaceOption, result, expectedMatchLength);
        }

        [Fact]
        public void IsSuffix_Invalid()
        {
            // Source is null
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IsSuffix(null, ""));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IsSuffix(null, "", CompareOptions.None));

            // Prefix is null
            AssertExtensions.Throws<ArgumentNullException>("suffix", () => s_invariantCompare.IsSuffix("", null));
            AssertExtensions.Throws<ArgumentNullException>("suffix", () => s_invariantCompare.IsSuffix("", null, CompareOptions.None));

            // Source and prefix are null
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IsSuffix(null, null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IsSuffix(null, null, CompareOptions.None));

            // Options are invalid
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsSuffix("Test's", "Tests", CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsSuffix("Test's", "Tests", CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsSuffix("Test's", "Tests", CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsSuffix("Test's", "Tests", (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IsSuffix("Test's", "Tests", (CompareOptions)0x11111111));
        }

        [Fact]
        public void IsSuffix_WithEmptyPrefix_DoesNotValidateOptions()
        {
            IsSuffix(s_invariantCompare, "Hello", "", (CompareOptions)(-1), true, 0);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        public void IsSuffixWithAsciiAndIgnoredCharacters()
        {
            Assert.EndsWith("A", "A\0");
            Assert.EndsWith("A\0", "A");
            Assert.EndsWith("a", "A\0", StringComparison.CurrentCultureIgnoreCase);
            Assert.EndsWith("a\0", "A", StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
