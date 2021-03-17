// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal sealed class SHAHashProvider : HashProvider
    {
        private readonly int hashSizeInBytes;
        private readonly Interop.BrowserCrypto.SimpleDigest impl;
        private MemoryStream buffer;

        public SHAHashProvider(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                    impl = Interop.BrowserCrypto.SimpleDigest.Sha1;
                    hashSizeInBytes = 20;
                    break;
                case HashAlgorithmNames.SHA256:
                    impl = Interop.BrowserCrypto.SimpleDigest.Sha256;
                    hashSizeInBytes = 32;
                    break;
                case HashAlgorithmNames.SHA384:
                    impl = Interop.BrowserCrypto.SimpleDigest.Sha384;
                    hashSizeInBytes = 48;
                    break;
                case HashAlgorithmNames.SHA512:
                    impl = Interop.BrowserCrypto.SimpleDigest.Sha512;
                    hashSizeInBytes = 64;
                    break;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
            }
        }

        public override void AppendHashData(ReadOnlySpan<byte> data)
        {
            if (buffer == null)
            {
                buffer = new MemoryStream(1000);
            }

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
                    if (res == 0)
                    {
                        throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyAlgorithms_PlatformNotSupported);
                    }
                }
            }

            return hashSizeInBytes;
        }

        public override int HashSizeInBytes => hashSizeInBytes;

        public override void Dispose(bool disposing)
        {
        }
    }
}
