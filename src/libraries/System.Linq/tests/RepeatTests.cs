// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class RepeatTests : EnumerableTests
    {
        [Fact]
        public void Repeat_ProduceCorrectSequence()
        {
            var repeatSequence = Enumerable.Repeat(1, 100);
            int count = 0;
            foreach (var val in repeatSequence)
            {
                count++;
                Assert.Equal(1, val);
            }

            Assert.Equal(100, count);
        }

        [Fact]
        public void Repeat_ToArray_ProduceCorrectResult()
        {
            var array = Enumerable.Repeat(1, 100).ToArray();
            Assert.Equal(100, array.Length);
            for (var i = 0; i < array.Length; i++)
                Assert.Equal(1, array[i]);
        }

        [Fact]
        public void Repeat_ToList_ProduceCorrectResult()
        {
            var list = Enumerable.Repeat(1, 100).ToList();
            Assert.Equal(100, list.Count);
            for (var i = 0; i < list.Count; i++)
                Assert.Equal(1, list[i]);
        }

        [Fact]
        public void Repeat_ProduceSameObject()
        {
            object objectInstance = new object();
            var array = Enumerable.Repeat(objectInstance, 100).ToArray();
            Assert.Equal(100, array.Length);
            for (var i = 0; i < array.Length; i++)
                Assert.Same(objectInstance, array[i]);
        }

        [Fact]
        public void Repeat_WorkWithNullElement()
        {
            object objectInstance = null;
            var array = Enumerable.Repeat(objectInstance, 100).ToArray();
            Assert.Equal(100, array.Length);
            for (var i = 0; i < array.Length; i++)
                Assert.Null(array[i]);
        }


        [Fact]
        public void Repeat_ZeroCountLeadToEmptySequence()
        {
            var array = Enumerable.Repeat(1, 0).ToArray();
            Assert.Equal(0, array.Length);
        }

        [Fact]
        public void Repeat_ThrowExceptionOnNegativeCount()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Enumerable.Repeat(1, -1));
        }


        [Fact]
        public void Repeat_NotEnumerateAfterEnd()
        {
            using (var repeatEnum = Enumerable.Repeat(1, 1).GetEnumerator())
            {
                Assert.True(repeatEnum.MoveNext());
                Assert.False(repeatEnum.MoveNext());
                Assert.False(repeatEnum.MoveNext());
            }
        }

        [Fact]
        public void Repeat_EnumerableAndEnumeratorAreSame()
        {
            var repeatEnumerable = Enumerable.Repeat(1, 1);
            using (var repeatEnumerator = repeatEnumerable.GetEnumerator())
            {
                Assert.Same(repeatEnumerable, repeatEnumerator);
            }
        }

        [Fact]
        public void Repeat_GetEnumeratorReturnUniqueInstances()
        {
            var repeatEnumerable = Enumerable.Repeat(1, 1);
            using (var enum1 = repeatEnumerable.GetEnumerator())
            using (var enum2 = repeatEnumerable.GetEnumerator())
            {
                Assert.NotSame(enum1, enum2);
            }
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            Assert.Equal(Enumerable.Repeat(-3, 0), Enumerable.Repeat(-3, 0));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            Assert.Equal(Enumerable.Repeat("SSS", 99), Enumerable.Repeat("SSS", 99));
        }

        [Fact]
        public void CountOneSingleResult()
        {
            int[] expected = { -15 };

            Assert.Equal(expected, Enumerable.Repeat(-15, 1));
        }

        [Fact]
        public void RepeatArbitraryCorrectResults()
        {
            int[] expected = { 12, 12, 12, 12, 12, 12, 12, 12 };

            Assert.Equal(expected, Enumerable.Repeat(12, 8));
        }

        [Fact]
        public void RepeatNull()
        {
            int?[] expected = { null, null, null, null };

            Assert.Equal(expected, Enumerable.Repeat((int?)null, 4));
        }

        [Fact]
        public void Take()
        {
            Assert.Equal(Enumerable.Repeat(12, 8), Enumerable.Repeat(12, 12).Take(8));
        }

        [Fact]
        public void TakeExcessive()
        {
            Assert.Equal(Enumerable.Repeat("", 4), Enumerable.Repeat("", 4).Take(22));
        }

        [Fact]
        public void Skip()
        {
            Assert.Equal(Enumerable.Repeat(12, 8), Enumerable.Repeat(12, 12).Skip(4));
        }

        [Fact]
        public void SkipExcessive()
        {
            Assert.Empty(Enumerable.Repeat(12, 8).Skip(22));
        }

        [Fact]
        public void TakeCanOnlyBeOne()
        {
            Assert.Equal(new[] { 1 }, Enumerable.Repeat(1, 10).Take(1));
            Assert.Equal(new[] { 1 }, Enumerable.Repeat(1, 10).Skip(1).Take(1));
            Assert.Equal(new[] { 1 }, Enumerable.Repeat(1, 10).Take(3).Skip(2));
            Assert.Equal(new[] { 1 }, Enumerable.Repeat(1, 10).Take(3).Take(1));
        }

        [Fact]
        public void SkipNone()
        {
            Assert.Equal(Enumerable.Repeat(12, 8), Enumerable.Repeat(12, 8).Skip(0));
        }

        [Fact]
        public void First()
        {
            Assert.Equal("Test", Enumerable.Repeat("Test", 42).First());
        }

        [Fact]
        public void FirstOrDefault()
        {
            Assert.Equal("Test", Enumerable.Repeat("Test", 42).FirstOrDefault());
        }

        [Fact]
        public void Last()
        {
            Assert.Equal("Test", Enumerable.Repeat("Test", 42).Last());
        }

        [Fact]
        public void LastOrDefault()
        {
            Assert.Equal("Test", Enumerable.Repeat("Test", 42).LastOrDefault());
        }

        [Fact]
        public void ElementAt()
        {
            Assert.Equal("Test", Enumerable.Repeat("Test", 42).ElementAt(13));
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            Assert.Equal("Test", Enumerable.Repeat("Test", 42).ElementAtOrDefault(13));
        }

        [Fact]
        public void ElementAtExcessive()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => Enumerable.Repeat(3, 3).ElementAt(100));
        }

        [Fact]
        public void ElementAtOrDefaultExcessive()
        {
            Assert.Equal(0, Enumerable.Repeat(3, 3).ElementAtOrDefault(100));
        }

        [Fact]
        public void Count()
        {
            Assert.Equal(42, Enumerable.Repeat("Test", 42).Count());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void ICollectionImplementationIsValid()
        {
            Validate(Enumerable.Repeat(42, 10), new[] { 42, 42, 42, 42, 42, 42, 42, 42, 42, 42 });
            Validate(Enumerable.Repeat(42, 10).Skip(3).Take(4), new[] { 42, 42, 42, 42 });

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
