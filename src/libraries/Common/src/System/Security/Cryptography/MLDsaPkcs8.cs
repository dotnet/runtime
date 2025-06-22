// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static class MLDsaPkcs8
    {
        internal static bool TryExportPkcs8PrivateKey(
            MLDsa dsa,
            bool hasSeed,
            bool hasSecretKey,
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
                else if (hasSecretKey)
                {
                    int secretKeySize = dsa.Algorithm.SecretKeySizeInBytes;
                    rented = CryptoPool.Rent(secretKeySize);
                    Memory<byte> buffer = rented.AsMemory(0, secretKeySize);
                    dsa.ExportMLDsaSecretKey(buffer.Span);
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

        // TODO: Remove this once Windows moves to the new format.
        internal static unsafe byte[] ConvertToOldChoicelessFormat(ReadOnlySpan<byte> pkcs8WithChoice)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(pkcs8WithChoice))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, pkcs8WithChoice.Length))
                {
                    PrivateKeyInfoAsn privateKeyInfo = PrivateKeyInfoAsn.Decode(manager.Memory, AsnEncodingRules.BER);
                    AlgorithmIdentifierAsn privateAlgorithm = privateKeyInfo.PrivateKeyAlgorithm;

                    if (privateAlgorithm.Algorithm is not (Oids.MLDsa44 or Oids.MLDsa65 or Oids.MLDsa87))
                    {
                        Debug.Fail("Unexpected algorithm");
                        throw new CryptographicException();
                    }

                    MLDsaPrivateKeyAsn mldsaPrivateKeyAsn = MLDsaPrivateKeyAsn.Decode(privateKeyInfo.PrivateKey, AsnEncodingRules.BER);
                    privateKeyInfo.PrivateKey = mldsaPrivateKeyAsn.Seed
                        ?? mldsaPrivateKeyAsn.ExpandedKey.GetValueOrDefault(); // Old format does not support having both

                    AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                    privateKeyInfo.Encode(writer);
                    return writer.Encode();
                }
            }
        }

        // TODO: Remove this once Windows moves to the new format.
        internal static unsafe byte[] ConvertFromOldChoicelessFormat(ReadOnlySpan<byte> pkcs8WithoutChoice)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(pkcs8WithoutChoice))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, pkcs8WithoutChoice.Length))
                {
                    PrivateKeyInfoAsn privateKeyInfo = PrivateKeyInfoAsn.Decode(manager.Memory, AsnEncodingRules.BER);
                    AlgorithmIdentifierAsn privateAlgorithm = privateKeyInfo.PrivateKeyAlgorithm;

                    int seedSize = privateAlgorithm.Algorithm switch
                    {
                        Oids.MLDsa44 => MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes,
                        Oids.MLDsa65 => MLDsaAlgorithm.MLDsa65.PrivateSeedSizeInBytes,
                        Oids.MLDsa87 => MLDsaAlgorithm.MLDsa87.PrivateSeedSizeInBytes,
                        _ => throw new CryptographicException(),
                    };

                    ReadOnlyMemory<byte> key = privateKeyInfo.PrivateKey;

                    MLDsaPrivateKeyAsn mldsaPrivateKeyAsn = default;

                    if (key.Length == seedSize)
                    {
                        mldsaPrivateKeyAsn.Seed = key;
                    }
                    else
                    {
                        mldsaPrivateKeyAsn.ExpandedKey = key;
                    }

                    AsnWriter writer = new(AsnEncodingRules.DER);
                    mldsaPrivateKeyAsn.Encode(writer);
                    privateKeyInfo.PrivateKey = writer.Encode();

                    writer = new AsnWriter(AsnEncodingRules.DER);
                    privateKeyInfo.Encode(writer);
                    return writer.Encode();
                }
            }
        }
    }
}
