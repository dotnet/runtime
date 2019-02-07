// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SECURITY_LOGON_SESSION_DATA
    {
        internal uint Size;
        internal LUID LogonId;
        internal UNICODE_INTPTR_STRING UserName;
        internal UNICODE_INTPTR_STRING LogonDomain;
        internal UNICODE_INTPTR_STRING AuthenticationPackage;
        internal uint LogonType;
        internal uint Session;
        internal IntPtr Sid;
        internal long LogonTime;
    }
}
