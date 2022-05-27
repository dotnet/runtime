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
        private readonly int _hashSizeInBytes;
        private readonly SimpleDigest _impl;
        private MemoryStream? _buffer;

        public SHANativeHashProvider(string hashAlgorithmId)
        {
            Debug.Assert(HashProviderDispenser.CanUseSubtleCryptoImpl);

            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                    _impl = SimpleDigest.Sha1;
                    _hashSizeInBytes = 20;
                    break;
                case HashAlgorithmNames.SHA256:
                    _impl = SimpleDigest.Sha256;
                    _hashSizeInBytes = 32;
                    break;
                case HashAlgorithmNames.SHA384:
                    _impl = SimpleDigest.Sha384;
                    _hashSizeInBytes = 48;
                    break;
                case HashAlgorithmNames.SHA512:
                    _impl = SimpleDigest.Sha512;
                    _hashSizeInBytes = 64;
                    break;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
            }
        }

        public override void AppendHashData(ReadOnlySpan<byte> data)
        {
            _buffer ??= new MemoryStream(1000);
            _buffer.Write(data);
        }

        public override int FinalizeHashAndReset(Span<byte> destination)
        {
            GetCurrentHash(destination);
            _buffer = null;

            return _hashSizeInBytes;
        }

        public override int GetCurrentHash(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            byte[] srcArray = Array.Empty<byte>();
            int srcLength = 0;
            if (_buffer != null)
            {
                srcArray = _buffer.GetBuffer();
                srcLength = (int)_buffer.Length;
            }

            unsafe
            {
                fixed (byte* src = srcArray)
                fixed (byte* dest = destination)
                {
                    int res = Interop.BrowserCrypto.SimpleDigestHash(_impl, src, srcLength, dest, destination.Length);
                    Debug.Assert(res != 0);
                }
            }

            return _hashSizeInBytes;
        }

        public override int HashSizeInBytes => _hashSizeInBytes;

        public override void Dispose(bool disposing)
        {
        }

        public override void Reset()
        {
            _buffer = null;
        }
    }
}
