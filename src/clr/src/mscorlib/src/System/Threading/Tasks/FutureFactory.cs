// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// As with TaskFactory, TaskFactory<TResult> encodes common factory patterns into helper methods.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Security;
using System.Security.Permissions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Runtime.Versioning;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides support for creating and scheduling
    /// <see cref="T:System.Threading.Tasks.Task{TResult}">Task{TResult}</see> objects.
    /// </summary>
    /// <typeparam name="TResult">The type of the results that are available though 
    /// the <see cref="T:System.Threading.Tasks.Task{TResult}">Task{TResult}</see> objects that are associated with 
    /// the methods in this class.</typeparam>
    /// <remarks>
    /// <para>
    /// There are many common patterns for which tasks are relevant. The <see cref="TaskFactory{TResult}"/>
    /// class encodes some of these patterns into methods that pick up default settings, which are
    /// configurable through its constructors.
    /// </para>
    /// <para>
    /// A default instance of <see cref="TaskFactory{TResult}"/> is available through the
    /// <see cref="System.Threading.Tasks.Task{TResult}.Factory">Task{TResult}.Factory</see> property.
    /// </para>
    /// </remarks>
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public class TaskFactory<TResult>
    {
        // Member variables, DefaultScheduler, other properties and ctors 
        // copied right out of TaskFactory...  Lots of duplication here...
        // Should we be thinking about a TaskFactoryBase class?

        // member variables
        private CancellationToken m_defaultCancellationToken;
        private TaskScheduler m_defaultScheduler;
        private TaskCreationOptions m_defaultCreationOptions;
        private TaskContinuationOptions m_defaultContinuationOptions;

        private TaskScheduler DefaultScheduler
        {
            get
            {
                if (m_defaultScheduler == null) return TaskScheduler.Current;
                else return m_defaultScheduler;
            }
        }

        // sister method to above property -- avoids a TLS lookup
        private TaskScheduler GetDefaultScheduler(Task currTask)
        {
            if (m_defaultScheduler != null) return m_defaultScheduler;
            else if ((currTask != null)
                && ((currTask.CreationOptions & TaskCreationOptions.HideScheduler) == 0)
                )
                return currTask.ExecutingTaskScheduler;
            else return TaskScheduler.Default;
        }

        /* Constructors */

        /// <summary>
        /// Initializes a <see cref="TaskFactory{TResult}"/> instance with the default configuration.
        /// </summary>
        /// <remarks>
        /// This constructor creates a <see cref="TaskFactory{TResult}"/> instance with a default configuration. The
        /// <see cref="TaskCreationOptions"/> property is initialized to
        /// <see cref="System.Threading.Tasks.TaskCreationOptions.None">TaskCreationOptions.None</see>, the
        /// <see cref="TaskContinuationOptions"/> property is initialized to <see
        /// cref="System.Threading.Tasks.TaskContinuationOptions.None">TaskContinuationOptions.None</see>,
        /// and the <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see> property is
        /// initialized to the current scheduler (see <see
        /// cref="System.Threading.Tasks.TaskScheduler.Current">TaskScheduler.Current</see>).
        /// </remarks>
        public TaskFactory()
            : this(default(CancellationToken), TaskCreationOptions.None, TaskContinuationOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a <see cref="TaskFactory{TResult}"/> instance with the default configuration.
        /// </summary>
        /// <param name="cancellationToken">The default <see cref="CancellationToken"/> that will be assigned 
        /// to tasks created by this <see cref="TaskFactory"/> unless another CancellationToken is explicitly specified 
        /// while calling the factory methods.</param>
        /// <remarks>
        /// This constructor creates a <see cref="TaskFactory{TResult}"/> instance with a default configuration. The
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
        /// Initializes a <see cref="TaskFactory{TResult}"/> instance with the specified configuration.
        /// </summary>
        /// <param name="scheduler">
        /// The <see cref="System.Threading.Tasks.TaskScheduler">
        /// TaskScheduler</see> to use to schedule any tasks created with this TaskFactory{TResult}. A null value
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
        public TaskFactory(TaskScheduler scheduler) // null means to use TaskScheduler.Current
            : this(default(CancellationToken), TaskCreationOptions.None, TaskContinuationOptions.None, scheduler)
        {
        }

        /// <summary>
        /// Initializes a <see cref="TaskFactory{TResult}"/> instance with the specified configuration.
        /// </summary>
        /// <param name="creationOptions">
        /// The default <see cref="System.Threading.Tasks.TaskCreationOptions">
        /// TaskCreationOptions</see> to use when creating tasks with this TaskFactory{TResult}.
        /// </param>
        /// <param name="continuationOptions">
        /// The default <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> to use when creating continuation tasks with this TaskFactory{TResult}.
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
            : this(default(CancellationToken), creationOptions, continuationOptions, null)
        {
        }

        /// <summary>
        /// Initializes a <see cref="TaskFactory{TResult}"/> instance with the specified configuration.
        /// </summary>
        /// <param name="cancellationToken">The default <see cref="CancellationToken"/> that will be assigned 
        /// to tasks created by this <see cref="TaskFactory"/> unless another CancellationToken is explicitly specified 
        /// while calling the factory methods.</param>
        /// <param name="creationOptions">
        /// The default <see cref="System.Threading.Tasks.TaskCreationOptions">
        /// TaskCreationOptions</see> to use when creating tasks with this TaskFactory{TResult}.
        /// </param>
        /// <param name="continuationOptions">
        /// The default <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> to use when creating continuation tasks with this TaskFactory{TResult}.
        /// </param>
        /// <param name="scheduler">
        /// The default <see cref="System.Threading.Tasks.TaskScheduler">
        /// TaskScheduler</see> to use to schedule any Tasks created with this TaskFactory{TResult}. A null value
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
        public TaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            TaskFactory.CheckCreationOptions(creationOptions);

            m_defaultCancellationToken = cancellationToken;
            m_defaultScheduler = scheduler;
            m_defaultCreationOptions = creationOptions;
            m_defaultContinuationOptions = continuationOptions;
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
        /// TaskFactory{TResult}.
        /// </summary>
        /// <remarks>
        /// This property returns the default scheduler for this factory.  It will be used to schedule all 
        /// tasks unless another scheduler is explicitly specified during calls to this factory's methods.  
        /// If null, <see cref="System.Threading.Tasks.TaskScheduler.Current">TaskScheduler.Current</see> 
        /// will be used.
        /// </remarks>
        public TaskScheduler Scheduler { get { return m_defaultScheduler; } }

        /// <summary>
        /// Gets the <see cref="System.Threading.Tasks.TaskCreationOptions">TaskCreationOptions
        /// </see> value of this TaskFactory{TResult}.
        /// </summary>
        /// <remarks>
        /// This property returns the default creation options for this factory.  They will be used to create all 
        /// tasks unless other options are explicitly specified during calls to this factory's methods.
        /// </remarks>
        public TaskCreationOptions CreationOptions { get { return m_defaultCreationOptions; } }

        /// <summary>
        /// Gets the <see cref="System.Threading.Tasks.TaskCreationOptions">TaskContinuationOptions
        /// </see> value of this TaskFactory{TResult}.
        /// </summary>
        /// <remarks>
        /// This property returns the default continuation options for this factory.  They will be used to create 
        /// all continuation tasks unless other options are explicitly specified during calls to this factory's methods.
        /// </remarks>
        public TaskContinuationOptions ContinuationOptions { get { return m_defaultContinuationOptions; } }


        /* StartNew */

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> StartNew(Func<TResult> function)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, m_defaultCancellationToken,
                m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, cancellationToken,
                m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, m_defaultCancellationToken,
                creationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="creationOptions">A TaskCreationOptions value that controls the behavior of the
        /// created
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task<TResult>.StartNew(
                Task.InternalCurrentIfAttached(creationOptions), function, cancellationToken,
                creationOptions, InternalTaskOptions.None, scheduler, ref stackMark);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> StartNew(Func<Object, TResult> function, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, m_defaultCancellationToken,
                m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        /// <param name="function">A function delegate that returns the future result to be available through
        /// the <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="function"/>
        /// delegate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> StartNew(Func<Object, TResult> function, Object state, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, cancellationToken,
                m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> StartNew(Func<Object, TResult> function, Object state, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, m_defaultCancellationToken,
                creationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        /// <summary>
        /// Creates and starts a <see cref="T:System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> StartNew(Func<Object, TResult> function, Object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task<TResult>.StartNew(Task.InternalCurrentIfAttached(creationOptions), function, state, cancellationToken,
                creationOptions, InternalTaskOptions.None, scheduler, ref stackMark);
        }

        //
        // APM Factory methods
        //

        // Common core logic for FromAsync calls.  This minimizes the chance of "drift" between overload implementations.
        private static void FromAsyncCoreLogic(
            IAsyncResult iar,
            Func<IAsyncResult, TResult> endFunction,
            Action<IAsyncResult> endAction,
            Task<TResult> promise,
            bool requiresSynchronization)
        {
            Contract.Requires((endFunction != null) != (endAction != null), "Expected exactly one of endFunction/endAction to be non-null");

            Exception ex = null;
            OperationCanceledException oce = null;
            TResult result = default(TResult);

            try
            {
                if (endFunction != null)
                {
                    result = endFunction(iar);
                }
                else
                {
                    endAction(iar);
                }
            }
            catch (OperationCanceledException _oce) { oce = _oce; }
            catch (Exception e) { ex = e; }
            finally
            {
                if (oce != null)
                {
                    promise.TrySetCanceled(oce.CancellationToken, oce);
                }
                else if (ex != null)
                {
                    bool bWonSetException = promise.TrySetException(ex);
                    if (bWonSetException && ex is ThreadAbortException)
                    {
                        promise.m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
                    }
                }
                else
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Completed);

                    if (Task.s_asyncDebuggingEnabled)
                    {
                        Task.RemoveFromActiveTasks(promise.Id);
                    }
                    if (requiresSynchronization)
                    {
                        promise.TrySetResult(result);
                    }
                    else
                    {
                        promise.DangerousSetResult(result);
                    }
                }
                
               
            }
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that executes an end
        /// method function when a specified <see cref="T:System.IAsyncResult">IAsyncResult</see> completes.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return FromAsyncImpl(asyncResult, endMethod, null, m_defaultCreationOptions, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that executes an end
        /// method function when a specified <see cref="T:System.IAsyncResult">IAsyncResult</see> completes.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> FromAsync(
            IAsyncResult asyncResult,
            Func<IAsyncResult, TResult> endMethod,
            TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return FromAsyncImpl(asyncResult, endMethod, null, creationOptions, DefaultScheduler, ref stackMark);
        }



        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that executes an end
        /// method function when a specified <see cref="T:System.IAsyncResult">IAsyncResult</see> completes.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> FromAsync(
            IAsyncResult asyncResult,
            Func<IAsyncResult, TResult> endMethod,
            TaskCreationOptions creationOptions,
            TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return FromAsyncImpl(asyncResult, endMethod, null, creationOptions, scheduler, ref stackMark);
        }

        // internal overload that supports StackCrawlMark
        // We also need this logic broken out into a static method so that the similar TaskFactory.FromAsync()
        // method can access the logic w/o declaring a TaskFactory<TResult> instance.
        internal static Task<TResult> FromAsyncImpl(
            IAsyncResult asyncResult,
            Func<IAsyncResult, TResult> endFunction,
            Action<IAsyncResult> endAction,
            TaskCreationOptions creationOptions,
            TaskScheduler scheduler,
            ref StackCrawlMark stackMark)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endMethod");

            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");

            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();

            TaskFactory.CheckFromAsyncOptions(creationOptions, false);

            Task<TResult> promise = new Task<TResult>((object)null, creationOptions);

            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync", 0);

            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            // Just specify this task as detached. No matter what happens, we want endMethod 
            // to be called -- even if the parent is canceled.  So we don't want to flow 
            // RespectParentCancellation.
            Task t = new Task(delegate
            {
                FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization:true);
            },
                (object)null, null,
                default(CancellationToken), TaskCreationOptions.None, InternalTaskOptions.None, null, ref stackMark);

            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Verbose, t.Id, "TaskFactory.FromAsync Callback", 0);

            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(t);
            }

            if (asyncResult.IsCompleted)
            {
                try { t.InternalRunSynchronously(scheduler, waitForCompletion:false); }
                catch (Exception e) { promise.TrySetException(e); } // catch and log any scheduler exceptions
            }
            else
            {
                ThreadPool.RegisterWaitForSingleObject(
                    asyncResult.AsyncWaitHandle,
                    delegate
                    {
                        try { t.InternalRunSynchronously(scheduler, waitForCompletion: false); }
                        catch (Exception e) { promise.TrySetException(e); } // catch and log any scheduler exceptions
                    },
                    null,
                    Timeout.Infinite,
                    true);
            }

            return promise;
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
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
        public Task<TResult> FromAsync(
            Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
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
        public Task<TResult> FromAsync(
            Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, state, creationOptions);
        }

        // We need this logic broken out into a static method so that the similar TaskFactory.FromAsync()
        // method can access the logic w/o declaring a TaskFactory<TResult> instance.
        internal static Task<TResult> FromAsyncImpl(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction,
            object state, TaskCreationOptions creationOptions)
        {
            if (beginMethod == null)
                throw new ArgumentNullException("beginMethod");

            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endMethod");

            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");
            
            TaskFactory.CheckFromAsyncOptions(creationOptions, true);

            Task<TResult> promise = new Task<TResult>(state, creationOptions);

            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync: " + beginMethod.Method.Name, 0);

            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            try
            {
                // Do NOT change the code below. 
                // 4.5 relies on the fact that IAsyncResult CompletedSynchronously flag needs to be set correctly,
                // sadly this has not been the case that is why the behaviour from 4.5 broke 4.0 buggy apps. Any other
                // change will likely brake 4.5 behavior so if possible never touch this code again.
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    //This is 4.5 behaviour
                    //if we don't require synchronization, a faster set result path is taken
                    var asyncResult = beginMethod(iar =>
                    {
                        if (!iar.CompletedSynchronously)
                            FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }, state);
                    if (asyncResult.CompletedSynchronously)
                    {
                        Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                        FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: false);
                    }
                }
                else
                {
                    //This is the original 4.0 behaviour
                    var asyncResult = beginMethod(iar =>
                    {
                        FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }, state);
                }
            }
            catch
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Error);

                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(promise.Id);
                }

                // Make sure we don't leave promise "dangling".
                promise.TrySetResult(default(TResult));
                throw;
            }

            return promise;
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
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
        public Task<TResult> FromAsync<TArg1>(
            Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod,
            TArg1 arg1, object state)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
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
        public Task<TResult> FromAsync<TArg1>(
            Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod,
            TArg1 arg1, object state, TaskCreationOptions creationOptions)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, state, creationOptions);
        }

        // We need this logic broken out into a static method so that the similar TaskFactory.FromAsync()
        // method can access the logic w/o declaring a TaskFactory<TResult> instance.
        internal static Task<TResult> FromAsyncImpl<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction,
            TArg1 arg1, object state, TaskCreationOptions creationOptions)
        {
            if (beginMethod == null)
                throw new ArgumentNullException("beginMethod");

            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endFunction");

            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");

            TaskFactory.CheckFromAsyncOptions(creationOptions, true);

            Task<TResult> promise = new Task<TResult>(state, creationOptions);

            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync: " + beginMethod.Method.Name, 0);

            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            try
            {
                // Do NOT change the code below. 
                // 4.5 relies on the fact that IAsyncResult CompletedSynchronously flag needs to be set correctly,
                // sadly this has not been the case that is why the behaviour from 4.5 broke 4.0 buggy apps. Any other
                // change will likely brake 4.5 behavior so if possible never touch this code again.
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    //if we don't require synchronization, a faster set result path is taken
                    var asyncResult = beginMethod(arg1, iar =>
                    {
                        if (!iar.CompletedSynchronously)
                            FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }, state);
                    if (asyncResult.CompletedSynchronously)
                    {
                        Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                        FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: false);
                    }
                }
                else
                {
                    //quirk for previous versions
                    var asyncResult = beginMethod(arg1, iar =>
                    {
                        FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }, state);
                }
            }
            catch
            {

                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Error);

                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(promise.Id);
                }

                // Make sure we don't leave promise "dangling".
                promise.TrySetResult(default(TResult));
                throw;
            }

            return promise;
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
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
        /// <returns>The created <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that
        /// represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method throws any exceptions thrown by the <paramref name="beginMethod"/>.
        /// </remarks>
        public Task<TResult> FromAsync<TArg1, TArg2>(
            Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod,
            TArg1 arg1, TArg2 arg2, object state)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, state, m_defaultCreationOptions);
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> that represents a pair of
        /// begin and end methods that conform to the Asynchronous Programming Model pattern.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first argument passed to the <paramref
        /// name="beginMethod"/> delegate.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument passed to <paramref name="beginMethod"/>
        /// delegate.</typeparam>
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
        public Task<TResult> FromAsync<TArg1, TArg2>(
            Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod,
            TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, state, creationOptions);
        }

        // We need this logic broken out into a static method so that the similar TaskFactory.FromAsync()
        // method can access the logic w/o declaring a TaskFactory<TResult> instance.
        internal static Task<TResult> FromAsyncImpl<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction,
            TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
        {
            if (beginMethod == null)
                throw new ArgumentNullException("beginMethod");

            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endMethod");

            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");

            TaskFactory.CheckFromAsyncOptions(creationOptions, true);

            Task<TResult> promise = new Task<TResult>(state, creationOptions);

            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync: " + beginMethod.Method.Name, 0);

            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            try
            {
                // Do NOT change the code below. 
                // 4.5 relies on the fact that IAsyncResult CompletedSynchronously flag needs to be set correctly,
                // sadly this has not been the case that is why the behaviour from 4.5 broke 4.0 buggy apps. Any other
                // change will likely brake 4.5 behavior so if possible never touch this code again.
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    //if we don't require synchronization, a faster set result path is taken
                    var asyncResult = beginMethod(arg1, arg2, iar =>
                    {
                        if (!iar.CompletedSynchronously)
                            FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }, state);
                    if (asyncResult.CompletedSynchronously)
                    {
                        Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                        FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: false);
                    }
                }
                else
                {
                    //quirk for previous versions
                    var asyncResult = beginMethod(arg1, arg2, iar =>
                    {
                        FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }, state);
                }
            }
            catch
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Error);

                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(promise.Id);
                }

                // Make sure we don't leave promise "dangling".
                promise.TrySetResult(default(TResult));
                throw;
            }

            return promise;
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
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, object state)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, arg3, state, m_defaultCreationOptions);
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
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, arg3, state, creationOptions);
        }

        // We need this logic broken out into a static method so that the similar TaskFactory.FromAsync()
        // method can access the logic w/o declaring a TaskFactory<TResult> instance.
        internal static Task<TResult> FromAsyncImpl<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
        {
            if (beginMethod == null)
                throw new ArgumentNullException("beginMethod");

            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endMethod");

            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");

            TaskFactory.CheckFromAsyncOptions(creationOptions, true);

            Task<TResult> promise = new Task<TResult>(state, creationOptions);

            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync: " + beginMethod.Method.Name, 0);

            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            try
            {
                // Do NOT change the code below. 
                // 4.5 relies on the fact that IAsyncResult CompletedSynchronously flag needs to be set correctly,
                // sadly this has not been the case that is why the behaviour from 4.5 broke 4.0 buggy apps. Any other
                // change will likely brake 4.5 behavior so if possible never touch this code again.
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    //if we don't require synchronization, a faster set result path is taken
                    var asyncResult = beginMethod(arg1, arg2, arg3, iar =>
                    {
                        if (!iar.CompletedSynchronously)
                            FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }, state);
                    if (asyncResult.CompletedSynchronously)
                    {
                        Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                        FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: false);
                    }
                }
                else
                {
                    //quirk for previous versions
                    var asyncResult = beginMethod(arg1, arg2, arg3, iar =>
                    {
                        FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }, state);
                }
            }
            catch
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Error);

                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(promise.Id);
                }

                // Make sure we don't leave the promise "dangling".
                promise.TrySetResult(default(TResult));
                throw;
            }

            return promise;
        }

        /// <summary>
        /// Special internal-only FromAsync support used by System.IO to wrap
        /// APM implementations with minimal overhead, avoiding unnecessary closure
        /// and delegate allocations.
        /// </summary>
        /// <typeparam name="TInstance">Specifies the type of the instance on which the APM implementation lives.</typeparam>
        /// <typeparam name="TArg1">Specifies the type containing the arguments.</typeparam>
        /// <param name="thisRef">The instance from which the begin and end methods are invoked.</param>
        /// <param name="beginMethod">The begin method.</param>
        /// <param name="endMethod">The end method.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal static Task<TResult> FromAsyncTrim<TInstance, TArgs>(
            TInstance thisRef, TArgs args,
            Func<TInstance, TArgs, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<TInstance, IAsyncResult, TResult> endMethod) 
            where TInstance : class
        {
            // Validate arguments, but only with asserts, as this is an internal only implementation.
            Contract.Assert(thisRef != null, "Expected a non-null thisRef");
            Contract.Assert(beginMethod != null, "Expected a non-null beginMethod");
            Contract.Assert(endMethod != null, "Expected a non-null endMethod");

            // Create the promise and start the operation.
            // No try/catch is necessary here as we want exceptions to bubble out, and because
            // the task doesn't have AttachedToParent set on it, there's no need to complete it in
            // case of an exception occurring... we can just let it go unresolved.
            var promise = new FromAsyncTrimPromise<TInstance>(thisRef, endMethod);
            var asyncResult = beginMethod(thisRef, args, FromAsyncTrimPromise<TInstance>.s_completeFromAsyncResult, promise);

            // If the IAsyncResult completed asynchronously, completing the promise will be handled by the callback.
            // If it completed synchronously, we'll handle that here.
            if (asyncResult.CompletedSynchronously)
            {
                Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                promise.Complete(thisRef, endMethod, asyncResult, requiresSynchronization: false);
            }

            // Return the promise
            return promise;
        }

        /// <summary>
        /// A specialized task used by FromAsyncTrim.  Stores relevant information as instance
        /// state so that we can avoid unnecessary closure/delegate allocations.
        /// </summary>
        /// <typeparam name="TInstance">Specifies the type of the instance on which the APM implementation lives.</typeparam>
        private sealed class FromAsyncTrimPromise<TInstance> : Task<TResult> where TInstance : class
        {
            /// <summary>A cached delegate used as the callback for the BeginXx method.</summary>
            internal readonly static AsyncCallback s_completeFromAsyncResult = CompleteFromAsyncResult;

            /// <summary>A reference to the object on which the begin/end methods are invoked.</summary>
            private TInstance m_thisRef;
            /// <summary>The end method.</summary>
            private Func<TInstance, IAsyncResult, TResult> m_endMethod;

            /// <summary>Initializes the promise.</summary>
            /// <param name="thisRef">A reference to the object on which the begin/end methods are invoked.</param>
            /// <param name="endMethod">The end method.</param>
            internal FromAsyncTrimPromise(TInstance thisRef, Func<TInstance, IAsyncResult, TResult> endMethod) : base()
            {
                Contract.Requires(thisRef != null, "Expected a non-null thisRef");
                Contract.Requires(endMethod != null, "Expected a non-null endMethod");
                m_thisRef = thisRef;
                m_endMethod = endMethod;
            }

            /// <summary>
            /// Completes the asynchronous operation using information in the IAsyncResult.  
            /// IAsyncResult.AsyncState neeeds to be the FromAsyncTrimPromise to complete.
            /// </summary>
            /// <param name="asyncResult">The IAsyncResult for the async operation.</param>
            internal static void CompleteFromAsyncResult(IAsyncResult asyncResult)
            {
                // Validate argument
                if (asyncResult == null) throw new ArgumentNullException("asyncResult");
                Contract.EndContractBlock();

                var promise = asyncResult.AsyncState as FromAsyncTrimPromise<TInstance>;
                if (promise == null) throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndCalledMultiple"), "asyncResult");

                // Grab the relevant state and then null it out so that the task doesn't hold onto the state unnecessarily
                var thisRef = promise.m_thisRef;
                var endMethod = promise.m_endMethod;
                promise.m_thisRef = default(TInstance);
                promise.m_endMethod = null;
                if (endMethod == null) throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndCalledMultiple"), "asyncResult");

                // Complete the promise.  If the IAsyncResult completed synchronously,
                // we'll instead complete the promise at the call site.
                if (!asyncResult.CompletedSynchronously)
                {
                    promise.Complete(thisRef, endMethod, asyncResult, requiresSynchronization:true);
                }
            }

            /// <summary>Completes the promise.</summary>
            /// <param name="requiresSynchronization">
            /// true if synchronization is needed to completed the task;
            /// false if the task may be completed without synchronization
            /// because it hasn't been handed out.
            /// </param>
            /// <param name="thisRef">The target instance on which the end method should be called.</param>
            /// <param name="endMethod">The end method to call to retrieve the result.</param>
            /// <param name="asyncResult">The IAsyncResult for the async operation.</param>
            /// <permission cref="requiresSynchronization">
            /// Whether completing the task requires synchronization.  This should be true
            /// unless absolutely sure that the task has not yet been handed out to any consumers.
            /// </permission>
            internal void Complete(
                TInstance thisRef, Func<TInstance, IAsyncResult, TResult> endMethod, IAsyncResult asyncResult,
                bool requiresSynchronization)
            {
                Contract.Assert(!IsCompleted, "The task should not have been completed yet.");

                // Run the end method and complete the task
                bool successfullySet = false;
                try
                {
                    var result = endMethod(thisRef, asyncResult);
                    if (requiresSynchronization)
                    {
                        successfullySet = TrySetResult(result);
                    }
                    else
                    {
                        // If requiresSynchronization is false, we can use the DangerousSetResult
                        // method, which uses no synchronization to complete the task.  This is
                        // only valid when the operation is completing synchronously such
                        // that the task has not yet been handed out to any consumers.
                        DangerousSetResult(result);
                        successfullySet = true;
                    }
                }
                catch (OperationCanceledException oce)
                {
                    successfullySet = TrySetCanceled(oce.CancellationToken, oce);
                }
                catch (Exception exc)
                {
                    successfullySet = TrySetException(exc);
                }
                Contract.Assert(successfullySet, "Expected the task to not yet be completed");
            }
        }

        // Utility method to create a canceled future-style task.
        // Used by ContinueWhenAll/Any to bail out early on a pre-canceled token.
        private static Task<TResult> CreateCanceledTask(TaskContinuationOptions continuationOptions, CancellationToken ct)
        {
            TaskCreationOptions tco;
            InternalTaskOptions dontcare;
            Task.CreationOptionsFromContinuationOptions(continuationOptions, out tco, out dontcare);
            return new Task<TResult>(true, default(TResult), tco, ct);
        }

        //
        // ContinueWhenAll() methods
        //

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> 
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in 
        /// the <paramref name="tasks"/> array have completed.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the 
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the 
        /// <paramref name="tasks"/> array is empty.</exception>
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see> 
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in 
        /// the <paramref name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</returns>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the <paramref
        /// name="tasks"/> array have completed.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</returns>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <param name="continuationFunction">The function delegate to execute when all tasks in the <paramref
        /// name="tasks"/> array have completed.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</returns>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction,
            CancellationToken cancellationToken)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction,
            TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of a set of provided Tasks.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

   
        // Core implementation of ContinueWhenAll -- the generic version
        // Note: if you make any changes to this method, please do the same to the non-generic version too. 
        internal static Task<TResult> ContinueWhenAllImpl<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>[], TResult> continuationFunction, Action<Task<TAntecedentResult>[]> continuationAction,
            TaskContinuationOptions continuationOptions, CancellationToken cancellationToken, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            // check arguments
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            if (tasks == null) throw new ArgumentNullException("tasks");
            //ArgumentNullException of continuationFunction or continuationAction is checked by the caller
            Contract.Requires((continuationFunction != null) != (continuationAction != null), "Expected exactly one of endFunction/endAction to be non-null");
            if (scheduler == null) throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();

            // Check tasks array and make defensive copy
            Task<TAntecedentResult>[] tasksCopy = TaskFactory.CheckMultiContinuationTasksAndCopy<TAntecedentResult>(tasks);

            // Bail early if cancellation has been requested.
            if (cancellationToken.IsCancellationRequested
                && ((continuationOptions & TaskContinuationOptions.LazyCancellation) == 0)
                )
            {
                return CreateCanceledTask(continuationOptions, cancellationToken);
            }

            // Call common ContinueWhenAll() setup logic, extract starter task.
            var starter = TaskFactory.CommonCWAllLogic(tasksCopy);

            // returned continuation task, off of starter
            if (continuationFunction != null)
            {
                return starter.ContinueWith<TResult>(
                   // use a cached delegate
                   GenericDelegateCache<TAntecedentResult, TResult>.CWAllFuncDelegate,
                   continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
            else
            {
                Contract.Assert(continuationAction != null);

                return starter.ContinueWith<TResult>(
                   // use a cached delegate
                   GenericDelegateCache<TAntecedentResult, TResult>.CWAllActionDelegate,
                   continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
        }

        // Core implementation of ContinueWhenAll -- the non-generic version
        // Note: if you make any changes to this method, please do the same to the generic version too. 
        internal static Task<TResult> ContinueWhenAllImpl(Task[] tasks,
            Func<Task[], TResult> continuationFunction, Action<Task[]> continuationAction,
            TaskContinuationOptions continuationOptions, CancellationToken cancellationToken, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            // check arguments
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            if (tasks == null) throw new ArgumentNullException("tasks");
            //ArgumentNullException of continuationFunction or continuationAction is checked by the caller
            Contract.Requires((continuationFunction != null) != (continuationAction != null), "Expected exactly one of endFunction/endAction to be non-null");
            if (scheduler == null) throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();

            // Check tasks array and make defensive copy
            Task[] tasksCopy = TaskFactory.CheckMultiContinuationTasksAndCopy(tasks);

            // Bail early if cancellation has been requested.
            if (cancellationToken.IsCancellationRequested
                && ((continuationOptions & TaskContinuationOptions.LazyCancellation) == 0)
                )
            {
                return CreateCanceledTask(continuationOptions, cancellationToken);
            }

            // Perform common ContinueWhenAll() setup logic, extract starter task
            var starter = TaskFactory.CommonCWAllLogic(tasksCopy);

            // returned continuation task, off of starter
            if (continuationFunction != null)
            {
                return starter.ContinueWith(
                    //the following delegate avoids closure capture as much as possible
                    //completedTasks.Result == tasksCopy;
                    //state == continuationFunction
                    (completedTasks, state) => 
                    {
                        completedTasks.NotifyDebuggerOfWaitCompletionIfNecessary();
                        return ((Func<Task[], TResult>)state)(completedTasks.Result); 
                    },
                    continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
            else
            {
                Contract.Assert(continuationAction != null);
                return starter.ContinueWith<TResult>(
                    //the following delegate avoids closure capture as much as possible
                    //completedTasks.Result == tasksCopy;
                    //state == continuationAction
                   (completedTasks, state) => 
                   {
                       completedTasks.NotifyDebuggerOfWaitCompletionIfNecessary();
                       ((Action<Task[]>)state)(completedTasks.Result); return default(TResult); 
                   },
                   continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
        }

        //
        // ContinueWhenAny() methods
        //

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the <paramref
        /// name="tasks"/> array completes.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</returns>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is null.</exception>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="continuationFunction"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array contains a null value.</exception>
        /// <exception cref="T:System.ArgumentException">The exception that is thrown when the
        /// <paramref name="tasks"/> array is empty.</exception>
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the <paramref
        /// name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</returns>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the <paramref
        /// name="tasks"/> array completes.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</returns>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
        /// <param name="tasks">The array of tasks from which to continue when one task completes.</param>
        /// <param name="continuationFunction">The function delegate to execute when one task in the <paramref
        /// name="tasks"/> array completes.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken">CancellationToken</see> 
        /// that will be assigned to the new continuation task.</param>
        /// <param name="continuationOptions">The <see cref="System.Threading.Tasks.TaskContinuationOptions">
        /// TaskContinuationOptions</see> value that controls the behavior of
        /// the created continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</param>
        /// <param name="scheduler">The <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// that is used to schedule the created continuation <see
        /// cref="T:System.Threading.Tasks.Task">Task</see>.</param>
        /// <returns>The new continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>.</returns>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction,
            CancellationToken cancellationToken)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction,
            TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        /// <summary>
        /// Creates a continuation <see cref="T:System.Threading.Tasks.Task{TResult}">Task</see>
        /// that will be started upon the completion of any Task in the provided set.
        /// </summary>
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable            
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null) throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        // Core implementation of ContinueWhenAny, non-generic version
        // Note: if you make any changes to this method, be sure to do the same to the generic version 
        internal static Task<TResult> ContinueWhenAnyImpl(Task[] tasks,
            Func<Task, TResult> continuationFunction, Action<Task> continuationAction,
            TaskContinuationOptions continuationOptions, CancellationToken cancellationToken, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            // check arguments
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            if (tasks == null) throw new ArgumentNullException("tasks");
            if(tasks.Length == 0) throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");

            //ArgumentNullException of continuationFunction or continuationAction is checked by the caller
            Contract.Requires((continuationFunction != null) != (continuationAction != null), "Expected exactly one of endFunction/endAction to be non-null");
            if (scheduler == null) throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();

            // Call common ContinueWhenAny() setup logic, extract starter
            Task<Task> starter = TaskFactory.CommonCWAnyLogic(tasks);

            // Bail early if cancellation has been requested.
            if (cancellationToken.IsCancellationRequested
                && ((continuationOptions & TaskContinuationOptions.LazyCancellation) == 0)
                )
            {
                return CreateCanceledTask(continuationOptions, cancellationToken);
            }

            // returned continuation task, off of starter
            if (continuationFunction != null)
            {
                return starter.ContinueWith(
                    //the following delegate avoids closure capture as much as possible
                    //completedTask.Result is the winning task; state == continuationAction
                     (completedTask, state) => { return ((Func<Task, TResult>)state)(completedTask.Result); },
                     continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
            else
            {
                Contract.Assert(continuationAction != null);
                return starter.ContinueWith<TResult>(
                    //the following delegate avoids closure capture as much as possible
                    //completedTask.Result is the winning task; state == continuationAction
                    (completedTask, state) => { ((Action<Task>)state)(completedTask.Result); return default(TResult); },
                    continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
        }


        // Core implementation of ContinueWhenAny, generic version
        // Note: if you make any changes to this method, be sure to do the same to the non-generic version 
        internal static Task<TResult> ContinueWhenAnyImpl<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>, TResult> continuationFunction, Action<Task<TAntecedentResult>> continuationAction,
            TaskContinuationOptions continuationOptions, CancellationToken cancellationToken, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            // check arguments
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            if (tasks == null) throw new ArgumentNullException("tasks");
            if (tasks.Length == 0) throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");
            //ArgumentNullException of continuationFunction or continuationAction is checked by the caller
            Contract.Requires((continuationFunction != null) != (continuationAction != null), "Expected exactly one of endFunction/endAction to be non-null");
            if (scheduler == null) throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();

            // Call common ContinueWhenAny setup logic, extract starter
            var starter = TaskFactory.CommonCWAnyLogic(tasks);

            // Bail early if cancellation has been requested.
            if (cancellationToken.IsCancellationRequested
                && ((continuationOptions & TaskContinuationOptions.LazyCancellation) == 0)
                )
            {
                return CreateCanceledTask(continuationOptions, cancellationToken);
            }

            // returned continuation task, off of starter
            if (continuationFunction != null)
            {
                return starter.ContinueWith<TResult>(
                    // Use a cached delegate
                    GenericDelegateCache<TAntecedentResult, TResult>.CWAnyFuncDelegate,
                    continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
            else
            {
                Contract.Assert(continuationAction != null);
                return starter.ContinueWith<TResult>(
                    // Use a cached delegate
                    GenericDelegateCache<TAntecedentResult,TResult>.CWAnyActionDelegate,
                    continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
        }
    }

    // For the ContinueWhenAnyImpl/ContinueWhenAllImpl methods that are generic on TAntecedentResult,
    // the compiler won't cache the internal ContinueWith delegate because it is generic on both
    // TAntecedentResult and TResult.  The GenericDelegateCache serves as a cache for those delegates.
    internal static class GenericDelegateCache<TAntecedentResult, TResult>
    {
        // ContinueWith delegate for TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(non-null continuationFunction)
        internal static Func<Task<Task>, object, TResult> CWAnyFuncDelegate =
            (Task<Task> wrappedWinner, object state) =>
            {
                var func = (Func<Task<TAntecedentResult>, TResult>)state;
                var arg = (Task<TAntecedentResult>)wrappedWinner.Result;
                return func(arg);
            };

        // ContinueWith delegate for TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(non-null continuationAction)
        internal static Func<Task<Task>, object, TResult> CWAnyActionDelegate =
            (Task<Task> wrappedWinner, object state) =>
            {
                var action = (Action<Task<TAntecedentResult>>)state;
                var arg = (Task<TAntecedentResult>)wrappedWinner.Result;
                action(arg);
                return default(TResult);
            };

        // ContinueWith delegate for TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(non-null continuationFunction)
        internal static Func<Task<Task<TAntecedentResult>[]>, object, TResult> CWAllFuncDelegate =
            (Task<Task<TAntecedentResult>[]> wrappedAntecedents, object state) =>
            {
                wrappedAntecedents.NotifyDebuggerOfWaitCompletionIfNecessary();
                var func = (Func<Task<TAntecedentResult>[], TResult>)state;
                return func(wrappedAntecedents.Result);
            };

        // ContinueWith delegate for TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(non-null continuationAction)
        internal static Func<Task<Task<TAntecedentResult>[]>, object, TResult> CWAllActionDelegate =
            (Task<Task<TAntecedentResult>[]> wrappedAntecedents, object state) =>
            {
                wrappedAntecedents.NotifyDebuggerOfWaitCompletionIfNecessary();
                var action = (Action<Task<TAntecedentResult>[]>)state;
                action(wrappedAntecedents.Result);
                return default(TResult);
            };

    }

}
