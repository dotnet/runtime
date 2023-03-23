// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public sealed partial class Thread
    {

        private Thread()
        {
            _managedThreadId = System.Threading.ManagedThreadId.GetCurrentThreadId();

            PlatformSpecificInitialize();
            RegisterThreadExitCallback();
        }

        internal void SetWaitSleepJoinState() => SetWaitSleepJoinStateCore();

        internal void ClearWaitSleepJoinState() => ClearWaitSleepJoinStateCrore();

        public bool Join(int millisecondsTimeout) => JoinCore(millisecondsTimeout);

        internal static void SpinWaitInternal(int iterations) => SpinWaitInternalCore(iterations);

        public static void SpinWait(int iterations) => SpinWaitCore();

        [MethodImpl(MethodImplOptions.NoInlining)] // Slow path method. Make sure that the caller frame does not pay for PInvoke overhead.
        public static bool Yield() => YieldCore();

        internal static void IncrementRunningForeground() => IncrementRunningForegroundCore();

        internal static void DecrementRunningForeground() => DecrementRunningForegroundCore();

        internal static void WaitForForegroundThreads() => WaitForForegroundThreadsCore();
    }
}
