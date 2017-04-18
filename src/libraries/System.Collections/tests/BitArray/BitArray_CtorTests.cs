// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public static class BitArray_CtorTests
    {
        private const int BitsPerByte = 8;
        private const int BitsPerInt32 = 32;

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(BitsPerByte)]
        [InlineData(BitsPerByte * 2)]
        [InlineData(BitsPerInt32)]
        [InlineData(BitsPerInt32 * 2)]
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
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(1, false)]
        [InlineData(BitsPerByte, true)]
        [InlineData(BitsPerByte, false)]
        [InlineData(BitsPerByte * 2, true)]
        [InlineData(BitsPerByte * 2, false)]
        [InlineData(BitsPerInt32, true)]
        [InlineData(BitsPerInt32, false)]
        [InlineData(BitsPerInt32 * 2, true)]
        [InlineData(BitsPerInt32 * 2, false)]
        [InlineData(200, true)]
        [InlineData(200, false)]
        [InlineData(65551, true)]
        [InlineData(65551, false)]
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
        }

        [Fact]
        public static void Ctor_Int_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new BitArray(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new BitArray(-1, false));
        }

        public static IEnumerable<object[]> Ctor_BoolArray_TestData()
        {
            yield return new object[] { new bool[0] };
            foreach (int size in new[] { 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2 })
            {
                yield return new object[] { Enumerable.Repeat(true, size).ToArray() };
                yield return new object[] { Enumerable.Repeat(false, size).ToArray() };
                yield return new object[] { Enumerable.Range(0, size).Select(x => x % 2 == 0).ToArray() };
            }
        }

        [Theory]
        [MemberData(nameof(Ctor_BoolArray_TestData))]
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
        }

        [Fact]
        public static void Ctor_NullBoolArray_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("values", () => new BitArray((bool[])null));
        }

        public static IEnumerable<object[]> Ctor_BitArray_TestData()
        {
            yield return new object[] { "bool[](empty)", new BitArray(new bool[0]) };
            yield return new object[] { "byte[](empty)", new BitArray(new byte[0]) };
            yield return new object[] { "int[](empty)", new BitArray(new int[0]) };

            foreach (int size in new[] { 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2 })
            {
                yield return new object[] { "length", new BitArray(size) };
                yield return new object[] { "length|default(true)", new BitArray(size, true) };
                yield return new object[] { "length|default(false)", new BitArray(size, false) };
                yield return new object[] { "bool[](all)", new BitArray(Enumerable.Repeat(true, size).ToArray()) };
                yield return new object[] { "bool[](none)", new BitArray(Enumerable.Repeat(false, size).ToArray()) };
                yield return new object[] { "bool[](alternating)", new BitArray(Enumerable.Range(0, size).Select(x => x % 2 == 0).ToArray()) };
                if (size >= BitsPerByte)
                {
                    yield return new object[] { "byte[](all)", new BitArray(Enumerable.Repeat((byte)0xff, size / BitsPerByte).ToArray()) };
                    yield return new object[] { "byte[](none)", new BitArray(Enumerable.Repeat((byte)0x00, size / BitsPerByte).ToArray()) };
                    yield return new object[] { "byte[](alternating)", new BitArray(Enumerable.Repeat((byte)0xaa, size / BitsPerByte).ToArray()) };
                }
                if (size >= BitsPerInt32)
                {
                    yield return new object[] { "int[](all)", new BitArray(Enumerable.Repeat(unchecked((int)0xffffffff), size / BitsPerInt32).ToArray()) };
                    yield return new object[] { "int[](none)", new BitArray(Enumerable.Repeat(0x00000000, size / BitsPerInt32).ToArray()) };
                    yield return new object[] { "int[](alternating)", new BitArray(Enumerable.Repeat(unchecked((int)0xaaaaaaaa), size / BitsPerInt32).ToArray()) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Ctor_BitArray_TestData))]
        public static void Ctor_BitArray(string label, BitArray bits)
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

        [Fact]
        public static void Ctor_NullBitArray_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("bits", () => new BitArray((BitArray)null));
        }

        public static IEnumerable<object[]> Ctor_IntArray_TestData()
        {
            yield return new object[] { new int[0], new bool[0] };
            foreach (int size in new[] { 1, 10 })
            {
                yield return new object[] { Enumerable.Repeat(unchecked((int)0xffffffff), size).ToArray(), Enumerable.Repeat(true, size * BitsPerInt32).ToArray() };
                yield return new object[] { Enumerable.Repeat(0x00000000, size).ToArray(), Enumerable.Repeat(false, size * BitsPerInt32).ToArray() };
                yield return new object[] { Enumerable.Repeat(unchecked((int)0xaaaaaaaa), size).ToArray(), Enumerable.Range(0, size * BitsPerInt32).Select(i => i % 2 == 1).ToArray() };
            }
        }

        [Theory]
        [MemberData(nameof(Ctor_IntArray_TestData))]
        public static void Ctor_IntArray(int[] array, bool[] expected)
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
        public static void Ctor_NullIntArray_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("values", () => new BitArray((int[])null));
        }

        [Fact]
        public static void Ctor_LargeIntArrayOverflowingBitArray_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("values", () => new BitArray(new int[int.MaxValue / BitsPerInt32 + 1 ]));
        }

        public static IEnumerable<object[]> Ctor_ByteArray_TestData()
        {
            yield return new object[] { new byte[0], new bool[0] };
            foreach (int size in new[] { 1, 2, 3, BitsPerInt32 / BitsPerByte, 2 * BitsPerInt32 / BitsPerByte })
            {
                yield return new object[] { Enumerable.Repeat((byte)0xff, size).ToArray(), Enumerable.Repeat(true, size * BitsPerByte).ToArray() };
                yield return new object[] { Enumerable.Repeat((byte)0x00, size).ToArray(), Enumerable.Repeat(false, size * BitsPerByte).ToArray() };
                yield return new object[] { Enumerable.Repeat((byte)0xaa, size).ToArray(), Enumerable.Range(0, size * BitsPerByte).Select(i => i % 2 == 1).ToArray() };
            }
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
        public static void Ctor_NullByteArray_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("bytes", () => new BitArray((byte[])null));
        }

        [Fact]
        public static void Ctor_LargeByteArrayOverflowingBitArray_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("bytes", () => new BitArray(new byte[int.MaxValue / BitsPerByte + 1 ]));
        }

        [Fact]
        public static void Ctor_Simple_Method_Tests()
        {
            int length = 0;
            BitArray bitArray = new BitArray(length);

            Assert.NotNull(bitArray.SyncRoot);
            Assert.False(bitArray.IsSynchronized);
            Assert.False(bitArray.IsReadOnly);
            Assert.Equal(bitArray, bitArray.Clone());
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "A bug in BitArray.Clone() caused an ArgumentExeption to be thrown in this case.")]
        [Fact]
        public static void Clone_LongLength_Works()
        {
            BitArray bitArray = new BitArray(int.MaxValue - 30);
            BitArray clone = (BitArray)bitArray.Clone();

            Assert.Equal(bitArray.Length, clone.Length);  
        }
    }
}
