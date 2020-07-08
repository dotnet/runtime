// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Tests;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public unsafe partial class Utf8StringTests
    {
        private delegate Utf8String.SplitResult Utf8StringSplitDelegate(Utf8String ustr, Utf8StringSplitOptions splitOptions);

        [Fact]
        public static void Split_Utf8StringSeparator_WithNullOrEmptySeparator_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(() => { u8("Hello").Split((Utf8String)null); });
            Assert.Equal("separator", ex.ParamName);

            // Shouldn't be able to split on an empty Utf8String.
            // Such an enumerator would iterate forever, so we forbid it.

            ex = Assert.Throws<ArgumentException>(() => { u8("Hello").Split(Utf8String.Empty); });
            Assert.Equal("separator", ex.ParamName);
        }

        [Fact]
        public static void Split_InvalidChar_Throws()
        {
            // Shouldn't be able to split on a standalone surrogate char
            // Other search methods (TryFind) return false when given a standalone surrogate char as input,
            // but the Split methods returns a complex data structure instead of a simple bool. So to keep
            // the logic of that data structure relatively simple we'll forbid the bad char at the call site.

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { u8("Hello").Split('\ud800'); });
            Assert.Equal("separator", ex.ParamName);
        }

        public static IEnumerable<object[]> SplitData_CharSeparator => Utf8SpanTests.SplitData_CharSeparator();

        [Theory]
        [MemberData(nameof(SplitData_CharSeparator))]
        public static void Split_Char(Utf8String source, char separator, Range[] expectedRanges)
        {
            SplitTest_Common(source ?? Utf8String.Empty, (ustr, splitOptions) => ustr.Split(separator, splitOptions), expectedRanges);
        }

        [Fact]
        public static void Split_Deconstruct()
        {
            Utf8String ustr = u8("a,b,c,d,e");

            {
                (Utf8String a, Utf8String b) = ustr.Split('x'); // not found
                Assert.Same(ustr, a); // Expected referential equality of input
                Assert.Null(b);
            }

            {
                (Utf8String a, Utf8String b) = ustr.Split(',');
                Assert.Equal(u8("a"), a);
                Assert.Equal(u8("b,c,d,e"), b);
            }

            {
                (Utf8String a, Utf8String b, Utf8String c, Utf8String d, Utf8String e) = ustr.Split(',');
                Assert.Equal(u8("a"), a);
                Assert.Equal(u8("b"), b);
                Assert.Equal(u8("c"), c);
                Assert.Equal(u8("d"), d);
                Assert.Equal(u8("e"), e);
            }

            {
                (Utf8String a, Utf8String b, Utf8String c, Utf8String d, Utf8String e, Utf8String f, Utf8String g, Utf8String h) = ustr.Split(',');
                Assert.Equal(u8("a"), a);
                Assert.Equal(u8("b"), b);
                Assert.Equal(u8("c"), c);
                Assert.Equal(u8("d"), d);
                Assert.Equal(u8("e"), e);
                Assert.Null(f);
                Assert.Null(g);
                Assert.Null(h);
            }
        }

        [Fact]
        public static void Split_Deconstruct_WithOptions()
        {
            Utf8String ustr = u8("a, , b, c,, d, e");

            // Note referential equality checks below (since we want to know exact slices
            // into the original buffer), not deep (textual) equality checks.

            {
                (Utf8String a, Utf8String b) = ustr.Split(',', Utf8StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(u8("a"), a);
                Assert.Equal(u8(" , b, c,, d, e"), b);
            }

            {
                (Utf8String a, Utf8String x, Utf8String b, Utf8String c, Utf8String d, Utf8String e) = ustr.Split(',', Utf8StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(u8("a"), a); // "a"
                Assert.Equal(u8(" "), x); // " "
                Assert.Equal(u8(" b"), b); // " b"
                Assert.Equal(u8(" c"), c); // " c"
                Assert.Equal(u8(" d"), d); // " d"
                Assert.Equal(u8(" e"), e); // " e"
            }

            {
                (Utf8String a, Utf8String b, Utf8String c, Utf8String d, Utf8String e, Utf8String f, Utf8String g, Utf8String h) = ustr.Split(',', Utf8StringSplitOptions.RemoveEmptyEntries | Utf8StringSplitOptions.TrimEntries);
                Assert.Equal(u8("a"), a);
                Assert.Equal(u8("b"), b);
                Assert.Equal(u8("c"), c);
                Assert.Equal(u8("d"), d);
                Assert.Equal(u8("e"), e);
                Assert.Null(f);
                Assert.Null(g);
                Assert.Null(h);
            }
        }

        public static IEnumerable<object[]> SplitData_RuneSeparator() => Utf8SpanTests.SplitData_RuneSeparator();

        [Theory]
        [MemberData(nameof(SplitData_RuneSeparator))]
        public static void Split_Rune(Utf8String source, Rune separator, Range[] expectedRanges)
        {
            SplitTest_Common(source ?? Utf8String.Empty, (ustr, splitOptions) => ustr.Split(separator, splitOptions), expectedRanges);
        }

        public static IEnumerable<object[]> SplitData_Utf8StringSeparator() => Utf8SpanTests.SplitData_Utf8SpanSeparator();

        [Theory]
        [MemberData(nameof(SplitData_Utf8StringSeparator))]
        public static void Split_Utf8String(Utf8String source, Utf8String separator, Range[] expectedRanges)
        {
            SplitTest_Common(source ?? Utf8String.Empty, (ustr, splitOptions) => ustr.Split(separator, splitOptions), expectedRanges);
        }

        private static void SplitTest_Common(Utf8String source, Utf8StringSplitDelegate splitAction, Range[] expectedRanges)
        {
            // First, run the split with default options and make sure the results are equivalent

            Assert.Equal(
                expected: expectedRanges.Select(range => source[range]),
                actual: splitAction(source, Utf8StringSplitOptions.None));

            // Next, run the split with empty entries removed

            Assert.Equal(
                expected: expectedRanges.Select(range => source[range]).Where(ustr => ustr.Length != 0),
                actual: splitAction(source, Utf8StringSplitOptions.RemoveEmptyEntries));

            // Next, run the split with results trimmed (but allowing empty results)

            Assert.Equal(
                expected: expectedRanges.Select(range => source[range].Trim()),
                actual: splitAction(source, Utf8StringSplitOptions.TrimEntries));

            // Finally, run the split both trimmed and with empty entries removed

            Assert.Equal(
                expected: expectedRanges.Select(range => source[range].Trim()).Where(ustr => ustr.Length != 0),
                actual: splitAction(source, Utf8StringSplitOptions.TrimEntries | Utf8StringSplitOptions.RemoveEmptyEntries));
        }

        public static IEnumerable<object[]> Trim_TestData() => Utf8SpanTests.Trim_TestData();

        [Theory]
        [MemberData(nameof(Trim_TestData))]
        public static void Trim(string input)
        {
            if (input is null)
            {
                return; // don't want to null ref
            }

            Utf8String utf8Input = u8(input);

            void RunTest(Func<Utf8String, Utf8String> utf8TrimAction, Func<string, string> utf16TrimAction)
            {
                Utf8String utf8Trimmed = utf8TrimAction(utf8Input);
                string utf16Trimmed = utf16TrimAction(input);

                if (utf16Trimmed.Length == input.Length)
                {
                    Assert.Same(utf8Input, utf8Trimmed); // Trimming should no-op, return original input
                }
                else if (utf16Trimmed.Length == 0)
                {
                    Assert.Same(Utf8String.Empty, utf8Trimmed); // Trimming an all-whitespace input, return Empty
                }
                else
                {
                    Assert.True(Utf8String.AreEquivalent(utf8Trimmed, utf16Trimmed));
                }
            }

            RunTest(ustr => ustr.Trim(), str => str.Trim());
            RunTest(ustr => ustr.TrimStart(), str => str.TrimStart());
            RunTest(ustr => ustr.TrimEnd(), str => str.TrimEnd());
        }
    }
}
