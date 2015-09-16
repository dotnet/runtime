// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public partial class ParallelQueryCombinationTests
    {
        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Aggregate_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Aggregate((x, y) => x));
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Aggregate(0, (x, y) => x + y));
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Aggregate(0, (x, y) => x + y, r => r));
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Aggregate(0, (a, x) => a + x, (l, r) => l + r, r => r));
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Aggregate(() => 0, (a, x) => a + x, (l, r) => l + r, r => r));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void All_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).All(x => true));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Any_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Any(x => true));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Average_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Average());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Contains_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Contains(DefaultStart));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Count_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Count());
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).LongCount());
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Count(x => true));
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).LongCount(x => true));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ElementAt_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, (start, count, ignore) => source.Item(start, count).WithCancellation(cs.Token)).ElementAt(0));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ElementAtOrDefault_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, (start, count, ignore) => source.Item(start, count).WithCancellation(cs.Token)).ElementAtOrDefault(0));
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, (start, count, ignore) => source.Item(start, count).WithCancellation(cs.Token)).ElementAtOrDefault(DefaultSize + 1));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void First_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).First());
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).First(x => false));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void FirstOrDefault_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).FirstOrDefault());
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).FirstOrDefault(x => false));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ForAll_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).ForAll(x => { }));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Last_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Last());
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Last(x => true));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void LastOrDefault_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, (start, count, ignore) => source.Item(start, count).WithCancellation(cs.Token)).LastOrDefault());
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).LastOrDefault(x => true));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Max_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, (start, count, ignore) => source.Item(start, count).WithCancellation(cs.Token)).Max());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Min_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Min());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SequenceEqual_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).SequenceEqual(ParallelEnumerable.Range(0, 2)));
            Functions.AssertIsCanceled(cs, () => ParallelEnumerable.Range(0, 2).SequenceEqual(operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item)));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Single_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Single());
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Single(x => false));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SingleOrDefault_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).SingleOrDefault());
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).SingleOrDefault(x => false));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Sum_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).Sum());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ToArray_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).ToArray());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ToDictionary_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).ToDictionary(x => x));
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).ToDictionary(x => x, y => y));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ToList_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).ToList());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ToLookup_OperationCanceledException_PreCanceled(LabeledOperation source, LabeledOperation operation)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).ToLookup(x => x));
            Functions.AssertIsCanceled(cs, () => operation.Item(DefaultStart, DefaultSize, source.Append(WithCancellation(cs.Token)).Item).ToLookup(x => x, y => y));
        }
    }
}
