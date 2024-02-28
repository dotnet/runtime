// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public sealed partial class Lock
    {
        private const short SpinCountNotInitialized = short.MinValue;

        // NOTE: Lock must not have a static (class) constructor, as Lock itself is used to synchronize
        // class construction.  If Lock has its own class constructor, this can lead to infinite recursion.
        // All static data in Lock must be lazy-initialized.
        private static int s_staticsInitializationStage;
        private static bool s_isSingleProcessor;
        private static short s_maxSpinCount;
        private static short s_minSpinCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lock"/> class.
        /// </summary>
        public Lock() => _spinCount = SpinCountNotInitialized;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryEnterOneShot(int currentManagedThreadId)
        {
            Debug.Assert(currentManagedThreadId != 0);

            if (State.TryLock(this))
            {
                Debug.Assert(_owningThreadId == 0);
                Debug.Assert(_recursionCount == 0);
                _owningThreadId = (uint)currentManagedThreadId;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Exit(int currentManagedThreadId)
        {
            Debug.Assert(currentManagedThreadId != 0);

            if (_owningThreadId != (uint)currentManagedThreadId)
            {
                ThrowHelper.ThrowSynchronizationLockException_LockExit();
            }

            ExitImpl();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryEnterSlow(int timeoutMs, int currentManagedThreadId) =>
            TryEnterSlow(timeoutMs, new ThreadId((uint)currentManagedThreadId)).IsInitialized;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool GetIsHeldByCurrentThread(int currentManagedThreadId)
        {
            Debug.Assert(currentManagedThreadId != 0);

            bool isHeld = _owningThreadId == (uint)currentManagedThreadId;
            Debug.Assert(!isHeld || new State(this).IsLocked);
            return isHeld;
        }

        internal uint ExitAll()
        {
            Debug.Assert(IsHeldByCurrentThread);

            uint recursionCount = _recursionCount;
            _owningThreadId = 0;
            _recursionCount = 0;

            State state = State.Unlock(this);
            if (state.HasAnyWaiters)
            {
                SignalWaiterIfNecessary(state);
            }

            return recursionCount;
        }

        internal void Reenter(uint previousRecursionCount)
        {
            Debug.Assert(!IsHeldByCurrentThread);

            Enter();
            _recursionCount = previousRecursionCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TryLockResult LazyInitializeOrEnter()
        {
            StaticsInitializationStage stage = (StaticsInitializationStage)Volatile.Read(ref s_staticsInitializationStage);
            switch (stage)
            {
                case StaticsInitializationStage.Complete:
                    if (_spinCount == SpinCountNotInitialized)
                    {
                        _spinCount = s_maxSpinCount;
                    }
                    return TryLockResult.Spin;

                case StaticsInitializationStage.Started:
                    // Spin-wait until initialization is complete or the lock is acquired to prevent class construction cycles
                    // later during a full wait
                    bool sleep = true;
                    while (true)
                    {
                        if (sleep)
                        {
                            Thread.UninterruptibleSleep0();
                        }
                        else
                        {
                            Thread.SpinWait(1);
                        }

                        stage = (StaticsInitializationStage)Volatile.Read(ref s_staticsInitializationStage);
                        if (stage == StaticsInitializationStage.Complete)
                        {
                            goto case StaticsInitializationStage.Complete;
                        }
                        else if (stage == StaticsInitializationStage.NotStarted)
                        {
                            goto default;
                        }

                        if (State.TryLock(this))
                        {
                            return TryLockResult.Locked;
                        }

                        sleep = !sleep;
                    }

                default:
                    Debug.Assert(stage == StaticsInitializationStage.NotStarted);
                    if (TryInitializeStatics())
                    {
                        goto case StaticsInitializationStage.Complete;
                    }
                    goto case StaticsInitializationStage.Started;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryInitializeStatics()
        {
            // Since Lock is used to synchronize class construction, and some of the statics initialization may involve class
            // construction, update the stage first to avoid infinite recursion
            switch (
                (StaticsInitializationStage)
                Interlocked.CompareExchange(
                    ref s_staticsInitializationStage,
                    (int)StaticsInitializationStage.Started,
                    (int)StaticsInitializationStage.NotStarted))
            {
                case StaticsInitializationStage.Started:
                    return false;
                case StaticsInitializationStage.Complete:
                    return true;
            }

            try
            {
                s_isSingleProcessor = Environment.IsSingleProcessor;
                s_maxSpinCount = DetermineMaxSpinCount();
                s_minSpinCount = DetermineMinSpinCount();

                // Also initialize some types that are used later to prevent potential class construction cycles
                _ = NativeRuntimeEventSource.Log;
            }
            catch
            {
                s_staticsInitializationStage = (int)StaticsInitializationStage.NotStarted;
                throw;
            }

            Volatile.Write(ref s_staticsInitializationStage, (int)StaticsInitializationStage.Complete);
            return true;
        }

        // Returns false until the static variable is lazy-initialized
        internal static bool IsSingleProcessor => s_isSingleProcessor;

        // Used to transfer the state when inflating thin locks
        internal void InitializeLocked(int managedThreadId, uint recursionCount)
        {
            Debug.Assert(recursionCount == 0 || managedThreadId != 0);

            _state = managedThreadId == 0 ? State.InitialStateValue : State.LockedStateValue;
            _owningThreadId = (uint)managedThreadId;
            _recursionCount = recursionCount;
        }

        internal struct ThreadId
        {
            private uint _id;

            public ThreadId(uint id) => _id = id;
            public uint Id => _id;
            public bool IsInitialized => _id != 0;
            public static ThreadId Current_NoInitialize => new ThreadId((uint)ManagedThreadId.CurrentManagedThreadIdUnchecked);

            public void InitializeForCurrentThread()
            {
                Debug.Assert(!IsInitialized);
                _id = (uint)ManagedThreadId.Current;
                Debug.Assert(IsInitialized);
            }
        }

        private enum StaticsInitializationStage
        {
            NotStarted,
            Started,
            Complete
        }
    }
}
