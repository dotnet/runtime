// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Thread
    {
        internal static void UninterruptibleSleep0() => Interop.Kernel32.Sleep(0);

#if !CORECLR
        private static void SleepInternal(int millisecondsTimeout)
        {
            Debug.Assert(millisecondsTimeout >= -1);
            Interop.Kernel32.Sleep((uint)millisecondsTimeout);
        }
#endif

        internal static int GetCurrentProcessorNumber()
        {
            Interop.Kernel32.PROCESSOR_NUMBER procNumber;
            Interop.Kernel32.GetCurrentProcessorNumberEx(out procNumber);
            return (procNumber.Group << 6) | procNumber.Number;
        }
    }
}
