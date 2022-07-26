// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
        /// <summary>
        /// Runs code that may be aborted asynchronously.
        /// </summary>
        /// <param name="action">The delegate that represents the code to execute.</param>
        /// <param name="cancellationToken">The cancellation token that may be used to abort execution.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="action"/> argument is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// The current thread is already running the <see cref="ControlledExecution.Run"/> method.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">The execution was aborted.</exception>
        /// <remarks>
        /// <see cref="ControlledExecution"/> enables aborting arbitrary code in a non-cooperative manner.
        /// Doing so may corrupt the process.  This method is not recommended for use in production code
        /// in which reliability is important.
        /// </remarks>
        [Obsolete(Obsoletions.ControlledExecutionRunMessage, DiagnosticId = Obsoletions.ControlledExecutionRunDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void Run(Action action, CancellationToken cancellationToken)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException();

            ArgumentNullException.ThrowIfNull(action);

            // Recursive ControlledExecution.Run calls are not supported
            if (Execution.t_executing)
                throw new InvalidOperationException(SR.InvalidOperation_NestedControlledExecutionRun);

            new Execution(action, cancellationToken).Run();
        }

        private sealed partial class Execution
        {
            // The state transition diagram (F means the Finished flag and so on):
            // N  ⟶ F
            // ↓
            // AR ⟶ FAR
            // ↓      ↓
            // A  ⟶ FA
            private enum State : int
            {
                None = 0,
                Finished = 1,
                AbortRequested = 2,
                RunningAbort = 4
            }

            [ThreadStatic]
            internal static bool t_executing;

            // Interpreted as a value of the State enumeration type
            private int _state;
            private readonly Action _action;
            private readonly CancellationToken _cancellationToken;
            private Thread? _thread;

            public Execution(Action action, CancellationToken cancellationToken)
            {
                _action = action;
                _cancellationToken = cancellationToken;
            }

            public void Run()
            {
                Debug.Assert(_state == (int)State.None && _thread == null);
                _thread = Thread.CurrentThread;

                try
                {
                    try
                    {
                        t_executing = true;
                        // Cannot Dispose this registration in a finally or a catch block as that may deadlock with AbortThread
                        _cancellationToken.UnsafeRegister(e => ((Execution)e!).Abort(), this);
                        _action();
                    }
                    finally
                    {
                        t_executing = false;

                        // Set the Finished flag to prevent a potential subsequent AbortThread call
                        State oldState = (State)Interlocked.Or(ref _state, (int)State.Finished);

                        if ((oldState & State.AbortRequested) != 0)
                        {
                            // Either in FAR or FA state
                            while (true)
                            {
                                // The enclosing finally may be cloned by the JIT for the non-exceptional code flow.
                                // In that case this code is not guarded against a thread abort. In particular, any
                                // QCall may be aborted. That is OK as we will catch the ThreadAbortException and call
                                // ResetAbortThread again below. The only downside is that a successfully executed
                                // action may be reported as canceled.
                                bool resetAbortRequest = ResetAbortThread() != Interop.BOOL.FALSE;

                                // If there is an Abort in progress, we need to wait until it sets the TS_AbortRequested
                                // flag on this thread, then we can reset the flag and safely exit this frame.
                                if (((oldState & State.RunningAbort) == 0) || resetAbortRequest)
                                {
                                    break;
                                }

                                // It should take very short time for AbortThread to set the TS_AbortRequested flag
                                Thread.Sleep(0);
                                oldState = (State)Volatile.Read(ref _state);
                            }
                        }
                    }
                }
                catch (ThreadAbortException tae) when (_cancellationToken.IsCancellationRequested)
                {
                    t_executing = false;
                    ResetAbortThread();

                    var e = new OperationCanceledException(_cancellationToken);
                    if (tae.StackTrace is string stackTrace)
                    {
                        ExceptionDispatchInfo.SetRemoteStackTrace(e, stackTrace);
                    }
                    throw e;
                }
            }

            public void Abort()
            {
                // Prevent potential refetching of _state from shared memory
                State curState = (State)Volatile.Read(ref _state);
                State oldState;

                do
                {
                    Debug.Assert((curState & (State.AbortRequested | State.RunningAbort)) == 0);

                    // If the execution has finished, there is nothing to do
                    if ((curState & State.Finished) != 0)
                        return;

                    // Try to set the AbortRequested and RunningAbort flags
                    oldState = curState;
                    curState = (State)Interlocked.CompareExchange(ref _state,
                        (int)(oldState | State.AbortRequested | State.RunningAbort), (int)oldState);
                }
                while (curState != oldState);

                try
                {
                    // Must be in AR or FAR state now
                    Debug.Assert(_thread != null);
                    AbortThread(_thread.GetNativeHandle());
                }
                finally
                {
                    // Reset the RunningAbort flag to signal the executing thread it is safe to exit
                    Interlocked.And(ref _state, (int)~State.RunningAbort);
                }
            }

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Abort")]
            private static partial void AbortThread(ThreadHandle thread);

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_ResetAbort")]
            [SuppressGCTransition]
            private static partial Interop.BOOL ResetAbortThread();
        }
    }
}
