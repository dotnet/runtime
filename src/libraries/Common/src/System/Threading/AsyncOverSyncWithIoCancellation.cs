// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// Helper for performing asynchronous I/O on Windows implemented as queueing a work item that performs synchronous I/O, complete with cancellation support.
    /// </summary>
    internal sealed class AsyncOverSyncWithIoCancellation : IThreadPoolWorkItem, ICriticalNotifyCompletion
    {
        /// <summary>A thread handle for the current OS thread.</summary>
        /// <remarks>This is lazily-initialized for the current OS thread. We rely on finalization to clean up after it when the thread goes away.</remarks>
        [ThreadStatic]
        private static SafeThreadHandle? t_currentThreadHandle;

        /// <summary>The OS handle of the thread performing the I/O.</summary>
        private SafeThreadHandle? ThreadHandle;
        /// <summary>Whether the call to CancellationToken.UnsafeRegister completed.</summary>
        private volatile bool FinishedCancellationRegistration;
        /// <summary>Whether the I/O operation has finished (successfully or unsuccessfully) and is requesting cancellation attempts stop.</summary>
        private volatile bool ContinueTryingToCancel = true;
        /// <summary>
        /// A task that may be checked after the <see cref="CancellationTokenRegistration"/> has been disposed.  If it's null at that point,
        /// the callback wasn't and will never be invoked.  If it's non-null, its completion represents the completion of the asynchronous callback.
        /// </summary>
        private volatile Task? CallbackCompleted;
        /// <summary>The <see cref="Action"/> continuation object handed to this instance when used as an awaiter to scheduler work to the thread pool.</summary>
        private Action? _continuation;

        // awaitable / awaiter implementation that enables this instance to be awaited in order to queue
        // execution to the thread pool.  This is purely a cost-saving measure in order to reuse this
        // object we already need as the queued work item.
        public AsyncOverSyncWithIoCancellation GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => throw new NotSupportedException();
        public void UnsafeOnCompleted(Action continuation)
        {
            Debug.Assert(_continuation is null);
            _continuation = continuation;
            ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
        }
        void IThreadPoolWorkItem.Execute() => _continuation!();

        /// <summary>Queues the invocation of <paramref name="action"/> to the thread pool.</summary>
        /// <typeparam name="TState">The type of the state passed to <paramref name="action"/>.</typeparam>
        /// <param name="action">The action to invoke asynchronously.</param>
        /// <param name="state">The state to pass to the action.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to register with to cancel the synchronous I/O performed by <paramref name="action"/>.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// The implementation will queue the invocation of the <paramref name="action"/> to the thread pool, with
        /// the returned <see cref="ValueTask"/> representing its completion.  If the <paramref name="cancellationToken"/> has
        /// cancellation requested, the implementation will attempt to use CancelSynchronousIo to cancel any I/O being
        /// performed by the function.
        /// </remarks>
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        public static async ValueTask InvokeAsync<TState>(Action<TState> action, TState state, CancellationToken cancellationToken)
        {
            // Create the work item state object.  This is used to pass around state through various APIs,
            // while also serving double duty as the work item used to queue the operation to the thread pool.
            var workItem = new AsyncOverSyncWithIoCancellation();

            // Queue the work to the thread pool.  This is implemented as a custom awaiter that queues the
            // awaiter itself to the thread pool.
            await workItem;

            // Register for cancellation, perform the work, and clean up. Even though we're in an async method, awaits _must not_ be used inside
            // the using block, or else the I/O cancellation could both not work and negatively interact with I/O on another thread.  The func
            // _must_ be invoked on the same thread that invoked RegisterCancellation, with no intervening work.
            await using (workItem.RegisterCancellation(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    action(state);
                }
                catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested && oce.CancellationToken != cancellationToken)
                {
                    throw CreateAppropriateCancellationException(cancellationToken, oce);
                }
            }
        }

        /// <summary>Queues the invocation of <paramref name="func"/> to the thread pool.</summary>
        /// <typeparam name="TState">The type of the state passed to <paramref name="func"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result from <paramref name="func"/>.</typeparam>
        /// <param name="func">The function to invoke asynchronously.</param>
        /// <param name="state">The state to pass to the function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to register with to cancel the synchronous I/O performed by <paramref name="func"/>.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        /// <remarks>
        /// The implementation will queue the invocation of the <paramref name="func"/> to the thread pool, with
        /// the returned <see cref="ValueTask"/> representing its completion.  If the <paramref name="cancellationToken"/> has
        /// cancellation requested, the implementation will attempt to use CancelSynchronousIo to cancel any I/O being
        /// performed by the function.
        /// </remarks>
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public static async ValueTask<TResult> InvokeAsync<TState, TResult>(Func<TState, TResult> func, TState state, CancellationToken cancellationToken)
        {
            // Create the work item state object.  This is used to pass around state through various APIs,
            // while also serving double duty as the work item used to queue the operation to the thread pool.
            var workItem = new AsyncOverSyncWithIoCancellation();

            // Queue the work to the thread pool.  This is implemented as a custom awaiter that queues the
            // awaiter itself to the thread pool.
            await workItem;

            // Register for cancellation, perform the work, and clean up. Even though we're in an async method, awaits _must not_ be used inside
            // the using block, or else the I/O cancellation could both not work and negatively interact with I/O on another thread.  The func
            // _must_ be invoked on the same thread that invoked RegisterCancellation, with no intervening work.
            await using (workItem.RegisterCancellation(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    return func(state);
                }
                catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested && oce.CancellationToken != cancellationToken)
                {
                    throw CreateAppropriateCancellationException(cancellationToken, oce);
                }
            }
        }

        /// <summary>Translates an <see cref="OperationCanceledException"/> that's not associated with the relevant <see cref="CancellationToken"/> to one that is.</summary>
        private static OperationCanceledException CreateAppropriateCancellationException(CancellationToken cancellationToken, OperationCanceledException originalOce)
        {
            Debug.Assert(cancellationToken.IsCancellationRequested && originalOce.CancellationToken != cancellationToken);

            // If the operation fails because of cancellation, make sure it contains this cancellation token
            // if this cancellation token could have been the cause.
            var newOce = new OperationCanceledException(cancellationToken);
            if (originalOce.StackTrace is string stackTrace)
            {
                ExceptionDispatchInfo.SetRemoteStackTrace(newOce, stackTrace);
            }

            return newOce;
        }

        /// <summary>The struct IDisposable returned from <see cref="RegisterCancellation"/> in order to clean up after the registration.</summary>
        private struct SyncAsyncWorkItemRegistration : IDisposable, IAsyncDisposable
        {
            public AsyncOverSyncWithIoCancellation WorkItem;
            public CancellationTokenRegistration CancellationRegistration;

            /// <summary>Waits for any pending cancellation callback to complete and cleans up resources.</summary>
            public void Dispose()
            {
                if (WorkItem is null)
                {
                    return;
                }

                // Prior to calling Dispose on the CancellationTokenRegistration, we need to tell
                // the registration callback to exit if it's currently running; otherwise, we could deadlock.
                WorkItem.ContinueTryingToCancel = false;

                // Then we need to dispose of the registration.  Upon Dispose returning, we know that
                // either the synchronous invocation of the callback completed or that the callback
                // will never be invoked.
                CancellationRegistration.Dispose();

                // Now that we know the synchronous callback has quiesced, check to see whether it scheduled
                // asynchronous work.  If it did, wait for that work to complete.
                WorkItem.CallbackCompleted?.GetAwaiter().GetResult();
            }

            /// <summary>Asynchronously waits for any pending cancellation callback to complete and cleans up resources.</summary>
            public async ValueTask DisposeAsync()
            {
                if (WorkItem is null)
                {
                    return;
                }

                // Prior to calling Dispose on the CancellationTokenRegistration, we need to tell
                // the registration callback to exit if it's currently running; otherwise, we could deadlock.
                WorkItem.ContinueTryingToCancel = false;

                // Then we need to dispose of the registration.  Upon Dispose returning, we know that
                // either the synchronous invocation of the callback completed or that the callback
                // will never be invoked.
                await CancellationRegistration.DisposeAsync().ConfigureAwait(false);

                // Now that we know the synchronous callback has quiesced, check to see whether it scheduled
                // asynchronous work.  If it did, wait for that work to complete.
                if (WorkItem.CallbackCompleted is Task t)
                {
                    await t.ConfigureAwait(false);
                }
            }
        }

        /// <summary>Registers for cancellation with the specified token.</summary>
        /// <remarks>Upon cancellation being requested, the implementation will attempt to CancelSynchronousIo for the thread calling RegisterCancellation.</remarks>
        private SyncAsyncWorkItemRegistration RegisterCancellation(CancellationToken cancellationToken)
        {
            // If the token can't be canceled, there's nothing to register.
            if (!cancellationToken.CanBeCanceled)
            {
                return default;
            }

            // Get a handle for the current thread. This is stored and used to cancel the I/O on this thread
            // in response to the cancellation token having cancellation requested.  If the handle is invalid,
            // which could happen if OpenThread fails, skip attempts at cancellation. The handle needs to be
            // opened with THREAD_TERMINATE in order to be able to call CancelSynchronousIo.
            ThreadHandle = t_currentThreadHandle;
            if (ThreadHandle is null)
            {
                ThreadHandle = Interop.Kernel32.OpenThread(Interop.Kernel32.THREAD_TERMINATE, bInheritHandle: false, Interop.Kernel32.GetCurrentThreadId());
                if (ThreadHandle.IsInvalid)
                {
                    int lastError = Marshal.GetLastPInvokeError();
                    Debug.Fail($"{nameof(Interop.Kernel32.OpenThread)} unexpectedly failed with 0x{lastError:X8}: {Marshal.GetPInvokeErrorMessage(lastError)}");
                    return default;
                }

                t_currentThreadHandle = ThreadHandle;
            }

            // Register with the token.
            SyncAsyncWorkItemRegistration reg = default;
            reg.WorkItem = this;
            reg.CancellationRegistration = cancellationToken.UnsafeRegister(static s =>
            {
                var state = (AsyncOverSyncWithIoCancellation)s!;

                // If cancellation was already requested when UnsafeRegister was called, it'll invoke
                // the callback immediately.  If we allowed that to loop until cancellation was successful,
                // we'd deadlock, as we'd never perform the very I/O it was waiting for.  As such, if
                // the callback is invoked prior to be ready for it, we ignore the callback.
                if (!state.FinishedCancellationRegistration)
                {
                    return;
                }

                // In the rare situation where between registration with the token and invocation of the I/O
                // cancellation is requested, we need to loop until the I/O happens; otherwise, we could try
                // to cancel it too early and miss it.  However, if such looping takes too long, it could end
                // up blocking the thread invoking CancellationTokenSource.Cancel.  Thus, rather than doing
                // this looping synchronously, we instead queue the invocation of the looping so that it
                // runs asynchronously from the Cancel call.  Then in order to be able to track its completion,
                // we store the Task representing that asynchronous work, such that cleanup can wait for the Task.
                state.CallbackCompleted = Task.Factory.StartNew(static s =>
                {
                    var state = (AsyncOverSyncWithIoCancellation)s!;

                    // Cancel the I/O.  If the cancellation happens too early and we haven't yet initiated
                    // the synchronous operation, CancelSynchronousIo will fail with ERROR_NOT_FOUND, and
                    // we'll loop to try again.
                    SpinWait sw = default;
                    while (state.ContinueTryingToCancel)
                    {
                        if (Interop.Kernel32.CancelSynchronousIo(state.ThreadHandle!))
                        {
                            // Successfully canceled I/O.
                            break;
                        }

                        if (Marshal.GetLastPInvokeError() != Interop.Errors.ERROR_NOT_FOUND)
                        {
                            // Failed to cancel even though there may have been I/O to cancel.
                            // Attempting to keep trying could result in an infinite loop, so
                            // give up on trying to cancel.
                            break;
                        }

                        sw.SpinOnce();
                    }
                }, s, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }, this);

            // Now that we've registered with the token, tell the callback it's safe to enter
            // its cancellation loop if the callback is invoked.
            FinishedCancellationRegistration = true;

            // And now since cancellation may have been requested and we may have suppressed it
            // until the previous line, check to see if cancellation has now been requested, and
            // if it has, stop any callback, remove the registration, and throw.
            if (cancellationToken.IsCancellationRequested)
            {
                reg.Dispose();
                throw new OperationCanceledException(cancellationToken);
            }

            // Return the registration.  Now and moving forward, a cancellation request could come in,
            // and the callback will end up spinning until we reach the actual I/O.
            return reg;
        }
    }
}
