// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Threading
{
    internal partial class TimerQueue
    {
        private bool SetTimer(uint actualDuration) =>
            ThreadPool.UseWindowsThreadPool ?
            SetTimerWindowsThreadPool(actualDuration) :
            SetTimerPortable(actualDuration);
    }
}
