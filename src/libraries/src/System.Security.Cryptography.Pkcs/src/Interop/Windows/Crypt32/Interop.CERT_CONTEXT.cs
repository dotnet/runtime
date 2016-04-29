// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CERT_CONTEXT
        {
            internal MsgEncodingType dwCertEncodingType;
            internal byte* pbCertEncoded;
            internal int cbCertEncoded;
            internal CERT_INFO* pCertInfo;
            internal IntPtr hCertStore;
        }
    }
}

