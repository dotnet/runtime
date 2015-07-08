// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        public static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                    return new EvpHashProvider(Interop.libcrypto.EVP_sha1());
                case HashAlgorithmNames.SHA256:
                    return new EvpHashProvider(Interop.libcrypto.EVP_sha256());
                case HashAlgorithmNames.SHA384:
                    return new EvpHashProvider(Interop.libcrypto.EVP_sha384());
                case HashAlgorithmNames.SHA512:
                    return new EvpHashProvider(Interop.libcrypto.EVP_sha512());
                case HashAlgorithmNames.MD5:
                    return new EvpHashProvider(Interop.libcrypto.EVP_md5());
            }
            throw new CryptographicException();
        }

        public static unsafe HashProvider CreateMacProvider(string hashAlgorithmId, byte[] key)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                    return new HmacHashProvider(Interop.libcrypto.EVP_sha1(), key);
                case HashAlgorithmNames.SHA256:
                    return new HmacHashProvider(Interop.libcrypto.EVP_sha256(), key);
                case HashAlgorithmNames.SHA384:
                    return new HmacHashProvider(Interop.libcrypto.EVP_sha384(), key);
                case HashAlgorithmNames.SHA512:
                    return new HmacHashProvider(Interop.libcrypto.EVP_sha512(), key);
                case HashAlgorithmNames.MD5:
                    return new HmacHashProvider(Interop.libcrypto.EVP_md5(), key);
            }
            throw new CryptographicException();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private sealed class EvpHashProvider : HashProvider
        {
            private readonly IntPtr _algorithmEvp;
            private readonly int _hashSize;
            private readonly SafeEvpMdCtxHandle _ctx;

            public EvpHashProvider(IntPtr algorithmEvp)
            {
                _algorithmEvp = algorithmEvp;
                Debug.Assert(algorithmEvp != IntPtr.Zero);

                _hashSize = Interop.libcrypto.EVP_MD_size(algorithmEvp);
                if (_hashSize <= 0 || _hashSize > Interop.libcrypto.EVP_MAX_MD_SIZE)
                {
                    throw new CryptographicException();
                }

                _ctx = Interop.libcrypto.EVP_MD_CTX_create();

                Interop.libcrypto.CheckValidOpenSslHandle(_ctx);

                Check(Interop.libcrypto.EVP_DigestInit_ex(_ctx, algorithmEvp, IntPtr.Zero));
            }

            public sealed override unsafe void AppendHashDataCore(byte[] data, int offset, int count)
            {
                fixed (byte* md = data)
                {
                    Check(Interop.libcrypto.EVP_DigestUpdate(_ctx, md + offset, (IntPtr)count));
                }
            }

            public sealed override unsafe byte[] FinalizeHashAndReset()
            {
                byte* md = stackalloc byte[Interop.libcrypto.EVP_MAX_MD_SIZE];
                uint length = Interop.libcrypto.EVP_MAX_MD_SIZE;
                Check(Interop.libcrypto.EVP_DigestFinal_ex(_ctx, md, ref length));
                Debug.Assert(length == _hashSize);

                // Reset the algorithm provider.
                Check(Interop.libcrypto.EVP_DigestInit_ex(_ctx, _algorithmEvp, IntPtr.Zero));

                byte[] result = new byte[(int)length];
                Marshal.Copy((IntPtr)md, result, 0, (int)length);
                return result;
            }

            public sealed override int HashSizeInBytes
            {
                get { return _hashSize; }
            }

            public sealed override void Dispose(bool disposing)
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
            private Interop.libcrypto.HMAC_CTX _hmacCtx;

            public unsafe HmacHashProvider(IntPtr algorithmEvp, byte[] key)
            {
                Debug.Assert(algorithmEvp != IntPtr.Zero);
                Debug.Assert(key != null);

                _hashSize = Interop.libcrypto.EVP_MD_size(algorithmEvp);
                if (_hashSize <= 0 || _hashSize > Interop.libcrypto.EVP_MAX_MD_SIZE)
                {
                    throw new CryptographicException();
                }

                fixed (byte* keyPtr = key)
                {
                    Check(Interop.libcrypto.HMAC_Init(out _hmacCtx, keyPtr, key.Length, algorithmEvp));
                }
            }

            public sealed override unsafe void AppendHashDataCore(byte[] data, int offset, int count)
            {
                fixed (byte* md = data)
                {
                    Check(Interop.libcrypto.HMAC_Update(ref _hmacCtx, md + offset, count));
                }
            }

            public sealed override unsafe byte[] FinalizeHashAndReset()
            {
                byte* md = stackalloc byte[Interop.libcrypto.EVP_MAX_MD_SIZE];
                uint length = Interop.libcrypto.EVP_MAX_MD_SIZE;
                Check(Interop.libcrypto.HMAC_Final(ref _hmacCtx, md, ref length));
                Debug.Assert(length == _hashSize);

                // HMAC_Init_ex with all NULL values keeps the key and algorithm (and engine) intact,
                // but resets the values for another computation.
                Check(Interop.libcrypto.HMAC_Init_ex(ref _hmacCtx, null, 0, IntPtr.Zero, IntPtr.Zero));

                byte[] result = new byte[(int)length];
                Marshal.Copy((IntPtr)md, result, 0, (int)length);
                return result;
            }

            public sealed override int HashSizeInBytes 
            {
                get { return _hashSize; } 
            }

            public sealed override void Dispose(bool disposing)
            {
                Interop.libcrypto.HMAC_CTX_cleanup(ref _hmacCtx);
            }
        }

        private static void Check(int result)
        {
            const int Success = 1;
            if (result != Success)
            {
                Debug.Assert(result == 0);
                throw Interop.libcrypto.CreateOpenSslCryptographicException();
            }
        }
    }
}
