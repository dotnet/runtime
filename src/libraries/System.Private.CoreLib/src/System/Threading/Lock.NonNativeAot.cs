// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public sealed partial class Lock
    {
        private static readonly short s_maxSpinCount = DetermineMaxSpinCount();
        private static readonly short s_minSpinCountForAdaptiveSpin = DetermineMinSpinCountForAdaptiveSpin();

        /// <summary>
        /// Initializes a new instance of the <see cref="Lock"/> class.
        /// </summary>
        public Lock() => _spinCount = s_maxSpinCount;

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
        internal bool TryEnterSlow(int timeoutMs, int currentManagedThreadId) =>
            TryEnterSlow(timeoutMs, new ThreadId((uint)currentManagedThreadId)).IsInitialized;

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

        internal void InitializeToLockedWithNoWaiters(int currentManagedThreadId, uint recursionLevel)
        {
            Debug.Assert(currentManagedThreadId != 0);

            _owningThreadId = (uint)currentManagedThreadId;
            _recursionCount = recursionLevel;
            _state = State.LockedStateValue;
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

        private static TryLockResult LazyInitializeOrEnter() => TryLockResult.Spin;
        internal static bool IsSingleProcessor => Environment.IsSingleProcessor;

        internal partial struct ThreadId
        {
            [ThreadStatic]
            private static uint t_threadId;

            private uint _id;

            public ThreadId(uint id) => _id = id;
            public uint Id => _id;

            public bool IsInitialized => _id != 0;
            public static ThreadId Current_NoInitialize => new ThreadId(t_threadId);

            public void InitializeForCurrentThread()
            {
                Debug.Assert(!IsInitialized);
                Debug.Assert(t_threadId == 0);

                t_threadId = _id = (uint)Environment.CurrentManagedThreadId;
                Debug.Assert(IsInitialized);
            }
        }
    }
}
