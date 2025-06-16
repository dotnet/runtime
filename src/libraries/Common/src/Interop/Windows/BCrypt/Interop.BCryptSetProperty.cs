// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial NTSTATUS BCryptSetProperty(
            SafeBCryptHandle hObject,
            string pszProperty,
            void* pbInput,
            uint cbInput,
            uint dwFlags);

        internal static unsafe void BCryptSetSZProperty(SafeBCryptHandle hObject, string pszProperty, string pszValue)
        {
            Debug.Assert(pszValue is not null);

            fixed (void* pbInput = pszValue)
            {
                NTSTATUS status = BCryptSetProperty(
                    hObject,
                    pszProperty,
                    pbInput,
                    checked(((uint)pszValue.Length + 1) * 2),
                    0);

                if (status != NTSTATUS.STATUS_SUCCESS)
                {
                    throw CreateCryptographicException(status);
                }
            }
        }
    }
}
