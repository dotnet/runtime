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

        internal static LiteXof CreateXof(string hashAlgorithmId)
        {
            _ = hashAlgorithmId;
            throw new PlatformNotSupportedException();
        }
    }

    internal struct LiteXof : ILiteHash
    {
        // Nothing uses this for Browser but we need the type.
#pragma warning disable CA1822 // Member does not access instance data
#pragma warning disable IDE0060 // Remove unused parameter
        public int HashSizeInBytes => throw new PlatformNotSupportedException();
        public void Append(ReadOnlySpan<byte> data) =>  throw new PlatformNotSupportedException();
        public int Finalize(Span<byte> destination) =>  throw new PlatformNotSupportedException();
        public void Current(Span<byte> destination) =>  throw new PlatformNotSupportedException();
        public int Reset() =>  throw new PlatformNotSupportedException();
        public void Dispose() =>  throw new PlatformNotSupportedException();
#pragma warning restore IDE0060
#pragma warning restore CA1822
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
