// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        [LibraryImport(Libraries.Wevtapi, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvtRender(
                            EventLogHandle context,
                            EventLogHandle eventHandle,
                            EVT_RENDER_FLAGS flags,
                            int buffSize,
                            [Out] char[]? buffer,
                            out int buffUsed,
                            out int propCount);

        [LibraryImport(Libraries.Wevtapi, EntryPoint = "EvtRender", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvtRender(
                            EventLogHandle context,
                            EventLogHandle eventHandle,
                            EVT_RENDER_FLAGS flags,
                            int buffSize,
                            IntPtr buffer,
                            out int buffUsed,
                            out int propCount);
    }
}
