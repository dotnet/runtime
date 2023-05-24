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
#pragma warning disable IDE0060
        private TimerQueue(int id)
        {
        }
#pragma warning restore IDE0060

        private bool SetTimer(uint actualDuration) => SetTimerPortable(actualDuration);
    }
}
