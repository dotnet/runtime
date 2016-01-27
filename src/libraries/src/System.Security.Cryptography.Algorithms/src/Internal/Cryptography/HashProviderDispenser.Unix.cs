// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    return new EvpHashProvider(Interop.Crypto.EvpSha1());
                case HashAlgorithmNames.SHA256:
                    return new EvpHashProvider(Interop.Crypto.EvpSha256());
                case HashAlgorithmNames.SHA384:
                    return new EvpHashProvider(Interop.Crypto.EvpSha384());
                case HashAlgorithmNames.SHA512:
                    return new EvpHashProvider(Interop.Crypto.EvpSha512());
                case HashAlgorithmNames.MD5:
                    return new EvpHashProvider(Interop.Crypto.EvpMd5());
            }
            throw new CryptographicException();
        }

        public static unsafe HashProvider CreateMacProvider(string hashAlgorithmId, byte[] key)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                    return new HmacHashProvider(Interop.Crypto.EvpSha1(), key);
                case HashAlgorithmNames.SHA256:
                    return new HmacHashProvider(Interop.Crypto.EvpSha256(), key);
                case HashAlgorithmNames.SHA384:
                    return new HmacHashProvider(Interop.Crypto.EvpSha384(), key);
                case HashAlgorithmNames.SHA512:
                    return new HmacHashProvider(Interop.Crypto.EvpSha512(), key);
                case HashAlgorithmNames.MD5:
                    return new HmacHashProvider(Interop.Crypto.EvpMd5(), key);
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

                _hashSize = Interop.Crypto.EvpMdSize(_algorithmEvp);
                if (_hashSize <= 0 || _hashSize > Interop.Crypto.EVP_MAX_MD_SIZE)
                {
                    throw new CryptographicException();
                }

                _ctx = Interop.Crypto.EvpMdCtxCreate(_algorithmEvp);

                Interop.Crypto.CheckValidOpenSslHandle(_ctx);
            }

            public sealed override unsafe void AppendHashDataCore(byte[] data, int offset, int count)
            {
                fixed (byte* md = data)
                {
                    Check(Interop.Crypto.EvpDigestUpdate(_ctx, md + offset, count));
                }
            }

            public sealed override unsafe byte[] FinalizeHashAndReset()
            {
                byte* md = stackalloc byte[Interop.Crypto.EVP_MAX_MD_SIZE];
                uint length = (uint)Interop.Crypto.EVP_MAX_MD_SIZE;
                Check(Interop.Crypto.EvpDigestFinalEx(_ctx, md, ref length));
                Debug.Assert(length == _hashSize);

                // Reset the algorithm provider.
                Check(Interop.Crypto.EvpDigestReset(_ctx, _algorithmEvp));

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
            private SafeHmacCtxHandle _hmacCtx;

            public unsafe HmacHashProvider(IntPtr algorithmEvp, byte[] key)
            {
                Debug.Assert(algorithmEvp != IntPtr.Zero);
                Debug.Assert(key != null);

                _hashSize = Interop.Crypto.EvpMdSize(algorithmEvp);
                if (_hashSize <= 0 || _hashSize > Interop.Crypto.EVP_MAX_MD_SIZE)
                {
                    throw new CryptographicException();
                }

                fixed (byte* keyPtr = key)
                {
                    _hmacCtx = Interop.Crypto.HmacCreate(keyPtr, key.Length, algorithmEvp);
                    Interop.Crypto.CheckValidOpenSslHandle(_hmacCtx);
                }
            }

            public sealed override unsafe void AppendHashDataCore(byte[] data, int offset, int count)
            {
                fixed (byte* md = data)
                {
                    Check(Interop.Crypto.HmacUpdate(_hmacCtx, md + offset, count));
                }
            }

            public sealed override unsafe byte[] FinalizeHashAndReset()
            {
                byte* md = stackalloc byte[Interop.Crypto.EVP_MAX_MD_SIZE];
                int length = Interop.Crypto.EVP_MAX_MD_SIZE;
                Check(Interop.Crypto.HmacFinal(_hmacCtx, md, ref length));
                Debug.Assert(length == _hashSize);

                Check(Interop.Crypto.HmacReset(_hmacCtx));

                byte[] result = new byte[length];
                Marshal.Copy((IntPtr)md, result, 0, length);
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
                    if (_hmacCtx != null)
                    {
                        _hmacCtx.Dispose();
                        _hmacCtx = null;
                    }
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
