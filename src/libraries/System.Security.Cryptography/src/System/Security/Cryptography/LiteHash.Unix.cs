// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    internal static partial class LiteHashProvider
    {
        private static LiteHash CreateHash(string hashAlgorithmId)
        {
            IntPtr algorithm = Interop.Crypto.HashAlgorithmToEvp(hashAlgorithmId);
            return new LiteHash(algorithm);
        }

        private static LiteHmac CreateHmac(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            IntPtr algorithm = Interop.Crypto.HashAlgorithmToEvp(hashAlgorithmId);
            return new LiteHmac(algorithm, key);
        }
    }

    internal readonly struct LiteHash : ILiteHash
    {
        private readonly SafeEvpMdCtxHandle _ctx;
        private readonly int _hashSizeInBytes;

        private const int Success = 1;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHash(IntPtr algorithm)
        {
            Debug.Assert(algorithm != IntPtr.Zero);
            _hashSizeInBytes = Interop.Crypto.EvpMdSize(algorithm);

            if (_hashSizeInBytes <= 0 || _hashSizeInBytes > Interop.Crypto.EVP_MAX_MD_SIZE)
            {
                Debug.Fail($"Unexpected hash '{_hashSizeInBytes}' size from {nameof(Interop.Crypto.EvpMdSize)}.");
                throw new CryptographicException();
            }

            _ctx = Interop.Crypto.EvpMdCtxCreate(algorithm);
            Interop.Crypto.CheckValidOpenSslHandle(_ctx);
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            int ret = Interop.Crypto.EvpDigestUpdate(_ctx, data, data.Length);

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        public int Finalize(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            uint length = (uint)destination.Length;
            int ret = Interop.Crypto.EvpDigestFinalEx(_ctx, ref MemoryMarshal.GetReference(destination), ref length);

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            Debug.Assert(length == _hashSizeInBytes);
            return _hashSizeInBytes;
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }
    }

    internal readonly struct LiteHmac : ILiteHash
    {
        private readonly SafeHmacCtxHandle _ctx;
        private readonly int _hashSizeInBytes;

        private const int Success = 1;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHmac(IntPtr algorithm, ReadOnlySpan<byte> key)
        {
            Debug.Assert(algorithm != IntPtr.Zero);
            _hashSizeInBytes = Interop.Crypto.EvpMdSize(algorithm);

            if (_hashSizeInBytes <= 0 || _hashSizeInBytes > Interop.Crypto.EVP_MAX_MD_SIZE)
            {
                Debug.Fail($"Unexpected hash '{_hashSizeInBytes}' size from {nameof(Interop.Crypto.EvpMdSize)}.");
                throw new CryptographicException();
            }

            _ctx = Interop.Crypto.HmacCreate(ref MemoryMarshal.GetReference(key), key.Length, algorithm);
            Interop.Crypto.CheckValidOpenSslHandle(_ctx);
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            int ret = Interop.Crypto.HmacUpdate(_ctx, data, data.Length);

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        public int Finalize(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            int length = destination.Length;
            int ret = Interop.Crypto.HmacFinal(_ctx, ref MemoryMarshal.GetReference(destination), ref length);

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            Debug.Assert(length == _hashSizeInBytes);
            return _hashSizeInBytes;
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }
    }
}
