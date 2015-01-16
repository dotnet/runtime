// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    /// <summary>The class that contains the unit tests of the BlockingCollection.</summary>
    public class BlockingCollectionTests
    {
        [Fact]
        public static void TestBlockingCollectionBasic()
        {
            BlockingCollection<int> bc = new BlockingCollection<int>(3);
            Task[] tks = new Task[2];
            // A simple blocking consumer with no cancellation.
            int expect = 1;
            tks[0] = Task.Factory.StartNew(() =>
            {
                while (!bc.IsCompleted)
                {
                    try
                    {
                        int data = bc.Take();
                        Assert.Equal(expect, data);
                        expect++;
                    }
                    catch (InvalidOperationException) { } // throw when CompleteAdding called
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            // A simple blocking producer with no cancellation.
            tks[1] = Task.Factory.StartNew(() =>
            {
                bc.Add(1);
                bc.Add(2);
                bc.Add(3);
                // Let consumer know we are done.
                bc.CompleteAdding();
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            Task.WaitAll(tks);
        }

        /// <summary>
        /// Bug description: BlockingCollection throws  InvalidOperationException when calling CompleteAdding even after adding and taking all elements
        /// </summary>
        /// <returns></returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest_BugFix544259()
        {
            int count = 8;
            CountdownEvent cde = new CountdownEvent(count);
            BlockingCollection<object> bc = new BlockingCollection<object>();

            //creates 8 consumers, each calling take to block itself
            for (int i = 0; i < count; i++)
            {
                int myi = i;
                Task.Run(() =>
                {
                    bc.Take();
                    cde.Signal();
                });
            }
            //create 8 producers, each calling add to unblock a consumer
            for (int i = 0; i < count; i++)
            {
                int myi = i;
                Task.Run(() =>
                {
                    bc.Add(new object());
                });
            }

            //CountdownEvent waits till all consumers are unblocked
            cde.Wait();
            bc.CompleteAdding();
        }

        // as part of the bugfix for 626345, this code was suffering occassional ObjectDisposedExceptions due
        // to the expected race between cts.Dispose and the cts.Cancel coming from the linking sources.
        // ML: update - since the change to wait as part of CTS.Dispose, the ODE no longer occurs
        // but we keep the test as a good example of how cleanup of linkedCTS must be carefully handled to prevent 
        // users of the source CTS mistakenly calling methods on disposed targets.
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest_Bug626345()
        {
            const int noOfProducers = 1;
            const int noOfConsumers = 50;
            const int noOfItemsToProduce = 2;
            //Console.WriteLine("Producer: {0}, Consumer: {1}, Items: {2}", noOfProducers, noOfConsumers, noOfItemsToProduce);

            BlockingCollection<long> m_BlockingQueueUnderTest = new BlockingCollection<long>(new ConcurrentQueue<long>());

            Task[] producers = new Task[noOfProducers];
            for (int prodIndex = 0; prodIndex < noOfProducers; prodIndex++)
            {
                producers[prodIndex] = Task.Run(() =>
                {
                    for (int dummyItem = 0; dummyItem < noOfItemsToProduce; dummyItem++)
                    {
                        int i = 0;
                        for (int j = 0; j < 5; j++)
                        {
                            i += j;
                        }
                        m_BlockingQueueUnderTest.Add(dummyItem);

                    }
                });
            }

            //consumers
            Task[] consumers = new Task[noOfConsumers];
            for (int consumerIndex = 0; consumerIndex < noOfConsumers; consumerIndex++)
            {
                consumers[consumerIndex] = Task.Run(() =>
                {
                    while (!m_BlockingQueueUnderTest.IsCompleted)
                    {
                        long item;
                        if (m_BlockingQueueUnderTest.TryTake(out item, 1))
                        {
                            int i = 0;
                            for (int j = 0; j < 5; j++)
                            {
                                i += j;
                            }
                        }
                    }
                });
            }

            //Wait for the producers to finish.
            //It is possible for some of the tasks in the array to be null, because the
            //test was cancelled before all the tasks were creates, so we filter out the null values
            foreach (Task t in producers)
            {
                if (t != null)
                {
                    t.Wait();
                    //t.Join();
                }
            }

            m_BlockingQueueUnderTest.CompleteAdding(); //signal all producers are done adding items

            //Wait for the consumers to finish.
            foreach (Task t in consumers)
            {
                if (t != null)
                {
                    t.Wait();
                    //t.Join();
                }
            }

            // success is not suffering exceptions.
        }

        /// <summary>
        /// Verify the bug fix by making sure TryTakeFromAny succeeds and returns the correct index, the bug reason is
        /// TryTakeFromAny construct a copy list of BCs and  another list for the wait handles, and calls WaitAny using these handles
        /// If it fails (one or more of the collections completed) it removes the completed BC wait handles from the handles list and retry again
        /// We don’t remove the correspondent BC from the list!
        /// 
        /// Now er update the BC list copy as well
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest_Bug914998()
        {
            var producer1 = new BlockingCollection<int>();
            var producer2 = new BlockingCollection<int>();
            var producerArray = new BlockingCollection<int>[] { producer2, producer1 };

            producer2.CompleteAdding();

            Task.Factory.StartNew(() =>
            {
                producer1.Add(100);
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            int ignored, index, timeout = 5000;
            index = BlockingCollection<int>.TryTakeFromAny(producerArray, out ignored, timeout);
            if (index != 1)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest_Bug914998:  >Failed, TryTakeFromAny failed and returned {0} expected 1", index));
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTestConstruction()
        {
            RunBlockingCollectionTest0_Construction(-1);
            RunBlockingCollectionTest0_Construction(10);
        }

        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTestAddTake()
        {
            RunBlockingCollectionTest1_AddTake(1, 1, -1);
            RunBlockingCollectionTest1_AddTake(10, 9, -1);
            RunBlockingCollectionTest1_AddTake(10, 10, 10);
            RunBlockingCollectionTest1_AddTake(10, 10, 9);
        }

        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTestConcurrentAdd()
        {
            RunBlockingCollectionTest2_ConcurrentAdd(2, 10240);
            RunBlockingCollectionTest2_ConcurrentAdd(16, 1024);
        }

        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTestConcurrentAddTake()
        {
            RunBlockingCollectionTest3_ConcurrentAddTake(16, 1024);
        }

        /// <summary>
        /// Tests the default BlockingCollection constructor which initializes a BlockingQueue
        /// </summary>
        /// <param name="boundedCapacity"></param>
        /// <returns></returns>
        private static void RunBlockingCollectionTest0_Construction(int boundedCapacity)
        {
            BlockingCollection<int> blockingQueue;
            if (boundedCapacity != -1)
            {
                blockingQueue = new BlockingCollection<int>(boundedCapacity);
            }
            else
            {
                blockingQueue = new BlockingCollection<int>();
            }

            if (blockingQueue.BoundedCapacity != boundedCapacity)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest0_Constructor(boundedCapacity={0}:  > test failed - Bounded cpacitities do not match", boundedCapacity));
            }

            // Test for queue properties, Taked item should be i nthe same order of the insertion
            int count = boundedCapacity != -1 ? boundedCapacity : 10;

            for (int i = 0; i < count; i++)
            {
                blockingQueue.Add(i);
            }
            for (int i = 0; i < count; i++)
            {
                if (blockingQueue.Take() != i)
                {
                    Assert.False(true, String.Format("RunBlockingCollectionTest0_Constructor: > test failed - the default underlying collection is not a queue"));
                }
            }
        }

        /// <summary>Adds "numOfAdds" elements to the BlockingCollection and then Takes "numOfTakes" elements and 
        /// checks that the count is as expected, the elements removed matched those added and verifies the return 
        /// values of TryAdd() and TryTake().</summary>
        /// <param name="numOfAdds">The number of elements to add to the BlockingCollection.</param>
        /// <param name="numOfTakes">The number of elements to Take from the BlockingCollection.</param>
        /// <param name="boundedCapacity">The bounded capacity of the BlockingCollection, -1 is unbounded.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static void RunBlockingCollectionTest1_AddTake(int numOfAdds, int numOfTakes, int boundedCapacity)
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(boundedCapacity);
            AddAnyTakeAny(numOfAdds, numOfTakes, boundedCapacity, blockingCollection, null, -1);
        }

        /// <summary> Launch some threads performing Add operation and makes sure that all items added are 
        /// present in the collection.</summary>
        /// <param name="numOfThreads">Number of producer threads.</param>
        /// <param name="numOfElementsPerThread">Number of elements added per thread.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static void RunBlockingCollectionTest2_ConcurrentAdd(int numOfThreads, int numOfElementsPerThread)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            Task[] threads = new Task[numOfThreads];

            for (int i = 0; i < threads.Length; ++i)
            {
                threads[i] = Task.Factory.StartNew(delegate(object index)
                {
                    int startOfSequence = ((int)index) * numOfElementsPerThread;
                    int endOfSequence = startOfSequence + numOfElementsPerThread;

                    mre.WaitOne();

                    for (int j = startOfSequence; j < endOfSequence; ++j)
                    {
                        if (!blockingCollection.TryAdd(j))
                            Assert.False(true,
                                String.Format("RunBlockingCollectionTest2_ConcurrentAdd(numOfThreads={0}, numOfElementsPerThread={1}): > test failed - TryAdd returned false unexpectedly", numOfThreads, numOfElementsPerThread));
                    }
                }, i, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            mre.Set();
            Task.WaitAll(threads);

            int expectedCount = numOfThreads * numOfElementsPerThread;
            if (blockingCollection.Count != expectedCount)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest2_ConcurrentAdd: > test failed - expected count = {0}, actual = {1}", expectedCount, blockingCollection.Count));
            }
            List<int> sortedElementsInCollection = new List<int>(blockingCollection);
            sortedElementsInCollection.Sort();
            VerifyElementsAreMembersOfSequence(sortedElementsInCollection, 0, expectedCount - 1);
        }

        /// <summary>Launch threads/2 producers and threads/2 consumers then make sure that all elements produced
        /// are consumed by consumers with no element lost nor consumed more than once.</summary>
        /// <param name="threads">Total number of producer and consumer threads.</param>
        /// <param name="numOfElementsPerThread">Number of elements to Add/Take per thread.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static void RunBlockingCollectionTest3_ConcurrentAddTake(int numOfThreads, int numOfElementsPerThread)
        {
            //If numOfThreads is not an even number, make it even.
            if ((numOfThreads % 2) != 0)
                numOfThreads++;

            ManualResetEvent mre = new ManualResetEvent(false);
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            Task[] threads = new Task[numOfThreads];
            List<int> removedElementsFromAllThreads = new List<int>();

            for (int i = 0; i < threads.Length; ++i)
            {
                if (i < (threads.Length / 2))
                {
                    threads[i] = Task.Factory.StartNew(delegate(object index)
                    {
                        int startOfSequence = ((int)index) * numOfElementsPerThread;
                        int endOfSequence = startOfSequence + numOfElementsPerThread;

                        mre.WaitOne();
                        for (int j = startOfSequence; j < endOfSequence; ++j)
                        {
                            if (!blockingCollection.TryAdd(j))
                                Assert.False(true, String.Format("RunBlockingCollectionTest3_ConcurrentAddTake(numOfThreads={0}, numOfElementsPerThread={1}): > test failed - TryAdd returned false unexpectedly", numOfThreads, numOfElementsPerThread));
                        }
                    }, i, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                else
                {
                    threads[i] = Task.Run(delegate()
                    {
                        List<int> removedElements = new List<int>();
                        mre.WaitOne();
                        for (int j = 0; j < numOfElementsPerThread; ++j)
                        {
                            removedElements.Add(blockingCollection.Take());
                        }

                        //The elements are added later in this loop to removedElementsFromAllThreads List<int> and not in 
                        //the loop above so that the synchronization mechanisms of removedElementsFromAllThreads do not 
                        //interfere in coordinating the threads and only blockingCollection is coordinating the threads.
                        for (int j = 0; j < numOfElementsPerThread; ++j)
                        {
                            lock (removedElementsFromAllThreads)
                            {
                                removedElementsFromAllThreads.Add(removedElements[j]);
                            }
                        }
                    });
                }
            }

            mre.Set();
            Task.WaitAll(threads);

            int expectedCount = 0;
            if (blockingCollection.Count != expectedCount)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest3_ConcurrentAddTake: > test failed - expected count = {0}, actual = {1}", expectedCount, blockingCollection.Count));
            }
            int[] arrayOfRemovedElementsFromAllThreads = (int[])(removedElementsFromAllThreads.ToArray());
            List<int> sortedElementsInCollection = new List<int>(arrayOfRemovedElementsFromAllThreads);
            sortedElementsInCollection.Sort();
            VerifyElementsAreMembersOfSequence(sortedElementsInCollection, 0, (numOfThreads / 2 * numOfElementsPerThread) - 1);
        }

        /// <summary>Validates the Dispose() method.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest4_Dispose()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            blockingCollection.Dispose();
            bool testSuceeded = false;
            int numOfExceptionsThrown = 0;
            int numOfTests = 0;

            try
            {
                numOfTests++;
                blockingCollection.Add(default(int));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryAdd(default(int));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryAdd(default(int), 1);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryAdd(default(int), new TimeSpan(1));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            int item;
            try
            {
                numOfTests++;
                blockingCollection.Take();
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryTake(out item);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryTake(out item, 1);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryTake(out item, new TimeSpan(1));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            const int NUM_OF_COLLECTIONS = 10;
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[NUM_OF_COLLECTIONS];
            for (int i = 0; i < NUM_OF_COLLECTIONS - 1; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>(-1);
            }

            blockingCollections[NUM_OF_COLLECTIONS - 1] = blockingCollection;
            try
            {
                numOfTests++;
                BlockingCollection<int>.AddToAny(blockingCollections, default(int));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryAddToAny(blockingCollections, default(int));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryAddToAny(blockingCollections, default(int), 1);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryAddToAny(blockingCollections, default(int), new TimeSpan(1));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TakeFromAny(blockingCollections, out item);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, 1);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, new TimeSpan(1));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.CompleteAdding();
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.ToArray();
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.CopyTo(new int[1], 0);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            int? boundedCapacity = 0;
            try
            {
                numOfTests++;
                boundedCapacity = blockingCollection.BoundedCapacity;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            bool isCompleted = false;
            try
            {
                numOfTests++;
                isCompleted = blockingCollection.IsCompleted;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            bool addingIsCompleted = false;
            try
            {
                numOfTests++;
                addingIsCompleted = blockingCollection.IsAddingCompleted;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            int count = 0;
            try
            {
                numOfTests++;
                count = blockingCollection.Count;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            object syncRoot = null;
            try
            {
                numOfTests++;
                syncRoot = ((ICollection)blockingCollection).SyncRoot;
            }
            catch (NotSupportedException)
            {
                numOfExceptionsThrown++;
            }

            bool isSynchronized = false;
            try
            {
                numOfTests++;
                isSynchronized = ((ICollection)blockingCollection).IsSynchronized;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                blockingCollection.Dispose();
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                foreach (int element in blockingCollection)
                {
                    int temp = element;
                }
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                foreach (int element in blockingCollection.GetConsumingEnumerable())
                {
                    int temp = element;
                }
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }


            testSuceeded = (numOfExceptionsThrown == numOfTests);

            if (!testSuceeded)
            {
                Assert.False(true, "RunBlockingCollectionTest4_Dispose: > test failed - Not all methods threw ObjectDisposedExpection");
            }
        }

        /// <summary>Validates GetEnumerator and makes sure that BlockingCollection.GetEnumerator() produces the 
        /// same results as IConcurrentCollection.GetEnumerator().</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest5_GetEnumerator()
        {
            ConcurrentStackCollection<int> concurrentCollection = new ConcurrentStackCollection<int>();
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            const int MAX_NUM_TO_ADD = 100;
            for (int i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                concurrentCollection.TryAdd(i);
            }

            List<int> resultOfEnumOfBlockingCollection = new List<int>();
            foreach (int i in blockingCollection)
            {
                resultOfEnumOfBlockingCollection.Add(i);
            }

            List<int> resultOfEnumOfConcurrentCollection = new List<int>();
            foreach (int i in concurrentCollection)
            {
                resultOfEnumOfConcurrentCollection.Add(i);
            }

            if (resultOfEnumOfBlockingCollection.Count != resultOfEnumOfConcurrentCollection.Count)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest5_GetEnumerator: > test failed - number of elements returned from enumerators mismatch: ConcurrentCollection={0}, BlockingCollection={1}",
                                    resultOfEnumOfConcurrentCollection.Count,
                                    resultOfEnumOfBlockingCollection.Count));
            }

            for (int i = 0; i < resultOfEnumOfBlockingCollection.Count; ++i)
            {
                if ((int)resultOfEnumOfBlockingCollection[i] != (int)resultOfEnumOfConcurrentCollection[i])
                {
                    Assert.False(true, String.Format("RunBlockingCollectionTest5_GetEnumerator: > test failed - elements returned from enumerators mismatch: ConcurrentCollection={0}, BlockingCollection={1}",
                                    (int)resultOfEnumOfConcurrentCollection[i],
                                    (int)resultOfEnumOfBlockingCollection[i]));
                }
            }
        }

        /// <summary>Validates GetConsumingEnumerator and makes sure that BlockingCollection.GetConsumingEnumerator() 
        /// produces the same results as if call Take in a loop.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest6_GetConsumingEnumerable()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            BlockingCollection<int> blockingCollectionMirror = ConstructBlockingCollection<int>();

            const int MAX_NUM_TO_ADD = 100;
            for (int i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                blockingCollectionMirror.Add(i);
            }

            if (blockingCollection.Count != MAX_NUM_TO_ADD)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest6_GetConsumingEnumerable: > test failed - unexpcted count: actual={0}, expected={1}", blockingCollection.Count, MAX_NUM_TO_ADD));
            }

            List<int> resultOfEnumOfBlockingCollection = new List<int>();

            //CompleteAdding() is called so that the MoveNext() on the Enumerable resulting from 
            //GetConsumingEnumerable return false after the collection is empty.
            blockingCollection.CompleteAdding();
            foreach (int i in blockingCollection.GetConsumingEnumerable())
            {
                resultOfEnumOfBlockingCollection.Add(i);
            }

            if (blockingCollection.Count != 0)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest6_GetConsumingEnumerable: > test failed - unexpcted count: actual={0}, expected=0", blockingCollection.Count));
            }

            List<int> resultOfEnumOfBlockingCollectionMirror = new List<int>();
            while (blockingCollectionMirror.Count != 0)
            {
                resultOfEnumOfBlockingCollectionMirror.Add(blockingCollectionMirror.Take());
            }

            if (resultOfEnumOfBlockingCollection.Count != resultOfEnumOfBlockingCollectionMirror.Count)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest6_GetConsumingEnumerable: > test failed - number of elements mismatch: BlockingCollectionMirror={0}, BlockingCollection={1}",
                                    resultOfEnumOfBlockingCollectionMirror.Count,
                                    resultOfEnumOfBlockingCollection.Count));
            }

            for (int i = 0; i < resultOfEnumOfBlockingCollection.Count; ++i)
            {
                if ((int)resultOfEnumOfBlockingCollection[i] != (int)resultOfEnumOfBlockingCollectionMirror[i])
                {
                    Assert.False(true, String.Format("RunBlockingCollectionTest6_GetConsumingEnumerable: > test failed - elements mismatch: BlockingCollectionMirror={0}, BlockingCollection={1}",
                                    (int)resultOfEnumOfBlockingCollectionMirror[i],
                                    (int)resultOfEnumOfBlockingCollection[i]));

                }
            }
        }

        /// <summary>Validates that after CompleteAdding() is called, future calls to Add will throw exceptions, calls
        /// to Take will not block waiting for more input, and calls to MoveNext on the enumerator returned from GetEnumerator 
        /// on the enumerable returned from GetConsumingEnumerable will return false when the collection’s count reaches 0.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest7_CompleteAdding()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            blockingCollection.Add(0);

            blockingCollection.CompleteAdding();

            try
            {
                blockingCollection.Add(1);
                Assert.False(true, "RunBlockingCollectionTest7_CompleteAdding: > test failed - Add should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
            }

            if (blockingCollection.Count != 1)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest7_CompleteAdding: > test failed - Unexpected count: Actual={0}, Expected=1", blockingCollection.Count));
            }

            blockingCollection.Take();

            try
            {
                blockingCollection.Take();
                Assert.False(true, "RunBlockingCollectionTest7_CompleteAdding: > test failed - Take should have thrown OperationCanceledException");
            }
            catch (InvalidOperationException)
            {
            }

            int item = 0;

            if (blockingCollection.TryTake(out item))
            {
                Assert.False(true, "RunBlockingCollectionTest7_CompleteAdding: > test failed - TryTake should have return false");
            }

            int counter = 0;
            foreach (int i in blockingCollection.GetConsumingEnumerable())
            {
                counter++;
            }

            if (counter > 0)
            {
                Assert.False(true, "RunBlockingCollectionTest7_CompleteAdding: > test failed - the enumerable returned from GetConsumingEnumerable() should not have enumerated through the collection");
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest7_ConcurrentAdd_CompleteAdding()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            Task[] threads = new Task[4];
            int succeededAdd = 0;
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        try
                        {
                            blockingCollection.Add(j);
                            Interlocked.Increment(ref succeededAdd);
                        }
                        catch (InvalidOperationException)
                        {
                            break;
                        }

                    }
                });
            }

            blockingCollection.CompleteAdding();
            int count1 = blockingCollection.Count;
            Task.WaitAll(Task.Delay(100));
            int count2 = blockingCollection.Count;

            if (count1 != count2)
            {
                Assert.False(true, "RunBlockingCollectionTest7_CompleteAdding: > test failed - The count has been changed after returning from CompleteAdding");
            }

            if (count1 != succeededAdd)
            {
                Assert.False(true, "RunBlockingCollectionTest7_CompleteAdding: > test failed - The collection count doesn't match the read count succeededCount = " + succeededAdd + " read count = " + count1);
            }
        }

        /// <summary>Validates that BlockingCollection.ToArray() produces same results as 
        /// IConcurrentCollection.ToArray().</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest8_ToArray()
        {
            ConcurrentStackCollection<int> concurrentCollection = new ConcurrentStackCollection<int>();
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            const int MAX_NUM_TO_ADD = 100;
            for (int i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                concurrentCollection.TryAdd(i);
            }

            int[] arrBlockingCollection = blockingCollection.ToArray();
            int[] arrConcurrentCollection = concurrentCollection.ToArray();

            if (arrBlockingCollection.Length != arrConcurrentCollection.Length)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest8_ToArray: > test failed - Arrays length mismatch: arrBlockingCollection={0}, arrConcurrentCollection={1}",
                                    arrBlockingCollection.Length,
                                    arrConcurrentCollection.Length));
            }

            for (int i = 0; i < arrBlockingCollection.Length; ++i)
            {
                if (arrBlockingCollection[i] != arrConcurrentCollection[i])
                {
                    Assert.False(true, String.Format("RunBlockingCollectionTest8_ToArray: > test failed - Array elements mismatch: arrBlockingCollection[{2}]={0}, arrConcurrentCollection[{2}]={1}",
                                        arrBlockingCollection[i],
                                        arrConcurrentCollection[i],
                                        i));
                }
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTestCopyTo()
        {
            RunBlockingCollectionTest9_CopyTo(0);
            RunBlockingCollectionTest9_CopyTo(8);
        }

        /// <summary>Validates that BlockingCollection.CopyTo() produces same results as IConcurrentCollection.CopyTo().</summary>        
        /// <param name="indexOfInsertion">The zero-based index in the array at which copying begins.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>    
        private static void RunBlockingCollectionTest9_CopyTo(int indexOfInsertion)
        {
            ConcurrentStackCollection<int> concurrentCollection = new ConcurrentStackCollection<int>();
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            const int MAX_NUM_TO_ADD = 100;
            for (int i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                concurrentCollection.TryAdd(i);
            }

            //Array is automatically initialized to default(int).
            int[] arrBlockingCollection = new int[MAX_NUM_TO_ADD + indexOfInsertion];
            int[] arrConcurrentCollection = new int[MAX_NUM_TO_ADD + indexOfInsertion];

            blockingCollection.CopyTo(arrBlockingCollection, indexOfInsertion);
            concurrentCollection.CopyTo(arrConcurrentCollection, indexOfInsertion);

            for (int i = 0; i < arrBlockingCollection.Length; ++i)
            {
                if (arrBlockingCollection[i] != arrConcurrentCollection[i])
                {
                    Assert.False(true, String.Format("RunBlockingCollectionTest9_CopyTo(indexOfInsertion={3}): > test failed - Array elements mismatch: arrBlockingCollection[{2}]={0}, arrConcurrentCollection[{2}]={1}",
                                        arrBlockingCollection[i],
                                        arrConcurrentCollection[i],
                                        i,
                                        indexOfInsertion));
                }
            }
        }

        /// <summary>Validates BlockingCollection.Count.</summary>        
        /// <returns>True if test succeeded, false otherwise.</returns>    
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest10_Count()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(1);

            if (blockingCollection.Count != 0)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest10_Count: > test failed - Unexpected count: Actual={0}, Expected=0", blockingCollection.Count));
            }

            blockingCollection.Add(1);

            if (blockingCollection.Count != 1)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest10_Count: > test failed - Unexpected count: Actual={0}, Expected=1", blockingCollection.Count));
            }

            blockingCollection.TryAdd(1);

            if (blockingCollection.Count != 1)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest10_Count: > test failed - Unexpected count: Actual={0}, Expected=1", blockingCollection.Count));
            }

            blockingCollection.Take();

            if (blockingCollection.Count != 0)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest10_Count: > test failed - Unexpected count: Actual={0}, Expected=0", blockingCollection.Count));
            }
        }

        /// <summary>Validates BlockingCollection.BoundedCapacity.</summary>        
        /// <returns>True if test succeeded, false otherwise.</returns>    
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest11_BoundedCapacity()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(1);

            if (blockingCollection.BoundedCapacity != 1)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest11_BoundedCapacity: > test failed - Unexpected boundedCapacity: Actual={0}, Expected=1", blockingCollection.BoundedCapacity));
            }

            blockingCollection = ConstructBlockingCollection<int>();

            if (blockingCollection.BoundedCapacity != -1)
            {
                Assert.False(true, String.Format("RunBlockingCollectionTest11_BoundedCapacity: > test failed - Unexpected boundedCapacity: Actual={0}, Expected=-1", blockingCollection.BoundedCapacity));
            }
        }

        /// <summary>Validates BlockingCollection.IsCompleted and BlockingCollection.AddingIsCompleted.</summary>        
        /// <returns>True if test succeeded, false otherwise.</returns>    
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            if (blockingCollection.IsAddingCompleted)
            {
                Assert.False(true, "RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted: > test failed (Empty Collection) - AddingIsCompleted should be false");
            }

            if (blockingCollection.IsCompleted)
            {
                Assert.False(true, "RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted: > test failed (Empty Collection) - IsCompleted should be false");
            }

            blockingCollection.CompleteAdding();

            if (!blockingCollection.IsAddingCompleted)
            {
                Assert.False(true, "RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted: > test failed (Empty Collection) - AddingIsCompleted should be true");
            }

            if (!blockingCollection.IsCompleted)
            {
                Assert.False(true, "RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted: > test failed (Empty Collection) - IsCompleted should be true");
            }

            blockingCollection = ConstructBlockingCollection<int>();
            blockingCollection.Add(0);
            blockingCollection.CompleteAdding();

            if (!blockingCollection.IsAddingCompleted)
            {
                Assert.False(true, "RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted: > test failed (NonEmpty Collection) - AddingIsCompleted should be true");
            }

            if (blockingCollection.IsCompleted)
            {
                Assert.False(true, "RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted: > test failed (NonEmpty Collection) - IsCompleted should be false");
            }

            blockingCollection.Take();

            if (!blockingCollection.IsCompleted)
            {
                Assert.False(true, "RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted: > test failed (NonEmpty Collection) - IsCompleted should be true");
            }
        }

        /// <summary>Validates BlockingCollection.IsSynchronized and BlockingCollection.SyncRoot.</summary>        
        /// <returns>True if test succeeded, false otherwise.</returns>    
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest13_IsSynchronized_SyncRoot()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            bool exceptionThrown = false;
            try
            {
                var dummy = ((ICollection)blockingCollection).SyncRoot;

            }
            catch (NotSupportedException)
            {
                exceptionThrown = true;
            }
            if (!exceptionThrown)
            {
                Assert.False(true, "RunBlockingCollectionTest13_IsSynchronized_SyncRoot: > test failed - SyncRoot should throw NotSupportException");
            }

            if (((ICollection)blockingCollection).IsSynchronized)
            {
                Assert.False(true, "RunBlockingCollectionTest13_IsSynchronized_SyncRoot: > test failed - IsSynchronized should be false");
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTestAddAnyTakeAny()
        {
            RunBlockingCollectionTest14_AddAnyTakeAny(1, 1, 16, 0, -1);
            RunBlockingCollectionTest14_AddAnyTakeAny(10, 9, 16, 15, -1);
            RunBlockingCollectionTest14_AddAnyTakeAny(10, 10, 16, 14, 10);
            RunBlockingCollectionTest14_AddAnyTakeAny(10, 10, 16, 1, 9);
        }

        /// <summary>Initializes an array of blocking collections such that all are full except one in case of adds and
        /// all are empty except one (the same blocking collection) in case of Takes.
        /// Adds "numOfAdds" elements to the BlockingCollection and then Takes "numOfTakes" elements and checks
        /// that the count is as expected, the elements Taked matched those added and verifies the return values of 
        /// TryAdd() and TryTake().</summary>
        /// <param name="numOfAdds">Number of elements to Add.</param>
        /// <param name="numOfTakes">Number of elements to Take.</param>
        /// <param name="numOfBlockingCollections">Length of BlockingCollections array.</param>
        /// <param name="indexOfBlockingCollectionUnderTest">Index of the BlockingCollection that will accept the operations.</param>
        /// <param name="boundedCapacity">The bounded capacity of the BlockingCollection under test.</param>
        /// <returns>True if test succeeds, false otherwise.</returns>
        private static void RunBlockingCollectionTest14_AddAnyTakeAny(int numOfAdds,
                                                                        int numOfTakes,
                                                                        int numOfBlockingCollections,
                                                                        int indexOfBlockingCollectionUnderTest,
                                                                        int boundedCapacity)
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(boundedCapacity);
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[numOfBlockingCollections];

            AddAnyTakeAny(numOfAdds, numOfTakes, boundedCapacity, blockingCollection, blockingCollections, indexOfBlockingCollectionUnderTest);
        }

        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTestConcurrentAddAnyTakeAny()
        {
            RunBlockingCollectionTest15_ConcurrentAddAnyTakeAny(4, 4096, 2, 64);
        }

        /// <summary>Launch threads/2 producers and threads/2 consumers then makes sure that all elements produced
        /// are consumed by consumers with no element lost nor consumed more than once.</summary>
        /// <param name="threads">Total number of producer and consumer threads.</param>
        /// <param name="numOfElementsPerThread">Number of elements to Add/Take per thread.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static void RunBlockingCollectionTest15_ConcurrentAddAnyTakeAny(int numOfThreads, int numOfElementsPerThread, int numOfCollections, int boundOfCollections)
        {
            //If numOfThreads is not an even number, make it even.
            if ((numOfThreads % 2) != 0)
            {
                numOfThreads++;
            }

            ManualResetEvent mre = new ManualResetEvent(false);

            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[numOfCollections];
            for (int i = 0; i < numOfCollections; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>(boundOfCollections);
            }

            Task[] threads = new Task[numOfThreads];
            List<int> removedElementsFromAllThreads = new List<int>();

            for (int i = 0; i < threads.Length; ++i)
            {
                if (i < (threads.Length / 2))
                {
                    threads[i] = Task.Factory.StartNew(delegate(object index)
                    {
                        int startOfSequence = ((int)index) * numOfElementsPerThread;
                        int endOfSequence = startOfSequence + numOfElementsPerThread;

                        mre.WaitOne();
                        for (int j = startOfSequence; j < endOfSequence; ++j)
                        {
                            int indexOfCollection = BlockingCollection<int>.AddToAny(blockingCollections, j);
                            if (indexOfCollection < 0)
                            {
                                Console.WriteLine("RunBlockingCollectionTest15_ConcurrentAddAnyTakeAny(numOfThreads={0}, numOfElementsPerThread={1},numOfCollections={2},boundOfCollections={3})",
                                    numOfThreads,
                                    numOfElementsPerThread,
                                    numOfCollections,
                                    boundOfCollections);
                                Assert.False(true, String.Format(" > test failed - AddToAny returned {0} unexpectedly", indexOfCollection));
                            }
                        }
                    }, i, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                else
                {
                    threads[i] = Task.Run(delegate()
                    {
                        List<int> removedElements = new List<int>();
                        mre.WaitOne();
                        for (int j = 0; j < numOfElementsPerThread; ++j)
                        {
                            int item = -1;
                            int indexOfCollection = BlockingCollection<int>.TakeFromAny(blockingCollections, out item);
                            if (indexOfCollection < 0)
                            {
                                Console.WriteLine("RunBlockingCollectionTest15_ConcurrentAddAnyTakeAny(numOfThreads={0}, numOfElementsPerThread={1},numOfCollections={2},boundOfCollections={3})",
                                    numOfThreads,
                                    numOfElementsPerThread,
                                    numOfCollections,
                                    boundOfCollections);
                                Assert.False(true, String.Format(" > test failed - TakeFromAny returned {0} unexpectedly", indexOfCollection));
                            }
                            else
                            {
                                removedElements.Add(item);
                            }
                        }

                        //The elements are added later in this loop to removedElementsFromAllThreads List<int> and not in 
                        //the loop above so that the synchronization mechanisms of removedElementsFromAllThreads do not 
                        //interfere in coordinating the threads and only blockingCollection is coordinating the threads.
                        for (int j = 0; j < numOfElementsPerThread; ++j)
                        {
                            lock (removedElementsFromAllThreads)
                            {
                                removedElementsFromAllThreads.Add(removedElements[j]);
                            }
                        }
                    });
                }
            }

            mre.Set();
            Task.WaitAll(threads);

            int expectedCount = 0;
            int blockingCollectionIndex = 0;
            foreach (BlockingCollection<int> blockingCollection in blockingCollections)
            {
                if (blockingCollection.Count != expectedCount)
                {
                    Console.WriteLine("* RunBlockingCollectionTest15_ConcurrentAddAnyTakeAny(numOfThreads={0}, numOfElementsPerThread={1},numOfCollections={2},boundOfCollections={3})",
                                    numOfThreads,
                                    numOfElementsPerThread,
                                    numOfCollections,
                                    boundOfCollections);
                    Assert.False(true, String.Format(" > test failed - expected count = {0}, actual = {1}, blockingCollectionIndex = {2}", expectedCount, blockingCollection.Count, blockingCollectionIndex));
                }
                blockingCollectionIndex++;
            }
            int[] arrayOfRemovedElementsFromAllThreads = (int[])(removedElementsFromAllThreads.ToArray());
            List<int> sortedElementsInCollection = new List<int>(arrayOfRemovedElementsFromAllThreads);
            sortedElementsInCollection.Sort();
            VerifyElementsAreMembersOfSequence(sortedElementsInCollection, 0, (numOfThreads / 2 * numOfElementsPerThread) - 1);
        }

        /// <summary>Validates the constructor of BlockingCollection.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest16_Ctor()
        {
            BlockingCollection<int> blockingCollection = null;

            try
            {
                blockingCollection = new BlockingCollection<int>(null);
                Assert.False(true, "RunBlockingCollectionTest16_Ctor: > test failed - expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                blockingCollection = new BlockingCollection<int>(null, 1);
                Assert.False(true, "RunBlockingCollectionTest16_Ctor: > test failed - expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                blockingCollection = new BlockingCollection<int>(new ConcurrentStackCollection<int>(), 0);
                Assert.False(true, "RunBlockingCollectionTest16_Ctor: > test failed - expected ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection = new BlockingCollection<int>(new ConcurrentStackCollection<int>(), -1);
                Assert.False(true, "RunBlockingCollectionTest16_Ctor: > test failed - expected ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                ConcurrentStackCollection<int> concurrentStack = new ConcurrentStackCollection<int>();
                concurrentStack.TryAdd(1);
                concurrentStack.TryAdd(2);
                blockingCollection = new BlockingCollection<int>(concurrentStack, 1);
                Assert.False(true, "RunBlockingCollectionTest16_Ctor: > test failed - expected ArgumentException");
            }
            catch (ArgumentException)
            {
            }
        }

        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest17_AddExceptions()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            try
            {
                blockingCollection.TryAdd(0, new TimeSpan(0, 0, 0, 1, 2147483647));
                Assert.False(true, "RunBlockingCollectionTest17_AddExceptions: > test failed - expected exception ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.TryAdd(0, -2);
                Assert.False(true, "RunBlockingCollectionTest17_AddExceptions: > test failed - expected exception ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.CompleteAdding();
                blockingCollection.TryAdd(0);
                Assert.False(true, "RunBlockingCollectionTest17_AddExceptions: > test failed - expected exception InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
            }

            // test if the underlyingcollection.TryAdd returned flse
            BlockingCollection<int> bc = new BlockingCollection<int>(new QueueProxy1<int>());
            try
            {
                bc.Add(1);
                Assert.False(true, "RunBlockingCollectionTest17_AddExceptions: > test failed - expected exception InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest18_TakeExceptions()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            int item;
            try
            {
                blockingCollection.TryTake(out item, new TimeSpan(0, 0, 0, 1, 2147483647));
                Assert.False(true, "RunBlockingCollectionTest18_TakeExceptions: > test failed - expected exception ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.TryTake(out item, -2);
                Assert.False(true, "RunBlockingCollectionTest18_TakeExceptions: > test failed - expected exception ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.CompleteAdding();
                blockingCollection.Take();
                Assert.False(true, "RunBlockingCollectionTest18_TakeExceptions: > test failed - expected exception OperationCanceledException");
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest19_AddAnyExceptions()
        {
            const int NUM_OF_COLLECTIONS = 2;
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[NUM_OF_COLLECTIONS];
            for (int i = 0; i < NUM_OF_COLLECTIONS; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>();
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(blockingCollections, 0, new TimeSpan(0, 0, 0, 1, 2147483647));
                Assert.False(true, "RunBlockingCollectionTest19_AddAnyExceptions: > test failed - expected exception ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(blockingCollections, 0, -2);
                Assert.False(true, "RunBlockingCollectionTest19_AddAnyExceptions: > test failed - expected exception ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(new BlockingCollection<int>[NUM_OF_COLLECTIONS], 0);
                Assert.False(true, "RunBlockingCollectionTest19_AddAnyExceptions: > test failed - expected exception ArgumentException");
            }
            catch (ArgumentException)
            {
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(new BlockingCollection<int>[0], 0);
                Assert.False(true, "RunBlockingCollectionTest19_AddAnyExceptions: > test failed - expected exception ArgumentException");
            }
            catch (ArgumentException)
            {
            }

            try
            {
                blockingCollections[NUM_OF_COLLECTIONS - 1].CompleteAdding();
                BlockingCollection<int>.TryAddToAny(blockingCollections, 0);
                Assert.False(true, "RunBlockingCollectionTest19_AddAnyExceptions: > test failed - expected exception ArgumentException");
            }
            catch (ArgumentException)
            {
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(null, 0);
                Assert.False(true, "RunBlockingCollectionTest19_AddAnyExceptions: > test failed - expected exception ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
            }

            // test if the underlyingcollection.TryAdd returned flse
            BlockingCollection<int> collection = new BlockingCollection<int>(new QueueProxy1<int>());
            try
            {
                BlockingCollection<int>.AddToAny(new BlockingCollection<int>[] { collection }, 1);
                Assert.False(true, "RunBlockingCollectionTest19_AddAnyExceptions: > test failed - expected exception InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest20_TakeAnyExceptions()
        {
            const int NUM_OF_COLLECTIONS = 2;
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[NUM_OF_COLLECTIONS];
            for (int i = 0; i < NUM_OF_COLLECTIONS; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>();
            }

            int item;
            try
            {
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, new TimeSpan(0, 0, 0, 1, 2147483647));
                Assert.False(true, "RunBlockingCollectionTest20_TakeAnyExceptions: > test failed - expected exception ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, -2);
                Assert.False(true, "RunBlockingCollectionTest20_TakeAnyExceptions: > test failed - expected exception ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                BlockingCollection<int>.TryTakeFromAny(new BlockingCollection<int>[NUM_OF_COLLECTIONS], out item);
                Assert.False(true, "RunBlockingCollectionTest20_TakeAnyExceptions: > test failed - expected exception ArgumentException");
            }
            catch (ArgumentException)
            {
            }

            try
            {
                BlockingCollection<int>.TryTakeFromAny(new BlockingCollection<int>[0], out item);
                Assert.False(true, "RunBlockingCollectionTest20_TakeAnyExceptions: > test failed - expected exception ArgumentException");
            }
            catch (ArgumentException)
            {
            }

            try
            {
                BlockingCollection<int>.TryTakeFromAny(null, out item);
                Assert.False(true, "RunBlockingCollectionTest20_TakeAnyExceptions: > test failed - expected exception ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
            }

            // new behaviour for TakeFromAny after Dev10, to throw argumenexception if all the collections are completed, 
            // however TryTakeFromAny will return false
            for (int i = 0; i < NUM_OF_COLLECTIONS; ++i)
            {
                blockingCollections[i].CompleteAdding();
            }
            try
            {
                BlockingCollection<int>.TakeFromAny(blockingCollections, out item);
                Assert.False(true, "RunBlockingCollectionTest20_TakeAnyExceptions: > test failed - expected exception ArgumentException");
            }
            catch (ArgumentException)
            {
            }
        }

        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        [OuterLoop]
        public static void RunBlockingCollectionTest21_CopyToExceptions()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            blockingCollection.Add(0);
            blockingCollection.Add(0);
            int[] arr = new int[2];
            try
            {
                blockingCollection.CopyTo(null, 0);
                Assert.False(true, "RunBlockingCollectionTest22_CopyToExceptions: > test failed - expected exception ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                blockingCollection.CopyTo(arr, -1);
                Assert.False(true, "RunBlockingCollectionTest22_CopyToExceptions: > test failed - expected exception ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.CopyTo(arr, 2);
                Assert.False(true, "RunBlockingCollectionTest22_CopyToExceptions: > test failed - expected exception ArgumentException");
            }
            catch (ArgumentException)
            {
            }

            try
            {
                int[,] twoDArray = new int[2, 2];
                ((ICollection)blockingCollection).CopyTo(twoDArray, 0);
                Assert.False(true, "RunBlockingCollectionTest22_CopyToExceptions: > test failed - expected exception ArgumentException");
            }
            catch (ArgumentException)
            {
            }

            try
            {
                float[,] twoDArray = new float[2, 2];
                ((ICollection)blockingCollection).CopyTo(twoDArray, 0);
                Assert.False(true, "RunBlockingCollectionTest22_CopyToExceptions: > test failed - expected exception ArgumentException");
            }
            catch (ArgumentException)
            {
            }
        }

        #region Helper Methods / Classes

        /// <summary>Initializes an array of blocking collections (if its not null) such that all are full except one in case 
        /// of adds and all are empty except one (the same blocking collection) in case of Takes.
        /// Adds "numOfAdds" elements to the BlockingCollection and then Takes "numOfTakes" elements and checks
        /// that the count is as expected, the elements Taked matched those added and verifies the return values of 
        /// TryAdd() and TryTake().</summary>        
        /// <param name="numOfAdds">Number of elements to Add.</param>
        /// <param name="numOfTakes">Number of elements to Take.</param>
        /// <param name="boundedCapacity">The bounded capacity of the BlockingCollection under test.</param>
        /// <param name="blockingCollection">The blocking collection under test.</param>
        /// <param name="blockingCollections">The array of blocking collections under test. Null if this method should use TryAdd/Take
        /// and not AddToAny/TakeFromAny.</param>
        /// <param name="indexOfBlockingCollectionUnderTest">Index of the BlockingCollection that will accept the operations.</param>
        /// <returns>True if test succeeds, false otherwise.</returns>
        private static void AddAnyTakeAny(int numOfAdds,
                                            int numOfTakes,
                                            int boundedCapacity,
                                            BlockingCollection<int> blockingCollection,
                                            BlockingCollection<int>[] blockingCollections,
                                            int indexOfBlockingCollectionUnderTest
                                            )
        {

            if (blockingCollections != null)
            {
                //Initialize all other blocking collections to be full so that Adds are done on blockingCollection.
                for (int i = 0; i < blockingCollections.Length; ++i)
                {
                    if (i == indexOfBlockingCollectionUnderTest)
                    {
                        blockingCollections[i] = blockingCollection;
                    }
                    else
                    {
                        blockingCollections[i] = ConstructFullBlockingCollection<int>();
                    }
                }
            }

            ConcurrentStackCollection<int> concurrentCollection = new ConcurrentStackCollection<int>();
            int numberToAdd = 0;
            int numOfTrueTryAdds = 0;
            int capacity = (boundedCapacity == -1) ? Int32.MaxValue : boundedCapacity;

            int expectedNumOfSuccessfulTryAdds;
            if (numOfAdds <= capacity)
                expectedNumOfSuccessfulTryAdds = numOfAdds;
            else
                expectedNumOfSuccessfulTryAdds = capacity;

            for (int i = 0; i < numOfAdds; ++i)
            {
                if (blockingCollections == null)
                {
                    if (blockingCollection.TryAdd(numberToAdd))
                    {
                        numOfTrueTryAdds++;
                    }
                }
                else
                {
                    int indexOfCollectionThatAcceptedTheOperation = BlockingCollection<int>.TryAddToAny(blockingCollections, numberToAdd);
                    if (indexOfCollectionThatAcceptedTheOperation == indexOfBlockingCollectionUnderTest)
                    {
                        numOfTrueTryAdds++;
                    }
                    else if (i < expectedNumOfSuccessfulTryAdds)
                    {
                        Console.WriteLine("AddAnyTakeAny: numOfAdds: {0}, numOfTakes: {1}, boundCapacity: {2}, indexOfBlockingCollectionUnderTest: {3}",
                            numOfAdds,
                            numOfTakes,
                            boundedCapacity,
                            indexOfBlockingCollectionUnderTest);
                        Assert.False(true, String.Format(" > test failed - TryAddToAny returned #{0} while it should return #{1}", indexOfCollectionThatAcceptedTheOperation, indexOfBlockingCollectionUnderTest));
                    }
                }
                if (i < expectedNumOfSuccessfulTryAdds)
                {
                    concurrentCollection.TryAdd(numberToAdd);
                }
            }
            if (numOfTrueTryAdds != expectedNumOfSuccessfulTryAdds)
            {
                Console.WriteLine("AddAnyTakeAny: numOfAdds: {0}, numOfTakes: {1}, boundCapacity: {2}, indexOfBlockingCollectionUnderTest: {3}",
                            numOfAdds,
                            numOfTakes,
                            boundedCapacity,
                            indexOfBlockingCollectionUnderTest);
                Assert.False(true, String.Format(" > test failed - expected #{0} calls to TryAdd will return true while actual is #{1}", expectedNumOfSuccessfulTryAdds, numOfTrueTryAdds));
            }
            if (concurrentCollection.Count != blockingCollection.Count)
            {
                Console.WriteLine("AddAnyTakeAny: numOfAdds: {0}, numOfTakes: {1}, boundCapacity: {2}, indexOfBlockingCollectionUnderTest: {3}",
                            numOfAdds,
                            numOfTakes,
                            boundedCapacity,
                            indexOfBlockingCollectionUnderTest);
                Assert.False(true, String.Format(" > test failed - collections count differs: blockingCollection = {0}, concurrentCollection = {1}",
                                    blockingCollection.Count,
                                    concurrentCollection.Count));
            }
            int itemFromBlockingCollection;
            int itemFromConcurrentCollection;
            int numOfTrueTryTakes = 0;
            int expectedNumOfSuccessfulTryTakes;
            if (expectedNumOfSuccessfulTryAdds <= numOfTakes)
                expectedNumOfSuccessfulTryTakes = expectedNumOfSuccessfulTryAdds;
            else
                expectedNumOfSuccessfulTryTakes = numOfTakes;

            if (blockingCollections != null)
            {
                //Initialize all other blocking collections to be empty so that Takes are done on blockingCollection
                for (int i = 0; i < blockingCollections.Length; ++i)
                {
                    if (i != indexOfBlockingCollectionUnderTest)
                    {
                        blockingCollections[i] = ConstructBlockingCollection<int>();
                    }
                }
            }

            for (int i = 0; i < numOfTakes; ++i)
            {
                if (blockingCollections == null)
                {
                    if (blockingCollection.TryTake(out itemFromBlockingCollection))
                    {
                        numOfTrueTryTakes++;
                    }
                }
                else
                {
                    int indexOfCollectionThatAcceptedTheOperation = BlockingCollection<int>.TryTakeFromAny(blockingCollections, out itemFromBlockingCollection);
                    if (indexOfCollectionThatAcceptedTheOperation == indexOfBlockingCollectionUnderTest)
                    {
                        numOfTrueTryTakes++;
                    }
                    else if (i < expectedNumOfSuccessfulTryTakes)
                    {
                        Console.WriteLine("AddAnyTakeAny: numOfAdds: {0}, numOfTakes: {1}, boundCapacity: {2}, indexOfBlockingCollectionUnderTest: {3}",
                            numOfAdds,
                            numOfTakes,
                            boundedCapacity,
                            indexOfBlockingCollectionUnderTest);
                        Assert.False(true, String.Format(" > test failed - TryTakeFromAny returned #{0} while it should return #{1}", indexOfCollectionThatAcceptedTheOperation, indexOfBlockingCollectionUnderTest));
                    }

                }
                if (i < expectedNumOfSuccessfulTryTakes)
                {
                    concurrentCollection.TryTake(out itemFromConcurrentCollection);
                    if (itemFromBlockingCollection != itemFromConcurrentCollection)
                    {
                        Console.WriteLine("AddAnyTakeAny: numOfAdds: {0}, numOfTakes: {1}, boundCapacity: {2}, indexOfBlockingCollectionUnderTest: {3}",
                            numOfAdds,
                            numOfTakes,
                            boundedCapacity,
                            indexOfBlockingCollectionUnderTest);
                        Assert.False(true, String.Format(" > test failed - Taked elements differ : itemFromBlockingCollection = {0}, itemFromConcurrentCollection = {1}",
                                            itemFromBlockingCollection,
                                            itemFromConcurrentCollection));
                    }
                }

            }
            if (numOfTrueTryTakes != expectedNumOfSuccessfulTryTakes)
            {
                Console.WriteLine("AddAnyTakeAny: numOfAdds: {0}, numOfTakes: {1}, boundCapacity: {2}, indexOfBlockingCollectionUnderTest: {3}",
                            numOfAdds,
                            numOfTakes,
                            boundedCapacity,
                            indexOfBlockingCollectionUnderTest);
                Assert.False(true, String.Format(" > test failed - expected #{0} calls to TryTake will return true while actual is #{1}", expectedNumOfSuccessfulTryTakes, numOfTrueTryTakes));
            }
            int expectedCount = expectedNumOfSuccessfulTryAdds - expectedNumOfSuccessfulTryTakes;
            expectedCount = (expectedCount < 0) ? 0 : expectedCount;

            if (blockingCollection.Count != expectedCount)
            {
                Console.WriteLine("AddAnyTakeAny: numOfAdds: {0}, numOfTakes: {1}, boundCapacity: {2}, indexOfBlockingCollectionUnderTest: {3}",
                            numOfAdds,
                            numOfTakes,
                            boundedCapacity,
                            indexOfBlockingCollectionUnderTest);
                Assert.False(true, String.Format(" > test failed - count is not as expected: expected = {0}, actual = {1}",
                    expectedCount,
                    blockingCollection.Count));

            }

            //Test the new behaviour of TryTakeFromAny after Dev10 to return false if all collections are completed, this is the same as before
            // except it was throwing when the timeout is -1, now will return false immediately

            var collectionsArray = new[] { new BlockingCollection<int>(), new BlockingCollection<int>() };
            collectionsArray[0].CompleteAdding();
            collectionsArray[1].CompleteAdding();
            int result;
            int index = BlockingCollection<int>.TryTakeFromAny(collectionsArray, out result, Timeout.Infinite);
            if (index != -1)
            {
                Assert.False(true, " > test failed - TryTakeFromAny succeeded and exepected failure because all collections are completed");
            }
        }

        /// <summary>Constructs and returns an unbounded blocking collection.</summary>
        /// <typeparam name="T">The type of the elements in the blocking collection.</typeparam>
        /// <returns>An unbounded blocking collection.</returns>
        private static BlockingCollection<T> ConstructBlockingCollection<T>()
        {
            return ConstructBlockingCollection<T>(-1);
        }

        /// <summary>Constructs and returns a full bounded blocking collection.</summary>
        /// <typeparam name="T">The type of the elements in the blocking collection.</typeparam>
        /// <returns>An full bounded blocking collection.</returns>
        private static BlockingCollection<T> ConstructFullBlockingCollection<T>()
        {
            BlockingCollection<T> blockingCollection = ConstructBlockingCollection<T>(1);
            blockingCollection.Add(default(T));
            return blockingCollection;
        }

        /// <summary>Constructs and returns a blocking collection.</summary>
        /// <typeparam name="T">The type of the elements in the blocking collection.</typeparam>
        /// <param name="boundedCapacity">The bounded capacity of the collection.</param>
        /// <returns>A blocking collection.</returns>
        private static BlockingCollection<T> ConstructBlockingCollection<T>(int boundedCapacity)
        {
            ConcurrentStackCollection<T> concurrentCollection = new ConcurrentStackCollection<T>();
            BlockingCollection<T> blockingCollection = null;

            if (boundedCapacity == -1)
            {
                blockingCollection = new BlockingCollection<T>(concurrentCollection);
            }
            else
            {
                blockingCollection = new BlockingCollection<T>(concurrentCollection, boundedCapacity);
            }
            return blockingCollection;
        }

        /// <summary>Verifies that the elements in sortedElementsInCollection are a sequence from start to end.</summary>
        /// <param name="sortedElementsInCollection">The enumerable containing the elements.</param>
        /// <param name="start">The start of the sequence.</param>
        /// <param name="end">The end of the sequence.</param>
        /// <returns></returns>
        private static void VerifyElementsAreMembersOfSequence(IEnumerable sortedElementsInCollection, int start, int end)
        {
            int current = start;
            bool elementsAreMembersOfSequence = true;

            foreach (int element in sortedElementsInCollection)
            {
                if (element != current)
                {
                    elementsAreMembersOfSequence = false;
                }
                current++;
            }
            if ((current - 1) != end)
            {
                if ((current - 1) < end)
                {
                    Assert.False(true, String.Format("VerifyElementsAreMembersOfSequence: > test failed - the collection contains less elements than expected: actual={0}, expected{1}",
                                        current - start,
                                        end - start + 1));
                }
            }
            if (!elementsAreMembersOfSequence)
            {
                StringBuilder builder = new StringBuilder("VerifyElementsAreMembersOfSequence: > test failed - elements are not properly added");
                foreach (int element in sortedElementsInCollection)
                    builder.AppendFormat(" > {0}, ", element);
                Assert.False(true, builder.ToString());
            }
        }

        /// <summary>This is a Stack implementing IConcurrentCollection to be used in the tests of BlockingCollection.</summary>
        /// <typeparam name="T">The type of elements stored in the stack.</typeparam>
        private class ConcurrentStackCollection<T> : IProducerConsumerCollection<T>
        {
            ConcurrentStack<T> concurrentStack;

            public ConcurrentStackCollection()
            {
                concurrentStack = new ConcurrentStack<T>();
            }

            #region IProducerConsumerCollection<T> Members

            public void CopyTo(T[] dest, int idx)
            {
                concurrentStack.CopyTo(dest, idx);
            }

            public T[] ToArray()
            {
                return concurrentStack.ToArray();
            }

            public bool TryAdd(T item)
            {
                concurrentStack.Push(item);
                return true;
            }

            public bool TryTake(out T item)
            {
                return concurrentStack.TryPop(out item);
            }

            #endregion

            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return concurrentStack.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return concurrentStack.GetEnumerator();
            }

            #endregion

            #region ICollection Members

            public void CopyTo(Array array, int index)
            {
                ((ICollection)concurrentStack).CopyTo(array, index);
            }

            public int Count
            {
                get { return concurrentStack.Count; }
            }

            public bool IsSynchronized
            {
                get { throw new NotImplementedException(); }
            }

            public object SyncRoot
            {
                get { throw new NotImplementedException(); }
            }

            #endregion
        }

        /// <summary>
        /// Internal IPCC implementer that its TryAdd returns false
        /// </summary>
        private class QueueProxy1<T> : ConcurrentQueue<T>, IProducerConsumerCollection<T>
        {
            bool IProducerConsumerCollection<T>.TryAdd(T item)
            {
                return false;
            }
        }

        #endregion
    }
}
