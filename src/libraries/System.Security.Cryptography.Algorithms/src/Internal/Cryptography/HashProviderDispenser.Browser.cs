// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Internal.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        private static readonly JSObject? s_crypto = (JSObject)System.Runtime.InteropServices.JavaScript.Runtime.GetGlobalObject("crypto");
        private static readonly JSObject? s_subtle = (JSObject?)s_crypto?.GetObjectProperty("subtle");

        public static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            if (s_subtle == null)
            {
                Debug.Fail($"WebCrypto can not be found");
                throw new CryptographicException();
            }

            return new BrowserAsyncDigestProvider(hashAlgorithmId);
        }

        public static unsafe HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            if (s_subtle == null)
            {
                Debug.Fail($"WebCrypto can not be found");
                throw new CryptographicException();
            }

            return new BrowserAsyncHmacProvider(hashAlgorithmId, key);
        }
        private static string HashAlgorithmToPal(string hashAlgorithmId) => hashAlgorithmId switch {
            // https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest
            // Note: MD5 is not supported by WebCrypto API
            HashAlgorithmNames.SHA1 => "SHA-1",
            HashAlgorithmNames.SHA256 => "SHA-256",
            HashAlgorithmNames.SHA384 => "SHA-384",
            HashAlgorithmNames.SHA512 => "SHA-512",
            _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId))
        };

        internal static class OneShotHashProvider
        {
            public static unsafe int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination) => throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyAlgorithms_PlatformNotSupported);

            public static async Task<int> HashDataAsync(string hashAlgorithmId, byte[] source, byte[] destination, CancellationToken cancellationToken)
            {
                string algorithm = HashAlgorithmToPal(hashAlgorithmId);

                int written = 0;
                if (s_subtle?.GetObjectProperty("digest") is Function digest)
                {
                    using Uint8Array taSource = Uint8Array.From(source);
                    using (digest)
                        if (digest.Call(s_subtle, algorithm, taSource) is Task<object> hic)
                        {
                            using ArrayBuffer? digestValue = await hic.ConfigureAwait(false) as ArrayBuffer;
                            using Uint8Array hashArray = new Uint8Array(digestValue!);
                            written = hashArray.Length;
                            Debug.Assert(written == destination.Length);
                            System.Array.Copy(hashArray.ToArray(), destination, destination.Length);
                        }
                        else
                        {
                            Debug.Fail($"WebCrypto digest() error.");
                            throw new CryptographicException();
                        }
                }
                else
                {
                    Debug.Fail($"WebCrypto API can not be found");
                    throw new CryptographicException();
                }
                return written;
            }
        }

        private sealed class BrowserAsyncDigestProvider : HashProvider
        {
            private Uint8Array? _dataToHash;
            private readonly string _hashAlgorithm;
            public override int HashSizeInBytes { get; }

            internal BrowserAsyncDigestProvider(string algorithm)
            {
                int hashSizeInBytes;
                switch (algorithm)
                {
                    case HashAlgorithmNames.SHA1:
                        hashSizeInBytes = 20;
                        break;
                    case HashAlgorithmNames.SHA256:
                        hashSizeInBytes = 32;
                        break;
                    case HashAlgorithmNames.SHA384:
                        hashSizeInBytes = 48;
                        break;
                    case HashAlgorithmNames.SHA512:
                        hashSizeInBytes = 64;
                        break;
                    default:
                        throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, algorithm));
                }

                _hashAlgorithm = HashAlgorithmToPal(algorithm);
                HashSizeInBytes = hashSizeInBytes;
            }

            public override void AppendHashData(ReadOnlySpan<byte> data) => throw new PlatformNotSupportedException();

            public override Task AppendHashDataAsync(byte[] array, int ibStart, int cbSize, CancellationToken cancellationToken)
            {
                //System.Diagnostics.Debug.WriteLine($"BrowserDigestAsyncProvider:AppendHashDataAsync data length: {array.Length}");
                if (_dataToHash == null)
                {
                    _dataToHash = Uint8Array.From(array.AsSpan<byte>(ibStart, cbSize));
                }
                else
                {
                    // TypedArrays do not support concatenation
                    // So we have to resort to creating multiple arrays.
                    long hashLength = _dataToHash.Length;
                    byte[] appendage = new byte[_dataToHash.Length + cbSize];
                    System.Array.Copy(_dataToHash.ToArray(), appendage, hashLength);
                    System.Array.Copy(array, ibStart, appendage, hashLength, cbSize);
                    _dataToHash = Uint8Array.From(appendage.AsSpan<byte>());
                }
                return Task.CompletedTask;
            }
            public override int FinalizeHashAndReset(Span<byte> destination) => throw new PlatformNotSupportedException();

            public override async Task<int> FinalizeHashAndResetAsync(byte[] destination, CancellationToken cancellationToken)
            {
                int written = 0;
                if (s_subtle?.GetObjectProperty("digest") is Function digest)
                {
                    using (digest)
                    using (_dataToHash)
                        if (digest!.Call(s_subtle, _hashAlgorithm, _dataToHash) is Task<object> hic)
                        {
                            using ArrayBuffer? digestValue = await hic.ConfigureAwait(false) as ArrayBuffer;
                            using Uint8Array hashArray = new Uint8Array(digestValue!);
                            written = hashArray.Length;
                            Debug.Assert(written == destination.Length);
                            System.Array.Copy(hashArray.ToArray(), destination, destination.Length);
                        }
                    _dataToHash = null;
                }
                else
                {
                    Debug.Fail($"WebCrypto API can not be found");
                    throw new CryptographicException();
                }
                return written;
            }

            public override int GetCurrentHash(Span<byte> destination) => throw new PlatformNotSupportedException();

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _dataToHash?.Dispose();
                    _dataToHash = null;
                }
            }
        }
        private sealed class BrowserAsyncHmacProvider : HashProvider
        {
            private Uint8Array? _dataToHash;

            private readonly byte[] _key;
            private JSObject? _cryptoKey;
            private readonly string _hashAlgorithm;
            public override int HashSizeInBytes { get; }

            internal BrowserAsyncHmacProvider(string algorithm, ReadOnlySpan<byte> key)
            {
                _key = key.ToArray();
                int hashSizeInBytes;
                switch (algorithm)
                {
                    case HashAlgorithmNames.SHA1:
                        hashSizeInBytes = 20;
                        break;
                    case HashAlgorithmNames.SHA256:
                        hashSizeInBytes = 32;
                        break;
                    case HashAlgorithmNames.SHA384:
                        hashSizeInBytes = 48;
                        break;
                    case HashAlgorithmNames.SHA512:
                        hashSizeInBytes = 64;
                        break;
                    default:
                        throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, algorithm));
                }

                _hashAlgorithm = HashAlgorithmToPal(algorithm);
                HashSizeInBytes = hashSizeInBytes;
            }

            public override void AppendHashData(ReadOnlySpan<byte> data) => throw new PlatformNotSupportedException();

            public override Task AppendHashDataAsync(byte[] array, int ibStart, int cbSize, CancellationToken cancellationToken)
            {
                if (_dataToHash == null)
                {
                    _dataToHash = Uint8Array.From(array.AsSpan<byte>(ibStart, cbSize));
                }
                else
                {
                    // TypedArrays do not support concatenation
                    // So we have to resort to creating multiple arrays.
                    long hashLength = _dataToHash.Length;
                    byte[] appendage = new byte[_dataToHash.Length + cbSize];
                    System.Array.Copy(_dataToHash.ToArray(), appendage, hashLength);
                    System.Array.Copy(array, ibStart, appendage, hashLength, cbSize);
                    _dataToHash = Uint8Array.From(appendage.AsSpan<byte>());
                }
                return Task.CompletedTask;
            }

            public override unsafe int FinalizeHashAndReset(Span<byte> destination) => throw new PlatformNotSupportedException();

            public override async Task<int> FinalizeHashAndResetAsync(byte[] destination, CancellationToken cancellationToken)
            {
                int written = 0;
                if (s_subtle?.GetObjectProperty("sign") is Function sign)
                {
                    await ImportKey().ConfigureAwait(false);

                    using (sign)
                    using (_dataToHash)
                        if (sign!.Call(s_subtle, "HMAC", _cryptoKey, _dataToHash) is Task<object> hic)
                        {
                            using ArrayBuffer? digestValue = await hic.ConfigureAwait(false) as ArrayBuffer;
                            using Uint8Array hashArray = new Uint8Array(digestValue!);
                            written = hashArray.Length;
                            Debug.Assert(written == destination.Length);
                            System.Array.Copy(hashArray.ToArray(), destination, destination.Length);
                        }
                    _dataToHash = null;
                }
                else
                {
                    Debug.Fail($"WebCrypto API can not be found");
                    throw new CryptographicException();
                }
                return written;
            }

            private async Task ImportKey()
            {
                if (_cryptoKey == null)
                {
                    if (s_subtle?.GetObjectProperty("importKey") is Function importKey)
                    {
                        using Uint8Array keyData = Uint8Array.From(_key);
                        using JSObject hmacImportParams = new JSObject();
                        hmacImportParams.SetObjectProperty("name", "HMAC");

                        using JSObject hash = new JSObject();
                        hash.SetObjectProperty("name", _hashAlgorithm);

                        hmacImportParams.SetObjectProperty("hash", hash);

                        using System.Runtime.InteropServices.JavaScript.Array keyUsages = new System.Runtime.InteropServices.JavaScript.Array();
                        keyUsages.Push("sign");
                        keyUsages.Push("verify");

                        using (importKey)
                            if (importKey!.Call(s_subtle, "raw", keyData, hmacImportParams, false, keyUsages) is Task<object> hic)
                            {
                                _cryptoKey = await hic.ConfigureAwait(false) as JSObject;
                                Debug.Assert(_cryptoKey != null);
                            }
                    }
                    else
                    {
                        Debug.Fail($"WebCrypto API can not be found");
                        throw new CryptographicException();
                    }
                }
                return;
            }

            public override unsafe int GetCurrentHash(Span<byte> destination)  => throw new PlatformNotSupportedException();

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    System.Array.Clear(_key, 0, _key.Length);
                    _cryptoKey?.Dispose();
                    _cryptoKey = null;
                }
            }
        }
    }
}
