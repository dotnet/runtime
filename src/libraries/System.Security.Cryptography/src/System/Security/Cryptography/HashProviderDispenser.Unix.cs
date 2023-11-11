// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        internal static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            return new EvpHashProvider(hashAlgorithmId);
        }

        internal static HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            return new HmacHashProvider(hashAlgorithmId, key);
        }

        internal static bool HashSupported(string hashAlgorithmId)
        {
            return Interop.Crypto.HashAlgorithmSupported(hashAlgorithmId);
        }

        internal static bool MacSupported(string hashAlgorithmId) => HashSupported(hashAlgorithmId);

        internal static partial class OneShotHashProvider
        {
            public static int MacData(
                string hashAlgorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination)
            {
                IntPtr evpType = Interop.Crypto.HashAlgorithmToEvp(hashAlgorithmId);
                Debug.Assert(evpType != IntPtr.Zero);

                int hashSize = Interop.Crypto.EvpMdSize(evpType);

                if (hashSize <= 0 || destination.Length < hashSize)
                {
                    Debug.Fail("Destination length or hash size not valid.");
                    throw new CryptographicException();
                }

                int written = Interop.Crypto.HmacOneShot(evpType, key, source, destination);
                Debug.Assert(written == hashSize);
                return written;
            }

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
                    const int Success = 1;
                    uint length = (uint)destination.Length;
                    int ret = Interop.Crypto.EvpDigestOneShot(evpType, pSource, source.Length, pDestination, &length);

                    if (ret != Success)
                    {
                        Debug.Assert(ret == 0);
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    Debug.Assert(length == hashSize);
                }

                return hashSize;
            }
        }

        private sealed class EvpHashProvider : HashProvider
        {
            private readonly LiteHash _liteHash;
            private bool _running;

            public EvpHashProvider(string hashAlgorithmId)
            {
                _liteHash = LiteHashProvider.CreateHash(hashAlgorithmId);
            }

            public override void AppendHashData(ReadOnlySpan<byte> data)
            {
                _liteHash.Append(data);
                _running = true;
            }

            public override int FinalizeHashAndReset(Span<byte> destination)
            {
                int written = _liteHash.Finalize(destination);
                _liteHash.Reset();
                _running = false;
                return written;
            }

            public override int GetCurrentHash(Span<byte> destination)
            {
                return _liteHash.Current(destination);
            }

            public override int HashSizeInBytes => _liteHash.HashSizeInBytes;

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _liteHash.Dispose();
                }
            }

            public override void Reset()
            {
                if (_running)
                {
                    _liteHash.Reset();
                    _running = false;
                }
            }
        }

        private sealed class HmacHashProvider : HashProvider
        {
            private readonly LiteHmac _liteHmac;
            private bool _running;

            public HmacHashProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
            {
                _liteHmac = LiteHashProvider.CreateHmac(hashAlgorithmId, key);
            }

            public override void AppendHashData(ReadOnlySpan<byte> data)
            {
                _liteHmac.Append(data);
                _running = true;
            }

            public override int FinalizeHashAndReset(Span<byte> destination)
            {
                int written = _liteHmac.Finalize(destination);
                _liteHmac.Reset();
                _running = false;
                return written;
            }

            public override int GetCurrentHash(Span<byte> destination)
            {
                return _liteHmac.Current(destination);
            }

            public override int HashSizeInBytes => _liteHmac.HashSizeInBytes;

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _liteHmac.Dispose();
                }
            }

            public override void Reset()
            {
                if (_running)
                {
                    _liteHmac.Reset();
                    _running = false;
                }
            }
        }
    }
}
