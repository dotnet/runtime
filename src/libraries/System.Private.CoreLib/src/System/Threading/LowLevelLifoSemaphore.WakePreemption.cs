// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal sealed partial class LowLevelLifoSemaphore
    {
        private readonly struct WakePreemptionScope
        {
#if TARGET_LINUX
            internal readonly bool Suppressed;

            internal WakePreemptionScope(bool suppressed)
            {
                Suppressed = suppressed;
            }
#endif
        }

        private static WakePreemptionScope SuppressWakePreemption()
        {
#if TARGET_WINDOWS
            // GetCurrentThread() returns a pseudo-handle (-2) that is valid
            // only on the calling thread and does not need to be closed.
            Interop.Kernel32.SetThreadPriorityBoost(Interop.Kernel32.GetCurrentThread(), bDisablePriorityBoost: true);
            return default;
#elif TARGET_LINUX
            int result = Interop.Sys.SuppressWakePreemption();
            Debug.Assert(result == 0, $"SuppressWakePreemption failed with error {result}");
            return new WakePreemptionScope(suppressed: result == 0);
#else
            return default;
#endif
        }

        private static void RestoreWakePreemption(WakePreemptionScope scope)
        {
#if TARGET_WINDOWS
            Interop.Kernel32.SetThreadPriorityBoost(Interop.Kernel32.GetCurrentThread(), bDisablePriorityBoost: false);
#elif TARGET_LINUX
            if (scope.Suppressed)
            {
                int result = Interop.Sys.RestoreWakePreemption();
                Debug.Assert(result == 0, $"RestoreWakePreemption failed with error {result}");
            }
#else
            _ = scope;
#endif
        }
    }
}
