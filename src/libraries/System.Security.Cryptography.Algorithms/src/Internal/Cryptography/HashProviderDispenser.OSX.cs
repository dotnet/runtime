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
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.MD5:
                    return new AppleDigestProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Md5);
                case HashAlgorithmNames.SHA1:
                    return new AppleDigestProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Sha1);
                case HashAlgorithmNames.SHA256:
                    return new AppleDigestProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Sha256);
                case HashAlgorithmNames.SHA384:
                    return new AppleDigestProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Sha384);
                case HashAlgorithmNames.SHA512:
                    return new AppleDigestProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Sha512);
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
        }

        public static HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.MD5:
                    return new AppleHmacProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Md5, key);
                case HashAlgorithmNames.SHA1:
                    return new AppleHmacProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Sha1, key);
                case HashAlgorithmNames.SHA256:
                    return new AppleHmacProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Sha256, key);
                case HashAlgorithmNames.SHA384:
                    return new AppleHmacProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Sha384, key);
                case HashAlgorithmNames.SHA512:
                    return new AppleHmacProvider(Interop.AppleCrypto.PAL_HashAlgorithm.Sha512, key);
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
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
        }

        private sealed class AppleDigestProvider : HashProvider
        {
            private readonly SafeDigestCtxHandle _ctx;

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
