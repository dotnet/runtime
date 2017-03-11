// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// This file contains functional tests for ParallelLoopState
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=--=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Xunit;

namespace System.Threading.Tasks.Test
{
    public sealed class ParallelStateTest
    {
        #region Private Fields

        private readonly object _lock = new object();

        private readonly IEnumerable<int> _collection = null;  // the collection used in Foreach
        private readonly Barrier _barrier;

        // Holds list of available actions 
        private readonly Dictionary<string, Action<long, ParallelLoopState>> _availableActions = new Dictionary<string, Action<long, ParallelLoopState>>();
        private readonly Dictionary<string, Action<ParallelLoopResult?>> _availableVerifications = new Dictionary<string, Action<ParallelLoopResult?>>();

        private readonly TestParameters _parameters;
        private readonly ManualResetEventSlim _mreSlim;

        private readonly double[] _results;  // global place to store the workload result for verification

        // data structure used with ParallelLoopState<TLocal>
        // each row is the sequence of loop "index" finished in the same thread
        // private Dictionary<int, List<int>> sequences; 
        private long _threadCount;
        private readonly List<int>[] _sequences;
        private readonly List<long>[] _sequences64;

        private long _startIndex = 0;  // start index for the loop

        // Hold list of actions to be performed
        private List<Action<long, ParallelLoopState>> _actions = new List<Action<long, ParallelLoopState>>();

        // Hold list of verification
        private Queue<Action<ParallelLoopResult?>> _verifications = new Queue<Action<ParallelLoopResult?>>();

        private volatile bool _isStopped = false;   			// Flag to indicate that we called Stop() on the Parallel state
        private long? _lowestBreakIter = null;				    // LowestBreakIteration value holder, null indicates that Break hasn't been called
        private volatile bool _isExceptional = false;       	// Flag to indicate exception thrown in the test

        private int _iterCount = 0;  // test own counter for certain scenario, so the test can change behaviour after certain number of loop iteration

        #endregion

        #region Constructor

        public ParallelStateTest(TestParameters parameters)
        {
            _parameters = parameters;

            _mreSlim = new ManualResetEventSlim(false);

            _results = new double[parameters.Count];

            _sequences = new List<int>[1024];
            _sequences64 = new List<long>[1024];
            _threadCount = 0;

            // Set available actions
            _availableActions["Stop"] = StopAction;
            _availableActions["Break"] = BreakAction;
            _availableActions["Exceptional"] = ExceptionalAction;
            _availableActions["MultipleStop"] = MultipleStopAction;
            _availableActions["MultipleBreak"] = MultipleBreakAction;
            _availableActions["MultipleException"] = MultipleExceptionAction;
            _availableActions["SyncWaitStop"] = SyncWaitStop;
            _availableActions["SyncSetStop"] = SyncSetStop;
            _availableActions["SyncWaitBreak"] = SyncWaitBreak;
            _availableActions["SyncSetBreak"] = SyncSetBreak;

            _availableActions["SyncWaitStopCatchExp"] = SyncWaitStopCatchExp;
            _availableActions["SyncWaitBreakCatchExp"] = SyncWaitBreakCatchExp;

            _availableActions["SyncWaitExceptional"] = SyncWaitExceptional;
            _availableActions["SyncSetExceptional"] = SyncSetExceptional;

            // Set available verifications
            _availableVerifications["StopVerification"] = StopVerification;
            _availableVerifications["BreakVerification"] = BreakVerification;
            _availableVerifications["ExceptionalVerification"] = ExceptionalVerification;

            _barrier = new Barrier(parameters.Count);

            // A barrier is used in the workload to ensure that all tasks are running before any proceed.
            // This causes delays if the count is higher than the number of processors, as the thread pool
            // will need to (slowly) inject additional threads to meet the demand.  As a less-than-ideal
            // workaround, we change the thread pool's min thread count to be at least the number required
            // for the test.  Not perfect, but better than nothing.
            ThreadPoolHelpers.EnsureMinThreadsAtLeast(parameters.Count);

            int length = parameters.Count;
            if (length < 0)
                length = 0;

            if (parameters.Api != API.For)
            {
                int[] collArray = new int[length];
                for (int j = 0; j < length; j++)
                    collArray[j] = ((int)_startIndex) + j;

                if (parameters.Api == API.ForeachOnArray)
                    _collection = collArray;
                else if (parameters.Api == API.ForeachOnList)
                    _collection = new List<int>(collArray);
                else
                    _collection = collArray;
            }

            int index = 0;
            for (index = 0; index < parameters.Count; index++)
                _actions.Add(DummyAction);

            index = 0;
            foreach (string action in parameters.Actions)
            {
                Action<long, ParallelLoopState> a = null;
                string[] actionIndexPair = action.Split('_');

                if (!_availableActions.TryGetValue(actionIndexPair[0], out a))
                    throw new ArgumentException(actionIndexPair[0] + " is not a valid action");

                _actions[actionIndexPair.Length > 1 ? int.Parse(actionIndexPair[1]) : index++] = a;
            }

            foreach (string verification in parameters.Verifications)
            {
                Action<ParallelLoopResult?> act = null;

                if (!_availableVerifications.TryGetValue(verification, out act))
                    throw new ArgumentException(verification + " is not a valid verification");

                _verifications.Enqueue(act);
            }
        }

