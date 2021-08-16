// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class Thread
    {
        internal static void UninterruptibleSleep0() => Interop.Kernel32.Sleep(0);

        private static void SleepInternal(int millisecondsTimeout)
        {
            Debug.Assert(millisecondsTimeout >= -1);
            Interop.Kernel32.Sleep((uint)millisecondsTimeout);
        }
    }
}
