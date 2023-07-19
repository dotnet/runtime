// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "minipal_get_non_cryptographically_secure_random_bytes")]
        internal static unsafe partial void GetNonCryptographicallySecureRandomBytes(byte* buffer, int length);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "minipal_get_cryptographically_secure_random_bytes")]
        internal static unsafe partial int GetCryptographicallySecureRandomBytes(byte* buffer, int length);
    }

    internal static unsafe void GetRandomBytes(byte* buffer, int length)
    {
        Sys.GetNonCryptographicallySecureRandomBytes(buffer, length);
    }

    internal static unsafe void GetCryptographicallySecureRandomBytes(byte* buffer, int length)
    {
        if (Sys.GetCryptographicallySecureRandomBytes(buffer, length) != 0)
            throw new CryptographicException();
    }
}