        #endregion

        internal void RealRun()
        {
            ParallelLoopResult? loopResult = null;
            try
            {
                if (!_parameters.Is64)
                {
                    if (_parameters.Api == API.For)
                    {
                        if (_parameters.WithLocalState)
                        {
                            // call Parallel.For with step and ParallelLoopState<TLocal>, plus threadLocalFinally
                            loopResult = Parallel.For<List<int>>((int)_startIndex, (int)_startIndex + _parameters.Count, ThreadLocalInit, WorkWithLocalState, ThreadLocalFinally);
                        }
                        else
                        {
                            loopResult = Parallel.For((int)_startIndex, (int)_startIndex + _parameters.Count, WorkWithNoLocalState);
                        }
                    }
                    else
                    {
                        if (_parameters.WithLocalState)
                        {
                            // call Parallel.Foreach and ParallelLoopState<TLocal>, plus threadLocalFinally
                            loopResult = Parallel.ForEach<int, List<int>>(_collection, ThreadLocalInit, WorkWithLocalState, ThreadLocalFinally);
                        }
                        else
                        {
                            loopResult = Parallel.ForEach<int>(_collection, WorkWithNoLocalState);
                        }
                    }
                }
                else
                {
                    _startIndex = int.MaxValue;

                    if (_parameters.Api == API.For)
                    {
                        if (_parameters.WithLocalState)
                        {
                            // call Parallel.For with step and ParallelLoopState<TLocal>, plus threadLocalFinally
                            loopResult = Parallel.For<List<long>>(_startIndex, _startIndex + _parameters.Count, ThreadLocalInit64, WorkWithLocalState, ThreadLocalFinally64);
                        }
                        else
                        {
                            loopResult = Parallel.For(_startIndex, _startIndex + _parameters.Count, WorkWithNoLocalState);
                        }
                    }
                }

                Assert.False(_parameters.ExpectingException, "SystemInvalidOperation Exception was not thrown when expecting one");
            }
            catch (AggregateException exp)
            {
                if (_parameters.ExpectingException)
                    Assert.IsType<InvalidOperationException>(exp.Flatten().InnerException);
            }

            // If the config file specified what verifications to use for this test, verify result
            while (_verifications.Count > 0)
            {
                _verifications.Dequeue().Invoke(loopResult);
            }
        }

        #region Workloads

        // Workload for Parallel.For / Foreach
        private void Work(long i)
        {
            // 
            // Make sure all task are spawned, before moving on
            //
            _barrier.SignalAndWait();

            if (_results[i - _startIndex] == 0)
                _results[i - _startIndex] = ZetaSequence((int)(i - _startIndex) + 1000);
            else
                _results[i - _startIndex] = double.MinValue;  //same index should not be processed twice
        }

        // Workload for Parallel.For / Foreach with parallelloopstate but no thread local state
        private void WorkWithNoLocalState(int i, ParallelLoopState state)
        {
            Debug.WriteLine("WorkWithNoLocalState(int) on index {0}, StartIndex: {1}, real index {2}", i, _startIndex, i - _startIndex);
            Work(i);

            _actions[i].Invoke(i, state);
        }

        // Workload for Parallel.For / Foreach with parallel loopstate and thread local state
        private List<int> WorkWithLocalState(int i, ParallelLoopState state, List<int> threadLocalValue)
        {
            Debug.WriteLine("WorkWithLocalState(int) on index {0}, StartIndex: {1}, real index {2}", i, _startIndex, i - _startIndex);
            Work(i);

            threadLocalValue.Add(i + (int)_startIndex);

            _actions[i].Invoke(i, state);

            return threadLocalValue;
        }

