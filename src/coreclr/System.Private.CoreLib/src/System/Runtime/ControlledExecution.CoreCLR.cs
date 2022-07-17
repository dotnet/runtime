// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        [Obsolete(Obsoletions.ControlledExecutionRunMessage, DiagnosticId = Obsoletions.ControlledExecutionRunDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void Run(Action action, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(action);
            var execution = new Execution(action, cancellationToken);
            cancellationToken.Register(execution.Abort, useSynchronizationContext: false);
            execution.Run();
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Abort")]
        private static partial void AbortThread(ThreadHandle thread);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool ResetAbortThread();

        private sealed partial class Execution
        {
            // The state transition diagram (S means the Started flag and so on):
            // N ⟶  S  ⟶ SF
            // ↓     ↓
            // AR   SAR ⟶ SFAR
            // ↓     ↓       ↓
            // A    SA  ⟶ SFA
            private enum State : int
            {
                None = 0,
                Started = 1,
                Finished = 2,
                AbortRequested = 4,
                RunningAbort = 8
            }

            [ThreadStatic]
            private static Execution? t_execution;

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
                Debug.Assert((_state & (int)State.Started) == 0 && _thread == null);

                // Nested ControlledExecution.Run methods are not supported
                if (t_execution != null)
                    throw new InvalidOperationException(SR.InvalidOperation_NestedControlledExecutionRun);

                _thread = Thread.CurrentThread;

                try
                {
                    try
                    {
                        // As soon as the Started flag is set, this thread may be aborted asynchronously
                        if (Interlocked.CompareExchange(ref _state, (int)State.Started, (int)State.None) == (int)State.None)
                        {
                            t_execution = this;
                            _action();
                        }
                    }
                    finally
                    {
                        if ((_state & (int)State.Started) != 0)
                        {
                            // Set the Finished flag to prevent a potential subsequent AbortThread call
                            State oldState = (State)Interlocked.Or(ref _state, (int)State.Finished);

                            if ((oldState & State.AbortRequested) != 0)
                            {
                                // Either in SFAR or SFA state
                                while (true)
                                {
                                    // The enclosing finally may be cloned by the JIT for the non-exceptional code flow.
                                    // In that case this code is not guarded against a thread abort, so make this FCall as
                                    // soon as possible.
                                    bool resetAbortRequest = ResetAbortThread();

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

                            t_execution = null;
                        }
                    }
                }
                catch (ThreadAbortException) when (_cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(_cancellationToken);
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
                    // If the execution has not started yet, we are done
                    if ((curState & State.Started) == 0)
                        return;

                    // Must be in SAR or SFAR state now
                    Debug.Assert(_thread != null);
                    AbortThread(_thread.GetNativeHandle());
                }
                finally
                {
                    // Reset the RunningAbort flag to signal the executing thread it is safe to exit
                    Interlocked.And(ref _state, (int)~State.RunningAbort);
                }
            }
        }
    }
}
