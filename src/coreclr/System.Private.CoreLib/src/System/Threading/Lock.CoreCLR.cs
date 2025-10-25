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

        // NOTE: Like NativeAOT, we try to avoid a static constructor in Lock.
        // However, unlike NativeAOT, we don't do this because Lock is used to synchronize class construction.
        // Instead, we do this as our statics read from AppContext, and AppContext uses Monitor-based locks.
        // So, we can end up in the following situation:
        // - Thread 1 owns the thin-lock for the data store in AppContext
        // - Thread 2 tries to lock the thin-lock, forcing an upgrade to a sync-block
        // - Thread 2 tries to get the lock for the sync-block, triggering the Lock static constructor.
        // - The Lock static constructor tries to read from AppContext, blocking on the data store lock.
        // - Thread 1 tries to leave the data store lock, but it tries to do so by getting the Lock instance.
        // - Thread 1 tries to trigger the static constructor for Lock, but it is blocked as Thread 2
        //   is currently executing it.
        // - Deadlock!
        //
        // As a result, we'll do something similar to what NativeAOT does and delay statics initialization
        // until the first Lock entry.
        // This means that (in the example above), the Lock instance would be successfully constructed by Thread 2
        // and Thread 1 would be able to exit without calling into AppContext to initialize the statics.
        //
        // We'll match NativeAOT's behavior for the other statics cases to ensure that we don't get difficult-to-debug
        // issues if we ever port CoreCLR's class constructor running to match NativeAOT's.
        private static int s_staticsInitializationStage;
        private static bool s_isSingleProcessor;
        private static short s_maxSpinCount;
        private static short s_minSpinCountForAdaptiveSpin;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lock"/> class.
        /// </summary>
        public Lock() => _spinCount = SpinCountNotInitialized;

        internal void InitializeToLockedWithNoWaiters(uint owningManagedThreadId, uint recursionLevel)
        {
            Debug.Assert(owningManagedThreadId != 0);

            _owningThreadId = owningManagedThreadId;
            _recursionCount = recursionLevel;
            _state = State.LockedStateValue;
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
                    // Not using Environment.ProcessorCount here as it involves class construction.
                    // We don't do class-construction in managed today in CoreCLR,
                    // but if we ever port that logic from NativeAOT, this would be a nasty bug.
                    s_isSingleProcessor = Environment.GetProcessorCount() == 1;
                    s_maxSpinCount = DetermineMaxSpinCount();
                    s_minSpinCountForAdaptiveSpin = DetermineMinSpinCountForAdaptiveSpin();
                }

                // Also initialize some types that are used later to ensure we don't get bad bugs
                // if we port the NativeAOT logic for class construction.
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
        internal static bool IsSingleProcessor => s_isSingleProcessor;

        private enum StaticsInitializationStage
        {
            NotStarted,
            Started,
            PartiallyComplete,
            Complete
        }

        internal partial struct ThreadId(uint id)
        {
            // This thread-static is initialized by the runtime.
            [ThreadStatic]
            private static uint t_threadId;
            public uint Id => id;

            public bool IsInitialized => id != 0;
            public static ThreadId Current_NoInitialize => new ThreadId(t_threadId);

#pragma warning disable CA1822 // Mark members as static. This method is expected to exist for other runtimes.
            public void InitializeForCurrentThread()
#pragma warning restore CA1822 // Mark members as static
            {
                Debug.Fail("The managed thread ID for the current thread should always be initialized.");
            }
        }
    }
}
