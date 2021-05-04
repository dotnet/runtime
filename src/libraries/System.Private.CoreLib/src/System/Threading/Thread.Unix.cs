// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class Thread
    {
#if !CORECLR
        // [TODO] Remove when https://github.com/dotnet/runtime/issues/51991 is fixed.
        internal static unsafe IntPtr CreateAutoreleasePool(out IntPtr drainFunc)
            => throw new PlatformNotSupportedException();

        internal static void UninterruptibleSleep0() => WaitSubsystem.UninterruptibleSleep0();

        private static void SleepInternal(int millisecondsTimeout) => WaitSubsystem.Sleep(millisecondsTimeout);
#endif
    }
}
