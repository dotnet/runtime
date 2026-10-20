// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed partial class Thread
    {
        internal static int GetOptimalMaxSpinWaitsPerSpinIteration() => 0;

        internal static bool CurrentThreadIsFinalizerThread() => false;

        // Matches the sole native thread ID reported by minipal.
        internal static ulong CurrentOSThreadId => 1;

        public static bool Yield() => false;

        public static void SpinWait(int iterations)
        {
        }

        internal static void SpinWaitInternal(int iterations) => SpinWait(iterations);
    }
}
