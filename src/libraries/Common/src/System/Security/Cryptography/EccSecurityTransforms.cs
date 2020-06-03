// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Internal.Cryptography;
using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;

namespace System.Security.Cryptography
{
    internal sealed class EccSecurityTransforms : IDisposable
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

            CreateDataKey(
                privateKey,
                keySizeInBits,
                isPrivate: true,
                out SafeSecKeyRefHandle publicDataKeyHandle,
                out SafeSecKeyRefHandle? privateDataKeyHandle);
            Debug.Assert(privateDataKeyHandle != null);

            SecKeyPair newPair = SecKeyPair.PublicPrivatePair(publicKey, privateKey, publicDataKeyHandle, privateDataKeyHandle);

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
            int size = GetKeySize(keyPair.PublicKey);
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

        internal static ECParameters ExportPublicParametersFromPrivateKey(SafeSecKeyRefHandle handle)
        {
            const string ExportPassword = "DotnetExportPassphrase";
            byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(handle, exportPrivate: true, password: ExportPassword);
            EccKeyFormatHelper.ReadEncryptedPkcs8(keyBlob, ExportPassword, out _, out ECParameters key);
            CryptographicOperations.ZeroMemory(key.D);
            CryptographicOperations.ZeroMemory(keyBlob);
            key.D = null;
            return key;
        }

        private static ECParameters ExportParameters(SafeSecKeyRefHandle keyHandle, bool includePrivateParameters, int keySizeInBits)
        {
            // Apple requires all private keys to be exported encrypted, but since we're trying to export
            // as parsed structures we will need to decrypt it for the user.
            const string ExportPassword = "DotnetExportPassphrase";

            byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(
                keyHandle,
                exportPrivate: includePrivateParameters,
                password: ExportPassword);

            try
            {
                if (!includePrivateParameters)
                {
                    EccKeyFormatHelper.ReadSubjectPublicKeyInfo(
                        keyBlob,
                        out _,
                        out ECParameters key);
                    return key;
                }
                else
                {
                    EccKeyFormatHelper.ReadEncryptedPkcs8(
                        keyBlob,
                        ExportPassword,
                        out _,
                        out ECParameters key);
                    return key;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBlob);
            }
        }

        internal ECParameters ExportParameters(bool includePrivateParameters, int keySizeInBits)
        {
            SecKeyPair keys = GetOrGenerateKeys(keySizeInBits);

            if (includePrivateParameters && keys.PrivateKey == null)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            return ExportParameters(includePrivateParameters ? keys.PrivateKey! : keys.PublicKey, includePrivateParameters, keySizeInBits);
        }

        internal int ImportParameters(ECParameters parameters)
        {
            parameters.Validate();
            ThrowIfDisposed();

            bool isPrivateKey = parameters.D != null;
            bool hasPublicParameters = parameters.Q.X != null && parameters.Q.Y != null;
            SecKeyPair newKeys;
            int keySizeInBits;

            if (isPrivateKey)
            {
                // Start with the private key, in case some of the private key fields don't
                // match the public key fields and the system determines an integrity failure.
                //
                // Public import should go off without a hitch.
                SafeSecKeyRefHandle privateKey = ImportKey(parameters);

                ECParameters publicOnly;

                if (hasPublicParameters)
                {
                    publicOnly = parameters;
                    publicOnly.D = null;
                }
                else
                {
                    publicOnly = ExportPublicParametersFromPrivateKey(privateKey);
                }

                SafeSecKeyRefHandle publicKey;
                try
                {
                    publicKey = ImportKey(publicOnly);
                }
                catch
                {
                    privateKey.Dispose();
                    throw;
                }

                keySizeInBits = GetKeySize(publicKey);

                CreateDataKey(
                    privateKey,
                    keySizeInBits,
                    isPrivate: true,
                    out SafeSecKeyRefHandle publicDataKeyHandle,
                    out SafeSecKeyRefHandle? privateDataKeyHandle);

                Debug.Assert(privateDataKeyHandle != null);
                newKeys = SecKeyPair.PublicPrivatePair(publicKey, privateKey, publicDataKeyHandle, privateDataKeyHandle);
            }
            else
            {
                SafeSecKeyRefHandle publicKey = ImportKey(parameters);
                keySizeInBits = GetKeySize(publicKey);
                CreateDataKey(publicKey, keySizeInBits, isPrivate: false, out SafeSecKeyRefHandle publicDataKeyHandle, out _);
                newKeys = SecKeyPair.PublicOnly(publicKey, publicDataKeyHandle);
            }

            SetKey(newKeys);

            return keySizeInBits;
        }

