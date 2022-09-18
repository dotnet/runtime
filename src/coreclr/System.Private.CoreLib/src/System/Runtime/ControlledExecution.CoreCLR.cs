// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime
{
    /// <summary>
    /// Allows to run code and abort it asynchronously.
    /// </summary>
    public static partial class ControlledExecution
    {
        [ThreadStatic]
        private static bool t_executing;

        /// <summary>
        /// Runs code that may be aborted asynchronously.
        /// </summary>
        /// <param name="action">The delegate that represents the code to execute.</param>
        /// <param name="cancellationToken">The cancellation token that may be used to abort execution.</param>
        /// <exception cref="System.PlatformNotSupportedException">The method is not supported on this platform.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action"/> argument is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// The current thread is already running the <see cref="ControlledExecution.Run"/> method.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">The execution was aborted.</exception>
        /// <remarks>
        /// <para>This method enables aborting arbitrary managed code in a non-cooperative manner by throwing an exception
        /// in the thread executing that code.  While the exception may be caught by the code, it is re-thrown at the end
        /// of `catch` blocks until the execution flow returns to the `ControlledExecution.Run` method.</para>
        /// <para>Execution of the code is not guaranteed to abort immediately, or at all.  This situation can occur, for
        /// example, if a thread is stuck executing unmanaged code or the `catch` and `finally` blocks that are called as
        /// part of the abort procedure, thereby indefinitely delaying the abort.  Furthermore, execution may not be
        /// aborted immediately if the thread is currently executing a `catch` or `finally` block.</para>
        /// <para>Aborting code at an unexpected location may corrupt the state of data structures in the process and lead
        /// to unpredictable results.  For that reason, this method should not be used in production code and calling it
        /// produces a compile-time warning.</para>
        /// </remarks>
        [Obsolete(Obsoletions.ControlledExecutionRunMessage, DiagnosticId = Obsoletions.ControlledExecutionRunDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void Run(Action action, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(action);

            // ControlledExecution.Run does not support nested invocations.  If there's one already in flight
            // on this thread, fail.
            if (t_executing)
            {
                throw new InvalidOperationException(SR.InvalidOperation_NestedControlledExecutionRun);
            }

            // Store the current thread so that it may be referenced by the Canceler.Cancel callback if one occurs.
            Canceler canceler = new(Thread.CurrentThread);

            try
            {
                // Mark this thread as now running a ControlledExecution.Run to prevent recursive usage.
                t_executing = true;

                // Register for aborting.  From this moment until ctr.Unregister is called, this thread is subject to being
                // interrupted at any moment.  This could happen during the call to UnsafeRegister if cancellation has
                // already been requested at the time of the registration.
                CancellationTokenRegistration ctr = cancellationToken.UnsafeRegister(e => ((Canceler)e!).Cancel(), canceler);
                try
                {
                    // Invoke the caller's code.
                    action();
                }
                finally
                {
                    // This finally block may be cloned by JIT for the non-exceptional code flow.  In that case the code
                    // below is not guarded against aborting.  That is OK as the outer try block will catch the
                    // ThreadAbortException and call ResetAbortThread.

                    // Unregister the callback.  Unlike Dispose, Unregister will not block waiting for an callback in flight
                    // to complete, and will instead return false if the callback has already been invoked or is currently
                    // in flight.
                    if (!ctr.Unregister())
                    {
                        // Wait until the callback has completed.  Either the callback is already invoked and completed
                        // (in which case IsCancelCompleted will be true), or it may still be in flight.  If it's in flight,
                        // the AbortThread call may be waiting for this thread to exit this finally block to exit, so while
                        // spinning waiting for the callback to complete, we also need to call ResetAbortThread in order to
                        // reset the flag the AbortThread call is polling in its waiting loop.
                        SpinWait sw = default;
                        while (!canceler.IsCancelCompleted)
                        {
                            ResetAbortThread();
                            sw.SpinOnce();
                        }
                    }
                }
            }
            catch (ThreadAbortException tae)
            {
                // We don't want to leak ThreadAbortExceptions to user code.  Instead, translate the exception into
                // an OperationCanceledException, preserving stack trace details from the ThreadAbortException in
                // order to aid in diagnostics and debugging.
                OperationCanceledException e = cancellationToken.IsCancellationRequested ? new(cancellationToken) : new();
                if (tae.StackTrace is string stackTrace)
                {
                    ExceptionDispatchInfo.SetRemoteStackTrace(e, stackTrace);
                }
                throw e;
            }
            finally
            {
                // Unmark this thread for recursion detection.
                t_executing = false;

                if (cancellationToken.IsCancellationRequested)
                {
                    // Reset an abort request that may still be pending on this thread.
                    ResetAbortThread();
                }
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Abort")]
        private static partial void AbortThread(ThreadHandle thread);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_ResetAbort")]
        [SuppressGCTransition]
        private static partial void ResetAbortThread();

        private sealed class Canceler
        {
            private readonly Thread _thread;
            private volatile bool _cancelCompleted;

            public Canceler(Thread thread)
            {
                _thread = thread;
            }

            public bool IsCancelCompleted => _cancelCompleted;

            public void Cancel()
            {
                try
                {
                    // Abort the thread executing the action (which may be the current thread).
                    AbortThread(_thread.GetNativeHandle());
                }
                finally
                {
                    _cancelCompleted = true;
                }
            }
        }
    }
}
