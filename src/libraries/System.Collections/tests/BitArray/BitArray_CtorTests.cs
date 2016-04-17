// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public class BitArray_CtorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(40)]
        [InlineData(200)]
        [InlineData(65551)]
        public static void Ctor_Int(int length)
        {
            BitArray bitArray = new BitArray(length);
            Assert.Equal(length, bitArray.Length);
            for (int i = 0; i < bitArray.Length; i++)
            {
                Assert.False(bitArray[i]);
                Assert.False(bitArray.Get(i));
            }
            ICollection collection = bitArray;
            Assert.Equal(length, collection.Count);
            Assert.False(collection.IsSynchronized);

            Ctor_BitArray(bitArray);
            Ctor_Int_Bool(length, false);
            Ctor_Int_Bool(length, true);
        }

        public static void Ctor_Int_Bool(int length, bool defaultValue)
        {
            BitArray bitArray = new BitArray(length, defaultValue);
            Assert.Equal(length, bitArray.Length);
            for (int i = 0; i < bitArray.Length; i++)
            {
                Assert.Equal(defaultValue, bitArray[i]);
                Assert.Equal(defaultValue, bitArray.Get(i));
            }
            ICollection collection = bitArray;
            Assert.Equal(length, collection.Count);
            Assert.False(collection.IsSynchronized);

            Ctor_BitArray(bitArray);
        }

        [Fact]
        public static void Ctor_Int_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("length", () => new BitArray(-1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => new BitArray(-1, false));
        }

        [Theory]
        [InlineData(new bool[0])]
        [InlineData(new bool[] { false, false, true, false, false, false, true, false, false, true })]
        public static void Ctor_BoolArray(bool[] values)
        {
            BitArray bitArray = new BitArray(values);
            Assert.Equal(values.Length, bitArray.Length);
            for (int i = 0; i < bitArray.Length; i++)
            {
                Assert.Equal(values[i], bitArray[i]);
                Assert.Equal(values[i], bitArray.Get(i));
            }
            ICollection collection = bitArray;
            Assert.Equal(values.Length, collection.Count);
            Assert.False(collection.IsSynchronized);

            Ctor_BitArray(bitArray);
        }

        public static void Ctor_BitArray(BitArray bits)
        {
            BitArray bitArray = new BitArray(bits);
            Assert.Equal(bits.Length, bitArray.Length);
            for (int i = 0; i < bitArray.Length; i++)
            {
                Assert.Equal(bits[i], bitArray[i]);
                Assert.Equal(bits[i], bitArray.Get(i));
            }
            ICollection collection = bitArray;
            Assert.Equal(bits.Length, collection.Count);
            Assert.False(collection.IsSynchronized);
        }

        public static IEnumerable<object[]> Ctor_IntArray_TestData()
        {
            yield return new object[] { Enumerable.Repeat(unchecked((int)0xffffffff), 10).ToArray(), Enumerable.Repeat(true, 320).ToArray() };
            yield return new object[] { Enumerable.Repeat(0, 10).ToArray(), Enumerable.Repeat(false, 320).ToArray() };
            yield return new object[] { Enumerable.Repeat(unchecked((int)0xaaaaaaaa), 10).ToArray(), Enumerable.Range(0, 320).Select(i => i % 2 == 1).ToArray() };
        }

        [Theory]
        [MemberData(nameof(Ctor_IntArray_TestData))]
        public void Ctor_IntArray(int[] array, bool[] expected)
        {
            BitArray bitArray = new BitArray(array);
            Assert.Equal(expected.Length, bitArray.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], bitArray[i]);
                Assert.Equal(expected[i], bitArray.Get(i));
            }
            ICollection collection = bitArray;
            Assert.Equal(expected.Length, collection.Count);
            Assert.False(collection.IsSynchronized);
        }

        [Fact]
        public static void Ctor_BitArray_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("bits", () => new BitArray((BitArray)null));
        }
        
        [Fact]
        public static void Ctor_BoolArray_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("values", () => new BitArray((bool[])null));
        }

        [Fact]
        public static void Ctor_IntArray_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("values", () => new BitArray((int[])null));
        }

        public static IEnumerable<object[]> Ctor_ByteArray_TestData()
        {
            yield return new object[] { new byte[] { 255, 255 }, Enumerable.Repeat(true, 16).ToArray() };
            yield return new object[] { new byte[] { 0, 0 }, Enumerable.Repeat(false, 16).ToArray() };
        }

        [Theory]
        [MemberData(nameof(Ctor_ByteArray_TestData))]
        public static void Ctor_ByteArray(byte[] bytes, bool[] expected)
        {
            BitArray bitArray = new BitArray(bytes);
            Assert.Equal(expected.Length, bitArray.Length);
            for (int i = 0; i < bitArray.Length; i++)
            {
                Assert.Equal(expected[i], bitArray[i]);
                Assert.Equal(expected[i], bitArray.Get(i));
            }
            ICollection collection = bitArray;
            Assert.Equal(expected.Length, collection.Count);
            Assert.False(collection.IsSynchronized);
        }

        [Fact]
        public static void Ctor_ByteArray_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("bytes", () => new BitArray((byte[])null));
        }
    }
}
