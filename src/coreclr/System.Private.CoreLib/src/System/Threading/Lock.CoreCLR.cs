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

        internal void InitializeToLockedWithNoWaiters(uint owningManagedThreadId, uint recursionLevel)
        {
            Debug.Assert(owningManagedThreadId != 0);

            _owningThreadId = owningManagedThreadId;
            _recursionCount = recursionLevel;
            _state = State.LockedStateValue;
        }

        private static TryLockResult LazyInitializeOrEnter() => TryLockResult.Spin;
        internal static bool IsSingleProcessor => Environment.IsSingleProcessor;

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
