// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static partial class ParallelQueryCombinationTests
    {
        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void Aggregate_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate((x, y) => x));
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(0, (x, y) => x + y));
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(0, (x, y) => x + y, r => r));
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(0, (a, x) => a + x, (l, r) => l + r, r => r));
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(() => 0, (a, x) => a + x, (l, r) => l + r, r => r));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void All_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).All(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void Any_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Any(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void Average_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Average());
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void Contains_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Contains(DefaultStart + DefaultSize));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void Count_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Count());
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Count(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void ElementAt_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ElementAt(DefaultSize - 1));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void ElementAtOrDefault_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ElementAtOrDefault(DefaultSize - 1));
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ElementAtOrDefault(DefaultSize + 1));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public static void First_Predicate_None(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Throws<InvalidOperationException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).First(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void First_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            if (operation.ToString().Contains("Concat-Left"))
            {
                // The vast majority of the time, the operation returns a result instead of failing.
                // Sufficient cores on a test machine may make the optimizer start enumerating the results.
                int? result = null;
                var exception = Record.Exception(() => { result = operation.Item(DefaultStart, DefaultSize, source.Item).First(); });
                if (result.HasValue)
                {
                    Assert.Null(exception);
                    Assert.InRange(result.Value, DefaultStart, DefaultStart + DefaultSize);
                }
                else
                {
                    Assert.NotNull(exception);
                    var ae = Assert.IsType<AggregateException>(exception);
                    Assert.All(ae.InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));
                }
            }
            else
            {
                AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).First());
            }

            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).First(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void FirstOrDefault_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            if (operation.ToString().Contains("Concat-Left"))
            {
                // The vast majority of the time, the operation returns a result instead of failing.
                // Sufficient cores on a test machine may make the optimizer start enumerating the results.
                int? result = null;
                var exception = Record.Exception(() => { result = operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault(); });
                if (result.HasValue)
                {
                    Assert.Null(exception);
                    Assert.InRange(result.Value, DefaultStart, DefaultStart + DefaultSize);
                }
                else
                {
                    Assert.NotNull(exception);
                    var ae = Assert.IsType<AggregateException>(exception);
                    Assert.All(ae.InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));
                }
            }
            else
            {
                AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault());
            }

            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void ForAll_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ForAll(x => { }));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void GetEnumerator_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            IEnumerator<int> enumerator = operation.Item(DefaultStart, DefaultSize, source.Item).GetEnumerator();
            // Spin until concat hits
            // Union-Left needs to spin more than once rarely.
            if (operation.ToString().StartsWith("Concat") || operation.ToString().StartsWith("Union-Left"))
            {
                AssertThrows.Wrapped<DeliberateTestException>(() => { while (enumerator.MoveNext()) ; });
            }
            else
            {
                AssertThrows.Wrapped<DeliberateTestException>(() => enumerator.MoveNext());
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
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public static void Last_Predicate_None(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Throws<InvalidOperationException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Last(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void Last_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Last());
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Last(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void LastOrDefault_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault());
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void LongCount_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).LongCount());
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).LongCount(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void Max_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Max());
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void Min_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Min());
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void SequenceEqual_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            // Sequence equal double wraps queries that throw.
            ThrowsWrapped(() => operation.Item(DefaultStart, DefaultSize, source.Item).SequenceEqual(ParallelEnumerable.Range(DefaultStart, DefaultSize).AsOrdered()));
            ThrowsWrapped(() => ParallelEnumerable.Range(DefaultStart, DefaultSize).AsOrdered().SequenceEqual(operation.Item(DefaultStart, DefaultSize, source.Item)));
        }

        private static void ThrowsWrapped(Action query)
        {
            AggregateException outer = Assert.Throws<AggregateException>(query);
            Exception inner = Assert.Single(outer.InnerExceptions);
            AggregateException ae = Assert.IsType<AggregateException>(inner);
            Assert.All(ae.InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void Single_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, 2, source.Item).Single());
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, 2, source.Item).Single(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void SingleOrDefault_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, 2, source.Item).SingleOrDefault());
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, 2, source.Item).SingleOrDefault(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        public static void Sum_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).Sum());
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void ToArray_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToArray());
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void ToDictionary_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToDictionary(x => x));
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToDictionary(x => x, y => y));
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void ToList_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToList());
        }

        [Theory]
        [MemberData(nameof(UnaryFailingOperators))]
        [MemberData(nameof(BinaryFailingOperators))]
        [MemberData(nameof(OrderFailingOperators))]
        public static void ToLookup_AggregateException(Labeled<Operation> source, Labeled<Operation> operation)
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToLookup(x => x));
            AssertThrows.Wrapped<DeliberateTestException>(() => operation.Item(DefaultStart, DefaultSize, source.Item).ToLookup(x => x, y => y));
        }
    }
}
