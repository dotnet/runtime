// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

using SimpleDigest = Interop.BrowserCrypto.SimpleDigest;

namespace System.Security.Cryptography
{
    internal sealed class SHANativeHashProvider : HashProvider
    {
        private readonly int _hashSizeInBytes;
        private readonly SimpleDigest _impl;
        private MemoryStream? _buffer;

        public SHANativeHashProvider(string hashAlgorithmId)
        {
            Debug.Assert(Interop.BrowserCrypto.CanUseSubtleCrypto);
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

            ReadOnlySpan<byte> source = _buffer != null ?
                new ReadOnlySpan<byte>(_buffer.GetBuffer(), 0, (int)_buffer.Length) :
                default;

            SimpleDigestHash(_impl, source, destination);

            return _hashSizeInBytes;
        }

        public static int HashOneShot(string hashAlgorithmId, ReadOnlySpan<byte> data, Span<byte> destination)
        {
            (SimpleDigest impl, int hashSizeInBytes) = HashAlgorithmToPal(hashAlgorithmId);
            Debug.Assert(destination.Length >= hashSizeInBytes);

            SimpleDigestHash(impl, data, destination);

            return hashSizeInBytes;
        }

        private static unsafe void SimpleDigestHash(SimpleDigest hashName, ReadOnlySpan<byte> data, Span<byte> destination)
        {
            fixed (byte* src = data)
            fixed (byte* dest = destination)
            {
                int res = Interop.BrowserCrypto.SimpleDigestHash(hashName, src, data.Length, dest, destination.Length);
                if (res != 0)
                {
                    throw new CryptographicException(SR.Format(SR.Unknown_SubtleCrypto_Error, res));
                }
            }
        }

        public override int HashSizeInBytes => _hashSizeInBytes;

        public override void Dispose(bool disposing)
        {
        }

        public override void Reset()
        {
            _buffer = null;
        }

        internal static (SimpleDigest HashName, int HashSizeInBytes) HashAlgorithmToPal(string hashAlgorithmId)
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
