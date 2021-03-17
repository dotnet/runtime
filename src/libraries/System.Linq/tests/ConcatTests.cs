// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Linq.Tests
{
    public class ConcatTests : EnumerableTests
    {
        [Theory]
        [InlineData(new int[] { 2, 3, 2, 4, 5 }, new int[] { 1, 9, 4 })]
        public void SameResultsWithQueryAndRepeatCalls_Int(IEnumerable<int> first, IEnumerable<int> second)
        {
            // workaround: xUnit type inference doesn't work if the input type is not T (like IEnumerable<T>)
            SameResultsWithQueryAndRepeatCallsWorker(first, second);
        }

        [Theory]
        [InlineData(new[] { "AAA", "", "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }, new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" })]
        public void SameResultsWithQueryAndRepeatCalls_String(IEnumerable<string> first, IEnumerable<string> second)
        {
            // workaround: xUnit type inference doesn't work if the input type is not T (like IEnumerable<T>)
            SameResultsWithQueryAndRepeatCallsWorker(first, second);
        }

        private static void SameResultsWithQueryAndRepeatCallsWorker<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            first = from item in first select item;
            second = from item in second select item;

            VerifyEqualsWorker(first.Concat(second), first.Concat(second));
            VerifyEqualsWorker(second.Concat(first), second.Concat(first));
        }

        [Theory]
        [InlineData(new int[] { }, new int[] { }, new int[] { })] // Both inputs are empty
        [InlineData(new int[] { }, new int[] { 2, 6, 4, 6, 2 }, new int[] { 2, 6, 4, 6, 2 })] // One is empty
        [InlineData(new int[] { 2, 3, 5, 9 }, new int[] { 8, 10 }, new int[] { 2, 3, 5, 9, 8, 10 })] // Neither side is empty
        public void PossiblyEmptyInputs(IEnumerable<int> first, IEnumerable<int> second, IEnumerable<int> expected)
        {
            VerifyEqualsWorker(expected, first.Concat(second));
            VerifyEqualsWorker(expected.Skip(first.Count()).Concat(expected.Take(first.Count())), second.Concat(first)); // Swap the inputs around
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Concat(Enumerable.Range(0, 3));
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void FirstNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => ((IEnumerable<int>)null).Concat(Enumerable.Range(0, 0)));
            AssertExtensions.Throws<ArgumentNullException>("first", () => ((IEnumerable<int>)null).Concat(null)); // If both inputs are null, throw for "first" first
        }

        [Fact]
        public void SecondNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("second", () => Enumerable.Range(0, 0).Concat(null));
        }

        [Theory]
        [MemberData(nameof(ArraySourcesData))]
        [MemberData(nameof(SelectArraySourcesData))]
        [MemberData(nameof(EnumerableSourcesData))]
        [MemberData(nameof(NonCollectionSourcesData))]
        [MemberData(nameof(ListSourcesData))]
        [MemberData(nameof(ConcatOfConcatsData))]
        [MemberData(nameof(ConcatWithSelfData))]
        [MemberData(nameof(ChainedCollectionConcatData))]
        [MemberData(nameof(AppendedPrependedConcatAlternationsData))]
        public void VerifyEquals(IEnumerable<int> expected, IEnumerable<int> actual)
        {
            // workaround: xUnit type inference doesn't work if the input type is not T (like IEnumerable<T>)
            VerifyEqualsWorker(expected, actual);
        }

        private static void VerifyEqualsWorker<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            // Returns a list of functions that, when applied to enumerable, should return
            // another one that has equivalent contents.
            var identityTransforms = IdentityTransforms<T>();

            // We run the transforms N^2 times, by testing all transforms
            // of expected against all transforms of actual.
            foreach (var outTransform in identityTransforms)
            {
                foreach (var inTransform in identityTransforms)
                {
                    Assert.Equal(outTransform(expected), inTransform(actual));
                }
            }
        }

        public static IEnumerable<object[]> ArraySourcesData() => GenerateSourcesData(outerTransform: e => e.ToArray());

        public static IEnumerable<object[]> SelectArraySourcesData() => GenerateSourcesData(outerTransform: e => e.Select(i => i).ToArray());

        public static IEnumerable<object[]> EnumerableSourcesData() => GenerateSourcesData();

        public static IEnumerable<object[]> NonCollectionSourcesData() => GenerateSourcesData(outerTransform: e => ForceNotCollection(e));

        public static IEnumerable<object[]> ListSourcesData() => GenerateSourcesData(outerTransform: e => e.ToList());

        public static IEnumerable<object[]> ConcatOfConcatsData()
        {
            yield return new object[]
            {
                Enumerable.Range(0, 20),
                Enumerable.Concat(
                    Enumerable.Concat(
                        Enumerable.Range(0, 4),
                        Enumerable.Range(4, 6)),
                    Enumerable.Concat(
                        Enumerable.Range(10, 3),
                        Enumerable.Range(13, 7)))
            };
        }

        public static IEnumerable<object[]> ConcatWithSelfData()
        {
            IEnumerable<int> source = Enumerable.Repeat(1, 4).Concat(Enumerable.Repeat(1, 5));
            source = source.Concat(source);

            yield return new object[] { Enumerable.Repeat(1, 18), source };
        }

        public static IEnumerable<object[]> ChainedCollectionConcatData() => GenerateSourcesData(innerTransform: e => e.ToList());

        public static IEnumerable<object[]> AppendedPrependedConcatAlternationsData()
        {
            const int EnumerableCount = 4; // How many enumerables to concat together per test case.

            var foundation = Array.Empty<int>();
            var expected = new List<int>();
            IEnumerable<int> actual = foundation;

            // each bit in the last EnumerableCount bits of i represent whether we want to prepend/append a sequence for this iteration.
            // if it's set, we'll prepend. otherwise, we'll append.
            for (int i = 0; i < (1 << EnumerableCount); i++)
            {
                // each bit in last EnumerableCount bits of j is set if we want to ensure the nth enumerable
                // concat'd is an ICollection.
                // Note: It is important we run over the all-bits-set case, since currently
                // Concat is specialized for when all inputs are ICollection.
                for (int j = 0; j < (1 << EnumerableCount); j++)
                {
                    for (int k = 0; k < EnumerableCount; k++) // k is how much bits we shift by, and also the item that gets appended/prepended.
                    {
                        var nextRange = Enumerable.Range(k, 1);
                        bool prepend = ((i >> k) & 1) != 0;
                        bool forceCollection = ((j >> k) & 1) != 0;

                        if (forceCollection)
                        {
                            nextRange = nextRange.ToList();
                        }

                        actual = prepend ? nextRange.Concat(actual) : actual.Concat(nextRange);
                        if (prepend)
                        {
                            expected.Insert(0, k);
                        }
                        else
                        {
                            expected.Add(k);
                        }
                    }

                    yield return new object[] { expected.ToArray(), actual.ToArray() };

                    actual = foundation;
                    expected.Clear();
                }
            }
        }

        private static IEnumerable<object[]> GenerateSourcesData(
            Func<IEnumerable<int>, IEnumerable<int>> outerTransform = null,
            Func<IEnumerable<int>, IEnumerable<int>> innerTransform = null)
        {
            outerTransform = outerTransform ?? (e => e);
            innerTransform = innerTransform ?? (e => e);

            for (int i = 0; i <= 6; i++)
            {
                var expected = Enumerable.Range(0, i * 3);
                var actual = Enumerable.Empty<int>();
                for (int j = 0; j < i; j++)
                {
                    actual = outerTransform(actual.Concat(innerTransform(Enumerable.Range(j * 3, 3))));
                }

                yield return new object[] { expected, actual };
            }
        }

        [Theory]
        [MemberData(nameof(ManyConcatsData))]
        public void ManyConcats(IEnumerable<IEnumerable<int>> sources)
        {
            foreach (var transform in IdentityTransforms<int>())
            {
                IEnumerable<int> concatee = Enumerable.Empty<int>();
                foreach (var source in sources)
                {
                    concatee = concatee.Concat(transform(source));
                }

                Assert.Equal(sources.Sum(s => s.Count()), concatee.Count());
                VerifyEqualsWorker(sources.SelectMany(s => s), concatee);
            }
        }

        [Theory]
        [MemberData(nameof(ManyConcatsData))]
        public void ManyConcatsRunOnce(IEnumerable<IEnumerable<int>> sources)
        {
            foreach (var transform in IdentityTransforms<int>())
            {
                IEnumerable<int> concatee = Enumerable.Empty<int>();
                foreach (var source in sources)
                {
                    concatee = concatee.RunOnce().Concat(transform(source));
                }

                Assert.Equal(sources.Sum(s => s.Count()), concatee.Count());
            }
        }

        public static IEnumerable<object[]> ManyConcatsData()
        {
            yield return new object[] { Enumerable.Repeat(Enumerable.Empty<int>(), 256) };
            yield return new object[] { Enumerable.Repeat(Enumerable.Repeat(6, 1), 256) };
            // Make sure Concat doesn't accidentally swap around the sources, e.g. [3, 4], [1, 2] should not become [1..4]
            yield return new object[] { Enumerable.Range(0, 500).Select(i => Enumerable.Repeat(i, 1)).Reverse() };
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void CountOfConcatIteratorShouldThrowExceptionOnIntegerOverflow()
        {
            var supposedlyLargeCollection = new DelegateBasedCollection<int> { CountWorker = () => int.MaxValue };
            var tinyCollection = new DelegateBasedCollection<int> { CountWorker = () => 1 };

            // We need to use checked arithmetic summing up the collections' counts.
            Assert.Throws<OverflowException>(() => supposedlyLargeCollection.Concat(tinyCollection).Count());
            Assert.Throws<OverflowException>(() => tinyCollection.Concat(tinyCollection).Concat(supposedlyLargeCollection).Count());
            Assert.Throws<OverflowException>(() => tinyCollection.Concat(tinyCollection).Concat(tinyCollection).Concat(supposedlyLargeCollection).Count());

            // This applies to ToArray() and ToList() as well, which try to preallocate the exact size
            // needed if all inputs are ICollections.
            Assert.Throws<OverflowException>(() => supposedlyLargeCollection.Concat(tinyCollection).ToArray());
            Assert.Throws<OverflowException>(() => tinyCollection.Concat(tinyCollection).Concat(supposedlyLargeCollection).ToArray());
            Assert.Throws<OverflowException>(() => tinyCollection.Concat(tinyCollection).Concat(tinyCollection).Concat(supposedlyLargeCollection).ToArray());

            Assert.Throws<OverflowException>(() => supposedlyLargeCollection.Concat(tinyCollection).ToList());
            Assert.Throws<OverflowException>(() => tinyCollection.Concat(tinyCollection).Concat(supposedlyLargeCollection).ToList());
            Assert.Throws<OverflowException>(() => tinyCollection.Concat(tinyCollection).Concat(tinyCollection).Concat(supposedlyLargeCollection).ToList());
        }

        [Fact]
        [OuterLoop("This test tries to catch stack overflows and can take a long time.")]
        public void CountOfConcatCollectionChainShouldBeResilientToStackOverflow()
        {
            // Currently, .Concat chains of 3+ ICollections are represented as a
            // singly-linked list of iterators, each holding 1 collection. When
            // we call Count() on the last iterator, it needs to sum up the count
            // of its collection, plus the count of all the previous collections.

            // It is tempting to use recursion to solve this problem, by simply
            // returning [this collection].Count + [previous iterator].Count(),
            // since it is so much simpler than the iterative solution.
            // However, this can lead to a stack overflow for long chains of
            // .Concats. This is a test to guard against that.

            // Something big enough to cause a SO when this many method calls
            // are made, but not too large.
            const int NumberOfConcats = 30000;

            // For perf reasons (this test can take a long time to run)
            // we use a for-loop manually rather than .Repeat and .Aggregate
            IEnumerable<int> concatChain = Array.Empty<int>(); // note: all items in this chain must implement ICollection<T>
            for (int i = 0; i < NumberOfConcats; i++)
            {
                concatChain = concatChain.Concat(Array.Empty<int>());
            }

            Assert.Equal(0, concatChain.Count()); // should not throw a StackOverflowException
            // ToArray needs the count as well, and the process of copying all of the collections
            // to the array should also not be recursive.
            Assert.Equal(new int[] { }, concatChain.ToArray());
            Assert.Equal(new List<int> { }, concatChain.ToList()); // ToList also gets the count beforehand
        }

        [Fact]
        [OuterLoop("This test tries to catch stack overflows and can take a long time.")]
        public void CountOfConcatEnumerableChainShouldBeResilientToStackOverflow()
        {
            // Concat chains where one or more of the inputs is not an ICollection
            // (so we cannot figure out the total count w/o iterating through some
            // of the enumerables) are represented much the same, as singly-linked lists.
            // However, the underlying iterator type/implementation of Count()
            // will be different (it treats all of the concatenated enumerables as lazy),
            // so we have to make sure that implementation isn't recursive either.

            const int NumberOfConcats = 30000;

            // Start with a non-ICollection seed, which means all subsequent
            // Concats will be treated as lazy enumerables.
            IEnumerable<int> concatChain = ForceNotCollection(Array.Empty<int>());

            // Then, concatenate a bunch of ICollections to it.
            // Since Count() is optimized for when its input is an ICollection,
            // this will reduce the time it takes to run the test, which otherwise
            // would take quite long.
            for (int i = 0; i < NumberOfConcats; i++)
            {
                concatChain = concatChain.Concat(Array.Empty<int>());
            }

            Assert.Equal(0, concatChain.Count());
            // ToArray/ToList do not attempt to preallocate a result of the correct
            // size- if there's just 1 lazy enumerable in the chain, it's impossible
            // to get the count to preallocate without iterating through that, and then
            // when the data is being copied you'd need to iterate through it again
            // (not ideal). So we do not need to worry about those methods having a
            // stack overflow through getting the Count() of the iterator.
        }

        [Fact]
        [OuterLoop("This test tries to catch stack overflows and can take a long time.")]
        public void GettingFirstEnumerableShouldBeResilientToStackOverflow()
        {
            // When MoveNext() is first called on a chain of 3+ Concats, we have to
            // walk all the way to the tail of the linked list to reach the first
            // concatee. Likewise as before, make sure this isn't accomplished with
            // a recursive implementation.

            const int NumberOfConcats = 30000;

            // Start with a lazy seed.
            // The seed holds 1 item, so during the first MoveNext we won't have to
            // backtrack through the linked list 30000 times. This is for test perf.
            IEnumerable<int> concatChain = ForceNotCollection(new int[] { 0xf00 });

            for (int i = 0; i < NumberOfConcats; i++)
            {
                concatChain = concatChain.Concat(Array.Empty<int>());
            }

            using (IEnumerator<int> en = concatChain.GetEnumerator())
            {
                Assert.True(en.MoveNext()); // should not SO
                Assert.Equal(0xf00, en.Current);
            }
        }

        [Fact]
        [OuterLoop("This test tries to catch stack overflows and can take a long time.")]
        public void GetEnumerableOfConcatCollectionChainFollowedByEnumerableNodeShouldBeResilientToStackOverflow()
        {
            // Since concatenating even 1 lazy enumerable ruins the ability for ToArray/ToList
            // optimizations, if one is Concat'd to a collection-based iterator then we will
            // fall back to treating all subsequent concatenations as lazy.
            // Since it is possible for an enumerable iterator to be preceded by a collection
            // iterator, but not the other way around, an assumption is made. If an enumerable
            // iterator encounters a collection iterator during GetEnumerable(), it calls into
            // the collection iterator's GetEnumerable() under the assumption that the callee
            // will never call into another enumerable iterator's GetEnumerable(). This is
            // because collection iterators can only be preceded by other collection iterators.

            // Violation of this assumption means that the GetEnumerable() implementations could
            // become mutually recursive, which may lead to stack overflow for enumerable iterators
            // preceded by a long chain of collection iterators.

            const int NumberOfConcats = 30000;

            // This time, start with an ICollection seed. We want the subsequent Concats in
            // the loop to produce collection iterators.
            IEnumerable<int> concatChain = new int[] { 0xf00 };

            for (int i = 0; i < NumberOfConcats - 1; i++)
            {
                concatChain = concatChain.Concat(Array.Empty<int>());
            }

            // Finally, link an enumerable iterator at the head of the list.
            concatChain = concatChain.Concat(ForceNotCollection(Array.Empty<int>()));

            using (IEnumerator<int> en = concatChain.GetEnumerator())
            {
                Assert.True(en.MoveNext());
                Assert.Equal(0xf00, en.Current);
            }
        }

        [Theory]
        [MemberData(nameof(GetToArrayDataSources))]
        public void CollectionInterleavedWithLazyEnumerables_ToArray(IEnumerable<int>[] arrays)
        {
            // See https://github.com/dotnet/runtime/issues/23389

            IEnumerable<int> concats = arrays[0];

            for (int i = 1; i < arrays.Length; i++)
            {
                concats = concats.Concat(arrays[i]);
            }

            int[] results = concats.ToArray();

            for (int i = 0; i < results.Length; i++)
            {
                Assert.Equal(i, results[i]);
            }
        }

        public static IEnumerable<object[]> GetToArrayDataSources()
        {
            // Marker at the end
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new TestEnumerable<int>(new int[] { 0 }),
                    new TestEnumerable<int>(new int[] { 1 }),
                    new TestEnumerable<int>(new int[] { 2 }),
                    new int[] { 3 },
                }
            };

            // Marker at beginning
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new int[] { 0 },
                    new TestEnumerable<int>(new int[] { 1 }),
                    new TestEnumerable<int>(new int[] { 2 }),
                    new TestEnumerable<int>(new int[] { 3 }),
                }
            };

            // Marker in middle
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new TestEnumerable<int>(new int[] { 0 }),
                    new int[] { 1 },
                    new TestEnumerable<int>(new int[] { 2 }),
                }
            };

            // Non-marker in middle
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new int[] { 0 },
                    new TestEnumerable<int>(new int[] { 1 }),
                    new int[] { 2 },
                }
            };

            // Big arrays (marker in middle)
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new TestEnumerable<int>(Enumerable.Range(0, 100).ToArray()),
                    Enumerable.Range(100, 100).ToArray(),
                    new TestEnumerable<int>(Enumerable.Range(200, 100).ToArray()),
                }
            };

            // Big arrays (non-marker in middle)
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    Enumerable.Range(0, 100).ToArray(),
                    new TestEnumerable<int>(Enumerable.Range(100, 100).ToArray()),
                    Enumerable.Range(200, 100).ToArray(),
                }
            };

            // Interleaved (first marker)
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new int[] { 0 },
                    new TestEnumerable<int>(new int[] { 1 }),
                    new int[] { 2 },
                    new TestEnumerable<int>(new int[] { 3 }),
                    new int[] { 4 },
                }
            };

            // Interleaved (first non-marker)
            yield return new object[]
            {
                new IEnumerable<int>[]
                {
                    new TestEnumerable<int>(new int[] { 0 }),
                    new int[] { 1 },
                    new TestEnumerable<int>(new int[] { 2 }),
                    new int[] { 3 },
                    new TestEnumerable<int>(new int[] { 4 }),
                }
            };
        }
    }
}
