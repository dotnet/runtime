// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static class CryptoPool
    {
        internal const int ClearAll = -1;

        internal static byte[] Rent(int minimumLength) => ArrayPool<byte>.Shared.Rent(minimumLength);

        internal static void Return(ArraySegment<byte> arraySegment, int clearSize = ClearAll)
        {
            Debug.Assert(arraySegment.Array != null);
            Debug.Assert(arraySegment.Offset == 0);

            Return(arraySegment.Array, clearSize == ClearAll ? arraySegment.Count : clearSize);
        }

        internal static void Return(byte[] array, int clearSize = ClearAll)
        {
            Debug.Assert(clearSize <= array.Length);
            bool clearWholeArray = clearSize < 0;

            if (!clearWholeArray && clearSize != 0)
            {
#if (NET || NETSTANDARD2_1) && !CP_NO_ZEROMEMORY
                CryptographicOperations.ZeroMemory(array.AsSpan(0, clearSize));
#else
                Array.Clear(array, 0, clearSize);
#endif
            }

            ArrayPool<byte>.Shared.Return(array, clearWholeArray);
        }
    }

    internal ref struct CryptoPoolLease : IDisposable
    {
        private byte[]? _rented;
        private bool _skipClear;

        internal Span<byte> Span { get; private set; }

        internal readonly bool IsRented => _rented is not null;

        public void Dispose()
        {
            Return();
        }

        internal void Return()
        {
            Return(_skipClear ? 0 : Span.Length);
        }

        private void Return(int clearSize)
        {
            if (_rented is not null)
            {
                CryptoPool.Return(_rented, clearSize);
                _rented = null;
            }
            else if (!_skipClear && clearSize > 0)
            {
                Span<byte> toClear = Span.Slice(0, clearSize);

#if (NET || NETSTANDARD2_1) && !CP_NO_ZEROMEMORY
                CryptographicOperations.ZeroMemory(toClear);
#else
                toClear.Clear();
#endif
            }

            Span = default;
        }

        internal static CryptoPoolLease Rent(int length, bool skipClear = false)
        {
            byte[] rented = CryptoPool.Rent(length);

            return new CryptoPoolLease
            {
                _rented = rented,
                _skipClear = skipClear,
                Span = new Span<byte>(rented, 0, length)
            };
        }

        internal static CryptoPoolLease RentConditionally(
            int length,
            Span<byte> currentBuffer,
            bool skipClear = false,
            bool skipClearIfNotRented = false)
        {
            return RentConditionally(
                length,
                currentBuffer,
                out _,
                skipClear,
                skipClearIfNotRented);
        }

        internal static CryptoPoolLease RentConditionally(
            int length,
            Span<byte> currentBuffer,
            out bool rented,
            bool skipClear = false,
            bool skipClearIfNotRented = false)
        {
            if (currentBuffer.Length >= length)
            {
                rented = false;

                return new CryptoPoolLease
                {
                    _rented = null,
                    _skipClear = skipClearIfNotRented || skipClear,
                    Span = currentBuffer.Slice(0, length)
                };
            }

            rented = true;
            return Rent(length, skipClear);
        }
    }
}
