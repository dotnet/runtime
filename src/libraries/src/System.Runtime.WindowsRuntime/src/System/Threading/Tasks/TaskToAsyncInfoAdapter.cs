// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Implements a wrapper that allows to expose managed <code>System.Threading.Tasks.Task</code> objects as
    /// through the WinRT <code>Windows.Foundation.IAsyncInfo</code> interface.
    /// </summary>
    internal class TaskToAsyncInfoAdapter<TCompletedHandler, TProgressHandler, TResult, TProgressInfo>
                                                                                : IAsyncInfo, IProgress<TProgressInfo>
                                                                                where TCompletedHandler : class
                                                                                where TProgressHandler : class
    {
        #region Private Types, Statics and Constants

        // ! THIS DIAGRAM ILLUSTRATES THE CONSTANTS BELOW. UPDATE THIS IF UPDATING THE CONSTANTS BELOW!:
        //     3         2         1         0
        //    10987654321098765432109876543210
        //    X...............................   Reserved such that we can use Int32 and not worry about negative-valued state constants
        //    ..X.............................   STATEFLAG_COMPLETED_SYNCHRONOUSLY
        //    ...X............................   STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET
        //    ....X...........................   STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED
        //    ................................   STATE_NOT_INITIALIZED
        //    ...............................X   STATE_STARTED
        //    ..............................X.   STATE_RUN_TO_COMPLETION
        //    .............................X..   STATE_CANCELLATION_REQUESTED
        //    ............................X...   STATE_CANCELLATION_COMPLETED
        //    ...........................X....   STATE_ERROR
        //    ..........................X.....   STATE_CLOSED
        //    ..........................XXXXXX   STATEMASK_SELECT_ANY_ASYNC_STATE
        //    XXXXXXXXXXXXXXXXXXXXXXXXXX......   STATEMASK_CLEAR_ALL_ASYNC_STATES
        //     3         2         1         0
        //    10987654321098765432109876543210

        // These STATE_XXXX constants describe the async state of this object.
        // Objects of this type are in exactly in one of these states at any given time:
        private const int STATE_NOT_INITIALIZED = 0;   // 0x00
        private const int STATE_STARTED = 1;   // 0x01
        private const int STATE_RUN_TO_COMPLETION = 2;   // 0x02
        private const int STATE_CANCELLATION_REQUESTED = 4;   // 0x04
        private const int STATE_CANCELLATION_COMPLETED = 8;   // 0x08
        private const int STATE_ERROR = 16;  // 0x10
        private const int STATE_CLOSED = 32;  // 0x20

        // The STATEFLAG_XXXX constants can be bitmasked with the states to describe additional
        // state info that cannot be easily inferred from the async state:
        private const int STATEFLAG_COMPLETED_SYNCHRONOUSLY = 0x20000000;
        private const int STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET = 0x10000000;
        private const int STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED = 0x8000000;

        // These two masks are used to select any STATE_XXXX bits and clear all other (i.e. STATEFLAG_XXXX) bits.
        // It is set to (next power of 2 after the largest STATE_XXXX value) - 1.
        // !!! Make sure to update this if a new STATE_XXXX value is added above !!
        private const int STATEMASK_SELECT_ANY_ASYNC_STATE = (64 - 1);

        // These two masks are used to clear all STATE_XXXX bits and leave any STATEFLAG_XXXX bits.
        private const int STATEMASK_CLEAR_ALL_ASYNC_STATES = ~STATEMASK_SELECT_ANY_ASYNC_STATE;


        private static InvalidOperationException CreateCannotGetResultsFromIncompleteOperationException(Exception cause)
        {
            InvalidOperationException ex = (cause == null)
                            ? new InvalidOperationException(SR.InvalidOperation_CannotGetResultsFromIncompleteOperation)
                            : new InvalidOperationException(SR.InvalidOperation_CannotGetResultsFromIncompleteOperation, cause);
            ex.SetErrorCode(__HResults.E_ILLEGAL_METHOD_CALL);
            return ex;
        }

        #endregion Private Types, Statics and Constants


        #region Instance variables

        /// <summary>The token source used to cancel running operations.</summary>
        private CancellationTokenSource _cancelTokenSource = null;

        /// <summary>The async info's ID. InvalidAsyncId stands for not yet been initialised.</summary>
        private uint _id = AsyncInfoIdGenerator.InvalidId;

        /// <summary>The cached error code used to avoid creating several exception objects if the <code>ErrorCode</code>
        /// property is accessed several times. <code>null</code> indicates either no error or that <code>ErrorCode</code>
        /// has not yet been called.</summary>
        private Exception _error = null;

        /// <summary>The state of the async info. Interlocked operations are used to manipulate this field.</summary>
        private volatile int _state = STATE_NOT_INITIALIZED;

        /// <summary>For IAsyncInfo instances that completed synchronously (at creation time) this field holds the result;
        /// for instances backed by an actual Task, this field holds a reference to the task generated by the task generator.
        /// Since we always know which of the above is the case, we can always cast this field to TResult in the former case
        /// or to one of Task or Task{TResult} in the latter case. This approach allows us to save a field on all IAsyncInfos.
        /// Notably, this makes us pay the added cost of boxing for synchronously completing IAsyncInfos where TResult is a
        /// value type, however, this is expected to occur rather rare compared to non-synchronously completed user-IAsyncInfos.</summary>
        private object _dataContainer;

        /// <summary>Registered completed handler.</summary>
        private TCompletedHandler _completedHandler;

        /// <summary>Registered progress handler.</summary>
        private TProgressHandler _progressHandler;

        /// <summary>The synchronization context on which this instance was created/started. Used to callback invocations.</summary>
        private SynchronizationContext _startingContext;

        #endregion Instance variables


        #region Constructors and Destructor

        /// <summary>Creates an IAsyncInfo from the specified delegate. The delegate will be called to construct a task that will
        /// represent the future encapsulated by this IAsyncInfo.</summary>
        /// <param name="taskProvider">The task generator to use for creating the task.</param>
        internal TaskToAsyncInfoAdapter(Delegate taskProvider)
        {
            Debug.Assert(taskProvider != null);
            Debug.Assert((null != (taskProvider as Func<Task>))
                            || (null != (taskProvider as Func<CancellationToken, Task>))
                            || (null != (taskProvider as Func<IProgress<TProgressInfo>, Task>))
                            || (null != (taskProvider as Func<CancellationToken, IProgress<TProgressInfo>, Task>)));

            // The IAsyncInfo is reasonably expected to be created/started by the same code that wires up the Completed and Progress handlers.
            // Record the current SynchronizationContext so that we can invoke completion and progress callbacks in it later.
            _startingContext = GetStartingContext();

            // Construct task from the specified provider:
            Task task = InvokeTaskProvider(taskProvider);

            if (task == null)
                throw new NullReferenceException(SR.NullReference_TaskProviderReturnedNull);

            if (task.Status == TaskStatus.Created)
                throw new InvalidOperationException(SR.InvalidOperation_TaskProviderReturnedUnstartedTask);

            _dataContainer = task;
            _state = (STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED | STATE_STARTED);

            // Set the completion routine and let the task running:
            task.ContinueWith(
                (_, this_) => ((TaskToAsyncInfoAdapter<TCompletedHandler, TProgressHandler, TResult, TProgressInfo>)this_).TaskCompleted(),
                this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }


        /// <summary>
        /// Creates an IAsyncInfo from the Task object. The specified task represents the future encapsulated by this IAsyncInfo.
        /// The specified CancellationTokenSource and Progress are assumed to be the source of the specified Task's cancellation and
        /// the Progress that receives reports from the specified Task.
        /// </summary>
        /// <param name="underlyingTask">The Task whose operation is represented by this IAsyncInfo</param>
        /// <param name="underlyingCancelTokenSource">The cancellation control for the cancellation token observed
        /// by <code>underlyingTask</code>.</param>
        /// <param name="underlyingProgressDispatcher">A progress listener/pugblisher that receives progress notifications
        /// form <code>underlyingTask</code>.</param>
        internal TaskToAsyncInfoAdapter(Task underlyingTask,
                                        CancellationTokenSource underlyingCancelTokenSource, Progress<TProgressInfo> underlyingProgressDispatcher)
        {
            if (underlyingTask == null)
                throw new ArgumentNullException(nameof(underlyingTask));

            // Throw InvalidOperation and not Argument for parity with the constructor that takes Delegate taskProvider:
            if (underlyingTask.Status == TaskStatus.Created)
                throw new InvalidOperationException(SR.InvalidOperation_UnstartedTaskSpecified);

            // The IAsyncInfo is reasonably expected to be created/started by the same code that wires up the Completed and Progress handlers.
            // Record the current SynchronizationContext so that we can invoke completion and progress callbacks in it later.
            _startingContext = GetStartingContext();

            // We do not need to invoke any delegates to get the task, it is provided for us:
            _dataContainer = underlyingTask;

            // This must be the cancellation source for the token that the specified underlyingTask observes for cancellation:
            // (it may also be null in cases where the specified underlyingTask does nto support cancellation)
            _cancelTokenSource = underlyingCancelTokenSource;

            // Iff the specified underlyingTask reports progress, chain the reports to this IAsyncInfo's reporting method:
            if (underlyingProgressDispatcher != null)
                underlyingProgressDispatcher.ProgressChanged += OnReportChainedProgress;

            _state = (STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED | STATE_STARTED);

            underlyingTask.ContinueWith(
                (_, this_) => ((TaskToAsyncInfoAdapter<TCompletedHandler, TProgressHandler, TResult, TProgressInfo>)this_).TaskCompleted(),
                this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }


        /// <summary>
        /// Creates an IAsyncInfo from the specified result value. The IAsyncInfo is created in the Completed state and the
        /// specified <code>synchronousResult</code> is used as the result value.
        /// </summary>
        /// <param name="synchronousResult">The result of this synchronously completed IAsyncInfo.</param>
        internal TaskToAsyncInfoAdapter(TResult synchronousResult)
        {
            // We already completed. There will be no progress callback invokations and a potential completed handler invokation will be synchronous.
            // We do not need the starting SynchronizationContext:
            _startingContext = null;

            // Set the synchronous result:
            _dataContainer = synchronousResult;

            // CompletedSynchronously + MustRunCompletionHandleImmediatelyWhenSet + CompletionHandlerNotYetInvoked + RUN_TO_COMPLETION:
            // (same state as assigned by DangerousSetCompleted())
            _state = (STATEFLAG_COMPLETED_SYNCHRONOUSLY
                          | STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET
                          | STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED
                          | STATE_RUN_TO_COMPLETION);
        }


        ~TaskToAsyncInfoAdapter()
        {
            TransitionToClosed();
        }

        #endregion Constructors and Destructor


        #region Synchronous completion controls

        /// <summary> This method sets the result on a *synchronously completed* IAsyncInfo.
        /// It does not try to deal with the inherit races: Use it only when constructing a synchronously
        /// completed IAsyncInfo in a desired state when you understand the threading conditions well.</summary>
        /// <param name="synchronousResult">The new result of this synchronously completed IAsyncInfo (may be <code>default(TResult)</code>)</param>
        /// <returns>FALSE if this IAsyncInfo has not actually completed synchronously and this method had no effects, TRUE otherwise.</returns>
        internal bool DangerousSetCompleted(TResult synchronousResult)
        {
            if (!CompletedSynchronously)
                return false;

            _dataContainer = synchronousResult;
            _error = null;

            // CompletedSynchronously + MustRunCompletionHandleImmediatelyWhenSet + CompletionHandlerNotYetInvoked + RUN_TO_COMPLETION:
            _state = (STATEFLAG_COMPLETED_SYNCHRONOUSLY
                          | STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET
                          | STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED
                          | STATE_RUN_TO_COMPLETION);
            return true;
        }


        internal bool DangerousSetCanceled()
        {
            if (!CompletedSynchronously)
                return false;

            // Here we do not try to deal with the inherit races: Use this method only when constructing a synchronously
            // completed IAsyncInfo in a desired state when you understand the threading conditions well.

            _dataContainer = null;
            _error = null;

            // CompletedSynchronously + MustRunCompletionHandleImmediatelyWhenSet + CompletionHandlerNotYetInvoked + CANCELLATION_COMPLETED:
            _state = (STATEFLAG_COMPLETED_SYNCHRONOUSLY
                          | STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET
                          | STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED
                          | STATE_CANCELLATION_COMPLETED);
            return true;
        }


        internal bool DangerousSetError(Exception error)
        {
            if (!CompletedSynchronously)
                return false;

            if (error == null)
                throw new ArgumentNullException(nameof(error));

            // Here we do not try to deal with the inherit races: Use this method only when constructing a synchronously
            // completed IAsyncInfo in a desired state when you understand the threading conditions well.

            _dataContainer = null;
            _error = error;

            // CompletedSynchronously + MustRunCompletionHandleImmediatelyWhenSet + CompletionHandlerNotYetInvoked + ERROR:
            _state = (STATEFLAG_COMPLETED_SYNCHRONOUSLY
                          | STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET
                          | STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED
                          | STATE_ERROR);
            return true;
        }

        #endregion Synchronous completion controls


        #region State bit field operations

        internal bool CompletedSynchronously {[Pure] get { return (0 != (_state & STATEFLAG_COMPLETED_SYNCHRONOUSLY)); } }

        private bool IsInStartedState {[Pure] get { return (0 != (_state & STATE_STARTED)); } }

        private bool IsInRunToCompletionState {[Pure] get { return (0 != (_state & STATE_RUN_TO_COMPLETION)); } }

        private bool IsInErrorState {[Pure] get { return (0 != (_state & STATE_ERROR)); } }

        private bool IsInClosedState {[Pure] get { return (0 != (_state & STATE_CLOSED)); } }

        private bool IsInRunningState
        {
            [Pure]
            get
            {
                return (0 != (_state & (STATE_STARTED
                                       | STATE_CANCELLATION_REQUESTED)));
            }
        }

        private bool IsInTerminalState
        {
            [Pure]
            get
            {
                return (0 != (_state & (STATE_RUN_TO_COMPLETION
                                       | STATE_CANCELLATION_COMPLETED
                                       | STATE_ERROR)));
            }
        }

        [Pure]
        private bool CheckUniqueAsyncState(int state)
        {
            unchecked
            {
                uint asyncState = (uint)state;
                return (asyncState & (~asyncState + 1)) == asyncState; // This checks if asyncState is 0 or a power of 2.
            }
        }

        #endregion State bit field operations


        #region Infrastructure methods

        private SynchronizationContext GetStartingContext()
        {
#if DESKTOP // as a reminder that on most platforms we want a different behavior
            return SynchronizationContext.CurrentNoFlow;
#else
            return SynchronizationContext.Current;
#endif
        }


        internal Task Task
        {
            get
            {
                EnsureNotClosed();

                if (CompletedSynchronously)
                    return null;

                return (Task)_dataContainer;
            }
        }


        internal CancellationTokenSource CancelTokenSource
        {
            get { return _cancelTokenSource; }
        }


        [Pure]
        internal void EnsureNotClosed()
        {
            if (!IsInClosedState)
                return;

            ObjectDisposedException ex = new ObjectDisposedException(SR.ObjectDisposed_AsyncInfoIsClosed);
            ex.SetErrorCode(__HResults.E_ILLEGAL_METHOD_CALL);
            throw ex;
        }


        internal virtual void OnCompleted(TCompletedHandler userCompletionHandler, AsyncStatus asyncStatus)
        {
            Debug.Fail("This (sub-)type of IAsyncInfo does not support completion notifications "
                                 + " (" + this.GetType().ToString() + ")");
        }


        internal virtual void OnProgress(TProgressHandler userProgressHandler, TProgressInfo progressInfo)
        {
            Debug.Fail("This (sub-)type of IAsyncInfo does not support progress notifications "
                                 + " (" + this.GetType().ToString() + ")");
        }


        private void OnCompletedInvoker(AsyncStatus status)
        {
            bool conditionFailed;

            // Get the handler:
            TCompletedHandler handler = Volatile.Read(ref _completedHandler);

            // If we might not run the handler now, we need to remember that if it is set later, it will need to be run then:
            if (handler == null)
            {
                // Remember to run the handler when it is set:
                SetState(STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET, ~STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET,
                            conditionBitMask: 0, useCondition: false, conditionFailed: out conditionFailed);

                // The handler may have been set concurrently before we managed to SetState, so check for it again:
                handler = Volatile.Read(ref _completedHandler);

                // If handler was not set cuncurrently after all, then no worries:
                if (handler == null)
                    return;
            }

            // This method might be running cuncurrently. Create a block by emulating an interlocked un-set of
            // the STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED-bit in the m_state bit field. Only the thread that wins the race
            // for unsetting this bit, wins, others give up:
            SetState(0, ~STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED,
                        conditionBitMask: STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED, useCondition: true, conditionFailed: out conditionFailed);

            if (conditionFailed)
                return;

            // Invoke the user handler:
            OnCompleted(handler, status);
        }


        // This is a separate method from IProgress<TProgressInfo>.Report to avoid alocating the closure if it is not used.
        private void OnProgressInvokerCrossContext(TProgressHandler handler, TProgressInfo progressInfo)
        {
            Debug.Assert(handler != null);
            Debug.Assert(_startingContext != null);

            _startingContext.Post((tupleObject) =>
            {
                var tuple = (Tuple<TaskToAsyncInfoAdapter<TCompletedHandler, TProgressHandler, TResult, TProgressInfo>,
                                   TProgressHandler,
                                   TProgressInfo>)tupleObject;

                tuple.Item1.OnProgress(tuple.Item2, tuple.Item3);
            }, Tuple.Create(this, handler, progressInfo));
        }


        /// <summary>Reports a progress update.</summary>
        /// <param name="value">The new progress value to report.</param>
        void IProgress<TProgressInfo>.Report(TProgressInfo value)
        {
            // If no progress handler is set, there is nothing to do:
            TProgressHandler handler = Volatile.Read(ref _progressHandler);
            if (handler == null)
                return;

            // Try calling progress handler in the right synchronization context.
            // If the user callback throws an exception, it will bubble up through here and reach the
            // user worker code running as this async future. The user should catch it.
            // If the user does not catch it, it will be treated just as any other exception coming from the async execution code:
            // this AsyncInfo will be faulted.

            if (_startingContext == null)
            {
                // The starting context is null, invoke directly:
                OnProgress(handler, value);
            }
            else
            {
                // Invoke callback in the right context:
                OnProgressInvokerCrossContext(handler, value);
            }
        }


        private void OnReportChainedProgress(object sender, TProgressInfo progressInfo)
        {
            ((IProgress<TProgressInfo>)this).Report(progressInfo);
        }


        /// <summary>
        /// Sets the <code>m_state</code> bit field to reflect the specified async state with the corresponding STATE_XXX bit mask.
        /// </summary>
        /// <param name="newAsyncState">Must be one of the STATE_XXX (not STATEYYY_ZZZ !) constants defined in this class.</param>
        /// <param name="conditionBitMask">If <code>useCondition</code> is FALSE: this field is ignored.
        ///                                If <code>useCondition</code> is TRUE: Unless this value has at least one bit with <code>m_state</code> in
        ///                                                                      common, this method will not perform any action.</param>
        /// <param name="useCondition">If TRUE, use <code>conditionBitMask</code> to determine whether the state should be set;
        ///                            If FALSE, ignore <code>conditionBitMask</code>.</param>
        /// <param name="conditionFailed">If <code>useCondition</code> is FALSE: this field is set to FALSE;
        ///                               If <code>useCondition</code> is TRUE: this field indicated whether the specified <code>conditionBitMask</code>
        ///                                                                     had at least one bit in common with <code>m_state</code> (TRUE)
        ///                                                                     or not (FALSE).
        ///                               (!) Note that the meaning of this parameter to the caller is not quite the same as whether <code>m_state</code>
        ///                               is/was set to the specified value, because <code>m_state</code> may already have had the specified value, or it
        ///                               may be set and then immediately changed by another thread. The true meaning of this parameter is whether or not
        ///                               the specified condition did hold before trying to change the state.</param>
        /// <returns>The value at which the current invocation of this method left <code>m_state</code>.</returns>
        private int SetAsyncState(int newAsyncState, int conditionBitMask, bool useCondition, out bool conditionFailed)
        {
            Debug.Assert(CheckUniqueAsyncState(newAsyncState & STATEMASK_SELECT_ANY_ASYNC_STATE));
            Debug.Assert(CheckUniqueAsyncState(_state & STATEMASK_SELECT_ANY_ASYNC_STATE));

            int resultState = SetState(newAsyncState, STATEMASK_CLEAR_ALL_ASYNC_STATES, conditionBitMask, useCondition, out conditionFailed);
            Debug.Assert(CheckUniqueAsyncState(resultState & STATEMASK_SELECT_ANY_ASYNC_STATE));

            return resultState;
        }


        /// <summary>
        /// Sets the specified bits in the <code>m_state</code> bit field according to the specified bit-mask parameters.
        /// </summary>
        /// <param name="newStateSetMask">The bits to turn ON in the <code>m_state</code> bit field</param>
        /// <param name="newStateIgnoreMask">Any bits that are OFF in this value will get turned OFF,
        ///                                  unless they are explicitly switched on by <code>newStateSetMask</code>.</param>
        /// <param name="conditionBitMask">If <code>useCondition</code> is FALSE: this field is ignored.
        ///                                If <code>useCondition</code> is TRUE: Unless this value has at least one bit with <code>m_state</code> in
        ///                                                                      common, this method will not perform any action.</param>
        /// <param name="useCondition">If TRUE, use <code>conditionBitMask</code> to determine whether the state should be set;
        ///                            If FALSE, ignore <code>conditionBitMask</code>.</param>
        /// <param name="conditionFailed">If <code>useCondition</code> is FALSE: this field is set to FALSE;
        ///                               If <code>useCondition</code> is TRUE: this field indicated whether the specified <code>conditionBitMask</code>
        ///                                                                     had at least one bit in common with <code>m_state</code> (TRUE)
        ///                                                                     or not (FALSE).
        ///                               (!) Note that the meaning of this parameter to the caller is not quite the same as whether <code>m_state</code>
        ///                               is/was set to the specified value, because <code>m_state</code> may already have had the specified value, or it
        ///                               may be set and then immediately changed by another thread. The true meaning of this parameter is whether or not
        ///                               the specified condition did hold before trying to change the state.</param>
        /// <returns>The value at which the current invocation of this method left <code>m_state</code>.</returns>
        private int SetState(int newStateSetMask, int newStateIgnoreMask, int conditionBitMask, bool useCondition, out bool conditionFailed)
        {
            int origState = _state;

            if (useCondition && 0 == (origState & conditionBitMask))
            {
                conditionFailed = true;
                return origState;
            }

            int newState = (origState & newStateIgnoreMask) | newStateSetMask;
            int prevState = Interlocked.CompareExchange(ref _state, newState, origState);

            // If m_state changed concurrently, we want to make sure that the change being made is based on a bitmask that is up to date:
            // (this relies of the fact that all state machines that save their state in m_state have no cycles)
            while (true)
            {
                if (prevState == origState)
                {
                    conditionFailed = false;
                    return newState;
                }

                origState = _state;

                if (useCondition && 0 == (origState & conditionBitMask))
                {
                    conditionFailed = true;
                    return origState;
                }

                newState = (origState & newStateIgnoreMask) | newStateSetMask;
                prevState = Interlocked.CompareExchange(ref _state, newState, origState);
            }
        }


        private int TransitionToTerminalState()
        {
            Debug.Assert(IsInRunningState);
            Debug.Assert(!CompletedSynchronously);

            Task task = _dataContainer as Task;
            Debug.Assert(task != null);
            Debug.Assert(task.IsCompleted);

            // Recall that STATE_CANCELLATION_REQUESTED and STATE_CANCELLATION_COMPLETED both map to the public CANCELED state.
            // So, we are STARTED or CANCELED. We will ask the task how it completed and possibly transition out of CANCELED.
            // This may happen if cancellation was requested while in STARTED state, but the task does not support cancellation,
            // or if it can support cancellation in principle, but the Cancel request came in while still STARTED, but after the
            // last opportunity to cancel.
            // If the underlying operation was not able to react to the cancellation request and instead either run to completion
            // or faulted, then the state will transition into COMPLETED or ERROR accordingly. If the operation was really cancelled,
            // the state will remain CANCELED.

            // If the switch below defaults, we have an erroneous implementation.
            int terminalAsyncState = STATE_ERROR;

            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    terminalAsyncState = STATE_RUN_TO_COMPLETION;
                    break;

                case TaskStatus.Canceled:
                    terminalAsyncState = STATE_CANCELLATION_COMPLETED;
                    break;

                case TaskStatus.Faulted:
                    terminalAsyncState = STATE_ERROR;
                    break;

                default:
                    Debug.Fail("Unexpected task.Status: It should be terminal if TaskCompleted() is called.");
                    break;
            }

            bool ignore;
            int newState = SetAsyncState(terminalAsyncState,
                                           conditionBitMask: STATEMASK_SELECT_ANY_ASYNC_STATE, useCondition: true, conditionFailed: out ignore);

            Debug.Assert((newState & STATEMASK_SELECT_ANY_ASYNC_STATE) == terminalAsyncState);
            Debug.Assert((_state & STATEMASK_SELECT_ANY_ASYNC_STATE) == terminalAsyncState || IsInClosedState,
                            "We must either be in a state we just entered or we were concurrently closed");

            return newState;
        }


        private void TaskCompleted()
        {
            int terminalState = TransitionToTerminalState();
            Debug.Assert(IsInTerminalState);

            // We transitioned into a terminal state, so it became legal to close us concurrently.
            // So we use data from this stack and not m_state to get the completion status.
            // On this code path we will also fetch m_completedHandler, however that race is benign because in CLOSED the handler
            // can only change to null, so it won't be invoked, which is appropriate for CLOSED.
            AsyncStatus terminationStatus = GetStatus(terminalState);

            // Try calling completed handler in the right synchronization context.
            // If the user callback throws an exception, it will bubble up through here.
            // If we let it though, it will be caught and swallowed by the Task subsystem, which is just below us on the stack.
            // Instead we follow the same pattern as Task and other parallel libs and re-throw the excpetion on the threadpool
            // to ensure a diagnostic message and a fail-fast-like teardown.
            try
            {
                if (_startingContext == null)
                {
                    // The starting context is null, invoking directly:
                    OnCompletedInvoker(terminationStatus);
                }
                else
                {
                    // Invoke callback in the right context (delegate cached by compiler):
                    _startingContext.Post((tupleObject) =>
                    {
                        var tuple = (Tuple<TaskToAsyncInfoAdapter<TCompletedHandler, TProgressHandler, TResult, TProgressInfo>, AsyncStatus>)tupleObject;
                        tuple.Item1.OnCompletedInvoker(tuple.Item2);
                    }, Tuple.Create(this, terminationStatus));
                }
            }
            catch (Exception ex)
            {
                ExceptionDispatchHelper.ThrowAsync(ex, _startingContext);
            }
        }


        private AsyncStatus GetStatus(int state)
        {
            int asyncState = state & STATEMASK_SELECT_ANY_ASYNC_STATE;
            Debug.Assert(CheckUniqueAsyncState(asyncState));

            switch (asyncState)
            {
                case STATE_NOT_INITIALIZED:
                    Debug.Fail("STATE_NOT_INITIALIZED should only occur when this object was not"
                                         + " fully constructed, in which case we should never get here");
                    return AsyncStatus.Error;

                case STATE_STARTED:
                    return AsyncStatus.Started;

                case STATE_RUN_TO_COMPLETION:
                    return AsyncStatus.Completed;

                case STATE_CANCELLATION_REQUESTED:
                case STATE_CANCELLATION_COMPLETED:
                    return AsyncStatus.Canceled;

                case STATE_ERROR:
                    return AsyncStatus.Error;

                case STATE_CLOSED:
                    Debug.Fail("This method should never be called is this IAsyncInfo is CLOSED");
                    return AsyncStatus.Error;
            }

            Debug.Fail("The switch above is missing a case");
            return AsyncStatus.Error;
        }


        internal TResult GetResultsInternal()
        {
            EnsureNotClosed();

            // If this IAsyncInfo has actually faulted, GetResults will throw the same error as returned by ErrorCode:
            if (IsInErrorState)
            {
                Exception error = ErrorCode;
                Debug.Assert(error != null);
                ExceptionDispatchInfo.Capture(error).Throw();
            }

            // IAsyncInfo throws E_ILLEGAL_METHOD_CALL when called in a state other than COMPLETED:
            if (!IsInRunToCompletionState)
                throw CreateCannotGetResultsFromIncompleteOperationException(null);


            // If this is a synchronous operation, use the cached result:
            if (CompletedSynchronously)
                return (TResult)_dataContainer;

            // The operation is asynchronous:
            Task<TResult> task = _dataContainer as Task<TResult>;

            // Since CompletedSynchronously is false and EnsureNotClosed() did not throw, task can only be null if:
            //  - this IAsyncInfo has completed synchronously, however we checked for this above;
            //  - it was not converted to Task<TResult>, which means it is a non-generic Task. In that case we cannot get a result from Task.
            if (task == null)
                return default(TResult);

            Debug.Assert(IsInRunToCompletionState);

            // Pull out the task result and return.
            // Any exceptions thrown in the task will be rethrown.
            // If this exception is a cancelation exception, meaning there was actually no error except for being cancelled,
            // return an error code appropriate for WinRT instead (InvalidOperation with E_ILLEGAL_METHOD_CALL).
            try
            {
                return task.GetAwaiter().GetResult();
            }
            catch (TaskCanceledException tcEx)
            {
                throw CreateCannotGetResultsFromIncompleteOperationException(tcEx);
            }
        }


        private Task InvokeTaskProvider(Delegate taskProvider)
        {
            var funcVoidTask = taskProvider as Func<Task>;
            if (funcVoidTask != null)
            {
                return funcVoidTask();
            }

            var funcCTokTask = taskProvider as Func<CancellationToken, Task>;
            if (funcCTokTask != null)
            {
                _cancelTokenSource = new CancellationTokenSource();
                return funcCTokTask(_cancelTokenSource.Token);
            }

            var funcIPrgrTask = taskProvider as Func<IProgress<TProgressInfo>, Task>;
            if (funcIPrgrTask != null)
            {
                return funcIPrgrTask(this);
            }

            var funcCTokIPrgrTask = taskProvider as Func<CancellationToken, IProgress<TProgressInfo>, Task>;
            if (funcCTokIPrgrTask != null)
            {
                _cancelTokenSource = new CancellationTokenSource();
                return funcCTokIPrgrTask(_cancelTokenSource.Token, this);
            }

            Debug.Fail("We should never get here!"
                                 + " Public methods creating instances of this class must be typesafe to ensure that taskProvider"
                                 + " can always be cast to one of the above Func types."
                                 + " The taskProvider is " + (taskProvider == null
                                                                    ? "null."
                                                                    : "a " + taskProvider.GetType().ToString()) + ".");
            return null;
        }


        private void TransitionToClosed()
        {
            // From the finaliser we always call this Close version since finalisation can happen any time, even when STARTED (e.g. process ends)
            // and we do not want to throw in those cases.

            // Always go to closed, even from STATE_NOT_INITIALIZED.
            // Any checking whether it is legal to call CLosed inthe current state, should occur in Close().
            bool ignore;
            SetAsyncState(STATE_CLOSED, 0, useCondition: false, conditionFailed: out ignore);

            _cancelTokenSource = null;
            _dataContainer = null;
            _error = null;
            _completedHandler = null;
            _progressHandler = null;
            _startingContext = null;
        }

        #endregion Infrastructure methods


        #region Implementation of IAsyncInfo

        /// <summary>
        /// Gets or sets the completed handler.
        ///
        /// We will set the completion handler even when this IAsyncInfo is already started (no other choice).
        /// If we the completion handler is set BEFORE this IAsyncInfo completed, then the handler will be called upon completion as normal.
        /// If we the completion handler is set AFTER this IAsyncInfo already completed, then this setter will invoke the handler synchronously
        /// on the current context.
        /// </summary>
        public virtual TCompletedHandler Completed
        {
            get
            {
                TCompletedHandler handler = Volatile.Read(ref _completedHandler);
                EnsureNotClosed();
                return handler;
            }

            set
            {
                EnsureNotClosed();

                // Try setting completion handler, but only if this has not yet been done:
                // (Note: We allow setting Completed to null multiple times iff it has not yet been set to anything else than null.
                //  Some other WinRT projection languages do not allow setting the Completed handler more than once, even if it is set to null.
                //  We could do the same by introducing a new STATEFLAG_COMPLETION_HNDL_SET bit-flag constant and saving a this state into
                //  the m_state field to indicate that the completion handler has been set previously, but we choose not to do this.)
                TCompletedHandler handlerBefore = Interlocked.CompareExchange(ref _completedHandler, value, null);
                if (handlerBefore != null)
                {
                    InvalidOperationException ex = new InvalidOperationException(SR.InvalidOperation_CannotSetCompletionHanlderMoreThanOnce);
                    ex.SetErrorCode(__HResults.E_ILLEGAL_DELEGATE_ASSIGNMENT);
                    throw ex;
                }

                if (value == null)
                    return;

                // If STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET is OFF then we are done (i.e. no need to invoke the handler synchronously)
                if (0 == (_state & STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET))
                    return;

                // We have changed the handler and at some point this IAsyncInfo may have transitioned to the Closed state.
                // This is OK, but if this happened we need to ensure that we only leave a null handler behind:
                if (IsInClosedState)
                {
                    Interlocked.Exchange(ref _completedHandler, null);
                    return;
                }

                // The STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET-flag was set, so we need to call the completion handler now:
                Debug.Assert(IsInTerminalState);
                OnCompletedInvoker(Status);
            }
        }


        /// <summary>Gets or sets the progress handler.</summary>
        public virtual TProgressHandler Progress
        {
            get
            {
                TProgressHandler handler = Volatile.Read(ref _progressHandler);
                EnsureNotClosed();

                return handler;
            }

            set
            {
                EnsureNotClosed();

                Interlocked.Exchange(ref _progressHandler, value);

                // We transitioned into CLOSED after the above check, we will need to null out m_progressHandler:
                if (IsInClosedState)
                    Interlocked.Exchange(ref _progressHandler, null);
            }
        }


        /// <summary>Cancels the async info.</summary>
        public virtual void Cancel()
        {
            // Cancel will be ignored in any terminal state including CLOSED.
            // In other words, it is ignored in any state except STARTED.

            bool stateWasNotStarted;
            SetAsyncState(STATE_CANCELLATION_REQUESTED, conditionBitMask: STATE_STARTED, useCondition: true, conditionFailed: out stateWasNotStarted);

            if (!stateWasNotStarted)
            {  // i.e. if state was different from STATE_STARTED:
                if (_cancelTokenSource != null)
                    _cancelTokenSource.Cancel();
            }
        }


        /// <summary>Close the async info.</summary>
        public virtual void Close()
        {
            if (IsInClosedState)
                return;

            // Cannot Close from a non-terminal state:
            if (!IsInTerminalState)
            {
                // If we are STATE_NOT_INITIALIZED, the we probably threw from the ctor.
                // The finalizer will be called anyway and we need to free this partially constructed object correctly.
                // So we avoid throwing when we are in STATE_NOT_INITIALIZED.
                // In other words throw only if *some* async state is set:
                if (0 != (_state & STATEMASK_SELECT_ANY_ASYNC_STATE))
                {
                    InvalidOperationException ex = new InvalidOperationException(SR.InvalidOperation_IllegalStateChange);
                    ex.SetErrorCode(__HResults.E_ILLEGAL_STATE_CHANGE);
                    throw ex;
                }
            }

            TransitionToClosed();
        }


        /// <summary>Gets the error code for the async info.</summary>
        public virtual Exception ErrorCode
        {
            get
            {
                EnsureNotClosed();

                // If the task is faulted, hand back an HR representing its first exception.
                // Otherwise, hand back S_OK (which is a null Exception).

                if (!IsInErrorState)
                    return null;

                Exception error = Volatile.Read(ref _error);

                // ERROR is a terminal state. SO if we have an error, just return it.
                // If we completed synchronously, we return the current error iven if it is null since we do not expect this to change:
                if (error != null || CompletedSynchronously)
                    return error;

                Task task = _dataContainer as Task;
                Debug.Assert(task != null);

                AggregateException aggregateException = task.Exception;

                // By spec, if task.IsFaulted is true, then task.Exception must not be null and its InnerException must
                // also not be null. However, in case something is unexpected on the Task side of the road, let?s be defensive
                // instead of failing with an inexplicable NullReferenceException:

                if (aggregateException == null)
                {
                    error = new Exception(SR.WinRtCOM_Error);
                    error.SetErrorCode(__HResults.E_FAIL);
                }
                else
                {
                    Exception innerException = aggregateException.InnerException;

                    error = (innerException == null)
                                ? aggregateException
                                : innerException;
                }

                // If m_error was set concurrently, setError will be non-null. Then we use that - as it is the first m_error
                // that was set. If setError we know that we won any races and we can return error:
                Exception setError = Interlocked.CompareExchange(ref _error, error, null);
                return setError ?? error;
            }
        }


        public virtual uint Id
        {
            get
            {
                EnsureNotClosed();

                if (_id != AsyncInfoIdGenerator.InvalidId)
                    return _id;

                return AsyncInfoIdGenerator.EnsureInitializedThreadsafe(ref _id);
            }
        }


        /// <summary>Gets the status of the async info.</summary>
        public virtual AsyncStatus Status
        {
            get
            {
                EnsureNotClosed();
                return GetStatus(_state);
            }
        }
        #endregion Implementation of IAsyncInfo
    }  // class TaskToAsyncInfoAdapter<TCompletedHandler, TProgressHandler, TResult, TProgressInfo>
}  // namespace

// TaskToAsyncInfoAdapter.cs
