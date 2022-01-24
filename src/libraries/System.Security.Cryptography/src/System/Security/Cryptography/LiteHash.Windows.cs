// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;

using BCryptCreateHashFlags = Interop.BCrypt.BCryptCreateHashFlags;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal static partial class LiteHashProvider
    {
        private static LiteHash CreateHash(string hashAlgorithmId)
        {
            return new LiteHash(hashAlgorithmId);
        }

        private static LiteHmac CreateHmac(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            return new LiteHmac(hashAlgorithmId, key);
        }
    }

    internal readonly struct LiteHash : ILiteHash
    {
        private readonly SafeBCryptHashHandle _hashHandle;
        private readonly int _hashSizeInBytes;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHash(string algorithm)
        {

            BCryptOpenAlgorithmProviderFlags algorithmFlags =
                BCryptOpenAlgorithmProviderFlags.None;

            // This is a shared handle, do not put this in a using.
            SafeBCryptAlgorithmHandle algorithmHandle = Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(
                algorithm,
                algorithmFlags,
                out _hashSizeInBytes);

            SafeBCryptHashHandle hashHandle;

            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                algorithmHandle,
                out hashHandle,
                pbHashObject: IntPtr.Zero,
                cbHashObject: 0,
                secret: ReadOnlySpan<byte>.Empty,
                cbSecret: 0,
                BCryptCreateHashFlags.None);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                hashHandle.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            _hashHandle = hashHandle;
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            NTSTATUS ntStatus = Interop.BCrypt.BCryptHashData(_hashHandle, data, data.Length, dwFlags: 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }
        }

        public int Finalize(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(_hashHandle, destination, _hashSizeInBytes, dwFlags: 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            return _hashSizeInBytes;
        }

        public void Dispose()
        {
            _hashHandle.Dispose();
        }
    }

    internal readonly struct LiteHmac : ILiteHash
    {
        private readonly SafeBCryptHashHandle _hashHandle;
        private readonly int _hashSizeInBytes;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHmac(string algorithm, ReadOnlySpan<byte> key)
        {
            BCryptOpenAlgorithmProviderFlags algorithmFlags =
                BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;

            // This is a shared handle, do not put this in a using.
            SafeBCryptAlgorithmHandle algorithmHandle = Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(
                algorithm,
                algorithmFlags,
                out _hashSizeInBytes);

            SafeBCryptHashHandle hashHandle;

            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                algorithmHandle,
                out hashHandle,
                pbHashObject: IntPtr.Zero,
                cbHashObject: 0,
                key,
                key.Length,
                BCryptCreateHashFlags.None);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                hashHandle.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            _hashHandle = hashHandle;
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            NTSTATUS ntStatus = Interop.BCrypt.BCryptHashData(_hashHandle, data, data.Length, dwFlags: 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }
        }

        public int Finalize(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(_hashHandle, destination, _hashSizeInBytes, dwFlags: 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            return _hashSizeInBytes;
        }

        public void Dispose()
        {
            _hashHandle.Dispose();
        }
    }
}
