// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Threading
{
    public sealed partial class Thread
    {
        private void InitializeStartHelper(Delegate start, int maxStackSize)
        {
        }

        private void Start(object? parameter, bool captureContext) => throw new PlatformNotSupportedException();

        private void Start(bool captureContext) => throw new PlatformNotSupportedException();

        private void SetCultureOnUnstartedThread(CultureInfo value, bool uiCulture)
        {
            ArgumentNullException.ThrowIfNull(value);

            if ((ThreadState & ThreadState.Unstarted) == 0)
            {
                throw new InvalidOperationException(SR.Thread_Operation_RequiresCurrentThread);
            }
        }

        internal static void UninterruptibleSleep0() => Thread.Yield();

        private static void SleepInternal(int millisecondsTimeout) => WaitSubsystem.Sleep(millisecondsTimeout);

        private bool JoinInternal(int millisecondsTimeout)
        {
            // Only the current thread can have been started, so it is the only thread that can be joined.
            // A thread can't wait for itself to exit, so the join can only complete without blocking.
            Debug.Assert((ThreadState & ThreadState.Unstarted) == 0 || (millisecondsTimeout == 0));

            if (_isDead)
            {
                return true;
            }

            if (millisecondsTimeout == 0)
            {
                return false;
            }

            throw new PlatformNotSupportedException();
        }

        // Nothing can be waiting for the only thread to exit.
        private void SetJoinHandle()
        {
        }

        internal static int GetCurrentProcessorNumber() => -1;
    }
}
