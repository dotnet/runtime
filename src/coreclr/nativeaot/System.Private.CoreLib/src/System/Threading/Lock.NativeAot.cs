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
        // NOTE: Lock must not have a static (class) constructor, as Lock itself is used to synchronize
        // class construction.  If Lock has its own class constructor, this can lead to infinite recursion.
        // All static data in Lock must be lazy-initialized.
        private static int s_staticsInitializationStage;
        private static int s_processorCount;
        private static short s_maxSpinCount;
        private static short s_minSpinCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryEnterOneShot(int currentManagedThreadId)
        {
            Debug.Assert(currentManagedThreadId != 0);

            if (this.TryLock())
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
            TryEnterSlow(timeoutMs, new ThreadId((uint)currentManagedThreadId));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool GetIsHeldByCurrentThread(int currentManagedThreadId)
        {
            Debug.Assert(currentManagedThreadId != 0);

            bool isHeld = _owningThreadId == (uint)currentManagedThreadId;
            Debug.Assert(!isHeld || this.IsLocked);
            return isHeld;
        }

        internal uint ExitAll()
        {
            Debug.Assert(IsHeldByCurrentThread);

            uint recursionCount = _recursionCount;
            _recursionCount = 0;

            ReleaseCore();

            return recursionCount;
        }

        internal void Reenter(uint previousRecursionCount)
        {
            Debug.Assert(!IsHeldByCurrentThread);

            Enter();
            _recursionCount = previousRecursionCount;
        }

        // Returns false until the static variable is lazy-initialized
        internal static bool IsSingleProcessor => s_processorCount == 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LazyInit()
        {
            while (Volatile.Read(ref s_staticsInitializationStage) < (int)StaticsInitializationStage.Usable)
            {
                if (s_staticsInitializationStage == (int)StaticsInitializationStage.NotStarted &&
                    Interlocked.CompareExchange(
                        ref s_staticsInitializationStage,
                        (int)StaticsInitializationStage.Started,
                        (int)StaticsInitializationStage.NotStarted) == (int)StaticsInitializationStage.NotStarted)
                {
                    ScheduleStaticsInit();
                }
            }
        }

        internal static void ScheduleStaticsInit()
        {
            // initialize essentials
            // this is safe to do as these do not need to take locks
            s_maxSpinCount = DefaultMaxSpinCount << SpinCountScaleShift;
            s_minSpinCount = DefaultMinSpinCount << SpinCountScaleShift;

            // we can now use the slow path of the lock.
            Volatile.Write(ref s_staticsInitializationStage, (int)StaticsInitializationStage.Usable);

            // other static initialization is optional (but may take locks)
            // schedule initialization on finalizer thread to avoid reentrancies.
            StaticsInitializer.Schedule();

            // trigger an ephemeral GC, in case the app is not allocating anything.
            // this will be once per lifetime of the runtime, so it is ok.
            GC.Collect(0);
        }

        private class StaticsInitializer
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Schedule()
            {
                new StaticsInitializer();
            }

            ~StaticsInitializer()
            {
                s_processorCount = RuntimeImports.RhGetProcessCpuCount();
                if (s_processorCount > 1)
                {
                    s_minSpinCount = (short)(DetermineMinSpinCount() << SpinCountScaleShift);
                    s_maxSpinCount = (short)(DetermineMaxSpinCount() << SpinCountScaleShift);
                }
                else
                {
                    s_minSpinCount = 0;
                    s_maxSpinCount = 0;
                }

                NativeRuntimeEventSource.Log.IsEnabled();
                Stopwatch.GetTimestamp();
                Volatile.Write(ref s_staticsInitializationStage, (int)StaticsInitializationStage.Complete);
            }
        }

        internal static bool StaticsInitComplete()
        {
            return Volatile.Read(ref s_staticsInitializationStage) == (int)StaticsInitializationStage.Complete;
        }

        // Used to transfer the state when inflating thin locks
        internal void InitializeLocked(int managedThreadId, uint recursionCount)
        {
            Debug.Assert(recursionCount == 0 || managedThreadId != 0);

            _state = managedThreadId == 0 ? Unlocked : Locked;
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
            Usable,
            Complete
        }
    }
}
