// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingGetCharCount
    {
        #region Positive Testcases
        [Fact]
        public void PosTest1()
        {
            PositiveTestString(Encoding.UTF8, 10, new byte[] { 84, 101, 115, 116, 83, 116, 114, 105, 110, 103 }, "00A");
        }

        [Fact]
        public void PosTest2()
        {
            PositiveTestString(Encoding.UTF8, 0, new byte[] { }, "00B");
        }

        [Fact]
        public void PosTest3()
        {
            PositiveTestString(Encoding.UTF8, 7, new byte[] { 70, 111, 111, 66, 65, 208, 128, 82 }, "00C");
        }

        [Fact]
        public void PosTest4()
        {
            PositiveTestString(Encoding.UTF8, 7, new byte[] { 195, 128, 110, 105, 109, 97, 204, 128, 108 }, "00D");
        }

        [Fact]
        public void PosTest5()
        {
            PositiveTestString(Encoding.UTF8, 10, new byte[] { 84, 101, 115, 116, 240, 144, 181, 181, 84, 101, 115, 116 }, "00E");
        }

        [Fact]
        public void PosTest6()
        {
            PositiveTestString(Encoding.UTF8, 15, new byte[] { 0, 84, 101, 10, 115, 116, 0, 9, 0, 84, 15, 101, 115, 116, 0 }, "00F");
        }

        [Fact]
        public void PosTest7()
        {
            PositiveTestString(Encoding.UTF8, 14, new byte[] { 196, 84, 101, 115, 116, 196, 196, 196, 176, 176, 84, 101, 115, 116, 176 }, "00G");
        }

        [Fact]
        public void PosTest8()
        {
            PositiveTestString(Encoding.UTF8, 8, new byte[] { 84, 101, 115, 116, 84, 101, 115, 116 }, "00H");
        }

        [Fact]
        public void PosTest9()
        {
            PositiveTestString(Encoding.UTF8, 1, new byte[] { 176 }, "00I");
        }

        [Fact]
        public void PosTest10()
        {
            PositiveTestString(Encoding.UTF8, 1, new byte[] { 196 }, "00J");
        }

        [Fact]
        public void PosTest11()
        {
            PositiveTestString(Encoding.UTF8, 6, new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 }, "00K");
        }

        [Fact]
        public void PosTest12()
        {
            PositiveTestString(Encoding.UTF8, 1, new byte[] { 196, 176 }, "00L");
        }

        [Fact]
        public void PosTest13()
        {
            PositiveTestString(Encoding.UTF8, 7, new byte[] { 240, 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 240 }, "0A2");
        }

        [Fact]
        public void PosTest14()
        {
            PositiveTestString(Encoding.Unicode, 11, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0, 45 }, "00A3");
        }

        [Fact]
        public void PosTest15()
        {
            PositiveTestString(Encoding.Unicode, 0, new byte[] { }, "00B3");
        }

        [Fact]
        public void PosTest16()
        {
            PositiveTestString(Encoding.Unicode, 7, new byte[] { 70, 0, 111, 0, 111, 0, 66, 0, 65, 0, 0, 4, 82, 0 }, "00C3");
        }

        [Fact]
        public void PosTest17()
        {
            PositiveTestString(Encoding.Unicode, 7, new byte[] { 192, 0, 110, 0, 105, 0, 109, 0, 97, 0, 0, 3, 108, 0 }, "00D3");
        }

        [Fact]
        public void PosTest18()
        {
            PositiveTestString(Encoding.Unicode, 10, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 3, 216, 117, 221, 84, 0, 101, 0, 115, 0, 116, 0 }, "00E3");
        }

        [Fact]
        public void PosTest19()
        {
            PositiveTestString(Encoding.Unicode, 15, new byte[] { 0, 0, 84, 0, 101, 0, 10, 0, 115, 0, 116, 0, 0, 0, 9, 0, 0, 0, 84, 0, 15, 0, 101, 0, 115, 0, 116, 0, 0, 0 }, "00F3");
        }

        [Fact]
        public void PosTest20()
        {
            PositiveTestString(Encoding.Unicode, 8, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0 }, "00G3");
        }

        [Fact]
        public void PosTest21()
        {
            PositiveTestString(Encoding.Unicode, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 117, 221 }, "00H3");
        }

        [Fact]
        public void PosTest22()
        {
            PositiveTestString(Encoding.Unicode, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 3, 216 }, "00I3");
        }

        [Fact]
        public void PosTest23()
        {
            PositiveTestString(Encoding.Unicode, 2, new byte[] { 3, 216, 84 }, "00J3");
        }

        [Fact]
        public void PosTest24()
        {
            PositiveTestString(Encoding.Unicode, 6, new byte[] { 3, 216, 117, 221, 3, 216, 117, 221, 3, 216, 117, 221 }, "00K3");
        }

        [Fact]
        public void PosTest25()
        {
            PositiveTestString(Encoding.Unicode, 1, new byte[] { 48, 1 }, "00L3");
        }

        [Fact]
        public void PosTest26()
        {
            PositiveTestString(Encoding.Unicode, 4, new byte[] { 3, 216, 117, 221, 3, 216, 117, 221 }, "0A23");
        }

        [Fact]
        public void PosTest27()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 11, new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 }, "00A4");
        }

        [Fact]
        public void PosTest28()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 0, new byte[] { }, "00B4");
        }

        [Fact]
        public void PosTest29()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 8, new byte[] { 0, 70, 0, 111, 0, 111, 0, 66, 0, 65, 4, 0, 0, 82, 70 }, "00C4");
        }

        [Fact]
        public void PosTest30()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 7, new byte[] { 0, 192, 0, 110, 0, 105, 0, 109, 0, 97, 3, 0, 0, 108 }, "00D4");
        }

        [Fact]
        public void PosTest31()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 10, new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 216, 3, 221, 117, 0, 84, 0, 101, 0, 115, 0, 116 }, "00E4");
        }

        [Fact]
        public void PosTest32()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 16, new byte[] { 0, 0, 0, 84, 0, 101, 0, 10, 0, 115, 0, 116, 0, 0, 0, 9, 0, 0, 0, 84, 0, 15, 0, 101, 0, 115, 0, 116, 0, 0, 0 }, "00F4");
        }

        [Fact]
        public void PosTest33()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 8, new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116 }, "00G4");
        }

        [Fact]
        public void PosTest34()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 9, new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 221, 117 }, "00H4");
        }

        [Fact]
        public void PosTest35()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 9, new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 216, 3 }, "00I4");
        }

        [Fact]
        public void PosTest36()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 2, new byte[] { 216, 3, 48 }, "00J4");
        }

        [Fact]
        public void PosTest37()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 6, new byte[] { 216, 3, 221, 117, 216, 3, 221, 117, 216, 3, 221, 117 }, "00K4");
        }

        [Fact]
        public void PosTest38()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 1, new byte[] { 1, 48 }, "00L4");
        }

        [Fact]
        public void PosTest39()
        {
            PositiveTestString(Encoding.BigEndianUnicode, 4, new byte[] { 216, 3, 221, 117, 216, 3, 221, 117 }, "0A24");
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
        #endregion

        public void PositiveTestString(Encoding enc, int expected, byte[] bytes, string id)
        {
            int output = enc.GetCharCount(bytes);
            Assert.Equal(expected, output);
        }

        public void NegativeTestChars<T>(Encoding enc, byte[] bytes, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetCharCount(bytes);
            });
        }

        public void NegativeTestChars2<T>(Encoding enc, byte[] bytes, int index, int count, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetCharCount(bytes, index, count);
            });
        }
    }
}
