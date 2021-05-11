// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class RSAImplementation
    {
        public sealed partial class RSASecurityTransforms : RSA
        {
            private static RSAParameters ExportParametersFromLegacyKey(SecKeyPair keys, bool includePrivateParameters)
            {
                // Apple requires all private keys to be exported encrypted, but since we're trying to export
                // as parsed structures we will need to decrypt it for the user.
                const string ExportPassword = "DotnetExportPassphrase";

                byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(
                    includePrivateParameters ? keys.PrivateKey : keys.PublicKey,
                    exportPrivate: includePrivateParameters,
                    password: ExportPassword);

                try
                {
                    if (!includePrivateParameters)
                    {
                        // When exporting a key handle opened from a certificate, it seems to
                        // export as a PKCS#1 blob instead of an X509 SubjectPublicKeyInfo blob.
                        // So, check for that.
                        // NOTE: It doesn't affect macOS Mojave when SecCertificateCopyKey API
                        // is used.
                        RSAParameters key;

                        AsnReader reader = new AsnReader(keyBlob, AsnEncodingRules.BER);
                        AsnReader sequenceReader = reader.ReadSequence();

                        if (sequenceReader.PeekTag().Equals(Asn1Tag.Integer))
                        {
                            AlgorithmIdentifierAsn ignored = default;
                            RSAKeyFormatHelper.ReadRsaPublicKey(keyBlob, ignored, out key);
                        }
                        else
                        {
                            RSAKeyFormatHelper.ReadSubjectPublicKeyInfo(
                                keyBlob,
                                out int localRead,
                                out key);
                            Debug.Assert(localRead == keyBlob.Length);
                        }
                        return key;
                    }
                    else
                    {
                        RSAKeyFormatHelper.ReadEncryptedPkcs8(
                            keyBlob,
                            ExportPassword,
                            out int localRead,
                            out RSAParameters key);
                        return key;
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyBlob);
                }
            }

            private static bool HasWorkingPKCS1Padding { get; } = OperatingSystem.IsMacOSVersionAtLeast(10, 15);

            private static void ImportPrivateKey(
                RSAParameters rsaParameters,
                out SafeSecKeyRefHandle privateKey,
                out SafeSecKeyRefHandle publicKey)
            {
                // macOS 10.14 and older have broken PKCS#1 depadding for decryption
                // of empty data. The bug doesn't affect the legacy CSSM keys so we
                // use them instead.
                if (HasWorkingPKCS1Padding)
                {
                    privateKey = ImportKey(rsaParameters);
                    publicKey = Interop.AppleCrypto.CopyPublicKey(privateKey);
                }
                else
                {
                    privateKey = ImportLegacyPrivateKey(rsaParameters);

                    try
                    {
                        RSAParameters publicOnly = new RSAParameters
                        {
                            Modulus = rsaParameters.Modulus,
                            Exponent = rsaParameters.Exponent,
                        };

                        publicKey = ImportKey(publicOnly);
                    }
                    catch
                    {
                        privateKey.Dispose();
                        throw;
                    }
                }
            }

            private static SafeSecKeyRefHandle ImportLegacyPrivateKey(RSAParameters parameters)
            {
                Debug.Assert(parameters.D != null);

                AsnWriter keyWriter = RSAKeyFormatHelper.WritePkcs1PrivateKey(parameters);

                byte[] rented = CryptoPool.Rent(keyWriter.GetEncodedLength());

                if (!keyWriter.TryEncode(rented, out int written))
                {
                    Debug.Fail("TryEncode failed with a pre-allocated buffer");
                    throw new InvalidOperationException();
                }

                // Explicitly clear the inner buffer
                keyWriter.Reset();

                try
                {
                    return Interop.AppleCrypto.ImportEphemeralKey(rented.AsSpan(0, written), true);
                }
                finally
                {
                    CryptoPool.Return(rented, written);
                }
            }
        }
    }
}
