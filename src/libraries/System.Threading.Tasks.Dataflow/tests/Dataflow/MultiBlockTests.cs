// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockTests : DataflowBlockTestBase
    {
        [Fact]        
        public void RunMultiBlockTests()
        {
            Assert.True(RunNetworkTest(BuffersToNonGreedyJoinToAction));
            Assert.True(RunNetworkTest(TransformToAction));
            Assert.True(RunNetworkTest(TransformThroughFilterToAction));
            Assert.True(RunNetworkTest(TransformThroughDiscardingFilterToAction));
            Assert.True(RunNetworkTest(TenTransformsToAction));
            Assert.True(RunNetworkTest(BatchGreedyToAction));
            Assert.True(RunNetworkTest(WriteOnceToAction));
            Assert.True(RunNetworkTest(BatchedJoinToAction));
            Assert.True(RunNetworkTest(BufferBlocksToBatchNonGreedyToAction));
            Assert.True(RunNetworkTest(BroadcastToActions));
            Assert.True(RunNetworkTest(TransformManyEnumerableToAction));
            Assert.True(RunNetworkTest(ActionPingPong));
            Assert.True(RunNetworkTest(TransformPingPong));
        }

        private static bool RunNetworkTest(Func<bool> test)
        {
            var result = test();
            return result;
        }

        internal static bool TransformToAction()
        {
            bool passed = true;
            const int ITERS = 2;

            var t = new TransformBlock<int, int>(i => i * 2);
            int completedCount = 0;
            int prev = -2;
            var c = new ActionBlock<int>(i =>
            {
                completedCount++;
                if (i != prev + 2) passed &= false;
                prev = i;
            });
            t.LinkWithCompletion(c);

            for (int i = 0; i < ITERS; i++) t.Post(i);
            t.Complete();
            c.Completion.Wait();
            Assert.True(completedCount == ITERS);

            return passed;
        }

        internal static bool TransformThroughFilterToAction()
        {
            const int ITERS = 2;
            int completedCount = 0;

            var t = new TransformBlock<int, int>(i => i);
            var c = new ActionBlock<int>(i => completedCount++);

            t.LinkTo(c, i => true);
            t.Completion.ContinueWith(_ => c.Complete(), TaskScheduler.Default);

            for (int i = 0; i < ITERS; i++) t.Post(i);
            t.Complete();
            c.Completion.Wait();

            return completedCount == ITERS;
        }

        internal static bool TransformThroughDiscardingFilterToAction()
        {
            const int ITERS = 2;
            int completedCount = 0;

            var t = new TransformBlock<int, int>(i => i);
            var c = new ActionBlock<int>(i => completedCount++);

            t.LinkTo(c, i => i % 2 == 0);
            t.LinkTo(DataflowBlock.NullTarget<int>());
            t.Completion.ContinueWith(_ => c.Complete(), TaskScheduler.Default);

            for (int i = 0; i < ITERS; i++) t.Post(i);
            t.Complete();
            c.Completion.Wait();

            return completedCount == ITERS / 2;
        }

        internal static bool TenTransformsToAction()
        {
            const int ITERS = 2;
            var first = new TransformBlock<int, int>(item => item);

            TransformBlock<int, int> t = first;
            for (int i = 0; i < 9; i++)
            {
                var next = new TransformBlock<int, int>(item => item);
                t.LinkWithCompletion(next);
                t = next;
            }
            int completedCount = 0;
            var last = new ActionBlock<int>(i => completedCount++);
            t.LinkWithCompletion(last);

            for (int i = 0; i < ITERS; i++) first.Post(i);
            first.Complete();
            last.Completion.Wait();

            return completedCount == ITERS;
        }

        internal static bool BatchGreedyToAction()
        {
            const int ITERS = 2;
            var b = new BatchBlock<int>(1);
            int completedCount = 0;
            var c = new ActionBlock<int[]>(i => completedCount++);
            b.LinkWithCompletion(c);

            for (int i = 0; i < ITERS; i++) b.Post(i);

            b.Complete();
            c.Completion.Wait();

            return completedCount == ITERS / b.BatchSize;
        }

        internal static bool WriteOnceToAction()
        {
            const int ITERS = 2;
            int completedCount = 0;
            var c = new ActionBlock<int>(i => completedCount++);
            var singleAssignments = Enumerable.Range(0, ITERS).Select(_ =>
            {
                var s = new WriteOnceBlock<int>(i => i);
                s.LinkTo(c);
                return s;
            }).ToList();
            Task.Factory.ContinueWhenAll(singleAssignments.Select(s => s.Completion).ToArray(), _ => c.Complete(), 
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

            foreach (var s in singleAssignments) s.Post(1);
            c.Completion.Wait();

            return completedCount == ITERS;
        }

        internal static bool BatchedJoinToAction()
        {
            const int ITERS = 2;
            var b = new BatchedJoinBlock<int, int>(1);

            int completedCount = 0;
            var c = new ActionBlock<Tuple<IList<int>, IList<int>>>(i => completedCount++);
            b.LinkWithCompletion(c);

            for (int i = 0; i < ITERS; i++)
            {
                if (i % 2 == 0) b.Target1.Post(i);
                else b.Target2.Post(i);
            }
            b.Target1.Complete();
            b.Target2.Complete();
            c.Completion.Wait();

            return (completedCount == ITERS / b.BatchSize);
        }

        internal static bool BufferBlocksToBatchNonGreedyToAction()
        {
            const int ITERS = 2;
            var inputs = Enumerable.Range(0, 1).Select(_ => new BufferBlock<int>()).ToList();
            var b = new BatchBlock<int>(inputs.Count);
            int completedCount = 0;
            var c = new ActionBlock<int[]>(i => completedCount++);

            foreach (var input in inputs) input.LinkTo(b);
            Task.Factory.ContinueWhenAll(inputs.Select(i => i.Completion).ToArray(), _ => b.Complete(),
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            b.LinkWithCompletion(c);

            for (int i = 0; i < ITERS; i++)
            {
                inputs[i % inputs.Count].Post(i);
            }
            foreach (var input in inputs) input.Complete();

            c.Completion.Wait();

            return (completedCount == ITERS / b.BatchSize);
        }

        internal static bool BroadcastToActions()
        {
            const int ITERS = 2;
            var b = new BroadcastBlock<int>(i => i);
            int completedCount = 0;
            var tasks = Enumerable.Range(0, 1).Select(_ =>
            {
                var c = new ActionBlock<int>(i => Interlocked.Increment(ref completedCount));
                b.LinkWithCompletion(c);
                return c.Completion;
            }).ToArray();

            var posts = ITERS / tasks.Length;
            for (int i = 0; i < posts; i++) b.Post(i);
            b.Complete();
            Task.WaitAll(tasks);

            return completedCount == ITERS;
        }

        internal static bool TransformManyEnumerableToAction()
        {
            const int ITERS = 2;
            var data = new[] { 1 };
            var tm = new TransformManyBlock<int, int>(i => data);

            int completedCount = 0;
            var c = new ActionBlock<int>(i => completedCount++);
            tm.LinkWithCompletion(c);

            for (int i = 0; i < ITERS; i++) tm.Post(i);
            tm.Complete();
            c.Completion.Wait();

            return completedCount == ITERS;
        }

        internal static bool ActionPingPong()
        {
            const int ITERS = 2;
            using (ManualResetEventSlim mres = new ManualResetEventSlim())
            {
                ActionBlock<int> c1 = null, c2 = null;
                c1 = new ActionBlock<int>(i => c2.Post(i + 1));
                c2 = new ActionBlock<int>(i =>
                {
                    if (i >= ITERS) mres.Set();
                    else c1.Post(i + 1);
                });
                c1.Post(0);
                mres.Wait();
                return true;
            }
        }

        internal static bool TransformPingPong()
        {
            const int ITERS = 2;
            TransformBlock<int, int> t1 = null, t2 = null;
            t1 = new TransformBlock<int, int>(i =>
            {
                if (i >= ITERS) t2.Complete();
                return i + 1;
            });
            t2 = new TransformBlock<int, int>(i => i + 1);
            t1.LinkTo(t2);
            t2.LinkTo(t1);

            t1.Post(0);
            t2.Completion.Wait();
            return true;
        }

        internal static bool BuffersToNonGreedyJoinToAction()
        {
            bool passed = true;

            const int ITERS = 2;
            var b1 = new BufferBlock<string>();
            var b2 = new BufferBlock<int>();
            var j = new JoinBlock<string, int>(new GroupingDataflowBlockOptions { Greedy = false });
            b1.LinkWithCompletion(j.Target1);
            b2.LinkWithCompletion(j.Target2);
            var a = new ActionBlock<Tuple<string, int>>(t => Assert.True((t.Item1 == t.Item2.ToString())));
            j.LinkWithCompletion(a);

            for (int i = 0; i < ITERS; i++)
            {
                b1.Post(i.ToString());
                b2.Post(i);
            }
            b1.Complete();
            b2.Complete();

            a.Completion.Wait();

            return passed;
        }
    }
}
