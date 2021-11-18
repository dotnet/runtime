// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Logoncli
    {
        [DllImport(Libraries.Logoncli, EntryPoint = "DsGetDcNameW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int DsGetDcName(
            [In] string computerName,
            [In] string domainName,
            [In] IntPtr domainGuid,
            [In] string siteName,
            [In] int flags,
            [Out] out IntPtr domainControllerInfo);
    }
}
