// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal struct ConcurrentSafeKmac
    {
        private readonly LiteKmac _liteKmac;
        private ConcurrencyBlock _block;

        public int HashSizeInBytes => _liteKmac.HashSizeInBytes;

        // KMAC-256 with a 512-bit capacity is the biggest "typical" use of KMAC (See 8.4.2 from SP-800-185)
        private const int MaxKmacStackAlloc = 64;

        internal ConcurrentSafeKmac(string algorithmId, ReadOnlySpan<byte> key, ReadOnlySpan<byte> customizationString, bool xof)
        {
            _liteKmac = LiteHashProvider.CreateKmac(algorithmId, key, customizationString, xof);
        }

        private ConcurrentSafeKmac(LiteKmac liteKmac)
        {
            _liteKmac = liteKmac;
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

        public ConcurrentSafeKmac Clone()
        {
            using (ConcurrencyBlock.Enter(ref _block))
            {
                return new ConcurrentSafeKmac(_liteKmac.Clone());
            }
        }

        internal bool VerifyCurrentHash(ReadOnlySpan<byte> hash)
        {
            Debug.Assert(!hash.IsEmpty);

            Span<byte> hashBuffer = stackalloc byte[MaxKmacStackAlloc];

            if (hash.Length > MaxKmacStackAlloc)
            {
                hashBuffer = new byte[hash.Length];
            }
            else
            {
                hashBuffer = hashBuffer.Slice(0, hash.Length);
            }

            unsafe
            {
                fixed (byte* pHashBuffer = hashBuffer)
                {
                    int written = Current(hashBuffer);
                    Debug.Assert(written == hash.Length);

                    bool result = CryptographicOperations.FixedTimeEquals(hashBuffer, hash);
                    CryptographicOperations.ZeroMemory(hashBuffer);
                    return result;
                }
            }
        }

        internal bool VerifyHashAndReset(ReadOnlySpan<byte> hash)
        {
            Debug.Assert(!hash.IsEmpty);

            Span<byte> hashBuffer = stackalloc byte[MaxKmacStackAlloc];

            if (hash.Length > MaxKmacStackAlloc)
            {
                hashBuffer = new byte[hash.Length];
            }
            else
            {
                hashBuffer = hashBuffer.Slice(0, hash.Length);
            }

            unsafe
            {
                fixed (byte* pHashBuffer = hashBuffer)
                {
                    int written = Finalize(hashBuffer);
                    Debug.Assert(written == hash.Length);
                    Reset();

                    bool result = CryptographicOperations.FixedTimeEquals(hashBuffer, hash);
                    CryptographicOperations.ZeroMemory(hashBuffer);
                    return result;
                }
            }
        }

        public void Dispose() => _liteKmac.Dispose();
    }
}
