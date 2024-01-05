// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Tests;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic.CompilerServices.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class StringsTests
    {
        static StringsTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Fact]
        public void Asc_Char_ReturnsChar()
        {
            Assert.Equal('3', Strings.Asc('3'));
        }

        [Theory]
        [InlineData("3", 51)]
        [InlineData("345", 51)]
        [InlineData("ABCD", 65)]
        public void Asc_String_ReturnsExpected(string String, int expected)
        {
            Assert.Equal(expected, Strings.Asc(String));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Asc_NullOrEmpty_ThrowsArgumentException(string String)
        {
            AssertExtensions.Throws<ArgumentException>("String", null, () => Strings.Asc(String));
        }

        [Fact]
        public void AscW_Char_ReturnsChar()
        {
            Assert.Equal('3', Strings.AscW('3'));
        }

        [Theory]
        [InlineData("3", 51)]
        [InlineData("345", 51)]
        [InlineData("ABCD", 65)]
        public void AscW_String_ReturnsExpected(string String, int expected)
        {
            Assert.Equal(expected, Strings.AscW(String));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AscW_NullOrEmpty_ThrowsArgumentException(string String)
        {
            AssertExtensions.Throws<ArgumentException>("String", null, () => Strings.AscW(String));
        }

        [Theory]
        [InlineData(97)]
        [InlineData(65)]
        [InlineData(0)]
        public void Chr_CharCodeInRange_ReturnsExpected(int charCode)
        {
            Assert.Equal(Convert.ToChar(charCode & 0XFFFF), Strings.Chr(charCode));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(256)]
        public void Chr_CharCodeOutOfRange_ThrowsNotSupportedException(int charCode)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (new ThreadCultureChange("en-US")) // Strings.Chr doesn't fail on these inputs for all code pages, e.g. 949
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Strings.Chr(charCode));
            }
        }

        [Theory]
        [InlineData(-32769)]
        [InlineData(65536)]
        public void Chr_CharCodeOutOfRange_ThrowsArgumentException(int charCode)
        {
            AssertExtensions.Throws<ArgumentException>("CharCode", null, () => Strings.Chr(charCode));
        }

        [Theory]
        [InlineData(97)]
        [InlineData(65)]
        [InlineData(65535)]
        [InlineData(-32768)]
        public void ChrW_CharCodeInRange_ReturnsExpected(int charCode)
        {
            Assert.Equal(Convert.ToChar(charCode & 0XFFFF), Strings.ChrW(charCode));
        }

        [Theory]
        [InlineData(-32769)]
        [InlineData(65536)]
        public void ChrW_CharCodeOutOfRange_ThrowsArgumentException(int charCode)
        {
            AssertExtensions.Throws<ArgumentException>("CharCode", null, () => Strings.ChrW(charCode));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(33, 33)]
        [InlineData(172, 172)]
        [InlineData(255, 255)]
        public void Asc_Chr_Invariant(int charCode, int expected)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                Assert.Equal(1252, CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
                Assert.Equal(expected, Strings.Asc(Strings.Chr(charCode)));
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/30419", TargetFrameworkMonikers.NetFramework)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [InlineData(0, 0)]
        [InlineData(33, 33)]
        [InlineData(172, 0)]
        [InlineData(255, 255)]
        [InlineData(256, 1)]
        [InlineData(0x8141, 0x8141)]
        [InlineData(0xC8FE, 0xC8FE)]
        [InlineData(0xFFFF, 0xFF)]
        public void Asc_Chr_DoubleByte(int charCode, int expected)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (new ThreadCultureChange("ko-KR"))
            {
                Assert.Equal(949, CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
                Assert.Equal(expected, (ushort)Strings.Asc(Strings.Chr(charCode)));
            }
        }

        [Theory]
        [InlineData(new string[] { }, null, null)]
        [InlineData(new string[] { }, "", null)]
        public void Filter_WhenNoMatchArgument_ReturnsNull(string[] source, string match, string[] expected)
        {
            Assert.Equal(expected, Strings.Filter(source, match));
        }

        [Theory]
        [InlineData(new string[] { }, "a", new string[] { }, new string[] { })]
        public void Filter_NoElements(string[] source, string match, string[] includeExpected, string[] excludeExpected)
        {
            Assert.Equal(includeExpected, Strings.Filter(source, match, Include: true));
            Assert.Equal(excludeExpected, Strings.Filter(source, match, Include: false));
        }

        [Theory]
        [InlineData(new string[] { }, "a", new string[] { }, new string[] { })]
        [InlineData(new string[] { "a" }, "a", new string[] { "a" }, new string[] { })]
        [InlineData(new string[] { "ab" }, "a", new string[] { "ab" }, new string[] { })]
        [InlineData(new string[] { "ba" }, "a", new string[] { "ba" }, new string[] { })]
        [InlineData(new string[] { "bab" }, "a", new string[] { "bab" }, new string[] { })]
        [InlineData(new string[] { "b" }, "a", new string[] { }, new string[] { "b" })]
        [InlineData(new string[] { "a" }, "ab", new string[] { }, new string[] { "a" })]
        [InlineData(new string[] { "ab" }, "ab", new string[] { "ab" }, new string[] { })]
        public void Filter_SingleElement(string[] source, string match, string[] includeExpected, string[] excludeExpected)
        {
            Assert.Equal(includeExpected, Strings.Filter(source, match, Include: true));
            Assert.Equal(excludeExpected, Strings.Filter(source, match, Include: false));
        }

        [Theory]
        [InlineData(new string[] { "A" }, "a", new string[] { }, new string[] { "A" })]
        public void Filter_SingleElement_BinaryCompare(string[] source, string match, string[] includeExpected, string[] excludeExpected)
        {
            Assert.Equal(includeExpected, Strings.Filter(source, match, Include: true, Compare: CompareMethod.Binary));
            Assert.Equal(excludeExpected, Strings.Filter(source, match, Include: false, Compare: CompareMethod.Binary));
        }

        [Theory]
        [InlineData(new string[] { "A" }, "a", new string[] { "A" }, new string[] { })]
        public void Filter_SingleElement_TextCompare(string[] source, string match, string[] includeExpected, string[] excludeExpected)
        {
            Assert.Equal(includeExpected, Strings.Filter(source, match, Include: true, Compare: CompareMethod.Text));
            Assert.Equal(excludeExpected, Strings.Filter(source, match, Include: false, Compare: CompareMethod.Text));
        }

        [Theory]
        [InlineData(new string[] { "a", "a" }, "a", new string[] { "a", "a" }, new string[] { })]
        [InlineData(new string[] { "a", "b" }, "a", new string[] { "a" }, new string[] { "b" })]
        [InlineData(new string[] { "b", "a" }, "a", new string[] { "a" }, new string[] { "b" })]
        [InlineData(new string[] { "b", "b" }, "a", new string[] { }, new string[] { "b", "b" })]
        public void Filter_MultipleElements(string[] source, string match, string[] includeExpected, string[] excludeExpected)
        {
            Assert.Equal(includeExpected, Strings.Filter(source, match, Include: true));
            Assert.Equal(excludeExpected, Strings.Filter(source, match, Include: false));
        }

        [Fact]
        public void Filter_Objects_WhenObjectCannotBeConvertedToString_ThrowsArgumentOutOfRangeException()
        {
            object[] source = new object[] { typeof(object) };
            string match = "a";

            AssertExtensions.Throws<ArgumentException>("Source", null, () => Strings.Filter(source, match));
        }

        [Theory]
        [InlineData(new object[] { 42 }, "42", new string[] { "42" }, new string[] { })]
        [InlineData(new object[] { true }, "True", new string[] { "True" }, new string[] { })]
        public void Filter_Objects(object[] source, string match, string[] includeExpected, string[] excludeExpected)
        {
            Assert.Equal(includeExpected, Strings.Filter(source, match, Include: true));
            Assert.Equal(excludeExpected, Strings.Filter(source, match, Include: false));
        }

        [Theory]
        [MemberData(nameof(Format_TestData))]
        public void Format(object expression, string style, string expected)
        {
            Assert.Equal(expected, Strings.Format(expression, style));
        }

        [Theory]
        [MemberData(nameof(Format_InvalidCastException_TestData))]
        public void Format_InvalidCastException(object expression, string style)
        {
            Assert.Throws<InvalidCastException>(() => Strings.Format(expression, style));
        }

        public static IEnumerable<object[]> Format_TestData()
        {
            yield return new object[] { null, null, "" };
            yield return new object[] { null, "", "" };
            yield return new object[] { "", null, "" };
            yield return new object[] { "", "", "" };
            yield return new object[] { sbyte.MinValue, "0", "-128" };
            yield return new object[] { sbyte.MaxValue, "0", "127" };
            yield return new object[] { ushort.MinValue, "0", "0" };
            yield return new object[] { ushort.MaxValue, "0", "65535" };
            yield return new object[] { false, "", "False" };
            yield return new object[] { false, "0", "0" };
            if (IsEnUS())
            {
                yield return new object[] { 1.234, "", "1.234" };
                yield return new object[] { 1.234, "0", "1" };
                yield return new object[] { 1.234, "0.0", "1.2" };
                yield return new object[] { 1.234, "fixed", "1.23" };
                yield return new object[] { 1.234, "percent", "123.40%" };
                yield return new object[] { 1.234, "standard", "1.23" };
                yield return new object[] { 1.234, "currency", "$1.23" };
                yield return new object[] { false, "yes/no", "No" };
                yield return new object[] { true, "yes/no", "Yes" };
                yield return new object[] { false, "on/off", "Off" };
                yield return new object[] { true, "on/off", "On" };
                yield return new object[] { false, "true/false", "False" };
                yield return new object[] { true, "true/false",  "True" };
                yield return new object[] { 0, "yes/no", "No" };
                yield return new object[] { "ABC", "yes/no", "ABC" };
                yield return new object[] { 123.4, "scientific", "1.23E+02" };
            }
            DateTime d = DateTime.Now;
            yield return new object[] { d, "long time", d.ToString("T") };
            yield return new object[] { d, "medium time", d.ToString("T") };
            yield return new object[] { d, "short time", d.ToString("t") };
            yield return new object[] { d, "long date", d.ToString("D") };
            yield return new object[] { d, "medium date", d.ToString("D") };
            yield return new object[] { d, "short date", d.ToString("d") };
            yield return new object[] { d, "general date", d.ToString("G") };
            yield return new object[] { 123.4, "general number", 123.4.ToString("G", null) };
        }

        public static IEnumerable<object[]> Format_InvalidCastException_TestData()
        {
            yield return new object[] { new object(), null };
            yield return new object[] { new object(), "0" };
        }

        [Theory]
        [MemberData(nameof(FormatCurrency_TestData))]
        public void FormatCurrency(object expression, int numDigitsAfterDecimal, TriState includeLeadingDigit, TriState useParensForNegativeNumbers, TriState groupDigits, string expected)
        {
            Assert.Equal(expected, Strings.FormatCurrency(expression, numDigitsAfterDecimal, includeLeadingDigit, useParensForNegativeNumbers, groupDigits));
        }

        public static IEnumerable<object[]> FormatCurrency_TestData()
        {
            yield return new object[] { null, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "" };
            if (IsEnUS())
            {
                yield return new object[] { 0.123, 0, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "$0" };
                yield return new object[] { 0.123, 1, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "$0.1" };
                yield return new object[] { 0.123, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "$0.12" };
                yield return new object[] { 0.123, 4, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "$0.1230" };
                yield return new object[] { 0.123, 2, TriState.False, TriState.UseDefault, TriState.UseDefault, "$.12" };
                yield return new object[] { 0.123, 2, TriState.True, TriState.UseDefault, TriState.UseDefault, "$0.12" };
                yield return new object[] { -0.123, 2, TriState.UseDefault, TriState.False, TriState.UseDefault, "-$0.12" };
                yield return new object[] { -0.123, 2, TriState.UseDefault, TriState.True, TriState.UseDefault, "($0.12)" };
                yield return new object[] { 1234.5, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "$1,234.50" };
                yield return new object[] { 1234.5, 2, TriState.UseDefault, TriState.UseDefault, TriState.False, "$1234.50" };
                yield return new object[] { 1234.5, 2, TriState.UseDefault, TriState.UseDefault, TriState.True, "$1,234.50" };
            }
        }

        [Theory]
        [MemberData(nameof(FormatDateTime_TestData))]
        public void FormatDateTime(DateTime expression, DateFormat format, string expected)
        {
            Assert.Equal(expected, Strings.FormatDateTime(expression, format));
        }

        public static IEnumerable<object[]> FormatDateTime_TestData()
        {
            DateTime d = DateTime.Now;
            yield return new object[] { d, DateFormat.LongTime, d.ToString("T") };
            yield return new object[] { d, DateFormat.ShortTime, d.ToString("HH:mm") };
            yield return new object[] { d, DateFormat.LongDate, d.ToString("D") };
            yield return new object[] { d, DateFormat.ShortDate, d.ToString("d") };
            yield return new object[] { d, DateFormat.GeneralDate, d.ToString("G") };
        }

        [Theory]
        [MemberData(nameof(FormatNumber_TestData))]
        public void FormatNumber(object expression, int numDigitsAfterDecimal, TriState includeLeadingDigit, TriState useParensForNegativeNumbers, TriState groupDigits, string expected)
        {
            Assert.Equal(expected, Strings.FormatNumber(expression, numDigitsAfterDecimal, includeLeadingDigit, useParensForNegativeNumbers, groupDigits));
        }

        public static IEnumerable<object[]> FormatNumber_TestData()
        {
            yield return new object[] { null, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "" };
            if (IsEnUS())
            {
                yield return new object[] { 0.123, 0, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "0" };
                yield return new object[] { 0.123, 1, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "0.1" };
                yield return new object[] { 0.123, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "0.12" };
                yield return new object[] { 0.123, 4, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "0.1230" };
                yield return new object[] { 0.123, 2, TriState.False, TriState.UseDefault, TriState.UseDefault, ".12" };
                yield return new object[] { 0.123, 2, TriState.True, TriState.UseDefault, TriState.UseDefault, "0.12" };
                yield return new object[] { -0.123, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "-0.12" };
                yield return new object[] { -0.123, 2, TriState.UseDefault, TriState.False, TriState.UseDefault, "-0.12" };
                yield return new object[] { -0.123, 2, TriState.UseDefault, TriState.True, TriState.UseDefault, "(0.12)" };
                yield return new object[] { 1234.5, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "1,234.50" };
                yield return new object[] { 1234.5, 2, TriState.UseDefault, TriState.UseDefault, TriState.False, "1234.50" };
                yield return new object[] { 1234.5, 2, TriState.UseDefault, TriState.UseDefault, TriState.True, "1,234.50" };
            }
        }

        [Theory]
        [MemberData(nameof(FormatPercent_TestData))]
        public void FormatPercent(object expression, int numDigitsAfterDecimal, TriState includeLeadingDigit, TriState useParensForNegativeNumbers, TriState groupDigits, string expected)
        {
            Assert.Equal(expected, Strings.FormatPercent(expression, numDigitsAfterDecimal, includeLeadingDigit, useParensForNegativeNumbers, groupDigits));
        }

        public static IEnumerable<object[]> FormatPercent_TestData()
        {
            yield return new object[] { null, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "" };
            if (IsEnUS())
            {
                yield return new object[] { 0.123, 0, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "12%" };
                yield return new object[] { 0.123, 1, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "12.3%" };
                yield return new object[] { 0.123, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "12.30%" };
                yield return new object[] { 0.123, 4, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "12.3000%" };
                yield return new object[] { 0.00123, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "0.12%" };
                yield return new object[] { 0.00123, 2, TriState.False, TriState.UseDefault, TriState.UseDefault, ".12%" };
                yield return new object[] { 0.00123, 2, TriState.True, TriState.UseDefault, TriState.UseDefault, "0.12%" };
                yield return new object[] { -0.123, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "-12.30%" };
                yield return new object[] { -0.123, 2, TriState.UseDefault, TriState.False, TriState.UseDefault, "-12.30%" };
                yield return new object[] { -0.123, 2, TriState.UseDefault, TriState.True, TriState.UseDefault, "(12.30%)" };
                yield return new object[] { 12.345, 2, TriState.UseDefault, TriState.UseDefault, TriState.UseDefault, "1,234.50%" };
                yield return new object[] { 12.345, 2, TriState.UseDefault, TriState.UseDefault, TriState.False, "1234.50%" };
                yield return new object[] { 12.345, 2, TriState.UseDefault, TriState.UseDefault, TriState.True, "1,234.50%" };
            }
        }

        [Theory]
        [InlineData("ABC", 1, 'A')]
        [InlineData("ABC", 2, 'B')]
        [InlineData("ABC", 3, 'C')]
        public void GetChar(string str, int index, char expected)
        {
            Assert.Equal(expected, Strings.GetChar(str, index));
        }

        [Theory]
        [InlineData(null, 0)]
        [InlineData(null, 1)]
        [InlineData("", 0)]
        [InlineData("", 1)]
        [InlineData("ABC", 0)]
        [InlineData("ABC", 4)]
        public void GetChar_ArgumentException(string str, int index)
        {
            Assert.Throws< ArgumentException>(() => Strings.GetChar(str, index));
        }

        [Theory]
        [MemberData(nameof(InStr_TestData_NullsAndEmpties))]
        [MemberData(nameof(InStr_FromBegin_TestData))]
        public void InStr_FromBegin(string string1, string string2, int expected)
        {
            Assert.Equal(expected, Strings.InStr(string1, string2));
            Assert.Equal(expected, Strings.InStr(1, string1, string2));
        }

        [Theory]
        [MemberData(nameof(InStr_TestData_NullsAndEmpties))]
        [MemberData(nameof(InStr_FromWithin_TestData))]
        public void InStr_FromWithin(string string1, string string2, int expected)
        {
            Assert.Equal(expected, Strings.InStr(2, string1, string2));
        }

        [Theory]
        [InlineData("A", "a", 0)]
        [InlineData("Aa", "a", 2)]
        public void InStr_BinaryCompare(string string1, string string2, int expected)
        {
            Assert.Equal(expected, Strings.InStr(string1, string2, CompareMethod.Binary));
            Assert.Equal(expected, Strings.InStr(1, string1, string2, CompareMethod.Binary));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [InlineData("A", "a", 1)]
        [InlineData("Aa", "a", 1)]
        public void InStr_TextCompare(string string1, string string2, int expected)
        {
            Assert.Equal(expected, Strings.InStr(string1, string2, CompareMethod.Text));
            Assert.Equal(expected, Strings.InStr(1, string1, string2, CompareMethod.Text));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public void InStr_WhenStartGreaterThanLength_ReturnsZero(int start)
        {
            Assert.Equal(0, Strings.InStr(start, "a", "a"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void InStr_WhenStartZeroOrLess_ThrowsArgumentException(int start)
        {
            AssertExtensions.Throws<ArgumentException>("Start", null, () => Strings.InStr(start, "a", "a"));
        }

        [Theory]
        [MemberData(nameof(InStr_TestData_NullsAndEmpties))]
        [MemberData(nameof(InStrRev_FromEnd_TestData))]
        public void InStrRev_FromEnd(string stringCheck, string stringMatch, int expected)
        {
            Assert.Equal(expected, Strings.InStrRev(stringCheck, stringMatch));
        }

        [Theory]
        [MemberData(nameof(InStrRev_FromWithin_TestData))]
        public void InStrRev_FromWithin(string stringCheck, string stringMatch, int start, int expected)
        {
            Assert.Equal(expected, Strings.InStrRev(stringCheck, stringMatch, start));
        }

        [Theory]
        [InlineData("A", "a", 1, 0)]
        [InlineData("aA", "a", 2, 1)]
        public void InStrRev_BinaryCompare(string stringCheck, string stringMatch, int start, int expected)
        {
            Assert.Equal(expected, Strings.InStrRev(stringCheck, stringMatch, start, CompareMethod.Binary));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [InlineData("A", "a", 1, 1)]
        [InlineData("aA", "a", 2, 2)]
        public void InStrRev_TextCompare(string stringCheck, string stringMatch, int start, int expected)
        {
            Assert.Equal(expected, Strings.InStrRev(stringCheck, stringMatch, start, CompareMethod.Text));
        }

        [Fact]
        public void InStrRev_WhenStartMinusOne_SearchesFromEnd()
        {
            Assert.Equal(2, Strings.InStrRev("aa", "a", -1));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public void InStrRev_WhenStartGreaterThanLength_ReturnsZero(int start)
        {
            Assert.Equal(0, Strings.InStrRev("a", "a", start));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        [InlineData(-3)]
        public void InStrRev_WhenStartZeroOrMinusTwoOrLess_ThrowsArgumentException(int start)
        {
            AssertExtensions.Throws<ArgumentException>("Start", null, () => Strings.InStrRev("a", "a", start));
        }

        [Theory]
        [MemberData(nameof(Join_Object_TestData))]
        [MemberData(nameof(Join_String_TestData))]
        public void Join_ObjectArray(object[] source, string delimiter, string expected)
        {
            Assert.Equal(expected, Strings.Join(source, delimiter));
        }

        public static IEnumerable<object[]> Join_Object_TestData()
        {
            yield return new object[] { new object[0], null, null };
            yield return new object[] { new object[0], ",", null };
            yield return new object[] { new object[] { 1 }, ",", "1" };
            yield return new object[] { new object[] { 1, null, 3 }, null, "13" };
            yield return new object[] { new object[] { true, false }, "", "TrueFalse" };
            yield return new object[] { new object[] { 1, 2, 3 }, ", ", "1, 2, 3" };
        }

        [Theory]
        [MemberData(nameof(Join_String_TestData))]
        public void Join_StringArray(string[] source, string delimiter, string expected)
        {
            Assert.Equal(expected, Strings.Join(source, delimiter));
        }

        public static IEnumerable<object[]> Join_String_TestData()
        {
            yield return new object[] { new string[0], null, null };
            yield return new object[] { new string[0], ",", null };
            yield return new object[] { new string[] { "A" }, ",", "A" };
            yield return new object[] { new string[] { "", null, "" }, null, "" };
            yield return new object[] { new string[] { "", "AB", "C" }, "", "ABC" };
            yield return new object[] { new string[] { "A", "B", "C" }, ", ", "A, B, C" };
        }

        [Theory]
        [InlineData('\0', '\0')]
        [InlineData('\uffff', '\uffff')]
        [InlineData('a', 'a')]
        [InlineData('A', 'a')]
        [InlineData('1', '1')]
        public void LCase_Char(char value, char expected)
        {
            Assert.Equal(expected, Strings.LCase(value));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("\0", "\0")]
        [InlineData("\uffff", "\uffff")]
        [InlineData("abc", "abc")]
        [InlineData("ABC", "abc")]
        [InlineData("123", "123")]
        public void LCase_String(string value, string expected)
        {
            Assert.Equal(expected, Strings.LCase(value));
        }

        [Theory]
        [InlineData('\0', '\0')]
        [InlineData('\uffff', '\uffff')]
        [InlineData('a', 'A')]
        [InlineData('A', 'A')]
        [InlineData('1', '1')]
        public void UCase_Char(char value, char expected)
        {
            Assert.Equal(expected, Strings.UCase(value));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("\0", "\0")]
        [InlineData("\uffff", "\uffff")]
        [InlineData("abc", "ABC")]
        [InlineData("ABC", "ABC")]
        [InlineData("123", "123")]
        public void UCase_String(string value, string expected)
        {
            Assert.Equal(expected, Strings.UCase(value));
        }

        [Theory]
        [InlineData("a", -1)]
        public void Left_Invalid(string str, int length)
        {
            AssertExtensions.Throws<ArgumentException>("Length", null, () => Strings.Left(str, length));
        }

        [Theory]
        [InlineData("", 0, "")]
        [InlineData("", 1, "")]
        [InlineData("abc", 0, "")]
        [InlineData("abc", 2, "ab")]
        [InlineData("abc", int.MaxValue, "abc")]
        public void Left_Valid(string str, int length, string expected)
        {
            Assert.Equal(expected, Strings.Left(str, length));
        }

        [Theory]
        [MemberData(nameof(Len_Object_Data))]
        [MemberData(nameof(StructUtilsTestData.RecordsAndLength), MemberType = typeof(StructUtilsTestData))]
        public void Len_Object(object o, int length)
        {
            Assert.Equal(length, Strings.Len(o));
        }

        public static TheoryData<object, int> Len_Object_Data() => new TheoryData<object, int>
        {
            { null, 0 },
            { new bool(), 2 },
            { new sbyte(), 1 },
            { new byte(), 1 },
            { new short(), 2 },
            { new ushort(), 2 },
            { new uint(), 4 },
            { new int(), 4 },
            { new ulong(), 8 },
            { new decimal(), 16 },
            { new float(), 4 },
            { new double(), 8 },
            { new DateTime(), 8 },
            { new char(), 2 },
            { "", 0 },
            { "a", 1 },
            { "ab", 2 },
            { "ab\0", 3 },
        };

        [Theory]
        [InlineData("a", -1)]
        public void Right_Invalid(string str, int length)
        {
            AssertExtensions.Throws<ArgumentException>("Length", null, () => Strings.Right(str, length));
        }

        [Theory]
        [InlineData("", 0, "")]
        [InlineData("", 1, "")]
        [InlineData("abc", 0, "")]
        [InlineData("abc", 2, "bc")]
        [InlineData("abc", int.MaxValue, "abc")]
        public void Right_Valid(string str, int length, string expected)
        {
            Assert.Equal(expected, Strings.Right(str, length));
        }

        [Theory]
        [InlineData("a", -1)]
        public void Mid2_Invalid(string str, int start)
        {
            AssertExtensions.Throws<ArgumentException>("Start", null, () => Strings.Mid(str, start));
        }

        [Theory]
        [InlineData("", 1, "")]
        [InlineData(null, 1, null)]
        [InlineData("abc", 1000, "")]
        [InlineData("abcde", 2, "bcde")]
        [InlineData("abc", 1, "abc")]   // 1-based strings in VB
        [InlineData("abcd", 2, "bcd")]
        [InlineData("abcd", 3, "cd")]
        public void Mid2_Valid(string str, int start, string expected)
        {
            Assert.Equal(expected, Strings.Mid(str, start));
        }

        [Theory]
        [InlineData("a", 1, -1)]
        [InlineData("a", -1, 1)]
        public void Mid3_Invalid(string str, int start, int length)
        {
            AssertExtensions.Throws<ArgumentException>(start < 1 ? "Start" : "Length", null, () => Strings.Mid(str, start, length));
        }

        [Theory]
        [InlineData("", 1, 0, "")]
        [InlineData(null, 1, 1, "")]
        [InlineData("abc", 1000, 1, "")]
        [InlineData("abcde", 2, 1000, "bcde")]
        [InlineData("abc", 1, 2, "ab")]   // 1-based strings in VB
        [InlineData("abcd", 2, 2, "bc")]
        [InlineData("abcd", 2, 3, "bcd")]
        public void Mid3_Valid(string str, int start, int length, string expected)
        {
            Assert.Equal(expected, Strings.Mid(str, start, length));
        }

        [Theory]
        [InlineData(null, 0, "")]
        [InlineData(null, 1, " ")]
        [InlineData("", 0, "")]
        [InlineData("", 1, " ")]
        [InlineData("A", 0, "")]
        [InlineData("A", 1, "A")]
        [InlineData("A", 2, "A ")]
        [InlineData("AB", 0, "")]
        [InlineData("AB", 1, "A")]
        [InlineData("AB", 2, "AB")]
        [InlineData("AB", 4, "AB  ")]
        public void LSet(string source, int length, string expected)
        {
            Assert.Equal(expected, Strings.LSet(source, length));
        }

        [Theory]
        [InlineData(null, 0, "")]
        [InlineData(null, 1, " ")]
        [InlineData("", 0, "")]
        [InlineData("", 1, " ")]
        [InlineData("A", 0, "")]
        [InlineData("A", 1, "A")]
        [InlineData("A", 2, " A")]
        [InlineData("AB", 0, "")]
        [InlineData("AB", 1, "A")]
        [InlineData("AB", 2, "AB")]
        [InlineData("AB", 4, "  AB")]
        public void RSet(string source, int length, string expected)
        {
            Assert.Equal(expected, Strings.RSet(source, length));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData(" ", "")]
        [InlineData(" abc ", "abc ")]
        [InlineData("\u3000\nabc ", "\nabc ")]
        [InlineData("\nabc ", "\nabc ")]
        [InlineData("abc ", "abc ")]
        [InlineData("abc", "abc")]
        public void LTrim_Valid(string str, string expected)
        {
            // Trims only space and \u3000 specifically
            Assert.Equal(expected, Strings.LTrim(str));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData(" ", "")]
        [InlineData(" abc ", " abc")]
        [InlineData(" abc\n\u3000", " abc\n")]
        [InlineData(" abc\n", " abc\n")]
        [InlineData(" abc", " abc")]
        [InlineData("abc", "abc")]
        public void RTrim_Valid(string str, string expected)
        {
            // Trims only space and \u3000 specifically
            Assert.Equal(expected, Strings.RTrim(str));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData(" ", "")]
        [InlineData(" abc ", "abc")]
        [InlineData("abc\n\u3000", "abc\n")]
        [InlineData("\u3000abc\n\u3000", "abc\n")]
        [InlineData("abc\n", "abc\n")]
        [InlineData("abc", "abc")]
        public void Trim_Valid(string str, string expected)
        {
            // Trims only space and \u3000 specifically
            Assert.Equal(expected, Strings.Trim(str));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [InlineData("", "", null, 1, -1, CompareMethod.Text, null)]
        [InlineData("", null, "", 1, -1, CompareMethod.Text, null)]
        [InlineData("", "", "", 1, -1, CompareMethod.Text, null)]
        [InlineData("ABC", "", "", 1, -1, CompareMethod.Text, "ABC")]
        [InlineData("ABC", "bc", "23", 1, -1, CompareMethod.Binary, "ABC")]
        [InlineData("ABC", "BC", "23", 1, -1, CompareMethod.Binary, "A23")]
        [InlineData("ABC", "bc", "23", 1, -1, CompareMethod.Text, "A23")]
        [InlineData("abcbc", "bc", "23", 1, -1, CompareMethod.Text, "a2323")]
        [InlineData("abcbc", "bc", "23", 1, 0, CompareMethod.Text, "abcbc")]
        [InlineData("abcbc", "bc", "23", 1, 1, CompareMethod.Text, "a23bc")]
        [InlineData("abc", "bc", "23", 2, -1, CompareMethod.Text, "23")]
        [InlineData("abc", "bc", "23", 3, -1, CompareMethod.Text, "c")]
        [InlineData("abc", "bc", "23", 4, -1, CompareMethod.Text, null)]
        public void Replace(string expression, string find, string replacement, int start, int n, CompareMethod compare, string expected)
        {
            Assert.Equal(expected, Strings.Replace(expression, find, replacement, start, n, compare));
        }

        [Theory]
        [InlineData(null, null, null, 0, 0, CompareMethod.Text)]
        [InlineData(null, "", "", 0, 0, CompareMethod.Text)]
        public void Replace_ArgumentException(string expression, string find, string replacement, int start, int length, CompareMethod compare)
        {
            Assert.Throws< ArgumentException>(() => Strings.Replace(expression, find, replacement, start, length, compare));
        }

        [Theory]
        [InlineData(0, "")]
        [InlineData(1, " ")]
        [InlineData(3, "   ")]
        public void Space(int number, string expected)
        {
            Assert.Equal(expected, Strings.Space(number));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [InlineData(null, null, -1, CompareMethod.Text, new string[] { "" })]
        [InlineData(null, "", -1, CompareMethod.Text, new string[] { "" })]
        [InlineData("", null, -1, CompareMethod.Text, new string[] { "" })]
        [InlineData("", "", -1, CompareMethod.Text, new string[] { "" })]
        [InlineData("ABC", ",", -1, CompareMethod.Text, new string[] { "ABC" })]
        [InlineData("A,,BC", ",", -1, CompareMethod.Text, new string[] { "A", "", "BC" })]
        [InlineData("ABC", "b", -1, CompareMethod.Text, new string[] { "A", "C" })]
        [InlineData("ABC", "b", -1, CompareMethod.Binary, new string[] { "ABC" })]
        [InlineData("A, B, C", ", ", -1, CompareMethod.Text, new string[] { "A", "B", "C" })]
        [InlineData("A, B, C", ", ", 1, CompareMethod.Text, new string[] { "A, B, C" })]
        [InlineData("A, B, C", ", ", 2, CompareMethod.Text, new string[] { "A", "B, C" })]
        [InlineData("A, B, C", ", ", int.MaxValue, CompareMethod.Text, new string[] { "A", "B", "C" })]
        public void Split(string expression, string delimiter, int limit, CompareMethod compare, string[] expected)
        {
            Assert.Equal(expected, Strings.Split(expression, delimiter, limit, compare));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [InlineData("A, B, C", ", ", 0, CompareMethod.Text)]
        public void Split_IndexOutOfRangeException(string expression, string delimiter, int limit, CompareMethod compare)
        {
            Assert.Throws< IndexOutOfRangeException>(() => Strings.Split(expression, delimiter, limit, compare));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [InlineData("a", "a", 0, 0)]
        [InlineData("a", "b", -1, -1)]
        [InlineData("b", "a", 1, 1)]
        [InlineData("a", "ABC", 1, -1)]
        [InlineData("ABC", "a", -1, 1)]
        [InlineData("abc", "ABC", 1, 0)]
        public void StrComp(string left, string right, int expectedBinaryCompare, int expectedTextCompare)
        {
            Assert.Equal(expectedBinaryCompare, Strings.StrComp(left, right, CompareMethod.Binary));
            Assert.Equal(expectedTextCompare, Strings.StrComp(left, right, CompareMethod.Text));
        }

        [Theory]
        [InlineData(null, VbStrConv.None, 0, null)]
        [InlineData("", VbStrConv.None, 0, "")]
        [InlineData("ABC123", VbStrConv.None, 0, "ABC123")]
        [InlineData("", VbStrConv.Lowercase, 0, "")]
        [InlineData("Abc123", VbStrConv.Lowercase, 0, "abc123")]
        [InlineData("Abc123", VbStrConv.Uppercase, 0, "ABC123")]
        public void StrConv(string str, Microsoft.VisualBasic.VbStrConv conversion, int localeID, string expected)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(expected, Strings.StrConv(str, conversion, localeID));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => Strings.StrConv(str, conversion, localeID));
            }
        }

        [Theory]
        [MemberData(nameof(StrDup_Object_TestData))]
        [MemberData(nameof(StrDup_Char_TestData))]
        [MemberData(nameof(StrDup_String_TestData))]
        public void StrDup_Int_Object_Object(int number, object character, object expected)
        {
            Assert.Equal(expected, Strings.StrDup(number, character));
        }

        [Theory]
        [MemberData(nameof(StrDup_Object_ArgumentException_TestData))]
        public void StrDup_ArgumentException_Int_Object(int number, object character)
        {
            Assert.Throws< ArgumentException>(() => Strings.StrDup(number, character));
        }

        [Theory]
        [MemberData(nameof(StrDup_Char_TestData))]
        public void StrDup_Int_Char_String(int number, char character, string expected)
        {
            Assert.Equal(expected, Strings.StrDup(number, character));
        }

        [Theory]
        [MemberData(nameof(StrDup_Char_ArgumentException_TestData))]
        public void StrDup_ArgumentException_Int_Char(int number, char character)
        {
            Assert.Throws<ArgumentException>(() => Strings.StrDup(number, character));
        }

        [Theory]
        [MemberData(nameof(StrDup_String_TestData))]
        public void StrDup_Int_String_String(int number, string character, string expected)
        {
            Assert.Equal(expected, Strings.StrDup(number, character));
        }

        [Theory]
        [MemberData(nameof(StrDup_String_ArgumentException_TestData))]
        public void StrDup_ArgumentException_Int_String(int number, string character)
        {
            Assert.Throws<ArgumentException>(() => Strings.StrDup(number, character));
        }

        public static IEnumerable<object[]> StrDup_Object_TestData()
        {
            yield break;
        }

        public static IEnumerable<object[]> StrDup_Char_TestData()
        {
            yield return new object[] { 3, '\0', "\0\0\0" };
            yield return new object[] { 0, 'A', "" };
            yield return new object[] { 1, 'A', "A" };
            yield return new object[] { 3, 'A', "AAA" };
        }

        public static IEnumerable<object[]> StrDup_String_TestData()
        {
            yield return new object[] { 0, "A", "" };
            yield return new object[] { 1, "A", "A" };
            yield return new object[] { 3, "A", "AAA" };
            yield return new object[] { 0, "ABC", "" };
            yield return new object[] { 1, "ABC", "A" };
            yield return new object[] { 3, "ABC", "AAA" };
        }

        public static IEnumerable<object[]> StrDup_Object_ArgumentException_TestData()
        {
            yield return new object[] { -1, new object() };
            yield return new object[] { 1, 0 };
            yield return new object[] { 1, (int)'A' };
            yield return new object[] { -1, 'A' };
            yield return new object[] { -1, "A" };
            yield return new object[] { 1, "" };
        }

        public static IEnumerable<object[]> StrDup_Char_ArgumentException_TestData()
        {
            yield return new object[] { -1, 'A' };
        }

        public static IEnumerable<object[]> StrDup_String_ArgumentException_TestData()
        {
            yield return new object[] { -1, "A" };
            yield return new object[] { 1, null };
            yield return new object[] { 1, "" };
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("\0", "\0")]
        [InlineData("ABC", "CBA")]
        [InlineData("\ud83c\udfc8", "\ud83c\udfc8")]
        [InlineData("A\ud83c\udfc8", "\ud83c\udfc8A")]
        public void StrReverse(string str, string expected)
        {
            Assert.Equal(expected, Strings.StrReverse(str));
        }

        public static TheoryData<string, string, int> InStr_TestData_NullsAndEmpties() => new TheoryData<string, string, int>
        {
            {null, null, 0 },
            {null, "", 0 },
            {"", null, 0 },
            {"", "", 0 },
        };

        public static TheoryData<string, string, int> InStr_FromBegin_TestData() => new TheoryData<string, string, int>
        {
            { null, "a", 0 },
            { "a", null, 1 },
            { "a", "a", 1 },
            { "aa", "a", 1 },
            { "ab", "a", 1 },
            { "ba", "a", 2 },
            { "b", "a", 0 },
            { "a", "ab", 0 },
            { "ab", "ab", 1 },
        };

        public static TheoryData<string, string, int> InStr_FromWithin_TestData() => new TheoryData<string, string, int>
        {
            { null, "a", 0 },
            { "aa", null, 2 },
            { "aa", "a", 2 },
            { "aab", "a", 2 },
            { "aba", "a", 3 },
            { "ab", "a", 0 },
            { "aa", "ab", 0 },
            { "abab", "ab", 3 },
        };

        public static TheoryData<string, string, int> InStrRev_FromEnd_TestData() => new TheoryData<string, string, int>
        {
            { null, "a", 0 },
            { "a", null, 1 },
            { "a", "a", 1 },
            { "aa", "a", 2 },
            { "ba", "a", 2 },
            { "ab", "a", 1 },
            { "b", "a", 0 },
            { "a", "ab", 0 },
            { "ab", "ab", 1 },
        };

        public static TheoryData<string, string, int,int> InStrRev_FromWithin_TestData() => new TheoryData<string, string, int, int>
        {
            { null, null, 1, 0 },
            { null, "", 1, 0 },
            { "", null, 1, 0 },
            { "", "", 1, 0 },
            { null, "a", 1, 0 },
            { "aa", null, 1, 1 },
            { "aa", "a", 1, 1 },
            { "baa", "a", 2, 2 },
            { "aba", "a", 2, 1 },
            { "ba", "a", 1, 0 },
            { "aa", "ab", 1, 0 },
            { "abab", "ab", 3, 1 },
        };

        private static bool IsEnUS() => System.Threading.Thread.CurrentThread.CurrentCulture.Name == "en-US";
    }
}
