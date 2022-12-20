// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial NTSTATUS BCryptGetProperty(
            SafeBCryptHandle hObject,
            string pszProperty,
            void* pbOutput,
            int cbOutput,
            out int pcbResult,
            int dwFlags);

        internal static unsafe int BCryptGetDWordProperty(SafeBCryptHandle hObject, string pszProperty)
        {
            int ret = 0;

            NTSTATUS status = BCryptGetProperty(
                hObject,
                pszProperty,
                &ret,
                sizeof(int),
                out int written,
                0);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(status);
            }

            if (written != sizeof(int))
            {
                throw new CryptographicException();
            }

            return ret;
        }
    }
}
