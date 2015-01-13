// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class PartitionerStaticTests
    {
        [Fact]
        public static void RunPartitionerStaticTest_StaticPartitioningIList()
        {
            RunPartitionerStaticTest_StaticPartitioningIList(11, 8);
            RunPartitionerStaticTest_StaticPartitioningIList(8, 11);
            RunPartitionerStaticTest_StaticPartitioningIList(10, 10);
            RunPartitionerStaticTest_StaticPartitioningIList(10000, 1);
            RunPartitionerStaticTest_StaticPartitioningIList(10000, 4);
            RunPartitionerStaticTest_StaticPartitioningIList(10000, 357);
        }

        [Fact]
        public static void RunPartitionerStaticTest_StaticPartitioningArray()
        {
            RunPartitionerStaticTest_StaticPartitioningArray(11, 8);
            RunPartitionerStaticTest_StaticPartitioningArray(8, 11);
            RunPartitionerStaticTest_StaticPartitioningArray(10, 10);
            RunPartitionerStaticTest_StaticPartitioningArray(10000, 1);
            RunPartitionerStaticTest_StaticPartitioningArray(10000, 4);
            RunPartitionerStaticTest_StaticPartitioningArray(10000, 357);
        }

        [Fact]
        public static void RunPartitionerStaticTest_LoadBalanceIList()
        {
            RunPartitionerStaticTest_LoadBalanceIList(11, 8);
            RunPartitionerStaticTest_LoadBalanceIList(8, 11);
            RunPartitionerStaticTest_LoadBalanceIList(11, 11);
            RunPartitionerStaticTest_LoadBalanceIList(10000, 1);
            RunPartitionerStaticTest_LoadBalanceIList(10000, 4);
            RunPartitionerStaticTest_LoadBalanceIList(10000, 23);
        }

        [Fact]
        public static void RunPartitionerStaticTest_LoadBalanceArray()
        {
            RunPartitionerStaticTest_LoadBalanceArray(11, 8);
            RunPartitionerStaticTest_LoadBalanceArray(8, 11);
            RunPartitionerStaticTest_LoadBalanceArray(11, 11);
            RunPartitionerStaticTest_LoadBalanceArray(10000, 1);
            RunPartitionerStaticTest_LoadBalanceArray(10000, 4);
            RunPartitionerStaticTest_LoadBalanceArray(10000, 23);
        }

        [Fact]
        public static void RunPartitionerStaticTest_LoadBalanceEnumerator()
        {
            RunPartitionerStaticTest_LoadBalanceEnumerator(11, 8);
            RunPartitionerStaticTest_LoadBalanceEnumerator(8, 11);
            RunPartitionerStaticTest_LoadBalanceEnumerator(10, 10);
            RunPartitionerStaticTest_LoadBalanceEnumerator(10000, 1);
            RunPartitionerStaticTest_LoadBalanceEnumerator(10000, 4);
            RunPartitionerStaticTest_LoadBalanceEnumerator(10000, 37);
        }

        #region Dispose tests. The dispose logic of PartitionerStatic

        // In the official dev unit test run, this test should be commented out
        // - Each time we call GetDynamicPartitions method, we create an internal "reader enumerator" to read the 
        // source data, and we need to make sure that whenever the object returned by GetDynmaicPartitions is disposed,
        // the "reader enumerator" is also disposed.
        [Fact]
        public static void RunPartitionerStaticTest_DisposeException()
        {
            var data = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var enumerable = new DisposeTrackingEnumerable<int>(data);
            var partitioner = Partitioner.Create(enumerable);
            var partition = partitioner.GetDynamicPartitions();
            IDisposable d = partition as IDisposable;
            if (d == null)
            {
                Assert.False(true, "RunPartitionerStaticTest_DisposeException: failed casting to IDisposable");
            }
            else
            {
                d.Dispose();
            }

            try
            {
                var enum1 = partition.GetEnumerator();
                Assert.False(true, "RunPartitionerStaticTest_DisposeException: failed. Expecting ObjectDisposedException to be thrown");
            }
            catch (ObjectDisposedException)
            { }
        }

        /// <summary>
        /// BugFix 835284: Race in Partitioner's dynamic partitioning Dispose logic
        /// After the fix, the partitioner created through Partitioner.Create(IEnumerable) has the following behavior:
        ///     1. reference counting in static partitioning. All partitions need to be disposed explicitly
        ///     2. no reference counting in dynamic partitioning. The partitioner need to be disposed explicity
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

            try
            {
                using (var e = d.GetEnumerator()) { }
            }
            catch (System.ObjectDisposedException)
            {
                Assert.False(true, "RunDynamicPartitioningDispose:  FAILED! ObjectDisposedException thrown by test");
            }
        }


        #endregion


        [Fact]
        public static void RunPartitionerStaticTest_Exceptions()
        {
            // Testing ArgumentNullException with data==null
            bool gotException;
            // Test ArgumentNullException of source data
            OrderablePartitioner<int> partitioner;
            for (int algorithm = 0; algorithm < 5; algorithm++)
            {
                gotException = false;
                try
                {
                    partitioner = PartitioningWithAlgorithm<int>(null, algorithm);
                }
                catch (ArgumentNullException)
                {
                    gotException = true;
                }
                if (!gotException)
                {
                    Assert.False(true, String.Format(
                        "RunPartitionerStaticTest_Exceptions: Failure in partitioning algorithm {0}, didn't catch ArgumentNullException", algorithm));
                }
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
                gotException = false;
                try
                {
                    var partitions1 = partitioner.GetOrderablePartitions(0);
                }
                catch (ArgumentOutOfRangeException)
                {
                    gotException = true;
                }
                if (!gotException)
                {
                    Assert.False(true, String.Format(
                        "RunPartitionerStaticTest_Exceptions:  Failure in GetOrderablePartitions of algorithm {0}, didn't catch ArgumentOutOfRangeException", algorithm));
                }
            }
        }

        [Fact]
        public static void RunPartitionerStaticTest_EmptyPartitions()
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
                    if (!IsStaticPartition(algorithm))
                    {
                        Assert.False(true, "RunPartitionerStaticTest_EmptyPartitions:  IsStaticPartition(algorithm) should have been true.");
                    }
                }
            }
        }

        private static void RunPartitionerStaticTest_StaticPartitioningIList(int dataSize, int partitionCount)
        {
            RunTestWithAlgorithm(dataSize, partitionCount, 0);
        }

        private static void RunPartitionerStaticTest_StaticPartitioningArray(int dataSize, int partitionCount)
        {
            RunTestWithAlgorithm(dataSize, partitionCount, 1);
        }

        private static void RunPartitionerStaticTest_LoadBalanceIList(int dataSize, int partitionCount)
        {
            RunTestWithAlgorithm(dataSize, partitionCount, 2);
        }

        private static void RunPartitionerStaticTest_LoadBalanceArray(int dataSize, int partitionCount)
        {
            RunTestWithAlgorithm(dataSize, partitionCount, 3);
        }

        private static void RunPartitionerStaticTest_LoadBalanceEnumerator(int dataSize, int partitionCount)
        {
            RunTestWithAlgorithm(dataSize, partitionCount, 4);
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

            if (partitions1.Count != partitionCount)
            {
                Assert.False(true, String.Format(
                    "RunPartitionerStaticTest_LoadBalanceIList:  FAILED.  partitions1.count: {0} != partitioncount: {1}", partitions1.Count, partitionCount));
            }

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

            if (IsStaticPartition(algorithm) && !gotException)
            {
                Assert.False(true, "RunPartitionerStaticTest_LoadBalanceIList: Failure: didn't catch \"NotSupportedException\" for static partitioning");
            }
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

                //dynamic partitioning through Arrray
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
                threadArray[i] = new Task(() =>
                {
                    int localOffset = 0;
                    int lastElement = -1;

                    //variables to compute key/value consistency for static partitioning.
                    int quotient, remainder;
                    quotient = dataSize / partitionCount;
                    remainder = dataSize % partitionCount;

                    bool gotException = false;
                    //call Current before MoveNext, should throw an exception
                    try
                    {
                        var temp = partitions[my_i].Current;
                    }
                    catch (InvalidOperationException)
                    {
                        gotException = true;
                    }
                    if (!gotException)
                    {
                        Assert.False(true, "TestPartitioningCore:  Failure: didn't catch the InvalidOperationException when call Current before MoveNext");
                    }

                    while (partitions[my_i].MoveNext())
                    {
                        int key = (int)partitions[my_i].Current.Key,
                            value = partitions[my_i].Current.Value;

                        if (key != value)
                        {
                            Assert.False(true, String.Format("TestPartitioningCore:  TestPartitioningCore: FAILED.  key {0} does not equal value {1}", key, value));
                        }
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
                threadArray[i].Start();
            }

            for (int i = 0; i < threadArray.Length; i++)
            {
                threadArray[i].Wait();
            }

            if (keysOrderedWithinPartition)
                Console.WriteLine("TestPartitioningCore:  Keys are not strictly ordered within each partition");


            // Only check this with static partitioning
            //check keys are ordered across the partitions 
            if (staticPartitioning && !keysOrderedAcrossPartitions)
            {
                Assert.False(true, "TestPartitioningCore:  Keys are not strictly ordered across partitions");
            }

            //check data count
            if (enumCount != dataSize)
            {
                Assert.False(true, String.Format("TestPartitioningCore:  inconsistent count, requested {0}, added {1}", dataSize, enumCount));
            }

            //check if any elements are missing
            foreach (var item in boolarray)
            {
                if (!item)
                {
                    Assert.False(true, "TestPartitioningCore:  inconsistent data: some elements are missing");
                }
            }
        }

        //
        // Try calling MoveNext on a Partitioner enumerator after that enumerator has already returned false.
        //
        [Fact]
        public static void RunPartitionerStaticTest_ExtraMoveNext()
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
                try
                {
                    using (var ee = partitioners[i].GetPartitions(1)[0])
                    {
                        while (ee.MoveNext()) { }

                        Assert.False(ee.MoveNext(), "RunPartitionerStaticTest_ExtraMoveNext:  FAILED.  Partitioner " + i + ": First extra MoveNext expected to return false.");
                        Assert.False(ee.MoveNext(), "RunPartitionerStaticTest_ExtraMoveNext:  FAILED.  Partitioner " + i + ": Second extra MoveNext expected to return false.");
                        Assert.False(ee.MoveNext(), "RunPartitionerStaticTest_ExtraMoveNext:  FAILED.  Partitioner " + i + ": Third extra MoveNext expected to return false.");
                    }
                }
                catch (Exception ex)
                {
                    Assert.False(true, "RunPartitionerStaticTest_ExtraMoveNext:  FAILURE.  Partitioner " + i + " threw an exception: " + ex.ToString());
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
                    if (!s_enumerators[i].IsDisposed())
                    {
                        Assert.False(true, String.Format(
                            "PartitionerStaticTests - AreEnumeratorsDisposed:  FAILED.  enumerator {0} was not disposed for SCENARIO: {1}.", i, scenario));
                    }
                }
            }
        }

        /// <summary>
        /// This is the Enumerator that DisposeTtracking Enumerable generates when GetEnumerator is called.
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

            public Boolean MoveNext()
            {
                return m_elements.MoveNext();
            }

            public T Current
            {
                get { return m_elements.Current; }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return m_elements.Current; }
            }

            /// <summary>
            /// Dispose the underlying Enumerator, and supresses finalization
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
