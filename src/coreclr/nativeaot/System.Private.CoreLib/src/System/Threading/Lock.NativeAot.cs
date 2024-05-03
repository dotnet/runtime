// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime;
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
        private static short s_minSpinCountForAdaptiveSpin;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lock"/> class.
        /// </summary>
        public Lock() => _spinCount = SpinCountNotInitialized;

#pragma warning disable CA1822 // can be marked as static - varies between runtimes
        internal ulong OwningOSThreadId => 0;
#pragma warning restore CA1822

        internal int OwningManagedThreadId => (int)_owningThreadId;

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
                    Debug.Assert(
                        stage == StaticsInitializationStage.NotStarted ||
                        stage == StaticsInitializationStage.PartiallyComplete);
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
            var oldStage = (StaticsInitializationStage)s_staticsInitializationStage;
            while (true)
            {
                if (oldStage == StaticsInitializationStage.Complete)
                {
                    return true;
                }

                var stageBeforeUpdate =
                    (StaticsInitializationStage)Interlocked.CompareExchange(
                        ref s_staticsInitializationStage,
                        (int)StaticsInitializationStage.Started,
                        (int)oldStage);
                if (stageBeforeUpdate == StaticsInitializationStage.Started)
                {
                    return false;
                }
                if (stageBeforeUpdate == oldStage)
                {
                    Debug.Assert(
                        oldStage == StaticsInitializationStage.NotStarted ||
                        oldStage == StaticsInitializationStage.PartiallyComplete);
                    break;
                }

                oldStage = stageBeforeUpdate;
            }

            bool isFullyInitialized;
            try
            {
                if (oldStage == StaticsInitializationStage.NotStarted)
                {
                    // If the stage is PartiallyComplete, these will have already been initialized.
                    //
                    // Not using Environment.ProcessorCount here as it involves class construction, and if that property is
                    // already being constructed earlier in the stack on the same thread, it would return the default value
                    // here. Initialize s_isSingleProcessor first, as it may be used by other initialization afterwards.
                    s_isSingleProcessor = RuntimeImports.RhGetProcessCpuCount() == 1;
                    s_maxSpinCount = DetermineMaxSpinCount();
                    s_minSpinCountForAdaptiveSpin = DetermineMinSpinCountForAdaptiveSpin();
                }

                // Also initialize some types that are used later to prevent potential class construction cycles. If
                // NativeRuntimeEventSource is already being class-constructed by this thread earlier in the stack, Log can be
                // null. Avoid going down the wait path in that case to avoid null checks in several other places. If not fully
                // initialized, the stage will also be set to PartiallyComplete to try again.
                isFullyInitialized = NativeRuntimeEventSource.Log != null;
            }
            catch
            {
                s_staticsInitializationStage = (int)StaticsInitializationStage.NotStarted;
                throw;
            }

            Volatile.Write(
                ref s_staticsInitializationStage,
                isFullyInitialized
                    ? (int)StaticsInitializationStage.Complete
                    : (int)StaticsInitializationStage.PartiallyComplete);
            return isFullyInitialized;
        }

        // Returns false until the static variable is lazy-initialized
        internal static bool IsSingleProcessor => s_isSingleProcessor;

        // Used to transfer the state when inflating thin locks. The lock is considered unlocked if managedThreadId is zero, and
        // locked otherwise.
        internal void ResetForMonitor(int managedThreadId, uint recursionCount)
        {
            Debug.Assert(recursionCount == 0 || managedThreadId != 0);
            Debug.Assert(!new State(this).UseTrivialWaits);

            _state = managedThreadId == 0 ? State.InitialStateValue : State.LockedStateValue;
            _owningThreadId = (uint)managedThreadId;
            _recursionCount = recursionCount;

            Debug.Assert(!new State(this).UseTrivialWaits);
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
            PartiallyComplete,
            Complete
        }
    }
}
