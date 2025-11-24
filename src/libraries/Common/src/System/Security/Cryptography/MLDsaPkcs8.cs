// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static class MLDsaPkcs8
    {
        internal static bool TryExportPkcs8PrivateKey(
            MLDsa dsa,
            bool hasSeed,
            bool hasPrivateKey,
            Span<byte> destination,
            out int bytesWritten)
        {
            AlgorithmIdentifierAsn algorithmIdentifier = new()
            {
                Algorithm = dsa.Algorithm.Oid,
                Parameters = default(ReadOnlyMemory<byte>?),
            };

            MLDsaPrivateKeyAsn privateKeyAsn = default;
            byte[]? rented = null;
            int written = 0;

            try
            {
                if (hasSeed)
                {
                    int seedSize = dsa.Algorithm.PrivateSeedSizeInBytes;
                    rented = CryptoPool.Rent(seedSize);
                    Memory<byte> buffer = rented.AsMemory(0, seedSize);
                    dsa.ExportMLDsaPrivateSeed(buffer.Span);
                    written = buffer.Length;
                    privateKeyAsn.Seed = buffer;
                }
                else if (hasPrivateKey)
                {
                    int privateKeySize = dsa.Algorithm.PrivateKeySizeInBytes;
                    rented = CryptoPool.Rent(privateKeySize);
                    Memory<byte> buffer = rented.AsMemory(0, privateKeySize);
                    dsa.ExportMLDsaPrivateKey(buffer.Span);
                    written = buffer.Length;
                    privateKeyAsn.ExpandedKey = buffer;
                }
                else
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                }

                AsnWriter algorithmWriter = new(AsnEncodingRules.DER);
                algorithmIdentifier.Encode(algorithmWriter);
                AsnWriter privateKeyWriter = new(AsnEncodingRules.DER);
                privateKeyAsn.Encode(privateKeyWriter);
                AsnWriter pkcs8Writer = KeyFormatHelper.WritePkcs8(algorithmWriter, privateKeyWriter);

                bool result = pkcs8Writer.TryEncode(destination, out bytesWritten);
                privateKeyWriter.Reset();
                pkcs8Writer.Reset();
                return result;
            }
            finally
            {
                if (rented is not null)
                {
                    CryptoPool.Return(rented, written);
                }
            }
        }
    }
}
