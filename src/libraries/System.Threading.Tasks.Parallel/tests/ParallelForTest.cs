// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ParallelForTest.cs
//
// This file contains functional tests for Parallel.For and Parallel.ForEach
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=--=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public sealed class ParallelForTest
    {
        #region Private Fields

        private readonly TestParameters _parameters;

        private IList<int> _collection = null;  // the collection used in Foreach

        private readonly double[] _results;  // global place to store the workload result for verification

        // data structure used with ParallelLoopState<TLocal>
        // each row is the sequence of loop "index" finished in the same thread 
        private int _threadCount;
        private readonly List<int>[] _sequences;  // @TODO: remove if ConcurrentDictionary can be used

        private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random(unchecked((int)(DateTime.Now.Ticks))));  // Random generator for WorkloadPattern == Random

        private static readonly int s_zetaSeedOffset = 10000;  // offset to the zeta seed to ensure result converge to the expected

        private OrderablePartitioner<int> _partitioner = null;
        private OrderablePartitioner<Tuple<int, int>> _rangePartitioner = null;
        private ParallelOptions _parallelOption;

        #endregion

        public ParallelForTest(TestParameters parameters)
        {
            _parameters = parameters;

            _results = new double[parameters.Count];

            if (parameters.LocalOption != ActionWithLocal.None)
            {
                _sequences = new List<int>[1024];
                _threadCount = 0;
            }
        }

        #region Test Methods

        internal void RealRun()
        {
            if (_parameters.Api == API.For64)
                RunParallelFor64Test();
            else if (_parameters.Api == API.For)
                RunParallelForTest();
            else
                RunParallelForeachTest();

            // verify result
            for (int i = 0; i < _parameters.Count; i++)
                Verify(i);

            // verify unique  index sequences if run WithLocal
            if (_parameters.LocalOption != ActionWithLocal.None)
                VerifySequences();
        }

        // Tests Parallel.For version that takes 'long' from and to parameters
        private void RunParallelFor64Test()
        {
            if (_parameters.ParallelOption != WithParallelOption.None)
            {
                ParallelOptions option = GetParallelOptions();

                if (_parameters.LocalOption == ActionWithLocal.None)
                {
                    if (_parameters.StateOption == ActionWithState.None)
                    {
                        // call Parallel.For with ParallelOptions
                        Parallel.For(_parameters.StartIndex64, _parameters.StartIndex64 + _parameters.Count, option, Work);
                    }
                    else if (_parameters.StateOption == ActionWithState.Stop)
                    {
                        // call Parallel.For with ParallelLoopState and ParallelOptions
                        Parallel.For(_parameters.StartIndex64, _parameters.StartIndex64 + _parameters.Count, option, WorkWithStop);
                    }
                }
                else if (_parameters.LocalOption == ActionWithLocal.HasFinally)
                {
                    // call Parallel.For with ParallelLoopState<TLocal>, plus threadLocalFinally, plus ParallelOptions
                    Parallel.For<List<int>>(_parameters.StartIndex64, _parameters.StartIndex64 + _parameters.Count, option, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
                }
            }
            else
            {
                if (_parameters.LocalOption == ActionWithLocal.None)
                {
                    if (_parameters.StateOption == ActionWithState.None)
                    {
                        // call Parallel.For 
                        Parallel.For(_parameters.StartIndex64, _parameters.StartIndex64 + _parameters.Count, Work);
                    }
                    else if (_parameters.StateOption == ActionWithState.Stop)
                    {
                        // call Parallel.For with ParallelLoopState 
                        Parallel.For(_parameters.StartIndex64, _parameters.StartIndex64 + _parameters.Count, WorkWithStop);
                    }
                }
                else if (_parameters.LocalOption == ActionWithLocal.HasFinally)
                {
                    // call Parallel.For with ParallelLoopState<TLocal>, plus threadLocalFinally
                    Parallel.For<List<int>>(_parameters.StartIndex64, _parameters.StartIndex64 + _parameters.Count, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
                }
            }
        }

        // Tests Parallel.ForEach
        private void RunParallelForeachTest()
        {
            int length = _parameters.Count;
            if (length < 0)
                length = 0;

            int[] arrayCollection = new int[length];
            for (int i = 0; i < length; i++)
                arrayCollection[i] = _parameters.StartIndex + i;

            if (_parameters.Api == API.ForeachOnArray)
                _collection = arrayCollection;
            else if (_parameters.Api == API.ForeachOnList)
                _collection = new List<int>(arrayCollection);
            else
                _collection = arrayCollection;

            //if source is partitioner
            if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                _rangePartitioner = PartitionerFactory<Tuple<int, int>>.Create(_parameters.PartitionerType, _parameters.StartIndex, _parameters.StartIndex + _parameters.Count, _parameters.ChunkSize);
            else
                _partitioner = PartitionerFactory<int>.Create(_parameters.PartitionerType, _collection);

            if (_parameters.ParallelOption != WithParallelOption.None)
            {
                _parallelOption = GetParallelOptions();

                if (_parameters.LocalOption == ActionWithLocal.None)
                {
                    if (_parameters.StateOption == ActionWithState.None)
                    {
                        // call Parallel.Foreach with ParallelOptions
                        ParallelForEachWithOptions();
                    }
                    else if (_parameters.StateOption == ActionWithState.Stop)
                    {
                        // call Parallel.Foreach with ParallelLoopState and ParallelOptions
                        if (_parameters.Api == API.Foreach)
                            ParallelForEachWithOptionsAndState();
                        else // call indexed version for array / list overloads - to avoid calling too many combinations
                            ParallelForEachWithOptionsAndIndexAndState();
                    }
                }
                else if (_parameters.LocalOption == ActionWithLocal.HasFinally)
                {
                    // call Parallel.Foreach and ParallelLoopState<TLocal>, plus threadLocalFinally, plus ParallelOptions
                    if (_parameters.Api == API.Foreach)
                        ParallelForEachWithOptionsAndLocal();
                    else // call indexed version for array / list overloads - to avoid calling too many combinations
                        ParallelForEachWithOptionsAndLocalAndIndex();
                }
            }
            else
            {
                if (_parameters.LocalOption == ActionWithLocal.None)
                {
                    if (_parameters.StateOption == ActionWithState.None)
                    {
                        // call Parallel.Foreach
                        ParallelForEach();
                    }
                    else if (_parameters.StateOption == ActionWithState.Stop)
                    {
                        // call Parallel.Foreach with ParallelLoopState
                        if (_parameters.Api == API.Foreach)
                            ParallelForEachWithState();
                        else // call indexed version for array / list overloads - to avoid calling too many combinations
                            ParallelForEachWithIndexAndState();
                    }
                }
                else if (_parameters.LocalOption == ActionWithLocal.HasFinally)
                {
                    // call Parallel.Foreach and ParallelLoopState<TLocal>, plus threadLocalFinally
                    if (_parameters.Api == API.Foreach)
                        ParallelForeachWithLocal();
                    else // call indexed version for array / list overloads - to avoid calling too many combinations
                        ParallelForeachWithLocalAndIndex();
                }
            }
        }

        // Tests Parallel.For version that takes 'int' from and to parameters
        private void RunParallelForTest()
        {
            if (_parameters.ParallelOption != WithParallelOption.None)
            {
                ParallelOptions option = GetParallelOptions();

                if (_parameters.LocalOption == ActionWithLocal.None)
                {
                    if (_parameters.StateOption == ActionWithState.None)
                    {
                        // call Parallel.For with ParallelOptions
                        Parallel.For(_parameters.StartIndex, _parameters.StartIndex + _parameters.Count, option, Work);
                    }
                    else if (_parameters.StateOption == ActionWithState.Stop)
                    {
                        // call Parallel.For with ParallelLoopState and ParallelOptions
                        Parallel.For(_parameters.StartIndex, _parameters.StartIndex + _parameters.Count, option, WorkWithStop);
                    }
                }
                else if (_parameters.LocalOption == ActionWithLocal.HasFinally)
                {
                    // call Parallel.For with ParallelLoopState<TLocal>, plus threadLocalFinally, plus ParallelOptions
                    Parallel.For<List<int>>(_parameters.StartIndex, _parameters.StartIndex + _parameters.Count, option, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
                }
            }
            else
            {
                if (_parameters.LocalOption == ActionWithLocal.None)
                {
                    if (_parameters.StateOption == ActionWithState.None)
                    {
                        // call Parallel.For 
                        Parallel.For(_parameters.StartIndex, _parameters.StartIndex + _parameters.Count, Work);
                    }
                    else if (_parameters.StateOption == ActionWithState.Stop)
                    {
                        // call Parallel.For with ParallelLoopState
                        Parallel.For(_parameters.StartIndex, _parameters.StartIndex + _parameters.Count, WorkWithStop);
                    }
                }
                else if (_parameters.LocalOption == ActionWithLocal.HasFinally)
                {
                    // call Parallel.For with ParallelLoopState<TLocal>, plus threadLocalFinally
                    Parallel.For<List<int>>(_parameters.StartIndex, _parameters.StartIndex + _parameters.Count, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
                }
            }
        }

        #endregion

        #region ParallelForeach Overloads - with partitioner and without

        /// <summary>
        /// ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source,Action<TSource> body)
        /// </summary>
        private void ParallelForEach()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>>(_rangePartitioner, Work);
                else
                    Parallel.ForEach<int>(_partitioner, Work);
            }
            else
            {
                Parallel.ForEach<int>(_collection, Work);
            }
        }

        /// <summary>
        /// ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source,Action<T, ParallelLoopState> body)
        /// </summary>
        private void ParallelForEachWithState()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>>(_rangePartitioner, WorkWithStop);
                else
                    Parallel.ForEach<int>(_partitioner, WorkWithStop);
            }
            else
            {
                Parallel.ForEach<int>(_collection, WorkWithStop);
            }
        }

        /// <summary>
        /// ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source,Action<TSource, ParallelLoopState, Int64> body)
        /// </summary>
        private void ParallelForEachWithIndexAndState()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>>(_rangePartitioner, WorkWithIndexAndStopPartitioner);
                else
                    Parallel.ForEach<int>(_partitioner, WorkWithIndexAndStopPartitioner);
            }
            else
            {
                Parallel.ForEach<int>(_collection, WorkWithIndexAndStop);
            }
        }

        /// <summary>
        /// public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source,
        ///                                                             Func<TLocal> threadLocalInitlocalInit,
        ///                                                             Func<TSource, ParallelLoopState, TLocal, TLocal> body,
        ///                                                             Action<TLocal> threadLocalFinallylocalFinally)
        /// </summary>
        private void ParallelForeachWithLocal()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>, List<int>>(_rangePartitioner, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
                else
                    Parallel.ForEach<int, List<int>>(_partitioner, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
            }
            else
            {
                Parallel.ForEach<int, List<int>>(_collection, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
            }
        }

        /// <summary>
        /// public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source,
        ///                                                             Func<TLocal> threadLocalInitlocalInit,
        ///                                                             Func<TSource, ParallelLoopState, Int64, TLocal, TLocal> body,
        ///                                                             Action<TLocal> threadLocalFinallylocalFinally)
        /// </summary>
        private void ParallelForeachWithLocalAndIndex()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>, List<int>>(_rangePartitioner, ThreadLocalInit, WorkWithLocalAndIndexPartitioner, ThreadLocalFinally);
                else
                    Parallel.ForEach<int, List<int>>(_partitioner, ThreadLocalInit, WorkWithLocalAndIndexPartitioner, ThreadLocalFinally);
            }
            else
            {
                Parallel.ForEach<int, List<int>>(_collection, ThreadLocalInit, WorkWithIndexAndLocal, ThreadLocalFinally);
            }
        }

        /// <summary>
        /// ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source,ParallelOptions parallelOptions, Action<TSource> body)
        /// </summary>
        private void ParallelForEachWithOptions()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>>(_rangePartitioner, _parallelOption, Work);
                else
                    Parallel.ForEach<int>(_partitioner, _parallelOption, Work);
            }
            else
            {
                Parallel.ForEach<int>(_collection, _parallelOption, Work);
            }
        }

        /// <summary>
        /// ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<T, ParallelLoopState> body)
        /// </summary>
        private void ParallelForEachWithOptionsAndState()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>>(_rangePartitioner, _parallelOption, WorkWithStop);
                else
                    Parallel.ForEach<int>(_partitioner, _parallelOption, WorkWithStop);
            }
            else
            {
                Parallel.ForEach<int>(_collection, _parallelOption, WorkWithStop);
            }
        }

        /// <summary>
        /// ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions,, Action<TSource, ParallelLoopState, Int64> body)
        /// </summary>
        private void ParallelForEachWithOptionsAndIndexAndState()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>>(_rangePartitioner, _parallelOption, WorkWithIndexAndStopPartitioner);
                else
                    Parallel.ForEach<int>(_partitioner, _parallelOption, WorkWithIndexAndStopPartitioner);
            }
            else
            {
                Parallel.ForEach<int>(_collection, _parallelOption, WorkWithIndexAndStop);
            }
        }

        /// <summary>
        /// public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source,
        ///                                                             ParallelOptions parallelOptions,
        ///                                                             Func<TLocal> threadLocalInitlocalInit,
        ///                                                             Func<TSource, ParallelLoopState, TLocal, TLocal> body,
        ///                                                             Action<TLocal> threadLocalFinallylocalFinally)
        /// </summary>
        private void ParallelForEachWithOptionsAndLocal()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>, List<int>>(_rangePartitioner, _parallelOption, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
                else
                    Parallel.ForEach<int, List<int>>(_partitioner, _parallelOption, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
            }
            else
            {
                Parallel.ForEach<int, List<int>>(_collection, _parallelOption, ThreadLocalInit, WorkWithLocal, ThreadLocalFinally);
            }
        }

        /// <summary>
        /// public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source,
        ///                                                             ParallelOptions parallelOptions,
        ///                                                             Func<TLocal> threadLocalInitlocalInit,
        ///                                                             Func<TSource, ParallelLoopState, Int64, TLocal, TLocal> body,
        ///                                                             Action<TLocal> threadLocalFinallylocalFinally)
        /// </summary>
        private void ParallelForEachWithOptionsAndLocalAndIndex()
        {
            if (_parameters.ParallelForeachDataSourceType == DataSourceType.Partitioner)
            {
                if (_parameters.PartitionerType == PartitionerType.RangePartitioner)
                    Parallel.ForEach<Tuple<int, int>, List<int>>(_rangePartitioner, _parallelOption, ThreadLocalInit, WorkWithLocalAndIndexPartitioner, ThreadLocalFinally);
                else
                    Parallel.ForEach<int, List<int>>(_partitioner, _parallelOption, ThreadLocalInit, WorkWithLocalAndIndexPartitioner, ThreadLocalFinally);
            }
            else
            {
                Parallel.ForEach<int, List<int>>(_collection, _parallelOption, ThreadLocalInit, WorkWithIndexAndLocal, ThreadLocalFinally);
            }
        }

        #endregion

        #region Workloads

        private void InvokeZetaWorkload(int i)
        {
            if (_results[i] == 0)
            {
                int zetaIndex = s_zetaSeedOffset;
                switch (_parameters.WorkloadPattern)
                {
                    case WorkloadPattern.Similar:
                        zetaIndex += i;
                        break;

                    case WorkloadPattern.Increasing:
                        zetaIndex += i * s_zetaSeedOffset;
                        break;

                    case WorkloadPattern.Decreasing:
                        zetaIndex += (_parameters.Count - i) * s_zetaSeedOffset;
                        break;

                    case WorkloadPattern.Random:
                        zetaIndex += _random.Value.Next(0, _parameters.Count) * s_zetaSeedOffset;
                        break;
                }

                _results[i] = ZetaSequence(zetaIndex);
            }
            else
            {
                //same index should not be processed twice
                _results[i] = double.MinValue;
            }
        }

        // workload for normal For
        private void Work(int i)
        {
            InvokeZetaWorkload(i - _parameters.StartIndex);
        }

        // workload for Foreach overload that takes a range partitioner
        private void Work(Tuple<int, int> tuple)
        {
            for (int i = tuple.Item1; i < tuple.Item2; i++)
            {
                Work(i);
            }
        }

        // workload for Foreach overload that takes a range partitioner
        private void Work(Tuple<long, long> tuple)
        {
            for (long i = tuple.Item1; i < tuple.Item2; i++)
            {
                Work(i);
            }
        }

        // workload for 64-bit For
        private void Work(long i)
        {
            InvokeZetaWorkload((int)(i - _parameters.StartIndex64));
        }

        // workload for normal For which will possibly invoke ParallelLoopState.Stop
        private void WorkWithStop(int i, ParallelLoopState state)
        {
            Work(i);
            if (i > (_parameters.StartIndex + _parameters.Count / 2))
                state.Stop();  // if the current index is in the second half range, try stop all
        }

        // workload for Foreach overload that takes a range partitioner
        private void WorkWithStop(Tuple<int, int> tuple, ParallelLoopState state)
        {
            for (int i = tuple.Item1; i < tuple.Item2; i++)
            {
                WorkWithStop(i, state);
            }
        }

        // workload for Foreach overload that takes a range partitioner
        private void WorkWithStop(Tuple<long, long> tuple, ParallelLoopState state)
        {
            for (long i = tuple.Item1; i < tuple.Item2; i++)
            {
                WorkWithStop(i, state);
            }
        }

        // workload for Foreach overload that takes a range partitioner
        private void WorkWithIndexAndStop(Tuple<int, int> tuple, ParallelLoopState state, long index)
        {
            for (int i = tuple.Item1; i < tuple.Item2; i++)
            {
                WorkWithIndexAndStop(i, state, index);
            }
        }

        // workload for Foreach overload that takes a range partitioner
        private void WorkWithIndexAndStopPartitioner(Tuple<int, int> tuple, ParallelLoopState state, long index)
        {
            WorkWithIndexAndStop(tuple, state, index);
        }

        // workload for Foreach overload that takes a range partitioner
        private List<int> WorkWithLocal(Tuple<int, int> tuple, ParallelLoopState state, List<int> threadLocalValue)
        {
            for (int i = tuple.Item1; i < tuple.Item2; i++)
            {
                WorkWithLocal(i, state, threadLocalValue);
            }

            return threadLocalValue;
        }

        // workload for 64-bit For which will possibly invoke ParallelLoopState.Stop
        private void WorkWithStop(long i, ParallelLoopState state)
        {
            Work(i);
            if (i > (_parameters.StartIndex64 + _parameters.Count / 2))
                state.Stop();
        }

        // workload for Parallel.Foreach which will possibly invoke ParallelLoopState.Stop 
        private void WorkWithIndexAndStop(int i, ParallelLoopState state, long index)
        {
            Work(i);

            if (index > (_parameters.Count / 2))
                state.Stop();
        }

        // workload for Parallel.Foreach which will possibly invoke ParallelLoopState.Stop 
        private void WorkWithIndexAndStopPartitioner(int i, ParallelLoopState state, long index)
        {
            //index verification
            if (_parameters.PartitionerType == PartitionerType.IEnumerableOOB)
            {
                int itemAtIndex = _collection[(int)index];
                Assert.Equal(i, itemAtIndex);
            }
            WorkWithIndexAndStop(i, state, index);
        }

        // workload for normal For which uses the ThreadLocalState accessible from ParallelLoopState
        private List<int> WorkWithLocal(int i, ParallelLoopState state, List<int> threadLocalValue)
        {
            Work(i);
            threadLocalValue.Add(i - _parameters.StartIndex);

            if (_parameters.StateOption == ActionWithState.Stop)
            {
                if (i > (_parameters.StartIndex + _parameters.Count / 2))
                    state.Stop();
            }

            return threadLocalValue;
        }

        // workload for 64-bit For which invokes both Stop and ThreadLocalState from ParallelLoopState
        private List<int> WorkWithLocal(long i, ParallelLoopState state, List<int> threadLocalValue)
        {
            Work(i);
            threadLocalValue.Add((int)(i - _parameters.StartIndex64));

            if (_parameters.StateOption == ActionWithState.Stop)
            {
                if (i > (_parameters.StartIndex64 + _parameters.Count / 2))
                    state.Stop();
            }

            return threadLocalValue;
        }

        // workload for Foreach overload that takes a range partitioner
        private List<int> WorkWithLocal(Tuple<long, long> tuple, ParallelLoopState state, List<int> threadLocalValue)
        {
            for (long i = tuple.Item1; i < tuple.Item2; i++)
            {
                Work(i);
                threadLocalValue.Add((int)(i - _parameters.StartIndex64));

                if (_parameters.StateOption == ActionWithState.Stop)
                {
                    if (i > (_parameters.StartIndex64 + _parameters.Count / 2))
                        state.Stop();
                }
            }
            return threadLocalValue;
        }

        // workload for Foreach overload that takes a range partitioner
        private List<int> WorkWithIndexAndLocal(Tuple<int, int> tuple, ParallelLoopState state, long index, List<int> threadLocalValue)
        {
            for (int i = tuple.Item1; i < tuple.Item2; i++)
            {
                Work(i);
                threadLocalValue.Add((int)index);

                if (_parameters.StateOption == ActionWithState.Stop)
                {
                    if (index > (_parameters.StartIndex + _parameters.Count / 2))
                        state.Stop();
                }
            }

            return threadLocalValue;
        }

        // workload for Foreach overload that takes a range partitioner
        private List<int> WorkWithLocalAndIndexPartitioner(Tuple<int, int> tuple, ParallelLoopState state, long index, List<int> threadLocalValue)
        {
            for (int i = tuple.Item1; i < tuple.Item2; i++)
            {
                //index verification - only for enumerable 
                if (_parameters.PartitionerType == PartitionerType.IEnumerableOOB)
                {
                    int itemAtIndex = _collection[(int)index];
                    Assert.Equal(i, itemAtIndex);
                }
            }
            return WorkWithIndexAndLocal(tuple, state, index, threadLocalValue);
        }

        // workload for Foreach which invokes both Stop and ThreadLocalState from ParallelLoopState
        private List<int> WorkWithIndexAndLocal(int i, ParallelLoopState state, long index, List<int> threadLocalValue)
        {
            Work(i);
            threadLocalValue.Add((int)index);

            if (_parameters.StateOption == ActionWithState.Stop)
            {
                if (index > (_parameters.StartIndex + _parameters.Count / 2))
                    state.Stop();
            }

            return threadLocalValue;
        }

        // workload for Foreach overload that takes a range partitioner
        private List<int> WorkWithLocalAndIndexPartitioner(int i, ParallelLoopState state, long index, List<int> threadLocalValue)
        {
            //index verification - only for enumerable 
            if (_parameters.PartitionerType == PartitionerType.IEnumerableOOB)
            {
                int itemAtIndex = _collection[(int)index];
                Assert.Equal(i, itemAtIndex);
            }

            return WorkWithIndexAndLocal(i, state, index, threadLocalValue);
        }

        #endregion

        #region Helper Methods

        public static double ZetaSequence(int n)
        {
            double result = 0;
            for (int i = 1; i < n; i++)
            {
                result += 1.0 / ((double)i * (double)i);
            }

            return result;
        }

        private List<int> ThreadLocalInit()
        {
            return new List<int>();
        }

        private void ThreadLocalFinally(List<int> local)
        {
            //add this row to the global sequences
            int index = Interlocked.Increment(ref _threadCount) - 1;
            _sequences[index] = local;
        }

        // consolidate all the indexes of the global sequences into one list
        private List<int> Consolidate(out List<int> duplicates)
        {
            duplicates = new List<int>();
            List<int> processedIndexes = new List<int>();
            //foreach (List<int> perThreadSequences in sequences)
            for (int thread = 0; thread < _threadCount; thread++)
            {
                List<int> perThreadSequences = _sequences[thread];
                foreach (int i in perThreadSequences)
                {
                    if (processedIndexes.Contains(i))
                    {
                        duplicates.Add(i);
                    }
                    else
                    {
                        processedIndexes.Add(i);
                    }
                }
            }

            return processedIndexes;
        }

        // Creates an instance of ParallelOptions with an non-default DOP        
        private ParallelOptions GetParallelOptions()
        {
            switch (_parameters.ParallelOption)
            {
                case WithParallelOption.WithDOP:
                    return new ParallelOptions() { TaskScheduler = TaskScheduler.Current, MaxDegreeOfParallelism = _parameters.Count };
                default:
                    throw new ArgumentOutOfRangeException("Test error: Invalid option of " + _parameters.ParallelOption);
            }
        }

        /// <summary>
        /// Each Parallel.For loop stores the result of its computation in the 'result' array.
        /// This function checks if result[i] for each i from 0 to _parameters.Count is correct
        /// A result[i] == double[i] means that the body for index i was run more than once
        /// </summary>
        /// <param name="i">index to check</param>
        /// <returns>true if result[i] contains the expected value</returns>
        private void Verify(int i)
        {
            //Function point comparison cant be done by rounding off to nearest decimal points since
            //1.64 could be represented as 1.63999999 or as 1.6499999999. To perform floating point comparisons, 
            //a range has to be defined and check to ensure that the result obtained is within the specified range
            double minLimit = 1.63;
            double maxLimit = 1.65;

            if (_results[i] < minLimit || _results[i] > maxLimit)
            {
                Assert.False(double.MinValue == _results[i], string.Format("results[{0}] has been revisited", i));
                
                Assert.True(_parameters.StateOption == ActionWithState.Stop && 0 == _results[i],
                    string.Format("Incorrect results[{0}]. Expected result to lie between {1} and {2} but got {3})", i, minLimit, maxLimit, _results[i]));
            }
        }

        /// <summary>
        /// Checks if the ThreadLocal Functions - Init and Locally were run correctly
        /// Init creates a new List. Each body, pushes in a unique index and Finally consolidates
        /// the lists into 'sequences' array
        /// 
        /// Expected: The consolidated list contains all indices that were executed.
        /// Duplicates indicate that the body for a certain index was executed more than once
        /// </summary>
        /// <returns>true if consolidated list contains indices for all executed loops</returns>
        private void VerifySequences()
        {
            List<int> duplicates;
            List<int> processedIndexes = Consolidate(out duplicates);
            Assert.Empty(duplicates);

            // If result[i] != 0 then the body for that index was executed.
            // We expect the threadlocal list to also contain the same index
            Assert.All(Enumerable.Range(0, _parameters.Count), idx => Assert.Equal(processedIndexes.Contains(idx), _results[idx] != 0));
        }

        #endregion
    }

    #region Helper Classes / Enums

    public class TestParameters
    {
        public const int DEFAULT_STARTINDEXOFFSET = 1000;

        public TestParameters(API api, StartIndexBase startIndexBase, int? startIndexOffset = null)
        {
            Api = api;
            StartIndexBase = startIndexBase;
            StartIndexOffset = startIndexOffset.HasValue ? startIndexOffset.Value : DEFAULT_STARTINDEXOFFSET;

            if (api == API.For64)
            {
                // StartIndexBase.Int64 was set to -1 since Enum can't take a Int64.MaxValue. Fixing this below.
                long indexBase64 = (startIndexBase == StartIndexBase.Int64) ? long.MaxValue : (long)startIndexBase;
                StartIndex64 = indexBase64 + StartIndexOffset;
            }
            else
            {
                // startIndexBase must not be StartIndexBase.Int64 
                StartIndex = (int)startIndexBase + StartIndexOffset;
            }

            WorkloadPattern = WorkloadPattern.Similar;

            // setting defaults.
            Count = 0;
            ChunkSize = -1;
            StateOption = ActionWithState.None;
            LocalOption = ActionWithLocal.None;
            ParallelOption = WithParallelOption.None;

            //partitioner options
            ParallelForeachDataSourceType = DataSourceType.Collection;
            PartitionerType = PartitionerType.IListBalancedOOB;
        }

        public readonly API Api;     // the api to be tested

        public int StartIndex;        // the real start index (base + offset) for the loop
        public long StartIndex64;     // the real start index (base + offset) for the 64 version loop

        public readonly StartIndexBase StartIndexBase; // the base of the _parameters.StartIndex for boundary testing 
        public int StartIndexOffset;          // the offset to be added to the base 

        public int Count;   // the _parameters.Count of loop range
        public int ChunkSize; // the chunk size to use for the range Partitioner

        public ActionWithState StateOption;    // the ParallelLoopState option of the action body
        public ActionWithLocal LocalOption;    // the ParallelLoopState<TLocal> option of the action body
        public WithParallelOption ParallelOption;  // the ParallelOptions used in P.For/Foreach

        public WorkloadPattern WorkloadPattern;  // the workload pattern used by each workload

        //partitioner 
        public PartitionerType PartitionerType;  //the partitioner type of the partitioner used - used for Partitioner tests
        public DataSourceType ParallelForeachDataSourceType;
    }

    /// <summary>
    /// used for partitioner creation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PartitionerFactory<T>
    {
        public static OrderablePartitioner<T> Create(PartitionerType partitionerName, IEnumerable<T> dataSource)
        {
            switch (partitionerName)
            {
                case PartitionerType.IListBalancedOOB:
                    return Partitioner.Create(new List<T>(dataSource), true);
                case PartitionerType.ArrayBalancedOOB:
                    return Partitioner.Create(new List<T>(dataSource).ToArray(), true);
                case PartitionerType.IEnumerableOOB:
                    return Partitioner.Create(dataSource);
                case PartitionerType.IEnumerable1Chunk:
                    return Partitioner.Create<T>(dataSource, EnumerablePartitionerOptions.NoBuffering);
                default:
                    break;
            }
            return null;
        }
        public static OrderablePartitioner<Tuple<int, int>> Create(PartitionerType partitionerName, int from, int to, int chunkSize = -1)
        {
            switch (partitionerName)
            {
                case PartitionerType.RangePartitioner:
                    return (chunkSize == -1) ? Partitioner.Create(from, to) : Partitioner.Create(from, to, chunkSize);
                default:
                    break;
            }
            return null;
        }
    }

    /// <summary>
    /// Partitioner types used for ParallelForeach with partitioners
    /// </summary>
    [Flags]
    public enum PartitionerType
    {
        IListBalancedOOB = 0, // Out of the box List Partitioner
        ArrayBalancedOOB = 1, // Out of the box Array partitioner
        IEnumerableOOB = 2,  // Out of the box Enumerable partitioner
        RangePartitioner = 3,  // out of the box range partitioner
        IEnumerable1Chunk = 4  // partitioner one chunk
    }

    public enum DataSourceType
    {
        Partitioner,
        Collection
    }

    // List of APIs being tested
    public enum API
    {
        For,
        For64,
        ForeachOnArray,
        ForeachOnList,
        Foreach
    }

    public enum ActionWithState
    {
        None,          // no ParallelLoopState
        Stop,          // need ParallelLoopState and will do Stop
        //Break,         // need ParallelLoopState and will do Break, which is covered in ..\ParallelStateTests
    }

    public enum ActionWithLocal
    {
        None,            // no ParallelLoopState<TLocal>
        HasFinally,     // need ParallelLoopState<TLocal> and Action<TLocal> threadLocalFinally
    }

    public enum WithParallelOption
    {
        None,            // no ParallelOptions
        WithDOP,         // ParallelOptions created with DOP
    }

    public enum StartIndexBase
    {
        Zero = 0,
        Int16 = short.MaxValue,
        Int32 = int.MaxValue,
        Int64 = -1,     // Enum can't take a Int64.MaxValue
    }

    public enum WorkloadPattern
    {
        Similar,
        Increasing,
        Decreasing,
        Random,
    }

    #endregion
}