        // Workload for Parallel.For / Foreach with index, parallel loop state and thread local state
        private List<int> WorkWithLocalState(int i, int index, ParallelLoopState state, List<int> threadLocalValue)
        {
            Debug.WriteLine("WorkWithLocalState(int, index) on index {0}, StartIndex: {1}, real index {2}", i, _startIndex, i - _startIndex);
            Work(i);
            threadLocalValue.Add(index + (int)_startIndex);

            _actions[index].Invoke(index, state);

            return threadLocalValue;
        }

        // Workload for Parallel.For with long range
        private void WorkWithNoLocalState(long i, ParallelLoopState state)
        {
            Debug.WriteLine("WorkWithNoLocalState(long) on index {0}, StartIndex: {1}, real index {2}", i, _startIndex, i - _startIndex);
            Work(i);

            _actions[(int)(i - _startIndex)].Invoke(i, state);
        }

        // Workload for Parallel.For with long range
        private List<long> WorkWithLocalState(long i, ParallelLoopState state, List<long> threadLocalValue)
        {
            Debug.WriteLine("WorkWithLocalState(long) on index {0}, StartIndex: {1}, real index {2}", i, _startIndex, i - _startIndex);
            Work(i);
            threadLocalValue.Add(i + _startIndex);

            _actions[(int)(i - _startIndex)].Invoke(i, state);

            return threadLocalValue;
        }

        /// <summary>
        /// This action waits for the other iteration to call Stop and 
        /// set the MRE when its done. Once the MRE is set, this function 
        /// calls Break which results in an InvalidOperationException
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void SyncWaitBreakCatchExp(long i, ParallelLoopState state)
        {
            _mreSlim.Wait();
            BreakActionHelper(i, state, true);
        }

        /// <summary>
        /// This action waits for the other iteration to call Break and 
        /// set the MRE when its done. Once the MRE is set, this function 
        /// calls Stop which results in an InvalidOperationException
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void SyncWaitStopCatchExp(long i, ParallelLoopState state)
        {
            _mreSlim.Wait();
            StopActionHelper(i, state, true);
        }

        /// <summary>
        /// This action waits for the other iteration to call Break and 
        /// set the MRE when its done. Once the MRE is set, this function 
        /// calls Break which results in the lower iteration winning
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void SyncWaitBreak(long i, ParallelLoopState state)
        {
            //Logger.LogInformation("Calling SyncWaitBreakAction on index {0}, StartIndex: {1}, real index {2}", i, StartIndex, i - StartIndex);
            _mreSlim.Wait();
            BreakAction(i, state);
        }

        /// <summary>
        /// This action calls Break and notifies other iterations
        /// by setting the shared MRE
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void SyncSetBreak(long i, ParallelLoopState state)
        {
            //Logger.LogInformation("Calling SyncSetBreakAction on index {0}, StartIndex: {1}, real index {2}", i, StartIndex, i - StartIndex);
            // Do some sleep to reduce race condition with next action
            Task delay = Task.Delay(10);
            delay.Wait();
            BreakAction(i, state);
            _mreSlim.Set();
        }

        /// <summary>
        /// This function waits for another iteration to call Stop
        /// and then set the shared MRE to notify when it is done
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void SyncWaitStop(long i, ParallelLoopState state)
        {
            //Logger.LogInformation("Calling SyncWaitStopAction on index {0}, StartIndex: {1}, real index {2}", i, StartIndex, i - StartIndex);
            _mreSlim.Wait();
            StopAction(i, state);
        }

        /// <summary>
        /// This action calls Stop and notifies the other iteration by setting the MRE
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void SyncSetStop(long i, ParallelLoopState state)
        {
            //Logger.LogInformation("Calling SyncSetStopAction on index {0}, StartIndex: {1}, real index {2}", i, StartIndex, i - StartIndex);
            // Do some sleep to reduce race condition with next action
            Task delay = Task.Delay(10);
            delay.Wait();
            StopAction(i, state);
            _mreSlim.Set();
        }

        /// <summary>
        /// This action waits for another iteration to throw an exception and notify 
        /// when it is done by setting the MRE
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void SyncWaitExceptional(long i, ParallelLoopState state)
        {
            _mreSlim.Wait();
            ExceptionalAction(i, state);
        }

        /// <summary>
        /// This action throws an exception and notifies the rest of the iterations
        /// by setting a shared MRE
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void SyncSetExceptional(long i, ParallelLoopState state)
        {
            // Do some sleep to reduce race condition with next action
            Task delay = Task.Delay(10);
            delay.Wait();
            ExceptionalAction(i, state);
            _mreSlim.Set();
        }

