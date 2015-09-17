// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public partial class ParallelQueryCombinationTests
    {
        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Aggregate(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate((x, y) => x + y));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Aggregate_Seed(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(0, (x, y) => x + y));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Aggregate_Result(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(0, (x, y) => x + y, r => r));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Aggregate_Accumulator(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(0, (a, x) => a + x, (l, r) => l + r, r => r));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Aggregate_SeedFactory(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize),
                operation.Item(DefaultStart, DefaultSize, source.Item).Aggregate(() => 0, (a, x) => a + x, (l, r) => l + r, r => r));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void All_False(LabeledOperation source, LabeledOperation operation)
        {
            Assert.False(operation.Item(DefaultStart, DefaultSize, source.Item).All(x => false));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void All_True(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.True(operation.Item(DefaultStart, DefaultSize, source.Item).All(x => seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Any_False(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.False(operation.Item(DefaultStart, DefaultSize, source.Item).Any(x => !seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Any_True(LabeledOperation source, LabeledOperation operation)
        {
            Assert.True(operation.Item(DefaultStart, DefaultSize, source.Item).Any(x => true));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Average(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize) / (double)DefaultSize,
                operation.Item(DefaultStart, DefaultSize, source.Item).Average());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Average_Nullable(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize) / (double?)DefaultSize,
                operation.Item(DefaultStart, DefaultSize, source.Item).Average(x => (int?)x));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Cast(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Cast_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Cast<int?>().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Contains_True(LabeledOperation source, LabeledOperation operation)
        {
            Assert.True(operation.Item(DefaultStart, DefaultSize, source.Item).Contains(DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Contains_False(LabeledOperation source, LabeledOperation operation)
        {
            Assert.False(operation.Item(DefaultStart, DefaultSize, source.Item).Contains(DefaultStart + DefaultSize));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Count_Elements(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultSize, operation.Item(DefaultStart, DefaultSize, source.Item).Count());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Count_Predicate_Some(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, source.Item).Count(x => x < DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Count_Predicate_None(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(0, operation.Item(DefaultStart, DefaultSize, source.Item).Count(x => x < DefaultStart));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void DefaultIfEmpty(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).DefaultIfEmpty())
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void DefaultIfEmpty_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).DefaultIfEmpty().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Distinct(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Distinct_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            ParallelQuery<int> query = operation.Item(DefaultStart * 2, DefaultSize * 2, source.Item).Select(x => x / 2).Distinct();
            Assert.All(query.ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ElementAt(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ElementAtOrDefault(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Except(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            ParallelQuery<int> query = operation.Item(DefaultStart, DefaultSize + DefaultSize / 2, source.Item)
                .Except(operation.Item(DefaultStart + DefaultSize, DefaultSize, source.Item));
            foreach (int i in query)
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Except_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            ParallelQuery<int> query = operation.Item(DefaultStart, DefaultSize + DefaultSize / 2, source.Item)
                .Except(operation.Item(DefaultStart + DefaultSize, DefaultSize, source.Item));
            Assert.All(query.ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void First(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, DefaultSize, source.Item).First());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void First_Predicate(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart + DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, source.Item).First(x => x >= DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void FirstOrDefault(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void FirstOrDefault_Predicate(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart + DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault(x => x >= DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void FirstOrDefault_Predicate_None(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(default(int), operation.Item(DefaultStart, DefaultSize, source.Item).FirstOrDefault(x => false));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ForAll(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            operation.Item(DefaultStart, DefaultSize, source.Item).ForAll(x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void GetEnumerator(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void GroupBy(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void GroupBy_NotPipelined(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void GroupBy_ElementSelector(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void GroupBy_ElementSelector_NotPipelined(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Intersect(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            ParallelQuery<int> query = operation.Item(DefaultStart - DefaultSize / 2, DefaultSize + DefaultSize / 2, source.Item)
                .Intersect(operation.Item(DefaultStart, DefaultSize + DefaultSize / 2, source.Item));
            foreach (int i in query)
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Intersect_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            ParallelQuery<int> query = operation.Item(DefaultStart - DefaultSize / 2, DefaultSize + DefaultSize / 2, source.Item)
                .Intersect(operation.Item(DefaultStart, DefaultSize + DefaultSize / 2, source.Item));
            Assert.All(query.ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Last(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart + DefaultSize - 1, operation.Item(DefaultStart, DefaultSize, source.Item).Last());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Last_Predicate(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart + DefaultSize / 2 - 1, operation.Item(DefaultStart, DefaultSize, source.Item).Last(x => x < DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void LastOrDefault(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart + DefaultSize - 1, operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void LastOrDefault_Predicate(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart + DefaultSize / 2 - 1, operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault(x => x < DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void LastOrDefault_Predicate_None(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(default(int), operation.Item(DefaultStart, DefaultSize, source.Item).LastOrDefault(x => false));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void LongCount_Elements(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultSize, operation.Item(DefaultStart, DefaultSize, source.Item).LongCount());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void LongCount_Predicate_Some(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, source.Item).LongCount(x => x < DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void LongCount_Predicate_None(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(0, operation.Item(DefaultStart, DefaultSize, source.Item).LongCount(x => x < DefaultStart));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Max(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart + DefaultSize - 1, operation.Item(DefaultStart, DefaultSize, source.Item).Max());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Max_Nullable(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart + DefaultSize - 1, operation.Item(DefaultStart, DefaultSize, source.Item).Max(x => (int?)x));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Min(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, DefaultSize, source.Item).Min());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Min_Nullable(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, DefaultSize, source.Item).Min(x => (int?)x));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void OfType(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OfType<int>())
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void OfType_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OfType<int>().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void OfType_Other(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Empty(operation.Item(DefaultStart, DefaultSize, source.Item).OfType<long>());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void OfType_Other_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Empty(operation.Item(DefaultStart, DefaultSize, source.Item).OfType<long>().ToList());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OrderBy_Initial(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => x))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OrderBy_Initial_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => x).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OrderBy_OtherDirection(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => -x))
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OrderBy_OtherDirection_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => -x).ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OrderByDescending_Initial(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderByDescending(x => -x))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OrderByDescending_Initial_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderByDescending(x => -x).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OrderByDescending_OtherDirection(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderByDescending(x => x))
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OrderByDescending_OtherDirection_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderByDescending(x => x).ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Reverse(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Reverse())
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Reverse_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Reverse().ToList())
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Select(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Select(x => -x))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Select_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Select(x => -x).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Select_Indexed(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Select((x, index) => { Assert.Equal(DefaultStart + index, x); return -x; }))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Select_Indexed_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Select((x, index) => { Assert.Equal(DefaultStart + index, x); return -x; }).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SelectMany(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x)))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SelectMany_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x)).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SelectMany_Indexed(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { Assert.Equal(index, x); return new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x); }))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SelectMany_Indexed_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { Assert.Equal(index, x); return new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x); }).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SelectMany_ResultSelector(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }, (x, y) => y + -DefaultStart - 2 * x))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SelectMany_ResultSelector_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }, (x, y) => y + -DefaultStart - 2 * x).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SelectMany_Indexed_ResultSelector(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { Assert.Equal(index, x); return new[] { 0, -1 }; }, (x, y) => y + -DefaultStart - 2 * x))
            {
                Assert.Equal(seen--, i);
            }
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SelectMany_Indexed_ResultSelector_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = -DefaultStart;
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { Assert.Equal(index, x); return new[] { 0, -1 }; }, (x, y) => y + -DefaultStart - 2 * x).ToList(), x => Assert.Equal(seen--, x));
            Assert.Equal(-DefaultStart - DefaultSize * 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SequenceEqual(LabeledOperation source, LabeledOperation operation)
        {
            Assert.True(operation.Item(DefaultStart, DefaultSize, source.Item).SequenceEqual(ParallelEnumerable.Range(DefaultStart, DefaultSize).AsOrdered()));
            Assert.True(ParallelEnumerable.Range(DefaultStart, DefaultSize).AsOrdered().SequenceEqual(operation.Item(DefaultStart, DefaultSize, source.Item)));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SequenceEqual_Self(LabeledOperation source, LabeledOperation operation)
        {
            Assert.True(operation.Item(DefaultStart, DefaultSize, source.Item).SequenceEqual(operation.Item(DefaultStart, DefaultSize, source.Item)));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Single(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, 1, source.Item).Single());
            Assert.Equal(DefaultStart + DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, source.Item).Single(x => x == DefaultStart + DefaultSize / 2));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void SingleOrDefault(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(DefaultStart, operation.Item(DefaultStart, 1, source.Item).SingleOrDefault());
            Assert.Equal(DefaultStart + DefaultSize / 2, operation.Item(DefaultStart, DefaultSize, source.Item).SingleOrDefault(x => x == DefaultStart + DefaultSize / 2));

            if (!operation.ToString().StartsWith("DefaultIfEmpty"))
            {
                Assert.Equal(default(int), operation.Item(DefaultStart, 0, source.Item).SingleOrDefault());
                Assert.Equal(default(int), operation.Item(DefaultStart, 0, source.Item).SingleOrDefault(x => x == DefaultStart + DefaultSize / 2));
            }
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Skip(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Skip(DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Skip_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Skip(DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SkipWhile(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).SkipWhile(x => x < DefaultStart + DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SkipWhile_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).SkipWhile(x => x < DefaultStart + DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SkipWhile_Indexed(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).SkipWhile((x, index) => index < DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void SkipWhile_Indexed_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize / 2;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).SkipWhile((x, index) => index < DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Sum(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize), operation.Item(DefaultStart, DefaultSize, source.Item).Sum());
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Sum_Nullable(LabeledOperation source, LabeledOperation operation)
        {
            Assert.Equal(Functions.SumRange(DefaultStart, DefaultSize), operation.Item(DefaultStart, DefaultSize, source.Item).Sum(x => (int?)x));
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Take(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Take(DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Take_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Take(DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void TakeWhile(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).TakeWhile(x => x < DefaultStart + DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void TakeWhile_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).TakeWhile(x => x < DefaultStart + DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void TakeWhile_Indexed(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).TakeWhile((x, index) => index < DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void TakeWhile_Indexed_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).TakeWhile((x, index) => index < DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ThenBy_Initial(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenBy(x => x))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ThenBy_Initial_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenBy(x => x).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ThenBy_OtherDirection(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenBy(x => -x))
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ThenBy_OtherDirection_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenBy(x => -x).ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ThenByDescending_Initial(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenByDescending(x => -x))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ThenByDescending_Initial_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenByDescending(x => -x).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ThenByDescending_OtherDirection(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenByDescending(x => x))
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ThenByDescending_OtherDirection_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart + DefaultSize;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OrderBy(x => 0).ThenByDescending(x => x).ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(DefaultStart, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ToArray(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).ToArray(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ToDictionary(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).ToDictionary(x => x * 2),
                p =>
                {
                    seen.Add(p.Key / 2);
                    Assert.Equal(p.Key, p.Value * 2);
                });
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ToDictionary_ElementSelector(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).ToDictionary(x => x, y => y * 2),
                p =>
                {
                    seen.Add(p.Key);
                    Assert.Equal(p.Key * 2, p.Value);
                });
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void ToList(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ToLookup(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ToLookup_ElementSelector(LabeledOperation source, LabeledOperation operation)
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
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Where(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Where(x => x < DefaultStart + DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Where_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Where(x => x < DefaultStart + DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Where_Indexed(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Where((x, index) => index < DefaultSize / 2))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Where_Indexed_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Where((x, index) => index < DefaultSize / 2).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize / 2, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Zip(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            ParallelQuery<int> query = operation.Item(DefaultStart * 2, DefaultSize, source.Item)
                .Zip(operation.Item(0, DefaultSize, source.Item), (x, y) => (x + y) / 2);
            foreach (int i in query)
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }

        [Theory]
        [MemberData("UnaryOperators")]
        [MemberData("BinaryOperators")]
        public static void Zip_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            int seen = DefaultStart;
            ParallelQuery<int> query = operation.Item(0, DefaultSize, source.Item)
                .Zip(operation.Item(DefaultStart * 2, DefaultSize, source.Item), (x, y) => (x + y) / 2);
            Assert.All(query.ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(DefaultStart + DefaultSize, seen);
        }
    }
}
