// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class PartitionerStaticTests
    {
        [Fact]
        public static void TestStaticPartitioningIList()
        {
            RunTestWithAlgorithm(dataSize: 11, partitionCount: 8, algorithm: 0);
            RunTestWithAlgorithm(dataSize: 999, partitionCount: 1, algorithm: 0);
            RunTestWithAlgorithm(dataSize: 10000, partitionCount: 11, algorithm: 0);
        }

        [Fact]
        public static void TestStaticPartitioningArray()
        {
            RunTestWithAlgorithm(dataSize: 7, partitionCount: 4, algorithm: 1);
            RunTestWithAlgorithm(dataSize: 123, partitionCount: 1, algorithm: 1);
            RunTestWithAlgorithm(dataSize: 1000, partitionCount: 7, algorithm: 1);
        }

        [Fact]
        public static void TestLoadBalanceIList()
        {
            RunTestWithAlgorithm(dataSize: 7, partitionCount: 4, algorithm: 2);
            RunTestWithAlgorithm(dataSize: 123, partitionCount: 1, algorithm: 2);
            RunTestWithAlgorithm(dataSize: 1000, partitionCount: 7, algorithm: 2);
        }

        [Fact]
        public static void TestLoadBalanceArray()
        {
            RunTestWithAlgorithm(dataSize: 11, partitionCount: 8, algorithm: 3);
            RunTestWithAlgorithm(dataSize: 999, partitionCount: 1, algorithm: 3);
            RunTestWithAlgorithm(dataSize: 10000, partitionCount: 11, algorithm: 3);
        }

        [Fact]
        public static void TestLoadBalanceEnumerator()
        {
            RunTestWithAlgorithm(dataSize: 7, partitionCount: 4, algorithm: 4);
            RunTestWithAlgorithm(dataSize: 123, partitionCount: 1, algorithm: 4);
            RunTestWithAlgorithm(dataSize: 1000, partitionCount: 7, algorithm: 4);
        }

        #region Dispose tests. The dispose logic of PartitionerStatic

        // In the official dev unit test run, this test should be commented out
        // - Each time we call GetDynamicPartitions method, we create an internal "reader enumerator" to read the 
        // source data, and we need to make sure that whenever the object returned by GetDynamicPartitions is disposed,
        // the "reader enumerator" is also disposed.
        [Fact]
        public static void TestDisposeException()
        {
            var data = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var enumerable = new DisposeTrackingEnumerable<int>(data);
            var partitioner = Partitioner.Create(enumerable);
            var partition = partitioner.GetDynamicPartitions();
            IDisposable d = partition as IDisposable;
            Assert.NotNull(d);

            d.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { var enum1 = partition.GetEnumerator(); });
        }

        /// <summary>
        /// Race in Partitioner's dynamic partitioning Dispose logic
        /// After the fix, the partitioner created through Partitioner.Create(IEnumerable) has the following behavior:
        ///     1. reference counting in static partitioning. All partitions need to be disposed explicitly
        ///     2. no reference counting in dynamic partitioning. The partitioner need to be disposed explicitly
        /// </summary>
        /// <returns></returns>
        [Fact]
        public static void RunDynamicPartitioningDispose()
        {
            var p = Partitioner.Create(new int[] { 0, 1 });
            var d = p.GetDynamicPartitions();
            using (var e = d.GetEnumerator())
            {
                while (e.MoveNext()) { }
            }

            // should not throw
            using (var e = d.GetEnumerator()) { }; 
        }

        #endregion

        [Fact]
        public static void TestExceptions()
        {
            // Testing ArgumentNullException with data==null
            // Test ArgumentNullException of source data
            OrderablePartitioner<int> partitioner;
            for (int algorithm = 0; algorithm < 5; algorithm++)
            {
                Assert.Throws<ArgumentNullException>(() => { partitioner = PartitioningWithAlgorithm<int>(null, algorithm); });
            }
            // Test NotSupportedException of Reset: already tested in RunTestWithAlgorithm
            // Test InvalidOperationException: already tested in TestPartitioningCore

            // Test ArgumentOutOfRangeException of partitionCount == 0
            int[] data = new int[10000];
            for (int i = 0; i < 10000; i++)
                data[i] = i;

            //test GetOrderablePartitions method for 0-4 algorithms, try to catch ArgumentOutOfRangeException
            for (int algorithm = 0; algorithm < 5; algorithm++)
            {
                partitioner = PartitioningWithAlgorithm<int>(data, algorithm);

                Assert.Throws<ArgumentOutOfRangeException>(() => { var partitions1 = partitioner.GetOrderablePartitions(0); });
            }
        }

        [Fact]
        public static void TestEmptyPartitions()
        {
            int[] data = new int[0];

            // Test ArgumentNullException of source data
            OrderablePartitioner<int> partitioner;

            for (int algorithm = 0; algorithm < 5; algorithm++)
            {
                partitioner = PartitioningWithAlgorithm<int>(data, algorithm);
                //test GetOrderablePartitions
                var partitions1 = partitioner.GetOrderablePartitions(4);
                //verify all partitions are empty
                for (int i = 0; i < 4; i++)
                {
                    Assert.False(partitions1[i].MoveNext(), "Should not be able to move next in an empty partition.");
                }

                //test GetOrderableDynamicPartitions
                try
                {
                    var partitions2 = partitioner.GetOrderableDynamicPartitions();

                    //verify all partitions are empty
                    var newPartition = partitions2.GetEnumerator();
                    Assert.False(newPartition.MoveNext(), "Should not be able to move next in an empty partition.");
                }
                catch (NotSupportedException)
                {
                    Assert.True(IsStaticPartition(algorithm), "TestEmptyPartitions:  IsStaticPartition(algorithm) should have been true.");
                }
            }
        }

        private static void RunTestWithAlgorithm(int dataSize, int partitionCount, int algorithm)
        {
            //we set up the KeyValuePair in the way that keys and values should always be the same
            //for all partitioning algorithms. So that we can use a bitmap (boolarray) to check whether
            //any elements are missing in the end.
            int[] data = new int[dataSize];
            for (int i = 0; i < dataSize; i++)
                data[i] = i;

            IEnumerator<KeyValuePair<long, int>>[] partitionsUnderTest = new IEnumerator<KeyValuePair<long, int>>[partitionCount];

            //step 1: test GetOrderablePartitions
            OrderablePartitioner<int> partitioner = PartitioningWithAlgorithm<int>(data, algorithm);
            var partitions1 = partitioner.GetOrderablePartitions(partitionCount);

            //convert it to partition array for testing
            for (int i = 0; i < partitionCount; i++)
                partitionsUnderTest[i] = partitions1[i];

            Assert.Equal(partitionCount, partitions1.Count);

            TestPartitioningCore(dataSize, partitionCount, data, IsStaticPartition(algorithm), partitionsUnderTest);

            //step 2: test GetOrderableDynamicPartitions
            bool gotException = false;
            try
            {
                var partitions2 = partitioner.GetOrderableDynamicPartitions();
                for (int i = 0; i < partitionCount; i++)
                    partitionsUnderTest[i] = partitions2.GetEnumerator();

                TestPartitioningCore(dataSize, partitionCount, data, IsStaticPartition(algorithm), partitionsUnderTest);
            }
            catch (NotSupportedException)
            {
                //swallow this exception: static partitioning doesn't support GetOrderableDynamicPartitions
                gotException = true;
            }

            Assert.False(IsStaticPartition(algorithm) && !gotException, "TestLoadBalanceIList: Failure: didn't catch \"NotSupportedException\" for static partitioning");
        }

        private static OrderablePartitioner<T> PartitioningWithAlgorithm<T>(T[] data, int algorithm)
        {
            switch (algorithm)
            {
                //static partitioning through IList
                case (0):
                    return Partitioner.Create((IList<T>)data, false);

                //static partitioning through Array
                case (1):
                    return Partitioner.Create(data, false);

                //dynamic partitioning through IList
                case (2):
                    return Partitioner.Create((IList<T>)data, true);

                //dynamic partitioning through Array
                case (3):
                    return Partitioner.Create(data, true);

                //dynamic partitioning through IEnumerator
                case (4):
                    return Partitioner.Create((IEnumerable<T>)data);
                default:
                    throw new InvalidOperationException("PartitioningWithAlgorithm:  no such partitioning algorithm");
            }
        }

        private static void TestPartitioningCore(int dataSize, int partitionCount, int[] data, bool staticPartitioning,
            IEnumerator<KeyValuePair<long, int>>[] partitions)
        {
            bool[] boolarray = new bool[dataSize];
            bool keysOrderedWithinPartition = true,
                keysOrderedAcrossPartitions = true;
            int enumCount = 0; //count how many elements are enumerated by all partitions
            Task[] threadArray = new Task[partitionCount];

            for (int i = 0; i < partitionCount; i++)
            {
                int my_i = i;
                threadArray[i] = Task.Run(() =>
                {
                    int localOffset = 0;
                    int lastElement = -1;

                    //variables to compute key/value consistency for static partitioning.
                    int quotient, remainder;
                    quotient = dataSize / partitionCount;
                    remainder = dataSize % partitionCount;
                    Assert.Throws<InvalidOperationException>(() => { var temp = partitions[my_i].Current; });

                    while (partitions[my_i].MoveNext())
                    {
                        int key = (int)partitions[my_i].Current.Key,
                            value = partitions[my_i].Current.Value;

                        Assert.Equal(key, value);

                        boolarray[key] = true;
                        Interlocked.Increment(ref enumCount);

                        //todo: check if keys are ordered increasingly within each partition.
                        keysOrderedWithinPartition &= (lastElement >= key);
                        lastElement = key;

                        //Only check this with static partitioning
                        //check keys are ordered across the partitions 
                        if (staticPartitioning)
                        {
                            int originalPosition;
                            if (my_i < remainder)
                                originalPosition = localOffset + my_i * (quotient + 1);
                            else
                                originalPosition = localOffset + remainder * (quotient + 1) + (my_i - remainder) * quotient;
                            keysOrderedAcrossPartitions &= originalPosition == value;
                        }
                        localOffset++;
                    }
                }
                );
            }

            Task.WaitAll(threadArray);

            if (keysOrderedWithinPartition)
                Console.WriteLine("TestPartitioningCore:  Keys are not strictly ordered within each partition");

            // Only check this with static partitioning
            //check keys are ordered across the partitions 
            Assert.False(staticPartitioning && !keysOrderedAcrossPartitions, "TestPartitioningCore:  Keys are not strictly ordered across partitions");

            //check data count
            Assert.Equal(enumCount, dataSize);

            //check if any elements are missing
            foreach (var item in boolarray)
            {
                Assert.True(item);
            }
        }

        //
        // Try calling MoveNext on a Partitioner enumerator after that enumerator has already returned false.
        //
        [Fact]
        public static void TestExtraMoveNext()
        {
            Partitioner<int>[] partitioners = new[] 
            {
                Partitioner.Create(new int[] { 0 , 1, 2, 3, 4, 5}),
                Partitioner.Create(new int[] { 0 , 1, 2, 3, 4, 5}, false),
                Partitioner.Create(new int[] { 0 , 1, 2, 3, 4, 5}, true),
                Partitioner.Create(new int[] { 0 }),
                Partitioner.Create(new int[] { 0 }, false),
                Partitioner.Create(new int[] { 0 }, true),
            };

            for (int i = 0; i < partitioners.Length; i++)
            {
                using (var ee = partitioners[i].GetPartitions(1)[0])
                {
                    while (ee.MoveNext()) { }

                    Assert.False(ee.MoveNext(), "TestExtraMoveNext:  FAILED.  Partitioner " + i + ": First extra MoveNext expected to return false.");
                    Assert.False(ee.MoveNext(), "TestExtraMoveNext:  FAILED.  Partitioner " + i + ": Second extra MoveNext expected to return false.");
                    Assert.False(ee.MoveNext(), "TestExtraMoveNext:  FAILED.  Partitioner " + i + ": Third extra MoveNext expected to return false.");
                }
            }
        }

        #region Helper Methods / Classes

        private class DisposeTrackingEnumerable<T> : IEnumerable<T>
        {
            protected IEnumerable<T> m_data;
            List<DisposeTrackingEnumerator<T>> s_enumerators = new List<DisposeTrackingEnumerator<T>>();

            public DisposeTrackingEnumerable(IEnumerable<T> enumerable)
            {
                m_data = enumerable;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                DisposeTrackingEnumerator<T> walker = new DisposeTrackingEnumerator<T>(m_data.GetEnumerator());
                lock (s_enumerators)
                {
                    s_enumerators.Add(walker);
                }
                return walker;
            }

            public IEnumerator<T> GetEnumerator()
            {
                DisposeTrackingEnumerator<T> walker = new DisposeTrackingEnumerator<T>(m_data.GetEnumerator());
                lock (s_enumerators)
                {
                    s_enumerators.Add(walker);
                }
                return walker;
            }

            public void AreEnumeratorsDisposed(string scenario)
            {
                for (int i = 0; i < s_enumerators.Count; i++)
                {
                    Assert.True(s_enumerators[i].IsDisposed(), 
                        string.Format("AreEnumeratorsDisposed:  FAILED.  enumerator {0} was not disposed for SCENARIO: {1}.", i, scenario));
                }
            }
        }

        /// <summary>
        /// This is the Enumerator that DisposeTrackingEnumerable generates when GetEnumerator is called.
        /// We are simply wrapping an Enumerator and tracking whether Dispose had been called or not.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        private class DisposeTrackingEnumerator<T> : IEnumerator<T>
        {
            IEnumerator<T> m_elements;
            bool disposed;

            public DisposeTrackingEnumerator(IEnumerator<T> enumerator)
            {
                m_elements = enumerator;
                disposed = false;
            }

            public bool MoveNext()
            {
                return m_elements.MoveNext();
            }

            public T Current
            {
                get { return m_elements.Current; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return m_elements.Current; }
            }

            /// <summary>
            /// Dispose the underlying Enumerator, and suppresses finalization
            /// so that we will not throw.
            /// </summary>
            public void Dispose()
            {
                GC.SuppressFinalize(this);
                m_elements.Dispose();
                disposed = true;
            }

            public void Reset()
            {
                m_elements.Reset();
            }

            public bool IsDisposed()
            {
                return disposed;
            }
        }

        private static bool IsStaticPartition(int algorithm)
        {
            return algorithm < 2;
        }

        #endregion
    }
}