        /// <summary>
        /// This action is a NOP - does nothing
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void DummyAction(long i, ParallelLoopState state)
        {
        }

        /// <summary>
        /// This actions calls Stop on the current iteration. Note that this is called by only one iteration in the loop
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void StopAction(long i, ParallelLoopState state)
        {
            StopActionHelper(i, state, false);
        }

        /// <summary>
        /// Calls Break for the current Iteration. Note that this is called by only one iteration in the loop
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void BreakAction(long i, ParallelLoopState state)
        {
            BreakActionHelper(i, state, false);
        }

        /// <summary>
        /// Note!! This function is not threadsafe and care must be taken so that it is not called concurrently
        /// 
        /// Helper function that calls Stop for the current iteration and sets test flag(m_isStopped) to true
        /// 
        /// 1) If stop was already called, check if ParallelLoopState-->IsStopped is true
        /// 2) If stop was already called, check if ParallelLoopState-->ShouldExitCurrentIteration is true
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        /// <param name="catchException"></param>
        private void StopActionHelper(long i, ParallelLoopState state, bool catchException)
        {
            Debug.WriteLine("Calling StopAction on index: {0}, StartIndex: {1}, real index {2}", i, _startIndex, i - _startIndex);

            // We already called Stop() on the Parallel state
            Assert.False(_isStopped && _isStopped != state.IsStopped, String.Format("Expecting IsStopped to be true for iteration {0}", i));

            // If we previously called Stop() on the parallel state,
            // we expect all iterations see the state's ShouldExitCurrentIteration to be true
            Assert.False(_isStopped && !state.ShouldExitCurrentIteration, String.Format("Expecting ShouldExitCurrentIteration to be true for iteration {0}", i));

            try
            {
                state.Stop();
                _isStopped = true;

                // If Stop is called after a Break was called then an InvalidOperationException is expected
                Assert.False(catchException, "Not getting InvalidOperationException from Stop() when expecting one");
            }
            // If Stop is called after a Break was called then an InvalidOperationException is expected
            catch (InvalidOperationException) when (catchException)
            {
            }
        }

        /// <summary>
        /// Thread safe version of Stop Action. This can safely be invoked concurrently
        /// 
        /// Stops the loop for first parameters.Count/2 iterations and sets the test flag (m_iterCount) to indicate this
        /// 
        /// 1) If Stop was previously called, then check that ParallelLoopState-->IsStopped is set to true
        /// 2) If Stop was previously called, then check that ParallelLoopState-->ShouldExitCurrentIteration is true
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        /// <param name="catchException"></param>
        private void MultipleStopAction(long i, ParallelLoopState state)
        {
            if (Interlocked.Increment(ref _iterCount) < _parameters.Count / 2)
            {
                state.Stop();
                _isStopped = true;
            }
            else
            {
                // We already called Stop() on the Parallel state
                Assert.False(_isStopped && !state.IsStopped, String.Format("Expecting IsStopped to be true for iteration {0}", i));

                // If we previously called Stop() on the parallel state,
                // we expect all iterations see the state's ShouldExitCurrentIteration to be true
                Assert.False(_isStopped && !state.ShouldExitCurrentIteration, String.Format("Expecting ShouldExitCurrentIteration to be true for iteration {0}", i));
            }
        }

