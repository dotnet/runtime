// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal struct ConcurrentSafeKmac
    {
        private readonly LiteKmac _liteKmac;
        private ConcurrencyBlock _block;

        public int HashSizeInBytes => _liteKmac.HashSizeInBytes;

        internal ConcurrentSafeKmac(string algorithmId, ReadOnlySpan<byte> key, ReadOnlySpan<byte> customizationString, bool xof)
        {
            _liteKmac = LiteHashProvider.CreateKmac(algorithmId, key, customizationString, xof);
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            using (ConcurrencyBlock.Enter(ref _block))
            {
                _liteKmac.Append(data);
            }
        }

        public int Current(Span<byte> destination)
        {
            using (ConcurrencyBlock.Enter(ref _block))
            {
                return _liteKmac.Current(destination);
            }
        }

        public int Finalize(Span<byte> destination)
        {
            using (ConcurrencyBlock.Enter(ref _block))
            {
                return _liteKmac.Finalize(destination);
            }
        }

        public void Reset()
        {
            using (ConcurrencyBlock.Enter(ref _block))
            {
                _liteKmac.Reset();
            }
        }

        public void Dispose() => _liteKmac.Dispose();
    }
}
