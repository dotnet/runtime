// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public partial class DSA : AsymmetricAlgorithm
    {
        private static DSA CreateCore()
        {
            return new DSAImplementation.DSACng();
        }
    }

    internal static partial class DSAImplementation
    {
        public sealed partial class DSACng : DSA
        {
            private SafeNCryptKeyHandle? _keyHandle;
            private int _lastKeySize;
            private bool _disposed;

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(RSA));
                }
            }

            private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
            {
                ThrowIfDisposed();

                int keySize = KeySize;

                if (_lastKeySize != keySize)
                {
                    if (_keyHandle != null)
                    {
                        _keyHandle.Dispose();
                    }

                    const string BCRYPT_DSA_ALGORITHM = "DSA";

                    _keyHandle = CngKeyLite.GenerateNewExportableKey(BCRYPT_DSA_ALGORITHM, keySize);
                    _lastKeySize = keySize;
                }

                return new DuplicateSafeNCryptKeyHandle(_keyHandle!);
            }

            private byte[] ExportKeyBlob(bool includePrivateParameters)
            {
                // Use generic blob type for multiple version support
                string blobType = includePrivateParameters ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_PRIVATE_KEY_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_PUBLIC_KEY_BLOB;

                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    return CngKeyLite.ExportKeyBlob(keyHandle, blobType);
                }
            }

            private byte[] ExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount)
            {
                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    return CngKeyLite.ExportPkcs8KeyBlob(keyHandle, pkcs8Password, kdfCount);
                }
            }

            private bool TryExportEncryptedPkcs8(
                ReadOnlySpan<char> pkcs8Password,
                int kdfCount,
                Span<byte> destination,
                out int bytesWritten)
            {
                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    return CngKeyLite.TryExportPkcs8KeyBlob(
                        keyHandle,
                        pkcs8Password,
                        kdfCount,
                        destination,
                        out bytesWritten);
                }
            }

            private void ImportKeyBlob(byte[] dsaBlob, bool includePrivate)
            {
                ThrowIfDisposed();

                // Use generic blob type for multiple version support
                string blobType = includePrivate ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_PRIVATE_KEY_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_PUBLIC_KEY_BLOB;

                SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, dsaBlob);
                SetKeyHandle(keyHandle);
            }

            private void AcceptImport(CngPkcs8.Pkcs8Response response)
            {
                ThrowIfDisposed();

                SafeNCryptKeyHandle keyHandle = response.KeyHandle;
                SetKeyHandle(keyHandle);
            }

            private void SetKeyHandle(SafeNCryptKeyHandle keyHandle)
            {
                Debug.Assert(!keyHandle.IsInvalid);

                _keyHandle = keyHandle;

                int newKeySize = CngKeyLite.GetKeyLength(keyHandle);

                // Our LegalKeySizes value stores the values that we encoded as being the correct
                // legal key size limitations for this algorithm, as documented on MSDN.
                //
                // But on a new OS version we might not question if our limit is accurate, or MSDN
                // could have been inaccurate to start with.
                //
                // Since the key is already loaded, we know that Windows thought it to be valid;
                // therefore we should set KeySizeValue directly to bypass the LegalKeySizes conformance
                // check.
                ForceSetKeySize(newKeySize);
                _lastKeySize = newKeySize;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _keyHandle?.Dispose();
                    _keyHandle = null;
                    _disposed = true;
                }


                base.Dispose(disposing);
            }
        }
    }
}
