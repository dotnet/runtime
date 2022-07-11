// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal : PkcsPal
    {
        public override void AddCertsFromStoreForDecryption(X509Certificate2Collection certs)
        {
            certs.AddRange(PkcsHelpers.GetStoreCertificates(StoreName.My, StoreLocation.CurrentUser, openExistingOnly: false));

            try
            {
                // This store exists on macOS, but not Linux
                certs.AddRange(
                    PkcsHelpers.GetStoreCertificates(StoreName.My, StoreLocation.LocalMachine, openExistingOnly: false));
            }
            catch (CryptographicException)
            {
            }
        }

        public override byte[] GetSubjectKeyIdentifier(X509Certificate2 certificate)
        {
            Debug.Assert(certificate != null);

            X509Extension extension =
                certificate.Extensions[Oids.SubjectKeyIdentifier] ??
                new X509SubjectKeyIdentifierExtension( // Construct the value from the public key info.
                    certificate.PublicKey,
                    X509SubjectKeyIdentifierHashAlgorithm.CapiSha1,
                    false);

            try
            {
                // Certificates are DER encoded.
                AsnValueReader reader = new AsnValueReader(extension.RawData, AsnEncodingRules.DER);

                if (reader.TryReadPrimitiveOctetString(out ReadOnlySpan<byte> contents))
                {
                    reader.ThrowIfNotEmpty();
                    return contents.ToArray();
                }

                // TryGetPrimitiveOctetStringBytes will have thrown if the next tag wasn't
                // Universal (primitive) OCTET STRING, since we're in DER mode.
                // So there's really no way we can get here.
                Debug.Fail($"TryGetPrimitiveOctetStringBytes returned false in DER mode");
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public override T? GetPrivateKeyForSigning<T>(X509Certificate2 certificate, bool silent) where T : class
        {
            return GetPrivateKey<T>(certificate);
        }

        public override T? GetPrivateKeyForDecryption<T>(X509Certificate2 certificate, bool silent) where T : class
        {
            return GetPrivateKey<T>(certificate);
        }

        private static T? GetPrivateKey<T>(X509Certificate2 certificate) where T : AsymmetricAlgorithm
        {
            if (typeof(T) == typeof(RSA))
                return (T?)(object?)certificate.GetRSAPrivateKey();
            if (typeof(T) == typeof(ECDsa))
                return (T?)(object?)certificate.GetECDsaPrivateKey();
#if NETCOREAPP || NETSTANDARD2_1
            if (typeof(T) == typeof(DSA) && Internal.Cryptography.Helpers.IsDSASupported)
                return (T?)(object?)certificate.GetDSAPrivateKey();
#endif

            Debug.Fail($"Unknown key type requested: {typeof(T).FullName}");
            return null;
        }

        private static SymmetricAlgorithm OpenAlgorithm(AlgorithmIdentifierAsn contentEncryptionAlgorithm)
        {
            SymmetricAlgorithm alg = OpenAlgorithm(contentEncryptionAlgorithm.Algorithm);

            if (alg is RC2)
            {
                if (contentEncryptionAlgorithm.Parameters == null)
                {
                    // Windows issues CRYPT_E_BAD_DECODE
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                Rc2CbcParameters rc2Params = Rc2CbcParameters.Decode(
                    contentEncryptionAlgorithm.Parameters.Value,
                    AsnEncodingRules.BER);

                alg.KeySize = rc2Params.GetEffectiveKeyBits();
                alg.IV = rc2Params.Iv.ToArray();
            }
            else
            {
                if (contentEncryptionAlgorithm.Parameters == null)
                {
                    // Windows issues CRYPT_E_BAD_DECODE
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                try
                {
                    AsnReader reader = new AsnReader(contentEncryptionAlgorithm.Parameters.Value, AsnEncodingRules.BER);
                    alg.IV = reader.ReadOctetString();

                    if (alg.IV.Length != alg.BlockSize / 8)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }
                catch (AsnContentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
            }

            return alg;
        }

        private static SymmetricAlgorithm OpenAlgorithm(AlgorithmIdentifier algorithmIdentifier)
        {
            SymmetricAlgorithm alg = OpenAlgorithm(algorithmIdentifier.Oid.Value!);

            if (alg is RC2)
            {
                if (algorithmIdentifier.KeyLength != 0)
                {
                    alg.KeySize = algorithmIdentifier.KeyLength;
                }
                else
                {
                    alg.KeySize = KeyLengths.Rc2_128Bit;
                }
            }

            return alg;
        }

        private static SymmetricAlgorithm OpenAlgorithm(string algorithmIdentifier)
        {
            Debug.Assert(algorithmIdentifier != null);

            SymmetricAlgorithm alg;

            switch (algorithmIdentifier)
            {
                case Oids.Rc2Cbc:
                    if (!Helpers.IsRC2Supported)
                    {
                        throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(RC2)));
                    }
#pragma warning disable CA5351
                    alg = RC2.Create();
#pragma warning restore CA5351
                    break;
                case Oids.DesCbc:
#pragma warning disable CA5351
                    alg = DES.Create();
#pragma warning restore CA5351
                    break;
                case Oids.TripleDesCbc:
#pragma warning disable CA5350
                    alg = TripleDES.Create();
#pragma warning restore CA5350
                    break;
                case Oids.Aes128Cbc:
                    alg = Aes.Create();
                    alg.KeySize = 128;
                    break;
                case Oids.Aes192Cbc:
                    alg = Aes.Create();
                    alg.KeySize = 192;
                    break;
                case Oids.Aes256Cbc:
                    alg = Aes.Create();
                    alg.KeySize = 256;
                    break;
                default:
                    throw new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm, algorithmIdentifier);
            }

            // These are the defaults, but they're restated here for clarity.
            alg.Padding = PaddingMode.PKCS7;
            alg.Mode = CipherMode.CBC;
            return alg;
        }
    }
}
