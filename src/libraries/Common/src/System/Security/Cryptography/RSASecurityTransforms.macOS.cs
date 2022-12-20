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
                            (ReadOnlySpan<char>)ExportPassword,
                            out _,
                            out RSAParameters key);
                        return key;
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyBlob);
                }
            }
        }
    }
}
