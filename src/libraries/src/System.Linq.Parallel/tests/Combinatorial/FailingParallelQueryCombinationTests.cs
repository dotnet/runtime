// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public partial class ParallelQueryCombinationTests
    {
        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void Aggregate_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate((x, y) => x));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(0, (x, y) => x + y));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(0, (x, y) => x + y, r => r));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(0, (a, x) => a + x, (l, r) => l + r, r => r));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(() => 0, (a, x) => a + x, (l, r) => l + r, r => r));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void All_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).All(x => true));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void Any_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Any(x => false));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void Average_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Average());
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void Contains_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Contains(DefaultStart + DefaultSize));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void Count_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Count());
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Count(x => true));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void ElementAt_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ElementAt(DefaultSize - 1));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void ElementAtOrDefault_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ElementAtOrDefault(DefaultSize - 1));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ElementAtOrDefault(DefaultSize + 1));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void First_Predicate_None(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Throws<InvalidOperationException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).First(x => false));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void First_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            // Concat seems able to return the first element when the left query does not fail ("first" query).
            // This test might be flaky in the case that it decides to run the right query too...
            if (operation.ToString().Contains("Concat-Left"))
            {
                Assert.InRange(operation.Item(DefaultStart, DefaultSize, source.Item).First(), DefaultStart, DefaultStart + DefaultSize);
            }
            else
            {
                Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).First());
            }

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).First(x => false));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void FirstOrDefault_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            // Concat seems able to return the first element when the left query does not fail ("first" query).
            // This test might be flaky in the case that it decides to run the right query too...
            if (operation.ToString().Contains("Concat-Left"))
            {
                Assert.InRange(operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault(), DefaultStart, DefaultStart + DefaultSize);
            }
            else
            {
                Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault());
            }

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault(x => false));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void ForAll_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ForAll(x => { }));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void GetEnumerator_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            IEnumerator<int> enumerator = operation.Item(DefaultStart, DefaultSize, source.Item).GetEnumerator();
            // Spin until concat hits
            // Union-Left needs to spin more than once rarely.
            if (operation.ToString().StartsWith("Concat") || operation.ToString().StartsWith("Union-Left:ParallelEnumerable"))
            {
                Functions.AssertThrowsWrapped<DeliberateTestException>(() => { while (enumerator.MoveNext()) ; });
            }
            else
            {
                Functions.AssertThrowsWrapped<DeliberateTestException>(() => enumerator.MoveNext());
            }

            if (operation.ToString().StartsWith("OrderBy") || operation.ToString().StartsWith("ThenBy"))
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            }
            else
            {
                Assert.False(enumerator.MoveNext());
            }
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Last_Predicate_None(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Throws<InvalidOperationException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Last(x => false));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void Last_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Last());
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Last(x => true));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void LastOrDefault_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault());
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault(x => true));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void LongCount_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).LongCount());
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).LongCount(x => true));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void Max_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Max());
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void Min_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Min());
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void SequenceEqual_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            // Sequence equal double wraps queries that throw.
            ThrowsWrapped(() => operation.Item(DefaultStart, DefaultSize, source.Item).SequenceEqual(ParallelEnumerable.Range(DefaultStart, DefaultSize).AsOrdered()));
            ThrowsWrapped(() => ParallelEnumerable.Range(DefaultStart, DefaultSize).AsOrdered().SequenceEqual(operation.Item(DefaultStart, DefaultSize, source.Item)));
        }

        private static void ThrowsWrapped(Action query)
        {
            AggregateException outer = Assert.Throws<AggregateException>(query);
            Assert.All(outer.InnerExceptions, inner =>
            {
                Assert.IsType<AggregateException>(inner);
                Assert.All(((AggregateException)inner).InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));
            });
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void Single_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, 2, source.Item).Single());
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, 2, source.Item).Single(x => true));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void SingleOrDefault_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, 2, source.Item).SingleOrDefault());
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, 2, source.Item).SingleOrDefault(x => true));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        public static void Sum_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Sum());
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void ToArray_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToArray());
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void ToDictionary_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToDictionary(x => x));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToDictionary(x => x, y => y));
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void ToList_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToList());
        }

        [Theory]
        [MemberData("UnaryFailingOperators")]
        [MemberData("BinaryFailingOperators")]
        [MemberData("OrderFailingOperators")]
        public static void ToLookup_AggregateException(LabeledOperation source, LabeledOperation operation)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToLookup(x => x));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToLookup(x => x, y => y));
        }
    }
}
