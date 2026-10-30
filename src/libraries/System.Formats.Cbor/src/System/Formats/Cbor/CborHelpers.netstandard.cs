// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Formats.Cbor
{
    internal static partial class CborHelpers
    {
        public static BigInteger CreateBigIntegerFromUnsignedBigEndianBytes(byte[] bigEndianBytes)
        {
            if (bigEndianBytes.Length == 0)
            {
                return new BigInteger(bigEndianBytes);
            }

            byte[] temp;
            if ((bigEndianBytes[0] & 0x80) != 0) // Is negative?
            {
                // To prevent positive values from being misinterpreted as negative values,
                // you can add a zero-byte value to the most significant side of the array.
                // Right in this case as it is Big-endian.
                var bytesPlusOne = new byte[bigEndianBytes.Length + 1];
                bigEndianBytes.CopyTo(bytesPlusOne.AsSpan(1));
                temp = bytesPlusOne;
            }
            else
            {
                temp = bigEndianBytes;
            }

            // Reverse endianness
            temp.AsSpan().Reverse();

            return new BigInteger(temp);
        }

        public static byte[] CreateUnsignedBigEndianBytesFromBigInteger(BigInteger value)
        {
            byte[] littleEndianBytes = value.ToByteArray();

            if (littleEndianBytes.Length == 1)
            {
                return littleEndianBytes;
            }

            Span<byte> bytesAsSpan = littleEndianBytes;
            bytesAsSpan.Reverse();

            int start = 0;
            for (int i = 0; i < bytesAsSpan.Length; i++)
            {
                if (bytesAsSpan[i] == 0x00)
                {
                    start++;
                }
                else
                {
                    break;
                }
            }

            Debug.Assert(start <= 1); // If there is a case where we trim more than one byte, we want to add it to our tests.

            return start == 0 ? littleEndianBytes : bytesAsSpan.Slice(start).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadHalfBigEndian(ReadOnlySpan<byte> source)
            => BinaryPrimitives.ReadUInt16BigEndian(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalfBigEndian(Span<byte> destination, ushort value)
            => BinaryPrimitives.WriteUInt16BigEndian(destination, value);
    }
}
