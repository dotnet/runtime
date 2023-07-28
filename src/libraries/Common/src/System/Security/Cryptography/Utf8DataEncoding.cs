// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Security.Cryptography
{
    internal readonly ref struct Utf8DataEncoding
    {
        internal static Encoding ThrowingUtf8Encoding { get; } = new UTF8Encoding(false, true);

        private readonly byte[]? _rented;
        private readonly Span<byte> _buffer;

        internal Utf8DataEncoding(ReadOnlySpan<char> data, Span<byte> stackBuffer)
        {
            int maxLength = ThrowingUtf8Encoding.GetMaxByteCount(data.Length);
            _buffer = (uint)maxLength <= stackBuffer.Length ?
                stackBuffer :
                (_rented = CryptoPool.Rent(maxLength));

            int written = ThrowingUtf8Encoding.GetBytes(data, _buffer);
            _buffer = _buffer.Slice(0, written);
        }

        internal ReadOnlySpan<byte> Utf8Bytes => _buffer;

        internal void Dispose()
        {
            CryptographicOperations.ZeroMemory(_buffer);

            if (_rented is not null)
            {
                CryptoPool.Return(_rented, clearSize: 0);
            }
        }
    }

}
