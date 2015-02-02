using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Test
{
    public static class UnorderedSources
    {
        public const int AdditionalTypeLimit = 1024;

        private static IEnumerable<object[]> Ranges(Func<int, int> start, IEnumerable<int> counts)
        {
            foreach (int count in counts)
            {
                int s = start(count);
                foreach (Labeled<ParallelQuery<int>> query in LabeledRanges(s, count))
                {
                    yield return new object[] { query, count, s };
                }
            }
        }

        public static IEnumerable<object[]> Ranges(int start, IEnumerable<int> counts)
        {
            foreach (object[] parms in Ranges(x => start, counts)) yield return parms;
        }

        // Wrapper for attribute calls
        public static IEnumerable<object[]> Ranges(object[] counts)
        {
            foreach (object[] parms in Ranges(counts.Cast<int>())) yield return parms;
        }

        public static IEnumerable<object[]> Ranges(int start, object[] counts)
        {
            foreach (object[] parms in Ranges(start, counts.Cast<int>())) yield return parms;
        }

        public static IEnumerable<object[]> BinaryRanges(object[] leftCounts, object[] rightCounts)
        {
            foreach (object[] parms in BinaryRanges(leftCounts.Cast<int>(), rightCounts.Cast<int>())) yield return parms;
        }

        // Simple labeled-range source - just return a set of ranges from 0 to each of the counts.
        public static IEnumerable<object[]> Ranges(IEnumerable<int> counts)
        {
            foreach (object[] parms in Ranges(x => 0, counts)) yield return parms.Take(2).ToArray();
        }

        public static IEnumerable<object[]> BinaryRanges(IEnumerable<int> leftCounts, IEnumerable<int> rightCounts)
        {
            foreach (object[] left in Ranges(leftCounts))
            {
                foreach (object[] right in Ranges(rightCounts))
                {
                    yield return left.Concat(right).ToArray();
                }
            }
        }

        public static IEnumerable<object[]> BinaryRanges(IEnumerable<int> leftCounts, Func<int, int, int> rightStart, IEnumerable<int> rightCounts)
        {
            foreach (object[] left in Ranges(leftCounts))
            {
                foreach (object[] right in Ranges(right => rightStart((int)left[1], right), rightCounts))
                {
                    yield return left.Concat(right).ToArray();
                }
            }
        }

        // Allows for a set of output modifiers to be passed.
        // This is useful for things like showing an average (via the use of `x => (double)SumRange(0, x) / x`)
        public static IEnumerable<object[]> Ranges<T>(IEnumerable<int> counts, params Func<int, T>[] modifiers)
        {
            if (modifiers == null || !modifiers.Any())
            {
                foreach (object[] parms in Ranges(counts)) yield return parms;
            }
            else
            {
                foreach (object[] parms in Ranges(counts))
                {
                    int count = (int)parms[1];
                    yield return parms.Concat(modifiers.Select(f => f(count)).Cast<object>()).ToArray();
                }
            }
        }

        // For each count, each modifier has multiple outputs.
        // This is useful for things like dealing with `Max(predicate)`,
        // allowing multiple predicate values for the same source count to be tested.
        // The number of variations is equal to the longest inner enumeration (all others will cycle).
        public static IEnumerable<object[]> Ranges<T>(IEnumerable<int> counts, params Func<int, IEnumerable<T>>[] modifiers)
        {
            if (modifiers == null || !modifiers.Any())
            {
                foreach (object[] parms in Ranges(counts)) yield return parms;
            }
            else
            {
                foreach (object[] parms in Ranges(counts))
                {
                    IEnumerable<IEnumerable<T>> mod = modifiers.Select(f => f((int)parms[1]));

                    for (int i = 0, count = mod.Max(e => e.Count()); i < count; i++)
                    {
                        yield return parms.Concat(mod.Select(e => e.ElementAt(i % e.Count())).Cast<object>()).ToArray();
                    }
                }
            }
        }

        private static IEnumerable<Labeled<ParallelQuery<int>>> LabeledRanges(int start, int count)
        {
            yield return Labeled.Label("ParallelEnumerable.Range", ParallelEnumerable.Range(start, count));
            yield return Labeled.Label("Enumerable.Range", Enumerable.Range(start, count).AsParallel());
            int[] rangeArray = GetRangeArray(start, count);
            yield return Labeled.Label("Array", rangeArray.AsParallel());
            if (count < AdditionalTypeLimit + 1)
            {
                yield return Labeled.Label("Partitioner", Partitioner.Create(rangeArray).AsParallel());
                IList<int> rangeList = rangeArray.ToList();
                yield return Labeled.Label("List", rangeList.AsParallel());
                yield return Labeled.Label("ReadOnlyCollection", new ReadOnlyCollection<int>(rangeList).AsParallel());
            }
        }

        internal static int[] GetRangeArray(int start, int count)
        {
            int[] range = new int[count];
            for (int i = 0; i < count; i++) range[i] = start + i;
            return range;
        }
    }
}
