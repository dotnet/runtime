// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    // Task type used to implement: Task ContinueWith(Action<Task,...>)
    internal sealed class ContinuationTaskFromTask : Task
    {
        private Task? m_antecedent;

        public ContinuationTaskFromTask(
            Task antecedent, Delegate action, object? state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions) :
            base(action, state, Task.InternalCurrentIfAttached(creationOptions), default, creationOptions, internalOptions, null)
        {
            Debug.Assert(action is Action<Task> || action is Action<Task, object?>,
                "Invalid delegate type in ContinuationTaskFromTask");
            m_antecedent = antecedent;
        }

        /// <summary>
        /// Evaluates the value selector of the Task which is passed in as an object and stores the result.
        /// </summary>        
        internal override void InnerInvoke()
        {
            // Get and null out the antecedent.  This is crucial to avoid a memory
            // leak with long chains of continuations.
            Task? antecedent = m_antecedent;
            Debug.Assert(antecedent != null,
                "No antecedent was set for the ContinuationTaskFromTask.");
            m_antecedent = null;

            // Notify the debugger we're completing an asynchronous wait on a task
            antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();

            // Invoke the delegate
            Debug.Assert(m_action != null);
            if (m_action is Action<Task> action)
            {
                action(antecedent);
                return;
            }

            if (m_action is Action<Task, object?> actionWithState)
            {
                actionWithState(antecedent, m_stateObject);
                return;
            }
            Debug.Fail("Invalid m_action in ContinuationTaskFromTask");
        }
    }

    // Task type used to implement: Task<TResult> ContinueWith(Func<Task,...>)
    internal sealed class ContinuationResultTaskFromTask<TResult> : Task<TResult>
    {
        private Task? m_antecedent;

        public ContinuationResultTaskFromTask(
            Task antecedent, Delegate function, object? state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions) :
            base(function, state, Task.InternalCurrentIfAttached(creationOptions), default, creationOptions, internalOptions, null)
        {
            Debug.Assert(function is Func<Task, TResult> || function is Func<Task, object?, TResult>,
                "Invalid delegate type in ContinuationResultTaskFromTask");
            m_antecedent = antecedent;
        }

        /// <summary>
        /// Evaluates the value selector of the Task which is passed in as an object and stores the result.
        /// </summary>        
        internal override void InnerInvoke()
        {
            // Get and null out the antecedent.  This is crucial to avoid a memory
            // leak with long chains of continuations.
            Task? antecedent = m_antecedent;
            Debug.Assert(antecedent != null,
                "No antecedent was set for the ContinuationResultTaskFromTask.");
            m_antecedent = null;

            // Notify the debugger we're completing an asynchronous wait on a task
            antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();

            // Invoke the delegate
            Debug.Assert(m_action != null);
            if (m_action is Func<Task, TResult> func)
            {
                m_result = func(antecedent);
                return;
            }

            if (m_action is Func<Task, object?, TResult> funcWithState)
            {
                m_result = funcWithState(antecedent, m_stateObject);
                return;
            }
            Debug.Fail("Invalid m_action in ContinuationResultTaskFromTask");
        }
    }

    // Task type used to implement: Task ContinueWith(Action<Task<TAntecedentResult>,...>)
    internal sealed class ContinuationTaskFromResultTask<TAntecedentResult> : Task
    {
        private Task<TAntecedentResult>? m_antecedent;

        public ContinuationTaskFromResultTask(
            Task<TAntecedentResult> antecedent, Delegate action, object? state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions) :
            base(action, state, Task.InternalCurrentIfAttached(creationOptions), default, creationOptions, internalOptions, null)
        {
            Debug.Assert(action is Action<Task<TAntecedentResult>> || action is Action<Task<TAntecedentResult>, object?>,
                "Invalid delegate type in ContinuationTaskFromResultTask");
            m_antecedent = antecedent;
        }

        /// <summary>
        /// Evaluates the value selector of the Task which is passed in as an object and stores the result.
        /// </summary>
        internal override void InnerInvoke()
        {
            // Get and null out the antecedent.  This is crucial to avoid a memory
            // leak with long chains of continuations.
            Task<TAntecedentResult>? antecedent = m_antecedent;
            Debug.Assert(antecedent != null,
                "No antecedent was set for the ContinuationTaskFromResultTask.");
            m_antecedent = null;

            // Notify the debugger we're completing an asynchronous wait on a task
            antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();

            // Invoke the delegate
            Debug.Assert(m_action != null);
            if (m_action is Action<Task<TAntecedentResult>> action)
            {
                action(antecedent);
                return;
            }

            if (m_action is Action<Task<TAntecedentResult>, object?> actionWithState)
            {
                actionWithState(antecedent, m_stateObject);
                return;
            }
            Debug.Fail("Invalid m_action in ContinuationTaskFromResultTask");
        }
    }

    // Task type used to implement: Task<TResult> ContinueWith(Func<Task<TAntecedentResult>,...>)
    internal sealed class ContinuationResultTaskFromResultTask<TAntecedentResult, TResult> : Task<TResult>
    {
        private Task<TAntecedentResult>? m_antecedent;

        public ContinuationResultTaskFromResultTask(
            Task<TAntecedentResult> antecedent, Delegate function, object? state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions) :
            base(function, state, Task.InternalCurrentIfAttached(creationOptions), default, creationOptions, internalOptions, null)
        {
            Debug.Assert(function is Func<Task<TAntecedentResult>, TResult> || function is Func<Task<TAntecedentResult>, object?, TResult>,
                "Invalid delegate type in ContinuationResultTaskFromResultTask");
            m_antecedent = antecedent;
        }

        /// <summary>
        /// Evaluates the value selector of the Task which is passed in as an object and stores the result.
        /// </summary>
        internal override void InnerInvoke()
        {
            // Get and null out the antecedent.  This is crucial to avoid a memory
            // leak with long chains of continuations.
            Task<TAntecedentResult>? antecedent = m_antecedent;
            Debug.Assert(antecedent != null,
                "No antecedent was set for the ContinuationResultTaskFromResultTask.");
            m_antecedent = null;

            // Notify the debugger we're completing an asynchronous wait on a task
            antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();

            // Invoke the delegate
            Debug.Assert(m_action != null);
            if (m_action is Func<Task<TAntecedentResult>, TResult> func)
            {
                m_result = func(antecedent);
                return;
            }

            if (m_action is Func<Task<TAntecedentResult>, object?, TResult> funcWithState)
            {
                m_result = funcWithState(antecedent, m_stateObject);
                return;
            }
            Debug.Fail("Invalid m_action in ContinuationResultTaskFromResultTask");
        }
    }

    // For performance reasons, we don't just have a single way of representing
    // a continuation object.  Rather, we have a hierarchy of types:
    // - TaskContinuation: abstract base that provides a virtual Run method
    //     - StandardTaskContinuation: wraps a task,options,and scheduler, and overrides Run to process the task with that configuration
    //     - AwaitTaskContinuation: base for continuations created through TaskAwaiter; targets default scheduler by default
    //         - TaskSchedulerAwaitTaskContinuation: awaiting with a non-default TaskScheduler
    //         - SynchronizationContextAwaitTaskContinuation: awaiting with a "current" sync ctx

    /// <summary>Represents a continuation.</summary>
    internal abstract class TaskContinuation
    {
        /// <summary>Inlines or schedules the continuation.</summary>
        /// <param name="completedTask">The antecedent task that has completed.</param>
        /// <param name="canInlineContinuationTask">true if inlining is permitted; otherwise, false.</param>
        internal abstract void Run(Task completedTask, bool canInlineContinuationTask);

        /// <summary>Tries to run the task on the current thread, if possible; otherwise, schedules it.</summary>
        /// <param name="task">The task to run</param>
        /// <param name="needsProtection">
        /// true if we need to protect against multiple threads racing to start/cancel the task; otherwise, false.
        /// </param>
        protected static void InlineIfPossibleOrElseQueue(Task task, bool needsProtection)
        {
            Debug.Assert(task != null);
            Debug.Assert(task.m_taskScheduler != null);

            // Set the TASK_STATE_STARTED flag.  This only needs to be done
            // if the task may be canceled or if someone else has a reference to it
            // that may try to execute it.
            if (needsProtection)
            {
                if (!task.MarkStarted())
                    return; // task has been previously started or canceled.  Stop processing.
            }
            else
            {
                task.m_stateFlags |= Task.TASK_STATE_STARTED;
            }

            // Try to inline it but queue if we can't
            try
            {
                if (!task.m_taskScheduler.TryRunInline(task, taskWasPreviouslyQueued: false))
                {
                    task.m_taskScheduler.InternalQueueTask(task);
                }
            }
            catch (Exception e)
            {
                // Either TryRunInline() or QueueTask() threw an exception. Record the exception, marking the task as Faulted.
                // However if it was a ThreadAbortException coming from TryRunInline we need to skip here, 
                // because it would already have been handled in Task.Execute()
                TaskSchedulerException tse = new TaskSchedulerException(e);
                task.AddException(tse);
                task.Finish(false);
                // Don't re-throw.
            }
        }

        //
        // This helper routine is targeted by the debugger.
        //
#if PROJECTN
        [DependencyReductionRoot]
#endif
        internal abstract Delegate[] GetDelegateContinuationsForDebugger();
    }

    /// <summary>Provides the standard implementation of a task continuation.</summary>
    internal class StandardTaskContinuation : TaskContinuation
    {
        /// <summary>The unstarted continuation task.</summary>
        internal readonly Task m_task;
        /// <summary>The options to use with the continuation task.</summary>
        internal readonly TaskContinuationOptions m_options;
        /// <summary>The task scheduler with which to run the continuation task.</summary>
        private readonly TaskScheduler m_taskScheduler;

        /// <summary>Initializes a new continuation.</summary>
        /// <param name="task">The task to be activated.</param>
        /// <param name="options">The continuation options.</param>
        /// <param name="scheduler">The scheduler to use for the continuation.</param>
        internal StandardTaskContinuation(Task task, TaskContinuationOptions options, TaskScheduler scheduler)
        {
            Debug.Assert(task != null, "TaskContinuation ctor: task is null");
            Debug.Assert(scheduler != null, "TaskContinuation ctor: scheduler is null");
            m_task = task;
            m_options = options;
            m_taskScheduler = scheduler;
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(m_task, "Task.ContinueWith: " + task.m_action!.Method.Name);

            if (Task.s_asyncDebuggingEnabled)
                Task.AddToActiveTasks(m_task);
        }

        /// <summary>Invokes the continuation for the target completion task.</summary>
        /// <param name="completedTask">The completed task.</param>
        /// <param name="canInlineContinuationTask">Whether the continuation can be inlined.</param>
        internal override void Run(Task completedTask, bool canInlineContinuationTask)
        {
            Debug.Assert(completedTask != null);
            Debug.Assert(completedTask.IsCompleted, "ContinuationTask.Run(): completedTask not completed");

            // Check if the completion status of the task works with the desired 
            // activation criteria of the TaskContinuationOptions.
            TaskContinuationOptions options = m_options;
            bool isRightKind =
                completedTask.IsCompletedSuccessfully ?
                    (options & TaskContinuationOptions.NotOnRanToCompletion) == 0 :
                    (completedTask.IsCanceled ?
                        (options & TaskContinuationOptions.NotOnCanceled) == 0 :
                        (options & TaskContinuationOptions.NotOnFaulted) == 0);

            // If the completion status is allowed, run the continuation.
            Task continuationTask = m_task;
            if (isRightKind)
            {
                //If the task was cancel before running (e.g a ContinueWhenAll with a cancelled caancelation token)
                //we will still flow it to ScheduleAndStart() were it will check the status before running
                //We check here to avoid faulty logs that contain a join event to an operation that was already set as completed.
                if (!continuationTask.IsCanceled && AsyncCausalityTracer.LoggingOn)
                {
                    // Log now that we are sure that this continuation is being ran
                    AsyncCausalityTracer.TraceOperationRelation(continuationTask, CausalityRelation.AssignDelegate);
                }
                continuationTask.m_taskScheduler = m_taskScheduler;

                // Either run directly or just queue it up for execution, depending
                // on whether synchronous or asynchronous execution is wanted.
                if (canInlineContinuationTask && // inlining is allowed by the caller
                    (options & TaskContinuationOptions.ExecuteSynchronously) != 0) // synchronous execution was requested by the continuation's creator
                {
                    InlineIfPossibleOrElseQueue(continuationTask, needsProtection: true);
                }
                else
                {
                    try { continuationTask.ScheduleAndStart(needsProtection: true); }
                    catch (TaskSchedulerException)
                    {
                        // No further action is necessary -- ScheduleAndStart() already transitioned the 
                        // task to faulted.  But we want to make sure that no exception is thrown from here.
                    }
                }
            }
            // Otherwise, the final state of this task does not match the desired
            // continuation activation criteria; cancel it to denote this.
            else continuationTask.InternalCancel(false);
        }

#pragma warning disable CS8609 // TODO-NULLABLE: Covariant return types (https://github.com/dotnet/roslyn/issues/23268)
        internal override Delegate[]? GetDelegateContinuationsForDebugger()
        {
            if (m_task.m_action == null)
            {
                return m_task.GetDelegateContinuationsForDebugger();
            }

            return new Delegate[] { m_task.m_action };
        }
#pragma warning restore CS8609

    }

    /// <summary>Task continuation for awaiting with a current synchronization context.</summary>
    internal sealed class SynchronizationContextAwaitTaskContinuation : AwaitTaskContinuation
    {
        /// <summary>SendOrPostCallback delegate to invoke the action.</summary>
        private static readonly SendOrPostCallback s_postCallback = state =>
        {
            Debug.Assert(state is Action);
            ((Action)state)();
        };
        /// <summary>Cached delegate for PostAction</summary>
        private static ContextCallback? s_postActionCallback;
        /// <summary>The context with which to run the action.</summary>
        private readonly SynchronizationContext m_syncContext;

        /// <summary>Initializes the SynchronizationContextAwaitTaskContinuation.</summary>
        /// <param name="context">The synchronization context with which to invoke the action.  Must not be null.</param>
        /// <param name="action">The action to invoke. Must not be null.</param>
        /// <param name="flowExecutionContext">Whether to capture and restore ExecutionContext.</param>
        internal SynchronizationContextAwaitTaskContinuation(
            SynchronizationContext context, Action action, bool flowExecutionContext) :
            base(action, flowExecutionContext)
        {
            Debug.Assert(context != null);
            m_syncContext = context;
        }

        /// <summary>Inlines or schedules the continuation.</summary>
        /// <param name="task">The antecedent task, which is ignored.</param>
        /// <param name="canInlineContinuationTask">true if inlining is permitted; otherwise, false.</param>
        internal sealed override void Run(Task task, bool canInlineContinuationTask)
        {
            // If we're allowed to inline, run the action on this thread.
            if (canInlineContinuationTask &&
                m_syncContext == SynchronizationContext.Current)
            {
                RunCallback(GetInvokeActionCallback(), m_action, ref Task.t_currentTask);
            }
            // Otherwise, Post the action back to the SynchronizationContext.
            else
            {
                TplEventSource log = TplEventSource.Log;
                if (log.IsEnabled())
                {
                    m_continuationId = Task.NewId();
                    log.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, m_continuationId);
                }
                RunCallback(GetPostActionCallback(), this, ref Task.t_currentTask);
            }
            // Any exceptions will be handled by RunCallback.
        }

        /// <summary>Calls InvokeOrPostAction(false) on the supplied SynchronizationContextAwaitTaskContinuation.</summary>
        /// <param name="state">The SynchronizationContextAwaitTaskContinuation.</param>
        private static void PostAction(object? state)
        {
            Debug.Assert(state is SynchronizationContextAwaitTaskContinuation);
            var c = (SynchronizationContextAwaitTaskContinuation)state;

            TplEventSource log = TplEventSource.Log;
            if (log.TasksSetActivityIds && c.m_continuationId != 0)
            {
                c.m_syncContext.Post(s_postCallback, GetActionLogDelegate(c.m_continuationId, c.m_action));
            }
            else
            {
                c.m_syncContext.Post(s_postCallback, c.m_action); // s_postCallback is manually cached, as the compiler won't in a SecurityCritical method
            }
        }

        private static Action GetActionLogDelegate(int continuationId, Action action)
        {
            return () =>
                {
                    Guid savedActivityId;
                    Guid activityId = TplEventSource.CreateGuidForTaskID(continuationId);
                    System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(activityId, out savedActivityId);
                    try { action(); }
                    finally { System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(savedActivityId); }
                };
        }

        /// <summary>Gets a cached delegate for the PostAction method.</summary>
        /// <returns>
        /// A delegate for PostAction, which expects a SynchronizationContextAwaitTaskContinuation 
        /// to be passed as state.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ContextCallback GetPostActionCallback()
        {
            ContextCallback? callback = s_postActionCallback;
            if (callback == null) { s_postActionCallback = callback = PostAction; } // lazily initialize SecurityCritical delegate
            return callback;
        }
    }

    /// <summary>Task continuation for awaiting with a task scheduler.</summary>
    internal sealed class TaskSchedulerAwaitTaskContinuation : AwaitTaskContinuation
    {
        /// <summary>The scheduler on which to run the action.</summary>
        private readonly TaskScheduler m_scheduler;

        /// <summary>Initializes the TaskSchedulerAwaitTaskContinuation.</summary>
        /// <param name="scheduler">The task scheduler with which to invoke the action.  Must not be null.</param>
        /// <param name="action">The action to invoke. Must not be null.</param>
        /// <param name="flowExecutionContext">Whether to capture and restore ExecutionContext.</param>
        internal TaskSchedulerAwaitTaskContinuation(
            TaskScheduler scheduler, Action action, bool flowExecutionContext) :
            base(action, flowExecutionContext)
        {
            Debug.Assert(scheduler != null);
            m_scheduler = scheduler;
        }

        /// <summary>Inlines or schedules the continuation.</summary>
        /// <param name="ignored">The antecedent task, which is ignored.</param>
        /// <param name="canInlineContinuationTask">true if inlining is permitted; otherwise, false.</param>
        internal sealed override void Run(Task ignored, bool canInlineContinuationTask)
        {
            // If we're targeting the default scheduler, we can use the faster path provided by the base class.
            if (m_scheduler == TaskScheduler.Default)
            {
                base.Run(ignored, canInlineContinuationTask);
            }
            else
            {
                // We permit inlining if the caller allows us to, and 
                // either we're on a thread pool thread (in which case we're fine running arbitrary code)
                // or we're already on the target scheduler (in which case we'll just ask the scheduler
                // whether it's ok to run here).  We include the IsThreadPoolThread check here, whereas
                // we don't in AwaitTaskContinuation.Run, since here it expands what's allowed as opposed
                // to in AwaitTaskContinuation.Run where it restricts what's allowed.
                bool inlineIfPossible = canInlineContinuationTask &&
                    (TaskScheduler.InternalCurrent == m_scheduler || Thread.CurrentThread.IsThreadPoolThread);

                // Create the continuation task task. If we're allowed to inline, try to do so.  
                // The target scheduler may still deny us from executing on this thread, in which case this'll be queued.
                var task = CreateTask(state =>
                {
                    try
                    {
                        ((Action)state!)();
                    }
                    catch (Exception exception)
                    {
                        Task.ThrowAsync(exception, targetContext: null);
                    }
                }, m_action, m_scheduler);

                if (inlineIfPossible)
                {
                    InlineIfPossibleOrElseQueue(task, needsProtection: false);
                }
                else
                {
                    // We need to run asynchronously, so just schedule the task.
                    try { task.ScheduleAndStart(needsProtection: false); }
                    catch (TaskSchedulerException) { } // No further action is necessary, as ScheduleAndStart already transitioned task to faulted
                }
            }
        }
    }

    /// <summary>Base task continuation class used for await continuations.</summary>
    internal class AwaitTaskContinuation : TaskContinuation, IThreadPoolWorkItem
    {
        /// <summary>The ExecutionContext with which to run the continuation.</summary>
        private readonly ExecutionContext? m_capturedContext;
        /// <summary>The action to invoke.</summary>
        protected readonly Action m_action;

        protected int m_continuationId;

        /// <summary>Initializes the continuation.</summary>
        /// <param name="action">The action to invoke. Must not be null.</param>
        /// <param name="flowExecutionContext">Whether to capture and restore ExecutionContext.</param>
        internal AwaitTaskContinuation(Action action, bool flowExecutionContext)
        {
            Debug.Assert(action != null);
            m_action = action;
            if (flowExecutionContext)
            {
                m_capturedContext = ExecutionContext.Capture();
            }
        }

        /// <summary>Creates a task to run the action with the specified state on the specified scheduler.</summary>
        /// <param name="action">The action to run. Must not be null.</param>
        /// <param name="state">The state to pass to the action. Must not be null.</param>
        /// <param name="scheduler">The scheduler to target.</param>
        /// <returns>The created task.</returns>
        protected Task CreateTask(Action<object?> action, object? state, TaskScheduler scheduler)
        {
            Debug.Assert(action != null);
            Debug.Assert(scheduler != null);

            return new Task(
                action, state, null, default,
                TaskCreationOptions.None, InternalTaskOptions.QueuedByRuntime, scheduler)
            {
                CapturedContext = m_capturedContext
            };
        }

        /// <summary>Inlines or schedules the continuation onto the default scheduler.</summary>
        /// <param name="task">The antecedent task, which is ignored.</param>
        /// <param name="canInlineContinuationTask">true if inlining is permitted; otherwise, false.</param>
        internal override void Run(Task task, bool canInlineContinuationTask)
        {
            // For the base AwaitTaskContinuation, we allow inlining if our caller allows it
            // and if we're in a "valid location" for it.  See the comments on 
            // IsValidLocationForInlining for more about what's valid.  For performance
            // reasons we would like to always inline, but we don't in some cases to avoid
            // running arbitrary amounts of work in suspected "bad locations", like UI threads.
            if (canInlineContinuationTask && IsValidLocationForInlining)
            {
                RunCallback(GetInvokeActionCallback(), m_action, ref Task.t_currentTask); // any exceptions from m_action will be handled by s_callbackRunAction
            }
            else
            {
                TplEventSource log = TplEventSource.Log;
                if (log.IsEnabled())
                {
                    m_continuationId = Task.NewId();
                    log.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, m_continuationId);
                }

                // We couldn't inline, so now we need to schedule it
                ThreadPool.UnsafeQueueUserWorkItemInternal(this, preferLocal: true);
            }
        }

        /// <summary>
        /// Gets whether the current thread is an appropriate location to inline a continuation's execution.
        /// </summary>
        /// <remarks>
        /// Returns whether SynchronizationContext is null and we're in the default scheduler.
        /// If the await had a SynchronizationContext/TaskScheduler where it began and the 
        /// default/ConfigureAwait(true) was used, then we won't be on this path.  If, however, 
        /// ConfigureAwait(false) was used, or the SynchronizationContext and TaskScheduler were 
        /// naturally null/Default, then we might end up here.  If we do, we need to make sure
        /// that we don't execute continuations in a place that isn't set up to handle them, e.g.
        /// running arbitrary amounts of code on the UI thread.  It would be "correct", but very
        /// expensive, to always run the continuations asynchronously, incurring lots of context
        /// switches and allocations and locks and the like.  As such, we employ the heuristic
        /// that if the current thread has a non-null SynchronizationContext or a non-default
        /// scheduler, then we better not run arbitrary continuations here.
        /// </remarks>
        internal static bool IsValidLocationForInlining
        {
            get
            {
                // If there's a SynchronizationContext, we'll be conservative and say 
                // this is a bad location to inline.
                var ctx = SynchronizationContext.Current;
                if (ctx != null && ctx.GetType() != typeof(SynchronizationContext)) return false;

                // Similarly, if there's a non-default TaskScheduler, we'll be conservative
                // and say this is a bad location to inline.
                var sched = TaskScheduler.InternalCurrent;
                return sched == null || sched == TaskScheduler.Default;
            }
        }

        void IThreadPoolWorkItem.Execute()
        {
            var log = TplEventSource.Log;
            ExecutionContext? context = m_capturedContext;

            if (!log.IsEnabled() && context == null)
            {
                m_action();
                return;
            }

            Guid savedActivityId = default;
            if (log.TasksSetActivityIds && m_continuationId != 0)
            {
                Guid activityId = TplEventSource.CreateGuidForTaskID(m_continuationId);
                System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(activityId, out savedActivityId);
            }
            try
            {
                // We're not inside of a task, so t_currentTask doesn't need to be specially maintained.
                // We're on a thread pool thread with no higher-level callers, so exceptions can just propagate.

                ExecutionContext.CheckThreadPoolAndContextsAreDefault();
                // If there's no execution context or Default, just invoke the delegate as ThreadPool is on Default context.
                // We don't have to use ExecutionContext.Run for the Default context here as there is no extra processing after the delegate
                if (context == null || context.IsDefault)
                {
                    m_action();
                }
                // If there is an execution context, get the cached delegate and run the action under the context.
                else
                {
                    ExecutionContext.RunForThreadPoolUnsafe(context, s_invokeAction, m_action);
                }

                // ThreadPoolWorkQueue.Dispatch handles notifications and reset context back to default
            }
            finally
            {
                if (log.TasksSetActivityIds && m_continuationId != 0)
                {
                    System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(savedActivityId);
                }
            }
        }

        /// <summary>Cached delegate that invokes an Action passed as an object parameter.</summary>
        private readonly static ContextCallback s_invokeContextCallback = (state) =>
        {
            Debug.Assert(state is Action);
            ((Action)state)();
        };
        private readonly static Action<Action> s_invokeAction = (action) => action();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ContextCallback GetInvokeActionCallback() => s_invokeContextCallback;

        /// <summary>Runs the callback synchronously with the provided state.</summary>
        /// <param name="callback">The callback to run.</param>
        /// <param name="state">The state to pass to the callback.</param>
        /// <param name="currentTask">A reference to Task.t_currentTask.</param>
        protected void RunCallback(ContextCallback callback, object? state, ref Task? currentTask)
        {
            Debug.Assert(callback != null);
            Debug.Assert(currentTask == Task.t_currentTask);

            // Pretend there's no current task, so that no task is seen as a parent
            // and TaskScheduler.Current does not reflect false information
            var prevCurrentTask = currentTask;
            try
            {
                if (prevCurrentTask != null) currentTask = null;

                ExecutionContext? context = m_capturedContext;
                if (context == null)
                {
                    // If there's no captured context, just run the callback directly.
                    callback(state);
                }
                else
                {
                    // Otherwise, use the captured context to do so.
                    ExecutionContext.RunInternal(context, callback, state);
                }
            }
            catch (Exception exception) // we explicitly do not request handling of dangerous exceptions like AVs
            {
                Task.ThrowAsync(exception, targetContext: null);
            }
            finally
            {
                // Restore the current task information
                if (prevCurrentTask != null) currentTask = prevCurrentTask;
            }
        }

        /// <summary>Invokes or schedules the action to be executed.</summary>
        /// <param name="action">The action to invoke or queue.</param>
        /// <param name="allowInlining">
        /// true to allow inlining, or false to force the action to run asynchronously.
        /// </param>
        /// <remarks>
        /// No ExecutionContext work is performed used.  This method is only used in the
        /// case where a raw Action continuation delegate was stored into the Task, which
        /// only happens in Task.SetContinuationForAwait if execution context flow was disabled
        /// via using TaskAwaiter.UnsafeOnCompleted or a similar path.
        /// </remarks>
        internal static void RunOrScheduleAction(Action action, bool allowInlining)
        {
            ref Task? currentTask = ref Task.t_currentTask;
            Task? prevCurrentTask = currentTask;

            // If we're not allowed to run here, schedule the action
            if (!allowInlining || !IsValidLocationForInlining)
            {
                UnsafeScheduleAction(action, prevCurrentTask);
                return;
            }

            // Otherwise, run it, making sure that t_currentTask is null'd out appropriately during the execution
            try
            {
                if (prevCurrentTask != null) currentTask = null;
                action();
            }
            catch (Exception exception)
            {
                Task.ThrowAsync(exception, targetContext: null);
            }
            finally
            {
                if (prevCurrentTask != null) currentTask = prevCurrentTask;
            }
        }

        /// <summary>Invokes or schedules the action to be executed.</summary>
        /// <param name="box">The <see cref="IAsyncStateMachineBox"/> that needs to be invoked or queued.</param>
        /// <param name="allowInlining">
        /// true to allow inlining, or false to force the box's action to run asynchronously.
        /// </param>
        internal static void RunOrScheduleAction(IAsyncStateMachineBox box, bool allowInlining)
        {
            // Same logic as in the RunOrScheduleAction(Action, ...) overload, except invoking
            // box.Invoke instead of action().

            ref Task? currentTask = ref Task.t_currentTask;
            Task? prevCurrentTask = currentTask;

            // If we're not allowed to run here, schedule the action
            if (!allowInlining || !IsValidLocationForInlining)
            {
                // If logging is disabled, we can simply queue the box itself as a custom work
                // item, and its work item execution will just invoke its MoveNext.  However, if
                // logging is enabled, there is pre/post-work we need to do around logging to
                // match what's done for other continuations, and that requires flowing additional
                // information into the continuation, which we don't want to burden other cases of the
                // box with... so, in that case we just delegate to the AwaitTaskContinuation-based
                // path that already handles this, albeit at the expense of allocating the ATC
                // object, and potentially forcing the box's delegate into existence, when logging
                // is enabled.
                if (TplEventSource.Log.IsEnabled())
                {
                    UnsafeScheduleAction(box.MoveNextAction, prevCurrentTask);
                }
                else
                {
                    ThreadPool.UnsafeQueueUserWorkItemInternal(box, preferLocal: true);
                }
                return;
            }

            // Otherwise, run it, making sure that t_currentTask is null'd out appropriately during the execution
            try
            {
                if (prevCurrentTask != null) currentTask = null;
                box.MoveNext();
            }
            catch (Exception exception)
            {
                Task.ThrowAsync(exception, targetContext: null);
            }
            finally
            {
                if (prevCurrentTask != null) currentTask = prevCurrentTask;
            }
        }

        /// <summary>Schedules the action to be executed.  No ExecutionContext work is performed used.</summary>
        /// <param name="action">The action to invoke or queue.</param>
        /// <param name="task">The task scheduling the action.</param>
        internal static void UnsafeScheduleAction(Action action, Task? task)
        {
            AwaitTaskContinuation atc = new AwaitTaskContinuation(action, flowExecutionContext: false);

            var log = TplEventSource.Log;
            if (log.IsEnabled() && task != null)
            {
                atc.m_continuationId = Task.NewId();
                log.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, atc.m_continuationId);
            }

            ThreadPool.UnsafeQueueUserWorkItemInternal(atc, preferLocal: true);
        }

        internal override Delegate[] GetDelegateContinuationsForDebugger()
        {
            Debug.Assert(m_action != null);
            return new Delegate[] { AsyncMethodBuilderCore.TryGetStateMachineForDebugger(m_action) };
        }
    }
}
