// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        [LibraryImport(Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtSubscribe(
                            EventLogHandle session,
                            SafeWaitHandle signalEvent,
                            [MarshalAs(UnmanagedType.LPWStr)] string path,
                            [MarshalAs(UnmanagedType.LPWStr)] string query,
                            EventLogHandle bookmark,
                            IntPtr context,
                            IntPtr callback,
                            int flags);
    }
}
