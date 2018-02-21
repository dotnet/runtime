// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal : PkcsPal
    {
        public override void AddCertsFromStoreForDecryption(X509Certificate2Collection certs)
        {
            certs.AddRange(Helpers.GetStoreCertificates(StoreName.My, StoreLocation.CurrentUser, openExistingOnly: false));

            try
            {
                // This store exists on macOS, but not Linux
                certs.AddRange(
                    Helpers.GetStoreCertificates(StoreName.My, StoreLocation.LocalMachine, openExistingOnly: false));
            }
            catch (CryptographicException)
            {
            }
        }

        public override byte[] GetSubjectKeyIdentifier(X509Certificate2 certificate)
        {
            return certificate.GetSubjectKeyIdentifier();
        }

        public override T GetPrivateKeyForSigning<T>(X509Certificate2 certificate, bool silent)
        {
            return GetPrivateKey<T>(certificate);
        }

        public override T GetPrivateKeyForDecryption<T>(X509Certificate2 certificate, bool silent)
        {
            return GetPrivateKey<T>(certificate);
        }

        private T GetPrivateKey<T>(X509Certificate2 certificate) where T : AsymmetricAlgorithm
        {
            if (typeof(T) == typeof(RSA))
                return (T)(object)certificate.GetRSAPrivateKey();
            if (typeof(T) == typeof(ECDsa))
                return (T)(object)certificate.GetECDsaPrivateKey();
#if netcoreapp
            if (typeof(T) == typeof(DSA))
                return (T)(object)certificate.GetDSAPrivateKey();
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

                Rc2CbcParameters rc2Params = AsnSerializer.Deserialize<Rc2CbcParameters>(
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

                AsnReader reader = new AsnReader(contentEncryptionAlgorithm.Parameters.Value, AsnEncodingRules.BER);

                if (reader.TryGetPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> primitiveBytes))
                {
                    alg.IV = primitiveBytes.ToArray();
                }
                else
                {
                    byte[] iv = new byte[alg.BlockSize / 8];

                    if (!reader.TryCopyOctetStringBytes(iv, out int bytesWritten) ||
                        bytesWritten != iv.Length)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    alg.IV = iv;
                }
            }

            return alg;
        }

        private static SymmetricAlgorithm OpenAlgorithm(AlgorithmIdentifier algorithmIdentifier)
        {
            SymmetricAlgorithm alg = OpenAlgorithm(algorithmIdentifier.Oid);

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

        private static SymmetricAlgorithm OpenAlgorithm(Oid algorithmIdentifier)
        {
            Debug.Assert(algorithmIdentifier != null);

            SymmetricAlgorithm alg;

            switch (algorithmIdentifier.Value)
            {
                case Oids.Rc2Cbc:
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
                    throw new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm, algorithmIdentifier.Value);
            }

            // These are the defaults, but they're restated here for clarity.
            alg.Padding = PaddingMode.PKCS7;
            alg.Mode = CipherMode.CBC;
            return alg;
        }
    }
}
