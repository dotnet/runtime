// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Linq.Parallel.Tests
{
    public static class UnorderedSources
    {
        /// <summary>
        /// Returns a default ParallelQuery source.
        /// </summary>
        /// For most instances when dealing with unordered input, the individual source does not matter.
        ///
        /// Instead, that is reserved for ordered, where partitioning and dealing with indices has important
        /// secondary effects.  The goal of unordered input, then, is mostly to make sure the query works.
        /// <param name="count">The count of elements.</param>
        /// <returns>A ParallelQuery with elements running from 0 to count - 1</returns>
        public static ParallelQuery<int> Default(int count)
        {
            return Default(0, count);
        }

        /// <summary>
        /// Returns a default ParallelQuery source.
        /// </summary>
        /// For most instances when dealing with unordered input, the individual source does not matter.
        ///
        /// Instead, that is reserved for ordered, where partitioning and dealing with indices has important
        /// secondary effects.  The goal of unordered input, then, is mostly to make sure the query works.
        /// <param name="start">The starting element.</param>
        /// <param name="count">The count of elements.</param>
        /// <returns>A ParallelQuery with elements running from 0 to count - 1</returns>
        public static ParallelQuery<int> Default(int start, int count)
        {
            // Of the underlying types used elsewhere, some have "problems",
            // in the sense that they may be too-easily indexible.
            // For instance, Array and List are both trivially range partitionable and indexible.
            // A parallelized Enumerable.Range is being used (not easily partitionable or indexible),
            // but at the moment ParallelEnumerable.Range is being used for speed and ease of use.
            // ParallelEnumerable.Range is not trivially indexible, but is easily range partitioned.
            return ParallelEnumerable.Range(start, count);
        }


        // Get a set of ranges, of each count in `counts`.
        // The start of each range is determined by passing the count into the `start` predicate.
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

        /// <summary>
        /// Get a set of ranges, starting at `start`, and running for each count in `counts`.
        /// </summary>
        /// <param name="start">The starting element of the range.</param>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// the second element is the count, and the third is the start.</returns>
        public static IEnumerable<object[]> Ranges(int start, IEnumerable<int> counts)
        {
            foreach (object[] parms in Ranges(x => start, counts)) yield return parms;
        }

        /// <summary>
        /// Get a set of ranges, starting at 0, and running for each count in `counts`.
        /// </summary>
        /// <remarks>This version is a wrapper for use from the MemberData attribute.</remarks>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// and the second element is the count</returns>
        public static IEnumerable<object[]> Ranges(int[] counts)
        {
            foreach (object[] parms in Ranges(counts.Cast<int>())) yield return parms;
        }

        /// <summary>
        /// Get a set of ranges, starting at 0, and having OuterLoopCount elements.
        /// </summary>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// and the second element is the count</returns>
        public static IEnumerable<object[]> OuterLoopRanges()
        {
            foreach (object[] parms in Ranges(new[] { Sources.OuterLoopCount })) yield return parms;
        }

        /// <summary>
        /// Get a set of ranges, starting at `start`, and running for each count in `counts`.
        /// </summary>
        /// <remarks>This version is a wrapper for use from the MemberData attribute.</remarks>
        /// <param name="start">The starting element of the range.</param>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// the second element is the count, and the third is the start.</returns>
        public static IEnumerable<object[]> Ranges(int start, int[] counts)
        {
            foreach (object[] parms in Ranges(start, counts.Cast<int>())) yield return parms;
        }

        /// <summary>
        /// Return pairs of ranges, both from 0 to each respective count in `counts`.
        /// </summary>
        /// <remarks>This version is a wrapper for use from the MemberData attribute.</remarks>
        /// <param name="leftCounts">The sizes of left ranges to return.</param>
        /// <param name="rightCounts">The sizes of right ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the left Labeled{ParallelQuery{int}} range, the second element is the left count,
        /// the third element is the right Labeled{ParallelQuery{int}} range, and the fourth element is the right count, .</returns>
        public static IEnumerable<object[]> BinaryRanges(int[] leftCounts, int[] rightCounts)
        {
            foreach (object[] parms in BinaryRanges(leftCounts.Cast<int>(), rightCounts.Cast<int>())) yield return parms;
        }

        /// <summary>
        /// Get a set of ranges, starting at 0, and running for each count in `counts`.
        /// </summary>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// and the second element is the count</returns>
        public static IEnumerable<object[]> Ranges(IEnumerable<int> counts)
        {
            foreach (object[] parms in Ranges(x => 0, counts)) yield return parms.Take(2).ToArray();
        }

        /// <summary>
        /// Return pairs of ranges, both from 0 to each respective count in `counts`.
        /// </summary>
        /// <param name="leftCounts">The sizes of left ranges to return.</param>
        /// <param name="rightCounts">The sizes of right ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the left Labeled{ParallelQuery{int}} range, the second element is the left count,
        /// the third element is the right Labeled{ParallelQuery{int}} range, and the fourth element is the right count.</returns>
        public static IEnumerable<object[]> BinaryRanges(IEnumerable<int> leftCounts, IEnumerable<int> rightCounts)
        {
            IEnumerable<object[]> rightRanges = Ranges(rightCounts);
            foreach (object[] left in Ranges(leftCounts))
            {
                foreach (object[] right in rightRanges)
                {
                    yield return left.Concat(right).ToArray();
                }
            }
        }

        /// <summary>
        /// Return pairs of ranges, for each respective count in `counts`.
        /// </summary>
        /// <param name="leftCounts">The sizes of left ranges to return.</param>
        /// <param name="rightStart">A predicate to determine the start of the right range, by passing the left and right range size.</param>
        /// <param name="rightCounts">The sizes of right ranges to return.</param>
        /// <returns>Entries for test data.
        /// The first element is the left Labeled{ParallelQuery{int}} range, the second element is the left count,
        /// the third element is the right Labeled{ParallelQuery{int}} range, the fourth element is the right count,
        /// and the fifth is the right start.</returns>
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

        /// <summary>
        /// Get a set of ranges, starting at 0, and running for each count in `counts`.
        /// </summary>
        /// <remarks>
        /// This is useful for things like showing an average (via the use of `x => (double)SumRange(0, x) / x`)
        /// </remarks>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <param name="modifiers">A set of modifiers to return as additional parameters.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// the second element is the count, and one additional element for each modifier.</returns>
        public static IEnumerable<object[]> Ranges<T>(IEnumerable<int> counts, Func<int, T> modifiers)
        {
            foreach (object[] parms in Ranges(counts))
            {
                int count = (int)parms[1];
                yield return parms.Concat(new object[] { modifiers(count) }).ToArray();
            }
        }

        /// <summary>
        /// Get a set of ranges, starting at 0, and running for each count in `counts`.
        /// </summary>
        /// <remarks>
        /// This is useful for things like dealing with `Max(predicate)`,
        /// allowing multiple predicate values for the same source count to be tested.
        /// The number of variations is equal to the longest modifier enumeration (all others will cycle).
        /// </remarks>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <param name="modifiers">A set of modifiers to return as additional parameters.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// the second element is the count, and one additional element for each modifier.</returns>
        public static IEnumerable<object[]> Ranges<T>(IEnumerable<int> counts, Func<int, IEnumerable<T>> modifiers)
        {
            foreach (object[] parms in Ranges(counts))
            {
                foreach (T mod in modifiers((int)parms[1]))
                {
                    yield return parms.Concat(new object[] { mod }).ToArray();
                }
            }
        }

        // Return an enumerable which throws on first MoveNext.
        // Useful for testing promptness of cancellation.
        public static IEnumerable<object[]> ThrowOnFirstEnumeration()
        {
            yield return new object[] { Labeled.Label("ThrowOnFirstEnumeration", Enumerables<int>.ThrowOnEnumeration().AsParallel()), 8 };
        }

        private static IEnumerable<Labeled<ParallelQuery<int>>> LabeledRanges(int start, int count)
        {
            yield return Labeled.Label("ParallelEnumerable.Range", ParallelEnumerable.Range(start, count));
            yield return Labeled.Label("Enumerable.Range", Enumerable.Range(start, count).AsParallel());
            int[] rangeArray = Enumerable.Range(start, count).ToArray();
            yield return Labeled.Label("Array", rangeArray.AsParallel());
            IList<int> rangeList = rangeArray.ToList();
            yield return Labeled.Label("List", rangeList.AsParallel());
            yield return Labeled.Label("Partitioner", Partitioner.Create(rangeArray).AsParallel());

            // PLINQ doesn't currently have any special code paths for readonly collections.  If it ever does, this should be uncommented.
            // yield return Labeled.Label("ReadOnlyCollection", new ReadOnlyCollection<int>(rangeList).AsParallel());
        }
    }
}
