// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    [OuterLoop]
    public partial class ParallelQueryCombinationTests
    {
        private const int DefaultStart = 8;
        private const int DefaultSize = 16;

        private const int CountFactor = 8;
        private const int GroupFactor = 8;

        private static IEnumerable<LabeledOperation> UnorderedRangeSources()
        {
            // The difference between this and the existing sources is more control is needed over the range creation.
            // Specifically, start/count won't be known until the nesting level is resolved at runtime.
            yield return Label("ParallelEnumerable.Range", (start, count, ignore) => ParallelEnumerable.Range(start, count));
            yield return Label("Enumerable.Range", (start, count, ignore) => Enumerable.Range(start, count).AsParallel());
            yield return Label("Array", (start, count, ignore) => UnorderedSources.GetRangeArray(start, count).AsParallel());
            yield return Label("Partitioner", (start, count, ignore) => Partitioner.Create(UnorderedSources.GetRangeArray(start, count)).AsParallel());
            yield return Label("List", (start, count, ignore) => UnorderedSources.GetRangeArray(start, count).ToList().AsParallel());
            yield return Label("ReadOnlyCollection", (start, count, ignore) => new ReadOnlyCollection<int>(UnorderedSources.GetRangeArray(start, count).ToList()).AsParallel());
        }

        private static IEnumerable<LabeledOperation> RangeSources()
        {
            foreach (LabeledOperation source in UnorderedRangeSources())
            {
                foreach (LabeledOperation ordering in new[] { AsOrdered }.Concat(OrderOperators()))
                {
                    yield return source.Append(ordering);
                }
            }
        }

        private static IEnumerable<LabeledOperation> OrderOperators()
        {
            yield return Label("OrderBy", (start, count, source) => source(start, count).OrderBy(x => -x, ReverseComparer.Instance));
            yield return Label("OrderByDescending", (start, count, source) => source(start, count).OrderByDescending(x => x, ReverseComparer.Instance));
            yield return Label("ThenBy", (start, count, source) => source(start, count).OrderBy(x => 0).ThenBy(x => -x, ReverseComparer.Instance));
            yield return Label("ThenByDescending", (start, count, source) => source(start, count).OrderBy(x => 0).ThenByDescending(x => x, ReverseComparer.Instance));
        }

        public static IEnumerable<object[]> OrderFailingOperators()
        {
            LabeledOperation source = UnorderedRangeSources().First();

            foreach (LabeledOperation operation in new[] {
                    Label("OrderBy", (start, count, s) => s(start, count).OrderBy<int, int>(x => {throw new DeliberateTestException(); })),
                    Label("OrderBy-Comparer", (start, count, s) => s(start, count).OrderBy(x => x, new FailingComparer())),
                    Label("OrderByDescending", (start, count, s) => s(start, count).OrderByDescending<int, int>(x => {throw new DeliberateTestException(); })),
                    Label("OrderByDescending-Comparer", (start, count, s) => s(start, count).OrderByDescending(x => x, new FailingComparer())),
                    Label("ThenBy", (start, count, s) => s(start, count).OrderBy(x => 0).ThenBy<int, int>(x => {throw new DeliberateTestException(); })),
                    Label("ThenBy-Comparer", (start, count, s) => s(start, count).OrderBy(x => 0).ThenBy(x => x, new FailingComparer())),
                    Label("ThenByDescending", (start, count, s) => s(start, count).OrderBy(x => 0).ThenByDescending<int, int>(x => {throw new DeliberateTestException(); })),
                    Label("ThenByDescending-Comparer", (start, count, s) => s(start, count).OrderBy(x => 0).ThenByDescending(x => x, new FailingComparer())),
                })
            {
                yield return new object[] { source, operation };
            }

            foreach (LabeledOperation operation in OrderOperators())
            {
                yield return new object[] { Failing, operation };
            }
        }

        private static IEnumerable<LabeledOperation> UnaryOperations()
        {
            yield return Label("Cast", (start, count, source) => source(start, count).Cast<int>());
            yield return Label("DefaultIfEmpty", (start, count, source) => source(start, count).DefaultIfEmpty());
            yield return Label("Distinct", (start, count, source) => source(start * 2, count * 2).Select(x => x / 2).Distinct(new ModularCongruenceComparer(count)));
            yield return Label("OfType", (start, count, source) => source(start, count).OfType<int>());
            yield return Label("Reverse", (start, count, source) => source(start, count).Select(x => (start + count - 1) - (x - start)).Reverse());

            yield return Label("GroupBy", (start, count, source) => source(start, count * CountFactor).GroupBy(x => (x - start) % count, new ModularCongruenceComparer(count)).Select(g => g.Key + start));
            yield return Label("GroupBy-ElementSelector", (start, count, source) => source(start, count * CountFactor).GroupBy(x => x % count, y => y + 1, new ModularCongruenceComparer(count)).Select(g => g.Min() - 1));
            yield return Label("GroupBy-ResultSelector", (start, count, source) => source(start, count * CountFactor).GroupBy(x => (x - start) % count, (key, g) => key + start, new ModularCongruenceComparer(count)));
            yield return Label("GroupBy-ElementSelector-ResultSelector", (start, count, source) => source(start, count * CountFactor).GroupBy(x => x % count, y => y + 1, (key, g) => g.Min() - 1, new ModularCongruenceComparer(count)));

            yield return Label("Select", (start, count, source) => source(start - count, count).Select(x => x + count));
            yield return Label("Select-Index", (start, count, source) => source(start - count, count).Select((x, index) => x + count));

            yield return Label("SelectMany", (start, count, source) => source(0, (count - 1) / CountFactor + 1).SelectMany(x => Enumerable.Range(start + x * CountFactor, Math.Min(CountFactor, count - x * CountFactor))));
            yield return Label("SelectMany-Index", (start, count, source) => source(0, (count - 1) / CountFactor + 1).SelectMany((x, index) => Enumerable.Range(start + x * CountFactor, Math.Min(CountFactor, count - x * CountFactor))));
            yield return Label("SelectMany-ResultSelector", (start, count, source) => source(0, (count - 1) / CountFactor + 1).SelectMany(x => Enumerable.Range(0, Math.Min(CountFactor, count - x * CountFactor)), (group, element) => start + group * CountFactor + element));
            yield return Label("SelectMany-Index-ResultSelector", (start, count, source) => source(0, (count - 1) / CountFactor + 1).SelectMany((x, index) => Enumerable.Range(0, Math.Min(CountFactor, count - x * CountFactor)), (group, element) => start + group * CountFactor + element));

            yield return Label("Where", (start, count, source) => source(start - count / 2, count * 2).Where(x => x >= start && x < start + count));
            yield return Label("Where-Index", (start, count, source) => source(start - count / 2, count * 2).Where((x, index) => x >= start && x < start + count));
        }

        public static IEnumerable<object[]> UnaryUnorderedOperators()
        {
            foreach (LabeledOperation source in UnorderedRangeSources())
            {
                foreach (LabeledOperation operation in UnaryOperations())
                {
                    yield return new object[] { source, operation };
                }
            }
        }

        private static IEnumerable<LabeledOperation> SkipTakeOperations()
        {
            // Take/Skip-based operations require ordered input, or will disobey
            // the [start, start + count) convention expected in tests.
            yield return Label("Skip", (start, count, source) => source(start - count, count * 2).Skip(count));
            yield return Label("SkipWhile", (start, count, source) => source(start - count, count * 2).SkipWhile(x => x < start));
            yield return Label("SkipWhile-Index", (start, count, source) => source(start - count, count * 2).SkipWhile((x, index) => x < start));
            yield return Label("Take", (start, count, source) => source(start, count * 2).Take(count));
            yield return Label("TakeWhile", (start, count, source) => source(start, count * 2).TakeWhile(x => x < start + count));
            yield return Label("TakeWhile-Index", (start, count, source) => source(start, count * 2).TakeWhile((x, index) => x < start + count));
        }

        public static IEnumerable<object[]> SkipTakeOperators()
        {
            foreach (LabeledOperation source in RangeSources())
            {
                foreach (LabeledOperation operation in SkipTakeOperations())
                {
                    yield return new object[] { source, operation };
                }
            }
        }

        public static IEnumerable<object[]> UnaryOperators()
        {
            // Apply an ordered source to each operation
            foreach (LabeledOperation source in RangeSources())
            {
                foreach (LabeledOperation operation in UnaryOperations())
                {
                    yield return new object[] { source, operation };
                }
            }

            // Apply ordering to the output of each operation
            foreach (LabeledOperation source in UnorderedRangeSources())
            {
                foreach (LabeledOperation operation in UnaryOperations())
                {
                    foreach (LabeledOperation ordering in OrderOperators())
                    {
                        yield return new object[] { source, operation.Append(ordering) };
                    }
                }
            }

            foreach (object[] parms in SkipTakeOperators())
            {
                yield return parms;
            }
        }

        public static IEnumerable<object[]> UnaryFailingOperators()
        {
            foreach (LabeledOperation operation in new[] {
                    Label("Distinct", (start, count, s) => s(start, count).Distinct(new FailingEqualityComparer<int>())),
                    Label("GroupBy", (start, count, s) => s(start, count).GroupBy<int, int>(x => {throw new DeliberateTestException(); }).Select(g => g.Key)),
                    Label("GroupBy-Comparer", (start, count, s) => s(start, count).GroupBy(x => x, new FailingEqualityComparer<int>()).Select(g => g.Key)),
                    Label("GroupBy-ElementSelector", (start, count, s) => s(start, count).GroupBy<int, int, int>(x => x, x => { throw new DeliberateTestException(); }).Select(g => g.Key)),
                    Label("GroupBy-ResultSelector", (start, count, s) => s(start, count).GroupBy<int, int, int, int>(x => x, x => x, (x, g) => { throw new DeliberateTestException(); })),
                    Label("Select", (start, count, s) => s(start, count).Select<int, int>(x => {throw new DeliberateTestException(); })),
                    Label("Select-Index", (start, count, s) => s(start, count).Select<int, int>((x, index) => {throw new DeliberateTestException(); })),
                    Label("SelectMany", (start, count, s) => s(start, count).SelectMany<int, int>(x => {throw new DeliberateTestException(); })),
                    Label("SelectMany-Index", (start, count, s) => s(start, count).SelectMany<int, int>((x, index) => {throw new DeliberateTestException(); })),
                    Label("SelectMany-ResultSelector", (start, count, s) => s(start, count).SelectMany<int, int, int>(x => Enumerable.Range(x, 2), (group, elem) => { throw new DeliberateTestException(); })),
                    Label("SelectMany-Index-ResultSelector", (start, count, s) => s(start, count).SelectMany<int, int, int>((x, index) => Enumerable.Range(x, 2), (group, elem) => { throw new DeliberateTestException(); })),
                    Label("SkipWhile", (start, count, s) => s(start, count).SkipWhile(x => {throw new DeliberateTestException(); })),
                    Label("SkipWhile-Index", (start, count, s) => s(start, count).SkipWhile((x, index) => {throw new DeliberateTestException(); })),
                    Label("TakeWhile", (start, count, s) => s(start, count).TakeWhile(x => {throw new DeliberateTestException(); })),
                    Label("TakeWhile-Index", (start, count, s) => s(start, count).SkipWhile((x, index) => {throw new DeliberateTestException(); })),
                    Label("Where", (start, count, s) => s(start, count).Where(x => {throw new DeliberateTestException(); })),
                    Label("Where-Index", (start, count, s) => s(start, count).Where((x, index) => {throw new DeliberateTestException(); })),
                    })
            {
                foreach (LabeledOperation source in UnorderedRangeSources())
                {
                    yield return new object[] { source, operation };
                }
            }

            foreach (LabeledOperation operation in UnaryOperations().Concat(SkipTakeOperations()))
            {
                yield return new object[] { Failing, operation };
            }
        }

        private static IEnumerable<LabeledOperation> BinaryOperations(LabeledOperation otherSource)
        {
            String label = otherSource.ToString();
            Operation other = otherSource.Item;

            yield return Label("Concat-Right:" + label, (start, count, source) => source(start, count / 2).Concat(other(start + count / 2, count / 2 + count % 2)));
            yield return Label("Concat-Left:" + label, (start, count, source) => other(start, count / 2).Concat(source(start + count / 2, count / 2 + count % 2)));

            // Comparator needs to cover _source_ size, as which of two "equal" items is returned is undefined for unordered collections.
            yield return Label("Except-Right:" + label, (start, count, source) => source(start, count + count / 2).Except(other(start + count, count), new ModularCongruenceComparer(count * 2)));
            yield return Label("Except-Left:" + label, (start, count, source) => other(start, count + count / 2).Except(source(start + count, count), new ModularCongruenceComparer(count * 2)));

            yield return Label("GroupJoin-Right:" + label, (start, count, source) => source(start, count).GroupJoin(other(start, count * CountFactor), x => x, y => y % count, (x, g) => g.Min(), new ModularCongruenceComparer(count)));
            yield return Label("GroupJoin-Left:" + label, (start, count, source) => other(start, count).GroupJoin(source(start, count * CountFactor), x => x, y => y % count, (x, g) => g.Min(), new ModularCongruenceComparer(count)));

            // Comparator needs to cover _source_ size, as which of two "equal" items is returned is undefined.
            yield return Label("Intersect-Right:" + label, (start, count, source) => source(start, count + count / 2).Intersect(other(start - count / 2, count + count / 2), new ModularCongruenceComparer(count * 2)));
            yield return Label("Intersect-Left:" + label, (start, count, source) => other(start, count + count / 2).Intersect(source(start - count / 2, count + count / 2), new ModularCongruenceComparer(count * 2)));

            yield return Label("Join-Right:" + label, (start, count, source) => source(0, count).Join(other(start, count), x => x, y => y - start, (x, y) => x + start, new ModularCongruenceComparer(count)));
            yield return Label("Join-Left:" + label, (start, count, source) => other(0, count).Join(source(start, count), x => x, y => y - start, (x, y) => x + start, new ModularCongruenceComparer(count)));

            yield return Label("Union-Right:" + label, (start, count, source) => source(start, count * 3 / 4).Union(other(start + count / 2, count / 2 + count % 2), new ModularCongruenceComparer(count)));
            yield return Label("Union-Left:" + label, (start, count, source) => other(start, count * 3 / 4).Union(source(start + count / 2, count / 2 + count % 2), new ModularCongruenceComparer(count)));

            // When both sources are unordered any element can be matched to any other, so a different check is required.
            yield return Label("Zip-Unordered-Right:" + label, (start, count, source) => source(0, count).Zip(other(start * 2, count), (x, y) => x + start));
            yield return Label("Zip-Unordered-Left:" + label, (start, count, source) => other(start * 2, count).Zip(source(0, count), (x, y) => y + start));
        }

        public static IEnumerable<object[]> BinaryUnorderedOperators()
        {
            LabeledOperation otherSource = UnorderedRangeSources().First();
            foreach (LabeledOperation source in UnorderedRangeSources())
            {
                // Operations having multiple paths to check.
                foreach (LabeledOperation operation in BinaryOperations(otherSource))
                {
                    yield return new object[] { source, operation };
                }
            }
        }

        public static IEnumerable<object[]> BinaryOperators()
        {
            LabeledOperation unordered = UnorderedRangeSources().First();
            foreach (LabeledOperation source in RangeSources())
            {
                // Each binary can work differently, depending on which of the two source queries (or both) is ordered.

                // For most, only the ordering of the first query is important
                foreach (LabeledOperation operation in BinaryOperations(unordered).Where(op => !(op.ToString().StartsWith("Union") || op.ToString().StartsWith("Zip")) && op.ToString().Contains("Right")))
                {
                    yield return new object[] { source, operation };
                }

                // Concat currently doesn't play nice if only the right query is ordered via OrderBy et al.  Issue #1332
                // For Concat, since either one can be ordered the other side has to be tested
                //foreach (var operation in BinaryOperations().Where(op => op.ToString().Contains("Concat") && op.ToString().Contains("Left")))
                //{
                //    yield return new object[] { source, operation };
                //}

                // Zip is the same as Concat, but has a special check for for matching indices (as compared to unordered)
                foreach (LabeledOperation operation in Zip_Ordered_Operation(unordered))
                {
                    yield return new object[] { source, operation };
                }

                // Union is an odd duck that (currently) orders only the portion that came from an ordered source.
                foreach (LabeledOperation operation in BinaryOperations(RangeSources().First()).Where(op => op.ToString().StartsWith("Union")))
                {
                    yield return new object[] { source, operation };
                }
            }

            // Ordering the output should always be safe
            foreach (object[] parameters in BinaryUnorderedOperators())
            {
                foreach (LabeledOperation ordering in OrderOperators())
                {
                    yield return new[] { parameters[0], ((LabeledOperation)parameters[1]).Append(ordering) };
                }
            }
        }

        public static IEnumerable<object[]> BinaryFailingOperators()
        {
            LabeledOperation failing = Label("Failing", (start, count, s) => s(start, count).Select<int, int>(x => { throw new DeliberateTestException(); }));
            LabeledOperation source = UnorderedRangeSources().First();

            foreach (LabeledOperation operation in BinaryOperations(UnorderedRangeSources().First()))
            {
                foreach (LabeledOperation other in UnorderedRangeSources())
                {
                    yield return new object[] { other.Append(failing), operation };
                }
                yield return new object[] { Failing, operation };
            }

            foreach (LabeledOperation operation in new[]
                {
                     Label("Except-Fail", (start, count, s) => s(start, count).Except(source.Item(start, count), new FailingEqualityComparer<int>())),
                     Label("GroupJoin-Fail", (start, count, s) => s(start, count).GroupJoin(source.Item(start, count), x => x, y => y, (x, g) => x, new FailingEqualityComparer<int>())),
                     Label("Intersect-Fail", (start, count, s) => s(start, count).Intersect(source.Item(start, count), new FailingEqualityComparer<int>())),
                     Label("Join-Fail", (start, count, s) => s(start, count).Join(source.Item(start, count), x => x, y => y, (x, y) => x, new FailingEqualityComparer<int>())),
                     Label("Union-Fail", (start, count, s) => s(start, count).Union(source.Item(start, count), new FailingEqualityComparer<int>())),
                     Label("Zip-Fail", (start, count, s) => s(start, count).Zip<int, int, int>(source.Item(start, count), (x, y) => { throw new DeliberateTestException(); })),
                })
            {
                yield return new object[] { source, operation };
            }
        }

        #region operators

        private static LabeledOperation Failing = Label("ThrowOnFirstEnumeration", (start, count, source) => Enumerables<int>.ThrowOnEnumeration().AsParallel());

        private static LabeledOperation AsOrdered = Label("AsOrdered", (start, count, source) => source(start, count).AsOrdered());

        private static Func<CancellationToken, LabeledOperation> WithCancellation = token => Label("WithCancellation", (start, count, source) => source(start, count).WithCancellation(token));

        // There are two implementations here to help check that the 1st element is matched to the 1st element.
        private static Func<LabeledOperation, IEnumerable<LabeledOperation>> Zip_Ordered_Operation = sOther => new[] {
            Label("Zip-Ordered-Right:" + sOther.ToString(), (start, count, source) => source(0, count).Zip(sOther.Item(start * 2, count), (x, y) => (x + y) / 2)),
            Label("Zip-Ordered-Left:" + sOther.ToString(), (start, count, source) => sOther.Item(start * 2, count).Zip(source(0, count), (x, y) => (x + y) / 2))
        };

        #endregion operators
    }
}
