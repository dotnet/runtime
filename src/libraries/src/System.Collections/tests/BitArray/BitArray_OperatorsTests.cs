// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public enum Operator { Xor, Or, And };
    
    public class BitArray_OperatorsTests
    {
        [Fact]
        public static void And_EmptyArray()
        {
            BitArray bitArray1 = new BitArray(0);
            BitArray bitArray2 = new BitArray(0);
            
            Assert.Equal(0, bitArray1.And(bitArray2).Length);
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(0x1000F)]
        public static void Not(int length)
        {
            BitArray bitArray = new BitArray(length, false);
            if (length > 0)
            {
                bitArray[0] = true;
                bitArray[1] = true;
                bitArray[length - 2] = true;
                bitArray[length - 1] = true;
            }

            BitArray bitArrayNot = bitArray.Not();
            Assert.Equal(bitArray.Length, bitArrayNot.Length);
            Assert.Same(bitArray, bitArrayNot);
            for (int i = 0; i < bitArray.Length; i++)
            {
                if (i <= 1 || i >= length - 2)
                {
                    Assert.False(bitArrayNot[i]);
                }
                else
                {
                    Assert.True(bitArrayNot[i]);
                }
            }
        }
        
        [Theory]
        [InlineData(Operator.And)]
        [InlineData(Operator.Or)]
        [InlineData(Operator.Xor)]
        public static void OperatorTest(Operator op)
        {
            BitArray bitArray1 = new BitArray(6, false);
            BitArray bitArray2 = new BitArray(6, false);
            BitArray result;

            bitArray1.Set(0, true);
            bitArray1.Set(1, true);

            bitArray2.Set(1, true);
            bitArray2.Set(2, true);

            switch (op)
            {
                case Operator.Xor:
                    result = bitArray1.Xor(bitArray2);
                    Assert.Same(bitArray1, result);
                    Assert.True(result.Get(0));
                    Assert.False(result.Get(1));
                    Assert.True(result.Get(2));
                    Assert.False(result.Get(4));
                    break;

                case Operator.And:
                    result = bitArray1.And(bitArray2);
                    Assert.Same(bitArray1, result);
                    Assert.False(result.Get(0));
                    Assert.True(result.Get(1));
                    Assert.False(result.Get(2));
                    Assert.False(result.Get(4));
                    break;

                case Operator.Or:
                    result = bitArray1.Or(bitArray2);
                    Assert.Same(bitArray1, result);
                    Assert.True(result.Get(0));
                    Assert.True(result.Get(1));
                    Assert.True(result.Get(2));
                    Assert.False(result.Get(4));
                    break;
            }
            
            // Size stress cases.
            bitArray1 = new BitArray(0x1000F, false);
            bitArray2 = new BitArray(0x1000F, false);

            bitArray1.Set(0x10000, true); // The bit for 1 (2^0).
            bitArray1.Set(0x10001, true); // The bit for 2 (2^1).

            bitArray2.Set(0x10001, true); // The bit for 2 (2^1).

            switch (op)
            {
                case Operator.Xor:
                    result = bitArray1.Xor(bitArray2);
                    Assert.Same(bitArray1, result);
                    Assert.True(result.Get(0x10000));
                    Assert.False(result.Get(0x10001));
                    Assert.False(result.Get(0x10002));
                    Assert.False(result.Get(0x10004));
                    break;

                case Operator.And:
                    result = bitArray1.And(bitArray2);
                    Assert.Same(bitArray1, result);
                    Assert.False(result.Get(0x10000));
                    Assert.True(result.Get(0x10001));
                    Assert.False(result.Get(0x10002));
                    Assert.False(result.Get(0x10004));
                    break;

                case Operator.Or:
                    result = bitArray1.Or(bitArray2);
                    Assert.Same(bitArray1, result);
                    Assert.True(result.Get(0x10000));
                    Assert.True(result.Get(0x10001));
                    Assert.False(result.Get(0x10002));
                    Assert.False(result.Get(0x10004));
                    break;
            }
        }
        
        [Fact]
        public static void And_Invalid()
        {
            BitArray bitArray1 = new BitArray(11, false);
            BitArray bitArray2 = new BitArray(6, false);

            // Different lengths
            Assert.Throws<ArgumentException>(null, () => bitArray1.And(bitArray2));
            Assert.Throws<ArgumentException>(null, () => bitArray2.And(bitArray1));

            Assert.Throws<ArgumentNullException>("value", () => bitArray1.And(null));
        }
        
        [Fact]
        public static void Or_Invalid()
        {
            BitArray bitArray1 = new BitArray(11, false);
            BitArray bitArray2 = new BitArray(6, false);

            // Different lengths
            Assert.Throws<ArgumentException>(null, () => bitArray1.Or(bitArray2));
            Assert.Throws<ArgumentException>(null, () => bitArray2.Or(bitArray1));

            Assert.Throws<ArgumentNullException>("value", () => bitArray1.Or(null));
        }

        [Fact]
        public static void BitArray_XorTest_Negative()
        {
            BitArray bitArray1 = new BitArray(11, false);
            BitArray bitArray2 = new BitArray(6, false);

            // Different lengths
            Assert.Throws<ArgumentException>(null, () => bitArray1.Xor(bitArray2));
            Assert.Throws<ArgumentException>(null, () => bitArray2.Xor(bitArray1));

            Assert.Throws<ArgumentNullException>("value", () => bitArray1.Xor(null));
        }
    }
}
