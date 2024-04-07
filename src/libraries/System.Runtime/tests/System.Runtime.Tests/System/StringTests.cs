// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public partial class StringTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, 1)]
        public static void Ctor_CharSpan_EmptyString(int length, int offset)
        {
            Assert.Same(string.Empty, new string(new ReadOnlySpan<char>(new char[length], offset, 0)));
        }

        [Fact]
        public static unsafe void Ctor_CharSpan_Empty()
        {
            Assert.Same(string.Empty, new string((ReadOnlySpan<char>)null));
            Assert.Same(string.Empty, new string(ReadOnlySpan<char>.Empty));
        }

        [Theory]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0' }, 0, 8, "abcdefgh")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0', 'i', 'j', 'k' }, 0, 12, "abcdefgh\0ijk")]
        [InlineData(new char[] { 'a', 'b', 'c' }, 0, 0, "")]
        [InlineData(new char[] { 'a', 'b', 'c' }, 0, 1, "a")]
        [InlineData(new char[] { 'a', 'b', 'c' }, 2, 1, "c")]
        [InlineData(new char[] { '\u8001', '\u8002', '\ufffd', '\u1234', '\ud800', '\udfff' }, 0, 6, "\u8001\u8002\ufffd\u1234\ud800\udfff")]
        public static void Ctor_CharSpan(char[] valueArray, int startIndex, int length, string expected)
        {
            var span = new ReadOnlySpan<char>(valueArray, startIndex, length);
            Assert.Equal(expected, new string(span));
        }

        [Fact]
        public static unsafe void Ctor_CharPtr_DoesNotAccessInvalidPage()
        {
            // Allocates a buffer of all 'x' followed by a null terminator,
            // then attempts to create a string instance from this at various offsets.

            const int MaxCharCount = 128;
            using BoundedMemory<char> boundedMemory = BoundedMemory.Allocate<char>(MaxCharCount);
            boundedMemory.Span.Fill('x');
            boundedMemory.Span[MaxCharCount - 1] = '\0';
            boundedMemory.MakeReadonly();

            using MemoryHandle memoryHandle = boundedMemory.Memory.Pin();

            for (int i = 0; i < MaxCharCount; i++)
            {
                string expectedString = new string('x', MaxCharCount - i - 1);
                string actualString = new string((char*)memoryHandle.Pointer + i);
                Assert.Equal(expectedString, actualString);
            }
        }

        [ConditionalFact(nameof(IsSimpleActiveCodePage))]
        public static unsafe void Ctor_SBytePtr_DoesNotAccessInvalidPage()
        {
            // Allocates a buffer of all ' ' followed by a null terminator,
            // then attempts to create a string instance from this at various offsets.
            // We use U+0020 SPACE instead of any other character because it lives
            // at offset 0x20 across every supported code page.

            const int MaxByteCount = 128;
            using BoundedMemory<sbyte> boundedMemory = BoundedMemory.Allocate<sbyte>(MaxByteCount);
            boundedMemory.Span.Fill((sbyte)' ');
            boundedMemory.Span[MaxByteCount - 1] = (sbyte)'\0';
            boundedMemory.MakeReadonly();

            using MemoryHandle memoryHandle = boundedMemory.Memory.Pin();

            for (int i = 0; i < MaxByteCount; i++)
            {
                string expectedString = new string(' ', MaxByteCount - i - 1);
                string actualString = new string((sbyte*)memoryHandle.Pointer + i);
                Assert.Equal(expectedString, actualString);
            }
        }

        [Fact]
        public static void Create_InvalidArguments_Throw()
        {
            AssertExtensions.Throws<ArgumentNullException>("action", () => string.Create(-1, 0, null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => string.Create(-1, 0, (span, state) => { }));
        }

        [Fact]
        public static void Create_Length0_ReturnsEmptyString()
        {
            bool actionInvoked = false;
            Assert.Same(string.Empty, string.Create(0, 0, (span, state) => actionInvoked = true));
            Assert.False(actionInvoked);
        }

        [Fact]
        public static void Create_NullState_Allowed()
        {
            string result = string.Create(1, (object)null, (span, state) =>
            {
                span[0] = 'a';
                Assert.Null(state);
            });
            Assert.Equal("a", result);
        }

        [Fact]
        public static void Create_ClearsMemory()
        {
            const int Length = 10;
            string result = string.Create(Length, (object)null, (span, state) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal('\0', span[i]);
                }
            });
            Assert.Equal(new string('\0', Length), result);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("this is a test")]
        [InlineData("\0\u8001\u8002\ufffd\u1234\ud800\udfff")]
        public static void Create_ReturnsExpectedString(string expected)
        {
            char[] input = expected.ToCharArray();
            string result = string.Create(input.Length, input, (span, state) =>
            {
                Assert.Same(input, state);
                for (int i = 0; i < state.Length; i++)
                {
                    span[i] = state[i];
                }
            });
            Assert.Equal(expected, result);
        }

        [Fact]
        public static void Create_InterpolatedString_ConstructsStringAndClearsBuilder()
        {
            Span<char> initialBuffer = stackalloc char[16];

            DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(0, 0, CultureInfo.InvariantCulture, initialBuffer);
            handler.AppendLiteral("hello");
            Assert.Equal("hello", string.Create(CultureInfo.InvariantCulture, initialBuffer, ref handler));
            Assert.Equal("", string.Create(CultureInfo.InvariantCulture, initialBuffer, ref handler));

            handler = new DefaultInterpolatedStringHandler(0, 0, CultureInfo.InvariantCulture);
            handler.AppendLiteral("hello");
            Assert.Equal("hello", string.Create(CultureInfo.InvariantCulture, ref handler));
            Assert.Equal("", string.Create(CultureInfo.InvariantCulture, ref handler));
        }

        [Theory]
        [InlineData("Hello", 'H', true)]
        [InlineData("Hello", 'Z', false)]
        [InlineData("Hello", 'e', true)]
        [InlineData("Hello", 'E', false)]
        [InlineData("", 'H', false)]
        public static void Contains_Char(string s, char value, bool expected)
        {
            Assert.Equal(expected, s.Contains(value));

            ReadOnlySpan<char> span = s.AsSpan();
            Assert.Equal(expected, span.Contains(value));
        }

        [Theory]
        // CurrentCulture
        [InlineData("Hello", 'H', StringComparison.CurrentCulture, true)]
        [InlineData("Hello", 'Z', StringComparison.CurrentCulture, false)]
        [InlineData("Hello", 'e', StringComparison.CurrentCulture, true)]
        [InlineData("Hello", 'E', StringComparison.CurrentCulture, false)]
        [InlineData("", 'H', StringComparison.CurrentCulture, false)]
        // CurrentCultureIgnoreCase
        [InlineData("Hello", 'H', StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", 'Z', StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", 'e', StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", 'E', StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", 'H', StringComparison.CurrentCultureIgnoreCase, false)]
        // InvariantCulture
        [InlineData("Hello", 'H', StringComparison.InvariantCulture, true)]
        [InlineData("Hello", 'Z', StringComparison.InvariantCulture, false)]
        [InlineData("Hello", 'e', StringComparison.InvariantCulture, true)]
        [InlineData("Hello", 'E', StringComparison.InvariantCulture, false)]
        [InlineData("", 'H', StringComparison.InvariantCulture, false)]
        // InvariantCultureIgnoreCase
        [InlineData("Hello", 'H', StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", 'Z', StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", 'e', StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", 'E', StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", 'H', StringComparison.InvariantCultureIgnoreCase, false)]
        // Ordinal
        [InlineData("Hello", 'H', StringComparison.Ordinal, true)]
        [InlineData("Hello", 'Z', StringComparison.Ordinal, false)]
        [InlineData("Hello", 'e', StringComparison.Ordinal, true)]
        [InlineData("Hello", 'E', StringComparison.Ordinal, false)]
        [InlineData("", 'H', StringComparison.Ordinal, false)]
        // OrdinalIgnoreCase
        [InlineData("Hello", 'H', StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", 'Z', StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", 'e', StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", 'E', StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("", 'H', StringComparison.OrdinalIgnoreCase, false)]
        public static void Contains_Char_StringComparison(string s, char value, StringComparison comparisonType, bool expected)
        {
            Assert.Equal(expected, s.Contains(value, comparisonType));
        }

        public static IEnumerable<object[]> Contains_String_StringComparison_TestData()
        {
            yield return new object[] { "Hello", "ello", StringComparison.CurrentCulture, true };
            yield return new object[] { "Hello", "ELL", StringComparison.CurrentCulture, false };
            yield return new object[] { "Hello", "ElLo", StringComparison.CurrentCulture, false };
            yield return new object[] { "Hello", "Larger Hello", StringComparison.CurrentCulture, false };
            yield return new object[] { "Hello", "Goodbye", StringComparison.CurrentCulture, false };
            yield return new object[] { "", "", StringComparison.CurrentCulture, true };
            yield return new object[] { "", "hello", StringComparison.CurrentCulture, false };
            yield return new object[] { "Hello", "", StringComparison.CurrentCulture, true };
            yield return new object[] { "Hello", "Ell" + SoftHyphen, StringComparison.CurrentCulture, false };

            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                yield return new object[] { "Hello", "ell" + SoftHyphen, StringComparison.CurrentCulture, true };

            // CurrentCultureIgnoreCase
            yield return new object[] { "Hello", "ello", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "ELL", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "ElLo", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Larger Hello", StringComparison.CurrentCultureIgnoreCase, false };
            yield return new object[] { "Hello", "Goodbye", StringComparison.CurrentCultureIgnoreCase, false };
            yield return new object[] { "", "", StringComparison.CurrentCultureIgnoreCase, true };
            yield return new object[] { "", "hello", StringComparison.CurrentCultureIgnoreCase, false };
            yield return new object[] { "Hello", "", StringComparison.CurrentCultureIgnoreCase, true };

            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { "Hello", "ell" + SoftHyphen, StringComparison.CurrentCultureIgnoreCase, true };
                yield return new object[] { "Hello", "Ell" + SoftHyphen, StringComparison.CurrentCultureIgnoreCase, true };
            }

            // InvariantCulture
            yield return new object[] { "Hello", "ello", StringComparison.InvariantCulture, true };
            yield return new object[] { "Hello", "ELL", StringComparison.InvariantCulture, false };
            yield return new object[] { "Hello", "ElLo", StringComparison.InvariantCulture, false };
            yield return new object[] { "Hello", "Larger Hello", StringComparison.InvariantCulture, false };
            yield return new object[] { "Hello", "Goodbye", StringComparison.InvariantCulture, false };
            yield return new object[] { "", "", StringComparison.InvariantCulture, true };
            yield return new object[] { "", "hello", StringComparison.InvariantCulture, false };
            yield return new object[] { "Hello", "", StringComparison.InvariantCulture, true };
            yield return new object[] { "Hello", "Ell" + SoftHyphen, StringComparison.InvariantCulture, false };

            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                yield return new object[] { "Hello", "ell" + SoftHyphen, StringComparison.InvariantCulture, true };

            // InvariantCultureIgnoreCase
            yield return new object[] { "Hello", "ello", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "ELL", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "ElLo", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "Hello", "Larger Hello", StringComparison.InvariantCultureIgnoreCase, false };
            yield return new object[] { "Hello", "Goodbye", StringComparison.InvariantCultureIgnoreCase, false };
            yield return new object[] { "", "", StringComparison.InvariantCultureIgnoreCase, true };
            yield return new object[] { "", "hello", StringComparison.InvariantCultureIgnoreCase, false };
            yield return new object[] { "Hello", "", StringComparison.InvariantCultureIgnoreCase, true };

            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { "Hello", "ell" + SoftHyphen, StringComparison.InvariantCultureIgnoreCase, true };
                yield return new object[] { "Hello", "Ell" + SoftHyphen, StringComparison.InvariantCultureIgnoreCase, true };
            }

            // Ordinal
            yield return new object[] { "Hello", "ello", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "ELL", StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "ElLo", StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "Larger Hello", StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "Goodbye", StringComparison.Ordinal, false };
            yield return new object[] { "", "", StringComparison.Ordinal, true };
            yield return new object[] { "", "hello", StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "", StringComparison.Ordinal, true };
            yield return new object[] { "Hello", "ell" + SoftHyphen, StringComparison.Ordinal, false };
            yield return new object[] { "Hello", "Ell" + SoftHyphen, StringComparison.Ordinal, false };

            // OrdinalIgnoreCase
            yield return new object[] { "Hello", "ello", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "ELL", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "ElLo", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "Larger Hello", StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "Hello", "Goodbye", StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "", "", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "", "hello", StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "Hello", "", StringComparison.OrdinalIgnoreCase, true };
            yield return new object[] { "Hello", "ell" + SoftHyphen, StringComparison.OrdinalIgnoreCase, false };
            yield return new object[] { "Hello", "Ell" + SoftHyphen, StringComparison.OrdinalIgnoreCase, false };
        }

        [Theory]
        [MemberData(nameof(Contains_String_StringComparison_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95473", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        public static void Contains_String_StringComparison(string s, string value, StringComparison comparisonType, bool expected)
        {
            Assert.Equal(expected, s.Contains(value, comparisonType));
            Assert.Equal(expected, s.AsSpan().Contains(value, comparisonType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60568", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public static void Contains_StringComparison_TurkishI()
        {
            const string Source = "\u0069\u0130";

            using (new ThreadCultureChange("tr-TR"))
            {
                Assert.True(Source.Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));
                Assert.True(Source.AsSpan().Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));
            }

            using (new ThreadCultureChange("en-US"))
            {
                Assert.False(Source.Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));
                Assert.False(Source.AsSpan().Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Fact]
        public static void Contains_Match_Char()
        {
            Assert.False("".Contains('a'));
            Assert.False("".AsSpan().Contains('a'));

            // Use a long-enough string to incur vectorization code
            const int max = 250;

            for (var length = 1; length < max; length++)
            {
                char[] ca = new char[length];
                for (int i = 0; i < length; i++)
                {
                    ca[i] = (char)(i + 1);
                }

                var span = new Span<char>(ca);
                var ros = new ReadOnlySpan<char>(ca);
                var str = new string(ca);

                for (var targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    char target = ca[targetIndex];

                    // Span
                    bool found = span.Contains(target);
                    Assert.True(found);

                    // ReadOnlySpan
                    found = ros.Contains(target);
                    Assert.True(found);

                    // String
                    found = str.Contains(target);
                    Assert.True(found);
                }
            }
        }

        [Fact]
        public static void Contains_ZeroLength_Char()
        {
            // Span
            var span = new Span<char>(Array.Empty<char>());
            bool found = span.Contains((char)0);
            Assert.False(found);

            span = Span<char>.Empty;
            found = span.Contains((char)0);
            Assert.False(found);

            // ReadOnlySpan
            var ros = new ReadOnlySpan<char>(Array.Empty<char>());
            found = ros.Contains((char)0);
            Assert.False(found);

            ros = ReadOnlySpan<char>.Empty;
            found = ros.Contains((char)0);
            Assert.False(found);

            // String
            found = string.Empty.Contains((char)0);
            Assert.False(found);
        }

        [Fact]
        public static void Contains_MultipleMatches_Char()
        {
            for (int length = 2; length < 32; length++)
            {
                var ca = new char[length];
                for (int i = 0; i < length; i++)
                {
                    ca[i] = (char)(i + 1);
                }

                ca[length - 1] = (char)200;
                ca[length - 2] = (char)200;

                // Span
                var span = new Span<char>(ca);
                bool found = span.Contains((char)200);
                Assert.True(found);

                // ReadOnlySpan
                var ros = new ReadOnlySpan<char>(ca);
                found = ros.Contains((char)200);
                Assert.True(found);

                // String
                var str = new string(ca);
                found = str.Contains((char)200);
                Assert.True(found);
            }
        }

        [Fact]
        public static void Contains_EnsureNoChecksGoOutOfRange_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                var ca = new char[length + 2];
                ca[0] = '9';
                ca[length + 1] = '9';

                // Span
                var span = new Span<char>(ca, 1, length);
                bool found = span.Contains('9');
                Assert.False(found);

                // ReadOnlySpan
                var ros = new ReadOnlySpan<char>(ca, 1, length);
                found = ros.Contains('9');
                Assert.False(found);

                // String
                var str = new string(ca, 1, length);
                found = str.Contains('9');
                Assert.False(found);
            }
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public static void Contains_NullValue_WithComparisonType_ThrowsArgumentNullException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".Contains(null, comparisonType));
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture - 1)]
        [InlineData(StringComparison.OrdinalIgnoreCase + 1)]
        public static void Contains_InvalidComparisonType_ThrowsArgumentOutOfRangeException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "ab".Contains("a", comparisonType));
        }

        [Theory]
        [InlineData("Hello", 'o', true)]
        [InlineData("Hello", 'O', false)]
        [InlineData("o", 'o', true)]
        [InlineData("o", 'O', false)]
        [InlineData("Hello", 'e', false)]
        [InlineData("Hello", '\0', false)]
        [InlineData("", '\0', false)]
        [InlineData("\0", '\0', true)]
        [InlineData("", 'a', false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", 'z', true)]
        public static void EndsWith(string s, char value, bool expected)
        {
            Assert.Equal(expected, s.EndsWith(value));
        }

        [Theory]
        [InlineData(new char[0], new int[0])] // empty
        [InlineData(new char[] { 'x', 'y', 'z' }, new int[] { 'x', 'y', 'z' })]
        [InlineData(new char[] { 'x', '\uD86D', '\uDF54', 'y' }, new int[] { 'x', 0x2B754, 'y' })] // valid surrogate pair
        [InlineData(new char[] { 'x', '\uD86D', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // standalone high surrogate
        [InlineData(new char[] { 'x', '\uDF54', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // standalone low surrogate
        [InlineData(new char[] { 'x', '\uD86D' }, new int[] { 'x', 0xFFFD })] // standalone high surrogate at end of string
        [InlineData(new char[] { 'x', '\uDF54' }, new int[] { 'x', 0xFFFD })] // standalone low surrogate at end of string
        [InlineData(new char[] { 'x', '\uD86D', '\uD86D', 'y' }, new int[] { 'x', 0xFFFD, 0xFFFD, 'y' })] // two high surrogates should be two replacement chars
        [InlineData(new char[] { 'x', '\uFFFD', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // literal U+FFFD
        public static void EnumerateRunes(char[] chars, int[] expected)
        {
            // Test data is smuggled as char[] instead of straight-up string since the test framework
            // doesn't like invalid UTF-16 literals.

            string asString = new string(chars);

            // First, use a straight-up foreach keyword to ensure pattern matching works as expected

            List<int> enumeratedScalarValues = new List<int>();
            foreach (Rune rune in asString.EnumerateRunes())
            {
                enumeratedScalarValues.Add(rune.Value);
            }
            Assert.Equal(expected, enumeratedScalarValues.ToArray());

            // Then use LINQ to ensure IEnumerator<...> works as expected

            int[] enumeratedValues = new string(chars).EnumerateRunes().Select(r => r.Value).ToArray();
            Assert.Equal(expected, enumeratedValues);
        }

        [Fact]
        public static void ReplaceLineEndings_NullReplacementText_Throws()
        {
            Assert.Throws<ArgumentNullException>("replacementText", () => "Hello!".ReplaceLineEndings(null));
        }

        [Theory]
        [InlineData("", new[] { "" })]
        [InlineData("abc", new[] { "abc" })]
        [InlineData("<CR>", new[] { "", "" })] // empty sequences before and after the CR
        [InlineData("<CR><CR>", new[] { "", "", "" })] // empty sequences before and after the CR (CR doesn't consume CR)
        [InlineData("<CR><LF>", new[] { "", "" })] // CR should swallow any LF which follows
        [InlineData("a<CR><LF><LF>z", new[] { "a", "", "z" })] // CR should swallow only a single LF which follows
        [InlineData("a<CR>b<LF>c", new[] { "a", "b", "c" })] // CR shouldn't swallow anything other than LF
        [InlineData("aa<CR>bb<LF><CR>cc", new[] { "aa", "bb", "", "cc" })] // LF shouldn't swallow CR which follows
        [InlineData("a<CR>b<VT>c<LF>d<NEL>e<FF>f<PS>g<LS>h", new[] { "a", "b<VT>c", "d", "e", "f", "g", "h" })] // VT not recognized as NLF
        [InlineData("xyz<NEL>", new[] { "xyz", "" })] // sequence at end produces empty string
        [InlineData("<NEL>xyz", new[] { "", "xyz" })] // sequence at beginning produces empty string
        [InlineData("abc<NAK>%def", new[] { "abc<NAK>%def" })] // we don't recognize EBCDIC encodings for LF (see Unicode Standard, Sec. 5.8, Table 5-1)
        public static void ReplaceLineEndings(string input, string[] expectedSegments)
        {
            input = FixupSequences(input);
            expectedSegments = Array.ConvertAll(expectedSegments, FixupSequences);

            // Try Environment.NewLine (and parameterless ctor)

            string expectedEnvNewLineConcat = string.Join(Environment.NewLine, expectedSegments);
            Assert.Equal(expectedEnvNewLineConcat, input.ReplaceLineEndings());
            Assert.Equal(expectedEnvNewLineConcat, input.ReplaceLineEndings(Environment.NewLine));

            // Try removing newlines entirely

            Assert.Equal(string.Concat(expectedSegments) /* no joiner */, input.ReplaceLineEndings(""));

            // And try using a custom separator

            Assert.Equal(string.Join("<SEPARATOR>", expectedSegments), input.ReplaceLineEndings("<SEPARATOR>"));

            if (expectedSegments.Length == 1)
            {
                // If no newline sequences at all, we should return the original string instance as an optimization
                Assert.Same(input, input.ReplaceLineEndings());
                Assert.Same(input, input.ReplaceLineEndings(Environment.NewLine));
                Assert.Same(input, input.ReplaceLineEndings(""));
                Assert.Same(input, input.ReplaceLineEndings("<SEPARATOR>"));
            }

            static string FixupSequences(string input)
            {
                // We use <XYZ> markers so that the original strings show up better in the xunit test runner
                // <VT> is included as a negative test; we *do not* want ReplaceLineEndings to honor it

                return input.Replace("<CR>", "\r")
                    .Replace("<LF>", "\n")
                    .Replace("<VT>", "\v")
                    .Replace("<FF>", "\f")
                    .Replace("<NAK>", "\u0015")
                    .Replace("<NEL>", "\u0085")
                    .Replace("<LS>", "\u2028")
                    .Replace("<PS>", "\u2029");
            }
        }

        [Theory]
        [InlineData("Hello", 'H', true)]
        [InlineData("Hello", 'h', false)]
        [InlineData("H", 'H', true)]
        [InlineData("H", 'h', false)]
        [InlineData("Hello", 'e', false)]
        [InlineData("Hello", '\0', false)]
        [InlineData("", '\0', false)]
        [InlineData("\0", '\0', true)]
        [InlineData("", 'a', false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", 'a', true)]
        public static void StartsWith(string s, char value, bool expected)
        {
            Assert.Equal(expected, s.StartsWith(value));
        }

        public static IEnumerable<object[]> Join_Char_StringArray_TestData()
        {
            yield return new object[] { '|', new string[0], 0, 0, "" };
            yield return new object[] { '|', new string[] { "a" }, 0, 1, "a" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 0, 3, "a|b|c" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 0, 2, "a|b" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 1, 1, "b" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 1, 2, "b|c" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 3, 0, "" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 0, 0, "" };
            yield return new object[] { '|', new string[] { "", "", "" }, 0, 3, "||" };
            yield return new object[] { '|', new string[] { null, null, null }, 0, 3, "||" };
        }

        [Theory]
        [MemberData(nameof(Join_Char_StringArray_TestData))]
        public static void Join_Char_StringArray(char separator, string[] values, int startIndex, int count, string expected)
        {
            if (startIndex == 0 && count == values.Length)
            {
                Assert.Equal(expected, string.Join(separator, values));
                Assert.Equal(expected, string.Join(separator, (IEnumerable<string>)values));
                // We are using concat to force the value to be an IEnumerable and avoid the optimizations for List<T> and T[]
                Assert.Equal(expected, string.Join(separator, values.Concat(new string[0])));
                // Validate the optimization for List<T>
                Assert.Equal(expected, string.Join(separator, new List<string>(values)));
                Assert.Equal(expected, string.Join(separator, (object[])values));
                Assert.Equal(expected, string.Join(separator, (IEnumerable<object>)values));
            }

            Assert.Equal(expected, string.Join(separator, values, startIndex, count));
            Assert.Equal(expected, string.Join(separator.ToString(), values, startIndex, count));
        }

        public static IEnumerable<object[]> Join_Char_ObjectArray_TestData()
        {
            yield return new object[] { '|', new object[0], "" };
            yield return new object[] { '|', new object[] { 1 }, "1" };
            yield return new object[] { '|', new object[] { 1, 2, 3 }, "1|2|3" };
            yield return new object[] { '|', new object[] { new ObjectWithNullToString(), 2, new ObjectWithNullToString() }, "|2|" };
            yield return new object[] { '|', new object[] { "1", null, "3" }, "1||3" };
            yield return new object[] { '|', new object[] { "", "", "" }, "||" };
            yield return new object[] { '|', new object[] { "", null, "" }, "||" };
            yield return new object[] { '|', new object[] { null, null, null }, "||" };
        }

        [Theory]
        [MemberData(nameof(Join_Char_ObjectArray_TestData))]
        public static void Join_Char_ObjectArray(char separator, object[] values, string expected)
        {
            Assert.Equal(expected, string.Join(separator, values));
            Assert.Equal(expected, string.Join(separator, (IEnumerable<object>)values));
        }

        [Fact]
        public static void Join_Char_NullValues_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => string.Join('|', (string[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => string.Join('|', (string[])null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join('|', (object[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join('|', (IEnumerable<object>)null));
        }

        [Fact]
        public static void Join_Char_NegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => string.Join('|', new string[] { "Foo" }, -1, 0));
        }

        [Fact]
        public static void Join_Char_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => string.Join('|', new string[] { "Foo" }, 0, -1));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(2, 0)]
        [InlineData(1, 2)]
        [InlineData(1, 1)]
        [InlineData(0, 2)]
        [InlineData(-1, 0)]
        public static void Join_Char_InvalidStartIndexCount_ThrowsArgumentOutOfRangeException(int startIndex, int count)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => string.Join('|', new string[] { "Foo" }, startIndex, count));
        }

        public static IEnumerable<object[]> Replace_StringComparison_TestData()
        {
            yield return new object[] { "abc", "abc", "def", StringComparison.CurrentCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.CurrentCulture, "abc" };
            yield return new object[] { "abc", "abc", "", StringComparison.CurrentCulture, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.CurrentCulture, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.CurrentCulture, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.CurrentCulture, "ac" };

            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.CurrentCulture, "def" };

            yield return new object[] { "abc", "abc", "def", StringComparison.CurrentCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.CurrentCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "abc", "", StringComparison.CurrentCultureIgnoreCase, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.CurrentCultureIgnoreCase, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.CurrentCultureIgnoreCase, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.CurrentCultureIgnoreCase, "ac" };

            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.CurrentCultureIgnoreCase, "def" };

            yield return new object[] { "abc", "abc", "def", StringComparison.Ordinal, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.Ordinal, "abc" };
            yield return new object[] { "abc", "abc", "", StringComparison.Ordinal, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.Ordinal, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.Ordinal, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.Ordinal, "ac" };

            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.Ordinal, "abc" };

            yield return new object[] { "abc", "abc", "def", StringComparison.OrdinalIgnoreCase, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.OrdinalIgnoreCase, "def" };
            yield return new object[] { "abc", "abc", "", StringComparison.OrdinalIgnoreCase, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.OrdinalIgnoreCase, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.OrdinalIgnoreCase, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.OrdinalIgnoreCase, "ac" };


            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.OrdinalIgnoreCase, "abc" };

            yield return new object[] { "abc", "abc", "def", StringComparison.InvariantCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.InvariantCulture, "abc" };
            yield return new object[] { "abc", "abc", "", StringComparison.InvariantCulture, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.InvariantCulture, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.InvariantCulture, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.InvariantCulture, "ac" };


            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.InvariantCulture, "def" };

            yield return new object[] { "abc", "abc", "def", StringComparison.InvariantCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.InvariantCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "abc", "", StringComparison.InvariantCultureIgnoreCase, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.InvariantCultureIgnoreCase, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.InvariantCultureIgnoreCase, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.InvariantCultureIgnoreCase, "ac" };


            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.InvariantCultureIgnoreCase, "def" };

                string turkishSource = "\u0069\u0130";

                yield return new object[] { turkishSource, "\u0069", "a", StringComparison.Ordinal, "a\u0130" };
                yield return new object[] { turkishSource, "\u0069", "a", StringComparison.OrdinalIgnoreCase, "a\u0130" };
                yield return new object[] { turkishSource, "\u0130", "a", StringComparison.Ordinal, "\u0069a" };
                yield return new object[] { turkishSource, "\u0130", "a", StringComparison.OrdinalIgnoreCase, "\u0069a" };

                yield return new object[] { turkishSource, "\u0069", "a", StringComparison.InvariantCulture, "a\u0130" };
                yield return new object[] { turkishSource, "\u0069", "a", StringComparison.InvariantCultureIgnoreCase, "a\u0130" };
                yield return new object[] { turkishSource, "\u0130", "a", StringComparison.InvariantCulture, "\u0069a" };
                yield return new object[] { turkishSource, "\u0130", "a", StringComparison.InvariantCultureIgnoreCase, "\u0069a" };
            }

            // To catch regressions when dealing with zero-length "this" inputs
            yield return new object[] { "", "x", "y", StringComparison.InvariantCulture, "" };
            yield return new object[] { "", "\u200d", "y", StringComparison.InvariantCulture, "" };
            yield return new object[] { "", "\0", "y", StringComparison.InvariantCulture, "" };
        }

        [Theory]
        [MemberData(nameof(Replace_StringComparison_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95503", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95473", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        public void Replace_StringComparison_ReturnsExpected(string original, string oldValue, string newValue, StringComparison comparisonType, string expected)
        {
            Assert.Equal(expected, original.Replace(oldValue, newValue, comparisonType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60568", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95503", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        public void Replace_StringComparison_TurkishI()
        {
            const string Source = "\u0069\u0130";

            using (new ThreadCultureChange("tr-TR"))
            {
                Assert.True("\u0069".Equals("\u0130", StringComparison.CurrentCultureIgnoreCase));

                Assert.Equal("a\u0130", Source.Replace("\u0069", "a", StringComparison.CurrentCulture));
                Assert.Equal("aa", Source.Replace("\u0069", "a", StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal("\u0069a", Source.Replace("\u0130", "a", StringComparison.CurrentCulture));
                Assert.Equal("aa", Source.Replace("\u0130", "a", StringComparison.CurrentCultureIgnoreCase));
            }

            using (new ThreadCultureChange("en-US"))
            {
                Assert.False("\u0069".Equals("\u0130", StringComparison.CurrentCultureIgnoreCase));

                Assert.Equal("a\u0130", Source.Replace("\u0069", "a", StringComparison.CurrentCulture));
                Assert.Equal("a\u0130", Source.Replace("\u0069", "a", StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal("\u0069a", Source.Replace("\u0130", "a", StringComparison.CurrentCulture));
                Assert.Equal("\u0069a", Source.Replace("\u0130", "a", StringComparison.CurrentCultureIgnoreCase));
            }
        }

        public static IEnumerable<object[]> Replace_StringComparisonCulture_TestData()
        {
            yield return new object[] { "abc", "abc", "def", false, null, "def" };
            yield return new object[] { "abc", "ABC", "def", false, null, "abc" };
            yield return new object[] { "abc", "abc", "def", false, CultureInfo.InvariantCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", false, CultureInfo.InvariantCulture, "abc" };

            yield return new object[] { "abc", "abc", "def", true, null, "def" };
            yield return new object[] { "abc", "ABC", "def", true, null, "def" };
            yield return new object[] { "abc", "abc", "def", true, CultureInfo.InvariantCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", true, CultureInfo.InvariantCulture, "def" };

            if (PlatformDetection.IsNotInvariantGlobalization && PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", false, null, "def" };
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", true, null, "def" };
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", false, CultureInfo.InvariantCulture, "def" };
                yield return new object[] { "abc", "abc" + SoftHyphen, "def", true, CultureInfo.InvariantCulture, "def" };

                // Android has different results w/ tr-TR
                // See https://github.com/dotnet/runtime/issues/60568
                if (!PlatformDetection.IsAndroid && !PlatformDetection.IsLinuxBionic)
                {
                    yield return new object[] { "\u0069\u0130", "\u0069", "a", false, new CultureInfo("tr-TR"), "a\u0130" };
                    yield return new object[] { "\u0069\u0130", "\u0069", "a", true, new CultureInfo("tr-TR"), "aa" };
                }

                yield return new object[] { "\u0069\u0130", "\u0069", "a", false, CultureInfo.InvariantCulture, "a\u0130" };
                yield return new object[] { "\u0069\u0130", "\u0069", "a", true, CultureInfo.InvariantCulture, "a\u0130" };
            }
        }

        [Theory]
        [MemberData(nameof(Replace_StringComparisonCulture_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95471", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95503", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        public void Replace_StringComparisonCulture_ReturnsExpected(string original, string oldValue, string newValue, bool ignoreCase, CultureInfo culture, string expected)
        {
            Assert.Equal(expected, original.Replace(oldValue, newValue, ignoreCase, culture));
            if (culture == null)
            {
                Assert.Equal(expected, original.Replace(oldValue, newValue, ignoreCase, CultureInfo.CurrentCulture));
            }
        }

        [Fact]
        public void Replace_StringComparison_NullOldValue_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentNullException>("oldValue", () => "abc".Replace(null, "def", StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentNullException>("oldValue", () => "abc".Replace(null, "def", true, CultureInfo.CurrentCulture));
        }

        [Fact]
        public void Replace_StringComparison_EmptyOldValue_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("oldValue", () => "abc".Replace("", "def", StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentException>("oldValue", () => "abc".Replace("", "def", true, CultureInfo.CurrentCulture));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public void Replace_StringComparison_WeightlessOldValue_WithOrdinalComparison_Succeeds()
        {
            Assert.Equal("abcdef", ("abc" + ZeroWidthJoiner).Replace(ZeroWidthJoiner, "def"));
            Assert.Equal("abcdef", ("abc" + ZeroWidthJoiner).Replace(ZeroWidthJoiner, "def", StringComparison.Ordinal));
            Assert.Equal("abcdef", ("abc" + ZeroWidthJoiner).Replace(ZeroWidthJoiner, "def", StringComparison.OrdinalIgnoreCase));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public void Replace_StringComparison_WeightlessOldValue_WithLinguisticComparison_TerminatesReplacement()
        {
            Assert.Equal("abc" + ZeroWidthJoiner + "def", ("abc" + ZeroWidthJoiner + "def").Replace(ZeroWidthJoiner, "xyz", StringComparison.CurrentCulture));
            Assert.Equal("abc" + ZeroWidthJoiner + "def", ("abc" + ZeroWidthJoiner + "def").Replace(ZeroWidthJoiner, "xyz", true, CultureInfo.CurrentCulture));
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture - 1)]
        [InlineData(StringComparison.OrdinalIgnoreCase + 1)]
        public void Replace_NoSuchStringComparison_ThrowsArgumentException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "abc".Replace("abc", "def", comparisonType));
        }


        private static readonly StringComparison[] StringComparisons = (StringComparison[])Enum.GetValues(typeof(StringComparison));

        [Fact]
        public static void GetHashCode_OfSpan_EmbeddedNull_ReturnsDifferentHashCodes()
        {
            Assert.NotEqual(string.GetHashCode("\0AAAAAAAAA".AsSpan()), string.GetHashCode("\0BBBBBBBBBBBB".AsSpan()));
        }

        [Fact]
        public static void GetHashCode_OfSpan_MatchesOfString()
        {
            // parameterless should be ordinal only
            Assert.Equal("abc".GetHashCode(), string.GetHashCode("abc".AsSpan()));
            Assert.NotEqual("abc".GetHashCode(), string.GetHashCode("ABC".AsSpan())); // case differences
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public static void GetHashCode_CompareInfo()
        {
            // ordinal
            Assert.Equal("abc".GetHashCode(), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("abc", CompareOptions.Ordinal));
            Assert.NotEqual("abc".GetHashCode(), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("ABC", CompareOptions.Ordinal));

            // ordinal ignore case
            Assert.Equal("abc".GetHashCode(StringComparison.OrdinalIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("abc", CompareOptions.OrdinalIgnoreCase));
            Assert.Equal("abc".GetHashCode(StringComparison.OrdinalIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("ABC", CompareOptions.OrdinalIgnoreCase));

            // culture-aware
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.CurrentCulture), CultureInfo.CurrentCulture.CompareInfo.GetHashCode("aeiXXabc", CompareOptions.None));
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.CurrentCultureIgnoreCase), CultureInfo.CurrentCulture.CompareInfo.GetHashCode("aeiXXabc", CompareOptions.IgnoreCase));

            // invariant culture
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.InvariantCulture), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("aeiXXabc", CompareOptions.None));
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.InvariantCultureIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("aeiXXabc", CompareOptions.IgnoreCase));
        }

        [Fact]
        public static void GetHashCode_CompareInfo_OfSpan()
        {
            // ordinal
            Assert.Equal("abc".GetHashCode(), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("abc".AsSpan(), CompareOptions.Ordinal));
            Assert.NotEqual("abc".GetHashCode(), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("ABC".AsSpan(), CompareOptions.Ordinal));

            // ordinal ignore case
            Assert.Equal("abc".GetHashCode(StringComparison.OrdinalIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("abc".AsSpan(), CompareOptions.OrdinalIgnoreCase));
            Assert.Equal("abc".GetHashCode(StringComparison.OrdinalIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("ABC".AsSpan(), CompareOptions.OrdinalIgnoreCase));

            // culture-aware
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.CurrentCulture), CultureInfo.CurrentCulture.CompareInfo.GetHashCode("aeiXXabc".AsSpan(), CompareOptions.None));
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.CurrentCultureIgnoreCase), CultureInfo.CurrentCulture.CompareInfo.GetHashCode("aeiXXabc".AsSpan(), CompareOptions.IgnoreCase));

            // invariant culture
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.InvariantCulture), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("aeiXXabc".AsSpan(), CompareOptions.None));
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.InvariantCultureIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("aeiXXabc".AsSpan(), CompareOptions.IgnoreCase));
        }

        public static IEnumerable<object[]> GetHashCode_StringComparison_Data => StringComparisons.Select(value => new object[] { value });

        [Theory]
        [MemberData(nameof(GetHashCode_StringComparison_Data))]
        public static void GetHashCode_StringComparison(StringComparison comparisonType)
        {
            int hashCodeFromStringComparer = StringComparer.FromComparison(comparisonType).GetHashCode("abc");
            int hashCodeFromStringGetHashCode = "abc".GetHashCode(comparisonType);
            int hashCodeFromStringGetHashCodeOfSpan = string.GetHashCode("abc".AsSpan(), comparisonType);

            Assert.Equal(hashCodeFromStringComparer, hashCodeFromStringGetHashCode);
            Assert.Equal(hashCodeFromStringComparer, hashCodeFromStringGetHashCodeOfSpan);
        }

        public static IEnumerable<object[]> GetHashCode_NoSuchStringComparison_ThrowsArgumentException_Data => new[]
        {
            new object[] { StringComparisons.Min() - 1 },
            new object[] { StringComparisons.Max() + 1 },
        };

        [Theory]
        [MemberData(nameof(GetHashCode_NoSuchStringComparison_ThrowsArgumentException_Data))]
        public static void GetHashCode_NoSuchStringComparison_ThrowsArgumentException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "abc".GetHashCode(comparisonType));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.GetHashCode("abc".AsSpan(), comparisonType));
        }

        [Theory]
        [InlineData("")] // empty string
        [InlineData("hello")] // non-empty string
        public static unsafe void GetPinnableReference_ReturnsSameAsGCHandleAndLegacyFixed(string input)
        {
            Assert.NotNull(input); // test shouldn't have null input

            // First, ensure the value pointed to by GetPinnableReference is correct.
            // It should point to the first character (or the null terminator for empty inputs).

            ref readonly char rChar = ref input.GetPinnableReference();
            Assert.Equal((input.Length > 0) ? input[0] : '\0', rChar);

            // Next, ensure that GetPinnableReference() and GCHandle.AddrOfPinnedObject agree
            // on the address being returned.

            GCHandle gcHandle = GCHandle.Alloc(input, GCHandleType.Pinned);
            try
            {
                // Unsafe.AsPointer is safe since it's pinned by the gc handle
                Assert.Equal((IntPtr)Unsafe.AsPointer(ref Unsafe.AsRef(in rChar)), gcHandle.AddrOfPinnedObject());
            }
            finally
            {
                gcHandle.Free();
            }

            // Next, ensure that GetPinnableReference matches the string projected as a ROS<char>.

            Assert.True(Unsafe.AreSame(ref Unsafe.AsRef(in rChar), ref MemoryMarshal.GetReference((ReadOnlySpan<char>)input)));

            // Finally, ensure that GetPinnableReference matches the legacy 'fixed' keyword.

            if (PlatformDetection.IsReflectionEmitSupported)
            {
                DynamicMethod dynamicMethod = new DynamicMethod("tester", typeof(bool), new[] { typeof(string) });
                ILGenerator ilGen = dynamicMethod.GetILGenerator();
                LocalBuilder pinnedLocal = ilGen.DeclareLocal(typeof(object), pinned: true);

                ilGen.Emit(OpCodes.Ldarg_0); // load 'input' and pin it
                ilGen.Emit(OpCodes.Stloc, pinnedLocal);

                ilGen.Emit(OpCodes.Ldloc, pinnedLocal); // get the address of field 0 from pinned 'input'
                ilGen.Emit(OpCodes.Conv_I);

                ilGen.Emit(OpCodes.Call, typeof(RuntimeHelpers).GetProperty("OffsetToStringData").GetMethod); // get pointer to start of string data
                ilGen.Emit(OpCodes.Add);

                ilGen.Emit(OpCodes.Ldarg_0); // get value of input.GetPinnableReference()
                ilGen.Emit(OpCodes.Callvirt, typeof(string).GetMethod("GetPinnableReference"));

                // At this point, the top of the evaluation stack is traditional (fixed char* = input) and input.GetPinnableReference().
                // Compare for equality and return.

                ilGen.Emit(OpCodes.Ceq);
                ilGen.Emit(OpCodes.Ret);

                Assert.True((bool)dynamicMethod.Invoke(null, new[] { input }));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public static unsafe void GetPinnableReference_WithNullInput_ThrowsNullRef()
        {
            // This test uses an explicit call instead of the normal callvirt that C# would emit.
            // This allows us to make sure the NullReferenceException is coming from *within*
            // the GetPinnableReference method rather than on the call site to that method.

            DynamicMethod dynamicMethod = new DynamicMethod("tester", typeof(void), Type.EmptyTypes);
            ILGenerator ilGen = dynamicMethod.GetILGenerator();

            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Call, typeof(string).GetMethod("GetPinnableReference"));
            ilGen.Emit(OpCodes.Pop);
            ilGen.Emit(OpCodes.Ret);

            Action del = (Action)dynamicMethod.CreateDelegate(typeof(Action));

            Assert.NotNull(del);
            Assert.Throws<NullReferenceException>(del);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("\0")]
        [InlineData("abc")]
        public static unsafe void ImplicitCast_ResultingSpanMatches(string s)
        {
            ReadOnlySpan<char> span = s;
            Assert.Equal(s.Length, span.Length);
            fixed (char* stringPtr = s)
            fixed (char* spanPtr = &MemoryMarshal.GetReference(span))
            {
                Assert.Equal((IntPtr)stringPtr, (IntPtr)spanPtr);
            }
        }

        [Fact]
        public static void ImplicitCast_NullString_ReturnsDefaultSpan()
        {
            ReadOnlySpan<char> span = (string)null;
            Assert.True(span == default);
        }

        [Theory]
        [InlineData("Hello", 'l', StringComparison.Ordinal, 2)]
        [InlineData("Hello", 'x', StringComparison.Ordinal, -1)]
        [InlineData("Hello", 'h', StringComparison.Ordinal, -1)]
        [InlineData("Hello", 'o', StringComparison.Ordinal, 4)]
        [InlineData("Hello", 'h', StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("HelLo", 'L', StringComparison.OrdinalIgnoreCase, 2)]
        [InlineData("HelLo", 'L', StringComparison.Ordinal, 3)]
        [InlineData("HelLo", '\0', StringComparison.Ordinal, -1)]
        [InlineData("!@#$%", '%', StringComparison.Ordinal, 4)]
        [InlineData("!@#$", '!', StringComparison.Ordinal, 0)]
        [InlineData("!@#$", '@', StringComparison.Ordinal, 1)]
        [InlineData("!@#$%", '%', StringComparison.OrdinalIgnoreCase, 4)]
        [InlineData("!@#$", '!', StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("!@#$", '@', StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("_____________\u807f", '\u007f', StringComparison.Ordinal, -1)]
        [InlineData("_____________\u807f__", '\u007f', StringComparison.Ordinal, -1)]
        [InlineData("_____________\u807f\u007f_", '\u007f', StringComparison.Ordinal, 14)]
        [InlineData("__\u807f_______________", '\u007f', StringComparison.Ordinal, -1)]
        [InlineData("__\u807f___\u007f___________", '\u007f', StringComparison.Ordinal, 6)]
        [InlineData("_____________\u807f", '\u007f', StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("_____________\u807f__", '\u007f', StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("_____________\u807f\u007f_", '\u007f', StringComparison.OrdinalIgnoreCase, 14)]
        [InlineData("__\u807f_______________", '\u007f', StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("__\u807f___\u007f___________", '\u007f', StringComparison.OrdinalIgnoreCase, 6)]
        public static void IndexOf_SingleLetter_StringComparison(string s, char target, StringComparison stringComparison, int expected)
        {
            Assert.Equal(expected, s.IndexOf(target, stringComparison));
            var charArray = new char[1];
            charArray[0] = target;
            Assert.Equal(expected, s.AsSpan().IndexOf(charArray, stringComparison));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60568", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public static void IndexOf_TurkishI_TurkishCulture_Char()
        {
            using (new ThreadCultureChange("tr-TR"))
            {
                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                char value = '\u0130';
                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(19, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(19, span.IndexOf(new char[] { value }, StringComparison.CurrentCulture));
                Assert.Equal(4, span.IndexOf(new char[] { value }, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, span.IndexOf(new char[] { value }, StringComparison.Ordinal));
                Assert.Equal(19, span.IndexOf(new char[] { value }, StringComparison.OrdinalIgnoreCase));

                value = '\u0131';
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                Assert.Equal(10, span.IndexOf(new char[] { value }, StringComparison.CurrentCulture));
                Assert.Equal(8, span.IndexOf(new char[] { value }, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, span.IndexOf(new char[] { value }, StringComparison.Ordinal));
                Assert.Equal(10, span.IndexOf(new char[] { value }, StringComparison.OrdinalIgnoreCase));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public static void IndexOf_TurkishI_InvariantCulture_Char()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                char value = '\u0130';

                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = '\u0131';
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public static void IndexOf_TurkishI_EnglishUSCulture_Char()
        {
            using (new ThreadCultureChange("en-US"))
            {
                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                char value = '\u0130';

                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = '\u0131';
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public static void IndexOf_EquivalentDiacritics_EnglishUSCulture_Char()
        {
            string s = "Exhibit a\u0300\u00C0";
            char value = '\u00C0';

            using (new ThreadCultureChange("en-US"))
            {
                Assert.Equal(10, s.IndexOf(value));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_EquivalentDiacritics_InvariantCulture_Char()
        {
            string s = "Exhibit a\u0300\u00C0";
            char value = '\u00C0';

            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                Assert.Equal(10, s.IndexOf(value));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(PlatformDetection.IsInvariantGlobalization ? 10 : 8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_CyrillicE_EnglishUSCulture_Char()
        {
            string s = "Foo\u0400Bar";
            char value = '\u0400';

            using (new ThreadCultureChange("en-US"))
            {
                Assert.Equal(3, s.IndexOf(value));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(3, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(3, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_CyrillicE_InvariantCulture_Char()
        {
            string s = "Foo\u0400Bar";
            char value = '\u0400';

            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                Assert.Equal(3, s.IndexOf(value));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [Fact]
        public static void IndexOf_Invalid_Char()
        {
            // Invalid comparison type
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf('o', StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf('o', StringComparison.OrdinalIgnoreCase + 1));
        }

        public static IEnumerable<object[]> IndexOf_String_StringComparison_TestData()
        {
            yield return new object[] { "Hello\uD801\uDC28", "\uD801\uDC4f", StringComparison.Ordinal, -1};
            yield return new object[] { "Hello\uD801\uDC28", "\uD801\uDC00", StringComparison.OrdinalIgnoreCase, 5};
            yield return new object[] { "Hello\u0200\u0202", "\u0201\u0203", StringComparison.OrdinalIgnoreCase, 5};
            yield return new object[] { "Hello\u0200\u0202", "\u0201\u0203", StringComparison.Ordinal, -1};
            yield return new object[] { "Hello\uD801\uDC00", "\uDC00", StringComparison.Ordinal, 6};
            yield return new object[] { "Hello\uD801\uDC00", "\uDC00", StringComparison.OrdinalIgnoreCase, 6};
            yield return new object[] { "Hello\uD801\uDC00", "\uD801", StringComparison.OrdinalIgnoreCase, 5};
            yield return new object[] { "Hello\uD801\uDC00", "\uD801\uDC00", StringComparison.Ordinal, 5};
        }


        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(IndexOf_String_StringComparison_TestData))]
        public static void IndexOf_Ordinal_Misc(string source, string target, StringComparison stringComparison, int expected)
        {
            Assert.Equal(expected, source.IndexOf(target, stringComparison));
        }

        public static IEnumerable<object[]> LastIndexOf_String_StringComparison_TestData()
        {
            yield return new object[] { "\uD801\uDC28Hello", "\uD801\uDC4f", 6, StringComparison.Ordinal, -1};
            yield return new object[] { "\uD801\uDC28Hello", "\uD801\uDC00", 6, StringComparison.OrdinalIgnoreCase, 0};
            yield return new object[] { "\uD801\uDC28Hello\uD801\uDC28", "\uD801\uDC00", 1, StringComparison.OrdinalIgnoreCase, 0};
            yield return new object[] { "\u0200\u0202Hello", "\u0201\u0203", 6, StringComparison.OrdinalIgnoreCase, 0};
            yield return new object[] { "\u0200\u0202Hello\u0200\u0202", "\u0201\u0203", 1, StringComparison.OrdinalIgnoreCase, 0};
            yield return new object[] { "\u0200\u0202Hello", "\u0201\u0203", 6, StringComparison.Ordinal, -1};
            yield return new object[] { "\uD801\uDC00Hello", "\uDC00", 6, StringComparison.Ordinal, 1};
            yield return new object[] { "\uD801\uDC00Hello\uDC00", "\uDC00", 3, StringComparison.Ordinal, 1};
            yield return new object[] { "\uD801\uDC00Hello", "\uDC00", 6, StringComparison.OrdinalIgnoreCase, 1};
            yield return new object[] { "\uD801\uDC00Hello\uDC00", "\uDC00", 4, StringComparison.OrdinalIgnoreCase, 1};
            yield return new object[] { "\uD801\uDC00Hello", "\uD801", 6, StringComparison.OrdinalIgnoreCase, 0};
            yield return new object[] { "\uD801\uD801Hello", "\uD801", 0, StringComparison.OrdinalIgnoreCase, 0};
            yield return new object[] { "\uD801\uDC00Hello", "\uD801\uDC00", 6, StringComparison.Ordinal, 0};
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(LastIndexOf_String_StringComparison_TestData))]
        public static void LastIndexOf_Ordinal_Misc(string source, string target, int startIndex, StringComparison stringComparison, int expected)
        {
            Assert.Equal(expected, source.LastIndexOf(target, startIndex, stringComparison));
        }

        public static IEnumerable<object[]>Ordinal_String_StringComparison_TestData()
        {
            yield return new object[] { "\u0200\u0202", "\u0201\u0203", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "\uD801\uDC28", "\uD801\uDC00", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "\u0200\u0202", "\u0201\u0203", StringComparison.Ordinal, false};
            yield return new object[] { "\uD801\uDC28", "\uD801\uDC00", StringComparison.Ordinal, false};
            yield return new object[] { "\uD801\uD801\uDC28", "\uD801\uD801\uDC00", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "\uD801\uD801\uDC28", "\uD801\uD801\uDC00", StringComparison.Ordinal, false};
            yield return new object[] { "\u0200\u0202", "\u0200\u0202", StringComparison.Ordinal, true};
            yield return new object[] { "\u0200\u0202", "\u0200\u0202", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "\u0200\u0202", "\u0200\u0202A", StringComparison.Ordinal, false};
            yield return new object[] { "\u0200\u0202", "\u0200\u0202A", StringComparison.OrdinalIgnoreCase, false};
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(Ordinal_String_StringComparison_TestData))]
        public static void Compare_Ordinal_Misc(string source, string target, StringComparison stringComparison, bool expected)
        {
            Assert.Equal(expected, string.Compare(source, target, stringComparison) == 0);
            Assert.Equal(expected, string.GetHashCode(source, stringComparison) == string.GetHashCode(target, stringComparison));
        }

        public static IEnumerable<object[]>StartsWith_String_StringComparison_TestData()
        {
            yield return new object[] { "\u0200\u0202ABC", "\u0201\u0203", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "\uD801\uDC28ABC", "\uD801\uDC00", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "\u0200\u0202AB", "\u0201\u0203", StringComparison.Ordinal, false};
            yield return new object[] { "\uD801\uDC28AB", "\uD801\uDC00", StringComparison.Ordinal, false};
            yield return new object[] { "\uD801\uD801\uDC28AAA", "\uD801\uD801\uDC00", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "\uD801\uD801\uDC28AAA", "\uD801\uD801\uDC00", StringComparison.Ordinal, false};
            yield return new object[] { "\u0200\u0202AAA", "\u0200\u0202", StringComparison.Ordinal, true};
            yield return new object[] { "\u0200\u0202AAA", "\u0200\u0202", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "\u0200\u0202AAA", "\u0200\u0202A", StringComparison.Ordinal, true};
            yield return new object[] { "\u0200\u0202AAA", "\u0200\u0202A", StringComparison.OrdinalIgnoreCase, true};
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(StartsWith_String_StringComparison_TestData))]
        public static void StartsWith_Ordinal_Misc(string source, string target, StringComparison stringComparison, bool expected)
        {
            Assert.Equal(expected, source.StartsWith(target, stringComparison));
        }

        public static IEnumerable<object[]>EndsWith_String_StringComparison_TestData()
        {
            yield return new object[] { "ABC\u0200\u0202", "\u0201\u0203", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "ABC\uD801\uDC28", "\uD801\uDC00", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "AB\u0200\u0202", "\u0201\u0203", StringComparison.Ordinal, false};
            yield return new object[] { "AB\uD801\uDC28", "\uD801\uDC00", StringComparison.Ordinal, false};
            yield return new object[] { "AAA\uD801\uD801\uDC28", "\uD801\uD801\uDC00", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "AAA\uD801\uD801\uDC28", "\uD801\uD801\uDC00", StringComparison.Ordinal, false};
            yield return new object[] { "AAA\u0200\u0202", "\u0200\u0202", StringComparison.Ordinal, true};
            yield return new object[] { "AAA\u0200\u0202", "\u0200\u0202", StringComparison.OrdinalIgnoreCase, true};
            yield return new object[] { "AAA\u0200\u0202A", "\u0200\u0202A", StringComparison.Ordinal, true};
            yield return new object[] { "AAA\u0200\u0202A", "\u0200\u0202A", StringComparison.OrdinalIgnoreCase, true};
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(EndsWith_String_StringComparison_TestData))]
        public static void EndsWith_Ordinal_Misc(string source, string target, StringComparison stringComparison, bool expected)
        {
            Assert.Equal(expected, source.EndsWith(target, stringComparison));
        }

        [Theory]
        [MemberData(nameof(Concat_Strings_2_3_4_TestData))]
        public static void Concat_Spans(string[] values, string expected)
        {
            Assert.InRange(values.Length, 2, 4);

            string result =
                values.Length == 2 ? string.Concat(values[0].AsSpan(), values[1].AsSpan()) :
                values.Length == 3 ? string.Concat(values[0].AsSpan(), values[1].AsSpan(), values[2].AsSpan()) :
                string.Concat(values[0].AsSpan(), values[1].AsSpan(), values[2].AsSpan(), values[3].AsSpan());

            if (result.Length == 0)
            {
                Assert.Same(string.Empty, result);
            }

            Assert.Equal(expected, result);
        }

        [Fact]
        public static void IndexerUsingIndexTest()
        {
            Index index;
            string s = "0123456789ABCDEF";

            for (int i = 0; i < s.Length; i++)
            {
                index = Index.FromStart(i);
                Assert.Equal(s[i], s[index]);

                index = Index.FromEnd(i + 1);
                Assert.Equal(s[s.Length - i - 1], s[index]);
            }

            index = Index.FromStart(s.Length + 1);
            char c;
            Assert.Throws<IndexOutOfRangeException>(() => c = s[index]);

            index = Index.FromEnd(s.Length + 1);
            Assert.Throws<IndexOutOfRangeException>(() => c = s[index]);
        }

        [Fact]
        public static void IndexerUsingRangeTest()
        {
            Range range;
            string s = "0123456789ABCDEF";

            for (int i = 0; i < s.Length; i++)
            {
                range = new Range(Index.FromStart(0), Index.FromStart(i));
                Assert.Equal(s.Substring(0, i), s[range]);

                range = new Range(Index.FromEnd(s.Length), Index.FromEnd(i));
                Assert.Equal(s.Substring(0, s.Length - i), s[range]);
            }

            range = new Range(Index.FromStart(s.Length - 2), Index.FromStart(s.Length + 1));
            string s1;
            Assert.Throws<ArgumentOutOfRangeException>(() => s1 = s[range]);

            range = new Range(Index.FromEnd(s.Length + 1), Index.FromEnd(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s1 = s[range]);
        }

        [Fact]
        public static void SubstringUsingIndexTest()
        {
            string s = "0123456789ABCDEF";

            for (int i = 0; i < s.Length; i++)
            {
                Assert.Equal(s.Substring(i), s[i..]);
                Assert.Equal(s.Substring(s.Length - i - 1), s[^(i + 1)..]);
            }

            // String.Substring allows the string length as a valid input.
            Assert.Equal(s.Substring(s.Length), s[s.Length..]);

            Assert.Throws<ArgumentOutOfRangeException>(() => s[(s.Length + 1)..]);
            Assert.Throws<ArgumentOutOfRangeException>(() => s[^(s.Length + 1)..]);
        }

        [Fact]
        public static void SubstringUsingRangeTest()
        {
            string s = "0123456789ABCDEF";
            Range range;

            for (int i = 0; i < s.Length; i++)
            {
                range = new Range(Index.FromStart(0), Index.FromStart(i));
                Assert.Equal(s.Substring(0, i), s[range]);

                range = new Range(Index.FromEnd(s.Length), Index.FromEnd(i));
                Assert.Equal(s.Substring(0, s.Length - i), s[range]);
            }

            range = new Range(Index.FromStart(s.Length - 2), Index.FromStart(s.Length + 1));
            string s1;
            Assert.Throws<ArgumentOutOfRangeException>(() => s1 = s[range]);

            range = new Range(Index.FromEnd(s.Length + 1), Index.FromEnd(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s1 = s[range]);
        }

        /// <summary>
        /// Returns true only if U+0020 SPACE is represented as the single byte 0x20 in the active code page.
        /// </summary>
        public static unsafe bool IsSimpleActiveCodePage
        {
            get
            {
                IntPtr pAnsiStr = IntPtr.Zero;
                try
                {
                    pAnsiStr = Marshal.StringToHGlobalAnsi(" ");
                    return ((byte*)pAnsiStr)[0] == (byte)' ' && ((byte*)pAnsiStr)[1] == (byte)'\0';
                }
                finally
                {
                    if (pAnsiStr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pAnsiStr);
                    }
                }
            }
        }

        [Theory]
        [InlineData("  Hello  ", "Hello")]
        [InlineData("      \t      ", "")]
        [InlineData("", "")]
        [InlineData("      ", "")]
        public static void Trim_Memory(string s, string expected)
        {
            Assert.Equal(expected, s.AsSpan().Trim().ToString()); // ReadOnlySpan
            Assert.Equal(expected, new Span<char>(s.ToCharArray()).Trim().ToString());
            Assert.Equal(expected, new Memory<char>(s.ToCharArray()).Trim().ToString());
            Assert.Equal(expected, s.AsMemory().Trim().ToString()); // ReadOnlyMemory
        }

        [Theory]
        [InlineData("  Hello  ", "  Hello")]
        [InlineData("      \t      ", "")]
        [InlineData("", "")]
        [InlineData("      ", "")]
        public static void TrimEnd_Memory(string s, string expected)
        {
            Assert.Equal(expected, s.AsSpan().TrimEnd().ToString()); // ReadOnlySpan
            Assert.Equal(expected, new Span<char>(s.ToCharArray()).TrimEnd().ToString());
            Assert.Equal(expected, new Memory<char>(s.ToCharArray()).TrimEnd().ToString());
            Assert.Equal(expected, s.AsMemory().TrimEnd().ToString()); // ReadOnlyMemory
        }

        [Theory]
        [InlineData("  Hello  ", "Hello  ")]
        [InlineData("      \t      ", "")]
        [InlineData("", "")]
        [InlineData("      ", "")]
        public static void TrimStart_Memory(string s, string expected)
        {
            Assert.Equal(expected, s.AsSpan().TrimStart().ToString()); // ReadOnlySpan
            Assert.Equal(expected, new Span<char>(s.ToCharArray()).TrimStart().ToString());
            Assert.Equal(expected, new Memory<char>(s.ToCharArray()).TrimStart().ToString());
            Assert.Equal(expected, s.AsMemory().TrimStart().ToString()); // ReadOnlyMemory
        }

        [Fact]
        public static void ZeroLengthTrim_Memory()
        {
            string s1 = string.Empty;

            ReadOnlySpan<char> ros = s1.AsSpan();
            Assert.True(ros.SequenceEqual(ros.Trim()));
            Assert.True(ros.SequenceEqual(ros.TrimStart()));
            Assert.True(ros.SequenceEqual(ros.TrimEnd()));

            Span<char> span = new Span<char>(s1.ToCharArray());
            Assert.True(span.SequenceEqual(span.Trim()));
            Assert.True(span.SequenceEqual(span.TrimStart()));
            Assert.True(span.SequenceEqual(span.TrimEnd()));

            Memory<char> mem = new Memory<char>(s1.ToCharArray());
            Assert.True(mem.Span.SequenceEqual(mem.Trim().Span));
            Assert.True(mem.Span.SequenceEqual(mem.TrimStart().Span));
            Assert.True(mem.Span.SequenceEqual(mem.TrimEnd().Span));

            ReadOnlyMemory<char> rom = new ReadOnlyMemory<char>(s1.ToCharArray());
            Assert.True(rom.Span.SequenceEqual(rom.Trim().Span));
            Assert.True(rom.Span.SequenceEqual(rom.TrimStart().Span));
            Assert.True(rom.Span.SequenceEqual(rom.TrimEnd().Span));
        }

        [Fact]
        public static void NoWhiteSpaceTrim_Memory()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                string s1 = new string(a);

                ReadOnlySpan<char> ros = s1.AsSpan();
                Assert.True(ros.SequenceEqual(ros.Trim()));
                Assert.True(ros.SequenceEqual(ros.TrimStart()));
                Assert.True(ros.SequenceEqual(ros.TrimEnd()));

                Span<char> span = new Span<char>(a);
                Assert.True(span.SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));

                Memory<char> mem = new Memory<char>(a);
                Assert.True(mem.Span.SequenceEqual(mem.Trim().Span));
                Assert.True(mem.Span.SequenceEqual(mem.TrimStart().Span));
                Assert.True(mem.Span.SequenceEqual(mem.TrimEnd().Span));

                ReadOnlyMemory<char> rom = new ReadOnlyMemory<char>(a);
                Assert.True(rom.Span.SequenceEqual(rom.Trim().Span));
                Assert.True(rom.Span.SequenceEqual(rom.TrimStart().Span));
                Assert.True(rom.Span.SequenceEqual(rom.TrimEnd().Span));
            }
        }

        [Fact]
        public static void OnlyWhiteSpaceTrim_Memory()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = ' ';
                }
                string s1 = new string(a);

                ReadOnlySpan<char> ros = new ReadOnlySpan<char>(a);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(ros.Trim()));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(ros.TrimStart()));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(ros.TrimEnd()));

                Span<char> span = new Span<char>(a);
                Assert.True(Span<char>.Empty.SequenceEqual(span.Trim()));
                Assert.True(Span<char>.Empty.SequenceEqual(span.TrimStart()));
                Assert.True(Span<char>.Empty.SequenceEqual(span.TrimEnd()));

                Memory<char> mem = new Memory<char>(a);
                Assert.True(Memory<char>.Empty.Span.SequenceEqual(mem.Trim().Span));
                Assert.True(Memory<char>.Empty.Span.SequenceEqual(mem.TrimStart().Span));
                Assert.True(Memory<char>.Empty.Span.SequenceEqual(mem.TrimEnd().Span));

                ReadOnlyMemory<char> rom = new ReadOnlyMemory<char>(a);
                Assert.True(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(rom.Trim().Span));
                Assert.True(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(rom.TrimStart().Span));
                Assert.True(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(rom.TrimEnd().Span));
            }
        }

        [Fact]
        public static void WhiteSpaceAtStartTrim_Memory()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[0] = ' ';
                string s1 = new string(a);

                ReadOnlySpan<char> ros = s1.AsSpan();
                Assert.True(ros.Slice(1).SequenceEqual(ros.Trim()));
                Assert.True(ros.Slice(1).SequenceEqual(ros.TrimStart()));
                Assert.True(ros.SequenceEqual(ros.TrimEnd()));

                Span<char> span = new Span<char>(a);
                Assert.True(span.Slice(1).SequenceEqual(span.Trim()));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));

                Memory<char> mem = new Memory<char>(a);
                Assert.True(mem.Slice(1).Span.SequenceEqual(mem.Trim().Span));
                Assert.True(mem.Slice(1).Span.SequenceEqual(mem.TrimStart().Span));
                Assert.True(mem.Span.SequenceEqual(mem.TrimEnd().Span));

                ReadOnlyMemory<char> rom = new ReadOnlyMemory<char>(a);
                Assert.True(rom.Slice(1).Span.SequenceEqual(rom.Trim().Span));
                Assert.True(rom.Slice(1).Span.SequenceEqual(rom.TrimStart().Span));
                Assert.True(rom.Span.SequenceEqual(rom.TrimEnd().Span));
            }
        }

        [Fact]
        public static void WhiteSpaceAtEndTrim_Memory()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[length - 1] = ' ';
                string s1 = new string(a);

                ReadOnlySpan<char> ros = s1.AsSpan();
                Assert.True(ros.Slice(0, length - 1).SequenceEqual(ros.Trim()));
                Assert.True(ros.SequenceEqual(ros.TrimStart()));
                Assert.True(ros.Slice(0, length - 1).SequenceEqual(ros.TrimEnd()));

                Span<char> span = new Span<char>(a);
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd()));

                Memory<char> mem = new Memory<char>(a);
                Assert.True(mem.Slice(0, length - 1).Span.SequenceEqual(mem.Trim().Span));
                Assert.True(mem.Span.SequenceEqual(mem.TrimStart().Span));
                Assert.True(mem.Slice(0, length - 1).Span.SequenceEqual(mem.TrimEnd().Span));

                ReadOnlyMemory<char> rom = new ReadOnlyMemory<char>(a);
                Assert.True(rom.Slice(0, length - 1).Span.SequenceEqual(rom.Trim().Span));
                Assert.True(rom.Span.SequenceEqual(rom.TrimStart().Span));
                Assert.True(rom.Slice(0, length - 1).Span.SequenceEqual(rom.TrimEnd().Span));
            }
        }

        [Fact]
        public static void WhiteSpaceAtStartAndEndTrim_Memory()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[0] = ' ';
                a[length - 1] = ' ';
                string s1 = new string(a);

                ReadOnlySpan<char> ros = s1.AsSpan();
                Assert.True(ros.Slice(1, length - 2).SequenceEqual(ros.Trim()));
                Assert.True(ros.Slice(1).SequenceEqual(ros.TrimStart()));
                Assert.True(ros.Slice(0, length - 1).SequenceEqual(ros.TrimEnd()));

                Span<char> span = new Span<char>(a);
                Assert.True(span.Slice(1, length - 2).SequenceEqual(span.Trim()));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart()));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd()));

                Memory<char> mem = new Memory<char>(a);
                Assert.True(mem.Slice(1, length - 2).Span.SequenceEqual(mem.Trim().Span));
                Assert.True(mem.Slice(1).Span.SequenceEqual(mem.TrimStart().Span));
                Assert.True(mem.Slice(0, length - 1).Span.SequenceEqual(mem.TrimEnd().Span));

                ReadOnlyMemory<char> rom = new ReadOnlyMemory<char>(a);
                Assert.True(rom.Slice(1, length - 2).Span.SequenceEqual(rom.Trim().Span));
                Assert.True(rom.Slice(1).Span.SequenceEqual(rom.TrimStart().Span));
                Assert.True(rom.Slice(0, length - 1).Span.SequenceEqual(rom.TrimEnd().Span));
            }
        }

        [Fact]
        public static void WhiteSpaceInMiddleTrim_Memory()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[1] = ' ';
                string s1 = new string(a);

                ReadOnlySpan<char> ros = s1.AsSpan();
                Assert.True(ros.SequenceEqual(ros.Trim()));
                Assert.True(ros.SequenceEqual(ros.TrimStart()));
                Assert.True(ros.SequenceEqual(ros.TrimEnd()));

                Span<char> span = new Span<char>(a);
                Assert.True(span.SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));

                Memory<char> mem = new Memory<char>(a);
                Assert.True(mem.Span.SequenceEqual(mem.Trim().Span));
                Assert.True(mem.Span.SequenceEqual(mem.TrimStart().Span));
                Assert.True(mem.Span.SequenceEqual(mem.TrimEnd().Span));

                ReadOnlyMemory<char> rom = new ReadOnlyMemory<char>(a);
                Assert.True(rom.Span.SequenceEqual(rom.Trim().Span));
                Assert.True(rom.Span.SequenceEqual(rom.TrimStart().Span));
                Assert.True(rom.Span.SequenceEqual(rom.TrimEnd().Span));
            }
        }

        [Fact]
        public static void TrimWhiteSpaceMultipleTimes_Memory()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[0] = ' ';
                a[length - 1] = ' ';
                string s1 = new string(a);

                // ReadOnlySpan
                {
                    ReadOnlySpan<char> ros = s1.AsSpan();
                    ReadOnlySpan<char> trimResult = ros.Trim();
                    ReadOnlySpan<char> trimStartResult = ros.TrimStart();
                    ReadOnlySpan<char> trimEndResult = ros.TrimEnd();
                    Assert.True(ros.Slice(1, length - 2).SequenceEqual(trimResult));
                    Assert.True(ros.Slice(1).SequenceEqual(trimStartResult));
                    Assert.True(ros.Slice(0, length - 1).SequenceEqual(trimEndResult));

                    // 2nd attempt should do nothing
                    Assert.True(trimResult.SequenceEqual(trimResult.Trim()));
                    Assert.True(trimStartResult.SequenceEqual(trimStartResult.TrimStart()));
                    Assert.True(trimEndResult.SequenceEqual(trimEndResult.TrimEnd()));
                }

                // Span
                {
                    Span<char> span = new Span<char>(a);
                    Span<char> trimResult = span.Trim();
                    Span<char> trimStartResult = span.TrimStart();
                    Span<char> trimEndResult = span.TrimEnd();
                    Assert.True(span.Slice(1, length - 2).SequenceEqual(trimResult));
                    Assert.True(span.Slice(1).SequenceEqual(trimStartResult));
                    Assert.True(span.Slice(0, length - 1).SequenceEqual(trimEndResult));

                    // 2nd attempt should do nothing
                    Assert.True(trimResult.SequenceEqual(trimResult.Trim()));
                    Assert.True(trimStartResult.SequenceEqual(trimStartResult.TrimStart()));
                    Assert.True(trimEndResult.SequenceEqual(trimEndResult.TrimEnd()));
                }

                // Memory
                {
                    Memory<char> mem = new Memory<char>(a);
                    Memory<char> trimResult = mem.Trim();
                    Memory<char> trimStartResult = mem.TrimStart();
                    Memory<char> trimEndResult = mem.TrimEnd();
                    Assert.True(mem.Slice(1, length - 2).Span.SequenceEqual(trimResult.Span));
                    Assert.True(mem.Slice(1).Span.SequenceEqual(trimStartResult.Span));
                    Assert.True(mem.Slice(0, length - 1).Span.SequenceEqual(trimEndResult.Span));

                    // 2nd attempt should do nothing
                    Assert.True(trimResult.Span.SequenceEqual(trimResult.Trim().Span));
                    Assert.True(trimStartResult.Span.SequenceEqual(trimStartResult.TrimStart().Span));
                    Assert.True(trimEndResult.Span.SequenceEqual(trimEndResult.TrimEnd().Span));
                }

                // ReadOnlyMemory
                {
                    ReadOnlyMemory<char> rom = new ReadOnlyMemory<char>(a);
                    ReadOnlyMemory<char> trimResult = rom.Trim();
                    ReadOnlyMemory<char> trimStartResult = rom.TrimStart();
                    ReadOnlyMemory<char> trimEndResult = rom.TrimEnd();
                    Assert.True(rom.Slice(1, length - 2).Span.SequenceEqual(trimResult.Span));
                    Assert.True(rom.Slice(1).Span.SequenceEqual(trimStartResult.Span));
                    Assert.True(rom.Slice(0, length - 1).Span.SequenceEqual(trimEndResult.Span));

                    // 2nd attempt should do nothing
                    Assert.True(trimResult.Span.SequenceEqual(trimResult.Trim().Span));
                    Assert.True(trimStartResult.Span.SequenceEqual(trimStartResult.TrimStart().Span));
                    Assert.True(trimEndResult.Span.SequenceEqual(trimEndResult.TrimEnd().Span));
                }
            }
        }

        [Fact]
        public static void MakeSureNoTrimChecksGoOutOfRange_Memory()
        {
            for (int length = 3; length < 64; length++)
            {
                char[] first = new char[length];
                first[0] = ' ';
                first[length - 1] = ' ';
                string s1 = new string(first, 1, length - 2);

                ReadOnlySpan<char> ros = s1.AsSpan();
                Assert.True(ros.SequenceEqual(ros.Trim()));
                Assert.True(ros.SequenceEqual(ros.TrimStart()));
                Assert.True(ros.SequenceEqual(ros.TrimEnd()));

                Span<char> span = new Span<char>(s1.ToCharArray());
                Assert.True(span.SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));

                Memory<char> mem = new Memory<char>(s1.ToCharArray());
                Assert.True(mem.Span.SequenceEqual(mem.Trim().Span));
                Assert.True(mem.Span.SequenceEqual(mem.TrimStart().Span));
                Assert.True(mem.Span.SequenceEqual(mem.TrimEnd().Span));

                ReadOnlyMemory<char> rom = new ReadOnlyMemory<char>(s1.ToCharArray());
                Assert.True(rom.Span.SequenceEqual(rom.Trim().Span));
                Assert.True(rom.Span.SequenceEqual(rom.TrimStart().Span));
                Assert.True(rom.Span.SequenceEqual(rom.TrimEnd().Span));
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(CompareOptions.None)]
        [InlineData(CompareOptions.IgnoreCase)]
        [InlineData(CompareOptions.IgnoreNonSpace)]
        [InlineData(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace)]
        public void TestStringSearchCacheSynchronization(CompareOptions options)
        {
            int parallelism = Environment.ProcessorCount / 2;
            if (Environment.ProcessorCount == 0) // 1 processor case
            {
                return;
            }

            string source = "Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh " +
                            "Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh " +
                            "Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh " +
                            "\u0441\u0435\u043D\u0442\u044F\u0431\u0440\u044F Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh " +
                            "Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh " +
                            "Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh Abcdefgh ";

            string source1 = "\u0441\u0435\u043D\u0442\u044F\u0431\u0440\u044F Abcdefgh \u0441\u0435\u043D\u0442\u044F\u0431\u0440\u044F ";

            string pattern = "\u0441\u0435\u043D\u0442\u044F\u0431\u0440\u044F ";
            string pattern1 = "\u0441\u0435\u043D\u0442\u044F\u0431\u0440\u044Fnone";

            CompareInfo ci = CultureInfo.CurrentCulture.CompareInfo;

            Task [] tasks = new Task[parallelism];
            for (int i = 0; i < parallelism; i++)
            {
                tasks[i] = new Task(() =>
                {
                    for (int i = 0; i < 1_00_000; i++)
                    {
                        Assert.True(ci.IndexOf(source, pattern, options) > 0, "ci.IndexOf 1");
                        Assert.True(ci.LastIndexOf(source, pattern, options) > 0, "LastIndexOf 1");

                        Assert.False(ci.IndexOf(source, pattern1, options) > 0, "IndexOf 2");
                        Assert.False(ci.LastIndexOf(source, pattern1, options) > 0, "LastIndexOf 2");

                        Assert.True(ci.IsPrefix(source1, pattern, options), "IsPrefix 1");
                        Assert.True(ci.IsSuffix(source1, pattern, options), "IsSuffix 1");

                        Assert.False(ci.IsPrefix(source, pattern, options), "IsPrefix 2");
                        Assert.False(ci.IsSuffix(source, pattern, options), "IsPrefix 2");
                    }
                });
            }

            for (int i = 0; i < parallelism; i++)
            {
                tasks[i].Start();
            }

            Task.WaitAll(tasks);
        }

        [Fact]
        public static void EqualityTests_AsciiOptimizations()
        {
            for (int i = 0; i < 128; i++)
            {
                for (int j = 0; j < 128; j++)
                {
                    for (int len = 0; len < 8; len++)
                    {
                        bool expectedEqualOrdinal = i == j;
                        bool expectedEqualOrdinalIgnoreCase = (i == j) || ((i | 0x20) >= 'a' && (i | 0x20) <= 'z' && ((i | 0x20) == (j | 0x20)));

                        // optimization might vary based on string length, so we use 'len' to vary the string length
                        // in order to hit as many code paths as possible
                        string prefix = new string('a', len);
                        string suffix = new string('b', len);
                        string s1 = prefix + (char)i + suffix;
                        string s2 = prefix + (char)j + suffix;

                        bool actualEqualOrdinal = string.Equals(s1, s2, StringComparison.Ordinal);
                        bool actualEqualOrdinalIgnoreCase = string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);

                        int actualCompareToOrdinal = string.Compare(s1, s2, StringComparison.Ordinal);
                        int actualCompareToOrdinalIgnoreCase = string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);

                        try
                        {
                            Assert.Equal(expectedEqualOrdinal, actualEqualOrdinal);
                            Assert.Equal(expectedEqualOrdinal, actualCompareToOrdinal == 0);
                            Assert.Equal(expectedEqualOrdinalIgnoreCase, actualEqualOrdinalIgnoreCase);
                            Assert.Equal(expectedEqualOrdinalIgnoreCase, actualCompareToOrdinalIgnoreCase == 0);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Chars U+{i:X4} ('{(char)i}') and U+{j:X4} ('{(char)j}') did not compare as expected. Iteration: len = {len}.", ex);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("a", "A", 0)]
        [InlineData("A", "a", 0)]
        [InlineData("Ab", "aB", 0)]
        [InlineData("aB", "Ab", 0)]
        [InlineData("aB", "Aa", 1)]
        [InlineData("aa", "aB", -1)]
        [InlineData("\u0160a", "\u0160A", 0)]
        [InlineData("\u0160a", "\u0160B", -1)]
        [InlineData("\u0160b", "\u0160A", 1)]
        [InlineData("\u0160b\u0160\u0160\u0160", "\u0160A\u0160\u0160\u0160", 1)]
        [InlineData("\u0160A\u0160\u0160\u0160", "\u0160b\u0160\u0160\u0160", -1)]
        public static void TestCompareOrdinalIgnoreCase(string a, string b, int sign)
        {
            Assert.Equal(sign, Math.Sign(string.Compare(a, b, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
