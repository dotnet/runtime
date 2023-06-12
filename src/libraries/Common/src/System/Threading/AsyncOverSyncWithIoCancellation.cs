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
    internal sealed class AsyncOverSyncWithIoCancellation
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

        /// <summary>Prevent external instantiation.</summary>
        private AsyncOverSyncWithIoCancellation() { }

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
            // Queue the work to complete asynchronously.
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            // Register for cancellation, perform the work, and clean up. Even though we're in an async method, awaits _must not_ be used inside
            // the using block, or else the I/O cancellation could both not work and negatively interact with I/O on another thread.  The func
            // _must_ be invoked on the same thread that invoked RegisterCancellation, with no intervening work.
            SyncAsyncWorkItemRegistration reg = RegisterCancellation(cancellationToken);
            try
            {
                action(state);
            }
            catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested && oce.CancellationToken != cancellationToken)
            {
                throw CreateAppropriateCancellationException(cancellationToken, oce);
            }
            finally
            {
                await reg.DisposeAsync().ConfigureAwait(false);
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
            // Queue the work to complete asynchronously.
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            // Register for cancellation, perform the work, and clean up. Even though we're in an async method, awaits _must not_ be used inside
            // the using block, or else the I/O cancellation could both not work and negatively interact with I/O on another thread.  The func
            // _must_ be invoked on the same thread that invoked RegisterCancellation, with no intervening work.
            SyncAsyncWorkItemRegistration reg = RegisterCancellation(cancellationToken);
            try
            {
                return func(state);
            }
            catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested && oce.CancellationToken != cancellationToken)
            {
                throw CreateAppropriateCancellationException(cancellationToken, oce);
            }
            finally
            {
                await reg.DisposeAsync().ConfigureAwait(false);
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

        /// <summary>The struct IDisposable returned from RegisterCancellation in order to clean up after the registration.</summary>
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
        private static SyncAsyncWorkItemRegistration RegisterCancellation(CancellationToken cancellationToken) =>
            cancellationToken.CanBeCanceled ? RegisterCancellation(new AsyncOverSyncWithIoCancellation(), cancellationToken) :
            default; // If the token can't be canceled, there's nothing to register.

        /// <summary>Registers for cancellation with the specified token.</summary>
        /// <remarks>Upon cancellation being requested, the implementation will attempt to CancelSynchronousIo for the thread calling RegisterCancellation.</remarks>
        private static SyncAsyncWorkItemRegistration RegisterCancellation(AsyncOverSyncWithIoCancellation instance, CancellationToken cancellationToken)
        {
            // Get a handle for the current thread. This is stored and used to cancel the I/O on this thread
            // in response to the cancellation token having cancellation requested.  If the handle is invalid,
            // which could happen if OpenThread fails, skip attempts at cancellation. The handle needs to be
            // opened with THREAD_TERMINATE in order to be able to call CancelSynchronousIo.
            instance.ThreadHandle = t_currentThreadHandle;
            if (instance.ThreadHandle is null)
            {
                instance.ThreadHandle = Interop.Kernel32.OpenThread(Interop.Kernel32.THREAD_TERMINATE, bInheritHandle: false, Interop.Kernel32.GetCurrentThreadId());
                if (instance.ThreadHandle.IsInvalid)
                {
                    int lastError = Marshal.GetLastPInvokeError();
                    Debug.Fail($"{nameof(Interop.Kernel32.OpenThread)} unexpectedly failed with 0x{lastError:X8}: {Marshal.GetPInvokeErrorMessage(lastError)}");
                    return default;
                }

                t_currentThreadHandle = instance.ThreadHandle;
            }

            // Register with the token.
            SyncAsyncWorkItemRegistration reg = default;
            reg.WorkItem = instance;
            reg.CancellationRegistration = cancellationToken.UnsafeRegister(static s =>
            {
                var instance = (AsyncOverSyncWithIoCancellation)s!;

                // If cancellation was already requested when UnsafeRegister was called, it'll invoke
                // the callback immediately.  If we allowed that to loop until cancellation was successful,
                // we'd deadlock, as we'd never perform the very I/O it was waiting for.  As such, if
                // the callback is invoked prior to be ready for it, we ignore the callback.
                if (!instance.FinishedCancellationRegistration)
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
                instance.CallbackCompleted = Task.Factory.StartNew(static s =>
                {
                    var instance = (AsyncOverSyncWithIoCancellation)s!;

                    // Cancel the I/O.  If the cancellation happens too early and we haven't yet initiated
                    // the synchronous operation, CancelSynchronousIo will fail with ERROR_NOT_FOUND, and
                    // we'll loop to try again.
                    SpinWait sw = default;
                    while (instance.ContinueTryingToCancel)
                    {
                        if (Interop.Kernel32.CancelSynchronousIo(instance.ThreadHandle!))
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
                }, instance, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }, instance);

            // Now that we've registered with the token, tell the callback it's safe to enter
            // its cancellation loop if the callback is invoked.
            instance.FinishedCancellationRegistration = true;

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