        private static int GetKeySize(SafeSecKeyRefHandle publicKey)
        {
            long size = Interop.AppleCrypto.EccGetKeySizeInBits(publicKey);
            Debug.Assert(size == 256 || size == 384 || size == 521, $"Unknown keysize ({size})");
            return (int)size;
        }

        private static SafeSecKeyRefHandle ImportKey(ECParameters parameters)
        {
            AsnWriter keyWriter;
            bool hasPrivateKey;

            if (parameters.D != null)
            {
                keyWriter = EccKeyFormatHelper.WriteECPrivateKey(parameters);
                hasPrivateKey = true;
            }
            else
            {
                keyWriter = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(parameters);
                hasPrivateKey = false;
            }

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
                return Interop.AppleCrypto.ImportEphemeralKey(rented.AsSpan(0, written), hasPrivateKey);
            }
            finally
            {
                CryptoPool.Return(rented, written);
            }
        }

        internal unsafe int ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            ThrowIfDisposed();

            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    // Validate the DER value and get the number of bytes.
                    EccKeyFormatHelper.ReadSubjectPublicKeyInfo(
                        manager.Memory,
                        out int localRead);

                    SafeSecKeyRefHandle publicKey = Interop.AppleCrypto.ImportEphemeralKey(source.Slice(0, localRead), false);
                    SecKeyPair newKeys = SecKeyPair.PublicOnly(publicKey);
                    int size = GetKeySize(newKeys.PublicKey);
                    SetKey(newKeys);

                    bytesRead = localRead;
                    return size;
                }
            }
        }

        private static void CreateDataKey(
            SafeSecKeyRefHandle keyHandle,
            int keySizeInBits,
            bool isPrivate,
            out SafeSecKeyRefHandle publicKeyHandle,
            out SafeSecKeyRefHandle? privateKeyHandle)
        {
            ECParameters ecParameters = ExportParameters(keyHandle, isPrivate, keySizeInBits);
            int fieldSize = AsymmetricAlgorithmHelpers.BitsToBytes(keySizeInBits);

            Debug.Assert(ecParameters.Q.Y != null && ecParameters.Q.Y.Length == fieldSize);
            Debug.Assert(ecParameters.Q.X != null && ecParameters.Q.X.Length == fieldSize);

            int keySize = 1 + fieldSize * (isPrivate ? 3 : 2);
            byte[] dataKeyPool = CryptoPool.Rent(keySize);
            Span<byte> dataKey = dataKeyPool;
            Range? privateKey = null;
            Range publicKey;

            try
            {
                (publicKey, privateKey) = AsymmetricAlgorithmHelpers.EncodeToUncompressedAnsiX963Key(
                    ecParameters.Q.X,
                    ecParameters.Q.Y,
                    isPrivate ? ecParameters.D : default,
                    dataKey);

                publicKeyHandle = Interop.AppleCrypto.CreateDataKey(
                    dataKey[publicKey],
                    keySizeInBits,
                    Interop.AppleCrypto.PAL_KeyAlgorithm.EC,
                    isPublic: true);

                Debug.Assert(privateKey.HasValue == isPrivate, "privateKey.HasValue == isPrivate");

                if (privateKey.HasValue)
                {
                    privateKeyHandle = Interop.AppleCrypto.CreateDataKey(
                        dataKey[privateKey.Value],
                        keySizeInBits,
                        Interop.AppleCrypto.PAL_KeyAlgorithm.EC,
                        isPublic: false);
                }
                else
                {
                    privateKeyHandle = null;
                }
            }
            finally
            {
                if (privateKey.HasValue)
                {
                    CryptographicOperations.ZeroMemory(dataKey[privateKey.Value]);
                }

                CryptographicOperations.ZeroMemory(ecParameters.D);

                // We manually cleared out the private key bytes above if the
                // key was private, we don't need to clear the buffer again
                CryptoPool.Return(dataKeyPool, clearSize: 0);
            }
        }
    }
}
