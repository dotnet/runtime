// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static class ArrayTests
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
        public static void IList_IndexOf()
        {
            IList iList = new int[] { 1, 2, 3, 4 };
            for (int i = 0; i < iList.Count; i++)
            {
                Assert.Equal(i, iList.IndexOf(iList[i]));
            }
            Assert.Equal(-1, iList.IndexOf(999)); // No such value
            Assert.Equal(-1, iList.IndexOf(null));
            Assert.Equal(-1, iList.IndexOf("1")); // Value not an int
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
        public static void IList_Contains()
        {
            IList iList = new int[] { 1, 2, 3, 4 };
            for (int i = 0; i < iList.Count; i++)
            {
                Assert.True(iList.Contains(iList[i]));
            }
            Assert.False(iList.Contains(999)); // No such value
            Assert.False(iList.Contains(null));
            Assert.False(iList.Contains("1")); // Value not an int
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
            VerifyArray(array, 1, new int[] { 3 }, new int[] { 0 }, new int[] { 2 }, true);

            array = new int[,] { { 1, 2, 3 }, { 4, 5, 6 } };
            VerifyArray(array, 2, new int[] { 2, 3 }, new int[] { 0, 0 }, new int[] { 1, 2 }, true);

            array = new int[2, 3, 4];
            VerifyArray(array, 3, new int[] { 2, 3, 4 }, new int[] { 0, 0, 0 }, new int[] { 1, 2, 3 }, true);

            array = new int[2, 3, 4, 5];
            VerifyArray(array, 4, new int[] { 2, 3, 4, 5 }, new int[] { 0, 0, 0, 0 }, new int[] { 1, 2, 3, 4 }, true);
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

        public static IEnumerable<object[]> BinarySearch_NonGeneric_TestData()
        {
            string[] stringArray = new string[] { null, "aa", "bb", "bb", "cc", "dd", "ee" };

            yield return new object[] { stringArray, 0, 7, "bb", null, 3 };
            yield return new object[] { stringArray, 0, 7, "ee", null, 6 };
            yield return new object[] { stringArray, 0, 7, null, null, 0 };
            
            yield return new object[] { stringArray, 3, 4, "bb", null, 3 };
            yield return new object[] { stringArray, 4, 3, "bb", null, -5 };
            yield return new object[] { stringArray, 4, 0, "bb", null, -5 };

            yield return new object[] { stringArray, 0, 7, "bb", new StringComparer(), 3 };
            yield return new object[] { stringArray, 0, 7, "ee", new StringComparer(), 6 };
            yield return new object[] { stringArray, 0, 7, null, new StringComparer(), 0 };
            yield return new object[] { stringArray, 0, 7, "no-such-object", new StringComparer(), -8 };
        }

        public static IEnumerable<object[]> BinarySearch_Generic_TestData()
        {
            int[] intArray = new int[] { 1, 3, 6, 6, 8, 10, 12, 16 };

            yield return new object[] { intArray, 0, 8, 8, null, 4 };
            yield return new object[] { intArray, 0, 8, 6, null, 3 };
            yield return new object[] { intArray, 0, 8, 12, null, 6 };

            yield return new object[] { intArray, 0, 8, 0, null, -1 };
            yield return new object[] { intArray, 0, 8, 99, null, ~intArray.Length };

            yield return new object[] { intArray, 1, 5, 16, null, -7 };

            yield return new object[] { intArray, 0, 8, 8, new IntegerComparer(), 4 };
            yield return new object[] { intArray, 0, 8, 6, new IntegerComparer(), 3 };
            yield return new object[] { intArray, 0, 8, 0, new IntegerComparer(), -1 };
        }

        [Theory]
        [MemberData(nameof(BinarySearch_Generic_TestData))]
        [MemberData(nameof(BinarySearch_NonGeneric_TestData))]
        public static void BinarySearch_NonGeneric(Array array, int index, int length, object value, IComparer comparer, int expected)
        {
            bool isDefaultComparer = comparer == null || comparer == Comparer.Default;
            if (index == 0 && length == array.Length)
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
        [MemberData(nameof(BinarySearch_Generic_TestData))]
        public static void BinarySearch_Generic(int[] array, int index, int length, int value, IComparer<int> comparer, int expected)
        {
            bool isDefaultComparer = comparer == null || comparer == Comparer<int>.Default;
            if (index == 0 && length == array.Length)
            {
                if (isDefaultComparer)
                {
                    // Use BinarySearch<T>(T[], T)
                    Assert.Equal(expected, Array.BinarySearch(array, value));
                    Assert.Equal(expected, Array.BinarySearch(array, value, Comparer<int>.Default));
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
        public static void BinarySearch_Invalid()
        {
            var objectArray = new object[] { new object(), new object() };

            // Array is null
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch((int[])null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch(null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch((int[])null, "", null));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch(null, "", null));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch((int[])null, 0, 0, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch(null, 0, 0, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch((int[])null, 0, 0, "", null));
            Assert.Throws<ArgumentNullException>("array", () => Array.BinarySearch(null, 0, 0, "", null));

            // Array is multidimensional
            Assert.Throws<RankException>(() => Array.BinarySearch(new string[0, 0], ""));
            Assert.Throws<RankException>(() => Array.BinarySearch(new string[0, 0], "", null));
            Assert.Throws<RankException>(() => Array.BinarySearch(new string[0, 0], 0, 0, ""));
            Assert.Throws<RankException>(() => Array.BinarySearch(new string[0, 0], 0, 0, "", null));

            // Incompatible value
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(new int[1], ""));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(new int[1], "", null));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(new int[1], 0, 1, ""));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(new int[1], 0, 1, "", null));

            // Not IComparable
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch((Array)objectArray, new object()));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(objectArray, new object()));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch((Array)objectArray, new object(), null));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(objectArray, new object()));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch((Array)objectArray, 0, 1, new object()));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch(objectArray, new object()));
            Assert.Throws<InvalidOperationException>(() => Array.BinarySearch((Array)objectArray, 0, 1, new object(), null));

            // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.BinarySearch(new int[3], -1, 0, ""));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.BinarySearch(new string[3], -1, 0, ""));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.BinarySearch(new int[3], -1, 0, "", null));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.BinarySearch(new string[3], -1, 0, "", null));

            // Length < 0
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.BinarySearch(new int[3], 0, -1, ""));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.BinarySearch(new string[3], 0, -1, ""));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.BinarySearch(new int[3], 0, -1, "", null));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.BinarySearch(new string[3], 0, -1, "", null));

            // Length > array.Length
            Assert.Throws<ArgumentException>(() => Array.BinarySearch(new int[3], 0, 4, ""));
            Assert.Throws<ArgumentException>(() => Array.BinarySearch(new string[3], 0, 4, ""));
            Assert.Throws<ArgumentException>(() => Array.BinarySearch(new int[3], 0, 4, "", null));
            Assert.Throws<ArgumentException>(() => Array.BinarySearch(new string[3], 0, 4, "", null));

            // Index + length > array.Length
            Assert.Throws<ArgumentException>(() => Array.BinarySearch(new int[3], 3, 1, ""));
            Assert.Throws<ArgumentException>(() => Array.BinarySearch(new string[3], 3, 1, ""));
            Assert.Throws<ArgumentException>(() => Array.BinarySearch(new int[3], 3, 1, "", null));
            Assert.Throws<ArgumentException>(() => Array.BinarySearch(new string[3], 3, 1, "", null));
        }

        [Fact]
        public static void GetValue_SetValue()
        {
            var intArray = new int[3] { 7, 8, 9 };
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
        }

        [Fact]
        public static void GetValue_Invalid()
        {
            Assert.Throws<IndexOutOfRangeException>(() => new int[10].GetValue(-1)); // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => new int[10].GetValue(10)); // Index >= array.Length
            Assert.Throws<ArgumentException>(null, () => new int[10, 10].GetValue(0)); // Array is multidimensional

            Assert.Throws<ArgumentNullException>("indices", () => new int[10].GetValue(null)); // Indices is null
            Assert.Throws<ArgumentException>(null, () => new int[10, 10].GetValue(new int[] { 1, 2, 3 })); // Indices.Length > array.Rank

            Assert.Throws<IndexOutOfRangeException>(() => new int[8, 10].GetValue(new int[] { -1, 2 })); // Indices[0] < 0
            Assert.Throws<IndexOutOfRangeException>(() => new int[8, 10].GetValue(new int[] { 9, 2 })); // Indices[0] > array.GetLength(0)

            Assert.Throws<IndexOutOfRangeException>(() => new int[10, 8].GetValue(new int[] { 1, -1 })); // Indices[1] < 0
            Assert.Throws<IndexOutOfRangeException>(() => new int[10, 8].GetValue(new int[] { 1, 9 })); // Indices[1] > array.GetLength(1)
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
                Array testArray = (Array)array.Clone();
                ((IList)testArray).Clear();
                Assert.Equal(expected, testArray);
            }
            Array.Clear(array, index, length);
            Assert.Equal(expected, array);
        }

        [Fact]
        public static void Clear_Struct_WithReferenceAndValueTypeFields_Array()
        {
            var array = new G[]
            {
            new G { x = 1, s = "Hello", z = 2 },
            new G { x = 2, s = "Hello", z = 3 },
            new G { x = 3, s = "Hello", z = 4 },
            new G { x = 4, s = "Hello", z = 5 },
            new G { x = 5, s = "Hello", z = 6 }
            };

            Array.Clear(array, 0, 5);
            for (int i = 0; i < array.Length; i++)
            {
                Assert.Equal(0, array[i].x);
                Assert.Null(array[i].s);
                Assert.Equal(0, array[i].z);
            }

            array = new G[]
            {
            new G { x = 1, s = "Hello", z = 2 },
            new G { x = 2, s = "Hello", z = 3 },
            new G { x = 3, s = "Hello", z = 4 },
            new G { x = 4, s = "Hello", z = 5 },
            new G { x = 5, s = "Hello", z = 6 }
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

        public static IEnumerable<object[]> ConstrainedCopy_TestData()
        {
            yield return new object[] { new string[] { "Red", "Green", null, "Blue" }, 0, new string[] { "X", "X", "X", "X" }, 0, 4, new string[] { "Red", "Green", null, "Blue" } };

            string[] stringArray = new string[] { "Red", "Green", null, "Blue" };
            yield return new object[] { stringArray, 1, stringArray, 2, 2, new string[] { "Red", "Green", "Green", null } };

            yield return new object[] { new int[] { 0x12345678, 0x22334455, 0x778899aa }, 0, new int[3], 0, 3, new int[] { 0x12345678, 0x22334455, 0x778899aa } };

            int[] intArray1 = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
            yield return new object[] { intArray1, 3, intArray1, 2, 2, new int[] { 0x12345678, 0x22334455, 0x55443322, 0x33445566, 0x33445566 } };

            int[] intArray2 = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
            yield return new object[] { intArray2, 2, intArray2, 3, 2, new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x778899aa, 0x55443322 } };
        }

        [Theory]
        [MemberData(nameof(ConstrainedCopy_TestData))]
        public static void ConstrainedCopy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, Array expected)
        {
            Array.ConstrainedCopy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
            Assert.Equal(expected, destinationArray);
        }

        [Fact]
        public static void ConstrainedCopy_Struct_WithReferenceAndValueTypeFields_Array()
        {
            var src = new G[]
            {
            new G { x = 1, s = "Hello1", z = 2 },
            new G { x = 2, s = "Hello2", z = 3 },
            new G { x = 3, s = "Hello3", z = 4 },
            new G { x = 4, s = "Hello4", z = 5 },
            new G { x = 5, s = "Hello5", z = 6 }
            };

            var dst = new G[5];
            Array.ConstrainedCopy(src, 0, dst, 0, 5);
            for (int i = 0; i < dst.Length; i++)
            {
                Assert.Equal(src[i].x, dst[i].x);
                Assert.Equal(src[i].s, dst[i].s);
                Assert.Equal(src[i].z, dst[i].z);
            }

            // With overlap
            Array.ConstrainedCopy(src, 1, src, 2, 3);
            Assert.Equal(1, src[0].x);
            Assert.Equal("Hello1", src[0].s);
            Assert.Equal(2, src[0].z);

            Assert.Equal(2, src[1].x);
            Assert.Equal("Hello2", src[1].s);
            Assert.Equal(3, src[1].z);

            Assert.Equal(2, src[2].x);
            Assert.Equal("Hello2", src[2].s);
            Assert.Equal(3, src[2].z);

            Assert.Equal(3, src[3].x);
            Assert.Equal("Hello3", src[3].s);
            Assert.Equal(4, src[3].z);

            Assert.Equal(4, src[4].x);
            Assert.Equal("Hello4", src[4].s);
            Assert.Equal(5, src[4].z);
        }

        [Fact]
        public static void ConstrainedCopy_Invalid()
        {
            Assert.Throws<ArgumentNullException>("source", () => Array.ConstrainedCopy(null, 0, new string[10], 0, 0)); // Source array is null
            Assert.Throws<ArgumentNullException>("dest", () => Array.ConstrainedCopy(new string[10], 0, null, 0, 0)); // Destination array is null

            Assert.Throws<RankException>(() => Array.ConstrainedCopy(new string[10, 10], 0, new string[10], 0, 0)); // Source and destination arrays have different ranks
            Assert.Throws<ArrayTypeMismatchException>(() => Array.ConstrainedCopy(new string[10], 0, new int[10], 0, 0)); // Source and destination arrays hold different types

            Assert.Throws<ArgumentOutOfRangeException>("srcIndex", () => Array.ConstrainedCopy(new string[10], -1, new string[10], 0, 0)); // Start index < 0
            Assert.Throws<ArgumentException>("", () => Array.ConstrainedCopy(new string[10], 11, new string[10], 0, 0)); // Start index + length > sourceArray.Length
            Assert.Throws<ArgumentException>("", () => Array.ConstrainedCopy(new string[10], 10, new string[10], 0, 1)); // Start index + length> sourceArray.Length

            Assert.Throws<ArgumentOutOfRangeException>("dstIndex", () => Array.ConstrainedCopy(new string[10], 0, new string[10], -1, 0)); // Destination index < 0

            Assert.Throws<ArgumentException>("", () => Array.ConstrainedCopy(new string[10], 0, new string[8], 9, 0)); // Destination index > destinationArray.Length
            Assert.Throws<ArgumentException>("", () => Array.ConstrainedCopy(new string[10], 0, new string[8], 8, 1));

            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.ConstrainedCopy(new string[10], 0, new string[10], 0, -1)); // Length < 0
        }

        public static IEnumerable<object[]> Copy_TestData()
        {
            yield return new object[] { new int[] { 0x12345678, 0x22334455, 0x778899aa }, 0, new int[3], 0, 3, new int[] { 0x12345678, 0x22334455, 0x778899aa } };

            int[] intArray1 = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
            yield return new object[] { intArray1, 3, intArray1, 2, 2, new int[] { 0x12345678, 0x22334455, 0x55443322, 0x33445566, 0x33445566 } };

            int[] intArray2 = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
            yield return new object[] { intArray2, 2, intArray2, 3, 2, new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x778899aa, 0x55443322 } };

            yield return new object[] { new string[] { "Red", "Green", null, "Blue" }, 0, new string[] { "X", "X", "X", "X" }, 0, 4, new string[] { "Red", "Green", null, "Blue" } };

            string[] stringArray = new string[] { "Red", "Green", null, "Blue" };
            yield return new object[] { stringArray, 1, stringArray, 2, 2, new string[] { "Red", "Green", "Green", null } };

            // Value type array to reference type array
            yield return new object[] { new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, new object[10], 5, 3, new object[] { null, null, null, null, null, 2, 3, 4, null, null } };
            yield return new object[] { new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, new IEquatable<int>[10], 5, 3, new IEquatable<int>[] { null, null, null, null, null, 2, 3, 4, null, null } };
            yield return new object[] { new int?[] { 0, 1, 2, default(int?), 4, 5, 6, 7, 8, 9 }, 2, new object[10], 5, 3, new object[] { null, null, null, null, null, 2, null, 4, null, null } };

            // Reference type array to value type array
            yield return new object[] { new object[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc }, 5, 3, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 2, 3, 4, 0xcc, 0xcc } };
            yield return new object[] { new IEquatable<int>[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc }, 5, 3, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 2, 3, 4, 0xcc, 0xcc } };
            yield return new object[] { new IEquatable<int>[] { 0, new NotInt32(), 2, 3, 4, new NotInt32(), 6, 7, 8, 9 }, 2, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc }, 5, 3, new int[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 2, 3, 4, 0xcc, 0xcc } };

            yield return new object[] { new object[] { 0, 1, 2, 3, null, 5, 6, 7, 8, 9 }, 2, new int?[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc }, 5, 3, new int?[] { 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 2, 3, null, 0xcc, 0xcc } };
        }

        [Theory]
        [MemberData(nameof(Copy_TestData))]
        public static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, Array expected)
        {
            if (sourceIndex == 0 && destinationIndex == 0)
            {
                // Use Copy(Array, Array, int)
                Array testArray = (Array)sourceArray.Clone();
                Array.Copy(sourceArray, destinationArray, length);
                Assert.Equal(expected, destinationArray);
            }
            // Use Copy(Array, int, Array, int, int)
            Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
            Assert.Equal(expected, destinationArray);
        }

        [Fact]
        public static void Copy_ValueTypeArray_ToObjectArray()
        {
            var src = new G[]
            {
            new G { x = 1, s = "Hello1", z = 2 },
            new G { x = 2, s = "Hello2", z = 3 },
            new G { x = 3, s = "Hello3", z = 4 },
            new G { x = 4, s = "Hello4", z = 5 },
            new G { x = 5, s = "Hello5", z = 6 }
            };

            var dst = new object[5];
            Array.Copy(src, 0, dst, 0, 5);
            for (int i = 0; i < dst.Length; i++)
            {
                Assert.True(dst[i] is G);
                G g = (G)dst[i];
                Assert.Equal(src[i].x, g.x);
                Assert.Equal(src[i].s, g.s);
                Assert.Equal(src[i].z, g.z);
            }
        }

        [Fact]
        public static void Copy_Struct_WithReferenceAndValueTypeFields_Array()
        {
            var src = new G[]
            {
            new G { x = 1, s = "Hello1", z = 2 },
            new G { x = 2, s = "Hello2", z = 3 },
            new G { x = 3, s = "Hello3", z = 4 },
            new G { x = 4, s = "Hello4", z = 5 },
            new G { x = 5, s = "Hello5", z = 6 }
            };

            var dst = new G[5];
            Array.Copy(src, 0, dst, 0, 5);
            for (int i = 0; i < dst.Length; i++)
            {
                Assert.Equal(src[i].x, dst[i].x);
                Assert.Equal(src[i].s, dst[i].s);
                Assert.Equal(src[i].z, dst[i].z);
            }

            // With overlap
            Array.Copy(src, 1, src, 2, 3);
            Assert.Equal(1, src[0].x);
            Assert.Equal("Hello1", src[0].s);
            Assert.Equal(2, src[0].z);

            Assert.Equal(2, src[1].x);
            Assert.Equal("Hello2", src[1].s);
            Assert.Equal(3, src[1].z);

            Assert.Equal(2, src[2].x);
            Assert.Equal("Hello2", src[2].s);
            Assert.Equal(3, src[2].z);

            Assert.Equal(3, src[3].x);
            Assert.Equal("Hello3", src[3].s);
            Assert.Equal(4, src[3].z);

            Assert.Equal(4, src[4].x);
            Assert.Equal("Hello4", src[4].s);
            Assert.Equal(5, src[4].z);
        }

        [Fact]
        public static void Copy_Invalid()
        {
            Assert.Throws<ArrayTypeMismatchException>(() => Array.Copy(new int[10], 0, new IEnumerable<int>[10], 0, 10)); // Different array types
            Assert.Throws<ArrayTypeMismatchException>(() => Array.Copy(new string[10], 0, new int[10], 0, 10)); // Different array types

            Assert.Throws<InvalidCastException>(() =>
            {
                IEquatable<int>[] sourceArray = new IEquatable<int>[10];
                sourceArray[4] = new NotInt32();
                int[] destinationArray = new int[10];
            // Legacy: Note that the cast checks are done during copying, so some elements in the destination
            // array may already have been overwritten.
            Array.Copy(sourceArray, 2, destinationArray, 5, 3);
            });

            Assert.Throws<InvalidCastException>(() =>
            {
                IEquatable<int>[] sourceArray = new IEquatable<int>[10];
                sourceArray[4] = null;
                int[] destinationArray = new int[10];
            // Legacy: Note that the cast checks are done during copying, so some elements in the destination
            // array may already have been overwritten.
            Array.Copy(sourceArray, 2, destinationArray, 5, 3);
            });
        }

        [Fact]
        public static void Copy_Array_Array_Int_Invalid()
        {
            Assert.Throws<ArgumentNullException>("sourceArray", () => Array.Copy(null, new string[10], 0)); // Source array is null
            Assert.Throws<ArgumentNullException>("destinationArray", () => Array.Copy(new string[10], null, 0)); // Destination array is null

            Assert.Throws<RankException>(() => Array.Copy(new string[10, 10], new string[10], 0)); // Source and destination arrays have different ranks
            Assert.Throws<ArrayTypeMismatchException>(() => Array.Copy(new string[10], new int[10], 0)); // Source and destination arrays hold different types
            Assert.Throws<InvalidCastException>(() => Array.Copy(new object[] { "1" }, new int[1], 1)); // Source and destination arrays hold uncovertible types

            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Copy(new string[10], new string[10], -1)); // Length < 0
            Assert.Throws<ArgumentException>("", () => Array.Copy(new string[8], new string[10], 9)); // Length > sourceArray.Length
            Assert.Throws<ArgumentException>("", () => Array.Copy(new string[10], new string[8], 9)); // Length > destinationArray.Length
        }

        [Fact]
        public static void Copy_Array_Int_Array_Int_Int_Invalid()
        {
            Assert.Throws<ArgumentNullException>("source", () => Array.Copy(null, 0, new string[10], 0, 0)); // Source array is null
            Assert.Throws<ArgumentNullException>("dest", () => Array.Copy(new string[10], 0, null, 0, 0)); // Destination array is null            

            Assert.Throws<RankException>(() => Array.Copy(new string[10, 10], 0, new string[10], 0, 0)); // Source and destination arrays have different ranks
            Assert.Throws<ArrayTypeMismatchException>(() => Array.Copy(new string[10], 0, new int[10], 0, 0)); // Source and destination arrays hold different types
            Assert.Throws<InvalidCastException>(() => Array.Copy(new object[] { "1" }, 0, new int[1], 0, 1)); // Source and destination arrays hold uncovertible types

            Assert.Throws<ArgumentOutOfRangeException>("srcIndex", () => Array.Copy(new string[10], -1, new string[10], 0, 0)); // Start index < 0
            Assert.Throws<ArgumentException>("", () => Array.Copy(new string[8], 9, new string[10], 0, 0)); // Start index + length > sourceArray.Length
            Assert.Throws<ArgumentException>("", () => Array.Copy(new string[8], 8, new string[10], 0, 1)); // Start index + length> sourceArray.Length

            Assert.Throws<ArgumentOutOfRangeException>("dstIndex", () => Array.Copy(new string[10], 0, new string[10], -1, 0)); // Destination index < 0
            Assert.Throws<ArgumentException>("", () => Array.Copy(new string[10], 0, new string[8], 9, 0)); // Destination index > destinationArray.Length
            Assert.Throws<ArgumentException>("", () => Array.Copy(new string[10], 0, new string[8], 8, 1)); // Destination index > destinationArray.Length

            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Copy(new string[10], 0, new string[10], 0, -1)); // Length < 0
        }

        public static IEnumerable<object[]> CopyTo_TestData()
        {
            yield return new object[] { new B1[10], new D1[10], 0, new D1[10] };
            yield return new object[] { new D1[10], new B1[10], 0, new B1[10] };
            yield return new object[] { new B1[10], new I1[10], 0, new I1[10] };
            yield return new object[] { new I1[10], new B1[10], 0, new B1[10] };

            yield return new object[] { new int[] { 0, 1, 2, 3 }, new int[4], 0, new int[] { 0, 1, 2, 3 } };
            yield return new object[] { new int[] { 0, 1, 2, 3 }, new int[7], 2, new int[] { 0, 0, 0, 1, 2, 3, 0 } };
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public static void CopyTo(Array source, Array destination, int index, Array expected)
        {
            source.CopyTo(destination, index);
            Assert.Equal(expected, destination);
        }

        [Fact]
        public static void CopyTo_Invalid()
        {
            Assert.Throws<RankException>(() => new int[3, 3].CopyTo(new int[3], 0)); // Source is multidimensional

            Assert.Throws<ArgumentNullException>("dest", () => new int[3].CopyTo(null, 0)); // Destination is null
            Assert.Throws<ArgumentException>(null, () => new int[3].CopyTo(new int[10, 10], 0)); // Destination array is multidimensional

            Assert.Throws<ArrayTypeMismatchException>(() => new int[3].CopyTo(new string[10], 0)); // Source and destination types are incompatible
            Assert.Throws<ArrayTypeMismatchException>(() => new B1[10].CopyTo(new B2[10], 0));// Source and destination types hold uncovertible types

            Assert.Throws<InvalidCastException>(() => new object[] { "1" }.CopyTo(new int[1], 0)); // Source and destination types hold uncovertible types
            Assert.Throws<InvalidCastException>(() => new B1[] { new B1() }.CopyTo(new I1[1], 0));// Source and destination types hold uncovertible types

            Assert.Throws<ArgumentOutOfRangeException>("dstIndex", () => new int[3].CopyTo(new int[10], -1)); // Index < 0
            Assert.Throws<ArgumentException>("", () => new int[3].CopyTo(new int[10], 10)); // Index > destination.Length
        }

        [Fact]
        public static void CreateInstance_Type_Int()
        {
            string[] stringArray = (string[])Array.CreateInstance(typeof(string), 10);
            Assert.Equal(stringArray, new string[10]);

            stringArray = (string[])Array.CreateInstance(typeof(string), 0);
            Assert.Equal(stringArray, new string[0]);

            int[] intArray = (int[])Array.CreateInstance(typeof(int), 10);
            Assert.Equal(intArray, new int[10]);

            intArray = (int[])Array.CreateInstance(typeof(int), 0);
            Assert.Equal(intArray, new int[0]);
        }

        [Fact]
        public static void CreateInstance_Type_Int_Invalid()
        {
            Assert.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, 0)); // Element type is null

            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(void), 0)); // Element type is not supported (void)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(List<>), 0)); // Element type is not supported (generic)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(int).MakeByRefType(), 0)); // Element type is not supported (ref)

            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.CreateInstance(typeof(int), -1)); // Length < 0

            // Type is not a valid RuntimeType
            Assert.Throws<ArgumentException>("elementType", () => Array.CreateInstance(Helpers.NonRuntimeType(), 0));
        }

        [Fact]
        public static void CreateInstance_Type_IntArray()
        {
            string[] stringArray = (string[])Array.CreateInstance(typeof(string), new int[] { 10 });
            Assert.Equal(stringArray, new string[10]);

            stringArray = (string[])Array.CreateInstance(typeof(string), new int[] { 0 });
            Assert.Equal(stringArray, new string[0]);

            int[] intArray1 = (int[])Array.CreateInstance(typeof(int), new int[] { 1 });
            VerifyArray(intArray1, 1, new int[] { 1 }, new int[] { 0 }, new int[] { 0 }, false);
            Assert.Equal(intArray1, new int[1]);

            int[,] intArray2 = (int[,])Array.CreateInstance(typeof(int), new int[] { 1, 2 });
            VerifyArray(intArray2, 2, new int[] { 1, 2 }, new int[] { 0, 0 }, new int[] { 0, 1 }, false);
            intArray2[0, 1] = 42;
            Assert.Equal(42, intArray2[0, 1]);

            int[,,] intArray3 = (int[,,])Array.CreateInstance(typeof(int), new int[] { 1, 2, 3 });
            VerifyArray(intArray3, 3, new int[] { 1, 2, 3 }, new int[] { 0, 0, 0 }, new int[] { 0, 1, 2 }, false);
            intArray3[0, 1, 2] = 42;
            Assert.Equal(42, intArray3[0, 1, 2]);

            int[,,,] intArray4 = (int[,,,])Array.CreateInstance(typeof(int), new int[] { 1, 2, 3, 4 });
            VerifyArray(intArray4, 4, new int[] { 1, 2, 3, 4 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 1, 2 }, false);
            intArray4[0, 1, 2, 3] = 42;
            Assert.Equal(42, intArray4[0, 1, 2, 3]);
        }

        [Fact]
        public static void CreateInstance_Type_IntArray_Invalid()
        {
            Assert.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, new int[] { 10 })); // Element type is null

            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(void), new int[] { 1 })); // Element type is not supported (void)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(List<>), new int[] { 1 })); // Element type is not supported (generic)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(int).MakeByRefType(), new int[] { 1 })); // Element type is not supported (ref)

            Assert.Throws<ArgumentNullException>("lengths", () => Array.CreateInstance(typeof(int), null)); // Lengths is null
            Assert.Throws<ArgumentException>(null, () => Array.CreateInstance(typeof(int), new int[0])); // Lengths is empty
            Assert.Throws<ArgumentOutOfRangeException>("lengths[0]", () => Array.CreateInstance(typeof(int), new int[] { -1 })); // Lengths contains negative integers

            // Type is not a valid RuntimeType
            Assert.Throws<ArgumentException>("elementType", () => Array.CreateInstance(Helpers.NonRuntimeType(), new int[] { 1 }));
        }

        [Fact]
        public static void CreateInstance_Type_IntArray_IntArray()
        {
            int[] intArray1 = (int[])Array.CreateInstance(typeof(int), new int[] { 5 }, new int[] { 0 });
            Assert.Equal(intArray1, new int[5]);
            VerifyArray(intArray1, 1, new int[] { 5 }, new int[] { 0 }, new int[] { 4 }, false);

            int[,,] intArray2 = (int[,,])Array.CreateInstance(typeof(int), new int[] { 7, 8, 9 }, new int[] { 1, 2, 3 });
            Assert.Equal(intArray2, new int[7, 8, 9]);
            VerifyArray(intArray2, 3, new int[] { 7, 8, 9 }, new int[] { 1, 2, 3 }, new int[] { 7, 9, 11 }, false);
        }

        [Fact]
        public static void CreateInstance_Type_IntArray_IntArray_Invalid()
        {
            Assert.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, new int[] { 1 }, new int[] { 1 })); // Element type is null

            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(void), new int[] { 1 }, new int[] { 1 })); // Element type is not supported (void)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(List<>), new int[] { 1 }, new int[] { 1 })); // Element type is not supported (generic)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(int).MakeByRefType(), new int[] { 1 }, new int[] { 1 })); // Element type is not supported (ref)

            Assert.Throws<ArgumentNullException>("lengths", () => Array.CreateInstance(typeof(int), null, new int[] { 1 })); // Lengths is null
            Assert.Throws<ArgumentException>(null, () => Array.CreateInstance(typeof(int), new int[0], new int[0])); // Lengths is empty
            Assert.Throws<ArgumentOutOfRangeException>("lengths[0]", () => Array.CreateInstance(typeof(int), new int[] { -1 }, new int[] { 1 })); // Lengths contains negative integers

            Assert.Throws<ArgumentNullException>("lowerBounds", () => Array.CreateInstance(typeof(int), new int[] { 1 }, null)); // Lower bounds is null

            Assert.Throws<ArgumentException>(null, () => Array.CreateInstance(typeof(int), new int[] { 1 }, new int[] { 1, 2 })); // Lengths and lower bounds have different lengths

            // Type is not a valid RuntimeType
            Assert.Throws<ArgumentException>("elementType", () => Array.CreateInstance(Helpers.NonRuntimeType(), new int[] { 1 }, new int[] { 0 }));
        }

        [Fact]
        public static void CreateInstance_Type_IntArray_IntArray_Invalid_UpperBoundTooLarge()
        {
            Assert.Throws<ArgumentOutOfRangeException>(null, () => Array.CreateInstance(typeof(int), new int[] { int.MaxValue }, new int[] { 2 })); // upper bound would exceed int.MaxValue
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

        public static IEnumerable<object[]> IndexOf_NonGeneric_TestData()
        {
            var stringArray = new string[] { null, null, "Hello", "Hello", "Goodbye", "Goodbye", null, null };
            yield return new object[] { stringArray, null, 0, 8, 0 };
            yield return new object[] { stringArray, "Hello", 0, 8, 2 };
            yield return new object[] { stringArray, "Goodbye", 0, 8, 4 };
            yield return new object[] { stringArray, "Nowhere", 0, 8, -1 };
            yield return new object[] { stringArray, "Hello", 3, 5, 3 };
            yield return new object[] { stringArray, "Hello", 4, 4, -1 };
            yield return new object[] { stringArray, "Goodbye", 2, 3, 4 };
            yield return new object[] { stringArray, "Goodbye", 2, 2, -1 };

            var stringArrayNoNulls = new string[] { "Hello", "Hello", "Goodbye", "Goodbye" };
            yield return new object[] { stringArrayNoNulls, null, 0, 4, -1 };

            var enumArray = new TestEnum[] { TestEnum.Case1, TestEnum.Case2, TestEnum.Case1 };
            yield return new object[] { enumArray, TestEnum.Case1, 0, 3, 0 };
            yield return new object[] { enumArray, TestEnum.Case3, 0, 3, -1 };

            var nullableArray = new int?[] { 0, null, 10 };
            yield return new object[] { nullableArray, null, 0, 3, 1 };
            yield return new object[] { nullableArray, 10, 0, 3, 2 };
            yield return new object[] { nullableArray, 100, 0, 3, -1 };
        }

        public static IEnumerable<object[]> IndexOf_Generic_TestData()
        {
            var intArray = new int[] { 7, 7, 8, 8, 9, 9 };
            yield return new object[] { intArray, 8, 0, 6, 2 };
            yield return new object[] { intArray, 8, 3, 3, 3 };
            yield return new object[] { intArray, 8, 4, 2, -1 };
            yield return new object[] { intArray, 9, 2, 2, -1 };
            yield return new object[] { intArray, 9, 2, 3, 4 };
            yield return new object[] { intArray, 10, 0, 6, -1 };
        }

        [Theory]
        [MemberData(nameof(IndexOf_NonGeneric_TestData))]
        [MemberData(nameof(IndexOf_Generic_TestData))]
        public static void IndexOf_NonGeneric(Array array, object value, int startIndex, int count, int expected)
        {
            if (startIndex + count == array.Length)
            {
                // Use IndexOf(Array, object) or IndexOf(Array, object, int)
                if (startIndex == 0)
                {
                    // Use IndexOf(Array, object)
                    Assert.Equal(expected, Array.IndexOf(array, value));
                }
                // Use IndexOf(Array, object, int)
                Assert.Equal(expected, Array.IndexOf(array, value, startIndex));
            }
            // Use IndexOf(Array, object, int, int)
            Assert.Equal(expected, Array.IndexOf(array, value, startIndex, count));
        }

        [Theory]
        [MemberData(nameof(IndexOf_Generic_TestData))]
        public static void IndexOf_Generic(int[] array, int value, int startIndex, int count, int expected)
        {
            if (startIndex + count == array.Length)
            {
                // Use IndexOf<T>(T[], T) or IndexOf(T[], T, int)
                if (startIndex == 0)
                {
                    // Use IndexOf<T>(T[], T)
                    Assert.Equal(expected, Array.IndexOf(array, value));
                }
                // Use IndexOf<T>(T[], T, int)
                Assert.Equal(expected, Array.IndexOf(array, value, startIndex));
            }
            // Use IndexOf<T>(T[], T, int, int)
            Assert.Equal(expected, Array.IndexOf(array, value, startIndex, count));
        }

        [Fact]
        public static void IndexOf_Invalid()
        {
            var intArray = new int[] { 1, 2, 3 };
            var stringArray = new string[] { "a", "b", "c" };

            // Array is null
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf((int[])null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf((int[])null, "", 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf((int[])null, "", 0, 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf(null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf(null, "", 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.IndexOf(null, "", 0, 0));

            // Start index < 0
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.IndexOf(intArray, "", -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.IndexOf(intArray, "", -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.IndexOf(stringArray, "", -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.IndexOf(stringArray, "", -1, 0));

            // Count < 0
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.IndexOf(intArray, "", 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.IndexOf(stringArray, "", 0, -1));

            // Start index + count > array.Length
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.IndexOf(intArray, "", intArray.Length, 1));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.IndexOf(stringArray, "", stringArray.Length, 1));
        }

        public static IEnumerable<object[]> LastIndexOf_NonGeneric_TestData()
        {
            var stringArray = new string[] { null, null, "Hello", "Hello", "Goodbye", "Goodbye", null, null };
            yield return new object[] { stringArray, null, 7, 8, 7 };
            yield return new object[] { stringArray, "Hello", 7, 8, 3 };
            yield return new object[] { stringArray, "Goodbye", 7, 8, 5 };
            yield return new object[] { stringArray, "Nowhere", 7, 8, -1 };
            yield return new object[] { stringArray, "Hello", 2, 2, 2 };
            yield return new object[] { stringArray, "Hello", 3, 3, 3 };
            yield return new object[] { stringArray, "Goodbye", 7, 2, -1 };
            yield return new object[] { stringArray, "Goodbye", 7, 3, 5 };

            var stringArrayNoNulls = new string[] { "Hello", "Hello", "Goodbye", "Goodbye" };
            yield return new object[] { stringArrayNoNulls, null, 3, 4, -1 };

            var enumArray = new TestEnum[] { TestEnum.Case1, TestEnum.Case2, TestEnum.Case1 };
            yield return new object[] { enumArray, TestEnum.Case1, 2, 3, 2 };
            yield return new object[] { enumArray, TestEnum.Case3, 2, 3, -1 };

            var nullableArray = new int?[] { 0, null, 10, 10, 0 };
            yield return new object[] { nullableArray, null, 4, 5, 1 };
            yield return new object[] { nullableArray, 10, 4, 5, 3 };
            yield return new object[] { nullableArray, 100, 4, 5, -1 };

            yield return new object[] { new int[0], 0, 0, 0, -1 };
            yield return new object[] { new int[0], 0, -1, 0, -1 };
        }

        public static IEnumerable<object[]> LastIndexOf_Generic_TestData()
        {
            var intArray = new int[] { 7, 7, 8, 8, 9, 9 };
            yield return new object[] { intArray, 8, 5, 6, 3 };
            yield return new object[] { intArray, 8, 1, 1, -1 };
            yield return new object[] { intArray, 8, 3, 3, 3 };
            yield return new object[] { intArray, 7, 3, 2, -1 };
            yield return new object[] { intArray, 7, 3, 3, 1 };
            yield return new object[] { new int[0], 0, 0, 0, -1 };
            yield return new object[] { new int[0], 0, -1, 0, -1 };
        }

        [Theory]
        [MemberData(nameof(LastIndexOf_NonGeneric_TestData))]
        [MemberData(nameof(LastIndexOf_Generic_TestData))]
        public static void LastIndexOf_NonGeneric(Array array, object value, int startIndex, int count, int expected)
        {
            if (count - startIndex - 1 == 0 || array.Length == 0)
            {
                // Use LastIndexOf(Array, object) or LastIndexOf(Array, object, int)
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

        [Theory]
        [MemberData(nameof(LastIndexOf_Generic_TestData))]
        public static void LastIndexOf_Generic(int[] array, int value, int startIndex, int count, int expected)
        {
            if (count - startIndex - 1 == 0 || array.Length == 0)
            {
                // Use LastIndexOf<T>(T[], T) or LastIndexOf<T>(T[], T, int)
                if (count == array.Length)
                {
                    // Use LastIndexOf<T>(T[], T)
                    Assert.Equal(expected, Array.LastIndexOf(array, value));
                }
                // Use LastIndexOf<T>(T[], T, int)
                Assert.Equal(expected, Array.LastIndexOf(array, value, startIndex));
            }
            // Use LastIndexOf<T>(T[], T, int, int)
            Assert.Equal(expected, Array.LastIndexOf(array, value, startIndex, count));
        }

        [Fact]
        public static void LastIndexOf_Invalid()
        {
            var intArray = new int[] { 1, 2, 3 };
            var stringArray = new string[] { "a", "b", "c" };

            // Array is null
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf((int[])null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf((int[])null, "", 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf((int[])null, "", 0, 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf(null, ""));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf(null, "", 0));
            Assert.Throws<ArgumentNullException>("array", () => Array.LastIndexOf(null, "", 0, 0));

            // Array is multidimensional
            Assert.Throws<RankException>(() => Array.LastIndexOf(new int[1, 1], ""));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new int[1, 1], "", 0));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new int[1, 1], "", 0, 0));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new string[1, 1], ""));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new string[1, 1], "", 0));
            Assert.Throws<RankException>(() => Array.LastIndexOf(new string[1, 1], "", 0, 0));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(new int[0], 0, 1, 0)); // Array is empty, and start index != 0 or -1
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.LastIndexOf(new int[0], 0, 0, 1)); // Array is empty, and count != 0

            // Start index < 0
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(intArray, "", -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(intArray, "", -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(stringArray, "", -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => Array.LastIndexOf(stringArray, "", -1, 0));

            // Count < 0
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.LastIndexOf(intArray, "", 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.LastIndexOf(stringArray, "", 0, -1));

            // Count > startIndex + 1
            Assert.Throws<ArgumentOutOfRangeException>("endIndex", () => Array.LastIndexOf(intArray, "", 2, 4));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => Array.LastIndexOf(intArray, 0, 2, 4));
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

            Assert.Throws<NullReferenceException>(() => comparable.CompareTo(new int[] { 1, 2, 3 }, null)); // Comparer is null
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
        public static void IStructuralEquatable_Invalid()
        {
            IStructuralEquatable equatable = new int[] { 1, 2, 3 };
            Assert.Throws<NullReferenceException>(() => equatable.Equals(new int[] { 1, 2, 3 }, null)); // Comparer is null
            Assert.Throws<ArgumentNullException>("comparer", () => equatable.GetHashCode(null)); // Comparer is null
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
            // int[] is a primitive type
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, 0, 5, new int[] { 5, 4, 3, 2, 1 } };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, 2, 3, new int[] { 1, 2, 5, 4, 3 } };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, 0, 0, new int[] { 1, 2, 3, 4, 5 } };
            yield return new object[] { new int[] { 1, 2, 3, 4, 5 }, 5, 0, new int[] { 1, 2, 3, 4, 5 } };

            // string[] can be cast to object[]
            yield return new object[] { new string[] { "1", "2", "3", "4", "5" }, 0, 5, new string[] { "5", "4", "3", "2", "1" } };
            yield return new object[] { new string[] { "1", "2", "3", "4", "5" }, 2, 3, new string[] { "1", "2", "5", "4", "3" } };
            yield return new object[] { new string[] { "1", "2", "3", "4", "5" }, 0, 0, new string[] { "1", "2", "3", "4", "5" } };
            yield return new object[] { new string[] { "1", "2", "3", "4", "5" }, 5, 0, new string[] { "1", "2", "3", "4", "5" } };

            // TestEnum[] can be cast to int[]
            var enumArray = new TestEnum[] { TestEnum.Case1, TestEnum.Case2, TestEnum.Case3, TestEnum.Case1 };
            yield return new object[] { enumArray, 0, 4, new TestEnum[] { TestEnum.Case1, TestEnum.Case3, TestEnum.Case2, TestEnum.Case1 } };
            yield return new object[] { enumArray, 2, 2, new TestEnum[] { TestEnum.Case1, TestEnum.Case2, TestEnum.Case1, TestEnum.Case3 } };
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
        public static void Reverse(Array array, int index, int length, Array expected)
        {
            if (index == 0 && length == array.Length)
            {
                // Use Reverse(Array)
                Array testArray = (Array)array.Clone();
                Array.Reverse(testArray);
                Assert.Equal(expected, testArray);
            }
            // Use Reverse(Array, int, int)
            Array.Reverse(array, index, length);
            Assert.Equal(expected, expected);
        }

        [Fact]
        public static void Reverse_Invalid()
        {
            // Array is null
            Assert.Throws<ArgumentNullException>("array", () => Array.Reverse(null));
            Assert.Throws<ArgumentNullException>("array", () => Array.Reverse(null, 0, 0));

            // Array is multidimensional
            Assert.Throws<RankException>(() => Array.Reverse(new int[10, 10]));
            Assert.Throws<RankException>(() => Array.Reverse(new int[10, 10], 0, 0));

            // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Reverse(new int[10], -1, 10));

            // Length < 0
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Reverse(new int[10], 0, -1));

            // Index + length > array.Length
            Assert.Throws<ArgumentException>(() => Array.Reverse(new int[10], 11, 0));
            Assert.Throws<ArgumentException>(() => Array.Reverse(new int[10], 10, 1));
            Assert.Throws<ArgumentException>(() => Array.Reverse(new int[10], 9, 2));
            Assert.Throws<ArgumentException>(() => Array.Reverse(new int[10], 0, 11));
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
            var arr1 = new S[3];
            arr1[1].X = 0x22222222;
            arr1.SetValue(null, new int[] { 1 });
            Assert.Equal(0, arr1[1].X);

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
            arr5.SetValue(E1.MinusTwo, new int[] { 1 });
            Assert.Equal(-2, arr5[1]);
        }

        [Fact]
        public static void SetValue_Casting_Invalid()
        {
            // Unlike most of the other reflection apis, converting or widening a primitive to an enum is NOT allowed.
            var arr1 = new E1[3];
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

            Assert.Throws<ArgumentNullException>("indices", () => new int[10].SetValue(1, null)); // Indices is null
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

        private static void VerifyArray(Array array, int rank, int[] lengths, int[] lowerBounds, int[] upperBounds, bool checkIList)
        {
            Assert.Equal(rank, array.Rank);

            for (int i = 0; i < lengths.Length; i++)
                Assert.Equal(lengths[i], array.GetLength(i));

            for (int i = 0; i < lowerBounds.Length; i++)
                Assert.Equal(lowerBounds[i], array.GetLowerBound(i));

            for (int i = 0; i < upperBounds.Length; i++)
                Assert.Equal(upperBounds[i], array.GetUpperBound(i));


            Assert.Throws<IndexOutOfRangeException>(() => array.GetLength(-1)); // Dimension < 0
            Assert.Throws<IndexOutOfRangeException>(() => array.GetLength(array.Rank)); // Dimension >= array.Rank

            Assert.Throws<IndexOutOfRangeException>(() => array.GetLowerBound(-1)); // Dimension < 0
            Assert.Throws<IndexOutOfRangeException>(() => array.GetLowerBound(array.Rank)); // Dimension >= array.Rank

            Assert.Throws<IndexOutOfRangeException>(() => array.GetUpperBound(-1)); // Dimension < 0
            Assert.Throws<IndexOutOfRangeException>(() => array.GetUpperBound(array.Rank)); // Dimension >= array.Rank

            if (checkIList)
            {
                VerifyArrayAsIList(array);
            }
        }

        private static void VerifyArrayAsIList(Array array)
        {
            IList ils = array;
            Assert.Equal(array.Length, ils.Count);

            Assert.Equal(array, ils.SyncRoot);

            Assert.False(ils.IsSynchronized);
            Assert.True(ils.IsFixedSize);
            Assert.False(ils.IsReadOnly);

            Assert.Throws<NotSupportedException>(() => ils.Add(2));
            Assert.Throws<NotSupportedException>(() => ils.Insert(0, 2));
            Assert.Throws<NotSupportedException>(() => ils.Remove(0));
            Assert.Throws<NotSupportedException>(() => ils.RemoveAt(0));

            if (array.Rank == 1)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    object obj = ils[i];
                    Assert.Equal(array.GetValue(i), obj);
                    Assert.True(ils.Contains(obj));
                    Assert.Equal(i, ils.IndexOf(obj));
                }

                Assert.False(ils.Contains(null));
                Assert.False(ils.Contains(999));
                Assert.Equal(-1, ils.IndexOf(null));
                Assert.Equal(-1, ils.IndexOf(999));

                ils[1] = 10;
                Assert.Equal(10, ils[1]);
            }
            else
            {
                Assert.Throws<RankException>(() => ils.Contains(null));
                Assert.Throws<RankException>(() => ils.IndexOf(null));
            }
        }

        public enum TestEnum
        {
            Case1,
            Case2,
            Case3
        }

        private struct G
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

        private enum E1 : sbyte
        {
            MinusTwo = -2
        }

        private struct S
        {
            public int X;
        }

        private class NotInt32 : IEquatable<int>
        {
            public bool Equals(int other)
            {
                throw new NotImplementedException();
            }
        }

        private class B1 { }
        private class D1 : B1 { }
        private class B2 { }
        private class D2 : B2 { }
        private interface I1 { }
        private interface I2 { }
    }
}
