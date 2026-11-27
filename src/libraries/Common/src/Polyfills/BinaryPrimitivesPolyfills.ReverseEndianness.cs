// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Binary;

/// <summary>Provides downlevel polyfills for reversing the endianness of spans.</summary>
internal static partial class BinaryPrimitivesPolyfills
{
    extension(BinaryPrimitives)
    {
        public static void ReverseEndianness(ReadOnlySpan<short> source, Span<short> destination)
        {
            source.CopyTo(destination);
            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = BinaryPrimitives.ReverseEndianness(destination[i]);
            }
        }

        public static void ReverseEndianness(ReadOnlySpan<int> source, Span<int> destination)
        {
            source.CopyTo(destination);
            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = BinaryPrimitives.ReverseEndianness(destination[i]);
            }
        }

        public static void ReverseEndianness(ReadOnlySpan<long> source, Span<long> destination)
        {
            source.CopyTo(destination);
            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = BinaryPrimitives.ReverseEndianness(destination[i]);
            }
        }
    }
}
