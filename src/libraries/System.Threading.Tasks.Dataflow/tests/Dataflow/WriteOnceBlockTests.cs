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
        public void RunWriteOnceBlockTests()
        {
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new WriteOnceBlock<int>(null, new DataflowBlockOptions() { NameFormat = nameFormat }) : new WriteOnceBlock<int>(null)));
            Assert.True(ISourceBlockTestHelper.TestLinkTo<int>(ConstructWriteOnceNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestReserveMessageAndReleaseReservation<int>(ConstructWriteOnceNewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestConsumeMessage<int>(ConstructWriteOnceNewWithNMessages(1)));
            Assert.True(ITargetBlockTestHelper.TestOfferMessage<int>(new WriteOnceBlock<int>(i => i)));
            Assert.True(ITargetBlockTestHelper.TestPost<int>(new WriteOnceBlock<int>(i => i)));
            Assert.True(ITargetBlockTestHelper.TestCompletionTask<int>(new WriteOnceBlock<int>(i => i)));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveWithFilter<int>(ConstructWriteOnceNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveAll<int>(ConstructWriteOnceNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTargetOrder<int>(new WriteOnceBlock<int>(i => i), 1));
            Assert.True(ITargetBlockTestHelper.TestComplete<int>(new WriteOnceBlock<int>(i => i)));

            Assert.True(ISourceBlockTestHelper.TestTargetOrder<int>(new WriteOnceBlock<int>(i => i), 1));
            Assert.True(ITargetBlockTestHelper.TestComplete<int>(new WriteOnceBlock<int>(i => i)));
        }

        private static WriteOnceBlock<int> ConstructWriteOnceNewWithNMessages(int messagesCount)
        {
            var block = new WriteOnceBlock<int>(i => i);
            for (int i = 0; i < messagesCount; i++)
            {
                block.Post(i);
            }
            return block;
        }

        [Fact]
        public void TestWriteOnceBlockConstructor()
        {
            try
            {
                // without option
                var block = new WriteOnceBlock<int>(i => i);
                block = new WriteOnceBlock<int>(null);
                // There is no property to verify. If construction itself fails, there will be an exception.

                //with not cancelled token and default scheduler
                block = new WriteOnceBlock<int>(i => i, new DataflowBlockOptions { MaxMessagesPerTask = 1 });
                block = new WriteOnceBlock<int>(null, new DataflowBlockOptions { MaxMessagesPerTask = 1 });
                // There is no property to verify. If construction itself fails, there will be an exception.

                //with a cancelled token and default scheduler
                var token = new CancellationToken(true);
                block = new WriteOnceBlock<int>(i => i, new DataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
                block = new WriteOnceBlock<int>(null, new DataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
                // There is no property to verify. If construction itself fails, there will be an exception.
            }
            catch (Exception ex)
            {
                Assert.True(false, "Test failed with exception: " + ex.Message);
            }
        }

        [Fact]
        public void TestWriteOnceInvalidArgumentValidation()
        {
            bool passed = true;
            Assert.Throws<ArgumentNullException>(() => new WriteOnceBlock<int>(i => i, null));
            passed &= ITargetBlockTestHelper.TestArgumentsExceptions<int>(new WriteOnceBlock<int>(i => i));
            passed &= ISourceBlockTestHelper.TestArgumentsExceptions<int>(new WriteOnceBlock<int>(i => i));

            Assert.True(passed, "Argument Validation failed.");
        }

        //[Fact(Skip = "Outerloop")]
        public void RunWriteOnceBlockConformanceTests()
        {
            bool passed = true, localPassed = true;
            {
                // Test posting then receiving
                localPassed = true;
                var wob = new WriteOnceBlock<int>(i => i);
                int successfulPosts = 0;
                for (int i = 10; i <= 20; i++)
                {
                    successfulPosts += wob.Post(i) ? 1 : 0;
                }
                localPassed |= successfulPosts == 1;
                localPassed |= wob.Receive() == 10;
                Console.WriteLine("{0}: Posting then receiving", localPassed ? "Success" : "Failure");
                passed &= localPassed;
            }

            {
                // Test receiving then posting
                localPassed = true;
                var wob = new WriteOnceBlock<int>(i => i);
                Task.Factory.StartNew(() =>
                {
                    Task.Delay(1000).Wait();
                    wob.Post(42);
                });
                localPassed |= wob.Receive() == 42;
                localPassed |= wob.Post(43) == false;
                wob.Completion.Wait();
                Console.WriteLine("{0}: Receiving then posting", localPassed ? "Success" : "Failure");
                passed &= localPassed;
            }

            {
                // Test broadcasting
                localPassed = true;
                var wob = new WriteOnceBlock<int>(i => i + 1);
                var tb1 = new TransformBlock<int, int>(i => i);
                var tb2 = new TransformBlock<int, int>(i => i);
                var tb3 = new TransformBlock<int, int>(i => i);
                wob.LinkTo(tb1);
                wob.LinkTo(tb2);
                wob.LinkTo(tb3);
                wob.Post(42);
                localPassed |= tb1.Receive() == 43;
                localPassed |= tb2.Receive() == 43;
                localPassed |= tb3.Receive() == 43;
                Console.WriteLine("{0}: Broadcasting", localPassed ? "Success" : "Failure");
                passed &= localPassed;
            }

            {
                // Test using a precanceled token
                localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    var dbo = new DataflowBlockOptions { CancellationToken = cts.Token };
                    var wob = new WriteOnceBlock<int>(i => i, dbo);

                    int ignoredValue;
                    IList<int> ignoredValues;
                    localPassed &= wob.LinkTo(new ActionBlock<int>(delegate { })) != null;
                    localPassed &= wob.SendAsync(42).Result == false;
                    localPassed &= ((IReceivableSourceBlock<int>)wob).TryReceiveAll(out ignoredValues) == false;
                    localPassed &= wob.Post(42) == false;
                    localPassed &= wob.TryReceive(out ignoredValue) == false;
                    localPassed &= wob.Completion != null;
                    wob.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }
                Console.WriteLine("{0}: Precanceled tokens work correctly", localPassed ? "Success" : "Failure");
                passed &= localPassed;
            }

            {
                // Test using token canceled after construction
                localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    var dbo = new DataflowBlockOptions { CancellationToken = cts.Token };
                    var wob = new WriteOnceBlock<int>(i => i, dbo);
                    cts.Cancel();

                    int ignoredValue;
                    IList<int> ignoredValues;
                    localPassed &= wob.LinkTo(new ActionBlock<int>(delegate { })) != null;
                    localPassed &= wob.SendAsync(42).Result == false;
                    localPassed &= ((IReceivableSourceBlock<int>)wob).TryReceiveAll(out ignoredValues) == false;
                    localPassed &= wob.Post(42) == false;
                    localPassed &= wob.TryReceive(out ignoredValue) == false;
                    localPassed &= wob.Completion != null;
                    wob.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }
                Console.WriteLine("{0}: Precanceled tokens work correctly", localPassed ? "Success" : "Failure");
                passed &= localPassed;
            }

            Assert.True(passed, "Test failed.");
        }

        [Fact]
        public void TestStatusAfterComplete()
        {
            bool passed = true;

            var writeOnce = new WriteOnceBlock<int>(x => x);
            writeOnce.Complete();
            try
            {
                writeOnce.Completion.Wait();
                Console.WriteLine("Completed without exception - Passed");
                passed = writeOnce.Completion.Status == TaskStatus.RanToCompletion;
                Console.WriteLine("Status ({0}) - {1}", writeOnce.Completion.Status, passed ? "Passed" : "FAILED");
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => e is TaskCanceledException);
                passed = false;
                Console.WriteLine("Completed without exception - FAILED");
            }

            Assert.True(passed, "Test failed.");
        }

        [Fact]
        public void TestCanceledLinking()
        {
            bool passed = true;

            var cts = new CancellationTokenSource();
            cts.Cancel();
            var options = new DataflowBlockOptions { CancellationToken = cts.Token };
            var writeOnce = new WriteOnceBlock<int>(x => x, options);
            var target = new ActionBlock<int>(x => { });
            try
            {
                writeOnce.LinkTo(target);
                Console.WriteLine("Completed without exception - Passed");
            }
            catch (Exception)
            {
                passed = false;
                Console.WriteLine("Completed without exception - FAILED");
            }

            Assert.True(passed, "Test failed.");
        }

        //[Fact(Skip = "Outerloop")]
        public void TestWriteOnceCloning()
        {
            // Test cloning when a clone function is provided
            {
                var writeOnce = new WriteOnceBlock<int>(x => -x);
                Assert.True(writeOnce.Post(42), "Expected initial post on cloning WriteOnce to succeed");
                Assert.False(writeOnce.Post(43), "Expected secondary post on cloning WriteOnce to fail");
                Assert.True(writeOnce.Receive() == -42, "Expected Receive'd data to be a clone");
                int item;
                Assert.True(writeOnce.TryReceive(out item) && item == -42, "Expected TryReceive'd data to be a clone");
                IList<int> items;
                Assert.True(((IReceivableSourceBlock<int>)writeOnce).TryReceiveAll(out items) && items.Count == 1 && items[0] == -42, "Expected TryReceiveAll'd data to be a clone");
                var ab = new ActionBlock<int>(i =>
                {
                    Assert.True(i == -42, "Expected propagated data to be a clone.");
                });
                writeOnce.LinkTo(ab);
                ab.Complete();
                Assert.True(ab.Completion.Wait(4000), "Expected action block to complete after cloned data flowed to it");
            }

            // Test successful processing when no clone function exists
            {
                var data = new object();
                var writeOnce = new WriteOnceBlock<object>(null);
                Assert.True(writeOnce.Post(data), "Expected initial post on non-cloning WriteOnce to succeed");
                Assert.False(writeOnce.Post(new object()), "Expected secondary post on non-cloning WriteOnce to fail");
                Assert.True(writeOnce.Receive() == data, "Expected Receive'd data to be original data");
                object item;
                Assert.True(writeOnce.TryReceive(out item) && item == data, "Expected TryReceive'd data to be original data");
                IList<object> items;
                Assert.True(((IReceivableSourceBlock<object>)writeOnce).TryReceiveAll(out items) && items.Count == 1 && items[0] == data, "Expected TryReceiveAll'd data to be original data");
                var ab = new ActionBlock<object>(i =>
                {
                    Assert.True(i == data, "Expected propagated data to be original data.");
                });
                writeOnce.LinkTo(ab);
                ab.Complete();
                Assert.True(ab.Completion.Wait(4000), "Expected action block to complete after original data flowed to it");
            }
        }
    }
}
