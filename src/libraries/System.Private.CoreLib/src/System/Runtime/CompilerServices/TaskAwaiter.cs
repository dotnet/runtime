// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// Types for awaiting Task and Task<T>. These types are emitted from Task{<T>}.GetAwaiter
// and Task{<T>}.ConfigureAwait.  They are meant to be used only by the compiler, e.g.
//
//   await nonGenericTask;
//   =====================
//       var $awaiter = nonGenericTask.GetAwaiter();
//       if (!$awaiter.IsCompleted)
//       {
//           SPILL:
//           $builder.AwaitUnsafeOnCompleted(ref $awaiter, ref this);
//           return;
//           Label:
//           UNSPILL;
//       }
//       $awaiter.GetResult();
//
//   result += await genericTask.ConfigureAwait(false);
//   ===================================================================================
//       var $awaiter = genericTask.ConfigureAwait(false).GetAwaiter();
//       if (!$awaiter.IsCompleted)
//       {
//           SPILL;
//           $builder.AwaitUnsafeOnCompleted(ref $awaiter, ref this);
//           return;
//           Label:
//           UNSPILL;
//       }
//       result += $awaiter.GetResult();
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

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
    /// <summary>Provides an awaiter for awaiting a <see cref="System.Threading.Tasks.Task"/>.</summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    public readonly struct TaskAwaiter : ICriticalNotifyCompletion, ITaskAwaiter
    {
        // WARNING: Unsafe.As is used to access the generic TaskAwaiter<> as TaskAwaiter.
        // Its layout must remain the same.

        /// <summary>The task being awaited.</summary>
        internal readonly Task m_task;

        /// <summary>Initializes the <see cref="TaskAwaiter"/>.</summary>
        /// <param name="task">The <see cref="System.Threading.Tasks.Task"/> to be awaited.</param>
        internal TaskAwaiter(Task task)
        {
            Debug.Assert(task != null, "Constructing an awaiter requires a task to await.");
            m_task = task;
        }

        /// <summary>Gets whether the task being awaited is completed.</summary>
        /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
        /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
        public bool IsCompleted => m_task.IsCompleted;

        /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        public void OnCompleted(Action continuation)
        {
            OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: true);
        }

        /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: false);
        }

        /// <summary>Ends the await on the completed <see cref="System.Threading.Tasks.Task"/>.</summary>
        /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <exception cref="System.Threading.Tasks.TaskCanceledException">The task was canceled.</exception>
        /// <exception cref="System.Exception">The task completed in a Faulted state.</exception>
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
        [StackTraceHidden]
        internal static void ValidateEnd(Task task)
        {
            // Fast checks that can be inlined.
            if (task.IsWaitNotificationEnabledOrNotRanToCompletion)
            {
                // If either the end await bit is set or we're not completed successfully,
                // fall back to the slower path.
                HandleNonSuccessAndDebuggerNotification(task);
            }
        }

        /// <summary>
        /// Ensures the task is completed, triggers any necessary debugger breakpoints for completing
        /// the await on the task, and throws an exception if the task did not complete successfully.
        /// </summary>
        /// <param name="task">The awaited task.</param>
        [StackTraceHidden]
        private static void HandleNonSuccessAndDebuggerNotification(Task task)
        {
            // NOTE: The JIT refuses to inline ValidateEnd when it contains the contents
            // of HandleNonSuccessAndDebuggerNotification, hence the separation.

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
            if (!task.IsCompletedSuccessfully) ThrowForNonSuccess(task);
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

        /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="task">The task being awaited.</param>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <param name="continueOnCapturedContext">Whether to capture and marshal back to the current context.</param>
        /// <param name="flowExecutionContext">Whether to flow ExecutionContext across the await.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        internal static void OnCompletedInternal(Task task, Action continuation, bool continueOnCapturedContext, bool flowExecutionContext)
        {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));

            // If TaskWait* ETW events are enabled, trace a beginning event for this await
            // and set up an ending event to be traced when the asynchronous await completes.
            if (TplEventSource.Log.IsEnabled() || Task.s_asyncDebuggingEnabled)
            {
                continuation = OutputWaitEtwEvents(task, continuation);
            }

            // Set the continuation onto the awaited task.
            task.SetContinuationForAwait(continuation, continueOnCapturedContext, flowExecutionContext);
        }

        /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
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

    /// <summary>Provides an awaiter for awaiting a <see cref="System.Threading.Tasks.Task{TResult}"/>.</summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    public readonly struct TaskAwaiter<TResult> : ICriticalNotifyCompletion, ITaskAwaiter
    {
        // WARNING: Unsafe.As is used to access TaskAwaiter<> as the non-generic TaskAwaiter.
        // Its layout must remain the same.

        /// <summary>The task being awaited.</summary>
        private readonly Task<TResult> m_task;

        /// <summary>Initializes the <see cref="TaskAwaiter{TResult}"/>.</summary>
        /// <param name="task">The <see cref="System.Threading.Tasks.Task{TResult}"/> to be awaited.</param>
        internal TaskAwaiter(Task<TResult> task)
        {
            Debug.Assert(task != null, "Constructing an awaiter requires a task to await.");
            m_task = task;
        }

        /// <summary>Gets whether the task being awaited is completed.</summary>
        /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
        /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
        public bool IsCompleted => m_task.IsCompleted;

        /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        public void OnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: true);
        }

        /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        public void UnsafeOnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: false);
        }

        /// <summary>Ends the await on the completed <see cref="System.Threading.Tasks.Task{TResult}"/>.</summary>
        /// <returns>The result of the completed <see cref="System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
        /// <exception cref="System.Threading.Tasks.TaskCanceledException">The task was canceled.</exception>
        /// <exception cref="System.Exception">The task completed in a Faulted state.</exception>
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

    /// <summary>Provides an awaitable object that allows for configured awaits on <see cref="System.Threading.Tasks.Task"/>.</summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    public readonly struct ConfiguredTaskAwaitable
    {
        /// <summary>The task being awaited.</summary>
        private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter m_configuredTaskAwaiter;

        /// <summary>Initializes the <see cref="ConfiguredTaskAwaitable"/>.</summary>
        /// <param name="task">The awaitable <see cref="System.Threading.Tasks.Task"/>.</param>
        /// <param name="continueOnCapturedContext">
        /// true to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        internal ConfiguredTaskAwaitable(Task task, bool continueOnCapturedContext)
        {
            Debug.Assert(task != null, "Constructing an awaitable requires a task to await.");
            m_configuredTaskAwaiter = new ConfiguredTaskAwaitable.ConfiguredTaskAwaiter(task, continueOnCapturedContext);
        }

        /// <summary>Gets an awaiter for this awaitable.</summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter()
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
            /// <summary>Whether to attempt marshaling back to the original context.</summary>
            internal readonly bool m_continueOnCapturedContext;

            /// <summary>Initializes the <see cref="ConfiguredTaskAwaiter"/>.</summary>
            /// <param name="task">The <see cref="System.Threading.Tasks.Task"/> to await.</param>
            /// <param name="continueOnCapturedContext">
            /// true to attempt to marshal the continuation back to the original context captured
            /// when BeginAwait is called; otherwise, false.
            /// </param>
            internal ConfiguredTaskAwaiter(Task task, bool continueOnCapturedContext)
            {
                Debug.Assert(task != null, "Constructing an awaiter requires a task to await.");
                m_task = task;
                m_continueOnCapturedContext = continueOnCapturedContext;
            }

            /// <summary>Gets whether the task being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            public bool IsCompleted => m_task.IsCompleted;

            /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, m_continueOnCapturedContext, flowExecutionContext: true);
            }

            /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, m_continueOnCapturedContext, flowExecutionContext: false);
            }

            /// <summary>Ends the await on the completed <see cref="System.Threading.Tasks.Task"/>.</summary>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <exception cref="System.Threading.Tasks.TaskCanceledException">The task was canceled.</exception>
            /// <exception cref="System.Exception">The task completed in a Faulted state.</exception>
            [StackTraceHidden]
            public void GetResult()
            {
                TaskAwaiter.ValidateEnd(m_task);
            }
        }
    }

    /// <summary>Provides an awaitable object that allows for configured awaits on <see cref="System.Threading.Tasks.Task{TResult}"/>.</summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    public readonly struct ConfiguredTaskAwaitable<TResult>
    {
        /// <summary>The underlying awaitable on whose logic this awaitable relies.</summary>
        private readonly ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter m_configuredTaskAwaiter;

        /// <summary>Initializes the <see cref="ConfiguredTaskAwaitable{TResult}"/>.</summary>
        /// <param name="task">The awaitable <see cref="System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="continueOnCapturedContext">
        /// true to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        internal ConfiguredTaskAwaitable(Task<TResult> task, bool continueOnCapturedContext)
        {
            m_configuredTaskAwaiter = new ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter(task, continueOnCapturedContext);
        }

        /// <summary>Gets an awaiter for this awaitable.</summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter GetAwaiter()
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
            /// <summary>Whether to attempt marshaling back to the original context.</summary>
            private readonly bool m_continueOnCapturedContext;

            /// <summary>Initializes the <see cref="ConfiguredTaskAwaiter"/>.</summary>
            /// <param name="task">The awaitable <see cref="System.Threading.Tasks.Task{TResult}"/>.</param>
            /// <param name="continueOnCapturedContext">
            /// true to attempt to marshal the continuation back to the original context captured; otherwise, false.
            /// </param>
            internal ConfiguredTaskAwaiter(Task<TResult> task, bool continueOnCapturedContext)
            {
                Debug.Assert(task != null, "Constructing an awaiter requires a task to await.");
                m_task = task;
                m_continueOnCapturedContext = continueOnCapturedContext;
            }

            /// <summary>Gets whether the task being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            public bool IsCompleted => m_task.IsCompleted;

            /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, m_continueOnCapturedContext, flowExecutionContext: true);
            }

            /// <summary>Schedules the continuation onto the <see cref="System.Threading.Tasks.Task"/> associated with this <see cref="TaskAwaiter"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="System.ArgumentNullException">The <paramref name="continuation"/> argument is null (Nothing in Visual Basic).</exception>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, m_continueOnCapturedContext, flowExecutionContext: false);
            }

            /// <summary>Ends the await on the completed <see cref="System.Threading.Tasks.Task{TResult}"/>.</summary>
            /// <returns>The result of the completed <see cref="System.Threading.Tasks.Task{TResult}"/>.</returns>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <exception cref="System.Threading.Tasks.TaskCanceledException">The task was canceled.</exception>
            /// <exception cref="System.Exception">The task completed in a Faulted state.</exception>
            [StackTraceHidden]
            public TResult GetResult()
            {
                TaskAwaiter.ValidateEnd(m_task);
                return m_task.ResultOnSuccess;
            }
        }
    }
}
