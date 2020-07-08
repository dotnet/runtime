// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Formats.Cbor
{
    // Temporarily implements missing APIs for System.Half
    // Remove class once https://github.com/dotnet/runtime/issues/38288 has been addressed
    internal static class HalfHelpers
    {
        public const int SizeOfHalf = sizeof(short);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half ReadHalfBigEndian(ReadOnlySpan<byte> source)
        {
            return BitConverter.IsLittleEndian ?
                Int16BitsToHalf(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<short>(source))) :
                MemoryMarshal.Read<Half>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalfBigEndian(Span<byte> destination, Half value)
        {
            if (BitConverter.IsLittleEndian)
            {
                short tmp = BinaryPrimitives.ReverseEndianness(HalfToInt16Bits(value));
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe short HalfToInt16Bits(Half value)
        {
            return *((short*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Half Int16BitsToHalf(short value)
        {
            return *(Half*)&value;
        }
    }
}
