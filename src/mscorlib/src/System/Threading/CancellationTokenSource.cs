// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma warning disable 0420

//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Security;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Diagnostics.Contracts;
using System.Runtime;

namespace System.Threading
{
    /// <summary>
    /// Signals to a <see cref="System.Threading.CancellationToken"/> that it should be canceled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="T:System.Threading.CancellationTokenSource"/> is used to instantiate a <see
    /// cref="T:System.Threading.CancellationToken"/>
    /// (via the source's <see cref="System.Threading.CancellationTokenSource.Token">Token</see> property)
    /// that can be handed to operations that wish to be notified of cancellation or that can be used to
    /// register asynchronous operations for cancellation. That token may have cancellation requested by
    /// calling to the source's <see cref="System.Threading.CancellationTokenSource.Cancel()">Cancel</see>
    /// method.
    /// </para>
    /// <para>
    /// All members of this class, except <see cref="Dispose">Dispose</see>, are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </para>
    /// </remarks>
    [ComVisible(false)]
    [HostProtection(Synchronization = true, ExternalThreading = true)]

    public class CancellationTokenSource : IDisposable
    {
        //static sources that can be used as the backing source for 'fixed' CancellationTokens that never change state.
        private static readonly CancellationTokenSource _staticSource_Set = new CancellationTokenSource(true);
        private static readonly CancellationTokenSource _staticSource_NotCancelable = new CancellationTokenSource(false);

        //Note: the callback lists array is only created on first registration.
        //      the actual callback lists are only created on demand.
        //      Storing a registered callback costs around >60bytes, hence some overhead for the lists array is OK
        // At most 24 lists seems reasonable, and caps the cost of the listsArray to 96bytes(32-bit,24-way) or 192bytes(64-bit,24-way).
        private static readonly int s_nLists = (PlatformHelper.ProcessorCount > 24) ? 24 : PlatformHelper.ProcessorCount; 

        private volatile ManualResetEvent m_kernelEvent; //lazily initialized if required.

        private volatile SparselyPopulatedArray<CancellationCallbackInfo>[] m_registeredCallbacksLists;
 
        // legal values for m_state
        private const int CANNOT_BE_CANCELED = 0;
        private const int NOT_CANCELED = 1;
        private const int NOTIFYING = 2;
        private const int NOTIFYINGCOMPLETE = 3;
        
        //m_state uses the pattern "volatile int32 reads, with cmpxch writes" which is safe for updates and cannot suffer torn reads.
        private volatile int m_state;


        /// The ID of the thread currently executing the main body of CTS.Cancel()
        /// this helps us to know if a call to ctr.Dispose() is running 'within' a cancellation callback.
        /// This is updated as we move between the main thread calling cts.Cancel() and any syncContexts that are used to 
        /// actually run the callbacks.
        private volatile int m_threadIDExecutingCallbacks = -1;

        private bool m_disposed;
        
        // we track the running callback to assist ctr.Dispose() to wait for the target callback to complete.
        private volatile CancellationCallbackInfo m_executingCallback;

        // provided for CancelAfter and timer-related constructors
        private volatile Timer m_timer;
        
        // ---------------------- 
        // ** public properties

        /// <summary>
        /// Gets whether cancellation has been requested for this <see
        /// cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see>.
        /// </summary>
        /// <value>Whether cancellation has been requested for this <see
        /// cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see>.</value>
        /// <remarks>
        /// <para>
        /// This property indicates whether cancellation has been requested for this token source, such as
        /// due to a call to its
        /// <see cref="System.Threading.CancellationTokenSource.Cancel()">Cancel</see> method.
        /// </para>
        /// <para>
        /// If this property returns true, it only guarantees that cancellation has been requested. It does not
        /// guarantee that every handler registered with the corresponding token has finished executing, nor
        /// that cancellation requests have finished propagating to all registered handlers. Additional
        /// synchronization may be required, particularly in situations where related objects are being
        /// canceled concurrently.
        /// </para>
        /// </remarks>
        public bool IsCancellationRequested
        {
            get { return m_state >= NOTIFYING; }
        }

        /// <summary>
        /// A simple helper to determine whether cancellation has finished.
        /// </summary>
        internal bool IsCancellationCompleted
        {
            get { return m_state == NOTIFYINGCOMPLETE; }
        }

        /// <summary>
        /// A simple helper to determine whether disposal has occurred.
        /// </summary>
        internal bool IsDisposed
        {
            get { return m_disposed; }
        }

        /// <summary>
        /// The ID of the thread that is running callbacks.
        /// </summary>
        internal int ThreadIDExecutingCallbacks
        {
            set { m_threadIDExecutingCallbacks = value; }
            get { return m_threadIDExecutingCallbacks; }
        }

        /// <summary>
        /// Gets the <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// associated with this <see cref="CancellationTokenSource"/>.
        /// </summary>
        /// <value>The <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// associated with this <see cref="CancellationTokenSource"/>.</value>
        /// <exception cref="T:System.ObjectDisposedException">The token source has been
        /// disposed.</exception>
        public CancellationToken Token
        {
            get
            {
                ThrowIfDisposed();
                return new CancellationToken(this);
            }
        }

        // ---------------------- 
        // ** internal and private properties.

        /// <summary>
        ///
        /// </summary>
        internal bool CanBeCanceled
        {
            get { return m_state != CANNOT_BE_CANCELED; }
        }

