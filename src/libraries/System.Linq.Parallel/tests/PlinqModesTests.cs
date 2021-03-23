// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class PlinqModesTests
    {
        public static IEnumerable<Labeled<Action<UsedTaskTracker, ParallelQuery<int>>>> EasyUnorderedQueries(int count)
        {
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("TakeWhile+Select+ToArray",
                (verifier, query) => query.TakeWhile(x => true).Select(x => verifier.AddCurrent(x)).ToArray());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("TakeWhile+Select+foreach",
                (verifier, query) => query.TakeWhile(x => true).Select(x => verifier.AddCurrent(x)).Enumerate());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Select+Take+ToArray",
                (verifier, query) => query.Select(x => verifier.AddCurrent(x)).Take(count).ToArray());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Take+Select+foreach",
                (verifier, query) => query.Take(count).Select(x => verifier.AddCurrent(x)).Enumerate());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Select+ElementAt",
                (verifier, query) => query.Select(x => verifier.AddCurrent(x)).ElementAt(count - 1));
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Select+SelectMany+foreach",
                (verifier, query) => query.Select(x => verifier.AddCurrent(x)).SelectMany((x, i) => Enumerable.Repeat(1, 2)).Enumerate());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("AsUnordered+Select+Select+foreach",
                (verifier, query) => query.AsUnordered().Select(x => verifier.AddCurrent(x)).Select((x, i) => x).Enumerate());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("AsUnordered+Where+Select+First",
                (verifier, query) => query.AsUnordered().Where(x => true).Select(x => verifier.AddCurrent(x)).First());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Select+OrderBy+ToArray",
                (verifier, query) => query.Select(x => verifier.AddCurrent(x)).OrderBy(x => x).ToArray());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Select+OrderBy+foreach",
                (verifier, query) => query.Select(x => verifier.AddCurrent(x)).OrderBy(x => x).Enumerate());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Where+Select+Take+ToArray",
                (verifier, query) => query.Where(x => true).Select(x => verifier.AddCurrent(x)).Take(count).ToArray());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Where+Select+Take+foreach",
                (verifier, query) => query.Where(x => true).Select(x => verifier.AddCurrent(x)).Take(count).Enumerate());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Select+TakeWhile+ToArray",
                (verifier, query) => query.Select(x => verifier.AddCurrent(x)).TakeWhile(x => true).ToArray());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Select+TakeWhile+foreach",
                (verifier, query) => query.Select(x => verifier.AddCurrent(x)).TakeWhile(x => true).Enumerate());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("OrderBy+Select+ElementAt",
                (verifier, query) => query.OrderBy(x => x).Select(x => verifier.AddCurrent(x)).ElementAt(count - 1));
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("OrderBy+Select+foreach",
                (verifier, query) => query.OrderBy(x => x).Select(x => verifier.AddCurrent(x)).Enumerate());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Where+Select+OrderBy+Take+foreach",
                (verifier, query) => query.Where(x => true).Select(x => verifier.AddCurrent(x)).OrderBy(x => x).Take(count).Enumerate());
        }

        public static IEnumerable<Labeled<Action<UsedTaskTracker, ParallelQuery<int>>>> EasyOrderedQueries(int count)
        {
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Where+Select+Concat(AsOrdered+Where)+ToList",
                (verifier, query) => query.Where(x => true).Select(x => verifier.AddCurrent(x)).Concat(Enumerable.Range(0, count).AsParallel().AsOrdered().Where(x => true)).ToList());
        }

        public static IEnumerable<Labeled<Action<UsedTaskTracker, ParallelQuery<int>>>> HardQueries(int count)
        {
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Select+Where+TakeWhile+ToArray",
                (verifier, query) => query.Select(x => verifier.AddCurrent(x)).Where(x => true).TakeWhile((x, i) => true).ToArray());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Select+Where+TakeWhile+foreach",
                (verifier, query) => query.Select(x => verifier.AddCurrent(x)).Where(x => true).TakeWhile((x, i) => true).Enumerate());
            yield return Labeled.Label<Action<UsedTaskTracker, ParallelQuery<int>>>("Where+Select+ElementAt",
                (verifier, query) => query.Where(x => true).Select(x => verifier.AddCurrent(x)).ElementAt(count - 1));
        }

        /// <summary>
        /// Get a combination of partitioned data sources, degree of parallelism, expected resulting dop,
        /// query to execute on the data source, and mode of execution.
        /// </summary>
        /// <param name="dop">A set of the desired degrees of parallelism to be employed.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} data source,
        /// the second is the desired dop,
        /// the third is the expected resulting dop,
        /// the fourth is the query to execute on the data source,
        /// and the fifth is the execution mode.</returns>
        public static IEnumerable<object[]> WithExecutionModeQueryData(int[] dops)
        {
            foreach (int dop in dops)
            {
                // Use data sources that have a fixed set of elements in each partition (no load balancing between the partitions).
                // PLINQ will assign a Task to each partition, and no other task will process that partition. As a result, we can
                // verify that we get a known number of tasks doing the processing. (This doesn't guarantee that such tasks are
                // running in parallel, but it's "good enough".  If PLINQ's implementation is ever changed to proactively exit
                // tasks and spawn replicas to continue the processing, ala Parallel.For*, this test will need to be updated.)
                int count = 3 * dop; // 3 chosen arbitrarily as a small value; any positive value will do
                var partitionedRanges = new Labeled<ParallelQuery<int>>[]
                {
                    Labeled.Label("ParallelEnumerable.Range", ParallelEnumerable.Range(0, count)),
                    Labeled.Label("Partitioner.Create", Partitioner.Create(Enumerable.Range(0, count).ToArray(), loadBalance: false).AsParallel())
                };

                // For each source and mode, get both unordered and ordered queries that should easily parallelize for all execution modes
                foreach (ParallelExecutionMode mode in new[] { ParallelExecutionMode.Default, ParallelExecutionMode.ForceParallelism })
                {
                    foreach (Labeled<ParallelQuery<int>> source in partitionedRanges)
                    {
                        foreach (var query in EasyUnorderedQueries(count))
                            yield return new object[] { source, dop, dop, query, mode };

                        foreach (var query in EasyOrderedQueries(count))
                            yield return new object[] { source.Order(), dop, dop, query, mode };
                    }
                }

                // For each source, get queries that are difficult to parallelize and thus only do so with ForceParallelism.
                foreach (Labeled<ParallelQuery<int>> source in partitionedRanges)
                {
                    foreach (var query in HardQueries(count))
                    {
                        yield return new object[] { source, dop, dop, query, ParallelExecutionMode.ForceParallelism }; // should parallelize, thus expected DOP of > 1
                        yield return new object[] { source, dop, 1, query, ParallelExecutionMode.Default }; // won't parallelize, thus expected DOP of 1
                    }
                }
            }
        }

        /// <summary>
        /// Return execution mode combinations, for testing multiple calls to WithExecutionMode
        /// </summary>
        /// <returns>Entries for test data.
        /// Both entries are a ParallelExecutionMode in a Cartesian join.</returns>
        public static IEnumerable<object[]> AllExecutionModes_Multiple()
        {
            ParallelExecutionMode[] modes = new[] { ParallelExecutionMode.Default, ParallelExecutionMode.ForceParallelism };

            foreach (ParallelMergeOptions first in modes)
            {
                foreach (ParallelMergeOptions second in modes)
                {
                    yield return new object[] { first, second };
                }
            }
        }

        // Check that some queries run in parallel by default, and some require forcing.
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(WithExecutionModeQueryData), new[] { 1, 4 })] // DOP of 1 to verify sequential and 4 to verify parallel
        public static void WithExecutionMode(
            Labeled<ParallelQuery<int>> labeled,
            int requestedDop, int expectedDop,
            Labeled<Action<UsedTaskTracker, ParallelQuery<int>>> operation,
            ParallelExecutionMode mode)
        {
            UsedTaskTracker tracker = new UsedTaskTracker();
            operation.Item(tracker, labeled.Item.WithDegreeOfParallelism(requestedDop).WithExecutionMode(mode));
            Assert.Equal(expectedDop, tracker.UniqueTasksCount);
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 2 }, MemberType = typeof(UnorderedSources))]
        public static void WithExecutionMode_ArgumentException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            _ = count;
            ParallelQuery<int> query = labeled.Item;
            AssertExtensions.Throws<ArgumentException>(null, () => query.WithExecutionMode((ParallelExecutionMode)2));
        }

        [Theory]
        [MemberData(nameof(AllExecutionModes_Multiple))]
        public static void WithExecutionMode_Multiple(ParallelExecutionMode first, ParallelExecutionMode second)
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Range(0, 1).WithExecutionMode(first).WithExecutionMode(second));
        }

        [Fact]
        public static void WithExecutionMode_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).WithExecutionMode(ParallelExecutionMode.Default));
        }

        /// <summary>Tracks all of the Tasks from which AddCurrent is called.</summary>
        public sealed class UsedTaskTracker
        {
            private readonly ConcurrentDictionary<int, bool> _taskIdToUsageCount = new ConcurrentDictionary<int, bool>();

            internal int AddCurrent(int x)
            {
                _taskIdToUsageCount.TryAdd(Task.CurrentId.GetValueOrDefault(), true);
                return x;
            }

            internal int UniqueTasksCount { get { return _taskIdToUsageCount.Count; } }
        }
    }
}