        /// <summary>
        /// NOTE!!! that this function is not thread safe and cannot be called concurrently
        /// 
        /// Helper function that calls Break for the current iteration if 
        /// 1) Break has never been called so far
        /// 2) if the current iteration is smaller than the iteration for which Break was previously called
        /// 
        /// If Break was already called then check that
        /// 1) The lowest break iteration stored by us is the same as the one passed in State
        /// 2) If this iteration is greater than the lowest break iteration, then shouldExitCurrentIteration should be true
        /// 3) if this iteration is lower than the lowest break iteration then shouldExitCurrentIteration should be false
        /// </summary>
        /// <param name="i">current iteration </param>
        /// <param name="state">the parallel loop state</param>
        /// <param name="catchException">whether calling Break will throw an InvalidOperationException</param>
        private void BreakActionHelper(long i, ParallelLoopState state, bool catchException)
        {
            Debug.WriteLine("Calling BreakAction on index {0}, StartIndex: {1}, real index {2}", i, _startIndex, i - _startIndex);

            // If we previously called Break() on the parallel state,
            // we expect all iterations to have the same LowestBreakIteration value
            if (_lowestBreakIter.HasValue)
            {
                Assert.False(state.LowestBreakIteration.Value != _lowestBreakIter.Value,
                    String.Format("Expecting LowestBreakIteration value to be {0} for iteration {1}, while getting {2}", _lowestBreakIter, i, state.LowestBreakIteration.Value));

                // If we previously called Break() on the parallel state,
                // we expect all higher iterations see the state's ShouldExitCurrentIteration to be true
                Assert.False(i > _lowestBreakIter.Value && !state.ShouldExitCurrentIteration,
                    String.Format("Expecting ShouldExitCurrentIteration to be true for iteration {0}, LowestBreakIteration is {1}", i, _lowestBreakIter));
            }

            if (_lowestBreakIter.HasValue && i < _lowestBreakIter.Value && state.ShouldExitCurrentIteration)
            {
                long lbi = _lowestBreakIter.Value;
                // If we previously called Break() on the parallel state,
                // we expect all lower iterations see the state's ShouldExitCurrentIteration to be false.
                // There is however a race condition during the check here, another Break could've happen
                // in between retrieving LowestBreakIteration value and ShouldExitCurrentIteration
                // which changes the value of ShouldExitCurrentIteration. 
                // We do another sample instead of LowestBreakIteration before failing the test
                Assert.False(i < lbi, String.Format("Expecting ShouldExitCurrentIteration to be false for iteration {0}, LowestBreakIteration is {1}", i, lbi));
            }

            if (!_lowestBreakIter.HasValue || (_lowestBreakIter.HasValue && i < _lowestBreakIter.Value))
            {
                // If calls Break for the first time or if current iteration less than LowestBreakIteration, 
                // call Break() again, and make sure LowestBreakIteration value gets updated
                try
                {
                    state.Break();
                    _lowestBreakIter = state.LowestBreakIteration; // Save the lowest beak iteration
                    // If the test is checking the scenario where break is called after stop then 
                    // we expect an InvalidOperationException
                    Assert.False(catchException, "Not getting InvalidOperationException from Break() when expecting one");
                }
                // If the test is checking the scenario where break is called after stop then 
                // we expect an InvalidOperationException
                catch (InvalidOperationException) when (catchException)
                {
                }
            }
        }

        /// <summary>
        /// This actions tests multiple Break calls from different iteration loops
        ///  
        /// Helper function that calls Break for the first parameters.Count/2 iterations
        /// 
        /// If Break was already called then check that
        /// 1) If this iteration is greater than the lowest break iteration, then shouldExitCurrentIteration should be true
        /// 2) if this iteration is lower than the lowest break iteration then shouldExitCurrentIteration should be false
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        /// <param name="catchException"></param>
        private void MultipleBreakAction(long i, ParallelLoopState state)
        {
            if (Interlocked.Increment(ref _iterCount) < _parameters.Count / 2)
            {
                state.Break();
                lock (_lock)
                {
                    // Save the lowest beak iteration
                    //m_lowestBreakIter = !m_lowestBreakIter.HasValue ? i : Math.Min(m_lowestBreakIter.Value, i); 
                    if (!_lowestBreakIter.HasValue)
                        _lowestBreakIter = i;

                    if (_lowestBreakIter.Value > i)
                        _lowestBreakIter = i;
                }
            }
            else
            {
                // If we previously called Break() on the parallel state,
                // we expect all higher iterations see the state's ShouldExitCurrentIteration to be true
                if (state.LowestBreakIteration.HasValue)
                {
                    Assert.False(i > state.LowestBreakIteration.Value && !state.ShouldExitCurrentIteration,
                        String.Format("Expecting ShouldExitCurrentIteration to be true for iteration {0}, LowestBreakIteration is {1}", i, state.LowestBreakIteration.Value));
                }

                if (state.LowestBreakIteration.HasValue && i < state.LowestBreakIteration.Value && state.ShouldExitCurrentIteration)
                {
                    long lbi = state.LowestBreakIteration.Value;

                    // If we previously called Break() on the parallel state,
                    // we expect all lower iterations see the state's ShouldExitCurrentIteration to be false.
                    // There is however a race condition during the check here, another Break could've happen
                    // in between retrieving LowestBreakIteration value and ShouldExitCurrentIteration
                    // which changes the value of ShouldExitCurrentIteration. 
                    // We do another sample instead of LowestBreakIteration before failing the test
                    Assert.False(i < lbi, String.Format("Expecting ShouldExitCurrentIteration to be false for iteration {0}, LowestBreakIteration is {1}", i, lbi));
                }
            }
        }

