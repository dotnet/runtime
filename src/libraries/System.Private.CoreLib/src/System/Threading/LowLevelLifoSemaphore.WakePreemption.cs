// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal sealed partial class LowLevelLifoSemaphore
    {
        private readonly struct WakePreemptionScope
        {
#if TARGET_LINUX
            internal readonly int PreviousPolicy;
            internal readonly int PreviousPriority;

            internal WakePreemptionScope(int previousPolicy, int previousPriority)
            {
                PreviousPolicy = previousPolicy;
                PreviousPriority = previousPriority;
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
            int previousPolicy = -1;
            int previousPriority = 0;
            unsafe
            {
                Interop.Sys.SuppressWakePreemption(&previousPolicy, &previousPriority);
            }

            return new WakePreemptionScope(previousPolicy, previousPriority);
#else
            return default;
#endif
        }

        private static void RestoreWakePreemption(WakePreemptionScope scope)
        {
#if TARGET_WINDOWS
            Interop.Kernel32.SetThreadPriorityBoost(Interop.Kernel32.GetCurrentThread(), bDisablePriorityBoost: false);
#elif TARGET_LINUX
            if (scope.PreviousPolicy != -1)
            {
                Interop.Sys.RestoreWakePreemption(scope.PreviousPolicy, scope.PreviousPriority);
            }
#else
            _ = scope;
#endif
        }
    }
}
