// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>Provides internal helper methods for implementing channels.</summary>
    internal static class ChannelUtilities
    {
        /// <summary>Sentinel object used to indicate being done writing.</summary>
        internal static readonly Exception s_doneWritingSentinel = new Exception(nameof(s_doneWritingSentinel));
        /// <summary>A cached task with a Boolean true result.</summary>
        internal static readonly Task<bool> s_trueTask = Task.FromResult(result: true);
        /// <summary>A cached task with a Boolean false result.</summary>
        internal static readonly Task<bool> s_falseTask = Task.FromResult(result: false);
        /// <summary>A cached task that never completes.</summary>
        internal static readonly Task s_neverCompletingTask = new TaskCompletionSource<bool>().Task;

        /// <summary>Completes the specified TaskCompletionSource.</summary>
        /// <param name="tcs">The source to complete.</param>
        /// <param name="error">
        /// The optional exception with which to complete.  
        /// If this is null or the DoneWritingSentinel, the source will be completed successfully.
        /// If this is an OperationCanceledException, it'll be completed with the exception's token.
        /// Otherwise, it'll be completed as faulted with the exception.
        /// </param>
        internal static void Complete(TaskCompletionSource<VoidResult> tcs, Exception error = null)
        {
            if (error is OperationCanceledException oce)
            {
                tcs.TrySetCanceled(oce.CancellationToken);
            }
            else if (error != null && error != s_doneWritingSentinel)
            {
                tcs.TrySetException(error);
            }
            else
            {
                tcs.TrySetResult(default);
            }
        }

        /// <summary>Wake up all of the waiters and null out the field.</summary>
        /// <param name="waiters">The waiters.</param>
        /// <param name="result">The value with which to complete each waiter.</param>
        internal static void WakeUpWaiters(ref ReaderInteractor<bool> waiters, bool result)
        {
            ReaderInteractor<bool> w = waiters;
            if (w != null)
            {
                w.Success(result);
                waiters = null;
            }
        }

        /// <summary>Wake up all of the waiters and null out the field.</summary>
        /// <param name="waiters">The waiters.</param>
        /// <param name="result">The success value with which to complete each waiter if <paramref name="error">error</paramref> is null.</param>
        /// <param name="error">The failure with which to cmplete each waiter, if non-null.</param>
        internal static void WakeUpWaiters(ref ReaderInteractor<bool> waiters, bool result, Exception error = null)
        {
            ReaderInteractor<bool> w = waiters;
            if (w != null)
            {
                if (error != null)
                {
                    w.Fail(error);
                }
                else
                {
                    w.Success(result);
                }
                waiters = null;
            }
        }

        /// <summary>Removes all interactors from the queue, failing each.</summary>
        /// <param name="interactors">The queue of interactors to complete.</param>
        /// <param name="error">The error with which to complete each interactor.</param>
        internal static void FailInteractors<T, TInner>(Dequeue<T> interactors, Exception error) where T : Interactor<TInner>
        {
            Debug.Assert(error != null);
            while (!interactors.IsEmpty)
            {
                interactors.DequeueHead().Fail(error);
            }
        }

        /// <summary>Gets or creates a "waiter" (e.g. WaitForRead/WriteAsync) interactor.</summary>
        /// <param name="waiter">The field storing the waiter interactor.</param>
        /// <param name="runContinuationsAsynchronously">true to force continuations to run asynchronously; otherwise, false.</param>
        /// <param name="cancellationToken">The token to use to cancel the wait.</param>
        internal static Task<bool> GetOrCreateWaiter(ref ReaderInteractor<bool> waiter, bool runContinuationsAsynchronously, CancellationToken cancellationToken)
        {
            // Get the existing waiters interactor.
            ReaderInteractor<bool> w = waiter;

            // If there isn't one, create one.  This explicitly does not include the cancellation token,
            // as we reuse it for any number of waiters that overlap.
            if (w == null)
            {
                waiter = w = ReaderInteractor<bool>.Create(runContinuationsAsynchronously);
            }

            // If the cancellation token can't be canceled, then just return the waiter task.
            // If it can, we need to return a task that will complete when the waiter task does but that can also be canceled.
            // Easiest way to do that is with a cancelable continuation.
            return cancellationToken.CanBeCanceled ?
                w.Task.ContinueWith(t => t.Result, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default) :
                w.Task;
        }

        /// <summary>Creates and returns an exception object to indicate that a channel has been closed.</summary>
        internal static Exception CreateInvalidCompletionException(Exception inner = null) =>
            inner is OperationCanceledException ? inner :
            inner != null && inner != s_doneWritingSentinel ? new ChannelClosedException(inner) :
            new ChannelClosedException();
    }
}
