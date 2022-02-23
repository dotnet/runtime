// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

using SimpleDigest = Interop.BrowserCrypto.SimpleDigest;

namespace Internal.Cryptography
{
    internal sealed class SHANativeHashProvider : HashProvider
    {
        private readonly int hashSizeInBytes;
        private readonly SimpleDigest impl;
        private MemoryStream? buffer;

        public SHANativeHashProvider(string hashAlgorithmId)
        {
            Debug.Assert(HashProviderDispenser.CanUseSubtleCryptoImpl);

            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                    impl = SimpleDigest.Sha1;
                    hashSizeInBytes = 20;
                    break;
                case HashAlgorithmNames.SHA256:
                    impl = SimpleDigest.Sha256;
                    hashSizeInBytes = 32;
                    break;
                case HashAlgorithmNames.SHA384:
                    impl = SimpleDigest.Sha384;
                    hashSizeInBytes = 48;
                    break;
                case HashAlgorithmNames.SHA512:
                    impl = SimpleDigest.Sha512;
                    hashSizeInBytes = 64;
                    break;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
            }
        }

        public override void AppendHashData(ReadOnlySpan<byte> data)
        {
            buffer ??= new MemoryStream(1000);
            buffer.Write(data);
        }

        public override int FinalizeHashAndReset(Span<byte> destination)
        {
            GetCurrentHash(destination);
            buffer = null;

            return hashSizeInBytes;
        }

        public override int GetCurrentHash(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= hashSizeInBytes);

            byte[] srcArray = Array.Empty<byte>();
            int srcLength = 0;
            if (buffer != null)
            {
                srcArray = buffer.GetBuffer();
                srcLength = (int)buffer.Length;
            }

            unsafe
            {
                fixed (byte* src = srcArray)
                fixed (byte* dest = destination)
                {
                    int res = Interop.BrowserCrypto.SimpleDigestHash(impl, src, srcLength, dest, destination.Length);
                    Debug.Assert(res != 0);
                }
            }

            return hashSizeInBytes;
        }

        public override int HashSizeInBytes => hashSizeInBytes;

        public override void Dispose(bool disposing)
        {
        }

        public override void Reset()
        {
            buffer = null;
        }
    }
}
