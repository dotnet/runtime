// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptGetProperty(SafeNCryptHandle hObject, string pszProperty, [Out] void* pbOutput, int cbOutput, out int pcbResult, CngPropertyOptions dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptSetProperty(SafeNCryptHandle hObject, string pszProperty, [In] void* pbInput, int cbInput, CngPropertyOptions dwFlags);

        internal static ErrorCode NCryptGetIntProperty(SafeNCryptHandle hObject, string pszProperty, ref int result)
        {
            int cbResult;
            ErrorCode errorCode;

            unsafe
            {
                fixed (int* pResult = &result)
                {
                    errorCode = Interop.NCrypt.NCryptGetProperty(
                        hObject,
                        pszProperty,
                        pResult,
                        sizeof(int),
                        out cbResult,
                        CngPropertyOptions.None);
                }
            }

            if (errorCode == ErrorCode.ERROR_SUCCESS)
            {
                Debug.Assert(cbResult == sizeof(int));
            }

            return errorCode;
        }
    }
}
