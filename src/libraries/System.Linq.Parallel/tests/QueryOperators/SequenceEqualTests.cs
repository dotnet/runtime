// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class SequenceEqualTests
    {
        private const int DuplicateFactor = 8;

        //
        // SequenceEqual
        //
        public static IEnumerable<object[]> SequenceEqualData(int[] counts)
        {
            foreach (object[] left in Sources.Ranges(counts.Cast<int>()))
            {
                foreach (object[] right in Sources.Ranges(new[] { (int)left[1] }))
                {
                    yield return new object[] { left[0], right[0], right[1] };
                }
            }
        }

        public static IEnumerable<object[]> SequenceEqualUnequalSizeData(int[] counts)
        {
            foreach (object[] left in Sources.Ranges(counts.Cast<int>()))
            {
                foreach (object[] right in Sources.Ranges(new[] { 1, ((int)left[1] - 1) / 2 + 1, (int)left[1] * 2 + 1 }.Distinct()))
                {
                    yield return new object[] { left[0], left[1], right[0], right[1] };
                }
            }
        }

        public static IEnumerable<object[]> SequenceEqualUnequalData(int[] counts)
        {
            foreach (object[] left in Sources.Ranges(counts.Cast<int>()))
            {
                Func<int, IEnumerable<int>> items = x => new[] { 0, x / 8, x / 2, x * 7 / 8, x - 1 }.Distinct();
                foreach (object[] right in Sources.Ranges(new[] { (int)left[1] }, items))
                {
                    yield return new object[] { left[0], right[0], right[1], right[2] };
                }
            }
        }

        [Theory]
        [MemberData("SequenceEqualData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SequenceEqual(Labeled<ParallelQuery<int>> left, Labeled<ParallelQuery<int>> right, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            Assert.True(leftQuery.SequenceEqual(rightQuery));
            Assert.True(rightQuery.SequenceEqual(leftQuery));
            Assert.True(leftQuery.SequenceEqual(leftQuery));
        }

        [Theory]
        [OuterLoop]
        [MemberData("SequenceEqualData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void SequenceEqual_Longrunning(Labeled<ParallelQuery<int>> left, Labeled<ParallelQuery<int>> right, int count)
        {
            SequenceEqual(left, right, count);
        }

        [Theory]
        [MemberData("SequenceEqualData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SequenceEqual_CustomComparator(Labeled<ParallelQuery<int>> left, Labeled<ParallelQuery<int>> right, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            Assert.True(leftQuery.SequenceEqual(rightQuery, new ModularCongruenceComparer(DuplicateFactor)));
            Assert.True(rightQuery.SequenceEqual(leftQuery, new ModularCongruenceComparer(DuplicateFactor)));
            Assert.True(leftQuery.SequenceEqual(leftQuery, new ModularCongruenceComparer(DuplicateFactor)));

            ParallelQuery<int> repeating = Enumerable.Range(0, (count + (DuplicateFactor - 1)) / DuplicateFactor).SelectMany(x => Enumerable.Range(0, DuplicateFactor)).Take(count).AsParallel().AsOrdered();
            Assert.True(leftQuery.SequenceEqual(repeating, new ModularCongruenceComparer(DuplicateFactor)));
            Assert.True(rightQuery.SequenceEqual(repeating, new ModularCongruenceComparer(DuplicateFactor)));
            Assert.True(repeating.SequenceEqual(rightQuery, new ModularCongruenceComparer(DuplicateFactor)));
            Assert.True(repeating.SequenceEqual(leftQuery, new ModularCongruenceComparer(DuplicateFactor)));
        }

        [Theory]
        [OuterLoop]
        [MemberData("SequenceEqualData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void SequenceEqual_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> left, Labeled<ParallelQuery<int>> right, int count)
        {
            SequenceEqual_CustomComparator(left, right, count);
        }

        [Theory]
        [MemberData("SequenceEqualUnequalSizeData", (object)(new int[] { 0, 4, 16 }))]
        public static void SequenceEqual_UnequalSize(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;
            Assert.False(leftQuery.SequenceEqual(rightQuery));
            Assert.False(rightQuery.SequenceEqual(leftQuery));
            Assert.False(leftQuery.SequenceEqual(rightQuery, new ModularCongruenceComparer(2)));
            Assert.False(rightQuery.SequenceEqual(leftQuery, new ModularCongruenceComparer(2)));
        }

        [Theory]
        [OuterLoop]
        [MemberData("SequenceEqualUnequalSizeData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void SequenceEqual_UnequalSize_Longrunning(Labeled<ParallelQuery<int>> left, int leftCount, Labeled<ParallelQuery<int>> right, int rightCount)
        {
            SequenceEqual_UnequalSize(left, leftCount, right, rightCount);
        }

        [Theory]
        [MemberData("SequenceEqualUnequalData", (object)(new int[] { 1, 2, 16 }))]
        public static void SequenceEqual_Unequal(Labeled<ParallelQuery<int>> left, Labeled<ParallelQuery<int>> right, int count, int item)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item.Select(x => x == item ? -1 : x);

            Assert.False(leftQuery.SequenceEqual(rightQuery));
            Assert.False(rightQuery.SequenceEqual(leftQuery));
            Assert.True(leftQuery.SequenceEqual(leftQuery));
        }

        [Theory]
        [OuterLoop]
        [MemberData("SequenceEqualUnequalData", (object)(new int[] { 1024 * 4, 1024 * 128 }))]
        public static void SequenceEqual_Unequal_Longrunning(Labeled<ParallelQuery<int>> left, Labeled<ParallelQuery<int>> right, int count, int item)
        {
            SequenceEqual_Unequal(left, right, count, item);
        }

        public static void SequenceEqual_NotSupportedException()
        {
#pragma warning disable 618
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).SequenceEqual(Enumerable.Range(0, 1)));
            Assert.Throws<NotSupportedException>(() => ParallelEnumerable.Range(0, 1).SequenceEqual(Enumerable.Range(0, 1), null));
#pragma warning restore 618
        }

        [Theory]
        [MemberData("SequenceEqualUnequalData", (object)(new int[] { 4 }))]
        public static void SequenceEqual_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> left, Labeled<ParallelQuery<int>> right, int count, int item)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => left.Item.WithCancellation(cs.Token).SequenceEqual(right.Item));
            Functions.AssertIsCanceled(cs, () => left.Item.WithCancellation(cs.Token).SequenceEqual(right.Item, new ModularCongruenceComparer(1)));

            Functions.AssertIsCanceled(cs, () => left.Item.SequenceEqual(right.Item.WithCancellation(cs.Token)));
            Functions.AssertIsCanceled(cs, () => left.Item.SequenceEqual(right.Item.WithCancellation(cs.Token), new ModularCongruenceComparer(1)));
        }

        [Theory]
        [MemberData("SequenceEqualUnequalData", (object)(new int[] { 4 }))]
        public static void SequenceEqual_AggregateException(Labeled<ParallelQuery<int>> left, Labeled<ParallelQuery<int>> right, int count, int item)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => left.Item.SequenceEqual(right.Item, new FailingEqualityComparer<int>()));
        }

        [Fact]
        // Should not get the same setting from both operands.
        public static void SequenceEqual_NoDuplicateSettings()
        {
            CancellationToken t = new CancellationTokenSource().Token;
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithCancellation(t).SequenceEqual(ParallelEnumerable.Range(0, 1).WithCancellation(t)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1).SequenceEqual(ParallelEnumerable.Range(0, 1).WithDegreeOfParallelism(1)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default).SequenceEqual(ParallelEnumerable.Range(0, 1).WithExecutionMode(ParallelExecutionMode.Default)));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default).SequenceEqual(ParallelEnumerable.Range(0, 1).WithMergeOptions(ParallelMergeOptions.Default)));
        }

        [Fact]
        public static void SequenceEqual_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).SequenceEqual(ParallelEnumerable.Range(0, 1)));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).SequenceEqual((ParallelQuery<int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).SequenceEqual(ParallelEnumerable.Range(0, 1), EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).SequenceEqual((ParallelQuery<int>)null, EqualityComparer<int>.Default));
        }

        [Theory]
        [MemberData("SequenceEqualData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SequenceEqual_DisposeException(Labeled<ParallelQuery<int>> left, Labeled<ParallelQuery<int>> right, int count)
        {
            ParallelQuery<int> leftQuery = left.Item;
            ParallelQuery<int> rightQuery = right.Item;

            AggregateException ex = Assert.Throws<AggregateException>(() => leftQuery.SequenceEqual(new DisposeExceptionEnumerable<int>(rightQuery).AsParallel()));
            Assert.All(ex.InnerExceptions, e => Assert.IsType<TestDisposeException>(e));

            ex = Assert.Throws<AggregateException>(() => new DisposeExceptionEnumerable<int>(leftQuery).AsParallel().SequenceEqual(rightQuery));
            Assert.All(ex.InnerExceptions, e => Assert.IsType<TestDisposeException>(e));
        }

        private class DisposeExceptionEnumerable<T> : IEnumerable<T>
        {
            private IEnumerable<T> _enumerable;

            public DisposeExceptionEnumerable(IEnumerable<T> enumerable)
            {
                _enumerable = enumerable;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new DisposeExceptionEnumerator(_enumerable.GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class DisposeExceptionEnumerator : IEnumerator<T>
            {
                private IEnumerator<T> _enumerator;

                public DisposeExceptionEnumerator(IEnumerator<T> enumerator)
                {
                    _enumerator = enumerator;
                }

                public T Current
                {
                    get { return _enumerator.Current; }
                }

                public void Dispose()
                {
                    throw new TestDisposeException();
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    return _enumerator.MoveNext();
                }

                public void Reset()
                {
                    _enumerator.Reset();
                }
            }
        }

        private class TestDisposeException : Exception
        {
        }
    }
}
