// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingGetChars1
    {
        #region Positive Testcases
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
            PositiveTestString(Encoding.UTF8, "\0Te\nst\0\t\0T\u000Fest\0", new byte[] { 0, 84, 101, 10, 115, 116, 0, 9, 0, 84, 15, 101, 115, 116, 0 }, "00F");
        }

        [Fact]
        public void PosTest7()
        {
            PositiveTestString(Encoding.UTF8, "\uFFFDTest\uFFFD\uFFFD\u0130\uFFFDTest\uFFFD", new byte[] { 196, 84, 101, 115, 116, 196, 196, 196, 176, 176, 84, 101, 115, 116, 176 }, "00G");
        }

        [Fact]
        public void PosTest8()
        {
            PositiveTestString(Encoding.GetEncoding("utf-8"), "TestTest", new byte[] { 84, 101, 115, 116, 84, 101, 115, 116 }, "00H");
        }

        [Fact]
        public void PosTest9()
        {
            PositiveTestString(Encoding.GetEncoding("utf-8"), "\uFFFD", new byte[] { 176 }, "00I");
        }

        [Fact]
        public void PosTest10()
        {
            PositiveTestString(Encoding.GetEncoding("utf-8"), "\uFFFD", new byte[] { 196 }, "00J");
        }

        [Fact]
        public void PosTest11()
        {
            PositiveTestString(Encoding.GetEncoding("utf-8"), "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 }, "00K");
        }

        [Fact]
        public void PosTest12()
        {
            PositiveTestString(Encoding.GetEncoding("utf-8"), "\u0130", new byte[] { 196, 176 }, "00L");
        }

        [Fact]
        public void PosTest13()
        {
            PositiveTestString(Encoding.GetEncoding("utf-8"), "\uFFFD\uD803\uDD75\uD803\uDD75\uFFFD\uFFFD", new byte[] { 240, 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 240 }, "0A2");
        }

        [Fact]
        public void PosTest14()
        {
            PositiveTestString(Encoding.Unicode, "TestString\uFFFD", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0, 45 }, "00A3");
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
            PositiveTestString(Encoding.Unicode, "\0Te\nst\0\t\0T\u000Fest\0", new byte[] { 0, 0, 84, 0, 101, 0, 10, 0, 115, 0, 116, 0, 0, 0, 9, 0, 0, 0, 84, 0, 15, 0, 101, 0, 115, 0, 116, 0, 0, 0 }, "00F3");
        }

        [Fact]
        public void PosTest20()
        {
            PositiveTestString(Encoding.GetEncoding("utf-16"), "TestTest", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0 }, "00G3");
        }

        [Fact]
        public void PosTest21()
        {
            PositiveTestString(Encoding.GetEncoding("utf-16"), "TestTest\uFFFD", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 117, 221 }, "00H3");
        }

        [Fact]
        public void PosTest22()
        {
            PositiveTestString(Encoding.GetEncoding("utf-16"), "TestTest\uFFFD", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 3, 216 }, "00I3");
        }

        [Fact]
        public void PosTest23()
        {
            PositiveTestString(Encoding.GetEncoding("utf-16"), "\uFFFD\uFFFD", new byte[] { 3, 216, 84 }, "00J3");
        }

        [Fact]
        public void PosTest24()
        {
            PositiveTestString(Encoding.GetEncoding("utf-16"), "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 3, 216, 117, 221, 3, 216, 117, 221, 3, 216, 117, 221 }, "00K3");
        }

        [Fact]
        public void PosTest25()
        {
            PositiveTestString(Encoding.GetEncoding("utf-16"), "\u0130", new byte[] { 48, 1 }, "00L3");
        }

        [Fact]
        public void PosTest26()
        {
            PositiveTestString(Encoding.GetEncoding("utf-16"), "\uD803\uDD75\uD803\uDD75", new byte[] { 3, 216, 117, 221, 3, 216, 117, 221 }, "0A23");
        }

        [Fact]
        public void PosTest27()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "TestString\uFFFD", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 }, "00A4");
        }

        [Fact]
        public void PosTest28()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "", new byte[] { }, "00B4");
        }

        [Fact]
        public void PosTest29()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "FooBA\u0400R\uFFFD", new byte[] { 0, 70, 0, 111, 0, 111, 0, 66, 0, 65, 4, 0, 0, 82, 70 }, "00C4");
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
            PositiveTestString(Encoding.BigEndianUnicode, "\0Te\nst\0\t\0T\u000Fest\0\uFFFD", new byte[] { 0, 0, 0, 84, 0, 101, 0, 10, 0, 115, 0, 116, 0, 0, 0, 9, 0, 0, 0, 84, 0, 15, 0, 101, 0, 115, 0, 116, 0, 0, 0 }, "00F4");
        }

        [Fact]
        public void PosTest33()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "TestTest", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116 }, "00G4");
        }

        [Fact]
        public void PosTest34()
        {
            PositiveTestString(Encoding.BigEndianUnicode, "TestTest\uFFFD", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 221, 117 }, "00H4");
        }

        [Fact]
        public void PosTest35()
        {
            PositiveTestString(Encoding.GetEncoding("UTF-16BE"), "TestTest\uFFFD", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 216, 3 }, "00I4");
        }

        [Fact]
        public void PosTest36()
        {
            PositiveTestString(Encoding.GetEncoding("UTF-16BE"), "\uFFFD\uFFFD", new byte[] { 216, 3, 48 }, "00J4");
        }

        [Fact]
        public void PosTest37()
        {
            PositiveTestString(Encoding.GetEncoding("UTF-16BE"), "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 216, 3, 221, 117, 216, 3, 221, 117, 216, 3, 221, 117 }, "00K4");
        }

        [Fact]
        public void PosTest38()
        {
            PositiveTestString(Encoding.GetEncoding("UTF-16BE"), "\u0130", new byte[] { 1, 48 }, "00L4");
        }

        [Fact]
        public void PosTest39()
        {
            PositiveTestString(Encoding.GetEncoding("UTF-16BE"), "\uD803\uDD75\uD803\uDD75", new byte[] { 216, 3, 221, 117, 216, 3, 221, 117 }, "0A24");
        }
        #endregion

        #region Negative Testcases
        [Fact]
        public void NegTest1()
        {
            NegativeTestChars<ArgumentNullException>(new UTF8Encoding(), null, "00O");
        }

        [Fact]
        public void NegTest2()
        {
            NegativeTestChars<ArgumentNullException>(new UnicodeEncoding(), null, "00O3");
        }

        [Fact]
        public void NegTest3()
        {
            NegativeTestChars<ArgumentNullException>(new UnicodeEncoding(true, false), null, "00O4");
        }

        [Fact]
        public void NegTest4()
        {
            NegativeTestChars2<ArgumentNullException>(new UTF8Encoding(), null, 0, 0, "00P");
        }

        [Fact]
        public void NegTest5()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new byte[] { 0, 0 }, -1, 1, "00P");
        }

        [Fact]
        public void NegTest6()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new byte[] { 0, 0 }, 1, -1, "00Q");
        }

        [Fact]
        public void NegTest7()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new byte[] { 0, 0 }, 0, 10, "00R");
        }

        [Fact]
        public void NegTest8()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UTF8Encoding(), new byte[] { 0, 0 }, 3, 0, "00S");
        }

        [Fact]
        public void NegTest9()
        {
            NegativeTestChars2<ArgumentNullException>(new UnicodeEncoding(), null, 0, 0, "00P3");
        }

        [Fact]
        public void NegTest10()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new byte[] { 0, 0 }, -1, 1, "00P3");
        }

        [Fact]
        public void NegTest11()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new byte[] { 0, 0 }, 1, -1, "00Q3");
        }

        [Fact]
        public void NegTest12()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new byte[] { 0, 0 }, 0, 10, "00R3");
        }

        [Fact]
        public void NegTest13()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(), new byte[] { 0, 0 }, 3, 0, "00S3");
        }

        [Fact]
        public void NegTest14()
        {
            NegativeTestChars2<ArgumentNullException>(new UnicodeEncoding(true, false), null, 0, 0, "00P4");
        }

        [Fact]
        public void NegTest15()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new byte[] { 0, 0 }, -1, 1, "00P4");
        }

        [Fact]
        public void NegTest16()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new byte[] { 0, 0 }, 1, -1, "00Q4");
        }

        [Fact]
        public void NegTest17()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new byte[] { 0, 0 }, 0, 10, "00R4");
        }

        [Fact]
        public void NegTest18()
        {
            NegativeTestChars2<ArgumentOutOfRangeException>(new UnicodeEncoding(true, false), new byte[] { 0, 0 }, 3, 0, "00S4");
        }

        private static char[] s_output = new char[20];
        [Fact]
        public void NegTest19()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.UTF8, null, 0, 0, s_output, 0, "00T");
        }

        [Fact]
        public void NegTest20()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.UTF8, new byte[] { 0, 0 }, 0, 0, null, 0, "00U");
        }

        [Fact]
        public void NegTest21()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new byte[] { 0, 0 }, -1, 0, s_output, 0, "00V");
        }

        [Fact]
        public void NegTest22()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new byte[] { 0, 0 }, 0, 0, s_output, -1, "00W");
        }

        [Fact]
        public void NegTest23()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new byte[] { 0, 0 }, 3, 0, s_output, 0, "00X");
        }

        [Fact]
        public void NegTest24()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new byte[] { 0, 0 }, 0, 0, s_output, 21, "00Y");
        }

        [Fact]
        public void NegTest25()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new byte[] { 0, 0 }, 0, 10, s_output, 0, "00Z");
        }

        [Fact]
        public void NegTest26()
        {
            NegativeTestChars3<ArgumentException>(Encoding.UTF8, new byte[] { 0, 0 }, 0, 2, s_output, 20, "0A0");
        }

        [Fact]
        public void NegTest27()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.UTF8, new byte[] { 0, 0 }, 0, -1, s_output, 0, "0A1");
        }

        [Fact]
        public void NegTest28()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.Unicode, null, 0, 0, s_output, 0, "00T3");
        }

        [Fact]
        public void NegTest29()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.Unicode, new byte[] { 0, 0 }, 0, 0, null, 0, "00U3");
        }

        [Fact]
        public void NegTest30()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new byte[] { 0, 0 }, -1, 0, s_output, 0, "00V3");
        }

        [Fact]
        public void NegTest31()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new byte[] { 0, 0 }, 0, 0, s_output, -1, "00W3");
        }

        [Fact]
        public void NegTest32()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new byte[] { 0, 0 }, 3, 0, s_output, 0, "00X3");
        }

        [Fact]
        public void NegTest33()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new byte[] { 0, 0 }, 0, 0, s_output, 21, "00Y3");
        }

        [Fact]
        public void NegTest34()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new byte[] { 0, 0 }, 0, 10, s_output, 0, "00Z3");
        }

        [Fact]
        public void NegTest35()
        {
            NegativeTestChars3<ArgumentException>(Encoding.Unicode, new byte[] { 0, 0 }, 0, 2, s_output, 20, "0A03");
        }

        [Fact]
        public void NegTest36()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.Unicode, new byte[] { 0, 0 }, 0, -1, s_output, 0, "0A13");
        }

        [Fact]
        public void NegTest37()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.BigEndianUnicode, null, 0, 0, s_output, 0, "00T4");
        }

        [Fact]
        public void NegTest38()
        {
            NegativeTestChars3<ArgumentNullException>(Encoding.BigEndianUnicode, new byte[] { 0, 0 }, 0, 0, null, 0, "00U4");
        }

        [Fact]
        public void NegTest39()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new byte[] { 0, 0 }, -1, 0, s_output, 0, "00V4");
        }

        [Fact]
        public void NegTest40()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new byte[] { 0, 0 }, 0, 0, s_output, -1, "00W4");
        }

        [Fact]
        public void NegTest41()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new byte[] { 0, 0 }, 3, 0, s_output, 0, "00X4");
        }

        [Fact]
        public void NegTest42()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new byte[] { 0, 0 }, 0, 0, s_output, 21, "00Y4");
        }

        [Fact]
        public void NegTest43()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new byte[] { 0, 0 }, 0, 10, s_output, 0, "00Z4");
        }

        [Fact]
        public void NegTest44()
        {
            NegativeTestChars3<ArgumentException>(Encoding.BigEndianUnicode, new byte[] { 0, 0 }, 0, 2, s_output, 20, "0A04");
        }

        [Fact]
        public void NegTest45()
        {
            NegativeTestChars3<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, new byte[] { 0, 0 }, 0, -1, s_output, 0, "0A14");
        }
        #endregion
        public void PositiveTestString(Encoding enc, string expected, byte[] bytes, string id)
        {
            char[] chars = enc.GetChars(bytes);
            string str = new string(chars);
            Assert.Equal(expected, str);
        }

        public void NegativeTestChars<T>(Encoding enc, byte[] bytes, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                char[] chars = enc.GetChars(bytes);
                string str = new string(chars);
            });
        }

        public void NegativeTestChars2<T>(Encoding enc, byte[] bytes, int index, int count, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                char[] chars = enc.GetChars(bytes, index, count);
                string str = new string(chars);
            });
        }

        public void NegativeTestChars3<T>(Encoding enc, byte[] bytes, int index, int count, char[] chars, int bIndex, string id)
            where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetChars(bytes, index, count, chars, bIndex);
                string str = new string(chars);
            });
        }
    }
}
