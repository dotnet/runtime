// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Linq.Tests
{
    public class ConcatTests : EnumerableTests
    {
        private static List<Func<IEnumerable<T>, IEnumerable<T>>> IdentityTransforms<T>()
        {
            // All of these transforms should take an enumerable and produce
            // another enumerable with the same contents.
            return new List<Func<IEnumerable<T>, IEnumerable<T>>>
            {
                e => e,
                e => e.ToArray(),
                e => e.ToList(),
                e => e.Select(i => i),
                e => e.Concat(Array.Empty<T>()),
                e => ForceNotCollection(e),
                e => e.Concat(ForceNotCollection(Array.Empty<T>()))
            };
        }

        [Theory]
        [InlineData(new int[] { 2, 3, 2, 4, 5 }, new int[] { 1, 9, 4 })]
        public void SameResultsWithQueryAndRepeatCalls(IEnumerable<int> first, IEnumerable<int> second)
        {
            // workaround: xUnit type inference doesn't work if the input type is not T (like IEnumerable<T>)
            SameResultsWithQueryAndRepeatCallsWorker(first, second);
        }

        [Theory]
        [InlineData(new[] { "AAA", "", "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }, new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" })]
        public void SameResultsWithQueryAndRepeatCalls(IEnumerable<string> first, IEnumerable<string> second)
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
            Assert.Throws<ArgumentNullException>("first", () => ((IEnumerable<int>)null).Concat(Enumerable.Range(0, 0)));
            Assert.Throws<ArgumentNullException>("first", () => ((IEnumerable<int>)null).Concat(null)); // If both inputs are null, throw for "first" first
        }

        [Fact]
        public void SecondNull()
        {
            Assert.Throws<ArgumentNullException>("second", () => Enumerable.Range(0, 0).Concat(null));
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
        public void ManyConcats(IEnumerable<IEnumerable<int>> sources, IEnumerable<int> expected)
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

        public static IEnumerable<object[]> ManyConcatsData()
        {
            yield return new object[] { Enumerable.Repeat(Enumerable.Empty<int>(), 256), Enumerable.Empty<int>() };
            yield return new object[] { Enumerable.Repeat(Enumerable.Repeat(6, 1), 256), Enumerable.Repeat(6, 256) };
            // Make sure Concat doesn't accidentally swap around the sources, e.g. [3, 4], [1, 2] should not become [1..4]
            yield return new object[] { Enumerable.Range(0, 500).Select(i => Enumerable.Repeat(i, 1)).Reverse(), Enumerable.Range(0, 500).Reverse() };
        }

        [Fact]
        public void CountOfConcatIteratorShouldThrowExceptionOnIntegerOverflow()
        {
            var supposedlyLargeCollection = new DelegateBasedCollection<int> { CountWorker = () => int.MaxValue };
            var tinyCollection = new DelegateBasedCollection<int> { CountWorker = () => 1 };

            // We need to use checked arithmetic summing up the collections' counts.
            Assert.Throws<OverflowException>(() => supposedlyLargeCollection.Concat(tinyCollection).Count());
            Assert.Throws<OverflowException>(() => tinyCollection.Concat(tinyCollection).Concat(supposedlyLargeCollection).Count());
            Assert.Throws<OverflowException>(() => tinyCollection.Concat(tinyCollection).Concat(tinyCollection).Concat(supposedlyLargeCollection).Count());
        }
    }
}
