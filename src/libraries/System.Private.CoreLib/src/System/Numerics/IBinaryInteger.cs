// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines an integer type that is represented in a base-2 format.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface IBinaryInteger<TSelf>
        : IBinaryNumber<TSelf>,
          IShiftOperators<TSelf, TSelf>
        where TSelf : IBinaryInteger<TSelf>
    {
        /// <summary>Computes the quotient and remainder of two values.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The quotient and remainder of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        static abstract (TSelf Quotient, TSelf Remainder) DivRem(TSelf left, TSelf right);

        /// <summary>Computes the number of leading zeros in a value.</summary>
        /// <param name="value">The value whose leading zeroes are to be counted.</param>
        /// <returns>The number of leading zeros in <paramref name="value" />.</returns>
        static abstract TSelf LeadingZeroCount(TSelf value);

        /// <summary>Computes the number of bits that are set in a value.</summary>
        /// <param name="value">The value whose set bits are to be counted.</param>
        /// <returns>The number of set bits in <paramref name="value" />.</returns>
        static abstract TSelf PopCount(TSelf value);

        /// <summary>Rotates a value left by a given amount.</summary>
        /// <param name="value">The value which is rotated left by <paramref name="rotateAmount" />.</param>
        /// <param name="rotateAmount">The amount by which <paramref name="value" /> is rotated left.</param>
        /// <returns>The result of rotating <paramref name="value" /> left by <paramref name="rotateAmount" />.</returns>
        static abstract TSelf RotateLeft(TSelf value, int rotateAmount);

        /// <summary>Rotates a value right by a given amount.</summary>
        /// <param name="value">The value which is rotated right by <paramref name="rotateAmount" />.</param>
        /// <param name="rotateAmount">The amount by which <paramref name="value" /> is rotated right.</param>
        /// <returns>The result of rotating <paramref name="value" /> right by <paramref name="rotateAmount" />.</returns>
        static abstract TSelf RotateRight(TSelf value, int rotateAmount);

        /// <summary>Computes the number of trailing zeros in a value.</summary>
        /// <param name="value">The value whose trailing zeroes are to be counted.</param>
        /// <returns>The number of trailing zeros in <paramref name="value" />.</returns>
        static abstract TSelf TrailingZeroCount(TSelf value);

        /// <summary>Gets the length, in bits, of the shortest two's complement representation of the current value.</summary>
        /// <returns>The length, in bits, of the shortest two's complement representation of the current value.</returns>
        long GetShortestBitLength();

        /// <summary>Gets the number of bytes that will be written as part of <see cref="TryWriteLittleEndian(Span{byte}, out int)" />.</summary>
        /// <returns>The number of bytes that will be written as part of <see cref="TryWriteLittleEndian(Span{byte}, out int)" />.</returns>
        int GetByteCount();

        /// <summary>Tries to write the current value, in little-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current value should be written.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination" />.</param>
        /// <returns><c>true</c> if the value was succesfully written to <paramref name="destination" />; otherwise, <c>false</c>.</returns>
        bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten);

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