        /// <summary>
        /// Note!! This function is not thread safe and care must be taken so it is not called concurrently
        /// 
        /// This helper throws an exception from the current iteration if an exception is not already thrown
        /// 
        /// 1) If an exception was previously thrown (m_isExceptional = true), then it checks if 
        ///    ParallelLoopState-->IsExceptional is set
        /// 2) If an exception was previously thrown then it checks if ParallelLoopState-->ShouldExitCurrentIteration is true
        /// 
        /// If an exception was not thrown before this, then throw an exception and set test flag m_isExceptional to true
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void ExceptionalAction(long i, ParallelLoopState state)
        {
            Debug.WriteLine("Calling ExceptionalAction on index {0}, StartIndex: {1}, real index {2}", i, _startIndex, i - _startIndex);

            Assert.False(_isExceptional != state.IsExceptional, String.Format("IsExceptional is expected to be {0} while getting {1}", _isExceptional, state.IsExceptional));

            // Previous iteration throws exception, the Parallel should stop it's work
            Assert.False(_isExceptional && !state.ShouldExitCurrentIteration, String.Format("Expecting ShouldExitCurrentIteration to be true, since Exception was thrown on previous iterations"));

            try
            {
                throw new InvalidOperationException("Throws test exception to verify it got handled properly");
            }
            finally
            {
                _isExceptional = true;
            }
        }

