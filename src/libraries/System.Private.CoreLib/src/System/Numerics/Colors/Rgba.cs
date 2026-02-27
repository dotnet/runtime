// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.Numerics.Colors
{
    /// <summary>
    /// Static methods for <see cref="Rgba{T}"/> color.
    /// </summary>
    public static class Rgba
    {
        /// <summary>
        /// Creates an <see cref="Rgba{T}"/> color from a big-endian <see langword="uint"/> representing an RGBA color.
        /// </summary>
        /// <param name="color">A big-endian <see langword="uint"/> representing an RGBA color.</param>
        /// <returns>A new instance of the <see cref="Rgba{T}"/> color.</returns>
        /// <remarks>The value is expected in <c>0xRRGGBBAA</c> byte order.</remarks>
        [CLSCompliant(false)]
        public static Rgba<byte> CreateBigEndian(uint color)
        {
            if (BitConverter.IsLittleEndian)
            {
                color = BinaryPrimitives.ReverseEndianness(color);
            }

            return Unsafe.BitCast<uint, Rgba<byte>>(color);
        }

        /// <summary>
        /// Creates an <see cref="Rgba{T}"/> color from a little-endian <see langword="uint"/> representing an RGBA
        /// color.
        /// </summary>
        /// <param name="color">A little-endian <see langword="uint"/> representing an RGBA color.</param>
        /// <returns>A new instance of the <see cref="Rgba{T}"/> color.</returns>
        /// <remarks>The value is expected in <c>0xAABBGGRR</c> byte order.</remarks>
        [CLSCompliant(false)]
        public static Rgba<byte> CreateLittleEndian(uint color)
        {
            if (!BitConverter.IsLittleEndian)
            {
                color = BinaryPrimitives.ReverseEndianness(color);
            }

            return Unsafe.BitCast<uint, Rgba<byte>>(color);
        }

        /// <summary>
        /// Converts the specified <see cref="Rgba{T}"/> color to a big-endian <see langword="uint"/> representing an
        /// RGBA color.
        /// </summary>
        /// <param name="color">The <see cref="Rgba{T}"/> color to convert.</param>
        /// <returns>A big-endian <see langword="uint"/> representing an RGBA color.</returns>
        /// <remarks>The returned value is in <c>0xRRGGBBAA</c> byte order.</remarks>
        [CLSCompliant(false)]
        public static uint ToUInt32BigEndian(this Rgba<byte> color)
        {
            uint bits = Unsafe.BitCast<Rgba<byte>, uint>(color);

            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(bits) : bits;
        }

        /// <summary>
        /// Converts the specified <see cref="Rgba{T}"/> color to a little-endian <see langword="uint"/> representing
        /// an RGBA color.
        /// </summary>
        /// <param name="color">The <see cref="Rgba{T}"/> color to convert.</param>
        /// <returns>A little-endian <see langword="uint"/> representing an RGBA color.</returns>
        /// <remarks>The returned value is in <c>0xAABBGGRR</c> byte order.</remarks>
        [CLSCompliant(false)]
        public static uint ToUInt32LittleEndian(this Rgba<byte> color)
        {
            uint bits = Unsafe.BitCast<Rgba<byte>, uint>(color);

            return BitConverter.IsLittleEndian ? bits : BinaryPrimitives.ReverseEndianness(bits);
        }
    }
}
