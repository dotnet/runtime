// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Binary;

/// <summary>Provides downlevel polyfills for static methods on <see cref="BinaryPrimitives"/>.</summary>
internal static partial class BinaryPrimitivesPolyfills
{
    extension(BinaryPrimitives)
    {
        public static float ReadSingleBigEndian(ReadOnlySpan<byte> source) =>
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(source));

        public static double ReadDoubleBigEndian(ReadOnlySpan<byte> source) =>
            BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(source));

        public static void WriteSingleBigEndian(Span<byte> destination, float value) =>
            BinaryPrimitives.WriteInt32BigEndian(destination, BitConverter.SingleToInt32Bits(value));

        public static void WriteDoubleBigEndian(Span<byte> destination, double value) =>
            BinaryPrimitives.WriteInt64BigEndian(destination, BitConverter.DoubleToInt64Bits(value));

        public static void WriteSingleLittleEndian(Span<byte> destination, float value) =>
            BinaryPrimitives.WriteInt32LittleEndian(destination, BitConverter.SingleToInt32Bits(value));

        public static void WriteDoubleLittleEndian(Span<byte> destination, double value) =>
            BinaryPrimitives.WriteInt64LittleEndian(destination, BitConverter.DoubleToInt64Bits(value));
    }
}
