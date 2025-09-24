// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Threading
{
    /// <summary>
    /// Limits the number of threads that can access a resource or pool of resources concurrently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="SemaphoreSlim"/> provides a lightweight semaphore class that doesn't
    /// use Windows kernel semaphores.
    /// </para>
    /// <para>
    /// All public and protected members of <see cref="SemaphoreSlim"/> are thread-safe and may be used
    /// concurrently from multiple threads, with the exception of Dispose, which
    /// must only be used when all other operations on the <see cref="SemaphoreSlim"/> have
    /// completed.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("Current Count = {m_currentCount}")]
    public class SemaphoreSlim : IDisposable
    {
        #region Private Fields

        // The semaphore count, initialized in the constructor to the initial value, every release call increments it
        // and every wait call decrements it as long as its value is positive otherwise the wait will block.
        // Its value must be between the maximum semaphore value and zero
        private volatile int m_currentCount;

        // The maximum semaphore value, it is initialized to Int.MaxValue if the client didn't specify it. it is used
        // to check if the count exceeded the maximum value or not.
        private readonly int m_maxCount;

        // The number of synchronously waiting threads, it is set to zero in the constructor and increments before blocking the
        // threading and decrements it back after that. It is used as flag for the release call to know if there are
        // waiting threads in the monitor or not.
        private int m_waitCount;

        /// <summary>
        /// This is used to help prevent waking more waiters than necessary. It's not perfect and sometimes more waiters than
        /// necessary may still be woken, see <see cref="WaitUntilCountOrTimeout"/>.
        /// </summary>
        private int m_countOfWaitersPulsedToWake;

        // Object used to synchronize access to state on the instance.  The contained
        // Boolean value indicates whether the instance has been disposed.
        private readonly StrongBox<bool> m_lockObjAndDisposed;

        // Act as the semaphore wait handle, it's lazily initialized if needed, the first WaitHandle call initialize it
        // and wait an release sets and resets it respectively as long as it is not null
        private volatile ManualResetEvent? m_waitHandle;

        // Head of list representing asynchronous waits on the semaphore.
        private TaskNode? m_asyncHead;

        // Tail of list representing asynchronous waits on the semaphore.
        private TaskNode? m_asyncTail;

        // No maximum constant
        private const int NO_MAXIMUM = int.MaxValue;

        // Task in a linked list of asynchronous waiters
        private sealed class TaskNode : Task<bool>
        {
            internal TaskNode? Prev, Next;
            internal TaskNode() : base((object?)null, TaskCreationOptions.RunContinuationsAsynchronously) { }
        }
        #endregion

        #region Public properties

        /// <summary>
        /// Gets the current count of the <see cref="SemaphoreSlim"/>.
        /// </summary>
        /// <value>The current count of the <see cref="SemaphoreSlim"/>.</value>
        public int CurrentCount => Math.Max(m_currentCount, 0);

        /// <summary>
        /// Returns a <see cref="WaitHandle"/> that can be used to wait on the semaphore.
        /// </summary>
        /// <value>A <see cref="WaitHandle"/> that can be used to wait on the
        /// semaphore.</value>
        /// <remarks>
        /// A successful wait on the <see cref="AvailableWaitHandle"/> does not imply a successful wait on
        /// the <see cref="SemaphoreSlim"/> itself, nor does it decrement the semaphore's
        /// count. <see cref="AvailableWaitHandle"/> exists to allow a thread to block waiting on multiple
        /// semaphores, but such a wait should be followed by a true wait on the target semaphore.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see
        /// cref="SemaphoreSlim"/> has been disposed.</exception>
        public WaitHandle AvailableWaitHandle
        {
            get
            {
                CheckDispose();

                // Return it directly if it is not null
                if (m_waitHandle is null)
                {
                    // lock the count to avoid multiple threads initializing the handle if it is null
                    lock (m_lockObjAndDisposed)
                    {
                        // The initial state for the wait handle is true if the count is greater than zero
                        // false otherwise
                        m_waitHandle ??= new ManualResetEvent(m_currentCount != 0);
                    }
                }

                return m_waitHandle;
            }
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreSlim"/> class, specifying
        /// the initial number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted
        /// concurrently.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount"/>
        /// is less than 0.</exception>
        public SemaphoreSlim(int initialCount)
            : this(initialCount, NO_MAXIMUM)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreSlim"/> class, specifying
        /// the initial and maximum number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted
        /// concurrently.</param>
        /// <param name="maxCount">The maximum number of requests for the semaphore that can be granted
        /// concurrently.</param>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="initialCount"/>
        /// is less than 0. -or-
        /// <paramref name="initialCount"/> is greater than <paramref name="maxCount"/>. -or-
        /// <paramref name="maxCount"/> is equal to or less than 0.</exception>
        public SemaphoreSlim(int initialCount, int maxCount)
        {
            if (initialCount < 0 || initialCount > maxCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialCount), initialCount, SR.SemaphoreSlim_ctor_InitialCountWrong);
            }

            // validate input
            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, SR.SemaphoreSlim_ctor_MaxCountWrong);
            }

            m_maxCount = maxCount;
            m_currentCount = initialCount;
            m_lockObjAndDisposed = new StrongBox<bool>();
        }

        #endregion

        #region  Methods
        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreSlim"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        [UnsupportedOSPlatform("browser")]
        public void Wait()
        {
#if TARGET_WASI
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
#endif
            // Call wait with infinite timeout
            WaitCore(Timeout.Infinite, CancellationToken.None);
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreSlim"/>, while observing a
        /// <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> token to
        /// observe.</param>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was
        /// canceled.</exception>
        /// <exception cref="ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        [UnsupportedOSPlatform("browser")]
        public void Wait(CancellationToken cancellationToken)
        {
#if TARGET_WASI
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
#endif
            // Call wait with infinite timeout
            WaitCore(Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreSlim"/>, using a <see
        /// cref="TimeSpan"/> to measure the time interval.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>true if the current thread successfully entered the <see cref="SemaphoreSlim"/>;
        /// otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out.</exception>
        [UnsupportedOSPlatform("browser")]
        public bool Wait(TimeSpan timeout)
        {
#if TARGET_WASI
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
#endif
            // Validate the timeout
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout), timeout, SR.SemaphoreSlim_Wait_TimeSpanTimeoutWrong);
            }

            // Call wait with the timeout milliseconds
            return WaitCore(totalMilliseconds, CancellationToken.None);
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreSlim"/>, using a <see
        /// cref="TimeSpan"/> to measure the time interval, while observing a <see
        /// cref="CancellationToken"/>.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to
        /// observe.</param>
        /// <returns>true if the current thread successfully entered the <see cref="SemaphoreSlim"/>;
        /// otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        [UnsupportedOSPlatform("browser")]
        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
#if TARGET_WASI
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
#endif
            // Validate the timeout
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout), timeout, SR.SemaphoreSlim_Wait_TimeSpanTimeoutWrong);
            }

            // Call wait with the timeout milliseconds
            return WaitCore(totalMilliseconds, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreSlim"/>, using a 32-bit
        /// signed integer to measure the time interval.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="Timeout.Infinite"/>(-1) to wait indefinitely.</param>
        /// <returns>true if the current thread successfully entered the <see cref="SemaphoreSlim"/>;
        /// otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        [UnsupportedOSPlatform("browser")]
        public bool Wait(int millisecondsTimeout)
        {
#if TARGET_WASI
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
#endif

            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsTimeout), millisecondsTimeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
            }


            return WaitCore(millisecondsTimeout, CancellationToken.None);
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreSlim"/>,
        /// using a 32-bit signed integer to measure the time interval,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="Timeout.Infinite"/>(-1) to
        /// wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>true if the current thread successfully entered the <see cref="SemaphoreSlim"/>; otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a negative number other than -1,
        /// which represents an infinite time-out.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        [UnsupportedOSPlatform("browser")]
        public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
