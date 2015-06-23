// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace Test
{
    public static class PlinqModesTests
    {
        private static IEnumerable<Labeled<Action<ParVerifier, ParallelQuery<int>>>> EasyUnorderedQueries()
        {
            // Some queries may be brittle, depending on source type - tests may fail to be run in parallel by default..
            // In particular, ParallelEnumerable.Range with Take+Select+foreach failed until the count in Take was increased (from 100).
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("TakeWhile+Select+ToArray",
                (verifier, query) => query.TakeWhile(x => true).Select(x => verifier.Verify(x)).ToArray());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("TakeWhile+Select+foreach",
                (verifier, query) => query.TakeWhile(x => true).Select(x => verifier.Verify(x)).Enumerate());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Select+Take+ToArray",
                (verifier, query) => query.Select(x => verifier.Verify(x)).Take(128).ToArray());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Take+Select+foreach",
                (verifier, query) => query.Take(512).Select(x => verifier.Verify(x)).Enumerate());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Select+ElementAt",
                (verifier, query) => query.Select(x => verifier.Verify(x)).ElementAt(8));
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Select+SelectMany+foreach",
                (verifier, query) => query.Select(x => verifier.Verify(x)).SelectMany((x, i) => Enumerable.Repeat(1, 2)).Enumerate());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("AsUnordered+Select+Select+foreach",
                (verifier, query) => query.AsUnordered().Select(x => verifier.Verify(x)).Select((x, i) => x).Enumerate());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("AsUnordered+Where+Select+First",
                (verifier, query) => query.AsUnordered().Where(x => true).Select(x => verifier.Verify(x)).First());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Select+OrderBy+ToArray",
                (verifier, query) => query.Select(x => verifier.Verify(x)).OrderBy(x => x).ToArray());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Select+OrderBy+foreach",
                (verifier, query) => query.Select(x => verifier.Verify(x)).OrderBy(x => x).Enumerate());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Where+Select+Take+ToArray",
                (verifier, query) => query.Where(x => true).Select(x => verifier.Verify(x)).Take(128).ToArray());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Where+Select+Take+foreach",
                (verifier, query) => query.Where(x => true).Select(x => verifier.Verify(x)).Take(128).Enumerate());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Select+TakeWhile+ToArray",
                (verifier, query) => query.Select(x => verifier.Verify(x)).TakeWhile(x => true).ToArray());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Select+TakeWhile+foreach",
                (verifier, query) => query.Select(x => verifier.Verify(x)).TakeWhile(x => true).Enumerate());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("OrderBy+Select+ElementAt",
                (verifier, query) => query.OrderBy(x => x).Select(x => verifier.Verify(x)).ElementAt(8));
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("OrderBy+Select+foreach",
                (verifier, query) => query.OrderBy(x => x).Select(x => verifier.Verify(x)).Enumerate());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Where+Select+OrderBy+Take+foreach",
                (verifier, query) => query.Where(x => true).Select(x => verifier.Verify(x)).OrderBy(x => x).Take(128).Enumerate());
        }

        private static IEnumerable<Labeled<Action<ParVerifier, ParallelQuery<int>>>> EasyOrderedQueries()
        {
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Where+Select+Concat(AsOrdered+Where)+ToList",
                (verifier, query) => query.Where(x => true).Select(x => verifier.Verify(x)).Concat(Enumerable.Range(0, 1000).AsParallel().AsOrdered().Where(x => true)).ToList());
        }

        /// <summary>
        /// Get a a combination of ranges and "easy" queries to run on them, starting at 0, running for each count in `counts`,
        /// .with each ExecutionMode specified.
        /// </summary>
        /// <remarks>
        /// "Easy" queries are ones PLINQ can trivially parallelize (the overhead would not be a significant factor).
        /// </remarks>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <param name="modes">The ExecutionMode to use.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// the second element is the count, and the third is the execution mode.</returns>
        public static IEnumerable<object[]> EasyQueryData(object[] counts, object[] modes)
        {
            // Test doesn't apply to DOP == 1.  It verifies that work is actually
            // happening in parallel, which won't be the case with DOP == 1.
            if (Environment.ProcessorCount == 1) yield break;

            foreach (object[] results in UnorderedSources.Ranges(counts))
            {
                foreach (var query in EasyUnorderedQueries())
                {
                    foreach (ParallelExecutionMode mode in modes)
                    {
                        yield return new object[] { results[0], results[1], query, mode };
                    }
                }
            }
            foreach (object[] results in Sources.Ranges(counts))
            {
                foreach (var query in EasyOrderedQueries())
                {
                    foreach (ParallelExecutionMode mode in modes)
                    {
                        yield return new object[] { results[0], results[1], query, mode };
                    }
                }
            }
        }

        private static IEnumerable<Labeled<Action<ParVerifier, ParallelQuery<int>>>> HardQueries()
        {
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Select+Where+TakeWhile+ToArray",
                (verifier, query) => query.Select(x => verifier.Verify(x)).Where(x => true).TakeWhile((x, i) => true).ToArray());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Select+Where+TakeWhile+foreach",
                (verifier, query) => query.Select(x => verifier.Verify(x)).Where(x => true).TakeWhile((x, i) => true).Enumerate());
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Where+Select+ElementAt",
                (verifier, query) => query.Where(x => true).Select(x => verifier.Verify(x)).ElementAt(8));
            yield return Labeled.Label<Action<ParVerifier, ParallelQuery<int>>>("Where+Select+foreach",
                (verifier, query) => query.Where(x => true).Select(x => verifier.Verify(x)).Enumerate());
        }

        /// <summary>
        /// Get a a combination of ranges and "hard" queries to run on them, starting at 0, running for each count in `counts`,
        /// .with each ExecutionMode specified.
        /// </summary>
        /// <remarks>
        /// While both modes may be specified, "hard" queries are ones it is difficult to parallelize by default,
        /// so many queries may fail if ExecutionMode.ForceParallism is not the mode specified.
        /// </remarks>
        /// <param name="counts">The sizes of ranges to return.</param>
        /// <param name="modes">The ExecutionMode to use.</param>
        /// <returns>Entries for test data.
        /// The first element is the Labeled{ParallelQuery{int}} range,
        /// the second element is the count, and the third is the execution mode.</returns>
        public static IEnumerable<object[]> HardQueryData(object[] counts, object[] modes)
        {
            // Test doesn't apply to DOP == 1.  It verifies that work is actually
            // happening in parallel, which won't be the case with DOP == 1.
            if (Environment.ProcessorCount == 1) yield break;

            foreach (object[] results in UnorderedSources.Ranges(counts))
            {
                foreach (var query in HardQueries())
                {
                    foreach (ParallelExecutionMode mode in modes)
                    {
                        yield return new object[] { results[0], results[1], query, mode };
                    }
                }
            }
            foreach (object[] results in Sources.Ranges(counts))
            {
                foreach (var query in EasyOrderedQueries())
                {
                    foreach (ParallelExecutionMode mode in modes)
                    {
                        yield return new object[] { results[0], results[1], query, mode };
                    }
                }
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("EasyQueryData", new[] { 1024 }, new[] { ParallelExecutionMode.Default, ParallelExecutionMode.ForceParallelism })]
        [MemberData("HardQueryData", new[] { 1024 }, new[] { ParallelExecutionMode.ForceParallelism })]
        // Check that some queries run in parallel by default, and some require forcing.
        public static void WithExecutionMode(Labeled<ParallelQuery<int>> labeled, int count,
            Labeled<Action<ParVerifier, ParallelQuery<int>>> operation, ParallelExecutionMode mode)
        {
            ParVerifier verifier = new ParVerifier();
            operation.Item(verifier, labeled.Item.WithExecutionMode(mode));
            verifier.AssertPassed();
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void WithExecutionMode_ArgumentException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;

            Assert.Throws<ArgumentException>(() => query.WithExecutionMode((ParallelExecutionMode)2));
        }

        [Fact]
        public static void WithExecutionMode_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).WithExecutionMode(ParallelExecutionMode.Default));
        }

        private static void Enumerate<T>(this IEnumerable<T> e)
        {
            foreach (var x in e) { }
        }

        // A class that checks whether the Verify method got called from at least two threads.
        // The first call to Verify() blocks. If another call to Verify() occurs prior to the timeout
        // then we know that Verify() is getting called from multiple threads.
        public class ParVerifier
        {
            private int _counter = 0;
            private bool _passed = false;
            private const int TimeoutLimit = 30000;

            internal int Verify(int x)
            {
                lock (this)
                {
                    _counter++;
                    if (_counter == 1)
                    {
                        if (Monitor.Wait(this, TimeoutLimit))
                        {
                            _passed = true;
                        }
                    }
                    else if (_counter == 2)
                    {
                        Monitor.Pulse(this);
                    }
                }

                return x;
            }

            internal void AssertPassed()
            {
                Assert.True(_passed);
            }
        }
    }
}
