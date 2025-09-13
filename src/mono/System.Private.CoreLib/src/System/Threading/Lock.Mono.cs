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

        private static TryLockResult LazyInitializeOrEnter() => TryLockResult.Spin;
        internal static bool IsSingleProcessor => Environment.IsSingleProcessor;

        internal partial struct ThreadId(uint id)
        {
            [ThreadStatic]
            private static uint t_threadId;

            public uint Id => id;

            public bool IsInitialized => id != 0;
            public static ThreadId Current_NoInitialize => new ThreadId(t_threadId);

            public void InitializeForCurrentThread()
            {
                Debug.Assert(!IsInitialized);
                Debug.Assert(t_threadId == 0);

                t_threadId = id = (uint)Environment.CurrentManagedThreadId;
                Debug.Assert(IsInitialized);
            }
        }
    }
}
