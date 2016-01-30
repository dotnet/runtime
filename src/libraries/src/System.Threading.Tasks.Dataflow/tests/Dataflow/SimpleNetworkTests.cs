// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class SimpleNetworkTests
    {
        const int Iterations = 1000;

        [Fact]
        public async Task TransformToAction()
        {
            var t = new TransformBlock<int, int>(i => i * 2);
            int completedCount = 0;
            int prev = -2;
            var c = new ActionBlock<int>(i =>
            {
                completedCount++;
                Assert.Equal(expected: i, actual: prev + 2);
                prev = i;
            });
            t.LinkTo(c, new DataflowLinkOptions { PropagateCompletion = true });

            t.PostRange(0, Iterations);
            t.Complete();

            await c.Completion;
            Assert.True(completedCount == Iterations);
        }

        [Fact]
        public async Task TransformThroughFilterToAction()
        {
            int completedCount = 0;

            var t = new TransformBlock<int, int>(i => i);
            var c = new ActionBlock<int>(i => completedCount++);
            t.LinkTo(c, new DataflowLinkOptions { PropagateCompletion = true }, i => true);

            t.PostRange(0, Iterations);
            t.Complete();

            await c.Completion;
            Assert.Equal(expected: Iterations, actual: completedCount);
        }

        [Fact]
        public async Task TransformThroughDiscardingFilterToAction()
        {
            int completedCount = 0;

            var t = new TransformBlock<int, int>(i => i);
            var c = new ActionBlock<int>(i => completedCount++);

            t.LinkTo(c, new DataflowLinkOptions { PropagateCompletion = true }, i => i % 2 == 0);
            t.LinkTo(DataflowBlock.NullTarget<int>());

            t.PostRange(0, Iterations);
            t.Complete();

            await c.Completion;
            Assert.Equal(expected: Iterations / 2, actual: completedCount);
        }

        [Fact]
        public async Task TenTransformsToAction()
        {
            var first = new TransformBlock<int, int>(item => item);

            TransformBlock<int, int> t = first;
            for (int i = 0; i < 9; i++)
            {
                var next = new TransformBlock<int, int>(item => item);
                t.LinkTo(next, new DataflowLinkOptions { PropagateCompletion = true });
                t = next;
            }
            int completedCount = 0;
            var last = new ActionBlock<int>(i => completedCount++);
            t.LinkTo(last, new DataflowLinkOptions { PropagateCompletion = true });

            first.PostRange(0, Iterations);
            first.Complete();

            await last.Completion;
            Assert.Equal(expected: Iterations, actual: completedCount);
        }

        [Fact]
        public async Task BatchGreedyToAction()
        {
            var b = new BatchBlock<int>(1);
            int completedCount = 0;
            var c = new ActionBlock<int[]>(i => completedCount++);
            b.LinkTo(c, new DataflowLinkOptions { PropagateCompletion = true });

            b.PostRange(0, Iterations);
            b.Complete();

            await c.Completion;
            Assert.Equal(expected: Iterations / b.BatchSize, actual: completedCount);
        }

        [Fact]
        public async Task WriteOnceToAction()
        {
            int completedCount = 0;
            var c = new ActionBlock<int>(i => completedCount++);
            var singleAssignments = Enumerable.Range(0, Iterations).Select(_ =>
            {
                var s = new WriteOnceBlock<int>(i => i);
                s.LinkTo(c);
                return s;
            }).ToList();
            var ignored = Task.WhenAll(singleAssignments.Select(s => s.Completion)).ContinueWith(
                _ => c.Complete(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

            foreach (var s in singleAssignments) s.Post(1);

            await c.Completion;
            Assert.Equal(expected: Iterations, actual: completedCount);
        }

        [Fact]
        public async Task BatchedJoinToAction()
        {
            var b = new BatchedJoinBlock<int, int>(1);

            int completedCount = 0;
            var c = new ActionBlock<Tuple<IList<int>, IList<int>>>(i => completedCount++);
            b.LinkTo(c, new DataflowLinkOptions { PropagateCompletion = true });

            for (int i = 0; i < Iterations; i++)
            {
                if (i % 2 == 0) b.Target1.Post(i);
                else b.Target2.Post(i);
            }
            b.Target1.Complete();
            b.Target2.Complete();

            await c.Completion;
            Assert.Equal(expected: Iterations / b.BatchSize, actual: completedCount);
        }

        [Fact]
        public async Task BufferBlocksToBatchNonGreedyToAction()
        {
            var inputs = Enumerable.Range(0, 1).Select(_ => new BufferBlock<int>()).ToList();
            var b = new BatchBlock<int>(inputs.Count);
            int completedCount = 0;
            var c = new ActionBlock<int[]>(i => completedCount++);

            foreach (var input in inputs) input.LinkTo(b);
            var ignored = Task.WhenAll(inputs.Select(s => s.Completion)).ContinueWith(
                _ => b.Complete(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            b.LinkTo(c, new DataflowLinkOptions { PropagateCompletion = true });

            for (int i = 0; i < Iterations; i++)
            {
                inputs[i % inputs.Count].Post(i);
            }
            foreach (var input in inputs) input.Complete();

            await c.Completion;
            Assert.Equal(expected: Iterations / b.BatchSize, actual: completedCount);
        }

        [Fact]
        public async Task BroadcastToActions()
        {
            var b = new BroadcastBlock<int>(i => i);
            int completedCount = 0;
            var tasks = Enumerable.Range(0, 1).Select(_ =>
            {
                var c = new ActionBlock<int>(i => Interlocked.Increment(ref completedCount));
                b.LinkTo(c, new DataflowLinkOptions { PropagateCompletion = true });
                return c.Completion;
            }).ToArray();

            var posts = Iterations / tasks.Length;
            b.PostRange(0, posts);
            b.Complete();

            await Task.WhenAll(tasks);
            Assert.Equal(expected: Iterations, actual: completedCount);
        }

        [Fact]
        public async Task TransformManyEnumerableToAction()
        {
            var data = new[] { 1 };
            var tm = new TransformManyBlock<int, int>(i => data);

            int completedCount = 0;
            var c = new ActionBlock<int>(i => completedCount++);
            tm.LinkTo(c, new DataflowLinkOptions { PropagateCompletion = true });

            tm.PostRange(0, Iterations);
            tm.Complete();

            await c.Completion;
            Assert.Equal(expected: Iterations, actual: completedCount);
        }

        [Fact]
        public async Task ActionPingPong()
        {
            var tcs = new TaskCompletionSource<bool>();

            ActionBlock<int> c1 = null, c2 = null;
            c1 = new ActionBlock<int>(i => c2.Post(i + 1));
            c2 = new ActionBlock<int>(i => {
                if (i >= Iterations) tcs.SetResult(true);
                else c1.Post(i + 1);
            });
            c1.Post(0);

            await tcs.Task;
        }

        [Fact]
        public async Task TransformPingPong()
        {
            TransformBlock<int, int> t1 = null, t2 = null;
            t1 = new TransformBlock<int, int>(i =>
            {
                if (i >= Iterations) t2.Complete();
                return i + 1;
            });
            t2 = new TransformBlock<int, int>(i => i + 1);
            t1.LinkTo(t2);
            t2.LinkTo(t1);

            t1.Post(0);
            await t2.Completion;
        }

        [Fact]
        public async Task BuffersToNonGreedyJoinToAction()
        {
            var b1 = new BufferBlock<string>();
            var b2 = new BufferBlock<int>();
            var j = new JoinBlock<string, int>(new GroupingDataflowBlockOptions { Greedy = false });
            b1.LinkTo(j.Target1, new DataflowLinkOptions { PropagateCompletion = true });
            b2.LinkTo(j.Target2, new DataflowLinkOptions { PropagateCompletion = true });
            var a = new ActionBlock<Tuple<string, int>>(t => Assert.True((t.Item1 == t.Item2.ToString())));
            j.LinkTo(a, new DataflowLinkOptions { PropagateCompletion = true });

            for (int i = 0; i < Iterations; i++)
            {
                b1.Post(i.ToString());
                b2.Post(i);
            }
            b1.Complete();
            b2.Complete();

            await a.Completion;
        }
    }
}
