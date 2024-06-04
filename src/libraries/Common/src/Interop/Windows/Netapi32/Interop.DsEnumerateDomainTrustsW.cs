// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Netapi32
    {
        [LibraryImport(Libraries.Netapi32, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DsEnumerateDomainTrustsW(string serverName, DS_DOMAINTRUST_FLAG flags, out IntPtr domains, out int count);
    }
}
