// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Logoncli
    {
        [LibraryImport(Libraries.Logoncli, EntryPoint = "DsGetDcNameW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DsGetDcName(
            string computerName,
            string domainName,
            IntPtr domainGuid,
            string siteName,
            int flags,
            out IntPtr domainControllerInfo);
    }
}
