// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingGetMaxCharCount
    {
        #region Positive Testcases
        [Fact]
        public void PosTest1()
        {
            PositiveTest(Encoding.UTF8, 0, 1, "00A");
        }

        [Fact]
        public void PosTest2()
        {
            PositiveTest(Encoding.UTF8, 1, 2, "00B");
        }

        [Fact]
        public void PosTest3()
        {
            PositiveTest(Encoding.UTF8, 100, 101, "00C");
        }

        [Fact]
        public void PosTest4()
        {
            PositiveTest(Encoding.Unicode, 0, 1, "00A3");
        }

        [Fact]
        public void PosTest5()
        {
            PositiveTest(Encoding.Unicode, 1, 2, "00B3");
        }

        [Fact]
        public void PosTest6()
        {
            PositiveTest(Encoding.Unicode, 100, 51, "00C3");
        }

        [Fact]
        public void PosTest7()
        {
            PositiveTest(Encoding.BigEndianUnicode, 0, 1, "00A4");
        }

        [Fact]
        public void PosTest8()
        {
            PositiveTest(Encoding.BigEndianUnicode, 1, 2, "00B4");
        }

        [Fact]
        public void PosTest9()
        {
            PositiveTest(Encoding.BigEndianUnicode, 100, 51, "00C4");
        }

        [Fact]
        public void PosTest10()
        {
            PositiveTest(Encoding.UTF8, 0, 1, "00A5");
        }

        [Fact]
        public void PosTest11()
        {
            PositiveTest(Encoding.UTF8, 1, 2, "00B5");
        }

        [Fact]
        public void PosTest12()
        {
            PositiveTest(Encoding.UTF8, 100, 101, "00C5");
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
            NegativeTest<ArgumentOutOfRangeException>(Encoding.Unicode, -1, "00D9");
        }

        [Fact]
        public void NegTest4()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.BigEndianUnicode, -1, "00DA");
        }

        [Fact]
        public void NegTest5()
        {
            NegativeTest<ArgumentOutOfRangeException>(Encoding.UTF8, -1, "00DB");
        }
        #endregion
        public void PositiveTest(Encoding enc, int input, int expected, string id)
        {
            int output = enc.GetMaxCharCount(input);
            Assert.Equal(expected, output);
        }

        public void NegativeTest<T>(Encoding enc, int input, string id) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int output = enc.GetMaxCharCount(input);
            });
        }
    }
}