#if TARGET_WASI
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
#endif

            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsTimeout), millisecondsTimeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
            }

            return WaitCore(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Attempts to enter the <see cref="SemaphoreSlim"/> immediately, and returns a value that indicates whether the
        /// attempt was successful.
        /// </summary>
        /// <returns>true if the current thread successfully entered the <see cref="SemaphoreSlim"/>; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAcquireSemaphore()
        {
            if (m_currentCount <= 0)
            {
                return false;
            }

            int newCount = Interlocked.Decrement(ref m_currentCount);
            if (newCount < 0)
            {
                Interlocked.Increment(ref m_currentCount);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreSlim"/>,
        /// using a 32-bit unsigned integer to measure the time interval,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="Timeout.UnsignedInfinite"/> to
        /// wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>true if the current thread successfully entered the <see cref="SemaphoreSlim"/>; otherwise, false.</returns>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        [UnsupportedOSPlatform("browser")]
        private bool WaitCore(long millisecondsTimeout, CancellationToken cancellationToken)
        {
            CheckDispose();
#if FEATURE_WASM_MANAGED_THREADS
            Thread.AssureBlockingPossible();
#endif
            cancellationToken.ThrowIfCancellationRequested();

            // Perf: Check the stack timeout parameter before checking the volatile count
            if (millisecondsTimeout == 0 && m_currentCount == 0)
            {
                // Pessimistic fail fast, check volatile count outside lock (only when timeout is zero!)
                return false;
            }

            // Fast Path: If the count is greater than zero, try to acquire the semaphore.
            // Check if it's possible to decrease the current count with an Interlocked operation instead of taking the Monitor lock.
            // Only attempt this if there is no wait handle, otherwise it's not possible to guarantee that the wait handle is in the correct state with this optimization.
            if (m_waitHandle is null)
            {
                if (TryAcquireSemaphore())
                {
                    return true;
                }
            }

            long startTime = 0;
            if (millisecondsTimeout != Timeout.Infinite && millisecondsTimeout > 0)
            {
                startTime = Environment.TickCount64;
            }

            bool waitSuccessful = false;
            Task<bool>? asyncWaitTask = null;
            bool lockTaken = false;

            // Register for cancellation outside of the main lock.
            // NOTE: Register/unregister inside the lock can deadlock as different lock acquisition orders could
            //      occur for (1)this.m_lockObjAndDisposed and (2)cts.internalLock
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.UnsafeRegister(s_cancellationTokenCanceledEventHandler, this);
            try
            {
                // Perf: first spin wait for the count to be positive.
                // This additional amount of spinwaiting in addition
                // to Monitor.Enter()'s spinwaiting has shown measurable perf gains in test scenarios.
                if (m_currentCount == 0)
                {
                    // Monitor.Enter followed by Monitor.Wait is much more expensive than waiting on an event as it involves another
                    // spin, contention, etc. The usual number of spin iterations that would otherwise be used here is increased to
                    // lessen that extra expense of doing a proper wait.
                    int spinCount = SpinWait.SpinCountforSpinBeforeWait * 4;

                    SpinWait spinner = default;
                    while (spinner.Count < spinCount)
                    {
                        spinner.SpinOnce(sleep1Threshold: -1);

                        if (m_currentCount != 0)
                        {
                            break;
                        }
                    }
                }
                Monitor.Enter(m_lockObjAndDisposed, ref lockTaken);

                m_waitCount++;

                // If there are any async waiters, for fairness we'll get in line behind
                // then by translating our synchronous wait into an asynchronous one that we
                // then block on (once we've released the lock).
                if (m_asyncHead is not null)
                {
                    Debug.Assert(m_asyncTail is not null, "tail should not be null if head isn't");
                    asyncWaitTask = WaitAsyncCore(millisecondsTimeout, cancellationToken);
                }
                // There are no async waiters, so we can proceed with normal synchronous waiting.
                else
                {
                    // If we cannot wait, then try to acquire the semaphore.
                    // If the new count becomes negative, that means that the count was zero
                    // so we should revert this invalid operation and return false.
                    if (millisecondsTimeout == 0)
                    {
                        return TryAcquireSemaphore();
                    }

                    waitSuccessful = WaitUntilCountOrTimeout(millisecondsTimeout, startTime, cancellationToken);

                    // Exposing wait handle which is lazily initialized if needed
                    if (m_waitHandle is not null && m_currentCount == 0)
                    {
                        m_waitHandle.Reset();
                    }
                }
            }
            finally
            {
                // Release the lock
                if (lockTaken)
                {
                    m_waitCount--;
                    Monitor.Exit(m_lockObjAndDisposed);
                }

                // Unregister the cancellation callback.
                cancellationTokenRegistration.Dispose();
            }

            // If we had to fall back to asynchronous waiting, block on it
            // here now that we've released the lock, and return its
            // result when available.  Otherwise, this was a synchronous
            // wait, and whether we successfully acquired the semaphore is
            // stored in waitSuccessful.

            return (asyncWaitTask is not null) ? asyncWaitTask.GetAwaiter().GetResult() : waitSuccessful;
        }

        /// <summary>
        /// Local helper function, waits on the monitor until the monitor receives signal or the
        /// timeout is expired
        /// </summary>
        /// <param name="millisecondsTimeout">The maximum timeout</param>
        /// <param name="startTime">The start ticks to calculate the elapsed time</param>
        /// <param name="cancellationToken">The CancellationToken to observe.</param>
        /// <returns>true if the monitor received a signal, false if the timeout expired</returns>
        [UnsupportedOSPlatform("browser")]
        private bool WaitUntilCountOrTimeout(long millisecondsTimeout, long startTime, CancellationToken cancellationToken)
        {
#if TARGET_WASI
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185
#endif
            int monitorWaitMilliseconds = Timeout.Infinite;

            // Wait on the monitor as long we cannot acquire the semaphore
            while (true)
            {
                if (TryAcquireSemaphore())
                {
                    return true;
                }

                // If cancelled, we throw. Trying to wait could lead to deadlock.
                cancellationToken.ThrowIfCancellationRequested();

                // Since Monitor.Wait will handle the actual wait and it accepts an int timeout,
                // we may need to cap the timeout to int.MaxValue.
                bool timeoutIsCapped = false;
                if (millisecondsTimeout != Timeout.Infinite)
                {
                    long remainingWaitMilliseconds = TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout);
                    if (remainingWaitMilliseconds <= 0)
                    {
                        // The thread has expires its timeout
                        return false;
                    }
                    if (remainingWaitMilliseconds <= int.MaxValue)
                    {
                        monitorWaitMilliseconds = (int)remainingWaitMilliseconds;
                    }
                    else
                    {
                        timeoutIsCapped = true;
                        monitorWaitMilliseconds = int.MaxValue;
                    }
                }


                // The actual wait. If the timeout was capped and waitSuccessful is false, it doesn't imply
                // a timeout, we are just limited by Monitor.Wait's maximum timeout value.
                bool waitSuccessful = Monitor.Wait(m_lockObjAndDisposed, monitorWaitMilliseconds);

                // This waiter has woken up and this needs to be reflected in the count of waiters pulsed to wake. Since we
                // don't have thread-specific pulse state, there is not enough information to tell whether this thread woke up
                // because it was pulsed. For instance, this thread may have timed out and may have been waiting to reacquire
                // the lock before returning from Monitor.Wait, in which case we don't know whether this thread got pulsed. So
                // in any woken case, decrement the count if possible. As such, timeouts could cause more waiters to wake than
                // necessary.
                if (m_countOfWaitersPulsedToWake != 0)
                {
                    --m_countOfWaitersPulsedToWake;
                }

                if (!timeoutIsCapped && !waitSuccessful)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/>.
        /// </summary>
        /// <returns>A task that will complete when the semaphore has been entered.</returns>
        public Task WaitAsync()
        {
            return WaitAsyncCore(Timeout.Infinite, default);
        }

        /// <summary>
        /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/>, while observing a
        /// <see cref="CancellationToken"/>.
        /// </summary>
        /// <returns>A task that will complete when the semaphore has been entered.</returns>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> token to observe.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// The current instance has already been disposed.
        /// </exception>
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            return WaitAsyncCore(Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/>,
        /// using a 32-bit signed integer to measure the time interval.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="Timeout.Infinite"/>(-1) to wait indefinitely.
        /// </param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the <see cref="SemaphoreSlim"/>, otherwise with a result of false.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a negative number other than -1,
        /// which represents an infinite time-out.
        /// </exception>
        public Task<bool> WaitAsync(int millisecondsTimeout)
        {
            return WaitAsyncCore(millisecondsTimeout, default);
        }

        /// <summary>
        /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/>, using a <see
        /// cref="TimeSpan"/> to measure the time interval, while observing a
        /// <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the <see cref="SemaphoreSlim"/>, otherwise with a result of false.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The current instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="timeout"/> is a negative number other than -1 milliseconds, which represents
        /// an infinite time-out -or- timeout is greater than <see cref="int.MaxValue"/>.
        /// </exception>
        public Task<bool> WaitAsync(TimeSpan timeout)
        {
            // Validate the timeout
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout), timeout, SR.SemaphoreSlim_Wait_TimeSpanTimeoutWrong);
            }

            // Call wait with the timeout milliseconds
            return WaitAsyncCore(totalMilliseconds, default);
        }

        /// <summary>
        /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/>, using a <see
        /// cref="TimeSpan"/> to measure the time interval.
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> token to observe.
        /// </param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the <see cref="SemaphoreSlim"/>, otherwise with a result of false.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="timeout"/> is a negative number other than -1 milliseconds, which represents
        /// an infinite time-out -or- timeout is greater than <see cref="int.MaxValue"/>.
        /// </exception>
        public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            // Validate the timeout
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout), timeout, SR.SemaphoreSlim_Wait_TimeSpanTimeoutWrong);
            }

            // Call wait with the timeout milliseconds
            return WaitAsyncCore(totalMilliseconds, cancellationToken);
        }

        /// <summary>
        /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/>,
        /// using a 32-bit signed integer to measure the time interval,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="Timeout.Infinite"/>(-1) to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the <see cref="SemaphoreSlim"/>, otherwise with a result of false.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a negative number other than -1,
        /// which represents an infinite time-out.
        /// </exception>
        public Task<bool> WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsTimeout), millisecondsTimeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
            }

            return WaitAsyncCore(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/>,
        /// using a 32-bit unsigned integer to measure the time interval,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="Timeout.UnsignedInfinite"/> to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the <see cref="SemaphoreSlim"/>, otherwise with a result of false.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        private Task<bool> WaitAsyncCore(long millisecondsTimeout, CancellationToken cancellationToken)
        {
            CheckDispose();

            // Bail early for cancellation
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<bool>(cancellationToken);

            // Perf: Check if it's possible to decrease the current count with an Interlocked operation instead of taking the Monitor lock.
            // Only attempt this if there is no wait handle, otherwise it's not possible to guarantee that the wait handle is in the correct state with this optimization.
            if (m_waitHandle is null)
            {
                if (TryAcquireSemaphore())
                {
                    return Task.FromResult(true);
                }
            }

            lock (m_lockObjAndDisposed)
            {
                // If there are counts available, allow this waiter to succeed.
                if (TryAcquireSemaphore())
                {
                    if (m_waitHandle is not null && m_currentCount == 0) m_waitHandle.Reset();
                    return Task.FromResult(true);
                }
                else if (millisecondsTimeout == 0)
                {
                    // No counts, if timeout is zero fail fast
                    return Task.FromResult(false);
                }
                // If there aren't, create and return a task to the caller.
                // The task will be completed either when they've successfully acquired
                // the semaphore or when the timeout expired or cancellation was requested.
                else
                {
                    Debug.Assert(m_currentCount == 0, "m_currentCount should never be negative");
                    TaskNode asyncWaiter = CreateAndAddAsyncWaiter();
                    Task<bool> result = (millisecondsTimeout == Timeout.Infinite && !cancellationToken.CanBeCanceled) ?
                        asyncWaiter :
                        WaitUntilCountOrTimeoutAsync(asyncWaiter, millisecondsTimeout, cancellationToken);
                    return result;
                }
            }
        }

        /// <summary>Creates a new task and stores it into the async waiters list.</summary>
        /// <returns>The created task.</returns>
        private TaskNode CreateAndAddAsyncWaiter()
        {
            Debug.Assert(Monitor.IsEntered(m_lockObjAndDisposed), "Requires the lock be held");

            // Create the task
            var task = new TaskNode();

            // Add it to the linked list
            if (m_asyncHead is null)
            {
                Debug.Assert(m_asyncTail is null, "If head is null, so too should be tail");
                m_asyncHead = task;
                m_asyncTail = task;
            }
            else
            {
                Debug.Assert(m_asyncTail is not null, "If head is not null, neither should be tail");
                m_asyncTail.Next = task;
                task.Prev = m_asyncTail;
                m_asyncTail = task;
            }

            // Hand it back
            return task;
        }

        /// <summary>Removes the waiter task from the linked list.</summary>
        /// <param name="task">The task to remove.</param>
        /// <returns>true if the waiter was in the list; otherwise, false.</returns>
        private bool RemoveAsyncWaiter(TaskNode task)
        {
            Debug.Assert(task is not null, "Expected non-null task");
            Debug.Assert(Monitor.IsEntered(m_lockObjAndDisposed), "Requires the lock be held");

            // Is the task in the list?  To be in the list, either it's the head or it has a predecessor that's in the list.
            bool wasInList = m_asyncHead == task || task.Prev is not null;

            // Remove it from the linked list
            task.Next?.Prev = task.Prev;
            task.Prev?.Next = task.Next;
            if (m_asyncHead == task) m_asyncHead = task.Next;
            if (m_asyncTail == task) m_asyncTail = task.Prev;
            Debug.Assert((m_asyncHead is null) == (m_asyncTail is null), "Head is null iff tail is null");

            // Make sure not to leak
            task.Next = task.Prev = null;

            // Return whether the task was in the list
            return wasInList;
        }

        /// <summary>Performs the asynchronous wait.</summary>
        /// <param name="asyncWaiter">The asynchronous waiter.</param>
        /// <param name="millisecondsTimeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task to return to the caller.</returns>
        private async Task<bool> WaitUntilCountOrTimeoutAsync(TaskNode asyncWaiter, long millisecondsTimeout, CancellationToken cancellationToken)
        {
            Debug.Assert(asyncWaiter is not null, "Waiter should have been constructed");
            Debug.Assert(Monitor.IsEntered(m_lockObjAndDisposed), "Requires the lock be held");

            await ((Task)asyncWaiter.WaitAsync(
                TimeSpan.FromMilliseconds(millisecondsTimeout),
                cancellationToken)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            if (cancellationToken.IsCancellationRequested)
            {
                // If we might be running as part of a cancellation callback, force the completion to be asynchronous
                // so as to maintain semantics similar to when no token is passed (neither Release nor Cancel would invoke
                // continuations off of this task).
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            }

            if (asyncWaiter.IsCompleted)
            {
                return true; // successfully acquired
            }

            // The wait has timed out or been canceled.

            // If the await completed synchronously, we still hold the lock.  If it didn't,
            // we no longer hold the lock.  As such, acquire it.
            lock (m_lockObjAndDisposed)
            {
                // Remove the task from the list.  If we're successful in doing so,
                // we know that no one else has tried to complete this waiter yet,
                // so we can safely cancel or timeout.
                if (RemoveAsyncWaiter(asyncWaiter))
                {
                    cancellationToken.ThrowIfCancellationRequested(); // cancellation occurred
                    return false; // timeout occurred
                }
            }

            // The waiter had already been removed, which means it's already completed or is about to
            // complete, so let it, and don't return until it does.
            return await asyncWaiter.ConfigureAwait(false);
        }

        /// <summary>
        /// Exits the <see cref="SemaphoreSlim"/> once.
        /// </summary>
        /// <returns>The previous count of the <see cref="SemaphoreSlim"/>.</returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public int Release()
        {
            return Release(1);
        }

        /// <summary>
        /// Exits the <see cref="SemaphoreSlim"/> a specified number of times.
        /// </summary>
        /// <param name="releaseCount">The number of times to exit the semaphore.</param>
        /// <returns>The previous count of the <see cref="SemaphoreSlim"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="releaseCount"/> is less
        /// than 1.</exception>
        /// <exception cref="SemaphoreFullException">The <see cref="SemaphoreSlim"/> has
        /// already reached its maximum size.</exception>
        /// <exception cref="ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public int Release(int releaseCount)
        {
            CheckDispose();

            if (releaseCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(releaseCount), releaseCount, SR.SemaphoreSlim_Release_CountWrong);
            }

            // Fast path: If the count is greater than zero, try to release the semaphore without taking the lock.
            // If it's zero or less, we need to take the lock to ensure that we properly wake up waiters
            // and potentially update m_waitHandle.
            int currentCount = m_currentCount;
            if (currentCount > 0 && currentCount + releaseCount <= m_maxCount &&
                Interlocked.CompareExchange(ref m_currentCount, currentCount + releaseCount, currentCount) == currentCount)
            {
                return currentCount;
            }

            int returnCount;

            // If m_currentCount was not greater than 0, it may be negative if some threads attempted to acquire
            // the semaphore during the wait fast path but failed.
            // In this case, the threads themselves will make the count back to zero after a little while, spin until then.
            SpinWait spinner = default;
            while (m_currentCount < 0)
            {
                spinner.SpinOnce();
            }

            // The current count must be 0 and we can take the lock knowing that no threads executing a wait operation
            // will be able to modify the count until we release the lock.
            Debug.Assert(m_currentCount == 0, "m_currentCount should never be negative here");


            lock (m_lockObjAndDisposed)
            {
                // Read the m_currentCount into a local variable to avoid unnecessary volatile accesses inside the lock.
                currentCount = m_currentCount;
                returnCount = currentCount;

                // If the release count would result exceeding the maximum count, throw SemaphoreFullException.
                if (m_maxCount - currentCount < releaseCount)
                {
                    throw new SemaphoreFullException();
                }

                // Increment the count by the actual release count
                currentCount += releaseCount;

                // Signal to any synchronous waiters, taking into account how many waiters have previously been pulsed to wake
                // but have not yet woken
                int waitCount = m_waitCount;
                Debug.Assert(m_countOfWaitersPulsedToWake <= waitCount);
                int waitersToNotify = Math.Min(currentCount, waitCount) - m_countOfWaitersPulsedToWake;
                if (waitersToNotify > 0)
                {
                    // Ideally, limiting to a maximum of releaseCount would not be necessary and could be an assert instead, but
                    // since WaitUntilCountOrTimeout() does not have enough information to tell whether a woken thread was
                    // pulsed, it's possible for m_countOfWaitersPulsedToWake to be less than the number of threads that have
                    // actually been pulsed to wake.
                    if (waitersToNotify > releaseCount)
                    {
                        waitersToNotify = releaseCount;
                    }

                    m_countOfWaitersPulsedToWake += waitersToNotify;
                    for (int i = 0; i < waitersToNotify; i++)
                    {
                        Monitor.Pulse(m_lockObjAndDisposed);
                    }
                }

                // Now signal to any asynchronous waiters, if there are any.  While we've already
                // signaled the synchronous waiters, we still hold the lock, and thus
                // they won't have had an opportunity to acquire this yet.  So, when releasing
                // asynchronous waiters, we assume that all synchronous waiters will eventually
                // acquire the semaphore.  That could be a faulty assumption if those synchronous
                // waits are canceled, but the wait code path will handle that.
                if (m_asyncHead is not null)
                {
                    Debug.Assert(m_asyncTail is not null, "tail should not be null if head isn't null");
                    int maxAsyncToRelease = currentCount - waitCount;
                    while (maxAsyncToRelease > 0 && m_asyncHead is not null)
                    {
                        --currentCount;
                        --maxAsyncToRelease;

                        // Get the next async waiter to release and queue it to be completed
                        TaskNode waiterTask = m_asyncHead;
                        RemoveAsyncWaiter(waiterTask); // ensures waiterTask.Next/Prev are null
                        waiterTask.TrySetResult(result: true);
                    }
                }
                m_currentCount = currentCount;

                // Exposing wait handle if it is not null
                if (m_waitHandle is not null && returnCount == 0 && currentCount > 0)
                {
                    m_waitHandle.Set();
                }
            }

            // And return the count
            return returnCount;
        }

        /// <summary>
        /// Releases all resources used by the current instance of <see
        /// cref="SemaphoreSlim"/>.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="SemaphoreSlim"/>, <see cref="Dispose()"/> is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// When overridden in a derived class, releases the unmanaged resources used by the
        /// <see cref="ManualResetEventSlim"/>, and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources.</param>
        /// <remarks>
        /// Unlike most of the members of <see cref="SemaphoreSlim"/>, <see cref="Dispose(bool)"/> is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                WaitHandle? wh = m_waitHandle;
                if (wh is not null)
                {
                    wh.Dispose();
                    m_waitHandle = null;
                }

                m_lockObjAndDisposed.Value = true;

                m_asyncHead = null;
                m_asyncTail = null;
            }
        }

        /// <summary>
        /// Private helper method to wake up waiters when a cancellationToken gets canceled.
        /// </summary>
        private static readonly Action<object?> s_cancellationTokenCanceledEventHandler = new Action<object?>(CancellationTokenCanceledEventHandler);
        private static void CancellationTokenCanceledEventHandler(object? obj)
        {
            Debug.Assert(obj is SemaphoreSlim, "Expected a SemaphoreSlim");
            SemaphoreSlim semaphore = (SemaphoreSlim)obj;
            lock (semaphore.m_lockObjAndDisposed)
            {
                Monitor.PulseAll(semaphore.m_lockObjAndDisposed); // wake up all waiters.
            }
        }

        /// <summary>
        /// Checks the dispose status by checking the lock object, if it is null means that object
        /// has been disposed and throw ObjectDisposedException
        /// </summary>
        private void CheckDispose()
        {
            ObjectDisposedException.ThrowIf(m_lockObjAndDisposed.Value, this);
        }
        #endregion
    }
}
