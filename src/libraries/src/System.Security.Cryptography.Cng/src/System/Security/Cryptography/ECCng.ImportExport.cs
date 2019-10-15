// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            out string curveName)
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
