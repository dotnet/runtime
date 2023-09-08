// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// Helper for performing asynchronous I/O on Windows implemented as queueing a work item that performs synchronous I/O, complete with cancellation support.
    /// </summary>
    internal sealed class AsyncOverSyncWithIoCancellation
    {
        /// <summary>The <see cref="AsyncOverSyncWithIoCancellation"/> for the current thread.</summary>
        /// <remarks>
        /// The safety of caching this on the current thread is based on the fact that all work performed
        /// is synchronous on this thread.  If a cancellation callback occurs on a different thread, that
        /// could be using this instance, but only between the time that the operation is initiated on this
        /// thread and disposal completes on this thread.
        /// </remarks>
        [ThreadStatic]
        private static AsyncOverSyncWithIoCancellation? t_instance;

        /// <summary>The OS handle of the thread performing the I/O.</summary>
        /// <remarks>This is stored as part of the object's construction because the objects are thread affinitized.</remarks>
        private readonly SafeThreadHandle? _threadHandle;

        /// <summary>Whether the call to CancellationToken.UnsafeRegister completed.</summary>
        private bool _finishedCancellationRegistration;
        /// <summary>Whether the I/O operation has finished (successfully or unsuccessfully) and is requesting cancellation attempts stop.</summary>
        private bool _continueTryingToCancel;
        /// <summary>
        /// A task that may be checked after the <see cref="CancellationTokenRegistration"/> has been disposed.  If it's null at that point,
        /// the callback wasn't and will never be invoked.  If it's non-null, its completion represents the completion of the asynchronous callback.
        /// </summary>
        private Task? _callbackCompleted;

        /// <summary>Initialize the instance.  This should be done once per thread.</summary>
        private AsyncOverSyncWithIoCancellation()
        {
            // Get a handle for the current thread. This is stored and used to cancel the I/O on this thread
            // in response to the cancellation token having cancellation requested.  If the handle is invalid,
            // which could happen if OpenThread fails, skip attempts at cancellation. The handle needs to be
            // opened with THREAD_TERMINATE in order to be able to call CancelSynchronousIo.
            SafeThreadHandle handle = Interop.Kernel32.OpenThread(Interop.Kernel32.THREAD_TERMINATE, bInheritHandle: false, Interop.Kernel32.GetCurrentThreadId());
            if (!handle.IsInvalid)
            {
                _threadHandle = handle;
            }
            else
            {
#if DEBUG
                int lastError = Marshal.GetLastPInvokeError();
                Debug.Fail($"{nameof(Interop.Kernel32.OpenThread)} unexpectedly failed with 0x{lastError:X8}: {Marshal.GetPInvokeErrorMessage(lastError)}");
#endif
                handle.Dispose();
            }
        }

        /// <summary>Resets this instance's state to be ready for another use on this thread.</summary>
        private void Reset()
        {
            _finishedCancellationRegistration = false;
            _continueTryingToCancel = true;
            _callbackCompleted = null;
        }

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
            // Queue the work to complete asynchronously. Logically, this is just queueing a work item to the thread pool.
            // We use a ForceYielding awaiter in combination with the PoolingAsyncValueTaskMethodBuilder to reduce allocation.
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            // Register for cancellation, perform the work, and clean up. Even though we're in an async method, awaits _must not_ be used
            // after this point, or else the I/O cancellation could both not work and negatively interact with I/O on another thread.
            // The func _must_ be invoked on the same thread that invoked RegisterCancellation, with no intervening work.
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
                reg.Dispose();
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
            // Queue the work to complete asynchronously. Logically, this is just queueing a work item to the thread pool.
            // We use a ForceYielding awaiter in combination with the PoolingAsyncValueTaskMethodBuilder to reduce allocation.
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            // Register for cancellation, perform the work, and clean up. Even though we're in an async method, awaits _must not_ be used
            // after this point, or else the I/O cancellation could both not work and negatively interact with I/O on another thread.
            // The func _must_ be invoked on the same thread that invoked RegisterCancellation, with no intervening work.
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
                reg.Dispose();
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
        /// <remarks>
        /// This does not implement IAsyncDisposable, even though async disposal could await both cancellation registration disposal
        /// and the callback completion.  By only supporting synchronous disposal, we can ensure that all relevant work happens
        /// on the calling thread, which in turn allows us to use a per-thread singleton that avoids allocation in the most
        /// common case (an operation being performed with a cancelable token).  The benefit of async disposal would be that _if_
        /// cancellation occurred while the operation was in progress, we could avoid blocking the disposing thread until the
        /// cancellation request completes.  However, this is a rare case, and even when it occurs, it's expected to be very fast,
        /// and in general should be completed by the time we even get to the disposal itself.
        /// </remarks>
        private struct SyncAsyncWorkItemRegistration : IDisposable
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
                Volatile.Write(ref WorkItem._continueTryingToCancel, false);

                // Then we need to dispose of the registration.  Upon Dispose returning, we know that
                // either the synchronous invocation of the callback completed or that the callback
                // will never be invoked.
                CancellationRegistration.Dispose();

                // Now that we know the synchronous callback has quiesced, check to see whether it scheduled
                // asynchronous work.  If it did, wait for that work to complete.
                WorkItem._callbackCompleted?.GetAwaiter().GetResult();
            }
        }

        /// <summary>Registers for cancellation with the specified token.</summary>
        /// <remarks>Upon cancellation being requested, the implementation will attempt to CancelSynchronousIo for the thread calling RegisterCancellation.</remarks>
        private static SyncAsyncWorkItemRegistration RegisterCancellation(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return default;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Get the instance for this thread. If the instance doesn't have a thread handle, that
            // means we were unable to obtain one previously, and we shouldn't try to cancel I/O.
            AsyncOverSyncWithIoCancellation instance = t_instance ??= new AsyncOverSyncWithIoCancellation();
            if (instance._threadHandle is null)
            {
                return default;
            }

            // Reset the instance's state so we can use it again.
            instance.Reset();

            // Register with the caller's cancellation token.
            SyncAsyncWorkItemRegistration reg = default;
            reg.WorkItem = instance;
            reg.CancellationRegistration = cancellationToken.UnsafeRegister(static s =>
            {
                var instance = (AsyncOverSyncWithIoCancellation)s!;

                // If cancellation was already requested when UnsafeRegister was called, it'll invoke
                // the callback immediately.  If we allowed that to loop until cancellation was successful,
                // we'd deadlock, as we'd never perform the very I/O it was waiting for.  As such, if
                // the callback is invoked prior to be ready for it, we ignore the callback.
                if (!Volatile.Read(ref instance._finishedCancellationRegistration))
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
                instance._callbackCompleted = Task.Factory.StartNew(static s =>
                {
                    var instance = (AsyncOverSyncWithIoCancellation)s!;

                    // Cancel the I/O.  If the cancellation happens too early and we haven't yet initiated
                    // the synchronous operation, CancelSynchronousIo will fail with ERROR_NOT_FOUND, and
                    // we'll loop to try again.
                    SpinWait sw = default;
                    while (Volatile.Read(ref instance._continueTryingToCancel))
                    {
                        if (Interop.Kernel32.CancelSynchronousIo(instance._threadHandle!))
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
            Volatile.Write(ref instance._finishedCancellationRegistration, true);

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
