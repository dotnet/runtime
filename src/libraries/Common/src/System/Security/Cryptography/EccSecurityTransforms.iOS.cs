// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class EccSecurityTransforms
    {
        internal ECParameters ExportParameters(bool includePrivateParameters, int keySizeInBits)
        {
            SecKeyPair keys = GetOrGenerateKeys(keySizeInBits);

            if (includePrivateParameters && keys.PrivateKey == null)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            byte[] keyBlob = Interop.AppleCrypto.SecKeyCopyExternalRepresentation(
                includePrivateParameters ? keys.PrivateKey! : keys.PublicKey);

            try
            {
                AsymmetricAlgorithmHelpers.DecodeFromUncompressedAnsiX963Key(
                    keyBlob,
                    includePrivateParameters,
                    out ECParameters key);

                switch (GetKeySize(keys))
                {
                    case 256: key.Curve = ECCurve.NamedCurves.nistP256; break;
                    case 384: key.Curve = ECCurve.NamedCurves.nistP384; break;
                    case 521: key.Curve = ECCurve.NamedCurves.nistP521; break;
                }

                return key;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBlob);
            }
        }

        internal int ImportParameters(ECParameters parameters)
        {
            parameters.Validate();
            ThrowIfDisposed();

            if (!parameters.Curve.IsNamed)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            switch (parameters.Curve.Oid.Value)
            {
                case Oids.secp256r1:
                case Oids.secp384r1:
                case Oids.secp521r1:
                    break;
                default:
                    throw new PlatformNotSupportedException(
                        SR.Format(SR.Cryptography_CurveNotSupported, parameters.Curve.Oid.Value ?? parameters.Curve.Oid.FriendlyName));
            }

            if (parameters.Q.X == null || parameters.Q.Y == null)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            bool isPrivateKey = parameters.D != null;
            SecKeyPair newKeys;

            if (isPrivateKey)
            {
                // Start with the private key, in case some of the private key fields don't
                // match the public key fields and the system determines an integrity failure.
                //
                // Public import should go off without a hitch.
                SafeSecKeyRefHandle privateKey = ImportKey(parameters);
                SafeSecKeyRefHandle publicKey = Interop.AppleCrypto.CopyPublicKey(privateKey);
                newKeys = SecKeyPair.PublicPrivatePair(publicKey, privateKey);
            }
            else
            {
                SafeSecKeyRefHandle publicKey = ImportKey(parameters);
                newKeys = SecKeyPair.PublicOnly(publicKey);
            }

            int size = GetKeySize(newKeys);
            SetKey(newKeys);

            return size;
        }

        private static SafeSecKeyRefHandle ImportKey(ECParameters parameters)
        {
            int fieldSize = parameters.Q!.X!.Length;

            Debug.Assert(parameters.Q.Y != null && parameters.Q.Y.Length == fieldSize);
            Debug.Assert(parameters.Q.X != null && parameters.Q.X.Length == fieldSize);

            int keySize = 1 + fieldSize * (parameters.D != null ? 3 : 2);
            byte[] dataKeyPool = CryptoPool.Rent(keySize);
            Span<byte> dataKey = dataKeyPool.AsSpan(0, keySize);

            try
            {
                AsymmetricAlgorithmHelpers.EncodeToUncompressedAnsiX963Key(
                    parameters.Q.X,
                    parameters.Q.Y,
                    parameters.D,
                    dataKey);

                return Interop.AppleCrypto.CreateDataKey(
                    dataKey,
                    Interop.AppleCrypto.PAL_KeyAlgorithm.EC,
                    isPublic: parameters.D == null);
            }
            finally
            {
                CryptoPool.Return(dataKeyPool, keySize);
            }
        }
    }
}
