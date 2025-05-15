// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        private const int SID_MAX_SUB_AUTHORITIES = 15;
        internal const int SECURITY_MAX_SID_SIZE =
            12 /* sizeof(SID) */ - 4 /* sizeof(DWORD) */ + SID_MAX_SUB_AUTHORITIES * 4 /* sizeof(DWORD) */;

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        internal static partial int CreateWellKnownSid(
            int sidType,
            byte[]? domainSid,
            byte[] resultSid,
            ref uint resultSidLength);

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CreateWellKnownSid(
            int sidType,
            nint domainSid,
            nint resultSid,
            ref uint resultSidLength);

        internal enum WELL_KNOWN_SID_TYPE
        {
            WinBuiltinAdministratorsSid = 26,
            WinMediumLabelSid = 67
        }
    }
}
