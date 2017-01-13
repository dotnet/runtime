// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static class Array17Tests
    {
        [Fact]
        public static void AsReadOnly()
        {
            var array = new string[] { "a", "b" };
            System.Collections.ObjectModel.ReadOnlyCollection<string> ro = Array.AsReadOnly(array);
            Assert.Equal(array, ro);
            Assert.Equal(new System.Collections.ObjectModel.ReadOnlyCollection<string>(array), ro);
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

        [Theory]
        public static void ForEach()
        {
            Array.ForEach<int>(new int[] { }, new Action<int>(i => { throw new InvalidOperationException(); }));
            // Did not throw; no items

            int counter = 0;
            int exp = 0;
            Array.ForEach<int>(new int[] { 1, 2, 3 }, new Action<int>(i => { counter += i; exp = (i==1)?1:(i==2)?3:6; Assert.Equal(exp, counter); }));
        }

        [Fact]
        public static void ForEach_Invalid()
        {
            Assert.Throws<ArgumentNullException>("array", () => { Array.ForEach<short>(null, new Action<short>(i => i++)); });  // Array is null
            Assert.Throws<ArgumentNullException>("action", () => { Array.ForEach<string>(new string[] { }, null); }); // Action is null
            Assert.Throws<InvalidOperationException>(() => {
                Array.ForEach<string>(new string[] { "a" }, i => { throw new InvalidOperationException(); }); // Action throws
            });
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
        public static void Copy_Long(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, Array expected)
        {
            if (sourceIndex == 0 && destinationIndex == 0)
            {
                // Use Copy(Array, Array, long)
                Array testArray = (Array)sourceArray.Clone();
                Array.Copy(sourceArray, destinationArray, (long)length);
                Assert.Equal(expected, destinationArray);
            }
            // Use Copy(Array, long, Array, long, long)
            Array.Copy(sourceArray, (long)sourceIndex, destinationArray, (long)destinationIndex, (long)length);
            Assert.Equal(expected, destinationArray);
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
        public static void CopyTo_Long(Array source, Array destination, long index, Array expected)
        {
            source.CopyTo(destination, index);
            Assert.Equal(expected, destination);
        }

        [Fact]
        public static void CreateInstance_Type_Int_Int()
        {
            int[,] intArray2 = (int[,])Array.CreateInstance(typeof(int), 1, 2);
            VerifyArray(intArray2, 2, new int[] { 1, 2 }, new int[] { 0, 0 }, new int[] { 0, 1 }, false);
            intArray2[0, 1] = 42;
            Assert.Equal(42, intArray2[0, 1]);
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
        public static void CreateInstance_Type_Int_Int_Invalid()
        {
            Assert.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, 0, 1)); // Element type is null

            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(void), 0, 1)); // Element type is not supported (void)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(List<>), 0, 1)); // Element type is not supported (generic)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(int).MakeByRefType(), 0, 1)); // Element type is not supported (ref)

            Assert.Throws<ArgumentOutOfRangeException>("length2", () => Array.CreateInstance(typeof(int), 0, -1)); // Length < 0

            // Type is not a valid RuntimeType
            Assert.Throws<ArgumentException>("elementType", () => Array.CreateInstance(Helpers.NonRuntimeType(), 0, 1));
        }

        [Fact]
        public static void CreateInstance_Type_Int_Int_Int()
        {
            int[,,] intArray3 = (int[,,])Array.CreateInstance(typeof(int), 1, 2, 3);
            VerifyArray(intArray3, 3, new int[] { 1, 2, 3 }, new int[] { 0, 0, 0 }, new int[] { 0, 1, 2 }, false);
            intArray3[0, 1, 2] = 42;
            Assert.Equal(42, intArray3[0, 1, 2]);
        }

        [Fact]
        public static void CreateInstance_Type_Int_Int_Int_Invalid()
        {
            Assert.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, 0, 1, 2)); // Element type is null

            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(void), 0, 1, 2)); // Element type is not supported (void)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(List<>), 0, 1, 2)); // Element type is not supported (generic)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(int).MakeByRefType(), 0, 1, 2)); // Element type is not supported (ref)

            Assert.Throws<ArgumentOutOfRangeException>("length3", () => Array.CreateInstance(typeof(int), 0, 1, -1)); // Length < 0

            // Type is not a valid RuntimeType
            Assert.Throws<ArgumentException>("elementType", () => Array.CreateInstance(Helpers.NonRuntimeType(), 0, 1, 2));
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
        public static void CreateInstance_Type_IntArray_IntArray()
        {
            int[] intArray1 = (int[])Array.CreateInstance(typeof(int), new int[] { 5 }, new int[] { 0 });
            Assert.Equal(intArray1, new int[5]);
            VerifyArray(intArray1, 1, new int[] { 5 }, new int[] { 0 }, new int[] { 4 }, false);

            int[,,] intArray2 = (int[,,])Array.CreateInstance(typeof(int), new int[] { 7, 8, 9 }, new int[] { 1, 2, 3 });
            Assert.Equal(intArray2, new int[7, 8, 9]);
            VerifyArray(intArray2, 3, new int[] { 7, 8, 9 }, new int[] { 1, 2, 3 }, new int[] { 7, 9, 11 }, false);
        }

        [Theory]
        public static void ConvertAll()
        {
            var result = Array.ConvertAll<int, int>(new int[] { }, new Converter<int, int>(i => { throw new InvalidOperationException(); }));
            // Does not throw - no entries
            Assert.Equal(new int[] { }, result);

            var result2 = Array.ConvertAll<int, string>(new int[] { 1 }, new Converter<int, string>(i => i++.ToString()));
            Assert.Equal(new string[] { "2" }, result2);

            result2 = Array.ConvertAll<int, string>(new int[] { 1, 2 }, new Converter<int, string>(i => i++.ToString()));
            Assert.Equal(new string[] { "2", "3" }, result2);

            result2 = Array.ConvertAll<int, string>(new int[] { 1 }, new Converter<int, string>(i => null));
            Assert.Equal(new string[] { null }, result2);
        }

        [Fact]
        public static void ConvertAll_Invalid()
        {
            Assert.Throws<ArgumentNullException>("array", () => { Array.ConvertAll<short, short>(null, i => i); });  // Array is null
            Assert.Throws<ArgumentNullException>("converter", () => { Array.ConvertAll<string, string>(new string[] { }, null); }); // Converter is null
            Assert.Throws<InvalidOperationException>(() => {
                Array.ConvertAll<string, string>(new string[] { "x" }, i => { throw new InvalidOperationException(); }); // Converter throws
            });
        }

        [Fact]
        public static void IsFixedSize()
        {
            Assert.Equal(true, new string[] { }.IsFixedSize);
        }

        [Fact]
        public static void IsReadOnly()
        {
            Assert.Equal(false, new int[] { }.IsReadOnly);
        }

        [Fact]
        public static void IsSynchronized()
        {
            Assert.Equal(false, new int[] { }.IsSynchronized);
        }

        public static IEnumerable<object[]> Length_TestData()
        {
            yield return new object[] { new object[] { }, 0 };
            yield return new object[] { new object[] { 1, 2 }, 2 };
        }

        [Theory]
        [MemberData(nameof(Length_TestData))]
        public static void Length(Array array, int expected)
        {
            Assert.Equal(expected, array.Length);
            Assert.Equal(expected, array.LongLength);
        }

        [Fact]
        public static void SyncRoot_Equals_This()
        {
            var array = new string[] { };
            Assert.Same(array, array.SyncRoot);
        }

        internal static void VerifyArray(Array array, int rank, int[] lengths, int[] lowerBounds, int[] upperBounds, bool checkIList)
        {
            Assert.Equal(rank, array.Rank);

            for (int i = 0; i < lengths.Length; i++)
            {
                Assert.Equal(lengths[i], array.GetLength(i));
                Assert.Equal(lengths[i], array.GetLongLength(i));
            }

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

            Assert.Equal(array, array.SyncRoot);

            Assert.False(array.IsSynchronized);
            Assert.True(array.IsFixedSize);
            Assert.False(array.IsReadOnly);
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