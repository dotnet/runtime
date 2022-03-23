// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class LiteHashProvider
    {
        private static LiteHash CreateHash(string hashAlgorithmId)
        {
            return new LiteHash(hashAlgorithmId);
        }

        private static LiteHmac CreateHmac(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            return new LiteHmac(hashAlgorithmId, key);
        }
    }

    internal readonly struct LiteHash : ILiteHash
    {
        private readonly HashProvider _provider;
        private readonly int _hashSizeInBytes;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHash(string hashAlgorithmId)
        {
            _provider = HashProviderDispenser.CreateHashProvider(hashAlgorithmId);
            _hashSizeInBytes = _provider.HashSizeInBytes;
        }

        public void Append(ReadOnlySpan<byte> data) => _provider.AppendHashData(data);
        public int Finalize(Span<byte> destination) => _provider.FinalizeHashAndReset(destination);
        public void Dispose() => _provider.Dispose();
    }

    internal readonly struct LiteHmac : ILiteHash
    {
        private readonly HashProvider _provider;
        private readonly int _hashSizeInBytes;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHmac(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            _provider = HashProviderDispenser.CreateMacProvider(hashAlgorithmId, key);
            _hashSizeInBytes = _provider.HashSizeInBytes;
        }

        public void Append(ReadOnlySpan<byte> data) => _provider.AppendHashData(data);
        public int Finalize(Span<byte> destination) => _provider.FinalizeHashAndReset(destination);
        public void Dispose() => _provider.Dispose();
    }
}
