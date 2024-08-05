// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Threading
{
    //
    // Wasi-specific implementation of Timer
    // Based on TimerQueue.Portable.cs
    // Not thread safe
    //
    internal partial class TimerQueue
    {
        private static long TickCount64 => Environment.TickCount64;

        private TimerQueue(int _)
        {
            throw new PlatformNotSupportedException();
        }
#pragma warning disable CA1822 // Mark members as static
        private bool SetTimer(uint actualDuration)
        {
            throw new PlatformNotSupportedException();
        }
#pragma warning restore CA1822
    }
}
