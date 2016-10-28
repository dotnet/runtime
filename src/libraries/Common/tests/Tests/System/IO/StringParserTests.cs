// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace Tests.System.IO
{
    public class StringParserTests
    {
        [Fact]
        public void TestParserMoveWithSkipEmpty()
        {
            string buffer = "My,, ,Te,,st,Str|ing,,123,,";
            char separator = ',';

            StringParser sp = new StringParser(buffer, separator, true);
            sp.MoveNextOrFail();
            Assert.Equal("My", sp.ExtractCurrent());
            Assert.Equal(' ', sp.ParseNextChar());
            sp.MoveNextOrFail();
            Assert.Equal("Te", sp.ExtractCurrent());
            Assert.Equal("st", sp.MoveAndExtractNext());
            sp.MoveNextOrFail();
            Assert.Equal("Str|ing", sp.ExtractCurrent());
            Assert.Equal(123, sp.ParseNextInt32());
            Assert.Throws<InvalidDataException>(() => sp.MoveNextOrFail());
            sp.MoveNext();
            Assert.Equal("", sp.ExtractCurrent());
        }

        [Fact]
        public void TestParserMoveWithNoSkipEmpty()
        {
            string buffer = "My,, ,Te,,st,Str|ing,,123,";
            char separator = ',';

            StringParser sp = new StringParser(buffer, separator, false);
            sp.MoveNextOrFail();
            Assert.Equal("My", sp.ExtractCurrent());
            sp.MoveNextOrFail();
            Assert.Equal(' ', sp.ParseNextChar());
            sp.MoveNextOrFail();
            Assert.Equal("Te", sp.ExtractCurrent());
            sp.MoveNextOrFail();
            Assert.Equal("st", sp.MoveAndExtractNext());
            sp.MoveNextOrFail();
            Assert.Equal("Str|ing", sp.ExtractCurrent());
            sp.MoveNextOrFail();
            Assert.Equal(123, sp.ParseNextInt32());
            sp.MoveNext();
            Assert.Throws<InvalidDataException>(() => sp.MoveNextOrFail());
            Assert.Equal("", sp.ExtractCurrent());
        }

        [Fact]
        public void TestParserWithNoEndingSeparatorWithSkipEmpty()
        {
            string buffer = ",,,Str|ing,,123";
            char separator = ',';

            StringParser sp = new StringParser(buffer, separator, true);
            Assert.Equal("Str|ing", sp.MoveAndExtractNext());
            Assert.Equal(123, sp.ParseNextInt32());
        }

        [Fact]
        public void TestParserWithNoEndingSeparatorWithNoSkipEmpty()
        {
            string buffer = ",,Str|ing,,123";
            char separator = ',';

            StringParser sp = new StringParser(buffer, separator, false);
            sp.MoveNextOrFail();
            sp.MoveNextOrFail();
            Assert.Equal("Str|ing", sp.MoveAndExtractNext());
            Assert.Throws<InvalidDataException>(() => sp.ParseNextInt32());
            Assert.Equal(123, sp.ParseNextInt32());
        }

        [Fact]
        public void TestValidParseNumericMethods()
        {
            string buffer = int.MaxValue + "," + int.MinValue + "," + uint.MinValue + "," + uint.MaxValue + "," + long.MinValue + "," + long.MaxValue + "," + ulong.MinValue + "," + ulong.MaxValue;
            char separator = ',';

            StringParser sp = new StringParser(buffer, separator);
            Assert.Equal(int.MaxValue, sp.ParseNextInt32());
            Assert.Equal(int.MinValue, sp.ParseNextInt32());
            Assert.Equal(uint.MinValue, sp.ParseNextUInt32());
            Assert.Equal(uint.MaxValue, sp.ParseNextUInt32());
            Assert.Equal(long.MinValue, sp.ParseNextInt64());
            Assert.Equal(long.MaxValue, sp.ParseNextInt64());
            Assert.Equal(ulong.MinValue, sp.ParseNextUInt64());
            Assert.Equal(ulong.MaxValue, sp.ParseNextUInt64());
        }

        [Fact]
        public void TestOverflowFromNumericParsing()
        {
            string buffer = long.MinValue + "," + long.MaxValue + "," + decimal.MinValue + "," + decimal.MaxValue;
            char separator = ',';

            StringParser sp = new StringParser(buffer, separator);
            Assert.Throws<OverflowException>(() => sp.ParseNextInt32());
            Assert.Throws<OverflowException>(() => sp.ParseNextUInt32());
            Assert.Throws<OverflowException>(() => sp.ParseNextInt64());
            Assert.Throws<OverflowException>(() => sp.ParseNextUInt64());
        }

        [Fact]
        public void TestParseNextChar()
        {
            string buffer = "\u0020,kkk,|";
            char separator = ',';

            StringParser sp = new StringParser(buffer, separator);
            Assert.Equal('\u0020', sp.ParseNextChar());
            sp.MoveNextOrFail();
            Assert.Equal('|', sp.ParseNextChar());
        }

        [Fact]
        public void TestUnicodeSeparator()
        {
            string buffer = "\u0020some\u0020123";
            char separator = '\u0020';

            StringParser sp = new StringParser(buffer, separator);
            sp.MoveNextOrFail();
            Assert.Equal("some", sp.MoveAndExtractNext());
            Assert.Equal((uint)123, sp.ParseNextUInt32());
        }

        [Fact]
        public void TestExtractingStringFromParentheses()
        {
            string buffer = "This(is a unicode \u0020)something,(89),(haha123blah)After brace,";
            char separator = ',';

            StringParser sp = new StringParser(buffer, separator);
            Assert.Throws<InvalidDataException>(() => sp.MoveAndExtractNextInOuterParens());
            Assert.Equal("This(is a unicode \u0020)something", sp.ExtractCurrent());
            Assert.Equal("89),(haha123blah", sp.MoveAndExtractNextInOuterParens());
            Assert.Equal("(89),(haha123blah)", sp.ExtractCurrent());
            Assert.Equal("fter brace", sp.MoveAndExtractNext());
            Assert.Equal("", sp.MoveAndExtractNext());
        }
    }
}
