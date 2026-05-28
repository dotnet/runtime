// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptSecretAgreement(
            SafeBCryptKeyHandle hPrivKey,
            SafeBCryptKeyHandle hPubKey,
            out SafeBCryptSecretHandle phAgreedSecret,
            uint dwFlags);

        internal static SafeBCryptSecretHandle BCryptSecretAgreement(
            SafeBCryptKeyHandle hPrivKey,
            SafeBCryptKeyHandle hPubKey)
        {
            NTSTATUS status = BCryptSecretAgreement(
                hPrivKey,
                hPubKey,
                out SafeBCryptSecretHandle agreedSecret,
                0);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                agreedSecret.Dispose();
                throw CreateCryptographicException(status);
            }

            return agreedSecret;
        }
    }
}
