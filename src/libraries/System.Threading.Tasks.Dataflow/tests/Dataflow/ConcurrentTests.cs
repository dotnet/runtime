// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockTests : DataflowBlockTestBase
    {
        //[Fact(Skip = "Outerloop")]
        public void RunConcurrentTests()
        {
            int[] item1;
            Assert.True(TestConcurrently("BatchBlock", "TryReceive",
                          s => s.TryReceive(out item1),
                          ConstructBatchNewWithNMessages(Parallelism.ActualDegreeOfParallelism * s_iterationCount) as IReceivableSourceBlock<int[]>, true));

            Tuple<IList<int>, IList<int>> item2;
            Assert.True(TestConcurrently("BatchedJoinBlock", "TryReceive",
                          s => s.TryReceive(out item2),
                          ConstructBatchedJoin2NewWithNMessages(Parallelism.ActualDegreeOfParallelism * s_iterationCount) as IReceivableSourceBlock<Tuple<IList<int>, IList<int>>>, true));

            int item3;
            Assert.True(TestConcurrently("BroadcastBlock", "TryReceive",
                          s => s.TryReceive(out item3),
                          ConstructBroadcastNewWithNMessages(Parallelism.ActualDegreeOfParallelism * s_iterationCount) as IReceivableSourceBlock<int>, true));

            int item4;
            Assert.True(TestConcurrently("BufferBlock", "TryReceive",
                          s => s.TryReceive(out item4),
                          ConstructBufferNewWithNMessages(Parallelism.ActualDegreeOfParallelism * s_iterationCount) as IReceivableSourceBlock<int>, true));

            Tuple<int, int> item5;
            Assert.True(TestConcurrently("JoinBlock", "TryReceive",
                          s => s.TryReceive(out item5),
                          ConstructJoinNewWithNMessages(Parallelism.ActualDegreeOfParallelism * s_iterationCount) as IReceivableSourceBlock<Tuple<int, int>>, true));

            string item6;
            Assert.True(TestConcurrently("TransformBlock", "TryReceive",
                            s => s.TryReceive(out item6),
                            ConstructTransformWithNMessages(Parallelism.ActualDegreeOfParallelism * s_iterationCount) as IReceivableSourceBlock<string>, true));

            int item7;
            Assert.True(TestConcurrently("TransformManyBlock", "TryReceive",
                            s => s.TryReceive(out item7),
                            ConstructTransformManyWithNMessages(Parallelism.ActualDegreeOfParallelism * s_iterationCount) as IReceivableSourceBlock<int>, true));

            int item8;
            Assert.True(TestConcurrently("WriteOnceBlock", "TryReceive",
                          s => s.TryReceive(out item8),
                          ConstructWriteOnceNewWithNMessages(Parallelism.ActualDegreeOfParallelism * s_iterationCount) as IReceivableSourceBlock<int>, true));

            Assert.True(TestConcurrently("BatchBlock", "OfferMessage",
                                      t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                                      new BatchBlock<int>(1) as ITargetBlock<int>, DataflowMessageStatus.Accepted));
            Assert.True(TestConcurrently("BatchBlock", "Post",
                          t => t.Post(default(int)),
                          new BatchBlock<int>(1), true));

            Assert.True(TestConcurrently("ActionBlock", "OfferMessage",
                          t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                          new ActionBlock<int>(i => { }) as ITargetBlock<int>, DataflowMessageStatus.Accepted));
            Assert.True(TestConcurrently("ActionBlock", "Post",
                          t => t.Post(default(int)),
                          new ActionBlock<int>(i => { }), true));

            Assert.True(TestConcurrently("TransformBlock", "OfferMessage",
                            t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                            new TransformBlock<int, string>(i => i.ToString()) as ITargetBlock<int>, DataflowMessageStatus.Accepted));
            Assert.True(TestConcurrently("TransformBlock", "Post",
                            t => t.Post(default(int)),
                            new TransformBlock<int, string>(i => i.ToString()), true));
            Assert.True(TestConcurrently("BroadcastBlock", "OfferMessage",
                          t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                          new BroadcastBlock<int>(i => i) as ITargetBlock<int>, DataflowMessageStatus.Accepted));
            Assert.True(TestConcurrently("BroadcastBlock", "Post",
                          t => t.Post(default(int)),
                          new BroadcastBlock<int>(i => i), true));

            Assert.True(TestConcurrently("BufferBlock", "OfferMessage",
                         t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                         new BufferBlock<int>() as ITargetBlock<int>, DataflowMessageStatus.Accepted));
            Assert.True(TestConcurrently("BufferBlock", "Post",
                          t => t.Post(default(int)),
                          new BufferBlock<int>(), true));

            Assert.True(TestConcurrently("TransformManyBlock", "OfferMessage",
                            t => t.OfferMessage(new DataflowMessageHeader(1), default(int), null, false), // Message ID doesn't matter because consumeTosAccept:false
                            new TransformManyBlock<int, int>(i => new int[] { i }) as ITargetBlock<int>, DataflowMessageStatus.Accepted));
            Assert.True(TestConcurrently("TransformManyBlock", "Post",
                            t => t.Post(default(int)),
                            new TransformManyBlock<int, int>(i => new int[] { i }), true));

            // Intentionally skipping testing OfferMessage and Post concurrently.
            Assert.True(TestConcurrently("WriteOnceBlock", "ConsumeMessage",
                          s => ISourceBlockTestHelper.TestConsumeMessage(s),
                          ConstructWriteOnceNewWithNMessages(Parallelism.ActualDegreeOfParallelism * s_iterationCount) as ISourceBlock<int>, true));
        }
    }
}
