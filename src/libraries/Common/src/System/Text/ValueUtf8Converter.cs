// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Text
{
    /// <summary>
    /// Helper to allow utilizing stack buffer for conversion to UTF-8. Will
    /// switch to ArrayPool if not given enough memory. As such, make sure to
    /// call Clear() to return any potentially rented buffer after conversion.
    /// </summary>
    internal ref struct ValueUtf8Converter
    {
        private byte[]? _arrayToReturnToPool;
        private Span<byte> _bytes;

        public ValueUtf8Converter(Span<byte> initialBuffer)
        {
            _arrayToReturnToPool = null;
            _bytes = initialBuffer;
        }

        public Span<byte> ConvertAndTerminateString(ReadOnlySpan<char> value)
        {
            int maxSize = Encoding.UTF8.GetMaxByteCount(value.Length) + 1;
            if (_bytes.Length < maxSize)
            {
                Dispose();
                _arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(maxSize);
                _bytes = new Span<byte>(_arrayToReturnToPool);
            }

            // Grab the bytes and null terminate
            int byteCount = Encoding.UTF8.GetBytes(value, _bytes);
            _bytes[byteCount] = 0;
            return _bytes.Slice(0, byteCount + 1);
        }

        public void Dispose()
        {
            byte[]? toReturn = _arrayToReturnToPool;
            if (toReturn != null)
            {
                _arrayToReturnToPool = null;
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }
    }
}
