// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Partitioner1Chunk.cs
//
//
// Contains tests for testing the Partitioner1Chunk new Dev11 feature.
// In this partitioner the chunk size is always 1
//
// The included scenarios are:
//  1. Partitioner Correctness:
//          - Chunk is one
//          - ParallelForEach support iteration dependencies 
//  2. Enumerators are disposed in ParallelForEach usage
//  3. Negative tests.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class Partitioner1Chunk
    {
        /// <summary>
        /// Test the fact that every call of the get*DynamicPartitions.GetEnumerator().MoveNext 
        /// results in only one call of the datasource.GetEnumerator().MoveNext
        /// 
        /// the default chunking algorithm use 2^n chunks. Use these values as the test input data.
        /// </summary>
        /// <param name="length">the data source length</param>
        /// <param name="isOrderable">if OrderablePartitions are used or not</param>
        [Fact]
        public static void OneMoveNext()
        {
            int[] lengthsArray = new[] { 1, 8, 16, 32, 64, 1024 };
            bool[] isOrderableArray = new[] { true, false };

            foreach (var length in lengthsArray)
            {
                foreach (var order in isOrderableArray)
                    OneMoveNext(length, order);
            }
        }
        private static void OneMoveNext(int length, bool isOrderable)
        {
            Debug.WriteLine("Length: {0} IsOrderable: {1}", length, isOrderable);
            List<int> ds = new List<int>();
            for (int i = 0; i < length; i++)
                ds.Add(i);
            int dataSourceMoveNextCalls = 0;

            //this is an enumerable that will execute user actions on move next, current and dispose
            //in this case we will set it to wait on MoveNext for the even indexes
            UserActionEnumerable<int> customEnumerable = new UserActionEnumerable<int>(ds);
            Action<int> moveNextUserAction = (currentElement) =>
            {
                //keep track how many times the move next of the data source was called
                //it is expected as 
                //every call of MoveNext on partitioner>GetDynamicPartions.GetEnumerator 
                //to result in only one call of datasource Move Next
                //there is not need to guard for concurrency issues because this scenario is single threaded
                dataSourceMoveNextCalls++;
            };

            customEnumerable.MoveNextAction = moveNextUserAction;

            var partitioner = Partitioner.Create<int>(customEnumerable, EnumerablePartitionerOptions.NoBuffering);
            //get the dynamic partitions - enumerator
            if (isOrderable)
            {
                IEnumerator<KeyValuePair<long, int>> enumerator = partitioner.GetOrderableDynamicPartitions().GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Assert.Equal(dataSourceMoveNextCalls, 1);
                    //reset the count - for the next moveNext call 
                    dataSourceMoveNextCalls = 0;
                }
            }
            else
            {
                IEnumerator<int> enumerator = partitioner.GetDynamicPartitions().GetEnumerator();

                while (enumerator.MoveNext())
                {
                    Assert.Equal(dataSourceMoveNextCalls, 1);
                    //reset the count - for the next moveNext call 
                    dataSourceMoveNextCalls = 0;
                }
            }
        }

        /// <summary>
        /// Test that in a parallel Foreach loop can be dependencies between iterations if a partitioner of chunk size 1 is used
        /// </summary>
        /// <param name="length"></param>
        [Fact]
        public static void IterationsWithDependency()
        {
            IterationsWithDependency(128, 126);
            IterationsWithDependency(128, 65);
        }
        private static void IterationsWithDependency(int length, int dependencyIndex)
        {
            List<int> ds = new List<int>();
            for (int i = 0; i < length; i++)
                ds.Add(i);
            var partitioner = Partitioner.Create<int>(ds, EnumerablePartitionerOptions.NoBuffering);
            ManualResetEvent mre = new ManualResetEvent(false);
            ConcurrentQueue<int> savedDS = new ConcurrentQueue<int>();

            Parallel.ForEach(partitioner, (index) =>
                {
                    if (index == dependencyIndex + 1)
                    {
                        mre.Set();
                    }
                    if (index == dependencyIndex)
                    {
                        //if the chunk size will not be one, 
                        //this iteration and the next one will not be processed by the same thread
                        //waiting here will lead to a deadlock
                        mre.WaitOne();
                    }
                    savedDS.Enqueue(index);
                });
            //if the PForEach ends this means pass
            //verify the collection
            Assert.True(CompareCollections(savedDS, ds));
        }

        /// <summary>
        /// Verify that the enumerators used while executing the ParalleForEach over the partitioner are disposed
        /// </summary>

        [Fact]
        public static void PFEDisposeEnum()
        {
            PFEDisposeEnum(1204);
        }
        private static void PFEDisposeEnum(int length)
        {
            List<int> ds = new List<int>();
            for (int i = 0; i < length; i++)
                ds.Add(i);
            //this is an enumerable that will execute user actions on move next, current and dispose
            //in this case we will set it to wait on MoveNext for the even indexes
            UserActionEnumerable<int> customEnumerable = new UserActionEnumerable<int>(ds);
            ConcurrentQueue<int> savedDS = new ConcurrentQueue<int>();
            var partitioner = Partitioner.Create<int>(customEnumerable, EnumerablePartitionerOptions.NoBuffering);
            Parallel.ForEach(partitioner, (index) =>
                {
                    savedDS.Enqueue(index);
                });
            Assert.True(customEnumerable.AreEnumeratorsDisposed());
            Assert.True(CompareCollections(savedDS, ds));
        }

        /// <summary>
        /// Negative test:
        /// Move Next throws
        /// Partitioner is used in ParallelForEach
        /// Exception is expected and the enumerators are disposed 
        /// </summary>
        [Fact]
        public static void ExceptionOnMoveNext()
        {
            ExceptionOnMoveNext(128, 65, true);
            ExceptionOnMoveNext(128, 65, false);
        }
        private static void ExceptionOnMoveNext(int length, int indexToThrow, bool isOrderable)
        {
            List<int> ds = new List<int>();
            for (int i = 0; i < length; i++)
                ds.Add(i);

            Exception userEx = new InvalidOperationException("UserException");
            //this is an enumerable that will execute user actions on move next, current and dispose
            //in this case we will set it to throw on MoveNext for specified index
            UserActionEnumerable<int> customEnumerable = new UserActionEnumerable<int>(ds);
            Action<int> moveNextUserAction = (currentElement) =>
                                                            {
                                                                if (currentElement == indexToThrow)
                                                                {
                                                                    throw userEx;
                                                                };
                                                            };


            customEnumerable.MoveNextAction = moveNextUserAction;
            var partitioner = Partitioner.Create<int>(customEnumerable, EnumerablePartitionerOptions.NoBuffering);
            var exception = Assert.Throws<AggregateException>(() => Parallel.ForEach(partitioner, (index) => { }));
            VerifyAggregateException(exception, userEx);
            Assert.True(customEnumerable.AreEnumeratorsDisposed());
        }

        /// <summary>
        /// Use an incorrect buffering value for the EnumerablePartitionerOptions
        /// </summary>
        [Fact]
        public static void IncorrectBuffering()
        {
            int length = 16;
            int[] ds = new int[length];
            for (int i = 0; i < 16; i++)
                ds[i] = i;
            Assert.Throws<ArgumentOutOfRangeException>(() => { var partitioner = Partitioner.Create<int>(ds, (EnumerablePartitionerOptions)0x2); });
        }

        /// <summary>
        /// Use null data source
        /// </summary>
        [Fact]
        public static void NullDataSource()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var partitioner = Partitioner.Create<int>(null, EnumerablePartitionerOptions.NoBuffering);
            });
        }

        #region Helper Methods

        /// <summary>
        /// Compare the two collections
        /// </summary>
        /// <param name="savedDS">concurrent queue used for saving the consumed data</param>
        /// <param name="ds">an IEnumerable data source</param>
        /// <returns></returns>
        private static bool CompareCollections(ConcurrentQueue<int> savedDS, IEnumerable<int> ds)
        {
            List<int> dsList = new List<int>(savedDS);
            dsList.Sort();
            List<int> expected = new List<int>(ds);
            expected.Sort();

            if (expected.Count != dsList.Count)
                return false;

            for (int i = 0; i < expected.Count; i++)
            {
                int actual = dsList[i];
                int exp = expected[i];
                if (!actual.Equals(exp))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// return the elements from the collection in order; as a string
        /// </summary>
        /// <param name="savedDS"></param>
        /// <returns></returns>
        private static string Print(ConcurrentQueue<int> savedDS)
        {
            List<int> dsList = new List<int>(savedDS);
            dsList.Sort();
            return string.Join(",", dsList);
        }

        /// <summary>
        /// Verifies if an aggregate exception contains a specific user exception
        /// </summary>
        /// <param name="aggregatEx"></param>
        /// <param name="userException"></param>
        private static void VerifyAggregateException(AggregateException aggregatEx, Exception userException)
        {
            Assert.True(aggregatEx.InnerExceptions.Contains(userException));
            Assert.Equal(aggregatEx.Flatten().InnerExceptions.Count, 1);
        }

        #endregion
    }

    /// <summary>
    /// an IEnumerable whose enumerator can be configured to execute user code from.
    /// - MoveNext
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UserActionEnumerable<T> : IEnumerable<T>
    {
        protected List<T> _data;

        //keeps track of how many enumerators are created
        //in case of an exception in parallel foreach
        //the enumerators should be disposed
        private ConcurrentBag<UserActionEnumerator<T>> _allEnumerators = new ConcurrentBag<UserActionEnumerator<T>>();

        //called in the beginning of enumerator Move Next 
        private Action<int> _moveNextAction = null;

        public UserActionEnumerable(List<T> enumerable, Action<int> moveNextAction)
        {
            _data = enumerable;
            _moveNextAction = moveNextAction;
        }

        public UserActionEnumerable(List<T> enumerable)
        {
            _data = enumerable;
        }

        /// <summary>
        /// User action for MoveNext
        /// </summary>
        public Action<int> MoveNextAction
        {
            set
            {
                _moveNextAction = value;
            }
        }


        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (System.Collections.IEnumerator)this.GetEnumerator(); ;
        }

        public IEnumerator<T> GetEnumerator()
        {
            UserActionEnumerator<T> en = new UserActionEnumerator<T>(_data, _moveNextAction);
            _allEnumerators.Add(en);

            return en;
        }


        /// <summary>
        /// verifies if all the enumerators are disposed
        /// </summary>
        /// <returns></returns>
        public bool AreEnumeratorsDisposed()
        {
            foreach (UserActionEnumerator<T> en in _allEnumerators)
            {
                if (!en.IsDisposed())
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Enumerator used by the UserActionEnumerable class
    /// </summary>
    /// <typeparam name="T">The type of the element</typeparam>
    public class UserActionEnumerator<T> : IEnumerator<T>
    {
        private List<T> _data;
        private volatile int _positionCurrent = -1;
        private bool _disposed;
        private object _lock = new object();
        private int _length = 0;

        //called in enumerator's MoveNext 
        private Action<int> _moveNextAction = null;

        internal UserActionEnumerator(List<T> data, Action<int> moveNextAction)
        {
            _data = data;
            _disposed = false;
            _length = data.Count;
            _moveNextAction = moveNextAction;
        }

        /// <summary>
        /// MoveNext - 
        /// the move next is performed under lock in order to avoid race condition with the Current 
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            bool result = false;

            lock (_lock)
            {
                _positionCurrent++;
                result = _positionCurrent < _length;
            }
            if (_moveNextAction != null && result) _moveNextAction(_positionCurrent);

            return result;
        }

        /// <summary>
        /// current under lock
        /// </summary>
        public T Current
        {
            get
            {
                lock (_lock)
                {
                    return _data[_positionCurrent];
                }
            }
        }

        Object System.Collections.IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        /// <summary>
        /// Dispose the underlying Enumerator, and suppresses finalization
        /// so that we will not throw.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        public void Reset()
        {
            throw new System.NotImplementedException("Reset not implemented");
        }

        public bool IsDisposed()
        {
            return _disposed;
        }
    }
}
