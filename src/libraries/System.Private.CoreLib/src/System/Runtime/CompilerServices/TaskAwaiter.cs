// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

// NOTE: For performance reasons, initialization is not verified.  If a developer
//       incorrectly initializes a task awaiter, which should only be done by the compiler,
//       NullReferenceExceptions may be generated (the alternative would be for us to detect
//       this case and then throw a different exception instead).  This is the same tradeoff
//       that's made with other compiler-focused value types like List<T>.Enumerator.

namespace System.Runtime.CompilerServices
{
    /// <summary>Provides an awaiter for awaiting a <see cref="Task"/>.</summary>
    public readonly struct TaskAwaiter : ICriticalNotifyCompletion, ITaskAwaiter
    {
        // WARNING: Unsafe.As is used to access the generic TaskAwaiter<> as TaskAwaiter.
        // Its layout must remain the same.

        /// <summary>The task being awaited.</summary>
        internal readonly Task m_task;

        /// <summary>Initializes the <see cref="TaskAwaiter"/>.</summary>
        /// <param name="task">The <see cref="Task"/> to be awaited.</param>
        internal TaskAwaiter(Task task)
        {
            Debug.Assert(task != null, "Constructing an awaiter requires a task to await.");
            m_task = task;
        }

        /// <summary>Gets whether the task being awaited is completed.</summary>
        /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
        /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
        public bool IsCompleted => m_task.IsCompleted;

        /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="continuation"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
        /// <exception cref="InvalidOperationException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        public void OnCompleted(Action continuation)
        {
            OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: true);
        }

