// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingGetByteCount
    {
        // Positive Tests
        [Fact]
        public void PosTests()
        {
            PositiveTestString(Encoding.UTF8, "TestString", 10, "00A");
            PositiveTestString(Encoding.UTF8, "", 0, "00B");
            PositiveTestString(Encoding.UTF8, "FooBA\u0400R", 8, "00C");
            PositiveTestString(Encoding.UTF8, "\u00C0nima\u0300l", 9, "00D");
            PositiveTestString(Encoding.UTF8, "Test\uD803\uDD75Test", 12, "00E");
            PositiveTestString(Encoding.UTF8, "Test\uD803Test", 11, "00F");
            PositiveTestString(Encoding.UTF8, "Test\uDD75Test", 11, "00G");
            PositiveTestString(Encoding.UTF8, "TestTest\uDD75", 11, "00H");
            PositiveTestString(Encoding.UTF8, "TestTest\uD803", 11, "00I");
            PositiveTestString(Encoding.UTF8, "\uDD75", 3, "00J");
            PositiveTestString(Encoding.UTF8, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", 12, "00K");
            PositiveTestString(Encoding.UTF8, "\u0130", 2, "00L");

            PositiveTestString(Encoding.Unicode, "TestString", 20, "00A3");
            PositiveTestString(Encoding.Unicode, "", 0, "00B3");
            PositiveTestString(Encoding.Unicode, "FooBA\u0400R", 14, "00C3");
            PositiveTestString(Encoding.Unicode, "\u00C0nima\u0300l", 14, "00D3");
            PositiveTestString(Encoding.Unicode, "Test\uD803\uDD75Test", 20, "00E3");
            PositiveTestString(Encoding.Unicode, "Test\uD803Test", 18, "00F3");
            PositiveTestString(Encoding.Unicode, "Test\uDD75Test", 18, "00G3");
            PositiveTestString(Encoding.Unicode, "TestTest\uDD75", 18, "00H3");
            PositiveTestString(Encoding.Unicode, "TestTest\uD803", 18, "00I3");
            PositiveTestString(Encoding.Unicode, "\uDD75", 2, "00J3");
            PositiveTestString(Encoding.Unicode, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", 12, "00K3");
            PositiveTestString(Encoding.Unicode, "\u0130", 2, "00L3");

            PositiveTestString(Encoding.BigEndianUnicode, "TestString", 20, "00A4");
            PositiveTestString(Encoding.BigEndianUnicode, "", 0, "00B4");
            PositiveTestString(Encoding.BigEndianUnicode, "FooBA\u0400R", 14, "00C4");
            PositiveTestString(Encoding.BigEndianUnicode, "\u00C0nima\u0300l", 14, "00D4");
            PositiveTestString(Encoding.BigEndianUnicode, "Test\uD803\uDD75Test", 20, "00E4");
            PositiveTestString(Encoding.BigEndianUnicode, "Test\uD803Test", 18, "00F4");
            PositiveTestString(Encoding.BigEndianUnicode, "Test\uDD75Test", 18, "00G4");
            PositiveTestString(Encoding.BigEndianUnicode, "TestTest\uDD75", 18, "00H4");
            PositiveTestString(Encoding.BigEndianUnicode, "TestTest\uD803", 18, "00I4");
            PositiveTestString(Encoding.BigEndianUnicode, "\uDD75", 2, "00J4");
            PositiveTestString(Encoding.BigEndianUnicode, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", 12, "00K4");
            PositiveTestString(Encoding.BigEndianUnicode, "\u0130", 2, "00L4");

            PositiveTestChars(Encoding.UTF8, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, 10, "00M");
            PositiveTestChars(Encoding.Unicode, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, 20, "00M3");
            PositiveTestChars(Encoding.BigEndianUnicode, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, 20, "00M4");
        }


        // Negative Tests
        [Fact]
        public void NegTests()
        {
            NegativeTestString<ArgumentNullException>(new UTF8Encoding(), null, "00N");
            NegativeTestString<ArgumentNullException>(new UnicodeEncoding(), null, "00N3");
            NegativeTestString<ArgumentNullException>(new UnicodeEncoding(true, false), null, "00N4");

            NegativeTestChars<ArgumentNullException>(new UTF8Encoding(), null, "00O");
            NegativeTestChars<ArgumentNullException>(new UnicodeEncoding(), null, "00O3");
            NegativeTestChars<ArgumentNullException>(new UnicodeEncoding(true, false), null, "00O4");

            NegativeTestChars2<ArgumentNullException>(new UTF8Encoding(), null, 0, 0, "00P");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new char[] { 't' }, -1, 1, "00P");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new char[] { 't' }, 1, -1, "00Q");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new char[] { 't' }, 0, 10, "00R");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new char[] { 't' }, 2, 0, "00S");

            NegativeTestChars2<ArgumentNullException>(new UnicodeEncoding(), null, 0, 0, "00P3");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new char[] { 't' }, -1, 1, "00P3");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new char[] { 't' }, 1, -1, "00Q3");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new char[] { 't' }, 0, 10, "00R3");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new char[] { 't' }, 2, 0, "00S3");

            NegativeTestChars2<ArgumentNullException>(new UnicodeEncoding(true, false), null, 0, 0, "00P4");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new char[] { 't' }, -1, 1, "00P4");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new char[] { 't' }, 1, -1, "00Q4");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new char[] { 't' }, 0, 10, "00R4");
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new char[] { 't' }, 2, 0, "00S4");
        }

        public void PositiveTestString(Encoding enc, string str, int expected, string id)
        {
            int output = enc.GetByteCount(str);
            Assert.Equal(expected, output);
        }

        public void NegativeTestString<T>(Encoding enc, string str, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetByteCount(str);
            });
        }

        public void PositiveTestChars(Encoding enc, char[] chars, int expected, string id)
        {
            int output = enc.GetByteCount(chars);
            Assert.Equal(expected, output);
        }

        public void NegativeTestChars<T>(Encoding enc, char[] str, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetByteCount(str);
            });
        }

        public void NegativeTestChars2<T>(Encoding enc, char[] str, int index, int count, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetByteCount(str, index, count);
            });
        }
    }
}
