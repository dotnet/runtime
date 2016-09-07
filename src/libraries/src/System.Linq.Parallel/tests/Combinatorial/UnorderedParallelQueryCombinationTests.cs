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
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Cast_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            foreach (int? i in operation.Item(DefaultStart, DefaultSize, DefaultSource).Cast<int?>())
            {
                Assert.True(i.HasValue);
                seen.Add(i.Value);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Cast_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).Cast<int?>().ToList(), x => seen.Add((int)x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Concat_Unordered(Labeled<Operation> operation)
        {
            Action<Operation, Operation> concat = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                foreach (int i in left(DefaultStart, DefaultSize / 2, DefaultSource)
                    .Concat(right(DefaultStart + DefaultSize / 2, DefaultSize / 2, DefaultSource)))
                {
                    seen.Add(i);
                }
                seen.AssertComplete();
            };
            concat(operation.Item, DefaultSource);
            concat(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Concat_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            Action<Operation, Operation> concat = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                Assert.All(
                    left(DefaultStart, DefaultSize / 2, DefaultSource)
                        .Concat(right(DefaultStart + DefaultSize / 2, DefaultSize / 2, DefaultSource)).ToList(),
                    x => seen.Add(x));
                seen.AssertComplete();
            };
            concat(operation.Item, DefaultSource);
            concat(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void DefaultIfEmpty_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, DefaultSource).DefaultIfEmpty())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void DefaultIfEmpty_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).DefaultIfEmpty().ToList(), x => seen.Add((int)x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Distinct_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(DefaultStart * 2, DefaultSize * 2, DefaultSource).Select(x => x / 2).Distinct();
            foreach (int i in query)
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Distinct_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            ParallelQuery<int> query = operation.Item(DefaultStart * 2, DefaultSize * 2, DefaultSource).Select(x => x / 2).Distinct();
            Assert.All(query.ToList(), x => seen.Add((int)x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Except_Unordered(Labeled<Operation> operation)
        {
            Action<Operation, Operation> except = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                ParallelQuery<int> query = left(DefaultStart, DefaultSize + DefaultSize / 2, DefaultSource)
                    .Except(right(DefaultStart + DefaultSize, DefaultSize, DefaultSource));
                foreach (int i in query)
                {
                    seen.Add(i);
                }
                seen.AssertComplete();
            };
            except(operation.Item, DefaultSource);
            except(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Except_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            Action<Operation, Operation> except = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                ParallelQuery<int> query = left(DefaultStart, DefaultSize + DefaultSize / 2, DefaultSource)
                    .Except(right(DefaultStart + DefaultSize, DefaultSize, DefaultSource));
                Assert.All(query.ToList(), x => seen.Add((int)x));
                seen.AssertComplete();
            };
            except(operation.Item, DefaultSource);
            except(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void GetEnumerator_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            IEnumerator<int> enumerator = operation.Item(DefaultStart, DefaultSize, DefaultSource).GetEnumerator();
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
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void GroupBy_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, (DefaultSize + (GroupFactor - 1)) / GroupFactor);
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, DefaultSource).GroupBy(x => x / GroupFactor))
            {
                seenKey.Add(group.Key);
                IntegerRangeSet seenElement = new IntegerRangeSet(group.Key * GroupFactor, Math.Min(GroupFactor, DefaultSize - (group.Key - 1) * GroupFactor));
                Assert.All(group, x => seenElement.Add(x));
                seenElement.AssertComplete();
            }
            seenKey.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void GroupBy_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, (DefaultSize + (GroupFactor - 1)) / GroupFactor);
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, DefaultSource).GroupBy(x => x / GroupFactor).ToList())
            {
                seenKey.Add(group.Key);
                IntegerRangeSet seenElement = new IntegerRangeSet(group.Key * GroupFactor, Math.Min(GroupFactor, DefaultSize - (group.Key - 1) * GroupFactor));
                Assert.All(group, x => seenElement.Add(x));
                seenElement.AssertComplete();
            }
            seenKey.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void GroupBy_ElementSelector_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, (DefaultSize + (GroupFactor - 1)) / GroupFactor);
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, DefaultSource).GroupBy(x => x / GroupFactor, y => -y))
            {
                seenKey.Add(group.Key);
                IntegerRangeSet seenElement = new IntegerRangeSet(1 - Math.Min(DefaultStart + DefaultSize, (group.Key + 1) * GroupFactor), Math.Min(GroupFactor, DefaultSize - (group.Key - 1) * GroupFactor));
                Assert.All(group, x => seenElement.Add(x));
                seenElement.AssertComplete();
            }
            seenKey.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void GroupBy_ElementSelector_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, (DefaultSize + (GroupFactor - 1)) / GroupFactor);
            foreach (IGrouping<int, int> group in operation.Item(DefaultStart, DefaultSize, DefaultSource).GroupBy(x => x / GroupFactor, y => -y).ToList())
            {
                seenKey.Add(group.Key);
                IntegerRangeSet seenElement = new IntegerRangeSet(1 - Math.Min(DefaultStart + DefaultSize, (group.Key + 1) * GroupFactor), Math.Min(GroupFactor, DefaultSize - (group.Key - 1) * GroupFactor));
                Assert.All(group, x => seenElement.Add(x));
                seenElement.AssertComplete();
            }
            seenKey.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void GroupJoin_Unordered(Labeled<Operation> operation)
        {
            Action<Operation, Operation> groupJoin = (left, right) =>
            {
                IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, DefaultSize / GroupFactor);
                foreach (KeyValuePair<int, IEnumerable<int>> group in left(DefaultStart / GroupFactor, DefaultSize / GroupFactor, DefaultSource)
                    .GroupJoin(right(DefaultStart, DefaultSize, DefaultSource), x => x, y => y / GroupFactor, (k, g) => new KeyValuePair<int, IEnumerable<int>>(k, g)))
                {
                    Assert.True(seenKey.Add(group.Key));
                    IntegerRangeSet seenElement = new IntegerRangeSet(group.Key * GroupFactor, GroupFactor);
                    Assert.All(group.Value, x => seenElement.Add(x));
                    seenElement.AssertComplete();
                }
                seenKey.AssertComplete();
            };
            groupJoin(operation.Item, DefaultSource);
            groupJoin(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void GroupJoin_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            Action<Operation, Operation> groupJoin = (left, right) =>
            {
                IntegerRangeSet seenKey = new IntegerRangeSet(DefaultStart / GroupFactor, DefaultSize / GroupFactor);
                foreach (KeyValuePair<int, IEnumerable<int>> group in left(DefaultStart / GroupFactor, DefaultSize / GroupFactor, DefaultSource)
                    .GroupJoin(right(DefaultStart, DefaultSize, DefaultSource), x => x, y => y / GroupFactor, (k, g) => new KeyValuePair<int, IEnumerable<int>>(k, g)).ToList())
                {
                    Assert.True(seenKey.Add(group.Key));
                    IntegerRangeSet seenElement = new IntegerRangeSet(group.Key * GroupFactor, GroupFactor);
                    Assert.All(group.Value, x => seenElement.Add(x));
                    seenElement.AssertComplete();
                }
                seenKey.AssertComplete();
            };
            groupJoin(operation.Item, DefaultSource);
            groupJoin(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Intersect_Unordered(Labeled<Operation> operation)
        {
            Action<Operation, Operation> intersect = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                ParallelQuery<int> query = left(DefaultStart - DefaultSize / 2, DefaultSize + DefaultSize / 2, DefaultSource)
                    .Intersect(right(DefaultStart, DefaultSize + DefaultSize / 2, DefaultSource));
                foreach (int i in query)
                {
                    seen.Add(i);
                }
                seen.AssertComplete();
            };
            intersect(operation.Item, DefaultSource);
            intersect(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Intersect_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            Action<Operation, Operation> intersect = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                ParallelQuery<int> query = left(DefaultStart - DefaultSize / 2, DefaultSize + DefaultSize / 2, DefaultSource)
                    .Intersect(right(DefaultStart, DefaultSize + DefaultSize / 2, DefaultSource));
                Assert.All(query.ToList(), x => seen.Add(x));
                seen.AssertComplete();
            };
            intersect(operation.Item, DefaultSource);
            intersect(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Join_Unordered(Labeled<Operation> operation)
        {
            Action<Operation, Operation> join = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                ParallelQuery<KeyValuePair<int, int>> query = left(DefaultStart / GroupFactor, DefaultSize / GroupFactor, DefaultSource)
                    .Join(right(DefaultStart, DefaultSize, DefaultSource), x => x, y => y / GroupFactor, (x, y) => new KeyValuePair<int, int>(x, y));
                foreach (KeyValuePair<int, int> p in query)
                {
                    Assert.Equal(p.Key, p.Value / GroupFactor);
                    seen.Add(p.Value);
                }
                seen.AssertComplete();
            };
            join(operation.Item, DefaultSource);
            join(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryUnorderedOperators))]
        [MemberData(nameof(BinaryUnorderedOperators))]
        public static void Join_Unordered_NotPipelined(Labeled<Operation> source, Labeled<Operation> operation)
        {
            Action<Operation, Operation> join = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                ParallelQuery<KeyValuePair<int, int>> query = left(DefaultStart / GroupFactor, DefaultSize / GroupFactor, DefaultSource)
                    .Join(right(DefaultStart, DefaultSize, DefaultSource), x => x, y => y / GroupFactor, (x, y) => new KeyValuePair<int, int>(x, y));
                foreach (KeyValuePair<int, int> p in query.ToList())
                {
                    Assert.Equal(p.Key, p.Value / GroupFactor);
                    seen.Add(p.Value);
                }
                seen.AssertComplete();
            };
            join(operation.Item, DefaultSource);
            join(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void OfType_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, DefaultSource).OfType<int>())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void OfType_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).OfType<int>().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Select_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize + 1, DefaultSize);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, DefaultSource).Select(x => -x))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Select_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize + 1, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).Select(x => -x).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Select_Index_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize + 1, DefaultSize);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, DefaultSource).Select((x, index) => { indices.Add(index); return -x; }))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Select_Index_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize + 1, DefaultSize);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).Select((x, index) => { indices.Add(index); return -x; }).ToList(), x => seen.Add(x));
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void SelectMany_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            foreach (int i in operation.Item(0, DefaultSize, DefaultSource).SelectMany(x => new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x)))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void SelectMany_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            Assert.All(operation.Item(0, DefaultSize, DefaultSource).SelectMany(x => new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x)).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void SelectMany_Indexed_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            foreach (int i in operation.Item(0, DefaultSize, DefaultSource).SelectMany((x, index) => { indices.Add(index); return new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x); }))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void SelectMany_Indexed_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            Assert.All(operation.Item(0, DefaultSize, DefaultSource).SelectMany((x, index) => { indices.Add(index); return new[] { 0, -1 }.Select(y => y + -DefaultStart - 2 * x); }).ToList(), x => seen.Add(x));
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void SelectMany_ResultSelector_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            foreach (int i in operation.Item(0, DefaultSize, DefaultSource).SelectMany(x => new[] { 0, -1 }, (x, y) => y + -DefaultStart - 2 * x))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void SelectMany_ResultSelector_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            Assert.All(operation.Item(0, DefaultSize, DefaultSource).SelectMany(x => new[] { 0, -1 }, (x, y) => y + -DefaultStart - 2 * x).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void SelectMany_Indexed_ResultSelector_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            foreach (int i in operation.Item(0, DefaultSize, DefaultSource).SelectMany((x, index) => { indices.Add(index); return new[] { 0, -1 }; }, (x, y) => y + -DefaultStart - 2 * x))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void SelectMany_Indexed_ResultSelector_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(-DefaultStart - DefaultSize * 2 + 1, DefaultSize * 2);
            IntegerRangeSet indices = new IntegerRangeSet(0, DefaultSize);
            Assert.All(operation.Item(0, DefaultSize, DefaultSource).SelectMany((x, index) => { indices.Add(index); return new[] { 0, -1 }; }, (x, y) => y + -DefaultStart - 2 * x).ToList(), x => seen.Add(x));
            seen.AssertComplete();
            indices.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Skip_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            int count = 0;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, DefaultSource).Skip(DefaultSize / 2))
            {
                seen.Add(i);
                count++;
            }
            Assert.Equal((DefaultSize - 1) / 2 + 1, count);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Skip_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            int count = 0;
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).Skip(DefaultSize / 2).ToList(), x => { seen.Add(x); count++; });
            Assert.Equal((DefaultSize - 1) / 2 + 1, count);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Take_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            int count = 0;
            foreach (int i in operation.Item(DefaultStart, DefaultSize, DefaultSource).Take(DefaultSize / 2))
            {
                seen.Add(i);
                count++;
            }
            Assert.Equal(DefaultSize / 2, count);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Take_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            int count = 0;
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).Take(DefaultSize / 2).ToList(), x => { seen.Add(x); count++; });
            Assert.Equal(DefaultSize / 2, count);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void ToArray_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).ToArray(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Union_Unordered(Labeled<Operation> operation)
        {
            Action<Operation, Operation> union = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                ParallelQuery<int> query = left(DefaultStart, DefaultSize * 3 / 4, DefaultSource)
                    .Union(right(DefaultStart + DefaultSize / 2, DefaultSize / 2, DefaultSource));
                foreach (int i in query)
                {
                    seen.Add(i);
                }
                seen.AssertComplete();
            };
            union(operation.Item, DefaultSource);
            union(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Union_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            Action<Operation, Operation> union = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                ParallelQuery<int> query = left(DefaultStart, DefaultSize * 3 / 4, DefaultSource)
                    .Union(right(DefaultStart + DefaultSize / 2, DefaultSize / 2, DefaultSource));
                Assert.All(query.ToList(), x => seen.Add(x));
                seen.AssertComplete();
            };
            union(operation.Item, DefaultSource);
            union(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Where_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize / 2);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, DefaultSource).Where(x => x < DefaultStart + DefaultSize / 2))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Where_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize / 2);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).Where(x => x < DefaultStart + DefaultSize / 2).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Where_Indexed_Unordered(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize / 2);
            foreach (int i in operation.Item(DefaultStart, DefaultSize, DefaultSource).Where((x, index) => x < DefaultStart + DefaultSize / 2))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Where_Indexed_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize / 2);
            Assert.All(operation.Item(DefaultStart, DefaultSize, DefaultSource).Where((x, index) => x < DefaultStart + DefaultSize / 2).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Zip_Unordered(Labeled<Operation> operation)
        {
            Action<Operation, Operation> zip = (left, right) =>
             {
                 IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                 ParallelQuery<int> query = left(DefaultStart, DefaultSize, DefaultSource)
                     .Zip(right(0, DefaultSize, DefaultSource), (x, y) => x);
                 foreach (int i in query)
                 {
                     seen.Add(i);
                 }
                 seen.AssertComplete();
             };
            zip(operation.Item, DefaultSource);
            zip(DefaultSource, operation.Item);
        }

        [Theory]
        [MemberData(nameof(UnaryOperations))]
        [MemberData(nameof(BinaryOperations))]
        public static void Zip_Unordered_NotPipelined(Labeled<Operation> operation)
        {
            Action<Operation, Operation> zip = (left, right) =>
            {
                IntegerRangeSet seen = new IntegerRangeSet(DefaultStart, DefaultSize);
                ParallelQuery<int> query = left(DefaultStart, DefaultSize, DefaultSource)
                    .Zip(right(0, DefaultSize, DefaultSource), (x, y) => x);
                Assert.All(query.ToList(), x => seen.Add(x));
                seen.AssertComplete();
            };
            zip(operation.Item, DefaultSource);
            zip(DefaultSource, operation.Item);
        }
    }
}
