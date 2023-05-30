// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Text.Unicode;

namespace System.Globalization
{
    internal static partial class Ordinal
    {
        internal static int CompareStringIgnoreCaseUtf8(ref byte strA, int lengthA, ref byte strB, int lengthB)
        {
            int length = Math.Min(lengthA, lengthB);
            int range = length;

            ref byte charA = ref strA;
            ref byte charB = ref strB;

            const byte maxChar = 0x7F;

            while ((length != 0) && (charA <= maxChar) && (charB <= maxChar))
            {
                // Ordinal equals or lowercase equals if the result ends up in the a-z range
                if (charA == charB ||
                    ((charA | 0x20) == (charB | 0x20) && char.IsAsciiLetter((char)charA)))
                {
                    length--;
                    charA = ref Unsafe.Add(ref charA, 1);
                    charB = ref Unsafe.Add(ref charB, 1);
                }
                else
                {
                    int currentA = charA;
                    int currentB = charB;

                    // Uppercase both chars if needed
                    if (char.IsAsciiLetterLower((char)charA))
                    {
                        currentA -= 0x20;
                    }
                    if (char.IsAsciiLetterLower((char)charB))
                    {
                        currentB -= 0x20;
                    }

                    // Return the (case-insensitive) difference between them.
                    return currentA - currentB;
                }
            }

            if (length == 0)
            {
                return lengthA - lengthB;
            }

            range -= length;

            return CompareStringIgnoreCaseNonAsciiUtf8(ref charA, lengthA - range, ref charB, lengthB - range);
        }

        internal static int CompareStringIgnoreCaseNonAsciiUtf8(ref byte strA, int lengthA, ref byte strB, int lengthB)
        {
            // NLS/ICU doesn't provide native UTF-8 support so we need to convert to UTF-16 and compare that way

            ReadOnlySpan<char> stringAUtf16 = Encoding.Unicode.GetString(MemoryMarshal.CreateReadOnlySpan(ref strA, lengthA));
            ReadOnlySpan<char> stringBUtf16 = Encoding.Unicode.GetString(MemoryMarshal.CreateReadOnlySpan(ref strB, lengthB));

            return CompareStringIgnoreCaseNonAscii(
                ref MemoryMarshal.GetReference(stringAUtf16), stringAUtf16.Length,
                ref MemoryMarshal.GetReference(stringBUtf16), stringBUtf16.Length
            );
        }

        private static bool EqualsIgnoreCaseUtf8_Vector128(ref byte charA, ref byte charB, int length)
        {
            Debug.Assert(length >= Vector128<byte>.Count);
            Debug.Assert(Vector128.IsHardwareAccelerated);

            nuint lengthU = (nuint)length;
            nuint lengthToExamine = lengthU - (nuint)Vector128<byte>.Count;
            nuint i = 0;
            Vector128<byte> vec1;
            Vector128<byte> vec2;
            do
            {
                vec1 = Vector128.LoadUnsafe(ref charA, i);
                vec2 = Vector128.LoadUnsafe(ref charB, i);

                if (!Utf8Utility.AllBytesInVector128AreAscii(vec1 | vec2))
                {
                    goto NON_ASCII;
                }

                if (!Utf8Utility.Vector128OrdinalIgnoreCaseAscii(vec1, vec2))
                {
                    return false;
                }

                i += (nuint)Vector128<byte>.Count;
            } while (i <= lengthToExamine);

            // Use scalar path for trailing elements
            return i == lengthU || EqualsIgnoreCaseUtf8(ref Unsafe.Add(ref charA, i), ref Unsafe.Add(ref charB, i), (int)(lengthU - i));

        NON_ASCII:
            if (Utf8Utility.AllBytesInVector128AreAscii(vec1) || Utf8Utility.AllBytesInVector128AreAscii(vec2))
            {
                // No need to use the fallback if one of the inputs is full-ASCII
                return false;
            }

            // Fallback for Non-ASCII inputs
            return CompareStringIgnoreCaseUtf8(
                ref Unsafe.Add(ref charA, i), (int)(lengthU - i),
                ref Unsafe.Add(ref charB, i), (int)(lengthU - i)
            ) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsIgnoreCaseUtf8(ref byte charA, ref byte charB, int length)
        {
            if (!Vector128.IsHardwareAccelerated || length < Vector128<byte>.Count)
            {
                return EqualsIgnoreCaseUtf8_Scalar(ref charA, ref charB, length);
            }

            return EqualsIgnoreCaseUtf8_Vector128(ref charA, ref charB, length);
        }

        internal static bool EqualsIgnoreCaseUtf8_Scalar(ref byte charA, ref byte charB, int length)
        {
            IntPtr byteOffset = IntPtr.Zero;

#if TARGET_64BIT
            ulong valueAu64 = 0;
            ulong valueBu64 = 0;
            // Read 8 chars (64 bits) at a time from each string
            while ((uint)length >= 8)
            {
                valueAu64 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref charA, byteOffset));
                valueBu64 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref charB, byteOffset));

