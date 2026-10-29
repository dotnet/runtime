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
    public partial class SemaphoreSlim : IDisposable
    {

        // The maximum semaphore value, it is initialized to Int.MaxValue if the client didn't specify it. it is used
        // to check if the count exceeded the maximum value or not.
        private readonly int m_maxCount;

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


        /// <summary>
        /// Gets the current count of the <see cref="SemaphoreSlim"/>.
        /// </summary>
        /// <value>The current count of the <see cref="SemaphoreSlim"/>.</value>
        public int CurrentCount => m_currentCount;

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
        public WaitHandle AvailableWaitHandle => AvailableWaitHandleCore;


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
#if FEATURE_MULTITHREADING
            m_lockObjAndDisposed = new StrongBox<bool>();
#endif
        }


        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreSlim"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        [UnsupportedOSPlatform("browser")]
        public void Wait()
        {
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
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsTimeout), millisecondsTimeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
            }

            return WaitCore(millisecondsTimeout, cancellationToken);
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
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsTimeout), millisecondsTimeout, SR.SemaphoreSlim_Wait_TimeoutWrong);
            }
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

        private Task<bool> WaitAsyncCoreLocked(long millisecondsTimeout, CancellationToken cancellationToken)
        {
            AssertIsLocked();

            if (m_currentCount > 0)
            {
                --m_currentCount;
                if (m_waitHandle is not null && m_currentCount == 0) m_waitHandle.Reset();
                return Task.FromResult(true);
            }
            else if (millisecondsTimeout == 0)
            {
                return Task.FromResult(false);
            }
            else
            {
                Debug.Assert(m_currentCount == 0, "m_currentCount should never be negative");
                TaskNode asyncWaiter = CreateAndAddAsyncWaiter();
                return (millisecondsTimeout == Timeout.Infinite && !cancellationToken.CanBeCanceled) ?
                    asyncWaiter :
                    WaitUntilCountOrTimeoutAsync(asyncWaiter, millisecondsTimeout, cancellationToken);
            }
        }

        /// <summary>Creates a new task and stores it into the async waiters list.</summary>
        /// <returns>The created task.</returns>
        private TaskNode CreateAndAddAsyncWaiter()
        {
            AssertIsLocked();

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
            AssertIsLocked();

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
            AssertIsLocked();

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

            // Resolve cancellation or timeout against release by removing the waiter.
            if (RemoveAsyncWaiterWithLock(asyncWaiter))
            {
                cancellationToken.ThrowIfCancellationRequested(); // cancellation occurred
                return false; // timeout occurred
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
            return ReleaseCore(releaseCount);
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
            DisposeCore(disposing);
        }

        /// <summary>
        /// Checks whether the semaphore has been disposed.
        /// </summary>
        private void CheckDispose()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
        }
    }
}
