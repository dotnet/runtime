// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

using SimpleDigest = Interop.BrowserCrypto.SimpleDigest;

namespace System.Security.Cryptography
{
    internal sealed class HMACNativeHashProvider : HashProvider
    {
        private readonly int _hashSizeInBytes;
        private readonly SimpleDigest _hashAlgorithm;
        private readonly byte[] _key;
        private MemoryStream? _buffer;

        public HMACNativeHashProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            Debug.Assert(HashProviderDispenser.CanUseSubtleCryptoImpl);

            (_hashAlgorithm, _hashSizeInBytes) = SHANativeHashProvider.HashAlgorithmToPal(hashAlgorithmId);
            _key = key.ToArray();
        }

        public override void AppendHashData(ReadOnlySpan<byte> data)
        {
            _buffer ??= new MemoryStream(1000);
            _buffer.Write(data);
        }

        public override int FinalizeHashAndReset(Span<byte> destination)
        {
            int written = GetCurrentHash(destination);
            _buffer = null;

            return written;
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
                fixed (byte* key = _key)
                fixed (byte* src = srcArray)
                fixed (byte* dest = destination)
                {
                    int res = Interop.BrowserCrypto.Sign(_hashAlgorithm, key, _key.Length, src, srcLength, dest, destination.Length);
                    Debug.Assert(res != 0);
                }
            }

            return _hashSizeInBytes;
        }

        public static unsafe int MacDataOneShot(string hashAlgorithmId, ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, Span<byte> destination)
        {
            (SimpleDigest hashName, int hashSizeInBytes) = SHANativeHashProvider.HashAlgorithmToPal(hashAlgorithmId);
            Debug.Assert(destination.Length >= hashSizeInBytes);

            fixed (byte* k = key)
            fixed (byte* src = data)
            fixed (byte* dest = destination)
            {
                int res = Interop.BrowserCrypto.Sign(hashName, k, key.Length, src, data.Length, dest, destination.Length);
                Debug.Assert(res != 0);
            }

            return hashSizeInBytes;
        }

        public override int HashSizeInBytes => _hashSizeInBytes;

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CryptographicOperations.ZeroMemory(_key);
            }
         }

        public override void Reset()
        {
            _buffer = null;
        }
    }
}
