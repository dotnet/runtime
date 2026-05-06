// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal sealed partial class TimerQueue
    {
        private TimerQueue(int id)
        {
            _id = id;
        }

        private bool SetTimer(uint actualDuration) =>
            ThreadPool.UseWindowsThreadPool ?
            SetTimerWindowsThreadPool(actualDuration) :
            SetTimerPortable(actualDuration);
    }
}
