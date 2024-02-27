// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    /// <summary>
    /// Provides a way to get mutual exclusion in regions of code between different threads. A lock may be held by one thread at
    /// a time.
    /// </summary>
    /// <remarks>
    /// Threads that cannot immediately enter the lock may wait for the lock to be exited or until a specified timeout. A thread
    /// that holds a lock may enter the lock repeatedly without exiting it, such as recursively, in which case the thread should
    /// eventually exit the lock the same number of times to fully exit the lock and allow other threads to enter the lock.
    /// </remarks>
    [Runtime.Versioning.RequiresPreviewFeatures]
    public sealed partial class Lock
    {
        private const short DefaultMaxSpinCount = 22;
        private const short DefaultAdaptiveSpinPeriod = 100;
        private const short SpinSleep0Threshold = 10;
        private const ushort MaxDurationMsForPreemptingWaiters = 100;

        private static long s_contentionCount;

        // The field's type is not ThreadId to try to retain the relative order of fields of intrinsic types. The type system
        // appears to place struct fields after fields of other types, in which case there can be a greater chance that
        // _owningThreadId is not in the same cache line as _state.
#if TARGET_OSX && !NATIVEAOT
        private ulong _owningThreadId;
#else
        private uint _owningThreadId;
#endif

        private uint _state; // see State for layout
        private uint _recursionCount;
        private short _spinCount;
        private ushort _waiterStartTimeMs;
        private AutoResetEvent? _waitEvent;

        /// <summary>
        /// Enters the lock. Once the method returns, the calling thread would be the only thread that holds the lock.
        /// </summary>
        /// <remarks>
        /// If the lock cannot be entered immediately, the calling thread waits for the lock to be exited. If the lock is
        /// already held by the calling thread, the lock is entered again. The calling thread should exit the lock as many times
        /// as it had entered the lock to fully exit the lock and allow other threads to enter the lock.
        /// </remarks>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Enter()
        {
            ThreadId currentThreadId = TryEnter_Inlined(timeoutMs: -1);
            Debug.Assert(currentThreadId.IsInitialized);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ThreadId EnterAndGetCurrentThreadId()
        {
            ThreadId currentThreadId = TryEnter_Inlined(timeoutMs: -1);
            Debug.Assert(currentThreadId.IsInitialized);
            Debug.Assert(currentThreadId.Id == _owningThreadId);
            return currentThreadId;
        }

        /// <summary>
        /// Enters the lock and returns a <see cref="Scope"/> that may be disposed to exit the lock. Once the method returns,
        /// the calling thread would be the only thread that holds the lock. This method is intended to be used along with a
        /// language construct that would automatically dispose the <see cref="Scope"/>, such as with the C# <code>using</code>
        /// statement.
        /// </summary>
        /// <returns>
        /// A <see cref="Scope"/> that may be disposed to exit the lock.
        /// </returns>
        /// <remarks>
        /// If the lock cannot be entered immediately, the calling thread waits for the lock to be exited. If the lock is
        /// already held by the calling thread, the lock is entered again. The calling thread should exit the lock, such as by
        /// disposing the returned <see cref="Scope"/>, as many times as it had entered the lock to fully exit the lock and
        /// allow other threads to enter the lock.
        /// </remarks>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Scope EnterScope() => new Scope(this, EnterAndGetCurrentThreadId());

        /// <summary>
        /// A disposable structure that is returned by <see cref="EnterScope()"/>, which when disposed, exits the lock.
        /// </summary>
        public ref struct Scope
        {
            private Lock? _lockObj;
            private ThreadId _currentThreadId;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Scope(Lock lockObj, ThreadId currentThreadId)
            {
                _lockObj = lockObj;
                _currentThreadId = currentThreadId;
            }

            /// <summary>
            /// Exits the lock.
            /// </summary>
            /// <remarks>
            /// If the calling thread holds the lock multiple times, such as recursively, the lock is exited only once. The
            /// calling thread should ensure that each enter is matched with an exit.
            /// </remarks>
            /// <exception cref="SynchronizationLockException">
            /// The calling thread does not hold the lock.
            /// </exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                Lock? lockObj = _lockObj;
                if (lockObj is not null)
                {
                    _lockObj = null;
                    lockObj.Exit(_currentThreadId);
                }
            }
        }

        /// <summary>
        /// Tries to enter the lock without waiting. If the lock is entered, the calling thread would be the only thread that
        /// holds the lock.
        /// </summary>
        /// <returns>
        /// <code>true</code> if the lock was entered, <code>false</code> otherwise.
        /// </returns>
        /// <remarks>
        /// If the lock cannot be entered immediately, the method returns <code>false</code>. If the lock is already held by the
        /// calling thread, the lock is entered again. The calling thread should exit the lock as many times as it had entered
        /// the lock to fully exit the lock and allow other threads to enter the lock.
        /// </remarks>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TryEnter() => TryEnter_Inlined(timeoutMs: 0).IsInitialized;

        /// <summary>
        /// Tries to enter the lock, waiting for roughly the specified duration. If the lock is entered, the calling thread
        /// would be the only thread that holds the lock.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The rough duration in milliseconds for which the method will wait if the lock is not available. A value of
        /// <code>0</code> specifies that the method should not wait, and a value of <see cref="Timeout.Infinite"/> or
        /// <code>-1</code> specifies that the method should wait indefinitely until the lock is entered.
        /// </param>
        /// <returns>
        /// <code>true</code> if the lock was entered, <code>false</code> otherwise.
        /// </returns>
        /// <remarks>
        /// If the lock cannot be entered immediately, the calling thread waits for roughly the specified duration for the lock
        /// to be exited. If the lock is already held by the calling thread, the lock is entered again. The calling thread
        /// should exit the lock as many times as it had entered the lock to fully exit the lock and allow other threads to
        /// enter the lock.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="millisecondsTimeout"/> is less than <code>-1</code>.
        /// </exception>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        public bool TryEnter(int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);
            return TryEnter_Outlined(millisecondsTimeout);
        }

        /// <summary>
        /// Tries to enter the lock, waiting for roughly the specified duration. If the lock is entered, the calling thread
        /// would be the only thread that holds the lock.
        /// </summary>
        /// <param name="timeout">
        /// The rough duration for which the method will wait if the lock is not available. The timeout is converted to a number
        /// of milliseconds by casting <see cref="TimeSpan.TotalMilliseconds"/> of the timeout to an integer value. A value
        /// representing <code>0</code> milliseconds specifies that the method should not wait, and a value representing
        /// <see cref="Timeout.Infinite"/> or <code>-1</code> milliseconds specifies that the method should wait indefinitely
        /// until the lock is entered.
        /// </param>
        /// <returns>
        /// <code>true</code> if the lock was entered, <code>false</code> otherwise.
        /// </returns>
        /// <remarks>
        /// If the lock cannot be entered immediately, the calling thread waits for roughly the specified duration for the lock
        /// to be exited. If the lock is already held by the calling thread, the lock is entered again. The calling thread
        /// should exit the lock as many times as it had entered the lock to fully exit the lock and allow other threads to
        /// enter the lock.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="timeout"/>, after its conversion to an integer millisecond value, represents a value that is less
        /// than <code>-1</code> milliseconds or greater than <see cref="int.MaxValue"/> milliseconds.
        /// </exception>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        public bool TryEnter(TimeSpan timeout) => TryEnter_Outlined(WaitHandle.ToTimeoutMilliseconds(timeout));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryEnter_Outlined(int timeoutMs) => TryEnter_Inlined(timeoutMs).IsInitialized;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ThreadId TryEnter_Inlined(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

            ThreadId currentThreadId = ThreadId.Current_NoInitialize;
            if (currentThreadId.IsInitialized && State.TryLock(this))
            {
                Debug.Assert(!new ThreadId(_owningThreadId).IsInitialized);
                Debug.Assert(_recursionCount == 0);
                _owningThreadId = currentThreadId.Id;
                return currentThreadId;
            }

            return TryEnterSlow(timeoutMs, currentThreadId);
        }

        /// <summary>
        /// Exits the lock.
        /// </summary>
        /// <remarks>
        /// If the calling thread holds the lock multiple times, such as recursively, the lock is exited only once. The
        /// calling thread should ensure that each enter is matched with an exit.
        /// </remarks>
        /// <exception cref="SynchronizationLockException">
        /// The calling thread does not hold the lock.
        /// </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Exit()
        {
            var owningThreadId = new ThreadId(_owningThreadId);
            if (!owningThreadId.IsInitialized || owningThreadId.Id != ThreadId.Current_NoInitialize.Id)
            {
                ThrowHelper.ThrowSynchronizationLockException_LockExit();
            }

            ExitImpl();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Exit(ThreadId currentThreadId)
        {
            Debug.Assert(currentThreadId.IsInitialized);
            Debug.Assert(currentThreadId.Id == ThreadId.Current_NoInitialize.Id);

            if (_owningThreadId != currentThreadId.Id)
            {
                ThrowHelper.ThrowSynchronizationLockException_LockExit();
            }

            ExitImpl();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExitImpl()
        {
            Debug.Assert(new ThreadId(_owningThreadId).IsInitialized);
            Debug.Assert(_owningThreadId == ThreadId.Current_NoInitialize.Id);
            Debug.Assert(new State(this).IsLocked);

            if (_recursionCount == 0)
            {
                _owningThreadId = 0;

                State state = State.Unlock(this);
                if (state.HasAnyWaiters)
                {
                    SignalWaiterIfNecessary(state);
                }
            }
            else
            {
                _recursionCount--;
            }
        }

        private static bool IsAdaptiveSpinEnabled(short minSpinCount) => minSpinCount <= 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ThreadId TryEnterSlow(int timeoutMs, ThreadId currentThreadId)
        {
            Debug.Assert(timeoutMs >= -1);

            if (!currentThreadId.IsInitialized)
            {
                // The thread info hasn't been initialized yet for this thread, and the fast path hasn't been tried yet. After
                // initializing the thread info, try the fast path first.
                currentThreadId.InitializeForCurrentThread();
                Debug.Assert(_owningThreadId != currentThreadId.Id);
                if (State.TryLock(this))
                {
                    goto Locked;
                }
            }
            else if (_owningThreadId == currentThreadId.Id)
            {
                Debug.Assert(new State(this).IsLocked);

                uint newRecursionCount = _recursionCount + 1;
                if (newRecursionCount != 0)
                {
                    _recursionCount = newRecursionCount;
                    return currentThreadId;
                }

                throw new LockRecursionException(SR.Lock_Enter_LockRecursionException);
            }

            if (timeoutMs == 0)
            {
                return new ThreadId(0);
            }

            if (LazyInitializeOrEnter() == TryLockResult.Locked)
            {
                goto Locked;
            }

            bool isSingleProcessor = IsSingleProcessor;
            short maxSpinCount = s_maxSpinCount;
            if (maxSpinCount == 0)
            {
                goto Wait;
            }

            short minSpinCount = s_minSpinCount;
            short spinCount = _spinCount;
            if (spinCount < 0)
            {
                // When negative, the spin count serves as a counter for contentions such that a spin-wait can be attempted
                // periodically to see if it would be beneficial. Increment the spin count and skip spin-waiting.
                Debug.Assert(IsAdaptiveSpinEnabled(minSpinCount));
                _spinCount = (short)(spinCount + 1);
                goto Wait;
            }

            // Try to acquire the lock, and check if non-waiters should stop preempting waiters. If this thread should not
            // preempt waiters, skip spin-waiting. Upon contention, register a spinner.
            TryLockResult tryLockResult = State.TryLockBeforeSpinLoop(this, spinCount, out bool isFirstSpinner);
            if (tryLockResult != TryLockResult.Spin)
            {
                goto LockedOrWait;
            }

            // Lock was not acquired and a spinner was registered

            if (isFirstSpinner)
            {
                // Whether a full-length spin-wait would be effective is determined by having the first spinner do a full-length
                // spin-wait to see if it is effective. Shorter spin-waits would more often be ineffective just because they are
                // shorter.
                spinCount = maxSpinCount;
            }

            for (short spinIndex = 0; ;)
            {
                LowLevelSpinWaiter.Wait(spinIndex, SpinSleep0Threshold, isSingleProcessor);

                if (++spinIndex >= spinCount)
                {
                    // The last lock attempt for this spin will be done after the loop
                    break;
                }

                // Try to acquire the lock and unregister the spinner
                tryLockResult = State.TryLockInsideSpinLoop(this);
                if (tryLockResult == TryLockResult.Spin)
                {
                    continue;
                }

                if (tryLockResult == TryLockResult.Locked)
                {
                    if (isFirstSpinner && IsAdaptiveSpinEnabled(minSpinCount))
                    {
                        // Since the first spinner does a full-length spin-wait, and to keep upward and downward changes to the
                        // spin count more balanced, only the first spinner adjusts the spin count
                        spinCount = _spinCount;
                        if (spinCount < maxSpinCount)
                        {
                            _spinCount = (short)(spinCount + 1);
                        }
                    }

                    goto Locked;
                }

                // The lock was not acquired and the spinner was not unregistered, stop spinning
                Debug.Assert(tryLockResult == TryLockResult.Wait);
                break;
            }

            // Unregister the spinner and try to acquire the lock
            tryLockResult = State.TryLockAfterSpinLoop(this);
            if (isFirstSpinner && IsAdaptiveSpinEnabled(minSpinCount))
            {
                // Since the first spinner does a full-length spin-wait, and to keep upward and downward changes to the
                // spin count more balanced, only the first spinner adjusts the spin count
                if (tryLockResult == TryLockResult.Locked)
                {
                    spinCount = _spinCount;
                    if (spinCount < maxSpinCount)
                    {
                        _spinCount = (short)(spinCount + 1);
                    }
                }
                else
                {
                    // If the spin count is already zero, skip spin-waiting for a while, even for the first spinners. After a
                    // number of contentions, the first spinner will attempt a spin-wait again to see if it is effective.
                    Debug.Assert(tryLockResult == TryLockResult.Wait);
                    spinCount = _spinCount;
                    _spinCount = spinCount > 0 ? (short)(spinCount - 1) : minSpinCount;
                }
            }

        LockedOrWait:
            Debug.Assert(tryLockResult != TryLockResult.Spin);
            if (tryLockResult == TryLockResult.Wait)
            {
                goto Wait;
            }

            Debug.Assert(tryLockResult == TryLockResult.Locked);

        Locked:
            Debug.Assert(!new ThreadId(_owningThreadId).IsInitialized);
            Debug.Assert(_recursionCount == 0);
            _owningThreadId = currentThreadId.Id;
            return currentThreadId;

        Wait:
            bool areContentionEventsEnabled =
                NativeRuntimeEventSource.Log?.IsEnabled(
                    EventLevel.Informational,
                    NativeRuntimeEventSource.Keywords.ContentionKeyword) ?? false;
            AutoResetEvent waitEvent = _waitEvent ?? CreateWaitEvent(areContentionEventsEnabled);
            if (State.TryLockBeforeWait(this))
            {
                // Lock was acquired and a waiter was not registered
                goto Locked;
            }

            // Lock was not acquired and a waiter was registered. All following paths need to unregister the waiter, including
            // exceptional paths.
            try
            {
                Interlocked.Increment(ref s_contentionCount);

                long waitStartTimeTicks = 0;
                if (areContentionEventsEnabled)
                {
                    NativeRuntimeEventSource.Log!.ContentionStart(this);
                    waitStartTimeTicks = Stopwatch.GetTimestamp();
                }

                bool acquiredLock = false;
                int waitStartTimeMs = timeoutMs < 0 ? 0 : Environment.TickCount;
                int remainingTimeoutMs = timeoutMs;
                while (true)
                {
                    if (!waitEvent.WaitOne(remainingTimeoutMs))
                    {
                        break;
                    }

                    // Spin a bit while trying to acquire the lock. This has a few benefits:
                    // - Spinning helps to reduce waiter starvation. Since other non-waiter threads can take the lock while
                    //   there are waiters (see State.TryLock()), once a waiter wakes it will be able to better compete with
                    //   other spinners for the lock.
                    // - If there is another thread that is repeatedly acquiring and releasing the lock, spinning before waiting
                    //   again helps to prevent a waiter from repeatedly context-switching in and out
                    // - Further in the same situation above, waking up and waiting shortly thereafter deprioritizes this waiter
                    //   because events release waiters in FIFO order. Spinning a bit helps a waiter to retain its priority at
                    //   least for one spin duration before it gets deprioritized behind all other waiters.
                    for (short spinIndex = 0; spinIndex < maxSpinCount; spinIndex++)
                    {
                        if (State.TryLockInsideWaiterSpinLoop(this))
                        {
                            acquiredLock = true;
                            break;
                        }

                        LowLevelSpinWaiter.Wait(spinIndex, SpinSleep0Threshold, isSingleProcessor);
                    }

                    if (acquiredLock)
                    {
                        break;
                    }

                    if (State.TryLockAfterWaiterSpinLoop(this))
                    {
                        acquiredLock = true;
                        break;
                    }

                    if (remainingTimeoutMs < 0)
                    {
                        continue;
                    }

                    uint waitDurationMs = (uint)(Environment.TickCount - waitStartTimeMs);
                    if (waitDurationMs >= (uint)timeoutMs)
                    {
                        break;
                    }

                    remainingTimeoutMs = timeoutMs - (int)waitDurationMs;
                }

                if (acquiredLock)
                {
                    // In NativeAOT, ensure that class construction cycles do not occur after the lock is acquired but before
                    // the state is fully updated. Update the state to fully reflect that this thread owns the lock before doing
                    // other things.
                    Debug.Assert(!new ThreadId(_owningThreadId).IsInitialized);
                    Debug.Assert(_recursionCount == 0);
                    _owningThreadId = currentThreadId.Id;

                    if (areContentionEventsEnabled)
                    {
                        double waitDurationNs =
                            (Stopwatch.GetTimestamp() - waitStartTimeTicks) * 1_000_000_000.0 / Stopwatch.Frequency;
                        NativeRuntimeEventSource.Log!.ContentionStop(waitDurationNs);
                    }

                    return currentThreadId;
                }
            }
            catch // run this code before exception filters in callers
            {
                State.UnregisterWaiter(this);
                throw;
            }

            State.UnregisterWaiter(this);
            return new ThreadId(0);
        }

        private void ResetWaiterStartTime() => _waiterStartTimeMs = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordWaiterStartTime()
        {
            ushort currentTimeMs = (ushort)Environment.TickCount;
            if (currentTimeMs == 0)
            {
                // Don't record zero, that value is reserved for indicating that a time is not recorded
                currentTimeMs--;
            }
            _waiterStartTimeMs = currentTimeMs;
        }

        private bool ShouldStopPreemptingWaiters
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // If the recorded time is zero, a time has not been recorded yet
                ushort waiterStartTimeMs = _waiterStartTimeMs;
                return
                    waiterStartTimeMs != 0 &&
                    (ushort)Environment.TickCount - waiterStartTimeMs >= MaxDurationMsForPreemptingWaiters;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe AutoResetEvent CreateWaitEvent(bool areContentionEventsEnabled)
        {
            var newWaitEvent = new AutoResetEvent(false);
            AutoResetEvent? waitEventBeforeUpdate = Interlocked.CompareExchange(ref _waitEvent, newWaitEvent, null);
            if (waitEventBeforeUpdate == null)
            {
                // Also check NativeRuntimeEventSource.Log.IsEnabled() to enable trimming
                if (areContentionEventsEnabled && NativeRuntimeEventSource.Log.IsEnabled())
                {
                    NativeRuntimeEventSource.Log.ContentionLockCreated(this);
                }

                return newWaitEvent;
            }

            newWaitEvent.Dispose();
            return waitEventBeforeUpdate;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SignalWaiterIfNecessary(State state)
        {
            if (State.TrySetIsWaiterSignaledToWake(this, state))
            {
                // Signal a waiter to wake
                Debug.Assert(_waitEvent != null);
                bool signaled = _waitEvent.Set();
                Debug.Assert(signaled);
            }
        }

        /// <summary>
        /// <code>true</code> if the lock is held by the calling thread, <code>false</code> otherwise.
        /// </summary>
        public bool IsHeldByCurrentThread
        {
            get
            {
                var owningThreadId = new ThreadId(_owningThreadId);
                bool isHeld = owningThreadId.IsInitialized && owningThreadId.Id == ThreadId.Current_NoInitialize.Id;
                Debug.Assert(!isHeld || new State(this).IsLocked);
                return isHeld;
            }
        }

        internal static long ContentionCount => s_contentionCount;
        internal void Dispose() => _waitEvent?.Dispose();

        internal nint LockIdForEvents
        {
            get
            {
                Debug.Assert(_waitEvent != null);
                return _waitEvent.SafeWaitHandle.DangerousGetHandle();
            }
        }

        internal unsafe nint ObjectIdForEvents
        {
            get
            {
                Lock lockObj = this;
                return *(nint*)Unsafe.AsPointer(ref lockObj);
            }
        }

        internal ulong OwningThreadId => _owningThreadId;

        private static short DetermineMaxSpinCount() =>
            AppContextConfigHelper.GetInt16Config(
                "System.Threading.Lock.SpinCount",
                "DOTNET_Lock_SpinCount",
                DefaultMaxSpinCount,
                allowNegative: false);

        private static short DetermineMinSpinCount()
        {
            // The config var can be set to -1 to disable adaptive spin
            short adaptiveSpinPeriod =
                AppContextConfigHelper.GetInt16Config(
                    "System.Threading.Lock.AdaptiveSpinPeriod",
                    "DOTNET_Lock_AdaptiveSpinPeriod",
                    DefaultAdaptiveSpinPeriod,
                    allowNegative: true);
            if (adaptiveSpinPeriod < -1)
            {
                adaptiveSpinPeriod = DefaultAdaptiveSpinPeriod;
            }

            return (short)-adaptiveSpinPeriod;
        }

        private struct State : IEquatable<State>
        {
            // Layout constants for Lock._state
            private const uint IsLockedMask = (uint)1 << 0; // bit 0
            private const uint ShouldNotPreemptWaitersMask = (uint)1 << 1; // bit 1
            private const uint SpinnerCountIncrement = (uint)1 << 2; // bits 2-4
            private const uint SpinnerCountMask = (uint)0x7 << 2;
            private const uint IsWaiterSignaledToWakeMask = (uint)1 << 5; // bit 5
            private const byte WaiterCountShift = 6;
            private const uint WaiterCountIncrement = (uint)1 << WaiterCountShift; // bits 6-31

            private uint _state;

            public State(Lock lockObj) : this(lockObj._state) { }
            private State(uint state) => _state = state;

            public static uint InitialStateValue => 0;
            public static uint LockedStateValue => IsLockedMask;
            private static uint Neg(uint state) => (uint)-(int)state;
            public bool IsInitialState => this == default;
            public bool IsLocked => (_state & IsLockedMask) != 0;

            private void SetIsLocked()
            {
                Debug.Assert(!IsLocked);
                _state += IsLockedMask;
            }

            private bool ShouldNotPreemptWaiters => (_state & ShouldNotPreemptWaitersMask) != 0;

            private void SetShouldNotPreemptWaiters()
            {
                Debug.Assert(!ShouldNotPreemptWaiters);
                Debug.Assert(HasAnyWaiters);

                _state += ShouldNotPreemptWaitersMask;
            }

            private void ClearShouldNotPreemptWaiters()
            {
                Debug.Assert(ShouldNotPreemptWaiters);
                _state -= ShouldNotPreemptWaitersMask;
            }

            private bool ShouldNonWaiterAttemptToAcquireLock
            {
                get
                {
                    Debug.Assert(HasAnyWaiters || !ShouldNotPreemptWaiters);
                    return (_state & (IsLockedMask | ShouldNotPreemptWaitersMask)) == 0;
                }
            }

            private bool HasAnySpinners => (_state & SpinnerCountMask) != 0;

            private bool TryIncrementSpinnerCount()
            {
                uint newState = _state + SpinnerCountIncrement;
                if (new State(newState).HasAnySpinners) // overflow check
                {
                    _state = newState;
                    return true;
                }
                return false;
            }

            private void DecrementSpinnerCount()
            {
                Debug.Assert(HasAnySpinners);
                _state -= SpinnerCountIncrement;
            }

            private bool IsWaiterSignaledToWake => (_state & IsWaiterSignaledToWakeMask) != 0;

            private void SetIsWaiterSignaledToWake()
            {
                Debug.Assert(HasAnyWaiters);
                Debug.Assert(NeedToSignalWaiter);

                _state += IsWaiterSignaledToWakeMask;
            }

            private void ClearIsWaiterSignaledToWake()
            {
                Debug.Assert(IsWaiterSignaledToWake);
                _state -= IsWaiterSignaledToWakeMask;
            }

            public bool HasAnyWaiters => _state >= WaiterCountIncrement;

            private bool TryIncrementWaiterCount()
            {
                uint newState = _state + WaiterCountIncrement;
                if (new State(newState).HasAnyWaiters) // overflow check
                {
                    _state = newState;
                    return true;
                }
                return false;
            }

            private void DecrementWaiterCount()
            {
                Debug.Assert(HasAnyWaiters);
                _state -= WaiterCountIncrement;
            }

            public bool NeedToSignalWaiter
            {
                get
                {
                    Debug.Assert(HasAnyWaiters);
                    return (_state & (SpinnerCountMask | IsWaiterSignaledToWakeMask)) == 0;
                }
            }

            public static bool operator ==(State state1, State state2) => state1._state == state2._state;
            public static bool operator !=(State state1, State state2) => !(state1 == state2);

            bool IEquatable<State>.Equals(State other) => this == other;
            public override bool Equals(object? obj) => obj is State other && this == other;
            public override int GetHashCode() => (int)_state;

            private static State CompareExchange(Lock lockObj, State toState, State fromState) =>
                new State(Interlocked.CompareExchange(ref lockObj._state, toState._state, fromState._state));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryLock(Lock lockObj)
            {
                // The lock is mostly fair to release waiters in a typically FIFO order (though the order is not guaranteed).
                // However, it allows non-waiters to acquire the lock if it's available to avoid lock convoys.
                //
                // Lock convoys can be detrimental to performance in scenarios where work is being done on multiple threads and
                // the work involves periodically taking a particular lock for a short time to access shared resources. With a
                // lock convoy, once there is a waiter for the lock (which is not uncommon in such scenarios), a worker thread
                // would be forced to context-switch on the subsequent attempt to acquire the lock, often long before the worker
                // thread exhausts its time slice. This process repeats as long as the lock has a waiter, forcing every worker
                // to context-switch on each attempt to acquire the lock, killing performance and creating a positive feedback
                // loop that makes it more likely for the lock to have waiters. To avoid the lock convoy, each worker needs to
                // be allowed to acquire the lock multiple times in sequence despite there being a waiter for the lock in order
                // to have the worker continue working efficiently during its time slice as long as the lock is not contended.
                //
                // This scheme has the possibility to starve waiters. Waiter starvation is mitigated by other means, see
                // TryLockBeforeSpinLoop() and references to ShouldNotPreemptWaiters.

                var state = new State(lockObj);
                if (!state.ShouldNonWaiterAttemptToAcquireLock)
                {
                    return false;
                }

                State newState = state;
                newState.SetIsLocked();

                return CompareExchange(lockObj, newState, state) == state;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static State Unlock(Lock lockObj)
            {
                Debug.Assert(IsLockedMask == 1);

                var state = new State(Interlocked.Decrement(ref lockObj._state));
                Debug.Assert(!state.IsLocked);
                return state;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TryLockResult TryLockBeforeSpinLoop(Lock lockObj, short spinCount, out bool isFirstSpinner)
            {
                // Normally, threads are allowed to preempt waiters to acquire the lock in order to avoid creating lock convoys,
                // see TryLock(). There can be cases where waiters can be easily starved as a result. For example, a thread that
                // holds a lock for a significant amount of time (much longer than the time it takes to do a context switch),
                // then releases and reacquires the lock in quick succession, and repeats. Though a waiter would be woken upon
                // lock release, usually it will not have enough time to context-switch-in and take the lock, and can be starved
                // for an unreasonably long duration.
                //
                // In order to prevent such starvation and force a bit of fair forward progress, it is sometimes necessary to
                // change the normal policy and disallow threads from preempting waiters. ShouldNotPreemptWaiters() indicates
                // the current state of the policy and this method determines whether the policy should be changed to disallow
                // non-waiters from preempting waiters.
                //   - When the first waiter begins waiting, it records the current time as a "waiter starvation start time".
                //     That is a point in time after which no forward progress has occurred for waiters. When a waiter acquires
                //     the lock, the time is updated to the current time.
                //   - This method checks whether the starvation duration has crossed a threshold and if so, sets
                //     ShouldNotPreemptWaitersMask
                //
                // When unreasonable starvation is occurring, the lock will be released occasionally and if caused by spinners,
                // those threads may start to spin again.
                //   - Before starting to spin this method is called. If ShouldNotPreemptWaitersMask is set, the spinner will
                //     skip spinning and wait instead. Spinners that are already registered at the time
                //     ShouldNotPreemptWaitersMask is set will stop spinning as necessary. Eventually, all spinners will drain
                //     and no new ones will be registered.
                //   - Upon releasing a lock, if there are no spinners, a waiter will be signaled to wake. On that path,
                //     TrySetIsWaiterSignaledToWake() is called.
                //   - Eventually, after spinners have drained, only a waiter will be able to acquire the lock. When a waiter
                //     acquires the lock, or when the last waiter unregisters itself, ShouldNotPreemptWaitersMask is cleared to
                //     restore the normal policy.

                Debug.Assert(spinCount >= 0);

                isFirstSpinner = false;
                var state = new State(lockObj);
                while (true)
                {
                    State newState = state;
                    TryLockResult result = TryLockResult.Spin;
                    if (newState.HasAnyWaiters)
                    {
                        if (newState.ShouldNotPreemptWaiters)
                        {
                            return TryLockResult.Wait;
                        }
                        if (lockObj.ShouldStopPreemptingWaiters)
                        {
                            newState.SetShouldNotPreemptWaiters();
                            result = TryLockResult.Wait;
                        }
                    }
                    if (result == TryLockResult.Spin)
                    {
                        Debug.Assert(!newState.ShouldNotPreemptWaiters);
                        if (!newState.IsLocked)
                        {
                            newState.SetIsLocked();
                            result = TryLockResult.Locked;
                        }
                        else if ((newState.HasAnySpinners && spinCount == 0) || !newState.TryIncrementSpinnerCount())
                        {
                            return TryLockResult.Wait;
                        }
                    }

                    State stateBeforeUpdate = CompareExchange(lockObj, newState, state);
                    if (stateBeforeUpdate == state)
                    {
                        if (result == TryLockResult.Spin && !state.HasAnySpinners)
                        {
                            isFirstSpinner = true;
                        }
                        return result;
                    }

                    state = stateBeforeUpdate;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TryLockResult TryLockInsideSpinLoop(Lock lockObj)
            {
                // This method is called from inside a spin loop, it must unregister the spinner if the lock is acquired

                var state = new State(lockObj);
                while (true)
                {
                    Debug.Assert(state.HasAnySpinners);
                    if (!state.ShouldNonWaiterAttemptToAcquireLock)
                    {
                        return state.ShouldNotPreemptWaiters ? TryLockResult.Wait : TryLockResult.Spin;
                    }

                    State newState = state;
                    newState.SetIsLocked();
                    newState.DecrementSpinnerCount();

                    State stateBeforeUpdate = CompareExchange(lockObj, newState, state);
                    if (stateBeforeUpdate == state)
                    {
                        return TryLockResult.Locked;
                    }

                    state = stateBeforeUpdate;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TryLockResult TryLockAfterSpinLoop(Lock lockObj)
            {
                // This method is called at the end of a spin loop, it must unregister the spinner always and acquire the lock
                // if it's available. If the lock is available, a spinner must acquire the lock along with unregistering itself,
                // because a lock releaser does not wake a waiter when there is a spinner registered.

                var state = new State(Interlocked.Add(ref lockObj._state, Neg(SpinnerCountIncrement)));
                Debug.Assert(new State(state._state + SpinnerCountIncrement).HasAnySpinners);

                while (true)
                {
                    Debug.Assert(state.HasAnyWaiters || !state.ShouldNotPreemptWaiters);
                    if (state.IsLocked)
                    {
                        return TryLockResult.Wait;
                    }

                    State newState = state;
                    newState.SetIsLocked();

                    State stateBeforeUpdate = CompareExchange(lockObj, newState, state);
                    if (stateBeforeUpdate == state)
                    {
                        return TryLockResult.Locked;
                    }

                    state = stateBeforeUpdate;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryLockBeforeWait(Lock lockObj)
            {
                // This method is called before waiting. It must either acquire the lock or register a waiter. It also keeps
                // track of the waiter starvation start time.

                var state = new State(lockObj);
                bool waiterStartTimeWasReset = false;
                while (true)
                {
                    State newState = state;
                    if (newState.ShouldNonWaiterAttemptToAcquireLock)
                    {
                        newState.SetIsLocked();
                    }
                    else
                    {
                        if (!newState.TryIncrementWaiterCount())
                        {
                            ThrowHelper.ThrowOutOfMemoryException_LockEnter_WaiterCountOverflow();
                        }

                        if (!state.HasAnyWaiters && !waiterStartTimeWasReset)
                        {
                            // This would be the first waiter. Once the waiter is registered, another thread may check the
                            // waiter starvation start time and the previously recorded value may be stale, causing
                            // ShouldNotPreemptWaitersMask to be set unnecessarily. Reset the start time before registering the
                            // waiter.
                            waiterStartTimeWasReset = true;
                            lockObj.ResetWaiterStartTime();
                        }
                    }

                    State stateBeforeUpdate = CompareExchange(lockObj, newState, state);
                    if (stateBeforeUpdate == state)
                    {
                        if (state.ShouldNonWaiterAttemptToAcquireLock)
                        {
                            return true;
                        }

                        Debug.Assert(state.HasAnyWaiters || waiterStartTimeWasReset);
                        if (!state.HasAnyWaiters || waiterStartTimeWasReset)
                        {
                            // This was the first waiter or the waiter start time was reset, record the waiter start time
                            lockObj.RecordWaiterStartTime();
                        }
                        return false;
                    }

                    state = stateBeforeUpdate;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryLockInsideWaiterSpinLoop(Lock lockObj)
            {
                // This method is called from inside the waiter's spin loop and should observe the wake signal only if the lock
                // is taken, to prevent a lock releaser from waking another waiter while one is already spinning to acquire the
                // lock

                bool waiterStartTimeWasRecorded = false;
                var state = new State(lockObj);
                while (true)
                {
                    Debug.Assert(state.HasAnyWaiters);
                    Debug.Assert(state.IsWaiterSignaledToWake);

                    if (state.IsLocked)
                    {
                        return false;
                    }

                    State newState = state;
                    newState.SetIsLocked();
                    newState.ClearIsWaiterSignaledToWake();
                    newState.DecrementWaiterCount();
                    if (newState.ShouldNotPreemptWaiters)
                    {
                        newState.ClearShouldNotPreemptWaiters();

                        if (newState.HasAnyWaiters && !waiterStartTimeWasRecorded)
                        {
                            // Update the waiter starvation start time. The time must be recorded before
                            // ShouldNotPreemptWaitersMask is cleared, as once that is cleared, another thread may check the
                            // waiter starvation start time and the previously recorded value may be stale, causing
                            // ShouldNotPreemptWaitersMask to be set again unnecessarily.
                            waiterStartTimeWasRecorded = true;
                            lockObj.RecordWaiterStartTime();
                        }
                    }

                    State stateBeforeUpdate = CompareExchange(lockObj, newState, state);
                    if (stateBeforeUpdate == state)
                    {
                        if (newState.HasAnyWaiters)
                        {
                            Debug.Assert(!state.ShouldNotPreemptWaiters || waiterStartTimeWasRecorded);
                            if (!waiterStartTimeWasRecorded)
                            {
                                // Since the lock was acquired successfully by a waiter, update the waiter starvation start time
                                lockObj.RecordWaiterStartTime();
                            }
                        }
                        return true;
                    }

                    state = stateBeforeUpdate;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryLockAfterWaiterSpinLoop(Lock lockObj)
            {
                // This method is called at the end of the waiter's spin loop. It must observe the wake signal always, and if
                // the lock is available, it must acquire the lock and unregister the waiter. If the lock is available, a waiter
                // must acquire the lock along with observing the wake signal, because a lock releaser does not wake a waiter
                // when a waiter was signaled but the wake signal has not been observed. If the lock is acquired, the waiter
                // starvation start time is also updated.

                var state = new State(Interlocked.Add(ref lockObj._state, Neg(IsWaiterSignaledToWakeMask)));
                Debug.Assert(new State(state._state + IsWaiterSignaledToWakeMask).IsWaiterSignaledToWake);

                bool waiterStartTimeWasRecorded = false;
                while (true)
                {
                    Debug.Assert(state.HasAnyWaiters);

                    if (state.IsLocked)
                    {
                        return false;
                    }

                    State newState = state;
                    newState.SetIsLocked();
                    newState.DecrementWaiterCount();
                    if (newState.ShouldNotPreemptWaiters)
                    {
                        newState.ClearShouldNotPreemptWaiters();

                        if (newState.HasAnyWaiters && !waiterStartTimeWasRecorded)
                        {
                            // Update the waiter starvation start time. The time must be recorded before
                            // ShouldNotPreemptWaitersMask is cleared, as once that is cleared, another thread may check the
                            // waiter starvation start time and the previously recorded value may be stale, causing
                            // ShouldNotPreemptWaitersMask to be set again unnecessarily.
                            waiterStartTimeWasRecorded = true;
                            lockObj.RecordWaiterStartTime();
                        }
                    }

                    State stateBeforeUpdate = CompareExchange(lockObj, newState, state);
                    if (stateBeforeUpdate == state)
                    {
                        if (newState.HasAnyWaiters)
                        {
                            Debug.Assert(!state.ShouldNotPreemptWaiters || waiterStartTimeWasRecorded);
                            if (!waiterStartTimeWasRecorded)
                            {
                                // Since the lock was acquired successfully by a waiter, update the waiter starvation start time
                                lockObj.RecordWaiterStartTime();
                            }
                        }
                        return true;
                    }

                    state = stateBeforeUpdate;
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void UnregisterWaiter(Lock lockObj)
            {
                // This method is called upon an exception while waiting, or when a wait has timed out. It must unregister the
                // waiter, and if it's the last waiter, clear ShouldNotPreemptWaitersMask to allow other threads to acquire the
                // lock.

                var state = new State(lockObj);
                while (true)
                {
                    Debug.Assert(state.HasAnyWaiters);

                    State newState = state;
                    newState.DecrementWaiterCount();
                    if (newState.ShouldNotPreemptWaiters && !newState.HasAnyWaiters)
                    {
                        newState.ClearShouldNotPreemptWaiters();
                    }

                    State stateBeforeUpdate = CompareExchange(lockObj, newState, state);
                    if (stateBeforeUpdate == state)
                    {
                        return;
                    }

                    state = stateBeforeUpdate;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TrySetIsWaiterSignaledToWake(Lock lockObj, State state)
            {
                // Determine whether we must signal a waiter to wake. Keep track of whether a thread has been signaled to wake
                // but has not yet woken from the wait. IsWaiterSignaledToWakeMask is cleared when a signaled thread wakes up by
                // observing a signal. Since threads can preempt waiting threads and acquire the lock (see TryLock()), it allows
                // for example, one thread to acquire and release the lock multiple times while there are multiple waiting
                // threads. In such a case, we don't want that thread to signal a waiter every time it releases the lock, as
                // that will cause unnecessary context switches with more and more signaled threads waking up, finding that the
                // lock is still locked, and going back into a wait state. So, signal only one waiting thread at a time.

                Debug.Assert(state.HasAnyWaiters);

                while (true)
                {
                    if (!state.NeedToSignalWaiter)
                    {
                        return false;
                    }

                    State newState = state;
                    newState.SetIsWaiterSignaledToWake();
                    if (!newState.ShouldNotPreemptWaiters && lockObj.ShouldStopPreemptingWaiters)
                    {
                        newState.SetShouldNotPreemptWaiters();
                    }

                    State stateBeforeUpdate = CompareExchange(lockObj, newState, state);
                    if (stateBeforeUpdate == state)
                    {
                        return true;
                    }
                    if (!stateBeforeUpdate.HasAnyWaiters)
                    {
                        return false;
                    }

                    state = stateBeforeUpdate;
                }
            }
        }

        private enum TryLockResult
        {
            Locked,
            Spin,
            Wait
        }
    }
}