        /// <summary>
        /// This is the thread safe version of an action that throws exceptions and called be called concurrently
        /// 
        /// This action throws an exception for the first parameters.Count/2 iterations
        /// 
        /// For the rest of the actions, it performs the following checks
        /// 1) If an exception was already thrown then check that ParallelLoopState->IsException is true
        /// 2) If an exception was already thrown then check that ParallelLoopState->ShouldExitCurrentIteration is true
        /// </summary>
        /// <param name="i"></param>
        /// <param name="state"></param>
        private void MultipleExceptionAction(long i, ParallelLoopState state)
        {
            Debug.WriteLine("Calling ExceptionalAction2 on index {0}, StartIndex: {1}, real index {2}", i, _startIndex, i - _startIndex);

            if (Interlocked.Increment(ref _iterCount) < _parameters.Count / 2)
            {
                try
                {
                    throw new System.InvalidOperationException("Throws test exception to verify it got handled properly");
                }
                finally
                {
                    _isExceptional = true;
                }
            }
            else
            {
                Assert.False(state.IsExceptional && !_isExceptional, String.Format("IsExceptional is expected to be {0} while getting {1}", _isExceptional, state.IsExceptional));

                // Previous iteration throws exception, the Parallel should stop it's work
                Assert.False(state.IsExceptional && !state.ShouldExitCurrentIteration,
                    String.Format("Expecting ShouldExitCurrentIteration to be true, since Exception was thrown on previous iterations"));
            }
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

        /// <summary>
        /// Called when a Thread is being used for the first time in the Parallel loop
        /// Used by the 64 bit versions of Parallel.For
        /// </summary>
        /// <returns>a list where each loop body will store a unique result for verification</returns>
        private List<long> ThreadLocalInit64()
        {
            List<long> local = new List<long>();

            return local;
        }

        /// <summary>
        /// Called when a Thread has completed execution in the Parallel loop
        /// Used by the 64 bit versions of Parallel.For
        /// </summary>
        /// <returns>Stores the ThreadLocal list of results to a global container for verification</returns>
        private void ThreadLocalFinally64(List<long> local)
        {
            //add this row to the global sequences
            //sequences.Add(Thread.CurrentThread.ManagedThreadId, local);
            long index = Interlocked.Increment(ref _threadCount) - 1;
            _sequences64[index] = local;
        }

        /// <summary>
        /// Called when a Thread is being used for the first time in the Parallel loop        
        /// </summary>
        /// <returns>a list where each loop body will store a unique result for verification</returns>
        private List<int> ThreadLocalInit()
        {
            List<int> local = new List<int>();

            return local;
        }

        /// <summary>
        /// Called when a Thread has completed execution in the Parallel loop
        /// </summary>
        /// <returns>Stores the ThreadLocal list of results to a global container for verification</returns>
        private void ThreadLocalFinally(List<int> local)
        {
            //add this row to the global sequences
            //sequences.Add(Thread.CurrentThread.ManagedThreadId, local);
            long index = Interlocked.Increment(ref _threadCount) - 1;
            _sequences[(int)index] = local;
        }

        /// <summary>
        /// Checks that the result returned by the body of iteration i is correct
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private void Verify(int i)
        {
            //Function point comparison cant be done by rounding off to nearest decimal points since
            //1.64 could be represented as 1.63999999 or as 1.6499999999. To perform floating point comparisons, 
            //a range has to be defined and check to ensure that the result obtained is within the specified range
            double minLimit = 1.63;
            double maxLimit = 1.65;

            if (_results[i] < minLimit || _results[i] > maxLimit)
            {
                Assert.False(double.MinValue == _results[i], String.Format("results[{0}] has been revisited", i));

                if (_isStopped && 0 == _results[i])
                    Debug.WriteLine("Stopped calculation at index = {0}", i);

                Assert.True(_isStopped && 0 == _results[i],
                    String.Format("Incorrect results[{0}]. Expected to lie between {1} and {2}, but got {3})", i, minLimit, maxLimit, _results[i]));
            }
        }

        /// <summary>
        /// Used to verify the result of a loop that was 'Stopped'
        /// 
        /// Expected: 
        /// 1) A ParallelLoopResult with IsCompleted = false and LowestBreakIteration = null
        /// 2) For results that were processed, the body stored the correct value
        /// </summary>
        /// <param name="loopResult"></param>
        /// <returns></returns>
        private void StopVerification(ParallelLoopResult? loopResult)
        {
            Assert.False(loopResult == null, "No ParallelLoopResult returned");

            Assert.False(loopResult.Value.IsCompleted == true || loopResult.Value.LowestBreakIteration != null,
                    String.Format("ParallelLoopResult invalid, expecting Completed=false,LowestBreakIteration=null, actual: {0}, {1}", loopResult.Value.IsCompleted, loopResult.Value.LowestBreakIteration));

            for (int i = 0; i < _parameters.Count; i++)
                Verify(i);
        }

        /// <summary>
        /// This verification is used we successfully called 'Break' on the loop
        /// 
        /// Expected:
        /// 1) A valid ParallelLoopResult was returned with IsCompleted = false & LowestBreakIteration = lowest iteration on which
        ///    the test called Break
        /// 2) For results that were processed, the body stored the correct value
        /// </summary>
        /// <param name="loopResult"></param>
        /// <returns></returns>
        private void BreakVerification(ParallelLoopResult? loopResult)
        {
            Assert.False(loopResult == null, "No ParallelLoopResult returned");

            Assert.False(loopResult.Value.IsCompleted == true || loopResult.Value.LowestBreakIteration == null || loopResult.Value.LowestBreakIteration != _lowestBreakIter,
                String.Format("ParallelLoopResult invalid, expecting Completed=false,LowestBreakIteration={0}, actual: {1}, {2}", _lowestBreakIter, loopResult.Value.IsCompleted, loopResult.Value.LowestBreakIteration));

            for (int i = 0; i < _lowestBreakIter.Value - _startIndex; i++)
                Verify(i);
        }

        /// <summary>
        /// This verification is called when we expect an exception from the test
        /// 
        /// Expected: ParallelLoopResult is returned as null
        /// </summary>
        /// <param name="loopResult"></param>
        /// <returns></returns>
        private void ExceptionalVerification(ParallelLoopResult? loopResult)
        {
            Assert.Null(loopResult);
        }

        #endregion
    }

    public enum API
    {
        For,
        ForeachOnArray,
        ForeachOnList,
    }

    public class TestParameters
    {
        public TestParameters()
        {
            Api = API.For;
        }

        public API Api; // the api to be tested
        public int Count; // the count of loop range
        public IEnumerable<string> Actions;
        public IEnumerable<string> Verifications;
        public bool ExpectingException; // Exception is expected
        public bool WithLocalState;
        public bool Is64;
    }

