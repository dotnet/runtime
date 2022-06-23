// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines a floating-point type.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface IFloatingPoint<TSelf>
        : INumber<TSelf>,
          ISignedNumber<TSelf>
        where TSelf : IFloatingPoint<TSelf>
    {
        /// <summary>Computes the ceiling of a value.</summary>
        /// <param name="x">The value whose ceiling is to be computed.</param>
        /// <returns>The ceiling of <paramref name="x" />.</returns>
        static virtual TSelf Ceiling(TSelf x) => TSelf.Round(x, digits: 0, MidpointRounding.ToPositiveInfinity);

        /// <summary>Computes the floor of a value.</summary>
        /// <param name="x">The value whose floor is to be computed.</param>
        /// <returns>The floor of <paramref name="x" />.</returns>
        static virtual TSelf Floor(TSelf x) => TSelf.Round(x, digits: 0, MidpointRounding.ToNegativeInfinity);

        /// <summary>Rounds a value to the nearest integer using the default rounding mode (<see cref="MidpointRounding.ToEven" />).</summary>
        /// <param name="x">The value to round.</param>
        /// <returns>The result of rounding <paramref name="x" /> to the nearest integer using the default rounding mode.</returns>
        static virtual TSelf Round(TSelf x) => TSelf.Round(x, digits: 0, MidpointRounding.ToEven);

        /// <summary>Rounds a value to a specified number of fractional-digits using the default rounding mode (<see cref="MidpointRounding.ToEven" />).</summary>
        /// <param name="x">The value to round.</param>
        /// <param name="digits">The number of fractional digits to which <paramref name="x" /> should be rounded.</param>
        /// <returns>The result of rounding <paramref name="x" /> to <paramref name="digits" /> fractional-digits using the default rounding mode.</returns>
        static virtual TSelf Round(TSelf x, int digits) => TSelf.Round(x, digits, MidpointRounding.ToEven);

        /// <summary>Rounds a value to the nearest integer using the specified rounding mode.</summary>
        /// <param name="x">The value to round.</param>
        /// <param name="mode">The mode under which <paramref name="x" /> should be rounded.</param>
        /// <returns>The result of rounding <paramref name="x" /> to the nearest integer using <paramref name="mode" />.</returns>
        static virtual TSelf Round(TSelf x, MidpointRounding mode) => TSelf.Round(x, digits: 0, MidpointRounding.ToEven);

        /// <summary>Rounds a value to a specified number of fractional-digits using the default rounding mode (<see cref="MidpointRounding.ToEven" />).</summary>
        /// <param name="x">The value to round.</param>
        /// <param name="digits">The number of fractional digits to which <paramref name="x" /> should be rounded.</param>
        /// <param name="mode">The mode under which <paramref name="x" /> should be rounded.</param>
        /// <returns>The result of rounding <paramref name="x" /> to <paramref name="digits" /> fractional-digits using <paramref name="mode" />.</returns>
        static abstract TSelf Round(TSelf x, int digits, MidpointRounding mode);

        /// <summary>Truncates a value.</summary>
        /// <param name="x">The value to truncate.</param>
        /// <returns>The truncation of <paramref name="x" />.</returns>
        static virtual TSelf Truncate(TSelf x) => TSelf.Round(x, digits: 0, MidpointRounding.ToZero);

        /// <summary>Gets the number of bytes that will be written as part of <see cref="TryWriteExponentLittleEndian(Span{byte}, out int)" />.</summary>
        /// <returns>The number of bytes that will be written as part of <see cref="TryWriteExponentLittleEndian(Span{byte}, out int)" />.</returns>
        int GetExponentByteCount();

        /// <summary>Gets the length, in bits, of the shortest two's complement representation of the current exponent.</summary>
        /// <returns>The length, in bits, of the shortest two's complement representation of the current exponent.</returns>
        int GetExponentShortestBitLength();

        /// <summary>Gets the length, in bits, of the current significand.</summary>
        /// <returns>The length, in bits, of the current significand.</returns>
        int GetSignificandBitLength();

        /// <summary>Gets the number of bytes that will be written as part of <see cref="TryWriteSignificandLittleEndian(Span{byte}, out int)" />.</summary>
        /// <returns>The number of bytes that will be written as part of <see cref="TryWriteSignificandLittleEndian(Span{byte}, out int)" />.</returns>
        int GetSignificandByteCount();

        /// <summary>Tries to write the current exponent, in big-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current exponent should be written.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination" />.</param>
        /// <returns><c>true</c> if the exponent was succesfully written to <paramref name="destination" />; otherwise, <c>false</c>.</returns>
        bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten);

        /// <summary>Tries to write the current exponent, in little-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current exponent should be written.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination" />.</param>
        /// <returns><c>true</c> if the exponent was succesfully written to <paramref name="destination" />; otherwise, <c>false</c>.</returns>
        bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten);

        /// <summary>Tries to write the current significand, in big-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current significand should be written.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination" />.</param>
        /// <returns><c>true</c> if the significand was succesfully written to <paramref name="destination" />; otherwise, <c>false</c>.</returns>
        bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten);

        /// <summary>Tries to write the current significand, in little-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current significand should be written.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination" />.</param>
        /// <returns><c>true</c> if the significand was succesfully written to <paramref name="destination" />; otherwise, <c>false</c>.</returns>
        bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten);

        /// <summary>Writes the current exponent, in big-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current exponent should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteExponentBigEndian(byte[] destination)
        {
            if (!TryWriteExponentBigEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current exponent, in big-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current exponent should be written.</param>
        /// <param name="startIndex">The starting index at which the exponent should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" /> starting at <paramref name="startIndex" />.</returns>
        int WriteExponentBigEndian(byte[] destination, int startIndex)
        {
            if (!TryWriteExponentBigEndian(destination.AsSpan(startIndex), out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current exponent, in big-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current exponent should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteExponentBigEndian(Span<byte> destination)
        {
            if (!TryWriteExponentBigEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current exponent, in little-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current exponent should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteExponentLittleEndian(byte[] destination)
        {
            if (!TryWriteExponentLittleEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current exponent, in little-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current exponent should be written.</param>
        /// <param name="startIndex">The starting index at which the exponent should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" /> starting at <paramref name="startIndex" />.</returns>
        int WriteExponentLittleEndian(byte[] destination, int startIndex)
        {
            if (!TryWriteExponentLittleEndian(destination.AsSpan(startIndex), out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current exponent, in little-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current exponent should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteExponentLittleEndian(Span<byte> destination)
        {
            if (!TryWriteExponentLittleEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current significand, in big-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current significand should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteSignificandBigEndian(byte[] destination)
        {
            if (!TryWriteSignificandBigEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current significand, in big-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current significand should be written.</param>
        /// <param name="startIndex">The starting index at which the significand should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" /> starting at <paramref name="startIndex" />.</returns>
        int WriteSignificandBigEndian(byte[] destination, int startIndex)
        {
            if (!TryWriteSignificandBigEndian(destination.AsSpan(startIndex), out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current significand, in big-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current significand should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteSignificandBigEndian(Span<byte> destination)
        {
            if (!TryWriteSignificandBigEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current significand, in little-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current significand should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteSignificandLittleEndian(byte[] destination)
        {
            if (!TryWriteSignificandLittleEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current significand, in little-endian format, to a given array.</summary>
        /// <param name="destination">The array to which the current significand should be written.</param>
        /// <param name="startIndex">The starting index at which the significand should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" /> starting at <paramref name="startIndex" />.</returns>
        int WriteSignificandLittleEndian(byte[] destination, int startIndex)
        {
            if (!TryWriteSignificandLittleEndian(destination.AsSpan(startIndex), out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        /// <summary>Writes the current significand, in little-endian format, to a given span.</summary>
        /// <param name="destination">The span to which the current significand should be written.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        int WriteSignificandLittleEndian(Span<byte> destination)
        {
            if (!TryWriteSignificandLittleEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }
    }
}
