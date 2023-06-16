// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public sealed partial class Lock
    {
        private const int SpinCountNotInitialized = int.MinValue;

        // NOTE: Lock must not have a static (class) constructor, as Lock itself is used to synchronize
        // class construction.  If Lock has its own class constructor, this can lead to infinite recursion.
        // All static data in Lock must be lazy-initialized.
        private static int s_staticsInitializationStage;
        private static bool s_isSingleProcessor;
        private static int s_maxSpinCount;
        private static int s_minSpinCount;

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
            int stage = Volatile.Read(ref s_staticsInitializationStage);
            switch (stage)
            {
                case 2:
                    if (_spinCount == SpinCountNotInitialized)
                    {
                        _spinCount = s_maxSpinCount;
                    }
                    return TryLockResult.Spin;

                case 1:
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

                        if (Volatile.Read(ref s_staticsInitializationStage) == 2)
                        {
                            goto case 2;
                        }

                        if (State.TryLock(this))
                        {
                            return TryLockResult.Locked;
                        }

                        sleep = !sleep;
                    }

                default:
                    Debug.Assert(stage == 0);
                    if (TryInitializeStatics())
                    {
                        goto case 2;
                    }
                    goto case 1;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryInitializeStatics()
        {
            // Since Lock is used to synchronize class construction, and some of the statics initialization may involve class
            // construction, update the stage first to avoid infinite recursion
            switch (Interlocked.CompareExchange(ref s_staticsInitializationStage, 1, 0))
            {
                case 1:
                    return false;
                case 2:
                    return true;
            }

            s_isSingleProcessor = Environment.IsSingleProcessor;
            s_maxSpinCount = DetermineMaxSpinCount();
            s_minSpinCount = DetermineMinSpinCount();

            Volatile.Write(ref s_staticsInitializationStage, 2);
            return true;
        }

        // Returns false until the static variable is lazy-initialized
        internal static bool IsSingleProcessor => s_isSingleProcessor;

        // Used to transfer the state when inflating thin locks
        internal void InitializeLocked(int managedThreadId, int recursionCount)
        {
            Debug.Assert(recursionCount == 0 || managedThreadId != 0);

            _state = managedThreadId == 0 ? State.InitialStateValue : State.LockedStateValue;
            _owningThreadId = (uint)managedThreadId;
            _recursionCount = (uint)recursionCount;
        }

        private struct ThreadId
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
    }
}
