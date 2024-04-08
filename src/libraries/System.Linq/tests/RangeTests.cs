// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class RangeTests : EnumerableTests
    {
        [Fact]
        public void Range_ProduceCorrectSequence()
        {
            var rangeSequence = Enumerable.Range(1, 100);
            int expected = 0;
            foreach (var val in rangeSequence)
            {
                expected++;
                Assert.Equal(expected, val);
            }

            Assert.Equal(100, expected);
        }

        public static IEnumerable<object[]> Range_ToArray_ProduceCorrectResult_MemberData()
        {
            for (int i = 0; i < 64; i++)
            {
                yield return new object[] { i };
            }
        }

        [Theory]
        [MemberData(nameof(Range_ToArray_ProduceCorrectResult_MemberData))]
        public void Range_ToArray_ProduceCorrectResult(int length)
        {
            var array = Enumerable.Range(1, length).ToArray();
            Assert.Equal(length, array.Length);
            for (var i = 0; i < array.Length; i++)
                Assert.Equal(i + 1, array[i]);
        }

        [Fact]
        public void Range_ToList_ProduceCorrectResult()
        {
            var list = Enumerable.Range(1, 100).ToList();
            Assert.Equal(100, list.Count);
            for (var i = 0; i < list.Count; i++)
                Assert.Equal(i + 1, list[i]);
        }

        [Fact]
        public void Range_ZeroCountLeadToEmptySequence()
        {
            var array = Enumerable.Range(1, 0).ToArray();
            var array2 = Enumerable.Range(int.MinValue, 0).ToArray();
            var array3 = Enumerable.Range(int.MaxValue, 0).ToArray();
            Assert.Equal(0, array.Length);
            Assert.Equal(0, array2.Length);
            Assert.Equal(0, array3.Length);
        }

        [Fact]
        public void Range_ThrowExceptionOnNegativeCount()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Enumerable.Range(1, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Enumerable.Range(1, int.MinValue));
        }

        [Fact]
        public void Range_ThrowExceptionOnOverflow()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Enumerable.Range(1000, int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Enumerable.Range(int.MaxValue, 1000));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Enumerable.Range(int.MaxValue - 10, 20));
        }

        [Fact]
        public void Range_NotEnumerateAfterEnd()
        {
            using (var rangeEnum = Enumerable.Range(1, 1).GetEnumerator())
            {
                Assert.True(rangeEnum.MoveNext());
                Assert.False(rangeEnum.MoveNext());
                Assert.False(rangeEnum.MoveNext());
            }
        }

        [Fact]
        public void Range_EnumerableAndEnumeratorAreSame()
        {
            var rangeEnumerable = Enumerable.Range(1, 1);
            using (var rangeEnumerator = rangeEnumerable.GetEnumerator())
            {
                Assert.Same(rangeEnumerable, rangeEnumerator);
            }
        }

        [Fact]
        public void Range_GetEnumeratorReturnUniqueInstances()
        {
            var rangeEnumerable = Enumerable.Range(1, 1);
            using (var enum1 = rangeEnumerable.GetEnumerator())
            using (var enum2 = rangeEnumerable.GetEnumerator())
            {
                Assert.NotSame(enum1, enum2);
            }
        }

        [Fact]
        public void Range_ToInt32MaxValue()
        {
            int from = int.MaxValue - 3;
            int count = 4;
            var rangeEnumerable = Enumerable.Range(from, count);

            Assert.Equal(count, rangeEnumerable.Count());

            int[] expected = { int.MaxValue - 3, int.MaxValue - 2, int.MaxValue - 1, int.MaxValue };
            Assert.Equal(expected, rangeEnumerable);
        }

        [Fact]
        public void RepeatedCallsSameResults()
        {
            Assert.Equal(Enumerable.Range(-1, 2), Enumerable.Range(-1, 2));
            Assert.Equal(Enumerable.Range(0, 0), Enumerable.Range(0, 0));
        }

        [Fact]
        public void NegativeStart()
        {
            int start = -5;
            int count = 1;
            int[] expected = { -5 };

            Assert.Equal(expected, Enumerable.Range(start, count));
        }

        [Fact]
        public void ArbitraryStart()
        {
            int start = 12;
            int count = 6;
            int[] expected = { 12, 13, 14, 15, 16, 17 };

            Assert.Equal(expected, Enumerable.Range(start, count));
        }

        [Fact]
        public void Take()
        {
            Assert.Equal(Enumerable.Range(0, 10), Enumerable.Range(0, 20).Take(10));
        }

        [Fact]
        public void TakeExcessive()
        {
            Assert.Equal(Enumerable.Range(0, 10), Enumerable.Range(0, 10).Take(int.MaxValue));
        }

        [Fact]
        public void Skip()
        {
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).Skip(10));
        }

        [Fact]
        public void SkipExcessive()
        {
            Assert.Empty(Enumerable.Range(10, 10).Skip(20));
        }

        [Fact]
        public void SkipTakeCanOnlyBeOne()
        {
            Assert.Equal(new[] { 1 }, Enumerable.Range(1, 10).Take(1));
            Assert.Equal(new[] { 2 }, Enumerable.Range(1, 10).Skip(1).Take(1));
            Assert.Equal(new[] { 3 }, Enumerable.Range(1, 10).Take(3).Skip(2));
            Assert.Equal(new[] { 1 }, Enumerable.Range(1, 10).Take(3).Take(1));
        }

        [Fact]
        public void ElementAt()
        {
            Assert.Equal(4, Enumerable.Range(0, 10).ElementAt(4));
        }

        [Fact]
        public void ElementAtExcessiveThrows()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => Enumerable.Range(0, 10).ElementAt(100));
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            Assert.Equal(4, Enumerable.Range(0, 10).ElementAtOrDefault(4));
        }

        [Fact]
        public void ElementAtOrDefaultExcessiveIsDefault()
        {
            Assert.Equal(0, Enumerable.Range(52, 10).ElementAtOrDefault(100));
        }

        [Fact]
        public void First()
        {
            Assert.Equal(57, Enumerable.Range(57, 1000000000).First());
        }

        [Fact]
        public void FirstOrDefault()
        {
            Assert.Equal(-100, Enumerable.Range(-100, int.MaxValue).FirstOrDefault());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void Last()
        {
            Assert.Equal(1000000056, Enumerable.Range(57, 1000000000).Last());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void LastOrDefault()
        {
            Assert.Equal(int.MaxValue - 101, Enumerable.Range(-100, int.MaxValue).LastOrDefault());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void IListImplementationIsValid()
        {
            Validate(Enumerable.Range(42, 10), new[] { 42, 43, 44, 45, 46, 47, 48, 49, 50, 51 });
            Validate(Enumerable.Range(42, 10).Skip(3).Take(4), new[] { 45, 46, 47, 48 });

            static void Validate(IEnumerable<int> e, int[] expected)
            {
                IList<int> list = Assert.IsAssignableFrom<IList<int>>(e);
                IReadOnlyList<int> roList = Assert.IsAssignableFrom<IReadOnlyList<int>>(e);

                Assert.Throws<NotSupportedException>(() => list.Add(42));
                Assert.Throws<NotSupportedException>(() => list.Insert(0, 42));
                Assert.Throws<NotSupportedException>(() => list.Clear());
                Assert.Throws<NotSupportedException>(() => list.Remove(42));
                Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
                Assert.Throws<NotSupportedException>(() => list[0] = 42);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[-1]);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[expected.Length]);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => roList[-1]);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => roList[expected.Length]);

                Assert.True(list.IsReadOnly);
                Assert.Equal(expected.Length, list.Count);
                Assert.Equal(expected.Length, roList.Count);

                Assert.False(list.Contains(expected[0] - 1));
                Assert.False(list.Contains(expected[^1] + 1));
                Assert.Equal(-1, list.IndexOf(expected[0] - 1));
                Assert.Equal(-1, list.IndexOf(expected[^1] + 1));
                Assert.All(expected, i => Assert.True(list.Contains(i)));
                Assert.All(expected, i => Assert.Equal(Array.IndexOf(expected, i), list.IndexOf(i)));
                for (int i = 0; i < expected.Length; i++)
                {
                    Assert.Equal(expected[i], list[i]);
                    Assert.Equal(expected[i], roList[i]);
                }

                int[] actual = new int[expected.Length + 2];
                list.CopyTo(actual, 1);
                Assert.Equal(0, actual[0]);
                Assert.Equal(0, actual[^1]);
                AssertExtensions.SequenceEqual(expected, actual.AsSpan(1, expected.Length));
            }
        }
    }
}
