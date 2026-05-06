// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial NTSTATUS BCryptDeriveKey(
            SafeBCryptSecretHandle hSharedSecret,
            string pwszKDF,
            ref readonly BCryptBufferDesc pParameterList,
            Span<byte> pbDerivedKey,
            uint cbDerivedKey,
            out uint pcbResult,
            uint dwFlags);

        internal static unsafe void BCryptDeriveKey(
            SafeBCryptSecretHandle hSharedSecret,
            string pwszKDF,
            ref readonly BCryptBufferDesc parameterList,
            Span<byte> destination,
            out int written)
        {
            NTSTATUS status = BCryptDeriveKey(
                hSharedSecret,
                pwszKDF,
                in parameterList,
                destination,
                (uint)destination.Length,
                out uint pcbResult,
                0);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(status);
            }

            written = checked((int)pcbResult);
        }
    }
}
