// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    [OuterLoop]
    public static partial class ParallelQueryCombinationTests
    {
        private const int DefaultStart = 8;
        private const int DefaultSize = 16;

        private const int CountFactor = 8;
        private const int GroupFactor = 8;

        private static readonly Operation DefaultSource = (start, count, ignore) => ParallelEnumerable.Range(start, count);
        private static readonly Labeled<Operation> LabeledDefaultSource = Label("Default", DefaultSource);

        private static readonly Labeled<Operation> Failing = Label("ThrowOnFirstEnumeration", (start, count, source) => Enumerables<int>.ThrowOnEnumeration().AsParallel());

        private static IEnumerable<Labeled<Operation>> UnorderedRangeSources()
        {
            // The difference between this and the existing sources is more control is needed over the range creation.
            // Specifically, start/count won't be known until the nesting level is resolved at runtime.
            yield return Label("ParallelEnumerable.Range", (start, count, ignore) => ParallelEnumerable.Range(start, count));
            yield return Label("Enumerable.Range", (start, count, ignore) => Enumerable.Range(start, count).AsParallel());
            yield return Label("Array", (start, count, ignore) => Enumerable.Range(start, count).ToArray().AsParallel());
            yield return Label("List", (start, count, ignore) => Enumerable.Range(start, count).ToList().AsParallel());
            yield return Label("Partitioner", (start, count, ignore) => Partitioner.Create(Enumerable.Range(start, count).ToArray()).AsParallel());

            // PLINQ doesn't currently have any special code paths for readonly collections.  If it ever does, this should be uncommented.
            // yield return Label("ReadOnlyCollection", (start, count, ignore) => new System.Collections.ReadOnlyCollection<int>(Enumerable.Range(start, count).ToList()).AsParallel());
        }

        private static IEnumerable<Labeled<Operation>> RangeSources()
        {
            foreach (Labeled<Operation> source in UnorderedRangeSources())
            {
                yield return source.AsOrdered();
                foreach (Labeled<Operation> ordering in OrderOperators())
                {
                    yield return source.Append(ordering);
                }
            }
        }

        private static IEnumerable<Labeled<Operation>> OrderOperators()
        {
            yield return Label("OrderBy", (start, count, source) => source(start, count).OrderBy(x => x));
            yield return Label("OrderByDescending", (start, count, source) => source(start, count).OrderByDescending(x => -x));
            yield return Label("ThenBy", (start, count, source) => source(start, count).OrderBy(x => 0).ThenBy(x => x));
            yield return Label("ThenByDescending", (start, count, source) => source(start, count).OrderBy(x => 0).ThenByDescending(x => -x));
        }

        private static IEnumerable<Labeled<Operation>> ReverseOrderOperators()
        {
            yield return Label("OrderBy-Reversed", (start, count, source) => source(start, count).OrderBy(x => x, ReverseComparer.Instance));
            yield return Label("OrderByDescending-Reversed", (start, count, source) => source(start, count).OrderByDescending(x => -x, ReverseComparer.Instance));
            yield return Label("ThenBy-Reversed", (start, count, source) => source(start, count).OrderBy(x => 0).ThenBy(x => x, ReverseComparer.Instance));
            yield return Label("ThenByDescending-Reversed", (start, count, source) => source(start, count).OrderBy(x => 0).ThenByDescending(x => -x, ReverseComparer.Instance));
        }

        public static IEnumerable<object[]> OrderFailingOperators()
        {
            foreach (Labeled<Operation> operation in new[] {
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
                yield return new object[] { LabeledDefaultSource, operation };
            }

            foreach (Labeled<Operation> operation in OrderOperators())
            {
                yield return new object[] { Failing, operation };
            }
        }

        public static IEnumerable<object[]> OrderCancelingOperators()
        {
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("OrderBy-Comparer", (source, cancel) => source.OrderBy(x => x, new CancelingComparer(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("OrderByDescending-Comparer", (source, cancel) => source.OrderByDescending(x => x, new CancelingComparer(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("ThenBy-Comparer", (source, cancel) => source.OrderBy(x => 0).ThenBy(x => x, new CancelingComparer(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("ThenByDescending-Comparer", (source, cancel) => source.OrderBy(x => 0).ThenByDescending(x => x, new CancelingComparer(cancel))) };
        }

        public static IEnumerable<object[]> UnaryOperations()
        {
            yield return new object[] { Label("Cast", (start, count, source) => source(start, count).Cast<int>()) };
            yield return new object[] { Label("DefaultIfEmpty", (start, count, source) => source(start, count).DefaultIfEmpty()) };
            yield return new object[] { Label("Distinct", (start, count, source) => source(start * 2, count * 2).Select(x => x / 2).Distinct(new ModularCongruenceComparer(count))) };
            yield return new object[] { Label("OfType", (start, count, source) => source(start, count).OfType<int>()) };
            yield return new object[] { Label("Reverse", (start, count, source) => source(start, count).Reverse()) };

            yield return new object[] { Label("GroupBy", (start, count, source) => source(start, count * CountFactor).GroupBy(x => (x - start) % count, new ModularCongruenceComparer(count)).Select(g => g.Key + start)) };
            yield return new object[] { Label("GroupBy-ElementSelector", (start, count, source) => source(start, count * CountFactor).GroupBy(x => x % count, y => y + 1, new ModularCongruenceComparer(count)).Select(g => g.Min() - 1)) };
            yield return new object[] { Label("GroupBy-ResultSelector", (start, count, source) => source(start, count * CountFactor).GroupBy(x => (x - start) % count, (key, g) => key + start, new ModularCongruenceComparer(count))) };
            yield return new object[] { Label("GroupBy-ElementSelector-ResultSelector", (start, count, source) => source(start, count * CountFactor).GroupBy(x => x % count, y => y + 1, (key, g) => g.Min() - 1, new ModularCongruenceComparer(count))) };

            yield return new object[] { Label("Select", (start, count, source) => source(start - count, count).Select(x => x + count)) };
            yield return new object[] { Label("Select-Index", (start, count, source) => source(start - count, count).Select((x, index) => x + count)) };

            yield return new object[] { Label("SelectMany", (start, count, source) => source(0, (count - 1) / CountFactor + 1).SelectMany(x => Enumerable.Range(start + x * CountFactor, Math.Min(CountFactor, count - x * CountFactor)))) };
            yield return new object[] { Label("SelectMany-Index", (start, count, source) => source(0, (count - 1) / CountFactor + 1).SelectMany((x, index) => Enumerable.Range(start + x * CountFactor, Math.Min(CountFactor, count - x * CountFactor)))) };
            yield return new object[] { Label("SelectMany-ResultSelector", (start, count, source) => source(0, (count - 1) / CountFactor + 1).SelectMany(x => Enumerable.Range(0, Math.Min(CountFactor, count - x * CountFactor)), (group, element) => start + group * CountFactor + element)) };
            yield return new object[] { Label("SelectMany-Index-ResultSelector", (start, count, source) => source(0, (count - 1) / CountFactor + 1).SelectMany((x, index) => Enumerable.Range(0, Math.Min(CountFactor, count - x * CountFactor)), (group, element) => start + group * CountFactor + element)) };

            yield return new object[] { Label("Where", (start, count, source) => source(start - count / 2, count * 2).Where(x => x >= start && x < start + count)) };
            yield return new object[] { Label("Where-Index", (start, count, source) => source(start - count / 2, count * 2).Where((x, index) => x >= start && x < start + count)) };

            yield return new object[] { Label("WithCancellation", (start, count, source) => source(start, count).WithCancellation(CancellationToken.None)) };
            yield return new object[] { Label("WithDegreesOfParallelism", (start, count, source) => source(start, count).WithDegreeOfParallelism(Environment.ProcessorCount)) };
            yield return new object[] { Label("WithExecutionMode", (start, count, source) => source(start, count).WithExecutionMode(ParallelExecutionMode.Default)) };
            yield return new object[] { Label("WithMergeOptions", (start, count, source) => source(start, count).WithMergeOptions(ParallelMergeOptions.Default)) };
        }

        public static IEnumerable<object[]> UnaryUnorderedOperators()
        {
            foreach (Labeled<Operation> source in UnorderedRangeSources())
            {
                foreach (Labeled<Operation> operation in UnaryOperations().Select(i => i[0]))
                {
                    yield return new object[] { source, operation };
                }
            }
        }

        private static IEnumerable<Labeled<Operation>> SkipTakeOperations()
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
            foreach (Labeled<Operation> source in RangeSources())
            {
                foreach (Labeled<Operation> operation in SkipTakeOperations())
                {
                    yield return new object[] { source, operation };
                }
            }
        }

        public static IEnumerable<object[]> UnaryOperators()
        {
            // Apply an ordered source to each operation
            foreach (Labeled<Operation> source in RangeSources())
            {
                foreach (Labeled<Operation> operation in UnaryOperations().Select(i => i[0]).Where(op => !op.ToString().Contains("Reverse")))
                {
                    yield return new object[] { source, operation };
                }
            }

            Labeled<Operation> reverse = UnaryOperations().Select(i => (Labeled<Operation>)i[0]).Where(op => op.ToString().Contains("Reverse")).Single();
            foreach (Labeled<Operation> source in UnorderedRangeSources())
            {
                foreach (Labeled<Operation> ordering in ReverseOrderOperators())
                {
                    yield return new object[] { source.Append(ordering), reverse };
                }
            }

            // Apply ordering to the output of each operation
            foreach (Labeled<Operation> source in UnorderedRangeSources())
            {
                foreach (Labeled<Operation> operation in UnaryOperations().Select(i => i[0]))
                {
                    foreach (Labeled<Operation> ordering in OrderOperators())
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
            foreach (Labeled<Operation> operation in new[] {
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
                yield return new object[] { LabeledDefaultSource, operation };
            }

            foreach (Labeled<Operation> operation in UnaryOperations().Select(i => i[0]).Cast<Labeled<Operation>>().Concat(SkipTakeOperations()))
            {
                yield return new object[] { Failing, operation };
            }
        }

        public static IEnumerable<object[]> UnaryCancelingOperators()
        {
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Distinct", (source, cancel) => source.Distinct(new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("GroupBy-Comparer", (source, cancel) => source.GroupBy(x => x, new CancelingEqualityComparer<int>(cancel)).Select(g => g.Key)) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("SelectMany", (source, cancel) => source.SelectMany(x => { cancel(); return new[] { x }; })) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("SelectMany-Index", (source, cancel) => source.SelectMany((x, index) => { cancel(); return new[] { x }; })) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("SelectMany-ResultSelector", (source, cancel) => source.SelectMany(x => new[] { x }, (group, elem) => { cancel(); return elem; })) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("SelectMany-Index-ResultSelector", (source, cancel) => source.SelectMany((x, index) => new[] { x }, (group, elem) => { cancel(); return elem; })) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("SkipWhile", (source, cancel) => source.SkipWhile(x => { cancel(); return true; })) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("SkipWhile-Index", (source, cancel) => source.SkipWhile((x, index) => { cancel(); return true; })) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("TakeWhile", (source, cancel) => source.TakeWhile(x => { cancel(); return true; })) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("TakeWhile-Index", (source, cancel) => source.TakeWhile((x, index) => { cancel(); return true; })) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Where", (source, cancel) => source.Where(x => { cancel(); return true; })) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Where-Index", (source, cancel) => source.Where((x, index) => { cancel(); return true; })) };
        }

        private static IEnumerable<Labeled<Operation>> BinaryOperations(Labeled<Operation> otherSource)
        {
            string label = otherSource.ToString();
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

        public static IEnumerable<object[]> BinaryOperations()
        {
            return BinaryOperations(LabeledDefaultSource).Select(op => new object[] { op });
        }

        public static IEnumerable<object[]> BinaryUnorderedOperators()
        {
            foreach (Labeled<Operation> source in UnorderedRangeSources())
            {
                // Operations having multiple paths to check.
                foreach (Labeled<Operation> operation in BinaryOperations(LabeledDefaultSource))
                {
                    yield return new object[] { source, operation };
                }
            }
        }

        public static IEnumerable<object[]> BinaryOperators()
        {
            foreach (Labeled<Operation> source in RangeSources())
            {
                // Each binary can work differently, depending on which of the two source queries (or both) is ordered.

                // For most, only the ordering of the first query is important
                foreach (Labeled<Operation> operation in BinaryOperations(LabeledDefaultSource).Where(op => !(op.ToString().StartsWith("Union") || op.ToString().StartsWith("Zip") || op.ToString().StartsWith("Concat")) && op.ToString().Contains("Right")))
                {
                    yield return new object[] { source, operation };
                }

                // For Concat and Union, both sources must be ordered
                foreach (var operation in BinaryOperations(RangeSources().First()).Where(op => op.ToString().StartsWith("Concat") || op.ToString().StartsWith("Union")))
                {
                    yield return new object[] { source, operation };
                }

                // Zip is the same as Concat, but has a special check for matching indices (as compared to unordered)
                foreach (Labeled<Operation> operation in new[] {
                    Label("Zip-Ordered-Right", (start, count, s) => s(0, count).Zip(DefaultSource(start * 2, count).AsOrdered(), (x, y) => (x + y) / 2)),
                    Label("Zip-Ordered-Left", (start, count, s) => DefaultSource(start * 2, count).AsOrdered().Zip(s(0, count), (x, y) => (x + y) / 2))
                })
                {
                    yield return new object[] { source, operation };
                }
            }

            // Ordering the output should always be safe
            foreach (object[] parameters in BinaryUnorderedOperators())
            {
                foreach (Labeled<Operation> ordering in OrderOperators())
                {
                    yield return new[] { parameters[0], ((Labeled<Operation>)parameters[1]).Append(ordering) };
                }
            }
        }

        public static IEnumerable<object[]> BinaryFailingOperators()
        {
            Labeled<Operation> failing = Label("Failing", (start, count, s) => s(start, count).Select<int, int>(x => { throw new DeliberateTestException(); }));

            foreach (Labeled<Operation> operation in BinaryOperations(LabeledDefaultSource))
            {
                yield return new object[] { LabeledDefaultSource.Append(failing), operation };
                yield return new object[] { Failing, operation };
            }

            foreach (Labeled<Operation> operation in new[]
                {
                     Label("Except-Fail", (start, count, s) => s(start, count).Except(DefaultSource(start, count), new FailingEqualityComparer<int>())),
                     Label("GroupJoin-Fail", (start, count, s) => s(start, count).GroupJoin(DefaultSource(start, count), x => x, y => y, (x, g) => x, new FailingEqualityComparer<int>())),
                     Label("Intersect-Fail", (start, count, s) => s(start, count).Intersect(DefaultSource(start, count), new FailingEqualityComparer<int>())),
                     Label("Join-Fail", (start, count, s) => s(start, count).Join(DefaultSource(start, count), x => x, y => y, (x, y) => x, new FailingEqualityComparer<int>())),
                     Label("Union-Fail", (start, count, s) => s(start, count).Union(DefaultSource(start, count), new FailingEqualityComparer<int>())),
                     Label("Zip-Fail", (start, count, s) => s(start, count).Zip<int, int, int>(DefaultSource(start, count), (x, y) => { throw new DeliberateTestException(); })),
                })
            {
                yield return new object[] { LabeledDefaultSource, operation };
            }
        }

        public static IEnumerable<object[]> BinaryCancelingOperators()
        {
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Except", (source, cancel) => source.Except(ParallelEnumerable.Range(DefaultStart, EventualCancellationSize), new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Except-Right", (source, cancel) => ParallelEnumerable.Range(DefaultStart, EventualCancellationSize).Except(source, new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("GroupJoin", (source, cancel) => source.GroupJoin(ParallelEnumerable.Range(DefaultStart, EventualCancellationSize), x => x, y => y, (x, g) => x, new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("GroupJoin-Right", (source, cancel) => ParallelEnumerable.Range(DefaultStart, EventualCancellationSize).GroupJoin(source, x => x, y => y, (x, g) => x, new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Intersect", (source, cancel) => source.Intersect(ParallelEnumerable.Range(DefaultStart, EventualCancellationSize), new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Intersect-Right", (source, cancel) => ParallelEnumerable.Range(DefaultStart, EventualCancellationSize).Intersect(source, new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Join", (source, cancel) => source.Join(ParallelEnumerable.Range(DefaultStart, EventualCancellationSize), x => x, y => y, (x, y) => x, new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Join-Right", (source, cancel) => ParallelEnumerable.Range(DefaultStart, EventualCancellationSize).Join(source, x => x, y => y, (x, y) => x, new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Union", (source, cancel) => source.Union(ParallelEnumerable.Range(DefaultStart, EventualCancellationSize), new CancelingEqualityComparer<int>(cancel))) };
            yield return new object[] { Labeled.Label<Func<ParallelQuery<int>, Action, ParallelQuery<int>>>("Union-Right", (source, cancel) => ParallelEnumerable.Range(DefaultStart, EventualCancellationSize).Union(source, new CancelingEqualityComparer<int>(cancel))) };
        }

        public delegate ParallelQuery<int> Operation(int start, int count, Operation source = null);

        public static Labeled<Operation> Label(string label, Operation item)
        {
            return Labeled.Label(label, item);
        }

        public static Labeled<Operation> Append(this Labeled<Operation> item, Labeled<Operation> next)
        {
            Operation op = item.Item;
            Operation nxt = next.Item;
            return Label(item.ToString() + "|" + next.ToString(), (start, count, source) => nxt(start, count, (s, c, ignore) => op(s, c, source)));
        }

        public static Labeled<Operation> AsOrdered(this Labeled<Operation> query)
        {
            return query.Append(Label("AsOrdered", (start, count, source) => source(start, count).AsOrdered()));
        }
    }
}
