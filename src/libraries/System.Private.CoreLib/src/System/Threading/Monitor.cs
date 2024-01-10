// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class Monitor
    {
        public static bool TryEnter(object obj, TimeSpan timeout)
            => TryEnter(obj, WaitHandle.ToTimeoutMilliseconds(timeout));

        public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
            => TryEnter(obj, WaitHandle.ToTimeoutMilliseconds(timeout), ref lockTaken);

#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public static bool Wait(object obj, TimeSpan timeout) => Wait(obj, WaitHandle.ToTimeoutMilliseconds(timeout));

#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public static bool Wait(object obj) => Wait(obj, Timeout.Infinite);

        // Remoting is not supported, exitContext argument is unused
#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public static bool Wait(object obj, int millisecondsTimeout, bool exitContext)
            => Wait(obj, millisecondsTimeout);

        // Remoting is not supported, exitContext argument is unused
#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public static bool Wait(object obj, TimeSpan timeout, bool exitContext)
            => Wait(obj, WaitHandle.ToTimeoutMilliseconds(timeout));
    }
}