        /// <summary>
        ///
        /// </summary>
        internal WaitHandle WaitHandle
        {
            get
            {
                ThrowIfDisposed();

                // fast path if already allocated.
                if (m_kernelEvent != null)
                    return m_kernelEvent;
                
                // lazy-init the mre.
                ManualResetEvent mre = new ManualResetEvent(false);
                if (Interlocked.CompareExchange(ref m_kernelEvent, mre, null) != null)
                {    
                    ((IDisposable)mre).Dispose();
                }

                // There is a race condition between checking IsCancellationRequested and setting the event.
                // However, at this point, the kernel object definitely exists and the cases are:
                //   1. if IsCancellationRequested = true, then we will call Set()
                //   2. if IsCancellationRequested = false, then NotifyCancellation will see that the event exists, and will call Set().
                if (IsCancellationRequested)
                    m_kernelEvent.Set();

                return m_kernelEvent;
            }
        }


        /// <summary>
        /// The currently executing callback
        /// </summary>
        internal CancellationCallbackInfo ExecutingCallback
        {
            get { return m_executingCallback; }
        }

#if DEBUG
        /// <summary>
        /// Used by the dev unit tests to check the number of outstanding registrations.
        /// They use private reflection to gain access.  Because this would be dead retail
        /// code, however, it is ifdef'd out to work only in debug builds.
        /// </summary>
        private int CallbackCount
        {
            get
            {
                SparselyPopulatedArray<CancellationCallbackInfo>[] callbackLists = m_registeredCallbacksLists;
                if (callbackLists == null)
                    return 0;

                int count = 0;
                foreach(SparselyPopulatedArray<CancellationCallbackInfo> sparseArray in callbackLists)
                {
                    if(sparseArray != null)
                    {
                        SparselyPopulatedArrayFragment<CancellationCallbackInfo> currCallbacks = sparseArray.Head;
                        while (currCallbacks != null)
                        {
                            for (int i = 0; i < currCallbacks.Length; i++)
                                if (currCallbacks[i] != null)
                                    count++;

                            currCallbacks = currCallbacks.Next;
                        }
                    }
                }
                return count;
            }
        }
#endif

        // ** Public Constructors

        /// <summary>
        /// Initializes the <see cref="T:System.Threading.CancellationTokenSource"/>.
        /// </summary>
        public CancellationTokenSource()
        {
            m_state = NOT_CANCELED;
        }

        // ** Private constructors for static sources.
        // set=false ==> cannot be canceled.
        // set=true  ==> is canceled. 
        private CancellationTokenSource(bool set)
        {
            m_state = set ? NOTIFYINGCOMPLETE : CANNOT_BE_CANCELED;
        }

        /// <summary>
        /// Constructs a <see cref="T:System.Threading.CancellationTokenSource"/> that will be canceled after a specified time span.
        /// </summary>
        /// <param name="delay">The time span to wait before canceling this <see cref="T:System.Threading.CancellationTokenSource"/></param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The exception that is thrown when <paramref name="delay"/> is less than -1 or greater than Int32.MaxValue.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The countdown for the delay starts during the call to the constructor.  When the delay expires, 
        /// the constructed <see cref="T:System.Threading.CancellationTokenSource"/> is canceled, if it has
        /// not been canceled already.
        /// </para>
        /// <para>
        /// Subsequent calls to CancelAfter will reset the delay for the constructed 
        /// <see cref="T:System.Threading.CancellationTokenSource"/>, if it has not been
        /// canceled already.
        /// </para>
        /// </remarks>
        public CancellationTokenSource(TimeSpan delay)
        {
            long totalMilliseconds = (long)delay.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("delay");
            }

            InitializeWithTimer((int)totalMilliseconds);
        }

        /// <summary>
        /// Constructs a <see cref="T:System.Threading.CancellationTokenSource"/> that will be canceled after a specified time span.
        /// </summary>
        /// <param name="millisecondsDelay">The time span to wait before canceling this <see cref="T:System.Threading.CancellationTokenSource"/></param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The exception that is thrown when <paramref name="millisecondsDelay"/> is less than -1.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The countdown for the millisecondsDelay starts during the call to the constructor.  When the millisecondsDelay expires, 
        /// the constructed <see cref="T:System.Threading.CancellationTokenSource"/> is canceled (if it has
        /// not been canceled already).
        /// </para>
        /// <para>
        /// Subsequent calls to CancelAfter will reset the millisecondsDelay for the constructed 
        /// <see cref="T:System.Threading.CancellationTokenSource"/>, if it has not been
        /// canceled already.
        /// </para>
        /// </remarks>
        public CancellationTokenSource(Int32 millisecondsDelay)
        {
            if (millisecondsDelay < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay");
            }

            InitializeWithTimer(millisecondsDelay);
        }

        // Common initialization logic when constructing a CTS with a delay parameter
        private void InitializeWithTimer(Int32 millisecondsDelay)
        {
            m_state = NOT_CANCELED;
            m_timer = new Timer(s_timerCallback, this, millisecondsDelay, -1);
        }

        // ** Public Methods

        /// <summary>
        /// Communicates a request for cancellation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The associated <see cref="T:System.Threading.CancellationToken" /> will be
        /// notified of the cancellation and will transition to a state where 
        /// <see cref="System.Threading.CancellationToken.IsCancellationRequested">IsCancellationRequested</see> returns true. 
        /// Any callbacks or cancelable operations
        /// registered with the <see cref="T:System.Threading.CancellationToken"/>  will be executed.
        /// </para>
        /// <para>
        /// Cancelable operations and callbacks registered with the token should not throw exceptions.
        /// However, this overload of Cancel will aggregate any exceptions thrown into a <see cref="System.AggregateException"/>,
        /// such that one callback throwing an exception will not prevent other registered callbacks from being executed.
        /// </para>
        /// <para>
        /// The <see cref="T:System.Threading.ExecutionContext"/> that was captured when each callback was registered
        /// will be reestablished when the callback is invoked.
        /// </para>
        /// </remarks>
        /// <exception cref="T:System.AggregateException">An aggregate exception containing all the exceptions thrown
        /// by the registered callbacks on the associated <see cref="T:System.Threading.CancellationToken"/>.</exception>
        /// <exception cref="T:System.ObjectDisposedException">This <see
        /// cref="T:System.Threading.CancellationTokenSource"/> has been disposed.</exception> 
        public void Cancel()
        {
            Cancel(false);
        }

