// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// A pair of schedulers that together support concurrent (reader) / exclusive (writer)
// task scheduling.  Using just the exclusive scheduler can be used to simulate a serial
// processing queue, and using just the concurrent scheduler with a specified
// MaximumConcurrentlyLevel can be used to achieve a MaxDegreeOfParallelism across
// a bunch of tasks, parallel loops, dataflow blocks, etc.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides concurrent and exclusive task schedulers that coordinate to execute
    /// tasks while ensuring that concurrent tasks may run concurrently and exclusive tasks never do.
    /// </summary>
    [DebuggerDisplay("Concurrent={ConcurrentTaskCountForDebugger}, Exclusive={ExclusiveTaskCountForDebugger}, Mode={ModeForDebugger}")]
    [DebuggerTypeProxy(typeof(ConcurrentExclusiveSchedulerPair.DebugView))]
    public class ConcurrentExclusiveSchedulerPair
    {
        /// <summary>A processing mode to denote what kinds of tasks are currently being processed on this thread.</summary>
        private readonly ThreadLocal<ProcessingMode> m_threadProcessingMode = new ThreadLocal<ProcessingMode>();
        /// <summary>The scheduler used to queue and execute "concurrent" tasks that may run concurrently with other concurrent tasks.</summary>
        private readonly ConcurrentExclusiveTaskScheduler m_concurrentTaskScheduler;
        /// <summary>The scheduler used to queue and execute "exclusive" tasks that must run exclusively while no other tasks for this pair are running.</summary>
        private readonly ConcurrentExclusiveTaskScheduler m_exclusiveTaskScheduler;
        /// <summary>The underlying task scheduler to which all work should be scheduled.</summary>
        private readonly TaskScheduler m_underlyingTaskScheduler;
        /// <summary>
        /// The maximum number of tasks allowed to run concurrently.  This only applies to concurrent tasks,
        /// since exclusive tasks are inherently limited to 1.
        /// </summary>
        private readonly int m_maxConcurrencyLevel;
        /// <summary>The maximum number of tasks we can process before recycling our runner tasks.</summary>
        private readonly int m_maxItemsPerTask;
        /// <summary>
        /// If positive, it represents the number of concurrently running concurrent tasks.
        /// If negative, it means an exclusive task has been scheduled.
        /// If 0, nothing has been scheduled.
        /// </summary>
        private int m_processingCount;
        /// <summary>Completion state for a task representing the completion of this pair.</summary>
        /// <remarks>Lazily-initialized only if the scheduler pair is shutting down or if the Completion is requested.</remarks>
        private CompletionState? m_completionState;
        /// <summary>Lazily-initialized work item for processing when targeting the default scheduler.</summary>
        private SchedulerWorkItem? m_threadPoolWorkItem;

        /// <summary>A constant value used to signal unlimited processing.</summary>
        private const int UNLIMITED_PROCESSING = -1;
        /// <summary>Constant used for m_processingCount to indicate that an exclusive task is being processed.</summary>
        private const int EXCLUSIVE_PROCESSING_SENTINEL = -1;
        /// <summary>Default MaxItemsPerTask to use for processing if none is specified.</summary>
        private const int DEFAULT_MAXITEMSPERTASK = UNLIMITED_PROCESSING;
        /// <summary>Default MaxConcurrencyLevel is the processor count if not otherwise specified.</summary>
        private static int DefaultMaxConcurrencyLevel => Environment.ProcessorCount;

        /// <summary>Gets the sync obj used to protect all state on this instance.</summary>
        private object ValueLock => m_threadProcessingMode;

        /// <summary>
        /// Initializes the ConcurrentExclusiveSchedulerPair.
        /// </summary>
        public ConcurrentExclusiveSchedulerPair() :
            this(TaskScheduler.Default, DefaultMaxConcurrencyLevel, DEFAULT_MAXITEMSPERTASK)
        { }

        /// <summary>
        /// Initializes the ConcurrentExclusiveSchedulerPair to target the specified scheduler.
        /// </summary>
        /// <param name="taskScheduler">The target scheduler on which this pair should execute.</param>
        public ConcurrentExclusiveSchedulerPair(TaskScheduler taskScheduler) :
            this(taskScheduler, DefaultMaxConcurrencyLevel, DEFAULT_MAXITEMSPERTASK)
        { }

        /// <summary>
        /// Initializes the ConcurrentExclusiveSchedulerPair to target the specified scheduler with a maximum concurrency level.
        /// </summary>
        /// <param name="taskScheduler">The target scheduler on which this pair should execute.</param>
        /// <param name="maxConcurrencyLevel">The maximum number of tasks to run concurrently.</param>
        public ConcurrentExclusiveSchedulerPair(TaskScheduler taskScheduler, int maxConcurrencyLevel) :
            this(taskScheduler, maxConcurrencyLevel, DEFAULT_MAXITEMSPERTASK)
        { }

        /// <summary>
        /// Initializes the ConcurrentExclusiveSchedulerPair to target the specified scheduler with a maximum
        /// concurrency level and a maximum number of scheduled tasks that may be processed as a unit.
        /// </summary>
        /// <param name="taskScheduler">The target scheduler on which this pair should execute.</param>
        /// <param name="maxConcurrencyLevel">The maximum number of tasks to run concurrently.</param>
        /// <param name="maxItemsPerTask">The maximum number of tasks to process for each underlying scheduled task used by the pair.</param>
        public ConcurrentExclusiveSchedulerPair(TaskScheduler taskScheduler, int maxConcurrencyLevel, int maxItemsPerTask)
        {
            // Validate arguments
            if (taskScheduler == null) throw new ArgumentNullException(nameof(taskScheduler));
            if (maxConcurrencyLevel == 0 || maxConcurrencyLevel < -1) throw new ArgumentOutOfRangeException(nameof(maxConcurrencyLevel));
            if (maxItemsPerTask == 0 || maxItemsPerTask < -1) throw new ArgumentOutOfRangeException(nameof(maxItemsPerTask));

            // Store configuration
            m_underlyingTaskScheduler = taskScheduler;
            m_maxConcurrencyLevel = maxConcurrencyLevel;
            m_maxItemsPerTask = maxItemsPerTask;

            // Downgrade to the underlying scheduler's max degree of parallelism if it's lower than the user-supplied level
            int mcl = taskScheduler.MaximumConcurrencyLevel;
            if (mcl > 0 && mcl < m_maxConcurrencyLevel) m_maxConcurrencyLevel = mcl;

            // Treat UNLIMITED_PROCESSING/-1 for both MCL and MIPT as the biggest possible value so that we don't
            // have to special case UNLIMITED_PROCESSING later on in processing.
            if (m_maxConcurrencyLevel == UNLIMITED_PROCESSING) m_maxConcurrencyLevel = int.MaxValue;
            if (m_maxItemsPerTask == UNLIMITED_PROCESSING) m_maxItemsPerTask = int.MaxValue;

            // Create the concurrent/exclusive schedulers for this pair
            m_exclusiveTaskScheduler = new ConcurrentExclusiveTaskScheduler(this, 1, ProcessingMode.ProcessingExclusiveTask);
            m_concurrentTaskScheduler = new ConcurrentExclusiveTaskScheduler(this, m_maxConcurrencyLevel, ProcessingMode.ProcessingConcurrentTasks);
        }

        /// <summary>Informs the scheduler pair that it should not accept any more tasks.</summary>
        /// <remarks>
        /// Calling <see cref="Complete"/> is optional, and it's only necessary if the <see cref="Completion"/>
        /// will be relied on for notification of all processing being completed.
        /// </remarks>
        public void Complete()
        {
            lock (ValueLock)
            {
                if (!CompletionRequested)
                {
                    RequestCompletion();
                    CleanupStateIfCompletingAndQuiesced();
                }
            }
        }

        /// <summary>Gets a <see cref="System.Threading.Tasks.Task"/> that will complete when the scheduler has completed processing.</summary>
        public Task Completion => EnsureCompletionStateInitialized();

        /// <summary>Gets the lazily-initialized completion state.</summary>
        private CompletionState EnsureCompletionStateInitialized() =>
            // ValueLock not needed, but it's ok if it's held
            LazyInitializer.EnsureInitialized(ref m_completionState, () => new CompletionState());

        /// <summary>Gets whether completion has been requested.</summary>
        private bool CompletionRequested => m_completionState != null && Volatile.Read(ref m_completionState.m_completionRequested);

        /// <summary>Sets that completion has been requested.</summary>
        private void RequestCompletion()
        {
            ContractAssertMonitorStatus(ValueLock, held: true);
            EnsureCompletionStateInitialized().m_completionRequested = true;
        }

        /// <summary>
        /// Cleans up state if and only if there's no processing currently happening
        /// and no more to be done later.
        /// </summary>
        private void CleanupStateIfCompletingAndQuiesced()
        {
            ContractAssertMonitorStatus(ValueLock, held: true);
            if (ReadyToComplete) CompleteTaskAsync();
        }

        /// <summary>Gets whether the pair is ready to complete.</summary>
        private bool ReadyToComplete
        {
            get
            {
                ContractAssertMonitorStatus(ValueLock, held: true);

                // We can only complete if completion has been requested and no processing is currently happening.
                if (!CompletionRequested || m_processingCount != 0) return false;

                // Now, only allow shutdown if an exception occurred or if there are no more tasks to process.
                CompletionState cs = EnsureCompletionStateInitialized();
                return
                    (cs.m_exceptions != null && cs.m_exceptions.Count > 0) ||
                    (m_concurrentTaskScheduler.m_tasks.IsEmpty && m_exclusiveTaskScheduler.m_tasks.IsEmpty);
            }
        }

        /// <summary>Completes the completion task asynchronously.</summary>
        private void CompleteTaskAsync()
        {
            Debug.Assert(ReadyToComplete, "The block must be ready to complete to be here.");
            ContractAssertMonitorStatus(ValueLock, held: true);

            // Ensure we only try to complete once, then schedule completion
            // in order to escape held locks and the caller's context
            CompletionState cs = EnsureCompletionStateInitialized();
            if (!cs.m_completionQueued)
            {
                cs.m_completionQueued = true;
                ThreadPool.QueueUserWorkItem(static state =>
                {
                    Debug.Assert(state is ConcurrentExclusiveSchedulerPair);
                    var localThis = (ConcurrentExclusiveSchedulerPair)state;
                    Debug.Assert(!localThis.m_completionState!.IsCompleted, "Completion should only happen once.");

                    List<Exception>? exceptions = localThis.m_completionState.m_exceptions;
                    bool success = (exceptions != null && exceptions.Count > 0) ?
                        localThis.m_completionState.TrySetException(exceptions) :
                        localThis.m_completionState.TrySetResult();
                    Debug.Assert(success, "Expected to complete completion task.");

                    localThis.m_threadProcessingMode.Dispose();
                }, this);
            }
        }

        /// <summary>Initiates scheduler shutdown due to a worker task faulting.</summary>
        /// <param name="faultedTask">The faulted worker task that's initiating the shutdown.</param>
        private void FaultWithTask(Task faultedTask)
        {
            Debug.Assert(faultedTask != null && faultedTask.IsFaulted && faultedTask.Exception!.InnerExceptions.Count > 0,
                "Needs a task in the faulted state and thus with exceptions.");
            ContractAssertMonitorStatus(ValueLock, held: true);

            // Store the faulted task's exceptions
            CompletionState cs = EnsureCompletionStateInitialized();
            cs.m_exceptions ??= new List<Exception>();
            cs.m_exceptions.AddRange(faultedTask.Exception.InnerExceptions);

            // Now that we're doomed, request completion
            RequestCompletion();
        }

        /// <summary>
        /// Gets a TaskScheduler that can be used to schedule tasks to this pair
        /// that may run concurrently with other tasks on this pair.
        /// </summary>
        public TaskScheduler ConcurrentScheduler => m_concurrentTaskScheduler;
        /// <summary>
        /// Gets a TaskScheduler that can be used to schedule tasks to this pair
        /// that must run exclusively with regards to other tasks on this pair.
        /// </summary>
        public TaskScheduler ExclusiveScheduler => m_exclusiveTaskScheduler;

        /// <summary>Gets the number of tasks waiting to run concurrently.</summary>
        /// <remarks>This does not take the necessary lock, as it's only called from under the debugger.</remarks>
        private int ConcurrentTaskCountForDebugger => m_concurrentTaskScheduler.m_tasks.Count;

        /// <summary>Gets the number of tasks waiting to run exclusively.</summary>
        /// <remarks>This does not take the necessary lock, as it's only called from under the debugger.</remarks>
        private int ExclusiveTaskCountForDebugger => m_exclusiveTaskScheduler.m_tasks.Count;

        /// <summary>Notifies the pair that new work has arrived to be processed.</summary>
        /// <param name="fairly">Whether tasks should be scheduled fairly with regards to other tasks.</param>
        /// <remarks>Must only be called while holding the lock.</remarks>
        private void ProcessAsyncIfNecessary(bool fairly = false)
        {
            ContractAssertMonitorStatus(ValueLock, held: true);

            // If the current processing count is >= 0, we can potentially launch further processing.
            if (m_processingCount >= 0)
            {
                // We snap whether there are any exclusive tasks or concurrent tasks waiting.
                // (We grab the concurrent count below only once we know we need it.)
                // With processing happening concurrent to this operation, this data may
                // immediately be out of date, but it can only go from non-empty
                // to empty and not the other way around.  As such, this is safe,
                // as worst case is we'll schedule an extra  task when we didn't
                // otherwise need to, and we'll just eat its overhead.
                bool exclusiveTasksAreWaiting = !m_exclusiveTaskScheduler.m_tasks.IsEmpty;

                // If there's no processing currently happening but there are waiting exclusive tasks,
                // let's start processing those exclusive tasks.
                Task? processingTask = null;
                if (m_processingCount == 0 && exclusiveTasksAreWaiting)
                {
                    // Launch exclusive task processing
                    m_processingCount = EXCLUSIVE_PROCESSING_SENTINEL; // -1
                    if (!TryQueueThreadPoolWorkItem(fairly))
                    {
                        try
                        {
                            processingTask = new Task(thisPair => ((ConcurrentExclusiveSchedulerPair)thisPair!).ProcessExclusiveTasks(), this,
                                default, GetCreationOptionsForTask(fairly));
                            processingTask.Start(m_underlyingTaskScheduler);
                            // When we call Start, if the underlying scheduler throws in QueueTask, TPL will fault the task and rethrow
                            // the exception.  To deal with that, we need a reference to the task object, so that we can observe its exception.
                            // Hence, we separate creation and starting, so that we can store a reference to the task before we attempt QueueTask.
                        }
                        catch (Exception e)
                        {
                            m_processingCount = 0;
                            FaultWithTask(processingTask ?? Task.FromException(e));
                        }
                    }
                }
                // If there are no waiting exclusive tasks, there are concurrent tasks, and we haven't reached our maximum
                // concurrency level for processing, let's start processing more concurrent tasks.
                else
                {
                    int concurrentTasksWaitingCount = m_concurrentTaskScheduler.m_tasks.Count;

                    if (concurrentTasksWaitingCount > 0 && !exclusiveTasksAreWaiting && m_processingCount < m_maxConcurrencyLevel)
                    {
                        // Launch concurrent task processing, up to the allowed limit
                        for (int i = 0; i < concurrentTasksWaitingCount && m_processingCount < m_maxConcurrencyLevel; ++i)
                        {
                            ++m_processingCount;
                            if (!TryQueueThreadPoolWorkItem(fairly))
                            {
                                try
                                {
                                    processingTask = new Task(thisPair => ((ConcurrentExclusiveSchedulerPair)thisPair!).ProcessConcurrentTasks(), this,
                                        default, GetCreationOptionsForTask(fairly));
                                    processingTask.Start(m_underlyingTaskScheduler); // See above logic for why we use new + Start rather than StartNew
                                }
                                catch (Exception e)
                                {
                                    --m_processingCount;
                                    FaultWithTask(processingTask ?? Task.FromException(e));
                                }
                            }
                        }
                    }
                }

                // Check to see if all tasks have completed and if completion has been requested.
                CleanupStateIfCompletingAndQuiesced();
            }
            else Debug.Assert(m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL, "The processing count must be the sentinel if it's not >= 0.");
        }

        /// <summary>Queues concurrent or exclusive task processing to the ThreadPool if the underlying scheduler is the default.</summary>
        /// <param name="fairly">Whether tasks should be scheduled fairly with regards to other tasks.</param>
        /// <returns>true if we're targeting the thread pool such that a worker could be queued; otherwise, false.</returns>
        private bool TryQueueThreadPoolWorkItem(bool fairly)
        {
            if (TaskScheduler.Default == m_underlyingTaskScheduler)
            {
                IThreadPoolWorkItem workItem = m_threadPoolWorkItem ??= new SchedulerWorkItem(this);
                ThreadPool.UnsafeQueueUserWorkItemInternal(workItem, preferLocal: !fairly);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Processes exclusive tasks serially until either there are no more to process
        /// or we've reached our user-specified maximum limit.
        /// </summary>
        private void ProcessExclusiveTasks()
        {
            Debug.Assert(m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL, "Processing exclusive tasks requires being in exclusive mode.");
            Debug.Assert(!m_exclusiveTaskScheduler.m_tasks.IsEmpty, "Processing exclusive tasks requires tasks to be processed.");
            ContractAssertMonitorStatus(ValueLock, held: false);
            try
            {
                // Note that we're processing exclusive tasks on the current thread
                Debug.Assert(m_threadProcessingMode.Value == ProcessingMode.NotCurrentlyProcessing,
                    "This thread should not yet be involved in this pair's processing.");
                m_threadProcessingMode.Value = ProcessingMode.ProcessingExclusiveTask;

                // Process up to the maximum number of items per task allowed
                for (int i = 0; i < m_maxItemsPerTask; i++)
                {
                    // Get the next available exclusive task.  If we can't find one, bail.
                    if (!m_exclusiveTaskScheduler.m_tasks.TryDequeue(out Task? exclusiveTask)) break;

                    // Execute the task.  If the scheduler was previously faulted,
                    // this task could have been faulted when it was queued; ignore such tasks.
                    if (!exclusiveTask.IsFaulted) m_exclusiveTaskScheduler.ExecuteTask(exclusiveTask);
                }
            }
            finally
            {
                // We're no longer processing exclusive tasks on the current thread
                Debug.Assert(m_threadProcessingMode.Value == ProcessingMode.ProcessingExclusiveTask,
                    "Somehow we ended up escaping exclusive mode.");
                m_threadProcessingMode.Value = ProcessingMode.NotCurrentlyProcessing;

                lock (ValueLock)
                {
                    // When this task was launched, we tracked it by setting m_processingCount to WRITER_IN_PROGRESS.
                    // now reset it to 0.  Then check to see whether there's more processing to be done.
                    // There might be more concurrent tasks available, for example, if concurrent tasks arrived
                    // after we exited the loop, or if we exited the loop while concurrent tasks were still
                    // available but we hit our maxItemsPerTask limit.
                    Debug.Assert(m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL, "The processing mode should not have deviated from exclusive.");
                    m_processingCount = 0;
                    ProcessAsyncIfNecessary(true);
                }
            }
        }

        /// <summary>
        /// Processes concurrent tasks serially until either there are no more to process,
        /// we've reached our user-specified maximum limit, or exclusive tasks have arrived.
        /// </summary>
        private void ProcessConcurrentTasks()
        {
            Debug.Assert(m_processingCount > 0, "Processing concurrent tasks requires us to be in concurrent mode.");
            ContractAssertMonitorStatus(ValueLock, held: false);
            try
            {
                // Note that we're processing concurrent tasks on the current thread
                Debug.Assert(m_threadProcessingMode.Value == ProcessingMode.NotCurrentlyProcessing,
                    "This thread should not yet be involved in this pair's processing.");
                m_threadProcessingMode.Value = ProcessingMode.ProcessingConcurrentTasks;

                // Process up to the maximum number of items per task allowed
                for (int i = 0; i < m_maxItemsPerTask; i++)
                {
                    // Get the next available concurrent task.  If we can't find one, bail.
                    if (!m_concurrentTaskScheduler.m_tasks.TryDequeue(out Task? concurrentTask)) break;

                    // Execute the task.  If the scheduler was previously faulted,
                    // this task could have been faulted when it was queued; ignore such tasks.
                    if (!concurrentTask.IsFaulted) m_concurrentTaskScheduler.ExecuteTask(concurrentTask);

                    // Now check to see if exclusive tasks have arrived; if any have, they take priority
                    // so we'll bail out here.  Note that we could have checked this condition
                    // in the for loop's condition, but that could lead to extra overhead
                    // in the case where a concurrent task arrives, this task is launched, and then
                    // before entering the loop an exclusive task arrives.  If we didn't execute at
                    // least one task, we would have spent all of the overhead to launch a
                    // task but with none of the benefit.  There's of course also an inherent
                    // race condition here with regards to exclusive tasks arriving, and we're ok with
                    // executing one more concurrent task than we should before giving priority to exclusive tasks.
                    if (!m_exclusiveTaskScheduler.m_tasks.IsEmpty) break;
                }
            }
            finally
            {
                // We're no longer processing concurrent tasks on the current thread
                Debug.Assert(m_threadProcessingMode.Value == ProcessingMode.ProcessingConcurrentTasks,
                    "Somehow we ended up escaping concurrent mode.");
                m_threadProcessingMode.Value = ProcessingMode.NotCurrentlyProcessing;

                lock (ValueLock)
                {
                    // When this task was launched, we tracked it with a positive processing count;
                    // decrement that count.  Then check to see whether there's more processing to be done.
                    // There might be more concurrent tasks available, for example, if concurrent tasks arrived
                    // after we exited the loop, or if we exited the loop while concurrent tasks were still
                    // available but we hit our maxItemsPerTask limit.
                    Debug.Assert(m_processingCount > 0, "The procesing mode should not have deviated from concurrent.");
                    if (m_processingCount > 0) --m_processingCount;
                    ProcessAsyncIfNecessary(true);
                }
            }
        }

        /// <summary>
        /// Holder for lazily-initialized state about the completion of a scheduler pair.
        /// Completion is only triggered either by rare exceptional conditions or by
        /// the user calling Complete, and as such we only lazily initialize this
        /// state in one of those conditions or if the user explicitly asks for
        /// the Completion.
        /// </summary>
        private sealed class CompletionState : Task
        {
            /// <summary>Whether the scheduler has had completion requested.</summary>
            /// <remarks>This variable is not volatile, so to gurantee safe reading reads, Volatile.Read is used in TryExecuteTaskInline.</remarks>
            internal bool m_completionRequested;
            /// <summary>Whether completion processing has been queued.</summary>
            internal bool m_completionQueued;
            /// <summary>Unrecoverable exceptions incurred while processing.</summary>
            internal List<Exception>? m_exceptions;
        }

        /// <summary>Reusable immutable work item that can be scheduled to the thread pool to run processing.</summary>
        private sealed class SchedulerWorkItem : IThreadPoolWorkItem
        {
            private readonly ConcurrentExclusiveSchedulerPair _pair;

            internal SchedulerWorkItem(ConcurrentExclusiveSchedulerPair pair) => _pair = pair;

            void IThreadPoolWorkItem.Execute()
            {
                if (_pair.m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL)
                {
                    _pair.ProcessExclusiveTasks();
                }
                else
                {
                    _pair.ProcessConcurrentTasks();
                }
            }
        }

        /// <summary>
        /// A scheduler shim used to queue tasks to the pair and execute those tasks on request of the pair.
        /// </summary>
        [DebuggerDisplay("Count={CountForDebugger}, MaxConcurrencyLevel={m_maxConcurrencyLevel}, Id={Id}")]
        [DebuggerTypeProxy(typeof(ConcurrentExclusiveTaskScheduler.DebugView))]
        private sealed class ConcurrentExclusiveTaskScheduler : TaskScheduler
        {
            /// <summary>Cached delegate for invoking TryExecuteTaskShim.</summary>
            private static readonly Func<object?, bool> s_tryExecuteTaskShim = new Func<object?, bool>(TryExecuteTaskShim);
            /// <summary>The parent pair.</summary>
            private readonly ConcurrentExclusiveSchedulerPair m_pair;
            /// <summary>The maximum concurrency level for the scheduler.</summary>
            private readonly int m_maxConcurrencyLevel;
            /// <summary>The processing mode of this scheduler, exclusive or concurrent.</summary>
            private readonly ProcessingMode m_processingMode;
            /// <summary>Gets the queue of tasks for this scheduler.</summary>
            internal readonly IProducerConsumerQueue<Task> m_tasks;

            /// <summary>Initializes the scheduler.</summary>
            /// <param name="pair">The parent pair.</param>
            /// <param name="maxConcurrencyLevel">The maximum degree of concurrency this scheduler may use.</param>
            /// <param name="processingMode">The processing mode of this scheduler.</param>
            internal ConcurrentExclusiveTaskScheduler(ConcurrentExclusiveSchedulerPair pair, int maxConcurrencyLevel, ProcessingMode processingMode)
            {
                Debug.Assert(pair != null, "Scheduler must be associated with a valid pair.");
                Debug.Assert(processingMode == ProcessingMode.ProcessingConcurrentTasks || processingMode == ProcessingMode.ProcessingExclusiveTask,
                    "Scheduler must be for concurrent or exclusive processing.");
                Debug.Assert(
                    (processingMode == ProcessingMode.ProcessingConcurrentTasks && (maxConcurrencyLevel >= 1 || maxConcurrencyLevel == UNLIMITED_PROCESSING)) ||
                    (processingMode == ProcessingMode.ProcessingExclusiveTask && maxConcurrencyLevel == 1),
                    "If we're in concurrent mode, our concurrency level should be positive or unlimited.  If exclusive, it should be 1.");

                m_pair = pair;
                m_maxConcurrencyLevel = maxConcurrencyLevel;
                m_processingMode = processingMode;
                m_tasks = (processingMode == ProcessingMode.ProcessingExclusiveTask) ?
                    (IProducerConsumerQueue<Task>)new SingleProducerSingleConsumerQueue<Task>() :
                    (IProducerConsumerQueue<Task>)new MultiProducerMultiConsumerQueue<Task>();
            }

            /// <summary>Gets the maximum concurrency level this scheduler is able to support.</summary>
            public override int MaximumConcurrencyLevel => m_maxConcurrencyLevel;

            /// <summary>Queues a task to the scheduler.</summary>
            /// <param name="task">The task to be queued.</param>
            protected internal override void QueueTask(Task task)
            {
                Debug.Assert(task != null, "Infrastructure should have provided a non-null task.");
                lock (m_pair.ValueLock)
                {
                    // If the scheduler has already had completion requested, no new work is allowed to be scheduled
                    if (m_pair.CompletionRequested) throw new InvalidOperationException(GetType().ToString());

                    // Queue the task, and then let the pair know that more work is now available to be scheduled
                    m_tasks.Enqueue(task);
                    m_pair.ProcessAsyncIfNecessary();
                }
            }

            /// <summary>Executes a task on this scheduler.</summary>
            /// <param name="task">The task to be executed.</param>
            internal void ExecuteTask(Task task)
            {
                Debug.Assert(task != null, "Infrastructure should have provided a non-null task.");
                base.TryExecuteTask(task);
            }

            /// <summary>Tries to execute the task synchronously on this scheduler.</summary>
            /// <param name="task">The task to execute.</param>
            /// <param name="taskWasPreviouslyQueued">Whether the task was previously queued to the scheduler.</param>
            /// <returns>true if the task could be executed; otherwise, false.</returns>
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                Debug.Assert(task != null, "Infrastructure should have provided a non-null task.");

                // If the scheduler has had completion requested, no new work is allowed to be scheduled.
                // A non-locked read on m_completionRequested (in CompletionRequested) is acceptable here because:
                // a) we don't need to be exact... a Complete call could come in later in the function anyway
                // b) this is only a fast path escape hatch.  To actually inline the task,
                //    we need to be inside of an already executing task, and in such a case,
                //    while completion may have been requested, we can't have shutdown yet.
                if (!taskWasPreviouslyQueued && m_pair.CompletionRequested) return false;

                // We know the implementation of the default scheduler and how it will behave.
                // As it's the most common underlying scheduler, we optimize for it.
                bool isDefaultScheduler = m_pair.m_underlyingTaskScheduler == TaskScheduler.Default;

                // If we're targeting the default scheduler and taskWasPreviouslyQueued is true,
                // we know that the default scheduler will only allow it to be inlined
                // if we're on a thread pool thread (but it won't always allow it in that case,
                // since it'll only allow inlining if it can find the task in the local queue).
                // As such, if we're not on a thread pool thread, we know for sure the
                // task won't be inlined, so let's not even try.
                if (isDefaultScheduler && taskWasPreviouslyQueued && !Thread.CurrentThread.IsThreadPoolThread)
                {
                    return false;
                }
                else
                {
                    // If a task is already running on this thread, allow inline execution to proceed.
                    // If there's already a task from this scheduler running on the current thread, we know it's safe
                    // to run this task, in effect temporarily taking that task's count allocation.
                    if (m_pair.m_threadProcessingMode.Value == m_processingMode)
                    {
                        // If we're targeting the default scheduler and taskWasPreviouslyQueued is false,
                        // we know the default scheduler will allow it, so we can just execute it here.
                        // Otherwise, delegate to the target scheduler's inlining.
                        return (isDefaultScheduler && !taskWasPreviouslyQueued) ?
                            TryExecuteTask(task) :
                            TryExecuteTaskInlineOnTargetScheduler(task);
                    }
                }

                // We're not in the context of a task already executing on this scheduler.  Bail.
                return false;
            }

            /// <summary>
            /// Implements a reasonable approximation for TryExecuteTaskInline on the underlying scheduler,
            /// which we can't call directly on the underlying scheduler.
            /// </summary>
            /// <param name="task">The task to execute inline if possible.</param>
            /// <returns>true if the task was inlined successfully; otherwise, false.</returns>
            private bool TryExecuteTaskInlineOnTargetScheduler(Task task)
            {
                // We'd like to simply call TryExecuteTaskInline here, but we can't.
                // As there's no built-in API for this, a workaround is to create a new task that,
                // when executed, will simply call TryExecuteTask to run the real task, and then
                // we run our new shim task synchronously on the target scheduler.  If all goes well,
                // our synchronous invocation will succeed in running the shim task on the current thread,
                // which will in turn run the real task on the current thread.  If the scheduler
                // doesn't allow that execution, RunSynchronously will block until the underlying scheduler
                // is able to invoke the task, which might account for an additional but unavoidable delay.
                // Once it's done, we can return whether the task executed by returning the
                // shim task's Result, which is in turn the result of TryExecuteTask.
                var t = new Task<bool>(s_tryExecuteTaskShim, Tuple.Create(this, task));
                try
                {
                    t.RunSynchronously(m_pair.m_underlyingTaskScheduler);
                    return t.Result;
                }
                catch
                {
                    Debug.Assert(t.IsFaulted, "Task should be faulted due to the scheduler faulting it and throwing the exception.");
                    _ = t.Exception;
                    throw;
                }
                finally { t.Dispose(); }
            }

            /// <summary>Shim used to invoke this.TryExecuteTask(task).</summary>
            /// <param name="state">A tuple of the ConcurrentExclusiveTaskScheduler and the task to execute.</param>
            /// <returns>true if the task was successfully inlined; otherwise, false.</returns>
            /// <remarks>
            /// This method is separated out not because of performance reasons but so that
            /// the SecuritySafeCritical attribute may be employed.
            /// </remarks>
            private static bool TryExecuteTaskShim(object? state)
            {
                Debug.Assert(state is Tuple<ConcurrentExclusiveTaskScheduler, Task>);
                var tuple = (Tuple<ConcurrentExclusiveTaskScheduler, Task>)state;
                return tuple.Item1.TryExecuteTask(tuple.Item2);
            }

            /// <summary>Gets for debugging purposes the tasks scheduled to this scheduler.</summary>
            /// <returns>An enumerable of the tasks queued.</returns>
            protected override IEnumerable<Task> GetScheduledTasks() { return m_tasks; }

            /// <summary>Gets the number of tasks queued to this scheduler.</summary>
            private int CountForDebugger => m_tasks.Count;

            /// <summary>Provides a debug view for ConcurrentExclusiveTaskScheduler.</summary>
            private sealed class DebugView
            {
                /// <summary>The scheduler being debugged.</summary>
                private readonly ConcurrentExclusiveTaskScheduler m_taskScheduler;

                /// <summary>Initializes the debug view.</summary>
                /// <param name="scheduler">The scheduler being debugged.</param>
                public DebugView(ConcurrentExclusiveTaskScheduler scheduler)
                {
                    Debug.Assert(scheduler != null, "Need a scheduler with which to construct the debug view.");
                    m_taskScheduler = scheduler;
                }

                /// <summary>Gets this pair's maximum allowed concurrency level.</summary>
                public int MaximumConcurrencyLevel => m_taskScheduler.m_maxConcurrencyLevel;
                /// <summary>Gets the tasks scheduled to this scheduler.</summary>
                public IEnumerable<Task> ScheduledTasks => m_taskScheduler.m_tasks;
                /// <summary>Gets the scheduler pair with which this scheduler is associated.</summary>
                public ConcurrentExclusiveSchedulerPair SchedulerPair => m_taskScheduler.m_pair;
            }
        }

        /// <summary>Provides a debug view for ConcurrentExclusiveSchedulerPair.</summary>
        private sealed class DebugView
        {
            /// <summary>The pair being debugged.</summary>
            private readonly ConcurrentExclusiveSchedulerPair m_pair;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="pair">The pair being debugged.</param>
            public DebugView(ConcurrentExclusiveSchedulerPair pair)
            {
                Debug.Assert(pair != null, "Need a pair with which to construct the debug view.");
                m_pair = pair;
            }

            /// <summary>Gets a representation of the execution state of the pair.</summary>
            public ProcessingMode Mode => m_pair.ModeForDebugger;
            /// <summary>Gets the number of tasks waiting to run exclusively.</summary>
            public IEnumerable<Task> ScheduledExclusive => m_pair.m_exclusiveTaskScheduler.m_tasks;
            /// <summary>Gets the number of tasks waiting to run concurrently.</summary>
            public IEnumerable<Task> ScheduledConcurrent => m_pair.m_concurrentTaskScheduler.m_tasks;
            /// <summary>Gets the number of tasks currently being executed.</summary>
            public int CurrentlyExecutingTaskCount => (m_pair.m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL) ? 1 : m_pair.m_processingCount;
            /// <summary>Gets the underlying task scheduler that actually executes the tasks.</summary>
            public TaskScheduler TargetScheduler => m_pair.m_underlyingTaskScheduler;
        }

        /// <summary>Gets an enumeration for debugging that represents the current state of the scheduler pair.</summary>
        /// <remarks>This is only for debugging.  It does not take the necessary locks to be useful for runtime usage.</remarks>
        private ProcessingMode ModeForDebugger
        {
            get
            {
                // If our completion task is done, so are we.
                if (m_completionState != null && m_completionState.IsCompleted) return ProcessingMode.Completed;

                // Otherwise, summarize our current state.
                ProcessingMode mode = ProcessingMode.NotCurrentlyProcessing;
                if (m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL) mode |= ProcessingMode.ProcessingExclusiveTask;
                if (m_processingCount >= 1) mode |= ProcessingMode.ProcessingConcurrentTasks;
                if (CompletionRequested) mode |= ProcessingMode.Completing;
                return mode;
            }
        }

        /// <summary>Asserts that a given synchronization object is either held or not held.</summary>
        /// <param name="syncObj">The monitor to check.</param>
        /// <param name="held">Whether we want to assert that it's currently held or not held.</param>
        [Conditional("DEBUG")]
        private static void ContractAssertMonitorStatus(object syncObj, bool held)
        {
            Debug.Assert(syncObj != null, "The monitor object to check must be provided.");
            Debug.Assert(Monitor.IsEntered(syncObj) == held, "The locking scheme was not correctly followed.");
        }

        /// <summary>Gets the options to use for tasks.</summary>
        /// <param name="isReplacementReplica">If this task is being created to replace another.</param>
        /// <remarks>
        /// These options should be used for all tasks that have the potential to run user code or
        /// that are repeatedly spawned and thus need a modicum of fair treatment.
        /// </remarks>
        /// <returns>The options to use.</returns>
        internal static TaskCreationOptions GetCreationOptionsForTask(bool isReplacementReplica = false)
        {
            TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;
            if (isReplacementReplica) options |= TaskCreationOptions.PreferFairness;
            return options;
        }

        /// <summary>Provides an enumeration that represents the current state of the scheduler pair.</summary>
        [Flags]
        private enum ProcessingMode : byte
        {
            /// <summary>The scheduler pair is currently dormant, with no work scheduled.</summary>
            NotCurrentlyProcessing = 0x0,
            /// <summary>The scheduler pair has queued processing for exclusive tasks.</summary>
            ProcessingExclusiveTask = 0x1,
            /// <summary>The scheduler pair has queued processing for concurrent tasks.</summary>
            ProcessingConcurrentTasks = 0x2,
            /// <summary>Completion has been requested.</summary>
            Completing = 0x4,
            /// <summary>The scheduler pair is finished processing.</summary>
            Completed = 0x8
        }
    }
}
