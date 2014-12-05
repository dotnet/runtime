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
        public void RunBroadcastBlockTests()
        {
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new BroadcastBlock<int>(null, new DataflowBlockOptions() { NameFormat = nameFormat }) : new BroadcastBlock<int>(null)));
            Assert.True(ISourceBlockTestHelper.TestLinkTo<int>(ConstructBroadcastNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestReserveMessageAndReleaseReservation<int>(ConstructBroadcastNewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestConsumeMessage<int>(ConstructBroadcastNewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveWithFilter<int>(ConstructBroadcastNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTargetOrder<int>(new BroadcastBlock<int>(i => i), 1));
            Assert.True(ITargetBlockTestHelper.TestPost<int>(new BroadcastBlock<int>(i => i)));
            Assert.True(ITargetBlockTestHelper.TestCompletionTask<int>(new BroadcastBlock<int>(i => i)));
        }

        private static BroadcastBlock<int> ConstructBroadcastNewWithNMessages(int messagesCount)
        {
            var block = new BroadcastBlock<int>(i => i);
            for (int i = 0; i < messagesCount; i++)
            {
                block.Post(i);
            }

            // Wait until the messages have been properly buffered up. 
            // Otherwise TryReceive fails.
            // Since there is no property to check on the BroadcastBlock, just sleep for 2 ms
            Task.Delay(2).Wait();

            return block;
        }

        //[Fact(Skip = "Outerloop")]
        public void TestBroadcastBlockConstructor()
        {
            try
            {
                // without option
                var block = new BroadcastBlock<int>(i => i);
                block = new BroadcastBlock<int>(null);
                // There is no property to verify. If construction itself fails, there will be an exception.

                //with not cancelled token and default scheduler
                block = new BroadcastBlock<int>(i => i, new DataflowBlockOptions { MaxMessagesPerTask = 1 });
                block = new BroadcastBlock<int>(null, new DataflowBlockOptions { MaxMessagesPerTask = 1 });
                // There is no property to verify. If construction itself fails, there will be an exception.

                //with a cancelled token and default scheduler
                var token = new CancellationToken(true);
                block = new BroadcastBlock<int>(i => i, new DataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
                block = new BroadcastBlock<int>(null, new DataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
                // There is no property to verify. If construction itself fails, there will be an exception.
            }
            catch (Exception ex)
            {
                Assert.True(false, "Test Failed, exception thrown during construction: " + ex.Message);
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestBroadcastBlockInvalidArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new BroadcastBlock<int>(i => i, null));
            Assert.True(ITargetBlockTestHelper.TestArgumentsExceptions<int>(new BroadcastBlock<int>(i => i)));
            Assert.True(ISourceBlockTestHelper.TestArgumentsExceptions<int>(new BroadcastBlock<int>(i => i)));
        }

        //[Fact(Skip = "Outerloop")]
        public void RunBroadcastBlockConformanceTests()
        {
            bool localPassed = true;
            {
                // Test posting then receiving
                localPassed = true;
                var bb = new BroadcastBlock<int>(i => i);
                for (int i = 0; i < 2; i++) bb.Post(i);
                Task.Delay(1).Wait();
                localPassed |= bb.Receive() == 1;
                Assert.True(localPassed, string.Format("{0}: Posting then receiving", localPassed ? "Success" : "Failure"));
            }

            {
                // Test receiving then posting
                localPassed = true;
                var bb = new BroadcastBlock<int>(i => i);
                Task.Factory.StartNew(() =>
                {
                    Task.Delay(1).Wait();
                    bb.Post(42);
                });

                localPassed |= bb.Receive() == 42;
                Assert.True(localPassed, string.Format("{0}: Receiving then posting", localPassed ? "Success" : "Failure"));
            }

            {
                // Test broadcasting
                localPassed = true;
                var bb = new BroadcastBlock<int>(i => i + 1);
                var tb1 = new TransformBlock<int, int>(i => i);
                var tb2 = new TransformBlock<int, int>(i => i);
                var tb3 = new TransformBlock<int, int>(i => i);
                bb.LinkTo(tb1);
                bb.LinkTo(tb2);
                bb.LinkTo(tb3);
                for (int i = 0; i < 2; i++)
                {
                    bb.Post(i);
                }
                for (int i = 0; i < 2; i++)
                {
                    localPassed |= tb1.Receive() == i + 1;
                    localPassed |= tb2.Receive() == i + 1;
                    localPassed |= tb3.Receive() == i + 1;
                }

                Assert.True(localPassed, string.Format("{0}: Broadcasting", localPassed ? "Success" : "Failure"));
            }

            // Test using a precanceled token
            {
                localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    var dbo = new DataflowBlockOptions { CancellationToken = cts.Token };
                    var bb = new BroadcastBlock<int>(i => i, dbo);

                    int ignoredValue;
                    IList<int> ignoredValues;
                    localPassed &= bb.LinkTo(new ActionBlock<int>(delegate { })) != null;
                    localPassed &= bb.SendAsync(42).Result == false;
                    localPassed &= ((IReceivableSourceBlock<int>)bb).TryReceiveAll(out ignoredValues) == false;
                    localPassed &= bb.Post(42) == false;
                    localPassed &= bb.TryReceive(out ignoredValue) == false;
                    localPassed &= bb.Completion != null;
                    bb.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }

                Assert.True(localPassed, string.Format("{0}: Precanceled tokens work correctly", localPassed ? "Success" : "Failure"));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestBroadcastCloning()
        {
            // Test cloning when a clone function is provided
            {
                var broadcast = new BroadcastBlock<int>(x => -x);
                Assert.True(broadcast.Post(42), "Expected initial post on cloning Broadcast to succeed");
                Assert.True(broadcast.Receive() == -42, "Expected Receive'd data to be a clone");
                int item;
                Assert.True(broadcast.TryReceive(out item) && item == -42, "Expected TryReceive'd data to be a clone");
                IList<int> items;
                Assert.True(((IReceivableSourceBlock<int>)broadcast).TryReceiveAll(out items) && items.Count == 1 && items[0] == -42, "Expected TryReceiveAll'd data to be a clone");
                var ab = new ActionBlock<int>(i =>
                {
                    Assert.True(i == -42, "Expected propagated data to be a clone.");
                });
                broadcast.LinkTo(ab);
                ab.Complete();
                Assert.True(ab.Completion.Wait(4000), "Expected action block to complete after cloned data flowed to it");
            }

            // Test successful processing when no clone function exists
            {
                var data = new object();
                var broadcast = new WriteOnceBlock<object>(null);
                Assert.True(broadcast.Post(data), "Expected initial post on non-cloning Broadcast to succeed");
                Assert.True(broadcast.Receive() == data, "Expected Receive'd data to be original data");
                object item;
                Assert.True(broadcast.TryReceive(out item) && item == data, "Expected TryReceive'd data to be original data");
                IList<object> items;
                Assert.True(((IReceivableSourceBlock<object>)broadcast).TryReceiveAll(out items) && items.Count == 1 && items[0] == data, "Expected TryReceiveAll'd data to be original data");
                var ab = new ActionBlock<object>(i =>
                {
                    Assert.True(i == data, "Expected propagated data to be original data.");
                });
                broadcast.LinkTo(ab);
                ab.Complete();
                Assert.True(ab.Completion.Wait(4000), "Expected action block to complete after original data flowed to it");
            }
        }
    }
}
