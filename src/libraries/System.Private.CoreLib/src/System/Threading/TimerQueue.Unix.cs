// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal sealed partial class TimerQueue
    {
        public static long TickCount64 => Environment.TickCount64;

#pragma warning disable IDE0060
        private TimerQueue(int id)
        {
        }
#pragma warning restore IDE0060

        private bool SetTimer(uint actualDuration) => SetTimerPortable(actualDuration);
    }
}
