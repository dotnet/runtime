// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers.Binary;

/// <summary>Provides downlevel polyfills for static methods on <see cref="BinaryPrimitives"/>.</summary>
internal static class BinaryPrimitivesPolyfills
{
    [StructLayout(LayoutKind.Explicit)]
    private struct SingleUInt32Bits
    {
        [FieldOffset(0)]
        public float Single;

        [FieldOffset(0)]
        public uint UInt32;
    }

    extension(BinaryPrimitives)
    {
        public static void WriteDoubleLittleEndian(Span<byte> destination, double value) =>
            BinaryPrimitives.WriteUInt64LittleEndian(destination, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));

        public static void WriteSingleLittleEndian(Span<byte> destination, float value) =>
            BinaryPrimitives.WriteUInt32LittleEndian(destination, new SingleUInt32Bits { Single = value }.UInt32);

        public static void ReverseEndianness(ReadOnlySpan<short> source, Span<short> destination)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, source.Length);

            if (source.Overlaps(destination, out int elementOffset) && elementOffset > 0)
            {
                for (int i = source.Length - 1; i >= 0; i--)
                {
                    destination[i] = BinaryPrimitives.ReverseEndianness(source[i]);
                }
            }
            else
            {
                for (int i = 0; i < source.Length; i++)
                {
                    destination[i] = BinaryPrimitives.ReverseEndianness(source[i]);
                }
            }
        }

        public static void ReverseEndianness(ReadOnlySpan<int> source, Span<int> destination)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, source.Length);

            if (source.Overlaps(destination, out int elementOffset) && elementOffset > 0)
            {
                for (int i = source.Length - 1; i >= 0; i--)
                {
                    destination[i] = BinaryPrimitives.ReverseEndianness(source[i]);
                }
            }
            else
            {
                for (int i = 0; i < source.Length; i++)
                {
                    destination[i] = BinaryPrimitives.ReverseEndianness(source[i]);
                }
            }
        }

        public static void ReverseEndianness(ReadOnlySpan<long> source, Span<long> destination)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, source.Length);

            if (source.Overlaps(destination, out int elementOffset) && elementOffset > 0)
            {
                for (int i = source.Length - 1; i >= 0; i--)
                {
                    destination[i] = BinaryPrimitives.ReverseEndianness(source[i]);
                }
            }
            else
            {
                for (int i = 0; i < source.Length; i++)
                {
                    destination[i] = BinaryPrimitives.ReverseEndianness(source[i]);
                }
            }
        }
    }
}
