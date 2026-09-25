// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class EccAppleCrypto
    {
#pragma warning disable IDE0060
        private static ECParameters ExportParametersFromLegacyKey(SecKeyPair keys, bool includePrivateParameters)
            => throw new CryptographicException();

        private static void ExtractPublicKeyFromPrivateKey(ref ECParameters ecParameters)
        {
            int keySizeInBits = ecParameters.Curve.Oid.Value switch
            {
                Oids.secp256r1 => 256,
                Oids.secp384r1 => 384,
                Oids.secp521r1 => 521,
                _ => throw new UnreachableException(),
            };

            byte[] privateKey = ecParameters.D!;
            int fieldSize = (keySizeInBits + 7) / 8;
            Debug.Assert(privateKey.Length == fieldSize);

            Span<byte> publicKey = stackalloc byte[1 + 2 * fieldSize];
            Interop.AppleCrypto.EccExportPublicKeyFromPrivateKey(keySizeInBits, privateKey, publicKey);
            AsymmetricAlgorithmHelpers.DecodeFromUncompressedAnsiX963Key(
                publicKey,
                hasPrivateKey: false,
                out ECParameters publicParameters);
            ecParameters.Q = publicParameters.Q;
        }
#pragma warning restore IDE0060
    }
}
