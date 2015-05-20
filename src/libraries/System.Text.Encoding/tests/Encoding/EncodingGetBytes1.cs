// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingGetBytes1
    {
        #region Positive Test Cases
        [Fact]
        public void PosTest1()
        {
            PositiveTestString(Encoding.UTF8, "TestString", new byte[] { 84, 101, 115, 116, 83, 116, 114, 105, 110, 103 }, "00A");
        }

        [Fact]
        public void PosTest2()
        {
            PositiveTestString(Encoding.UTF8, "", new byte[] { }, "00B");
        }

        [Fact]
        public void PosTest3()
        {
            PositiveTestString(Encoding.UTF8, "FooBA\u0400R", new byte[] { 70, 111, 111, 66, 65, 208, 128, 82 }, "00C");
        }

        [Fact]
        public void PosTest4()
        {
            PositiveTestString(Encoding.UTF8, "\u00C0nima\u0300l", new byte[] { 195, 128, 110, 105, 109, 97, 204, 128, 108 }, "00D");
        }

        [Fact]
        public void PosTest5()
        {
            PositiveTestString(Encoding.UTF8, "Test\uD803\uDD75Test", new byte[] { 84, 101, 115, 116, 240, 144, 181, 181, 84, 101, 115, 116 }, "00E");
        }

        [Fact]
        public void PosTest6()
        {
            PositiveTestString(Encoding.UTF8, "Test\uD803Test", new byte[] { 84, 101, 115, 116, 239, 191, 189, 84, 101, 115, 116 }, "00F");
        }

        [Fact]
        public void PosTest7()
        {
            PositiveTestString(Encoding.UTF8, "Test\uDD75Test", new byte[] { 84, 101, 115, 116, 239, 191, 189, 84, 101, 115, 116 }, "00G");
        }

        [Fact]
        public void PosTest8()
        {
            PositiveTestString(Encoding.UTF8, "TestTest\uDD75", new byte[] { 84, 101, 115, 116, 84, 101, 115, 116, 239, 191, 189 }, "00H");
        }

        [Fact]
        public void PosTest9()
        {
            PositiveTestString(Encoding.UTF8, "TestTest\uD803", new byte[] { 84, 101, 115, 116, 84, 101, 115, 116, 239, 191, 189 }, "00I");
        }

        [Fact]
        public void PosTest10()
        {
            PositiveTestString(Encoding.UTF8, "\uDD75", new byte[] { 239, 191, 189 }, "00J");
        }

        [Fact]
        public void PosTest11()
        {
            PositiveTestString(Encoding.UTF8, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 }, "00K");
        }

        [Fact]
        public void PosTest12()
        {
            PositiveTestString(Encoding.UTF8, "\u0130", new byte[] { 196, 176 }, "00L");
        }

        [Fact]
        public void PosTest13()
        {
            PositiveTestString(Encoding.UTF8, "\uDD75\uDD75\uD803\uDD75\uDD75\uDD75\uDD75\uD803\uD803\uD803\uDD75\uDD75\uDD75\uDD75", new byte[] { 239, 191, 189, 239, 191, 189, 240, 144, 181, 181, 239, 191, 189, 239, 191, 189, 239, 191, 189, 239, 191, 189, 239, 191, 189, 240, 144, 181, 181, 239, 191, 189, 239, 191, 189, 239, 191, 189 }, "0A2");
        }

        [Fact]
        public void PosTest14()
        {
            PositiveTestString(Encoding.Unicode, "TestString", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 }, "00A3");
        }

        [Fact]
        public void PosTest15()
        {
            PositiveTestString(Encoding.Unicode, "", new byte[] { }, "00B3");
        }

        [Fact]
        public void PosTest16()
        {
            PositiveTestString(Encoding.Unicode, "FooBA\u0400R", new byte[] { 70, 0, 111, 0, 111, 0, 66, 0, 65, 0, 0, 4, 82, 0 }, "00C3");
        }

        [Fact]
        public void PosTest17()
        {
            PositiveTestString(Encoding.Unicode, "\u00C0nima\u0300l", new byte[] { 192, 0, 110, 0, 105, 0, 109, 0, 97, 0, 0, 3, 108, 0 }, "00D3");
        }

        [Fact]
        public void PosTest18()
        {
            PositiveTestString(Encoding.Unicode, "Test\uD803\uDD75Test", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 3, 216, 117, 221, 84, 0, 101, 0, 115, 0, 116, 0 }, "00E3");
        }

        [Fact]
        public void PosTest19()
        {
            PositiveTestString(Encoding.Unicode, "Test\uD803Test", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 253, 255, 84, 0, 101, 0, 115, 0, 116, 0 }, "00F3");
        }

        [Fact]
        public void PosTest20()
        {
            PositiveTestString(Encoding.Unicode, "Test\uDD75Test", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 253, 255, 84, 0, 101, 0, 115, 0, 116, 0, }, "00G3");
        }

        [Fact]
        public void PosTest21()
        {
            PositiveTestString(Encoding.Unicode, "TestTest\uDD75", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 253, 255 }, "00H3");
        }

        [Fact]
        public void PosTest22()
        {
            PositiveTestString(Encoding.Unicode, "TestTest\uD803", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 253, 255 }, "00I3");
        }

        [Fact]
        public void PosTest23()
        {
            PositiveTestString(Encoding.Unicode, "\uDD75", new byte[] { 253, 255 }, "00J3");
        }

        [Fact]
        public void PosTest24()
        {
            PositiveTestString(Encoding.Unicode, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 3, 216, 117, 221, 3, 216, 117, 221, 3, 216, 117, 221 }, "00K3");
        }

        [Fact]
        public void PosTest25()
        {
            PositiveTestString(Encoding.Unicode, "\u0130", new byte[] { 48, 1 }, "00L3");
        }

        [Fact]
        public void PosTest26()
        {
            PositiveTestString(Encoding.Unicode, "\uDD75\uDD75\uD803\uDD75\uDD75\uDD75\uDD75\uD803\uD803\uD803\uDD75\uDD75\uDD75\uDD75", new byte[] { 253, 255, 253, 255, 3, 216, 117, 221, 253, 255, 253, 255, 253, 255, 253, 255, 253, 255, 3, 216, 117, 221, 253, 255, 253, 255, 253, 255 }, "0A23");
        }

        [Fact]
        public void PosTest27()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "TestString", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103 }, "00A4");
        }

        [Fact]
        public void PosTest28()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "", new byte[] { }, "00B4");
        }

        [Fact]
        public void PosTest29()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "FooBA\u0400R", new byte[] { 0, 70, 0, 111, 0, 111, 0, 66, 0, 65, 4, 0, 0, 82 }, "00C4");
        }

        [Fact]
        public void PosTest30()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "\u00C0nima\u0300l", new byte[] { 0, 192, 0, 110, 0, 105, 0, 109, 0, 97, 3, 0, 0, 108 }, "00D4");
        }

        [Fact]
        public void PosTest31()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "Test\uD803\uDD75Test", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 216, 3, 221, 117, 0, 84, 0, 101, 0, 115, 0, 116 }, "00E4");
        }

        [Fact]
        public void PosTest32()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "Test\uD803Test", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 255, 253, 0, 84, 0, 101, 0, 115, 0, 116 }, "00F4");
        }

        [Fact]
        public void PosTest33()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "Test\uDD75Test", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 255, 253, 0, 84, 0, 101, 0, 115, 0, 116 }, "00G4");
        }

        [Fact]
        public void PosTest34()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "TestTest\uDD75", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 255, 253 }, "00H4");
        }

        [Fact]
        public void PosTest35()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "TestTest\uD803", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 255, 253 }, "00I4");
        }

        [Fact]
        public void PosTest36()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "\uDD75", new byte[] { 255, 253 }, "00J4");
        }

        [Fact]
        public void PosTest37()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 216, 3, 221, 117, 216, 3, 221, 117, 216, 3, 221, 117 }, "00K4");
        }

        [Fact]
        public void PosTest38()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "\u0130", new byte[] { 1, 48 }, "00L4");
        }

        [Fact]
        public void PosTest39()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "\uDD75\uDD75\uD803\uDD75\uDD75\uDD75\uDD75\uD803\uD803\uD803\uDD75\uDD75\uDD75\uDD75", new byte[] { 255, 253, 255, 253, 216, 3, 221, 117, 255, 253, 255, 253, 255, 253, 255, 253, 255, 253, 216, 3, 221, 117, 255, 253, 255, 253, 255, 253 }, "0A24");
        }

        [Fact]
        public void PosTest40()
        {
            PositiveTestChars(Encoding.UTF8, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, new byte[] { 84, 101, 115, 116, 83, 116, 114, 105, 110, 103 }, "00M");
        }

        [Fact]
        public void PosTest41()
        {
            PositiveTestChars(Encoding.Unicode, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 }, "00M3");
        }

        [Fact]
        public void PosTest42()
        {
            PositiveTestChars(Encoding.BigEndianUnicode, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103 }, "00M4");
        }
        #endregion
        #region Negative Test Cases
        [Fact]
        public void NegTest1()
        {
            NegativeTestString<ArgumentNullException>(new UTF8Encoding(), null, "00N");
        }

        [Fact]
        public void NegTest2()
        {
            NegativeTestString<ArgumentNullException>(new UnicodeEncoding(), null, "00N3");
        }

        [Fact]
        public void NegTest3()
        {
            NegativeTestString<ArgumentNullException>(new UnicodeEncoding(true, false), null, "00N4");
        }

        [Fact]
        public void NegTest4()
        {
            NegativeTestChars<ArgumentNullException>(new UTF8Encoding(), null, "00O");
        }

        [Fact]
        public void NegTest5()
        {
            NegativeTestChars<ArgumentNullException>(new UnicodeEncoding(), null, "00O3");
        }

        [Fact]
        public void NegTest6()
        {
            NegativeTestChars<ArgumentNullException>(new UnicodeEncoding(true, false), null, "00O4");
        }

        [Fact]
        public void NegTest7()
        {
            NegativeTestChars2<ArgumentNullException>(new UTF8Encoding(), null, 0, 0, "00P");
        }

        [Fact]
        public void NegTest8()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new char[] { 't' }, -1, 1, "00P");
        }

        [Fact]
        public void NegTest9()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new char[] { 't' }, 1, -1, "00Q");
        }

        [Fact]
        public void NegTest10()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new char[] { 't' }, 0, 10, "00R");
        }

        [Fact]
        public void NegTest11()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new char[] { 't' }, 2, 0, "00S");
        }

        [Fact]
        public void NegTest12()
        {
            NegativeTestChars2<ArgumentNullException>(new UnicodeEncoding(), null, 0, 0, "00P3");
        }

        [Fact]
        public void NegTest13()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new char[] { 't' }, -1, 1, "00P3");
        }

        [Fact]
        public void NegTest14()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new char[] { 't' }, 1, -1, "00Q3");
        }

        [Fact]
        public void NegTest15()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new char[] { 't' }, 0, 10, "00R3");
        }

        [Fact]
        public void NegTest16()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new char[] { 't' }, 2, 0, "00S3");
        }

        [Fact]
        public void NegTest17()
        {
            NegativeTestChars2<ArgumentNullException>(new UnicodeEncoding(true, false), null, 0, 0, "00P4");
        }

        [Fact]
        public void NegTest18()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new char[] { 't' }, -1, 1, "00P4");
        }

        [Fact]
        public void NegTest19()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new char[] { 't' }, 1, -1, "00Q4");
        }

        [Fact]
        public void NegTest20()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new char[] { 't' }, 0, 10, "00R4");
        }

        [Fact]
        public void NegTest21()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new char[] { 't' }, 2, 0, "00S4");
        }

        private static byte[] s_output = new byte[20];

        [Fact]
        public void NegTest22()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.UTF8, null, 0, 0, s_output, 0, "00T");
        }

        [Fact]
        public void NegTest23()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.UTF8, new char[] { 't' }, 0, 0, null, 0, "00U");
        }

        [Fact]
        public void NegTest24()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new char[] { 't' }, -1, 0, s_output, 0, "00V");
        }

        [Fact]
        public void NegTest25()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new char[] { 't' }, 0, 0, s_output, -1, "00W");
        }

        [Fact]
        public void NegTest26()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new char[] { 't' }, 2, 0, s_output, 0, "00X");
        }

        [Fact]
        public void NegTest27()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new char[] { 't' }, 0, 0, s_output, 21, "00Y");
        }

        [Fact]
        public void NegTest28()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new char[] { 't' }, 0, 10, s_output, 0, "00Z");
        }

        [Fact]
        public void NegTest29()
        {
            NegativeTestChars3<ArgumentException>(Encoding.UTF8, new char[] { 't' }, 0, 1, s_output, 20, "0A0");
        }

        [Fact]
        public void NegTest30()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new char[] { 't' }, 0, -1, s_output, 0, "0A1");
        }

        [Fact]
        public void NegTest31()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.Unicode, null, 0, 0, s_output, 0, "00T3");
        }

        [Fact]
        public void NegTest32()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.Unicode, new char[] { 't' }, 0, 0, null, 0, "00U3");
        }

        [Fact]
        public void NegTest33()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new char[] { 't' }, -1, 0, s_output, 0, "00V3");
        }

        [Fact]
        public void NegTest34()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new char[] { 't' }, 0, 0, s_output, -1, "00W3");
        }

        [Fact]
        public void NegTest35()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new char[] { 't' }, 2, 0, s_output, 0, "00X3");
        }

        [Fact]
        public void NegTest36()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new char[] { 't' }, 0, 0, s_output, 21, "00Y3");
        }

        [Fact]
        public void NegTest37()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new char[] { 't' }, 0, 10, s_output, 0, "00Z3");
        }

        [Fact]
        public void NegTest38()
        {
            NegativeTestChars3<ArgumentException>(Encoding.Unicode, new char[] { 't' }, 0, 1, s_output, 20, "0A03");
        }

        [Fact]
        public void NegTest39()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new char[] { 't' }, 0, -1, s_output, 0, "0A13");
        }

        [Fact]
        public void NegTest40()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.BigEndianUnicode, null, 0, 0, s_output, 0, "00T4");
        }

        [Fact]
        public void NegTest41()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 0, null, 0, "00U4");
        }

        [Fact]
        public void NegTest42()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new char[] { 't' }, -1, 0, s_output, 0, "00V4");
        }

        [Fact]
        public void NegTest43()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 0, s_output, -1, "00W4");
        }

        [Fact]
        public void NegTest44()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new char[] { 't' }, 2, 0, s_output, 0, "00X4");
        }

        [Fact]
        public void NegTest45()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 0, s_output, 21, "00Y4");
        }

        [Fact]
        public void NegTest46()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 10, s_output, 0, "00Z4");
        }

        [Fact]
        public void NegTest47()
        {
            NegativeTestChars3<ArgumentException>(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 1, s_output, 20, "0A04");
        }

        [Fact]
        public void NegTest48()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new char[] { 't' }, 0, -1, s_output, 0, "0A14");
        }

        [Fact]
        public void NegTest49()
        {
            NegativeTestString1<ArgumentNullException>(Encoding.UTF8, null, 0, 0, s_output, 0, "00Ta");
        }

        [Fact]
        public void NegTest50()
        {
            NegativeTestString1<ArgumentNullException>(Encoding.UTF8, "t", 0, 0, null, 0, "00Ua");
        }

        [Fact]
        public void NegTest51()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.UTF8, "t", -1, 0, s_output, 0, "00Va");
        }

        [Fact]
        public void NegTest52()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.UTF8, "t", 0, 0, s_output, -1, "00Wa");
        }

        [Fact]
        public void NegTest53()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.UTF8, "t", 2, 0, s_output, 0, "00Xa");
        }

        [Fact]
        public void NegTest54()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.UTF8, "t", 0, 0, s_output, 21, "00Ya");
        }

        [Fact]
        public void NegTest55()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.UTF8, "t", 0, 10, s_output, 0, "00Za");
        }

        [Fact]
        public void NegTest56()
        {
            NegativeTestString1<ArgumentException>(Encoding.UTF8, "t", 0, 1, s_output, 20, "0A0a");
        }

        [Fact]
        public void NegTest57()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.UTF8, "t", 0, -1, s_output, 0, "0A1a");
        }

        [Fact]
        public void NegTest58()
        {
            NegativeTestString1<ArgumentNullException>(Encoding.Unicode, null, 0, 0, s_output, 0, "00T3a");
        }

        [Fact]
        public void NegTest59()
        {
            NegativeTestString1<ArgumentNullException>(Encoding.Unicode, "t", 0, 0, null, 0, "00U3a");
        }

        [Fact]
        public void NegTest60()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.Unicode, "t", -1, 0, s_output, 0, "00V3a");
        }

        [Fact]
        public void NegTest61()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.Unicode, "t", 0, 0, s_output, -1, "00W3a");
        }

        [Fact]
        public void NegTest62()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.Unicode, "t", 2, 0, s_output, 0, "00X3a");
        }

        [Fact]
        public void NegTest63()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.Unicode, "t", 0, 0, s_output, 21, "00Y3a");
        }

        [Fact]
        public void NegTest64()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.Unicode, "t", 0, 10, s_output, 0, "00Z3a");
        }

        [Fact]
        public void NegTest65()
        {
            NegativeTestString1<ArgumentException>(Encoding.Unicode, "t", 0, 1, s_output, 20, "0A03a");
        }

        [Fact]
        public void NegTest66()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.Unicode, "t", 0, -1, s_output, 0, "0A13a");
        }

        [Fact]
        public void NegTest67()
        {
            NegativeTestString1<ArgumentNullException>(Encoding.BigEndianUnicode, null, 0, 0, s_output, 0, "00T4a");
        }

        [Fact]
        public void NegTest68()
        {
            NegativeTestString1<ArgumentNullException>(Encoding.BigEndianUnicode, "t", 0, 0, null, 0, "00U4a");
        }

        [Fact]
        public void NegTest69()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, "t", -1, 0, s_output, 0, "00V4a");
        }

        [Fact]
        public void NegTest70()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, "t", 0, 0, s_output, -1, "00W4a");
        }

        [Fact]
        public void NegTest71()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, "t", 2, 0, s_output, 0, "00X4a");
        }

        [Fact]
        public void NegTest72()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, "t", 0, 0, s_output, 21, "00Y4a");
        }

        [Fact]
        public void NegTest73()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, "t", 0, 10, s_output, 0, "00Z4a");
        }

        [Fact]
        public void NegTest74()
        {
            NegativeTestString1<ArgumentException>(Encoding.BigEndianUnicode, "t", 0, 1, s_output, 20, "0A04a");
        }

        [Fact]
        public void NegTest75()
        {
            NegativeTestString1<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, "t", 0, -1, s_output, 0, "0A14a");
        }
        #endregion
        public void PositiveTestString(Encoding enc, string str, byte[] expected, string id)
        {
            byte[] bytes = enc.GetBytes(str);
            Assert.True(CompareBytes(bytes, expected));
        }

        public void NegativeTestString<T>(Encoding enc, string str, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                byte[] bytes = enc.GetBytes(str);
            });
        }

        public void PositiveTestChars(Encoding enc, char[] chars, byte[] expected, string id)
        {
            byte[] bytes = enc.GetBytes(chars);
            Assert.True(CompareBytes(bytes, expected));
        }

        public void NegativeTestChars<T>(Encoding enc, char[] str, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                byte[] bytes = enc.GetBytes(str);
            });
        }

        public void NegativeTestChars2<T>(Encoding enc, char[] str, int index, int count, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                byte[] bytes = enc.GetBytes(str, index, count);
            });
        }

        public void NegativeTestChars3<T>(Encoding enc, char[] str, int index, int count, byte[] bytes, int bIndex, string id)
            where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetBytes(str, index, count, bytes, bIndex);
            });
        }

        public void NegativeTestString1<T>(Encoding enc, string str, int index, int count, byte[] bytes, int bIndex, string id)
            where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetBytes(str, index, count, bytes, bIndex);
            });
        }

        public bool CompareBytes(byte[] arr1, byte[] arr2)
        {
            if (arr1 == null) return (arr2 == null);
            if (arr2 == null) return false;

            if (arr1.Length != arr2.Length) return false;

            for (int i = 0; i < arr1.Length; i++) if (arr1[i] != arr2[i]) return false;

            return true;
        }
    }
}
