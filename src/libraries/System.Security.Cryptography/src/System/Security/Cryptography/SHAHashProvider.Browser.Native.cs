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
            (_impl, _hashSizeInBytes) = HashAlgorithmToPal(hashAlgorithmId);
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

        public static unsafe int HashOneShot(string hashAlgorithmId, ReadOnlySpan<byte> data, Span<byte> destination)
        {
            (SimpleDigest impl, int hashSizeInBytes) = HashAlgorithmToPal(hashAlgorithmId);
            Debug.Assert(destination.Length >= hashSizeInBytes);

            fixed (byte* src = data)
            fixed (byte* dest = destination)
            {
                int res = Interop.BrowserCrypto.SimpleDigestHash(impl, src, data.Length, dest, destination.Length);
                Debug.Assert(res != 0);
            }

            return hashSizeInBytes;
        }

        public override int HashSizeInBytes => _hashSizeInBytes;

        public override void Dispose(bool disposing)
        {
        }

        public override void Reset()
        {
            _buffer = null;
        }

        private static (SimpleDigest, int) HashAlgorithmToPal(string hashAlgorithmId)
        {
            return hashAlgorithmId switch
            {
                HashAlgorithmNames.SHA256 => (SimpleDigest.Sha256, SHA256.HashSizeInBytes),
                HashAlgorithmNames.SHA1 => (SimpleDigest.Sha1, SHA1.HashSizeInBytes),
                HashAlgorithmNames.SHA384 => (SimpleDigest.Sha384, SHA384.HashSizeInBytes),
                HashAlgorithmNames.SHA512 => (SimpleDigest.Sha512, SHA512.HashSizeInBytes),
                _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId)),
            };
        }
    }
}
