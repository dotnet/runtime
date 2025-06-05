// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptGenerateKeyPair(
            SafeBCryptAlgorithmHandle hAlgorithm,
            out SafeBCryptKeyHandle phKey,
            int dwLength,
            uint dwFlags);

        internal static SafeBCryptKeyHandle BCryptGenerateKeyPair(
            SafeBCryptAlgorithmHandle hAlgorithm,
            int keyLength)
        {
            NTSTATUS status = BCryptGenerateKeyPair(
                hAlgorithm,
                out SafeBCryptKeyHandle hKey,
                keyLength,
                0);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                hKey.Dispose();
                throw CreateCryptographicException(status);
            }

            return hKey;
        }
    }
}
