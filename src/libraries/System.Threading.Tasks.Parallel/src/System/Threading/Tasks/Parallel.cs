// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
// A helper class that contains parallel versions of various looping constructs.  This
// internally uses the task parallel library, but takes care to expose very little
// evidence of this infrastructure being used.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Stores options that configure the operation of methods on the
    /// <see cref="System.Threading.Tasks.Parallel">Parallel</see> class.
    /// </summary>
    /// <remarks>
    /// By default, methods on the Parallel class attempt to utilize all available processors, are non-cancelable, and target
    /// the default TaskScheduler (TaskScheduler.Default). <see cref="ParallelOptions"/> enables
    /// overriding these defaults.
    /// </remarks>
    public class ParallelOptions
    {
        private TaskScheduler? _scheduler;
        private int _maxDegreeOfParallelism;
        private CancellationToken _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelOptions"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor initializes the instance with default values.  <see cref="MaxDegreeOfParallelism"/>
        /// is initialized to -1, signifying that there is no upper bound set on how much parallelism should
        /// be employed.  <see cref="CancellationToken"/> is initialized to a non-cancelable token,
        /// and <see cref="TaskScheduler"/> is initialized to the default scheduler (TaskScheduler.Default).
        /// All of these defaults may be overwritten using the property set accessors on the instance.
        /// </remarks>
        public ParallelOptions()
        {
            _scheduler = TaskScheduler.Default;
            _maxDegreeOfParallelism = -1;
            _cancellationToken = CancellationToken.None;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Threading.Tasks.TaskScheduler">TaskScheduler</see>
        /// associated with this <see cref="ParallelOptions"/> instance. Setting this property to null
        /// indicates that the current scheduler should be used.
        /// </summary>
        public TaskScheduler? TaskScheduler
        {
            get { return _scheduler; }
            set { _scheduler = value; }
        }

        // Convenience property used by TPL logic
        internal TaskScheduler EffectiveTaskScheduler => _scheduler ?? TaskScheduler.Current;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism enabled by this ParallelOptions instance.
        /// </summary>
        /// <remarks>
        /// The <see cref="MaxDegreeOfParallelism"/> limits the number of concurrent operations run by <see
        /// cref="System.Threading.Tasks.Parallel">Parallel</see> method calls that are passed this
        /// ParallelOptions instance to the set value, if it is positive. If <see
        /// cref="MaxDegreeOfParallelism"/> is -1, then there is no limit placed on the number of concurrently
        /// running operations.
        /// </remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The exception that is thrown when this <see cref="MaxDegreeOfParallelism"/> is set to 0 or some
        /// value less than -1.
        /// </exception>
        public int MaxDegreeOfParallelism
        {
            get { return _maxDegreeOfParallelism; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfZero(value, nameof(MaxDegreeOfParallelism));
                ArgumentOutOfRangeException.ThrowIfLessThan(value, -1, nameof(MaxDegreeOfParallelism));
                _maxDegreeOfParallelism = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// associated with this <see cref="ParallelOptions"/> instance.
        /// </summary>
        /// <remarks>
        /// Providing a <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// to a <see cref="System.Threading.Tasks.Parallel">Parallel</see> method enables the operation to be
        /// exited early. Code external to the operation may cancel the token, and if the operation observes the
        /// token being set, it may exit early by throwing an
        /// <see cref="System.OperationCanceledException"/>.
        /// </remarks>
        public CancellationToken CancellationToken
        {
            get { return _cancellationToken; }
            set { _cancellationToken = value; }
        }

        internal int EffectiveMaxConcurrencyLevel
        {
            get
            {
                int rval = MaxDegreeOfParallelism;
                int schedulerMax = EffectiveTaskScheduler.MaximumConcurrencyLevel;
                if ((schedulerMax > 0) && (schedulerMax != int.MaxValue))
                {
                    rval = (rval == -1) ? schedulerMax : Math.Min(schedulerMax, rval);
                }
                return rval;
            }
        }
    }  // class ParallelOptions

    /// <summary>
    /// Provides support for parallel loops and regions.
    /// </summary>
    /// <remarks>
    /// The <see cref="System.Threading.Tasks.Parallel"/> class provides library-based data parallel replacements
    /// for common operations such as for loops, for each loops, and execution of a set of statements.
    /// </remarks>
    public static partial class Parallel
    {
        // static counter for generating unique Fork/Join Context IDs to be used in ETW events
        internal static int s_forkJoinContextID;

        // We use a stride for loops to amortize the frequency of interlocked operations.
        internal const int DEFAULT_LOOP_STRIDE = 16;

        // Static variable to hold default parallel options
        internal static readonly ParallelOptions s_defaultParallelOptions = new ParallelOptions();

        /// <summary>
        /// Executes each of the provided actions, possibly in parallel.
        /// </summary>
        /// <param name="actions">An array of <see cref="System.Action">Actions</see> to execute.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="actions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentException">The exception that is thrown when the
        /// <paramref name="actions"/> array contains a null element.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown when any
        /// action in the <paramref name="actions"/> array throws an exception.</exception>
        /// <remarks>
        /// This method can be used to execute a set of operations, potentially in parallel.
        /// No guarantees are made about the order in which the operations execute or whether
        /// they execute in parallel.  This method does not return until each of the
        /// provided operations has completed, regardless of whether completion
        /// occurs due to normal or exceptional termination.
        /// </remarks>
        public static void Invoke(params Action[] actions)
        {
            Invoke(s_defaultParallelOptions, actions);
        }

        /// <summary>
        /// Executes each of the provided actions, possibly in parallel.
        /// </summary>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="actions">An array of <see cref="System.Action">Actions</see> to execute.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="actions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentException">The exception that is thrown when the
        /// <paramref name="actions"/> array contains a null element.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> is set.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown when any
        /// action in the <paramref name="actions"/> array throws an exception.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <remarks>
        /// This method can be used to execute a set of operations, potentially in parallel.
        /// No guarantees are made about the order in which the operations execute or whether
        /// the they execute in parallel.  This method does not return until each of the
        /// provided operations has completed, regardless of whether completion
        /// occurs due to normal or exceptional termination.
        /// </remarks>
        public static void Invoke(ParallelOptions parallelOptions, params Action[] actions)
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(actions);

            // On .NET Framework, we throw an ODE if we're passed a disposed CancellationToken.
            // Here, CancellationToken.ThrowIfSourceDisposed() is not exposed.
            // This is benign, because we'll end up throwing ODE when we register
            // with the token later.

            // Quit early if we're already canceled -- avoid a bunch of work.
            parallelOptions.CancellationToken.ThrowIfCancellationRequested();

            // We must validate that the actions array contains no null elements, and also
            // make a defensive copy of the actions array.
            Action[] actionsCopy = new Action[actions.Length];
            for (int i = 0; i < actionsCopy.Length; i++)
            {
                actionsCopy[i] = actions[i];
                if (actionsCopy[i] == null)
                {
                    throw new ArgumentException(SR.Parallel_Invoke_ActionNull);
                }
            }

            // ETW event for Parallel Invoke Begin
            int forkJoinContextID = 0;
            if (ParallelEtwProvider.Log.IsEnabled())
            {
                forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
                ParallelEtwProvider.Log.ParallelInvokeBegin(TaskScheduler.Current.Id, Task.CurrentId ?? 0,
                                                            forkJoinContextID, ParallelEtwProvider.ForkJoinOperationType.ParallelInvoke,
                                                            actionsCopy.Length);
            }

#if DEBUG
            actions = null!; // Ensure we don't accidentally use this below.
#endif

            // If we have no work to do, we are done.
            if (actionsCopy.Length < 1) return;

            // In the algorithm below, if the number of actions is greater than this, we automatically
            // use Parallel.For() to handle the actions, rather than the Task-per-Action strategy.
            const int SMALL_ACTIONCOUNT_LIMIT = 10;

            try
            {
                // If we've gotten this far, it's time to process the actions.

#if !FEATURE_WASM_MANAGED_THREADS
                // Web browsers need special treatment that is implemented in TaskReplicator
                if (OperatingSystem.IsBrowser() ||
#else
                if (
#endif
                    // This is more efficient for a large number of actions, or for enforcing MaxDegreeOfParallelism:
                    (actionsCopy.Length > SMALL_ACTIONCOUNT_LIMIT) ||
                    (parallelOptions.MaxDegreeOfParallelism != -1 && parallelOptions.MaxDegreeOfParallelism < actionsCopy.Length)
                )
                {
                    // Used to hold any exceptions encountered during action processing
                    ConcurrentQueue<Exception>? exceptionQ = null; // will be lazily initialized if necessary

                    // Launch a task replicator to handle the execution of all actions.
                    // This allows us to use as many cores as are available, and no more.
                    // The exception to this rule is that, in the case of a blocked action,
                    // the ThreadPool may inject extra threads, which means extra tasks can run.
                    int actionIndex = 0;

                    try
                    {
                        TaskReplicator.Run(
                            (ref object state, long timeout, out bool replicationDelegateYieldedBeforeCompletion) =>
                            {
                                // In this particular case, we do not participate in cooperative multitasking:
                                replicationDelegateYieldedBeforeCompletion = false;

                                // Each for-task will pull an action at a time from the list
                                int myIndex = Interlocked.Increment(ref actionIndex); // = index to use + 1
                                while (myIndex <= actionsCopy.Length)
                                {
                                    // Catch and store any exceptions.  If we don't catch them, the self-replicating
                                    // task will exit, and that may cause other SR-tasks to exit.
                                    // And (absent cancellation) we want all actions to execute.
                                    try
                                    {
                                        actionsCopy[myIndex - 1]();
                                    }
                                    catch (Exception e)
                                    {
                                        LazyInitializer.EnsureInitialized<ConcurrentQueue<Exception>>(ref exceptionQ, () => { return new ConcurrentQueue<Exception>(); });
                                        exceptionQ.Enqueue(e);
                                    }

                                    // Check for cancellation.  If it is encountered, then exit the delegate.
                                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                                    // You're still in the game.  Grab your next action index.
                                    myIndex = Interlocked.Increment(ref actionIndex);
                                }
                            },
                            parallelOptions,
                            stopOnFirstFailure: false);
                    }
                    catch (Exception e)
                    {
                        LazyInitializer.EnsureInitialized<ConcurrentQueue<Exception>>(ref exceptionQ, () => { return new ConcurrentQueue<Exception>(); });

                        // Since we're consuming all action exceptions, there are very few reasons that
                        // we would see an exception here.  Two that come to mind:
                        //   (1) An OCE thrown by one or more actions (AggregateException thrown)
                        //   (2) An exception thrown from the TaskReplicator constructor
                        //       (regular exception thrown).
                        // We'll need to cover them both.

                        if (e is ObjectDisposedException)
                            throw;

                        if (e is AggregateException ae)
                        {
                            // Strip off outer container of an AggregateException, because downstream
                            // logic needs OCEs to be at the top level.
                            foreach (Exception exc in ae.InnerExceptions) exceptionQ.Enqueue(exc);
                        }
                        else
                        {
                            exceptionQ.Enqueue(e);
                        }
                    }

                    // If we have encountered any exceptions, then throw.
                    if ((exceptionQ != null) && (!exceptionQ.IsEmpty))
                    {
                        ThrowSingleCancellationExceptionOrOtherException(exceptionQ, parallelOptions.CancellationToken,
                                                                         new AggregateException(exceptionQ));
                    }
                }
                else  // This is more efficient for a small number of actions and no DOP support:
                {
                    // Initialize our array of tasks, one per action.
                    Task[] tasks = new Task[actionsCopy.Length];

                    // One more check before we begin...
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    // Invoke all actions as tasks.  Queue N-1 of them, and run 1 synchronously.
                    for (int i = 1; i < tasks.Length; i++)
                    {
                        tasks[i] = Task.Factory.StartNew(actionsCopy[i], parallelOptions.CancellationToken, TaskCreationOptions.None,
                                                         parallelOptions.EffectiveTaskScheduler);
                    }
                    tasks[0] = new Task(actionsCopy[0], parallelOptions.CancellationToken, TaskCreationOptions.None);
                    tasks[0].RunSynchronously(parallelOptions.EffectiveTaskScheduler);

                    // Now wait for the tasks to complete.  This will not unblock until all of
                    // them complete, and it will throw an exception if one or more of them also
                    // threw an exception.  We let such exceptions go completely unhandled.
                    try
                    {
#pragma warning disable CA1416 // Validate platform compatibility, issue: https://github.com/dotnet/runtime/issues/44605
                        Task.WaitAll(tasks);
#pragma warning restore CA1416
                    }
                    catch (AggregateException aggExp)
                    {
                        // see if we can combine it into a single OCE. If not propagate the original exception
                        ThrowSingleCancellationExceptionOrOtherException(aggExp.InnerExceptions, parallelOptions.CancellationToken, aggExp);
                    }
                }
            }
            finally
            {
                // ETW event for Parallel Invoke End
                if (ParallelEtwProvider.Log.IsEnabled())
                {
                    ParallelEtwProvider.Log.ParallelInvokeEnd(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID);
                }
            }
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the iteration count (an Int32) as a parameter.
        /// </remarks>
        public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int> body)
        {
            ArgumentNullException.ThrowIfNull(body);

            return ForWorker<object, int>(
                fromInclusive, toExclusive,
                s_defaultParallelOptions,
                body, null, null, null, null);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the iteration count (an Int64) as a parameter.
        /// </remarks>
        public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long> body)
        {
            ArgumentNullException.ThrowIfNull(body);

            return ForWorker<object, long>(
                fromInclusive, toExclusive, s_defaultParallelOptions,
                body, null, null, null, null);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the iteration count (an Int32) as a parameter.
        /// </remarks>
        public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int> body)
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return ForWorker<object, int>(
                fromInclusive, toExclusive, parallelOptions,
                body, null, null, null, null);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the iteration count (an Int64) as a parameter.
        /// </remarks>
        public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long> body)
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return ForWorker<object, long>(
                fromInclusive, toExclusive, parallelOptions,
                body, null, null, null, null);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the following parameters: the iteration count (an Int32),
        /// and a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely.
        /// </para>
        /// <para>
        /// Calling <see cref="System.Threading.Tasks.ParallelLoopState.Break()">ParallelLoopState.Break()</see>
        /// informs the For operation that iterations after the current one need not
        /// execute.  However, all iterations before the current one will still need to be executed if they have not already.
        /// Therefore, calling Break is similar to using a break operation within a
        /// conventional for loop in a language like C#, but it is not a perfect substitute: for example, there is no guarantee that iterations
        /// after the current one will definitely not execute.
        /// </para>
        /// <para>
        /// If executing all iterations before the current one is not necessary,
        /// <see cref="System.Threading.Tasks.ParallelLoopState.Stop()">ParallelLoopState.Stop()</see>
        /// should be preferred to using Break.  Calling Stop informs the For loop that it may abandon all remaining
        /// iterations, regardless of whether they're for iterations above or below the current,
        /// since all required work has already been completed.  As with Break, however, there are no guarantees regarding
        /// which other iterations will not execute.
        /// </para>
        /// <para>
        /// When a loop is ended prematurely, the <see cref="ParallelLoopState"/> that's returned will contain
        /// relevant information about the loop's completion.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int, ParallelLoopState> body)
        {
            ArgumentNullException.ThrowIfNull(body);

            return ForWorker<object, int>(
                fromInclusive, toExclusive, s_defaultParallelOptions,
                null, body, null, null, null);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the following parameters: the iteration count (an Int64),
        /// and a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely.
        /// </remarks>
        public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long, ParallelLoopState> body)
        {
            ArgumentNullException.ThrowIfNull(body);

            return ForWorker<object, long>(
                fromInclusive, toExclusive, s_defaultParallelOptions,
                null, body, null, null, null);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the following parameters: the iteration count (an Int32),
        /// and a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely.
        /// </remarks>
        public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int, ParallelLoopState> body)
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return ForWorker<object, int>(
                fromInclusive, toExclusive, parallelOptions,
                null, body, null, null, null);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the following parameters: the iteration count (an Int64),
        /// and a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely.
        /// </remarks>
        public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions,
            Action<long, ParallelLoopState> body)
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return ForWorker<object, long>(
                fromInclusive, toExclusive, parallelOptions,
                null, body, null, null, null);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the following parameters: the iteration count (an Int32),
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and some local state that may be shared amongst iterations
        /// that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult For<TLocal>(
            int fromInclusive, int toExclusive,
            Func<TLocal> localInit,
            Func<int, ParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return ForWorker(
                fromInclusive, toExclusive, s_defaultParallelOptions,
                null, null, body, localInit, localFinally);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.  Supports 64-bit indices.
        /// </summary>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the following parameters: the iteration count (an Int64),
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and some local state that may be shared amongst iterations
        /// that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult For<TLocal>(
            long fromInclusive, long toExclusive,
            Func<TLocal> localInit,
            Func<long, ParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return ForWorker(
                fromInclusive, toExclusive, s_defaultParallelOptions,
                null, null, body, localInit, localFinally);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the following parameters: the iteration count (an Int32),
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and some local state that may be shared amongst iterations
        /// that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult For<TLocal>(
            int fromInclusive, int toExclusive, ParallelOptions parallelOptions,
            Func<TLocal> localInit,
            Func<int, ParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return ForWorker(
                fromInclusive, toExclusive, parallelOptions,
                null, null, body, localInit, localFinally);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each value in the iteration range:
        /// [fromInclusive, toExclusive).  It is provided with the following parameters: the iteration count (an Int64),
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and some local state that may be shared amongst iterations
        /// that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult For<TLocal>(
            long fromInclusive, long toExclusive, ParallelOptions parallelOptions,
            Func<TLocal> localInit,
            Func<long, ParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return ForWorker(
                fromInclusive, toExclusive, parallelOptions,
                null, null, body, localInit, localFinally);
        }

        private static bool CheckTimeoutReached(long timeoutOccursAt) =>
            timeoutOccursAt - Environment.TickCount64 <= 0;

        private static long ComputeTimeoutPoint(long timeoutLength) =>
            Environment.TickCount64 + timeoutLength;

        /// <summary>
        /// Performs the major work of the 64-bit parallel for loop. It assumes that argument validation has already
        /// been performed by the caller. This function's whole purpose in life is to enable as much reuse of
        /// common implementation details for the various For overloads we offer. Without it, we'd end up
        /// with lots of duplicate code. It handles: (1) simple for loops, (2) for loops that depend on
        /// ParallelState, and (3) for loops with thread local data.
        ///
        /// </summary>
        /// <typeparam name="TLocal">The type of the local data.</typeparam>
        /// <typeparam name="TInt">The type of the range integer.</typeparam>
        /// <param name="fromInclusive">The loop's start index, inclusive.</param>
        /// <param name="toExclusive">The loop's end index, exclusive.</param>
        /// <param name="parallelOptions">A ParallelOptions instance.</param>
        /// <param name="body">The simple loop body.</param>
        /// <param name="bodyWithState">The loop body for ParallelState overloads.</param>
        /// <param name="bodyWithLocal">The loop body for thread local state overloads.</param>
        /// <param name="localInit">A selector function that returns new thread local state.</param>
        /// <param name="localFinally">A cleanup function to destroy thread local state.</param>
        /// <remarks>Only one of the body arguments may be supplied (i.e. they are exclusive).</remarks>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult"/> structure.</returns>
        private static ParallelLoopResult ForWorker<TLocal, TInt>(
            TInt fromInclusive, TInt toExclusive,
            ParallelOptions parallelOptions,
            Action<TInt>? body,
            Action<TInt, ParallelLoopState>? bodyWithState,
            Func<TInt, ParallelLoopState, TLocal, TLocal>? bodyWithLocal,
            Func<TLocal>? localInit, Action<TLocal>? localFinally)
            where TInt : struct, IBinaryInteger<TInt>, IMinMaxValue<TInt>
        {
            Debug.Assert(typeof(TInt) == typeof(int) || typeof(TInt) == typeof(long));
            Debug.Assert(((body == null ? 0 : 1) + (bodyWithState == null ? 0 : 1) + (bodyWithLocal == null ? 0 : 1)) == 1,
                "expected exactly one body function to be supplied");
            Debug.Assert(bodyWithLocal != null || (localInit == null && localFinally == null),
                "thread local functions should only be supplied for loops w/ thread local bodies");

            // Instantiate our result.  Specifics will be filled in later.
            ParallelLoopResult result = default;

            // We just return immediately if 'to' is smaller (or equal to) 'from'.
            if (toExclusive <= fromInclusive)
            {
                result._completed = true;
                return result;
            }

            // For all loops we need a shared flag even though we don't have a body with state,
            // because the shared flag contains the exceptional bool, which triggers other workers
            // to exit their loops if one worker catches an exception
            ParallelLoopStateFlags<TInt> sharedPStateFlags = new ParallelLoopStateFlags<TInt>();

            // Before getting started, do a quick peek to see if we have been canceled already
            parallelOptions.CancellationToken.ThrowIfCancellationRequested();


            // initialize ranges with passed in loop arguments and expected number of workers
            int numExpectedWorkers = (parallelOptions.EffectiveMaxConcurrencyLevel == -1) ?
                Environment.ProcessorCount :
                parallelOptions.EffectiveMaxConcurrencyLevel;
            RangeManager rangeManager = new RangeManager(long.CreateTruncating(fromInclusive), long.CreateTruncating(toExclusive), 1, numExpectedWorkers);

            // Keep track of any cancellations
            OperationCanceledException? oce = null;

            // if cancellation is enabled, we need to register a callback to stop the loop when it gets signaled
            CancellationTokenRegistration ctr = (!parallelOptions.CancellationToken.CanBeCanceled)
                            ? default(CancellationTokenRegistration)
                            : parallelOptions.CancellationToken.UnsafeRegister((o) =>
                            {
                                // Record our cancellation before stopping processing
                                oce = new OperationCanceledException(parallelOptions.CancellationToken);
                                // Cause processing to stop
                                sharedPStateFlags.Cancel();
                            }, state: null);

            // ETW event for Parallel For begin
            int forkJoinContextID = 0;
            if (ParallelEtwProvider.Log.IsEnabled())
            {
                forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
                ParallelEtwProvider.Log.ParallelLoopBegin(TaskScheduler.Current.Id, Task.CurrentId ?? 0,
                                                          forkJoinContextID, ParallelEtwProvider.ForkJoinOperationType.ParallelFor,
                                                          long.CreateTruncating(fromInclusive), long.CreateTruncating(toExclusive));
            }

            try
            {
                try
                {
                    TaskReplicator.Run(
                        (ref RangeWorker currentWorker, long timeout, out bool replicationDelegateYieldedBeforeCompletion) =>
                        {
                            // First thing we do upon entering the task is to register as a new "RangeWorker" with the
                            // shared RangeManager instance.

                            if (!currentWorker.IsInitialized)
                                currentWorker = rangeManager.RegisterNewWorker();

                            // We will need to reset this to true if we exit due to a timeout:
                            replicationDelegateYieldedBeforeCompletion = false;


                            // These are the local index values to be used in the sequential loop.
                            // Their values filled in by FindNewWork
                            TInt nFromInclusiveLocal;
                            TInt nToExclusiveLocal;

                            if (currentWorker.FindNewWork(out nFromInclusiveLocal, out nToExclusiveLocal) == false ||
                                sharedPStateFlags.ShouldExitLoop(nFromInclusiveLocal))
                            {
                                return; // no need to run
                            }


                            // ETW event for ParallelFor Worker Fork
                            if (ParallelEtwProvider.Log.IsEnabled())
                            {
                                ParallelEtwProvider.Log.ParallelFork(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID);
                            }

                            TLocal localValue = default!;
                            bool bLocalValueInitialized = false; // Tracks whether localInit ran without exceptions, so that we can skip localFinally if it wasn't

                            try
                            {
                                // Create a new state object that references the shared "stopped" and "exceptional" flags
                                // If needed, it will contain a new instance of thread-local state by invoking the selector.
                                ParallelLoopState<TInt>? state = null;

                                if (bodyWithState != null)
                                {
                                    Debug.Assert(sharedPStateFlags != null);
                                    state = new ParallelLoopState<TInt>(sharedPStateFlags);
                                }
                                else if (bodyWithLocal != null)
                                {
                                    Debug.Assert(sharedPStateFlags != null);
                                    state = new ParallelLoopState<TInt>(sharedPStateFlags);

                                    // If a thread-local selector was supplied, invoke it. Otherwise, use the default.
                                    if (localInit != null)
                                    {
                                        localValue = localInit();
                                        bLocalValueInitialized = true;
                                    }
                                }

                                // initialize a loop timer which will help us decide whether we should exit early
                                long loopTimeout = ComputeTimeoutPoint(timeout);

                                // Now perform the loop itself.
                                do
                                {
                                    if (body != null)
                                    {
                                        for (TInt j = nFromInclusiveLocal;
                                             j < nToExclusiveLocal && (sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.ParallelLoopStateNone  // fast path check as SEL() doesn't inline
                                                                       || !sharedPStateFlags.ShouldExitLoop()); // the no-arg version is used since we have no state
                                             j++)
                                        {
                                            body(j);
                                        }
                                    }
                                    else if (bodyWithState != null)
                                    {
                                        for (TInt j = nFromInclusiveLocal;
                                             j < nToExclusiveLocal && (sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.ParallelLoopStateNone  // fast path check as SEL() doesn't inline
                                                                       || !sharedPStateFlags.ShouldExitLoop(j));
                                             j++)
                                        {
                                            state!.CurrentIteration = j;
                                            bodyWithState(j, state);
                                        }
                                    }
                                    else
                                    {
                                        for (TInt j = nFromInclusiveLocal;
                                             j < nToExclusiveLocal && (sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.ParallelLoopStateNone  // fast path check as SEL() doesn't inline
                                                                       || !sharedPStateFlags.ShouldExitLoop(j));
                                             j++)
                                        {
                                            state!.CurrentIteration = j;
                                            localValue = bodyWithLocal!(j, state, localValue);
                                        }
                                    }

                                    // Cooperative multitasking:
                                    // Check if allowed loop time is exceeded, if so save current state and return.
                                    // The task replicator will queue up a replacement task. Note that we don't do this on the root task.
                                    if (CheckTimeoutReached(loopTimeout))
                                    {
                                        replicationDelegateYieldedBeforeCompletion = true;
                                        break;
                                    }
                                    // Exit DO-loop if we can't find new work, or if the loop was stopped:
                                } while (currentWorker.FindNewWork(out nFromInclusiveLocal, out nToExclusiveLocal) &&
                                          ((sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.ParallelLoopStateNone) ||
                                            !sharedPStateFlags.ShouldExitLoop(nFromInclusiveLocal)));
                            }
                            catch (Exception ex)
                            {
                                // if we catch an exception in a worker, we signal the other workers to exit the loop, and we rethrow
                                sharedPStateFlags.SetExceptional();
                                ExceptionDispatchInfo.Throw(ex);
                            }
                            finally
                            {
                                // If a cleanup function was specified, call it. Otherwise, if the type is
                                // IDisposable, we will invoke Dispose on behalf of the user.
                                if (localFinally != null && bLocalValueInitialized)
                                {
                                    localFinally(localValue);
                                }

                                // ETW event for ParallelFor Worker Join
                                if (ParallelEtwProvider.Log.IsEnabled())
                                {
                                    ParallelEtwProvider.Log.ParallelJoin(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID);
                                }
                            }
                        },
                        parallelOptions,
                        stopOnFirstFailure: true);
                }
                finally
                {
                    // Dispose the cancellation token registration before checking for a cancellation exception
                    if (parallelOptions.CancellationToken.CanBeCanceled)
                        ctr.Dispose();
                }

                // If we got through that with no exceptions, and we were canceled, then
                // throw our cancellation exception
                if (oce != null) throw oce;
            }
            catch (AggregateException aggExp)
            {
                // If we have many cancellation exceptions all caused by the specified user cancel control, then throw only one OCE:
                ThrowSingleCancellationExceptionOrOtherException(aggExp.InnerExceptions, parallelOptions.CancellationToken, aggExp);
            }
            finally
            {
                int sb_status = sharedPStateFlags.LoopStateFlags;
                result._completed = (sb_status == ParallelLoopStateFlags.ParallelLoopStateNone);
                if ((sb_status & ParallelLoopStateFlags.ParallelLoopStateBroken) != 0)
                {
                    result._lowestBreakIteration = long.CreateTruncating(sharedPStateFlags.LowestBreakIteration);
                }

                // ETW event for Parallel For End
                if (ParallelEtwProvider.Log.IsEnabled())
                {
                    TInt nTotalIterations;

                    // calculate how many iterations we ran in total
                    if (sb_status == ParallelLoopStateFlags.ParallelLoopStateNone)
                        nTotalIterations = toExclusive - fromInclusive;
                    else if ((sb_status & ParallelLoopStateFlags.ParallelLoopStateBroken) != 0)
                        nTotalIterations = sharedPStateFlags.LowestBreakIteration - fromInclusive;
                    else
                        nTotalIterations = TInt.CreateTruncating(-1); //ParallelLoopStateStopped! We can't determine this if we were stopped..

                    ParallelEtwProvider.Log.ParallelLoopEnd(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID, long.CreateTruncating(nTotalIterations));
                }
            }

            return result;
        }


        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the current element as a parameter.
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);

            return ForEachWorker<TSource, object>(
                source, s_defaultParallelOptions, body, null, null, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the current element as a parameter.
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return ForEachWorker<TSource, object>(
                source, parallelOptions, body, null, null, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the following parameters: the current element,
        /// and a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely.
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);

            return ForEachWorker<TSource, object>(
                source, s_defaultParallelOptions, null, body, null, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the following parameters: the current element,
        /// and a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely.
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return ForEachWorker<TSource, object>(
                source, parallelOptions, null, body, null, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and the current element's index (an Int64).
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState, long> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);

            return ForEachWorker<TSource, object>(
                source, s_defaultParallelOptions, null, null, body, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and the current element's index (an Int64).
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState, long> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return ForEachWorker<TSource, object>(
                source, parallelOptions, null, null, body, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and some local state that may be shared amongst iterations
        /// that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return ForEachWorker<TSource, TLocal>(
                source, s_defaultParallelOptions, null, null, null, body, null, localInit, localFinally);
        }

        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and some local state that may be shared amongst iterations
        /// that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source,
            ParallelOptions parallelOptions, Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return ForEachWorker<TSource, TLocal>(
                source, parallelOptions, null, null, null, body, null, localInit, localFinally);
        }

        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, the current element's index (an Int64), and some local
        /// state that may be shared amongst iterations that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return ForEachWorker<TSource, TLocal>(
                source, s_defaultParallelOptions, null, null, null, null, body, localInit, localFinally);
        }

        /// <summary>
        /// Executes a for each operation on an <see cref="System.Collections.Generic.IEnumerable{TSource}"/>
        /// in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// enumerable.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, the current element's index (an Int64), and some local
        /// state that may be shared amongst iterations that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return ForEachWorker<TSource, TLocal>(
                source, parallelOptions, null, null, null, null, body, localInit, localFinally);
        }


        /// <summary>
        /// Performs the major work of the parallel foreach loop. It assumes that argument validation has
        /// already been performed by the caller. This function's whole purpose in life is to enable as much
        /// reuse of common implementation details for the various For overloads we offer. Without it, we'd
        /// end up with lots of duplicate code. It handles: (1) simple foreach loops, (2) foreach loops that
        /// depend on ParallelState, and (3) foreach loops that access indices, (4) foreach loops with thread
        /// local data, and any necessary permutations thereof.
        ///
        /// </summary>
        /// <typeparam name="TSource">The type of the source data.</typeparam>
        /// <typeparam name="TLocal">The type of the local data.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="parallelOptions">ParallelOptions instance to use with this ForEach-loop</param>
        /// <param name="body">The simple loop body.</param>
        /// <param name="bodyWithState">The loop body for ParallelState overloads.</param>
        /// <param name="bodyWithStateAndIndex">The loop body for ParallelState/indexed overloads.</param>
        /// <param name="bodyWithStateAndLocal">The loop body for ParallelState/thread local state overloads.</param>
        /// <param name="bodyWithEverything">The loop body for ParallelState/indexed/thread local state overloads.</param>
        /// <param name="localInit">A selector function that returns new thread local state.</param>
        /// <param name="localFinally">A cleanup function to destroy thread local state.</param>
        /// <remarks>Only one of the bodyXX arguments may be supplied (i.e. they are exclusive).</remarks>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult"/> structure.</returns>
        private static ParallelLoopResult ForEachWorker<TSource, TLocal>(
            IEnumerable<TSource> source,
            ParallelOptions parallelOptions,
            Action<TSource>? body,
            Action<TSource, ParallelLoopState>? bodyWithState,
            Action<TSource, ParallelLoopState, long>? bodyWithStateAndIndex,
            Func<TSource, ParallelLoopState, TLocal, TLocal>? bodyWithStateAndLocal,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal>? bodyWithEverything,
            Func<TLocal>? localInit, Action<TLocal>? localFinally)
        {
            Debug.Assert(((body == null ? 0 : 1) + (bodyWithState == null ? 0 : 1) +
                (bodyWithStateAndIndex == null ? 0 : 1) + (bodyWithStateAndLocal == null ? 0 : 1) + (bodyWithEverything == null ? 0 : 1)) == 1,
                "expected exactly one body function to be supplied");
            Debug.Assert((bodyWithStateAndLocal != null) || (bodyWithEverything != null) || (localInit == null && localFinally == null),
                "thread local functions should only be supplied for loops w/ thread local bodies");

            // Before getting started, do a quick peek to see if we have been canceled already
            parallelOptions.CancellationToken.ThrowIfCancellationRequested();

            // If it's an array, we can use a fast-path that uses ldelems in the IL.
            if (source is TSource[] sourceAsArray)
            {
                return ForEachWorker<TSource, TLocal>(
                    sourceAsArray, parallelOptions, body, bodyWithState, bodyWithStateAndIndex, bodyWithStateAndLocal,
                    bodyWithEverything, localInit, localFinally);
            }

            // If we can index into the list, we can use a faster code-path that doesn't result in
            // contention for the single, shared enumerator object.
            if (source is IList<TSource> sourceAsList)
            {
                return ForEachWorker<TSource, TLocal>(
                    sourceAsList, parallelOptions, body, bodyWithState, bodyWithStateAndIndex, bodyWithStateAndLocal,
                    bodyWithEverything, localInit, localFinally);
            }

            // This is an honest-to-goodness IEnumerable.  Wrap it in a Partitioner and defer to our
            // ForEach(Partitioner) logic.
            return PartitionerForEachWorker<TSource, TLocal>(Partitioner.Create(source), parallelOptions, body, bodyWithState,
                bodyWithStateAndIndex, bodyWithStateAndLocal, bodyWithEverything, localInit, localFinally);
        }

        /// <summary>
        /// A fast path for the more general ForEachWorker method above. This uses ldelem instructions to
        /// access the individual elements of the array, which will be faster.
        /// </summary>
        /// <typeparam name="TSource">The type of the source data.</typeparam>
        /// <typeparam name="TLocal">The type of the local data.</typeparam>
        /// <param name="array">An array data source.</param>
        /// <param name="parallelOptions">The options to use for execution.</param>
        /// <param name="body">The simple loop body.</param>
        /// <param name="bodyWithState">The loop body for ParallelState overloads.</param>
        /// <param name="bodyWithStateAndIndex">The loop body for indexed/ParallelLoopState overloads.</param>
        /// <param name="bodyWithStateAndLocal">The loop body for local/ParallelLoopState overloads.</param>
        /// <param name="bodyWithEverything">The loop body for the most generic overload.</param>
        /// <param name="localInit">A selector function that returns new thread local state.</param>
        /// <param name="localFinally">A cleanup function to destroy thread local state.</param>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult"/> structure.</returns>
        private static ParallelLoopResult ForEachWorker<TSource, TLocal>(
            TSource[] array,
            ParallelOptions parallelOptions,
            Action<TSource>? body,
            Action<TSource, ParallelLoopState>? bodyWithState,
            Action<TSource, ParallelLoopState, long>? bodyWithStateAndIndex,
            Func<TSource, ParallelLoopState, TLocal, TLocal>? bodyWithStateAndLocal,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal>? bodyWithEverything,
            Func<TLocal>? localInit, Action<TLocal>? localFinally)
        {
            Debug.Assert(array != null);
            Debug.Assert(parallelOptions != null, "ForEachWorker(array): parallelOptions is null");

            int from = array.GetLowerBound(0);
            int to = array.GetUpperBound(0) + 1;

            if (body != null)
            {
                return ForWorker<object, int>(
                    from, to, parallelOptions, (i) => body(array[i]), null, null, null, null);
            }
            else if (bodyWithState != null)
            {
                return ForWorker<object, int>(
                    from, to, parallelOptions, null, (i, state) => bodyWithState(array[i], state), null, null, null);
            }
            else if (bodyWithStateAndIndex != null)
            {
                return ForWorker<object, int>(
                    from, to, parallelOptions, null, (i, state) => bodyWithStateAndIndex(array[i], state, i), null, null, null);
            }
            else if (bodyWithStateAndLocal != null)
            {
                return ForWorker(
                    from, to, parallelOptions, null, null, (i, state, local) => bodyWithStateAndLocal(array[i], state, local), localInit, localFinally);
            }
            else
            {
                return ForWorker(
                    from, to, parallelOptions, null, null, (i, state, local) => bodyWithEverything!(array[i], state, i, local), localInit, localFinally);
            }
        }

        /// <summary>
        /// A fast path for the more general ForEachWorker method above. This uses IList&lt;T&gt;'s indexer
        /// capabilities to access the individual elements of the list rather than an enumerator.
        /// </summary>
        /// <typeparam name="TSource">The type of the source data.</typeparam>
        /// <typeparam name="TLocal">The type of the local data.</typeparam>
        /// <param name="list">A list data source.</param>
        /// <param name="parallelOptions">The options to use for execution.</param>
        /// <param name="body">The simple loop body.</param>
        /// <param name="bodyWithState">The loop body for ParallelState overloads.</param>
        /// <param name="bodyWithStateAndIndex">The loop body for indexed/ParallelLoopState overloads.</param>
        /// <param name="bodyWithStateAndLocal">The loop body for local/ParallelLoopState overloads.</param>
        /// <param name="bodyWithEverything">The loop body for the most generic overload.</param>
        /// <param name="localInit">A selector function that returns new thread local state.</param>
        /// <param name="localFinally">A cleanup function to destroy thread local state.</param>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult"/> structure.</returns>
        private static ParallelLoopResult ForEachWorker<TSource, TLocal>(
            IList<TSource> list,
            ParallelOptions parallelOptions,
            Action<TSource>? body,
            Action<TSource, ParallelLoopState>? bodyWithState,
            Action<TSource, ParallelLoopState, long>? bodyWithStateAndIndex,
            Func<TSource, ParallelLoopState, TLocal, TLocal>? bodyWithStateAndLocal,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal>? bodyWithEverything,
            Func<TLocal>? localInit, Action<TLocal>? localFinally)
        {
            Debug.Assert(list != null);
            Debug.Assert(parallelOptions != null, "ForEachWorker(list): parallelOptions is null");

            if (body != null)
            {
                return ForWorker<object, int>(
                    0, list.Count, parallelOptions, (i) => body(list[i]), null, null, null, null);
            }
            else if (bodyWithState != null)
            {
                return ForWorker<object, int>(
                    0, list.Count, parallelOptions, null, (i, state) => bodyWithState(list[i], state), null, null, null);
            }
            else if (bodyWithStateAndIndex != null)
            {
                return ForWorker<object, int>(
                    0, list.Count, parallelOptions, null, (i, state) => bodyWithStateAndIndex(list[i], state, i), null, null, null);
            }
            else if (bodyWithStateAndLocal != null)
            {
                return ForWorker(
                    0, list.Count, parallelOptions, null, null, (i, state, local) => bodyWithStateAndLocal(list[i], state, local), localInit, localFinally);
            }
            else
            {
                return ForWorker(
                    0, list.Count, parallelOptions, null, null, (i, state, local) => bodyWithEverything!(list[i], state, i, local), localInit, localFinally);
            }
        }



        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.Partitioner{TSource}">
        /// Partitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <param name="source">The Partitioner that contains the original data source.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> Partitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> Partitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner does not return
        /// the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner returns an IList
        /// with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() method in the <paramref name="source"/> Partitioner returns an
        /// IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the current element as a parameter.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(
            Partitioner<TSource> source,
            Action<TSource> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);

            return PartitionerForEachWorker<TSource, object>(source, s_defaultParallelOptions, body, null, null, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.Partitioner{TSource}">
        /// Partitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <param name="source">The Partitioner that contains the original data source.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> Partitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> Partitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner does not return
        /// the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner returns an IList
        /// with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() method in the <paramref name="source"/> Partitioner returns an
        /// IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the following parameters: the current element,
        /// and a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(
            Partitioner<TSource> source,
            Action<TSource, ParallelLoopState> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);

            return PartitionerForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, body, null, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <param name="source">The OrderablePartitioner that contains the original data source.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> OrderablePartitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// KeysNormalized property in the <paramref name="source"/> OrderablePartitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> OrderablePartitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() or GetOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner do not return the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() or GetOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner return an IList with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() or GetDynamicOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner return an IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and the current element's index (an Int64).
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(
            OrderablePartitioner<TSource> source,
            Action<TSource, ParallelLoopState, long> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);

            if (!source.KeysNormalized)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_OrderedPartitionerKeysNotNormalized);
            }

            return PartitionerForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, null, body, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.Partitioner{TSource}">
        /// Partitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="source">The Partitioner that contains the original data source.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> Partitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> Partitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner does not return
        /// the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner returns an IList
        /// with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() method in the <paramref name="source"/> Partitioner returns an
        /// IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and some local state that may be shared amongst iterations
        /// that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource, TLocal>(
            Partitioner<TSource> source,
            Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return PartitionerForEachWorker<TSource, TLocal>(source, s_defaultParallelOptions, null, null, null, body, null, localInit, localFinally);
        }

        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="source">The OrderablePartitioner that contains the original data source.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> OrderablePartitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// KeysNormalized property in the <paramref name="source"/> OrderablePartitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> OrderablePartitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() or GetOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner do not return the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() or GetOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner return an IList with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() or GetDynamicOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner return an IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, the current element's index (an Int64), and some local
        /// state that may be shared amongst iterations that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource, TLocal>(
            OrderablePartitioner<TSource> source,
            Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            if (!source.KeysNormalized)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_OrderedPartitionerKeysNotNormalized);
            }

            return PartitionerForEachWorker<TSource, TLocal>(source, s_defaultParallelOptions, null, null, null, null, body, localInit, localFinally);
        }

        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.Partitioner{TSource}">
        /// Partitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <param name="source">The Partitioner that contains the original data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> Partitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> Partitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner does not return
        /// the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner returns an IList
        /// with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() method in the <paramref name="source"/> Partitioner returns an
        /// IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the current element as a parameter.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(
            Partitioner<TSource> source,
            ParallelOptions parallelOptions,
            Action<TSource> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return PartitionerForEachWorker<TSource, object>(source, parallelOptions, body, null, null, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.Partitioner{TSource}">
        /// Partitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <param name="source">The Partitioner that contains the original data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> Partitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> Partitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner does not return
        /// the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner returns an IList
        /// with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() method in the <paramref name="source"/> Partitioner returns an
        /// IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the following parameters: the current element,
        /// and a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(
            Partitioner<TSource> source,
            ParallelOptions parallelOptions,
            Action<TSource, ParallelLoopState> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return PartitionerForEachWorker<TSource, object>(source, parallelOptions, null, body, null, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <param name="source">The OrderablePartitioner that contains the original data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> OrderablePartitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// KeysNormalized property in the <paramref name="source"/> OrderablePartitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> OrderablePartitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() or GetOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner do not return the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() or GetOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner return an IList with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() or GetDynamicOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner return an IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and the current element's index (an Int64).
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource>(
            OrderablePartitioner<TSource> source,
            ParallelOptions parallelOptions,
            Action<TSource, ParallelLoopState, long> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            if (!source.KeysNormalized)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_OrderedPartitionerKeysNotNormalized);
            }

            return PartitionerForEachWorker<TSource, object>(source, parallelOptions, null, null, body, null, null, null, null);
        }

        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.Partitioner{TSource}">
        /// Partitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="source">The Partitioner that contains the original data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> Partitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> Partitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner does not return
        /// the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() method in the <paramref name="source"/> Partitioner returns an IList
        /// with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() method in the <paramref name="source"/> Partitioner returns an
        /// IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, and some local state that may be shared amongst iterations
        /// that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource, TLocal>(
            Partitioner<TSource> source,
            ParallelOptions parallelOptions,
            Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            return PartitionerForEachWorker<TSource, TLocal>(source, parallelOptions, null, null, null, body, null, localInit, localFinally);
        }

        /// <summary>
        /// Executes a for each operation on a <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see> in which iterations may run in parallel.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <typeparam name="TLocal">The type of the thread-local data.</typeparam>
        /// <param name="source">The OrderablePartitioner that contains the original data source.</param>
        /// <param name="parallelOptions">A <see cref="System.Threading.Tasks.ParallelOptions">ParallelOptions</see>
        /// instance that configures the behavior of this operation.</param>
        /// <param name="localInit">The function delegate that returns the initial state of the local data
        /// for each thread.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <param name="localFinally">The delegate that performs a final action on the local state of each
        /// thread.</param>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="source"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="parallelOptions"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the <paramref name="body"/>
        /// argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localInit"/> argument is null.</exception>
        /// <exception cref="System.ArgumentNullException">The exception that is thrown when the
        /// <paramref name="localFinally"/> argument is null.</exception>
        /// <exception cref="System.OperationCanceledException">The exception that is thrown when the
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> in the <paramref name="parallelOptions"/>
        /// argument is set</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// SupportsDynamicPartitions property in the <paramref name="source"/> OrderablePartitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// KeysNormalized property in the <paramref name="source"/> OrderablePartitioner returns
        /// false.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when any
        /// methods in the <paramref name="source"/> OrderablePartitioner return null.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() or GetOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner do not return the correct number of partitions.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetPartitions() or GetOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner return an IList with at least one null value.</exception>
        /// <exception cref="System.InvalidOperationException">The exception that is thrown when the
        /// GetDynamicPartitions() or GetDynamicOrderablePartitions() methods in the <paramref name="source"/>
        /// OrderablePartitioner return an IEnumerable whose GetEnumerator() method returns null.</exception>
        /// <exception cref="System.AggregateException">The exception that is thrown to contain an exception
        /// thrown from one of the specified delegates.</exception>
        /// <exception cref="System.ObjectDisposedException">The exception that is thrown when the
        /// the <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> associated with the
        /// the <see cref="System.Threading.CancellationToken">CancellationToken</see> in the
        /// <paramref name="parallelOptions"/> has been disposed.</exception>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult">ParallelLoopResult</see> structure
        /// that contains information on what portion of the loop completed.</returns>
        /// <remarks>
        /// <para>
        /// The <see cref="System.Collections.Concurrent.Partitioner{TSource}">Partitioner</see> is used to retrieve
        /// the elements to be processed, in place of the original data source.  If the current element's
        /// index is desired, the source must be an <see cref="System.Collections.Concurrent.OrderablePartitioner{TSource}">
        /// OrderablePartitioner</see>.
        /// </para>
        /// <para>
        /// The <paramref name="body"/> delegate is invoked once for each element in the <paramref name="source"/>
        /// Partitioner.  It is provided with the following parameters: the current element,
        /// a <see cref="System.Threading.Tasks.ParallelLoopState">ParallelLoopState</see> instance that may be
        /// used to break out of the loop prematurely, the current element's index (an Int64), and some local
        /// state that may be shared amongst iterations that execute on the same thread.
        /// </para>
        /// <para>
        /// The <paramref name="localInit"/> delegate is invoked once for each thread that participates in the loop's
        /// execution and returns the initial local state for each of those threads.  These initial states are passed to the first
        /// <paramref name="body"/> invocations on each thread.  Then, every subsequent body invocation returns a possibly
        /// modified state value that is passed to the next body invocation.  Finally, the last body invocation on each thread returns a state value
        /// that is passed to the <paramref name="localFinally"/> delegate.  The localFinally delegate is invoked once per thread to perform a final
        /// action on each thread's local state.
        /// </para>
        /// </remarks>
        public static ParallelLoopResult ForEach<TSource, TLocal>(
            OrderablePartitioner<TSource> source,
            ParallelOptions parallelOptions,
            Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            if (!source.KeysNormalized)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_OrderedPartitionerKeysNotNormalized);
            }

            return PartitionerForEachWorker<TSource, TLocal>(source, parallelOptions, null, null, null, null, body, localInit, localFinally);
        }

        // Main worker method for Parallel.ForEach() calls w/ Partitioners.
        private static ParallelLoopResult PartitionerForEachWorker<TSource, TLocal>(
            Partitioner<TSource> source, // Might be OrderablePartitioner
            ParallelOptions parallelOptions,
            Action<TSource>? simpleBody,
            Action<TSource, ParallelLoopState>? bodyWithState,
            Action<TSource, ParallelLoopState, long>? bodyWithStateAndIndex,
            Func<TSource, ParallelLoopState, TLocal, TLocal>? bodyWithStateAndLocal,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal>? bodyWithEverything,
            Func<TLocal>? localInit,
            Action<TLocal>? localFinally)
        {
            Debug.Assert(((simpleBody == null ? 0 : 1) + (bodyWithState == null ? 0 : 1) +
                (bodyWithStateAndIndex == null ? 0 : 1) + (bodyWithStateAndLocal == null ? 0 : 1) + (bodyWithEverything == null ? 0 : 1)) == 1,
                "PartitionForEach: expected exactly one body function to be supplied");
            Debug.Assert((bodyWithStateAndLocal != null) || (bodyWithEverything != null) || (localInit == null && localFinally == null),
                "PartitionForEach: thread local functions should only be supplied for loops w/ thread local bodies");

            OrderablePartitioner<TSource>? orderedSource = source as OrderablePartitioner<TSource>;
            Debug.Assert((orderedSource != null) || (bodyWithStateAndIndex == null && bodyWithEverything == null),
                "PartitionForEach: bodies with indices are only allowable for OrderablePartitioner");

            if (!source.SupportsDynamicPartitions)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_PartitionerNotDynamic);
            }

            // Before getting started, do a quick peek to see if we have been canceled already
            parallelOptions.CancellationToken.ThrowIfCancellationRequested();

            // ETW event for Parallel For begin
            int forkJoinContextID = 0;
            if (ParallelEtwProvider.Log.IsEnabled())
            {
                forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
                ParallelEtwProvider.Log.ParallelLoopBegin(TaskScheduler.Current.Id, Task.CurrentId ?? 0,
                                                          forkJoinContextID, ParallelEtwProvider.ForkJoinOperationType.ParallelForEach,
                                                          0, 0);
            }

            // For all loops we need a shared flag even though we don't have a body with state,
            // because the shared flag contains the exceptional bool, which triggers other workers
            // to exit their loops if one worker catches an exception
            ParallelLoopStateFlags<long> sharedPStateFlags = new ParallelLoopStateFlags<long>();

            // Instantiate our result.  Specifics will be filled in later.
            ParallelLoopResult result = default;

            // Keep track of any cancellations
            OperationCanceledException? oce = null;

            // if cancellation is enabled, we need to register a callback to stop the loop when it gets signaled
            CancellationTokenRegistration ctr = (!parallelOptions.CancellationToken.CanBeCanceled)
                            ? default(CancellationTokenRegistration)
                            : parallelOptions.CancellationToken.UnsafeRegister((o) =>
                            {
                                // Record our cancellation before stopping processing
                                oce = new OperationCanceledException(parallelOptions.CancellationToken);
                                // Cause processing to stop
                                sharedPStateFlags.Cancel();
                            }, state: null);

            // Get our dynamic partitioner -- depends on whether source is castable to OrderablePartitioner
            // Also, do some error checking.
            IEnumerable<TSource>? partitionerSource = null;
            IEnumerable<KeyValuePair<long, TSource>>? orderablePartitionerSource = null;
            if (orderedSource != null)
            {
                orderablePartitionerSource = orderedSource.GetOrderableDynamicPartitions();
                if (orderablePartitionerSource == null)
                {
                    throw new InvalidOperationException(SR.Parallel_ForEach_PartitionerReturnedNull);
                }
            }
            else
            {
                partitionerSource = source.GetDynamicPartitions();
                if (partitionerSource == null)
                {
                    throw new InvalidOperationException(SR.Parallel_ForEach_PartitionerReturnedNull);
                }
            }

            try
            {
                try
                {
                    TaskReplicator.Run(
                        (ref IEnumerator partitionState, long timeout, out bool replicationDelegateYieldedBeforeCompletion) =>
                        {
                            // We will need to reset this to true if we exit due to a timeout:
                            replicationDelegateYieldedBeforeCompletion = false;

                            // ETW event for ParallelForEach Worker Fork
                            if (ParallelEtwProvider.Log.IsEnabled())
                            {
                                ParallelEtwProvider.Log.ParallelFork(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID);
                            }

                            TLocal localValue = default!;
                            bool bLocalValueInitialized = false; // Tracks whether localInit ran without exceptions, so that we can skip localFinally if it wasn't

                            try
                            {
                                // Create a new state object that references the shared "stopped" and "exceptional" flags.
                                // If needed, it will contain a new instance of thread-local state by invoking the selector.
                                ParallelLoopState<long>? state = null;

                                if (bodyWithState != null || bodyWithStateAndIndex != null)
                                {
                                    state = new ParallelLoopState<long>(sharedPStateFlags);
                                }
                                else if (bodyWithStateAndLocal != null || bodyWithEverything != null)
                                {
                                    state = new ParallelLoopState<long>(sharedPStateFlags);
                                    // If a thread-local selector was supplied, invoke it. Otherwise, stick with the default.
                                    if (localInit != null)
                                    {
                                        localValue = localInit();
                                        bLocalValueInitialized = true;
                                    }
                                }

                                // initialize a loop timer which will help us decide whether we should exit early
                                long loopTimeout = ComputeTimeoutPoint(timeout);

                                if (orderedSource != null)  // Use this path for OrderablePartitioner:
                                {
                                    // first check if there's saved state from a previous replica that we might be replacing.
                                    // the only state to be passed down in such a transition is the enumerator
                                    IEnumerator<KeyValuePair<long, TSource>>? myPartition = partitionState as IEnumerator<KeyValuePair<long, TSource>>;
                                    if (myPartition == null)
                                    {
                                        myPartition = orderablePartitionerSource!.GetEnumerator();
                                        partitionState = myPartition;
                                    }

                                    if (myPartition == null)
                                        throw new InvalidOperationException(SR.Parallel_ForEach_NullEnumerator);

                                    while (myPartition.MoveNext())
                                    {
                                        KeyValuePair<long, TSource> kvp = myPartition.Current;
                                        long index = kvp.Key;
                                        TSource value = kvp.Value;

                                        // Update our iteration index
                                        if (state != null) state.CurrentIteration = index;

                                        if (simpleBody != null)
                                            simpleBody(value);
                                        else if (bodyWithState != null)
                                            bodyWithState(value, state!);
                                        else if (bodyWithStateAndIndex != null)
                                            bodyWithStateAndIndex(value, state!, index);
                                        else if (bodyWithStateAndLocal != null)
                                            localValue = bodyWithStateAndLocal(value, state!, localValue);
                                        else
                                            localValue = bodyWithEverything!(value, state!, index, localValue);

                                        if (sharedPStateFlags.ShouldExitLoop(index)) break;

                                        // Cooperative multitasking:
                                        // Check if allowed loop time is exceeded, if so save current state and return.
                                        // The task replicator will queue up a replacement task. Note that we don't do this on the root task.
                                        if (CheckTimeoutReached(loopTimeout))
                                        {
                                            replicationDelegateYieldedBeforeCompletion = true;
                                            break;
                                        }
                                    }
                                }
                                else  // Use this path for Partitioner that is not OrderablePartitioner:
                                {
                                    // first check if there's saved state from a previous replica that we might be replacing.
                                    // the only state to be passed down in such a transition is the enumerator
                                    IEnumerator<TSource>? myPartition = partitionState as IEnumerator<TSource>;
                                    if (myPartition == null)
                                    {
                                        myPartition = partitionerSource!.GetEnumerator();
                                        partitionState = myPartition;
                                    }

                                    if (myPartition == null)
                                        throw new InvalidOperationException(SR.Parallel_ForEach_NullEnumerator);

                                    // I'm not going to try to maintain this
                                    if (state != null)
                                        state.CurrentIteration = 0;

                                    while (myPartition.MoveNext())
                                    {
                                        TSource t = myPartition.Current;

                                        if (simpleBody != null)
                                            simpleBody(t);
                                        else if (bodyWithState != null)
                                            bodyWithState(t, state!);
                                        else if (bodyWithStateAndLocal != null)
                                            localValue = bodyWithStateAndLocal(t, state!, localValue);
                                        else
                                            Debug.Fail("PartitionerForEach: illegal body type in Partitioner handler");

                                        // Any break, stop or exception causes us to halt
                                        // We don't have the global indexing information to discriminate whether or not
                                        // we are before or after a break point.
                                        if (sharedPStateFlags.LoopStateFlags != ParallelLoopStateFlags.ParallelLoopStateNone)
                                            break;

                                        // Cooperative multitasking:
                                        // Check if allowed loop time is exceeded, if so save current state and return.
                                        // The task replicator will queue up a replacement task. Note that we don't do this on the root task.
                                        if (CheckTimeoutReached(loopTimeout))
                                        {
                                            replicationDelegateYieldedBeforeCompletion = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Inform other tasks of the exception, then rethrow
                                sharedPStateFlags.SetExceptional();
                                ExceptionDispatchInfo.Throw(ex);
                            }
                            finally
                            {
                                if (localFinally != null && bLocalValueInitialized)
                                {
                                    localFinally(localValue);
                                }

                                if (!replicationDelegateYieldedBeforeCompletion)
                                {
                                    if (partitionState is IDisposable partitionToDispose)
                                        partitionToDispose.Dispose();
                                }

                                // ETW event for ParallelFor Worker Join
                                if (ParallelEtwProvider.Log.IsEnabled())
                                {
                                    ParallelEtwProvider.Log.ParallelJoin(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID);
                                }
                            }
                        },
                        parallelOptions,
                        stopOnFirstFailure: true);
                }
                finally
                {
                    // Dispose the cancellation token registration before checking for a cancellation exception
                    if (parallelOptions.CancellationToken.CanBeCanceled)
                        ctr.Dispose();
                }

                // If we got through that with no exceptions, and we were canceled, then
                // throw our cancellation exception
                if (oce != null) throw oce;
            }
            catch (AggregateException aggExp)
            {
                // If we have many cancellation exceptions all caused by the specified user cancel control, then throw only one OCE:
                ThrowSingleCancellationExceptionOrOtherException(aggExp.InnerExceptions, parallelOptions.CancellationToken, aggExp);
            }
            finally
            {
                int sb_status = sharedPStateFlags.LoopStateFlags;
                result._completed = (sb_status == ParallelLoopStateFlags.ParallelLoopStateNone);
                if ((sb_status & ParallelLoopStateFlags.ParallelLoopStateBroken) != 0)
                {
                    result._lowestBreakIteration = sharedPStateFlags.LowestBreakIteration;
                }

                //dispose the partitioner source if it implements IDisposable
                IDisposable? d = null;
                if (orderablePartitionerSource != null)
                {
                    d = orderablePartitionerSource as IDisposable;
                }
                else
                {
                    d = partitionerSource as IDisposable;
                }

                d?.Dispose();

                // ETW event for Parallel For End
                if (ParallelEtwProvider.Log.IsEnabled())
                {
                    ParallelEtwProvider.Log.ParallelLoopEnd(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID, 0);
                }
            }

            return result;
        }

        /// <summary>
        /// If all exceptions in the specified collection are OperationCanceledExceptions with the specified token,
        /// then get one such exception (the first one). Otherwise, return null.
        /// </summary>
        private static OperationCanceledException? ReduceToSingleCancellationException(ICollection exceptions,
                                                                                      CancellationToken cancelToken)
        {
            // If collection is empty - no match:
            if (exceptions == null || exceptions.Count == 0)
                return null;

            // If token is not cancelled, it can not be part of an exception:
            if (!cancelToken.IsCancellationRequested)
                return null;

            // Check all exceptions:
            Exception? first = null;
            foreach (object? exObj in exceptions)
            {
                Debug.Assert(exObj is Exception);
                Exception ex = (Exception)exObj;

                first ??= ex;

                // If mismatch found, fail-fast:
                OperationCanceledException? ocEx = ex as OperationCanceledException;
                if (ocEx == null || !cancelToken.Equals(ocEx.CancellationToken))
                    return null;
            }

            // All exceptions are OCEs with this token, let's just pick the first:
            Debug.Assert(first is OperationCanceledException);
            return (OperationCanceledException)first;
        }


        /// <summary>
        /// IF exceptions are all OperationCanceledExceptions with the specified cancelToken,
        /// THEN throw that unique OperationCanceledException (pick any);
        /// OTHERWISE throw the specified otherException.
        /// </summary>
        private static void ThrowSingleCancellationExceptionOrOtherException(ICollection exceptions,
                                                                             CancellationToken cancelToken,
                                                                             Exception otherException)
        {
            OperationCanceledException? reducedCancelEx = ReduceToSingleCancellationException(exceptions, cancelToken);
            ExceptionDispatchInfo.Throw(reducedCancelEx ?? otherException);
        }
    }  // class Parallel
}  // namespace