                // A 32-bit test - even with the bit-twiddling here - is more efficient than a 64-bit test.
                ulong temp = valueAu64 | valueBu64;
                if (!Utf8Utility.AllBytesInUInt32AreAscii((uint)temp | (uint)(temp >> 32)))
                {
                    goto NonAscii64; // one of the inputs contains non-ASCII data
                }

                // Generally, the caller has likely performed a first-pass check that the input strings
                // are likely equal. Consider a dictionary which computes the hash code of its key before
                // performing a proper deep equality check of the string contents. We want to optimize for
                // the case where the equality check is likely to succeed, which means that we want to avoid
                // branching within this loop unless we're about to exit the loop, either due to failure or
                // due to us running out of input data.

                if (!Utf8Utility.UInt64OrdinalIgnoreCaseAscii(valueAu64, valueBu64))
                {
                    return false;
                }

                byteOffset += 8;
                length -= 8;
            }
#endif
            uint valueAu32 = 0;
            uint valueBu32 = 0;
            // Read 4 chars (32 bits) at a time from each string
#if TARGET_64BIT
            if ((uint)length >= 4)
#else
            while ((uint)length >= 4)
#endif
            {
                valueAu32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref charA, byteOffset));
                valueBu32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref charB, byteOffset));

                if (!Utf8Utility.AllBytesInUInt32AreAscii(valueAu32 | valueBu32))
                {
                    goto NonAscii32; // one of the inputs contains non-ASCII data
                }

                // Generally, the caller has likely performed a first-pass check that the input strings
                // are likely equal. Consider a dictionary which computes the hash code of its key before
                // performing a proper deep equality check of the string contents. We want to optimize for
                // the case where the equality check is likely to succeed, which means that we want to avoid
                // branching within this loop unless we're about to exit the loop, either due to failure or
                // due to us running out of input data.

                if (!Utf8Utility.UInt32OrdinalIgnoreCaseAscii(valueAu32, valueBu32))
                {
                    return false;
                }

                byteOffset += 4;
                length -= 4;
            }

            if (length != 0)
            {
                // We have 1, 2, or 3 bytes remaining. We want to backtrack
                // so we read exactly 4 bytes and then do one final iteration.

                Debug.Assert(length <= 3);
                int backtrack = 4 - length;

                length += backtrack;
                byteOffset -= backtrack;

                valueAu32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref charA, byteOffset));
                valueBu32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref charB, byteOffset));

                if (!Utf8Utility.AllBytesInUInt32AreAscii(valueAu32 | valueBu32))
                {
                    goto NonAscii32; // one of the inputs contains non-ASCII data
                }

                if (valueAu32 == valueBu32)
                {
                    return true; // exact match
                }

                if (!Utf8Utility.UInt32OrdinalIgnoreCaseAscii(valueAu32, valueBu32))
                {
                    return false;
                }

                byteOffset += 4;
                length -= 4;
            }

            Debug.Assert(length == 0);
            return true;

        NonAscii32:
            // Both values have to be non-ASCII to use the slow fallback, in case if one of them is not we return false
            if (Utf8Utility.AllBytesInUInt32AreAscii(valueAu32) || Utf8Utility.AllBytesInUInt32AreAscii(valueBu32))
            {
                return false;
            }
            goto NonAscii;

#if TARGET_64BIT
        NonAscii64:
            // Both values have to be non-ASCII to use the slow fallback, in case if one of them is not we return false
            if (Utf8Utility.AllBytesInUInt64AreAscii(valueAu64) || Utf8Utility.AllBytesInUInt64AreAscii(valueBu64))
            {
                return false;
            }
#endif
        NonAscii:
            // The non-ASCII case is factored out into its own helper method so that the JIT
            // doesn't need to emit a complex prolog for its caller (this method).
            return CompareStringIgnoreCaseUtf8(ref Unsafe.AddByteOffset(ref charA, byteOffset), length, ref Unsafe.AddByteOffset(ref charB, byteOffset), length) == 0;
        }
    }
}
