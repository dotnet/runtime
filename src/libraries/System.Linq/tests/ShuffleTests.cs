// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
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

                Assert.Equal(length, source.Shuffle().Take(length).Count());
                Assert.Equal(Math.Min(length, 1), source.Shuffle().Take(1).Count());
                Assert.Equal(Math.Min(length, 1), source.Shuffle().Take(2).Take(1).Count());
                Assert.Equal(Math.Min(length, 1), source.Shuffle().Take(1).Take(2).Count());
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
                List<int> shuffled;

                shuffled = [];
                foreach (int i in source.Shuffle()) shuffled.Add(i);
                Assert.Equal(length, shuffled.Count);
                shuffled.Sort();
                Assert.Equal(Enumerable.Range(0, length), shuffled);

                shuffled = [];
                foreach (int i in source.Shuffle().Take(length)) shuffled.Add(i);
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
                int[] shuffled;

                shuffled = source.Shuffle().ToArray();
                Assert.Equal(length, shuffled.Length);
                Array.Sort(shuffled);
                Assert.Equal(Enumerable.Range(0, length), shuffled);

                shuffled = source.Shuffle().Take(length).ToArray();
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
                List<int> shuffled;

                shuffled = source.Shuffle().ToList();
                Assert.Equal(length, shuffled.Count);
                shuffled.Sort();
                Assert.Equal(Enumerable.Range(0, length), shuffled);

                shuffled = source.Shuffle().Take(length).ToList();
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
                List<int> first, second;

                first = [];
                second = [];
                foreach (int i in source.Shuffle()) first.Add(i);
                foreach (int i in source.Shuffle()) second.Add(i);
                Assert.Equal(length, first.Count);
                Assert.Equal(length, second.Count);
                Assert.Equal(length, first.Distinct().Count());
                Assert.Equal(length, second.Distinct().Count());
                Assert.NotEqual(first, second);

                foreach (int takeCount in new[] { length - 1, length + 1 })
                {
                    first = [];
                    second = [];
                    foreach (int i in source.Shuffle().Take(takeCount)) first.Add(i);
                    foreach (int i in source.Shuffle().Take(takeCount)) second.Add(i);
                    Assert.Equal(Math.Min(takeCount, length), first.Count);
                    Assert.Equal(Math.Min(takeCount, length), second.Count);
                    Assert.Equal(Math.Min(takeCount, length), first.Distinct().Count());
                    Assert.Equal(Math.Min(takeCount, length), second.Distinct().Count());
                    Assert.NotEqual(first, second);

                    first = [];
                    second = [];
                    foreach (int i in source.Shuffle().Take(takeCount + 1).Take(takeCount)) first.Add(i);
                    foreach (int i in source.Shuffle().Take(takeCount + 1).Take(takeCount)) second.Add(i);
                    Assert.Equal(Math.Min(takeCount, length), first.Count);
                    Assert.Equal(Math.Min(takeCount, length), second.Count);
                    Assert.Equal(Math.Min(takeCount, length), first.Distinct().Count());
                    Assert.Equal(Math.Min(takeCount, length), second.Distinct().Count());
                    Assert.NotEqual(first, second);
                }
            });
        }

        [Fact]
        public void ToArray_ElementsAreRandomized()
        {
            // The chance that shuffling a thousand elements produces the same order twice is infinitesimal.
            int length = 1000;
            Assert.All(CreateSources(Enumerable.Range(0, length)), source =>
            {
                try
                {
                    int[] first, second;

                    first = source.Shuffle().ToArray();
                    second = source.Shuffle().ToArray();
                    Assert.Equal(length, first.Length);
                    Assert.Equal(length, second.Length);
                    Assert.Equal(length, first.Distinct().Count());
                    Assert.Equal(length, second.Distinct().Count());
                    Assert.NotEqual(first, second);

                    foreach (int takeCount in new[] { length - 1, length + 1 })
                    {
                        first = source.Shuffle().Take(takeCount).ToArray();
                        second = source.Shuffle().Take(takeCount).ToArray();
                        Assert.Equal(Math.Min(takeCount, length), first.Length);
                        Assert.Equal(Math.Min(takeCount, length), second.Length);
                        Assert.Equal(Math.Min(takeCount, length), first.Distinct().Count());
                        Assert.Equal(Math.Min(takeCount, length), second.Distinct().Count());
                        Assert.NotEqual(first, second);

                        first = source.Shuffle().Take(takeCount + 1).Take(takeCount).ToArray();
                        second = source.Shuffle().Take(takeCount + 1).Take(takeCount).ToArray();
                        Assert.Equal(Math.Min(takeCount, length), first.Length);
                        Assert.Equal(Math.Min(takeCount, length), second.Length);
                        Assert.Equal(Math.Min(takeCount, length), first.Distinct().Count());
                        Assert.Equal(Math.Min(takeCount, length), second.Distinct().Count());
                        Assert.NotEqual(first, second);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(source.GetType().ToString(), e);
                }
            });
        }

        [Fact]
        public void ToList_ElementsAreRandomized()
        {
            // The chance that shuffling a thousand elements produces the same order twice is infinitesimal.
            int length = 1000;
            Assert.All(CreateSources(Enumerable.Range(0, length)), source =>
            {
                List<int> first, second;

                first = source.Shuffle().ToList();
                second = source.Shuffle().ToList();
                Assert.Equal(length, first.Count);
                Assert.Equal(length, second.Count);
                Assert.Equal(length, first.Distinct().Count());
                Assert.Equal(length, second.Distinct().Count());
                Assert.NotEqual(first, second);

                foreach (int takeCount in new[] { length - 1, length + 1 })
                {
                    first = source.Shuffle().Take(takeCount).ToList();
                    second = source.Shuffle().Take(takeCount).ToList();
                    Assert.Equal(Math.Min(takeCount, length), first.Count);
                    Assert.Equal(Math.Min(takeCount, length), second.Count);
                    Assert.Equal(Math.Min(takeCount, length), first.Distinct().Count());
                    Assert.Equal(Math.Min(takeCount, length), second.Distinct().Count());
                    Assert.NotEqual(first, second);

                    first = source.Shuffle().Take(takeCount + 1).Take(takeCount).ToList();
                    second = source.Shuffle().Take(takeCount + 1).Take(takeCount).ToList();
                    Assert.Equal(Math.Min(takeCount, length), first.Count);
                    Assert.Equal(Math.Min(takeCount, length), second.Count);
                    Assert.Equal(Math.Min(takeCount, length), first.Distinct().Count());
                    Assert.Equal(Math.Min(takeCount, length), second.Distinct().Count());
                    Assert.NotEqual(first, second);
                }
            });
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            // Don't insist on this behaviour, but check it's correct if it happens

            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Shuffle();
            var en = iterator as IEnumerator<int>;
            Assert.False(en is not null && en.MoveNext());

            iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Shuffle().Take(1);
            en = iterator as IEnumerator<int>;
            Assert.False(en is not null && en.MoveNext());

            iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Shuffle().Take(2).Take(1);
            en = iterator as IEnumerator<int>;
            Assert.False(en is not null && en.MoveNext());
        }

        [Fact]
        public void First_Last_GetElement_Invalid_ExpectedExceptions()
        {
            Assert.All(CreateSources(Enumerable.Empty<string>()), source =>
            {
                Assert.Throws<InvalidOperationException>(() => source.Shuffle().First());
                Assert.Throws<InvalidOperationException>(() => source.Shuffle().Last());
                Assert.Throws<InvalidOperationException>(() => source.Shuffle().Take(1).First());
                Assert.Throws<InvalidOperationException>(() => source.Shuffle().Take(1).Last());

                Assert.Null(source.Shuffle().ElementAtOrDefault(1));
                Assert.Null(source.Shuffle().ElementAtOrDefault(-1));
                Assert.Null(source.Shuffle().FirstOrDefault());
                Assert.Null(source.Shuffle().LastOrDefault());

                Assert.Null(source.Shuffle().Take(1).ElementAtOrDefault(1));
                Assert.Null(source.Shuffle().Take(1).ElementAtOrDefault(-1));
                Assert.Null(source.Shuffle().Take(1).FirstOrDefault());
                Assert.Null(source.Shuffle().Take(1).LastOrDefault());

                Assert.Null(source.Shuffle().Take(3).Take(1).ElementAtOrDefault(1));
                Assert.Null(source.Shuffle().Take(3).Take(1).ElementAtOrDefault(-1));
                Assert.Null(source.Shuffle().Take(3).Take(1).FirstOrDefault());
                Assert.Null(source.Shuffle().Take(3).Take(1).LastOrDefault());
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

                Assert.InRange(source.Shuffle().Take(1).First(), 0, 9);
                Assert.InRange(source.Shuffle().Take(1).Last(), 0, 9);
                Assert.InRange(source.Shuffle().Take(8).ElementAt(5), 0, 9);

                Assert.InRange(source.Shuffle().Take(3).Take(2).First(), 0, 9);
                Assert.InRange(source.Shuffle().Take(3).Take(2).Last(), 0, 9);
                Assert.InRange(source.Shuffle().Take(8).Take(7).ElementAt(5), 0, 9);
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

                AssertRetry(() => source.Shuffle().Take(10).First() != source.Shuffle().Take(10).First());
                AssertRetry(() => source.Shuffle().Take(10).Last() != source.Shuffle().Take(10).Last());
                AssertRetry(() => source.Shuffle().Take(10).ElementAt(5) != source.Shuffle().Take(10).ElementAt(5));

                AssertRetry(() => source.Shuffle().Take(10).Take(5).First() != source.Shuffle().Take(10).Take(5).First());
                AssertRetry(() => source.Shuffle().Take(10).Take(5).Last() != source.Shuffle().Take(10).Take(5).Last());
                AssertRetry(() => source.Shuffle().Take(10).Take(5).ElementAt(3) != source.Shuffle().Take(10).Take(5).ElementAt(3));
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

        [OuterLoop]
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ValidateShuffleTakeRandomDistribution(int mode)
        {
            const int InputLength = 10;
            const int Iterations = 100_000;
            const double Expected = Iterations / (double)InputLength;

            IEnumerable<int> ints = Enumerable.Range(0, InputLength);
            IEnumerable<int>[] sources = [ints, ints.ToList(), ForceNotCollection(ints)];

            foreach (IEnumerable<int> source in sources)
            {
                IEnumerable<int> selected = source.Shuffle().Take(1);

                Dictionary<int, int> counts = new();
                for (int i = 0; i < Iterations; i++)
                {
                    int value = 0;
                    switch (mode)
                    {
                        case 0:
                            using (IEnumerator<int> e = selected.GetEnumerator())
                            {
                                e.MoveNext();
                                value = e.Current;
                            }
                            break;

                        case 1:
                            value = selected.First();
                            break;

                        case 2:
                            value = selected.ToArray()[0];
                            break;

                        default:
                            Assert.Fail("Invalid mode");
                            break;
                    }

                    CollectionsMarshal.GetValueRefOrAddDefault(counts, value, out _)++;
                }

                Assert.All(counts, kvp => Assert.InRange(kvp.Value, Expected * 0.85, Expected * 1.15));
            }
        }
    }
}
