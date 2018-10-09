// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static partial class ParallelQueryCombinationTests
    {
        private const int EventualCancellationSize = 128;

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Aggregate_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Aggregate((i, j) => j));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(0, (i, j) => j));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(0, (i, j) => j, i => i));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(0, (i, j) => j, (i, j) => i, i => i));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(() => 0, (i, j) => j, (i, j) => i, i => i));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Aggregate_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Aggregate((i, j) => j));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(0, (i, j) => j));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(0, (i, j) => j, i => i));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(0, (i, j) => j, (i, j) => i, i => i));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(() => 0, (i, j) => j, (i, j) => i, i => i));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Aggregate((i, j) => j));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(0, (i, j) => j));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(0, (i, j) => j, i => i));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(0, (i, j) => j, (i, j) => i, i => i));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Aggregate(() => 0, (i, j) => j, (i, j) => i, i => i));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Aggregate_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Aggregate((x, y) => x));
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Aggregate(0, (x, y) => x + y));
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Aggregate(0, (x, y) => x + y, r => r));
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Aggregate(0, (a, x) => a + x, (l, r) => l + r, r => r));
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Aggregate(() => 0, (a, x) => a + x, (l, r) => l + r, r => r));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void All_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).All(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void All_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).All(x => true));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).All(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void All_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).All(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Any_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Any(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Any_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Any(x => false));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Any(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Any_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Any());
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Any(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Average_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average());
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (int?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (long)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (long?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (float)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (float?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (double)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (double?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (decimal)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (decimal?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Average_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average());
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (int?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (long)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (long?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (float)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (float?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (double)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (double?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (decimal)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (decimal?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average());
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (int?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (long)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (long?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (float)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (float?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (double)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (double?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (decimal)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Average(x => (decimal?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Average_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Average());
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Contains_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Contains(-1));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Contains_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Contains(-1));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Contains(-1));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Contains_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Contains(DefaultStart));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Count_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Count());
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).LongCount());
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Count(x => true));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).LongCount(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Count_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Count());
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).LongCount());
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Count(x => true));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).LongCount(x => true));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Count());
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).LongCount());
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Count(x => true));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).LongCount(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Count_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Count());
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).LongCount());
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Count(x => true));
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).LongCount(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ElementAt_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).ElementAt(int.MaxValue));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ElementAt_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).ElementAt(int.MaxValue));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).ElementAt(int.MaxValue));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ElementAt_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ElementAt(0));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ElementAtOrDefault_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).ElementAt(int.MaxValue));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ElementAtOrDefault_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).ElementAtOrDefault(int.MaxValue));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).ElementAtOrDefault(int.MaxValue));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ElementAtOrDefault_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ElementAtOrDefault(0));
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ElementAtOrDefault(DefaultSize + 1));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void First_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).First(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void First_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).First(x => false));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).First(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void First_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).First());
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).First(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void FirstOrDefault_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).FirstOrDefault(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void FirstOrDefault_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).FirstOrDefault(x => false));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).FirstOrDefault(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void FirstOrDefault_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).FirstOrDefault());
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).FirstOrDefault(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void ForAll_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).ForAll(x => { }));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void ForAll_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).ForAll(x => { }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).ForAll(x => { }));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void ForAll_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ForAll(x => { }));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ForEach_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => { foreach (int i in operation.Item(source, canceler)) ; });
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ForEach_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => { foreach (int i in operation.Item(source, canceler)) ; });
            AssertThrows.SameTokenNotCanceled((source, canceler) => { foreach (int i in operation.Item(source, canceler)) ; });
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ForEach_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => { foreach (int i in operation.Item(source, () => { })) ; });
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void Last_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Last());
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Last(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void Last_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Last());
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Last(x => true));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Last());
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Last(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void Last_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Last());
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Last(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void LastOrDefault_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).LastOrDefault());
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).LastOrDefault(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void LastOrDefault_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).LastOrDefault());
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).LastOrDefault(x => true));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).LastOrDefault());
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).LastOrDefault(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void LastOrDefault_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).LastOrDefault());
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).LastOrDefault(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Max_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max());

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (int)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (int?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (long)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (long?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (float)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (float?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (double)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (double?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (decimal)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (decimal?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Max_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max());

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (int)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (int?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (long)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (long?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (float)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (float?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (double)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (double?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (decimal)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (decimal?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max());

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (int)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (int?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (long)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (long?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (float)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (float?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (double)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (double?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (decimal)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Max(x => (decimal?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Max_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Max());
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Min_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min());

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (int)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (int?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (long)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (long?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (float)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (float?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (double)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (double?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (decimal)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (decimal?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Min_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min());

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (int)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (int?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (long)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (long?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (float)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (float?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (double)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (double?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (decimal)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (decimal?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min());

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (int)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (int?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (long)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (long?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (float)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (float?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (double)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (double?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (decimal)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Min(x => (decimal?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Min_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Min());
        }

        [Theory]
        [ActiveIssue(21876, TestPlatforms.Linux)]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void SequenceEqual_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).SequenceEqual(ParallelEnumerable.Range(0, EventualCancellationSize).AsOrdered()));
            AssertThrows.EventuallyCanceled((source, canceler) => ParallelEnumerable.Range(0, EventualCancellationSize).AsOrdered().SequenceEqual(operation.Item(source, canceler)));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void SequenceEqual_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            SequenceEqual_AssertAggregateAlternateCanceled((token, canceler) => WithCancellation(token, canceler, operation).SequenceEqual(ParallelEnumerable.Range(0, 128).AsOrdered()));
            SequenceEqual_AssertAggregateAlternateCanceled((token, canceler) => ParallelEnumerable.Range(0, 128).AsOrdered().SequenceEqual(WithCancellation(token, canceler, operation)));
            SequenceEqual_AssertAggregateNotCanceled((token, canceler) => WithCancellation(token, canceler, operation).SequenceEqual(ParallelEnumerable.Range(0, 128).AsOrdered()));
            SequenceEqual_AssertAggregateNotCanceled((token, canceler) => ParallelEnumerable.Range(0, 128).AsOrdered().SequenceEqual(WithCancellation(token, canceler, operation)));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void SequenceEqual_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).SequenceEqual(ParallelEnumerable.Range(0, 2)));
            AssertThrows.AlreadyCanceled(source => ParallelEnumerable.Range(0, 2).SequenceEqual(operation.Item(source, () => { })));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Single_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Single(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Single_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Single(x => false));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Single(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Single_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Single());
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Single(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void SingleOrDefault_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).SingleOrDefault(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void SingleOrDefault_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).SingleOrDefault(x => false));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).SingleOrDefault(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void SingleOrDefault_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).SingleOrDefault());
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).SingleOrDefault(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Sum_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum());
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (int?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (long)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (long?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (float)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (float?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (double)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (double?)x));

            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (decimal)x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (decimal?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Sum_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum());
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (int?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (long)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (long?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (float)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (float?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (double)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (double?)x));

            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (decimal)x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (decimal?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum());
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (int?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (long)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (long?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (float)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (float?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (double)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (double?)x));

            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (decimal)x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).Sum(x => (decimal?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void Sum_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).Sum());
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ToArray_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).ToArray());
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ToArray_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).ToArray());
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).ToArray());
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Core bug fix https://github.com/dotnet/corefx/pull/2307")]
        public static void ToArray_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ToArray());
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void ToDictionary_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).ToDictionary(x => x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).ToDictionary(x => x, y => y));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void ToDictionary_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).ToDictionary(x => x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).ToDictionary(x => x, y => y));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).ToDictionary(x => x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).ToDictionary(x => x, y => y));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        public static void ToDictionary_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ToDictionary(x => x));
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ToDictionary(x => x, y => y));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ToList_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).ToList());
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ToList_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).ToList());
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).ToList());
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ToList_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ToList());
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ToLookup_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).ToLookup(x => x));
            AssertThrows.EventuallyCanceled((source, canceler) => operation.Item(source, canceler).ToLookup(x => x, y => y));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ToLookup_AggregateException_Wraps_OperationCanceledException(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).ToLookup(x => x));
            AssertThrows.OtherTokenCanceled((source, canceler) => operation.Item(source, canceler).ToLookup(x => x, y => y));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).ToLookup(x => x));
            AssertThrows.SameTokenNotCanceled((source, canceler) => operation.Item(source, canceler).ToLookup(x => x, y => y));
        }

        [Theory]
        [MemberData(nameof(UnaryCancelingOperators))]
        [MemberData(nameof(BinaryCancelingOperators))]
        [MemberData(nameof(OrderCancelingOperators))]
        public static void ToLookup_OperationCanceledException_PreCanceled(Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ToLookup(x => x));
            AssertThrows.AlreadyCanceled(source => operation.Item(source, () => { }).ToLookup(x => x, y => y));
        }

        private static ParallelQuery<int> WithCancellation(CancellationToken token, Action canceler, Labeled<Func<ParallelQuery<int>, Action, ParallelQuery<int>>> operation)
        {
            return operation.Item(ParallelEnumerable.Range(DefaultStart, EventualCancellationSize).WithCancellation(token), canceler);
        }

        private static void SequenceEqual_AssertAggregateAlternateCanceled(Action<CancellationToken, Action> query)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            Action canceler = () => { throw new OperationCanceledException(cs.Token); };

            AggregateException outer = Assert.Throws<AggregateException>(() => query(new CancellationTokenSource().Token, canceler));
            Exception inner = Assert.Single(outer.InnerExceptions);
            AggregateException ae = Assert.IsType<AggregateException>(inner);
            Assert.All(ae.InnerExceptions, e => Assert.IsType<OperationCanceledException>(e));
        }

        private static void SequenceEqual_AssertAggregateNotCanceled(Action<CancellationToken, Action> query)
        {
            CancellationToken token = new CancellationTokenSource().Token;
            Action canceler = () => { throw new OperationCanceledException(token); };

            AggregateException outer = Assert.Throws<AggregateException>(() => query(token, canceler));
            Exception inner = Assert.Single(outer.InnerExceptions);
            AggregateException ae = Assert.IsType<AggregateException>(inner);
            Assert.All(ae.InnerExceptions, e => Assert.IsType<OperationCanceledException>(e));
        }
    }
}
