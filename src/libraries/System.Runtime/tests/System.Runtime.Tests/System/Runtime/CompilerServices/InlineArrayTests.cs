// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class InlineArrayTests
    {
        private static void WriteRead(Span<int> span, int expectedLength)
        {
            Assert.Equal(expectedLength, span.Length);

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = i + 1;
            }

            for (int i = 0; i < span.Length; i++)
            {
                Assert.Equal(i + 1, span[i]);
            }
        }

        [Fact]
        public void InlineArray2Test()
        {
            InlineArray2<int> inlineArray2 = new();
            WriteRead(inlineArray2, 2);
        }

        [Fact]
        public void InlineArray3Test()
        {
            InlineArray3<int> inlineArray3 = new();
            WriteRead(inlineArray3, 3);
        }

        [Fact]
        public void InlineArray4Test()
        {
            InlineArray4<int> inlineArray4 = new();
            WriteRead(inlineArray4, 4);
        }

        [Fact]
        public void InlineArray5Test()
        {
            InlineArray5<int> inlineArray5 = new();
            WriteRead(inlineArray5, 5);
        }

        [Fact]
        public void InlineArray6Test()
        {
            InlineArray6<int> inlineArray6 = new();
            WriteRead(inlineArray6, 6);
        }

        [Fact]
        public void InlineArray7Test()
        {
            InlineArray7<int> inlineArray7 = new();
            WriteRead(inlineArray7, 7);
        }

        [Fact]
        public void InlineArray8Test()
        {
            InlineArray8<int> inlineArray8 = new();
            WriteRead(inlineArray8, 8);
        }

        [Fact]
        public void InlineArray9Test()
        {
            InlineArray9<int> inlineArray9 = new();
            WriteRead(inlineArray9, 9);
        }

        [Fact]
        public void InlineArray10Test()
        {
            InlineArray10<int> inlineArray10 = new();
            WriteRead(inlineArray10, 10);
        }

        [Fact]
        public void InlineArray11Test()
        {
            InlineArray11<int> inlineArray11 = new();
            WriteRead(inlineArray11, 11);
        }

        [Fact]
        public void InlineArray12Test()
        {
            InlineArray12<int> inlineArray12 = new();
            WriteRead(inlineArray12, 12);
        }

        [Fact]
        public void InlineArray13Test()
        {
            InlineArray13<int> inlineArray13 = new();
            WriteRead(inlineArray13, 13);
        }

        [Fact]
        public void InlineArray14Test()
        {
            InlineArray14<int> inlineArray14 = new();
            WriteRead(inlineArray14, 14);
        }

        [Fact]
        public void InlineArray15Test()
        {
            InlineArray15<int> inlineArray15 = new();
            WriteRead(inlineArray15, 15);
        }
    }
}
