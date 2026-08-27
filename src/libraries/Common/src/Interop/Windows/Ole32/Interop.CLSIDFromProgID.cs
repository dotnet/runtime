// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal static partial class Ole32
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Ole32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);
    }
}
