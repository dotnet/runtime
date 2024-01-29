// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

using PAL_HashAlgorithm = Interop.AppleCrypto.PAL_HashAlgorithm;

namespace System.Security.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        public static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            return new AppleDigestProvider(hashAlgorithmId);
        }

        public static HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            return new AppleHmacProvider(hashAlgorithmId, key);
        }

        internal static bool HashSupported(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.MD5:
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool MacSupported(string hashAlgorithmId) => HashSupported(hashAlgorithmId);

        internal static bool KmacSupported(string algorithmId)
        {
            _ = algorithmId;
            return false;
        }

        internal static class OneShotHashProvider
        {
            public static int KmacData(
                string algorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination,
                ReadOnlySpan<byte> customizationString,
                bool xof)
            {
                _ = algorithmId;
                _ = key;
                _ = customizationString;
                _ = source;
                _ = destination;
                _ = xof;
                Debug.Fail("Platform should have checked if KMAC was available first.");
                throw new UnreachableException();
            }

            public static unsafe int MacData(
                string hashAlgorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination)
            {
                Interop.AppleCrypto.PAL_HashAlgorithm algorithm = HashAlgorithmNames.HashAlgorithmToPal(hashAlgorithmId);

                fixed (byte* pKey = key)
                fixed (byte* pSource = source)
                fixed (byte* pDestination = destination)
                {
                    int digestSize;
                    int ret = Interop.AppleCrypto.HmacOneShot(
                        algorithm,
                        pKey,
                        key.Length,
                        pSource,
                        source.Length,
                        pDestination,
                        destination.Length,
                        &digestSize);

                    if (ret != 1)
                    {
                        Debug.Fail($"MacData return value {ret} was not 1");
                        throw new CryptographicException();
                    }

                    Debug.Assert(digestSize <= destination.Length);

                    return digestSize;
                }
            }

            public static void HashDataXof(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                _ = hashAlgorithmId;
                _ = source;
                _ = destination;
                Debug.Fail("Caller should have checked if platform supported XOFs.");
                throw new UnreachableException();
            }

            public static unsafe int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                Interop.AppleCrypto.PAL_HashAlgorithm algorithm = HashAlgorithmNames.HashAlgorithmToPal(hashAlgorithmId);

                fixed (byte* pSource = source)
                fixed (byte* pDestination = destination)
                {
                    int digestSize;
                    int ret = Interop.AppleCrypto.DigestOneShot(
                        algorithm,
                        pSource,
                        source.Length,
                        pDestination,
                        destination.Length,
                        &digestSize);

                    if (ret != 1)
                    {
                        Debug.Fail($"HashData return value {ret} was not 1");
                        throw new CryptographicException();
                    }

                    Debug.Assert(digestSize <= destination.Length);

                    return digestSize;
                }
            }
        }

        private sealed class AppleDigestProvider : HashProvider
        {
            private readonly LiteHash _liteHash;
            private bool _running;

            public AppleDigestProvider(string hashAlgorithmId)
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
                // Apple's DigestFinal self-resets, so don't bother calling reset.
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

        private sealed class AppleHmacProvider : HashProvider
        {
            private readonly LiteHmac _liteHmac;
            private readonly byte[] _key;
            private bool _running;

            public AppleHmacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
            {
                PAL_HashAlgorithm algorithm = HashAlgorithmNames.HashAlgorithmToPal(hashAlgorithmId);
                _liteHmac = new LiteHmac(algorithm, key, preinitialize: false);
                _key = key.ToArray();
            }

            public override void AppendHashData(ReadOnlySpan<byte> data)
            {
                if (!_running)
                {
                    _liteHmac.Reset(_key);
                }

                _liteHmac.Append(data);
                _running = true;
            }

            public override int FinalizeHashAndReset(Span<byte> destination)
            {
                if (!_running)
                {
                    _liteHmac.Reset(_key);
                }

                int written = _liteHmac.Finalize(destination);
                _liteHmac.Reset(_key);
                _running = false;
                return written;
            }

            public override int GetCurrentHash(Span<byte> destination)
            {
                if (!_running)
                {
                    _liteHmac.Reset(_key);
                }

                return _liteHmac.Current(destination);
            }

            public override int HashSizeInBytes => _liteHmac.HashSizeInBytes;

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _liteHmac.Dispose();
                    Array.Clear(_key);
                }
            }

            public override void Reset()
            {
                _running = false;
            }
        }
    }
}
