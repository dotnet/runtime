// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Internal.NativeCrypto.BCryptNative;

using BCRYPT_ECCFULLKEY_BLOB = Interop.BCrypt.BCRYPT_ECCFULLKEY_BLOB;
using BCRYPT_ECCKEY_BLOB = Interop.BCrypt.BCRYPT_ECCKEY_BLOB;
using BCRYPT_ECC_PARAMETER_HEADER = Interop.BCrypt.BCRYPT_ECC_PARAMETER_HEADER;
using ErrorCode = Interop.NCrypt.ErrorCode;
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;

namespace System.Security.Cryptography
{
    internal static partial class ECCng
    {
        internal static byte[] GetNamedCurveBlob(ref ECParameters parameters, bool ecdh)
        {
            Debug.Assert(parameters.Curve.IsNamed);

            bool includePrivateParameters = (parameters.D != null);
            byte[] blob;
            unsafe
            {
                // We need to build a key blob structured as follows:
                //     BCRYPT_ECCKEY_BLOB   header
                //     byte[cbKey]          Q.X
                //     byte[cbKey]          Q.Y
                //     -- Only if "includePrivateParameters" is true --
                //     byte[cbKey]          D

                int blobSize = sizeof(BCRYPT_ECCKEY_BLOB) +
                    parameters.Q.X!.Length +
                    parameters.Q.Y!.Length;
                if (includePrivateParameters)
                {
                    blobSize += parameters.D!.Length;
                }

                blob = new byte[blobSize];
                fixed (byte* pBlob = &blob[0])
                {
                    // Build the header
                    BCRYPT_ECCKEY_BLOB* pBcryptBlob = (BCRYPT_ECCKEY_BLOB*)pBlob;
                    pBcryptBlob->Magic = ecdh ?
                        EcdhCurveNameToMagicNumber(parameters.Curve.Oid.FriendlyName, includePrivateParameters) :
                        EcdsaCurveNameToMagicNumber(parameters.Curve.Oid.FriendlyName, includePrivateParameters);
                    pBcryptBlob->cbKey = parameters.Q.X.Length;

                    // Emit the blob
                    int offset = sizeof(BCRYPT_ECCKEY_BLOB);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.Q.X);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.Q.Y);
                    if (includePrivateParameters)
                    {
                        Interop.BCrypt.Emit(blob, ref offset, parameters.D!);
                    }

                    // We better have computed the right allocation size above!
                    Debug.Assert(offset == blobSize, "offset == blobSize");
                }
            }
            return blob;
        }

        internal static byte[] GetPrimeCurveBlob(ref ECParameters parameters, bool ecdh)
        {
            Debug.Assert(parameters.Curve.IsPrime);

            bool includePrivateParameters = (parameters.D != null);
            ECCurve curve = parameters.Curve;
            byte[] blob;
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
                    curve.Prime!.Length +
                    curve.A!.Length +
                    curve.B!.Length +
                    curve.G.X!.Length +
                    curve.G.Y!.Length +
                    curve.Order!.Length +
                    curve.Cofactor!.Length +
                    (curve.Seed == null ? 0 : curve.Seed.Length) +
                    parameters.Q.X!.Length +
                    parameters.Q.Y!.Length;

                if (includePrivateParameters)
                {
                    blobSize += parameters.D!.Length;
                }

                blob = new byte[blobSize];
                fixed (byte* pBlob = &blob[0])
                {
                    // Build the header
                    BCRYPT_ECCFULLKEY_BLOB* pBcryptBlob = (BCRYPT_ECCFULLKEY_BLOB*)pBlob;
                    pBcryptBlob->Version = 1; // No constant for this found in bcrypt.h
                    pBcryptBlob->Magic = includePrivateParameters ?
                        (ecdh ? KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC : KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC) :
                        (ecdh ? KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC : KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC);
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
                        Interop.BCrypt.Emit(blob, ref offset, parameters.D!);
                    }

                    // We better have computed the right allocation size above!
                    Debug.Assert(offset == blobSize, "offset == blobSize");
                }

                return blob;
            }
        }

        internal static void ExportNamedCurveParameters(ref ECParameters ecParams, byte[] ecBlob, bool includePrivateParameters)
        {
            // We now have a buffer laid out as follows:
            //     BCRYPT_ECCKEY_BLOB   header
            //     byte[cbKey]          Q.X
            //     byte[cbKey]          Q.Y
            //     -- Private only --
            //     byte[cbKey]          D

            KeyBlobMagicNumber magic = (KeyBlobMagicNumber)BitConverter.ToInt32(ecBlob, 0);

            // Check the magic value in the key blob header. If the blob does not have the required magic,
            // then throw a CryptographicException.
            CheckMagicValueOfKey(magic, includePrivateParameters);

            unsafe
            {
                // Fail-fast if a rogue provider gave us a blob that isn't even the size of the blob header.
                if (ecBlob.Length < sizeof(BCRYPT_ECCKEY_BLOB))
                    throw ErrorCode.E_FAIL.ToCryptographicException();

                fixed (byte* pEcBlob = &ecBlob[0])
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
        }

        internal static void ExportPrimeCurveParameters(ref ECParameters ecParams, byte[] ecBlob, bool includePrivateParameters)
        {
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

            KeyBlobMagicNumber magic = (KeyBlobMagicNumber)BitConverter.ToInt32(ecBlob, 0);

            // Check the magic value in the key blob header. If the blob does not have the required magic,
            // then throw a CryptographicException.
            CheckMagicValueOfKey(magic, includePrivateParameters);

            unsafe
            {
                // Fail-fast if a rogue provider gave us a blob that isn't even the size of the blob header.
                if (ecBlob.Length < sizeof(BCRYPT_ECCFULLKEY_BLOB))
                    throw ErrorCode.E_FAIL.ToCryptographicException();

                fixed (byte* pEcBlob = &ecBlob[0])
                {
                    BCRYPT_ECCFULLKEY_BLOB* pBcryptBlob = (BCRYPT_ECCFULLKEY_BLOB*)pEcBlob;

                    ECCurve primeCurve = default;
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
                }
            }
        }

        internal static unsafe byte[] GetPrimeCurveParameterBlob(ref ECCurve curve)
        {
            // We need to build a key blob structured as follows:
            //     BCRYPT_ECC_PARAMETER_HEADER  header
            //     byte[cbFieldLength]          P
            //     byte[cbFieldLength]          A
            //     byte[cbFieldLength]          B
            //     byte[cbFieldLength]          G.X
            //     byte[cbFieldLength]          G.Y
            //     byte[cbSubgroupOrder]        Order (n)
            //     byte[cbCofactor]             Cofactor (h)
            //     byte[cbSeed]                 Seed

            int blobSize = sizeof(BCRYPT_ECC_PARAMETER_HEADER) +
                curve.Prime!.Length +
                curve.A!.Length +
                curve.B!.Length +
                curve.G.X!.Length +
                curve.G.Y!.Length +
                curve.Order!.Length +
                curve.Cofactor!.Length +
                (curve.Seed == null ? 0 : curve.Seed.Length);

            byte[] blob = new byte[blobSize];
            fixed (byte* pBlob = &blob[0])
            {
                // Build the header
                BCRYPT_ECC_PARAMETER_HEADER* pBcryptBlob = (BCRYPT_ECC_PARAMETER_HEADER*)pBlob;
                pBcryptBlob->Version = Interop.BCrypt.BCRYPT_ECC_PARAMETER_HEADER_V1;
                pBcryptBlob->cbCofactor = curve.Cofactor.Length;
                pBcryptBlob->cbFieldLength = curve.A.Length; // P, A, B, X, Y have the same length
                pBcryptBlob->cbSeed = curve.Seed == null ? 0 : curve.Seed.Length;
                pBcryptBlob->cbSubgroupOrder = curve.Order.Length;
                pBcryptBlob->CurveGenerationAlgId = ECCng.GetHashAlgorithmId(curve.Hash);
                pBcryptBlob->CurveType = ECCng.ConvertToCurveTypeEnum(curve.CurveType);

                // Emit the blob
                int offset = sizeof(BCRYPT_ECC_PARAMETER_HEADER);
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

                // We better have computed the right allocation size above!
                Debug.Assert(offset == blobSize, "offset == blobSize");
            }

            return blob;
        }

        /// <summary>
        ///     This function checks the magic value in the key blob header
        /// </summary>
        /// <param name="magic">The expected magic number.</param>
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
        private static KeyBlobMagicNumber EcdsaCurveNameToMagicNumber(string? name, bool includePrivateParameters) =>
            EcdsaCurveNameToAlgorithm(name) switch
            {
                AlgorithmName.ECDsaP256 => includePrivateParameters ?
                       KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P256_MAGIC :
                       KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P256_MAGIC,

                AlgorithmName.ECDsaP384 => includePrivateParameters ?
                        KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P384_MAGIC :
                        KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P384_MAGIC,

                AlgorithmName.ECDsaP521 => includePrivateParameters ?
                        KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P521_MAGIC :
                        KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P521_MAGIC,

                _ => includePrivateParameters ?
                        KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC :
                        KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC,
            };

        /// <summary>
        /// Map a curve name to magic number. Maps the names of the curves that worked pre-Win10
        /// to the pre-Win10 magic numbers to support import on pre-Win10 environments
        /// that don't have the named curve functionality.
        /// </summary>
        private static KeyBlobMagicNumber EcdhCurveNameToMagicNumber(string? name, bool includePrivateParameters) =>
            EcdhCurveNameToAlgorithm(name) switch
            {
                AlgorithmName.ECDHP256 => includePrivateParameters ?
                       KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_P256_MAGIC :
                       KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_P256_MAGIC,

                AlgorithmName.ECDHP384 => includePrivateParameters ?
                        KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_P384_MAGIC :
                        KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_P384_MAGIC,

                AlgorithmName.ECDHP521 => includePrivateParameters ?
                        KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_P521_MAGIC :
                        KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_P521_MAGIC,

                _ => includePrivateParameters ?
                        KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC :
                        KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC,
            };

        /// <summary>
        /// Helper method to map between BCrypt.ECC_CURVE_TYPE_ENUM and ECCurve.ECCurveType
        /// </summary>
        private static Interop.BCrypt.ECC_CURVE_TYPE_ENUM ConvertToCurveTypeEnum(ECCurve.ECCurveType value)
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
        private static ECCurve.ECCurveType ConvertToCurveTypeEnum(Interop.BCrypt.ECC_CURVE_TYPE_ENUM value)
        {
            // Currently values 1-3 are interchangeable
            ECCurve.ECCurveType curveType = (ECCurve.ECCurveType)value;
            Debug.Assert(curveType == ECCurve.ECCurveType.Characteristic2 ||
                curveType == ECCurve.ECCurveType.PrimeShortWeierstrass ||
                curveType == ECCurve.ECCurveType.PrimeTwistedEdwards);
            return curveType;
        }

        internal static SafeNCryptKeyHandle ImportKeyBlob(
            string blobType,
            ReadOnlySpan<byte> keyBlob,
            string curveName,
            SafeNCryptProviderHandle provider)
        {
            ErrorCode errorCode;
            SafeNCryptKeyHandle keyHandle;

            using (SafeUnicodeStringHandle safeCurveName = new SafeUnicodeStringHandle(curveName))
            {
                Interop.BCrypt.BCryptBufferDesc desc = default;
                Interop.BCrypt.BCryptBuffer buff = default;

                IntPtr descPtr = IntPtr.Zero;
                IntPtr buffPtr = IntPtr.Zero;
                try
                {
                    descPtr = Marshal.AllocHGlobal(Marshal.SizeOf(desc));
                    buffPtr = Marshal.AllocHGlobal(Marshal.SizeOf(buff));
                    buff.cbBuffer = (curveName.Length + 1) * 2; // Add 1 for null terminator
                    buff.BufferType = Interop.BCrypt.CngBufferDescriptors.NCRYPTBUFFER_ECC_CURVE_NAME;
                    buff.pvBuffer = safeCurveName.DangerousGetHandle();
                    Marshal.StructureToPtr(buff, buffPtr, false);

                    desc.cBuffers = 1;
                    desc.pBuffers = buffPtr;
                    desc.ulVersion = Interop.BCrypt.BCRYPTBUFFER_VERSION;
                    Marshal.StructureToPtr(desc, descPtr, false);

                    errorCode = Interop.NCrypt.NCryptImportKey(
                        provider,
                        IntPtr.Zero,
                        blobType,
                        descPtr,
                        out keyHandle,
                        ref MemoryMarshal.GetReference(keyBlob),
                        keyBlob.Length,
                        0);
                }
                finally
                {
                    Marshal.FreeHGlobal(descPtr);
                    Marshal.FreeHGlobal(buffPtr);
                }
            }

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                Exception e = errorCode.ToCryptographicException();
                keyHandle.Dispose();
                if (errorCode == ErrorCode.NTE_INVALID_PARAMETER)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, curveName), e);
                }
                throw e;
            }

            return keyHandle;
        }

        /// <summary>
        /// Map a curve name to algorithm. This enables curves that worked pre-Win10
        /// to work with newer APIs for import and export.
        /// </summary>
        internal static string EcdsaCurveNameToAlgorithm(string? algorithm)
        {
            switch (algorithm)
            {
                case "nistP256":
                case "ECDSA_P256":
                    return AlgorithmName.ECDsaP256;

                case "nistP384":
                case "ECDSA_P384":
                    return AlgorithmName.ECDsaP384;

                case "nistP521":
                case "ECDSA_P521":
                    return AlgorithmName.ECDsaP521;
            }

            // All other curves are new in Win10 so use generic algorithm
            return AlgorithmName.ECDsa;
        }

        /// <summary>
        /// Map a curve name to algorithm. This enables curves that worked pre-Win10
        /// to work with newer APIs for import and export.
        /// </summary>
        internal static string EcdhCurveNameToAlgorithm(string? algorithm)
        {
            switch (algorithm)
            {
                case "nistP256":
                case "ECDH_P256":
                case "ECDSA_P256":
                    return AlgorithmName.ECDHP256;

                case "nistP384":
                case "ECDH_P384":
                case "ECDSA_P384":
                    return AlgorithmName.ECDHP384;

                case "nistP521":
                case "ECDH_P521":
                case "ECDSA_P521":
                    return AlgorithmName.ECDHP521;
            }

            // All other curves are new in Win10 so use generic algorithm
            return AlgorithmName.ECDH;
        }
    }
}
