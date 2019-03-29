// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    /// <summary>The class that contains the unit tests of the BlockingCollection.</summary>
    public class BlockingCollectionTests
    {
        [Fact]
        public static void TestBasicScenarios()
        {
            BlockingCollection<int> bc = new BlockingCollection<int>(3);
            Task[] tks = new Task[2];
            // A simple blocking consumer with no cancellation.
            int expect = 1;
            tks[0] = Task.Run(() =>
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
            });

            // A simple blocking producer with no cancellation.
            tks[1] = Task.Run(() =>
            {
                bc.Add(1);
                bc.Add(2);
                bc.Add(3);
                // Let consumer know we are done.
                bc.CompleteAdding();
            });

            Task.WaitAll(tks);
        }

        /// <summary>
        /// BlockingCollection throws InvalidOperationException when calling CompleteAdding even after adding and taking all elements
        /// </summary>
        /// <returns></returns>
        [Fact]
        public static void TestBugFix544259()
        {
            int count = 8;
            CountdownEvent cde = new CountdownEvent(count);
            BlockingCollection<object> bc = new BlockingCollection<object>();

            //creates 8 consumers, each calling take to block itself
            for (int i = 0; i < count; i++)
            {
                Task.Run(() =>
                {
                    bc.Take();
                    cde.Signal();
                });
            }
            //create 8 producers, each calling add to unblock a consumer
            for (int i = 0; i < count; i++)
            {
                Task.Run(() =>
                {
                    bc.Add(new object());
                });
            }

            //CountdownEvent waits till all consumers are unblocked
            cde.Wait();
            bc.CompleteAdding();
        }

        // This code was suffering occasional ObjectDisposedExceptions due to
        // the expected race between cts.Dispose and the cts.Cancel coming from the linking sources.
        // Since the change to wait as part of CTS.Dispose, the ODE no longer occurs
        // but we keep the test as a good example of how cleanup of linkedCTS must be carefully handled
        // to prevent users of the source CTS mistakenly calling methods on disposed targets.
        [Fact]
        public static void TestBugFix626345()
        {
            const int noOfProducers = 1;
            const int noOfConsumers = 7;
            const int noOfItemsToProduce = 3;
            //Console.WriteLine("Producer: {0}, Consumer: {1}, Items: {2}", noOfProducers, noOfConsumers, noOfItemsToProduce);

            BlockingCollection<long> m_BlockingQueueUnderTest = new BlockingCollection<long>(new ConcurrentQueue<long>());

            Task[] producers = new Task[noOfProducers];
            for (int prodIndex = 0; prodIndex < noOfProducers; prodIndex++)
            {
                producers[prodIndex] = Task.Run(() =>
                {
                    for (int dummyItem = 0; dummyItem < noOfItemsToProduce; dummyItem++)
                    {
                        // dummy code for doing something
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
                            // dummy code for doing something
                            int i = 0;
                            for (int j = 0; j < 5; j++)
                            {
                                i += j;
                            }
                        }
                    }
                });
            }

            Task.WaitAll(producers);

            m_BlockingQueueUnderTest.CompleteAdding(); //signal all producers are done adding items

            //Wait for the consumers to finish.
            Task.WaitAll(consumers);

            // success is not suffering exceptions.
        }

        /// <summary>
        /// Making sure if TryTakeFromAny succeeds, it returns the correct index
        /// </summary>
        [Fact]
        public static void TestBugFix914998()
        {
            var producer1 = new BlockingCollection<int>();
            var producer2 = new BlockingCollection<int>();
            var producerArray = new BlockingCollection<int>[] { producer2, producer1 };

            producer2.CompleteAdding();

            Task.Run(() =>
            {
                producer1.Add(100);
            });

            int ignored;
            Assert.Equal(1, BlockingCollection<int>.TryTakeFromAny(producerArray, out ignored, -1));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void TestDebuggerAttributes()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(new BlockingCollection<int>());
            BlockingCollection<int> col = new BlockingCollection<int> { 1, 2, 3 };
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(col);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            int[] items = itemProperty.GetValue(info.Instance) as int[];
            Assert.Equal(col, items);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(new BlockingCollection<int>());
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        /// <summary>
        /// Tests the default BlockingCollection constructor which initializes a BlockingQueue
        /// </summary>
        /// <param name="boundedCapacity"></param>
        [Theory]
        [InlineData(-1)]
        [InlineData(10)]
        public static void TestConstruction(int boundedCapacity)
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

            Assert.Equal(boundedCapacity, blockingQueue.BoundedCapacity);

            // Test for queue properties. Taken items should be in the same order of the insertion
            int count = boundedCapacity != -1 ? boundedCapacity : 10;

            for (int i = 0; i < count; i++)
            {
                blockingQueue.Add(i);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(i, blockingQueue.Take());
            }
        }

        /// <summary>Adds "numOfAdds" elements to the BlockingCollection and then Takes "numOfTakes" elements and 
        /// checks that the count is as expected, the elements removed matched those added and verifies the return 
        /// values of TryAdd() and TryTake().</summary>
        /// <param name="numOfAdds">The number of elements to add to the BlockingCollection.</param>
        /// <param name="numOfTakes">The number of elements to Take from the BlockingCollection.</param>
        /// <param name="boundedCapacity">The bounded capacity of the BlockingCollection, -1 is unbounded.</param>
        [Theory]
        [InlineData(1, 1, -1)]
        [InlineData(5, 3, 1)]
        public static void TestAddTake(int numOfAdds, int numOfTakes, int boundedCapacity)
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(boundedCapacity);
            AddAnyTakeAny(numOfAdds, numOfTakes, boundedCapacity, blockingCollection, null, -1);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(10, 10, 9)]
        [OuterLoop]
        public static void TestAddTake_Longrunning(int numOfAdds, int numOfTakes, int boundedCapacity)
        {
            TestAddTake(numOfAdds, numOfTakes, boundedCapacity);
        }

        /// <summary> Launch some threads performing Add operation and makes sure that all items added are
        /// present in the collection.</summary>
        /// <param name="numOfThreads">Number of producer threads.</param>
        /// <param name="numOfElementsPerThread">Number of elements added per thread.</param>
        [Theory]
        [InlineData(2, 1024)]
        [InlineData(8, 512)]
        public static void TestConcurrentAdd(int numOfThreads, int numOfElementsPerThread)
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
                        Assert.True(blockingCollection.TryAdd(j));
                    }
                }, i, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            mre.Set();
            Task.WaitAll(threads);

            int expectedCount = numOfThreads * numOfElementsPerThread;
            Assert.Equal(expectedCount, blockingCollection.Count);
            List<int> sortedElementsInCollection = new List<int>(blockingCollection);
            sortedElementsInCollection.Sort();
            VerifyElementsAreMembersOfSequence(sortedElementsInCollection, 0, expectedCount - 1);
        }

        /// <summary>Launch threads/2 producers and threads/2 consumers then make sure that all elements produced
        /// are consumed by consumers with no element lost nor consumed more than once.</summary>
        /// <param name="threads">Total number of producer and consumer threads.</param>
        /// <param name="numOfElementsPerThread">Number of elements to Add/Take per thread.</param>
        [Theory]
        [InlineData(8, 1024)]
        public static void TestConcurrentAddTake(int numOfThreads, int numOfElementsPerThread)
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
                            Assert.True(blockingCollection.TryAdd(j));
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
            Assert.Equal(0, blockingCollection.Count);

            int[] arrayOfRemovedElementsFromAllThreads = (int[])(removedElementsFromAllThreads.ToArray());
            List<int> sortedElementsInCollection = new List<int>(arrayOfRemovedElementsFromAllThreads);
            sortedElementsInCollection.Sort();
            VerifyElementsAreMembersOfSequence(sortedElementsInCollection, 0, (numOfThreads / 2 * numOfElementsPerThread) - 1);
        }

        [Fact]
        public static void Test4_Dispose()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            blockingCollection.Dispose();

            int value = 0;
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.Add(value++));
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.TryAdd(value++));
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.TryAdd(value++, 1));
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.TryAdd(value++, new TimeSpan(1)));
            int item;
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.Take());
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.TryTake(out item));
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.TryTake(out item, 2));
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.TryTake(out item, new TimeSpan(2)));

            const int NUM_OF_COLLECTIONS = 10;
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[NUM_OF_COLLECTIONS];
            for (int i = 0; i < NUM_OF_COLLECTIONS - 1; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>(-1);
            }

            blockingCollections[NUM_OF_COLLECTIONS - 1] = blockingCollection;

            Assert.Throws<ObjectDisposedException>(() => BlockingCollection<int>.AddToAny(blockingCollections, value++));
            Assert.Throws<ObjectDisposedException>(() => BlockingCollection<int>.TryAddToAny(blockingCollections, value++));
            Assert.Throws<ObjectDisposedException>(() => BlockingCollection<int>.TryAddToAny(blockingCollections, value++, 3));
            Assert.Throws<ObjectDisposedException>(() => BlockingCollection<int>.TryAddToAny(blockingCollections, value++, new TimeSpan(3)));

            Assert.Throws<ObjectDisposedException>(() => BlockingCollection<int>.TakeFromAny(blockingCollections, out item));
            Assert.Throws<ObjectDisposedException>(() => BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item));
            Assert.Throws<ObjectDisposedException>(() => BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, 4));
            Assert.Throws<ObjectDisposedException>(() => BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, new TimeSpan(4)));

            Assert.Throws<ObjectDisposedException>(() => blockingCollection.CompleteAdding());
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.ToArray());
            Assert.Throws<ObjectDisposedException>(() => blockingCollection.CopyTo(new int[1], 0));

            int? boundedCapacity = 0;
            Assert.Throws<ObjectDisposedException>(() => { boundedCapacity = blockingCollection.BoundedCapacity; });
            bool isCompleted = false;
            Assert.Throws<ObjectDisposedException>(() => { isCompleted = blockingCollection.IsCompleted; });
            bool addingIsCompleted = false;
            Assert.Throws<ObjectDisposedException>(() => { addingIsCompleted = blockingCollection.IsAddingCompleted; });
            int count = 0;
            Assert.Throws<ObjectDisposedException>(() => { count = blockingCollection.Count; });
            object syncRoot = null;
            Assert.Throws<NotSupportedException>(() => { syncRoot = ((ICollection)blockingCollection).SyncRoot; });
            bool isSynchronized = false;
            Assert.Throws<ObjectDisposedException>(() => { isSynchronized = ((ICollection)blockingCollection).IsSynchronized; });

            blockingCollection.Dispose();
            Assert.Throws<ObjectDisposedException>(() =>
            {
                foreach (int element in blockingCollection)
                {
                    int temp = element;
                };
            } );

            Assert.Throws<ObjectDisposedException>(() =>
            {
                foreach (int element in blockingCollection.GetConsumingEnumerable())
                {
                    int temp = element;
                }
            });
        }

        /// <summary>Validates GetEnumerator and makes sure that BlockingCollection.GetEnumerator() produces the 
        /// same results as IConcurrentCollection.GetEnumerator().</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        public static void Test5_GetEnumerator()
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
            Assert.Equal(resultOfEnumOfBlockingCollection.Count, resultOfEnumOfConcurrentCollection.Count);

            for (int i = 0; i < resultOfEnumOfBlockingCollection.Count; ++i)
            {
                Assert.Equal<int>(resultOfEnumOfBlockingCollection[i], resultOfEnumOfConcurrentCollection[i]);
            }
        }

        /// <summary>Validates GetConsumingEnumerator and makes sure that BlockingCollection.GetConsumingEnumerator() 
        /// produces the same results as if call Take in a loop.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        public static void Test6_GetConsumingEnumerable()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            BlockingCollection<int> blockingCollectionMirror = ConstructBlockingCollection<int>();

            const int MAX_NUM_TO_ADD = 100;
            for (int i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                blockingCollectionMirror.Add(i);
            }

            Assert.Equal(MAX_NUM_TO_ADD, blockingCollection.Count);

            List<int> resultOfEnumOfBlockingCollection = new List<int>();

            //CompleteAdding() is called so that the MoveNext() on the Enumerable resulting from 
            //GetConsumingEnumerable return false after the collection is empty.
            blockingCollection.CompleteAdding();
            foreach (int i in blockingCollection.GetConsumingEnumerable())
            {
                resultOfEnumOfBlockingCollection.Add(i);
            }

            Assert.Equal(0, blockingCollection.Count);

            List<int> resultOfEnumOfBlockingCollectionMirror = new List<int>();
            while (blockingCollectionMirror.Count != 0)
            {
                resultOfEnumOfBlockingCollectionMirror.Add(blockingCollectionMirror.Take());
            }

            Assert.Equal(resultOfEnumOfBlockingCollection.Count, resultOfEnumOfBlockingCollectionMirror.Count);

            for (int i = 0; i < resultOfEnumOfBlockingCollection.Count; ++i)
            {
                Assert.Equal<int>(resultOfEnumOfBlockingCollection[i], resultOfEnumOfBlockingCollectionMirror[i]);
            }
        }

        /// <summary>Validates that after CompleteAdding() is called, future calls to Add will throw exceptions, calls
        /// to Take will not block waiting for more input, and calls to MoveNext on the enumerator returned from GetEnumerator 
        /// on the enumerable returned from GetConsumingEnumerable will return false when the collection’s count reaches 0.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        public static void Test7_CompleteAdding()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            blockingCollection.Add(0);
            blockingCollection.CompleteAdding();

            Assert.Throws<InvalidOperationException>(() => blockingCollection.Add(1));
            Assert.Equal(1, blockingCollection.Count);

            blockingCollection.Take();
            Assert.Throws<InvalidOperationException>(() => blockingCollection.Take());

            int item = 0;
            Assert.False(blockingCollection.TryTake(out item));

            int counter = 0;
            foreach (int i in blockingCollection.GetConsumingEnumerable())
            {
                counter++;
            }

            Assert.Equal(0, counter);
        }

        [Fact]
        public static void Test7_ConcurrentAdd_CompleteAdding()
        {
            BlockingCollection<ushort> blockingCollection = ConstructBlockingCollection<ushort>();
            Task[] threads = new Task[4];
            int succeededAdd = 0;
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = Task.Run(() =>
                {
                    for (ushort j = 0; j < 512; j++)
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
            Task.WaitAll(threads);

            int count2 = blockingCollection.Count;
            Assert.Equal(count1, count2);
            Assert.Equal(count1, succeededAdd);
        }

        /// <summary>Validates that BlockingCollection.ToArray() produces same results as 
        /// IConcurrentCollection.ToArray().</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        public static void Test8_ToArray()
        {
            ConcurrentStackCollection<byte> concurrentCollection = new ConcurrentStackCollection<byte>();
            BlockingCollection<byte> blockingCollection = ConstructBlockingCollection<byte>();

            const byte MAX_NUM_TO_ADD = 100;
            for (byte i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                concurrentCollection.TryAdd(i);
            }

            byte[] arrBlockingCollection = blockingCollection.ToArray();
            byte[] arrConcurrentCollection = concurrentCollection.ToArray();

            Assert.Equal(arrBlockingCollection.Length, arrConcurrentCollection.Length);

            for (byte i = 0; i < arrBlockingCollection.Length; ++i)
            {
                Assert.Equal(arrBlockingCollection[i], arrConcurrentCollection[i]);
            }
        }

        /// <summary>Validates that BlockingCollection.CopyTo() produces same results as IConcurrentCollection.CopyTo().</summary>
        /// <param name="indexOfInsertion">The zero-based index in the array at which copying begins.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(8)]
        public static void TestCopyTo(int indexOfInsertion)
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

            Assert.Equal(arrConcurrentCollection, arrBlockingCollection);
        }

        /// <summary>Validates BlockingCollection.Count.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        public static void Test10_Count()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(1);
            Assert.Equal(0, blockingCollection.Count);

            blockingCollection.Add(1);
            Assert.Equal(1, blockingCollection.Count);

            blockingCollection.TryAdd(1);
            Assert.Equal(1, blockingCollection.Count);

            blockingCollection.Take();
            Assert.Equal(0, blockingCollection.Count);
        }

        /// <summary>Validates BlockingCollection.BoundedCapacity.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        public static void Test11_BoundedCapacity()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(1);
            Assert.Equal(1, blockingCollection.BoundedCapacity);

            blockingCollection = ConstructBlockingCollection<int>();
            Assert.Equal(-1, blockingCollection.BoundedCapacity);
        }

        /// <summary>Validates BlockingCollection.IsCompleted and BlockingCollection.AddingIsCompleted.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        public static void Test12_IsCompleted_AddingIsCompleted()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            Assert.False(blockingCollection.IsAddingCompleted, 
                "Test12_IsCompleted_AddingIsCompleted: > test failed (Empty Collection) - AddingIsCompleted should be false");

            Assert.False(blockingCollection.IsCompleted,
                "Test12_IsCompleted_AddingIsCompleted: > test failed (Empty Collection) - IsCompleted should be false");

            blockingCollection.CompleteAdding();

            Assert.True(blockingCollection.IsAddingCompleted,
                "Test12_IsCompleted_AddingIsCompleted: > test failed (Empty Collection) - AddingIsCompleted should be true");

            Assert.True(blockingCollection.IsCompleted,
                "Test12_IsCompleted_AddingIsCompleted: > test failed (Empty Collection) - IsCompleted should be true");

            blockingCollection = ConstructBlockingCollection<int>();
            blockingCollection.Add(0);
            blockingCollection.CompleteAdding();

            Assert.True(blockingCollection.IsAddingCompleted,
                "Test12_IsCompleted_AddingIsCompleted: > test failed (NonEmpty Collection) - AddingIsCompleted should be true");

            Assert.False(blockingCollection.IsCompleted,
                "Test12_IsCompleted_AddingIsCompleted: > test failed (NonEmpty Collection) - IsCompleted should be false");

            blockingCollection.Take();

            Assert.True(blockingCollection.IsCompleted,
                "Test12_IsCompleted_AddingIsCompleted: > test failed (NonEmpty Collection) - IsCompleted should be true");
        }

        /// <summary>Validates BlockingCollection.IsSynchronized and BlockingCollection.SyncRoot.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        public static void Test13_IsSynchronized_SyncRoot()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            Assert.Throws<NotSupportedException>(() => { var dummy = ((ICollection)blockingCollection).SyncRoot; });

            Assert.False(((ICollection)blockingCollection).IsSynchronized,
                "Test13_IsSynchronized_SyncRoot: > test failed - IsSynchronized should be false");
        }

        /// <summary>Initializes an array of blocking collections such that all are full except one in case of Adds and
        /// all are empty except one (the same blocking collection) in case of Takes.
        /// Adds "numOfAdds" elements to the BlockingCollection and then takes "numOfTakes" elements and checks
        /// that the count is as expected, the elements taken matched those added and verifies the return values of 
        /// TryAdd() and TryTake().</summary>
        /// <param name="numOfAdds">Number of elements to add.</param>
        /// <param name="numOfTakes">Number of elements to take.</param>
        /// <param name="numOfBlockingCollections">Length of BlockingCollections array.</param>
        /// <param name="indexOfBlockingCollectionUnderTest">Index of the BlockingCollection that will accept the operations.</param>
        /// <param name="boundedCapacity">The bounded capacity of the BlockingCollection under test.</param>
        [Theory]
        [InlineData(1, 1, 16, 0, -1)]
        [InlineData(10, 10, 16, 14, 10)]
        public static void TestAddAnyTakeAny(int numOfAdds,
                                                 int numOfTakes,
                                                 int numOfBlockingCollections,
                                                 int indexOfBlockingCollectionUnderTest,
                                                 int boundedCapacity)
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(boundedCapacity);
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[numOfBlockingCollections];

            AddAnyTakeAny(numOfAdds, numOfTakes, boundedCapacity, blockingCollection, blockingCollections, indexOfBlockingCollectionUnderTest);
        }

        [Theory]
        [InlineData(10, 9, 16, 15, -1)]
        [InlineData(10, 10, 16, 1, 9)]
        [OuterLoop]
        public static void TestAddAnyTakeAny_Longrunning(int numOfAdds, int numOfTakes, int numOfBlockingCollections, int indexOfBlockingCollectionUnderTest, int boundedCapacity)
        {
            TestAddAnyTakeAny(numOfAdds, numOfTakes, numOfBlockingCollections, indexOfBlockingCollectionUnderTest, boundedCapacity);
        }

        /// <summary>Launch threads/2 producers and threads/2 consumers then makes sure that all elements produced
        /// are consumed by consumers with no element lost nor consumed more than once.</summary>
        /// <param name="threads">Total number of producer and consumer threads.</param>
        /// <param name="numOfElementsPerThread">Number of elements to Add/Take per thread.</param>
        [Theory]
        [InlineData(4, 2048, 2, 64)]
        [OuterLoop]
        private static void TestConcurrentAddAnyTakeAny(int numOfThreads, int numOfElementsPerThread, int numOfCollections, int boundOfCollections)
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
                            Assert.InRange(BlockingCollection<int>.AddToAny(blockingCollections, j), 0, int.MaxValue);
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
                            Assert.InRange(BlockingCollection<int>.TakeFromAny(blockingCollections, out item), 0, int.MaxValue);
                            removedElements.Add(item);
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
            Assert.All(blockingCollections, c => Assert.Equal(expectedCount, c.Count));

            int[] arrayOfRemovedElementsFromAllThreads = (int[])(removedElementsFromAllThreads.ToArray());
            List<int> sortedElementsInCollection = new List<int>(arrayOfRemovedElementsFromAllThreads);
            sortedElementsInCollection.Sort();
            VerifyElementsAreMembersOfSequence(sortedElementsInCollection, 0, (numOfThreads / 2 * numOfElementsPerThread) - 1);
        }

        /// <summary>Validates the constructor of BlockingCollection.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        [Fact]
        public static void Test16_Ctor()
        {
            BlockingCollection<int> blockingCollection = null;

            Assert.Throws<ArgumentNullException>( () => { blockingCollection = new BlockingCollection<int>(null); } );
            Assert.Throws<ArgumentNullException>(() => { blockingCollection = new BlockingCollection<int>(null, 1); });

            Assert.Throws<ArgumentOutOfRangeException>(() => { blockingCollection = new BlockingCollection<int>(new ConcurrentStackCollection<int>(), 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { blockingCollection = new BlockingCollection<int>(new ConcurrentStackCollection<int>(), -1); });

            AssertExtensions.Throws<ArgumentException>(null, () => 
            {
                ConcurrentStackCollection<int> concurrentStack = new ConcurrentStackCollection<int>();
                concurrentStack.TryAdd(1);
                concurrentStack.TryAdd(2);
                blockingCollection = new BlockingCollection<int>(concurrentStack, 1);
            });
        }

        /// <summary>Verifies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        public static void Test17_AddExceptions()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            Assert.Throws<ArgumentOutOfRangeException>(() => blockingCollection.TryAdd(0, new TimeSpan(0, 0, 0, 1, 2147483647)) );
            Assert.Throws<ArgumentOutOfRangeException>(() => blockingCollection.TryAdd(0, -2) );

            Assert.Throws<InvalidOperationException>(() =>
                {
                    blockingCollection.CompleteAdding();
                    blockingCollection.TryAdd(0);
                });

            // test if the underlyingcollection.TryAdd returned false
            BlockingCollection<int> bc = new BlockingCollection<int>(new QueueProxy1<int>());
            Assert.Throws<InvalidOperationException>(() => bc.Add(1));
        }

        /// <summary>Verifies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        public static void Test18_TakeExceptions()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            int item;
            Assert.Throws<ArgumentOutOfRangeException>(() => blockingCollection.TryTake(out item, new TimeSpan(0, 0, 0, 1, 2147483647)) );
            Assert.Throws<ArgumentOutOfRangeException>(() =>  blockingCollection.TryTake(out item, -2) );

            Assert.Throws<InvalidOperationException>(() => 
                {
                    blockingCollection.CompleteAdding();
                    blockingCollection.Take();
                });
        }

        /// <summary>Verifies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        public static void Test19_AddAnyExceptions()
        {
            const int NUM_OF_COLLECTIONS = 2;
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[NUM_OF_COLLECTIONS];
            for (int i = 0; i < NUM_OF_COLLECTIONS; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>();
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => BlockingCollection<int>.TryAddToAny(blockingCollections, 0, new TimeSpan(0, 0, 0, 1, 2147483647)) );
            Assert.Throws<ArgumentOutOfRangeException>(() => BlockingCollection<int>.TryAddToAny(blockingCollections, 0, -2) );

            AssertExtensions.Throws<ArgumentException>("collections", () => BlockingCollection<int>.TryAddToAny(new BlockingCollection<int>[NUM_OF_COLLECTIONS], 0));
            AssertExtensions.Throws<ArgumentException>("collections", () => BlockingCollection<int>.TryAddToAny(new BlockingCollection<int>[0], 0));

            AssertExtensions.Throws<ArgumentException>("collections", () =>
            {
                blockingCollections[NUM_OF_COLLECTIONS - 1].CompleteAdding();
                BlockingCollection<int>.TryAddToAny(blockingCollections, 0);
            });

            Assert.Throws<ArgumentNullException>(() => BlockingCollection<int>.TryAddToAny(null, 0));

            // test if the underlyingcollection.TryAdd returned false
            BlockingCollection<int> collection = new BlockingCollection<int>(new QueueProxy1<int>());
            Assert.Throws<InvalidOperationException>(() => BlockingCollection<int>.AddToAny(new BlockingCollection<int>[] { collection }, 1) );
        }

        /// <summary>Verifies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        public static void Test20_TakeAnyExceptions()
        {
            const int NUM_OF_COLLECTIONS = 2;
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[NUM_OF_COLLECTIONS];
            for (int i = 0; i < NUM_OF_COLLECTIONS; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>();
            }

            int item;
            Assert.Throws<ArgumentOutOfRangeException>(() => BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, new TimeSpan(0, 0, 0, 1, 2147483647)) );
            Assert.Throws<ArgumentOutOfRangeException>(() => BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, -2) );

            AssertExtensions.Throws<ArgumentException>("collections", () => BlockingCollection<int>.TryTakeFromAny(new BlockingCollection<int>[NUM_OF_COLLECTIONS], out item) );
            AssertExtensions.Throws<ArgumentException>("collections", () => BlockingCollection<int>.TryTakeFromAny(new BlockingCollection<int>[0], out item) );
            Assert.Throws<ArgumentNullException>(() => BlockingCollection<int>.TryTakeFromAny(null, out item) );

            // new behavior for TakeFromAny after Dev10, to throw ArgumentException if all the collections are completed, 
            // however TryTakeFromAny will return false
            for (int i = 0; i < NUM_OF_COLLECTIONS; ++i)
            {
                blockingCollections[i].CompleteAdding();
            }
            AssertExtensions.Throws<ArgumentException>("collections", () => BlockingCollection<int>.TakeFromAny(blockingCollections, out item) );
        }

        /// <summary>Verifies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        [Fact]
        public static void Test21_CopyToExceptions()
        {
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            blockingCollection.Add(0);
            blockingCollection.Add(0);
            int[] arr = new int[2];

            Assert.Throws<ArgumentNullException>(() => blockingCollection.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => blockingCollection.CopyTo(arr, -1));
            AssertExtensions.Throws<ArgumentException>("index", () =>  blockingCollection.CopyTo(arr, 2));
            AssertExtensions.Throws<ArgumentException>("array", () => 
                {
                    int[,] twoDArray = new int[2, 2];
                    ((ICollection)blockingCollection).CopyTo(twoDArray, 0);
                });

            AssertExtensions.Throws<ArgumentException>("array", () =>
            {
                float[,] twoDArray = new float[2, 2];
                ((ICollection)blockingCollection).CopyTo(twoDArray, 0);
            });
        }

        #region Helper Methods / Classes

        /// <summary>Initializes an array of blocking collections (if its not null) such that all are full except one in case 
        /// of Adds and all are empty except one (the same blocking collection) in case of Takes.
        /// Adds "numOfAdds" elements to the BlockingCollection and then takes "numOfTakes" elements and checks
        /// that the count is as expected, the elements taken matched those added and verifies the return values of 
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
            int capacity = (boundedCapacity == -1) ? int.MaxValue : boundedCapacity;

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
                    else
                    {
                        Assert.InRange(i, expectedNumOfSuccessfulTryAdds, int.MaxValue);
                    }
                }
                if (i < expectedNumOfSuccessfulTryAdds)
                {
                    concurrentCollection.TryAdd(numberToAdd);
                }
            }
            Assert.Equal(expectedNumOfSuccessfulTryAdds, numOfTrueTryAdds);
            Assert.Equal(blockingCollection.Count, concurrentCollection.Count);
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
                        Assert.InRange(i, expectedNumOfSuccessfulTryTakes, int.MaxValue);
                    }

                }
                if (i < expectedNumOfSuccessfulTryTakes)
                {
                    concurrentCollection.TryTake(out itemFromConcurrentCollection);
                    Assert.Equal(itemFromBlockingCollection, itemFromConcurrentCollection);
                }
            }
            Assert.Equal(expectedNumOfSuccessfulTryTakes, numOfTrueTryTakes);
            int expectedCount = expectedNumOfSuccessfulTryAdds - expectedNumOfSuccessfulTryTakes;
            expectedCount = (expectedCount < 0) ? 0 : expectedCount;
            Assert.Equal(expectedCount, blockingCollection.Count);

            //Test the new behavior of TryTakeFromAny after Dev10 to return false if all collections are completed, this is the same as before
            // except it was throwing when the timeout is -1, now will return false immediately

            var collectionsArray = new[] { new BlockingCollection<int>(), new BlockingCollection<int>() };
            collectionsArray[0].CompleteAdding();
            collectionsArray[1].CompleteAdding();
            int result;
            int index = BlockingCollection<int>.TryTakeFromAny(collectionsArray, out result, Timeout.Infinite);
            Assert.Equal(-1, index);
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
        private static void VerifyElementsAreMembersOfSequence(IEnumerable<int> sortedElementsInCollection, int start, int end)
        {
            int current = start;
            Assert.All(sortedElementsInCollection, elem => Assert.Equal(current++, elem));
            Assert.Equal(end, current - 1);
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
