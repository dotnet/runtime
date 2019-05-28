// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// There are a plethora of common patterns for which Tasks are created.  TaskFactory encodes 
// these patterns into helper methods.  These helpers also pick up default configuration settings 
// applicable to the entire factory and configurable through its constructors.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Security;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides support for creating and scheduling
    /// <see cref="T:System.Threading.Tasks.Task">Tasks</see>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There are many common patterns for which tasks are relevant. The <see cref="TaskFactory"/>
    /// class encodes some of these patterns into methods that pick up default settings, which are
    /// configurable through its constructors.
    /// </para>
    /// <para>
    /// A default instance of <see cref="TaskFactory"/> is available through the
    /// <see cref="System.Threading.Tasks.Task.Factory">Task.Factory</see> property.
    /// </para>
    /// </remarks>
    public class TaskFactory
    {
        // member variables
        private readonly CancellationToken m_defaultCancellationToken;
        private readonly TaskScheduler? m_defaultScheduler;
        private readonly TaskCreationOptions m_defaultCreationOptions;
        private readonly TaskContinuationOptions m_defaultContinuationOptions;

        private TaskScheduler DefaultScheduler => m_defaultScheduler ?? TaskScheduler.Current;

        // sister method to above property -- avoids a TLS lookup
        private TaskScheduler GetDefaultScheduler(Task? currTask)
        {
            return
                m_defaultScheduler ??
                (currTask != null && (currTask.CreationOptions & TaskCreationOptions.HideScheduler) == 0 ? currTask.ExecutingTaskScheduler! : // a "current" task must be executing, which means it must have a scheduler
                 TaskScheduler.Default);
        }

        /* Constructors */

        // ctor parameters provide defaults for the factory, which can be overridden by options provided to
        // specific calls on the factory


        /// <summary>
        /// Initializes a <see cref="TaskFactory"/> instance with the default configuration.
        /// </summary>
        /// <remarks>
        /// This constructor creates a <see cref="TaskFactory"/> instance with a default configuration. The
        /// <see cref="TaskCreationOptions"/> property is initialized to
        /// <see cref="System.Threading.Tasks.TaskCreationOptions.None">TaskCreationOptions.None</see>, the
        /// <see cref="TaskContinuationOptions"/> property is initialized to <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.None">TaskContinuationOptions.None</see>,
        /// and the <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see> property is
        /// initialized to the current scheduler (see <see
        /// cref="System.Threading.Tasks.TaskScheduler.Current">TaskScheduler.Current</see>).
        /// </remarks>
        public TaskFactory()
            : this(default, TaskCreationOptions.None, TaskContinuationOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a <see cref="TaskFactory"/> instance with the specified configuration.
        /// </summary>
        /// <param name="cancellationToken">The default <see cref="CancellationToken"/> that will be assigned 
        /// to tasks created by this <see cref="TaskFactory"/> unless another CancellationToken is explicitly specified 
        /// while calling the factory methods.</param>
        /// <remarks>
        /// This constructor creates a <see cref="TaskFactory"/> instance with a default configuration. The
        /// <see cref="TaskCreationOptions"/> property is initialized to
        /// <see cref="System.Threading.Tasks.TaskCreationOptions.None">TaskCreationOptions.None</see>, the
        /// <see cref="TaskContinuationOptions"/> property is initialized to <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.None">TaskContinuationOptions.None</see>,
        /// and the <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see> property is
        /// initialized to the current scheduler (see <see
        /// cref="System.Threading.Tasks.TaskScheduler.Current">TaskScheduler.Current</see>).
        /// </remarks>
        public TaskFactory(CancellationToken cancellationToken)
            : this(cancellationToken, TaskCreationOptions.None, TaskContinuationOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a <see cref="TaskFactory"/> instance with the specified configuration.
        /// </summary>
        /// <param name="scheduler">
        /// The <see cref="System.Threading.Tasks.TaskScheduler">
        /// TaskScheduler</see> to use to schedule any tasks created with this TaskFactory. A null value
        /// indicates that the current TaskScheduler should be used.
        /// </param>
        /// <remarks>
        /// With this constructor, the
        /// <see cref="TaskCreationOptions"/> property is initialized to
        /// <see cref="System.Threading.Tasks.TaskCreationOptions.None">TaskCreationOptions.None</see>, the
        /// <see cref="TaskContinuationOptions"/> property is initialized to <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.None">TaskContinuationOptions.None</see>,
        /// and the <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see> property is
        /// initialized to <paramref name="scheduler"/>, unless it's null, in which case the property is
        /// initialized to the current scheduler (see <see
        /// cref="System.Threading.Tasks.TaskScheduler.Current">TaskScheduler.Current</see>).
        /// </remarks>
        public TaskFactory(TaskScheduler? scheduler) // null means to use TaskScheduler.Current
            : this(default, TaskCreationOptions.None, TaskContinuationOptions.None, scheduler)
        {
        }

        /// <summary>
        /// Initializes a <see cref="TaskFactory"/> instance with the specified configuration.
        /// </summary>
        /// <param name="creationOptions">
        /// The default <see cref="System.Threading.Tasks.TaskCreationOptions">
        /// TaskCreationOptions</see> to use when creating tasks with this TaskFactory.
        /// </param>
        /// <param name="continuationOptions">
        /// The default <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> to use when creating continuation tasks with this TaskFactory.
        /// </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument or the <paramref name="continuationOptions"/>
        /// argument specifies an invalid value.
        /// </exception>
        /// <remarks>
        /// With this constructor, the
        /// <see cref="TaskCreationOptions"/> property is initialized to <paramref name="creationOptions"/>,
        /// the
        /// <see cref="TaskContinuationOptions"/> property is initialized to <paramref
        /// name="continuationOptions"/>, and the <see
        /// cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see> property is initialized to the
        /// current scheduler (see <see
        /// cref="System.Threading.Tasks.TaskScheduler.Current">TaskScheduler.Current</see>).
        /// </remarks>
        public TaskFactory(TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions)
            : this(default, creationOptions, continuationOptions, null)
        {
        }

        /// <summary>
        /// Initializes a <see cref="TaskFactory"/> instance with the specified configuration.
        /// </summary>
        /// <param name="cancellationToken">The default <see cref="CancellationToken"/> that will be assigned 
        /// to tasks created by this <see cref="TaskFactory"/> unless another CancellationToken is explicitly specified 
        /// while calling the factory methods.</param>
        /// <param name="creationOptions">
        /// The default <see cref="System.Threading.Tasks.TaskCreationOptions">
        /// TaskCreationOptions</see> to use when creating tasks with this TaskFactory.
        /// </param>
        /// <param name="continuationOptions">
        /// The default <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> to use when creating continuation tasks with this TaskFactory.
        /// </param>
        /// <param name="scheduler">
        /// The default <see cref="System.Threading.Tasks.TaskScheduler">
        /// TaskScheduler</see> to use to schedule any Tasks created with this TaskFactory. A null value
        /// indicates that TaskScheduler.Current should be used.
        /// </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument or the <paramref name="continuationOptions"/>
        /// argumentspecifies an invalid value.
        /// </exception>
        /// <remarks>
        /// With this constructor, the
        /// <see cref="TaskCreationOptions"/> property is initialized to <paramref name="creationOptions"/>,
        /// the
        /// <see cref="TaskContinuationOptions"/> property is initialized to <paramref
        /// name="continuationOptions"/>, and the <see
        /// cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see> property is initialized to
        /// <paramref name="scheduler"/>, unless it's null, in which case the property is initialized to the
        /// current scheduler (see <see
        /// cref="System.Threading.Tasks.TaskScheduler.Current">TaskScheduler.Current</see>).
        /// </remarks>
        public TaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler? scheduler)
        {
            CheckMultiTaskContinuationOptions(continuationOptions);
            CheckCreationOptions(creationOptions);

            m_defaultCancellationToken = cancellationToken;
            m_defaultScheduler = scheduler;
            m_defaultCreationOptions = creationOptions;
            m_defaultContinuationOptions = continuationOptions;
        }

        internal static void CheckCreationOptions(TaskCreationOptions creationOptions)
        {
            // Check for validity of options
            if ((creationOptions &
                    ~(TaskCreationOptions.AttachedToParent |
                      TaskCreationOptions.DenyChildAttach |
                      TaskCreationOptions.HideScheduler |
                      TaskCreationOptions.LongRunning |
                      TaskCreationOptions.PreferFairness |
                      TaskCreationOptions.RunContinuationsAsynchronously)) != 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.creationOptions);
            }
        }

        /* Properties */

        /// <summary>
        /// Gets the default <see cref="System.Threading.CancellationToken">CancellationToken</see> of this
        /// TaskFactory.
        /// </summary>
        /// <remarks>
        /// This property returns the default <see cref="CancellationToken"/> that will be assigned to all 
        /// tasks created by this factory unless another CancellationToken value is explicitly specified 
        /// during the call to the factory methods.
        /// </remarks>
        public CancellationToken CancellationToken { get { return m_defaultCancellationToken; } }

        /// <summary>
        /// Gets the <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see> of this
        /// TaskFactory.
        /// </summary>
        /// <remarks>
        /// This property returns the default scheduler for this factory.  It will be used to schedule all 
        /// tasks unless another scheduler is explicitly specified during calls to this factory's methods.  
        /// If null, <see cref="System.Threading.Tasks.TaskScheduler.Current">TaskScheduler.Current</see> 
        /// will be used.
        /// </remarks>
        public TaskScheduler? Scheduler { get { return m_defaultScheduler; } }

        /// <summary>
        /// Gets the <see cref="System.Threading.Tasks.TaskCreationOptions">TaskCreationOptions
        /// </see> value of this TaskFactory.
        /// </summary>
        /// <remarks>
        /// This property returns the default creation options for this factory.  They will be used to create all 
        /// tasks unless other options are explicitly specified during calls to this factory's methods.
        /// </remarks>
        public TaskCreationOptions CreationOptions { get { return m_defaultCreationOptions; } }

        /// <summary>
        /// Gets the <see cref="System.Threading.Tasks.TaskCreationOptions">TaskContinuationOptions
        /// </see> value of this TaskFactory.
        /// </summary>
        /// <remarks>
        /// This property returns the default continuation options for this factory.  They will be used to create 
        /// all continuation tasks unless other options are explicitly specified during calls to this factory's methods.
        /// </remarks>
        public TaskContinuationOptions ContinuationOptions { get { return m_defaultContinuationOptions; } }

        //
        // StartNew methods
        //

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task">Task</see>.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref name="action"/> 
        /// argument is null.</exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a Task using one of its constructors 
        /// and then calling 
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.  However,
        /// unless creation and scheduling must be separated, StartNew is the recommended
        /// approach for both simplicity and performance.
        /// </remarks>
        public Task StartNew(Action action)
        {
            Task? currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, null, m_defaultCancellationToken, GetDefaultScheduler(currTask),
                m_defaultCreationOptions, InternalTaskOptions.None);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task">Task</see>.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref name="action"/> 
        /// argument is null.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a Task using one of its constructors 
        /// and then calling 
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.  However,
        /// unless creation and scheduling must be separated, StartNew is the recommended
        /// approach for both simplicity and performance.
        /// </remarks>
        public Task StartNew(Action action, CancellationToken cancellationToken)
        {
            Task? currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, null, cancellationToken, GetDefaultScheduler(currTask),
                m_defaultCreationOptions, InternalTaskOptions.None);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task">Task</see>.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <param name="creationOptions">A TaskCreationOptions value that controls the behavior of the
        /// created
        /// <see cref="T:System.Threading.Tasks.Task">Task.</see></param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="action"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a Task using one of its constructors and
        /// then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task StartNew(Action action, TaskCreationOptions creationOptions)
        {
            Task? currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, null, m_defaultCancellationToken, GetDefaultScheduler(currTask), creationOptions,
                InternalTaskOptions.None);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task">Task</see>.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new <see cref="Task"/></param>
        /// <param name="creationOptions">A TaskCreationOptions value that controls the behavior of the
        /// created
        /// <see cref="T:System.Threading.Tasks.Task">Task.</see></param>
        /// <param name="scheduler">The <see
        /// cref="T:System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created <see
        /// cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="action"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="scheduler"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a Task using one of its constructors and
        /// then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            return Task.InternalStartNew(
                Task.InternalCurrentIfAttached(creationOptions), action, null, cancellationToken, scheduler, creationOptions,
                InternalTaskOptions.None);
        }


        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task">Task</see>.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="action"/>
        /// delegate.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="action"/>
        /// argument is null.</exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a Task using one of its constructors and
        /// then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task StartNew(Action<object?> action, object? state)
        {
            Task? currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, state, m_defaultCancellationToken, GetDefaultScheduler(currTask),
                m_defaultCreationOptions, InternalTaskOptions.None);
        }


        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task">Task</see>.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="action"/>
        /// delegate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new <see cref="Task"/></param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="action"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a Task using one of its constructors and
        /// then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task StartNew(Action<object?> action, object? state, CancellationToken cancellationToken)
        {
            Task? currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, state, cancellationToken, GetDefaultScheduler(currTask),
                m_defaultCreationOptions, InternalTaskOptions.None);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task">Task</see>.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="action"/>
        /// delegate.</param>
        /// <param name="creationOptions">A TaskCreationOptions value that controls the behavior of the
        /// created
        /// <see cref="T:System.Threading.Tasks.Task">Task.</see></param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="action"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a Task using one of its constructors and
        /// then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task StartNew(Action<object?> action, object? state, TaskCreationOptions creationOptions)
        {
            Task? currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, state, m_defaultCancellationToken, GetDefaultScheduler(currTask),
                creationOptions, InternalTaskOptions.None);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task">Task</see>.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="action"/>
        /// delegate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <param name="creationOptions">A TaskCreationOptions value that controls the behavior of the
        /// created
        /// <see cref="T:System.Threading.Tasks.Task">Task.</see></param>
        /// <param name="scheduler">The <see
        /// cref="T:System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created <see
        /// cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="action"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="scheduler"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a Task using one of its constructors and
        /// then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task StartNew(Action<object?> action, object? state, CancellationToken cancellationToken,
                            TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            return Task.InternalStartNew(
                Task.InternalCurrentIfAttached(creationOptions), action, state, cancellationToken, scheduler,
                creationOptions, InternalTaskOptions.None);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="function"/>
        /// argument is null.</exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a <see cref="Task{TResult}"/> using one
        /// of its constructors and then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task<TResult> StartNew<TResult>(Func<TResult> function)
        {
            Task? currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, m_defaultCancellationToken,
                m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask));
        }


        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new <see cref="Task"/></param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="function"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a <see cref="Task{TResult}"/> using one
        /// of its constructors and then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken)
        {
            Task? currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, cancellationToken,
                m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask));
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="creationOptions">A TaskCreationOptions value that controls the behavior of the
        /// created
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="function"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a <see cref="Task{TResult}"/> using one
        /// of its constructors and then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions)
        {
            Task? currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, m_defaultCancellationToken,
                creationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask));
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <param name="creationOptions">A TaskCreationOptions value that controls the behavior of the
        /// created
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="scheduler">The <see
        /// cref="T:System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created <see cref="T:System.Threading.Tasks.Task{TResult}">
        /// Task{TResult}</see>.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="function"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="scheduler"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a <see cref="Task{TResult}"/> using one
        /// of its constructors and then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            return Task<TResult>.StartNew(
                Task.InternalCurrentIfAttached(creationOptions), function, cancellationToken,
                creationOptions, InternalTaskOptions.None, scheduler);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="function"/>
        /// delegate.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="function"/>
        /// argument is null.</exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a <see cref="Task{TResult}"/> using one
        /// of its constructors and then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task<TResult> StartNew<TResult>(Func<object?, TResult> function, object? state)
        {
            Task? currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, m_defaultCancellationToken,
                m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask));
        }


        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="function"/>
        /// delegate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new <see cref="Task"/></param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="function"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a <see cref="Task{TResult}"/> using one
        /// of its constructors and then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task<TResult> StartNew<TResult>(Func<object?, TResult> function, object? state, CancellationToken cancellationToken)
        {
            Task? currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, cancellationToken,
                m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask));
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="function"/>
        /// delegate.</param>
        /// <param name="creationOptions">A TaskCreationOptions value that controls the behavior of the
        /// created
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="function"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a <see cref="Task{TResult}"/> using one
        /// of its constructors and then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task<TResult> StartNew<TResult>(Func<object?, TResult> function, object? state, TaskCreationOptions creationOptions)
        {
            Task? currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, m_defaultCancellationToken,
                creationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask));
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="function"/>
        /// delegate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <param name="creationOptions">A TaskCreationOptions value that controls the behavior of the
        /// created
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="scheduler">The <see
        /// cref="T:System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created <see cref="T:System.Threading.Tasks.Task{TResult}">
        /// Task{TResult}</see>.</param>
        /// <returns>The started <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="function"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the <paramref
        /// name="scheduler"/>
        /// argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// Calling StartNew is functionally equivalent to creating a <see cref="Task{TResult}"/> using one
        /// of its constructors and then calling
        /// <see cref="System.Threading.Tasks.Task.Start()">Start</see> to schedule it for execution.
        /// However, unless creation and scheduling must be separated, StartNew is the recommended approach
        /// for both simplicity and performance.
        /// </remarks>
        public Task<TResult> StartNew<TResult>(Func<object?, TResult> function, object? state, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            return Task<TResult>.StartNew(
                Task.InternalCurrentIfAttached(creationOptions), function, state, cancellationToken,
                creationOptions, InternalTaskOptions.None, scheduler);
        }

        //
        // FromAsync methods
        //

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that executes an end method action
        /// when a specified <see cref="T:System.IAsyncResult">IAsyncResult</see> completes.
        /// </summary>
        /// <param name="asyncResult">The IAsyncResult whose completion should trigger the processing of the
        /// <paramref name="endMethod"/>.</param>
        /// <param name="endMethod">The action delegate that processes the completed <paramref
        /// name="asyncResult"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="asyncResult"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous
        /// operation.</returns>
        public Task FromAsync(
            IAsyncResult asyncResult,
            Action<IAsyncResult> endMethod)
        {
            return FromAsync(asyncResult, endMethod, m_defaultCreationOptions, DefaultScheduler);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that executes an end method action
        /// when a specified <see cref="T:System.IAsyncResult">IAsyncResult</see> completes.
        /// </summary>
        /// <param name="asyncResult">The IAsyncResult whose completion should trigger the processing of the
        /// <paramref name="endMethod"/>.</param>
        /// <param name="endMethod">The action delegate that processes the completed <paramref
        /// name="asyncResult"/>.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="asyncResult"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous
        /// operation.</returns>
        public Task FromAsync(
            IAsyncResult asyncResult,
            Action<IAsyncResult> endMethod,
            TaskCreationOptions creationOptions)
        {
            return FromAsync(asyncResult, endMethod, creationOptions, DefaultScheduler);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that executes an end method action
        /// when a specified <see cref="T:System.IAsyncResult">IAsyncResult</see> completes.
        /// </summary>
        /// <param name="asyncResult">The IAsyncResult whose completion should trigger the processing of the
        /// <paramref name="endMethod"/>.</param>
        /// <param name="endMethod">The action delegate that processes the completed <paramref
        /// name="asyncResult"/>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the task that executes the end method.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="asyncResult"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous
        /// operation.</returns>
        public Task FromAsync(
            IAsyncResult asyncResult,
            Action<IAsyncResult> endMethod,
            TaskCreationOptions creationOptions,
            TaskScheduler scheduler)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl(asyncResult, null, endMethod, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents a pair of begin
        /// and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the
        /// asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task FromAsync(
            Func<AsyncCallback, object?, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod,
            object? state)
        {
            return FromAsync(beginMethod, endMethod, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents a pair of begin
        /// and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the
        /// asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task FromAsync(
            Func<AsyncCallback, object?, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, object? state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl(beginMethod, null, endMethod, state, creationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents a pair of begin
        /// and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/>
        /// delegate.</typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the
        /// asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task FromAsync<TArg1>(
            Func<TArg1, AsyncCallback, object?, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod,
            TArg1 arg1,
            object? state)
        {
            return FromAsync(beginMethod, endMethod, arg1, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents a pair of begin
        /// and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/>
        /// delegate.</typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the
        /// asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task FromAsync<TArg1>(
            Func<TArg1, AsyncCallback, object?, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod,
            TArg1 arg1, object? state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl(beginMethod, null, endMethod, arg1, state, creationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents a pair of begin
        /// and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the
        /// asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task FromAsync<TArg1, TArg2>(
            Func<TArg1, TArg2, AsyncCallback, object?, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod,
            TArg1 arg1, TArg2 arg2, object? state)
        {
            return FromAsync(beginMethod, endMethod, arg1, arg2, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents a pair of begin
        /// and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the
        /// asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task FromAsync<TArg1, TArg2>(
            Func<TArg1, TArg2, AsyncCallback, object?, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod,
            TArg1 arg1, TArg2 arg2, object? state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl(beginMethod, null, endMethod, arg1, arg2, state, creationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents a pair of begin
        /// and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg3">The third argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the
        /// asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task FromAsync<TArg1, TArg2, TArg3>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object?, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, object? state)
        {
            return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents a pair of begin
        /// and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg3">The third argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the
        /// asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task FromAsync<TArg1, TArg2, TArg3>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object?, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, object? state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl<TArg1, TArg2, TArg3>(beginMethod, null, endMethod, arg1, arg2, arg3, state, creationOptions);
        }

        //
        // Additional FromAsync() overloads used for inferencing convenience
        //

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that executes an end
        /// method function when a specified <see cref="T:System.IAsyncResult">IAsyncResult</see> completes.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="asyncResult">The IAsyncResult whose completion should trigger the processing of the
        /// <paramref name="endMethod"/>.</param>
        /// <param name="endMethod">The function delegate that processes the completed <paramref
        /// name="asyncResult"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="asyncResult"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents the
        /// asynchronous operation.</returns>
        public Task<TResult> FromAsync<TResult>(
            IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod)
        {
            return TaskFactory<TResult>.FromAsyncImpl(asyncResult, endMethod, null, m_defaultCreationOptions, DefaultScheduler);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that executes an end
        /// method function when a specified <see cref="T:System.IAsyncResult">IAsyncResult</see> completes.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="asyncResult">The IAsyncResult whose completion should trigger the processing of the
        /// <paramref name="endMethod"/>.</param>
        /// <param name="endMethod">The function delegate that processes the completed <paramref
        /// name="asyncResult"/>.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="asyncResult"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents the
        /// asynchronous operation.</returns>
        public Task<TResult> FromAsync<TResult>(
            IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions)
        {
            return TaskFactory<TResult>.FromAsyncImpl(asyncResult, endMethod, null, creationOptions, DefaultScheduler);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that executes an end
        /// method function when a specified <see cref="T:System.IAsyncResult">IAsyncResult</see> completes.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="asyncResult">The IAsyncResult whose completion should trigger the processing of the
        /// <paramref name="endMethod"/>.</param>
        /// <param name="endMethod">The function delegate that processes the completed <paramref
        /// name="asyncResult"/>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the task that executes the end method.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="asyncResult"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents the
        /// asynchronous operation.</returns>
        public Task<TResult> FromAsync<TResult>(
            IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            return TaskFactory<TResult>.FromAsyncImpl(asyncResult, endMethod, null, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that
        /// represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task<TResult> FromAsync<TResult>(
            Func<AsyncCallback, object?, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object? state)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that
        /// represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task<TResult> FromAsync<TResult>(
            Func<AsyncCallback, object?, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object? state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, state, creationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that
        /// represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task<TResult> FromAsync<TArg1, TResult>(
            Func<TArg1, AsyncCallback, object?, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object? state)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that
        /// represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object?, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object? state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, state, creationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that
        /// represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object?, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object? state)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that
        /// represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(
            Func<TArg1, TArg2, AsyncCallback, object?, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object? state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, state, creationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg3">The third argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that
        /// represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object?, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object? state)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, arg3, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
        /// <typeparam name="TResult">The type of the result available through the
        /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.
        /// </typeparam>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation.</param>
        /// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="arg3">The third argument passed to the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <param name="creationOptions">The TaskCreationOptions value that controls the behavior of the
        /// created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/>
        /// delegate.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="beginMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="endMethod"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="creationOptions"/> argument specifies an invalid TaskCreationOptions
        /// value.</exception>
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that
        /// represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object?, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object? state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, arg3, state, creationOptions);
        }

        /// <summary>
        /// Check validity of options passed to FromAsync method
        /// </summary>
        /// <param name="creationOptions">The options to be validated.</param>
        /// <param name="hasBeginMethod">determines type of FromAsync method that called this method</param>
        internal static void CheckFromAsyncOptions(TaskCreationOptions creationOptions, bool hasBeginMethod)
        {
            if (hasBeginMethod)
            {
                // Options detected here cause exceptions in FromAsync methods that take beginMethod as a parameter
                if ((creationOptions & TaskCreationOptions.LongRunning) != 0)
                    throw new ArgumentOutOfRangeException(nameof(creationOptions), SR.Task_FromAsync_LongRunning);
                if ((creationOptions & TaskCreationOptions.PreferFairness) != 0)
                    throw new ArgumentOutOfRangeException(nameof(creationOptions), SR.Task_FromAsync_PreferFairness);
            }

            // Check for general validity of options
            if ((creationOptions &
                    ~(TaskCreationOptions.AttachedToParent |
                      TaskCreationOptions.DenyChildAttach |
                      TaskCreationOptions.HideScheduler |
                      TaskCreationOptions.PreferFairness |
                      TaskCreationOptions.LongRunning)) != 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.creationOptions);
            }
        }


        //
        // ContinueWhenAll methods
        //

        // A Task<Task[]> that, given an initial collection of N tasks, will complete when
        // it has been invoked N times.  This allows us to replace this logic:
        //      Task<Task[]> promise = new Task<Task[]>(...);
        //      int _count = tasksCopy.Length;
        //      Action<Task> completionAction = delegate {if(Interlocked.Decrement(ref _count) == 0) promise.TrySetResult(tasksCopy);
        //      for(int i=0; i<_count; i++)
        //          tasksCopy[i].AddCompletionAction(completionAction);
        // with this logic:
        //      CompletionOnCountdownPromise promise = new CompletionOnCountdownPromise(tasksCopy);
        //      for(int i=0; i<tasksCopy.Length; i++) tasksCopy[i].AddCompletionAction(promise);
        // which saves a few allocations.
        //
        // Used in TaskFactory.CommonCWAllLogic(Task[]), below.
        private sealed class CompleteOnCountdownPromise : Task<Task[]>, ITaskCompletionAction
        {
            private readonly Task[] _tasks;
            private int _count;

            internal CompleteOnCountdownPromise(Task[] tasksCopy) : base()
            {
                Debug.Assert((tasksCopy != null) && (tasksCopy.Length > 0), "Expected non-null task array with at least one element in it");
                _tasks = tasksCopy;
                _count = tasksCopy.Length;

                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(this, "TaskFactory.ContinueWhenAll");

                if (Task.s_asyncDebuggingEnabled)
                    AddToActiveTasks(this);
            }

            public void Invoke(Task completingTask)
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationRelation(this, CausalityRelation.Join);

                if (completingTask.IsWaitNotificationEnabled) this.SetNotificationForWaitCompletion(enabled: true);
                if (Interlocked.Decrement(ref _count) == 0)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCompletion(this, AsyncCausalityStatus.Completed);

                    if (Task.s_asyncDebuggingEnabled)
                        RemoveFromActiveTasks(this);

                    TrySetResult(_tasks);
                }
                Debug.Assert(_count >= 0, "Count should never go below 0");
            }

            public bool InvokeMayRunArbitraryCode { get { return true; } }

            /// <summary>
            /// Returns whether we should notify the debugger of a wait completion.  This returns 
            /// true iff at least one constituent task has its bit set.
            /// </summary>
            internal override bool ShouldNotifyDebuggerOfWaitCompletion
            {
                get
                {
                    return
                        base.ShouldNotifyDebuggerOfWaitCompletion &&
                        Task.AnyTaskRequiresNotifyDebuggerOfWaitCompletion(_tasks);
                }
            }
        }

        // Performs some logic common to all ContinueWhenAll() overloads
        internal static Task<Task[]> CommonCWAllLogic(Task[] tasksCopy)
        {
            Debug.Assert(tasksCopy != null);

            // Create a promise task to be returned to the user
            CompleteOnCountdownPromise promise = new CompleteOnCountdownPromise(tasksCopy);

            for (int i = 0; i < tasksCopy.Length; i++)
            {
                if (tasksCopy[i].IsCompleted) promise.Invoke(tasksCopy[i]); // Short-circuit the completion action, if possible
                else tasksCopy[i].AddCompletionAction(promise); // simple completion action
            }

            return promise;
        }


        // A Task<Task<T>[]> that, given an initial collection of N tasks, will complete when
        // it has been invoked N times.  See comments for non-generic CompleteOnCountdownPromise class.
        //
        // Used in TaskFactory.CommonCWAllLogic<TResult>(Task<TResult>[]), below.
        private sealed class CompleteOnCountdownPromise<T> : Task<Task<T>[]>, ITaskCompletionAction
        {
            private readonly Task<T>[] _tasks;
            private int _count;

            internal CompleteOnCountdownPromise(Task<T>[] tasksCopy) : base()
            {
                Debug.Assert((tasksCopy != null) && (tasksCopy.Length > 0), "Expected non-null task array with at least one element in it");
                _tasks = tasksCopy;
                _count = tasksCopy.Length;

                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(this, "TaskFactory.ContinueWhenAll<>");

                if (Task.s_asyncDebuggingEnabled)
                    AddToActiveTasks(this);
            }

            public void Invoke(Task completingTask)
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationRelation(this, CausalityRelation.Join);

                if (completingTask.IsWaitNotificationEnabled) this.SetNotificationForWaitCompletion(enabled: true);
                if (Interlocked.Decrement(ref _count) == 0)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCompletion(this, AsyncCausalityStatus.Completed);

                    if (Task.s_asyncDebuggingEnabled)
                        RemoveFromActiveTasks(this);

                    TrySetResult(_tasks);
                }
                Debug.Assert(_count >= 0, "Count should never go below 0");
            }

            public bool InvokeMayRunArbitraryCode { get { return true; } }

            /// <summary>
            /// Returns whether we should notify the debugger of a wait completion.  This returns 
            /// true iff at least one constituent task has its bit set.
            /// </summary>
            internal override bool ShouldNotifyDebuggerOfWaitCompletion
            {
                get
                {
                    return
                        base.ShouldNotifyDebuggerOfWaitCompletion &&
                        Task.AnyTaskRequiresNotifyDebuggerOfWaitCompletion(_tasks);
                }
            }
        }


        internal static Task<Task<T>[]> CommonCWAllLogic<T>(Task<T>[] tasksCopy)
        {
            Debug.Assert(tasksCopy != null);

            // Create a promise task to be returned to the user
            CompleteOnCountdownPromise<T> promise = new CompleteOnCountdownPromise<T>(tasksCopy);

            for (int i = 0; i < tasksCopy.Length; i++)
            {
                if (tasksCopy[i].IsCompleted) promise.Invoke(tasksCopy[i]); // Short-circuit the completion action, if possible
                else tasksCopy[i].AddCompletionAction(promise); // simple completion action
            }

            return promise;
        }
        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see> 
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationAction">The action delegate to execute when all tasks in 
        /// the <paramref name="tasks"/> array have completed.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is empty.</exception>
        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl(tasks, null, continuationAction, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }


        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see> 
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationAction">The action delegate to execute when all tasks in 
        /// the <paramref name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl(tasks, null, continuationAction, m_defaultContinuationOptions, cancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationAction">The action delegate to execute when all tasks in the <paramref
        /// name="tasks"/> array have completed.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAll.
        /// </remarks>
        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl(tasks, null, continuationAction, continuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationAction">The action delegate to execute when all tasks in the <paramref
        /// name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAll.
        /// </remarks>
        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl(tasks, null, continuationAction, continuationOptions, cancellationToken, scheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see> 
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationAction">The action delegate to execute when all tasks in 
        /// the <paramref name="tasks"/> array have completed.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is empty.</exception>
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, null, continuationAction, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }


        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see> 
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationAction">The action delegate to execute when all tasks in 
        /// the <paramref name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction,
            CancellationToken cancellationToken)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, null, continuationAction, m_defaultContinuationOptions, cancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationAction">The action delegate to execute when all tasks in the <paramref
        /// name="tasks"/> array have completed.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAll.
        /// </remarks>
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction,
            TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, null, continuationAction, continuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationAction">The action delegate to execute when all tasks in the <paramref
        /// name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAll.
        /// </remarks>
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, null, continuationAction, continuationOptions, cancellationToken, scheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the
        /// <paramref name="tasks"/> array have completed.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAllImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }


        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the
        /// <paramref name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAllImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the
        /// <paramref name="tasks"/> array have completed.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAll.
        /// </remarks>
        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAllImpl(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the
        /// <paramref name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAll.
        /// </remarks>
        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAllImpl(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler);
        }


        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the
        /// <paramref name="tasks"/> array have completed.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the
        /// <paramref name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction,
            CancellationToken cancellationToken)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the
        /// <paramref name="tasks"/> array have completed.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAll.
        /// </remarks>
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction,
            TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the
        /// <paramref name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAll.
        /// </remarks>
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler);
        }

        //
        // ContinueWhenAny methods
        //

        // A Task<Task> that will be completed the first time that Invoke is called.
        // It allows us to replace this logic:
        //      Task<Task> promise = new Task<Task>(...);
        //      Action<Task> completionAction = delegate(Task completingTask) { promise.TrySetResult(completingTask); }
        //      for(int i=0; i<tasksCopy.Length; i++) tasksCopy[i].AddCompletionAction(completionAction);
        // with this logic:
        //      CompletionOnInvokePromise promise = new CompletionOnInvokePromise(tasksCopy);
        //      for(int i=0; i<tasksCopy.Length; i++) tasksCopy[i].AddCompletionAction(promise);
        // which saves a couple of allocations.
        //
        // Used in TaskFactory.CommonCWAnyLogic(), below.
        internal sealed class CompleteOnInvokePromise : Task<Task>, ITaskCompletionAction
        {
            private const int CompletedFlag = 0b_01;
            private const int SyncBlockingFlag = 0b_10;

            private IList<Task>? _tasks; // must track this for cleanup
            private int _stateFlags;

            public CompleteOnInvokePromise(IList<Task> tasks, bool isSyncBlocking) : base()
            {
                Debug.Assert(tasks != null, "Expected non-null collection of tasks");
                _tasks = tasks;

                if (isSyncBlocking)
                {
                    // Not completed, but blocking thread, set second bit
                    _stateFlags = SyncBlockingFlag;
                }

                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(this, "TaskFactory.ContinueWhenAny");

                if (Task.s_asyncDebuggingEnabled)
                    AddToActiveTasks(this);
            }

            public void Invoke(Task completingTask)
            {
                int flags = _stateFlags;
                int isSyncBlockingFlag = flags & SyncBlockingFlag;
                int isCompleted = flags & CompletedFlag;

                if (isCompleted == 0 &&
                    Interlocked.Exchange(ref _stateFlags, isSyncBlockingFlag | CompletedFlag) == isSyncBlockingFlag)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                    {
                        AsyncCausalityTracer.TraceOperationRelation(this, CausalityRelation.Choice);
                        AsyncCausalityTracer.TraceOperationCompletion(this, AsyncCausalityStatus.Completed);
                    }

                    if (Task.s_asyncDebuggingEnabled)
                        RemoveFromActiveTasks(this);

                    bool success = TrySetResult(completingTask);
                    Debug.Assert(success, "Only one task should have gotten to this point, and thus this must be successful.");

                    // We need to remove continuations that may be left straggling on other tasks.
                    // Otherwise, repeated calls to WhenAny using the same task could leak actions.
                    // This may also help to avoided unnecessary invocations of this whenComplete delegate.
                    // Note that we may be attempting to remove a continuation from a task that hasn't had it
                    // added yet; while there's overhead there, the operation won't hurt anything.
                    IList<Task>? tasks = _tasks;
                    Debug.Assert(tasks != null, "Should not have been nulled out yet.");
                    int numTasks = tasks.Count;
                    for (int i = 0; i < numTasks; i++)
                    {
                        var task = tasks[i];
                        if (task != null && // if an element was erroneously nulled out concurrently, just skip it; worst case is we don't remove a continuation
                            !task.IsCompleted) task.RemoveContinuation(this);
                    }
                    _tasks = null;
                }
            }

            public bool InvokeMayRunArbitraryCode => (_stateFlags & SyncBlockingFlag) == 0;
        }
        // Common ContinueWhenAny logic
        // If the tasks list is not an array, it must be an internal defensive copy so that 
        // we don't need to be concerned about concurrent modifications to the list.  If the task list
        // is an array, it should be a defensive copy if this functionality is being used
        // asynchronously (e.g. WhenAny) rather than synchronously (e.g. WaitAny).
        internal static Task<Task> CommonCWAnyLogic(IList<Task> tasks, bool isSyncBlocking = false)
        {
            Debug.Assert(tasks != null);

            // Create a promise task to be returned to the user.
            // (If this logic ever changes, also update CommonCWAnyLogicCleanup.)
            var promise = new CompleteOnInvokePromise(tasks, isSyncBlocking);

            // At the completion of any of the tasks, complete the promise.

            bool checkArgsOnly = false;
            int numTasks = tasks.Count;
            for (int i = 0; i < numTasks; i++)
            {
                var task = tasks[i];
                if (task == null) throw new ArgumentException(SR.Task_MultiTaskContinuation_NullTask, nameof(tasks));

                if (checkArgsOnly) continue;

                // If the promise has already completed, don't bother with checking any more tasks.
                if (promise.IsCompleted)
                {
                    checkArgsOnly = true;
                }
                // If a task has already completed, complete the promise.
                else if (task.IsCompleted)
                {
                    promise.Invoke(task);
                    checkArgsOnly = true;
                }
                // Otherwise, add the completion action and keep going.
                else
                {
                    task.AddCompletionAction(promise, addBeforeOthers: isSyncBlocking);
                    if (promise.IsCompleted)
                    {
                        // One of the previous tasks that already had its continuation registered may have
                        // raced to complete with our adding the continuation to this task.  The completion
                        // routine would have gone through and removed the continuation from all of the tasks
                        // with which it was already registered, but if the race causes this continuation to
                        // be added after that, it'll never be removed.  As such, after adding the continuation,
                        // we check to see whether the promise has already completed, and if it has, we try to
                        // manually remove the continuation from this task.  If it was already removed, it'll be
                        // a nop, and if we race to remove it, the synchronization in RemoveContinuation will
                        // keep things consistent.
                        task.RemoveContinuation(promise);
                    }
                }
            }

            return promise;
        }

        /// <summary>
        /// Cleans up the operations performed by CommonCWAnyLogic in a case where
        /// the created continuation task is being discarded.
        /// </summary>
        /// <param name="continuation">The task returned from CommonCWAnyLogic.</param>
        internal static void CommonCWAnyLogicCleanup(Task<Task> continuation)
        {
            // Force cleanup of the promise (e.g. removing continuations from each
            // constituent task), by completing the promise with any value (it's not observable).
            ((CompleteOnInvokePromise)continuation).Invoke(null!);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationAction">The action delegate to execute when one task in the <paramref
        /// name="tasks"/> array completes.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl(tasks, null, continuationAction, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationAction">The action delegate to execute when one task in the <paramref
        /// name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl(tasks, null, continuationAction, m_defaultContinuationOptions, cancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationAction">The action delegate to execute when one task in the <paramref
        /// name="tasks"/> array completes.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAny.
        /// </remarks>
        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl(tasks, null, continuationAction, continuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationAction">The action delegate to execute when one task in the <paramref
        /// name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAny.
        /// </remarks>
        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl(tasks, null, continuationAction, continuationOptions, cancellationToken, scheduler);
        }


        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAnyImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAnyImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAny.
        /// </remarks>
        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAnyImpl(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAny.
        /// </remarks>
        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAnyImpl(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));
            return TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction,
            CancellationToken cancellationToken)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAny.
        /// </remarks>
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction,
            TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TResult">The type of the result that is returned by the <paramref
        /// name="continuationFunction"/>
        /// delegate and associated with the created <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</typeparam>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAny.
        /// </remarks>
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null) throw new ArgumentNullException(nameof(continuationFunction));

            return TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler);
        }


        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationAction">The action delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, null, continuationAction, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationAction">The action delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction,
            CancellationToken cancellationToken)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, null, continuationAction, m_defaultContinuationOptions, cancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationAction">The action delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAny.
        /// </remarks>
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction,
            TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, null, continuationAction, continuationOptions, m_defaultCancellationToken, DefaultScheduler);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The type of the result of the antecedent <paramref name="tasks"/>.</typeparam>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationAction">The action delegate to execute when one task in the
        /// <paramref name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationAction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="scheduler"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The exception that is thrown when the
        /// <paramref name="continuationOptions"/> argument specifies an invalid TaskContinuationOptions
        /// value.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The provided <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// has already been disposed.
        /// </exception>
        /// <remarks>
        /// The NotOn* and OnlyOn* <see cref="System.Threading.Tasks.TaskContinuationOptions">TaskContinuationOptions</see>, 
        /// which constrain for which <see cref="System.Threading.Tasks.TaskStatus">TaskStatus</see> states a continuation 
        /// will be executed, are illegal with ContinueWhenAny.
        /// </remarks>
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationAction == null) throw new ArgumentNullException(nameof(continuationAction));

            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, null, continuationAction, continuationOptions, cancellationToken, scheduler);
        }

        // Check task array and return a defensive copy.
        // Used with ContinueWhenAll()/ContinueWhenAny().
        internal static Task[] CheckMultiContinuationTasksAndCopy(Task[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException(nameof(tasks));
            if (tasks.Length == 0)
                throw new ArgumentException(SR.Task_MultiTaskContinuation_EmptyTaskList, nameof(tasks));

            Task[] tasksCopy = new Task[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasksCopy[i] = tasks[i];

                if (tasksCopy[i] == null)
                    throw new ArgumentException(SR.Task_MultiTaskContinuation_NullTask, nameof(tasks));
            }

            return tasksCopy;
        }

        internal static Task<TResult>[] CheckMultiContinuationTasksAndCopy<TResult>(Task<TResult>[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException(nameof(tasks));
            if (tasks.Length == 0)
                throw new ArgumentException(SR.Task_MultiTaskContinuation_EmptyTaskList, nameof(tasks));

            Task<TResult>[] tasksCopy = new Task<TResult>[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasksCopy[i] = tasks[i];

                if (tasksCopy[i] == null)
                    throw new ArgumentException(SR.Task_MultiTaskContinuation_NullTask, nameof(tasks));
            }

            return tasksCopy;
        }

        // Throw an exception if "options" argument specifies illegal options
        internal static void CheckMultiTaskContinuationOptions(TaskContinuationOptions continuationOptions)
        {
            // Construct a mask to check for illegal options
            const TaskContinuationOptions NotOnAny = TaskContinuationOptions.NotOnCanceled |
                                               TaskContinuationOptions.NotOnFaulted |
                                               TaskContinuationOptions.NotOnRanToCompletion;

            // Check that LongRunning and ExecuteSynchronously are not specified together
            const TaskContinuationOptions illegalMask = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.LongRunning;
            if ((continuationOptions & illegalMask) == illegalMask)
            {
                throw new ArgumentOutOfRangeException(nameof(continuationOptions), SR.Task_ContinueWith_ESandLR);
            }

            // Check that no nonsensical options are specified.
            if ((continuationOptions & ~(
                TaskContinuationOptions.LongRunning |
                TaskContinuationOptions.PreferFairness |
                TaskContinuationOptions.AttachedToParent |
                TaskContinuationOptions.DenyChildAttach |
                TaskContinuationOptions.HideScheduler |
                TaskContinuationOptions.LazyCancellation |
                NotOnAny |
                TaskContinuationOptions.ExecuteSynchronously)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(continuationOptions));
            }

            // Check that no "fire" options are specified.
            if ((continuationOptions & NotOnAny) != 0)
                throw new ArgumentOutOfRangeException(nameof(continuationOptions), SR.Task_MultiTaskContinuation_FireOptions);
        }
    }
}
