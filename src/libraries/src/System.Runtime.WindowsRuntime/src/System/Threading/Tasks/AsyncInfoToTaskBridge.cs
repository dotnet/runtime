// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using System.Runtime.WindowsRuntime.Internal;

using Internal.Threading.Tasks;
using Internal.Interop;

namespace System.Threading.Tasks
{
    /// <summary>Provides a bridge between IAsyncOperation* and Task.</summary>
    /// <typeparam name="TResult">Specifies the type of the result of the asynchronous operation.</typeparam>
    /// <typeparam name="TProgress">Specifies the type of progress notification data.</typeparam>
    internal sealed class AsyncInfoToTaskBridge<TResult> : TaskCompletionSource<TResult>
    {
        /// <summary>The CancellationToken associated with this operation.</summary>
        private readonly CancellationToken m_ct;

        /// <summary>A registration for cancellation that needs to be disposed of when the operation completes.</summary>
        private CancellationTokenRegistration m_ctr;

        /// <summary>A flag set to true as soon as the completion callback begins to execute.</summary>
        private bool m_completing;

        internal AsyncInfoToTaskBridge(CancellationToken cancellationToken)
        {
            if (AsyncCausalitySupport.LoggingOn)
                AsyncCausalitySupport.TraceOperationCreation(this.Task, "WinRT Operation as Task");
            AsyncCausalitySupport.AddToActiveTasks(this.Task);

            m_ct = cancellationToken;
        }

        /// <summary>The synchronization object to use for protecting the state of this bridge.</summary>
        private object StateLock
        {
            get { return this; }  // "this" isn't available publicly, so we can safely use it as a syncobj
        }

        /// <summary>Registers the async operation for cancellation.</summary>
        /// <param name="asyncInfo">The asynchronous operation.</param>
        /// <param name="cancellationToken">The token used to request cancellation of the asynchronous operation.</param>
        internal void RegisterForCancellation(IAsyncInfo asyncInfo)
        {
            Contract.Requires(asyncInfo != null);

            try
            {
                if (m_ct.CanBeCanceled && !m_completing)
                { // benign race on m_completing... it's ok if it's not up-to-date.
                    var ctr = m_ct.Register(ai => ((IAsyncInfo)ai).Cancel(), asyncInfo); // delegate cached by compiler

                    // The operation may already be completing by this time, in which case
                    // we might need to dispose of our new cancellation registration here.
                    bool disposeOfCtr = false;
                    lock (StateLock)
                    {
                        if (m_completing) disposeOfCtr = true;
                        else m_ctr = ctr; // under lock to avoid torn writes
                    }

                    if (disposeOfCtr)
                        ctr.TryDeregister();
                }
            }
            catch (Exception ex)
            {
                // We do not want exceptions propagating out of the AsTask / GetAwaiter calls, as the
                // Completed handler will instead store the exception into the returned Task.
                // Such exceptions should cause the Completed handler to be invoked synchronously and thus the Task should already be completed.

                if (!base.Task.IsFaulted)
                {
                    Contract.Assert(false, String.Format("Expected base task to already be faulted but found it in state {0}", base.Task.Status));
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
                throw new ArgumentNullException("asyncInfo");

            Contract.EndContractBlock();

            try
            {
                Contract.Assert(asyncInfo.Status == asyncStatus,
                                "asyncInfo.Status does not match asyncStatus; are we dealing with a faulty IAsyncInfo implementation?");

                // Assuming a correct underlying implementation, the task should not have been
                // completed yet.  If it is completed, we shouldn't try to do any further work
                // with the operation or the task, as something is horked.
                bool taskAlreadyCompleted = Task.IsCompleted;

                Contract.Assert(!taskAlreadyCompleted, "Expected the task to not yet be completed.");

                if (taskAlreadyCompleted)
                    throw new InvalidOperationException(SR.InvalidOperation_InvalidAsyncCompletion);

                // Clean up our registration with the cancellation token, noting that we're now in the process of cleaning up.
                CancellationTokenRegistration ctr;
                lock (StateLock)
                {
                    m_completing = true;
                    ctr = m_ctr; // under lock to avoid torn reads
                    m_ctr = default(CancellationTokenRegistration);
                }
                ctr.TryDeregister(); // It's ok if we end up unregistering a not-initialized registration; it'll just be a nop.

                try
                {
                    // Find out how the async operation completed.  It must be in a terminal state.
                    bool terminalState = asyncStatus == AsyncStatus.Completed
                                            || asyncStatus == AsyncStatus.Canceled
                                            || asyncStatus == AsyncStatus.Error;

                    Contract.Assert(terminalState, "The async operation should be in a terminal state.");

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
                            Contract.Assert(false, "IAsyncInfo.Status == Error, but ErrorCode returns a null Exception (implying S_OK).");
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
                            Contract.Assert(error != null, "The error should have been retrieved previously.");
                            success = base.TrySetException(error);
                            break;

                        case AsyncStatus.Canceled:
                            success = base.TrySetCanceled(m_ct.IsCancellationRequested ? m_ct : new CancellationToken(true));
                            break;
                    }

                    Contract.Assert(success, "Expected the outcome to be successfully transfered to the task.");
                }
                catch (Exception exc)
                {
                    // This really shouldn't happen, but could in a variety of misuse cases
                    // such as a faulty underlying IAsyncInfo implementation.                
                    Contract.Assert(false, string.Format("Unexpected exception in Complete: {0}", exc.ToString()));

                    if (AsyncCausalitySupport.LoggingOn)
                        AsyncCausalitySupport.TraceOperationCompletedError(this.Task);

                    // For these cases, store the exception into the task so that it makes its way
                    // back to the caller.  Only if something went horribly wrong and we can't store the exception
                    // do we allow it to be propagated out to the invoker of the Completed handler.
                    if (!base.TrySetException(exc))
                    {
                        Contract.Assert(false, "The task was already completed and thus the exception couldn't be stored.");
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