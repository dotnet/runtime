// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptGetProperty(SafeNCryptHandle hObject, string pszProperty, [Out] void* pbOutput, int cbOutput, out int pcbResult, CngPropertyOptions dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptSetProperty(SafeNCryptHandle hObject, string pszProperty, [In] void* pbInput, int cbInput, CngPropertyOptions dwFlags);
    }
}
