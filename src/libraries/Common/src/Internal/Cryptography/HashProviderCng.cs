// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using NTSTATUS = Interop.BCrypt.NTSTATUS;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using BCryptCreateHashFlags = Interop.BCrypt.BCryptCreateHashFlags;

namespace Internal.Cryptography
{
    //
    // Provides hash services via the native provider (CNG). 
    //
    internal sealed class HashProviderCng : HashProvider
    {
        //
        //   - "hashAlgId" must be a name recognized by BCryptOpenAlgorithmProvider(). Examples: MD5, SHA1, SHA256.
        //
        //   - "key" activates MAC hashing if present. If null, this HashProvider performs a regular old hash.
        //
        public HashProviderCng(string hashAlgId, byte[] key)
        {
            BCryptOpenAlgorithmProviderFlags dwFlags = BCryptOpenAlgorithmProviderFlags.None;
            if (key != null)
            {
                _key = key.CloneByteArray();
                dwFlags |= BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;
            }

            _hAlgorithm = Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgId, dwFlags);

            // Win7 won't set hHash, Win8+ will; and both will set _hHash.
            // So keep hHash trapped in this scope to prevent (mis-)use of it.
            {
                SafeBCryptHashHandle hHash = null;
                NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(_hAlgorithm, out hHash, IntPtr.Zero, 0, key, key == null ? 0 : key.Length, BCryptCreateHashFlags.BCRYPT_HASH_REUSABLE_FLAG);
                if (ntStatus == NTSTATUS.STATUS_INVALID_PARAMETER)
                {
                    // If we got here, we're running on a downlevel OS (pre-Win8) that doesn't support reusable CNG hash objects. Fall back to creating a 
                    // new HASH object each time.
                    ResetHashObject();
                }
                else if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }
                else
                {
                    _hHash = hHash;
                    _reusable = true;
                }
            }

            unsafe
            {
                int cbSizeOfHashSize;
                int hashSize;
                NTSTATUS ntStatus = Interop.BCrypt.BCryptGetProperty(_hHash, Interop.BCrypt.BCryptPropertyStrings.BCRYPT_HASH_LENGTH, &hashSize, sizeof(int), out cbSizeOfHashSize, 0);
                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                _hashSize = hashSize;
            }
            return;
        }

        public sealed override unsafe void AppendHashData(ReadOnlySpan<byte> source)
        {
            NTSTATUS ntStatus = Interop.BCrypt.BCryptHashData(_hHash, source, source.Length, 0);
            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }
        }

        public sealed override byte[] FinalizeHashAndReset()
        {
            var hash = new byte[_hashSize];
            bool success = TryFinalizeHashAndReset(hash, out int bytesWritten);
            Debug.Assert(success);
            Debug.Assert(hash.Length == bytesWritten);
            return hash;
        }

        public override bool TryFinalizeHashAndReset(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < _hashSize)
            {
                bytesWritten = 0;
                return false;
            }

            NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(_hHash, destination, _hashSize, 0);
            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            bytesWritten = _hashSize;
            ResetHashObject();
            return true;
        }

        public sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DestroyHash();
                if (_key != null)
                {
                    byte[] key = _key;
                    _key = null;
                    Array.Clear(key, 0, key.Length);
                }
            }
        }

        public sealed override int HashSizeInBytes => _hashSize;

        private void ResetHashObject()
        {
            if (_reusable)
                return;
            DestroyHash();

            SafeBCryptHashHandle hHash;
            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(_hAlgorithm, out hHash, IntPtr.Zero, 0, _key, _key == null ? 0 : _key.Length, BCryptCreateHashFlags.None);
            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);

            _hHash = hHash;
        }

        private void DestroyHash()
        {
            SafeBCryptHashHandle hHash = _hHash;
            _hHash = null;
            if (hHash != null)
            {
                hHash.Dispose();
            }

            // Not disposing of _hAlgorithm as we got this from a cache. So it's not ours to Dispose().
        }

        private readonly SafeBCryptAlgorithmHandle _hAlgorithm;
        private SafeBCryptHashHandle _hHash;
        private byte[] _key;
        private readonly bool _reusable;

        private readonly int _hashSize;
    }
}


