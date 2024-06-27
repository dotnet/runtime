// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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

        internal static LiteXof CreateXof(string hashAlgorithmId)
        {
            _ = hashAlgorithmId;
            throw new PlatformNotSupportedException();
        }
    }

    internal readonly struct LiteXof : ILiteHash
    {
        // Nothing uses this for Browser but we need the type.
#pragma warning disable CA1822 // Member does not access instance data
#pragma warning disable IDE0060 // Remove unused parameter
        public int HashSizeInBytes => throw new UnreachableException();
        public void Append(ReadOnlySpan<byte> data) => throw new UnreachableException();
        public int Finalize(Span<byte> destination) => throw new UnreachableException();
        public void Current(Span<byte> destination) => throw new UnreachableException();
        public int Reset() => throw new UnreachableException();
        public LiteXof Clone() => throw new UnreachableException();
        public void Read(Span<byte> destination) => throw new UnreachableException();
        public void Dispose() => throw new UnreachableException();
#pragma warning restore IDE0060
#pragma warning restore CA1822
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
