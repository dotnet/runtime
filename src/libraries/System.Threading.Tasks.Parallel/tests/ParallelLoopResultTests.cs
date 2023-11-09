// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class ParallelLoopResultTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void ForPLRTests()
        {
            ParallelLoopResult plr =
            Parallel.For(1, 0, delegate (int i, ParallelLoopState ps)
            {
                if (i == 10) ps.Stop();
            });
            PLRcheck(plr, "For-Empty", true, null);

            plr =
            Parallel.For(0, 100, delegate (int i, ParallelLoopState ps)
            {
                //Thread.Sleep(20);
                if (i == 10) ps.Stop();
            });
            PLRcheck(plr, "For-Stop", false, null);

            plr =
            Parallel.For(0, 100, delegate (int i, ParallelLoopState ps)
            {
                //Thread.Sleep(20);
                if (i == 10) ps.Break();
            });
            PLRcheck(plr, "For-Break", false, 10);

            plr =
            Parallel.For(0, 100, delegate (int i, ParallelLoopState ps)
            {
                //Thread.Sleep(20);
            });
            PLRcheck(plr, "For-Completion", true, null);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void ForPLR64Tests()
        {
            ParallelLoopResult plr =
Parallel.For(1L, 0L, delegate (long i, ParallelLoopState ps)
{
    if (i == 10) ps.Stop();
});
            PLRcheck(plr, "For64-Empty", true, null);

            plr =
            Parallel.For(0L, 100L, delegate (long i, ParallelLoopState ps)
            {
                //Thread.Sleep(20);
                if (i == 10) ps.Stop();
            });
            PLRcheck(plr, "For64-Stop", false, null);

            plr =
            Parallel.For(0L, 100L, delegate (long i, ParallelLoopState ps)
            {
                //Thread.Sleep(20);
                if (i == 10) ps.Break();
            });
            PLRcheck(plr, "For64-Break", false, 10);

            plr =
            Parallel.For(0L, 100L, delegate (long i, ParallelLoopState ps)
            {
                //Thread.Sleep(20);
            });
            PLRcheck(plr, "For64-Completion", true, null);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void ForEachPLRTests()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            ParallelLoopResult plr =
            Parallel.ForEach(dict, delegate (KeyValuePair<string, string> kvp, ParallelLoopState ps)
            {
                if (kvp.Value.Equals("Purple")) ps.Stop();
            });
            PLRcheck(plr, "ForEach-Empty", true, null);

            dict.Add("Apple", "Red");
            dict.Add("Banana", "Yellow");
            dict.Add("Pear", "Green");
            dict.Add("Plum", "Red");
            dict.Add("Grape", "Green");
            dict.Add("Cherry", "Red");
            dict.Add("Carrot", "Orange");
            dict.Add("Eggplant", "Purple");

            plr =
            Parallel.ForEach(dict, delegate (KeyValuePair<string, string> kvp, ParallelLoopState ps)
            {
                if (kvp.Value.Equals("Purple")) ps.Stop();
            });

            PLRcheck(plr, "ForEach-Stop", false, null);

            plr =
            Parallel.ForEach(dict, delegate (KeyValuePair<string, string> kvp, ParallelLoopState ps)
            {
                if (kvp.Value.Equals("Purple")) ps.Break();
            });

            PLRcheck(plr, "ForEach-Break", false, 7); // right??

            plr =
            Parallel.ForEach(dict, delegate (KeyValuePair<string, string> kvp, ParallelLoopState ps)
            {
                //if (kvp.Value.Equals("Purple")) ps.Stop();
            });
            PLRcheck(plr, "ForEach-Complete", true, null);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void PartitionerForEachPLRTests()
        {
            //
            // Now try testing Partitionable, OrderablePartitionable
            //
            List<int> intlist = new List<int>();
            for (int i = 0; i < 20; i++)
                intlist.Add(i * i);
            MyPartitioner<int> mp = new MyPartitioner<int>(intlist);

            ParallelLoopResult plr =
            Parallel.ForEach(mp, delegate (int item, ParallelLoopState ps)
            {
                if (item == 0) ps.Stop();
            });
            PLRcheck(plr, "Partitioner-ForEach-Stop", false, null);

            plr = Parallel.ForEach(mp, delegate (int item, ParallelLoopState ps) { });
            PLRcheck(plr, "Partitioner-ForEach-Complete", true, null);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void OrderablePartitionerForEachTests()
        {
            List<int> intlist = new List<int>();
            for (int i = 0; i < 20; i++)
                intlist.Add(i * i);
            OrderablePartitioner<int> mop = Partitioner.Create(intlist, true);


            ParallelLoopResult plr =
            Parallel.ForEach(mop, delegate (int item, ParallelLoopState ps, long index)
            {
                if (index == 2) ps.Stop();
            });
            PLRcheck(plr, "OrderablePartitioner-ForEach-Stop", false, null);

            plr =
            Parallel.ForEach(mop, delegate (int item, ParallelLoopState ps, long index)
            {
                if (index == 2) ps.Break();
            });
            PLRcheck(plr, "OrderablePartitioner-ForEach-Break", false, 2);

            plr =
            Parallel.ForEach(mop, delegate (int item, ParallelLoopState ps, long index)
            {
            });
            PLRcheck(plr, "OrderablePartitioner-ForEach-Complete", true, null);
        }

        private static void PLRcheck(ParallelLoopResult plr, string ttype, bool shouldComplete, int? expectedLBI)
        {
            Assert.Equal(shouldComplete, plr.IsCompleted);
            Assert.Equal(expectedLBI, plr.LowestBreakIteration);
        }

        // Generalized test for testing For-loop results
        private static void ForPLRTest(
            Action<int, ParallelLoopState> body,
            string desc,
            bool excExpected,
            bool shouldComplete,
            bool shouldStop,
            bool shouldBreak)
        {
            ForPLRTest(body, new ParallelOptions(), desc, excExpected, shouldComplete, shouldStop, shouldBreak, false);
        }

        private static void ForPLRTest(
            Action<int, ParallelLoopState> body,
            ParallelOptions parallelOptions,
            string desc,
            bool excExpected,
            bool shouldComplete,
            bool shouldStop,
            bool shouldBreak,
            bool shouldCancel)
        {
            try
            {
                ParallelLoopResult plr = Parallel.For(0, 1, parallelOptions, body);

                Assert.False(shouldCancel);
                Assert.False(excExpected);

                Assert.Equal(shouldComplete, plr.IsCompleted);
                Assert.Equal(shouldStop, plr.LowestBreakIteration == null);
                Assert.Equal(shouldBreak, plr.LowestBreakIteration != null);
            }
            catch (OperationCanceledException)
            {
                Assert.True(shouldCancel);
            }
            catch (AggregateException)
            {
                Assert.True(excExpected);
            }
        }

        // ... and a 64-bit version
        private static void For64PLRTest(
            Action<long, ParallelLoopState> body,
            string desc,
            bool excExpected,
            bool shouldComplete,
            bool shouldStop,
            bool shouldBreak)
        {
            For64PLRTest(body, new ParallelOptions(), desc, excExpected, shouldComplete, shouldStop, shouldBreak, false);
        }

        private static void For64PLRTest(
            Action<long, ParallelLoopState> body,
            ParallelOptions parallelOptions,
            string desc,
            bool excExpected,
            bool shouldComplete,
            bool shouldStop,
            bool shouldBreak,
            bool shouldCancel)
        {
            try
            {
                ParallelLoopResult plr = Parallel.For(0L, 1L, parallelOptions, body);

                Assert.False(shouldCancel);
                Assert.False(excExpected);

                Assert.Equal(shouldComplete, plr.IsCompleted);
                Assert.Equal(shouldStop, plr.LowestBreakIteration == null);
                Assert.Equal(shouldBreak, plr.LowestBreakIteration != null);
            }
            catch (OperationCanceledException)
            {
                Assert.True(shouldCancel);
            }
            catch (AggregateException)
            {
                Assert.True(excExpected);
            }
        }

        // Generalized test for testing ForEach-loop results
        private static void ForEachPLRTest(
            Action<KeyValuePair<int, string>, ParallelLoopState> body,
            string desc,
            bool excExpected,
            bool shouldComplete,
            bool shouldStop,
            bool shouldBreak)
        {
            ForEachPLRTest(body, new ParallelOptions(), desc, excExpected, shouldComplete, shouldStop, shouldBreak, false);
        }

        private static void ForEachPLRTest(
            Action<KeyValuePair<int, string>, ParallelLoopState> body,
            ParallelOptions parallelOptions,
            string desc,
            bool excExpected,
            bool shouldComplete,
            bool shouldStop,
            bool shouldBreak,
            bool shouldCancel)
        {
            Dictionary<int, string> dict = new Dictionary<int, string>();
            dict.Add(1, "one");

            try
            {
                ParallelLoopResult plr = Parallel.ForEach(dict, parallelOptions, body);

                Assert.False(shouldCancel);
                Assert.False(excExpected);

                Assert.Equal(shouldComplete, plr.IsCompleted);
                Assert.Equal(shouldStop, plr.LowestBreakIteration == null);
                Assert.Equal(shouldBreak, plr.LowestBreakIteration != null);
            }
            catch (OperationCanceledException)
            {
                Assert.True(shouldCancel);
            }
            catch (AggregateException)
            {
                Assert.True(excExpected);
            }
        }

        // Generalized test for testing Partitioner ForEach-loop results
        private static void PartitionerForEachPLRTest(
            Action<int, ParallelLoopState> body,
            string desc,
            bool excExpected,
            bool shouldComplete,
            bool shouldStop,
            bool shouldBreak)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < 20; i++) list.Add(i);
            MyPartitioner<int> mp = new MyPartitioner<int>(list);

            try
            {
                ParallelLoopResult plr =
                Parallel.ForEach(mp, body);

                Assert.False(excExpected);

                Assert.Equal(shouldComplete, plr.IsCompleted);
                Assert.Equal(shouldStop, plr.LowestBreakIteration == null);
                Assert.Equal(shouldBreak, plr.LowestBreakIteration != null);
            }
            catch (AggregateException)
            {
                Assert.True(excExpected);
            }
        }

        // Generalized test for testing OrderablePartitioner ForEach-loop results
        private static void OrderablePartitionerForEachPLRTest(
            Action<int, ParallelLoopState, long> body,
            string desc,
            bool excExpected,
            bool shouldComplete,
            bool shouldStop,
            bool shouldBreak)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < 20; i++) list.Add(i);
            OrderablePartitioner<int> mop = Partitioner.Create(list, true);

            try
            {
                ParallelLoopResult plr = Parallel.ForEach(mop, body);

                Assert.False(excExpected);

                Assert.Equal(shouldComplete, plr.IsCompleted);
                Assert.Equal(shouldStop, plr.LowestBreakIteration == null);
                Assert.Equal(shouldBreak, plr.LowestBreakIteration != null);
            }
            catch (AggregateException)
            {
                Assert.True(excExpected);
            }
        }

        // Perform tests on various combinations of Stop()/Break()
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void SimultaneousStopBreakTests()
        {
            //
            // Test 32-bit Parallel.For()
            //


            ForPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Stop();
                ps.Break();
            },
                "Break After Stop",
                true,
                false,
                false,
                false);


            ForPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Break();
                ps.Stop();
            },
                "Stop After Break",
                true,
                false,
                false,
                false);

            CancellationTokenSource cts = new CancellationTokenSource();
            ParallelOptions options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            ForPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Break();
                cts.Cancel();
            },
                options,
                "Cancel After Break",
                false,
                false,
                false,
                false,
                true);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            ForPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Stop();
                cts.Cancel();
            },
                options,
                "Cancel After Stop",
                false,
                false,
                false,
                false,
                true);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            ForPLRTest(delegate (int i, ParallelLoopState ps)
            {
                cts.Cancel();
                ps.Stop();
            },
                options,
                "Stop After Cancel",
                false,
                false,
                false,
                false,
                true);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            ForPLRTest(delegate (int i, ParallelLoopState ps)
            {
                cts.Cancel();
                ps.Break();
            },
                options,
                "Break After Cancel",
                false,
                false,
                false,
                false,
                true);


            ForPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Break();
                try
                {
                    ps.Stop();
                }
                catch { }
            },
                "Stop(caught) after Break",
                false,
                false,
                false,
                true);


            ForPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Stop();
                try
                {
                    ps.Break();
                }
                catch { }
            },
                "Break(caught) after Stop",
                false,
                false,
                true,
                false);

            //
            // Test "vanilla" Parallel.ForEach
            //


            ForEachPLRTest(delegate (KeyValuePair<int, string> kvp, ParallelLoopState ps)
            {
                ps.Break();
                ps.Stop();
            },
               "Stop-After-Break",
               true,
               false,
               false,
               false);


            ForEachPLRTest(delegate (KeyValuePair<int, string> kvp, ParallelLoopState ps)
            {
                ps.Stop();
                ps.Break();
            },
               "Break-after-Stop",
               true,
               false,
               false,
               false);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            ForEachPLRTest(delegate (KeyValuePair<int, string> kvp, ParallelLoopState ps)
            {
                ps.Break();
                cts.Cancel();
            },
                options,
                "Cancel After Break",
                false,
                false,
                false,
                false,
                true);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            ForEachPLRTest(delegate (KeyValuePair<int, string> kvp, ParallelLoopState ps)
            {
                ps.Stop();
                cts.Cancel();
            },
                options,
                "Cancel After Stop",
                false,
                false,
                false,
                false,
                true);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            ForEachPLRTest(delegate (KeyValuePair<int, string> kvp, ParallelLoopState ps)
            {
                cts.Cancel();
                ps.Stop();
            },
                options,
                "Stop After Cancel",
                false,
                false,
                false,
                false,
                true);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            ForEachPLRTest(delegate (KeyValuePair<int, string> kvp, ParallelLoopState ps)
            {
                cts.Cancel();
                ps.Break();
            },
                options,
                "Break After Cancel",
                false,
                false,
                false,
                false,
                true);


            ForEachPLRTest(delegate (KeyValuePair<int, string> kvp, ParallelLoopState ps)
            {
                ps.Break();
                try
                {
                    ps.Stop();
                }
                catch { }
            },
               "Stop(caught)-after-Break",
               false,
               false,
               false,
               true);


            ForEachPLRTest(delegate (KeyValuePair<int, string> kvp, ParallelLoopState ps)
            {
                ps.Stop();
                try
                {
                    ps.Break();
                }
                catch { }
            },
               "Break(caught)-after-Stop",
               false,
               false,
               true,
               false);

            //
            // Test Parallel.ForEach w/ Partitioner
            //


            PartitionerForEachPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Break();
                ps.Stop();
            },
               "Stop-After-Break",
               true,
               false,
               false,
               false);


            PartitionerForEachPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Stop();
                ps.Break();
            },
               "Break-after-Stop",
               true,
               false,
               false,
               false);


            PartitionerForEachPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Break();
                try
                {
                    ps.Stop();
                }
                catch { }
            },
               "Stop(caught)-after-Break",
               false,
               false,
               false,
               true);


            PartitionerForEachPLRTest(delegate (int i, ParallelLoopState ps)
            {
                ps.Stop();
                try
                {
                    ps.Break();
                }
                catch { }
            },
               "Break(caught)-after-Stop",
               false,
               false,
               true,
               false);

            //
            // Test Parallel.ForEach w/ OrderablePartitioner
            //


            OrderablePartitionerForEachPLRTest(delegate (int i, ParallelLoopState ps, long index)
            {
                ps.Break();
                ps.Stop();
            },
               "Stop-After-Break",
               true,
               false,
               false,
               false);


            OrderablePartitionerForEachPLRTest(delegate (int i, ParallelLoopState ps, long index)
            {
                ps.Stop();
                ps.Break();
            },
               "Break-after-Stop",
               true,
               false,
               false,
               false);


            OrderablePartitionerForEachPLRTest(delegate (int i, ParallelLoopState ps, long index)
            {
                ps.Break();
                try
                {
                    ps.Stop();
                }
                catch { }
            },
               "Stop(caught)-after-Break",
               false,
               false,
               false,
               true);


            OrderablePartitionerForEachPLRTest(delegate (int i, ParallelLoopState ps, long index)
            {
                ps.Stop();
                try
                {
                    ps.Break();
                }
                catch { }
            },
               "Break(caught)-after-Stop",
               false,
               false,
               true,
               false);

            //
            // Test 64-bit Parallel.For
            //


            For64PLRTest(delegate (long i, ParallelLoopState ps)
            {
                ps.Stop();
                ps.Break();
            },
                "Break After Stop",
                true,
                false,
                false,
                false);


            For64PLRTest(delegate (long i, ParallelLoopState ps)
            {
                ps.Break();
                ps.Stop();
            },
                "Stop After Break",
                true,
                false,
                false,
                false);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            For64PLRTest(delegate (long i, ParallelLoopState ps)
            {
                ps.Break();
                cts.Cancel();
            },
                options,
                "Cancel After Break",
                false,
                false,
                false,
                false,
                true);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            For64PLRTest(delegate (long i, ParallelLoopState ps)
            {
                ps.Stop();
                cts.Cancel();
            },
                options,
                "Cancel After Stop",
                false,
                false,
                false,
                false,
                true);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            For64PLRTest(delegate (long i, ParallelLoopState ps)
            {
                cts.Cancel();
                ps.Stop();
            },
                options,
                "Stop after Cancel",
                false,
                false,
                false,
                false,
                true);

            cts = new CancellationTokenSource();
            options = new ParallelOptions();
            options.CancellationToken = cts.Token;


            For64PLRTest(delegate (long i, ParallelLoopState ps)
            {
                cts.Cancel();
                ps.Break();
            },
                options,
                "Break after Cancel",
                false,
                false,
                false,
                false,
                true);

            For64PLRTest(delegate (long i, ParallelLoopState ps)
            {
                ps.Break();
                try
                {
                    ps.Stop();
                }
                catch { }
            },
                "Stop(caught) after Break",
                false,
                false,
                false,
                true);

            For64PLRTest(delegate (long i, ParallelLoopState ps)
            {
                ps.Stop();
                try
                {
                    ps.Break();
                }
                catch { }
            },
                "Break(caught) after Stop",
                false,
                false,
                true,
                false);
        }

        #region Helper Classes and Methods

        //
        // Utility class for use w/ Partitioner-style ForEach testing.
        // Created by Cindy Song.
        //
        public class MyPartitioner<TSource> : Partitioner<TSource>
        {
            private IList<TSource> _data;

            public MyPartitioner(IList<TSource> data)
            {
                _data = data;
            }

            public override IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(partitionCount));
                }
                IEnumerator<TSource>[] partitions
                    = new IEnumerator<TSource>[partitionCount];
                IEnumerable<KeyValuePair<long, TSource>> partitionEnumerable = Partitioner.Create(_data, true).GetOrderableDynamicPartitions();
                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = DropIndices(partitionEnumerable.GetEnumerator());
                }
                return partitions;
            }

            public override IEnumerable<TSource> GetDynamicPartitions()
            {
                return DropIndices(Partitioner.Create(_data, true).GetOrderableDynamicPartitions());
            }

            private static IEnumerable<TSource> DropIndices(IEnumerable<KeyValuePair<long, TSource>> source)
            {
                foreach (KeyValuePair<long, TSource> pair in source)
                {
                    yield return pair.Value;
                }
            }

            private static IEnumerator<TSource> DropIndices(IEnumerator<KeyValuePair<long, TSource>> source)
            {
                while (source.MoveNext())
                {
                    yield return source.Current.Value;
                }
            }

            public override bool SupportsDynamicPartitions
            {
                get { return true; }
            }
        }

        #endregion
    }
}
