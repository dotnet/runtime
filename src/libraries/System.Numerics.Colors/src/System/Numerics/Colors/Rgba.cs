// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.Numerics.Colors
{
#pragma warning disable CS3001 // Argument type is not CLS-compliant
#pragma warning disable CS3002 // Return type is not CLS-compliant

    /// <summary>
    /// Static methods for <see cref="Rgba{T}"/> color.
    /// </summary>
    public static class Rgba
    {
        /// <summary>
        /// Creates an <see cref="Rgba{T}"/> color from a big-endian <see langword="uint"/> representing an RGBA color.
        /// </summary>
        /// <param name="color">A big-endian <see langword="uint"/> representing an RGBA color.</param>
        /// <returns>An new instance of the <see cref="Rgba{T}"/> color.</returns>
        public static Rgba<byte> CreateBigEndian(uint color) => Unsafe.As<uint, Rgba<byte>>(ref color);

        /// <summary>
        /// Creates an <see cref="Rgba{T}"/> color from a little-endian <see langword="uint"/> representing an RGBA
        /// color.
        /// </summary>
        /// <param name="color">A little-endian <see langword="uint"/> representing an RGBA color.</param>
        /// <returns>An new instance of the <see cref="Rgba{T}"/> color.</returns>
        public static Rgba<byte> CreateLittleEndian(uint color)
        {
            color = BinaryPrimitives.ReverseEndianness(color);
            return Unsafe.As<uint, Rgba<byte>>(ref color);
        }

        /// <summary>
        /// Converts the specified <see cref="Rgba{T}"/> color to a big-endian <see langword="uint"/> representing an
        /// RGBA color.
        /// </summary>
        /// <param name="color">The <see cref="Rgba{T}"/> color to convert.</param>
        /// <returns>A big-endian <see langword="uint"/> representing an RGBA color.</returns>
        public static uint ToUInt32BigEndian(this Rgba<byte> color)
            => Unsafe.As<Rgba<byte>, uint>(ref color);

        /// <summary>
        /// Converts the specified <see cref="Rgba{T}"/> color to a little-endian <see langword="uint"/> representing
        /// an RGBA color.
        /// </summary>
        /// <param name="color">The <see cref="Rgba{T}"/> color to convert.</param>
        /// <returns>A little-endian <see langword="uint"/> representing an RGBA color.</returns>
        public static uint ToUInt32LittleEndian(this Rgba<byte> color)
            => BinaryPrimitives.ReverseEndianness(Unsafe.As<Rgba<byte>, uint>(ref color));
    }

#pragma warning restore CS3001 // Argument type is not CLS-compliant
#pragma warning restore CS3002 // Return type is not CLS-compliant
}
