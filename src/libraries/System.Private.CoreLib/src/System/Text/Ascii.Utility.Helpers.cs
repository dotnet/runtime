// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Text
{
    public static partial class Ascii
    {
        /// <summary>
        /// A mask which selects only the high bit of each byte of the given <see cref="uint"/>.
        /// </summary>
        private const uint UInt32HighBitsOnlyMask = 0x80808080u;

        /// <summary>
        /// A mask which selects only the high bit of each byte of the given <see cref="ulong"/>.
        /// </summary>
        private const ulong UInt64HighBitsOnlyMask = 0x80808080_80808080ul;

        /// <summary>
        /// Returns <see langword="true"/> iff all bytes in <paramref name="value"/> are ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllBytesInUInt32AreAscii(uint value)
        {
            // If the high bit of any byte is set, that byte is non-ASCII.

            return (value & UInt32HighBitsOnlyMask) == 0;
        }

        /// <summary>
        /// Given a DWORD which represents a four-byte buffer read in machine endianness, and which
        /// the caller has asserted contains a non-ASCII byte *somewhere* in the data, counts the
        /// number of consecutive ASCII bytes starting from the beginning of the buffer. Returns
        /// a value 0 - 3, inclusive. (The caller is responsible for ensuring that the buffer doesn't
        /// contain all-ASCII data.)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(uint value)
        {
            Debug.Assert(!AllBytesInUInt32AreAscii(value), "Caller shouldn't provide an all-ASCII value.");

            if (BitConverter.IsLittleEndian)
            {
                return (uint)BitOperations.TrailingZeroCount(value & UInt32HighBitsOnlyMask) >> 3;
            }
            else
            {
                // Couldn't use tzcnt, use specialized software fallback.
                // The 'allBytesUpToNowAreAscii' DWORD uses bit twiddling to hold a 1 or a 0 depending
                // on whether all processed bytes were ASCII. Then we accumulate all of the
                // results to calculate how many consecutive ASCII bytes are present.

                value = ~value;

                // BinaryPrimitives.ReverseEndianness is only implemented as an intrinsic on
                // little-endian platforms, so using it in this big-endian path would be too
                // expensive. Instead we'll just change how we perform the shifts.

                // Read first byte
                value = BitOperations.RotateLeft(value, 1);
                uint allBytesUpToNowAreAscii = value & 1;
                uint numAsciiBytes = allBytesUpToNowAreAscii;

                // Read second byte
                value = BitOperations.RotateLeft(value, 8);
                allBytesUpToNowAreAscii &= value;
                numAsciiBytes += allBytesUpToNowAreAscii;

                // Read third byte
                value = BitOperations.RotateLeft(value, 8);
                allBytesUpToNowAreAscii &= value;
                numAsciiBytes += allBytesUpToNowAreAscii;

                return numAsciiBytes;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WritePairUnaligned<T>(ref byte destination, (T Lower, T Upper) source)
            where T : unmanaged
        {
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(ref destination, source.Lower);
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, sizeof(T)), source.Upper);
            }
            else
            {
                Unsafe.WriteUnaligned(ref destination, source.Upper);
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, sizeof(T)), source.Lower);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteUnalignedWidening(ref ushort destination, uint value)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                Vector128<byte> vecNarrow = AdvSimd.DuplicateToVector128(value).AsByte();
                Vector128<ulong> vecWide = AdvSimd.Arm64.ZipLow(vecNarrow, Vector128<byte>.Zero).AsUInt64();
                Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref destination), vecWide.ToScalar());
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                Vector128<byte> vecNarrow = Vector128.CreateScalar(value).AsByte();
                Vector128<ulong> vecWide = Vector128.WidenLower(vecNarrow).AsUInt64();
                Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref destination), vecWide.ToScalar());
            }
            else if (UIntPtr.Size >= sizeof(ulong))
            {
                ulong temp = value;
                temp |= temp << 16;
                temp &= 0x0000FFFF_0000FFFFuL;
                temp |= temp << 8;
                temp &= 0x00FF00FF_00FF00FFuL;
                Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref destination), temp);
            }
            else if (BitConverter.IsLittleEndian)
            {
                WriteUnalignedWideningLower(ref destination, value);
                WriteUnalignedWideningUpper(ref Unsafe.Add(ref destination, sizeof(uint) / sizeof(ushort)), value);
            }
            else
            {
                WriteUnalignedWideningUpper(ref destination, value);
                WriteUnalignedWideningLower(ref Unsafe.Add(ref destination, sizeof(uint) / sizeof(ushort)), value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteUnalignedWideningLower(ref ushort destination, uint value)
        {
            uint lower = (ushort)value;
            lower |= value << 8;
            lower &= 0x00FF00FFu;
            Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref destination), lower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteUnalignedWideningUpper(ref ushort destination, uint value)
        {
            uint upper = value >> 16;
            upper |= upper << 8;
            upper &= 0x00FF00FFu;
            Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref destination), upper);
        }
    }
}
