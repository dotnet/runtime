// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public class BitArray_GetSetTests
    {
        private static BitArray s_allTrue = new BitArray(320, true);
        private static BitArray s_allFalse = new BitArray(320, false);
        private static BitArray s_alternating = new BitArray(Enumerable.Repeat(unchecked((int)0xaaaaaaaa), 10).ToArray());

        [Theory]
        [InlineData(new bool[] { true })]
        [InlineData(new bool[] { false })]
        [InlineData(new bool[] { true, false, true, true, false, true })]
        public static void Get_Set(bool[] newValues)
        {
            BitArray bitArray = new BitArray(newValues.Length, false);
            for (int i = 0; i < newValues.Length; i++)
            {
                bitArray.Set(i, newValues[i]);
                Assert.Equal(newValues[i], bitArray[i]);
                Assert.Equal(newValues[i], bitArray.Get(i));
            }
        }

        [Fact]
        public static void Get_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            BitArray bitArray = new BitArray(4);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray.Get(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray.Get(bitArray.Length));

            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray[-1]);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray[bitArray.Length]);
        }

        [Fact]
        public static void Set_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            BitArray bitArray = new BitArray(4);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray.Set(-1, true));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray.Set(bitArray.Length, true));

            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray[-1] = true);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray[bitArray.Length] = true);
        }

        [Theory]
        [InlineData(6, true)]
        [InlineData(6, false)]
        [InlineData(0x1000F, true)]
        public static void SetAll(int size, bool defaultValue)
        {
            BitArray bitArray = new BitArray(6, defaultValue);
            bitArray.SetAll(!defaultValue);
            for (int i = 0; i < bitArray.Length; i++)
            {
                Assert.Equal(!defaultValue, bitArray[i]);
                Assert.Equal(!defaultValue, bitArray.Get(i));
            }

            bitArray.SetAll(defaultValue);
            for (int i = 0; i < bitArray.Length; i++)
            {
                Assert.Equal(defaultValue, bitArray[i]);
                Assert.Equal(defaultValue, bitArray.Get(i));
            }
        }
            
        [Theory]
        [InlineData(new bool[0])]
        [InlineData(new bool[] { true, false, true, false, true, false, true, false, true, false })]
        public static void GetEnumerator(bool[] values)
        {
            BitArray bitArray = new BitArray(values);
            Assert.NotSame(bitArray.GetEnumerator(), bitArray.GetEnumerator());
            IEnumerator enumerator = bitArray.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(bitArray[counter], enumerator.Current);
                    counter++;
                }
                Assert.Equal(bitArray.Length, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        public static void GetEnumerator_Invalid()
        {
            BitArray bitArray = new BitArray(10, true);
            IEnumerator enumerator = bitArray.GetEnumerator();

            // Has not started enumerating
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Has finished enumerating
            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Has resetted enumerating
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Has modified underlying collection
            enumerator.MoveNext();
            bitArray[0] = false;
            Assert.True((bool)enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        }

        [Theory]
        [InlineData(16, 48)]
        [InlineData(48, 24)]
        [InlineData(16384, 256)]
        [InlineData(48, 48)]
        public static void Length_Set(int originalSize, int newSize)
        {
            BitArray bitArray = new BitArray(originalSize, true);
            bitArray.Length = newSize;
            Assert.Equal(newSize, bitArray.Length);
            for (int i = 0; i < Math.Min(originalSize, bitArray.Length); i++)
            {
                Assert.True(bitArray[i]);
                Assert.True(bitArray.Get(i));
            }
            for (int i = originalSize; i < newSize; i++)
            {
                Assert.False(bitArray[i]);
                Assert.False(bitArray.Get(i));
            }
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray[newSize]);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray.Get(newSize));

            // Decrease then increase size
            bitArray.Length = 0;
            Assert.Equal(0, bitArray.Length);

            bitArray.Length = newSize;
            Assert.Equal(newSize, bitArray.Length);
            Assert.False(bitArray.Get(newSize - 1));
        }

        [Fact]
        public static void Length_Set_InvalidLength_ThrowsArgumentOutOfRangeException()
        {
            BitArray bitArray = new BitArray(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => bitArray.Length = -1);
        }

        public static IEnumerable<object[]> CopyTo_IntArray_TestData()
        {
            yield return new object[] { s_allTrue, new int[10], 0, Enumerable.Repeat(unchecked((int)0xffffffff), 10).ToArray(), typeof(int) };
            yield return new object[] { s_allFalse, new int[11], 1, Enumerable.Repeat(0, 10).ToArray(), typeof(int) };
            yield return new object[] { s_alternating, new int[12], 1, Enumerable.Repeat(unchecked((int)0xaaaaaaaa), 10).ToArray(), typeof(int) };

            yield return new object[] { s_allTrue, new bool[320], 0, Enumerable.Repeat(true, 320).ToArray(), typeof(bool) };
            yield return new object[] { s_allFalse, new bool[321], 1, Enumerable.Repeat(false, 320).ToArray(), typeof(bool) };
            yield return new object[] { s_alternating, new bool[322], 1, Enumerable.Range(0, 320).Select(i => i % 2 == 1).ToArray(), typeof(bool) };

            yield return new object[] { s_allTrue, new byte[40], 0, Enumerable.Repeat((byte)255, 40).ToArray(), typeof(byte) };
            yield return new object[] { s_allFalse, new byte[41], 1, Enumerable.Repeat((byte)0, 40).ToArray(), typeof(byte) };
            yield return new object[] { s_alternating, new byte[42], 1, Enumerable.Repeat((byte)170, 40).ToArray(), typeof(byte) };
        }

        [Theory]
        [MemberData(nameof(CopyTo_IntArray_TestData))]
        public void CopyTo(BitArray bitArray, Array array, int index, Array expected, Type arrayType)
        {
            object defaultValue = Activator.CreateInstance(arrayType);
            ICollection collection = bitArray;
            collection.CopyTo(array, index);
            for (int i = 0; i < index; i++)
            {
                Assert.Equal(defaultValue, array.GetValue(i));
            }
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected.GetValue(i), array.GetValue(i + index));
            }
            for (int i = index + expected.Length; i < array.Length; i++)
            {
                Assert.Equal(defaultValue, array.GetValue(i));
            }
        }

        [Fact]
        public void CopyTo_Invalid()
        {
            ICollection bitArray = new BitArray(10);
            // Invalid array
            Assert.Throws<ArgumentNullException>("array", () => bitArray.CopyTo(null, 0));
            Assert.Throws<ArgumentException>(null, () => bitArray.CopyTo(new long[10], 0));
            Assert.Throws<ArgumentException>(null, () => bitArray.CopyTo(new int[10, 10], 0));

            // Invalid index
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bitArray.CopyTo(new byte[10], -1));
            Assert.Throws<ArgumentException>(null, () => bitArray.CopyTo(new byte[1], 2));
            Assert.Throws<ArgumentException>(null, () => bitArray.CopyTo(new bool[10], 2));
        }

        [Fact]
        public void SyncRoot()
        {
            ICollection bitArray = new BitArray(10);
            Assert.Same(bitArray.SyncRoot, bitArray.SyncRoot);
        }
    }
}
