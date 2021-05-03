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

        private static int GetKeySize(SecKeyPair newKeys)
        {
            long size = Interop.AppleCrypto.EccGetKeySizeInBits(newKeys.PublicKey);
            Debug.Assert(size == 256 || size == 384 || size == 521, $"Unknown keysize ({size})");
            return (int)size;
        }
    }
}
