// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.Apple;

using PAL_HashAlgorithm = Interop.AppleCrypto.PAL_HashAlgorithm;

namespace System.Security.Cryptography
{
    internal static partial class LiteHashProvider
    {
        internal static LiteHash CreateHash(string hashAlgorithmId)
        {
            PAL_HashAlgorithm algorithm = HashAlgorithmNames.HashAlgorithmToPal(hashAlgorithmId);
            return new LiteHash(algorithm);
        }

        internal static LiteHmac CreateHmac(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            PAL_HashAlgorithm algorithm = HashAlgorithmNames.HashAlgorithmToPal(hashAlgorithmId);
            return new LiteHmac(algorithm, key, preinitialize: true);
        }
    }

    internal readonly struct LiteHash : ILiteHash
    {
        private readonly SafeDigestCtxHandle _ctx;
        private readonly int _hashSizeInBytes;

        private const int Success = 1;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHash(PAL_HashAlgorithm algorithm)
        {
            int hashSizeInBytes;
            _ctx = Interop.AppleCrypto.DigestCreate(algorithm, out hashSizeInBytes);

            if (hashSizeInBytes < 0)
            {
                _ctx.Dispose();
                throw new PlatformNotSupportedException(
                    SR.Format(
                        SR.Cryptography_UnknownHashAlgorithm,
                        Enum.GetName(typeof(PAL_HashAlgorithm), algorithm)));
            }

            if (_ctx.IsInvalid)
            {
                _ctx.Dispose();
                throw new CryptographicException();
            }

            _hashSizeInBytes = hashSizeInBytes;
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            int ret = Interop.AppleCrypto.DigestUpdate(_ctx, data);

            if (ret != Success)
            {
                Debug.Assert(ret == 0, $"{nameof(Interop.AppleCrypto.DigestUpdate)} return value {ret} was not 0 or 1");
                throw new CryptographicException();
            }
        }

        public int Current(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            int ret = Interop.AppleCrypto.DigestCurrent(_ctx, destination);

            if (ret != Success)
            {
                Debug.Assert(ret == 0, $"{nameof(Interop.AppleCrypto.DigestCurrent)} return value {ret} was not 0 or 1");
                throw new CryptographicException();
            }

            return _hashSizeInBytes;
        }

        public int Finalize(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            int ret = Interop.AppleCrypto.DigestFinal(_ctx, destination);

            if (ret != Success)
            {
                Debug.Assert(ret == 0, $"{nameof(Interop.AppleCrypto.DigestFinal)} return value {ret} was not 0 or 1");
                throw new CryptographicException();
            }

            return _hashSizeInBytes;
        }

        public void Reset()
        {
            int ret = Interop.AppleCrypto.DigestReset(_ctx);

            if (ret != Success)
            {
                Debug.Assert(ret == 0, $"DigestReset return value {ret} was not 0 or 1");
                throw new CryptographicException();
            }
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }
    }

    internal readonly struct LiteHmac : ILiteHash
    {
        private readonly SafeHmacHandle _ctx;
        private readonly int _hashSizeInBytes;

        private const int Success = 1;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHmac(PAL_HashAlgorithm algorithm, ReadOnlySpan<byte> key, bool preinitialize)
        {
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

            if (preinitialize)
            {
                if (Interop.AppleCrypto.HmacInit(_ctx, key) != Success)
                {
                    _ctx.Dispose();
                    throw new CryptographicException();
                }
            }

            _hashSizeInBytes = hashSizeInBytes;
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            if (Interop.AppleCrypto.HmacUpdate(_ctx, data) != Success)
            {
                Debug.Fail($"{nameof(Interop.AppleCrypto.HmacUpdate)} unexpectedly failed.");
                throw new CryptographicException();
            }
        }

        public int Current(ReadOnlySpan<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            if (Interop.AppleCrypto.HmacCurrent(_ctx, destination) != Success)
            {
                Debug.Fail($"{nameof(Interop.AppleCrypto.HmacCurrent)} unexpectedly failed.");
                throw new CryptographicException();
            }

            return _hashSizeInBytes;
        }

        public int Finalize(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            if (Interop.AppleCrypto.HmacFinal(_ctx, destination) != Success)
            {
                Debug.Fail($"{nameof(Interop.AppleCrypto.HmacFinal)} unexpectedly failed.");
                throw new CryptographicException();
            }

            return _hashSizeInBytes;
        }

        public void Reset(ReadOnlySpan<byte> key)
        {
            if (Interop.AppleCrypto.HmacInit(_ctx, key) != Success)
            {
                Debug.Fail($"{nameof(Interop.AppleCrypto.HmacInit)} unexpectedly failed.");
            }
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }
    }
}
