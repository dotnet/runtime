// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class DSAKeyFormatHelper
    {
        internal static void ReadEncryptedPkcs8(
            ReadOnlySpan<byte> source,
            ReadOnlySpan<char> password,
            out int bytesRead,
            out DSAParameters key)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<DSAParameters>(
                s_validOids,
                source,
                password,
                ReadDsaPrivateKey,
                out bytesRead,
                out key);
        }

        internal static void ReadEncryptedPkcs8(
            ReadOnlySpan<byte> source,
            ReadOnlySpan<byte> passwordBytes,
            out int bytesRead,
            out DSAParameters key)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<DSAParameters>(
                s_validOids,
                source,
                passwordBytes,
                ReadDsaPrivateKey,
                out bytesRead,
                out key);
        }
    }
}
