// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32, EntryPoint = "RegisterEventSourceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr RegisterEventSource(string lpUNCServerName, string lpSourceName);
    }
}
