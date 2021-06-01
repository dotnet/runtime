// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        internal static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            IntPtr evpType = Interop.Crypto.HashAlgorithmToEvp(hashAlgorithmId);
            return new EvpHashProvider(evpType);
        }

        internal static HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            IntPtr evpType = Interop.Crypto.HashAlgorithmToEvp(hashAlgorithmId);
            return new HmacHashProvider(evpType, key);
        }

        internal static class OneShotHashProvider
        {
            public static unsafe int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                IntPtr evpType = Interop.Crypto.HashAlgorithmToEvp(hashAlgorithmId);
                Debug.Assert(evpType != IntPtr.Zero);

                int hashSize = Interop.Crypto.EvpMdSize(evpType);

                if (hashSize <= 0 || destination.Length < hashSize)
                    throw new CryptographicException();

                fixed (byte* pSource = source)
                fixed (byte* pDestination = destination)
                {
                    uint length = (uint)destination.Length;
                    Check(Interop.Crypto.EvpDigestOneShot(evpType, pSource, source.Length, pDestination, ref length));
                    Debug.Assert(length == hashSize);
                }

                return hashSize;
            }
        }

        private sealed class EvpHashProvider : HashProvider
        {
            private readonly IntPtr _algorithmEvp;
            private readonly int _hashSize;
            private readonly SafeEvpMdCtxHandle _ctx;

            public EvpHashProvider(IntPtr algorithmEvp)
            {
                _algorithmEvp = algorithmEvp;
                Debug.Assert(algorithmEvp != IntPtr.Zero);

                _hashSize = Interop.Crypto.EvpMdSize(_algorithmEvp);
                if (_hashSize <= 0 || _hashSize > Interop.Crypto.EVP_MAX_MD_SIZE)
                {
                    throw new CryptographicException();
                }

                _ctx = Interop.Crypto.EvpMdCtxCreate(_algorithmEvp);

                Interop.Crypto.CheckValidOpenSslHandle(_ctx);
            }

            public override void AppendHashData(ReadOnlySpan<byte> data) =>
                Check(Interop.Crypto.EvpDigestUpdate(_ctx, data, data.Length));

            public override int FinalizeHashAndReset(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= _hashSize);

                uint length = (uint)destination.Length;
                Check(Interop.Crypto.EvpDigestFinalEx(_ctx, ref MemoryMarshal.GetReference(destination), ref length));
                Debug.Assert(length == _hashSize);

                // Reset the algorithm provider.
                Check(Interop.Crypto.EvpDigestReset(_ctx, _algorithmEvp));

                return _hashSize;
            }

            public override int GetCurrentHash(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= _hashSize);

                uint length = (uint)destination.Length;
                Check(Interop.Crypto.EvpDigestCurrent(_ctx, ref MemoryMarshal.GetReference(destination), ref length));
                Debug.Assert(length == _hashSize);

                return _hashSize;
            }

            public override int HashSizeInBytes => _hashSize;

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _ctx.Dispose();
                }
            }
        }

        private sealed class HmacHashProvider : HashProvider
        {
            private readonly int _hashSize;
            private SafeHmacCtxHandle _hmacCtx;

            public HmacHashProvider(IntPtr algorithmEvp, ReadOnlySpan<byte> key)
            {
                Debug.Assert(algorithmEvp != IntPtr.Zero);

                _hashSize = Interop.Crypto.EvpMdSize(algorithmEvp);
                if (_hashSize <= 0 || _hashSize > Interop.Crypto.EVP_MAX_MD_SIZE)
                {
                    throw new CryptographicException();
                }

                _hmacCtx = Interop.Crypto.HmacCreate(ref MemoryMarshal.GetReference(key), key.Length, algorithmEvp);
                Interop.Crypto.CheckValidOpenSslHandle(_hmacCtx);
            }

            public override void AppendHashData(ReadOnlySpan<byte> data) =>
                Check(Interop.Crypto.HmacUpdate(_hmacCtx, data, data.Length));

            public override int FinalizeHashAndReset(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= _hashSize);

                int length = destination.Length;
                Check(Interop.Crypto.HmacFinal(_hmacCtx, ref MemoryMarshal.GetReference(destination), ref length));
                Debug.Assert(length == _hashSize);

                Check(Interop.Crypto.HmacReset(_hmacCtx));
                return _hashSize;
            }

            public override int GetCurrentHash(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= _hashSize);

                int length = destination.Length;
                Check(Interop.Crypto.HmacCurrent(_hmacCtx, ref MemoryMarshal.GetReference(destination), ref length));
                Debug.Assert(length == _hashSize);

                return _hashSize;
            }

            public override int HashSizeInBytes => _hashSize;

            public override void Dispose(bool disposing)
            {
                if (disposing && _hmacCtx != null)
                {
                    _hmacCtx.Dispose();
                    _hmacCtx = null!;
                }
            }
        }

        private static void Check(int result)
        {
            const int Success = 1;
            if (result != Success)
            {
                Debug.Assert(result == 0);
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }
    }
}