        /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="continuation"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
        /// <exception cref="InvalidOperationException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: false);
        }

        /// <summary>Ends the await on the completed <see cref="Task"/>.</summary>
        /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <exception cref="TaskCanceledException">The task was canceled.</exception>
        /// <exception cref="Exception">The task completed in a Faulted state.</exception>
        [StackTraceHidden]
        public void GetResult()
        {
            ValidateEnd(m_task);
        }

        /// <summary>
        /// Fast checks for the end of an await operation to determine whether more needs to be done
        /// prior to completing the await.
        /// </summary>
        /// <param name="task">The awaited task.</param>
        /// <param name="options">The options used to configure an await.</param>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ValidateEnd(Task task, ConfigureAwaitOptions options = ConfigureAwaitOptions.None)
        {
            // Fast checks that can be inlined.
            if (task.IsWaitNotificationEnabledOrNotRanToCompletion)
            {
                // If either the end await bit is set or we're not completed successfully,
                // fall back to the slower path.
                HandleNonSuccessAndDebuggerNotification(task, options);
            }
        }

        /// <summary>
        /// Ensures the task is completed, triggers any necessary debugger breakpoints for completing
        /// the await on the task, and throws an exception if the task did not complete successfully.
        /// </summary>
        /// <param name="task">The awaited task.</param>
        /// <param name="options">The options used to configure an await.</param>
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void HandleNonSuccessAndDebuggerNotification(Task task, ConfigureAwaitOptions options)
        {
            // Synchronously wait for the task to complete.  When used by the compiler,
            // the task will already be complete.  This code exists only for direct GetResult use,
            // for cases where the same exception propagation semantics used by "await" are desired,
            // but where for one reason or another synchronous rather than asynchronous waiting is needed.
            if (!task.IsCompleted)
            {
                bool taskCompleted = task.InternalWait(Timeout.Infinite, default);
                Debug.Assert(taskCompleted, "With an infinite timeout, the task should have always completed.");
            }

            // Now that we're done, alert the debugger if so requested
            task.NotifyDebuggerOfWaitCompletionIfNecessary();

            // And throw an exception if the task is faulted or canceled.
            if (!task.IsCompletedSuccessfully)
            {
                if ((options & ConfigureAwaitOptions.SuppressThrowing) == 0)
                {
                    ThrowForNonSuccess(task);
                }

                task.MarkExceptionsAsHandled();
            }
        }

        /// <summary>Throws an exception to handle a task that completed in a state other than RanToCompletion.</summary>
        [StackTraceHidden]
        private static void ThrowForNonSuccess(Task task)
        {
            Debug.Assert(task.IsCompleted, "Task must have been completed by now.");
            Debug.Assert(task.Status != TaskStatus.RanToCompletion, "Task should not be completed successfully.");

            // Handle whether the task has been canceled or faulted
            switch (task.Status)
            {
                // If the task completed in a canceled state, throw an OperationCanceledException.
                // This will either be the OCE that actually caused the task to cancel, or it will be a new
                // TaskCanceledException. TCE derives from OCE, and by throwing it we automatically pick up the
                // completed task's CancellationToken if it has one, including that CT in the OCE.
                case TaskStatus.Canceled:
                    ExceptionDispatchInfo? oceEdi = task.GetCancellationExceptionDispatchInfo();
                    if (oceEdi != null)
                    {
                        oceEdi.Throw();
                        Debug.Fail("Throw() should have thrown");
                    }
                    throw new TaskCanceledException(task);

                // If the task faulted, throw its first exception,
                // even if it contained more than one.
                case TaskStatus.Faulted:
                    List<ExceptionDispatchInfo> edis = task.GetExceptionDispatchInfos();
                    if (edis.Count > 0)
                    {
                        edis[0].Throw();
                        Debug.Fail("Throw() should have thrown");
                        break; // Necessary to compile: non-reachable, but compiler can't determine that
                    }
                    else
                    {
                        Debug.Fail("There should be exceptions if we're Faulted.");
                        throw task.Exception!;
                    }
            }
        }

        /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="task">The task being awaited.</param>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <param name="continueOnCapturedContext">Whether to capture and marshal back to the current context.</param>
        /// <param name="flowExecutionContext">Whether to flow ExecutionContext across the await.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="continuation"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
        /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        internal static void OnCompletedInternal(Task task, Action continuation, bool continueOnCapturedContext, bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(continuation);

            // If TaskWait* ETW events are enabled, trace a beginning event for this await
            // and set up an ending event to be traced when the asynchronous await completes.
            if (TplEventSource.Log.IsEnabled() || Task.s_asyncDebuggingEnabled)
            {
                continuation = OutputWaitEtwEvents(task, continuation);
            }

            // Set the continuation onto the awaited task.
            task.SetContinuationForAwait(continuation, continueOnCapturedContext, flowExecutionContext);
        }

        /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="task">The task being awaited.</param>
        /// <param name="stateMachineBox">The box to invoke when the await operation completes.</param>
        /// <param name="continueOnCapturedContext">Whether to capture and marshal back to the current context.</param>
        internal static void UnsafeOnCompletedInternal(Task task, IAsyncStateMachineBox stateMachineBox, bool continueOnCapturedContext)
        {
            Debug.Assert(stateMachineBox != null);

            // If TaskWait* ETW events are enabled, trace a beginning event for this await
            // and set up an ending event to be traced when the asynchronous await completes.
            if (TplEventSource.Log.IsEnabled() || Task.s_asyncDebuggingEnabled)
            {
                task.SetContinuationForAwait(OutputWaitEtwEvents(task, stateMachineBox.MoveNextAction), continueOnCapturedContext, flowExecutionContext: false);
            }
            else
            {
                task.UnsafeSetContinuationForAwait(stateMachineBox, continueOnCapturedContext);
            }
        }

        /// <summary>
        /// Outputs a WaitBegin ETW event, and augments the continuation action to output a WaitEnd ETW event.
        /// </summary>
        /// <param name="task">The task being awaited.</param>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <returns>The action to use as the actual continuation.</returns>
        private static Action OutputWaitEtwEvents(Task task, Action continuation)
        {
            Debug.Assert(task != null, "Need a task to wait on");
            Debug.Assert(continuation != null, "Need a continuation to invoke when the wait completes");

            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(task);
            }

            TplEventSource log = TplEventSource.Log;

            if (log.IsEnabled())
            {
                // ETW event for Task Wait Begin
                Task? currentTaskAtBegin = Task.InternalCurrent;

                // If this task's continuation is another task, get it.
                Task? continuationTask = AsyncMethodBuilderCore.TryGetContinuationTask(continuation);
                log.TaskWaitBegin(
                    currentTaskAtBegin != null ? currentTaskAtBegin.m_taskScheduler!.Id : TaskScheduler.Default.Id,
                    currentTaskAtBegin != null ? currentTaskAtBegin.Id : 0,
                    task.Id, TplEventSource.TaskWaitBehavior.Asynchronous,
                    continuationTask != null ? continuationTask.Id : 0);
            }

            // Create a continuation action that outputs the end event and then invokes the user
            // provided delegate.  This incurs the allocations for the closure/delegate, but only if the event
            // is enabled, and in doing so it allows us to pass the awaited task's information into the end event
            // in a purely pay-for-play manner (the alternatively would be to increase the size of TaskAwaiter
            // just for this ETW purpose, not pay-for-play, since GetResult would need to know whether a real yield occurred).
            return AsyncMethodBuilderCore.CreateContinuationWrapper(continuation, static (innerContinuation, innerTask) =>
            {
                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(innerTask);
                }

                TplEventSource innerEtwLog = TplEventSource.Log;

                // ETW event for Task Wait End.
                Guid prevActivityId = default;
                bool bEtwLogEnabled = innerEtwLog.IsEnabled();
                if (bEtwLogEnabled)
                {
                    Task? currentTaskAtEnd = Task.InternalCurrent;
                    innerEtwLog.TaskWaitEnd(
                        currentTaskAtEnd != null ? currentTaskAtEnd.m_taskScheduler!.Id : TaskScheduler.Default.Id,
                        currentTaskAtEnd != null ? currentTaskAtEnd.Id : 0,
                        innerTask.Id);

                    // Ensure the continuation runs under the activity ID of the task that completed for the
                    // case the antecedent is a promise (in the other cases this is already the case).
                    if (innerEtwLog.TasksSetActivityIds && (innerTask.Options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) != 0)
                        EventSource.SetCurrentThreadActivityId(TplEventSource.CreateGuidForTaskID(innerTask.Id), out prevActivityId);
                }

                // Invoke the original continuation provided to OnCompleted.
                innerContinuation();

                if (bEtwLogEnabled)
                {
                    innerEtwLog.TaskWaitContinuationComplete(innerTask.Id);
                    if (innerEtwLog.TasksSetActivityIds && (innerTask.Options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) != 0)
                        EventSource.SetCurrentThreadActivityId(prevActivityId);
                }
            }, task);
        }
    }

    /// <summary>Provides an awaiter for awaiting a <see cref="Task{TResult}"/>.</summary>
    public readonly struct TaskAwaiter<TResult> : ICriticalNotifyCompletion, ITaskAwaiter
    {
        // WARNING: Unsafe.As is used to access TaskAwaiter<> as the non-generic TaskAwaiter.
        // Its layout must remain the same.

        /// <summary>The task being awaited.</summary>
        private readonly Task<TResult> m_task;

        /// <summary>Initializes the <see cref="TaskAwaiter{TResult}"/>.</summary>
        /// <param name="task">The <see cref="Task{TResult}"/> to be awaited.</param>
        internal TaskAwaiter(Task<TResult> task)
        {
            Debug.Assert(task != null, "Constructing an awaiter requires a task to await.");
            m_task = task;
        }

        /// <summary>Gets whether the task being awaited is completed.</summary>
        /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
        /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
        public bool IsCompleted => m_task.IsCompleted;

        /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="continuation"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
        /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        public void OnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: true);
        }

        /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="continuation"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
        /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        public void UnsafeOnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: false);
        }

        /// <summary>Ends the await on the completed <see cref="Task{TResult}"/>.</summary>
        /// <returns>The result of the completed <see cref="Task{TResult}"/>.</returns>
        /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <exception cref="TaskCanceledException">The task was canceled.</exception>
        /// <exception cref="Exception">The task completed in a Faulted state.</exception>
        [StackTraceHidden]
        public TResult GetResult()
        {
            TaskAwaiter.ValidateEnd(m_task);
            return m_task.ResultOnSuccess;
        }
    }

    /// <summary>
    /// Marker interface used to know whether a particular awaiter is either a
    /// TaskAwaiter or a TaskAwaiter`1.  It must not be implemented by any other
    /// awaiters.
    /// </summary>
    internal interface ITaskAwaiter { }

    /// <summary>
    /// Marker interface used to know whether a particular awaiter is either a
    /// CTA.ConfiguredTaskAwaiter or a CTA`1.ConfiguredTaskAwaiter.  It must not
    /// be implemented by any other awaiters.
    /// </summary>
    internal interface IConfiguredTaskAwaiter { }

    /// <summary>Provides an awaitable object that allows for configured awaits on <see cref="Task"/>.</summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    public readonly struct ConfiguredTaskAwaitable
    {
        /// <summary>The task being awaited.</summary>
        private readonly ConfiguredTaskAwaiter m_configuredTaskAwaiter;

        /// <summary>Initializes the <see cref="ConfiguredTaskAwaitable"/>.</summary>
        /// <param name="task">The awaitable <see cref="Task"/>.</param>
        /// <param name="options">Options to control the behavior of the awaiter.</param>
        internal ConfiguredTaskAwaitable(Task task, ConfigureAwaitOptions options)
        {
            Debug.Assert(task != null, "Constructing an awaitable requires a task to await.");
            m_configuredTaskAwaiter = new ConfiguredTaskAwaiter(task, options);
        }

        /// <summary>Gets an awaiter for this awaitable.</summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredTaskAwaiter GetAwaiter()
        {
            return m_configuredTaskAwaiter;
        }

        /// <summary>Provides an awaiter for a <see cref="ConfiguredTaskAwaitable"/>.</summary>
        /// <remarks>This type is intended for compiler use only.</remarks>
        public readonly struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, IConfiguredTaskAwaiter
        {
            // WARNING: Unsafe.As is used to access the generic ConfiguredTaskAwaiter as this.
            // Its layout must remain the same.

            /// <summary>The task being awaited.</summary>
            internal readonly Task m_task;
            /// <summary>Options for how this awaiter behaves. This is a bit field with values from <see cref="ConfigureAwaitOptions"/>.</summary>
            internal readonly ConfigureAwaitOptions m_options;

            /// <summary>Initializes the <see cref="ConfiguredTaskAwaiter"/>.</summary>
            /// <param name="task">The <see cref="Task"/> to await.</param>
            /// <param name="options">Options used to configure how an await is performed.</param>
            internal ConfiguredTaskAwaiter(Task task, ConfigureAwaitOptions options)
            {
                Debug.Assert(task != null, "Constructing an awaiter requires a task to await.");
                Debug.Assert((options & ~(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ForceYielding)) == 0);
                m_task = task;
                m_options = options;
            }

            /// <summary>Gets whether the task being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
            public bool IsCompleted => ((m_options & ConfigureAwaitOptions.ForceYielding) == 0) && m_task.IsCompleted;

            /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="ArgumentNullException">The <paramref name="continuation"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
            /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, (m_options & ConfigureAwaitOptions.ContinueOnCapturedContext) != 0, flowExecutionContext: true);
            }

            /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="ArgumentNullException">The <paramref name="continuation"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
            /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, (m_options & ConfigureAwaitOptions.ContinueOnCapturedContext) != 0, flowExecutionContext: false);
            }

            /// <summary>Ends the await on the completed <see cref="Task"/>.</summary>
            /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <exception cref="TaskCanceledException">The task was canceled.</exception>
            /// <exception cref="Exception">The task completed in a Faulted state.</exception>
            [StackTraceHidden]
            public void GetResult()
            {
                TaskAwaiter.ValidateEnd(m_task, m_options);
            }
        }
    }

    /// <summary>Provides an awaitable object that allows for configured awaits on <see cref="Task{TResult}"/>.</summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    public readonly struct ConfiguredTaskAwaitable<TResult>
    {
        /// <summary>The underlying awaitable on whose logic this awaitable relies.</summary>
        private readonly ConfiguredTaskAwaiter m_configuredTaskAwaiter;

        /// <summary>Initializes the <see cref="ConfiguredTaskAwaitable{TResult}"/>.</summary>
        /// <param name="task">The awaitable <see cref="Task{TResult}"/>.</param>
        /// <param name="options">Options to control the behavior of the awaiter.</param>
        internal ConfiguredTaskAwaitable(Task<TResult> task, ConfigureAwaitOptions options)
        {
            m_configuredTaskAwaiter = new ConfiguredTaskAwaiter(task, options);
        }

        /// <summary>Gets an awaiter for this awaitable.</summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredTaskAwaiter GetAwaiter()
        {
            return m_configuredTaskAwaiter;
        }

        /// <summary>Provides an awaiter for a <see cref="ConfiguredTaskAwaitable{TResult}"/>.</summary>
        /// <remarks>This type is intended for compiler use only.</remarks>
        public readonly struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, IConfiguredTaskAwaiter
        {
            // WARNING: Unsafe.As is used to access this as the non-generic ConfiguredTaskAwaiter.
            // Its layout must remain the same.

            /// <summary>The task being awaited.</summary>
            private readonly Task<TResult> m_task;
            /// <summary>Options for how this awaiter behaves. This is a bit field with values from <see cref="ConfigureAwaitOptions"/>.</summary>
            internal readonly ConfigureAwaitOptions m_options;

            /// <summary>Initializes the <see cref="ConfiguredTaskAwaiter"/>.</summary>
            /// <param name="task">The awaitable <see cref="Task{TResult}"/>.</param>
            /// <param name="options">The options used to configure the await's behavior.</param>
            internal ConfiguredTaskAwaiter(Task<TResult> task, ConfigureAwaitOptions options)
            {
                Debug.Assert(task != null, "Constructing an awaiter requires a task to await.");
                Debug.Assert((options & ~(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.ForceYielding)) == 0);
                m_task = task;
                m_options = options;
            }

            /// <summary>Gets whether the task being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
            public bool IsCompleted => ((m_options & ConfigureAwaitOptions.ForceYielding) == 0) && m_task.IsCompleted;

            /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="ArgumentNullException">The <paramref name="continuation"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
            /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, (m_options & ConfigureAwaitOptions.ContinueOnCapturedContext) != 0, flowExecutionContext: true);
            }

            /// <summary>Schedules the continuation onto the <see cref="Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="ArgumentNullException">The <paramref name="continuation"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
            /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, (m_options & ConfigureAwaitOptions.ContinueOnCapturedContext) != 0, flowExecutionContext: false);
            }

            /// <summary>Ends the await on the completed <see cref="Task{TResult}"/>.</summary>
            /// <returns>The result of the completed <see cref="Task{TResult}"/>.</returns>
            /// <exception cref="NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <exception cref="TaskCanceledException">The task was canceled.</exception>
            /// <exception cref="Exception">The task completed in a Faulted state.</exception>
            [StackTraceHidden]
            public TResult GetResult()
            {
                TaskAwaiter.ValidateEnd(m_task); // no need to pass options as SuppressThrowing isn't supported
                return m_task.ResultOnSuccess;
            }
        }
    }
}
