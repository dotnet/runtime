// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial ErrorCode NCryptGetProperty(
            SafeNCryptHandle hObject,
            string pszProperty,
            void* pbOutput,
            int cbOutput,
            out int pcbResult,
            CngPropertyOptions dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial ErrorCode NCryptSetProperty(
            SafeNCryptHandle hObject,
            string pszProperty,
            void* pbInput,
            int cbInput,
            CngPropertyOptions dwFlags);

        [SupportedOSPlatform("windows")]
        internal static unsafe ErrorCode NCryptGetByteProperty(SafeNCryptHandle hObject, string pszProperty, ref byte result, CngPropertyOptions options = CngPropertyOptions.None)
        {
            fixed (byte* pResult = &result)
            {
                ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(
                    hObject,
                    pszProperty,
                    pResult,
                    sizeof(byte),
                    out int cbResult,
                    options);

                if (errorCode == ErrorCode.ERROR_SUCCESS)
                {
                    Debug.Assert(cbResult == sizeof(byte));
                }

                return errorCode;
            }
        }

        internal static unsafe ErrorCode NCryptGetIntProperty(SafeNCryptHandle hObject, string pszProperty, ref int result)
        {
            fixed (int* pResult = &result)
            {
#if NETSTANDARD || NET
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
#endif

                ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(
                    hObject,
                    pszProperty,
                    pResult,
                    sizeof(int),
                    out int cbResult,
                    CngPropertyOptions.None);

                if (errorCode == ErrorCode.ERROR_SUCCESS)
                {
                    Debug.Assert(cbResult == sizeof(int));
                }

                return errorCode;
            }
        }
    }
}
