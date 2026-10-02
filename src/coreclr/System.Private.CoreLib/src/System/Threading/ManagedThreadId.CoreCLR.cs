// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal static class ManagedThreadId
    {
#if FEATURE_MULTITHREADING
        // This will be initialized by the runtime.
        [ThreadStatic]
        private static int t_currentManagedThreadId;

        internal static int CurrentManagedThreadIdUnchecked => t_currentManagedThreadId;

        public static int Current
        {
            get
            {
                Debug.Assert(t_currentManagedThreadId != 0, "The runtime should have initialized the thread id by now.");
                return t_currentManagedThreadId;
            }
        }
#else
        // The runtime always assigns ID 1 to the only thread.
        internal static int CurrentManagedThreadIdUnchecked => 1;

        public static int Current => 1;
#endif // FEATURE_MULTITHREADING
    }
}
