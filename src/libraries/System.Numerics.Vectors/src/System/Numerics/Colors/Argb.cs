// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.Numerics.Colors
{
    /// <summary>
    /// Static methods for <see cref="Argb{T}"/> color.
    /// </summary>
    public static class Argb
    {
        /// <summary>
        /// Creates an <see cref="Argb{T}"/> color from a big-endian <see langword="uint"/> representing an ARGB color.
        /// </summary>
        /// <param name="color">A big-endian <see langword="uint"/> representing an ARGB color.</param>
        /// <returns>A new instance of the <see cref="Argb{T}"/> color.</returns>
        [System.CLSCompliantAttribute(false)]
        public static Argb<byte> CreateBigEndian(uint color)
        {
            if (BitConverter.IsLittleEndian)
                color = BinaryPrimitives.ReverseEndianness(color);
            return Unsafe.As<uint, Argb<byte>>(ref color);
        }

        /// <summary>
        /// Creates an <see cref="Argb{T}"/> color from a little-endian <see langword="uint"/> representing an ARGB
        /// color.
        /// </summary>
        /// <param name="color">A little-endian <see langword="uint"/> representing an ARGB color.</param>
        /// <returns>A new instance of the <see cref="Argb{T}"/> color.</returns>
        [System.CLSCompliantAttribute(false)]
        public static Argb<byte> CreateLittleEndian(uint color)
        {
            if (!BitConverter.IsLittleEndian)
                color = BinaryPrimitives.ReverseEndianness(color);
            return Unsafe.As<uint, Argb<byte>>(ref color);
        }

        /// <summary>
        /// Converts the specified <see cref="Argb{T}"/> color to a big-endian <see langword="uint"/> representing an
        /// ARGB color.
        /// </summary>
        /// <param name="color">The <see cref="Argb{T}"/> color to convert.</param>
        /// <returns>A big-endian <see langword="uint"/> representing an ARGB color.</returns>
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32BigEndian(this Argb<byte> color)
        {
            uint bits = Unsafe.As<Argb<byte>, uint>(ref color);
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(bits) : bits;
        }

        /// <summary>
        /// Converts the specified <see cref="Argb{T}"/> color to a little-endian <see langword="uint"/> representing
        /// an ARGB color.
        /// </summary>
        /// <param name="color">The <see cref="Argb{T}"/> color to convert.</param>
        /// <returns>A little-endian <see langword="uint"/> representing an ARGB color.</returns>
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32LittleEndian(this Argb<byte> color)
        {
            uint bits = Unsafe.As<Argb<byte>, uint>(ref color);
            return BitConverter.IsLittleEndian ? bits : BinaryPrimitives.ReverseEndianness(bits);
        }
    }
}
