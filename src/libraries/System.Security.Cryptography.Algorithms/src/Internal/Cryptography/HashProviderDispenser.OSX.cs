// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
        }

        public static HashProvider CreateMacProvider(string hashAlgorithmId, byte[] key)
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

            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private sealed class AppleHmacProvider : HashProvider
        {
            private readonly byte[] _key;
            private readonly SafeHmacHandle _ctx;

            private bool _running;

            public override int HashSizeInBytes { get; }

            internal AppleHmacProvider(Interop.AppleCrypto.PAL_HashAlgorithm algorithm, byte[] key)
            {
                _key = key.CloneByteArray();
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

                if (Interop.AppleCrypto.HmacUpdate(_ctx, data, data.Length) != 1)
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

            public override unsafe byte[] FinalizeHashAndReset()
            {
                var output = new byte[HashSizeInBytes];
                bool success = TryFinalizeHashAndReset(output, out int bytesWritten);
                Debug.Assert(success);
                Debug.Assert(bytesWritten == output.Length);
                return output;
            }

            public override bool TryFinalizeHashAndReset(Span<byte> destination, out int bytesWritten)
            {
                if (destination.Length < HashSizeInBytes)
                {
                    bytesWritten = 0;
                    return false;
                }

                if (!_running)
                {
                    SetKey();
                }

                if (Interop.AppleCrypto.HmacFinal(_ctx, destination, destination.Length) != 1)
                {
                    throw new CryptographicException();
                }

                bytesWritten = HashSizeInBytes;
                _running = false;
                return true;
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
                int ret = Interop.AppleCrypto.DigestUpdate(_ctx, data, data.Length);
                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestUpdate return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }
            }

            public override byte[] FinalizeHashAndReset()
            {
                var hash = new byte[HashSizeInBytes];
                bool success = TryFinalizeHashAndReset(hash, out int bytesWritten);
                Debug.Assert(success);
                Debug.Assert(bytesWritten == hash.Length);
                return hash;
            }

            public override bool TryFinalizeHashAndReset(Span<byte> destination, out int bytesWritten)
            {
                if (destination.Length < HashSizeInBytes)
                {
                    bytesWritten = 0;
                    return false;
                }

                int ret = Interop.AppleCrypto.DigestFinal(_ctx, destination, destination.Length);
                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestFinal return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }

                bytesWritten = HashSizeInBytes;
                return true;
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
