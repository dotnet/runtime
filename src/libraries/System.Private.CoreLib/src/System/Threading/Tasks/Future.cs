// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// A task that produces a value.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Represents an asynchronous operation that produces a result at some time in the future.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the result produced by this <see cref="Task{TResult}"/>.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="Task{TResult}"/> instances may be created in a variety of ways. The most common approach is by
    /// using the task's <see cref="Factory"/> property to retrieve a <see
    /// cref="System.Threading.Tasks.TaskFactory{TResult}"/> instance that can be used to create tasks for several
    /// purposes. For example, to create a <see cref="Task{TResult}"/> that runs a function, the factory's StartNew
    /// method may be used:
    /// <code>
    /// // C#
    /// var t = Task&lt;int&gt;.Factory.StartNew(() => GenerateResult());
    /// - or -
    /// var t = Task.Factory.StartNew(() => GenerateResult());
    ///
    /// ' Visual Basic
    /// Dim t = Task&lt;int&gt;.Factory.StartNew(Function() GenerateResult())
    /// - or -
    /// Dim t = Task.Factory.StartNew(Function() GenerateResult())
    /// </code>
    /// </para>
    /// <para>
    /// The <see cref="Task{TResult}"/> class also provides constructors that initialize the task but that do not
    /// schedule it for execution. For performance reasons, the StartNew method should be the
    /// preferred mechanism for creating and scheduling computational tasks, but for scenarios where creation
    /// and scheduling must be separated, the constructors may be used, and the task's
    /// <see cref="System.Threading.Tasks.Task.Start()">Start</see>
    /// method may then be used to schedule the task for execution at a later time.
    /// </para>
    /// <para>
    /// All members of <see cref="Task{TResult}"/>, except for
    /// <see cref="System.Threading.Tasks.Task.Dispose()">Dispose</see>, are thread-safe
    /// and may be used from multiple threads concurrently.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(SystemThreadingTasks_FutureDebugView<>))]
    [DebuggerDisplay("Id = {Id}, Status = {Status}, Method = {DebuggerDisplayMethodDescription}, Result = {DebuggerDisplayResultDescription}")]
    public class Task<TResult> : Task
    {
        // The value itself, if set.
        internal TResult? m_result;

        private static readonly TaskFactory<TResult> s_Factory = new TaskFactory<TResult>();

        // Extract rarely used helper for a static method in a separate type so that the Func<Task<Task>, Task<TResult>>
        // generic instantiations don't contribute to all Task instantiations, but only those where WhenAny is used.
        internal static class TaskWhenAnyCast
        {
            // Delegate used by:
            //     public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks);
            //     public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks);
            // Used to "cast" from Task<Task> to Task<Task<TResult>>.
            internal static readonly Func<Task<Task>, Task<TResult>> Value = completed => (Task<TResult>)completed.Result;
        }

        // Construct a promise-style task without any options.
        internal Task()
        {
        }

        // Construct a promise-style task with state and options.
        internal Task(object? state, TaskCreationOptions options) :
            base(state, options, promiseStyle: true)
        {
        }


        // Construct a pre-completed Task<TResult>
        internal Task(TResult result) :
            base(false, TaskCreationOptions.None, default)
        {
            m_result = result;
        }

        internal Task(bool canceled, TResult? result, TaskCreationOptions creationOptions, CancellationToken ct)
            : base(canceled, creationOptions, ct)
        {
            if (!canceled)
            {
                m_result = result;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified function.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        public Task(Func<TResult> function)
            : this(function, null, default,
                TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
        }


        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified function.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be assigned to this task.</param>
        /// <exception cref="System.ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task(Func<TResult> function, CancellationToken cancellationToken)
            : this(function, null, cancellationToken,
                TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified function and creation options.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <param name="creationOptions">
        /// The <see cref="System.Threading.Tasks.TaskCreationOptions">TaskCreationOptions</see> used to
        /// customize the task's behavior.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskCreationOptions"/>.
        /// </exception>
        public Task(Func<TResult> function, TaskCreationOptions creationOptions)
            : this(function, Task.InternalCurrentIfAttached(creationOptions), default, creationOptions, InternalTaskOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified function and creation options.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <param name="creationOptions">
        /// The <see cref="System.Threading.Tasks.TaskCreationOptions">TaskCreationOptions</see> used to
        /// customize the task's behavior.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskCreationOptions"/>.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
            : this(function, Task.InternalCurrentIfAttached(creationOptions), cancellationToken, creationOptions, InternalTaskOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified function and state.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <param name="state">An object representing data to be used by the action.</param>
        /// <exception cref="System.ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        public Task(Func<object?, TResult> function, object? state)
            : this(function, state, null, default,
                TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified action, state, and options.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <param name="state">An object representing data to be used by the function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be assigned to the new task.</param>
        /// <exception cref="System.ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task(Func<object?, TResult> function, object? state, CancellationToken cancellationToken)
            : this(function, state, null, cancellationToken,
                    TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified action, state, and options.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <param name="state">An object representing data to be used by the function.</param>
        /// <param name="creationOptions">
        /// The <see cref="System.Threading.Tasks.TaskCreationOptions">TaskCreationOptions</see> used to
        /// customize the task's behavior.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskCreationOptions"/>.
        /// </exception>
        public Task(Func<object?, TResult> function, object? state, TaskCreationOptions creationOptions)
            : this(function, state, Task.InternalCurrentIfAttached(creationOptions), default,
                    creationOptions, InternalTaskOptions.None, null)
        {
        }


        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified action, state, and options.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <param name="state">An object representing data to be used by the function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be assigned to the new task.</param>
        /// <param name="creationOptions">
        /// The <see cref="System.Threading.Tasks.TaskCreationOptions">TaskCreationOptions</see> used to
        /// customize the task's behavior.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskCreationOptions"/>.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task(Func<object?, TResult> function, object? state, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
            : this(function, state, Task.InternalCurrentIfAttached(creationOptions), cancellationToken,
                    creationOptions, InternalTaskOptions.None, null)
        {
        }

        /// <summary>
        /// Creates a new future object.
        /// </summary>
        /// <param name="parent">The parent task for this future.</param>
        /// <param name="valueSelector">A function that yields the future value.</param>
        /// <param name="scheduler">The task scheduler which will be used to execute the future.</param>
        /// <param name="cancellationToken">The CancellationToken for the task.</param>
        /// <param name="creationOptions">Options to control the future's behavior.</param>
        /// <param name="internalOptions">Internal options to control the future's behavior.</param>
        internal Task(Func<TResult> valueSelector, Task? parent, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler? scheduler) :
            base(valueSelector, null, parent, cancellationToken, creationOptions, internalOptions, scheduler)
        {
        }

        /// <summary>
        /// Creates a new future object.
        /// </summary>
        /// <param name="parent">The parent task for this future.</param>
        /// <param name="state">An object containing data to be used by the action; may be null.</param>
        /// <param name="valueSelector">A function that yields the future value.</param>
        /// <param name="cancellationToken">The CancellationToken for the task.</param>
        /// <param name="scheduler">The task scheduler which will be used to execute the future.</param>
        /// <param name="creationOptions">Options to control the future's behavior.</param>
        /// <param name="internalOptions">Internal options to control the future's behavior.</param>
        internal Task(Delegate valueSelector, object? state, Task? parent, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler? scheduler) :
            base(valueSelector, state, parent, cancellationToken, creationOptions, internalOptions, scheduler)
        {
        }


        // Internal method used by TaskFactory<TResult>.StartNew() methods
        internal static Task<TResult> StartNew(Task? parent, Func<TResult> function, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler)
        {
            if (function == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.function);
            }
            if (scheduler == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.scheduler);
            }

            // Create and schedule the future.
            Task<TResult> f = new Task<TResult>(function, parent, cancellationToken, creationOptions, internalOptions | InternalTaskOptions.QueuedByRuntime, scheduler);

            f.ScheduleAndStart(false);
            return f;
        }

        // Internal method used by TaskFactory<TResult>.StartNew() methods
        internal static Task<TResult> StartNew(Task? parent, Func<object?, TResult> function, object? state, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler)
        {
            if (function == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.function);
            }
            if (scheduler == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.scheduler);
            }

            // Create and schedule the future.
            Task<TResult> f = new Task<TResult>(function, state, parent, cancellationToken, creationOptions, internalOptions | InternalTaskOptions.QueuedByRuntime, scheduler);

            f.ScheduleAndStart(false);
            return f;
        }

        // Debugger support
        private string DebuggerDisplayResultDescription =>
            IsCompletedSuccessfully ? "" + m_result : SR.TaskT_DebuggerNoResult;

        // Debugger support
        private string DebuggerDisplayMethodDescription =>
            m_action?.Method.ToString() ?? "{null}";


        // internal helper function breaks out logic used by TaskCompletionSource
        internal bool TrySetResult(TResult? result)
        {
            Debug.Assert(m_action == null, "Task<T>.TrySetResult(): non-null m_action");

            bool returnValue = false;

            // "Reserve" the completion for this task, while making sure that: (1) No prior reservation
            // has been made, (2) The result has not already been set, (3) An exception has not previously
            // been recorded, and (4) Cancellation has not been requested.
            //
            // If the reservation is successful, then set the result and finish completion processing.
            if (AtomicStateUpdate(TASK_STATE_COMPLETION_RESERVED,
                    TASK_STATE_COMPLETION_RESERVED | TASK_STATE_RAN_TO_COMPLETION | TASK_STATE_FAULTED | TASK_STATE_CANCELED))
            {
                m_result = result;

                // Signal completion, for waiting tasks

                // This logic used to be:
                //     Finish(false);
                // However, that goes through a windy code path, involves many non-inlineable functions
                // and which can be summarized more concisely with the following snippet from
                // FinishStageTwo, omitting everything that doesn't pertain to TrySetResult.
                Interlocked.Exchange(ref m_stateFlags, m_stateFlags | TASK_STATE_RAN_TO_COMPLETION);
                ContingentProperties? props = m_contingentProperties;
                if (props != null)
                {
                    NotifyParentIfPotentiallyAttachedTask();
                    props.SetCompleted();
                }
                FinishContinuations();
                returnValue = true;
            }

            return returnValue;
        }

        // Transitions the promise task into a successfully completed state with the specified result.
        // This is dangerous, as no synchronization is used, and thus must only be used
        // before this task is handed out to any consumers, before any continuations are hooked up,
        // before its wait handle is accessed, etc.  It's use is limited to places like in FromAsync
        // where the operation completes synchronously, and thus we know we can forcefully complete
        // the task, avoiding expensive completion paths, before the task is actually given to anyone.
        internal void DangerousSetResult(TResult result)
        {
            Debug.Assert(!IsCompleted, "The promise must not yet be completed.");

            // If we have a parent, we need to notify it of the completion.  Take the slow path to handle that.
            if (m_contingentProperties?.m_parent != null)
            {
                bool success = TrySetResult(result);

                // Nobody else has had a chance to complete this Task yet, so we should succeed.
                Debug.Assert(success);
            }
            else
            {
                m_result = result;
                m_stateFlags |= TASK_STATE_RAN_TO_COMPLETION;
            }
        }

        /// <summary>
        /// Gets the result value of this <see cref="Task{TResult}"/>.
        /// </summary>
        /// <remarks>
        /// The get accessor for this property ensures that the asynchronous operation is complete before
        /// returning. Once the result of the computation is available, it is stored and will be returned
        /// immediately on later calls to <see cref="Result"/>.
        /// </remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TResult Result =>
            IsWaitNotificationEnabledOrNotRanToCompletion ?
                GetResultCore(waitCompletionNotification: true) :
                m_result!;

        /// <summary>
        /// Gets the result value of this <see cref="Task{TResult}"/> once the task has completed successfully.
        /// </summary>
        /// <remarks>
        /// This version of Result should only be used if the task completed successfully and if there's
        /// no debugger wait notification enabled for this task.
        /// </remarks>
        internal TResult ResultOnSuccess
        {
            get
            {
                Debug.Assert(!IsWaitNotificationEnabledOrNotRanToCompletion,
                    "Should only be used when the task completed successfully and there's no wait notification enabled");
                return m_result!;
            }
        }

        // Implements Result.  Result delegates to this method if the result isn't already available.
        internal TResult GetResultCore(bool waitCompletionNotification)
        {
            // If the result has not been calculated yet, wait for it.
            if (!IsCompleted) InternalWait(Timeout.Infinite, default); // won't throw if task faulted or canceled; that's handled below

            // Notify the debugger of the wait completion if it's requested such a notification
            if (waitCompletionNotification) NotifyDebuggerOfWaitCompletionIfNecessary();

            // Throw an exception if appropriate.
            if (!IsCompletedSuccessfully) ThrowIfExceptional(includeTaskCanceledExceptions: true);

            // We shouldn't be here if the result has not been set.
            Debug.Assert(IsCompletedSuccessfully, "Task<T>.Result getter: Expected result to have been set.");

            return m_result!;
        }

        /// <summary>
        /// Provides access to factory methods for creating <see cref="Task{TResult}"/> instances.
        /// </summary>
        /// <remarks>
        /// The factory returned from <see cref="Factory"/> is a default instance
        /// of <see cref="System.Threading.Tasks.TaskFactory{TResult}"/>, as would result from using
        /// the default constructor on the factory type.
        /// </remarks>
        public static new TaskFactory<TResult> Factory => s_Factory;

        /// <summary>
        /// Evaluates the value selector of the Task which is passed in as an object and stores the result.
        /// </summary>
        internal override void InnerInvoke()
        {
            // Invoke the delegate
            Debug.Assert(m_action != null);
            if (m_action is Func<TResult> func)
            {
                m_result = func();
                return;
            }

            if (m_action is Func<object?, TResult> funcWithState)
            {
                m_result = funcWithState(m_stateObject);
                return;
            }
            Debug.Fail("Invalid m_action in Task<TResult>");
        }

        #region Await Support

        /// <summary>Gets an awaiter used to await this <see cref="System.Threading.Tasks.Task{TResult}"/>.</summary>
        /// <returns>An awaiter instance.</returns>
        /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
        public new TaskAwaiter<TResult> GetAwaiter()
        {
            return new TaskAwaiter<TResult>(this);
        }

        /// <summary>Configures an awaiter used to await this <see cref="System.Threading.Tasks.Task{TResult}"/>.</summary>
        /// <param name="continueOnCapturedContext">
        /// true to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        /// <returns>An object used to await this task.</returns>
        public new ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext)
        {
            return new ConfiguredTaskAwaitable<TResult>(this, continueOnCapturedContext);
        }

        #endregion

        #region Continuation methods

        #region Action<Task<TResult>> continuations

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>> continuationAction)
        {
            return ContinueWith(continuationAction, TaskScheduler.Current, default, TaskContinuationOptions.None);
        }


        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new continuation task.</param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>> continuationAction, CancellationToken cancellationToken)
        {
            return ContinueWith(continuationAction, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None);
        }


        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <param name="scheduler">
        /// The <see cref="TaskScheduler"/> to associate with the continuation task and to use for its execution.
        /// </param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="scheduler"/> argument is null.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>> continuationAction, TaskScheduler scheduler)
        {
            return ContinueWith(continuationAction, scheduler, default, TaskContinuationOptions.None);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <param name="continuationOptions">
        /// Options for when the continuation is scheduled and how it behaves. This includes criteria, such
        /// as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.OnlyOnCanceled">OnlyOnCanceled</see>, as
        /// well as execution options, such as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously">ExecuteSynchronously</see>.
        /// </param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed. If the continuation criteria specified through the <paramref
        /// name="continuationOptions"/> parameter are not met, the continuation task will be canceled
        /// instead of scheduled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="continuationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>> continuationAction, TaskContinuationOptions continuationOptions)
        {
            return ContinueWith(continuationAction, TaskScheduler.Current, default, continuationOptions);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">
        /// Options for when the continuation is scheduled and how it behaves. This includes criteria, such
        /// as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.OnlyOnCanceled">OnlyOnCanceled</see>, as
        /// well as execution options, such as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously">ExecuteSynchronously</see>.
        /// </param>
        /// <param name="scheduler">
        /// The <see cref="TaskScheduler"/> to associate with the continuation task and to use for its
        /// execution.
        /// </param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed. If the criteria specified through the <paramref name="continuationOptions"/> parameter
        /// are not met, the continuation task will be canceled instead of scheduled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="continuationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="scheduler"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>> continuationAction, CancellationToken cancellationToken,
                                 TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return ContinueWith(continuationAction, scheduler, cancellationToken, continuationOptions);
        }

        // Same as the above overload, only with a stack mark.
        internal Task ContinueWith(Action<Task<TResult>> continuationAction, TaskScheduler scheduler, CancellationToken cancellationToken,
                                   TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.continuationAction);
            }

            if (scheduler == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.scheduler);
            }

            CreationOptionsFromContinuationOptions(
                continuationOptions,
                out TaskCreationOptions creationOptions,
                out InternalTaskOptions internalOptions);

            Task continuationTask = new ContinuationTaskFromResultTask<TResult>(
                this, continuationAction, null,
                creationOptions, internalOptions
            );

            // Register the continuation.  If synchronous execution is requested, this may
            // actually invoke the continuation before returning.
            ContinueWithCore(continuationTask, scheduler, cancellationToken, continuationOptions);

            return continuationTask;
        }
        #endregion

        #region Action<Task<TResult>, Object> continuations

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation action.</param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>, object?> continuationAction, object? state)
        {
            return ContinueWith(continuationAction, state, TaskScheduler.Current, default, TaskContinuationOptions.None);
        }


        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation action.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new continuation task.</param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>, object?> continuationAction, object? state, CancellationToken cancellationToken)
        {
            return ContinueWith(continuationAction, state, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None);
        }


        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation action.</param>
        /// <param name="scheduler">
        /// The <see cref="TaskScheduler"/> to associate with the continuation task and to use for its execution.
        /// </param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="scheduler"/> argument is null.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>, object?> continuationAction, object? state, TaskScheduler scheduler)
        {
            return ContinueWith(continuationAction, state, scheduler, default, TaskContinuationOptions.None);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation action.</param>
        /// <param name="continuationOptions">
        /// Options for when the continuation is scheduled and how it behaves. This includes criteria, such
        /// as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.OnlyOnCanceled">OnlyOnCanceled</see>, as
        /// well as execution options, such as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously">ExecuteSynchronously</see>.
        /// </param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed. If the continuation criteria specified through the <paramref
        /// name="continuationOptions"/> parameter are not met, the continuation task will be canceled
        /// instead of scheduled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="continuationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>, object?> continuationAction, object? state, TaskContinuationOptions continuationOptions)
        {
            return ContinueWith(continuationAction, state, TaskScheduler.Current, default, continuationOptions);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation action.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">
        /// Options for when the continuation is scheduled and how it behaves. This includes criteria, such
        /// as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.OnlyOnCanceled">OnlyOnCanceled</see>, as
        /// well as execution options, such as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously">ExecuteSynchronously</see>.
        /// </param>
        /// <param name="scheduler">
        /// The <see cref="TaskScheduler"/> to associate with the continuation task and to use for its
        /// execution.
        /// </param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed. If the criteria specified through the <paramref name="continuationOptions"/> parameter
        /// are not met, the continuation task will be canceled instead of scheduled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="continuationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="scheduler"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task ContinueWith(Action<Task<TResult>, object?> continuationAction, object? state, CancellationToken cancellationToken,
                                 TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return ContinueWith(continuationAction, state, scheduler, cancellationToken, continuationOptions);
        }

        // Same as the above overload, only with a stack mark.
        internal Task ContinueWith(Action<Task<TResult>, object?> continuationAction, object? state, TaskScheduler scheduler, CancellationToken cancellationToken,
                                   TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.continuationAction);
            }

            if (scheduler == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.scheduler);
            }

            CreationOptionsFromContinuationOptions(
                continuationOptions,
                out TaskCreationOptions creationOptions,
                out InternalTaskOptions internalOptions);

            Task continuationTask = new ContinuationTaskFromResultTask<TResult>(
                this, continuationAction, state,
                creationOptions, internalOptions
            );

            // Register the continuation.  If synchronous execution is requested, this may
            // actually invoke the continuation before returning.
            ContinueWithCore(continuationTask, scheduler, cancellationToken, continuationOptions);

            return continuationTask;
        }

        #endregion

        #region Func<Task<TResult>,TNewResult> continuations

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current
        /// task has completed, whether it completes due to running to completion successfully, faulting due
        /// to an unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction)
        {
            return ContinueWith<TNewResult>(continuationFunction, TaskScheduler.Current, default, TaskContinuationOptions.None);
        }


        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current
        /// task has completed, whether it completes due to running to completion successfully, faulting due
        /// to an unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken)
        {
            return ContinueWith<TNewResult>(continuationFunction, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes.  When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <param name="scheduler">
        /// The <see cref="TaskScheduler"/> to associate with the continuation task and to use for its execution.
        /// </param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="scheduler"/> argument is null.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, TaskScheduler scheduler)
        {
            return ContinueWith<TNewResult>(continuationFunction, scheduler, default, TaskContinuationOptions.None);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <param name="continuationOptions">
        /// Options for when the continuation is scheduled and how it behaves. This includes criteria, such
        /// as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.OnlyOnCanceled">OnlyOnCanceled</see>, as
        /// well as execution options, such as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously">ExecuteSynchronously</see>.
        /// </param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// <para>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current
        /// task has completed, whether it completes due to running to completion successfully, faulting due
        /// to an unhandled exception, or exiting out early due to being canceled.
        /// </para>
        /// <para>
        /// The <paramref name="continuationFunction"/>, when executed, should return a <see
        /// cref="Task{TNewResult}"/>. This task's completion state will be transferred to the task returned
        /// from the ContinueWith call.
        /// </para>
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="continuationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            return ContinueWith<TNewResult>(continuationFunction, TaskScheduler.Current, default, continuationOptions);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be passed as
        /// an argument this completed task.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <param name="continuationOptions">
        /// Options for when the continuation is scheduled and how it behaves. This includes criteria, such
        /// as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.OnlyOnCanceled">OnlyOnCanceled</see>, as
        /// well as execution options, such as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously">ExecuteSynchronously</see>.
        /// </param>
        /// <param name="scheduler">
        /// The <see cref="TaskScheduler"/> to associate with the continuation task and to use for its
        /// execution.
        /// </param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// <para>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </para>
        /// <para>
        /// The <paramref name="continuationFunction"/>, when executed, should return a <see cref="Task{TNewResult}"/>.
        /// This task's completion state will be transferred to the task returned from the
        /// ContinueWith call.
        /// </para>
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="continuationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="scheduler"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return ContinueWith<TNewResult>(continuationFunction, scheduler, cancellationToken, continuationOptions);
        }

        // Same as the above overload, just with a stack mark.
        internal Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, TaskScheduler scheduler,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.continuationFunction);
            }

            if (scheduler == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.scheduler);
            }

            CreationOptionsFromContinuationOptions(
                continuationOptions,
                out TaskCreationOptions creationOptions,
                out InternalTaskOptions internalOptions);

            Task<TNewResult> continuationFuture = new ContinuationResultTaskFromResultTask<TResult, TNewResult>(
                this, continuationFunction, null,
                creationOptions, internalOptions
            );

            // Register the continuation.  If synchronous execution is requested, this may
            // actually invoke the continuation before returning.
            ContinueWithCore(continuationFuture, scheduler, cancellationToken, continuationOptions);

            return continuationFuture;
        }
        #endregion

        #region Func<Task<TResult>, Object,TNewResult> continuations

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation function.</param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current
        /// task has completed, whether it completes due to running to completion successfully, faulting due
        /// to an unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object?, TNewResult> continuationFunction, object? state)
        {
            return ContinueWith<TNewResult>(continuationFunction, state, TaskScheduler.Current, default, TaskContinuationOptions.None);
        }


        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current
        /// task has completed, whether it completes due to running to completion successfully, faulting due
        /// to an unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object?, TNewResult> continuationFunction, object? state,
            CancellationToken cancellationToken)
        {
            return ContinueWith<TNewResult>(continuationFunction, state, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes.  When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation function.</param>
        /// <param name="scheduler">
        /// The <see cref="TaskScheduler"/> to associate with the continuation task and to use for its execution.
        /// </param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="scheduler"/> argument is null.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object?, TNewResult> continuationFunction, object? state,
            TaskScheduler scheduler)
        {
            return ContinueWith<TNewResult>(continuationFunction, state, scheduler, default, TaskContinuationOptions.None);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation function.</param>
        /// <param name="continuationOptions">
        /// Options for when the continuation is scheduled and how it behaves. This includes criteria, such
        /// as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.OnlyOnCanceled">OnlyOnCanceled</see>, as
        /// well as execution options, such as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously">ExecuteSynchronously</see>.
        /// </param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// <para>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current
        /// task has completed, whether it completes due to running to completion successfully, faulting due
        /// to an unhandled exception, or exiting out early due to being canceled.
        /// </para>
        /// <para>
        /// The <paramref name="continuationFunction"/>, when executed, should return a <see
        /// cref="Task{TNewResult}"/>. This task's completion state will be transferred to the task returned
        /// from the ContinueWith call.
        /// </para>
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="continuationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object?, TNewResult> continuationFunction, object? state,
            TaskContinuationOptions continuationOptions)
        {
            return ContinueWith<TNewResult>(continuationFunction, state, TaskScheduler.Current, default, continuationOptions);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task{TResult}"/> completes.
        /// </summary>
        /// <typeparam name="TNewResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task{TResult}"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <param name="continuationOptions">
        /// Options for when the continuation is scheduled and how it behaves. This includes criteria, such
        /// as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.OnlyOnCanceled">OnlyOnCanceled</see>, as
        /// well as execution options, such as <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously">ExecuteSynchronously</see>.
        /// </param>
        /// <param name="scheduler">
        /// The <see cref="TaskScheduler"/> to associate with the continuation task and to use for its
        /// execution.
        /// </param>
        /// <returns>A new continuation <see cref="Task{TNewResult}"/>.</returns>
        /// <remarks>
        /// <para>
        /// The returned <see cref="Task{TNewResult}"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </para>
        /// <para>
        /// The <paramref name="continuationFunction"/>, when executed, should return a <see cref="Task{TNewResult}"/>.
        /// This task's completion state will be transferred to the task returned from the
        /// ContinueWith call.
        /// </para>
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <paramref name="continuationOptions"/> argument specifies an invalid value for <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="scheduler"/> argument is null.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object?, TNewResult> continuationFunction, object? state,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return ContinueWith<TNewResult>(continuationFunction, state, scheduler, cancellationToken, continuationOptions);
        }

        // Same as the above overload, just with a stack mark.
        internal Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, object?, TNewResult> continuationFunction, object? state,
            TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.continuationFunction);
            }

            if (scheduler == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.scheduler);
            }

            CreationOptionsFromContinuationOptions(
                continuationOptions,
                out TaskCreationOptions creationOptions,
                out InternalTaskOptions internalOptions);

            Task<TNewResult> continuationFuture = new ContinuationResultTaskFromResultTask<TResult, TNewResult>(
                this, continuationFunction, state,
                creationOptions, internalOptions
            );

            // Register the continuation.  If synchronous execution is requested, this may
            // actually invoke the continuation before returning.
            ContinueWithCore(continuationFuture, scheduler, cancellationToken, continuationOptions);

            return continuationFuture;
        }

        #endregion

        #endregion
    }

    // Proxy class for better debugging experience
    internal class SystemThreadingTasks_FutureDebugView<TResult>
    {
        private readonly Task<TResult> m_task;

        public SystemThreadingTasks_FutureDebugView(Task<TResult> task)
        {
            Debug.Assert(task != null);
            m_task = task;
        }

        public TResult? Result => m_task.Status == TaskStatus.RanToCompletion ? m_task.Result : default;
        public object? AsyncState => m_task.AsyncState;
        public TaskCreationOptions CreationOptions => m_task.CreationOptions;
        public Exception? Exception => m_task.Exception;
        public int Id => m_task.Id;
        public bool CancellationPending => (m_task.Status == TaskStatus.WaitingToRun) && m_task.CancellationToken.IsCancellationRequested;
        public TaskStatus Status => m_task.Status;
    }
}
