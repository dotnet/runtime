// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
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
            AssertExtensions.Throws<ArgumentException>(null, () => Strings.Chr(charCode));
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
        [InlineData(new string[] { }, null, null)]
        [InlineData(new string[] { }, "", null)]
        public void Filter_WhenNoMatchArgument_ReturnsNull(string[] source, string match, string[] expected)
        {
            Assert.Equal(expected, Strings.Filter(source, match));
        }

        [Theory]
        [InlineData(new string[] { }, "a", new string[] { }, new string[] { })]
        public void NoElements(string[] source, string match, string[] includeExpected, string[] excludeExpected)
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
            var source = new object[] { typeof(object) };
            var match = "a";

            AssertExtensions.Throws<ArgumentException>("Source", null, () => Strings.Filter(source, match));
        }

        [Theory]
        [InlineData(new object[] { 42 }, "42", new string[] { "42" }, new string[] { })]
        [InlineData(new object[] { true }, "True", new string[] { "True" }, new string[] { })]
        public void Objects(object[] source, string match, string[] includeExpected, string[] excludeExpected)
        {
            Assert.Equal(includeExpected, Strings.Filter(source, match, Include: true));
            Assert.Equal(excludeExpected, Strings.Filter(source, match, Include: false));
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
    }
}
