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
    internal sealed partial class EccSecurityTransforms : IDisposable
    {
        private SecKeyPair? _keys;
        private bool _disposed;
        private readonly string _disposedName;

        internal EccSecurityTransforms(string disposedTypeName)
        {
            Debug.Assert(disposedTypeName != null);
            _disposedName = disposedTypeName;
        }

        internal void DisposeKey()
        {
            _keys?.Dispose();
            _keys = null;
        }

        public void Dispose()
        {
            DisposeKey();
            _disposed = true;
        }

        internal int GenerateKey(ECCurve curve)
        {
            curve.Validate();
            ThrowIfDisposed();

            if (!curve.IsNamed)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            int keySize;

            switch (curve.Oid.Value)
            {
                case Oids.secp256r1:
                    keySize = 256;
                    break;
                case Oids.secp384r1:
                    keySize = 384;
                    break;
                case Oids.secp521r1:
                    keySize = 521;
                    break;
                default:
                    throw new PlatformNotSupportedException(
                        SR.Format(SR.Cryptography_CurveNotSupported, curve.Oid.Value ?? curve.Oid.FriendlyName));
            }

            GenerateKey(keySize);
            return keySize;
        }

        private SecKeyPair GenerateKey(int keySizeInBits)
        {
            SafeSecKeyRefHandle publicKey;
            SafeSecKeyRefHandle privateKey;

            Interop.AppleCrypto.EccGenerateKey(keySizeInBits, out publicKey, out privateKey);

            SecKeyPair newPair = SecKeyPair.PublicPrivatePair(publicKey, privateKey);
            SetKey(newPair);
            return newPair;
        }

        internal void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(_disposedName);
            }
        }

        internal SecKeyPair GetOrGenerateKeys(int keySizeInBits)
        {
            ThrowIfDisposed();

            SecKeyPair? current = _keys;

            if (current != null)
            {
                return current;
            }

            return GenerateKey(keySizeInBits);
        }

        internal int SetKeyAndGetSize(SecKeyPair keyPair)
        {
            int size = GetKeySize(keyPair);
            SetKey(keyPair);
            return size;
        }

        private void SetKey(SecKeyPair keyPair)
        {
            ThrowIfDisposed();

            SecKeyPair? current = _keys;
            _keys = keyPair;
            current?.Dispose();
        }

        internal ECParameters ExportParameters(bool includePrivateParameters, int keySizeInBits)
        {
            SecKeyPair keys = GetOrGenerateKeys(keySizeInBits);
            ECParameters key = default;

            if (!TryExportDataKeyParameters(keys, includePrivateParameters, ref key))
            {
                return ExportParametersFromLegacyKey(keys, includePrivateParameters);
            }

            return key;
        }

        internal bool TryExportDataKeyParameters(
            bool includePrivateParameters,
            int keySizeInBits,
            ref ECParameters ecParameters)
        {
            return TryExportDataKeyParameters(
                GetOrGenerateKeys(keySizeInBits),
                includePrivateParameters,
                ref ecParameters);
        }

        private static bool TryExportDataKeyParameters(
            SecKeyPair keys,
            bool includePrivateParameters,
            ref ECParameters ecParameters)
        {
            if (includePrivateParameters && keys.PrivateKey == null)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            bool gotKeyBlob = Interop.AppleCrypto.TrySecKeyCopyExternalRepresentation(
                includePrivateParameters ? keys.PrivateKey! : keys.PublicKey,
                out byte[] keyBlob);

            if (!gotKeyBlob)
            {
                return false;
            }

            try
            {
                AsymmetricAlgorithmHelpers.DecodeFromUncompressedAnsiX963Key(
                    keyBlob,
                    includePrivateParameters,
                    out ecParameters);

                switch (GetKeySize(keys))
                {
                    case 256: ecParameters.Curve = ECCurve.NamedCurves.nistP256; break;
                    case 384: ecParameters.Curve = ECCurve.NamedCurves.nistP384; break;
                    case 521: ecParameters.Curve = ECCurve.NamedCurves.nistP521; break;
                    default:
                        Debug.Fail("Unsupported curve");
                        throw new CryptographicException();
                }

                return true;
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
                ExtractPublicKeyFromPrivateKey(ref parameters);
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

        private static int GetKeySize(SecKeyPair newKeys)
        {
            int size = Interop.AppleCrypto.EccGetKeySizeInBits(newKeys.PublicKey);
            Debug.Assert(size == 256 || size == 384 || size == 521, $"Unknown keysize ({size})");
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
