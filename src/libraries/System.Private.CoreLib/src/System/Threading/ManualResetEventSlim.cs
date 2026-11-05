// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    // ManualResetEventSlim wraps a manual-reset event internally with a little bit of
    // spinning. When an event will be set imminently, it is often advantageous to avoid
    // a 4k+ cycle context switch in favor of briefly spinning. Therefore we layer on to
    // a brief amount of spinning that should, on the average, make using the slim event
    // cheaper than using Win32 events directly. This can be reset manually, much like
    // a Win32 manual-reset would be.
    //
    // Notes:
    //     We lazily allocate the Win32 event internally. Therefore, the caller should
    //     always call Dispose to clean it up, just in case. This API is a no-op of the
    //     event wasn't allocated, but if it was, ensures that the event goes away
    //     eagerly, instead of waiting for finalization.

    /// <summary>
    /// Provides a slimmed down version of <see cref="ManualResetEvent"/>.
    /// </summary>
    /// <remarks>
    /// All public and protected members of <see cref="ManualResetEventSlim"/> are thread-safe and may be used
    /// concurrently from multiple threads, with the exception of Dispose, which
    /// must only be used when all other operations on the <see cref="ManualResetEventSlim"/> have
    /// completed, and Reset, which should only be used when no other threads are
    /// accessing the event.
    /// </remarks>
    [DebuggerDisplay("Set = {IsSet}")]
    public partial class ManualResetEventSlim : IDisposable
    {
        // These are the default spin counts we use on single-proc and MP machines.
        private const int DEFAULT_SPIN_SP = 1;

        private ManualResetEvent? m_eventObj; // A true Win32 event used for waiting.
        private const int SpinCountState_MaxValue = (1 << 11) - 1; // 2047
        // ----------- //

        /// <summary>
        /// Gets the underlying <see cref="Threading.WaitHandle"/> object for this <see
        /// cref="ManualResetEventSlim"/>.
        /// </summary>
        /// <value>The underlying <see cref="Threading.WaitHandle"/> event object for this <see
        /// cref="ManualResetEventSlim"/>.</value>
        /// <remarks>
        /// Accessing this property forces initialization of an underlying event object if one hasn't
        /// already been created.  To simply wait on this <see cref="ManualResetEventSlim"/>,
        /// the public Wait methods should be preferred.
        /// </remarks>
        public WaitHandle WaitHandle
        {
            get
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                if (m_eventObj == null)
                {
                    // Lazily initialize the event object if needed.
                    LazyInitializeEvent();
                    Debug.Assert(m_eventObj != null);
                }

                return m_eventObj;
            }
        }

        /// <summary>
        /// Gets whether the event is set.
        /// </summary>
        /// <value>true if the event has is set; otherwise, false.</value>
        public bool IsSet
        {
            get => IsSetCore;
            private set => IsSetCore = value;
        }

        /// <summary>
        /// Gets the number of spin waits that will be occur before falling back to a true wait.
        /// </summary>
        public int SpinCount
        {
            get => SpinCountCore;
            private set => SpinCountCore = value;
        }

        //-----------------------------------------------------------------------------------
        // Constructs a new event, optionally specifying the initial state and spin count.
        // The defaults are that the event is unsignaled and some reasonable default spin.
        //

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualResetEventSlim"/>
        /// class with an initial state of nonsignaled.
        /// </summary>
        public ManualResetEventSlim()
            : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualResetEventSlim"/>
        /// class with a boolean value indicating whether to set the initial state to signaled.
        /// </summary>
        /// <param name="initialState">true to set the initial state signaled; false to set the initial state
        /// to nonsignaled.</param>
        public ManualResetEventSlim(bool initialState)
        {
            // Specify the default spin count, and use default spin if we're
            // on a multi-processor machine. Otherwise, we won't.
            Initialize(initialState, SpinWait.SpinCountForSpinBeforeWait);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualResetEventSlim"/>
        /// class with a Boolean value indicating whether to set the initial state to signaled and a specified
        /// spin count.
        /// </summary>
        /// <param name="initialState">true to set the initial state to signaled; false to set the initial state
        /// to nonsignaled.</param>
        /// <param name="spinCount">The number of spin waits that will occur before falling back to a true
        /// wait.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="spinCount"/> is less than
        /// 0 or greater than the maximum allowed value.</exception>
        public ManualResetEventSlim(bool initialState, int spinCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(spinCount);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(spinCount, SpinCountState_MaxValue);

            // We will suppress default spin  because the user specified a count.
            Initialize(initialState, spinCount);
        }

        /// <summary>
        /// Sets the state of the event to signaled, which allows one or more threads waiting on the event to
        /// proceed.
        /// </summary>
        public void Set()
        {
            Set(false);
        }

        /// <summary>
        /// Sets the state of the event to nonsignaled, which causes threads to block.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="ManualResetEventSlim"/>, <see cref="Reset()"/> is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        public void Reset()

        {
            ResetCore();
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
        /// <remarks>
        /// The caller of this method blocks indefinitely until the current instance is set. The caller will
        /// return immediately if the event is currently in a set state.
        /// </remarks>
#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public void Wait()
        {
            Wait(Timeout.Infinite, CancellationToken.None);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> receives a signal,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to
        /// observe.</param>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was
        /// canceled.</exception>
        /// <remarks>
        /// The caller of this method blocks indefinitely until the current instance is set. The caller will
        /// return immediately if the event is currently in a set state.
        /// </remarks>
#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public void Wait(CancellationToken cancellationToken)
        {
            Wait(Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set, using a
        /// <see cref="TimeSpan"/> to measure the time interval.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>true if the <see cref="ManualResetEventSlim"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater
        /// than <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public bool Wait(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;

            ArgumentOutOfRangeException.ThrowIfLessThan(totalMilliseconds, -1, nameof(timeout));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(totalMilliseconds, int.MaxValue, nameof(timeout));

            return Wait((int)totalMilliseconds, CancellationToken.None);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set, using a
        /// <see cref="TimeSpan"/> to measure the time interval, while observing a <see
        /// cref="CancellationToken"/>.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to
        /// observe.</param>
        /// <returns>true if the <see cref="ManualResetEventSlim"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater
        /// than <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref
        /// name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;

            ArgumentOutOfRangeException.ThrowIfLessThan(totalMilliseconds, -1, nameof(timeout));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(totalMilliseconds, int.MaxValue, nameof(timeout));

            return Wait((int)totalMilliseconds, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set, using a
        /// 32-bit signed integer to measure the time interval.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="Timeout.Infinite"/>(-1) to wait indefinitely.</param>
        /// <returns>true if the <see cref="ManualResetEventSlim"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public bool Wait(int millisecondsTimeout)
        {
            return Wait(millisecondsTimeout, CancellationToken.None);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set, using a
        /// 32-bit signed integer to measure the time interval, while observing a <see
        /// cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="Timeout.Infinite"/>(-1) to wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to
        /// observe.</param>
        /// <returns>true if the <see cref="ManualResetEventSlim"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref
        /// name="cancellationToken"/> was canceled.</exception>
#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)

        {
            return WaitCore(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="ManualResetEventSlim"/>.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="ManualResetEventSlim"/>, <see cref="Dispose()"/> is not
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
        /// Unlike most of the members of <see cref="ManualResetEventSlim"/>, <see cref="Dispose(bool)"/> is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        protected virtual void Dispose(bool disposing)

        {
            DisposeCore(disposing);
        }
    }
}
