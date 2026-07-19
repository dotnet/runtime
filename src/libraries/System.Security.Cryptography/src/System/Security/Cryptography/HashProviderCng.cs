// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;
using BCryptCreateHashFlags = Interop.BCrypt.BCryptCreateHashFlags;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
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
        public HashProviderCng(string hashAlgId, byte[]? key) : this(hashAlgId, key, isHmac: key != null)
        {
        }

        internal HashProviderCng(string hashAlgId, ReadOnlySpan<byte> key, bool isHmac)
        {
            BCryptOpenAlgorithmProviderFlags dwFlags = BCryptOpenAlgorithmProviderFlags.None;
            if (isHmac)
            {
                _key = key.ToArray();
                dwFlags |= BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;
            }

            _hAlgorithm = Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgId, dwFlags, out _hashSize);
            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(_hAlgorithm, out _hHash, IntPtr.Zero, 0, key, key.Length, BCryptCreateHashFlags.BCRYPT_HASH_REUSABLE_FLAG);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                _hHash.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }
        }

        private HashProviderCng(
            SafeBCryptAlgorithmHandle algorithmHandle,
            SafeBCryptHashHandle hashHandle,
            byte[]? key,
            int hashSize,
            bool running)
        {
            _hAlgorithm = algorithmHandle;
            _hHash = hashHandle;
            _key = key.CloneByteArray();
            _hashSize = hashSize;
            _running = running;
        }

        public sealed override void AppendHashData(ReadOnlySpan<byte> source)
        {
            Debug.Assert(_hHash != null);

            using (ConcurrencyBlock.Enter(ref _block))
            {
                NTSTATUS ntStatus = Interop.BCrypt.BCryptHashData(_hHash, source, source.Length, 0);
                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }

                _running = true;
            }
        }

        public override int FinalizeHashAndReset(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSize);
            Debug.Assert(_hHash != null);

            using (ConcurrencyBlock.Enter(ref _block))
            {
                NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(_hHash, destination, _hashSize, 0);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }

                _running = false;
                Reset();
                return _hashSize;
            }
        }

        public override int GetCurrentHash(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSize);
            Debug.Assert(_hHash != null);

            using (ConcurrencyBlock.Enter(ref _block))
            {
                using (SafeBCryptHashHandle tmpHash = Interop.BCrypt.BCryptDuplicateHash(_hHash))
                {
                    NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(tmpHash, destination, _hashSize, 0);

                    if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                    }

                    return _hashSize;
                }
            }
        }

        public override HashProviderCng Clone()
        {
            using (ConcurrencyBlock.Enter(ref _block))
            {
                SafeBCryptHashHandle clone = Interop.BCrypt.BCryptDuplicateHash(_hHash);
                return new HashProviderCng(_hAlgorithm, clone, _key, _hashSize, _running);
            }
        }

        public sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Not disposing of _hAlgorithm as we got this from a cache. So it's not ours to Dispose().
                _hHash.Dispose();

                if (_key != null)
                {
                    byte[] key = _key;
                    _key = null;
                    Array.Clear(key);
                }
            }
        }

        public sealed override int HashSizeInBytes => _hashSize;

        public override void Reset()
        {
            // Reset does not need to use ConcurrencyBlock. It either no-ops, or creates an entirely new handle, exchanges
            // them, and disposes of the old handle. We don't need to block concurrency on the Dispose because SafeHandle
            // does that.
            if (!_running)
            {
                return;
            }

            const BCryptCreateHashFlags Flags = BCryptCreateHashFlags.BCRYPT_HASH_REUSABLE_FLAG;

            SafeBCryptHashHandle hHash;
            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(_hAlgorithm, out hHash, IntPtr.Zero, 0, _key, _key == null ? 0 : _key.Length, Flags);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                hHash.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            SafeBCryptHashHandle? previousHash = Interlocked.Exchange(ref _hHash, hHash);
            previousHash?.Dispose();
        }

        private readonly SafeBCryptAlgorithmHandle _hAlgorithm;
        private SafeBCryptHashHandle _hHash;
        private byte[]? _key;

        private readonly int _hashSize;
        private bool _running;
        private ConcurrencyBlock _block;
    }
}
