// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Dnsapi
    {
        [LibraryImport(Libraries.Dnsapi, EntryPoint = "DnsQuery_W", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DnsQuery(
            string recordName,
            short recordType,
            int options,
            IntPtr servers,
            out IntPtr dnsResultList,
            IntPtr reserved);
    }
}
