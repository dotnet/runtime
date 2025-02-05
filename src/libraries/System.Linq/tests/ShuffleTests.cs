// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ShuffleTests : EnumerableTests
    {
        [Fact]
        public void InvalidArguments()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => Enumerable.Shuffle<string>(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(100)]
        public void Count_ExpectedCountReturned(int length)
        {
            Assert.All(CreateSources(Enumerable.Range(0, length)), source =>
            {
                Assert.Equal(length, source.Shuffle().Count());

                IEnumerable<int> shuffled = source.Shuffle();
                Assert.Equal(length, shuffled.Count());
                Assert.Equal(length, shuffled.Count());
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(100)]
        public void Enumeration_AllElementsReturned(int length)
        {
            Assert.All(CreateSources(Enumerable.Range(0, length)), source =>
            {
                List<int> shuffled = [];
                foreach (int i in source.Shuffle())
                {
                    shuffled.Add(i);
                }

                Assert.Equal(length, shuffled.Count);
                shuffled.Sort();
                Assert.Equal(Enumerable.Range(0, length), shuffled);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(100)]
        public void ToArray_AllElementsReturned(int length)
        {
            Assert.All(CreateSources(Enumerable.Range(0, length)), source =>
            {
                int[] shuffled = source.Shuffle().ToArray();
                Assert.Equal(length, shuffled.Length);
                Array.Sort(shuffled);
                Assert.Equal(Enumerable.Range(0, length), shuffled);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(100)]
        public void ToList_AllElementsReturned(int length)
        {
            Assert.All(CreateSources(Enumerable.Range(0, length)), source =>
            {
                List<int> shuffled = source.Shuffle().ToList();
                Assert.Equal(length, shuffled.Count);
                shuffled.Sort();
                Assert.Equal(Enumerable.Range(0, length), shuffled);
            });
        }

        [Fact]
        public void Enumeration_ElementsAreRandomized()
        {
            // The chance that shuffling a thousand elements produces the same order twice is infinitesimal.
            int length = 1000;
            Assert.All(CreateSources(Enumerable.Range(0, length)), source =>
            {
                List<int> first = [], second = [];
                foreach (int i in source.Shuffle())
                {
                    first.Add(i);
                }
                foreach (int i in source.Shuffle())
                {
                    second.Add(i);
                }

                Assert.Equal(length, first.Count);
                Assert.Equal(length, second.Count);
                Assert.NotEqual(first, second);
            });
        }

        [Fact]
        public void ToArray_ElementsAreRandomized()
        {
            // The chance that shuffling a thousand elements produces the same order twice is infinitesimal.
            int length = 1000;
            Assert.All(CreateSources(Enumerable.Range(0, length)), source =>
            {
                int[] first = source.Shuffle().ToArray(), second = source.Shuffle().ToArray();
                Assert.Equal(length, first.Length);
                Assert.Equal(length, second.Length);
                Assert.NotEqual(first, second);
            });
        }

        [Fact]
        public void ToList_ElementsAreRandomized()
        {
            // The chance that shuffling a thousand elements produces the same order twice is infinitesimal.
            int length = 1000;
            Assert.All(CreateSources(Enumerable.Range(0, length)), source =>
            {
                List<int> first = source.Shuffle().ToList(), second = source.Shuffle().ToList();
                Assert.Equal(length, first.Count);
                Assert.Equal(length, second.Count);
                Assert.NotEqual(first, second);
            });
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Reverse();
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en is not null && en.MoveNext());
        }

        [Fact]
        public void First_Last_GetElement_Invalid_ExpectedExceptions()
        {
            Assert.All(CreateSources(Enumerable.Empty<string>()), source =>
            {
                Assert.Throws<InvalidOperationException>(() => source.Shuffle().First());
                Assert.Throws<InvalidOperationException>(() => source.Shuffle().Last());

                Assert.Null(source.Shuffle().ElementAtOrDefault(1));
                Assert.Null(source.Shuffle().ElementAtOrDefault(-1));
                Assert.Null(source.Shuffle().FirstOrDefault());
                Assert.Null(source.Shuffle().LastOrDefault());
            });
        }

        [Fact]
        public void First_Last_GetElement_Valid_ExpectedElements()
        {
            Assert.All(CreateSources(Enumerable.Range(0, 10)), source =>
            {
                Assert.InRange(source.Shuffle().First(), 0, 9);
                Assert.InRange(source.Shuffle().Last(), 0, 9);
                Assert.InRange(source.Shuffle().ElementAt(5), 0, 9);
            });
        }

        [Fact]
        public void First_Last_GetElement_ProduceRandomElements()
        {
            Assert.All(CreateSources(Enumerable.Range(0, 100)), source =>
            {
                AssertRetry(() => source.Shuffle().First() != source.Shuffle().First());
                AssertRetry(() => source.Shuffle().Last() != source.Shuffle().Last());
                AssertRetry(() => source.Shuffle().ElementAt(5) != source.Shuffle().ElementAt(5));
            });

            static void AssertRetry(Func<bool> predicate)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (predicate()) return;
                }

                Assert.Fail("Predicate was true for 10 iterations");
            }
        }
    }
}
