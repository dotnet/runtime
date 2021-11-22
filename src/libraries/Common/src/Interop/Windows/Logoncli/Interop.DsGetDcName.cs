// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Logoncli
    {
        [GeneratedDllImport(Libraries.Logoncli, EntryPoint = "DsGetDcNameW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int DsGetDcName(
            string computerName,
            string domainName,
            IntPtr domainGuid,
            string siteName,
            int flags,
            out IntPtr domainControllerInfo);
    }
}
