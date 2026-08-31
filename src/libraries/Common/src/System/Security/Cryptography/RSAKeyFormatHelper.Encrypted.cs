// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static partial class RSAKeyFormatHelper
    {
        internal static void ReadEncryptedPkcs8(
            ReadOnlySpan<byte> source,
            ReadOnlySpan<char> password,
            out int bytesRead,
            out RSAParameters key)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<RSAParameters>(
                s_validOids,
                source,
                password,
                FromPkcs1PrivateKey,
                out bytesRead,
                out key);
        }

        internal static void ReadEncryptedPkcs8(
            ReadOnlySpan<byte> source,
            ReadOnlySpan<byte> passwordBytes,
            out int bytesRead,
            out RSAParameters key)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<RSAParameters>(
                s_validOids,
                source,
                passwordBytes,
                FromPkcs1PrivateKey,
                out bytesRead,
                out key);
        }
    }
}
