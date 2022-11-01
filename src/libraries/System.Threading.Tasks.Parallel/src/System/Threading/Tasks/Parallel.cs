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
    internal sealed class Box<T> where T: class
    {
        public T? Value { get; set; }
    }

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
                if ((value == 0) || (value < -1))
                    throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism));
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

    #region Worker Bodies

    internal interface IWorkerBody<in TValue>
    {
        void Body(TValue value);

        virtual void Finally() { }
    }

    internal  interface IWorkerBodyWithIndex<in TValue, in TIndex> : IWorkerBody<TValue>
        where TIndex : INumber<TIndex>
    {
        void SetIteration(TIndex index);
    }

    internal sealed class SimpleWorkerBody<TValue> : IWorkerBody<TValue>
    {
        private readonly Action<TValue> _body;

        internal SimpleWorkerBody(Action<TValue> body)
        {
            _body = body;
        }

        public void Body(TValue index)
        {
            _body(index);
        }
    }

    internal abstract class WorkerBodyWithIndex<TValue, TIndex> : IWorkerBodyWithIndex<TValue, TIndex>
        where TIndex : INumber<TIndex>
    {
        protected readonly ParallelLoopState State;

        protected WorkerBodyWithIndex(ParallelLoopStateFlags sharedPStateFlags)
        {
            State = sharedPStateFlags.CreateLoopState();
        }

        public abstract void Body(TValue value);

        public void SetIteration(TIndex index) => State.SetCurrentIteration(index);

        public virtual void Finally() {}
    }

    internal sealed class WorkerWithState<TValue, TIndex> : WorkerBodyWithIndex<TValue, TIndex>
        where TIndex : INumber<TIndex>
    {
        private readonly Action<TValue, ParallelLoopState> _bodyWithState;

        internal WorkerWithState(ParallelLoopStateFlags sharedPStateFlags,
            Action<TValue, ParallelLoopState> bodyWithState) : base(sharedPStateFlags)
        {
            _bodyWithState = bodyWithState;
        }

        public override void Body(TValue value)
        {
            _bodyWithState(value, State);
        }
    }

    internal sealed class WorkerWithStateAndIndex<TValue, TIndex> : WorkerBodyWithIndex<TValue, TIndex>
        where TIndex : INumber<TIndex>
    {
        private readonly Action<TValue, ParallelLoopState, TIndex> _bodyWithStateAndIndex;

        public WorkerWithStateAndIndex(ParallelLoopStateFlags sharedPStateFlags,
            Action<TValue, ParallelLoopState, TIndex> bodyWithStateAndIndex) : base(sharedPStateFlags)
        {
            _bodyWithStateAndIndex = bodyWithStateAndIndex;
        }

        public override void Body(TValue value)
        {
            _bodyWithStateAndIndex(value, State, State.GetCurrentIteration<TIndex>());
        }
    }

    internal abstract class WorkerWithLocal<TValue, TIndex, TLocal> : WorkerBodyWithIndex<TValue, TIndex>
        where TIndex : INumber<TIndex>
    {
        private readonly Action<TLocal>? _localFinally;
        protected TLocal LocalValue;

        protected WorkerWithLocal(
            ParallelLoopStateFlags sharedPStateFlags,
            Func<TLocal> localInit,
            Action<TLocal>? localFinally) : base(sharedPStateFlags)
        {
            LocalValue = localInit();
            _localFinally = localFinally;
        }

        public override void Finally()
        {
            // If a cleanup function was specified, call it. Otherwise, if the type is
            // IDisposable, we will invoke Dispose on behalf of the user.
            _localFinally?.Invoke(LocalValue);
        }
    }

    internal sealed class WorkerWithStateAndLocal<TValue, TIndex, TLocal> : WorkerWithLocal<TValue, TIndex, TLocal>
        where TIndex : INumber<TIndex>
    {
        private readonly Func<TValue, ParallelLoopState, TLocal, TLocal> _bodyWithLocal;

        internal WorkerWithStateAndLocal(
            ParallelLoopStateFlags sharedPStateFlags,
            Func<TValue, ParallelLoopState, TLocal, TLocal> bodyWithLocal,
            Func<TLocal> localInit,
            Action<TLocal>? localFinally) : base(sharedPStateFlags, localInit, localFinally)
        {
            _bodyWithLocal = bodyWithLocal;
        }

        public override void Body(TValue value)
        {
            LocalValue = _bodyWithLocal(value, State, LocalValue);
        }
    }

    internal sealed class WorkerWithEverything<TValue, TIndex, TLocal> : WorkerWithLocal<TValue, TIndex, TLocal>
        where TIndex : INumber<TIndex>
    {
        private readonly Func<TValue, ParallelLoopState, TIndex, TLocal, TLocal> _bodyWithEverything;

        public WorkerWithEverything(
            ParallelLoopStateFlags sharedPStateFlags,
            Func<TValue, ParallelLoopState, TIndex, TLocal, TLocal> bodyWithEverything,
            Func<TLocal> localInit,
            Action<TLocal>? localFinally) : base(sharedPStateFlags, localInit, localFinally)
        {
            _bodyWithEverything = bodyWithEverything;
        }

        public override void Body(TValue value)
        {
            LocalValue = _bodyWithEverything(value, State, State.GetCurrentIteration<TIndex>(), LocalValue);
        }
    }

    #endregion

    #region Worker Bodies Factories

    internal interface IWorkerBodyFactory<in TValue>
    {
        IWorkerBody<TValue> CreateWorkerBody(ParallelLoopStateFlags sharedPStateFlags);
    }

    internal sealed class WorkerWithSimpleBodyFactory<TValue>: IWorkerBodyFactory<TValue>
    {
        private readonly Action<TValue> _body;

        public WorkerWithSimpleBodyFactory(Action<TValue> body)
        {
            _body = body;
        }

        public IWorkerBody<TValue> CreateWorkerBody(ParallelLoopStateFlags sharedPStateFlags)
            => new SimpleWorkerBody<TValue>(_body);
    }

    internal sealed class WorkerWithStateFactory<TValue, TIndex> : IWorkerBodyFactory<TValue> where TIndex: INumber<TIndex>
    {
        private readonly Action<TValue, ParallelLoopState> _bodyWithState;

        public WorkerWithStateFactory(Action<TValue, ParallelLoopState> bodyWithState)
        {
            _bodyWithState = bodyWithState;
        }

        public IWorkerBody<TValue> CreateWorkerBody(ParallelLoopStateFlags sharedPStateFlags)
            => new WorkerWithState<TValue, TIndex>(sharedPStateFlags, _bodyWithState);
    }

    internal sealed class WorkerWithStateAndIndexFactory<TValue, TIndex> : IWorkerBodyFactory<TValue> where TIndex: INumber<TIndex>
    {
        private readonly Action<TValue, ParallelLoopState, TIndex> _bodyWithStateAndIndex;

        public WorkerWithStateAndIndexFactory(Action<TValue, ParallelLoopState, TIndex> bodyWithStateAndIndex)
        {
            _bodyWithStateAndIndex = bodyWithStateAndIndex;
        }

        public IWorkerBody<TValue> CreateWorkerBody(ParallelLoopStateFlags sharedPStateFlags)
            => new WorkerWithStateAndIndex<TValue, TIndex>(sharedPStateFlags, _bodyWithStateAndIndex);
    }


    internal sealed class WorkerWithLocalFactory<TValue, TIndex, TLocal> : IWorkerBodyFactory<TValue> where TIndex: INumber<TIndex>
    {
        private readonly Func<TValue, ParallelLoopState, TLocal, TLocal> _bodyWithLocal;
        private readonly Func<TLocal> _localInit;
        private readonly Action<TLocal>? _localFinally;

        public WorkerWithLocalFactory(Func<TValue, ParallelLoopState, TLocal, TLocal> bodyWithLocal, Func<TLocal> localInit, Action<TLocal>? localFinally)
        {
            _bodyWithLocal = bodyWithLocal;
            _localInit = localInit;
            _localFinally = localFinally;
        }

        public IWorkerBody<TValue> CreateWorkerBody(ParallelLoopStateFlags sharedPStateFlags)
            => new WorkerWithStateAndLocal<TValue, TIndex, TLocal>(sharedPStateFlags, _bodyWithLocal, _localInit, _localFinally);
    }

    internal sealed class WorkerWithEveryThingFactory<TValue, TIndex, TLocal> : IWorkerBodyFactory<TValue> where TIndex: INumber<TIndex>
    {
        private readonly Func<TValue, ParallelLoopState, TIndex, TLocal, TLocal> _bodyWithLocal;
        private readonly Func<TLocal> _localInit;
        private readonly Action<TLocal>? _localFinally;

        public WorkerWithEveryThingFactory(Func<TValue, ParallelLoopState, TIndex, TLocal, TLocal> bodyWithLocal, Func<TLocal> localInit, Action<TLocal>? localFinally)
        {
            _bodyWithLocal = bodyWithLocal;
            _localInit = localInit;
            _localFinally = localFinally;
        }

        public IWorkerBody<TValue> CreateWorkerBody(ParallelLoopStateFlags sharedPStateFlags)
            => new WorkerWithEverything<TValue, TIndex, TLocal>(sharedPStateFlags, _bodyWithLocal, _localInit, _localFinally);
    }

    #endregion

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
            Action[] actionsCopy = CopyActionArray(actions);

            // ETW event for Parallel Invoke Begin
            int forkJoinContextID = LogEtwEventInvokeBegin(actionsCopy);

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

                // Web browsers need special treatment that is implemented in TaskReplicator
                if (OperatingSystem.IsBrowser() ||
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
                            (ref object state, int timeout, out bool replicationDelegateYieldedBeforeCompletion) =>
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
                                        LazyInitializer.EnsureInitialized(ref exceptionQ, () => { return new ConcurrentQueue<Exception>(); });
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
                        LazyInitializer.EnsureInitialized(ref exceptionQ, () => { return new ConcurrentQueue<Exception>(); });

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
                        ThrowSingleCancellationExceptionOrOtherException(exceptionQ, parallelOptions.CancellationToken, new AggregateException(exceptionQ));
                    }
                }
                else // This is more efficient for a small number of actions and no DOP support:
                {
                    // Initialize our array of tasks, one per action.
                    Task[] tasks = new Task[actionsCopy.Length];

                    // One more check before we begin...
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();

                    // Invoke all actions as tasks.  Queue N-1 of them, and run 1 synchronously.
                    for (int i = 1; i < tasks.Length; i++)
                    {
                        tasks[i] = Task.Factory.StartNew(actionsCopy[i], parallelOptions.CancellationToken, TaskCreationOptions.None, parallelOptions.EffectiveTaskScheduler);
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
                LogEtwEventForParallelInvokeEnd(forkJoinContextID);
            }
        }

        private static ParallelLoopResult LoopCore<TSource, TInput, TValue, TWorker, TIndex>(TSource source, ParallelOptions parallelOptions, IWorkerBodyFactory<TValue> workerBodyFactory)
            where TWorker: IWorkerFactory<TSource, TInput, TValue, TIndex>
            where TIndex: INumber<TIndex>
        {
            // Instantiate our result.  Specifics will be filled in later.
            ParallelLoopResult result = default;

            // For all loops we need a shared flag even though we don't have a body with state,
            // because the shared flag contains the exceptional bool, which triggers other workers
            // to exit their loops if one worker catches an exception
            ParallelLoopStateFlags sharedPStateFlags = ParallelLoopStateFlags.Create<long>();

            // Before getting started, do a quick peek to see if we have been canceled already
            parallelOptions.CancellationToken.ThrowIfCancellationRequested();

            // Keep track of any cancellations
            Box<OperationCanceledException> oce = RegisterCallbackForLoopTermination(parallelOptions, sharedPStateFlags, out CancellationTokenRegistration ctr);

            const ParallelEtwProvider.ForkJoinOperationType OperationType = ParallelEtwProvider.ForkJoinOperationType.ParallelForEach;

            int forkJoinContextID = LogEtwEventParallelLoopBegin(OperationType, 0, 0);

            try
            {
                try
                {
                    TaskReplicator.ReplicatableUserAction<TInput> worker =
                        TWorker.CreateWorker(forkJoinContextID, sharedPStateFlags, source, workerBodyFactory).Work;
                    TaskReplicator.Run(worker, parallelOptions, stopOnFirstFailure: true);
                }
                finally
                {
                    // Dispose the cancellation token registration before checking for a cancellation exception
                    if (parallelOptions.CancellationToken.CanBeCanceled)
                        ctr.Dispose();
                }

                // If we got through that with no exceptions, and we were canceled, then
                // throw our cancellation exception
                if (oce.Value != null) throw oce.Value;
            }
            catch (AggregateException aggExp)
            {
                // If we have many cancellation exceptions all caused by the specified user cancel control, then throw only one OCE:
                ThrowSingleCancellationExceptionOrOtherException(aggExp.InnerExceptions, parallelOptions.CancellationToken, aggExp);
            }
            finally
            {
                SetLoopResultEndState(sharedPStateFlags, ref result);

                // ETW event for Parallel For End
                if (ParallelEtwProvider.Log.IsEnabled())
                {
                    LogEtwEventParallelLoopEnd(forkJoinContextID, TWorker.GetTotalIterations(source, sharedPStateFlags));
                }

                IDisposable? d = source as IDisposable;
                d?.Dispose();
            }

            return result;
        }


        #region For Entry Points

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
            return For<int>(fromInclusive, toExclusive, s_defaultParallelOptions, body);
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
            return For<long>(fromInclusive, toExclusive, s_defaultParallelOptions, body);
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
            return For<int>(fromInclusive, toExclusive, parallelOptions, body);
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
            return For<long>(fromInclusive, toExclusive, parallelOptions, body);
        }

        private static ParallelLoopResult For<TIndex>(TIndex fromInclusive, TIndex toExclusive, ParallelOptions parallelOptions, Action<TIndex> body)
            where TIndex : INumber<TIndex>
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            IWorkerBodyFactory<TIndex> workerBodyFactory = new WorkerWithSimpleBodyFactory<TIndex>(body);
            return ForWorkerOrchestrator(fromInclusive, toExclusive, parallelOptions, workerBodyFactory);
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
            return For<int>(fromInclusive, toExclusive, s_defaultParallelOptions, body);
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
            return For<long>(fromInclusive, toExclusive, s_defaultParallelOptions, body);
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
            return For<int>(fromInclusive, toExclusive, parallelOptions, body);
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
        public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long, ParallelLoopState> body)
        {
            return For<long>(fromInclusive, toExclusive, parallelOptions, body);
        }

        private static ParallelLoopResult For<TIndex>(TIndex fromInclusive, TIndex toExclusive, ParallelOptions parallelOptions, Action<TIndex, ParallelLoopState> body)
            where TIndex : INumber<TIndex>
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            IWorkerBodyFactory<TIndex> workerBodyFactory = new WorkerWithStateFactory<TIndex, TIndex>(body);
            return ForWorkerOrchestrator(fromInclusive, toExclusive, parallelOptions, workerBodyFactory);
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
            return For<int, TLocal>(fromInclusive, toExclusive, s_defaultParallelOptions, localInit, body, localFinally);
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
            return For<long, TLocal>(fromInclusive, toExclusive, s_defaultParallelOptions, localInit, body, localFinally);
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
            return For<int, TLocal>(fromInclusive, toExclusive, parallelOptions, localInit, body, localFinally);
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
            return For<long, TLocal>(fromInclusive, toExclusive, parallelOptions, localInit, body, localFinally);
        }

        private static ParallelLoopResult For<TIndex, TLocal>(
            TIndex fromInclusive, TIndex toExclusive, ParallelOptions parallelOptions,
            Func<TLocal> localInit,
            Func<TIndex, ParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
            where TIndex : INumber<TIndex>
        {
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(localInit);
            ArgumentNullException.ThrowIfNull(body);
            ArgumentNullException.ThrowIfNull(localFinally);

            IWorkerBodyFactory<TIndex> workerBodyFactory = new WorkerWithLocalFactory<TIndex, TIndex, TLocal>(body, localInit, localFinally);
            return ForWorkerOrchestrator(fromInclusive, toExclusive, parallelOptions, workerBodyFactory);
        }

        #endregion

        /// <summary>
        /// Performs the major work of the parallel for loop. It assumes that argument validation has already
        /// been performed by the caller. This function's whole purpose in life is to enable as much reuse of
        /// common implementation details for the various For overloads we offer. Without it, we'd end up
        /// with lots of duplicate code. It handles: (1) simple for loops, (2) for loops that depend on
        /// ParallelState, and (3) for loops with thread local data.
        ///
        /// </summary>
        /// <typeparam name="TIndex">The type of the index size used (int/long).</typeparam>
        /// <param name="fromInclusive">The loop's start index, inclusive.</param>
        /// <param name="toExclusive">The loop's end index, exclusive.</param>
        /// <param name="parallelOptions">A ParallelOptions instance.</param>
        /// <param name="workerBodyFactory">Factory for creating thread workers</param>
        /// <remarks>Only one of the body arguments may be supplied (i.e. they are exclusive).</remarks>
        /// <returns>A <see cref="System.Threading.Tasks.ParallelLoopResult"/> structure.</returns>
        private static ParallelLoopResult ForWorkerOrchestrator<TIndex>(
            TIndex fromInclusive, TIndex toExclusive,
            ParallelOptions parallelOptions,
            IWorkerBodyFactory<TIndex> workerBodyFactory) where TIndex : INumber<TIndex>
        {
            Debug.Assert(typeof(TIndex) == typeof(int) || typeof(TIndex) == typeof(long), "only long and int index types supported in TIndex");

            // We just return immediately if 'to' is smaller (or equal to) 'from'.
            if (toExclusive <= fromInclusive)
            {
                ParallelLoopResult result = default;
                result._completed = true;
                return result;
            }

            // initialize ranges with passed in loop arguments and expected number of workers
            int numExpectedWorkers = (parallelOptions.EffectiveMaxConcurrencyLevel == -1)
                ? Environment.ProcessorCount
                : parallelOptions.EffectiveMaxConcurrencyLevel;
            RangeManager rangeManager = new RangeManager(long.CreateChecked(fromInclusive),
                long.CreateChecked(toExclusive), 1, numExpectedWorkers);

            return LoopCore<RangeManager, RangeWorker, TIndex, ForWorker<TIndex>, TIndex>(
                rangeManager, parallelOptions, workerBodyFactory);
        }

        #region Workers

        private abstract class Worker<TInput, TValue>
        {
            private readonly int _forkJoinContextId;
            private readonly IWorkerBodyFactory<TValue> _workerBodyFactory;
            protected readonly ParallelLoopStateFlags SharedPStateFlags;

            protected Worker(int forkJoinContextId, ParallelLoopStateFlags sharedPStateFlags,
                IWorkerBodyFactory<TValue> workerBodyFactory)
            {
                _forkJoinContextId = forkJoinContextId;
                SharedPStateFlags = sharedPStateFlags;
                _workerBodyFactory = workerBodyFactory;
            }

            internal void Work(ref TInput input, int timeout, out bool replicationDelegateYieldedBeforeCompletion)
            {
                IWorkerBody<TValue> workerBody = _workerBodyFactory.CreateWorkerBody(SharedPStateFlags);
                // We will need to reset this to true if we exit due to a timeout:
                replicationDelegateYieldedBeforeCompletion = false;

                LogEtwEventParallelFork(_forkJoinContextId);

                try
                {
                    // initialize a loop timer which will help us decide whether we should exit early
                    int loopTimeout = ComputeTimeoutPoint(timeout);

                    LoopBody(workerBody, loopTimeout, ref input, ref replicationDelegateYieldedBeforeCompletion);
                }
                catch (Exception ex)
                {
                    // if we catch an exception in a worker, we signal the other workers to exit the loop, and we rethrow
                    SharedPStateFlags.SetExceptional();
                    ExceptionDispatchInfo.Throw(ex);
                }
                finally
                {
                    Finally(workerBody, ref input, replicationDelegateYieldedBeforeCompletion);

                    LogEtwEventParallelJoin(_forkJoinContextId);
                }
            }

            protected abstract void LoopBody(IWorkerBody<TValue> workerBody, int loopTimeout, ref TInput input,
                ref bool replicationDelegateYieldedBeforeCompletion);

            protected virtual void Finally(IWorkerBody<TValue> workerBody, ref TInput input, in bool replicationDelegateYieldedBeforeCompletion)
            {
                workerBody.Finally();
            }
        }

        private interface IWorkerFactory<in TSource, TInput, TValue, out TIndex> where TIndex: INumber<TIndex>
        {
            static abstract Worker<TInput, TValue> CreateWorker(int forkJoinContextId, ParallelLoopStateFlags sharedPStateFlags, TSource source, IWorkerBodyFactory<TValue> workerBodyFactory);
            static abstract TIndex GetTotalIterations(TSource source, ParallelLoopStateFlags sharedPStateFlags);
        }

        private sealed class ForWorker<TIndex> : Worker<RangeWorker, TIndex>, IWorkerFactory<RangeManager, RangeWorker, TIndex, TIndex> where TIndex : INumber<TIndex>
        {
            private readonly RangeManager _rangeManager;


            private ForWorker(int forkJoinContextId, ParallelLoopStateFlags sharedPStateFlags,
                RangeManager rangeManager, IWorkerBodyFactory<TIndex> workerBodyFactory)
                : base(forkJoinContextId, sharedPStateFlags, workerBodyFactory)
            {
                _rangeManager = rangeManager;
            }

            protected override void LoopBody(IWorkerBody<TIndex> workerBody, int loopTimeout,
                ref RangeWorker currentWorker,
                ref bool replicationDelegateYieldedBeforeCompletion)
            {
                // First thing we do upon entering the task is to register as a new "RangeWorker" with the
                // shared RangeManager instance.
                if (!currentWorker.IsInitialized)
                    currentWorker = _rangeManager.RegisterNewWorker();

                // We need to call FindNewWork() on it to see whether there's a chunk available.
                // These are the local index values to be used in the sequential loop.
                // Their values filled in by FindNewWork
                if (currentWorker.FindNewWork(out TIndex nFromInclusiveLocal, out TIndex nToExclusiveLocal) == false ||
                    SharedPStateFlags.ShouldExitLoop(nFromInclusiveLocal))
                {
                    return; // no need to run
                }

                // Now perform the loop itself.
                do
                {
                    for (TIndex j = nFromInclusiveLocal;
                         j < nToExclusiveLocal && (SharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.ParallelLoopStateNone // fast path check as SEL() doesn't inline
                                                   || !SharedPStateFlags.ShouldExitLoop(j));
                         j += TIndex.One)
                    {
                        if (workerBody is IWorkerBodyWithIndex<TIndex, TIndex> workerBodyWithIndex)
                        {
                            workerBodyWithIndex.SetIteration(j);
                        }

                        workerBody.Body(j);
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
                         ((SharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.ParallelLoopStateNone) ||
                          !SharedPStateFlags.ShouldExitLoop(nFromInclusiveLocal)));
            }

            public static Worker<RangeWorker, TIndex> CreateWorker(
                int forkJoinContextId, ParallelLoopStateFlags sharedPStateFlags, RangeManager source, IWorkerBodyFactory<TIndex> workerBodyFactory)
                => new ForWorker<TIndex>(forkJoinContextId, sharedPStateFlags, source, workerBodyFactory);

            public static TIndex GetTotalIterations(RangeManager manager, ParallelLoopStateFlags sharedPStateFlags)
            {
                //TODO: finish writing this function
                TIndex nTotalIterations;
                int sbStatus = sharedPStateFlags.LoopStateFlags;
                TIndex fromInclusive = TIndex.CreateChecked(manager.FromInclusive);
                TIndex toExclusive = TIndex.CreateChecked(manager.ToExclusive);

                // calculate how many iterations we ran in total
                if (sbStatus == ParallelLoopStateFlags.ParallelLoopStateNone)
                    nTotalIterations = toExclusive - fromInclusive;
                else if ((sbStatus & ParallelLoopStateFlags.ParallelLoopStateBroken) != 0)
                    nTotalIterations = TIndex.CreateChecked(sharedPStateFlags.LowestBreakIteration) - fromInclusive;
                else
                    nTotalIterations = TIndex.CreateChecked(-1); //ParallelLoopStateStopped! We can't determine this if we were stopped..
                return nTotalIterations;
            }
        }

        private abstract class ForeachWorker<TSource, TValue>: Worker<IEnumerator, TValue>
        {
            private readonly IEnumerable<TSource>? _partitionSource;

            protected ForeachWorker(int forkJoinContextId, ParallelLoopStateFlags sharedPStateFlags, IEnumerable<TSource>? partitionSource, IWorkerBodyFactory<TValue> workerBodyFactory)
                : base(forkJoinContextId, sharedPStateFlags, workerBodyFactory)
            {
                _partitionSource = partitionSource;
            }

            protected override void Finally(IWorkerBody<TValue> workerBody, ref IEnumerator partitionState, in bool replicationDelegateYieldedBeforeCompletion)
            {
                if (replicationDelegateYieldedBeforeCompletion) return;

                if (partitionState is IDisposable partitionToDispose)
                    partitionToDispose.Dispose();
                base.Finally(workerBody, ref partitionState, replicationDelegateYieldedBeforeCompletion);
            }

            protected sealed override void LoopBody(IWorkerBody<TValue> workerBody, int loopTimeout,
                ref IEnumerator partitionState, ref bool replicationDelegateYieldedBeforeCompletion)
            {
                // first check if there's saved state from a previous replica that we might be replacing.
                // the only state to be passed down in such a transition is the enumerator
                if (partitionState is not IEnumerator<TSource> myPartition)
                {
                    myPartition = _partitionSource!.GetEnumerator();
                    partitionState = myPartition;
                }

                if (myPartition == null)
                    throw new InvalidOperationException(SR.Parallel_ForEach_NullEnumerator);

                while (myPartition.MoveNext())
                {
                    TSource value = myPartition.Current;

                    Body(workerBody, value);

                    if (ShouldBreak(value)) break;

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

            protected abstract void Body(IWorkerBody<TValue> workerBody, TSource value);

            protected abstract bool ShouldBreak(TSource value);
        }

        private sealed class OrderablePartitionerForeachWorker<TValue>
            : ForeachWorker<KeyValuePair<long, TValue>, TValue>,
                IWorkerFactory<IEnumerable<KeyValuePair<long, TValue>>, IEnumerator, TValue, long>
        {
            private OrderablePartitionerForeachWorker(int forkJoinContextId, ParallelLoopStateFlags sharedPStateFlags,
                IEnumerable<KeyValuePair<long, TValue>>? partitionSource,
                IWorkerBodyFactory<TValue> workerBodyFactory)
                : base(forkJoinContextId, sharedPStateFlags, partitionSource, workerBodyFactory)
            {
            }

            protected override bool ShouldBreak(KeyValuePair<long, TValue> value) => SharedPStateFlags.ShouldExitLoop(value.Key);

            protected override void Body(IWorkerBody<TValue> workerBody, KeyValuePair<long, TValue> value)
            {
                if (workerBody is IWorkerBodyWithIndex<TValue, long> workerBodyWithIndex)
                {
                    workerBodyWithIndex.SetIteration(value.Key);
                }
                workerBody.Body(value.Value);
            }

            public static Worker<IEnumerator, TValue> CreateWorker(
                int forkJoinContextId, ParallelLoopStateFlags sharedPStateFlags,
                IEnumerable<KeyValuePair<long, TValue>> source,
                IWorkerBodyFactory<TValue> workerBodyFactory) =>
                new OrderablePartitionerForeachWorker<TValue>(forkJoinContextId, sharedPStateFlags, source, workerBodyFactory);

            public static long GetTotalIterations(IEnumerable<KeyValuePair<long, TValue>> source, ParallelLoopStateFlags sharedPStateFlags) => 0;
        }

        private sealed class PartitionerForeachWorker<TValue>
            : ForeachWorker<TValue, TValue>,
                IWorkerFactory<IEnumerable<TValue>, IEnumerator, TValue, long>
        {
            private PartitionerForeachWorker(int forkJoinContextId, ParallelLoopStateFlags sharedPStateFlags,
                IEnumerable<TValue>? partitionerSource, IWorkerBodyFactory<TValue> workerBodyFactory)
                : base(forkJoinContextId, sharedPStateFlags, partitionerSource, workerBodyFactory)
            {
            }

            protected override void Body(IWorkerBody<TValue> workerBody, TValue value) => workerBody.Body(value);

            protected override bool ShouldBreak(TValue value) =>
                SharedPStateFlags.LoopStateFlags != ParallelLoopStateFlags.ParallelLoopStateNone;

            public static Worker<IEnumerator, TValue> CreateWorker(
                int forkJoinContextId, ParallelLoopStateFlags sharedPStateFlags, IEnumerable<TValue> source,
                IWorkerBodyFactory<TValue> workerBodyFactory) =>
                new PartitionerForeachWorker<TValue>(forkJoinContextId, sharedPStateFlags, source, workerBodyFactory);

            public static long GetTotalIterations(IEnumerable<TValue> source, ParallelLoopStateFlags sharedPStateFlags) => 0;
        }

        #endregion

        #region Foreach EntryPoints

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
            return ForEach(source, s_defaultParallelOptions, body);
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

            if (source is TSource[] array)
            {
                int from = array.GetLowerBound(0);
                int to = array.GetUpperBound(0) + 1;
                return For(from, to, parallelOptions, (i) => body(array[i]));
            }

            if (source is IList<TSource> list)
            {
                return For(0, list.Count, parallelOptions, (i) => body(list[i]));
            }

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithSimpleBodyFactory<TSource>(body);
            return PartitionerForEachWorker(Partitioner.Create(source), parallelOptions, workerBodyFactory);
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
            return ForEach(source, s_defaultParallelOptions, body);
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

            if (source is TSource[] array)
            {
                int from = array.GetLowerBound(0);
                int to = array.GetUpperBound(0) + 1;
                return For(from, to, parallelOptions, (i, state) => body(array[i], state));
            }

            if (source is IList<TSource> list)
            {
                return For(0, list.Count, parallelOptions, (i, state) => body(list[i], state));
            }

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithStateFactory<TSource, long>(body);
            return PartitionerForEachWorker(Partitioner.Create(source), parallelOptions, workerBodyFactory);
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
            return ForEach(source, s_defaultParallelOptions, body);
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

            if (source is TSource[] array)
            {
                int from = array.GetLowerBound(0);
                int to = array.GetUpperBound(0) + 1;
                return For(from, to, parallelOptions, (i, state) => body(array[i], state, i));
            }

            if (source is IList<TSource> list)
            {
                return For(0, list.Count, parallelOptions, (i, state) => body(list[i], state, i));
            }

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithStateAndIndexFactory<TSource, long>(body);
            return PartitionerForEachWorker(Partitioner.Create(source), parallelOptions, workerBodyFactory);
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
            return ForEach(source, s_defaultParallelOptions, localInit, body, localFinally);
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

            if (source is TSource[] array)
            {
                int from = array.GetLowerBound(0);
                int to = array.GetUpperBound(0) + 1;
                return For(from, to, parallelOptions, localInit, (i, state, local) => body(array[i], state, local), localFinally);
            }

            if (source is IList<TSource> list)
            {
                return For(0, list.Count, parallelOptions, localInit, (i, state, local) => body(list[i], state, local), localFinally);
            }

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithLocalFactory<TSource, long, TLocal>(body, localInit, localFinally);
            return PartitionerForEachWorker(Partitioner.Create(source), parallelOptions, workerBodyFactory);
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
            return ForEach(source, s_defaultParallelOptions, localInit, body, localFinally);
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

            if (source is TSource[] array)
            {
                int from = array.GetLowerBound(0);
                int to = array.GetUpperBound(0) + 1;
                return For(from, to, parallelOptions, localInit, (i, state, local) => body(array[i], state, i, local), localFinally);
            }

            if (source is IList<TSource> list)
            {
                return For(0, list.Count, parallelOptions, localInit, (i, state, local) => body(list[i], state, i, local), localFinally);
            }

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithEveryThingFactory<TSource, long, TLocal>(body, localInit, localFinally);
            return PartitionerForEachWorker(Partitioner.Create(source), parallelOptions, workerBodyFactory);
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
        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource> body)
        {
            return ForEach(source, s_defaultParallelOptions, body);
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
        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource, ParallelLoopState> body)
        {
            return ForEach(source, s_defaultParallelOptions, body);
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
        public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, Action<TSource, ParallelLoopState, long> body)
        {
            return ForEach(source, s_defaultParallelOptions, body);
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
            Partitioner<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            return ForEach(source, s_defaultParallelOptions, localInit, body, localFinally);
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
            OrderablePartitioner<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            return ForEach(source, s_defaultParallelOptions, localInit, body, localFinally);
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
        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithSimpleBodyFactory<TSource>(body);
            return PartitionerForEachWorker(source, parallelOptions, workerBodyFactory);
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
        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithStateFactory<TSource, long>(body);
            return PartitionerForEachWorker(source, parallelOptions, workerBodyFactory);
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
        public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState, long> body)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            if (!source.KeysNormalized)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_OrderedPartitionerKeysNotNormalized);
            }

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithStateAndIndexFactory<TSource, long>(body);
            return PartitionerForEachWorker(source, parallelOptions, workerBodyFactory);
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

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithLocalFactory<TSource, long, TLocal>(body, localInit, localFinally);
            return PartitionerForEachWorker(source, parallelOptions, workerBodyFactory);
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

            IWorkerBodyFactory<TSource> workerBodyFactory = new WorkerWithEveryThingFactory<TSource, long, TLocal>(body, localInit, localFinally);
            return PartitionerForEachWorker(source, parallelOptions, workerBodyFactory);
        }

        #endregion

        // Main worker method for Parallel.ForEach() calls w/ Partitioners.
        private static ParallelLoopResult PartitionerForEachWorker<TValue>(Partitioner<TValue> source, ParallelOptions parallelOptions, IWorkerBodyFactory<TValue> workerBodyFactory)
        {
            if (source is OrderablePartitioner<TValue> orderedSource)
            {
                return PartitionerForEachWorker(orderedSource, parallelOptions, workerBodyFactory);
            }

            if (!source.SupportsDynamicPartitions)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_PartitionerNotDynamic);
            }

            IEnumerable<TValue>? partitionerSource = source.GetDynamicPartitions();
            if (partitionerSource == null)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_PartitionerReturnedNull);
            }

            return LoopCore<IEnumerable<TValue>, IEnumerator, TValue, PartitionerForeachWorker<TValue>, long>(
                partitionerSource, parallelOptions, workerBodyFactory);
        }

        // Main worker method for Parallel.ForEach() calls w/ Partitioners.
        private static ParallelLoopResult PartitionerForEachWorker<TValue>(OrderablePartitioner<TValue> orderedSource, ParallelOptions parallelOptions, IWorkerBodyFactory<TValue> workerBodyFactory)
        {
            if (!orderedSource.SupportsDynamicPartitions)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_PartitionerNotDynamic);
            }

            IEnumerable<KeyValuePair<long, TValue>>? orderablePartitionerSource = orderedSource.GetOrderableDynamicPartitions();
            if (orderablePartitionerSource == null)
            {
                throw new InvalidOperationException(SR.Parallel_ForEach_PartitionerReturnedNull);
            }

            return LoopCore<IEnumerable<KeyValuePair<long, TValue>>, IEnumerator, TValue, OrderablePartitionerForeachWorker<TValue>, long>(
                orderablePartitionerSource, parallelOptions, workerBodyFactory);
        }

        #region Helpers

        private static Box<OperationCanceledException> RegisterCallbackForLoopTermination(ParallelOptions parallelOptions,
            ParallelLoopStateFlags sharedPStateFlags, out CancellationTokenRegistration ctr)
        {
            Box<OperationCanceledException> oce = new() { Value = null };

            // if cancellation is enabled, we need to register a callback to stop the loop when it gets signaled
            ctr = (!parallelOptions.CancellationToken.CanBeCanceled)
                ? default
                : parallelOptions.CancellationToken.UnsafeRegister((o) =>
                {
                    // Record our cancellation before stopping processing
                    oce.Value = new OperationCanceledException(parallelOptions.CancellationToken);
                    // Cause processing to stop
                    sharedPStateFlags.Cancel();
                }, state: null);
            return oce;
        }

        private static bool CheckTimeoutReached(int timeoutOccursAt)
        {
            // Note that both, Environment.TickCount and timeoutOccursAt are ints and can overflow and become negative.
            int currentMillis = Environment.TickCount;

            if (currentMillis < timeoutOccursAt)
                return false;

            if (0 > timeoutOccursAt && 0 < currentMillis)
                return false;

            return true;
        }


        private static int ComputeTimeoutPoint(int timeoutLength)
        {
            // Environment.TickCount is an int that cycles. We intentionally let the point in time at which the
            // timeout occurs overflow. It will still stay ahead of Environment.TickCount for the comparisons made
            // in CheckTimeoutReached(..):
            unchecked
            {
                return Environment.TickCount + timeoutLength;
            }
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

        private static int LogEtwEventInvokeBegin(Action[] actions)
        {
            int forkJoinContextID = 0;
            if (!ParallelEtwProvider.Log.IsEnabled()) return forkJoinContextID;

            forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
            ParallelEtwProvider.Log.ParallelInvokeBegin(TaskScheduler.Current.Id, Task.CurrentId ?? 0,
                forkJoinContextID, ParallelEtwProvider.ForkJoinOperationType.ParallelInvoke,
                actions.Length);

            return forkJoinContextID;
        }

        private static int LogEtwEventParallelLoopBegin<TIndex>(ParallelEtwProvider.ForkJoinOperationType OperationType, TIndex fromInclusive,
            TIndex toExclusive) where TIndex : INumber<TIndex>
        {
            // ETW event for Parallel For begin
            int forkJoinContextID = 0;
            if (!ParallelEtwProvider.Log.IsEnabled()) return forkJoinContextID;

            forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
            ParallelEtwProvider.Log.ParallelLoopBegin(TaskScheduler.Current.Id, Task.CurrentId ?? 0,
                forkJoinContextID, OperationType,
                fromInclusive, toExclusive);

            return forkJoinContextID;
        }

        private static void LogEtwEventParallelFork(int forkJoinContextID)
        {
            if (!ParallelEtwProvider.Log.IsEnabled()) return;

            ParallelEtwProvider.Log.ParallelFork(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID);
        }

        private static void LogEtwEventParallelJoin(int forkJoinContextID)
        {
            if (!ParallelEtwProvider.Log.IsEnabled()) return;

            ParallelEtwProvider.Log.ParallelJoin(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID);
        }

        private static void LogEtwEventParallelLoopEnd<TIndex>(int forkJoinContextID, TIndex nTotalIterations)
            where TIndex : INumber<TIndex>
        {
            ParallelEtwProvider.Log.ParallelLoopEnd(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID, nTotalIterations);
        }


        private static void LogEtwEventForParallelInvokeEnd(int forkJoinContextID)
        {
            if (!ParallelEtwProvider.Log.IsEnabled()) return;

            ParallelEtwProvider.Log.ParallelInvokeEnd(TaskScheduler.Current.Id, Task.CurrentId ?? 0, forkJoinContextID);
        }

        private static Action[] CopyActionArray(Action[] actions)
        {
            Action[] actionsCopy = new Action[actions.Length];
            for (int i = 0; i < actionsCopy.Length; i++)
            {
                if (actions[i] == null)
                {
                    throw new ArgumentException(SR.Parallel_Invoke_ActionNull);
                }

                actionsCopy[i] = actions[i];
            }

            return actionsCopy;
        }

        private static void SetLoopResultEndState(ParallelLoopStateFlags sharedPStateFlags, ref ParallelLoopResult result)
        {
            int sb_status = sharedPStateFlags.LoopStateFlags;
            result._completed = (sb_status == ParallelLoopStateFlags.ParallelLoopStateNone);
            if ((sb_status & ParallelLoopStateFlags.ParallelLoopStateBroken) != 0)
            {
                result._lowestBreakIteration = sharedPStateFlags.LowestBreakIteration;
            }
        }

        #endregion
    }
}
