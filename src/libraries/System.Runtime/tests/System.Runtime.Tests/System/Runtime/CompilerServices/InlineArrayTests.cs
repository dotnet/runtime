// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class InlineArrayTests
    {
        [Fact]
        public void InlineArray2Test()
        {
            InlineArray2<int> inlineArray2 = new InlineArray2<int>();
            inlineArray2[0] = 1;
            inlineArray2[1] = 2;
            Assert.Equal(1, inlineArray2[0]);
            Assert.Equal(2, inlineArray2[1]);
        }

        [Fact]
        public void InlineArray3Test()
        {
            InlineArray3<int> inlineArray3 = new InlineArray3<int>();
            inlineArray3[0] = 1;
            inlineArray3[1] = 2;
            inlineArray3[2] = 3;
            Assert.Equal(1, inlineArray3[0]);
            Assert.Equal(2, inlineArray3[1]);
            Assert.Equal(3, inlineArray3[2]);
        }

        [Fact]
        public void InlineArray4Test()
        {
            InlineArray4<int> inlineArray4 = new InlineArray4<int>();
            for (int i = 0; i < 4; i++)
            {
                inlineArray4[i] = i + 1;
            }
            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i + 1, inlineArray4[i]);
            }
        }

        [Fact]
        public void InlineArray5Test()
        {
            InlineArray5<int> inlineArray5 = new InlineArray5<int>();
            for (int i = 0; i < 5; i++)
            {
                inlineArray5[i] = i + 1;
            }
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(i + 1, inlineArray5[i]);
            }
        }

        [Fact]
        public void InlineArray6Test()
        {
            InlineArray6<int> inlineArray6 = new InlineArray6<int>();
            for (int i = 0; i < 6; i++)
            {
                inlineArray6[i] = i + 1;
            }
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(i + 1, inlineArray6[i]);
            }
        }

        [Fact]
        public void InlineArray7Test()
        {
            InlineArray7<int> inlineArray7 = new InlineArray7<int>();
            for (int i = 0; i < 7; i++)
            {
                inlineArray7[i] = i + 1;
            }
            for (int i = 0; i < 7; i++)
            {
                Assert.Equal(i + 1, inlineArray7[i]);
            }
        }

        [Fact]
        public void InlineArray8Test()
        {
            InlineArray8<int> inlineArray8 = new InlineArray8<int>();
            for (int i = 0; i < 8; i++)
            {
                inlineArray8[i] = i + 1;
            }
            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(i + 1, inlineArray8[i]);
            }
        }

        [Fact]
        public void InlineArray9Test()
        {
            InlineArray9<int> inlineArray9 = new InlineArray9<int>();
            for (int i = 0; i < 9; i++)
            {
                inlineArray9[i] = i + 1;
            }
            for (int i = 0; i < 9; i++)
            {
                Assert.Equal(i + 1, inlineArray9[i]);
            }
        }

        [Fact]
        public void InlineArray10Test()
        {
            InlineArray10<int> inlineArray10 = new InlineArray10<int>();
            for (int i = 0; i < 10; i++)
            {
                inlineArray10[i] = i + 1;
            }
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i + 1, inlineArray10[i]);
            }
        }

        [Fact]
        public void InlineArray11Test()
        {
            InlineArray11<int> inlineArray11 = new InlineArray11<int>();
            for (int i = 0; i < 11; i++)
            {
                inlineArray11[i] = i + 1;
            }
            for (int i = 0; i < 11; i++)
            {
                Assert.Equal(i + 1, inlineArray11[i]);
            }
        }

        [Fact]
        public void InlineArray12Test()
        {
            InlineArray12<int> inlineArray12 = new InlineArray12<int>();
            for (int i = 0; i < 12; i++)
            {
                inlineArray12[i] = i + 1;
            }
            for (int i = 0; i < 12; i++)
            {
                Assert.Equal(i + 1, inlineArray12[i]);
            }
        }

        [Fact]
        public void InlineArray13Test()
        {
            InlineArray13<int> inlineArray13 = new InlineArray13<int>();
            for (int i = 0; i < 13; i++)
            {
                inlineArray13[i] = i + 1;
            }
            for (int i = 0; i < 13; i++)
            {
                Assert.Equal(i + 1, inlineArray13[i]);
            }
        }

        [Fact]
        public void InlineArray14Test()
        {
            InlineArray14<int> inlineArray14 = new InlineArray14<int>();
            for (int i = 0; i < 14; i++)
            {
                inlineArray14[i] = i + 1;
            }
            for (int i = 0; i < 14; i++)
            {
                Assert.Equal(i + 1, inlineArray14[i]);
            }
        }

        [Fact]
        public void InlineArray15Test()
        {
            InlineArray15<int> inlineArray15 = new InlineArray15<int>();
            for (int i = 0; i < 15; i++)
            {
                inlineArray15[i] = i + 1;
            }
            for (int i = 0; i < 15; i++)
            {
                Assert.Equal(i + 1, inlineArray15[i]);
            }
        }
    }
}
