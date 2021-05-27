// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;

namespace Internal.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        public static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            Interop.AppleCrypto.PAL_HashAlgorithm algorithm = HashAlgorithmToPal(hashAlgorithmId);
            return new AppleDigestProvider(algorithm);
        }

        public static HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            Interop.AppleCrypto.PAL_HashAlgorithm algorithm = HashAlgorithmToPal(hashAlgorithmId);
            return new AppleHmacProvider(algorithm, key);
        }

        private static Interop.AppleCrypto.PAL_HashAlgorithm HashAlgorithmToPal(string hashAlgorithmId) => hashAlgorithmId switch {
            HashAlgorithmNames.MD5 => Interop.AppleCrypto.PAL_HashAlgorithm.Md5,
            HashAlgorithmNames.SHA1 => Interop.AppleCrypto.PAL_HashAlgorithm.Sha1,
            HashAlgorithmNames.SHA256 => Interop.AppleCrypto.PAL_HashAlgorithm.Sha256,
            HashAlgorithmNames.SHA384 => Interop.AppleCrypto.PAL_HashAlgorithm.Sha384,
            HashAlgorithmNames.SHA512 => Interop.AppleCrypto.PAL_HashAlgorithm.Sha512,
            _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId))
        };

        internal static class OneShotHashProvider
        {
            public static unsafe int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                Interop.AppleCrypto.PAL_HashAlgorithm algorithm = HashAlgorithmToPal(hashAlgorithmId);

                fixed (byte* pSource = source)
                fixed (byte* pDestination = destination)
                {
                    int ret = Interop.AppleCrypto.DigestOneShot(
                        algorithm,
                        pSource,
                        source.Length,
                        pDestination,
                        destination.Length,
                        out int digestSize);

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

        private sealed class AppleHmacProvider : HashProvider
        {
            private readonly byte[] _key;
            private readonly SafeHmacHandle _ctx;

            private bool _running;

            public override int HashSizeInBytes { get; }

            internal AppleHmacProvider(Interop.AppleCrypto.PAL_HashAlgorithm algorithm, ReadOnlySpan<byte> key)
            {
                _key = key.ToArray();
                int hashSizeInBytes = 0;
                _ctx = Interop.AppleCrypto.HmacCreate(algorithm, ref hashSizeInBytes);

                if (hashSizeInBytes < 0)
                {
                    _ctx.Dispose();
                    throw new PlatformNotSupportedException(
                        SR.Format(
                            SR.Cryptography_UnknownHashAlgorithm,
                            Enum.GetName(typeof(Interop.AppleCrypto.PAL_HashAlgorithm), algorithm)));
                }

                if (_ctx.IsInvalid)
                {
                    _ctx.Dispose();
                    throw new CryptographicException();
                }

                HashSizeInBytes = hashSizeInBytes;
            }

            public override void AppendHashData(ReadOnlySpan<byte> data)
            {
                if (!_running)
                {
                    SetKey();
                }

                if (Interop.AppleCrypto.HmacUpdate(_ctx, data) != 1)
                {
                    throw new CryptographicException();
                }
            }

            private void SetKey()
            {
                if (Interop.AppleCrypto.HmacInit(_ctx, _key, _key.Length) != 1)
                {
                    throw new CryptographicException();
                }

                _running = true;
            }

            public override unsafe int FinalizeHashAndReset(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= HashSizeInBytes);

                if (!_running)
                {
                    SetKey();
                }

                if (Interop.AppleCrypto.HmacFinal(_ctx, destination) != 1)
                {
                    throw new CryptographicException();
                }

                _running = false;
                return HashSizeInBytes;
            }

            public override unsafe int GetCurrentHash(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= HashSizeInBytes);

                if (!_running)
                {
                    SetKey();
                }

                if (Interop.AppleCrypto.HmacCurrent(_ctx, destination) != 1)
                {
                    throw new CryptographicException();
                }

                return HashSizeInBytes;
            }

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _ctx?.Dispose();
                    Array.Clear(_key, 0, _key.Length);
                }
            }

            public override void Reset() => _running = false;
        }

        private sealed class AppleDigestProvider : HashProvider
        {
            private readonly SafeDigestCtxHandle _ctx;
            private bool _running;

            public override int HashSizeInBytes { get; }

            internal AppleDigestProvider(Interop.AppleCrypto.PAL_HashAlgorithm algorithm)
            {
                int hashSizeInBytes;
                _ctx = Interop.AppleCrypto.DigestCreate(algorithm, out hashSizeInBytes);

                if (hashSizeInBytes < 0)
                {
                    _ctx.Dispose();
                    throw new PlatformNotSupportedException(
                        SR.Format(
                            SR.Cryptography_UnknownHashAlgorithm,
                            Enum.GetName(typeof(Interop.AppleCrypto.PAL_HashAlgorithm), algorithm)));
                }

                if (_ctx.IsInvalid)
                {
                    _ctx.Dispose();
                    throw new CryptographicException();
                }

                HashSizeInBytes = hashSizeInBytes;
            }

            public override void AppendHashData(ReadOnlySpan<byte> data)
            {
                _running = true;
                int ret = Interop.AppleCrypto.DigestUpdate(_ctx, data);

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestUpdate return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }
            }

            public override int FinalizeHashAndReset(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= HashSizeInBytes);

                int ret = Interop.AppleCrypto.DigestFinal(_ctx, destination);
                _running = false;

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestFinal return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }

                return HashSizeInBytes;
            }

            public override int GetCurrentHash(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= HashSizeInBytes);

                int ret = Interop.AppleCrypto.DigestCurrent(_ctx, destination);

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestFinal return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }

                return HashSizeInBytes;
            }

            public override void Reset()
            {
                if (_running)
                {
                    int ret = Interop.AppleCrypto.DigestReset(_ctx);

                    if (ret != 1)
                    {
                        Debug.Assert(ret == 0, $"DigestReset return value {ret} was not 0 or 1");
                        throw new CryptographicException();
                    }

                    _running = false;
                }
            }

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _ctx?.Dispose();
                }
            }
        }
    }
}
