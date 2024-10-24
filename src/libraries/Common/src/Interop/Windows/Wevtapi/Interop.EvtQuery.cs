// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        [LibraryImport(Libraries.Wevtapi, SetLastError = true)]
        internal static partial EventLogHandle EvtQuery(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string path,
                            [MarshalAs(UnmanagedType.LPWStr)] string query,
                            int flags);
    }
}
