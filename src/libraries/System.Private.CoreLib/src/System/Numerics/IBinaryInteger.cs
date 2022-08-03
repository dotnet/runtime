// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines an integer type that is represented in a base-2 format.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface IBinaryInteger<TSelf>
        : IBinaryNumber<TSelf>,
          IShiftOperators<TSelf, int, TSelf>
        where TSelf : IBinaryInteger<TSelf>
    {
        /// <summary>Computes the quotient and remainder of two values.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The quotient and remainder of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        static virtual (TSelf Quotient, TSelf Remainder) DivRem(TSelf left, TSelf right)
        {
            TSelf quotient = left / right;
            return (quotient, (left - (quotient * right)));
        }

        /// <summary>Computes the number of leading zeros in a value.</summary>
        /// <param name="value">The value whose leading zeroes are to be counted.</param>
        /// <returns>The number of leading zeros in <paramref name="value" />.</returns>
        static virtual TSelf LeadingZeroCount(TSelf value)
        {
            TSelf bitCount = TSelf.CreateChecked(value.GetByteCount() * 8L);

            if (value == TSelf.Zero)
            {
                return TSelf.CreateChecked(bitCount);
            }

            return (bitCount - TSelf.One) ^ TSelf.Log2(value);
        }

        /// <summary>Computes the number of bits that are set in a value.</summary>
        /// <param name="value">The value whose set bits are to be counted.</param>
        /// <returns>The number of set bits in <paramref name="value" />.</returns>
        static abstract TSelf PopCount(TSelf value);

        /// <summary>Reads a two's complement number from a given array, in big-endian format, and converts it to an instance of the current type.</summary>
        /// <param name="source">The array from which the two's complement number should be read.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source" /> represents an unsigned two's complement number; otherwise, <c>false</c> to indicate it represents a signed two's complement number.</param>
        /// <returns>The value read from <paramref name="source" />.</returns>
        /// <exception cref="OverflowException"><paramref name="source" /> is not representable by <typeparamref name="TSelf" /></exception>
        static virtual TSelf ReadBigEndian(byte[] source, bool isUnsigned)
        {
            if (!TSelf.TryReadBigEndian(source, isUnsigned, out TSelf value))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return value;
        }

        /// <summary>Reads a two's complement number from a given array, in big-endian format, and converts it to an instance of the current type.</summary>
        /// <param name="source">The array from which the two's complement number should be read.</param>
        /// <param name="startIndex">The starting index from which the value should be read.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source" /> represents an unsigned two's complement number; otherwise, <c>false</c> to indicate it represents a signed two's complement number.</param>
        /// <returns>The value read from <paramref name="source" /> starting at <paramref name="startIndex" />.</returns>
        /// <exception cref="OverflowException"><paramref name="source" /> is not representable by <typeparamref name="TSelf" /></exception>
        static virtual TSelf ReadBigEndian(byte[] source, int startIndex, bool isUnsigned)
        {
            if (!TSelf.TryReadBigEndian(source.AsSpan(startIndex), isUnsigned, out TSelf value))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return value;
        }

        /// <summary>Reads a two's complement number from a given span, in big-endian format, and converts it to an instance of the current type.</summary>
        /// <param name="source">The array from which the two's complement number should be read.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source" /> represents an unsigned two's complement number; otherwise, <c>false</c> to indicate it represents a signed two's complement number.</param>
        /// <returns>The value read from <paramref name="source" />.</returns>
        /// <exception cref="OverflowException"><paramref name="source" /> is not representable by <typeparamref name="TSelf" /></exception>
        static virtual TSelf ReadBigEndian(Span<byte> source, bool isUnsigned)
        {
            if (!TSelf.TryReadBigEndian(source, isUnsigned, out TSelf value))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return value;
        }

        /// <summary>Reads a two's complement number from a given array, in little-endian format, and converts it to an instance of the current type.</summary>
        /// <param name="source">The array from which the two's complement number should be read.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source" /> represents an unsigned two's complement number; otherwise, <c>false</c> to indicate it represents a signed two's complement number.</param>
        /// <returns>The value read from <paramref name="source" />.</returns>
        /// <exception cref="OverflowException"><paramref name="source" /> is not representable by <typeparamref name="TSelf" /></exception>
        static virtual TSelf ReadLittleEndian(byte[] source, bool isUnsigned)
        {
            if (!TSelf.TryReadLittleEndian(source, isUnsigned, out TSelf value))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return value;
        }

        /// <summary>Reads a two's complement number from a given array, in little-endian format, and converts it to an instance of the current type.</summary>
        /// <param name="source">The array from which the two's complement number should be read.</param>
        /// <param name="startIndex">The starting index from which the value should be read.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source" /> represents an unsigned two's complement number; otherwise, <c>false</c> to indicate it represents a signed two's complement number.</param>
        /// <returns>The value read from <paramref name="source" /> starting at <paramref name="startIndex" />.</returns>
        /// <exception cref="OverflowException"><paramref name="source" /> is not representable by <typeparamref name="TSelf" /></exception>
        static virtual TSelf ReadLittleEndian(byte[] source, int startIndex, bool isUnsigned)
        {
            if (!TSelf.TryReadLittleEndian(source.AsSpan(startIndex), isUnsigned, out TSelf value))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return value;
        }

        /// <summary>Reads a two's complement number from a given span, in little-endian format, and converts it to an instance of the current type.</summary>
        /// <param name="source">The array from which the two's complement number should be read.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source" /> represents an unsigned two's complement number; otherwise, <c>false</c> to indicate it represents a signed two's complement number.</param>
        /// <returns>The value read from <paramref name="source" />.</returns>
        /// <exception cref="OverflowException"><paramref name="source" /> is not representable by <typeparamref name="TSelf" /></exception>
        static virtual TSelf ReadLittleEndian(Span<byte> source, bool isUnsigned)
        {
            if (!TSelf.TryReadLittleEndian(source, isUnsigned, out TSelf value))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return value;
        }

        /// <summary>Rotates a value left by a given amount.</summary>
        /// <param name="value">The value which is rotated left by <paramref name="rotateAmount" />.</param>
        /// <param name="rotateAmount">The amount by which <paramref name="value" /> is rotated left.</param>
        /// <returns>The result of rotating <paramref name="value" /> left by <paramref name="rotateAmount" />.</returns>
        static virtual TSelf RotateLeft(TSelf value, int rotateAmount)
        {
            int bitCount = checked(value.GetByteCount() * 8);
            return (value << rotateAmount) | (value >> (bitCount - rotateAmount));
        }

        /// <summary>Rotates a value right by a given amount.</summary>
        /// <param name="value">The value which is rotated right by <paramref name="rotateAmount" />.</param>
        /// <param name="rotateAmount">The amount by which <paramref name="value" /> is rotated right.</param>
        /// <returns>The result of rotating <paramref name="value" /> right by <paramref name="rotateAmount" />.</returns>
        static virtual TSelf RotateRight(TSelf value, int rotateAmount)
        {
            int bitCount = checked(value.GetByteCount() * 8);
            return (value >> rotateAmount) | (value << (bitCount - rotateAmount));
        }

        /// <summary>Computes the number of trailing zeros in a value.</summary>
        /// <param name="value">The value whose trailing zeroes are to be counted.</param>
        /// <returns>The number of trailing zeros in <paramref name="value" />.</returns>
        static abstract TSelf TrailingZeroCount(TSelf value);

        /// <summary>Tries to read a two's complement number from a span, in big-endian format, and convert it to an instance of the current type.</summary>
        /// <param name="source">The span from which the two's complement number should be read.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source" /> represents an unsigned two's complement number; otherwise, <c>false</c> to indicate it represents a signed two's complement number.</param>
        /// <param name="value">On return, contains the value read from <paramref name="source" /> or <c>default</c> if a value could not be read.</param>
        /// <returns><c>true</c> if the value was succesfully read from <paramref name="source" />; otherwise, <c>false</c>.</returns>
        static abstract bool TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out TSelf value);

        /// <summary>Tries to read a two's complement number from a span, in little-endian format, and convert it to an instance of the current type.</summary>
        /// <param name="source">The span from which the two's complement number should be read.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source" /> represents an unsigned two's complement number; otherwise, <c>false</c> to indicate it represents a signed two's complement number.</param>
        /// <param name="value">On return, contains the value read from <paramref name="source" /> or <c>default</c> if a value could not be read.</param>
        /// <returns><c>true</c> if the value was succesfully read from <paramref name="source" />; otherwise, <c>false</c>.</returns>
        static abstract bool TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out TSelf value);

        /// <summary>Gets the number of bytes that will be written as part of <see cref="TryWriteLittleEndian(Span{byte}, out int)" />.</summary>
        /// <returns>The number of bytes that will be written as part of <see cref="TryWriteLittleEndian(Span{byte}, out int)" />.</returns>
        int GetByteCount();

        /// <summary>Gets the length, in bits, of the shortest two's complement representation of the current value.</summary>
        /// <returns>The length, in bits, of the shortest two's complement representation of the current value.</returns>
        int GetShortestBitLength();

        /// <summary>Tries to write the current value, in big-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current value should be written.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination" />.</param>
        /// <returns><c>true</c> if the value was successfully written to <paramref name="destination" />; otherwise, <c>false</c>.</returns>
        bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten);

        /// <summary>Tries to write the current value, in little-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current value should be written.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination" />.</param>
        /// <returns><c>true</c> if the value was successfully written to <paramref name="destination" />; otherwise, <c>false</c>.</returns>
        bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten);

        /// <summary>Writes the current value, in big-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current value should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteBigEndian(byte[] destination)
        {
            if (!TryWriteBigEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current value, in big-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current value should be written.</param>
        /// <param name="startIndex">The starting index at which the value should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" /> starting at <paramref name="startIndex" />.</returns>
        int WriteBigEndian(byte[] destination, int startIndex)
        {
            if (!TryWriteBigEndian(destination.AsSpan(startIndex), out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current value, in big-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current value should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteBigEndian(Span<byte> destination)
        {
            if (!TryWriteBigEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current value, in little-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current value should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteLittleEndian(byte[] destination)
        {
            if (!TryWriteLittleEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current value, in little-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current value should be written.</param>
        /// <param name="startIndex">The starting index at which the value should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" /> starting at <paramref name="startIndex" />.</returns>
        int WriteLittleEndian(byte[] destination, int startIndex)
        {
            if (!TryWriteLittleEndian(destination.AsSpan(startIndex), out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current value, in little-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current value should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteLittleEndian(Span<byte> destination)
        {
            if (!TryWriteLittleEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }
    }
}
