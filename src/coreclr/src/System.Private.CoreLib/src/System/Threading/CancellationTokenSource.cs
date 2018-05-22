// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading
{
    /// <summary>Signals to a <see cref="CancellationToken"/> that it should be canceled.</summary>
    /// <remarks>
    /// <para>
    /// <see cref="CancellationTokenSource"/> is used to instantiate a <see cref="CancellationToken"/> (via
    /// the source's <see cref="Token">Token</see> property) that can be handed to operations that wish to be
    /// notified of cancellation or that can be used to register asynchronous operations for cancellation. That
    /// token may have cancellation requested by calling to the source's <see cref="Cancel()"/> method.
    /// </para>
    /// <para>
    /// All members of this class, except <see cref="Dispose"/>, are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </para>
    /// </remarks>
    public class CancellationTokenSource : IDisposable
    {
        /// <summary>A <see cref="CancellationTokenSource"/> that's already canceled.</summary>
        internal static readonly CancellationTokenSource s_canceledSource = new CancellationTokenSource() { _state = NotifyingCompleteState };
        /// <summary>A <see cref="CancellationTokenSource"/> that's never canceled.  This isn't enforced programmatically, only by usage.  Do not cancel!</summary>
        internal static readonly CancellationTokenSource s_neverCanceledSource = new CancellationTokenSource();

        /// <summary>Delegate used with <see cref="Timer"/> to trigger cancellation of a <see cref="CancellationTokenSource"/>.</summary>
        private static readonly TimerCallback s_timerCallback = obj =>
            ((CancellationTokenSource)obj).NotifyCancellation(throwOnFirstException: false); // skip ThrowIfDisposed() check in Cancel()

        /// <summary>The number of callback partitions to use in a <see cref="CancellationTokenSource"/>. Must be a power of 2.</summary>
        private static readonly int s_numPartitions = GetPartitionCount();
        /// <summary><see cref="s_numPartitions"/> - 1, used to quickly mod into <see cref="_callbackPartitions"/>.</summary>
        private static readonly int s_numPartitionsMask = s_numPartitions - 1;

        /// <summary>The current state of the CancellationTokenSource.</summary>
        private volatile int _state;
        /// <summary>The ID of the thread currently executing the main body of CTS.Cancel()</summary>
        /// <remarks>
        /// This helps us to know if a call to ctr.Dispose() is running 'within' a cancellation callback.
        /// This is updated as we move between the main thread calling cts.Cancel() and any syncContexts
        /// that are used to actually run the callbacks.
        /// </remarks>
        private volatile int _threadIDExecutingCallbacks = -1;
        /// <summary>Tracks the running callback to assist ctr.Dispose() to wait for the target callback to complete.</summary>
        private long _executingCallbackId;
        /// <summary>Partitions of callbacks.  Split into multiple partitions to help with scalability of registering/unregistering; each is protected by its own lock.</summary>
        private volatile CallbackPartition[] _callbackPartitions;
        /// <summary>Timer used by CancelAfter and Timer-related ctors.</summary>
        private volatile Timer _timer;
        /// <summary><see cref="System.Threading.WaitHandle"/> lazily initialized and returned from <see cref="WaitHandle"/>.</summary>
        private volatile ManualResetEvent _kernelEvent;
        /// <summary>Whether this <see cref="CancellationTokenSource"/> has been disposed.</summary>
        private bool _disposed;

        // legal values for _state
        private const int NotCanceledState = 1;
        private const int NotifyingState = 2;
        private const int NotifyingCompleteState = 3;

        /// <summary>Gets whether cancellation has been requested for this <see cref="CancellationTokenSource" />.</summary>
        /// <value>Whether cancellation has been requested for this <see cref="CancellationTokenSource" />.</value>
        /// <remarks>
        /// <para>
        /// This property indicates whether cancellation has been requested for this token source, such as
        /// due to a call to its <see cref="Cancel()"/> method.
        /// </para>
        /// <para>
        /// If this property returns true, it only guarantees that cancellation has been requested. It does not
        /// guarantee that every handler registered with the corresponding token has finished executing, nor
        /// that cancellation requests have finished propagating to all registered handlers. Additional
        /// synchronization may be required, particularly in situations where related objects are being
        /// canceled concurrently.
        /// </para>
        /// </remarks>
        public bool IsCancellationRequested => _state >= NotifyingState;

        /// <summary>A simple helper to determine whether cancellation has finished.</summary>
        internal bool IsCancellationCompleted => _state == NotifyingCompleteState;

        /// <summary>A simple helper to determine whether disposal has occurred.</summary>
        internal bool IsDisposed => _disposed;

        /// <summary>The ID of the thread that is running callbacks.</summary>
        internal int ThreadIDExecutingCallbacks
        {
            get => _threadIDExecutingCallbacks;
            set => _threadIDExecutingCallbacks = value;
        }

        /// <summary>Gets the <see cref="CancellationToken"/> associated with this <see cref="CancellationTokenSource"/>.</summary>
        /// <value>The <see cref="CancellationToken"/> associated with this <see cref="CancellationTokenSource"/>.</value>
        /// <exception cref="ObjectDisposedException">The token source has been disposed.</exception>
        public CancellationToken Token
        {
            get
            {
                ThrowIfDisposed();
                return new CancellationToken(this);
            }
        }

        internal WaitHandle WaitHandle
        {
            get
            {
                ThrowIfDisposed();

                // Return the handle if it was already allocated.
                if (_kernelEvent != null)
                {
                    return _kernelEvent;
                }

                // Lazily-initialize the handle.
                var mre = new ManualResetEvent(false);
                if (Interlocked.CompareExchange(ref _kernelEvent, mre, null) != null)
                {
                    mre.Dispose();
                }

                // There is a race condition between checking IsCancellationRequested and setting the event.
                // However, at this point, the kernel object definitely exists and the cases are:
                //   1. if IsCancellationRequested = true, then we will call Set()
                //   2. if IsCancellationRequested = false, then NotifyCancellation will see that the event exists, and will call Set().
                if (IsCancellationRequested)
                {
                    _kernelEvent.Set();
                }

                return _kernelEvent;
            }
        }


        /// <summary>Gets the ID of the currently executing callback.</summary>
        internal long ExecutingCallback => Volatile.Read(ref _executingCallbackId);

        /// <summary>Initializes the <see cref="CancellationTokenSource"/>.</summary>
        public CancellationTokenSource() => _state = NotCanceledState;

        /// <summary>
        /// Constructs a <see cref="CancellationTokenSource"/> that will be canceled after a specified time span.
        /// </summary>
        /// <param name="delay">The time span to wait before canceling this <see cref="CancellationTokenSource"/></param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The exception that is thrown when <paramref name="delay"/> is less than -1 or greater than Int32.MaxValue.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The countdown for the delay starts during the call to the constructor.  When the delay expires, 
        /// the constructed <see cref="CancellationTokenSource"/> is canceled, if it has
        /// not been canceled already.
        /// </para>
        /// <para>
        /// Subsequent calls to CancelAfter will reset the delay for the constructed 
        /// <see cref="CancellationTokenSource"/>, if it has not been
        /// canceled already.
        /// </para>
        /// </remarks>
        public CancellationTokenSource(TimeSpan delay)
        {
            long totalMilliseconds = (long)delay.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(delay));
            }

            InitializeWithTimer((int)totalMilliseconds);
        }

        /// <summary>
        /// Constructs a <see cref="CancellationTokenSource"/> that will be canceled after a specified time span.
        /// </summary>
        /// <param name="millisecondsDelay">The time span to wait before canceling this <see cref="CancellationTokenSource"/></param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The exception that is thrown when <paramref name="millisecondsDelay"/> is less than -1.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The countdown for the millisecondsDelay starts during the call to the constructor.  When the millisecondsDelay expires, 
        /// the constructed <see cref="CancellationTokenSource"/> is canceled (if it has
        /// not been canceled already).
        /// </para>
        /// <para>
        /// Subsequent calls to CancelAfter will reset the millisecondsDelay for the constructed 
        /// <see cref="CancellationTokenSource"/>, if it has not been
        /// canceled already.
        /// </para>
        /// </remarks>
        public CancellationTokenSource(int millisecondsDelay)
        {
            if (millisecondsDelay < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsDelay));
            }

            InitializeWithTimer(millisecondsDelay);
        }

        /// <summary>Common initialization logic when constructing a CTS with a delay parameter</summary>
        private void InitializeWithTimer(int millisecondsDelay)
        {
            _state = NotCanceledState;
            _timer = new Timer(s_timerCallback, this, millisecondsDelay, -1);
        }

        /// <summary>Communicates a request for cancellation.</summary>
        /// <remarks>
        /// <para>
        /// The associated <see cref="CancellationToken" /> will be notified of the cancellation
        /// and will transition to a state where <see cref="CancellationToken.IsCancellationRequested"/> returns true. 
        /// Any callbacks or cancelable operations registered with the <see cref="CancellationToken"/>  will be executed.
        /// </para>
        /// <para>
        /// Cancelable operations and callbacks registered with the token should not throw exceptions.
        /// However, this overload of Cancel will aggregate any exceptions thrown into a <see cref="AggregateException"/>,
        /// such that one callback throwing an exception will not prevent other registered callbacks from being executed.
        /// </para>
        /// <para>
        /// The <see cref="ExecutionContext"/> that was captured when each callback was registered
        /// will be reestablished when the callback is invoked.
        /// </para>
        /// </remarks>
        /// <exception cref="AggregateException">An aggregate exception containing all the exceptions thrown
        /// by the registered callbacks on the associated <see cref="CancellationToken"/>.</exception>
        /// <exception cref="ObjectDisposedException">This <see cref="CancellationTokenSource"/> has been disposed.</exception> 
        public void Cancel() => Cancel(false);

        /// <summary>Communicates a request for cancellation.</summary>
        /// <remarks>
        /// <para>
        /// The associated <see cref="CancellationToken" /> will be notified of the cancellation and will transition to a state where 
        /// <see cref="CancellationToken.IsCancellationRequested"/> returns true. Any callbacks or cancelable operationsregistered
        /// with the <see cref="CancellationToken"/>  will be executed.
        /// </para>
        /// <para>
        /// Cancelable operations and callbacks registered with the token should not throw exceptions. 
        /// If <paramref name="throwOnFirstException"/> is true, an exception will immediately propagate out of the
        /// call to Cancel, preventing the remaining callbacks and cancelable operations from being processed.
        /// If <paramref name="throwOnFirstException"/> is false, this overload will aggregate any 
        /// exceptions thrown into a <see cref="AggregateException"/>,
        /// such that one callback throwing an exception will not prevent other registered callbacks from being executed.
        /// </para>
        /// <para>
        /// The <see cref="ExecutionContext"/> that was captured when each callback was registered
        /// will be reestablished when the callback is invoked.
        /// </para>
        /// </remarks>
        /// <param name="throwOnFirstException">Specifies whether exceptions should immediately propagate.</param>
        /// <exception cref="AggregateException">An aggregate exception containing all the exceptions thrown
        /// by the registered callbacks on the associated <see cref="CancellationToken"/>.</exception>
        /// <exception cref="ObjectDisposedException">This <see cref="CancellationTokenSource"/> has been disposed.</exception> 
        public void Cancel(bool throwOnFirstException)
        {
            ThrowIfDisposed();
            NotifyCancellation(throwOnFirstException);
        }

        /// <summary>Schedules a Cancel operation on this <see cref="CancellationTokenSource"/>.</summary>
        /// <param name="delay">The time span to wait before canceling this <see cref="CancellationTokenSource"/>.
        /// </param>
        /// <exception cref="ObjectDisposedException">The exception thrown when this <see
        /// cref="CancellationTokenSource"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The exception thrown when <paramref name="delay"/> is less than -1 or 
        /// greater than Int32.MaxValue.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The countdown for the delay starts during this call.  When the delay expires, 
        /// this <see cref="CancellationTokenSource"/> is canceled, if it has
        /// not been canceled already.
        /// </para>
        /// <para>
        /// Subsequent calls to CancelAfter will reset the delay for this  
        /// <see cref="CancellationTokenSource"/>, if it has not been canceled already.
        /// </para>
        /// </remarks>
        public void CancelAfter(TimeSpan delay)
        {
            long totalMilliseconds = (long)delay.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(delay));
            }

            CancelAfter((int)totalMilliseconds);
        }

        /// <summary>
        /// Schedules a Cancel operation on this <see cref="CancellationTokenSource"/>.
        /// </summary>
        /// <param name="millisecondsDelay">The time span to wait before canceling this <see
        /// cref="CancellationTokenSource"/>.
        /// </param>
        /// <exception cref="ObjectDisposedException">The exception thrown when this <see
        /// cref="CancellationTokenSource"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The exception thrown when <paramref name="millisecondsDelay"/> is less than -1.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The countdown for the millisecondsDelay starts during this call.  When the millisecondsDelay expires, 
        /// this <see cref="CancellationTokenSource"/> is canceled, if it has
        /// not been canceled already.
        /// </para>
        /// <para>
        /// Subsequent calls to CancelAfter will reset the millisecondsDelay for this  
        /// <see cref="CancellationTokenSource"/>, if it has not been
        /// canceled already.
        /// </para>
        /// </remarks>
        public void CancelAfter(int millisecondsDelay)
        {
            ThrowIfDisposed();

            if (millisecondsDelay < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsDelay));
            }

            if (IsCancellationRequested)
            {
                return;
            }

            // There is a race condition here as a Cancel could occur between the check of
            // IsCancellationRequested and the creation of the timer.  This is benign; in the 
            // worst case, a timer will be created that has no effect when it expires.

            // Also, if Dispose() is called right here (after ThrowIfDisposed(), before timer
            // creation), it would result in a leaked Timer object (at least until the timer
            // expired and Disposed itself).  But this would be considered bad behavior, as
            // Dispose() is not thread-safe and should not be called concurrently with CancelAfter().

            if (_timer == null)
            {
                // Lazily initialize the timer in a thread-safe fashion.
                // Initially set to "never go off" because we don't want to take a
                // chance on a timer "losing" the initialization and then
                // cancelling the token before it (the timer) can be disposed.
                Timer newTimer = new Timer(s_timerCallback, this, -1, -1);
                if (Interlocked.CompareExchange(ref _timer, newTimer, null) != null)
                {
                    // We did not initialize the timer.  Dispose the new timer.
                    newTimer.Dispose();
                }
            }

            // It is possible that m_timer has already been disposed, so we must do
            // the following in a try/catch block.
            try
            {
                _timer.Change(millisecondsDelay, -1);
            }
            catch (ObjectDisposedException)
            {
                // Just eat the exception.  There is no other way to tell that
                // the timer has been disposed, and even if there were, there
                // would not be a good way to deal with the observe/dispose
                // race condition.
            }
        }



        /// <summary>Releases the resources used by this <see cref="CancellationTokenSource" />.</summary>
        /// <remarks>This method is not thread-safe for any other concurrent calls.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="CancellationTokenSource" /> class and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                // We specifically tolerate that a callback can be unregistered
                // after the CTS has been disposed and/or concurrently with cts.Dispose().
                // This is safe without locks because Dispose doesn't interact with values
                // in the callback partitions, only nulling out the ref to existing partitions.
                // 
                // We also tolerate that a callback can be registered after the CTS has been
                // disposed.  This is safe because InternalRegister is tolerant
                // of _callbackPartitions becoming null during its execution.  However,
                // we run the acceptable risk of _callbackPartitions getting reinitialized
                // to non-null if there is a race between Dispose and Register, in which case this
                // instance may unnecessarily hold onto a registered callback.  But that's no worse
                // than if Dispose wasn't safe to use concurrently, as Dispose would never be called,
                // and thus no handlers would be dropped.
                //
                // And, we tolerate Dispose being used concurrently with Cancel.  This is necessary
                // to properly support, e.g., LinkedCancellationTokenSource, where, due to common usage patterns,
                // it's possible for this pairing to occur with valid usage (e.g. a component accepts
                // an external CancellationToken and uses CreateLinkedTokenSource to combine it with an
                // internal source of cancellation, then Disposes of that linked source, which could
                // happen at the same time the external entity is requesting cancellation).

                _timer?.Dispose(); // Timer.Dispose is thread-safe

                _callbackPartitions = null; // free for GC; Cancel correctly handles a null field

                // If a kernel event was created via WaitHandle, we'd like to Dispose of it.  However,
                // we only want to do so if it's not being used by Cancel concurrently.  First, we
                // interlocked exchange it to be null, and then we check whether cancellation is currently
                // in progress.  NotifyCancellation will only try to set the event if it exists after it's
                // transitioned to and while it's in the NotifyingState.
                if (_kernelEvent != null)
                {
                    ManualResetEvent mre = Interlocked.Exchange(ref _kernelEvent, null);
                    if (mre != null && _state != NotifyingState)
                    {
                        mre.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>Throws an exception if the source has been disposed.</summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                ThrowObjectDisposedException();
            }
        }

        /// <summary>Throws an <see cref="ObjectDisposedException"/>.  Separated out from ThrowIfDisposed to help with inlining.</summary>
        private static void ThrowObjectDisposedException() =>
            throw new ObjectDisposedException(null, SR.CancellationTokenSource_Disposed);

        /// <summary>
        /// Registers a callback object. If cancellation has already occurred, the
        /// callback will have been run by the time this method returns.
        /// </summary>
        internal CancellationTokenRegistration InternalRegister(
            Action<object> callback, object stateForCallback, SynchronizationContext syncContext, ExecutionContext executionContext)
        {
            Debug.Assert(this != s_neverCanceledSource, "This source should never be exposed via a CancellationToken.");

            // If not canceled, register the handler; if canceled already, run the callback synchronously.
            // This also ensures that during ExecuteCallbackHandlers() there will be no mutation of the _callbackPartitions.
            if (!IsCancellationRequested)
            {
                // In order to enable code to not leak too many handlers, we allow Dispose to be called concurrently
                // with Register.  While this is not a recommended practice, consumers can and do use it this way.
                // We don't make any guarantees about whether the CTS will hold onto the supplied callback if the CTS
                // has already been disposed when the callback is registered, but we try not to while at the same time
                // not paying any non-negligible overhead.  The simple compromise is to check whether we're disposed
                // (not volatile), and if we see we are, to return an empty registration. If there's a race and _disposed
                // is false even though it's been disposed, or if the disposal request comes in after this line, we simply
                // run the minor risk of having _callbackPartitions reinitialized (after it was cleared to null during Dispose).
                if (_disposed)
                {
                    return new CancellationTokenRegistration();
                }

                // Get the partitions...
                CallbackPartition[] partitions = _callbackPartitions;
                if (partitions == null)
                {
                    partitions = new CallbackPartition[s_numPartitions];
                    partitions = Interlocked.CompareExchange(ref _callbackPartitions, partitions, null) ?? partitions;
                }

                // ...and determine which partition to use.
                int partitionIndex = Environment.CurrentManagedThreadId & s_numPartitionsMask;
                Debug.Assert(partitionIndex < partitions.Length, $"Expected {partitionIndex} to be less than {partitions.Length}");
                CallbackPartition partition = partitions[partitionIndex];
                if (partition == null)
                {
                    partition = new CallbackPartition(this);
                    partition = Interlocked.CompareExchange(ref partitions[partitionIndex], partition, null) ?? partition;
                }

                // Store the callback information into the callback arrays.
                long id;
                CallbackNode node;
                bool lockTaken = false;
                partition.Lock.Enter(ref lockTaken);
                try
                {
                    // Assign the next available unique ID.
                    id = partition.NextAvailableId++;

                    // Get a node, from the free list if possible or else a new one.
                    node = partition.FreeNodeList;
                    if (node != null)
                    {
                        partition.FreeNodeList = node.Next;
                        Debug.Assert(node.Prev == null, "Nodes in the free list should all have a null Prev");
                        // node.Next will be overwritten below so no need to set it here.
                    }
                    else
                    {
                        node = new CallbackNode(partition);
                    }

                    // Configure the node.
                    node.Id = id;
                    node.Callback = callback;
                    node.CallbackState = stateForCallback;
                    node.ExecutionContext = executionContext;
                    node.SynchronizationContext = syncContext;

                    // Add it to the callbacks list.
                    node.Next = partition.Callbacks;
                    if (node.Next != null)
                    {
                        node.Next.Prev = node;
                    }
                    partition.Callbacks = node;
                }
                finally
                {
                    partition.Lock.Exit(useMemoryBarrier: false); // no check on lockTaken needed without thread aborts
                }

                // If cancellation hasn't been requested, return the registration.
                // if cancellation has been requested, try to undo the registration and run the callback
                // ourselves, but if we can't unregister it (e.g. the thread running Cancel snagged
                // our callback for execution), return the registration so that the caller can wait
                // for callback completion in ctr.Dispose().
                var ctr = new CancellationTokenRegistration(id, node);
                if (!IsCancellationRequested || !partition.Unregister(id, node))
                {
                    return ctr;
                }
            }

            // Cancellation already occurred.  Run the callback on this thread and return an empty registration.
            callback(stateForCallback);
            return default;
        }

        private void NotifyCancellation(bool throwOnFirstException)
        {
            // If we're the first to signal cancellation, do the main extra work.
            if (!IsCancellationRequested && Interlocked.CompareExchange(ref _state, NotifyingState, NotCanceledState) == NotCanceledState)
            {
                // Dispose of the timer, if any.  Dispose may be running concurrently here, but Timer.Dispose is thread-safe.
                _timer?.Dispose();

                // Set the event if it's been lazily initialized and hasn't yet been disposed of.  Dispose may
                // be running concurrently, in which case either it'll have set m_kernelEvent back to null and
                // we won't see it here, or it'll see that we've transitioned to NOTIFYING and will skip disposing it,
                // leaving cleanup to finalization.
                _kernelEvent?.Set(); // update the MRE value.

                // - late enlisters to the Canceled event will have their callbacks called immediately in the Register() methods.
                // - Callbacks are not called inside a lock.
                // - After transition, no more delegates will be added to the 
                // - list of handlers, and hence it can be consumed and cleared at leisure by ExecuteCallbackHandlers.
                ExecuteCallbackHandlers(throwOnFirstException);
                Debug.Assert(IsCancellationCompleted, "Expected cancellation to have finished");
            }
        }

        /// <summary>Invoke all registered callbacks.</summary>
        /// <remarks>The handlers are invoked synchronously in LIFO order.</remarks>
        private void ExecuteCallbackHandlers(bool throwOnFirstException)
        {
            Debug.Assert(IsCancellationRequested, "ExecuteCallbackHandlers should only be called after setting IsCancellationRequested->true");

            // Record the threadID being used for running the callbacks.
            ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;

            // If there are no callbacks to run, we can safely exit.  Any race conditions to lazy initialize it
            // will see IsCancellationRequested and will then run the callback themselves.
            CallbackPartition[] partitions = Interlocked.Exchange(ref _callbackPartitions, null);
            if (partitions == null)
            {
                Interlocked.Exchange(ref _state, NotifyingCompleteState);
                return;
            }

            List<Exception> exceptionList = null;
            try
            {
                // For each partition, and each callback in that partition, execute the associated handler.
                // We call the delegates in LIFO order on each partition so that callbacks fire 'deepest first'.
                // This is intended to help with nesting scenarios so that child enlisters cancel before their parents.
                foreach (CallbackPartition partition in partitions)
                {
                    if (partition == null)
                    {
                        // Uninitialized partition. Nothing to do.
                        continue;
                    }

                    // Get the callbacks from the partition, substituting in null so that anyone
                    // else coming along (e.g. CTR.Dispose) will find the callbacks gone.
                    CallbackNode node;
                    bool lockTaken = false;
                    partition.Lock.Enter(ref lockTaken); // try/finally not needed without thread aborts
                    {
                        node = partition.Callbacks;
                        partition.Callbacks = null;
                    }
                    partition.Lock.Exit(useMemoryBarrier: false);

                    for (; node != null; node = node.Next)
                    {
                        // Publish the intended callback, to ensure ctr.Dispose can tell if a wait is necessary.
                        Volatile.Write(ref _executingCallbackId, node.Id);

                        // Invoke the callback on this thread if there's no sync context or on the
                        // target sync context if there is one.
                        try
                        {
                            if (node.SynchronizationContext != null)
                            {
                                // Transition to the target syncContext and continue there.
                                node.SynchronizationContext.Send(s =>
                                {
                                    var n = (CallbackNode)s;
                                    n.Partition.Source.ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
                                    n.ExecuteCallback();
                                }, node);
                                ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId; // above may have altered ThreadIDExecutingCallbacks, so reset it
                            }
                            else
                            {
                                node.ExecuteCallback();
                            }
                        }
                        catch (Exception ex) when (!throwOnFirstException)
                        {
                            // Store the exception and continue
                            (exceptionList ?? (exceptionList = new List<Exception>())).Add(ex);
                        }
                    }
                }
            }
            finally
            {
                _state = NotifyingCompleteState;
                Volatile.Write(ref _executingCallbackId, 0);
                Interlocked.MemoryBarrier(); // for safety, prevent reorderings crossing this point and seeing inconsistent state.
            }

            if (exceptionList != null)
            {
                Debug.Assert(exceptionList.Count > 0, $"Expected {exceptionList.Count} > 0");
                throw new AggregateException(exceptionList);
            }
        }

        /// <summary>Gets the number of callback partitions to use based on the number of cores.</summary>
        /// <returns>A power of 2 representing the number of partitions to use.</returns>
        private static int GetPartitionCount()
        {
            int procs = PlatformHelper.ProcessorCount;
            int count =
                procs > 8 ? 16 : // capped at 16 to limit memory usage on larger machines
                procs > 4 ? 8 :
                procs > 2 ? 4 :
                procs > 1 ? 2 :
                1;
            Debug.Assert(count > 0 && (count & (count - 1)) == 0, $"Got {count}, but expected a power of 2");
            return count;
        }

        /// <summary>
        /// Creates a <see cref="CancellationTokenSource"/> that will be in the canceled state
        /// when any of the source tokens are in the canceled state.
        /// </summary>
        /// <param name="token1">The first <see cref="CancellationToken">CancellationToken</see> to observe.</param>
        /// <param name="token2">The second <see cref="CancellationToken">CancellationToken</see> to observe.</param>
        /// <returns>A <see cref="CancellationTokenSource"/> that is linked 
        /// to the source tokens.</returns>
        public static CancellationTokenSource CreateLinkedTokenSource(CancellationToken token1, CancellationToken token2) =>
            !token1.CanBeCanceled ? CreateLinkedTokenSource(token2) :
            token2.CanBeCanceled ? new Linked2CancellationTokenSource(token1, token2) :
            (CancellationTokenSource)new Linked1CancellationTokenSource(token1);

        /// <summary>
        /// Creates a <see cref="CancellationTokenSource"/> that will be in the canceled state
        /// when any of the source tokens are in the canceled state.
        /// </summary>
        /// <param name="token">The first <see cref="CancellationToken">CancellationToken</see> to observe.</param>
        /// <returns>A <see cref="CancellationTokenSource"/> that is linked to the source tokens.</returns>
        internal static CancellationTokenSource CreateLinkedTokenSource(CancellationToken token) =>
            token.CanBeCanceled ? new Linked1CancellationTokenSource(token) : new CancellationTokenSource();

        /// <summary>
        /// Creates a <see cref="CancellationTokenSource"/> that will be in the canceled state
        /// when any of the source tokens are in the canceled state.
        /// </summary>
        /// <param name="tokens">The <see cref="CancellationToken">CancellationToken</see> instances to observe.</param>
        /// <returns>A <see cref="CancellationTokenSource"/> that is linked to the source tokens.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="tokens"/> is null.</exception>
        public static CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            switch (tokens.Length)
            {
                case 0:
                    throw new ArgumentException(SR.CancellationToken_CreateLinkedToken_TokensIsEmpty);
                case 1:
                    return CreateLinkedTokenSource(tokens[0]);
                case 2:
                    return CreateLinkedTokenSource(tokens[0], tokens[1]);
                default:
                    // a defensive copy is not required as the array has value-items that have only a single reference field,
                    // hence each item cannot be null itself, and reads of the payloads cannot be torn.
                    return new LinkedNCancellationTokenSource(tokens);
            }
        }



        /// <summary>
        /// Wait for a single callback to complete (or, more specifically, to not be running).
        /// It is ok to call this method if the callback has already finished.
        /// Calling this method before the target callback has been selected for execution would be an error.
        /// </summary>
        internal void WaitForCallbackToComplete(long id)
        {
            var sw = new SpinWait();
            while (ExecutingCallback == id)
            {
                sw.SpinOnce();  // spin, as we assume callback execution is fast and that this situation is rare.
            }
        }

        private sealed class Linked1CancellationTokenSource : CancellationTokenSource
        {
            private readonly CancellationTokenRegistration _reg1;

            internal Linked1CancellationTokenSource(CancellationToken token1)
            {
                _reg1 = token1.InternalRegisterWithoutEC(LinkedNCancellationTokenSource.s_linkedTokenCancelDelegate, this);
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing || _disposed)
                {
                    return;
                }

                _reg1.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class Linked2CancellationTokenSource : CancellationTokenSource
        {
            private readonly CancellationTokenRegistration _reg1;
            private readonly CancellationTokenRegistration _reg2;

            internal Linked2CancellationTokenSource(CancellationToken token1, CancellationToken token2)
            {
                _reg1 = token1.InternalRegisterWithoutEC(LinkedNCancellationTokenSource.s_linkedTokenCancelDelegate, this);
                _reg2 = token2.InternalRegisterWithoutEC(LinkedNCancellationTokenSource.s_linkedTokenCancelDelegate, this);
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing || _disposed)
                {
                    return;
                }

                _reg1.Dispose();
                _reg2.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class LinkedNCancellationTokenSource : CancellationTokenSource
        {
            internal static readonly Action<object> s_linkedTokenCancelDelegate =
                s => ((CancellationTokenSource)s).NotifyCancellation(throwOnFirstException: false); // skip ThrowIfDisposed() check in Cancel()
            private CancellationTokenRegistration[] m_linkingRegistrations;

            internal LinkedNCancellationTokenSource(params CancellationToken[] tokens)
            {
                m_linkingRegistrations = new CancellationTokenRegistration[tokens.Length];

                for (int i = 0; i < tokens.Length; i++)
                {
                    if (tokens[i].CanBeCanceled)
                    {
                        m_linkingRegistrations[i] = tokens[i].InternalRegisterWithoutEC(s_linkedTokenCancelDelegate, this);
                    }
                    // Empty slots in the array will be default(CancellationTokenRegistration), which are nops to Dispose.
                    // Based on usage patterns, such occurrences should also be rare, such that it's not worth resizing
                    // the array and incurring the related costs.
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing || _disposed)
                {
                    return;
                }

                CancellationTokenRegistration[] linkingRegistrations = m_linkingRegistrations;
                if (linkingRegistrations != null)
                {
                    m_linkingRegistrations = null; // release for GC once we're done enumerating
                    for (int i = 0; i < linkingRegistrations.Length; i++)
                    {
                        linkingRegistrations[i].Dispose();
                    }
                }

                base.Dispose(disposing);
            }
        }

        internal sealed class CallbackPartition
        {
            /// <summary>The associated source that owns this partition.</summary>
            public readonly CancellationTokenSource Source;
            /// <summary>Lock that protects all state in the partition.</summary>
            public SpinLock Lock = new SpinLock(enableThreadOwnerTracking: false); // mutable struct; do not make this readonly
            /// <summary>
            /// The array of callbacks registered in the partition.  Slots may be empty, meaning a default value of the struct.
            /// <see cref="NextCallbacksSlot"/> - 1 defines the last filled slot.
            /// </summary>
            /// <remarks>
            /// Initialized to an array with at least 1 slot because a partition is only ever created if we're about
            /// to store something into it.  And initialized with at most 1 slot to help optimize the common case where
            /// there's only ever a single registration in a CTS (that and many registrations are the most common cases).
            /// </remarks>
            public CallbackNode Callbacks;
            public CallbackNode FreeNodeList;
            /// <summary>
            /// Every callback is assigned a unique, never-reused ID.  This defines the next available ID.
            /// </summary>
            public long NextAvailableId = 1; // avoid using 0, as that's the default long value and used to represent an empty slot

            public CallbackPartition(CancellationTokenSource source)
            {
                Debug.Assert(source != null, "Expected non-null source");
                Source = source;
            }

            internal bool Unregister(long id, CallbackNode node)
            {
                Debug.Assert(id != 0, "Expected non-zero id");
                Debug.Assert(node != null, "Expected non-null node");

                bool lockTaken = false;
                Lock.Enter(ref lockTaken);
                try
                {
                    if (Callbacks == null || node.Id != id)
                    {
                        // Cancellation was already requested or the callback was already disposed.
                        // Even though we have the node itself, it's important to check Callbacks
                        // in order to synchronize with callback execution.
                        return false;
                    }

                    // Remove the registration from the list.
                    if (node.Prev != null) node.Prev.Next = node.Next;
                    if (node.Next != null) node.Next.Prev = node.Prev;
                    if (Callbacks == node) Callbacks = node.Next;

                    // Clear it out and put it on the free list
                    node.Clear();
                    node.Prev = null;
                    node.Next = FreeNodeList;
                    FreeNodeList = node;

                    return true;
                }
                finally
                {
                    Lock.Exit(useMemoryBarrier: false); // no check on lockTaken needed without thread aborts
                }
            }
        }

        /// <summary>All of the state associated a registered callback, in a node that's part of a linked list of registered callbacks.</summary>
        internal sealed class CallbackNode
        {
            public readonly CallbackPartition Partition;
            public CallbackNode Prev;
            public CallbackNode Next;

            public long Id;
            public Action<object> Callback;
            public object CallbackState;
            public ExecutionContext ExecutionContext;
            public SynchronizationContext SynchronizationContext;
            
            public CallbackNode(CallbackPartition partition)
            {
                Debug.Assert(partition != null, "Expected non-null partition");
                Partition = partition;
            }

            public void Clear()
            {
                Id = 0;
                Callback = null;
                CallbackState = null;
                ExecutionContext = null;
                SynchronizationContext = null;
            }

            public void ExecuteCallback()
            {
                ExecutionContext context = ExecutionContext;
                if (context != null)
                {
                    ExecutionContext.RunInternal(context, s =>
                    {
                        CallbackNode n = (CallbackNode)s;
                        n.Callback(n.CallbackState);
                    }, this);
                }
                else
                {
                    Callback(CallbackState);
                }
            }
        }
    }
}
