// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public sealed partial class Lock
    {
        private static readonly short s_maxSpinCount = DetermineMaxSpinCount();
        private static readonly short s_minSpinCount = DetermineMinSpinCount();

        /// <summary>
        /// Initializes a new instance of the <see cref="Lock"/> class.
        /// </summary>
        public Lock() => _spinCount = s_maxSpinCount;

        private static TryLockResult LazyInitializeOrEnter() => TryLockResult.Spin;
        private static bool IsSingleProcessor => Environment.IsSingleProcessor;

        internal partial struct ThreadId
        {
#if TARGET_OSX
            [ThreadStatic]
            private static ulong t_threadId;

            private ulong _id;

            public ThreadId(ulong id) => _id = id;
            public ulong Id => _id;
#else
            [ThreadStatic]
            private static uint t_threadId;

            private uint _id;

            public ThreadId(uint id) => _id = id;
            public uint Id => _id;
#endif

            public bool IsInitialized => _id != 0;
            public static ThreadId Current_NoInitialize => new ThreadId(t_threadId);

            public void InitializeForCurrentThread()
            {
                Debug.Assert(!IsInitialized);
                Debug.Assert(t_threadId == 0);

#if TARGET_WINDOWS
                uint id = (uint)Interop.Kernel32.GetCurrentThreadId();
#elif TARGET_OSX
                ulong id = Interop.Sys.GetUInt64OSThreadId();
#else
                uint id = Interop.Sys.TryGetUInt32OSThreadId();
                if (id == unchecked((uint)-1))
                {
                    id = (uint)Environment.CurrentManagedThreadId;
                    Debug.Assert(id != 0);
                }
                else
#endif

                if (id == 0)
                {
                    id--;
                }

                t_threadId = _id = id;
                Debug.Assert(IsInitialized);
            }
        }
    }
}
