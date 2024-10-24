// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        [LibraryImport(Libraries.Wevtapi, EntryPoint = "EvtCreateBookmark", SetLastError = true)]
        internal static partial EventLogHandle EvtCreateBookmark(
                            [MarshalAs(UnmanagedType.LPWStr)] string bookmarkXml);
    }
}
