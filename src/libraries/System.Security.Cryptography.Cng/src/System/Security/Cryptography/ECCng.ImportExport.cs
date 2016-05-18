// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    using BCRYPT_ECCFULLKEY_BLOB = Interop.BCrypt.BCRYPT_ECCFULLKEY_BLOB;
    using BCRYPT_ECCKEY_BLOB = Interop.BCrypt.BCRYPT_ECCKEY_BLOB;
    using ErrorCode = Interop.NCrypt.ErrorCode;
    using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;

    internal static partial class ECCng
    {
        private static CngKey ImportKeyBlob(byte[] ecBlob, ECCurve curve, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccPrivateBlob : CngKeyBlobFormat.EccPublicBlob;
            CngKey newKey = CngKey.Import(ecBlob, curve, blobFormat);
            newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;

            return newKey;
        }

        private static CngKey ImportFullKeyBlob(byte[] ecBlob, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccFullPrivateBlob : CngKeyBlobFormat.EccFullPublicBlob;
            CngKey newKey = CngKey.Import(ecBlob, blobFormat);
            newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;

            return newKey;
        }

        private static byte[] ExportKeyBlob(CngKey key, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccPrivateBlob : CngKeyBlobFormat.EccPublicBlob;
            return key.Export(blobFormat);
        }

        private static byte[] ExportFullKeyBlob(CngKey key, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccFullPrivateBlob : CngKeyBlobFormat.EccFullPublicBlob;
            return key.Export(blobFormat);
        }

        internal static CngKey ImportNamedCurveParameters(ECParameters parameters, bool includePrivateParameters)
        {
            Debug.Assert(parameters.Curve.IsNamed);

            unsafe
            {
                // We need to build a key blob structured as follows:
                //     BCRYPT_ECCKEY_BLOB   header
                //     byte[cbKey]          Q.X
                //     byte[cbKey]          Q.Y
                //     -- Only if "includePrivateParameters" is true --
                //     byte[cbKey]          D

                int blobSize = sizeof(BCRYPT_ECCKEY_BLOB) +
                    parameters.Q.X.Length +
                    parameters.Q.Y.Length;
                if (includePrivateParameters)
                {
                    blobSize += parameters.D.Length;
                }

                byte[] blob = new byte[blobSize];
                fixed (byte* pBlob = blob)
                {
                    // Build the header
                    BCRYPT_ECCKEY_BLOB* pBcryptBlob = (BCRYPT_ECCKEY_BLOB*)pBlob;
                    pBcryptBlob->Magic = CurveNameToMagicNumber(parameters.Curve.Oid.FriendlyName, includePrivateParameters);
                    pBcryptBlob->cbKey = parameters.Q.X.Length;

                    // Emit the blob
                    int offset = sizeof(BCRYPT_ECCKEY_BLOB);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.Q.X);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.Q.Y);
                    if (includePrivateParameters)
                    {
                        Interop.BCrypt.Emit(blob, ref offset, parameters.D);
                    }

                    // We better have computed the right allocation size above!
                    Debug.Assert(offset == blobSize, "offset == blobSize");
                }

                return ImportKeyBlob(blob, parameters.Curve, includePrivateParameters);
            }
        }

        internal static CngKey ImportPrimeCurveParameters(ECParameters parameters, bool includePrivateParameters)
        {
            Debug.Assert(parameters.Curve.IsPrime);

            ECCurve curve = parameters.Curve;

            unsafe
            {
                // We need to build a key blob structured as follows:
                //     BCRYPT_ECCFULLKEY_BLOB       header
                //     byte[cbFieldLength]          P
                //     byte[cbFieldLength]          A
                //     byte[cbFieldLength]          B
                //     byte[cbFieldLength]          G.X
                //     byte[cbFieldLength]          G.Y
                //     byte[cbSubgroupOrder]        Order (n)
                //     byte[cbCofactor]             Cofactor (h)
                //     byte[cbSeed]                 Seed
                //     byte[cbFieldLength]          Q.X
                //     byte[cbFieldLength]          Q.Y
                //     -- Only if "includePrivateParameters" is true --
                //     byte[cbSubgroupOrder]        D

                int blobSize = sizeof(BCRYPT_ECCFULLKEY_BLOB) +
                    curve.Prime.Length +
                    curve.A.Length +
                    curve.B.Length +
                    curve.G.X.Length +
                    curve.G.Y.Length +
                    curve.Order.Length +
                    curve.Cofactor.Length +
                    (curve.Seed == null ? 0 : curve.Seed.Length) +
                    parameters.Q.X.Length +
                    parameters.Q.Y.Length;

                if (includePrivateParameters)
                {
                    blobSize += parameters.D.Length;
                }

                byte[] blob = new byte[blobSize];
                fixed (byte* pBlob = blob)
                {
                    // Build the header
                    BCRYPT_ECCFULLKEY_BLOB* pBcryptBlob = (BCRYPT_ECCFULLKEY_BLOB*)pBlob;
                    pBcryptBlob->Version = 1; // No constant for this found in bcrypt.h
                    pBcryptBlob->Magic = includePrivateParameters ?
                        KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC :
                        KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC;
                    pBcryptBlob->cbCofactor = curve.Cofactor.Length;
                    pBcryptBlob->cbFieldLength = parameters.Q.X.Length;
                    pBcryptBlob->cbSeed = curve.Seed == null ? 0 : curve.Seed.Length;
                    pBcryptBlob->cbSubgroupOrder = curve.Order.Length;
                    pBcryptBlob->CurveGenerationAlgId = GetHashAlgorithmId(curve.Hash);
                    pBcryptBlob->CurveType = ConvertToCurveTypeEnum(curve.CurveType);

                    // Emit the blob
                    int offset = sizeof(BCRYPT_ECCFULLKEY_BLOB);
                    Interop.BCrypt.Emit(blob, ref offset, curve.Prime);
                    Interop.BCrypt.Emit(blob, ref offset, curve.A);
                    Interop.BCrypt.Emit(blob, ref offset, curve.B);
                    Interop.BCrypt.Emit(blob, ref offset, curve.G.X);
                    Interop.BCrypt.Emit(blob, ref offset, curve.G.Y);
                    Interop.BCrypt.Emit(blob, ref offset, curve.Order);
                    Interop.BCrypt.Emit(blob, ref offset, curve.Cofactor);
                    if (curve.Seed != null)
                    {
                        Interop.BCrypt.Emit(blob, ref offset, curve.Seed);
                    }
                    Interop.BCrypt.Emit(blob, ref offset, parameters.Q.X);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.Q.Y);
                    if (includePrivateParameters)
                    {
                        Interop.BCrypt.Emit(blob, ref offset, parameters.D);
                    }

                    // We better have computed the right allocation size above!
                    Debug.Assert(offset == blobSize, "offset == blobSize");
                }

                return ImportFullKeyBlob(blob, includePrivateParameters);
            }
        }

        internal static ECParameters ExportNamedCurveParameters(CngKey key, bool includePrivateParameters)
        {
            byte[] ecBlob = ExportKeyBlob(key, includePrivateParameters);
            // We now have a buffer laid out as follows:
            //     BCRYPT_ECCKEY_BLOB   header
            //     byte[cbKey]          Q.X
            //     byte[cbKey]          Q.Y
            //     -- Private only --
            //     byte[cbKey]          D

            ECParameters ecParams = new ECParameters();

            KeyBlobMagicNumber magic = (KeyBlobMagicNumber)BitConverter.ToInt32(ecBlob, 0);

            // Check the magic value in the key blob header. If the blob does not have the required magic,
            // then throw a CryptographicException.
            CheckMagicValueOfKey(magic, includePrivateParameters);

            unsafe
            {
                // Fail-fast if a rogue provider gave us a blob that isn't even the size of the blob header.
                if (ecBlob.Length < sizeof(BCRYPT_ECCKEY_BLOB))
                    throw ErrorCode.E_FAIL.ToCryptographicException();

                fixed (byte* pEcBlob = ecBlob)
                {
                    BCRYPT_ECCKEY_BLOB* pBcryptBlob = (BCRYPT_ECCKEY_BLOB*)pEcBlob;

                    int offset = sizeof(BCRYPT_ECCKEY_BLOB);

                    ecParams.Q = new ECPoint
                    {
                        X = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbKey),
                        Y = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbKey)
                    };

                    if (includePrivateParameters)
                    {
                        ecParams.D = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbKey);
                    }
                }
            }

            ecParams.Curve = ECCurve.CreateFromFriendlyName(key.GetCurveName());

            return ecParams;
        }

        internal static ECParameters ExportPrimeCurveParameters(CngKey key, bool includePrivateParameters)
        {
            byte[] ecBlob = ExportFullKeyBlob(key, includePrivateParameters);
            // We now have a buffer laid out as follows:
            //     BCRYPT_ECCFULLKEY_BLOB       header
            //     byte[cbFieldLength]          P
            //     byte[cbFieldLength]          A
            //     byte[cbFieldLength]          B
            //     byte[cbFieldLength]          G.X
            //     byte[cbFieldLength]          G.Y
            //     byte[cbSubgroupOrder]        Order (n)
            //     byte[cbCofactor]             Cofactor (h)
            //     byte[cbSeed]                 Seed
            //     byte[cbFieldLength]          Q.X
            //     byte[cbFieldLength]          Q.Y
            //     -- Private only --
            //     byte[cbSubgroupOrder]        D

            ECParameters ecParams = new ECParameters();

            KeyBlobMagicNumber magic = (KeyBlobMagicNumber)BitConverter.ToInt32(ecBlob, 0);

            // Check the magic value in the key blob header. If the blob does not have the required magic,
            // then throw a CryptographicException.
            CheckMagicValueOfKey(magic, includePrivateParameters);

            unsafe
            {
                // Fail-fast if a rogue provider gave us a blob that isn't even the size of the blob header.
                if (ecBlob.Length < sizeof(BCRYPT_ECCFULLKEY_BLOB))
                    throw ErrorCode.E_FAIL.ToCryptographicException();

                fixed (byte* pEcBlob = ecBlob)
                {
                    BCRYPT_ECCFULLKEY_BLOB* pBcryptBlob = (BCRYPT_ECCFULLKEY_BLOB*)pEcBlob;

                    var primeCurve = new ECCurve();
                    primeCurve.CurveType = ConvertToCurveTypeEnum(pBcryptBlob->CurveType);
                    primeCurve.Hash = GetHashAlgorithmName(pBcryptBlob->CurveGenerationAlgId);

                    int offset = sizeof(BCRYPT_ECCFULLKEY_BLOB);

                    primeCurve.Prime = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbFieldLength);
                    primeCurve.A = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbFieldLength);
                    primeCurve.B = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbFieldLength);
                    primeCurve.G = new ECPoint()
                    {
                        X = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbFieldLength),
                        Y = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbFieldLength),
                    };
                    primeCurve.Order = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbSubgroupOrder);
                    primeCurve.Cofactor = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbCofactor);

                    // Optional parameters
                    primeCurve.Seed = pBcryptBlob->cbSeed == 0 ? null : Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbSeed);

                    ecParams.Q = new ECPoint
                    {
                        X = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbFieldLength),
                        Y = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbFieldLength)
                    };

                    if (includePrivateParameters)
                    {
                        ecParams.D = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbSubgroupOrder);
                    }

                    ecParams.Curve = primeCurve;
                    return ecParams;
                }
            }
        }

        /// <summary>
        ///     This function checks the magic value in the key blob header
        /// </summary>
        /// <param name="includePrivateParameters">Private blob if true else public key blob</param>
        private static void CheckMagicValueOfKey(KeyBlobMagicNumber magic, bool includePrivateParameters)
        {
            if (includePrivateParameters)
            {
                if (!IsMagicValueOfKeyPrivate(magic))
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                }
            }
            else
            {
                if (!IsMagicValueOfKeyPublic(magic))
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                }
            }
        }

        private static bool IsMagicValueOfKeyPrivate(KeyBlobMagicNumber magic)
        {
            switch (magic)
            {
                case KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P256_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_P256_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P384_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_P384_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P521_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_P521_MAGIC:
                    return true;
            }

            return false;
        }

        private static bool IsMagicValueOfKeyPublic(KeyBlobMagicNumber magic)
        {
            switch (magic)
            {
                case KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P256_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_P256_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P384_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_P384_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P521_MAGIC:
                case KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_P521_MAGIC:
                    return true;
            }

            // Private key magic is permissible too since the public key can be derived from the private key blob.
            return IsMagicValueOfKeyPrivate(magic);
        }

        /// <summary>
        /// Map a curve name to magic number. Maps the names of the curves that worked pre-Win10
        /// to the pre-Win10 magic numbers to support import on pre-Win10 environments 
        /// that don't have the named curve functionality.
        /// </summary>
        private static KeyBlobMagicNumber CurveNameToMagicNumber(string name, bool includePrivateParameters)
        {
            CngAlgorithm alg = CngKey.EcdsaCurveNameToAlgorithm(name);
            if (alg == CngAlgorithm.ECDsaP256)
            {
                return includePrivateParameters ?
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P256_MAGIC :
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P256_MAGIC;
            }

            if (alg == CngAlgorithm.ECDsaP384)
            {
                return includePrivateParameters ?
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P384_MAGIC :
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P384_MAGIC;
            }

            if (alg == CngAlgorithm.ECDsaP521)
            {
                return includePrivateParameters ?
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P521_MAGIC :
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P521_MAGIC;
            }

            // all other curves are new in Win10 so use named curves
            return includePrivateParameters ?
                KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC :
                KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC;
        }

        /// <summary>
        /// Helper method to map between BCrypt.ECC_CURVE_TYPE_ENUM and ECCurve.ECCurveType
        /// </summary>
        internal static Interop.BCrypt.ECC_CURVE_TYPE_ENUM ConvertToCurveTypeEnum(ECCurve.ECCurveType value)
        {
            // Currently values 1-3 are interchangeable
            Debug.Assert(value == ECCurve.ECCurveType.Characteristic2 || 
                value == ECCurve.ECCurveType.PrimeShortWeierstrass || 
                value == ECCurve.ECCurveType.PrimeTwistedEdwards);
            return (Interop.BCrypt.ECC_CURVE_TYPE_ENUM)value;
        }

        /// <summary>
        /// Helper method to map between BCrypt.ECC_CURVE_TYPE_ENUM and ECCurve.ECCurveType
        /// </summary>
        internal static ECCurve.ECCurveType ConvertToCurveTypeEnum(Interop.BCrypt.ECC_CURVE_TYPE_ENUM value)
        {
            // Currently values 1-3 are interchangeable
            ECCurve.ECCurveType curveType = (ECCurve.ECCurveType)value;
            Debug.Assert(curveType == ECCurve.ECCurveType.Characteristic2 || 
                curveType == ECCurve.ECCurveType.PrimeShortWeierstrass || 
                curveType == ECCurve.ECCurveType.PrimeTwistedEdwards);
            return curveType;
        }
    }
}