        /// <summary>
        /// Communicates a request for cancellation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The associated <see cref="T:System.Threading.CancellationToken" /> will be
        /// notified of the cancellation and will transition to a state where 
        /// <see cref="System.Threading.CancellationToken.IsCancellationRequested">IsCancellationRequested</see> returns true. 
        /// Any callbacks or cancelable operations
        /// registered with the <see cref="T:System.Threading.CancellationToken"/>  will be executed.
        /// </para>
        /// <para>
        /// Cancelable operations and callbacks registered with the token should not throw exceptions. 
        /// If <paramref name="throwOnFirstException"/> is true, an exception will immediately propagate out of the
        /// call to Cancel, preventing the remaining callbacks and cancelable operations from being processed.
        /// If <paramref name="throwOnFirstException"/> is false, this overload will aggregate any 
        /// exceptions thrown into a <see cref="System.AggregateException"/>,
        /// such that one callback throwing an exception will not prevent other registered callbacks from being executed.
        /// </para>
        /// <para>
        /// The <see cref="T:System.Threading.ExecutionContext"/> that was captured when each callback was registered
        /// will be reestablished when the callback is invoked.
        /// </para>
        /// </remarks>
        /// <param name="throwOnFirstException">Specifies whether exceptions should immediately propagate.</param>
        /// <exception cref="T:System.AggregateException">An aggregate exception containing all the exceptions thrown
        /// by the registered callbacks on the associated <see cref="T:System.Threading.CancellationToken"/>.</exception>
        /// <exception cref="T:System.ObjectDisposedException">This <see
        /// cref="T:System.Threading.CancellationTokenSource"/> has been disposed.</exception> 
        public void Cancel(bool throwOnFirstException)
        {
            ThrowIfDisposed();
            NotifyCancellation(throwOnFirstException);            
        }

        /// <summary>
        /// Schedules a Cancel operation on this <see cref="T:System.Threading.CancellationTokenSource"/>.
        /// </summary>
        /// <param name="delay">The time span to wait before canceling this <see
        /// cref="T:System.Threading.CancellationTokenSource"/>.
        /// </param>
        /// <exception cref="T:System.ObjectDisposedException">The exception thrown when this <see
        /// cref="T:System.Threading.CancellationTokenSource"/> has been disposed.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The exception thrown when <paramref name="delay"/> is less than -1 or 
        /// greater than Int32.MaxValue.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The countdown for the delay starts during this call.  When the delay expires, 
        /// this <see cref="T:System.Threading.CancellationTokenSource"/> is canceled, if it has
        /// not been canceled already.
        /// </para>
        /// <para>
        /// Subsequent calls to CancelAfter will reset the delay for this  
        /// <see cref="T:System.Threading.CancellationTokenSource"/>, if it has not been
        /// canceled already.
        /// </para>
        /// </remarks>
        public void CancelAfter(TimeSpan delay)
        {
            long totalMilliseconds = (long)delay.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("delay");
            }

