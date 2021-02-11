// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        internal ECParameters ExportParameters(bool includePrivateParameters, int keySizeInBits)
        {
            // Apple requires all private keys to be exported encrypted, but since we're trying to export
            // as parsed structures we will need to decrypt it for the user.
            const string ExportPassword = "DotnetExportPassphrase";
            SecKeyPair keys = GetOrGenerateKeys(keySizeInBits);

            if (includePrivateParameters && keys.PrivateKey == null)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(
                includePrivateParameters ? keys.PrivateKey : keys.PublicKey,
                exportPrivate: includePrivateParameters,
                password: ExportPassword);

            try
            {
                if (!includePrivateParameters)
                {
                    EccKeyFormatHelper.ReadSubjectPublicKeyInfo(
                        keyBlob,
                        out int localRead,
                        out ECParameters key);
                    return key;
                }
                else
                {
                    EccKeyFormatHelper.ReadEncryptedPkcs8(
                        keyBlob,
                        ExportPassword,
                        out int localRead,
                        out ECParameters key);
                    return key;
                }
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

            bool isPrivateKey = parameters.D != null;
            bool hasPublicParameters = parameters.Q.X != null && parameters.Q.Y != null;
            SecKeyPair newKeys;

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
            long size = Interop.AppleCrypto.EccGetKeySizeInBits(newKeys.PublicKey);
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
                    int size = GetKeySize(newKeys);
                    SetKey(newKeys);

                    bytesRead = localRead;
                    return size;
                }
            }
        }
    }
}
