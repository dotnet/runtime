// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Netapi32
    {
        [LibraryImport(Libraries.Netapi32, EntryPoint = "DsGetDcOpenW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DsGetDcOpen(
            string? dnsName,
            int optionFlags,
            string? siteName,
            IntPtr domainGuid,
            string? dnsForestName,
            int dcFlags,
            out IntPtr retGetDcContext);
    }
}
