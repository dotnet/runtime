// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public partial class ArrayTests
    {
        public static IEnumerable<object[]> Fill_Generic_TestData()
        {
            var data = Enumerable.Empty<object[]>();

            var r = new Random(0x051778f7);
            int[] lengths = { 0, 1, 2, 3, 5, 8, 13 };
            
            foreach (int length in lengths)
            {
                IEnumerable<int> source = Enumerable.Range(1, length).Select(_ => r.Next());
                
                data = data.Concat(GenerateFillData(source, r.Next(), i => i))
                    .Concat(GenerateFillData(source, r.Next(), i => unchecked((byte)i)))
                    .Concat(GenerateFillData(source, r.Next(), i => unchecked((short)i)))
                    .Concat(GenerateFillData(source, r.Next(), i => (long)i))
                    .Concat(GenerateFillData(source, r.Next(), i => new StrongBox<int>(i)))
                    .Concat(GenerateFillData(source, r.Next(), i => i.ToString()))
                    .Concat(GenerateFillData(source, r.Next(), i => unchecked((ByteEnum)i)))
                    .Concat(GenerateFillData(source, r.Next(), i => unchecked((Int16Enum)i)))
                    .Concat(GenerateFillData(source, r.Next(), i => (Int32Enum)i))
                    .Concat(GenerateFillData(source, r.Next(), i => (Int64Enum)i))
                    .Concat(GenerateFillData(source, r.Next(), i => new object()));
            }

            return data;
        }

        private static IEnumerable<object[]> GenerateFillData<TSource, TResult>(IEnumerable<TSource> source, TSource seed, Func<TSource, TResult> transform)
        {
            int count = source.Count();
            TResult repeatedValue = transform(seed);
            // Force evaluation so neither `source` or `transform` are re-run if the sequence is enumerated more than once.
            IEnumerable<TResult> transformed = source.Select(transform).ToList();

            yield return new object[] { transformed, repeatedValue, 0, count }; // Fill the entire array.
            yield return new object[] { transformed, repeatedValue, 0, count / 2 }; // Fill the beginning of the array.
            yield return new object[] { transformed, repeatedValue, count / 2, count / 2 }; // Fill the end of the array, assuming `length` is even.
            yield return new object[] { transformed, repeatedValue, count / 4, count / 2 }; // Fill the middle of the array.
        }

        [Theory]
        [MemberData(nameof(Fill_Generic_TestData))]
        public static void Fill_Generic<T>(IEnumerable<T> source, T value, int startIndex, int count)
        {
            if (startIndex == 0 && count == source.Count())
            {
                T[] array = source.ToArray();
                Array.Fill(array, value);
                Assert.Equal(Enumerable.Repeat(value, count), array);
            }

            {
                T[] array = source.ToArray();

                // Before calling Fill, we want to capture the segments before/after the filled region.
                // We want to ensure that in addition to filling in what it's supposed to, Fill does
                // not touch the adjacent segments.
                T[] before = source.Take(startIndex).ToArray();
                T[] after = source.Skip(startIndex + count).ToArray();

                Array.Fill(array, value, startIndex, count);

                Assert.Equal(before, array.Take(startIndex));
                Assert.Equal(Enumerable.Repeat(value, count), array.Skip(startIndex).Take(count));
                Assert.Equal(after, array.Skip(startIndex + count));
            }
        }

        public static IEnumerable<object[]> Reverse_Generic_Int_TestData()
        {
            // TODO: use (or merge this data into) Reverse_TestData if/when xunit/xunit#965 is merged
            yield return new object[] { new int[] { 1, 2, 3 }, 0, 3, new int[] { 3, 2, 1 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 0, 2, new int[] { 2, 1, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 1, 2, new int[] { 1, 3, 2 } };

            // Nothing to reverse
            yield return new object[] { new int[] { 1, 2, 3 }, 2, 1, new int[] { 1, 2, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 0, 1, new int[] { 1, 2, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 0, 0, new int[] { 1, 2, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 3, 0, new int[] { 1, 2, 3 } };
            yield return new object[] { new int[0], 0, 0, new int[0] };
        }

        [Theory]
        [MemberData(nameof(Reverse_Generic_Int_TestData))]
        public static void Reverse_Generic(int[] array, int index, int length, int[] expected)
        {
            if (index == 0 && length == array.Length)
            {
                int[] arrayClone1 = (int[])array.Clone();
                Array.Reverse(arrayClone1);
                Assert.Equal(expected, arrayClone1);
            }
            int[] arrayClone2 = (int[])array.Clone();
            Array.Reverse(arrayClone2, index, length);
            Assert.Equal(expected, arrayClone2);
        }

        [Fact]
        public static void Reverse_Generic_NullArray_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("array", () => Array.Reverse((string[])null));
            AssertExtensions.Throws<ArgumentNullException>("array", () => Array.Reverse((string[])null, 0, 0));
        }

        [Fact]
        public static void Reverse_Generic_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => Array.Reverse(new string[0], -1, 0));
        }

        [Fact]
        public static void Reverse_Generic_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => Array.Reverse(new string[0], 0, -1));
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(3, 4, 0)]
        [InlineData(3, 3, 1)]
        [InlineData(3, 2, 2)]
        [InlineData(3, 1, 3)]
        [InlineData(3, 0, 4)]
        public static void Reverse_Generic_InvalidOffsetPlusLength_ThrowsArgumentException(int arrayLength, int index, int length)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Array.Reverse(new string[arrayLength], index, length));
        }

        [Fact]
        public void CreateInstance_TypeNotRuntimeType_ThrowsArgumentException()
        {
            // This cannot be a [Theory] due to https://github.com/xunit/xunit/issues/1325.
            foreach (Type elementType in Helpers.NonRuntimeTypes)
            {
                AssertExtensions.Throws<ArgumentException>("elementType", () => Array.CreateInstance(elementType, 1));
                AssertExtensions.Throws<ArgumentException>("elementType", () => Array.CreateInstance(elementType, 1, 1));
                AssertExtensions.Throws<ArgumentException>("elementType", () => Array.CreateInstance(elementType, 1, 1, 1));
                AssertExtensions.Throws<ArgumentException>("elementType", () => Array.CreateInstance(elementType, new int[1]));
                AssertExtensions.Throws<ArgumentException>("elementType", () => Array.CreateInstance(elementType, new long[1]));
                AssertExtensions.Throws<ArgumentException>("elementType", () => Array.CreateInstance(elementType, new int[1], new int[1]));
            }
        }
    }
}
