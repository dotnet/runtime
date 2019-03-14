// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Interop;
using Internal.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace System.Threading.Tasks
{
    /// <summary>Provides a bridge between IAsyncOperation* and Task.</summary>
    /// <typeparam name="TResult">Specifies the type of the result of the asynchronous operation.</typeparam>
    internal sealed class AsyncInfoToTaskBridge<TResult> : TaskCompletionSource<TResult>
    {
        /// <summary>The CancellationToken associated with this operation.</summary>
        private readonly CancellationToken _ct;

        /// <summary>A registration for cancellation that needs to be disposed of when the operation completes.</summary>
        private CancellationTokenRegistration _ctr;

        /// <summary>A flag set to true as soon as the completion callback begins to execute.</summary>
        private bool _completing;

        internal AsyncInfoToTaskBridge(CancellationToken cancellationToken)
        {
            if (AsyncCausalitySupport.LoggingOn)
                AsyncCausalitySupport.TraceOperationCreation(this.Task, "WinRT Operation as Task");
            AsyncCausalitySupport.AddToActiveTasks(this.Task);

            _ct = cancellationToken;
        }

        /// <summary>The synchronization object to use for protecting the state of this bridge.</summary>
        private object StateLock
        {
            get { return this; }  // "this" isn't available publicly, so we can safely use it as a syncobj
        }

        /// <summary>Registers the async operation for cancellation.</summary>
        /// <param name="asyncInfo">The asynchronous operation.</param>
        internal void RegisterForCancellation(IAsyncInfo asyncInfo)
        {
            Debug.Assert(asyncInfo != null);

            try
            {
                if (_ct.CanBeCanceled && !_completing)
                { // benign race on m_completing... it's ok if it's not up-to-date.
                    var ctr = _ct.Register(ai => ((IAsyncInfo)ai).Cancel(), asyncInfo); // delegate cached by compiler

                    // The operation may already be completing by this time, in which case
                    // we might need to dispose of our new cancellation registration here.
                    bool disposeOfCtr = false;
                    lock (StateLock)
                    {
                        if (_completing) disposeOfCtr = true;
                        else _ctr = ctr; // under lock to avoid torn writes
                    }

                    if (disposeOfCtr)
                        ctr.Unregister();
                }
            }
            catch (Exception ex)
            {
                // We do not want exceptions propagating out of the AsTask / GetAwaiter calls, as the
                // Completed handler will instead store the exception into the returned Task.
                // Such exceptions should cause the Completed handler to be invoked synchronously and thus the Task should already be completed.

                if (!base.Task.IsFaulted)
                {
                    Debug.Fail($"Expected base task to already be faulted but found it in state {base.Task.Status}");
                    base.TrySetException(ex);
                }
            }
        }


        /// <summary>Bridge to Completed handler on IAsyncAction.</summary>
        internal void CompleteFromAsyncAction(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, null, asyncStatus);
        }


        /// <summary>Bridge to Completed handler on IAsyncActionWithProgress{TProgress}.</summary>
        /// <typeparam name="TProgress">Specifies the type of progress notification data.</typeparam>
        internal void CompleteFromAsyncActionWithProgress<TProgress>(IAsyncActionWithProgress<TProgress> asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, null, asyncStatus);
        }


        /// <summary>Bridge to Completed handler on IAsyncOperation{TResult}.</summary>
        internal void CompleteFromAsyncOperation(IAsyncOperation<TResult> asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, ai => ((IAsyncOperation<TResult>)ai).GetResults(), asyncStatus); // delegate cached by compiler
        }


        /// <summary>Bridge to Completed handler on IAsyncOperationWithProgress{TResult,TProgress}.</summary>
        /// <typeparam name="TProgress">Specifies the type of progress notification data.</typeparam>
        internal void CompleteFromAsyncOperationWithProgress<TProgress>(IAsyncOperationWithProgress<TResult, TProgress> asyncInfo, AsyncStatus asyncStatus)
        {
            // delegate cached by compiler:
            Complete(asyncInfo, ai => ((IAsyncOperationWithProgress<TResult, TProgress>)ai).GetResults(), asyncStatus);
        }


        /// <summary>Completes the task from the completed asynchronous operation.</summary>
        /// <param name="asyncInfo">The asynchronous operation.</param>
        /// <param name="getResultsFunction">A function used to retrieve the TResult from the async operation; may be null.</param>
        /// <param name="asyncStatus">The status of the asynchronous operation.</param>
        private void Complete(IAsyncInfo asyncInfo, Func<IAsyncInfo, TResult> getResultsFunction, AsyncStatus asyncStatus)
        {
            if (asyncInfo == null)
                throw new ArgumentNullException(nameof(asyncInfo));

            AsyncCausalitySupport.RemoveFromActiveTasks(this.Task);

            try
            {
                Debug.Assert(asyncInfo.Status == asyncStatus,
                                "asyncInfo.Status does not match asyncStatus; are we dealing with a faulty IAsyncInfo implementation?");

                // Assuming a correct underlying implementation, the task should not have been
                // completed yet.  If it is completed, we shouldn't try to do any further work
                // with the operation or the task, as something is horked.
                bool taskAlreadyCompleted = Task.IsCompleted;

                Debug.Assert(!taskAlreadyCompleted, "Expected the task to not yet be completed.");

                if (taskAlreadyCompleted)
                    throw new InvalidOperationException(SR.InvalidOperation_InvalidAsyncCompletion);

                // Clean up our registration with the cancellation token, noting that we're now in the process of cleaning up.
                CancellationTokenRegistration ctr;
                lock (StateLock)
                {
                    _completing = true;
                    ctr = _ctr; // under lock to avoid torn reads
                    _ctr = default(CancellationTokenRegistration);
                }
                ctr.Unregister(); // It's ok if we end up unregistering a not-initialized registration; it'll just be a nop.

                try
                {
                    // Find out how the async operation completed.  It must be in a terminal state.
                    bool terminalState = asyncStatus == AsyncStatus.Completed
                                            || asyncStatus == AsyncStatus.Canceled
                                            || asyncStatus == AsyncStatus.Error;

                    Debug.Assert(terminalState, "The async operation should be in a terminal state.");

                    if (!terminalState)
                        throw new InvalidOperationException(SR.InvalidOperation_InvalidAsyncCompletion);

                    // Retrieve the completion data from the IAsyncInfo.
                    TResult result = default(TResult);
                    Exception error = null;
                    if (asyncStatus == AsyncStatus.Error)
                    {
                        error = asyncInfo.ErrorCode;

                        // Defend against a faulty IAsyncInfo implementation:
                        if (error == null)
                        {
                            Debug.Fail("IAsyncInfo.Status == Error, but ErrorCode returns a null Exception (implying S_OK).");
                            error = new InvalidOperationException(SR.InvalidOperation_InvalidAsyncCompletion);
                        }
                        else
                        {
                            error = asyncInfo.ErrorCode.AttachRestrictedErrorInfo();
                        }
                    }
                    else if (asyncStatus == AsyncStatus.Completed && getResultsFunction != null)
                    {
                        try
                        {
                            result = getResultsFunction(asyncInfo);
                        }
                        catch (Exception resultsEx)
                        {
                            // According to the WinRT team, this can happen in some egde cases, such as marshalling errors in GetResults.
                            error = resultsEx;
                            asyncStatus = AsyncStatus.Error;
                        }
                    }

                    // Nothing to retrieve for a canceled operation or for a completed operation with no result.

                    // Complete the task based on the previously retrieved results:
                    bool success = false;
                    switch (asyncStatus)
                    {
                        case AsyncStatus.Completed:
                            if (AsyncCausalitySupport.LoggingOn)
                                AsyncCausalitySupport.TraceOperationCompletedSuccess(this.Task);
                            success = base.TrySetResult(result);
                            break;

                        case AsyncStatus.Error:
                            Debug.Assert(error != null, "The error should have been retrieved previously.");
                            success = base.TrySetException(error);
                            break;

                        case AsyncStatus.Canceled:
                            success = base.TrySetCanceled(_ct.IsCancellationRequested ? _ct : new CancellationToken(true));
                            break;
                    }

                    Debug.Assert(success, "Expected the outcome to be successfully transfered to the task.");
                }
                catch (Exception exc)
                {
                    // This really shouldn't happen, but could in a variety of misuse cases
                    // such as a faulty underlying IAsyncInfo implementation.
                    Debug.Fail($"Unexpected exception in Complete: {exc}");

                    if (AsyncCausalitySupport.LoggingOn)
                        AsyncCausalitySupport.TraceOperationCompletedError(this.Task);

                    // For these cases, store the exception into the task so that it makes its way
                    // back to the caller.  Only if something went horribly wrong and we can't store the exception
                    // do we allow it to be propagated out to the invoker of the Completed handler.
                    if (!base.TrySetException(exc))
                    {
                        Debug.Fail("The task was already completed and thus the exception couldn't be stored.");
                        throw;
                    }
                }
            }
            finally
            {
                // We may be called on an STA thread which we don't own, so make sure that the RCW is released right
                // away. Otherwise, if we leave it up to the finalizer, the apartment may already be gone.
                if (Marshal.IsComObject(asyncInfo))
                    Marshal.ReleaseComObject(asyncInfo);
            }
        }  // private void Complete(..)
    }  // class AsyncInfoToTaskBridge<TResult, TProgress>
}  // namespace
