// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// A simple coordination data structure that we use for fork/join style parallelism.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// Represents a synchronization primitive that is signaled when its count reaches zero.
    /// </summary>
    /// <remarks>
    /// All public and protected members of <see cref="CountdownEvent"/> are thread-safe and may be used
    /// concurrently from multiple threads, with the exception of Dispose, which
    /// must only be used when all other operations on the <see cref="CountdownEvent"/> have
    /// completed, and Reset, which should only be used when no other threads are
    /// accessing the event.
    /// </remarks>
    [DebuggerDisplay("Initial Count={InitialCount}, Current Count={CurrentCount}")]
    public class CountdownEvent : IDisposable
    {
        // CountdownEvent is a simple synchronization primitive used for fork/join parallelism. We create a
        // latch with a count of N; threads then signal the latch, which decrements N by 1; other threads can
        // wait on the latch at any point; when the latch count reaches 0, all threads are woken and
        // subsequent waiters return without waiting. The implementation internally lazily creates a true
        // Win32 event as needed. We also use some amount of spinning on MP machines before falling back to a
        // wait.

        private int _initialCount; // The original # of signals the latch was instantiated with.
        private volatile int _currentCount;  // The # of outstanding signals before the latch transitions to a signaled state.
        private ManualResetEventSlim _event;   // An event used to manage blocking and signaling.
        private volatile bool _disposed; // Whether the latch has been disposed.

        /// <summary>
        /// Initializes a new instance of <see cref="T:System.Threading.CountdownEvent"/> class with the
        /// specified count.
        /// </summary>
        /// <param name="initialCount">The number of signals required to set the <see
        /// cref="T:System.Threading.CountdownEvent"/>.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="initialCount"/> is less
        /// than 0.</exception>
        public CountdownEvent(int initialCount)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCount));
            }

            _initialCount = initialCount;
            _currentCount = initialCount;

            // Allocate a thin event, which internally defers creation of an actual Win32 event.
            _event = new ManualResetEventSlim();

            // If the latch was created with a count of 0, then it's already in the signaled state.
            if (initialCount == 0)
            {
                _event.Set();
            }
        }

        /// <summary>
        /// Gets the number of remaining signals required to set the event.
        /// </summary>
        /// <value>
        /// The number of remaining signals required to set the event.
        /// </value>
        public int CurrentCount
        {
            get
            {
                int observedCount = _currentCount;
                return observedCount < 0 ? 0 : observedCount;
            }
        }

        /// <summary>
        /// Gets the numbers of signals initially required to set the event.
        /// </summary>
        /// <value>
        /// The number of signals initially required to set the event.
        /// </value>
        public int InitialCount
        {
            get { return _initialCount; }
        }

        /// <summary>
        /// Determines whether the event is set.
        /// </summary>
        /// <value>true if the event is set; otherwise, false.</value>
        public bool IsSet
        {
            get
            {
                // The latch is "completed" if its current count has reached 0. Note that this is NOT
                // the same thing is checking the event's IsSet property. There is a tiny window
                // of time, after the final decrement of the current count to 0 and before setting the
                // event, where the two values are out of sync.
                return (_currentCount <= 0);
            }
        }

        /// <summary>
        /// Gets a <see cref="T:System.Threading.WaitHandle"/> that is used to wait for the event to be set. 
        /// </summary>
        /// <value>A <see cref="T:System.Threading.WaitHandle"/> that is used to wait for the event to be set.</value>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <remarks>
        /// <see cref="WaitHandle"/> should only be used if it's needed for integration with code bases
        /// that rely on having a WaitHandle.  If all that's needed is to wait for the <see cref="CountdownEvent"/>
        /// to be set, the <see cref="Wait()"/> method should be preferred.
        /// </remarks>
        public WaitHandle WaitHandle
        {
            get
            {
                ThrowIfDisposed();
                return _event.WaitHandle;
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="T:System.Threading.CountdownEvent"/>.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="CountdownEvent"/>, <see cref="Dispose()"/> is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        public void Dispose()
        {
            // Gets rid of this latch's associated resources. This can consist of a Win32 event
            // which is (lazily) allocated by the underlying thin event. This method is not safe to
            // call concurrently -- i.e. a caller must coordinate to ensure only one thread is using
            // the latch at the time of the call to Dispose.

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// When overridden in a derived class, releases the unmanaged resources used by the
        /// <see cref="T:System.Threading.CountdownEvent"/>, and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release
        /// only unmanaged resources.</param>
        /// <remarks>
        /// Unlike most of the members of <see cref="CountdownEvent"/>, <see cref="Dispose()"/> is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _event.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Registers a signal with the <see cref="T:System.Threading.CountdownEvent"/>, decrementing its
        /// count.
        /// </summary>
        /// <returns>true if the signal caused the count to reach zero and the event was set; otherwise,
        /// false.</returns>
        /// <exception cref="T:System.InvalidOperationException">The current instance is already set.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public bool Signal()
        {
            ThrowIfDisposed();
            Debug.Assert(_event != null);

            if (_currentCount <= 0)
            {
                throw new InvalidOperationException(SR.CountdownEvent_Decrement_BelowZero);
            }

            int newCount = Interlocked.Decrement(ref _currentCount);
            if (newCount == 0)
            {
                _event.Set();
                return true;
            }
            else if (newCount < 0)
            {
                //if the count is decremented below zero, then throw, it's OK to keep the count negative, and we shouldn't set the event here
                //because there was a thread already which decremented it to zero and set the event
                throw new InvalidOperationException(SR.CountdownEvent_Decrement_BelowZero);
            }

            return false;
        }

        /// <summary>
        /// Registers multiple signals with the <see cref="T:System.Threading.CountdownEvent"/>,
        /// decrementing its count by the specified amount.
        /// </summary>
        /// <param name="signalCount">The number of signals to register.</param>
        /// <returns>true if the signals caused the count to reach zero and the event was set; otherwise,
        /// false.</returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// The current instance is already set. -or- Or <paramref name="signalCount"/> is greater than <see
        /// cref="CurrentCount"/>.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="signalCount"/> is less
        /// than 1.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public bool Signal(int signalCount)
        {
            if (signalCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(signalCount));
            }

            ThrowIfDisposed();
            Debug.Assert(_event != null);

            int observedCount;
            SpinWait spin = new SpinWait();
            while (true)
            {
                observedCount = _currentCount;

                // If the latch is already signaled, we will fail.
                if (observedCount < signalCount)
                {
                    throw new InvalidOperationException(SR.CountdownEvent_Decrement_BelowZero);
                }

                if (Interlocked.CompareExchange(ref _currentCount, observedCount - signalCount, observedCount) == observedCount)
                {
                    break;
                }

                // The CAS failed.  Spin briefly and try again.
                spin.SpinOnce(sleep1Threshold: -1);
            }

            // If we were the last to signal, set the event.
            if (observedCount == signalCount)
            {
                _event.Set();
                return true;
            }

            Debug.Assert(_currentCount >= 0, "latch was decremented below zero");
            return false;
        }

        /// <summary>
        /// Increments the <see cref="T:System.Threading.CountdownEvent"/>'s current count by one.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The current instance is already
        /// set.</exception>
        /// <exception cref="T:System.InvalidOperationException"><see cref="CurrentCount"/> is equal to <see
        /// cref="T:System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// The current instance has already been disposed.
        /// </exception>
        public void AddCount()
        {
            AddCount(1);
        }

        /// <summary>
        /// Attempts to increment the <see cref="T:System.Threading.CountdownEvent"/>'s current count by one.
        /// </summary>
        /// <returns>true if the increment succeeded; otherwise, false. If <see cref="CurrentCount"/> is
        /// already at zero. this will return false.</returns>
        /// <exception cref="T:System.InvalidOperationException"><see cref="CurrentCount"/> is equal to <see
        /// cref="T:System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public bool TryAddCount()
        {
            return TryAddCount(1);
        }

        /// <summary>
        /// Increments the <see cref="T:System.Threading.CountdownEvent"/>'s current count by a specified
        /// value.
        /// </summary>
        /// <param name="signalCount">The value by which to increase <see cref="CurrentCount"/>.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="signalCount"/> is less than
        /// 0.</exception>
        /// <exception cref="T:System.InvalidOperationException">The current instance is already
        /// set.</exception>
        /// <exception cref="T:System.InvalidOperationException"><see cref="CurrentCount"/> is equal to <see
        /// cref="T:System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public void AddCount(int signalCount)
        {
            if (!TryAddCount(signalCount))
            {
                throw new InvalidOperationException(SR.CountdownEvent_Increment_AlreadyZero);
            }
        }

        /// <summary>
        /// Attempts to increment the <see cref="T:System.Threading.CountdownEvent"/>'s current count by a
        /// specified value.
        /// </summary>
        /// <param name="signalCount">The value by which to increase <see cref="CurrentCount"/>.</param>
        /// <returns>true if the increment succeeded; otherwise, false. If <see cref="CurrentCount"/> is
        /// already at zero this will return false.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="signalCount"/> is less
        /// than 0.</exception>
        /// <exception cref="T:System.InvalidOperationException">The current instance is already
        /// set.</exception>
        /// <exception cref="T:System.InvalidOperationException"><see cref="CurrentCount"/> is equal to <see
        /// cref="T:System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public bool TryAddCount(int signalCount)
        {
            if (signalCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(signalCount));
            }

            ThrowIfDisposed();

            // Loop around until we successfully increment the count.
            int observedCount;
            SpinWait spin = new SpinWait();
            while (true)
            {
                observedCount = _currentCount;

                if (observedCount <= 0)
                {
                    return false;
                }
                else if (observedCount > (int.MaxValue - signalCount))
                {
                    throw new InvalidOperationException(SR.CountdownEvent_Increment_AlreadyMax);
                }

                if (Interlocked.CompareExchange(ref _currentCount, observedCount + signalCount, observedCount) == observedCount)
                {
                    break;
                }

                // The CAS failed.  Spin briefly and try again.
                spin.SpinOnce(sleep1Threshold: -1);
            }

            return true;
        }

        /// <summary>
        /// Resets the <see cref="CurrentCount"/> to the value of <see cref="InitialCount"/>.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="CountdownEvent"/>, Reset is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public void Reset()
        {
            Reset(_initialCount);
        }

        /// <summary>
        /// Resets the <see cref="CurrentCount"/> to a specified value.
        /// </summary>
        /// <param name="count">The number of signals required to set the <see
        /// cref="T:System.Threading.CountdownEvent"/>.</param>
        /// <remarks>
        /// Unlike most of the members of <see cref="CountdownEvent"/>, Reset is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="count"/> is
        /// less than 0.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been disposed.</exception>
        public void Reset(int count)
        {
            ThrowIfDisposed();

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _currentCount = count;
            _initialCount = count;

            if (count == 0)
            {
                _event.Set();
            }
            else
            {
                _event.Reset();
            }
        }

        /// <summary>
        /// Blocks the current thread until the <see cref="T:System.Threading.CountdownEvent"/> is set.
        /// </summary>
        /// <remarks>
        /// The caller of this method blocks indefinitely until the current instance is set. The caller will
        /// return immediately if the event is currently in a set state.
        /// </remarks>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public void Wait()
        {
            Wait(Timeout.Infinite, new CancellationToken());
        }


        /// <summary>
        /// Blocks the current thread until the <see cref="T:System.Threading.CountdownEvent"/> is set, while
        /// observing a <see cref="T:System.Threading.CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"/> to
        /// observe.</param>
        /// <remarks>
        /// The caller of this method blocks indefinitely until the current instance is set. The caller will
        /// return immediately if the event is currently in a set state.  If the 
        /// <see cref="T:System.Threading.CancellationToken">CancellationToken</see> being observed
        /// is canceled during the wait operation, an <see cref="T:System.OperationCanceledException"/>
        /// will be thrown.
        /// </remarks>
        /// <exception cref="T:System.OperationCanceledException"><paramref name="cancellationToken"/> has been
        /// canceled.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public void Wait(CancellationToken cancellationToken)
        {
            Wait(Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until the <see cref="T:System.Threading.CountdownEvent"/> is set, using a
        /// <see cref="T:System.TimeSpan"/> to measure the time interval.
        /// </summary>
        /// <param name="timeout">A <see cref="T:System.TimeSpan"/> that represents the number of
        /// milliseconds to wait, or a <see cref="T:System.TimeSpan"/> that represents -1 milliseconds to
        /// wait indefinitely.</param>
        /// <returns>true if the <see cref="System.Threading.CountdownEvent"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater
        /// than <see cref="System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public bool Wait(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return Wait((int)totalMilliseconds, new CancellationToken());
        }

        /// <summary>
        /// Blocks the current thread until the <see cref="T:System.Threading.CountdownEvent"/> is set, using
        /// a <see cref="T:System.TimeSpan"/> to measure the time interval, while observing a
        /// <see cref="T:System.Threading.CancellationToken"/>.
        /// </summary>
        /// <param name="timeout">A <see cref="T:System.TimeSpan"/> that represents the number of
        /// milliseconds to wait, or a <see cref="T:System.TimeSpan"/> that represents -1 milliseconds to
        /// wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"/> to
        /// observe.</param>
        /// <returns>true if the <see cref="System.Threading.CountdownEvent"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater
        /// than <see cref="System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        /// <exception cref="T:System.OperationCanceledException"><paramref name="cancellationToken"/> has
        /// been canceled.</exception>
        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return Wait((int)totalMilliseconds, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until the <see cref="T:System.Threading.CountdownEvent"/> is set, using a
        /// 32-bit signed integer to measure the time interval.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="Timeout.Infinite"/>(-1) to wait indefinitely.</param>
        /// <returns>true if the <see cref="System.Threading.CountdownEvent"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        public bool Wait(int millisecondsTimeout)
        {
            return Wait(millisecondsTimeout, new CancellationToken());
        }

        /// <summary>
        /// Blocks the current thread until the <see cref="T:System.Threading.CountdownEvent"/> is set, using a
        /// 32-bit signed integer to measure the time interval, while observing a
        /// <see cref="T:System.Threading.CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="Timeout.Infinite"/>(-1) to wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"/> to
        /// observe.</param>
        /// <returns>true if the <see cref="System.Threading.CountdownEvent"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        /// <exception cref="T:System.OperationCanceledException"><paramref name="cancellationToken"/> has
        /// been canceled.</exception>
        public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout));
            }

            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            // Check whether the event is already set.  This is checked instead of this.IsSet, as this.Signal
            // will first decrement the count and then if it's 0 will set the event, thus it's possible
            // we could observe this.IsSet as true while _event.IsSet is false; that could in turn lead
            // a caller to think it's safe to use Reset, while an operation is still in flight calling _event.Set.
            bool returnValue = _event.IsSet;

            // If not completed yet, wait on the event.
            if (!returnValue)
            {
                // ** the actual wait
                returnValue = _event.Wait(millisecondsTimeout, cancellationToken);
                //the Wait will throw OCE itself if the token is canceled.
            }

            return returnValue;
        }

        // --------------------------------------
        // Private methods


        /// <summary>
        /// Throws an exception if the latch has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("CountdownEvent");
            }
        }
    }
}