            CancelAfter((int)totalMilliseconds);
        }

        /// <summary>
        /// Schedules a Cancel operation on this <see cref="T:System.Threading.CancellationTokenSource"/>.
        /// </summary>
        /// <param name="millisecondsDelay">The time span to wait before canceling this <see
        /// cref="T:System.Threading.CancellationTokenSource"/>.
        /// </param>
        /// <exception cref="T:System.ObjectDisposedException">The exception thrown when this <see
        /// cref="T:System.Threading.CancellationTokenSource"/> has been disposed.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The exception thrown when <paramref name="millisecondsDelay"/> is less than -1.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The countdown for the millisecondsDelay starts during this call.  When the millisecondsDelay expires, 
        /// this <see cref="T:System.Threading.CancellationTokenSource"/> is canceled, if it has
        /// not been canceled already.
        /// </para>
        /// <para>
        /// Subsequent calls to CancelAfter will reset the millisecondsDelay for this  
        /// <see cref="T:System.Threading.CancellationTokenSource"/>, if it has not been
        /// canceled already.
        /// </para>
        /// </remarks>
        public void CancelAfter(Int32 millisecondsDelay)
        {
            ThrowIfDisposed();

            if (millisecondsDelay < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay");
            }

            if (IsCancellationRequested) return;

            // There is a race condition here as a Cancel could occur between the check of
            // IsCancellationRequested and the creation of the timer.  This is benign; in the 
            // worst case, a timer will be created that has no effect when it expires.

            // Also, if Dispose() is called right here (after ThrowIfDisposed(), before timer
            // creation), it would result in a leaked Timer object (at least until the timer
            // expired and Disposed itself).  But this would be considered bad behavior, as
            // Dispose() is not thread-safe and should not be called concurrently with CancelAfter().

            if (m_timer == null)
            {
                // Lazily initialize the timer in a thread-safe fashion.
                // Initially set to "never go off" because we don't want to take a
                // chance on a timer "losing" the initialization and then
                // cancelling the token before it (the timer) can be disposed.
                Timer newTimer = new Timer(s_timerCallback, this, -1, -1);
                if (Interlocked.CompareExchange(ref m_timer, newTimer, null) != null)
                {
                    // We did not initialize the timer.  Dispose the new timer.
                    newTimer.Dispose();
                }
            }

            
            // It is possible that m_timer has already been disposed, so we must do
            // the following in a try/catch block.
            try
            {
                m_timer.Change(millisecondsDelay, -1);
            }
            catch (ObjectDisposedException)
            {
                // Just eat the exception.  There is no other way to tell that
                // the timer has been disposed, and even if there were, there
                // would not be a good way to deal with the observe/dispose
                // race condition.
            }

        }

        private static readonly TimerCallback s_timerCallback = new TimerCallback(TimerCallbackLogic);

        // Common logic for a timer delegate
        private static void TimerCallbackLogic(object obj)
        {
            CancellationTokenSource cts = (CancellationTokenSource)obj;

            // Cancel the source; handle a race condition with cts.Dispose()
            if (!cts.IsDisposed)
            {
                // There is a small window for a race condition where a cts.Dispose can sneak
                // in right here.  I'll wrap the cts.Cancel() in a try/catch to proof us
                // against this race condition.
                try
                {
                    cts.Cancel(); // will take care of disposing of m_timer
                }
                catch (ObjectDisposedException)
                {
                    // If the ODE was not due to the target cts being disposed, then propagate the ODE.
                    if (!cts.IsDisposed) throw;
                }
            }
        }

        /// <summary>
        /// Releases the resources used by this <see cref="T:System.Threading.CancellationTokenSource" />.
        /// </summary>
        /// <remarks>
        /// This method is not thread-safe for any other concurrent calls.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Threading.CancellationTokenSource" /> class and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // There is nothing to do if disposing=false because the CancellationTokenSource holds no unmanaged resources.

            if (disposing && !m_disposed)
            {
                //NOTE: We specifically tolerate that a callback can be deregistered
                //      after the CTS has been disposed and/or concurrently with cts.Dispose().
                //      This is safe without locks because the reg.Dispose() only
                //      mutates a sparseArrayFragment and then reads from properties of the CTS that are not
                //      invalidated by cts.Dispose().
                //     
                //      We also tolerate that a callback can be registered after the CTS has been
                //      disposed.  This is safe without locks because InternalRegister is tolerant
                //      of m_registeredCallbacksLists becoming null during its execution.  However,
                //      we run the acceptable risk of m_registeredCallbacksLists getting reinitialized
                //      to non-null if there is a race between Dispose and Register, in which case this
                //      instance may unnecessarily hold onto a registered callback.  But that's no worse
                //      than if Dispose wasn't safe to use concurrently, as Dispose would never be called,
                //      and thus no handlers would be dropped.
                //
                //      And, we tolerate Dispose being used concurrently with Cancel.  This is necessary
                //      to properly support LinkedCancellationTokenSource, where, due to common usage patterns,
                //      it's possible for this pairing to occur with valid usage (e.g. a component accepts
                //      an external CancellationToken and uses CreateLinkedTokenSource to combine it with an
                //      internal source of cancellation, then Disposes of that linked source, which could
                //      happen at the same time the external entity is requesting cancellation).

                m_timer?.Dispose(); // Timer.Dispose is thread-safe

                // registered callbacks are now either complete or will never run, due to guarantees made by ctr.Dispose()
                // so we can now perform main disposal work without risk of linking callbacks trying to use this CTS.

                m_registeredCallbacksLists = null; // free for GC; Cancel correctly handles a null field

                // If a kernel event was created via WaitHandle, we'd like to Dispose of it.  However,
                // we only want to do so if it's not being used by Cancel concurrently.  First, we
                // interlocked exchange it to be null, and then we check whether cancellation is currently
                // in progress.  NotifyCancellation will only try to set the event if it exists after it's
                // transitioned to and while it's in the NOTIFYING state.
                if (m_kernelEvent != null)
                {
                    ManualResetEvent mre = Interlocked.Exchange(ref m_kernelEvent, null);
                    if (mre != null && m_state != NOTIFYING)
                    {
                        mre.Dispose();
                    }
                }

                m_disposed = true;
            }
        }

        // -- Internal methods.

        /// <summary>
        /// Throws an exception if the source has been disposed.
        /// </summary>
        internal void ThrowIfDisposed()
        {
            if (m_disposed)
                ThrowObjectDisposedException();
        }

        // separation enables inlining of ThrowIfDisposed
        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(null, Environment.GetResourceString("CancellationTokenSource_Disposed"));
        }

        /// <summary>
        /// InternalGetStaticSource()
        /// </summary>
        /// <param name="set">Whether the source should be set.</param>
        /// <returns>A static source to be shared among multiple tokens.</returns>
        internal static CancellationTokenSource InternalGetStaticSource(bool set)
        {
            return set ? _staticSource_Set : _staticSource_NotCancelable;
        }

        /// <summary>
        /// Registers a callback object. If cancellation has already occurred, the
        /// callback will have been run by the time this method returns.
        /// </summary>
        internal CancellationTokenRegistration InternalRegister(
            Action<object> callback, object stateForCallback, SynchronizationContext targetSyncContext, ExecutionContext executionContext)
        {
            if (AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
            {
                ThrowIfDisposed();
            }

            // the CancellationToken has already checked that the token is cancelable before calling this method.
            Contract.Assert(CanBeCanceled, "Cannot register for uncancelable token src");

            // if not canceled, register the event handlers
            // if canceled already, run the callback synchronously
            // Apart from the semantics of late-enlistment, this also ensures that during ExecuteCallbackHandlers() there
            // will be no mutation of the _registeredCallbacks list

            if (!IsCancellationRequested)
            {
                // In order to enable code to not leak too many handlers, we allow Dispose to be called concurrently
                // with Register.  While this is not a recommended practice, consumers can and do use it this way.
                // We don't make any guarantees about whether the CTS will hold onto the supplied callback
                // if the CTS has already been disposed when the callback is registered, but we try not to
                // while at the same time not paying any non-negligible overhead.  The simple compromise
                // is to check whether we're disposed (not volatile), and if we see we are, to return an empty
                // registration, just as if CanBeCanceled was false for the check made in CancellationToken.Register.
                // If there's a race and m_disposed is false even though it's been disposed, or if the disposal request
                // comes in after this line, we simply run the minor risk of having m_registeredCallbacksLists reinitialized
                // (after it was cleared to null during Dispose).

                if (m_disposed && !AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
                    return new CancellationTokenRegistration();

                int myIndex = Thread.CurrentThread.ManagedThreadId % s_nLists;

                CancellationCallbackInfo callbackInfo = targetSyncContext != null ?
                    new CancellationCallbackInfo.WithSyncContext(callback, stateForCallback, executionContext, this, targetSyncContext) :
                    new CancellationCallbackInfo(callback, stateForCallback, executionContext, this);

                //allocate the callback list array
                var registeredCallbacksLists = m_registeredCallbacksLists;
                if (registeredCallbacksLists == null)
                {
                    SparselyPopulatedArray<CancellationCallbackInfo>[] list = new SparselyPopulatedArray<CancellationCallbackInfo>[s_nLists];
                    registeredCallbacksLists = Interlocked.CompareExchange(ref m_registeredCallbacksLists, list, null);
                    if (registeredCallbacksLists == null) registeredCallbacksLists = list;
                }

                //allocate the actual lists on-demand to save mem in low-use situations, and to avoid false-sharing.
                var callbacks = Volatile.Read<SparselyPopulatedArray<CancellationCallbackInfo>>(ref registeredCallbacksLists[myIndex]);
                if (callbacks == null)
                {
                    SparselyPopulatedArray<CancellationCallbackInfo> callBackArray = new SparselyPopulatedArray<CancellationCallbackInfo>(4);
                    Interlocked.CompareExchange(ref (registeredCallbacksLists[myIndex]), callBackArray, null);
                    callbacks = registeredCallbacksLists[myIndex];
                }

                // Now add the registration to the list.
                SparselyPopulatedArrayAddInfo<CancellationCallbackInfo> addInfo = callbacks.Add(callbackInfo);
                CancellationTokenRegistration registration = new CancellationTokenRegistration(callbackInfo, addInfo);

                if (!IsCancellationRequested)
                    return registration;

                // If a cancellation has since come in, we will try to undo the registration and run the callback ourselves.
                // (this avoids leaving the callback orphaned)
                bool deregisterOccurred = registration.TryDeregister();

                if (!deregisterOccurred)
                {
                    // The thread that is running Cancel() snagged our callback for execution.
                    // So we don't need to run it, but we do return the registration so that 
                    // ctr.Dispose() will wait for callback completion.
                    return registration;
                }
            }

            // If cancellation already occurred, we run the callback on this thread and return an empty registration.
            callback(stateForCallback);
            return new CancellationTokenRegistration();
        }

        /// <summary>
        /// 
        /// </summary>
        private void NotifyCancellation(bool throwOnFirstException)
        {
            // fast-path test to check if Notify has been called previously
            if (IsCancellationRequested)
                return;

            // If we're the first to signal cancellation, do the main extra work.
            if (Interlocked.CompareExchange(ref m_state, NOTIFYING, NOT_CANCELED) == NOT_CANCELED)
            {
                // Dispose of the timer, if any.  Dispose may be running concurrently here, but Timer.Dispose is thread-safe.
                m_timer?.Dispose();

                // Record the threadID being used for running the callbacks.
                ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
                
                // Set the event if it's been lazily initialized and hasn't yet been disposed of.  Dispose may
                // be running concurrently, in which case either it'll have set m_kernelEvent back to null and
                // we won't see it here, or it'll see that we've transitioned to NOTIFYING and will skip disposing it,
                // leaving cleanup to finalization.
                m_kernelEvent?.Set(); // update the MRE value.

                // - late enlisters to the Canceled event will have their callbacks called immediately in the Register() methods.
                // - Callbacks are not called inside a lock.
                // - After transition, no more delegates will be added to the 
                // - list of handlers, and hence it can be consumed and cleared at leisure by ExecuteCallbackHandlers.
                ExecuteCallbackHandlers(throwOnFirstException);
                Contract.Assert(IsCancellationCompleted, "Expected cancellation to have finished");
            }
        }

        /// <summary>
        /// Invoke the Canceled event.
        /// </summary>
        /// <remarks>
        /// The handlers are invoked synchronously in LIFO order.
        /// </remarks>
        private void ExecuteCallbackHandlers(bool throwOnFirstException)
        {
            Contract.Assert(IsCancellationRequested, "ExecuteCallbackHandlers should only be called after setting IsCancellationRequested->true");
            Contract.Assert(ThreadIDExecutingCallbacks != -1, "ThreadIDExecutingCallbacks should have been set.");

            // Design decision: call the delegates in LIFO order so that callbacks fire 'deepest first'.
            // This is intended to help with nesting scenarios so that child enlisters cancel before their parents.
            List<Exception> exceptionList = null;
            SparselyPopulatedArray<CancellationCallbackInfo>[] callbackLists = m_registeredCallbacksLists;

            // If there are no callbacks to run, we can safely exit.  Any race conditions to lazy initialize it
            // will see IsCancellationRequested and will then run the callback themselves.
            if (callbackLists == null)
            {
                Interlocked.Exchange(ref m_state, NOTIFYINGCOMPLETE);
                return;
            }
            
            try
            {
                for (int index = 0; index < callbackLists.Length; index++)
                {
                    SparselyPopulatedArray<CancellationCallbackInfo> list = Volatile.Read<SparselyPopulatedArray<CancellationCallbackInfo>>(ref callbackLists[index]);
                    if (list != null)
                    {
                        SparselyPopulatedArrayFragment<CancellationCallbackInfo> currArrayFragment = list.Tail;

                        while (currArrayFragment != null)
                        {
                            for (int i = currArrayFragment.Length - 1; i >= 0; i--)
                            {
                                // 1a. publish the indended callback, to ensure ctr.Dipose can tell if a wait is necessary.
                                // 1b. transition to the target syncContext and continue there..
                                //  On the target SyncContext.
                                //   2. actually remove the callback
                                //   3. execute the callback
                                // re:#2 we do the remove on the syncCtx so that we can be sure we have control of the syncCtx before
                                //        grabbing the callback.  This prevents a deadlock if ctr.Dispose() might run on the syncCtx too.
                                m_executingCallback = currArrayFragment[i];
                                if (m_executingCallback != null)
                                {
                                    //Transition to the target sync context (if necessary), and continue our work there.
                                    CancellationCallbackCoreWorkArguments args = new CancellationCallbackCoreWorkArguments(currArrayFragment, i);

                                    // marshal exceptions: either aggregate or perform an immediate rethrow
                                    // We assume that syncCtx.Send() has forwarded on user exceptions when appropriate.
                                    try
                                    {
                                        var wsc = m_executingCallback as CancellationCallbackInfo.WithSyncContext;
                                        if (wsc != null)
                                        {
                                            Contract.Assert(wsc.TargetSyncContext != null, "Should only have derived CCI if non-null SyncCtx");
                                            wsc.TargetSyncContext.Send(CancellationCallbackCoreWork_OnSyncContext, args);
                                            // CancellationCallbackCoreWork_OnSyncContext may have altered ThreadIDExecutingCallbacks, so reset it. 
                                            ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
                                        }
                                        else
                                        {
                                            CancellationCallbackCoreWork(args);
                                        }
                                    }
                                    catch(Exception ex)
                                    {
                                        if (throwOnFirstException)
                                            throw;
    
                                        // Otherwise, log it and proceed.
                                        if(exceptionList == null)
                                            exceptionList = new List<Exception>();
                                        exceptionList.Add(ex);
                                    }
                                }
                            }

                            currArrayFragment = currArrayFragment.Prev;
                        }
                    }
                }
            }
            finally
            {
                m_state = NOTIFYINGCOMPLETE;
                m_executingCallback = null;
                Thread.MemoryBarrier(); // for safety, prevent reorderings crossing this point and seeing inconsistent state.
            }

            if (exceptionList != null)
            {
                Contract.Assert(exceptionList.Count > 0, "Expected exception count > 0");
                throw new AggregateException(exceptionList);
            }
        }

        // The main callback work that executes on the target synchronization context
        private void CancellationCallbackCoreWork_OnSyncContext(object obj)
        {
            CancellationCallbackCoreWork((CancellationCallbackCoreWorkArguments)obj);
        }

        private void CancellationCallbackCoreWork(CancellationCallbackCoreWorkArguments args)
        {
            // remove the intended callback..and ensure that it worked.
            // otherwise the callback has disappeared in the interim and we can immediately return.
            CancellationCallbackInfo callback = args.m_currArrayFragment.SafeAtomicRemove(args.m_currArrayIndex, m_executingCallback);
            if (callback == m_executingCallback)
            {
                if (callback.TargetExecutionContext != null)
                {
                    // we are running via a custom sync context, so update the executing threadID
                    callback.CancellationTokenSource.ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
                }
                callback.ExecuteCallback();
            }
        }


        /// <summary>
        /// Creates a <see cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> that will be in the canceled state
        /// when any of the source tokens are in the canceled state.
        /// </summary>
        /// <param name="token1">The first <see cref="T:System.Threading.CancellationToken">CancellationToken</see> to observe.</param>
        /// <param name="token2">The second <see cref="T:System.Threading.CancellationToken">CancellationToken</see> to observe.</param>
        /// <returns>A <see cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> that is linked 
        /// to the source tokens.</returns>
        public static CancellationTokenSource CreateLinkedTokenSource(CancellationToken token1, CancellationToken token2)
        {
            return token1.CanBeCanceled || token2.CanBeCanceled ?
                new LinkedCancellationTokenSource(token1, token2) :
                new CancellationTokenSource();
        }

        /// <summary>
        /// Creates a <see cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> that will be in the canceled state
        /// when any of the source tokens are in the canceled state.
        /// </summary>
        /// <param name="tokens">The <see cref="T:System.Threading.CancellationToken">CancellationToken</see> instances to observe.</param>
        /// <returns>A <see cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> that is linked 
        /// to the source tokens.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="tokens"/> is null.</exception>
        public static CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
        {
            if (tokens == null)
                throw new ArgumentNullException("tokens");

            if (tokens.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("CancellationToken_CreateLinkedToken_TokensIsEmpty"));

            // a defensive copy is not required as the array has value-items that have only a single IntPtr field,
            // hence each item cannot be null itself, and reads of the payloads cannot be torn.
            Contract.EndContractBlock();

            return new LinkedCancellationTokenSource(tokens);
        }


        // Wait for a single callback to complete (or, more specifically, to not be running).
        // It is ok to call this method if the callback has already finished.
        // Calling this method before the target callback has been selected for execution would be an error.
        internal void WaitForCallbackToComplete(CancellationCallbackInfo callbackInfo)
        {
            SpinWait sw = new SpinWait();
            while (ExecutingCallback == callbackInfo)
            {
                sw.SpinOnce();  //spin as we assume callback execution is fast and that this situation is rare.
            }
        }

        private sealed class LinkedCancellationTokenSource : CancellationTokenSource
        {
            private static readonly Action<object> s_linkedTokenCancelDelegate = 
                s => ((CancellationTokenSource)s).NotifyCancellation(throwOnFirstException: false); // skip ThrowIfDisposed() check in Cancel()
            private CancellationTokenRegistration[] m_linkingRegistrations;

            internal LinkedCancellationTokenSource(CancellationToken token1, CancellationToken token2)
            {
                bool token2CanBeCanceled = token2.CanBeCanceled;

                if (token1.CanBeCanceled)
                {
                    m_linkingRegistrations = new CancellationTokenRegistration[token2CanBeCanceled ? 2 : 1]; // there will be at least 1 and at most 2 linkings
                    m_linkingRegistrations[0] = token1.InternalRegisterWithoutEC(s_linkedTokenCancelDelegate, this);
                }

                if (token2CanBeCanceled)
                {
                    int index = 1;
                    if (m_linkingRegistrations == null)
                    {
                        m_linkingRegistrations = new CancellationTokenRegistration[1]; // this will be the only linking
                        index = 0;
                    }
                    m_linkingRegistrations[index] = token2.InternalRegisterWithoutEC(s_linkedTokenCancelDelegate, this);
                }
            }

            internal LinkedCancellationTokenSource(params CancellationToken[] tokens)
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
                if (!disposing || m_disposed)
                    return;

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
    }

    // ----------------------------------------------------------
    // -- CancellationCallbackCoreWorkArguments --
    // ----------------------------------------------------------
    // Helper struct for passing data to the target sync context
    internal struct CancellationCallbackCoreWorkArguments
    {
        internal SparselyPopulatedArrayFragment<CancellationCallbackInfo> m_currArrayFragment;
        internal int m_currArrayIndex;
        
        public CancellationCallbackCoreWorkArguments(SparselyPopulatedArrayFragment<CancellationCallbackInfo> currArrayFragment, int currArrayIndex)
        {
            m_currArrayFragment = currArrayFragment;
            m_currArrayIndex = currArrayIndex;
        }
    }

    // ----------------------------------------------------------
    // -- CancellationCallbackInfo --
    // ----------------------------------------------------------

    /// <summary>
    /// A helper class for collating the various bits of information required to execute 
    /// cancellation callbacks.
    /// </summary>
    internal class CancellationCallbackInfo
    {
        internal readonly Action<object> Callback;
        internal readonly object StateForCallback;
        internal readonly ExecutionContext TargetExecutionContext;
        internal readonly CancellationTokenSource CancellationTokenSource;

        internal sealed class WithSyncContext : CancellationCallbackInfo
        {
            // Very rarely used, and as such it is separated out into a 
            // a derived type so that the space for it is pay-for-play.
            internal readonly SynchronizationContext TargetSyncContext;

            internal WithSyncContext(
                Action<object> callback, object stateForCallback, ExecutionContext targetExecutionContext, CancellationTokenSource cancellationTokenSource,
                SynchronizationContext targetSyncContext) :
                base(callback, stateForCallback, targetExecutionContext, cancellationTokenSource)
            {
                TargetSyncContext = targetSyncContext;
            }

        }

        internal CancellationCallbackInfo(
            Action<object> callback, object stateForCallback, ExecutionContext targetExecutionContext, CancellationTokenSource cancellationTokenSource)
        {
            Callback = callback;
            StateForCallback = stateForCallback;
            TargetExecutionContext = targetExecutionContext;
            CancellationTokenSource = cancellationTokenSource;
        }

        // Cached callback delegate that's lazily initialized due to ContextCallback being SecurityCritical
        [SecurityCritical]
        private static ContextCallback s_executionContextCallback;

        /// <summary>
        /// InternalExecuteCallbackSynchronously_GeneralPath
        /// This will be called on the target synchronization context, however, we still need to restore the required execution context
        /// </summary>
        [SecuritySafeCritical]
        internal void ExecuteCallback()
        {
            if (TargetExecutionContext != null)
            {
                // Lazily initialize the callback delegate; benign race condition
                var callback = s_executionContextCallback;
                if (callback == null) s_executionContextCallback = callback = new ContextCallback(ExecutionContextCallback);
                
                ExecutionContext.Run(
                    TargetExecutionContext,
                    callback,
                    this);
            }
            else
            {
                //otherwise run directly
                ExecutionContextCallback(this);
            }
        }

        // the worker method to actually run the callback
        // The signature is such that it can be used as a 'ContextCallback'
        [SecurityCritical]
        private static void ExecutionContextCallback(object obj)
        {
            CancellationCallbackInfo callbackInfo = obj as CancellationCallbackInfo;
            Contract.Assert(callbackInfo != null);
            callbackInfo.Callback(callbackInfo.StateForCallback);
        }
    }


    // ----------------------------------------------------------
    // -- SparselyPopulatedArray --
    // ----------------------------------------------------------

    /// <summary>
    /// A sparsely populated array.  Elements can be sparse and some null, but this allows for
    /// lock-free additions and growth, and also for constant time removal (by nulling out).
    /// </summary>
    /// <typeparam name="T">The kind of elements contained within.</typeparam>
    internal class SparselyPopulatedArray<T> where T : class
    {
        private readonly SparselyPopulatedArrayFragment<T> m_head;
        private volatile SparselyPopulatedArrayFragment<T> m_tail;

        /// <summary>
        /// Allocates a new array with the given initial size.
        /// </summary>
        /// <param name="initialSize">How many array slots to pre-allocate.</param>
        internal SparselyPopulatedArray(int initialSize)
        {
            m_head = m_tail = new SparselyPopulatedArrayFragment<T>(initialSize);
        }

#if DEBUG
        // Used in DEBUG mode by CancellationTokenSource.CallbackCount
        /// <summary>
        /// The head of the doubly linked list.
        /// </summary>
        internal SparselyPopulatedArrayFragment<T> Head
        {
            get { return m_head; }
        }
#endif

        /// <summary>
        /// The tail of the doubly linked list.
        /// </summary>
        internal SparselyPopulatedArrayFragment<T> Tail
        {
            get { return m_tail; }
        }

        /// <summary>
        /// Adds an element in the first available slot, beginning the search from the tail-to-head.
        /// If no slots are available, the array is grown.  The method doesn't return until successful.
        /// </summary>
        /// <param name="element">The element to add.</param>
        /// <returns>Information about where the add happened, to enable O(1) deregistration.</returns>
        internal SparselyPopulatedArrayAddInfo<T> Add(T element)
        {
            while (true)
            {
                // Get the tail, and ensure it's up to date.
                SparselyPopulatedArrayFragment<T> tail = m_tail;
                while (tail.m_next != null)
                    m_tail = (tail = tail.m_next);

                // Search for a free index, starting from the tail.
                SparselyPopulatedArrayFragment<T> curr = tail;
                while (curr != null)
                {
                    const int RE_SEARCH_THRESHOLD = -10; // Every 10 skips, force a search.
                    if (curr.m_freeCount < 1)
                        --curr.m_freeCount;

                    if (curr.m_freeCount > 0 || curr.m_freeCount < RE_SEARCH_THRESHOLD)
                    {
                        int c = curr.Length;

                        // We'll compute a start offset based on how many free slots we think there
                        // are.  This optimizes for ordinary the LIFO deregistration pattern, and is
                        // far from perfect due to the non-threadsafe ++ and -- of the free counter.
                        int start = ((c - curr.m_freeCount) % c);
                        if (start < 0)
                        {
                            start = 0;
                            curr.m_freeCount--; // Too many free elements; fix up.
                        }
                        Contract.Assert(start >= 0 && start < c, "start is outside of bounds");

                        // Now walk the array until we find a free slot (or reach the end).
                        for (int i = 0; i < c; i++)
                        {
                            // If the slot is null, try to CAS our element into it.
                            int tryIndex = (start + i) % c;
                            Contract.Assert(tryIndex >= 0 && tryIndex < curr.m_elements.Length, "tryIndex is outside of bounds");
                            
                            if (curr.m_elements[tryIndex] == null && Interlocked.CompareExchange(ref curr.m_elements[tryIndex], element, null) == null)
                            {
                                // We adjust the free count by --. Note: if this drops to 0, we will skip
                                // the fragment on the next search iteration.  Searching threads will -- the
                                // count and force a search every so often, just in case fragmentation occurs.
                                int newFreeCount = curr.m_freeCount - 1;
                                curr.m_freeCount = newFreeCount > 0 ? newFreeCount : 0;
                                return new SparselyPopulatedArrayAddInfo<T>(curr, tryIndex);
                            }
                        }
                    }

                    curr = curr.m_prev;
                }

                // If we got here, we need to add a new chunk to the tail and try again.
                SparselyPopulatedArrayFragment<T> newTail = new SparselyPopulatedArrayFragment<T>(
                    tail.m_elements.Length == 4096 ? 4096 : tail.m_elements.Length * 2, tail);
                if (Interlocked.CompareExchange(ref tail.m_next, newTail, null) == null)
                {
                    m_tail = newTail;
                }
            }
        }
    }

    /// <summary>
    /// A struct to hold a link to the exact spot in an array an element was inserted, enabling
    /// constant time removal later on.
    /// </summary>
    internal struct SparselyPopulatedArrayAddInfo<T> where T : class
    {
        private SparselyPopulatedArrayFragment<T> m_source;
        private int m_index;

        internal SparselyPopulatedArrayAddInfo(SparselyPopulatedArrayFragment<T> source, int index)
        {
            Contract.Assert(source != null);
            Contract.Assert(index >= 0 && index < source.Length);
            m_source = source;
            m_index = index;
        }

        internal SparselyPopulatedArrayFragment<T> Source
        {
            get { return m_source; }
        }

        internal int Index
        {
            get { return m_index; }
        }
    }

    /// <summary>
    /// A fragment of a sparsely populated array, doubly linked.
    /// </summary>
    /// <typeparam name="T">The kind of elements contained within.</typeparam>
    internal class SparselyPopulatedArrayFragment<T> where T : class
    {
        internal readonly T[] m_elements; // The contents, sparsely populated (with nulls).
        internal volatile int m_freeCount; // A hint of the number of free elements.
        internal volatile SparselyPopulatedArrayFragment<T> m_next; // The next fragment in the chain.
        internal volatile SparselyPopulatedArrayFragment<T> m_prev; // The previous fragment in the chain.

        internal SparselyPopulatedArrayFragment(int size) : this(size, null)
        {
        }

        internal SparselyPopulatedArrayFragment(int size, SparselyPopulatedArrayFragment<T> prev)
        {
            m_elements = new T[size];
            m_freeCount = size;
            m_prev = prev;
        }

        internal T this[int index]
        {
            get { return Volatile.Read<T>(ref m_elements[index]); }
        }

        internal int Length
        {
            get { return m_elements.Length; }
        }

#if DEBUG
        // Used in DEBUG mode by CancellationTokenSource.CallbackCount
        internal SparselyPopulatedArrayFragment<T> Next
        {
            get { return m_next; }
        }
#endif
        internal SparselyPopulatedArrayFragment<T> Prev
        {
            get { return m_prev; }
        }

        // only removes the item at the specified index if it is still the expected one.
        // Returns the prevailing value.
        // The remove occurred successfully if the return value == expected element
        // otherwise the remove did not occur.
        internal T SafeAtomicRemove(int index, T expectedElement)
        {
            T prevailingValue = Interlocked.CompareExchange(ref m_elements[index], null, expectedElement);
            if (prevailingValue != null) 
                ++m_freeCount;
            return prevailingValue;
        }
    }
}
