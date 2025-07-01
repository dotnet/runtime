// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using static Interop.BCrypt;

namespace System.Security.Cryptography
{
    internal static partial class ECCng
    {
        internal static CngKey ImportKeyBlob(byte[] ecBlob, string curveName, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccPrivateBlob : CngKeyBlobFormat.EccPublicBlob;
            CngKey newKey = CngKey.Import(ecBlob, curveName, blobFormat);
            newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;

            return newKey;
        }

        internal static CngKey ImportFullKeyBlob(byte[] ecBlob, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccFullPrivateBlob : CngKeyBlobFormat.EccFullPublicBlob;
            CngKey newKey = CngKey.Import(ecBlob, blobFormat);
            newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;

            return newKey;
        }

        internal static byte[] ExportKeyBlob(CngKey key, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccPrivateBlob : CngKeyBlobFormat.EccPublicBlob;
            return key.Export(blobFormat);
        }

        internal static byte[] ExportFullKeyBlob(CngKey key, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccFullPrivateBlob : CngKeyBlobFormat.EccFullPublicBlob;
            return key.Export(blobFormat);
        }

        internal static byte[] ExportKeyBlob(
            CngKey key,
            bool includePrivateParameters,
            out CngKeyBlobFormat format,
            out string? curveName)
        {
            curveName = key.GetCurveName(out _);
            bool forceGenericBlob = false;

            if (string.IsNullOrEmpty(curveName))
            {
                // Normalize curveName to null.
                curveName = null;

                forceGenericBlob = true;
                format = includePrivateParameters ?
                    CngKeyBlobFormat.EccFullPrivateBlob :
                    CngKeyBlobFormat.EccFullPublicBlob;
            }
            else
            {
                format = includePrivateParameters ?
                    CngKeyBlobFormat.EccPrivateBlob :
                    CngKeyBlobFormat.EccPublicBlob;
            }

            byte[] blob = key.Export(format);

            // Importing a known NIST curve as explicit parameters NCryptExportKey may
            // cause it to export with the dwMagic of the known curve and a generic blob body.
            // This combination can't be re-imported. So correct the dwMagic value to allow it
            // to import.
            if (forceGenericBlob)
            {
                FixupGenericBlob(blob);
            }

            return blob;
        }

        internal static ECParameters ExportExplicitParameters(CngKey key, bool includePrivateParameters)
        {
            if (includePrivateParameters)
            {
                return ExportPrivateExplicitParameters(key);
            }
            else
            {
                byte[] blob = ExportFullKeyBlob(key, includePrivateParameters: false);
                ECParameters ecparams = default;
                ExportPrimeCurveParameters(ref ecparams, blob, includePrivateParameters: false);
                return ecparams;
            }
        }

        internal static ECParameters ExportParameters(CngKey key, bool includePrivateParameters)
        {
            ECParameters ecparams = default;

            const string TemporaryExportPassword = "DotnetExportPhrase";
            string? curveName = key.GetCurveName(out string? oidValue);

            if (string.IsNullOrEmpty(curveName))
            {
                if (includePrivateParameters)
                {
                    ecparams = ExportPrivateExplicitParameters(key);
                }
                else
                {
                    byte[] fullKeyBlob = ExportFullKeyBlob(key, includePrivateParameters: false);
                    ECCng.ExportPrimeCurveParameters(ref ecparams, fullKeyBlob, includePrivateParameters: false);
                }
            }
            else
            {
                bool encryptedOnlyExport = CngPkcs8.AllowsOnlyEncryptedExport(key);

                if (includePrivateParameters && encryptedOnlyExport)
                {
                    byte[] exported = key.ExportPkcs8KeyBlob(TemporaryExportPassword, 1);
                    EccKeyFormatHelper.ReadEncryptedPkcs8(
                        exported,
                        TemporaryExportPassword,
                        out _,
                        out ecparams);
                }
                else
                {
                    byte[] keyBlob = ExportKeyBlob(key, includePrivateParameters);
                    ECCng.ExportNamedCurveParameters(ref ecparams, keyBlob, includePrivateParameters);
                    ecparams.Curve = ECCurve.CreateFromOid(new Oid(oidValue, curveName));
                }
            }

            return ecparams;
        }

        private static ECParameters ExportPrivateExplicitParameters(CngKey key)
        {
            bool encryptedOnlyExport = CngPkcs8.AllowsOnlyEncryptedExport(key);

            ECParameters ecparams = default;

            if (encryptedOnlyExport)
            {
                // We can't ask CNG for the explicit parameters when performing a PKCS#8 export. Instead,
                // we ask CNG for the explicit parameters for the public part only, since the parameters are public.
                // Then we ask CNG by encrypted PKCS#8 for the private parameters (D) and combine the explicit public
                // key along with the private key.
                const string TemporaryExportPassword = "DotnetExportPhrase";
                byte[] publicKeyBlob = ExportFullKeyBlob(key, includePrivateParameters: false);
                ExportPrimeCurveParameters(ref ecparams, publicKeyBlob, includePrivateParameters: false);

                byte[] exported = key.ExportPkcs8KeyBlob(TemporaryExportPassword, 1);
                EccKeyFormatHelper.ReadEncryptedPkcs8(
                    exported,
                    TemporaryExportPassword,
                    out _,
                    out ECParameters localParameters);

                Debug.Assert(ecparams.Q.X.AsSpan().SequenceEqual(localParameters.Q.X));
                Debug.Assert(ecparams.Q.Y.AsSpan().SequenceEqual(localParameters.Q.Y));
                ecparams.D = localParameters.D;
            }
            else
            {
                byte[] blob = ExportFullKeyBlob(key, includePrivateParameters: true);
                ExportPrimeCurveParameters(ref ecparams, blob, includePrivateParameters: true);
            }

            return ecparams;
        }

        private static unsafe void FixupGenericBlob(byte[] blob)
        {
            if (blob.Length > sizeof(BCRYPT_ECCKEY_BLOB))
            {
                fixed (byte* pBlob = blob)
                {
                    BCRYPT_ECCKEY_BLOB* pBcryptBlob = (BCRYPT_ECCKEY_BLOB*)pBlob;

                    switch ((KeyBlobMagicNumber)pBcryptBlob->Magic)
                    {
                        case KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_P256_MAGIC:
                        case KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_P384_MAGIC:
                        case KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_P521_MAGIC:
                            pBcryptBlob->Magic = KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC;
                            break;
                        case KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_P256_MAGIC:
                        case KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_P384_MAGIC:
                        case KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_P521_MAGIC:
                            pBcryptBlob->Magic = KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC;
                            break;
                        case KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P256_MAGIC:
                        case KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P384_MAGIC:
                        case KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P521_MAGIC:
                            pBcryptBlob->Magic = KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC;
                            break;
                        case KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P256_MAGIC:
                        case KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P384_MAGIC:
                        case KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P521_MAGIC:
                            pBcryptBlob->Magic = KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC;
                            break;
                    }
                }
            }
        }
    }
}
