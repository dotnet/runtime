// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static partial class ArrayTests
    {
        [Fact]
        public static void IList_GetSetItem()
        {
            var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            IList<int> iList = intArray;

            Assert.Equal(intArray.Length, iList.Count);
            for (int i = 0; i < iList.Count; i++)
            {
                Assert.Equal(intArray[i], iList[i]);

                iList[i] = 99;
                Assert.Equal(99, iList[i]);
                Assert.Equal(99, intArray[i]);
            }
        }

        [Fact]
        public static void IList_GetSetItem_Invalid()
        {
            IList iList = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            Assert.Throws<IndexOutOfRangeException>(() => iList[-1]); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => iList[iList.Count]); // Index >= list.Count

            Assert.Throws<IndexOutOfRangeException>(() => iList[-1] = 0); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => iList[iList.Count] = 0); // Index >= list.Count

            iList = new int[,] { { 1 }, { 2 } };
            Assert.Throws<ArgumentException>(null, () => iList[0]); // Array is multidimensional
            Assert.Throws<ArgumentException>(null, () => iList[0] = 0); // Array is multidimensional
        }

        [Fact]
        public static void IList_ModifyingArray_ThrowsNotSupportedException()
        {
            var array = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            IList<int> iList = array;

            Assert.True(iList.IsReadOnly);
            Assert.True(((IList)array).IsFixedSize);

            Assert.Throws<NotSupportedException>(() => iList.Add(2));
            Assert.Throws<NotSupportedException>(() => iList.Insert(0, 0));
            Assert.Throws<NotSupportedException>(() => iList.Clear());
            Assert.Throws<NotSupportedException>(() => iList.Remove(2));
            Assert.Throws<NotSupportedException>(() => iList.RemoveAt(2));
        }

        [Fact]
        public static void CastAs_IListOfT()
        {
            var sa = new string[] { "Hello", "There" };
            Assert.True(sa is IList<string>);

            IList<string> ils = sa;
            Assert.Equal(2, ils.Count);

            ils[0] = "50";
            Assert.Equal("50", sa[0]);
            Assert.Equal(sa[1], ils[1]);
            Assert.Equal("There", sa[1]);
        }

        [Fact]
        public static void Construction()
        {
            // Check a number of the simple APIs on Array for dimensions up to 4.
            Array array = new int[] { 1, 2, 3 };
            VerifyArray(array, typeof(int), new int[] { 3 }, new int[1]);

            array = new int[,] { { 1, 2, 3 }, { 4, 5, 6 } };
            VerifyArray(array, typeof(int), new int[] { 2, 3 }, new int[2]);

            array = new int[2, 3, 4];
            VerifyArray(array, typeof(int), new int[] { 2, 3, 4 }, new int[3]);

            array = new int[2, 3, 4, 5];
            VerifyArray(array, typeof(int), new int[] { 2, 3, 4, 5 }, new int[4]);
        }

        [Fact]
        public static void Construction_MultiDimensionalArray()
        {
            // This C# initialization syntax generates some peculiar looking IL.
            // Initializations of this form are handled specially on Desktop and in .NET Native by UTC.
            var array = new int[,,,] { { { { 1, 2, 3 }, { 1, 2, 3 } }, { { 1, 2, 3 }, { 1, 2, 3 } } }, { { { 1, 2, 3 }, { 1, 2, 3 } }, { { 1, 2, 3 }, { 1, 2, 3 } } } };
            Assert.NotNull(array);
            Assert.Equal(array.GetValue(0, 0, 0, 0), 1);
            Assert.Equal(array.GetValue(0, 0, 0, 1), 2);
            Assert.Equal(array.GetValue(0, 0, 0, 2), 3);
        }

        public static IEnumerable<object[]> BinarySearch_SZArray_TestData()
        {
            string[] stringArray = new string[] { null, "aa", "bb", "bb", "cc", "dd", "ee" };

            yield return new object[] { stringArray, 0, 7, "bb", null, 3 };
            yield return new object[] { stringArray, 0, 7, "ee", null, 6 };
            
            yield return new object[] { stringArray, 3, 4, "bb", null, 3 };
            yield return new object[] { stringArray, 4, 3, "bb", null, -5 };
            yield return new object[] { stringArray, 4, 0, "bb", null, -5 };

            yield return new object[] { stringArray, 0, 7, "bb", new StringComparer(), 3 };
            yield return new object[] { stringArray, 0, 7, "ee", new StringComparer(), 6 };
            yield return new object[] { stringArray, 0, 7, "no-such-object", new StringComparer(), -8 };

            yield return new object[] { new string[0], 0, 0, "", null, -1 };

            // SByte
            sbyte[] sbyteArray = new sbyte[] { sbyte.MinValue, 0, 0, sbyte.MaxValue };

            yield return new object[] { sbyteArray, 0, 4, sbyte.MinValue, null, 0 };
            yield return new object[] { sbyteArray, 0, 4, (sbyte)0, null, 1 };
            yield return new object[] { sbyteArray, 0, 4, sbyte.MaxValue, null, 3 };
            yield return new object[] { sbyteArray, 0, 4, (sbyte)1, null, -4 };

            yield return new object[] { sbyteArray, 0, 1, sbyte.MinValue, null, 0 };
            yield return new object[] { sbyteArray, 1, 3, sbyte.MaxValue, null, 3 };
            yield return new object[] { sbyteArray, 1, 3, sbyte.MinValue, null, -2 };
            yield return new object[] { sbyteArray, 1, 0, (sbyte)0, null, -2 };

            yield return new object[] { new sbyte[0], 0, 0, (sbyte)0, null, -1 };

            // Byte
            byte[] byteArray = new byte[] { byte.MinValue, 5, 5, byte.MaxValue };

            yield return new object[] { byteArray, 0, 4, byte.MinValue, null, 0 };
            yield return new object[] { byteArray, 0, 4, (byte)5, null, 1 };
            yield return new object[] { byteArray, 0, 4, byte.MaxValue, null, 3 };
            yield return new object[] { byteArray, 0, 4, (byte)1, null, -2 };

            yield return new object[] { byteArray, 0, 1, byte.MinValue, null, 0 };
            yield return new object[] { byteArray, 1, 3, byte.MaxValue, null, 3 };
            yield return new object[] { byteArray, 1, 3, byte.MinValue, null, -2 };
            yield return new object[] { byteArray, 1, 0, (byte)5, null, -2 };

            yield return new object[] { new byte[0], 0, 0, (byte)0, null, -1 };

            // Int16
            short[] shortArray = new short[] { short.MinValue, 0, 0, short.MaxValue };

            yield return new object[] { shortArray, 0, 4, short.MinValue, null, 0 };
            yield return new object[] { shortArray, 0, 4, (short)0, null, 1 };
            yield return new object[] { shortArray, 0, 4, short.MaxValue, null, 3 };
            yield return new object[] { shortArray, 0, 4, (short)1, null, -4 };

            yield return new object[] { shortArray, 0, 1, short.MinValue, null, 0 };
            yield return new object[] { shortArray, 1, 3, short.MaxValue, null, 3 };
            yield return new object[] { shortArray, 1, 3, short.MinValue, null, -2 };
            yield return new object[] { shortArray, 1, 0, (short)0, null, -2 };

            yield return new object[] { new short[0], 0, 0, (short)0, null, -1 };

            // UInt16
            ushort[] ushortArray = new ushort[] { ushort.MinValue, 5, 5, ushort.MaxValue };

            yield return new object[] { ushortArray, 0, 4, ushort.MinValue, null, 0 };
            yield return new object[] { ushortArray, 0, 4, (ushort)5, null, 1 };
            yield return new object[] { ushortArray, 0, 4, ushort.MaxValue, null, 3 };
            yield return new object[] { ushortArray, 0, 4, (ushort)1, null, -2 };

            yield return new object[] { ushortArray, 0, 1, ushort.MinValue, null, 0 };
            yield return new object[] { ushortArray, 1, 3, ushort.MaxValue, null, 3 };
            yield return new object[] { ushortArray, 1, 3, ushort.MinValue, null, -2 };
            yield return new object[] { ushortArray, 1, 0, (ushort)5, null, -2 };

            yield return new object[] { new ushort[0], 0, 0, (ushort)0, null, -1 };

            // Int32
            int[] intArray = new int[] { int.MinValue, 0, 0, int.MaxValue };

            yield return new object[] { intArray, 0, 4, int.MinValue, null, 0 };
            yield return new object[] { intArray, 0, 4, 0, null, 1 };
            yield return new object[] { intArray, 0, 4, int.MaxValue, null, 3 };
            yield return new object[] { intArray, 0, 4, 1, null, -4 };

            yield return new object[] { intArray, 0, 1, int.MinValue, null, 0 };
            yield return new object[] { intArray, 1, 3, int.MaxValue, null, 3 };
            yield return new object[] { intArray, 1, 3, int.MinValue, null, -2 };

            int[] intArray2 = new int[] { 1, 3, 6, 6, 8, 10, 12, 16 };
            yield return new object[] { intArray2, 0, 8, 8, new IntegerComparer(), 4 };
            yield return new object[] { intArray2, 0, 8, 6, new IntegerComparer(), 3 };
            yield return new object[] { intArray2, 0, 8, 0, new IntegerComparer(), -1 };

            yield return new object[] { new int[0], 0, 0, 0, null, -1 };

            // UInt32
            uint[] uintArray = new uint[] { uint.MinValue, 5, 5, uint.MaxValue };

            yield return new object[] { uintArray, 0, 4, uint.MinValue, null, 0 };
            yield return new object[] { uintArray, 0, 4, (uint)5, null, 1 };
            yield return new object[] { uintArray, 0, 4, uint.MaxValue, null, 3 };
            yield return new object[] { uintArray, 0, 4, (uint)1, null, -2 };

            yield return new object[] { uintArray, 0, 1, uint.MinValue, null, 0 };
            yield return new object[] { uintArray, 1, 3, uint.MaxValue, null, 3 };
            yield return new object[] { uintArray, 1, 3, uint.MinValue, null, -2 };
            yield return new object[] { uintArray, 1, 0, (uint)5, null, -2 };

            yield return new object[] { new uint[0], 0, 0, (uint)0, null, -1 };

            // Int64
            long[] longArray = new long[] { long.MinValue, 0, 0, long.MaxValue };

            yield return new object[] { longArray, 0, 4, long.MinValue, null, 0 };
            yield return new object[] { longArray, 0, 4, (long)0, null, 1 };
            yield return new object[] { longArray, 0, 4, long.MaxValue, null, 3 };
            yield return new object[] { longArray, 0, 4, (long)1, null, -4 };

            yield return new object[] { longArray, 0, 1, long.MinValue, null, 0 };
            yield return new object[] { longArray, 1, 3, long.MaxValue, null, 3 };
            yield return new object[] { longArray, 1, 3, long.MinValue, null, -2 };
            yield return new object[] { longArray, 1, 0, (long)0, null, -2 };

            yield return new object[] { new long[0], 0, 0, (long)0, null, -1 };

            // UInt64
            ulong[] ulongArray = new ulong[] { ulong.MinValue, 5, 5, ulong.MaxValue };

            yield return new object[] { ulongArray, 0, 4, ulong.MinValue, null, 0 };
            yield return new object[] { ulongArray, 0, 4, (ulong)5, null, 1 };
            yield return new object[] { ulongArray, 0, 4, ulong.MaxValue, null, 3 };
            yield return new object[] { ulongArray, 0, 4, (ulong)1, null, -2 };

            yield return new object[] { ulongArray, 0, 1, ulong.MinValue, null, 0 };
            yield return new object[] { ulongArray, 1, 3, ulong.MaxValue, null, 3 };
            yield return new object[] { ulongArray, 1, 3, ulong.MinValue, null, -2 };
            yield return new object[] { ulongArray, 1, 0, (ulong)5, null, -2 };

            yield return new object[] { new ulong[0], 0, 0, (ulong)0, null, -1 };

            // Char
            char[] charArray = new char[] { char.MinValue, (char)5, (char)5, char.MaxValue };

            yield return new object[] { charArray, 0, 4, char.MinValue, null, 0 };
            yield return new object[] { charArray, 0, 4, (char)5, null, 1 };
            yield return new object[] { charArray, 0, 4, char.MaxValue, null, 3 };
            yield return new object[] { charArray, 0, 4, (char)1, null, -2 };

            yield return new object[] { charArray, 0, 1, char.MinValue, null, 0 };
            yield return new object[] { charArray, 1, 3, char.MaxValue, null, 3 };
            yield return new object[] { charArray, 1, 3, char.MinValue, null, -2 };
            yield return new object[] { charArray, 1, 0, (char)5, null, -2 };

            yield return new object[] { new char[0], 0, 0, '\0', null, -1 };

            // Bool
            bool[] boolArray = new bool[] { false, false, true };

            yield return new object[] { boolArray, 0, 3, false, null, 1 };
            yield return new object[] { boolArray, 0, 3, true, null, 2 };
            yield return new object[] { new bool[] { false }, 0, 1, true, null, -2 };

            yield return new object[] { boolArray, 0, 1, false, null, 0 };
            yield return new object[] { boolArray, 2, 1, true, null, 2 };
            yield return new object[] { boolArray, 2, 1, false, null, -3 };
            yield return new object[] { boolArray, 1, 0, false, null, -2 };

            yield return new object[] { new bool[0], 0, 0, false, null, -1 };

            // Single
            float[] floatArray = new float[] { float.MinValue, 0, 0, float.MaxValue };

            yield return new object[] { floatArray, 0, 4, float.MinValue, null, 0 };
            yield return new object[] { floatArray, 0, 4, 0f, null, 1 };
            yield return new object[] { floatArray, 0, 4, float.MaxValue, null, 3 };
            yield return new object[] { floatArray, 0, 4, (float)1, null, -4 };

            yield return new object[] { floatArray, 0, 1, float.MinValue, null, 0 };
            yield return new object[] { floatArray, 1, 3, float.MaxValue, null, 3 };
            yield return new object[] { floatArray, 1, 3, float.MinValue, null, -2 };
            yield return new object[] { floatArray, 1, 0, 0f, null, -2 };

            yield return new object[] { new float[0], 0, 0, 0f, null, -1 };

            // Double
            double[] doubleArray = new double[] { double.MinValue, 0, 0, double.MaxValue };

            yield return new object[] { doubleArray, 0, 4, double.MinValue, null, 0 };
            yield return new object[] { doubleArray, 0, 4, 0d, null, 1 };
            yield return new object[] { doubleArray, 0, 4, double.MaxValue, null, 3 };
            yield return new object[] { doubleArray, 0, 4, (double)1, null, -4 };

            yield return new object[] { doubleArray, 0, 1, double.MinValue, null, 0 };
            yield return new object[] { doubleArray, 1, 3, double.MaxValue, null, 3 };
            yield return new object[] { doubleArray, 1, 3, double.MinValue, null, -2 };
            yield return new object[] { doubleArray, 1, 0, 0d, null, -2 };

            yield return new object[] { new double[0], 0, 0, 0d, null, -1 };
        }
        
        [Theory]
        // Workaround: Move these tests to BinarySearch_SZArray_TestData if/ when https://github.com/xunit/xunit/pull/965 is available
        [InlineData(new sbyte[] { 0 }, 0, 1, null, null, -1)]
        [InlineData(new byte[] { 0 }, 0, 1, null, null, -1)]
        [InlineData(new short[] { 0 }, 0, 1, null, null, -1)]
        [InlineData(new ushort[] { 0 }, 0, 1, null, null, -1)]
        [InlineData(new int[] { 0 }, 0, 1, null, null, -1)]
        [InlineData(new uint[] { 0 }, 0, 1, null, null, -1)]
        [InlineData(new long[] { 0 }, 0, 1, null, null, -1)]
        [InlineData(new ulong[] { 0 }, 0, 1, null, null, -1)]
        [InlineData(new bool[] { false }, 0, 1, null, null, -1)]
        [InlineData(new char[] { '\0' }, 0, 1, null, null, -1)]
        [InlineData(new float[] { 0 }, 0, 1, null, null, -1)]
        [InlineData(new double[] { 0 }, 0, 1, null, null, -1)]
        public static void BinarySearch_Array(Array array, int index, int length, object value, IComparer comparer, int expected)
        {
            bool isDefaultComparer = comparer == null || comparer == Comparer.Default;
            if (index == array.GetLowerBound(0) && length == array.Length)
            {
                if (isDefaultComparer)
                {
                    // Use BinarySearch(Array, object)
                    Assert.Equal(expected, Array.BinarySearch(array, value));
                    Assert.Equal(expected, Array.BinarySearch(array, value, Comparer.Default));
                }
                // Use BinarySearch(Array, object, IComparer)
                Assert.Equal(expected, Array.BinarySearch(array, value, comparer));
            }
            if (isDefaultComparer)
            {
                // Use BinarySearch(Array, int, int, object)
                Assert.Equal(expected, Array.BinarySearch(array, index, length, value));
            }
            // Use BinarySearch(Array, int, int, object, IComparer)
            Assert.Equal(expected, Array.BinarySearch(array, index, length, value, comparer));
        }

        [Theory]
        [MemberData(nameof(BinarySearch_SZArray_TestData))]
        public static void BinarySearch_SZArray<T>(T[] array, int index, int length, T value, IComparer<T> comparer, int expected)
        {
            // Forward to the non-generic overload if we can.
            bool isDefaultComparer = comparer == null || comparer == Comparer<T>.Default;
            if (isDefaultComparer || comparer is IComparer)
            {
                // Basic: forward SZArray
                BinarySearch_Array(array, index, length, value, (IComparer)comparer, expected);

                // Advanced: convert SZArray to an array with non-zero lower bound
                const int lowerBound = 5;
                Array nonZeroLowerBoundArray = NonZeroLowerBoundArray(array, lowerBound);
                int lowerBoundExpected = expected < 0 ? expected - lowerBound : expected + lowerBound;
                BinarySearch_Array(nonZeroLowerBoundArray, index + lowerBound, length, value, (IComparer)comparer, lowerBoundExpected);
            }
            
            if (index == 0 && length == array.Length)
            {
                if (isDefaultComparer)
                {
                    // Use BinarySearch<T>(T[], T)
                    Assert.Equal(expected, Array.BinarySearch(array, value));
                    Assert.Equal(expected, Array.BinarySearch(array, value, Comparer<T>.Default));
                }
                // Use BinarySearch<T>(T[], T, IComparer)
                Assert.Equal(expected, Array.BinarySearch(array, value, comparer));
            }
            if (isDefaultComparer)
            {
                // Use BinarySearch<T>(T, int, int, T)
                Assert.Equal(expected, Array.BinarySearch(array, index, length, value));
            }
            // Use BinarySearch<T>(T[], int, int, T, IComparer)
            Assert.Equal(expected, Array.BinarySearch(array, index, length, value, comparer));
        }

        [Fact]
        public static void BinarySearch_SZArray_NonInferrableEntries()
        {
            // Workaround: Move these values to BinarySearch_SZArray_TestData if/ when https://github.com/xunit/xunit/pull/965 is available
            BinarySearch_SZArray(new string[] { null, "a", "b", "c" }, 0, 4, null, null, 0);
            BinarySearch_SZArray(new string[] { null, "a", "b", "c" }, 0, 4, null, new StringComparer(), 0);
        }

        [Fact]
        public static void BinarySearch_NullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch((int[])null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch(null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch((int[])null, "", null));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch(null, "", null));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch((int[])null, 0, 0, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch(null, 0, 0, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch((int[])null, 0, 0, "", null));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch(null, 0, 0, "", null));
        }

        [Fact]
        public static void BinarySearch_MultiDimensionalArray_ThrowsRankException()
        {
            Assert.Throws<RankException>(() => Array.BinarySearch(new string[0, 0], ""));
            Assert.Throws<RankException>(() => Array.BinarySearch(new string[0, 0], "", null));
            Assert.Throws<RankException>(() => Array.BinarySearch(new string[0, 0], 0, 0, ""));
            Assert.Throws<RankException>(() => Array.BinarySearch(new string[0, 0], 0, 0, "", null));
        }

        public static IEnumerable<object[]> BinarySearch_TypesNotComparable_TestData()
        {
            // Different types
            yield return new object[] { new int[] { 0 }, "", 0 };

            // Type does not implement IComparable
            yield return new object[] { new object[] { new object() }, new object(), new object() };

            // IntPtr and UIntPtr are not supported
            yield return new object[] { new IntPtr[] { IntPtr.Zero }, IntPtr.Zero, IntPtr.Zero };
            yield return new object[] { new UIntPtr[] { UIntPtr.Zero }, UIntPtr.Zero, UIntPtr.Zero };

            // Conversion between primitives is not allowed
            yield return new object[] { new sbyte[] { 0 }, 0, (sbyte)0 };
            yield return new object[] { new char[] { '\0' }, (ushort)0, '\0' };
        }

        [Theory]
        [MemberData(nameof(BinarySearch_TypesNotComparable_TestData))]
        public static void BinarySearch_TypesNotIComparable_ThrowsInvalidOperationException<T>(T[] array, object value, T dummy)
        {
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(array, value));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(array, value, null));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(array, 0, array.Length, value));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(array, 0, array.Length, value, null));

            if (value is T)
            {
                Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(array, (T)value));
                Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(array, 0, array.Length, (T)value));
                Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(array, 0, array.Length, (T)value, null));
            }
        }

        [Fact]
        public static void BinarySearch_IndexLessThanZero_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.BinarySearch(new int[3], -1, 0, ""));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.BinarySearch(new string[3], -1, 0, ""));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.BinarySearch(new int[3], -1, 0, "", null));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.BinarySearch(new string[3], -1, 0, "", null));
        }

        [Fact]
        public static void BinarySearch_LengthLessThanZero_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.BinarySearch(new int[3], 0, -1, ""));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.BinarySearch(new string[3], 0, -1, ""));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.BinarySearch(new int[3], 0, -1, "", null));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.BinarySearch(new string[3], 0, -1, "", null));
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(0, 1, 0)]
        [InlineData(3, 0, 4)]
        [InlineData(3, 1, 3)]
        [InlineData(3, 3, 1)]
        public static void BinarySearch_IndexPlusLengthInvalid_ThrowsArgumentException(int count, int index, int length)
        {
            Assert.Throws<ArgumentException>(null, () => Array.BinarySearch(new int[count], index, length, ""));
            Assert.Throws<ArgumentException>(null, () => Array.BinarySearch(new string[count], index, length, ""));
            Assert.Throws<ArgumentException>(null, () => Array.BinarySearch(new int[count], index, length, "", null));
            Assert.Throws<ArgumentException>(null, () => Array.BinarySearch(new string[count], index, length, "", null));
        }

        [Theory]
        [InlineData(typeof(object), 0)]
        [InlineData(typeof(object), 2)]
        [InlineData(typeof(int), 0)]
        [InlineData(typeof(int), 2)]
        [InlineData(typeof(IntPtr), 0)]
        [InlineData(typeof(IntPtr), 2)]
        [InlineData(typeof(UIntPtr), 0)]
        [InlineData(typeof(UIntPtr), 2)]
        public static void BinarySearch_CountZero_ValueInvalidType_DoesNotThrow(Type elementType, int length)
        {
            Array array = Array.CreateInstance(elementType, length);
            Assert.Equal(-1, Array.BinarySearch(array, 0, 0, new object()));
        }

        [Fact]
        public static void GetValue_SetValue()
        {
            var intArray = new int[] { 7, 8, 9 };
            Array array = intArray;

            Assert.Equal(7, array.GetValue(0));
            array.SetValue(41, 0);
            Assert.Equal(41, intArray[0]);

            Assert.Equal(8, array.GetValue(1));
            array.SetValue(42, 1);
            Assert.Equal(42, intArray[1]);

            Assert.Equal(9, array.GetValue(2));
            array.SetValue(43, 2);
            Assert.Equal(43, intArray[2]);

            array = new int[,] { { 1, 2, 3 }, { 4, 5, 6 } };
            Assert.Equal(1, array.GetValue(0, 0));
            Assert.Equal(6, array.GetValue(1, 2));
            array.SetValue(42, 1, 2);
            Assert.Equal(42, array.GetValue(1, 2));

            array = Array.CreateInstance(typeof(int), 2, 3, 4);
            array.SetValue(42, 1, 2, 3);
            Assert.Equal(42, array.GetValue(1, 2, 3));

            array = Array.CreateInstance(typeof(int), 2, 3, 4, 5);
            array.SetValue(42, 1, 2, 3, 4);
            Assert.Equal(42, array.GetValue(1, 2, 3, 4));
        }

        [Fact]
        public static void GetValue_Invalid()
        {
            Assert.Throws<IndexOutOfRangeException>(() => new int[10].GetValue(-1)); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => new int[10].GetValue(10)); // Index >= array.Length
            Assert.Throws<ArgumentException>(null, () => new int[10, 10].GetValue(0)); // Array is multidimensional

            Assert.Throws<ArgumentNullException>("indices", () => new int[10].GetValue((int[])null)); // Indices is null
            Assert.Throws<ArgumentException>(null, () => new int[10, 10].GetValue(new int[] { 1, 2, 3 })); // Indices.Length > array.Rank

            Assert.Throws<IndexOutOfRangeException>(() => new int[8, 10].GetValue(new int[] { -1, 2 })); // Indices[0] < 0
            Assert.Throws<IndexOutOfRangeException>(() => new int[8, 10].GetValue(new int[] { 9, 2 })); // Indices[0] > array.GetLength(0)

            Assert.Throws<IndexOutOfRangeException>(() => new int[10, 8].GetValue(new int[] { 1, -1 })); // Indices[1] < 0
            Assert.Throws<IndexOutOfRangeException>(() => new int[10, 8].GetValue(new int[] { 1, 9 })); // Indices[1] > array.GetLength(1)
        }

        [Fact]
        public static unsafe void GetValue_ArrayOfPointers_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => new int*[2].GetValue(0));
        }

        [Fact]
        public static unsafe void SetValue_ArrayOfPointers_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => new int*[2].SetValue(null, 0));
        }

        [Theory]
        [InlineData(new int[] { 7, 8, 9 }, 0, 3, new int[] { 0, 0, 0 })]
        [InlineData(new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 }, 0, 6, new int[] { 0, 0, 0, 0, 0, 0 })]
        [InlineData(new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 }, 2, 3, new int[] { 0x1234567, 0x789abcde, 0, 0, 0, 0x22446688 })]
        [InlineData(new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 }, 6, 0, new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 })]
        [InlineData(new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 }, 0, 0, new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 })]
        [InlineData(new string[] { "7", "8", "9" }, 0, 3, new string[] { null, null, null })]
        [InlineData(new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" }, 0, 6, new string[] { null, null, null, null, null, null })]
        [InlineData(new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" }, 2, 3, new string[] { "0x1234567", "0x789abcde", null, null, null, "0x22446688" })]
        [InlineData(new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" }, 6, 0, new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" })]
        [InlineData(new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" }, 0, 0, new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" })]
        public static void Clear(Array array, int index, int length, Array expected)
        {
            if (index == 0 && length == array.Length)
            {
                // Use IList.Clear()
                Array arrayClone1 = (Array)array.Clone();
                ((IList)arrayClone1).Clear();
                Assert.Equal(expected, arrayClone1);
            }
            Array arrayClone2 = (Array)array.Clone();
            Array.Clear(arrayClone2, index, length);
            Assert.Equal(expected, arrayClone2);
        }

        [Fact]
        public static void Clear_Struct_WithReferenceAndValueTypeFields_Array()
        {
            var array = new NonGenericStruct[]
            {
            new NonGenericStruct { x = 1, s = "Hello", z = 2 },
            new NonGenericStruct { x = 2, s = "Hello", z = 3 },
            new NonGenericStruct { x = 3, s = "Hello", z = 4 },
            new NonGenericStruct { x = 4, s = "Hello", z = 5 },
            new NonGenericStruct { x = 5, s = "Hello", z = 6 }
            };

            Array.Clear(array, 0, 5);
            for (int i = 0; i < array.Length; i++)
            {
                Assert.Equal(0, array[i].x);
                Assert.Null(array[i].s);
                Assert.Equal(0, array[i].z);
            }

            array = new NonGenericStruct[]
            {
            new NonGenericStruct { x = 1, s = "Hello", z = 2 },
            new NonGenericStruct { x = 2, s = "Hello", z = 3 },
            new NonGenericStruct { x = 3, s = "Hello", z = 4 },
            new NonGenericStruct { x = 4, s = "Hello", z = 5 },
            new NonGenericStruct { x = 5, s = "Hello", z = 6 }
            };

            Array.Clear(array, 2, 3);

            Assert.Equal(1, array[0].x);
            Assert.Equal("Hello", array[0].s);
            Assert.Equal(2, array[0].z);

            Assert.Equal(2, array[1].x);
            Assert.Equal("Hello", array[1].s);
            Assert.Equal(3, array[1].z);

            for (int i = 2; i < 2 + 3; i++)
            {
                Assert.Equal(0, array[i].x);
                Assert.Null(array[i].s);
                Assert.Equal(0, array[i].z);
            }
        }

        [Fact]
        public static void Clear_Invalid()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.Clear(null, 0, 0)); // Array is null

            Assert.Throws<IndexOutOfRangeException>(() => Array.Clear(new int[10], -1, 0)); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => Array.Clear(new int[10], 0, -1)); // Length < 0 

            // Index + length > array.Length
            Assert.Throws<IndexOutOfRangeException>(() => Array.Clear(new int[10], 0, 11));
            Assert.Throws<IndexOutOfRangeException>(() => Array.Clear(new int[10], 10, 1));
            Assert.Throws<IndexOutOfRangeException>(() => Array.Clear(new int[10], 9, 2));
            Assert.Throws<IndexOutOfRangeException>(() => Array.Clear(new int[10], 6, 0x7fffffff));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new char[] { '1', '2', '3' })]
        public static void Clone(Array array)
        {
            Array clone = (Array)array.Clone();
            Assert.Equal(clone, array);
            Assert.NotSame(clone, array);
        }

        public static IEnumerable<object[]> Copy_Array_Reliable_TestData()
        {
            // Array -> SZArray
            Array lowerBoundArray1 = Array.CreateInstance(typeof(int), new int[] { 1 }, new int[] { 1 });
            lowerBoundArray1.SetValue(2, lowerBoundArray1.GetLowerBound(0));
            yield return new object[] { lowerBoundArray1, lowerBoundArray1.GetLowerBound(0), new int[1], 0, 1, new int[] { 2 } };

            // SZArray -> Array
            Array lowerBoundArray2 = Array.CreateInstance(typeof(int), new int[] { 1 }, new int[] { 1 });
            yield return new object[] { new int[] { 2 }, 0, lowerBoundArray2, lowerBoundArray2.GetLowerBound(0), 1, lowerBoundArray1 };

            // int[,] -> int[,]
            int[,] intRank2Array = new int[,] { { 1, 2, 3 }, { 4, 5, 6 } };
            yield return new object[] { intRank2Array, 0, new int[2, 3], 0, 6, intRank2Array };
            yield return new object[] { intRank2Array, 0, new int[3, 2], 0, 6, new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } } };
            yield return new object[] { intRank2Array, 1, new int[2, 3], 2, 3, new int[,] { { 0, 0, 2 }, { 3, 4, 0 } } };

            // object[,] -> object[,]
            object[,] objectRank2Array = new object[,] { { 1, 2, 3 }, { 4, 5, 6 } };
            yield return new object[] { objectRank2Array, 0, new object[2, 3], 0, 6, objectRank2Array };
            yield return new object[] { objectRank2Array, 0, new object[3, 2], 0, 6, new object[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } } };
            yield return new object[] { objectRank2Array, 1, new object[2, 3], 2, 3, new object[,] { { null, null, 2 }, { 3, 4, null } } };
        }

        public static IEnumerable<object[]> Copy_SZArray_Reliable_TestData()
        {
            // Int64[] -> Int64[]
            yield return new object[] { new long[] { 1, 2, 3 }, 0, new long[3], 0, 3, new long[] { 1, 2, 3 } };
            yield return new object[] { new long[] { 1, 2, 3 }, 1, new long[] { 1, 2, 3, 4, 5 }, 2, 2, new long[] { 1, 2, 2, 3, 5 } };
            
            // UInt64[] -> UInt64[]
            yield return new object[] { new ulong[] { 1, 2, 3 }, 0, new ulong[3], 0, 3, new ulong[] { 1, 2, 3 } };
            yield return new object[] { new ulong[] { 1, 2, 3 }, 1, new ulong[] { 1, 2, 3, 4, 5 }, 2, 2, new ulong[] { 1, 2, 2, 3, 5 } };

            // UInt32[] -> UInt32[]
            yield return new object[] { new uint[] { 1, 2, 3 }, 0, new uint[3], 0, 3, new uint[] { 1, 2, 3 } };
            yield return new object[] { new uint[] { 1, 2, 3 }, 1, new uint[] { 1, 2, 3, 4, 5 }, 2, 2, new uint[] { 1, 2, 2, 3, 5 } };

            // Int32[] -> Int32[]
            yield return new object[] { new int[] { 1, 2, 3 }, 0, new int[3], 0, 3, new int[] { 1, 2, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 1, new int[] { 1, 2, 3, 4, 5 }, 2, 2, new int[] { 1, 2, 2, 3, 5 } };

            // Int16[] -> Int16[]
            yield return new object[] { new short[] { 1, 2, 3 }, 0, new short[3], 0, 3, new short[] { 1, 2, 3 } };
            yield return new object[] { new short[] { 1, 2, 3 }, 1, new short[] { 1, 2, 3, 4, 5 }, 2, 2, new short[] { 1, 2, 2, 3, 5 } };

            // UInt16[] -> UInt16[]
            yield return new object[] { new ushort[] { 1, 2, 3 }, 0, new ushort[3], 0, 3, new ushort[] { 1, 2, 3 } };
            yield return new object[] { new ushort[] { 1, 2, 3 }, 1, new ushort[] { 1, 2, 3, 4, 5 }, 2, 2, new ushort[] { 1, 2, 2, 3, 5 } };

            // SByte[] -> SByte[]
            yield return new object[] { new sbyte[] { 1, 2, 3 }, 0, new sbyte[3], 0, 3, new sbyte[] { 1, 2, 3 } };
            yield return new object[] { new sbyte[] { 1, 2, 3 }, 1, new sbyte[] { 1, 2, 3, 4, 5 }, 2, 2, new sbyte[] { 1, 2, 2, 3, 5 } };

            // Byte[] -> Byte[]
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new byte[3], 0, 3, new byte[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 1, new byte[] { 1, 2, 3, 4, 5 }, 2, 2, new byte[] { 1, 2, 2, 3, 5 } };

            // Char[] -> Char[]
            yield return new object[] { new char[] { '1', '2', '3' }, 0, new char[3], 0, 3, new char[] { '1', '2', '3' } };
            yield return new object[] { new char[] { '1', '2', '3' }, 1, new char[] { '1', '2', '3', '4', '5' }, 2, 2, new char[] { '1', '2', '2', '3', '5' } };
            
            // Bool[] -> Bool[]
            yield return new object[] { new bool[] { false, true, false }, 0, new bool[3], 0, 3, new bool[] { false, true, false } };
            yield return new object[] { new bool[] { false, true, false }, 1, new bool[] { false, true, false, true, false }, 2, 2, new bool[] { false, true, true, false, false } };

            // Single[] -> Single[]
            yield return new object[] { new float[] { 1, 2.2f, 3 }, 0, new float[3], 0, 3, new float[] { 1, 2.2f, 3 } };
            yield return new object[] { new float[] { 1, 2.2f, 3 }, 1, new float[] { 1, 2, 3.3f, 4, 5 }, 2, 2, new float[] { 1, 2, 2.2f, 3, 5 } };
            
            // Double[] -> Double[]
            yield return new object[] { new double[] { 1, 2.2, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2.2, 3 } };
            yield return new object[] { new double[] { 1, 2.2, 3 }, 1, new double[] { 1, 2, 3.3, 4, 5 }, 2, 2, new double[] { 1, 2, 2.2, 3, 5 } };
            
            // IntPtr[] -> IntPtr[]
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3 }, 0, new IntPtr[3], 0, 3, new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3 } };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3 }, 1, new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5 }, 2, 2, new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)2, (IntPtr)3, (IntPtr)5 } };

            // UIntPtr[] -> UIntPtr[]
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3 }, 0, new UIntPtr[3], 0, 3, new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3 } };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3 }, 1, new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)4, (UIntPtr)5 }, 2, 2, new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)2, (UIntPtr)3, (UIntPtr)5 } };

            // String[] -> String[]
            yield return new object[] { new string[] { "1", "2", "3" }, 0, new string[3], 0, 3, new string[] { "1", "2", "3" } };
            yield return new object[] { new string[] { "1", "2", "3" }, 1, new string[] { "1", "2", "3", "4", "5" }, 2, 2, new string[] { "1", "2", "2", "3", "5" } };

            // IntEnum[] conversions
            yield return new object[] { new Int32Enum[] { (Int32Enum)1, (Int32Enum)2, (Int32Enum)3 }, 0, new Int32Enum[3], 0, 3, new Int32Enum[] { (Int32Enum)1, (Int32Enum)2, (Int32Enum)3 } };
            yield return new object[] { new Int32Enum[] { (Int32Enum)1, (Int32Enum)2, (Int32Enum)3 }, 1, new Int32Enum[] { (Int32Enum)1, (Int32Enum)2, (Int32Enum)3, (Int32Enum)4, (Int32Enum)5 }, 2, 2, new Int32Enum[] { (Int32Enum)1, (Int32Enum)2, (Int32Enum)2, (Int32Enum)3, (Int32Enum)5 } };
            yield return new object[] { new Int32Enum[] { (Int32Enum)1 }, 0, new int[1], 0, 1, new int[] { 1 } };

            // Misc
            yield return new object[] { new int[] { 0x12345678, 0x22334455, 0x778899aa }, 0, new int[3], 0, 3, new int[] { 0x12345678, 0x22334455, 0x778899aa } };

            int[] intArray1 = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
            yield return new object[] { intArray1, 3, intArray1, 2, 2, new int[] { 0x12345678, 0x22334455, 0x55443322, 0x33445566, 0x33445566 } };

            int[] intArray2 = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
            yield return new object[] { intArray2, 2, intArray2, 3, 2, new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x778899aa, 0x55443322 } };

            yield return new object[] { new string[] { "Red", "Green", null, "Blue" }, 0, new string[] { "X", "X", "X", "X" }, 0, 4, new string[] { "Red", "Green", null, "Blue" } };

            string[] stringArray = new string[] { "Red", "Green", null, "Blue" };
            yield return new object[] { stringArray, 1, stringArray, 2, 2, new string[] { "Red", "Green", "Green", null } };
            
            // Struct[] -> Struct[]
            NonGenericStruct[] structArray1 = CreateStructArray();
            yield return new object[] { structArray1, 0, new NonGenericStruct[5], 0, 5, structArray1 };

            // Struct[] overlaps
            NonGenericStruct[] structArray2 = CreateStructArray();
            NonGenericStruct[] overlappingStructArrayExpected = new NonGenericStruct[]
            {
                new NonGenericStruct { x = 1, s = "Hello1", z = 2 },
                new NonGenericStruct { x = 2, s = "Hello2", z = 3 },
                new NonGenericStruct { x = 2, s = "Hello2", z = 3 },
                new NonGenericStruct { x = 3, s = "Hello3", z = 4 },
                new NonGenericStruct { x = 4, s = "Hello4", z = 5 }
            };
            yield return new object[] { structArray2, 1, structArray2, 2, 3, overlappingStructArrayExpected };

            // SubClass[] -> BaseClass[]
            yield return new object[] { new NonGenericSubClass1[10], 0, new NonGenericClass1[10], 0, 10, new NonGenericClass1[10] };
        }
        
        public static IEnumerable<object[]> Copy_SZArray_PrimitiveWidening_TestData()
        {
            // Int64[] -> primitive[]
            yield return new object[] { new long[] { 1, 2, 3 }, 0, new float[3], 0, 3, new float[] { 1, 2, 3 } };
            yield return new object[] { new long[] { 1, 2, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2, 3 } };

            // UInt64[] -> primitive[]
            yield return new object[] { new ulong[] { 1, 2, 3 }, 0, new float[3], 0, 3, new float[] { 1, 2, 3 } };
            yield return new object[] { new ulong[] { 1, 2, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2, 3 } };

            // Int32[] -> primitive[]
            yield return new object[] { new int[] { 1, 2, 3 }, 0, new long[3], 0, 3, new long[] { 1, 2, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 0, new float[3], 0, 3, new float[] { 1, 2, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2, 3 } };

            // UInt32[] -> primitive[]
            yield return new object[] { new uint[] { 1, 2, 3 }, 0, new long[3], 0, 3, new long[] { 1, 2, 3 } };
            yield return new object[] { new uint[] { 1, 2, 3 }, 0, new ulong[3], 0, 3, new ulong[] { 1, 2, 3 } };
            yield return new object[] { new uint[] { 1, 2, 3 }, 0, new float[3], 0, 3, new float[] { 1, 2, 3 } };
            yield return new object[] { new uint[] { 1, 2, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2, 3 } };

            // Int16[] -> primitive[]
            yield return new object[] { new short[] { 1, 2, 3 }, 0, new long[3], 0, 3, new long[] { 1, 2, 3 } };
            yield return new object[] { new short[] { 1, 2, 3 }, 0, new int[3], 0, 3, new int[] { 1, 2, 3 } };
            yield return new object[] { new short[] { 1, 2, 3 }, 0, new float[3], 0, 3, new float[] { 1, 2, 3 } };
            yield return new object[] { new short[] { 1, 2, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2, 3 } };
            
            // UInt16[] -> primitive[]
            yield return new object[] { new ushort[] { 1, 2, 3 }, 0, new long[3], 0, 3, new long[] { 1, 2, 3 } };
            yield return new object[] { new ushort[] { 1, 2, 3 }, 0, new ulong[3], 0, 3, new ulong[] { 1, 2, 3 } };
            yield return new object[] { new ushort[] { 1, 2, 3 }, 0, new int[3], 0, 3, new int[] { 1, 2, 3 } };
            yield return new object[] { new ushort[] { 1, 2, 3 }, 0, new uint[3], 0, 3, new uint[] { 1, 2, 3 } };
            yield return new object[] { new ushort[] { 1, 2, 3 }, 0, new char[3], 0, 3, new char[] { (char)1, (char)2, (char)3 } };
            yield return new object[] { new ushort[] { 1, 2, 3 }, 0, new float[3], 0, 3, new float[] { 1, 2, 3 } };
            yield return new object[] { new ushort[] { 1, 2, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2, 3 } };

            // SByte[] -> primitive[]
            yield return new object[] { new sbyte[] { 1, 2, 3 }, 0, new long[3], 0, 3, new long[] { 1, 2, 3 } };
            yield return new object[] { new sbyte[] { 1, 2, 3 }, 0, new int[3], 0, 3, new int[] { 1, 2, 3 } };
            yield return new object[] { new sbyte[] { 1, 2, 3 }, 0, new short[3], 0, 3, new short[] { 1, 2, 3 } };
            yield return new object[] { new sbyte[] { 1, 2, 3 }, 0, new float[3], 0, 3, new float[] { 1, 2, 3 } };
            yield return new object[] { new sbyte[] { 1, 2, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2, 3 } };

            // Byte[] -> primitive[]
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new long[3], 0, 3, new long[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new ulong[3], 0, 3, new ulong[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new int[3], 0, 3, new int[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new uint[3], 0, 3, new uint[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new short[3], 0, 3, new short[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new ushort[3], 0, 3, new ushort[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new char[3], 0, 3, new char[] { (char)1, (char)2, (char)3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new float[3], 0, 3, new float[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2, 3 } };

            // Char[] -> primitive[]
            yield return new object[] { new char[] { (char)1, (char)2, (char)3 }, 0, new long[3], 0, 3, new long[] { 1, 2, 3 } };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3 }, 0, new ulong[3], 0, 3, new ulong[] { 1, 2, 3 } };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3 }, 0, new int[3], 0, 3, new int[] { 1, 2, 3 } };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3 }, 0, new uint[3], 0, 3, new uint[] { 1, 2, 3 } };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3 }, 0, new ushort[3], 0, 3, new ushort[] { 1, 2, 3 } };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3 }, 0, new float[3], 0, 3, new float[] { 1, 2, 3 } };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3 }, 0, new double[3], 0, 3, new double[] { 1, 2, 3 } };

            // Single[] -> primitive[]
            yield return new object[] { new float[] { 1, 2.2f, 3 }, 0, new double[3], 0, 3, new double[] { 1, 2.2f, 3 } };
        }

        public static IEnumerable<object[]> Copy_SZArray_UnreliableConversion_CanPerform_TestData()
        {
            // Interface1[] -> InterfaceImplementingInterface1[] works when all values are null
            yield return new object[] { new NonGenericInterface1[1], 0, new NonGenericInterfaceWithNonGenericInterface1[1], 0, 1, new NonGenericInterfaceWithNonGenericInterface1[1] };

            // Interface1[] -> Interface2[] works when values are all null
            yield return new object[] { new NonGenericInterface1[1], 0, new NonGenericInterface2[1], 0, 1, new NonGenericInterface2[1] };

            // Interface1[] -> Interface2[] works when values all implement Interface2
            ClassWithNonGenericInterface1_2 twoInterfacesClass = new ClassWithNonGenericInterface1_2();
            yield return new object[] { new NonGenericInterface1[] { twoInterfacesClass }, 0, new NonGenericInterface2[1], 0, 1, new NonGenericInterface2[] { twoInterfacesClass } };

            StructWithNonGenericInterface1_2 twoInterfacesStruct = new StructWithNonGenericInterface1_2();
            yield return new object[] { new NonGenericInterface1[] { twoInterfacesStruct }, 0, new NonGenericInterface2[1], 0, 1, new NonGenericInterface2[] { twoInterfacesStruct } };

            // Interface1[] -> Any[] works when values are all null
            yield return new object[] { new NonGenericInterface1[1], 0, new ClassWithNonGenericInterface1[1], 0, 1, new ClassWithNonGenericInterface1[1] };

            // Interface1[] -> Any[] works when values are all Any
            ClassWithNonGenericInterface1 oneInterfaceClass = new ClassWithNonGenericInterface1();
            yield return new object[] { new NonGenericInterface1[] { oneInterfaceClass }, 0, new ClassWithNonGenericInterface1[1], 0, 1, new ClassWithNonGenericInterface1[] { oneInterfaceClass } };

            StructWithNonGenericInterface1 oneInterfaceStruct = new StructWithNonGenericInterface1();
            yield return new object[] { new NonGenericInterface1[] { oneInterfaceStruct }, 0, new StructWithNonGenericInterface1[1], 0, 1, new StructWithNonGenericInterface1[] { oneInterfaceStruct } };

            // ReferenceType[] -> InterfaceNotImplementedByReferenceType[] works when values are all null
            yield return new object[] { new ClassWithNonGenericInterface1[1], 0, new NonGenericInterface2[1], 0, 1, new NonGenericInterface2[1] };

            // ValueType[] -> ReferenceType[]
            yield return new object[] { new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, new object[10], 5, 3, new object[] { null, null, null, null, null, 2, 3, 4, null, null } };
            yield return new object[] { new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, new IEquatable<int>[10], 5, 3, new IEquatable<int>[] { null, null, null, null, null, 2, 3, 4, null, null } };
            yield return new object[] { new int?[] { 0, 1, 2, default(int?), 4, 5, 6, 7, 8, 9 }, 2, new object[10], 5, 3, new object[] { null, null, null, null, null, 2, null, 4, null, null } };

            // ReferenceType[] -> ValueType[]
            yield return new object[] { new object[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc }, 5, 3, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 2, 3, 4, 0xcc, 0xcc } };
            yield return new object[] { new IEquatable<int>[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc }, 5, 3, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 2, 3, 4, 0xcc, 0xcc } };
            yield return new object[] { new IEquatable<int>[] { 0, new NotInt32(), 2, 3, 4, new NotInt32(), 6, 7, 8, 9 }, 2, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc }, 5, 3, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 2, 3, 4, 0xcc, 0xcc } };

            yield return new object[] { new object[] { 0, 1, 2, 3, null, 5, 6, 7, 8, 9 }, 2, new int?[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc }, 5, 3, new int?[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 2, 3, null, 0xcc, 0xcc } };

            // Struct[] -> object[]
            NonGenericStruct[] structArray1 = CreateStructArray();
            yield return new object[] { structArray1, 0, new object[5], 0, 5, structArray1.Select(g => (object)g).ToArray() };

            // BaseClass[] -> SubClass[]
            yield return new object[] { new NonGenericClass1[10], 0, new NonGenericSubClass1[10], 0, 10, new NonGenericSubClass1[10] };

            // Class[] -> Interface[]
            yield return new object[] { new NonGenericClass1[10], 0, new NonGenericInterface1[10], 0, 10, new NonGenericInterface1[10] };

            // Interface[] -> Class[]
            yield return new object[] { new NonGenericInterface1[10], 0, new NonGenericClass1[10], 0, 10, new NonGenericClass1[10] };
        }

        public static IEnumerable<object[]> Copy_Array_UnreliableConversion_CanPerform_TestData()
        {
            // int[,] -> long[,]
            int[,] intRank2Array = new int[,] { { 1, 2, 3 }, { 4, 5, 6 } };
            yield return new object[] { intRank2Array, 0, new long[2, 3], 0, 6, new long[,] { { 1, 2, 3 }, { 4, 5, 6 } } };

            // int[,] -> object[,]
            yield return new object[] { intRank2Array, 0, new object[2, 3], 0, 6, new object[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } } };
            yield return new object[] { intRank2Array, 0, new object[3, 2], 0, 6, new object[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } } };
            yield return new object[] { intRank2Array, 1, new object[2, 3], 2, 3, new object[,] { { null, null, 2 }, { 3, 4, null } } };

            // object[,] -> int[,]
            object[,] objectRank2Array = new object[,] { { 1, 2, 3 }, { 4, 5, 6 } };
            yield return new object[] { objectRank2Array, 0, new int[2, 3], 0, 6, intRank2Array };
            yield return new object[] { objectRank2Array, 0, new int[3, 2], 0, 6, new int[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } } };
            yield return new object[] { objectRank2Array, 1, new int[2, 3], 2, 3, new int[,] { { 0, 0, 2 }, { 3, 4, 0 } } };
        }

        [Theory]
        [MemberData(nameof(Copy_SZArray_Reliable_TestData))]
        [MemberData(nameof(Copy_SZArray_PrimitiveWidening_TestData))]
        [MemberData(nameof(Copy_SZArray_UnreliableConversion_CanPerform_TestData))]
        public static void Copy_SZArray(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, Array expected)
        {
            // Basic: forward SZArray
            Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, expected);

            // Advanced: convert SZArray to an array with non-zero lower bound
            const int LowerBound = 5;
            Array nonZeroSourceArray = NonZeroLowerBoundArray(sourceArray, LowerBound);
            Array nonZeroDestinationArray = sourceArray == destinationArray ? nonZeroSourceArray : NonZeroLowerBoundArray(destinationArray, LowerBound);
            Copy(nonZeroSourceArray, sourceIndex + LowerBound, nonZeroDestinationArray, destinationIndex + LowerBound, length, NonZeroLowerBoundArray(expected, LowerBound));

            if (sourceIndex == 0 && length == sourceArray.Length)
            {
                // CopyTo(Array, int)
                Array sourceClone = (Array)sourceArray.Clone();
                Array destinationArrayClone = sourceArray == destinationArray ? sourceClone : (Array)destinationArray.Clone();
                sourceClone.CopyTo(destinationArrayClone, destinationIndex);
                Assert.Equal(expected, destinationArrayClone);
            }
        }

        [Theory]
        [MemberData(nameof(Copy_Array_Reliable_TestData))]
        [MemberData(nameof(Copy_Array_UnreliableConversion_CanPerform_TestData))]
        public static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, Array expected)
        {
            bool overlaps = sourceArray == destinationArray;
            if (sourceIndex == sourceArray.GetLowerBound(0) && destinationIndex == destinationArray.GetLowerBound(0))
            {
                // Use Copy(Array, Array, int)
                Array sourceArrayClone1 = (Array)sourceArray.Clone();
                Array destinationArrayClone1 = overlaps ? sourceArrayClone1 : (Array)destinationArray.Clone();
                Array.Copy(sourceArrayClone1, destinationArrayClone1, length);
                Assert.Equal(expected, destinationArrayClone1);
            }
            // Use Copy(Array, int, Array, int, int)
            Array sourceArrayClone2 = (Array)sourceArray.Clone();
            Array destinationArrayClone2 = overlaps ? sourceArrayClone2 : (Array)destinationArray.Clone();
            Array.Copy(sourceArrayClone2, sourceIndex, destinationArrayClone2, destinationIndex, length);
            Assert.Equal(expected, destinationArrayClone2);
        }

        [Theory]
        [MemberData(nameof(Copy_SZArray_Reliable_TestData))]
        [MemberData(nameof(Copy_Array_Reliable_TestData))]
        public static void ConstrainedCopy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, Array expected)
        {
            Array sourceArrayClone = (Array)sourceArray.Clone();
            Array destinationArrayClone = sourceArray == destinationArray ? sourceArrayClone : (Array)destinationArray.Clone();
            Array.ConstrainedCopy(sourceArrayClone, sourceIndex, destinationArrayClone, destinationIndex, length);
            Assert.Equal(expected, destinationArrayClone);
        }

        [Fact]
        public static void Copy_NullSourceArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("sourceArray", () => Array.Copy(null, new string[10], 0));
            Assert.Throws<ArgumentNullException>("source", () => Array.Copy(null, 0, new string[10], 0, 0));
            Assert.Throws<ArgumentNullException>("source", () => Array.ConstrainedCopy(null, 0, new string[10], 0, 0));
        }

        [Fact]
        public static void Copy_NullDestinationArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("destinationArray", () => Array.Copy(new string[10], null, 0));
            Assert.Throws<ArgumentNullException>("dest", () => Array.Copy(new string[10], 0, null, 0, 0));
            Assert.Throws<ArgumentNullException>("dest", () => Array.ConstrainedCopy(new string[10], 0, null, 0, 0));

            Assert.Throws<ArgumentNullException>("dest", () => new string[10].CopyTo(null, 0));
        }

        [Fact]
        public static void Copy_SourceAndDestinationArrayHaveDifferentRanks_ThrowsRankException()
        {
            Assert.Throws<RankException>(() => Array.Copy(new string[10, 10], new string[10], 0));
            Assert.Throws<RankException>(() => Array.Copy(new string[10, 10], 0, new string[10], 0, 0));
            Assert.Throws<RankException>(() => Array.ConstrainedCopy(new string[10, 10], 0, new string[10], 0, 0));
        }

        public static IEnumerable<object[]> Copy_SourceAndDestinationNeverConvertible_TestData()
        {
            yield return new object[] { new string[1], new int[1] };
            yield return new object[] { new int[1], new string[1] };
            yield return new object[] { new int[1], new IEnumerable<int>[1] };

            // Invalid jagged array
            yield return new object[] { new int[1][], new int[1][,] };
            yield return new object[] { new int[1][,], new int[1][] };
            yield return new object[] { new int[1][], new string[1][] };
            yield return new object[] { new string[1][], new int[1][] };

            // Can't primitive widen arrays
            yield return new object[] { new char[1][], new ushort[1][] };
            yield return new object[] { new ushort[1][], new char[1][] };

            // Can't primitive widen Int64
            yield return new object[] { new long[1], new ulong[1] };
            yield return new object[] { new long[1], new int[1] };
            yield return new object[] { new long[1], new uint[1] };
            yield return new object[] { new long[1], new short[1] };
            yield return new object[] { new long[1], new ushort[1] };
            yield return new object[] { new long[1], new sbyte[1] };
            yield return new object[] { new long[1], new byte[1] };
            yield return new object[] { new long[1], new char[1] };
            yield return new object[] { new long[1], new bool[1] };
            yield return new object[] { new long[1], new IntPtr[1] };
            yield return new object[] { new long[1], new UIntPtr[1] };

            // Can't primitive widen UInt64
            yield return new object[] { new ulong[1], new long[1] };
            yield return new object[] { new ulong[1], new int[1] };
            yield return new object[] { new ulong[1], new uint[1] };
            yield return new object[] { new ulong[1], new short[1] };
            yield return new object[] { new ulong[1], new ushort[1] };
            yield return new object[] { new ulong[1], new sbyte[1] };
            yield return new object[] { new ulong[1], new byte[1] };
            yield return new object[] { new ulong[1], new char[1] };
            yield return new object[] { new ulong[1], new bool[1] };
            yield return new object[] { new ulong[1], new IntPtr[1] };
            yield return new object[] { new ulong[1], new UIntPtr[1] };

            // Can't primitive widen Int32
            yield return new object[] { new int[1], new ulong[1] };
            yield return new object[] { new int[1], new uint[1] };
            yield return new object[] { new int[1], new short[1] };
            yield return new object[] { new int[1], new ushort[1] };
            yield return new object[] { new int[1], new sbyte[1] };
            yield return new object[] { new int[1], new byte[1] };
            yield return new object[] { new int[1], new char[1] };
            yield return new object[] { new int[1], new bool[1] };
            yield return new object[] { new int[1], new IntPtr[1] };
            yield return new object[] { new int[1], new UIntPtr[1] };

            // Can't primitive widen UInt32
            yield return new object[] { new uint[1], new short[1] };
            yield return new object[] { new uint[1], new ushort[1] };
            yield return new object[] { new uint[1], new sbyte[1] };
            yield return new object[] { new uint[1], new byte[1] };
            yield return new object[] { new uint[1], new char[1] };
            yield return new object[] { new uint[1], new bool[1] };
            yield return new object[] { new uint[1], new IntPtr[1] };
            yield return new object[] { new uint[1], new UIntPtr[1] };

            // Can't primitive widen Int16
            yield return new object[] { new short[1], new ulong[1] };
            yield return new object[] { new short[1], new ushort[1] };
            yield return new object[] { new short[1], new ushort[1] };
            yield return new object[] { new short[1], new sbyte[1] };
            yield return new object[] { new short[1], new byte[1] };
            yield return new object[] { new short[1], new char[1] };
            yield return new object[] { new short[1], new bool[1] };
            yield return new object[] { new short[1], new IntPtr[1] };
            yield return new object[] { new short[1], new UIntPtr[1] };

            // Can't primitive widen UInt16
            yield return new object[] { new ushort[1], new sbyte[1] };
            yield return new object[] { new ushort[1], new byte[1] };
            yield return new object[] { new ushort[1], new bool[1] };
            yield return new object[] { new ushort[1], new IntPtr[1] };
            yield return new object[] { new ushort[1], new UIntPtr[1] };

            // Can't primitive widen SByte
            yield return new object[] { new sbyte[1], new ulong[1] };
            yield return new object[] { new sbyte[1], new uint[1] };
            yield return new object[] { new sbyte[1], new ushort[1] };
            yield return new object[] { new sbyte[1], new byte[1] };
            yield return new object[] { new sbyte[1], new char[1] };
            yield return new object[] { new sbyte[1], new bool[1] };
            yield return new object[] { new sbyte[1], new IntPtr[1] };
            yield return new object[] { new sbyte[1], new UIntPtr[1] };

            // Can't primitive widen Byte
            yield return new object[] { new byte[1], new sbyte[1] };
            yield return new object[] { new byte[1], new bool[1] };
            yield return new object[] { new byte[1], new IntPtr[1] };
            yield return new object[] { new byte[1], new UIntPtr[1] };

            // Can't primitive widen Bool
            yield return new object[] { new bool[1], new long[1] };
            yield return new object[] { new bool[1], new ulong[1] };
            yield return new object[] { new bool[1], new int[1] };
            yield return new object[] { new bool[1], new uint[1] };
            yield return new object[] { new bool[1], new short[1] };
            yield return new object[] { new bool[1], new ushort[1] };
            yield return new object[] { new bool[1], new sbyte[1] };
            yield return new object[] { new bool[1], new byte[1] };
            yield return new object[] { new bool[1], new char[1] };
            yield return new object[] { new bool[1], new float[1] };
            yield return new object[] { new bool[1], new double[1] };
            yield return new object[] { new bool[1], new IntPtr[1] };
            yield return new object[] { new bool[1], new UIntPtr[1] };

            // Can't primitive widen Single
            yield return new object[] { new float[1], new long[1] };
            yield return new object[] { new float[1], new ulong[1] };
            yield return new object[] { new float[1], new int[1] };
            yield return new object[] { new float[1], new uint[1] };
            yield return new object[] { new float[1], new short[1] };
            yield return new object[] { new float[1], new ushort[1] };
            yield return new object[] { new float[1], new sbyte[1] };
            yield return new object[] { new float[1], new byte[1] };
            yield return new object[] { new float[1], new char[1] };
            yield return new object[] { new float[1], new bool[1] };
            yield return new object[] { new float[1], new IntPtr[1] };
            yield return new object[] { new float[1], new UIntPtr[1] };

            // Can't primitive widen Double
            yield return new object[] { new double[1], new long[1] };
            yield return new object[] { new double[1], new ulong[1] };
            yield return new object[] { new double[1], new int[1] };
            yield return new object[] { new double[1], new uint[1] };
            yield return new object[] { new double[1], new short[1] };
            yield return new object[] { new double[1], new ushort[1] };
            yield return new object[] { new double[1], new sbyte[1] };
            yield return new object[] { new double[1], new byte[1] };
            yield return new object[] { new double[1], new char[1] };
            yield return new object[] { new double[1], new bool[1] };
            yield return new object[] { new double[1], new float[1] };
            yield return new object[] { new double[1], new IntPtr[1] };
            yield return new object[] { new double[1], new UIntPtr[1] };

            // Can't primitive widen IntPtr
            yield return new object[] { new IntPtr[1], new long[1] };
            yield return new object[] { new IntPtr[1], new ulong[1] };
            yield return new object[] { new IntPtr[1], new int[1] };
            yield return new object[] { new IntPtr[1], new uint[1] };
            yield return new object[] { new IntPtr[1], new short[1] };
            yield return new object[] { new IntPtr[1], new ushort[1] };
            yield return new object[] { new IntPtr[1], new sbyte[1] };
            yield return new object[] { new IntPtr[1], new byte[1] };
            yield return new object[] { new IntPtr[1], new char[1] };
            yield return new object[] { new IntPtr[1], new bool[1] };
            yield return new object[] { new IntPtr[1], new float[1] };
            yield return new object[] { new IntPtr[1], new double[1] };
            yield return new object[] { new IntPtr[1], new UIntPtr[1] };

            // Can't primitive widen UIntPtr
            yield return new object[] { new UIntPtr[1], new long[1] };
            yield return new object[] { new UIntPtr[1], new ulong[1] };
            yield return new object[] { new UIntPtr[1], new int[1] };
            yield return new object[] { new UIntPtr[1], new uint[1] };
            yield return new object[] { new UIntPtr[1], new short[1] };
            yield return new object[] { new UIntPtr[1], new ushort[1] };
            yield return new object[] { new UIntPtr[1], new sbyte[1] };
            yield return new object[] { new UIntPtr[1], new byte[1] };
            yield return new object[] { new UIntPtr[1], new char[1] };
            yield return new object[] { new UIntPtr[1], new bool[1] };
            yield return new object[] { new UIntPtr[1], new float[1] };
            yield return new object[] { new UIntPtr[1], new double[1] };
            yield return new object[] { new UIntPtr[1], new IntPtr[1] };

            // Interface[] -> Any[] only works if Any implements Interface
            yield return new object[] { new NonGenericInterface2[1], new StructWithNonGenericInterface1[1] };

            // ValueType[] -> InterfaceNotImplementedByValueType[] never works
            yield return new object[] { new StructWithNonGenericInterface1[1], new NonGenericInterface2[1] };

            // Can't get Enum from its underlying type
            yield return new object[] { new int[1], new Int32Enum[1] };

            // Can't primitive widen Enum
            yield return new object[] { new Int32Enum[1], new long[1] };
            yield return new object[] { new Int32Enum[1], new Int64Enum[1] };
        }

        [Theory]
        [MemberData(nameof(Copy_SourceAndDestinationNeverConvertible_TestData))]
        public static void Copy_SourceAndDestinationNeverConvertible_ThrowsArrayTypeMismatchException(Array sourceArray, Array destinationArray)
        {
            Assert.Throws<ArrayTypeMismatchException>(() => Array.Copy(sourceArray, destinationArray, 0));
            Assert.Throws<ArrayTypeMismatchException>(() => Array.Copy(sourceArray, sourceArray.GetLowerBound(0), destinationArray, destinationArray.GetLowerBound(0), 0));
            Assert.Throws<ArrayTypeMismatchException>(() => sourceArray.CopyTo(destinationArray, destinationArray.GetLowerBound(0)));
        }

        [Fact]
        public static unsafe void Copy_PointerArrayToNonPointerArray_ThrowsArrayTypeMismatchException()
        {
            Copy_SourceAndDestinationNeverConvertible_ThrowsArrayTypeMismatchException(new int[1], new int*[1]);
            Copy_SourceAndDestinationNeverConvertible_ThrowsArrayTypeMismatchException(new int*[1], new int[1]);
        }

        public static IEnumerable<object[]> Copy_UnreliableCoversion_CantPerform_TestData()
        {
            yield return new object[] { new object[] { "1" }, new int[1] };

            IEquatable<int>[] interfaceArray1 = new IEquatable<int>[10] { 0, 0, 0, 0, new NotInt32(), 0, 0, 0, 0, 0 };
            yield return new object[] { interfaceArray1, new int[10]};

            IEquatable<int>[] interfaceArray2 = new IEquatable<int>[10] { 0, 0, 0, 0, new NotInt32(), 0, 0, 0, 0, 0 };
            yield return new object[] { interfaceArray2, new int[10] };

            // Interface1[] -> Interface2[] when an Interface1 can't be assigned to Interface2
            yield return new object[] { new NonGenericInterface1[] { new StructWithNonGenericInterface1() }, new NonGenericInterface2[1] };
            yield return new object[] { new NonGenericInterface1[] { new StructWithNonGenericInterface1() }, new NonGenericInterface2[1] };
            yield return new object[] { new NonGenericInterface1[] { new ClassWithNonGenericInterface1() }, new NonGenericInterfaceWithNonGenericInterface1[1] };
            yield return new object[] { new NonGenericInterface1[] { new StructWithNonGenericInterface1() }, new NonGenericInterfaceWithNonGenericInterface1[1] };

            // Interface1[] -> ValueType[] when an Interface1 is null
            yield return new object[] { new NonGenericInterface1[1], new StructWithNonGenericInterface1[1] };

            // Interface1[] -> ValueType[] when an Interface1 can't be assigned to ValueType
            yield return new object[] { new NonGenericInterface1[] { new ClassWithNonGenericInterface1() }, new StructWithNonGenericInterface1[1] };
        }

        [Theory]
        [MemberData(nameof(Copy_UnreliableCoversion_CantPerform_TestData))]
        public static void Copy_UnreliableConverson_CantPerform_ThrowsInvalidCastException(Array sourceArray, Array destinationArray)
        {
            int length = Math.Min(sourceArray.Length, destinationArray.Length);
            Assert.Throws<InvalidCastException>(() => Array.Copy(sourceArray, destinationArray, length));
            Assert.Throws<InvalidCastException>(() => Array.Copy(sourceArray, sourceArray.GetLowerBound(0), destinationArray, destinationArray.GetLowerBound(0), length));

            Assert.Throws<InvalidCastException>(() => sourceArray.CopyTo(destinationArray, destinationArray.GetLowerBound(0)));

            // No exception is thrown if length == 0, as conversion error checking occurs during, not before copying
            Array.Copy(sourceArray, sourceArray.GetLowerBound(0), destinationArray, destinationArray.GetLowerBound(0), 0);
        }

        [Theory]
        [MemberData(nameof(Copy_UnreliableCoversion_CantPerform_TestData))]
        public static void ConstrainedCopy_UnreliableConversion_CantPerform_ThrowsArrayTypeMismatchException(Array sourceArray, Array destinationArray)
        {
            int length = Math.Min(sourceArray.Length, destinationArray.Length);
            ConstrainedCopy_UnreliableConversion_ThrowsArrayTypeMismatchException(sourceArray, sourceArray.GetLowerBound(0), destinationArray, destinationArray.GetLowerBound(0), length, null);
        }

        [Theory]
        [MemberData(nameof(Copy_SZArray_PrimitiveWidening_TestData))]
        [MemberData(nameof(Copy_SZArray_UnreliableConversion_CanPerform_TestData))]
        [MemberData(nameof(Copy_Array_UnreliableConversion_CanPerform_TestData))]
        public static void ConstrainedCopy_UnreliableConversion_ThrowsArrayTypeMismatchException(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, Array _)
        {
            Assert.Throws<ArrayTypeMismatchException>(() => Array.ConstrainedCopy(sourceArray, sourceIndex, destinationArray, destinationIndex, length));
        }

        [Fact]
        public static void Copy_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Copy(new string[10], new string[10], -1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Copy(new string[10], 0, new string[10], 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.ConstrainedCopy(new string[10], 0, new string[10], 0, -1));
        }

        [Theory]
        [InlineData(8, 0, 10, 0, 9)]
        [InlineData(8, 8, 10, 0, 1)]
        [InlineData(8, 9, 10, 0, 0)]
        [InlineData(10, 0, 8, 0, 9)]
        [InlineData(10, 0, 8, 8, 1)]
        [InlineData(10, 0, 8, 9, 0)]
        public static void Copy_IndexPlusLengthGreaterThanArrayLength_ThrowsArgumentException(int sourceCount, int sourceIndex, int destinationCount, int destinationIndex, int count)
        {
            if (sourceIndex == 0 && destinationIndex == 0)
            {
                Assert.Throws<ArgumentException>("", () => Array.Copy(new string[sourceCount], new string[destinationCount], count));
            }
            Assert.Throws<ArgumentException>("", () => Array.Copy(new string[sourceCount], sourceIndex, new string[destinationCount], destinationIndex, count));
            Assert.Throws<ArgumentException>("", () => Array.ConstrainedCopy(new string[sourceCount], sourceIndex, new string[destinationCount], destinationIndex, count));
        }

        [Fact]
        public static void Copy_StartIndexNegative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("srcIndex", () => Array.Copy(new string[10], -1, new string[10], 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>("srcIndex", () => Array.ConstrainedCopy(new string[10], -1, new string[10], 0, 0));
        }

        [Fact]
        public static void Copy_DestinationIndexNegative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("dstIndex", () => Array.Copy(new string[10], 0, new string[10], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("dstIndex", () => Array.ConstrainedCopy(new string[10], 0, new string[10], -1, 0));

            Assert.Throws<ArgumentOutOfRangeException>("dstIndex", () => new string[10].CopyTo(new string[10], -1));
        }

        [Fact]
        public static void CopyTo_SourceMultiDimensional_ThrowsRankException()
        {
            Assert.Throws<RankException>(() => new int[3, 3].CopyTo(new int[3], 0));
        }

        [Fact]
        public static void CopyTo_DestinationMultiDimensional_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(null, () => new int[3].CopyTo(new int[10, 10], 0));
        }

        [Fact]
        public static void CopyTo_IndexInvalid_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("", () => new int[3].CopyTo(new int[10], 10)); // Index > destination.Length
        }

        public static unsafe IEnumerable<object[]> CreateInstance_TestData()
        {
            return new object[][]
            {
                // Primitives
                new object[] { typeof(string), default(string) },
                new object[] { typeof(sbyte), default(sbyte) },
                new object[] { typeof(byte), default(byte) },
                new object[] { typeof(short), default(short) },
                new object[] { typeof(ushort), default(ushort) },
                new object[] { typeof(int), default(int) },
                new object[] { typeof(uint), default(uint) },
                new object[] { typeof(long), default(long) },
                new object[] { typeof(ulong), default(ulong) },
                new object[] { typeof(char), default(char) },
                new object[] { typeof(bool), default(bool) },
                new object[] { typeof(float), default(float) },
                new object[] { typeof(double), default(double) },
                new object[] { typeof(IntPtr), default(IntPtr) },
                new object[] { typeof(UIntPtr), default(UIntPtr) },

                // Array, pointers
                new object[] { typeof(int[]), default(int[]) },
                new object[] { typeof(int*), null },

                // Classes, structs, interfaces, enums
                new object[] { typeof(NonGenericClass1), default(NonGenericClass1) },
                new object[] { typeof(GenericClass<int>), default(GenericClass<int>) },
                new object[] { typeof(NonGenericStruct), default(NonGenericStruct) },
                new object[] { typeof(GenericStruct<int>), default(GenericStruct<int>) },
                new object[] { typeof(NonGenericInterface1), default(NonGenericInterface1) },
                new object[] { typeof(GenericInterface<int>), default(GenericInterface<int>) },
                new object[] { typeof(AbstractClass), default(AbstractClass) },
                new object[] { typeof(StaticClass), default(StaticClass) },
                new object[] { typeof(Int32Enum), default(Int32Enum) }
            };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_TestData))]
        public static void CreateInstance(Type elementType, object repeatedValue)
        {
            CreateInstance(elementType, new int[] { 10 }, new int[1], repeatedValue);
            CreateInstance(elementType, new int[] { 0 }, new int[1], repeatedValue);
            CreateInstance(elementType, new int[] { 1, 2 }, new int[] { 1, 2 }, repeatedValue);
            CreateInstance(elementType, new int[] { 5, 6 }, new int[] { int.MinValue, 0 }, repeatedValue);
        }
        
        [Theory]
        [InlineData(typeof(int), new int[] { 1, 2 }, new int[] { 0, 0 }, default(int))]
        [InlineData(typeof(int), new int[] { 1, 2, 3 }, new int[] { 0, 0, 0 }, default(int))]
        [InlineData(typeof(int), new int[] { 1, 2, 3, 4 }, new int[] { 0, 0, 0, 0 }, default(int))]
        [InlineData(typeof(int), new int[] { 7, 8, 9 }, new int[] { 1, 2, 3 }, default(int))]
        public static void CreateInstance(Type elementType, int[] lengths, int[] lowerBounds, object repeatedValue)
        {
            if (lowerBounds.All(lowerBound => lowerBound == 0))
            {
                if (lengths.Length == 1)
                {
                    // Use CreateInstance(Type, int)
                    Array array1 = Array.CreateInstance(elementType, lengths[0]);
                    VerifyArray(array1, elementType, lengths, lowerBounds, repeatedValue);
                }
                // Use CreateInstance(Type, int[])
                Array array2 = Array.CreateInstance(elementType, lengths);
                VerifyArray(array2, elementType, lengths, lowerBounds, repeatedValue);
            }
            // Use CreateInstance(Type, int[], int[])
            Array array3 = Array.CreateInstance(elementType, lengths, lowerBounds);
            VerifyArray(array3, elementType, lengths, lowerBounds, repeatedValue);
        }

        [Fact]
        public static void CreateInstance_NullElementType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, 0));
            Assert.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, new int[1]));
            Assert.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, new int[1], new int[1]));
        }

        public static IEnumerable<object[]> CreateInstance_NotSupportedType_TestData()
        {
            yield return new object[] { typeof(void) };
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(GenericClass<>) };
            yield return new object[] { typeof(GenericClass<>).MakeGenericType(typeof(GenericClass<>)) };
            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GetGenericArguments()[0] };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_NotSupportedType_TestData))]
        public static void CreateInstance_NotSupportedType_ThrowsNotSupportedException(Type elementType)
        {
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(elementType, 0));
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(elementType, new int[1]));
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(elementType, new int[1], new int[1]));
        }

        [Fact]
        public static void CreateInstance_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.CreateInstance(typeof(int), -1));
            Assert.Throws<ArgumentOutOfRangeException>("lengths[0]", () => Array.CreateInstance(typeof(int), new int[] { -1 }));
            Assert.Throws<ArgumentOutOfRangeException>("lengths[0]", () => Array.CreateInstance(typeof(int), new int[] { -1 }, new int[1]));
        }

        [Fact]
        public static void CreateInstance_TypeNotRuntimeType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("elementType", () => Array.CreateInstance(Helpers.NonRuntimeType(), 0));
            Assert.Throws<ArgumentException>("elementType", () => Array.CreateInstance(Helpers.NonRuntimeType(), new int[1]));
            Assert.Throws<ArgumentException>("elementType", () => Array.CreateInstance(Helpers.NonRuntimeType(), new int[1], new int[1]));
        }

        [Fact]
        public static void CreateInstance_LengthsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("lengths", () => Array.CreateInstance(typeof(int), null, new int[1]));
        }

        [Fact]
        public static void CreateInstance_LengthsEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(null, () => Array.CreateInstance(typeof(int), new int[0]));
            Assert.Throws<ArgumentException>(null, () => Array.CreateInstance(typeof(int), new int[0], new int[1]));
        }

        [Fact]
        public static void CreateInstance_LowerBoundNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("lowerBounds", () => Array.CreateInstance(typeof(int), new int[] { 1 }, null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public static void CreateInstance_LengthsAndLowerBoundsHaveDifferentLengths_ThrowsArgumentException(int length)
        {
            Assert.Throws<ArgumentException>(null, () => Array.CreateInstance(typeof(int), new int[1], new int[length]));
        }
        
        [Fact]
        public static void CreateInstance_Type_LengthsPlusLowerBoundOverflows_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(null, () => Array.CreateInstance(typeof(int), new int[] { int.MaxValue }, new int[] { 2 }));
        }

        [Fact]
        public static void Empty()
        {
            Assert.True(Array.Empty<int>() != null);
            Assert.Equal(0, Array.Empty<int>().Length);
            Assert.Equal(1, Array.Empty<int>().Rank);
            Assert.Same(Array.Empty<int>(), Array.Empty<int>());

            Assert.True(Array.Empty<object>() != null);
            Assert.Equal(0, Array.Empty<object>().Length);
            Assert.Equal(1, Array.Empty<object>().Rank);
            Assert.Same(Array.Empty<object>(), Array.Empty<object>());
        }

        [Fact]
        public static void Find()
        {
            var intArray = new int[] { 7, 8, 9 };

            // Exists included here since it's a trivial wrapper around FindIndex
            Assert.True(Array.Exists(intArray, i => i == 8));
            Assert.False(Array.Exists(intArray, i => i == -1));

            int[] results = Array.FindAll(intArray, i => (i % 2) != 0);
            Assert.Equal(results.Length, 2);
            Assert.True(Array.Exists(results, i => i == 7));
            Assert.True(Array.Exists(results, i => i == 9));

            var stringArray = new string[] { "7", "8", "88", "888", "9" };
            Assert.Equal("8", Array.Find(stringArray, s => s.StartsWith("8")));
            Assert.Null(Array.Find(stringArray, s => s == "X"));

            intArray = new int[] { 40, 41, 42, 43, 44, 45, 46, 47, 48, 49 };
            Assert.Equal(3, Array.FindIndex(intArray, i => i >= 43));
            Assert.Equal(-1, Array.FindIndex(intArray, i => i == 99));

            Assert.Equal(3, Array.FindIndex(intArray, 3, i => i == 43));
            Assert.Equal(-1, Array.FindIndex(intArray, 4, i => i == 43));

            Assert.Equal(3, Array.FindIndex(intArray, 1, 3, i => i == 43));
            Assert.Equal(-1, Array.FindIndex(intArray, 1, 2, i => i == 43));

            stringArray = new string[] { "7", "8", "88", "888", "9" };
            Assert.Equal("888", Array.FindLast(stringArray, s => s.StartsWith("8")));
            Assert.Null(Array.FindLast(stringArray, s => s == "X"));

            intArray = new int[] { 40, 41, 42, 43, 44, 45, 46, 47, 48, 49 };
            Assert.Equal(9, Array.FindLastIndex(intArray, i => i >= 43));
            Assert.Equal(-1, Array.FindLastIndex(intArray, i => i == 99));

            Assert.Equal(3, Array.FindLastIndex(intArray, 3, i => i == 43));
            Assert.Equal(-1, Array.FindLastIndex(intArray, 2, i => i == 43));

            Assert.Equal(3, Array.FindLastIndex(intArray, 5, 3, i => i == 43));
            Assert.Equal(-1, Array.FindLastIndex(intArray, 5, 2, i => i == 43));

            intArray = new int[0];
            Assert.Equal(-1, Array.FindLastIndex(intArray, -1, i => i == 43));
        }

        [Fact]
        public static void Exists_Invalid()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.Exists((int[])null, i => i == 43)); // Array is null
            Assert.Throws<ArgumentNullException>("match", () => Array.Exists(new int[0], null)); // Match is null
        }

        [Fact]
        public static void Find_Invalid()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.Find((int[])null, i => i == 43)); // Array is null
            Assert.Throws<ArgumentNullException>("match", () => Array.Find(new int[0], null)); // Match is null
        }

        [Fact]
        public static void FindIndex_Invalid()
        {
            // Array is null
            Assert.Throws<ArgumentNullException>("array", () => Array.FindIndex((int[])null, i => i == 43));
            Assert.Throws<ArgumentNullException>("array", () => Array.FindIndex((int[])null, 0, i => i == 43));
            Assert.Throws<ArgumentNullException>("array", () => Array.FindIndex((int[])null, 0, 0, i => i == 43));

            // Start index < 0
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindIndex(new int[3], -1, i => i == 43));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindIndex(new int[3], -1, 0, i => i == 43));

            // Start index > array.Length 
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindIndex(new int[3], 4, i => i == 43));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindIndex(new int[3], 4, 0, i => i == 43));

            // Count < 0 or count > array.Length
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.FindIndex(new int[3], 0, -1, i => i == 43));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.FindIndex(new int[3], 0, 4, i => i == 43));

            // Start index + count > array.Length
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.FindIndex(new int[3], 3, 1, i => i == 43));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.FindIndex(new int[3], 2, 2, i => i == 43));
        }

        [Fact]
        public static void FindAll_Invalid()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.FindAll((int[])null, i => i == 43)); // Array is null
            Assert.Throws<ArgumentNullException>("match", () => Array.FindAll(new int[0], null)); // Match is null
        }

        [Fact]
        public static void FindLast_Invalid()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.FindLast((int[])null, i => i == 43)); // Array is null
            Assert.Throws<ArgumentNullException>("match", () => Array.FindLast(new int[0], null)); // Match is null
        }

        [Fact]
        public static void FindLastIndex_Invalid()
        {
            // Array is null
            Assert.Throws<ArgumentNullException>("array", () => Array.FindLastIndex((int[])null, i => i == 43));
            Assert.Throws<ArgumentNullException>("array", () => Array.FindLastIndex((int[])null, 0, i => i == 43));
            Assert.Throws<ArgumentNullException>("array", () => Array.FindLastIndex((int[])null, 0, 0, i => i == 43));

            // Match is null
            Assert.Throws<ArgumentNullException>("match", () => Array.FindLastIndex(new int[0], null));
            Assert.Throws<ArgumentNullException>("match", () => Array.FindLastIndex(new int[0], 0, null));
            Assert.Throws<ArgumentNullException>("match", () => Array.FindLastIndex(new int[0], 0, 0, null));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindLastIndex(new int[0], 0, i => i == 43)); // Start index != -1 for an empty array

            // Start index < 0
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindLastIndex(new int[3], -1, i => i == 43));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindLastIndex(new int[3], -1, 0, i => i == 43));

            // Start index > array.Length 
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindLastIndex(new int[3], 4, i => i == 43));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindLastIndex(new int[3], 4, 0, i => i == 43));

            // Count < 0 or count > array.Length
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.FindLastIndex(new int[3], 0, -1, i => i == 43));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.FindLastIndex(new int[3], 0, 4, i => i == 43));

            // Start index + count > array.Length
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.FindLastIndex(new int[3], 3, 1, i => i == 43));
        }

        public static IEnumerable<object[]> GetEnumerator_TestData()
        {
            yield return new object[] { new int[0] };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 } };
            yield return new object[] { new int[,] { { 1, 2 }, { 2, 4 } } };

            yield return new object[] { new char[] { '7', '8', '9' } };

            yield return new object[] { Array.CreateInstance(typeof(int), new int[] { 3 }, new int[] { 4 }) };
            yield return new object[] { Array.CreateInstance(typeof(int), new int[] { 3, 3 }, new int[] { 4, 5 }) };
        }

        [Theory]
        [MemberData(nameof(GetEnumerator_TestData))]
        public static void GetEnumerator(Array array)
        {
            Assert.NotSame(array.GetEnumerator(), array.GetEnumerator());
            Array expected = array.Cast<object>().ToArray(); // Flatten multidimensional arrays

            IEnumerator enumerator = array.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(expected.GetValue(counter), enumerator.Current);
                    counter++;
                }
                Assert.False(enumerator.MoveNext());
                Assert.Equal(array.Length, counter);

                enumerator.Reset();
            }
        }

        [Theory]
        [MemberData(nameof(GetEnumerator_TestData))]
        public static void GetEnumerator_Invalid(Array array)
        {
            IEnumerator enumerator = array.GetEnumerator();

            // Enumerator should throw when accessing Current before starting enumeration
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            while (enumerator.MoveNext()) ;

            // Enumerator should throw when accessing Current after finishing enumeration
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Enumerator should throw when accessing Current after being reset
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static unsafe void GetEnumerator_ArrayOfPointers_ThrowsNotSupportedException()
        {
            Array nonEmptyArray = new int*[2];
            Assert.Throws<NotSupportedException>(() => { foreach (object obj in nonEmptyArray) { } });

            Array emptyArray = new int*[0];
            foreach (object obj in emptyArray) { }
        }

        public static IEnumerable<object[]> IndexOf_SZArray_TestData()
        {
            // SByte
            yield return new object[] { new sbyte[] { 1, 2, 3, 3 }, (sbyte)1, 0, 4, 0 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3 }, (sbyte)3, 0, 4, 2 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3 }, (sbyte)2, 1, 2, 1 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3 }, (sbyte)1, 1, 2, -1 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3 }, (sbyte)1, 4, 0, -1 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3 }, (sbyte)1, 0, 0, -1 };
            yield return new object[] { new sbyte[0], (sbyte)1, 0, 0, -1 };

            // Byte
            yield return new object[] { new byte[] { 1, 2, 3, 3 }, (byte)1, 0, 4, 0 };
            yield return new object[] { new byte[] { 1, 2, 3, 3 }, (byte)3, 0, 4, 2 };
            yield return new object[] { new byte[] { 1, 2, 3, 3 }, (byte)2, 1, 2, 1 };
            yield return new object[] { new byte[] { 1, 2, 3, 3 }, (byte)1, 1, 2, -1 };
            yield return new object[] { new byte[] { 1, 2, 3, 3 }, (byte)1, 4, 0, -1 };
            yield return new object[] { new byte[] { 1, 2, 3, 3 }, (byte)1, 0, 0, -1 };
            yield return new object[] { new byte[0], (byte)1, 0, 0, -1 };

            // Int16
            yield return new object[] { new short[] { 1, 2, 3, 3 }, (short)1, 0, 4, 0 };
            yield return new object[] { new short[] { 1, 2, 3, 3 }, (short)3, 0, 4, 2 };
            yield return new object[] { new short[] { 1, 2, 3, 3 }, (short)2, 1, 2, 1 };
            yield return new object[] { new short[] { 1, 2, 3, 3 }, (short)1, 1, 2, -1 };
            yield return new object[] { new short[] { 1, 2, 3, 3 }, (short)1, 4, 0, -1 };
            yield return new object[] { new short[] { 1, 2, 3, 3 }, (short)1, 0, 0, -1 };
            yield return new object[] { new short[0], (short)1, 0, 0, -1 };

            // UInt16
            yield return new object[] { new ushort[] { 1, 2, 3, 3 }, (ushort)1, 0, 4, 0 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3 }, (ushort)3, 0, 4, 2 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3 }, (ushort)2, 1, 2, 1 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3 }, (ushort)1, 1, 2, -1 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3 }, (ushort)1, 4, 0, -1 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3 }, (ushort)1, 0, 0, -1 };
            yield return new object[] { new ushort[0], (ushort)1, 0, 0, -1 };

            // Int32
            var intArray = new int[] { 7, 7, 8, 8, 9, 9 };
            yield return new object[] { intArray, 8, 0, 6, 2 };
            yield return new object[] { intArray, 8, 3, 3, 3 };
            yield return new object[] { intArray, 8, 4, 2, -1 };
            yield return new object[] { intArray, 9, 2, 2, -1 };
            yield return new object[] { intArray, 9, 2, 3, 4 };
            yield return new object[] { intArray, 10, 0, 6, -1 };

            // UInt32
            yield return new object[] { new uint[] { 1, 2, 3, 3 }, (uint)1, 0, 4, 0 };
            yield return new object[] { new uint[] { 1, 2, 3, 3 }, (uint)3, 0, 4, 2 };
            yield return new object[] { new uint[] { 1, 2, 3, 3 }, (uint)2, 1, 2, 1 };
            yield return new object[] { new uint[] { 1, 2, 3, 3 }, (uint)1, 1, 2, -1 };
            yield return new object[] { new uint[] { 1, 2, 3, 3 }, (uint)1, 4, 0, -1 };
            yield return new object[] { new uint[] { 1, 2, 3, 3 }, (uint)1, 0, 0, -1 };
            yield return new object[] { new uint[0], (uint)1, 0, 0, -1 };

            // Int64
            yield return new object[] { new long[] { 1, 2, 3, 3 }, (long)1, 0, 4, 0 };
            yield return new object[] { new long[] { 1, 2, 3, 3 }, (long)3, 0, 4, 2 };
            yield return new object[] { new long[] { 1, 2, 3, 3 }, (long)2, 1, 2, 1 };
            yield return new object[] { new long[] { 1, 2, 3, 3 }, (long)1, 1, 2, -1 };
            yield return new object[] { new long[] { 1, 2, 3, 3 }, (long)1, 4, 0, -1 };
            yield return new object[] { new long[] { 1, 2, 3, 3 }, (long)1, 0, 0, -1 };
            yield return new object[] { new long[0], (long)1, 0, 0, -1 };

            // UInt64
            yield return new object[] { new ulong[] { 1, 2, 3, 3 }, (ulong)1, 0, 4, 0 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3 }, (ulong)3, 0, 4, 2 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3 }, (ulong)2, 1, 2, 1 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3 }, (ulong)1, 1, 2, -1 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3 }, (ulong)1, 4, 0, -1 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3 }, (ulong)1, 0, 0, -1 };
            yield return new object[] { new ulong[0], (ulong)1, 0, 0, -1 };

            // Char
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3 }, (char)1, 0, 4, 0 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3 }, (char)3, 0, 4, 2 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3 }, (char)2, 1, 2, 1 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3 }, (char)1, 1, 2, -1 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3 }, (char)1, 4, 0, -1 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3 }, (char)1, 0, 0, -1 };
            yield return new object[] { new char[0], (char)1, 0, 0, -1 };

            // Bool
            yield return new object[] { new bool[] { true, false, false, true }, true, 0, 4, 0 };
            yield return new object[] { new bool[] { true, false, false, true }, false, 1, 2, 1 };
            yield return new object[] { new bool[] { true, false, false, true }, true, 1, 1, -1 };
            yield return new object[] { new bool[] { true, false, false, true }, true, 4, 0, -1 };
            yield return new object[] { new bool[] { true, false, false, true }, true, 0, 0, -1 };
            yield return new object[] { new bool[0], true, 0, 0, -1 };

            // Single
            yield return new object[] { new float[] { 1, 2, 3, 3 }, (float)1, 0, 4, 0 };
            yield return new object[] { new float[] { 1, 2, 3, 3 }, (float)3, 0, 4, 2 };
            yield return new object[] { new float[] { 1, 2, 3, 3 }, (float)2, 1, 2, 1 };
            yield return new object[] { new float[] { 1, 2, 3, 3 }, (float)1, 1, 2, -1 };
            yield return new object[] { new float[] { 1, 2, 3, 3 }, (float)1, 4, 0, -1 };
            yield return new object[] { new float[] { 1, 2, 3, 3 }, (float)1, 0, 0, -1 };
            yield return new object[] { new float[0], (float)1, 0, 0, -1 };

            // Double
            yield return new object[] { new double[] { 1, 2, 3, 3 }, (double)1, 0, 4, 0 };
            yield return new object[] { new double[] { 1, 2, 3, 3 }, (double)3, 0, 4, 2 };
            yield return new object[] { new double[] { 1, 2, 3, 3 }, (double)2, 1, 2, 1 };
            yield return new object[] { new double[] { 1, 2, 3, 3 }, (double)1, 1, 2, -1 };
            yield return new object[] { new double[] { 1, 2, 3, 3 }, (double)1, 4, 0, -1 };
            yield return new object[] { new double[] { 1, 2, 3, 3 }, (double)1, 0, 0, -1 };
            yield return new object[] { new double[0], (double)1, 0, 0, -1 };

            // IntPtr
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3 }, (IntPtr)1, 0, 4, 0 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3 }, (IntPtr)3, 0, 4, 2 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3 }, (IntPtr)2, 1, 2, 1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3 }, (IntPtr)1, 1, 2, -1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3 }, (IntPtr)1, 4, 0, -1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3 }, (IntPtr)1, 0, 0, -1 };
            yield return new object[] { new IntPtr[0], (IntPtr)1, 0, 0, -1 };

            // UIntPtr
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3 }, (UIntPtr)1, 0, 4, 0 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3 }, (UIntPtr)3, 0, 4, 2 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3 }, (UIntPtr)2, 1, 2, 1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3 }, (UIntPtr)1, 1, 2, -1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3 }, (UIntPtr)1, 4, 0, -1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3 }, (UIntPtr)1, 0, 0, -1 };
            yield return new object[] { new UIntPtr[0], (UIntPtr)1, 0, 0, -1 };

            // String
            var stringArray = new string[] { null, null, "Hello", "Hello", "Goodbye", "Goodbye", null, null };
            yield return new object[] { stringArray, "Hello", 0, 8, 2 };
            yield return new object[] { stringArray, "Goodbye", 0, 8, 4 };
            yield return new object[] { stringArray, "Nowhere", 0, 8, -1 };
            yield return new object[] { stringArray, "Hello", 3, 5, 3 };
            yield return new object[] { stringArray, "Hello", 4, 4, -1 };
            yield return new object[] { stringArray, "Goodbye", 2, 3, 4 };
            yield return new object[] { stringArray, "Goodbye", 2, 2, -1 };

            // SByteEnum
            yield return new object[] { new SByteEnum[] { SByteEnum.MinusTwo, SByteEnum.Zero }, SByteEnum.Zero, 0, 2, 1 };
            yield return new object[] { new SByteEnum[] { SByteEnum.MinusTwo, SByteEnum.Zero }, SByteEnum.Zero, 1, 1, 1 };
            yield return new object[] { new SByteEnum[] { SByteEnum.MinusTwo, SByteEnum.Zero }, SByteEnum.Zero, 2, 0, -1 };
            yield return new object[] { new SByteEnum[] { SByteEnum.Five, SByteEnum.Five }, SByteEnum.Five, 0, 2, 0 };
            yield return new object[] { new SByteEnum[] { SByteEnum.Five, SByteEnum.Five }, SByteEnum.Five, 1, 1, 1 };
            yield return new object[] { new SByteEnum[] { SByteEnum.Five, SByteEnum.Five }, SByteEnum.Five, 2, 0, -1 };

            // Int16Enum
            yield return new object[] { new Int16Enum[] { Int16Enum.Min }, Int16Enum.Min, 0, 1, 0 };
            yield return new object[] { new Int16Enum[] { Int16Enum.Min + 1 }, Int16Enum.One, 0, 1, -1 };
            yield return new object[] { new Int16Enum[] { Int16Enum.Max, Int16Enum.Max }, Int16Enum.Max, 0, 2, 0 };
            yield return new object[] { new Int16Enum[] { Int16Enum.Max, Int16Enum.Max }, Int16Enum.Max, 1, 1, 1 };
            yield return new object[] { new Int16Enum[] { Int16Enum.Max, Int16Enum.Max }, Int16Enum.Max, 2, 0, -1 };

            // Int32Enum
            yield return new object[] { new Int32Enum[] { Int32Enum.Case1, Int32Enum.Case2, Int32Enum.Case1 }, Int32Enum.Case1, 0, 3, 0 };
            yield return new object[] { new Int32Enum[] { Int32Enum.Case1, Int32Enum.Case2, Int32Enum.Case1 }, Int32Enum.Case3, 0, 3, -1 };

            // Int64Enum
            yield return new object[] { new Int64Enum[] { (Int64Enum)1, (Int64Enum)2, (Int64Enum)1 }, (Int64Enum)1, 0, 3, 0 };
            yield return new object[] { new Int64Enum[] { (Int64Enum)1, (Int64Enum)2, (Int64Enum)1 }, (Int64Enum)3, 0, 3, -1 };

            // Class
            NonGenericClass1 classObject = new NonGenericClass1();
            yield return new object[] { new NonGenericClass1[] { classObject, new NonGenericClass1() }, classObject, 0, 2, 0 };
            yield return new object[] { new NonGenericClass1[] { classObject, new NonGenericClass1() }, new NonGenericClass1(), 0, 2, -1 };
            yield return new object[] { new NonGenericClass1[] { classObject, new NonGenericClass1() }, classObject, 2, 0, -1 };
            yield return new object[] { new NonGenericClass1[] { classObject, new NonGenericClass1() }, classObject, 0, 0, -1 };

            // Struct
            NonGenericStruct structObject = new NonGenericStruct();
            yield return new object[] { new NonGenericStruct[] { structObject, new NonGenericStruct() }, structObject, 0, 2, 0 };
            yield return new object[] { new NonGenericStruct[] { structObject, new NonGenericStruct() }, new NonGenericStruct(), 0, 2, 0 };
            yield return new object[] { new NonGenericStruct[] { structObject, new NonGenericStruct() }, structObject, 2, 0, -1 };
            yield return new object[] { new NonGenericStruct[] { structObject, new NonGenericStruct() }, structObject, 0, 0, -1 };

            // Interface
            ClassWithNonGenericInterface1 interfaceObject = new ClassWithNonGenericInterface1();
            yield return new object[] { new NonGenericInterface1[] { interfaceObject, new ClassWithNonGenericInterface1() }, interfaceObject, 0, 2, 0 };
            yield return new object[] { new NonGenericInterface1[] { interfaceObject, new ClassWithNonGenericInterface1() }, new ClassWithNonGenericInterface1(), 0, 2, -1 };
            yield return new object[] { new NonGenericInterface1[] { interfaceObject, new ClassWithNonGenericInterface1() }, interfaceObject, 2, 0, -1 };
            yield return new object[] { new NonGenericInterface1[] { interfaceObject, new ClassWithNonGenericInterface1() }, interfaceObject, 0, 0, -1 };

            // Object
            yield return new object[] { new object[] { new EqualsOverrider { Value = 1 } }, null, 0, 1, -1 };
            yield return new object[] { new object[] { new EqualsOverrider { Value = 1 } }, new EqualsOverrider { Value = 1 }, 0, 1, 0 };
            yield return new object[] { new object[] { new EqualsOverrider { Value = 1 } }, new EqualsOverrider { Value = 2 }, 0, 1, -1 };
            yield return new object[] { new object[1], null, 0, 1, 0 };
            yield return new object[] { new object[2], null, 2, 0, -1 };
            yield return new object[] { new object[2], null, 0, 0, -1 };
        }

        public static IEnumerable<object[]> IndexOf_Array_TestData()
        {
            // Workaround: Move these tests to IndexOf_SZArray_TestData if/ when https://github.com/xunit/xunit/pull/965 is available
            // SByte
            yield return new object[] { new sbyte[] { 1, 2 }, (byte)1, 0, 2, -1 };
            yield return new object[] { new sbyte[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new sbyte[] { 1, 2 }, null, 0, 2, -1 };

            // Byte
            yield return new object[] { new byte[] { 1, 2 }, (sbyte)1, 0, 2, -1 };
            yield return new object[] { new byte[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new byte[] { 1, 2 }, null, 0, 2, -1 };

            // Int16
            yield return new object[] { new short[] { 1, 2 }, (ushort)1, 0, 2, -1 };
            yield return new object[] { new short[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new short[] { 1, 2 }, null, 0, 2, -1 };

            // UInt16
            yield return new object[] { new ushort[] { 1, 2 }, (short)1, 0, 2, -1 };
            yield return new object[] { new ushort[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new ushort[] { 1, 2 }, null, 0, 2, -1 };

            // Int32
            yield return new object[] { new int[] { 1, 2 }, (uint)1, 0, 2, -1 };
            yield return new object[] { new int[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new int[] { 1, 2 }, null, 0, 2, -1 };

            // UInt32
            yield return new object[] { new uint[] { 1, 2 }, 1, 0, 2, -1 };
            yield return new object[] { new uint[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new uint[] { 1, 2 }, null, 0, 2, -1 };

            // Int64
            yield return new object[] { new long[] { 1, 2 }, (ulong)1, 0, 2, -1 };
            yield return new object[] { new long[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new long[] { 1, 2 }, null, 0, 2, -1 };

            // UInt64
            yield return new object[] { new ulong[] { 1, 2 }, (long)1, 0, 2, -1 };
            yield return new object[] { new ulong[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new ulong[] { 1, 2 }, null, 0, 2, -1 };

            // Char
            yield return new object[] { new char[] { (char)1, (char)2 }, (ushort)1, 0, 2, -1 };
            yield return new object[] { new char[] { (char)1, (char)2 }, new object(), 0, 2, -1 };
            yield return new object[] { new char[] { (char)1, (char)2 }, null, 0, 2, -1 };

            // Bool
            yield return new object[] { new bool[] { true, false }, (char)0, 0, 2, -1 };
            yield return new object[] { new bool[] { true, false }, new object(), 0, 2, -1 };
            yield return new object[] { new bool[] { true, false }, null, 0, 2, -1 };

            // Single
            yield return new object[] { new float[] { 1, 2 }, (double)1, 0, 2, -1 };
            yield return new object[] { new float[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new float[] { 1, 2 }, null, 0, 2, -1 };

            // Double
            yield return new object[] { new double[] { 1, 2 }, (float)1, 0, 2, -1 };
            yield return new object[] { new double[] { 1, 2 }, new object(), 0, 2, -1 };
            yield return new object[] { new double[] { 1, 2 }, null, 0, 2, -1 };

            // IntPtr
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2 }, (UIntPtr)1, 0, 2, -1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2 }, new object(), 0, 2, -1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2 }, null, 0, 2, -1 };

            // UIntPtr
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2 }, (IntPtr)1, 0, 2, -1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2 }, new object(), 0, 2, -1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2 }, null, 0, 2, -1 };

            // String
            yield return new object[] { new string[] { "Hello", "Hello", "Goodbyte", "Goodbye" }, new object(), 0, 4, -1 };

            // Nullable
            var nullableArray = new int?[] { 0, null, 10 };
            yield return new object[] { nullableArray, null, 0, 3, 1 };
            yield return new object[] { nullableArray, 10, 0, 3, 2 };
            yield return new object[] { nullableArray, 100, 0, 3, -1 };
        }

        [Theory]
        [MemberData(nameof(IndexOf_SZArray_TestData))]
        public static void IndexOf_SZArray<T>(T[] array, T value, int startIndex, int count, int expected)
        {
            if (startIndex + count == array.Length)
            {
                if (startIndex == 0)
                {
                    // Use IndexOf<T>(T[], T)
                    Assert.Equal(expected, Array.IndexOf(array, value));
                    IList<T> iList = array;
                    Assert.Equal(expected, iList.IndexOf(value));
                    Assert.Equal(expected >= startIndex, iList.Contains(value));
                }
                // Use IndexOf<T>(T[], T, int)
                Assert.Equal(expected, Array.IndexOf(array, value, startIndex));
            }
            // Use IndexOf<T>(T[], T, int, int)
            Assert.Equal(expected, Array.IndexOf(array, value, startIndex, count));

            // Basic: forward SZArray
            IndexOf_Array(array, value, startIndex, count, expected);

            // Advanced: convert SZArray to an array with non-zero lower bound
            const int LowerBound = 5;
            Array nonZeroLowerBoundArray = NonZeroLowerBoundArray(array, LowerBound);
            IndexOf_Array(nonZeroLowerBoundArray, value, startIndex + LowerBound, count, expected + LowerBound);
        }

        [Theory]
        [MemberData(nameof(IndexOf_Array_TestData))]
        public static void IndexOf_Array(Array array, object value, int startIndex, int count, int expected) 
        {
            if (startIndex + count == array.GetLowerBound(0) + array.Length)
            {
                if (startIndex == array.GetLowerBound(0))
                {
                    // Use IndexOf(Array, object)
                    Assert.Equal(expected, Array.IndexOf(array, value));
                    IList iList = array;
                    Assert.Equal(expected, iList.IndexOf(value));
                    Assert.Equal(expected >= startIndex, iList.Contains(value));
                }
                // Use IndexOf(Array, object, int)
                Assert.Equal(expected, Array.IndexOf(array, value, startIndex));
            }
            // Use IndexOf(Array, object, int, int)
            Assert.Equal(expected, Array.IndexOf(array, value, startIndex, count));
        }

        [Fact]
        public static void IndexOf_SZArray_NonInferrableEntries()
        {
            // Workaround: Move these values to IndexOf_SZArray_TestData if/ when https://github.com/xunit/xunit/pull/965 is available
            IndexOf_SZArray(new string[] { "Hello", "Hello", "Goodbyte", "Goodbye" }, null, 0, 4, -1);
        }

        [Fact]
        public static void IndexOf_NullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf((int[])null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf((int[])null, "", 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf((int[])null, "", 0, 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf(null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf(null, "", 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf(null, "", 0, 0));
        }

        [Fact]
        public static void IndexOf_MultimensionalArray_ThrowsRankException()
        {
            Assert.Throws<RankException>(() => Array.IndexOf(new string[0, 0], ""));
            Assert.Throws<RankException>(() => Array.IndexOf(new string[0, 0], "", 0));
            Assert.Throws<RankException>(() => Array.IndexOf(new string[0, 0], "", 0, 0));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public static void IndexOf_InvalidStartIndex_ThrowsArgumentOutOfRangeException(int startIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.IndexOf(new int[0], "", startIndex));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.IndexOf(new int[0], "", startIndex, 0));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.IndexOf(new string[0], "", startIndex));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.IndexOf(new string[0], "", startIndex, 0));
        }

        [Theory]
        [InlineData(0, 0, -1)]
        [InlineData(0, 0, 1)]
        [InlineData(2, 0, 3)]
        [InlineData(2, 2, 1)]
        public static void IndexOf_InvalidCount_ThrowsArgumentOutOfRangeException(int length, int startIndex, int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.IndexOf(new int[length], "", startIndex, count));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.IndexOf(new string[length], "", startIndex, count));
        }

        [Fact]
        public static unsafe void IndexOf_ArrayOfPointers_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Array.IndexOf((Array)new int*[2], null));
            Assert.Equal(-1, Array.IndexOf((Array)new int*[0], null));
        }

        public static IEnumerable<object[]> LastIndexOf_SZArray_TestData()
        {
            // SByte
            yield return new object[] { new sbyte[] { 1, 2, 3, 3, 4 }, (sbyte)1, 4, 5, 0 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3, 4 }, (sbyte)3, 4, 5, 3 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3, 4 }, (sbyte)2, 2, 3, 1 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3, 4 }, (sbyte)4, 2, 3, -1 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3, 4 }, (sbyte)5, 4, 5, -1 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3, 4 }, (sbyte)3, 0, 0, -1 };
            yield return new object[] { new sbyte[] { 1, 2, 3, 3, 4 }, (sbyte)3, 3, 0, -1 };

            // Byte
            yield return new object[] { new byte[] { 1, 2, 3, 3, 4 }, (byte)1, 4, 5, 0 };
            yield return new object[] { new byte[] { 1, 2, 3, 3, 4 }, (byte)3, 4, 5, 3 };
            yield return new object[] { new byte[] { 1, 2, 3, 3, 4 }, (byte)2, 2, 3, 1 };
            yield return new object[] { new byte[] { 1, 2, 3, 3, 4 }, (byte)4, 2, 3, -1 };
            yield return new object[] { new byte[] { 1, 2, 3, 3, 4 }, (byte)5, 4, 5, -1 };
            yield return new object[] { new byte[] { 1, 2, 3, 3, 4 }, (byte)3, 0, 0, -1 };
            yield return new object[] { new byte[] { 1, 2, 3, 3, 4 }, (byte)3, 3, 0, -1 };

            // Int16
            yield return new object[] { new short[] { 1, 2, 3, 3, 4 }, (short)1, 4, 5, 0 };
            yield return new object[] { new short[] { 1, 2, 3, 3, 4 }, (short)3, 4, 5, 3 };
            yield return new object[] { new short[] { 1, 2, 3, 3, 4 }, (short)2, 2, 3, 1 };
            yield return new object[] { new short[] { 1, 2, 3, 3, 4 }, (short)4, 2, 3, -1 };
            yield return new object[] { new short[] { 1, 2, 3, 3, 4 }, (short)5, 4, 5, -1 };
            yield return new object[] { new short[] { 1, 2, 3, 3, 4 }, (short)3, 0, 0, -1 };
            yield return new object[] { new short[] { 1, 2, 3, 3, 4 }, (short)3, 3, 0, -1 };

            // UInt16
            yield return new object[] { new ushort[] { 1, 2, 3, 3, 4 }, (ushort)1, 4, 5, 0 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3, 4 }, (ushort)3, 4, 5, 3 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3, 4 }, (ushort)2, 2, 3, 1 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3, 4 }, (ushort)4, 2, 3, -1 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3, 4 }, (ushort)5, 4, 5, -1 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3, 4 }, (ushort)3, 0, 0, -1 };
            yield return new object[] { new ushort[] { 1, 2, 3, 3, 4 }, (ushort)3, 3, 0, -1 };

            // Int32
            var intArray = new int[] { 7, 7, 8, 8, 9, 9 };
            yield return new object[] { intArray, 8, 5, 6, 3 };
            yield return new object[] { intArray, 8, 1, 1, -1 };
            yield return new object[] { intArray, 8, 3, 3, 3 };
            yield return new object[] { intArray, 7, 3, 2, -1 };
            yield return new object[] { intArray, 7, 3, 3, 1 };
            yield return new object[] { new int[0], 0, 0, 0, -1 };
            yield return new object[] { new int[0], 0, -1, 0, -1 };

            // UInt32
            yield return new object[] { new uint[] { 1, 2, 3, 3, 4 }, (uint)1, 4, 5, 0 };
            yield return new object[] { new uint[] { 1, 2, 3, 3, 4 }, (uint)3, 4, 5, 3 };
            yield return new object[] { new uint[] { 1, 2, 3, 3, 4 }, (uint)2, 2, 3, 1 };
            yield return new object[] { new uint[] { 1, 2, 3, 3, 4 }, (uint)4, 2, 3, -1 };
            yield return new object[] { new uint[] { 1, 2, 3, 3, 4 }, (uint)5, 4, 5, -1 };
            yield return new object[] { new uint[] { 1, 2, 3, 3, 4 }, (uint)3, 0, 0, -1 };
            yield return new object[] { new uint[] { 1, 2, 3, 3, 4 }, (uint)3, 3, 0, -1 };

            // UInt64
            yield return new object[] { new long[] { 1, 2, 3, 3, 4 }, (long)1, 4, 5, 0 };
            yield return new object[] { new long[] { 1, 2, 3, 3, 4 }, (long)3, 4, 5, 3 };
            yield return new object[] { new long[] { 1, 2, 3, 3, 4 }, (long)2, 2, 3, 1 };
            yield return new object[] { new long[] { 1, 2, 3, 3, 4 }, (long)4, 2, 3, -1 };
            yield return new object[] { new long[] { 1, 2, 3, 3, 4 }, (long)5, 4, 5, -1 };
            yield return new object[] { new long[] { 1, 2, 3, 3, 4 }, (long)3, 0, 0, -1 };
            yield return new object[] { new long[] { 1, 2, 3, 3, 4 }, (long)3, 3, 0, -1 };

            // UInt64
            yield return new object[] { new ulong[] { 1, 2, 3, 3, 4 }, (ulong)1, 4, 5, 0 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3, 4 }, (ulong)3, 4, 5, 3 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3, 4 }, (ulong)2, 2, 3, 1 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3, 4 }, (ulong)4, 2, 3, -1 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3, 4 }, (ulong)5, 4, 5, -1 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3, 4 }, (ulong)3, 0, 0, -1 };
            yield return new object[] { new ulong[] { 1, 2, 3, 3, 4 }, (ulong)3, 3, 0, -1 };

            // Char
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3, (char)4 }, (char)1, 4, 5, 0 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3, (char)4 }, (char)3, 4, 5, 3 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3, (char)4 }, (char)2, 2, 3, 1 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3, (char)4 }, (char)4, 2, 3, -1 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3, (char)4 }, (char)5, 4, 5, -1 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3, (char)4 }, (char)3, 0, 0, -1 };
            yield return new object[] { new char[] { (char)1, (char)2, (char)3, (char)3, (char)4 }, (char)3, 4, 0, -1 };

            // Bool
            yield return new object[] { new bool[] { false, true, true }, false, 2, 3, 0 };
            yield return new object[] { new bool[] { false, true, true }, true, 2, 3, 2 };
            yield return new object[] { new bool[] { false, true, true }, false, 1, 2, 0 };
            yield return new object[] { new bool[] { false, true, true }, false, 1, 1, -1 };
            yield return new object[] { new bool[] { false }, true, 0, 1, -1 };
            yield return new object[] { new bool[] { false, true, true }, false, 0, 0, -1 };
            yield return new object[] { new bool[] { false, true, true }, false, 2, 0, -1 };

            // Single
            yield return new object[] { new float[] { 1, 2, 3, 3, 4 }, (float)1, 4, 5, 0 };
            yield return new object[] { new float[] { 1, 2, 3, 3, 4 }, (float)3, 4, 5, 3 };
            yield return new object[] { new float[] { 1, 2, 3, 3, 4 }, (float)2, 2, 3, 1 };
            yield return new object[] { new float[] { 1, 2, 3, 3, 4 }, (float)4, 2, 3, -1 };
            yield return new object[] { new float[] { 1, 2, 3, 3, 4 }, (float)5, 4, 5, -1 };
            yield return new object[] { new float[] { 1, 2, 3, 3, 4 }, (float)3, 0, 0, -1 };
            yield return new object[] { new float[] { 1, 2, 3, 3, 4 }, (float)3, 3, 0, -1 };

            // Double
            yield return new object[] { new double[] { 1, 2, 3, 3, 4 }, (double)1, 4, 5, 0 };
            yield return new object[] { new double[] { 1, 2, 3, 3, 4 }, (double)3, 4, 5, 3 };
            yield return new object[] { new double[] { 1, 2, 3, 3, 4 }, (double)2, 2, 3, 1 };
            yield return new object[] { new double[] { 1, 2, 3, 3, 4 }, (double)4, 2, 3, -1 };
            yield return new object[] { new double[] { 1, 2, 3, 3, 4 }, (double)5, 4, 5, -1 };
            yield return new object[] { new double[] { 1, 2, 3, 3, 4 }, (double)3, 0, 0, -1 };
            yield return new object[] { new double[] { 1, 2, 3, 3, 4 }, (double)3, 3, 0, -1 };

            // IntPtr
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3, (IntPtr)4 }, (IntPtr)1, 4, 5, 0 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3, (IntPtr)4 }, (IntPtr)3, 4, 5, 3 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3, (IntPtr)4 }, (IntPtr)2, 2, 3, 1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3, (IntPtr)4 }, (IntPtr)4, 2, 3, -1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3, (IntPtr)4 }, (IntPtr)5, 4, 5, -1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3, (IntPtr)4 }, (IntPtr)3, 0, 0, -1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)3, (IntPtr)4 }, (IntPtr)3, 3, 0, -1 };

            // UIntPtr
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3, (UIntPtr)4 }, (UIntPtr)1, 4, 5, 0 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3, (UIntPtr)4 }, (UIntPtr)3, 4, 5, 3 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3, (UIntPtr)4 }, (UIntPtr)2, 2, 3, 1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3, (UIntPtr)4 }, (UIntPtr)4, 2, 3, -1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3, (UIntPtr)4 }, (UIntPtr)5, 4, 5, -1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3, (UIntPtr)4 }, (UIntPtr)3, 0, 0, -1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)3, (UIntPtr)4 }, (UIntPtr)3, 3, 0, -1 };

            // String
            var stringArray = new string[] { null, null, "Hello", "Hello", "Goodbye", "Goodbye", null, null };
            yield return new object[] { stringArray, "Hello", 7, 8, 3 };
            yield return new object[] { stringArray, "Goodbye", 7, 8, 5 };
            yield return new object[] { stringArray, "Nowhere", 7, 8, -1 };
            yield return new object[] { stringArray, "Hello", 2, 2, 2 };
            yield return new object[] { stringArray, "Hello", 3, 3, 3 };
            yield return new object[] { stringArray, "Goodbye", 7, 2, -1 };
            yield return new object[] { stringArray, "Goodbye", 7, 3, 5 };

            // Int32Enum
            yield return new object[] { new Int32Enum[] { Int32Enum.Case1, Int32Enum.Case2, Int32Enum.Case1 }, Int32Enum.Case1, 2, 3, 2 };
            yield return new object[] { new Int32Enum[] { Int32Enum.Case1, Int32Enum.Case2, Int32Enum.Case1 }, Int32Enum.Case3, 2, 3, -1 };

            // Int64Enum
            yield return new object[] { new Int64Enum[] { (Int64Enum)1, (Int64Enum)2, (Int64Enum)1 }, (Int64Enum)1, 2, 3, 2 };
            yield return new object[] { new Int64Enum[] { (Int64Enum)1, (Int64Enum)2, (Int64Enum)1 }, (Int64Enum)3, 2, 3, -1 };

            // Class
            NonGenericClass1 classObject = new NonGenericClass1();
            yield return new object[] { new NonGenericClass1[] { classObject, new NonGenericClass1() }, classObject, 1, 2, 0 };
            yield return new object[] { new NonGenericClass1[] { classObject, new NonGenericClass1() }, new NonGenericClass1(), 1, 2, -1 };
            yield return new object[] { new NonGenericClass1[] { classObject, new NonGenericClass1() }, classObject, 1, 0, -1 };
            yield return new object[] { new NonGenericClass1[] { classObject, new NonGenericClass1() }, classObject, 0, 0, -1 };

            // Struct
            NonGenericStruct structObject = new NonGenericStruct();
            yield return new object[] { new NonGenericStruct[] { structObject, new NonGenericStruct() }, structObject, 1, 2, 1 };
            yield return new object[] { new NonGenericStruct[] { structObject, new NonGenericStruct() }, new NonGenericStruct(), 1, 2, 1 };
            yield return new object[] { new NonGenericStruct[] { structObject, new NonGenericStruct() }, structObject, 1, 0, -1 };
            yield return new object[] { new NonGenericStruct[] { structObject, new NonGenericStruct() }, structObject, 0, 0, -1 };

            // Interface
            ClassWithNonGenericInterface1 interfaceObject = new ClassWithNonGenericInterface1();
            yield return new object[] { new NonGenericInterface1[] { interfaceObject, new ClassWithNonGenericInterface1() }, interfaceObject, 1, 2, 0 };
            yield return new object[] { new NonGenericInterface1[] { interfaceObject, new ClassWithNonGenericInterface1() }, new ClassWithNonGenericInterface1(), 1, 2, -1 };
            yield return new object[] { new NonGenericInterface1[] { interfaceObject, new ClassWithNonGenericInterface1() }, interfaceObject, 1, 0, -1 };
            yield return new object[] { new NonGenericInterface1[] { interfaceObject, new ClassWithNonGenericInterface1() }, interfaceObject, 0, 0, -1 };

            // Object
            yield return new object[] { new object[] { new EqualsOverrider { Value = 1 } }, null, 0, 1, -1 };
            yield return new object[] { new object[] { new EqualsOverrider { Value = 1 } }, new EqualsOverrider { Value = 1 }, 0, 1, 0 };
            yield return new object[] { new object[] { new EqualsOverrider { Value = 1 } }, new EqualsOverrider { Value = 2 }, 0, 1, -1 };
            yield return new object[] { new object[1], null, 0, 1, 0 };
            yield return new object[] { new object[2], null, 1, 0, -1 };
            yield return new object[] { new object[2], null, 0, 0, -1 };
        }

        public static IEnumerable<object[]> LastIndexOf_Array_TestData()
        {
            // Workaround: Move these values to LastIndexOf_SZArray_TestData if/ when https://github.com/xunit/xunit/pull/965 is available
            // SByte
            yield return new object[] { new sbyte[] { 1, 2 }, (byte)1, 1, 2, -1 };
            yield return new object[] { new sbyte[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new sbyte[] { 1, 2 }, null, 1, 2, -1 };

            // Byte
            yield return new object[] { new byte[] { 1, 2 }, (sbyte)1, 1, 2, -1 };
            yield return new object[] { new byte[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new byte[] { 1, 2 }, null, 1, 2, -1 };

            // Int16
            yield return new object[] { new short[] { 1, 2 }, (ushort)1, 1, 2, -1 };
            yield return new object[] { new short[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new short[] { 1, 2 }, null, 1, 2, -1 };

            // UInt16
            yield return new object[] { new ushort[] { 1, 2 }, (short)1, 1, 2, -1 };
            yield return new object[] { new ushort[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new ushort[] { 1, 2 }, null, 1, 2, -1 };

            // Int32
            yield return new object[] { new int[] { 1, 2 }, (uint)1, 1, 2, -1 };
            yield return new object[] { new int[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new int[] { 1, 2 }, null, 1, 2, -1 };

            // UInt32
            yield return new object[] { new uint[] { 1, 2 }, 1, 1, 2, -1 };
            yield return new object[] { new uint[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new uint[] { 1, 2 }, null, 1, 2, -1 };

            // Int64
            yield return new object[] { new long[] { 1, 2 }, (ulong)1, 1, 2, -1 };
            yield return new object[] { new long[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new long[] { 1, 2 }, null, 1, 2, -1 };

            // UInt64
            yield return new object[] { new ulong[] { 1, 2 }, (long)1, 1, 2, -1 };
            yield return new object[] { new ulong[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new ulong[] { 1, 2 }, null, 1, 2, -1 };

            // Char
            yield return new object[] { new char[] { (char)1, (char)2 }, (ushort)1, 1, 2, -1 };
            yield return new object[] { new char[] { (char)1, (char)2 }, new object(), 1, 2, -1 };
            yield return new object[] { new char[] { (char)1, (char)2 }, null, 1, 2, -1 };

            // Bool
            yield return new object[] { new bool[] { true, false }, (char)0, 1, 2, -1 };
            yield return new object[] { new bool[] { true, false }, new object(), 1, 2, -1 };
            yield return new object[] { new bool[] { true, false }, null, 1, 2, -1 };

            // Single
            yield return new object[] { new float[] { 1, 2 }, (double)1, 1, 2, -1 };
            yield return new object[] { new float[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new float[] { 1, 2 }, null, 1, 2, -1 };

            // Double
            yield return new object[] { new double[] { 1, 2 }, (float)1, 1, 2, -1 };
            yield return new object[] { new double[] { 1, 2 }, new object(), 1, 2, -1 };
            yield return new object[] { new double[] { 1, 2 }, null, 1, 2, -1 };

            // IntPtr
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2 }, (UIntPtr)1, 1, 2, -1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2 }, new object(), 1, 2, -1 };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2 }, null, 1, 2, -1 };

            // UIntPtr
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2 }, (IntPtr)1, 1, 2, -1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2 }, new object(), 1, 2, -1 };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2 }, null, 1, 2, -1 };

            // String
            yield return new object[] { new string[] { "Hello", "Hello", "Goodbyte", "Goodbye" }, new object(), 3, 4, -1 };

            // Nullable
            var nullableArray = new int?[] { 0, null, 10, 10, 0 };
            yield return new object[] { nullableArray, null, 4, 5, 1 };
            yield return new object[] { nullableArray, 10, 4, 5, 3 };
            yield return new object[] { nullableArray, 100, 4, 5, -1 };
        }

        [Theory]
        [MemberData(nameof(LastIndexOf_SZArray_TestData))]
        public static void LastIndexOf_SZArray<T>(T[] array, T value, int startIndex, int count, int expected)
        {
            if (count - startIndex - 1 == 0 || array.Length == 0)
            {
                if (count == array.Length)
                {
                    // Use LastIndexOf<T>(T[], T)
                    Assert.Equal(expected, Array.LastIndexOf(array, value));
                }
                // Use LastIndexOf<T>(T[], T, int)
                Assert.Equal(expected, Array.LastIndexOf(array, value, startIndex));
            }
            // Use LastIndexOf<T>(T[], int, T, int)
            Assert.Equal(expected, Array.LastIndexOf(array, value, startIndex, count));

            // Basic: forward SZArray
            LastIndexOf_Array(array, value, startIndex, count, expected);

            // Advanced: convert SZArray to an array with non-zero lower bound
            const int LowerBound = 5;
            Array nonZeroLowerBoundArray = NonZeroLowerBoundArray(array, LowerBound);
            LastIndexOf_Array(nonZeroLowerBoundArray, value, startIndex + LowerBound, count, expected + LowerBound);
        }

        [Theory]
        [MemberData(nameof(LastIndexOf_Array_TestData))]
        public static void LastIndexOf_Array(Array array, object value, int startIndex, int count, int expected)
        {
            if (count - startIndex - 1 == 0 || array.Length == 0)
            {
                if (count == array.Length)
                {
                    // Use LastIndexOf(Array, object)
                    Assert.Equal(expected, Array.LastIndexOf(array, value));
                }
                // Use LastIndexOf(Array, object, int)
                Assert.Equal(expected, Array.LastIndexOf(array, value, startIndex));
            }
            // Use LastIndexOf(Array, object, int, int)
            Assert.Equal(expected, Array.LastIndexOf(array, value, startIndex, count));
        }
        
        [Fact]
        public static void LastIndexOf_NonInferrableEntries()
        {
            // Workaround: Move these values to LastIndexOf_SZArray_TestData if/ when https://github.com/xunit/xunit/pull/965 is available
            LastIndexOf_SZArray(new string[] { null, null, "Hello", "Hello", "Goodbye", "Goodbye", null, null }, null, 7, 8, 7);
            LastIndexOf_SZArray(new string[] { "Hello", "Hello", "Goodbye", "Goodbye" }, null, 3, 4, -1);
        }

        [Fact]
        public static void LastIndexOf_NullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf((int[])null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf((int[])null, "", 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf((int[])null, "", 0, 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf(null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf(null, "", 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf(null, "", 0, 0));
        }

        [Fact]
        public static void LastIndexOf_MultidimensionalArray_ThrowsRankException()
        {
            Assert.Throws<RankException>(() => Array.LastIndexOf(new int[1, 1], ""));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new int[1, 1], "", 0));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new int[1, 1], "", 0, 0));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new string[1, 1], ""));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new string[1, 1], "", 0));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new string[1, 1], "", 0, 0));
        }

        [Fact]
        public static void LastIndexOf_EmptyArrayInvalidStartIndexCount_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(new int[0], 0, 1, 0)); // Start index != 0 or -1
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.LastIndexOf(new int[0], 0, 0, 1)); // Count != 0
        }

        [Fact]
        public static void LastIndexOf_NegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(new int[1], "", -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(new int[1], "", -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(new string[1], "", -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(new string[1], "", -1, 0));
        }

        [Fact]
        public static void LastIndexOf_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.LastIndexOf(new int[1], "", 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.LastIndexOf(new string[1], "", 0, -1));
        }

        [Theory]
        [InlineData(3, 2, 4)]
        [InlineData(3, 0, 3)]
        [InlineData(3, 1, 3)]
        public static void LastIndexOf_InvalidStartIndexCount_ThrowsArgumentOutOfRangeExeption(int length, int startIndex, int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>("endIndex", () => Array.LastIndexOf(new int[length], "", startIndex, count));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.LastIndexOf(new int[length], 0, startIndex, count));
        }

        [Fact]
        public static unsafe void LastIndexOf_ArrayOfPointers_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Array.LastIndexOf((Array)new int*[2], null));
            Assert.Equal(-1, Array.LastIndexOf((Array)new int*[0], null));
        }

        public static IEnumerable<object[]> IStructuralComparable_TestData()
        {
            var intArray = new int[] { 2, 3, 4, 5 };

            yield return new object[] { intArray, intArray, new IntegerComparer(), 0 };
            yield return new object[] { intArray, new int[] { 2, 3, 4, 5 }, new IntegerComparer(), 0 };

            yield return new object[] { intArray, new int[] { 1, 3, 4, 5 }, new IntegerComparer(), 1 };
            yield return new object[] { intArray, new int[] { 2, 2, 4, 5 }, new IntegerComparer(), 1 };
            yield return new object[] { intArray, new int[] { 2, 3, 3, 5 }, new IntegerComparer(), 1 };
            yield return new object[] { intArray, new int[] { 2, 3, 4, 4 }, new IntegerComparer(), 1 };

            yield return new object[] { intArray, new int[] { 3, 3, 4, 5 }, new IntegerComparer(), -1 };
            yield return new object[] { intArray, new int[] { 2, 4, 4, 5 }, new IntegerComparer(), -1 };
            yield return new object[] { intArray, new int[] { 2, 3, 5, 5 }, new IntegerComparer(), -1 };
            yield return new object[] { intArray, new int[] { 2, 3, 4, 6 }, new IntegerComparer(), -1 };

            yield return new object[] { intArray, null, new IntegerComparer(), 1 };
        }

        [Theory]
        [MemberData(nameof(IStructuralComparable_TestData))]
        public static void IStructuralComparable(Array array, object other, IComparer comparer, int expected)
        {
            IStructuralComparable comparable = array;
            Assert.Equal(expected, Math.Sign(comparable.CompareTo(other, comparer)));
        }

        [Fact]
        public static void IStructuralComparable_Invalid()
        {
            IStructuralComparable comparable = new int[] { 1, 2, 3 };
            Assert.Throws<ArgumentException>("other", () => comparable.CompareTo(new int[] { 1, 2 }, new IntegerComparer())); // Arrays have different lengths
            Assert.Throws<ArgumentException>("other", () => comparable.CompareTo(new int[] { 1, 2, 3, 4 }, new IntegerComparer())); // Arrays have different lengths

            Assert.Throws<ArgumentException>("other", () => comparable.CompareTo(123, new IntegerComparer())); // Other is not an array
        }

        [Fact]
        public static void IStructuralComparable_NullComparer_ThrowsNullReferenceException()
        {
            // This was not fixed in order to be compatible with the full .NET framework and Xamarin. See #13410
            IStructuralComparable comparable = new int[] { 1, 2, 3 };
            Assert.Throws<NullReferenceException>(() => comparable.CompareTo(new int[] { 1, 2, 3 }, null));
        }

        public static IEnumerable<object[]> IStructuralEquatable_TestData()
        {
            var intArray = new int[] { 2, 3, 4, 5 };

            yield return new object[] { intArray, intArray, new IntegerComparer(), true, true };
            yield return new object[] { intArray, new int[] { 2, 3, 4, 5 }, new IntegerComparer(), true, true };

            yield return new object[] { intArray, new int[] { 1, 3, 4, 5 }, new IntegerComparer(), false, true };
            yield return new object[] { intArray, new int[] { 2, 2, 4, 5 }, new IntegerComparer(), false, true };
            yield return new object[] { intArray, new int[] { 2, 3, 3, 5 }, new IntegerComparer(), false, false };
            yield return new object[] { intArray, new int[] { 2, 3, 4, 4 }, new IntegerComparer(), false, true };

            var longIntArray = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
            yield return new object[] { longIntArray, longIntArray, new IntegerComparer(), true, true };
            yield return new object[] { longIntArray, intArray, new IntegerComparer(), false, false };

            yield return new object[] { intArray, new int[] { 2 }, new IntegerComparer(), false, false };
            yield return new object[] { intArray, new int[] { 2, 3, 4, 5, 6 }, new IntegerComparer(), false, false };
            yield return new object[] { intArray, 123, new IntegerComparer(), false, false };
            yield return new object[] { intArray, null, new IntegerComparer(), false, false };
        }

        [Theory]
        [MemberData(nameof(IStructuralEquatable_TestData))]
        public static void IStructuralEquatable(Array array, object other, IEqualityComparer comparer, bool expected, bool expectHashEquality)
        {
            IStructuralEquatable equatable = array;
            Assert.Equal(expected, equatable.Equals(other, comparer));
            if (other is IStructuralEquatable)
            {
                IStructuralEquatable equatable2 = (IStructuralEquatable)other;
                Assert.Equal(expectHashEquality, equatable.GetHashCode(comparer).Equals(equatable2.GetHashCode(comparer)));
            }
        }

        [Fact]
        public static void IStructuralEquatable_Equals_NullComparer_ThrowsNullReferenceException()
        {
            // This was not fixed in order to be compatible with the full .NET framework and Xamarin. See #13410
            IStructuralEquatable equatable = new int[] { 1, 2, 3 };
            Assert.Throws<NullReferenceException>(() => equatable.Equals(new int[] { 1, 2, 3 }, null));
        }

        [Fact]
        public static void IStructuralEquatable_GetHashCode_NullComparer_ThrowsArgumentNullException()
        {
            IStructuralEquatable equatable = new int[] { 1, 2, 3 };
            Assert.Throws<ArgumentNullException>("comparer", () => equatable.GetHashCode(null));
        }

        [Theory]
        [InlineData(new int[] { 1, 2, 3, 4, 5 }, 7, new int[] { 1, 2, 3, 4, 5, default(int), default(int) })]
        [InlineData(new int[] { 1, 2, 3, 4, 5 }, 3, new int[] { 1, 2, 3 })]
        [InlineData(null, 3, new int[] { default(int), default(int), default(int) })]
        public static void Resize(int[] array, int newSize, int[] expected)
        {
            int[] testArray = array;
            Array.Resize(ref testArray, newSize);
            Assert.Equal(newSize, testArray.Length);
            Assert.Equal(expected, testArray);
        }

        [Fact]
        public static void Resize_NegativeNewSize_ThrowsArgumentOutOfRangeException()
        {
            var array = new int[0];
            Assert.Throws<ArgumentOutOfRangeException>("newSize", () => Array.Resize(ref array, -1)); // New size < 0
            Assert.Equal(new int[0], array);
        }

        public static IEnumerable<object[]> Reverse_TestData()
        {
            // SByte
            yield return new object[] { new sbyte[] { 1, 2, 3, 4, 5 }, 0, 5, new sbyte[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new sbyte[] { 1, 2, 3, 4, 5 }, 2, 3, new sbyte[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new sbyte[] { 1, 2, 3, 4, 5 }, 0, 0, new sbyte[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new sbyte[] { 1, 2, 3, 4, 5 }, 5, 0, new sbyte[] { 1, 2, 3, 4, 5 } };

            // Byte
            yield return new object[] { new byte[] { 1, 2, 3, 4, 5 }, 0, 5, new byte[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new byte[] { 1, 2, 3, 4, 5 }, 2, 3, new byte[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3, 4, 5 }, 0, 0, new byte[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new byte[] { 1, 2, 3, 4, 5 }, 5, 0, new byte[] { 1, 2, 3, 4, 5 } };

            // Int16
            yield return new object[] { new short[] { 1, 2, 3, 4, 5 }, 0, 5, new short[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new short[] { 1, 2, 3, 4, 5 }, 2, 3, new short[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new short[] { 1, 2, 3, 4, 5 }, 0, 0, new short[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new short[] { 1, 2, 3, 4, 5 }, 5, 0, new short[] { 1, 2, 3, 4, 5 } };

            // UInt16
            yield return new object[] { new ushort[] { 1, 2, 3, 4, 5 }, 0, 5, new ushort[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new ushort[] { 1, 2, 3, 4, 5 }, 2, 3, new ushort[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new ushort[] { 1, 2, 3, 4, 5 }, 0, 0, new ushort[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new ushort[] { 1, 2, 3, 4, 5 }, 5, 0, new ushort[] { 1, 2, 3, 4, 5 } };

            // Int32
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, 0, 5, new int[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, 2, 3, new int[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, 0, 0, new int[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, 5, 0, new int[] { 1, 2, 3, 4, 5 } };

            // UInt32
            yield return new object[] { new uint[] { 1, 2, 3, 4, 5 }, 0, 5, new uint[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new uint[] { 1, 2, 3, 4, 5 }, 2, 3, new uint[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new uint[] { 1, 2, 3, 4, 5 }, 0, 0, new uint[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new uint[] { 1, 2, 3, 4, 5 }, 5, 0, new uint[] { 1, 2, 3, 4, 5 } };

            // Int64
            yield return new object[] { new long[] { 1, 2, 3, 4, 5 }, 0, 5, new long[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new long[] { 1, 2, 3, 4, 5 }, 2, 3, new long[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new long[] { 1, 2, 3, 4, 5 }, 0, 0, new long[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new long[] { 1, 2, 3, 4, 5 }, 5, 0, new long[] { 1, 2, 3, 4, 5 } };

            // UInt64
            yield return new object[] { new ulong[] { 1, 2, 3, 4, 5 }, 0, 5, new ulong[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new ulong[] { 1, 2, 3, 4, 5 }, 2, 3, new ulong[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new ulong[] { 1, 2, 3, 4, 5 }, 0, 0, new ulong[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new ulong[] { 1, 2, 3, 4, 5 }, 5, 0, new ulong[] { 1, 2, 3, 4, 5 } };

            // Char
            yield return new object[] { new char[] { '1', '2', '3', '4', '5' }, 0, 5, new char[] { '5', '4', '3', '2', '1' } };
            yield return new object[] { new char[] { '1', '2', '3', '4', '5' }, 2, 3, new char[] { '1', '2', '5', '4', '3' } };
            yield return new object[] { new char[] { '1', '2', '3', '4', '5' }, 0, 0, new char[] { '1', '2', '3', '4', '5' } };
            yield return new object[] { new char[] { '1', '2', '3', '4', '5' }, 5, 0, new char[] { '1', '2', '3', '4', '5' } };

            // Bool
            yield return new object[] { new bool[] { false, false, true, true, false }, 0, 5, new bool[] { false, true, true, false, false } };
            yield return new object[] { new bool[] { false, false, true, true, false }, 2, 3, new bool[] { false, false, false, true, true } };
            yield return new object[] { new bool[] { false, false, true, true, false }, 0, 0, new bool[] { false, false, true, true, false } };
            yield return new object[] { new bool[] { false, false, true, true, false }, 5, 0, new bool[] { false, false, true, true, false } };

            // Single
            yield return new object[] { new float[] { 1, 2, 3, 4, 5 }, 0, 5, new float[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new float[] { 1, 2, 3, 4, 5 }, 2, 3, new float[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new float[] { 1, 2, 3, 4, 5 }, 0, 0, new float[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new float[] { 1, 2, 3, 4, 5 }, 5, 0, new float[] { 1, 2, 3, 4, 5 } };

            // Double
            yield return new object[] { new double[] { 1, 2, 3, 4, 5 }, 0, 5, new double[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new double[] { 1, 2, 3, 4, 5 }, 2, 3, new double[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new double[] { 1, 2, 3, 4, 5 }, 0, 0, new double[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new double[] { 1, 2, 3, 4, 5 }, 5, 0, new double[] { 1, 2, 3, 4, 5 } };

            // IntPtr
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5 }, 0, 5, new IntPtr[] { (IntPtr)5, (IntPtr)4, (IntPtr)3, (IntPtr)2, (IntPtr)1 } };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5 }, 2, 3, new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)5, (IntPtr)4, (IntPtr)3 } };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5 }, 0, 0, new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5 } };
            yield return new object[] { new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5 }, 5, 0, new IntPtr[] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5 } };

            // UIntPtr
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)4, (UIntPtr)5 }, 0, 5, new UIntPtr[] { (UIntPtr)5, (UIntPtr)4, (UIntPtr)3, (UIntPtr)2, (UIntPtr)1 } };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)4, (UIntPtr)5 }, 2, 3, new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)5, (UIntPtr)4, (UIntPtr)3 } };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)4, (UIntPtr)5 }, 0, 0, new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)4, (UIntPtr)5 } };
            yield return new object[] { new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)4, (UIntPtr)5 }, 5, 0, new UIntPtr[] { (UIntPtr)1, (UIntPtr)2, (UIntPtr)3, (UIntPtr)4, (UIntPtr)5 } };

            // string[] can be cast to object[]
            yield return new object[] { new string[] { "1", "2", "3", "4", "5" }, 0, 5, new string[] { "5", "4", "3", "2", "1" } };
            yield return new object[] { new string[] { "1", "2", "3", "4", "5" }, 2, 3, new string[] { "1", "2", "5", "4", "3" } };
            yield return new object[] { new string[] { "1", "2", "3", "4", "5" }, 0, 0, new string[] { "1", "2", "3", "4", "5" } };
            yield return new object[] { new string[] { "1", "2", "3", "4", "5" }, 5, 0, new string[] { "1", "2", "3", "4", "5" } };

            // TestEnum[] can be cast to int[]
            var enumArray = new Int32Enum[] { Int32Enum.Case1, Int32Enum.Case2, Int32Enum.Case3, Int32Enum.Case1 };
            yield return new object[] { enumArray, 0, 4, new Int32Enum[] { Int32Enum.Case1, Int32Enum.Case3, Int32Enum.Case2, Int32Enum.Case1 } };
            yield return new object[] { enumArray, 2, 2, new Int32Enum[] { Int32Enum.Case1, Int32Enum.Case2, Int32Enum.Case1, Int32Enum.Case3 } };
            yield return new object[] { enumArray, 0, 0, enumArray};
            yield return new object[] { enumArray, 4, 0, enumArray};

            // ValueType array
            ComparableValueType[] valueTypeArray = new ComparableValueType[] { new ComparableValueType(0), new ComparableValueType(1) };
            yield return new object[] { valueTypeArray, 0, 2, new ComparableValueType[] { new ComparableValueType(1), new ComparableValueType(0) } };
            yield return new object[] { valueTypeArray, 0, 0, valueTypeArray };
            yield return new object[] { valueTypeArray, 2, 0, valueTypeArray };
        }

        [Theory]
        [MemberData(nameof(Reverse_TestData))]
        public static void Reverse_SZArray(Array array, int index, int length, Array expected)
        {
            // Basic: forward SZArray
            Reverse(array, index, length, expected);

            // Advanced: convert SZArray to an array with non-zero lower bound
            const int LowerBound = 5;
            Reverse(NonZeroLowerBoundArray(array, LowerBound), index + LowerBound, length, NonZeroLowerBoundArray(expected, LowerBound));
        }

        public static void Reverse(Array array, int index, int length, Array expected)
        {
            if (index == array.GetLowerBound(0) && length == array.Length)
            {
                // Use Reverse(Array)
                Array arrayClone1 = (Array)array.Clone();
                Array.Reverse(arrayClone1);
                Assert.Equal(expected, arrayClone1);
            }
            // Use Reverse(Array, int, int)
            Array arrayClone2 = (Array)array.Clone();
            Array.Reverse(arrayClone2, index, length);
            Assert.Equal(expected, expected);
        }

        [Fact]
        public static void Reverse_NullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.Reverse(null));
            Assert.Throws<ArgumentNullException>("array", () => Array.Reverse(null, 0, 0));
        }

        [Fact]
        public static void Reverse_MultidimensionalArray_ThrowsRankException()
        {
            Assert.Throws<RankException>(() => Array.Reverse((Array)new int[10, 10]));
            Assert.Throws<RankException>(() => Array.Reverse((Array)new int[10, 10], 0, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1)]
        public static void Reverse_IndexLessThanLowerBound_ThrowsArgumentOutOfRangeException(int lowerBound)
        {
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Reverse(NonZeroLowerBoundArray(new int[0], lowerBound), lowerBound - 1, 0));
        }

        [Fact]
        public static void Reverse_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Reverse((Array)new int[10], 0, -1));
        }

        [Theory]
        [InlineData(11, 0)]
        [InlineData(10, 1)]
        [InlineData(9, 2)]
        [InlineData(0, 11)]
        public static void Reverse_InvalidIndexPlusLength_ThrowsArgumentException(int index, int length)
        {
            Assert.Throws<ArgumentException>(null, () => Array.Reverse((Array)new int[10], index, length));
        }

        [Fact]
        public static unsafe void Reverse_ArrayOfPointers_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Array.Reverse((Array)new int*[2]));
            Array.Reverse((Array)new int*[0]);
            Array.Reverse((Array)new int*[1]);
        }

        public static IEnumerable<object[]> Sort_Array_NonGeneric_TestData()
        {
            yield return new object[] { new int[0], 0, 0, new IntegerComparer(), new int[0] };
            yield return new object[] { new int[] { 5 }, 0, 1, new IntegerComparer(), new int[] { 5 } };
            yield return new object[] { new int[] { 5, 2 }, 0, 2, new IntegerComparer(), new int[] { 2, 5 } };
            yield return new object[] { new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 }, 0, 9, new IntegerComparer(), new int[] { 2, 2, 3, 4, 4, 5, 6, 8, 9 } };
            yield return new object[] { new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 }, 3, 4, new IntegerComparer(), new int[] { 5, 2, 9, 2, 3, 4, 8, 4, 6 } };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 0, 9, new IntegerComparer(), new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 } };

            yield return new object[] { new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 }, 0, 9, null, new int[] { 2, 2, 3, 4, 4, 5, 6, 8, 9 } };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 0, 9, null, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 } };

            yield return new object[] { new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 }, 0, 0, null, new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 } };
            yield return new object[] { new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 }, 9, 0, null, new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 } };
        }

        public static IEnumerable<object[]> Sort_Array_Generic_TestData()
        {
            yield return new object[] { new string[0], 0, 0, new StringComparer(), new string[0] };
            yield return new object[] { new string[] { "5" }, 0, 1, new StringComparer(), new string[] { "5" } };
            yield return new object[] { new string[] { "5", "2" }, 0, 2, new StringComparer(), new string[] { "2", "5" } };
            yield return new object[] { new string[] { "5", "2", "9", "8", "4", "3", "2", "4", "6" }, 0, 9, new StringComparer(), new string[] { "2", "2", "3", "4", "4", "5", "6", "8", "9" } };
            yield return new object[] { new string[] { "5", null, "2", "9", "8", "4", "3", "2", "4", "6" }, 0, 10, new StringComparer(), new string[] { null, "2", "2", "3", "4", "4", "5", "6", "8", "9" } };
            yield return new object[] { new string[] { "5", null, "2", "9", "8", "4", "3", "2", "4", "6" }, 3, 4, new StringComparer(), new string[] { "5", null, "2", "3", "4", "8", "9", "2", "4", "6" } };
        }

        [Theory]
        [MemberData(nameof(Sort_Array_NonGeneric_TestData))]
        [MemberData(nameof(Sort_Array_Generic_TestData))]
        public static void Sort_Array_NonGeneric(Array array, int index, int length, IComparer comparer, Array expected)
        {
            Array sortedArray;
            if (index == 0 && length == array.Length)
            {
                // Use Sort(Array) or Sort(Array, IComparer)
                if (comparer == null)
                {
                    // Use Sort(Array)
                    sortedArray = (Array)array.Clone();
                    Array.Sort(sortedArray);
                    Assert.Equal(expected, sortedArray);
                }
                // Use Sort(Array, IComparer)
                sortedArray = (Array)array.Clone();
                Array.Sort(sortedArray, comparer);
                Assert.Equal(expected, sortedArray);
            }
            if (comparer == null)
            {
                // Use Sort(Array, int, int)
                sortedArray = (Array)array.Clone();
                Array.Sort(sortedArray, index, length);
                Assert.Equal(expected, sortedArray);
            }
            // Use Sort(Array, int, int, IComparer)
            sortedArray = (Array)array.Clone();
            Array.Sort(sortedArray, index, length, comparer);
            Assert.Equal(expected, sortedArray);
        }

        [Theory]
        [MemberData(nameof(Sort_Array_Generic_TestData))]
        public static void Sort_Array_Generic(string[] array, int index, int length, IComparer comparer, string[] expected)
        {
            string[] sortedArray;
            if (index == 0 && length == array.Length)
            {
                // Use Sort<T>(T[]) or Sort<T>(T[], IComparer)
                if (comparer == null)
                {
                    // Use Sort(Array)
                    sortedArray = (string[])array.Clone();
                    Array.Sort(sortedArray);
                    Assert.Equal(expected, sortedArray);
                }
                // Use Sort<T>(T[], IComparer)
                sortedArray = (string[])array.Clone();
                Array.Sort(sortedArray, comparer);
                Assert.Equal(expected, sortedArray);
            }
            if (comparer == null)
            {
                // Use Sort<T>(T[], int, int)
                sortedArray = (string[])array.Clone();
                Array.Sort(sortedArray, index, length);
                Assert.Equal(expected, sortedArray);
            }
            // Use Sort<T>(T[], int, int, IComparer)
            sortedArray = (string[])array.Clone();
            Array.Sort(sortedArray, index, length, comparer);
            Assert.Equal(expected, sortedArray);
        }

        [Fact]
        public static void Sort_Array_Invalid()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.Sort(null)); // Array is null
            Assert.Throws<ArgumentNullException>("array", () => Array.Sort((int[])null)); // Array is null

            Assert.Throws<RankException>(() => Array.Sort(new int[10, 10])); // Array is multidimensional

            Assert.Throws<InvalidOperationException>(() => Array.Sort((Array)new object[] { "1", 2, new object() })); // One or more objects in array do not implement IComparable
            Assert.Throws<InvalidOperationException>(() => Array.Sort(new object[] { "1", 2, new object() })); // One or more objects in array do not implement IComparable
        }

        [Fact]
        public static void Sort_Array_IComparer_Invalid()
        {
            // Array is null
            Assert.Throws<ArgumentNullException>("array", () => Array.Sort(null, (IComparer)null));
            Assert.Throws<ArgumentNullException>("array", () => Array.Sort(null, (IComparer<int>)null));

            Assert.Throws<RankException>(() => Array.Sort(new int[10, 10], (IComparer)null)); // Array is multidimensional

            // One or more objects in array do not implement IComparable
            Assert.Throws<InvalidOperationException>(() => Array.Sort(new object[] { "1", 2, new object() }, (IComparer)null));
            Assert.Throws<InvalidOperationException>(() => Array.Sort(new object[] { "1", 2, new object() }, (IComparer<object>)null));
        }

        [Fact]
        public static void Sort_Array_Comparison_Invalid()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.Sort(null, (Comparison<int>)null)); // Array is null
            Assert.Throws<ArgumentNullException>("comparison", () => Array.Sort(new int[10], (Comparison<int>)null)); // Comparison is null
        }

        [Fact]
        public static void Sort_Array_Int_Int_Invalid()
        {
            // Array is null
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort(null, 0, 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.Sort((int[])null, 0, 0));

            Assert.Throws<RankException>(() => Array.Sort(new int[10, 10], 0, 0, null)); // Array is multidimensional

            // One or more objects in keys do not implement IComparable
            Assert.Throws<InvalidOperationException>(() => Array.Sort((Array)new object[] { "1", 2, new object() }, 0, 3));
            Assert.Throws<InvalidOperationException>(() => Array.Sort(new object[] { "1", 2, new object() }, 0, 3));

            // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Sort((Array)new int[10], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Sort(new int[10], -1, 0));

            // Length < 0
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Sort((Array)new int[10], 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Sort(new int[10], 0, -1));

            // Index + length > list.Count
            Assert.Throws<ArgumentException>(() => Array.Sort((Array)new int[10], 11, 0));
            Assert.Throws<ArgumentException>(() => Array.Sort(new int[10], 11, 0));
            Assert.Throws<ArgumentException>(() => Array.Sort((Array)new int[10], 10, 1));
            Assert.Throws<ArgumentException>(() => Array.Sort(new int[10], 10, 1));
            Assert.Throws<ArgumentException>(() => Array.Sort((Array)new int[10], 9, 2));
            Assert.Throws<ArgumentException>(() => Array.Sort(new int[10], 9, 2));
        }

        [Fact]
        public static void Sort_Array_Int_Int_IComparer_Invalid()
        {
            // Array is null
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort(null, 0, 0, null));
            Assert.Throws<ArgumentNullException>("array", () => Array.Sort((int[])null, 0, 0, null));

            Assert.Throws<RankException>(() => Array.Sort(new int[10, 10], 0, 0, null)); // Array is multidimensional

            // One or more objects in keys do not implement IComparable
            Assert.Throws<InvalidOperationException>(() => Array.Sort((Array)new object[] { "1", 2, new object() }, 0, 3, null));
            Assert.Throws<InvalidOperationException>(() => Array.Sort(new object[] { "1", 2, new object() }, 0, 3, null));

            // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Sort((Array)new int[10], -1, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Sort(new int[10], -1, 0, null));

            // Length < 0
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Sort((Array)new int[10], 0, -1, null));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Sort(new int[10], 0, -1, null));

            // Index + length > list.Count
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], 11, 0, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], 11, 0, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], 10, 1, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], 10, 1, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], 9, 2, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], 9, 2, null));
        }

        public static IEnumerable<object[]> Sort_Array_Array_NonGeneric_TestData()
        {
            yield return new object[] { new int[] { 3, 1, 2 }, new int[] { 4, 5, 6 }, 0, 3, new IntegerComparer(), new int[] { 1, 2, 3 }, new int[] { 5, 6, 4 } };
            yield return new object[] { new int[] { 3, 1, 2 }, new string[] { "a", "b", "c" }, 0, 3, new IntegerComparer(), new int[] { 1, 2, 3 }, new string[] { "b", "c", "a" } };

            yield return new object[] { new string[] { "bcd", "bc", "c", "ab" }, new int[] { 1, 2, 3, 4 }, 0, 4, new StringComparer(), new string[] { "ab", "bc", "bcd", "c" }, new int[] { 4, 2, 1, 3 } };
            yield return new object[] { new string[] { "bcd", "bc", "c", "ab" }, new int[] { 1, 2, 3, 4 }, 1, 2, new StringComparer(), new string[] { "bcd", "bc", "c", "ab" }, new int[] { 1, 2, 3, 4 } };

            yield return new object[] { new int[] { 3, 1, 2 }, new string[] { "a", "b", "c" }, 0, 2, new IntegerComparer(), new int[] { 1, 3, 2 }, new string[] { "b", "a", "c" } };
            yield return new object[] { new int[] { 3, 1, 2, 1 }, new string[] { "a", "b", "c", "d" }, 1, 3, new IntegerComparer(), new int[] { 3, 1, 1, 2 }, new string[] { "a", "b", "d", "c" } };

            yield return new object[] { new int[] { 3, 1, 2 }, new string[] { "a", "b", "c" }, 0, 3, null, new int[] { 1, 2, 3 }, new string[] { "b", "c", "a" } };
            yield return new object[] { new int[] { 3, 2, 1, 4 }, new string[] { "a", "b", "c", "d" }, 1, 2, null, new int[] { 3, 1, 2, 4 }, new string[] { "a", "c", "b", "d" } };

            yield return new object[] { new int[] { 3, 2, 1, 4 }, new string[] { "a", "b", "c", "d" }, 0, 4, new ReverseIntegerComparer(), new int[] { 4, 3, 2, 1 }, new string[] { "d", "a", "b", "c" } };

            yield return new object[] { new int[] { 3, 2, 1, 4 }, new string[] { "a", "b", "c", "d", "e", "f" }, 0, 4, new IntegerComparer(), new int[] { 1, 2, 3, 4 }, new string[] { "c", "b", "a", "d", "e", "f" } }; // Items.Length > keys.Length

            yield return new object[] { new int[] { 3, 2, 1, 4 }, null, 0, 4, null, new int[] { 1, 2, 3, 4 }, null };
            yield return new object[] { new int[] { 3, 2, 1, 4 }, null, 1, 2, null, new int[] { 3, 1, 2, 4 }, null };

            var refArray = new ComparableRefType[] { new ComparableRefType(-5), new ComparableRefType(-4), new ComparableRefType(2), new ComparableRefType(-10), new ComparableRefType(5), new ComparableRefType(0) };
            var sortedRefArray = new ComparableRefType[] { new ComparableRefType(-10), new ComparableRefType(-5), new ComparableRefType(-4), new ComparableRefType(0), new ComparableRefType(2), new ComparableRefType(5) };
            var reversedRefArray = new ComparableRefType[] { new ComparableRefType(5), new ComparableRefType(2), new ComparableRefType(0), new ComparableRefType(-4), new ComparableRefType(-5), new ComparableRefType(-10) };

            var valueArray = new ComparableValueType[] { new ComparableValueType(-5), new ComparableValueType(-4), new ComparableValueType(2), new ComparableValueType(-10), new ComparableValueType(5), new ComparableValueType(0) };
            var sortedValueArray = new ComparableValueType[] { new ComparableValueType(-10), new ComparableValueType(-5), new ComparableValueType(-4), new ComparableValueType(0), new ComparableValueType(2), new ComparableValueType(5) };

            yield return new object[] { refArray, refArray, 0, 6, null, sortedRefArray, sortedRefArray }; // Reference type, reference type
            yield return new object[] { valueArray, valueArray, 0, 6, null, sortedValueArray, sortedValueArray }; // Value type, value type
            yield return new object[] { refArray, valueArray, 0, 6, null, sortedRefArray, sortedValueArray }; // Reference type, value type
            yield return new object[] { valueArray, refArray, 0, 6, null, sortedValueArray, sortedRefArray }; // Value type, reference type

            yield return new object[] { refArray, refArray, 0, 6, new ReferenceTypeNormalComparer(), sortedRefArray, sortedRefArray }; // Reference type, reference type
            yield return new object[] { refArray, refArray, 0, 6, new ReferenceTypeReverseComparer(), reversedRefArray, reversedRefArray }; // Reference type, reference type

            yield return new object[] { new int[0], new int[0], 0, 0, null, new int[0], new int[0] };
            yield return new object[] { refArray, null, 0, 6, null, sortedRefArray, null }; // Null items
        }

        public static IEnumerable<object[]> Sort_Array_Array_Generic_TestData()
        {
            yield return new object[] { new string[] { "bcd", "bc", "c", "ab" }, new string[] { "a", "b", "c", "d" }, 0, 4, new StringComparer(), new string[] { "ab", "bc", "bcd", "c" }, new string[] { "d", "b", "a", "c" } };
            yield return new object[] { new string[] { "bcd", "bc", "c", "ab" }, new string[] { "a", "b", "c", "d" }, 0, 4, null, new string[] { "ab", "bc", "bcd", "c" }, new string[] { "d", "b", "a", "c" } };
        }

        [Theory]
        [MemberData(nameof(Sort_Array_Array_NonGeneric_TestData))]
        [MemberData(nameof(Sort_Array_Array_Generic_TestData))]
        public static void Sort_Array_Array_NonGeneric(Array keys, Array items, int index, int length, IComparer comparer, Array expectedKeys, Array expectedItems)
        {
            Array sortedKeysArray = null;
            Array sortedItemsArray = null;
            if (index == 0 && length == keys.Length)
            {
                // Use Sort(Array, Array) or Sort(Array, Array, IComparer)
                if (comparer == null)
                {
                    // Use Sort(Array, Array)
                    sortedKeysArray = (Array)keys.Clone();
                    if (items != null)
                    {
                        sortedItemsArray = (Array)items.Clone();
                    }
                    Array.Sort(sortedKeysArray, sortedItemsArray);
                    Assert.Equal(expectedKeys, sortedKeysArray);
                    Assert.Equal(expectedItems, sortedItemsArray);
                }
                // Use Sort(Array, Array, IComparer)
                sortedKeysArray = (Array)keys.Clone();
                if (items != null)
                {
                    sortedItemsArray = (Array)items.Clone();
                }
                Array.Sort(sortedKeysArray, sortedItemsArray, comparer);
                Assert.Equal(expectedKeys, sortedKeysArray);
                Assert.Equal(expectedItems, sortedItemsArray);
            }
            if (comparer == null)
            {
                // Use Sort(Array, Array, int, int)
                sortedKeysArray = (Array)keys.Clone();
                if (items != null)
                {
                    sortedItemsArray = (Array)items.Clone();
                }
                Array.Sort(sortedKeysArray, sortedItemsArray, index, length);
                Assert.Equal(expectedKeys, sortedKeysArray);
                Assert.Equal(expectedItems, sortedItemsArray);
            }
            // Use Sort(Array, Array, int, int, IComparer)
            sortedKeysArray = (Array)keys.Clone();
            if (items != null)
            {
                sortedItemsArray = (Array)items.Clone();
            }
            Array.Sort(sortedKeysArray, sortedItemsArray, index, length, comparer);
            Assert.Equal(expectedKeys, sortedKeysArray);
            Assert.Equal(expectedItems, sortedItemsArray);
        }

        [Theory]
        [MemberData(nameof(Sort_Array_Array_Generic_TestData))]
        public static void Sort_Array_Array_Generic(string[] keys, string[] items, int index, int length, IComparer comparer, string[] expectedKeys, string[] expectedItems)
        {
            string[] sortedKeysArray = null;
            string[] sortedItemsArray = null;
            if (index == 0 && length == keys.Length)
            {
                // Use Sort<T>(T[], T[]) or Sort<T>(T[], T[], IComparer)
                if (comparer == null)
                {
                    // Use Sort<T>(T[], T[])
                    sortedKeysArray = (string[])keys.Clone();
                    if (items != null)
                    {
                        sortedItemsArray = (string[])items.Clone();
                    }
                    Array.Sort(sortedKeysArray, sortedItemsArray);
                    Assert.Equal(expectedKeys, sortedKeysArray);
                    Assert.Equal(expectedItems, sortedItemsArray);
                }
                // Use Sort<T>(T[], T[], IComparer)
                sortedKeysArray = (string[])keys.Clone();
                if (items != null)
                {
                    sortedItemsArray = (string[])items.Clone();
                }
                Array.Sort(sortedKeysArray, sortedItemsArray, comparer);
                Assert.Equal(expectedKeys, sortedKeysArray);
                Assert.Equal(expectedItems, sortedItemsArray);
            }
            if (comparer == null)
            {
                // Use Sort<T>(T[], T[], int, int)
                sortedKeysArray = (string[])keys.Clone();
                if (items != null)
                {
                    sortedItemsArray = (string[])items.Clone();
                }
                Array.Sort(sortedKeysArray, sortedItemsArray, index, length);
                Assert.Equal(expectedKeys, sortedKeysArray);
                Assert.Equal(expectedItems, sortedItemsArray);
            }
            // Use Sort(Array, Array, int, int, IComparer)
            sortedKeysArray = (string[])keys.Clone();
            if (items != null)
            {
                sortedItemsArray = (string[])items.Clone();
            }
            Array.Sort(sortedKeysArray, sortedItemsArray, index, length, comparer);
            Assert.Equal(expectedKeys, sortedKeysArray);
            Assert.Equal(expectedItems, sortedItemsArray);
        }

        [Fact]
        public static void Sort_Array_Array_Invalid()
        {
            // Keys is null
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort(null, new int[10]));
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort((int[])null, new int[10]));

            // Keys.Length > items.Length
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[9]));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[9]));

            Assert.Throws<RankException>(() => Array.Sort(new int[10, 10], new int[10])); // Keys is multidimensional
            Assert.Throws<RankException>(() => Array.Sort(new int[10], new int[10, 10])); // Items is multidimensional

            Array keys = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { 1 });
            Array items = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { 2 });
            Assert.Throws<ArgumentException>(null, () => Array.Sort(keys, items)); // Keys and items have different lower bounds

            // One or more objects in keys do not implement IComparable
            Assert.Throws<InvalidOperationException>(() => Array.Sort((Array)new object[] { "1", 2, new object() }, new object[3]));
            Assert.Throws<InvalidOperationException>(() => Array.Sort(new object[] { "1", 2, new object() }, new object[3]));
        }

        [Fact]
        public static void Sort_Array_Array_IComparer_Invalid()
        {
            // Keys is null
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort(null, new int[10], null));
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort((int[])null, new int[10], null));

            // Keys.Length > items.Length
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[9], null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[9], null));

            Assert.Throws<RankException>(() => Array.Sort(new int[10, 10], new int[10], null)); // Keys is multidimensional
            Assert.Throws<RankException>(() => Array.Sort(new int[10], new int[10, 10], null)); // Items is multidimensional

            Array keys = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { 1 });
            Array items = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { 2 });
            Assert.Throws<ArgumentException>(null, () => Array.Sort(keys, items, null)); // Keys and items have different lower bounds

            // One or more objects in keys do not implement IComparable
            Assert.Throws<InvalidOperationException>(() => Array.Sort((Array)new object[] { "1", 2, new object() }, new object[3], null));
            Assert.Throws<InvalidOperationException>(() => Array.Sort(new object[] { "1", 2, new object() }, new object[3], null));
        }

        [Fact]
        public static void Sort_Array_Array_Int_Int_Invalid()
        {
            // Keys is null
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort(null, new int[10], 0, 0));
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort((int[])null, new int[10], 0, 0));

            // Keys.Length > items.Length
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[9], 0, 10));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[9], 0, 10));

            Assert.Throws<RankException>(() => Array.Sort(new int[10, 10], new int[10], 0, 0)); // Keys is multidimensional
            Assert.Throws<RankException>(() => Array.Sort(new int[10], new int[10, 10], 0, 0)); // Items is multidimensional

            Array keys = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { 1 });
            Array items = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { 2 });
            Assert.Throws<ArgumentException>(null, () => Array.Sort(keys, items, 0, 1)); // Keys and items have different lower bounds

            // One or more objects in keys do not implement IComparable
            Assert.Throws<InvalidOperationException>(() => Array.Sort((Array)new object[] { "1", 2, new object() }, new object[3], 0, 3));
            Assert.Throws<InvalidOperationException>(() => Array.Sort(new object[] { "1", 2, new object() }, new object[3], 0, 3));

            // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Sort((Array)new int[10], new int[10], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Sort(new int[10], new int[10], -1, 0));

            // Length < 0
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Sort((Array)new int[10], new int[10], 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Sort(new int[10], new int[10], 0, -1));

            // Index + length > list.Count
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[10], 11, 0));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[10], 11, 0));
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[10], 10, 1));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[10], 10, 1));
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[10], 9, 2));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[10], 9, 2));
        }

        [Fact]
        public static void Sort_Array_Array_Int_Int_IComparer_Invalid()
        {
            // Keys is null
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort(null, new int[10], 0, 0, null));
            Assert.Throws<ArgumentNullException>("keys", () => Array.Sort((int[])null, new int[10], 0, 0, null));

            // Keys.Length > items.Length
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[9], 0, 10, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[9], 0, 10, null));

            Assert.Throws<RankException>(() => Array.Sort(new int[10, 10], new int[10], 0, 0, null)); // Keys is multidimensional
            Assert.Throws<RankException>(() => Array.Sort(new int[10], new int[10, 10], 0, 0, null)); // Items is multidimensional

            Array keys = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { 1 });
            Array items = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { 2 });
            Assert.Throws<ArgumentException>(null, () => Array.Sort(keys, items, 0, 1, null)); // Keys and items have different lower bounds

            // One or more objects in keys do not implement IComparable
            Assert.Throws<InvalidOperationException>(() => Array.Sort((Array)new object[] { "1", 2, new object() }, new object[3], 0, 3, null));
            Assert.Throws<InvalidOperationException>(() => Array.Sort(new object[] { "1", 2, new object() }, new object[3], 0, 3, null));

            // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Sort((Array)new int[10], new int[10], -1, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Sort(new int[10], new int[10], -1, 0, null));

            // Length < 0
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Sort((Array)new int[10], new int[10], 0, -1, null));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Sort(new int[10], new int[10], 0, -1, null));

            // Index + length > list.Count
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[10], 11, 0, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[10], 11, 0, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[10], 10, 1, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[10], 10, 1, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort((Array)new int[10], new int[10], 9, 2, null));
            Assert.Throws<ArgumentException>(null, () => Array.Sort(new int[10], new int[10], 9, 2, null));
        }

        [Fact]
        public static void SetValue_Casting()
        {
            // Null -> default(null)
            var arr1 = new NonGenericStruct[3];
            arr1[1].x = 0x22222222;
            arr1.SetValue(null, new int[] { 1 });
            Assert.Equal(0, arr1[1].x);

            // T -> Nullable<T>
            var arr2 = new int?[3];
            arr2.SetValue(42, new int[] { 1 });
            int? nullable1 = arr2[1];
            Assert.True(nullable1.HasValue);
            Assert.Equal(42, nullable1.Value);

            // Null -> Nullable<T>
            var arr3 = new int?[3];
            arr3[1] = 42;
            arr3.SetValue(null, new int[] { 1 });
            int? nullable2 = arr3[1];
            Assert.False(nullable2.HasValue);

            // Primitive widening
            var arr4 = new int[3];
            arr4.SetValue((short)42, new int[] { 1 });
            Assert.Equal(42, arr4[1]);

            // Widening from enum to primitive
            var arr5 = new int[3];
            arr5.SetValue(SByteEnum.MinusTwo, new int[] { 1 });
            Assert.Equal(-2, arr5[1]);
        }

        [Fact]
        public static void SetValue_Casting_Invalid()
        {
            // Unlike most of the other reflection apis, converting or widening a primitive to an enum is NOT allowed.
            var arr1 = new SByteEnum[3];
            Assert.Throws<InvalidCastException>(() => arr1.SetValue((sbyte)1, new int[] { 1 }));

            // Primitive widening must be value-preserving
            var arr2 = new int[3];
            Assert.Throws<ArgumentException>(null, () => arr2.SetValue((uint)42, new int[] { 1 }));

            // T -> Nullable<T>  T must be exact
            var arr3 = new int?[3];
            Assert.Throws<InvalidCastException>(() => arr3.SetValue((short)42, new int[] { 1 }));
        }

        [Fact]
        public static void SetValue_Invalid()
        {
            Assert.Throws<InvalidCastException>(() => new int[10].SetValue("1", 1)); // Value has an incompatible type
            Assert.Throws<InvalidCastException>(() => new int[10, 10].SetValue("1", new int[] { 1, 1 })); // Value has an incompatible type

            Assert.Throws<IndexOutOfRangeException>(() => new int[10].SetValue(1, -1)); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => new int[10].SetValue(1, 10)); // Index >= array.Length
            Assert.Throws<ArgumentException>(null, () => new int[10, 10].SetValue(1, 0)); // Array is multidimensional

            Assert.Throws<ArgumentNullException>("indices", () => new int[10].SetValue(1, (int[])null)); // Indices is null
            Assert.Throws<ArgumentException>(null, () => new int[10, 10].SetValue(1, new int[] { 1, 2, 3 })); // Indices.Length > array.Length

            Assert.Throws<IndexOutOfRangeException>(() => new int[8, 10].SetValue(1, new int[] { -1, 2 })); // Indices[0] < 0
            Assert.Throws<IndexOutOfRangeException>(() => new int[8, 10].SetValue(1, new int[] { 9, 2 })); // Indices[0] > array.GetLength(0)

            Assert.Throws<IndexOutOfRangeException>(() => new int[10, 8].SetValue(1, new int[] { 1, -1 })); // Indices[1] < 0
            Assert.Throws<IndexOutOfRangeException>(() => new int[10, 8].SetValue(1, new int[] { 1, 9 })); // Indices[1] > array.GetLength(1)
        }
            
        public static IEnumerable<object[]> TrueForAll_TestData()
        {
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, (Predicate<int>)(i => i > 0), true };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, (Predicate<int>)(i => i == 3), false };
            yield return new object[] { new int[0], (Predicate<int>)(i => false), true };
        }

        [Theory]
        [MemberData(nameof(TrueForAll_TestData))]
        public static void TrueForAll(int[] array, Predicate<int> match, bool expected)
        {
            Assert.Equal(expected, Array.TrueForAll(array, match));
        }

        [Fact]
        public static void TrueForAll_Null_ThrowsArgumentNullException()
        {
            // Array is null
            Assert.Throws<ArgumentNullException>("array", () => Array.TrueForAll((int[])null, i => i > 0));
            Assert.Throws<ArgumentNullException>("match", () => Array.TrueForAll(new int[0], null));
        }

        [Fact]
        public static void ICollection_IsSynchronized_ReturnsFalse()
        {
            ICollection array = new int[] { 1, 2, 3 };
            Assert.False(array.IsSynchronized);
            Assert.Same(array, array.SyncRoot);
        }

        [Theory]
        [InlineData(new int[] { 0, 1, 2, 3, 4 }, new int[] { 9, 9, 9, 9, 9 }, 0, new int[] { 0, 1, 2, 3, 4 })]
        [InlineData(new int[] { 0, 1, 2, 3, 4 }, new int[] { 9, 9, 9, 9, 9, 9, 9, 9 }, 2, new int[] { 9, 9, 0, 1, 2, 3, 4, 9 })]
        public static void IList_CopyTo(Array array, Array destinationArray, int index, Array expected)
        {
            IList iList = array;
            iList.CopyTo(destinationArray, index);
            Assert.Equal(expected, destinationArray);
        }

        [Fact]
        public static void IList_CopyTo_Invalid()
        {
            IList iList = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            Assert.Throws<ArgumentNullException>("dest", () => iList.CopyTo(null, 0)); // Destination array is null

            Assert.Throws<ArgumentOutOfRangeException>("dstIndex", () => iList.CopyTo(new int[7], -1)); // Index < 0
            Assert.Throws<ArgumentException>("", () => iList.CopyTo(new int[7], 8)); // Index > destinationArray.Length
        }

        private static void VerifyArray(Array array, Type elementType, int[] lengths, int[] lowerBounds, object repeatedValue)
        {
            VerifyArray(array, elementType, lengths, lowerBounds);

            // Pointer arrays don't support enumeration
            if (!elementType.IsPointer)
            {
                foreach (object obj in array)
                {
                    Assert.Equal(repeatedValue, obj);
                }
            }
        }
        
        private static void VerifyArray(Array array, Type elementType, int[] lengths, int[] lowerBounds)
        {
            Assert.Equal(elementType, array.GetType().GetElementType());
            Assert.Equal(array.Rank, array.GetType().GetArrayRank());

            Assert.Equal(lengths.Length, array.Rank);
            Assert.Equal(GetLength(lengths), array.Length);

            for (int dimension = 0; dimension < array.Rank; dimension++)
            {
                Assert.Equal(lengths[dimension], array.GetLength(dimension));
                Assert.Equal(lowerBounds[dimension], array.GetLowerBound(dimension));

                Assert.Equal(lowerBounds[dimension] + lengths[dimension] - 1, array.GetUpperBound(dimension));
            }
            
            Assert.Throws<IndexOutOfRangeException>(() => array.GetLength(-1)); // Dimension < 0
            Assert.Throws<IndexOutOfRangeException>(() => array.GetLength(array.Rank)); // Dimension >= array.Rank

            Assert.Throws<IndexOutOfRangeException>(() => array.GetLowerBound(-1)); // Dimension < 0
            Assert.Throws<IndexOutOfRangeException>(() => array.GetLowerBound(array.Rank)); // Dimension >= array.Rank

            Assert.Throws<IndexOutOfRangeException>(() => array.GetUpperBound(-1)); // Dimension < 0
            Assert.Throws<IndexOutOfRangeException>(() => array.GetUpperBound(array.Rank)); // Dimension >= array.Rank

            if (!elementType.IsPointer)
            {
                VerifyArrayAsIList(array);
            }
        }

        private static void VerifyArrayAsIList(Array array)
        {
            IList iList = array;
            Assert.Equal(array.Length, iList.Count);
            
            Assert.Equal(array, iList.SyncRoot);

            Assert.False(iList.IsSynchronized);
            Assert.True(iList.IsFixedSize);
            Assert.False(iList.IsReadOnly);

            Assert.Throws<NotSupportedException>(() => iList.Add(2));
            Assert.Throws<NotSupportedException>(() => iList.Insert(0, 2));
            Assert.Throws<NotSupportedException>(() => iList.Remove(0));
            Assert.Throws<NotSupportedException>(() => iList.RemoveAt(0));

            if (array.Rank == 1)
            {
                int lowerBound = array.GetLowerBound(0);
                for (int i = lowerBound; i < lowerBound + array.Length; i++)
                {
                    object obj = iList[i];
                    Assert.Equal(array.GetValue(i), obj);
                    Assert.Equal(Array.IndexOf(array, obj) >= lowerBound, iList.Contains(obj));
                    Assert.Equal(Array.IndexOf(array, obj), iList.IndexOf(obj));
                }

                Assert.Equal(Array.IndexOf(array, null) >= lowerBound, iList.Contains(null));
                Assert.Equal(Array.IndexOf(array, 999) >= lowerBound, iList.Contains(999));
                Assert.Equal(Array.IndexOf(array, null), iList.IndexOf(null));
                Assert.Equal(Array.IndexOf(array, 999), iList.IndexOf(999));

                if (array.Length > 1)
                {
                    object oldValue = iList[lowerBound];
                    object newValue = iList[lowerBound + 1];
                    iList[lowerBound] = newValue;
                    Assert.Equal(newValue, iList[lowerBound]);
                    iList[lowerBound] = oldValue;
                }
            }
            else
            {
                Assert.Throws<RankException>(() => iList.Contains(null));
                Assert.Throws<RankException>(() => iList.IndexOf(null));
                Assert.Throws<ArgumentException>(null, () => iList[0]);
                Assert.Throws<ArgumentException>(null, () => iList[0] = 1);
            }
        }

        private static Array NonZeroLowerBoundArray(Array szArrayContents, int lowerBound)

        {
            Assert.Equal(0, szArrayContents.GetLowerBound(0));

            Array array = Array.CreateInstance(szArrayContents.GetType().GetElementType(), new int[] { szArrayContents.Length }, new int[] { lowerBound });
            for (int i = 0; i < szArrayContents.Length; i++)
            {
                array.SetValue(szArrayContents.GetValue(i), i + lowerBound);
            }
            return array;
        }

        private static int GetLength(int[] lengths)
        {
            int length = 1;
            for (int i = 0; i < lengths.Length; i++)
            {
                length *= lengths[i];
            }
            return length;
        }

        private static NonGenericStruct[] CreateStructArray()
        {
            return new NonGenericStruct[]
            {
                new NonGenericStruct { x = 1, s = "Hello1", z = 2 },
                new NonGenericStruct { x = 2, s = "Hello2", z = 3 },
                new NonGenericStruct { x = 3, s = "Hello3", z = 4 },
                new NonGenericStruct { x = 4, s = "Hello4", z = 5 },
                new NonGenericStruct { x = 5, s = "Hello5", z = 6 }
            };
        }

        private struct NonGenericStruct
        {
            public int x;
            public string s;
            public int z;
        }

        private class IntegerComparer : IComparer, IComparer<int>, IEqualityComparer
        {
            public int Compare(object x, object y) => Compare((int)x, (int)y);

            public int Compare(int x, int y) => x - y;

            bool IEqualityComparer.Equals(object x, object y) => ((int)x) == ((int)y);

            public int GetHashCode(object obj) => ((int)obj) >> 2;
        }

        public class ReverseIntegerComparer : IComparer
        {
            public int Compare(object x, object y) => -((int)x).CompareTo((int)y);
        }

        private class StringComparer : IComparer, IComparer<string>
        {
            public int Compare(object x, object y) => Compare((string)x, (string)y);

            public int Compare(string x, string y) => string.Compare(x, y);
        }

        private class ComparableRefType : IComparable, IEquatable<ComparableRefType>
        {
            public int Id;

            public ComparableRefType(int id)
            {
                Id = id;
            }

            public int CompareTo(object other)
            {
                ComparableRefType o = (ComparableRefType)other;
                return Id.CompareTo(o.Id);
            }

            public override string ToString() => "C:" + Id;

            public override bool Equals(object obj)
            {
                return obj is ComparableRefType && ((ComparableRefType)obj).Id == Id;
            }

            public bool Equals(ComparableRefType other) => other.Id == Id;

            public override int GetHashCode() => Id.GetHashCode();
        }

        private struct ComparableValueType : IComparable, IEquatable<ComparableValueType>
        {
            public int Id;

            public ComparableValueType(int id)
            {
                Id = id;
            }

            public int CompareTo(object other)
            {
                ComparableValueType o = (ComparableValueType)other;
                return Id.CompareTo(o.Id);
            }

            public override string ToString() => "S:" + Id;

            public override bool Equals(object obj)
            {
                return obj is ComparableValueType && ((ComparableValueType)obj).Equals(this);
            }

            public bool Equals(ComparableValueType other) => other.Id == Id;

            public override int GetHashCode() => Id.GetHashCode();
        }

        private class ReferenceTypeNormalComparer : IComparer
        {
            public int Compare(ComparableRefType x, ComparableRefType y) => x.CompareTo(y);

            public int Compare(object x, object y) => Compare((ComparableRefType)x, (ComparableRefType)y);
        }

        private class ReferenceTypeReverseComparer : IComparer
        {
            public int Compare(ComparableRefType x, ComparableRefType y) => -x.CompareTo(y);

            public int Compare(object x, object y) => Compare((ComparableRefType)x, (ComparableRefType)y);
        }

        private class NotInt32 : IEquatable<int>
        {
            public bool Equals(int other)
            {
                throw new NotImplementedException();
            }
        }

        public class EqualsOverrider
        {
            public int Value { get; set; }
            public override bool Equals(object other) => other is EqualsOverrider && ((EqualsOverrider)other).Value == Value;
            public override int GetHashCode() => Value;
        }

        public class NonGenericClass1 { }
        public class NonGenericClass2 { }

        public class NonGenericSubClass2 : NonGenericClass2 { }
        public class NonGenericSubClass1 : NonGenericClass1 { }

        public class GenericClass<T> { }
        public struct GenericStruct<T> { }

        public interface NonGenericInterface1 { }
        public interface NonGenericInterface2 { }
        public interface GenericInterface<T> { }

        public struct StructWithNonGenericInterface1 : NonGenericInterface1 { }
        public struct StructWithNonGenericInterface1_2 : NonGenericInterface1, NonGenericInterface2 { }

        public class ClassWithNonGenericInterface1 : NonGenericInterface1 { }
        public class ClassWithNonGenericInterface1_2 : NonGenericInterface1, NonGenericInterface2 { }

        public interface NonGenericInterfaceWithNonGenericInterface1 : NonGenericInterface1 { }
        public class ClassWithNonGenericInterfaceWithNonGenericInterface1 : NonGenericInterfaceWithNonGenericInterface1 { }

        public abstract class AbstractClass { }
        public static class StaticClass { }

        public enum SByteEnum : sbyte
        {
            MinusTwo = -2,
            Zero = 0,
            Five = 5
        }

        public enum Int16Enum : short
        {
            Min = short.MinValue,
            One = 1,
            Two = 2,
            Max = short.MaxValue
        }
            
        public enum Int32Enum
        {
            Case1,
            Case2,
            Case3
        }

        public enum Int64Enum : long { }
    }
}

