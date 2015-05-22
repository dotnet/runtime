// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingGetMaxByteCount
    {
        #region Positive Testcases
        [Fact]
        public void PosTest1()
        {
            PositiveTest(Encoding.UTF8, 0, 3, "00A");
        }

        [Fact]
        public void PosTest2()
        {
            PositiveTest(Encoding.UTF8, 1, 6, "00B");
        }

        [Fact]
        public void PosTest3()
        {
            PositiveTest(Encoding.UTF8, 100, 303, "00C");
        }

        [Fact]
        public void PosTest4()
        {
            PositiveTest(Encoding.Unicode, 0, 2, "00A3");
        }

        [Fact]
        public void PosTest5()
        {
            PositiveTest(Encoding.Unicode, 1, 4, "00B3");
        }

        [Fact]
        public void PosTest6()
        {
            PositiveTest(Encoding.Unicode, 100, 202, "00C3");
        }

        [Fact]
        public void PosTest7()
        {
            PositiveTest(Encoding.BigEndianUnicode, 0, 2, "00A4");
        }

        [Fact]
        public void PosTest8()
        {
            PositiveTest(Encoding.BigEndianUnicode, 1, 4, "00B4");
        }

        [Fact]
        public void PosTest9()
        {
            PositiveTest(Encoding.BigEndianUnicode, 100, 202, "00C4");
        }

        [Fact]
        public void PosTest10()
        {
            PositiveTest(Encoding.UTF8, 0, 3, "00A5");
        }

        [Fact]
        public void PosTest11()
        {
            PositiveTest(Encoding.UTF8, 1, 6, "00B5");
        }

        [Fact]
        public void PosTest12()
        {
            PositiveTest(Encoding.UTF8, 100, 303, "00C5");
        }
        #endregion

        #region Negative Testcases
        [Fact]
        public void NegTest1()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.UTF8, -1, "00D6");
        }

        [Fact]
        public void NegTest2()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.UTF8, Int32.MaxValue, "00E6");
        }

        [Fact]
        public void NegTest3()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.Unicode, Int32.MaxValue, "00E9");
        }

        [Fact]
        public void NegTest4()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.Unicode, -1, "00D9");
        }

        [Fact]
        public void NegTest5()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, -1, "00DA");
        }

        [Fact]
        public void NegTest6()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, Int32.MaxValue, "00EA");
        }

        [Fact]
        public void NegTest7()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.UTF8, Int32.MaxValue, "00EB");
        }

        [Fact]
        public void NegTest8()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.UTF8, -1, "00DB");
        }
        #endregion

        public void PositiveTest(Encoding enc, int input, int expected, string id)
        {
            int output = enc.GetMaxByteCount(input);
            Assert.Equal(expected, output);
        }


        public void NegativeTest<T>(Encoding enc, int input, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetMaxByteCount(input);
            });
        }
    }
}
