// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

// NOTE: This file of tests needs to be reviewed, scrubbed, and augmented.

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class ConcurrentTests
    {
        static readonly int s_dop = Environment.ProcessorCount * 2;
        const int IterationCount = 10000;

        [Fact]
        [OuterLoop] // should be a stress test that runs for a while, but needs cleanup
        public void RunConcurrentTests()
        {
            int count = s_dop * IterationCount;

            int[] item1;
            TestConcurrently("BatchBlock", "TryReceive",
                          s => s.TryReceive(out item1),
                          ConstructBatchNewWithNMessages(count), true);

            Tuple<IList<int>, IList<int>> item2;
            TestConcurrently("BatchedJoinBlock", "TryReceive",
                          s => s.TryReceive(out item2),
                          ConstructBatchedJoin2NewWithNMessages(count), true);

            int item3;
            TestConcurrently("BroadcastBlock", "TryReceive",
                          s => s.TryReceive(out item3),
                          ConstructBroadcastNewWithNMessages(count), true);

            int item4;
            TestConcurrently("BufferBlock", "TryReceive",
                          s => s.TryReceive(out item4),
                          ConstructBufferNewWithNMessages(count), true);

            Tuple<int, int> item5;
            TestConcurrently("JoinBlock", "TryReceive",
                          s => s.TryReceive(out item5),
                          ConstructJoinNewWithNMessages(count) as IReceivableSourceBlock<Tuple<int, int>>, true);

            string item6;
            TestConcurrently("TransformBlock", "TryReceive",
                            s => s.TryReceive(out item6),
                            ConstructTransformWithNMessages(count) as IReceivableSourceBlock<string>, true);

            int item7;
            TestConcurrently("TransformManyBlock", "TryReceive",
                            s => s.TryReceive(out item7),
                            ConstructTransformManyWithNMessages(count) as IReceivableSourceBlock<int>, true);

            int item8;
            TestConcurrently("WriteOnceBlock", "TryReceive",
                          s => s.TryReceive(out item8),
                          ConstructWriteOnce(), true);

            TestConcurrently("BatchBlock", "OfferMessage",
                                      t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                                      new BatchBlock<int>(1) as ITargetBlock<int>, DataflowMessageStatus.Accepted);
            TestConcurrently("BatchBlock", "Post",
                          t => t.Post(default(int)),
                          new BatchBlock<int>(1), true);

            TestConcurrently("ActionBlock", "OfferMessage",
                          t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                          new ActionBlock<int>(i => { }) as ITargetBlock<int>, DataflowMessageStatus.Accepted);
            TestConcurrently("ActionBlock", "Post",
                          t => t.Post(default(int)),
                          new ActionBlock<int>(i => { }), true);

            TestConcurrently("TransformBlock", "OfferMessage",
                            t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                            new TransformBlock<int, string>(i => i.ToString()) as ITargetBlock<int>, DataflowMessageStatus.Accepted);
            TestConcurrently("TransformBlock", "Post",
                            t => t.Post(default(int)),
                            new TransformBlock<int, string>(i => i.ToString()), true);
            TestConcurrently("BroadcastBlock", "OfferMessage",
                          t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                          new BroadcastBlock<int>(i => i) as ITargetBlock<int>, DataflowMessageStatus.Accepted);
            TestConcurrently("BroadcastBlock", "Post",
                          t => t.Post(default(int)),
                          new BroadcastBlock<int>(i => i), true);

            TestConcurrently("BufferBlock", "OfferMessage",
                         t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                         new BufferBlock<int>() as ITargetBlock<int>, DataflowMessageStatus.Accepted);
            TestConcurrently("BufferBlock", "Post",
                          t => t.Post(default(int)),
                          new BufferBlock<int>(), true);

            TestConcurrently("TransformManyBlock", "OfferMessage",
                            t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                            new TransformManyBlock<int, int>(i => new int[] { i }) as ITargetBlock<int>, DataflowMessageStatus.Accepted);
            TestConcurrently("TransformManyBlock", "Post",
                            t => t.Post(default(int)),
                            new TransformManyBlock<int, int>(i => new int[] { i }), true);
        }

        // Creates dop tasks that invoke method(arg) concurrently.
        private static Task TestConcurrently<T1, TR>(string blockName, string methodName, Func<T1, TR> method, T1 arg, TR expected)
        {
            const int iterationCount = 2000;
            var ce = new CountdownEvent(s_dop); // used to block tasks until all are ready for execution
            return Task.WhenAll(Enumerable.Range(0, s_dop).Select(_ => Task.Run(() => {
                ce.Signal();
                ce.Wait();
                for (int iteration = 0; iteration < iterationCount; iteration++)
                {
                    var result = method(arg);
                    Assert.True(result.Equals(expected), string.Format("{0} {1}. {2}!={3}", blockName, methodName, result, expected));
                }
            })));
        }

        private static BufferBlock<int> ConstructBufferNewWithNMessages(int messagesCount)
        {
            var block = new BufferBlock<int>();
            block.PostRange(0, messagesCount);
            SpinWait.SpinUntil(() => block.Count == messagesCount); // spin until messages available
            return block;
        }

        private static TransformBlock<int, string> ConstructTransformWithNMessages(int messagesCount)
        {
            var block = new TransformBlock<int, string>(i => i.ToString());
            block.PostRange(0, messagesCount);
            SpinWait.SpinUntil(() => block.OutputCount == messagesCount);
            return block;
        }

        private static TransformManyBlock<int, int> ConstructTransformManyWithNMessages(int messagesCount)
        {
            var block = new TransformManyBlock<int, int>(i => new int[] { i });
            block.PostRange(0, messagesCount);
            SpinWait.SpinUntil(() => block.OutputCount == messagesCount); // spin until messages available
            return block;
        }

        private static BatchBlock<int> ConstructBatchNewWithNMessages(int messagesCount)
        {
            var block = new BatchBlock<int>(1);
            block.PostRange(0, messagesCount);
            SpinWait.SpinUntil(() => block.OutputCount == messagesCount); // spin until messages available
            return block;
        }

        private static BatchedJoinBlock<int, int> ConstructBatchedJoin2NewWithNMessages(int messagesCount)
        {
            var block = new BatchedJoinBlock<int, int>(2);
            for (int i = 0; i < messagesCount; i++)
            {
                block.Target1.Post(i);
                block.Target2.Post(i);
            }
            SpinWait.SpinUntil(() => block.OutputCount == messagesCount); // spin until messages available
            return block;
        }

        private static BroadcastBlock<int> ConstructBroadcastNewWithNMessages(int messagesCount)
        {
            var block = new BroadcastBlock<int>(i => i);
            for (int i = 0; i < messagesCount; i++)
            {
                block.Post(i);
            }

            // Wait until the messages have been properly buffered up. Otherwise TryReceive fails.
            // Since there is no property to check on the BroadcastBlock, just sleep for 100 ms
            Task.Delay(100).Wait();

            return block;
        }

        private static JoinBlock<int, int> ConstructJoinNewWithNMessages(int messagesCount)
        {
            var block = new JoinBlock<int, int>();
            block.Target1.PostRange(0, messagesCount);
            block.Target2.PostRange(0, messagesCount);
            SpinWait.SpinUntil(() => block.OutputCount == messagesCount); // spin until messages available
            return block;
        }

        private static WriteOnceBlock<int> ConstructWriteOnce()
        {
            var block = new WriteOnceBlock<int>(i => i);
            block.Post(0);
            return block;
        }

    }
}
