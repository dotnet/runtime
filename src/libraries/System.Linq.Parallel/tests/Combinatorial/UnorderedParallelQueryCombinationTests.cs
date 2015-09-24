// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public partial class ParallelQueryCombinationTests
    {
        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Cast_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            foreach (int? i in operation.Item(DefaultStart, DefaultSize, source.Item).Cast<int?>())
            {
                Assert.True(i.HasValue);
                seen.Add(i.Value);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Cast_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Cast<int?>().ToList(), x => seen.Add((int)x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void DefaultIfEmpty_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).DefaultIfEmpty())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void DefaultIfEmpty_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).DefaultIfEmpty().ToList(), x => seen.Add((int)x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Distinct_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(DefaultStart * 2, DefaultSize * 2, source.Item).Select(x => x / 2).Distinct();
            foreach (int i in query)
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Distinct_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(DefaultStart * 2, DefaultSize * 2, source.Item).Select(x => x / 2).Distinct();
            Assert.All(query.ToList(), x => seen.Add((int)x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Except_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(DefaultStart, DefaultSize + DefaultSize / 2, source.Item)
                .Except(operation.Item(DefaultStart + DefaultSize, DefaultSize, source.Item));
            foreach (int i in query)
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Except_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(DefaultStart, DefaultSize + DefaultSize / 2, source.Item)
                .Except(operation.Item(DefaultStart + DefaultSize, DefaultSize, source.Item));
            Assert.All(query.ToList(), x => seen.Add((int)x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void GetEnumerator_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            IEnumerator<int> enumerator = operation.Item(DefaultStart, DefaultSize, source.Item).GetEnumerator();
            while (enumerator.MoveNext())
            {
                int current = enumerator.Current;
                seen.Add(current);
                Assert.Equal(current, enumerator.Current);
            }
            seen.AssertComplete();

            Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void GroupBy_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, (DefaultSize + (GroupFactor - 1)) / GroupFactor);
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, source.Item).GroupBy(x => x / GroupFactor))
            {
                seenKey.Add(group.Key);
                IntegerRangeSet seenElement = new IntegerRangeSet(group.Key * GroupFactor, Math.Min(GroupFactor, DefaultSize - (group.Key - 1) * GroupFactor));
                Assert.All(group, x => seenElement.Add(x));
                seenElement.AssertComplete();
            }
            seenKey.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void GroupBy_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, (DefaultSize + (GroupFactor - 1)) / GroupFactor);
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, source.Item).GroupBy(x => x / GroupFactor).ToList())
            {
                seenKey.Add(group.Key);
                IntegerRangeSet seenElement = new IntegerRangeSet(group.Key * GroupFactor, Math.Min(GroupFactor, DefaultSize - (group.Key - 1) * GroupFactor));
                Assert.All(group, x => seenElement.Add(x));
                seenElement.AssertComplete();
            }
            seenKey.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void GroupBy_ElementSelector_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, (DefaultSize + (GroupFactor - 1)) / GroupFactor);
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, source.Item).GroupBy(x => x / GroupFactor, y => -y))
            {
                seenKey.Add(group.Key);
                IntegerRangeSet seenElement = new IntegerRangeSet(1 - Math.Min(DefaultStart + DefaultSize, (group.Key + 1) * GroupFactor), Math.Min(GroupFactor, DefaultSize - (group.Key - 1) * GroupFactor));
                Assert.All(group, x => seenElement.Add(x));
                seenElement.AssertComplete();
            }
            seenKey.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void GroupBy_ElementSelector_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, (DefaultSize + (GroupFactor - 1)) / GroupFactor);
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, source.Item).GroupBy(x => x / GroupFactor, y => -y).ToList())
            {
                seenKey.Add(group.Key);
                IntegerRangeSet seenElement = new IntegerRangeSet(1 - Math.Min(DefaultStart + DefaultSize, (group.Key + 1) * GroupFactor), Math.Min(GroupFactor, DefaultSize - (group.Key - 1) * GroupFactor));
                Assert.All(group, x => seenElement.Add(x));
                seenElement.AssertComplete();
            }
            seenKey.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Intersect_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(DefaultStart - DefaultSize / 2, DefaultSize + DefaultSize / 2, source.Item)
                .Intersect(operation.Item(DefaultStart, DefaultSize + DefaultSize / 2, source.Item));
            foreach (int i in query)
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Intersect_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(DefaultStart - DefaultSize / 2, DefaultSize + DefaultSize / 2, source.Item)
                .Intersect(operation.Item(DefaultStart, DefaultSize + DefaultSize / 2, source.Item));
            Assert.All(query.ToList(), x => seen.Add((int)x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OfType_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).OfType<int>())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void OfType_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).OfType<int>().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Select_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize + 1, DefaultSize);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Select(x => -x))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Select_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize + 1, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Select(x => -x).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Select_Index_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize + 1, DefaultSize);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Select((x, index) => { indices.Add(index); return -x; }))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Select_Index_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize + 1, DefaultSize);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Select((x, index) => { indices.Add(index); return -x; }).ToList(), x => seen.Add(x));
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void SelectMany_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x)))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void SelectMany_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x)).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void SelectMany_Indexed_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { indices.Add(index); return new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x); }))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void SelectMany_Indexed_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { indices.Add(index); return new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x); }).ToList(), x => seen.Add(x));
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void SelectMany_ResultSelector_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }, (x, y) => y + -DefaultStart - 2 * x))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void SelectMany_ResultSelector_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany(x => new[] { 0, -1 }, (x, y) => y + -DefaultStart - 2 * x).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void SelectMany_Indexed_ResultSelector_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            foreach (int i in operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { indices.Add(index); return new[] { 0, -1 }; }, (x, y) => y + -DefaultStart - 2 * x))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void SelectMany_Indexed_ResultSelector_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            Assert.All(operation.Item(0, DefaultSize, source.Item).SelectMany((x, index) => { indices.Add(index); return new[] { 0, -1 }; }, (x, y) => y + -DefaultStart - 2 * x).ToList(), x => seen.Add(x));
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Skip_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            int count = 0;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Skip(DefaultSize / 2))
            {
                seen.Add(i);
                count++;
            }
            Assert.Equal((DefaultSize - 1) / 2 + 1, count);
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Skip_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            int count = 0;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Skip(DefaultSize / 2).ToList(), x => { seen.Add(x); count++; });
            Assert.Equal((DefaultSize - 1) / 2 + 1, count);
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Take_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            int count = 0;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Take(DefaultSize / 2))
            {
                seen.Add(i);
                count++;
            }
            Assert.Equal(DefaultSize / 2, count);
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Take_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            int count = 0;
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Take(DefaultSize / 2).ToList(), x => { seen.Add(x); count++; });
            Assert.Equal(DefaultSize / 2, count);
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void ToArray_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).ToArray(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Where_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize / 2);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Where(x => x < DefaultStart + DefaultSize / 2))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Where_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize / 2);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Where(x => x < DefaultStart + DefaultSize / 2).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Where_Indexed_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize / 2);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, source.Item).Where((x, index) => x < DefaultStart + DefaultSize / 2))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Where_Indexed_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize / 2);
            Assert.All(operation.Item(DefaultStart, DefaultSize, source.Item).Where((x, index) => x < DefaultStart + DefaultSize / 2).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Zip_Unordered(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(DefaultStart, DefaultSize, source.Item)
                .Zip(operation.Item(0, DefaultSize, source.Item), (x, y) => x);
            foreach (int i in query)
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData("UnaryUnorderedOperators")]
        [MemberData("BinaryUnorderedOperators")]
        public static void Zip_Unordered_NotPipelined(LabeledOperation source, LabeledOperation operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(0, DefaultSize, source.Item)
                .Zip(operation.Item(DefaultStart, DefaultSize, source.Item), (x, y) => y);
            Assert.All(query.ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }
    }
}