    public sealed class ParallelState
    {
        [Fact]
        [OuterLoop]
        public static void ParallelState0()
        {
            string[] actions = new string[] { "Break", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState1()
        {
            string[] actions = new string[] { "Break", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState2()
        {
            string[] actions = new string[] { "Break", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState3()
        {
            string[] actions = new string[] { "Exceptional", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState4()
        {
            string[] actions = new string[] { "Exceptional", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState5()
        {
            string[] actions = new string[] { "Exceptional", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState6()
        {
            string[] actions = new string[] { "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState7()
        {
            string[] actions = new string[] { "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState8()
        {
            string[] actions = new string[] { "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState9()
        {
            string[] actions = new string[] { "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState10()
        {
            string[] actions = new string[] { "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState11()
        {
            string[] actions = new string[] { "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState12()
        {
            string[] actions = new string[] { "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState13()
        {
            string[] actions = new string[] { "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState14()
        {
            string[] actions = new string[] { "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState15()
        {
            string[] actions = new string[] { "Stop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState16()
        {
            string[] actions = new string[] { "Stop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState17()
        {
            string[] actions = new string[] { "Stop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState18()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStopCatchExp_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState19()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStopCatchExp_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState20()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStopCatchExp_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState21()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStop_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState22()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStop_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState23()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStop_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState24()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreakCatchExp_1", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState25()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreakCatchExp_1", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState26()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreakCatchExp_1", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState27()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreak_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState28()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreak_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState29()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreak_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState30()
        {
            string[] actions = new string[] { "SyncWaitBreak_0", "SyncSetBreak_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState31()
        {
            string[] actions = new string[] { "SyncWaitBreak_0", "SyncSetBreak_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState32()
        {
            string[] actions = new string[] { "SyncWaitBreak_0", "SyncSetBreak_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.For,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = true,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState33()
        {
            string[] actions = new string[] { "Break", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState34()
        {
            string[] actions = new string[] { "Break", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState35()
        {
            string[] actions = new string[] { "Exceptional", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState36()
        {
            string[] actions = new string[] { "Exceptional", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState37()
        {
            string[] actions = new string[] { "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState38()
        {
            string[] actions = new string[] { "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState39()
        {
            string[] actions = new string[] { "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState40()
        {
            string[] actions = new string[] { "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState41()
        {
            string[] actions = new string[] { "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState42()
        {
            string[] actions = new string[] { "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState43()
        {
            string[] actions = new string[] { "Stop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState44()
        {
            string[] actions = new string[] { "Stop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void ParallelState45()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStopCatchExp_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void ParallelState46()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStopCatchExp_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState47()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStop_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState48()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStop_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void ParallelState49()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreakCatchExp_1", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void ParallelState50()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreakCatchExp_1", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState51()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreak_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState52()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreak_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState53()
        {
            string[] actions = new string[] { "SyncWaitBreak_0", "SyncSetBreak_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState54()
        {
            string[] actions = new string[] { "SyncWaitBreak_0", "SyncSetBreak_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnArray,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState55()
        {
            string[] actions = new string[] { "Break", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState56()
        {
            string[] actions = new string[] { "Break", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState57()
        {
            string[] actions = new string[] { "Exceptional", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState58()
        {
            string[] actions = new string[] { "Exceptional", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState59()
        {
            string[] actions = new string[] { "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState60()
        {
            string[] actions = new string[] { "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", "MultipleBreak", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState61()
        {
            string[] actions = new string[] { "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState62()
        {
            string[] actions = new string[] { "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", "MultipleException", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState63()
        {
            string[] actions = new string[] { "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState64()
        {
            string[] actions = new string[] { "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", "MultipleStop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState65()
        {
            string[] actions = new string[] { "Stop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState66()
        {
            string[] actions = new string[] { "Stop", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void ParallelState67()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStopCatchExp_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }
        [Fact]
        [OuterLoop]
        public static void ParallelState68()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStopCatchExp_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState69()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStop_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState70()
        {
            string[] actions = new string[] { "SyncSetBreak_0", "SyncWaitStop_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState71()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreakCatchExp_1", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState72()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreakCatchExp_1", };
            string[] verifications = new string[] { "StopVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState73()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreak_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState74()
        {
            string[] actions = new string[] { "SyncSetStop_0", "SyncWaitBreak_1", };
            string[] verifications = new string[] { "ExceptionalVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = true,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState75()
        {
            string[] actions = new string[] { "SyncWaitBreak_0", "SyncSetBreak_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = false,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }

        [Fact]
        [OuterLoop]
        public static void ParallelState76()
        {
            string[] actions = new string[] { "SyncWaitBreak_0", "SyncSetBreak_1", };
            string[] verifications = new string[] { "BreakVerification", };
            TestParameters parameters = new TestParameters
            {
                Api = API.ForeachOnList,
                Count = 10,
                Actions = actions,
                Verifications = verifications,
                ExpectingException = false,
                WithLocalState = true,
                Is64 = false,
            };
            ParallelStateTest test = new ParallelStateTest(parameters);
            test.RealRun();
        }
    }
}
