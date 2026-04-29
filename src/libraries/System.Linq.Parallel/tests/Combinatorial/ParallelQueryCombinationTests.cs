// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public partial class ParallelQueryCombinationTests
    {
        public ParallelQueryCombinationTests()
        {
            Assert.SkipUnless(ParallelQueryCombinationTests.RunSlowTests, "ConditionalClass: ParallelQueryCombinationTests.RunSlowTests");
        }

        // On ARM platforms, many available cores make this unbearably slow: https://github.com/dotnet/runtime/issues/29123
        public static bool RunSlowTests => PlatformDetection.IsNotArmNorArm64Process || Environment.ProcessorCount <= 8;

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Aggregate(Labeled<Operation> operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, DefaultSource).Aggregate((x, y) => x + y));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Aggregate_Seed(Labeled<Operation> operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, DefaultSource).Aggregate(0, (x, y) => x + y));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Aggregate_Result(Labeled<Operation> operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, DefaultSource).Aggregate(0, (x, y) => x + y, r => r));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Aggregate_Accumulator(Labeled<Operation> operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, DefaultSource).Aggregate(0, (a, x) => a + x, (l, r) => l + r, r => r));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Aggregate_SeedFactory(Labeled<Operation> operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, DefaultSource).Aggregate(() => 0, (a, x) => a + x, (l, r) => l + r, r => r));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void All_False(Labeled<Operation> operation)
        {
            Assert.False(operation.Item(DefaultStart, DefaultSize, DefaultSource).All(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void All_True(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.True(operation.Item(DefaultStart, DefaultSize, DefaultSource).All(x => seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Any_False(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.False(operation.Item(DefaultStart, DefaultSize, DefaultSource).Any(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Any_True(Labeled<Operation> operation)
        {
            Assert.True(operation.Item(DefaultStart, DefaultSize, DefaultSource).Any(x => true));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Average(Labeled<Operation> operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize) / (double)DefaultSize,
                operation.Item(DefaultStart, DefaultSize, DefaultSource).Average());
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Average_Nullable(Labeled<Operation> operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize) / (double?)DefaultSize,
                operation.Item(DefaultStart, DefaultSize, DefaultSource).Average(x => (int?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Cast(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int? i in operation.Item(DefaultStart, DefaultSize, source.Item).Cast<int?>())
            {
                Assert.True(i.HasValue);
                Assert.Equal(seen++, i.Value);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Cast_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Cast<int?>().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Concat(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Concat(Operation left, Operation right)
            {
                int seen = DefaultStart;
                foreach (int i in left(DefaultStart, DefaultSize / 2, source.Item)
                    .Concat(right(DefaultStart + DefaultSize / 2, DefaultSize / 2, source.Item)))
                {
                    Assert.Equal(seen++, i);
                }
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Concat(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            Concat(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Concat_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Concat(Operation left, Operation right)
            {
                int seen = DefaultStart;
                Assert.All(
                    left(DefaultStart, DefaultSize / 2, source.Item)
                        .Concat(right(DefaultStart + DefaultSize / 2, DefaultSize / 2, source.Item)).ToList(),
                    x => Assert.Equal(seen++, x)
                    );
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Concat(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            Concat(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Contains_True(Labeled<Operation> operation)
        {
            Assert.True(operation.Item(DefaultStart, DefaultSize, DefaultSource).Contains(DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Contains_False(Labeled<Operation> operation)
        {
            Assert.False(operation.Item(DefaultStart, DefaultSize, DefaultSource).Contains(DefaultStart + DefaultSize));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Count_Elements(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultSize, operation.Item(DefaultStart, DefaultSize, DefaultSource).Count());
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Count_Predicate_Some(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, DefaultSource).Count(x => x < DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Count_Predicate_None(Labeled<Operation> operation)
        {
            Assert.Equal(0, operation.Item(DefaultStart, DefaultSize, DefaultSource).Count(x => x < DefaultStart));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void DefaultIfEmpty(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).DefaultIfEmpty())
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void DefaultIfEmpty_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).DefaultIfEmpty().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Distinct(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            ParallelQuery<int> query = operation.Item(DefaultStart * 2, DefaultSize * 2, source.Item).Select(x => x / 2).Distinct();
            foreach (int i in query)
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Distinct_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            ParallelQuery<int> query = operation.Item(DefaultStart * 2, DefaultSize * 2, source.Item).Select(x => x / 2).Distinct();
            Assert.All(query.ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void ElementAt(Labeled<Operation> source, Labeled<Operation> operation)
        {
            ParallelQuery<int> query = operation.Item(DefaultStart, DefaultSize, source.Item);

            int seen = DefaultStart;
            for (int i = 0; i < DefaultSize; i++)
            {
                Assert.Equal(seen++, query.ElementAt(i));
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void ElementAtOrDefault(Labeled<Operation> source, Labeled<Operation> operation)
        {
            ParallelQuery<int> query = operation.Item(DefaultStart, DefaultSize, source.Item);

            int seen = DefaultStart;
            for (int i = 0; i < DefaultSize; i++)
            {
                Assert.Equal(seen++, query.ElementAtOrDefault(i));
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
            Assert.Equal(default(int), query.ElementAtOrDefault(-1));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Except(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Except(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<int> query = left(DefaultStart, DefaultSize + DefaultSize / 2, source.Item)
                    .Except(right(DefaultStart + DefaultSize, DefaultSize, source.Item));
                foreach (int i in query)
                {
                    Assert.Equal(seen++, i);
                }
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Except(operation.Item, DefaultSource);
            Except(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Except_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Except(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<int> query = left(DefaultStart, DefaultSize + DefaultSize / 2, source.Item)
                    .Except(right(DefaultStart + DefaultSize, DefaultSize, source.Item));
                Assert.All(query.ToList(), x => Assert.Equal(seen++, x));
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Except(operation.Item, DefaultSource);
            Except(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void First(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, DefaultSize, source.Item).First());
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void First_Predicate(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart + DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, source.Item).First(x => x >= DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void FirstOrDefault(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault());
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void FirstOrDefault_Predicate(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart + DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault(x => x >= DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void FirstOrDefault_Predicate_None(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(default(int), operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void ForAll(Labeled<Operation> source, Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            operation.Item(DefaultStart, DefaultSize, source.Item).ForAll(x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void GetEnumerator(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            IEnumerator<int> enumerator = operation.Item(DefaultStart, DefaultSize, source.Item).GetEnumerator();

            while (enumerator.MoveNext())
            {
                int current = enumerator.Current;
                Assert.Equal(seen++, current);
                Assert.Equal(current, enumerator.Current);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);

            Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void GroupBy(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seenKey = DefaultStart / GroupFactor;
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, source.Item).GroupBy(x => x / GroupFactor))
            {
                Assert.Equal(seenKey++, group.Key);
                int seenElement = group.Key * GroupFactor;
                Assert.All(group, x => Assert.Equal(seenElement++, x));
                Assert.Equal(Math.Min((group.Key + 1) * GroupFactor, DefaultStart + DefaultSize), seenElement);
            }
            Assert.Equal((DefaultSize + (GroupFactor - 1)) / GroupFactor + 1, seenKey);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void GroupBy_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seenKey = DefaultStart / GroupFactor;
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, source.Item).GroupBy(x => x / GroupFactor).ToList())
            {
                Assert.Equal(seenKey++, group.Key);
                int seenElement = group.Key * GroupFactor;
                Assert.All(group, x => Assert.Equal(seenElement++, x));
                Assert.Equal(Math.Min((group.Key + 1) * GroupFactor, DefaultStart + DefaultSize), seenElement);
            }
            Assert.Equal((DefaultSize + (GroupFactor - 1)) / GroupFactor + 1, seenKey);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void GroupBy_ElementSelector(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seenKey = DefaultStart / GroupFactor;
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, source.Item).GroupBy(x => x / GroupFactor, y => -y))
            {
                Assert.Equal(seenKey++, group.Key);
                int seenElement = -group.Key * GroupFactor;
                Assert.All(group, x => Assert.Equal(seenElement--, x));
                Assert.Equal(-Math.Min((group.Key + 1) * GroupFactor, DefaultStart + DefaultSize), seenElement);
            }
            Assert.Equal((DefaultSize + (GroupFactor - 1)) / GroupFactor + 1, seenKey);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void GroupBy_ElementSelector_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seenKey = DefaultStart / GroupFactor;
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, source.Item).GroupBy(x => x / GroupFactor, y => -y).ToList())
            {
                Assert.Equal(seenKey++, group.Key);
                int seenElement = -group.Key * GroupFactor;
                Assert.All(group, x => Assert.Equal(seenElement--, x));
                Assert.Equal(-Math.Min((group.Key + 1) * GroupFactor, DefaultStart + DefaultSize), seenElement);
            }
            Assert.Equal((DefaultSize + (GroupFactor - 1)) / GroupFactor + 1, seenKey);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void GroupJoin(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void GroupJoin(Operation left, Operation right)
            {
                int seenKey = DefaultStart / GroupFactor;
                foreach (KeyValuePair<int, IEnumerable<int>> group in left(DefaultStart / GroupFactor, DefaultSize / GroupFactor, source.Item)
                    .GroupJoin(right(DefaultStart, DefaultSize, source.Item), x => x, y => y / GroupFactor, (k, g) => new KeyValuePair<int, IEnumerable<int>>(k, g)))
                {
                    Assert.Equal(seenKey++, group.Key);
                    int seenElement = group.Key * GroupFactor;
                    Assert.All(group.Value, x => Assert.Equal(seenElement++, x));
                    Assert.Equal((group.Key + 1) * GroupFactor, seenElement);
                }
                Assert.Equal((DefaultStart + DefaultSize) / GroupFactor, seenKey);
            }
            GroupJoin(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            GroupJoin(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void GroupJoin_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void GroupJoin(Operation left, Operation right)
            {
                int seenKey = DefaultStart / GroupFactor;
                foreach (KeyValuePair<int, IEnumerable<int>> group in left(DefaultStart / GroupFactor, DefaultSize / GroupFactor, source.Item)
                    .GroupJoin(right(DefaultStart, DefaultSize, source.Item), x => x, y => y / GroupFactor, (k, g) => new KeyValuePair<int, IEnumerable<int>>(k, g)).ToList())
                {
                    Assert.Equal(seenKey++, group.Key);
                    int seenElement = group.Key * GroupFactor;
                    Assert.All(group.Value, x => Assert.Equal(seenElement++, x));
                    Assert.Equal((group.Key + 1) * GroupFactor, seenElement);
                }
                Assert.Equal((DefaultStart + DefaultSize) / GroupFactor, seenKey);
            }
            GroupJoin(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            GroupJoin(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Intersect(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Intersect(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<int> query = left(DefaultStart - DefaultSize / 2, DefaultSize + DefaultSize / 2, source.Item)
                    .Intersect(right(DefaultStart, DefaultSize + DefaultSize / 2, source.Item));
                foreach (int i in query)
                {
                    Assert.Equal(seen++, i);
                }
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Intersect(operation.Item, DefaultSource);
            Intersect(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Intersect_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Intersect(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<int> query = left(DefaultStart - DefaultSize / 2, DefaultSize + DefaultSize / 2, source.Item)
                    .Intersect(right(DefaultStart, DefaultSize + DefaultSize / 2, source.Item));
                Assert.All(query.ToList(), x => Assert.Equal(seen++, x));
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Intersect(operation.Item, DefaultSource);
            Intersect(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Join(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Join(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<KeyValuePair<int, int>> query = left(DefaultStart / GroupFactor, DefaultSize / GroupFactor, source.Item)
                      .Join(right(DefaultStart, DefaultSize, source.Item), x => x, y => y / GroupFactor, (x, y) => new KeyValuePair<int, int>(x, y));
                foreach (KeyValuePair<int, int> p in query)
                {
                    Assert.Equal(seen++, p.Value);
                    Assert.Equal(p.Key, p.Value / GroupFactor);
                }
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Join(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            Join(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Join_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Join(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<KeyValuePair<int, int>> query = left(DefaultStart / GroupFactor, DefaultSize / GroupFactor, source.Item)
                      .Join(right(DefaultStart, DefaultSize, source.Item), x => x, y => y / GroupFactor, (x, y) => new KeyValuePair<int, int>(x, y));
                foreach (KeyValuePair<int, int> p in query.ToList())
                {
                    Assert.Equal(seen++, p.Value);
                    Assert.Equal(p.Key, p.Value / GroupFactor);
                }
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Join(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            Join(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Last(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart + DefaultSize - 1, operation.Item(DefaultStart, DefaultSize, source.Item).Last());
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Last_Predicate(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart + DefaultSize / 2 - 1, operation.Item(DefaultStart, DefaultSize, source.Item).Last(x => x < DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void LastOrDefault(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart + DefaultSize - 1, operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault());
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void LastOrDefault_Predicate(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart + DefaultSize / 2 - 1, operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault(x => x < DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void LastOrDefault_Predicate_None(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Equal(default(int), operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault(x => false));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void LongCount_Elements(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultSize, operation.Item(DefaultStart, DefaultSize, DefaultSource).LongCount());
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void LongCount_Predicate_Some(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, DefaultSource).LongCount(x => x < DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void LongCount_Predicate_None(Labeled<Operation> operation)
        {
            Assert.Equal(0, operation.Item(DefaultStart, DefaultSize, DefaultSource).LongCount(x => x < DefaultStart));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Max(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart + DefaultSize - 1, operation.Item(DefaultStart, DefaultSize, DefaultSource).Max());
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Max_Nullable(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart + DefaultSize - 1, operation.Item(DefaultStart, DefaultSize, DefaultSource).Max(x => (int?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Min(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, DefaultSize, DefaultSource).Min());
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Min_Nullable(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, DefaultSize, DefaultSource).Min(x => (int?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void OfType(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OfType<int>())
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void OfType_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OfType<int>().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void OfType_Other(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Empty(operation.Item(DefaultStart, DefaultSize, source.Item).OfType<long>());
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void OfType_Other_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.Empty(operation.Item(DefaultStart, DefaultSize, source.Item).OfType<long>().ToList());
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void OrderBy_Initial(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => x))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void OrderBy_Initial_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => x).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void OrderBy_OtherDirection(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => -x))
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void OrderBy_OtherDirection_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => -x).ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void OrderByDescending_Initial(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderByDescending(x => -x))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void OrderByDescending_Initial_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderByDescending(x => -x).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void OrderByDescending_OtherDirection(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderByDescending(x => x))
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void OrderByDescending_OtherDirection_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderByDescending(x => x).ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Reverse(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Reverse())
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Reverse_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Reverse().ToList())
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Select(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Select(x => -x))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Select_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Select(x => -x).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Select_Indexed(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Select((x, index) => { Assert.Equal(DefaultStart + index, x); return -x; }))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Select_Indexed_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Select((x, index) => { Assert.Equal(DefaultStart + index, x); return -x; }).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SelectMany(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x)))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SelectMany_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x)).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SelectMany_Indexed(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { Assert.Equal(index, x); return new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x); }))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SelectMany_Indexed_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { Assert.Equal(index, x); return new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x); }).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SelectMany_ResultSelector(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }, (x, y) => y + -DefaultStart - 2 * x))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SelectMany_ResultSelector_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }, (x, y) => y + -DefaultStart - 2 * x).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SelectMany_Indexed_ResultSelector(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { Assert.Equal(index, x); return new[] { 0, -1 }; }, (x, y) => y + -DefaultStart - 2 * x))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SelectMany_Indexed_ResultSelector_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { Assert.Equal(index, x); return new[] { 0, -1 }; }, (x, y) => y + -DefaultStart - 2 * x).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SequenceEqual(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Assert.True(operation.Item(DefaultStart, DefaultSize, source.Item).SequenceEqual(ParallelEnumerable.Range(DefaultStart, DefaultSize).AsOrdered()));
            Assert.True(ParallelEnumerable.Range(DefaultStart, DefaultSize).AsOrdered().SequenceEqual(operation.Item(DefaultStart, DefaultSize, source.Item)));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Single(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, 1, DefaultSource).Single());
            Assert.Equal(DefaultStart + DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, DefaultSource).Single(x => x == DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void SingleOrDefault(Labeled<Operation> operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, 1, DefaultSource).SingleOrDefault());
            Assert.Equal(DefaultStart + DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, DefaultSource).SingleOrDefault(x => x == DefaultStart + DefaultSize / 2));

            if (!operation.ToString().StartsWith("DefaultIfEmpty"))
            {
                Assert.Equal(default(int), operation.Item(DefaultStart, 0, DefaultSource).SingleOrDefault());
                Assert.Equal(default(int), operation.Item(DefaultStart, 0, DefaultSource).SingleOrDefault(x => x == DefaultStart + DefaultSize / 2));
            }
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Skip(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Skip(DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Skip_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Skip(DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SkipWhile(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).SkipWhile(x => x < DefaultStart + DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SkipWhile_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).SkipWhile(x => x < DefaultStart + DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SkipWhile_Indexed(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).SkipWhile((x, index) => index < DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void SkipWhile_Indexed_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).SkipWhile((x, index) => index < DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Sum(Labeled<Operation> operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize), operation.Item(DefaultStart, DefaultSize, DefaultSource).Sum());
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void Sum_Nullable(Labeled<Operation> operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize), operation.Item(DefaultStart, DefaultSize, DefaultSource).Sum(x => (int?)x));
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Take(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Take(DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Take_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Take(DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void TakeWhile(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).TakeWhile(x => x < DefaultStart + DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void TakeWhile_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).TakeWhile(x => x < DefaultStart + DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void TakeWhile_Indexed(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).TakeWhile((x, index) => index < DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void TakeWhile_Indexed_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).TakeWhile((x, index) => index < DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ThenBy_Initial(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenBy(x => x))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ThenBy_Initial_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenBy(x => x).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ThenBy_OtherDirection(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenBy(x => -x))
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ThenBy_OtherDirection_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenBy(x => -x).ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ThenByDescending_Initial(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenByDescending(x => -x))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ThenByDescending_Initial_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenByDescending(x => -x).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ThenByDescending_OtherDirection(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenByDescending(x => x))
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ThenByDescending_OtherDirection_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart + DefaultSize;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenByDescending(x => x).ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void ToArray(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).ToArray(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void ToDictionary(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).ToDictionary(x => x * 2),
                p =>
                {
                    seen.Add(p.Key / 2);
                    Assert.Equal(p.Key, p.Value * 2);
                });
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public void ToDictionary_ElementSelector(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).ToDictionary(x => x, y => y * 2),
                p =>
                {
                    seen.Add(p.Key);
                    Assert.Equal(p.Key * 2, p.Value);
                });
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void ToList(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ToLookup(Labeled<Operation> source, Labeled<Operation> operation)
        {
            IntegerRangeSet seenOuter = new IntegerRangeSet(0, 2);
            ILookup<int, int> lookup = operation.Item(DefaultStart, DefaultSize, source.Item).ToLookup(x => x % 2);
            Assert.All(lookup,
                group =>
                {
                    seenOuter.Add(group.Key);
                    IntegerRangeSet seenInner = new IntegerRangeSet(DefaultStart / 2, (DefaultSize + ((1 + group.Key) % 2)) / 2);
                    Assert.All(group, y => { Assert.Equal(group.Key, y % 2); seenInner.Add(y / 2); });
                    seenInner.AssertComplete();
                });
            seenOuter.AssertComplete();
            Assert.Empty(lookup[-1]);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public void ToLookup_ElementSelector(Labeled<Operation> source, Labeled<Operation> operation)
        {
            IntegerRangeSet seenOuter = new IntegerRangeSet(0, 2);
            ILookup<int, int> lookup = operation.Item(DefaultStart, DefaultSize, source.Item).ToLookup(x => x % 2, y => -y);
            Assert.All(lookup,
                group =>
                {
                    seenOuter.Add(group.Key);
                    IntegerRangeSet seenInner = new IntegerRangeSet(DefaultStart / 2, (DefaultSize + ((1 + group.Key) % 2)) / 2);
                    Assert.All(group, y => { Assert.Equal(group.Key, -y % 2); seenInner.Add(-y / 2); });
                    seenInner.AssertComplete();
                });
            seenOuter.AssertComplete();
            Assert.Empty(lookup[-1]);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Union(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Union(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<int> query = left(DefaultStart, DefaultSize * 3 / 4, source.Item)
                    .Union(right(DefaultStart + DefaultSize / 2, DefaultSize / 2, source.Item));
                foreach (int i in query)
                {
                    Assert.Equal(seen++, i);
                }
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Union(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            Union(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Union_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Union(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<int> query = left(DefaultStart, DefaultSize * 3 / 4, source.Item)
                    .Union(right(DefaultStart + DefaultSize / 2, DefaultSize / 2, source.Item));
                Assert.All(query.ToList(), x => Assert.Equal(seen++, x));
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Union(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            Union(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Where(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Where(x => x < DefaultStart + DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Where_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Where(x => x < DefaultStart + DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Where_Indexed(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Where((x, index) => index < DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Where_Indexed_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Where((x, index) => index < DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Zip(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Zip(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<int> query = left(DefaultStart * 2, DefaultSize, source.Item)
                    .Zip(right(0, DefaultSize, source.Item), (x, y) => (x + y) / 2);
                foreach (int i in query)
                {
                    Assert.Equal(seen++, i);
                }
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Zip(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            Zip(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperators))]
        [MemberData(nameof(BinaryOperators))]
        public void Zip_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            void Zip(Operation left, Operation right)
            {
                int seen = DefaultStart;
                ParallelQuery<int> query = left(DefaultStart * 2, DefaultSize, source.Item)
                    .Zip(right(0, DefaultSize, source.Item), (x, y) => (x + y) / 2);
                Assert.All(query.ToList(), x => Assert.Equal(seen++, x));
                Assert.Equal(DefaultStart + DefaultSize, seen);
            }
            Zip(operation.Item, LabeledDefaultSource.AsOrdered().Item);
            Zip(LabeledDefaultSource.AsOrdered().Item, operation.Item);
        }
    }
}
