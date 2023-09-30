// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        internal static bool EqualsStringIgnoreCaseUtf8(ref byte strA, int lengthA, ref byte strB, int lengthB)
        {
            // NOTE: Two UTF-8 inputs of different length might compare as equal under
            // the OrdinalIgnoreCase comparer. This is distinct from UTF-16, where the
            // inputs being different length will mean that they can never compare as
            // equal under an OrdinalIgnoreCase comparer.

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
                    return false;
                }
            }

            if (length == 0)
            {
                // Success if we reached the end of both sequences
                return lengthA == lengthB;
            }

            range -= length;
            return EqualsStringIgnoreCaseNonAsciiUtf8(ref charA, lengthA - range, ref charB, lengthB - range);
        }

        internal static bool EqualsStringIgnoreCaseNonAsciiUtf8(ref byte strA, int lengthA, ref byte strB, int lengthB)
        {
            // NLS/ICU doesn't provide native UTF-8 support so we need to do our own corresponding ordinal comparison

            ReadOnlySpan<byte> spanA = MemoryMarshal.CreateReadOnlySpan(ref strA, lengthA);
            ReadOnlySpan<byte> spanB = MemoryMarshal.CreateReadOnlySpan(ref strB, lengthB);

            do
            {
                OperationStatus statusA = Rune.DecodeFromUtf8(spanA, out Rune runeA, out int bytesConsumedA);
                OperationStatus statusB = Rune.DecodeFromUtf8(spanB, out Rune runeB, out int bytesConsumedB);

                if (statusA != statusB)
                {
                    // OperationStatus don't match; fail immediately
                    return false;
                }

                if (statusA == OperationStatus.Done)
                {
                    if (Rune.ToUpperInvariant(runeA) != Rune.ToUpperInvariant(runeB))
                    {
                        // Runes don't match when ignoring case; fail immediately
                        return false;
                    }
                }
                else if (!spanA.Slice(0, bytesConsumedA).SequenceEqual(spanB.Slice(0, bytesConsumedB)))
                {
                    // OperationStatus match, but bytesConsumed or the sequence of bytes consumed do not; fail immediately
                    return false;
                }

                // The current runes or invalid byte sequences matched, slice and continue.
                // We'll exit the loop when the entirety of both spans have been processed.
                //
                // In the scenario where one buffer is empty before the other, we'll end up
                // with that span returning OperationStatus.NeedsMoreData and bytesConsumed=0
                // while the other span will return a different OperationStatus or different
                // bytesConsumed and thus fail the operation.

                spanA = spanA.Slice(bytesConsumedA);
                spanB = spanB.Slice(bytesConsumedB);
            }
            while ((spanA.Length | spanB.Length) != 0);

            return true;
        }

        private static bool EqualsIgnoreCaseUtf8_Vector128(ref byte charA, int lengthA, ref byte charB, int lengthB)
        {
            Debug.Assert(lengthA >= Vector128<byte>.Count);
            Debug.Assert(lengthB >= Vector128<byte>.Count);
            Debug.Assert(Vector128.IsHardwareAccelerated);

            nuint lengthU = Math.Min((uint)lengthA, (uint)lengthB);
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
            }
            while (i <= lengthToExamine);

            if (i == lengthU)
            {
                // success if we reached the end of both sequences
                return lengthA == lengthB;
            }

            // Use scalar path for trailing elements
            return EqualsIgnoreCaseUtf8_Scalar(ref Unsafe.Add(ref charA, i), (int)(lengthU - i), ref Unsafe.Add(ref charB, i), (int)(lengthU - i));

        NON_ASCII:
            if (Utf8Utility.AllBytesInVector128AreAscii(vec1) || Utf8Utility.AllBytesInVector128AreAscii(vec2))
            {
                // No need to use the fallback if one of the inputs is full-ASCII
                return false;
            }

            // Fallback for Non-ASCII inputs
            return EqualsStringIgnoreCaseUtf8(
                ref Unsafe.Add(ref charA, i), lengthA - (int)i,
                ref Unsafe.Add(ref charB, i), lengthB - (int)i
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsIgnoreCaseUtf8(ref byte charA, int lengthA, ref byte charB, int lengthB)
        {
            if (!Vector128.IsHardwareAccelerated || (lengthA < Vector128<byte>.Count) || (lengthB < Vector128<byte>.Count))
            {
                return EqualsIgnoreCaseUtf8_Scalar(ref charA, lengthA, ref charB, lengthB);
            }

            return EqualsIgnoreCaseUtf8_Vector128(ref charA, lengthA, ref charB, lengthB);
        }

        internal static bool EqualsIgnoreCaseUtf8_Scalar(ref byte charA, int lengthA, ref byte charB, int lengthB)
        {
            IntPtr byteOffset = IntPtr.Zero;

            int length = Math.Min(lengthA, lengthB);
            int range = length;

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
                    // one of the inputs contains non-ASCII data
                    goto NonAscii64;
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
                    // one of the inputs contains non-ASCII data
                    goto NonAscii32;
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
                // We have 1, 2, or 3 bytes remaining. We can't do anything fancy
                // like backtracking since we could have only had 1-3 bytes. So,
                // instead we'll do 1 or 2 reads to get all 3 bytes. Endianness
                // doesn't matter here since we only compare if all bytes are ascii
                // and the ordering will be consistent between the two comparisons

                if (length == 3)
                {
                    valueAu32 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref charA, byteOffset));
                    valueBu32 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref charB, byteOffset));

                    byteOffset += 2;

                    valueAu32 |= (uint)(Unsafe.AddByteOffset(ref charA, byteOffset) << 16);
                    valueBu32 |= (uint)(Unsafe.AddByteOffset(ref charB, byteOffset) << 16);
                }
                else if (length == 2)
                {
                    valueAu32 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref charA, byteOffset));
                    valueBu32 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref charB, byteOffset));
                }
                else
                {
                    Debug.Assert(length == 1);

                    valueAu32 = Unsafe.AddByteOffset(ref charA, byteOffset);
                    valueBu32 = Unsafe.AddByteOffset(ref charB, byteOffset);
                }

                if (!Utf8Utility.AllBytesInUInt32AreAscii(valueAu32 | valueBu32))
                {
                    // one of the inputs contains non-ASCII data
                    goto NonAscii32;
                }

                if (lengthA != lengthB)
                {
                    // Failure if we reached the end of one, but not both sequences
                    return false;
                }

                if (valueAu32 == valueBu32)
                {
                    // exact match
                    return true;
                }

                return Utf8Utility.UInt32OrdinalIgnoreCaseAscii(valueAu32, valueBu32);
            }

            Debug.Assert(length == 0);
            return lengthA == lengthB;

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
            range -= length;

            // The non-ASCII case is factored out into its own helper method so that the JIT
            // doesn't need to emit a complex prolog for its caller (this method).
            return EqualsStringIgnoreCaseUtf8(ref Unsafe.AddByteOffset(ref charA, byteOffset), lengthA - range, ref Unsafe.AddByteOffset(ref charB, byteOffset), lengthB - range);
        }

        internal static bool StartsWithStringIgnoreCaseUtf8(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
        {
            // NOTE: Two UTF-8 inputs of different length might compare as equal under
            // the OrdinalIgnoreCase comparer. This is distinct from UTF-16, where the
            // inputs being different length will mean that they can never compare as
            // equal under an OrdinalIgnoreCase comparer.

            int length = Math.Min(sourceLength, prefixLength);
            int range = length;

            const byte maxChar = 0x7F;

            while ((length != 0) && (source <= maxChar) && (prefix <= maxChar))
            {
                // Ordinal equals or lowercase equals if the result ends up in the a-z range
                if (source == prefix ||
                    ((source | 0x20) == (prefix | 0x20) && char.IsAsciiLetter((char)source)))
                {
                    length--;
                    source = ref Unsafe.Add(ref source, 1);
                    prefix = ref Unsafe.Add(ref prefix, 1);
                }
                else
                {
                    return false;
                }
            }

            if (length == 0)
            {
                // Success if we reached the end of the prefix
                return prefixLength == 0;
            }

            range -= length;
            return StartsWithStringIgnoreCaseNonAsciiUtf8(ref source, sourceLength - range, ref prefix, prefixLength - range);
        }

        internal static bool StartsWithStringIgnoreCaseNonAsciiUtf8(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
        {
            // NLS/ICU doesn't provide native UTF-8 support so we need to do our own corresponding ordinal comparison

            ReadOnlySpan<byte> spanA = MemoryMarshal.CreateReadOnlySpan(ref source, sourceLength);
            ReadOnlySpan<byte> spanB = MemoryMarshal.CreateReadOnlySpan(ref prefix, prefixLength);

            do
            {
                OperationStatus statusA = Rune.DecodeFromUtf8(spanA, out Rune runeA, out int bytesConsumedA);
                OperationStatus statusB = Rune.DecodeFromUtf8(spanB, out Rune runeB, out int bytesConsumedB);

                if (statusA != statusB)
                {
                    // OperationStatus don't match; fail immediately
                    return false;
                }

                if (statusA == OperationStatus.Done)
                {
                    if (Rune.ToUpperInvariant(runeA) != Rune.ToUpperInvariant(runeB))
                    {
                        // Runes don't match when ignoring case; fail immediately
                        return false;
                    }
                }
                else if (!spanA.Slice(0, bytesConsumedA).SequenceEqual(spanB.Slice(0, bytesConsumedB)))
                {
                    // OperationStatus match, but bytesConsumed or the sequence of bytes consumed do not; fail immediately
                    return false;
                }

                // The current runes or invalid byte sequences matched, slice and continue.
                // We'll exit the loop when the entirety of spanB has been processed.
                //
                // In the scenario where spanB is empty before spanB, we'll end up with that
                // span returning OperationStatus.NeedsMoreData and bytesConsumed=0 while spanB
                // will return a different OperationStatus or different bytesConsumed and thus
                // fail the operation.

                spanA = spanA.Slice(bytesConsumedA);
                spanB = spanB.Slice(bytesConsumedB);
            }
            while (spanB.Length != 0);

            return true;
        }

        private static bool StartsWithIgnoreCaseUtf8_Vector128(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
        {
            Debug.Assert(sourceLength >= Vector128<byte>.Count);
            Debug.Assert(prefixLength >= Vector128<byte>.Count);
            Debug.Assert(Vector128.IsHardwareAccelerated);

            nuint lengthU = Math.Min((uint)sourceLength, (uint)prefixLength);
            nuint lengthToExamine = lengthU - (nuint)Vector128<byte>.Count;

            nuint i = 0;

            Vector128<byte> vec1;
            Vector128<byte> vec2;

            do
            {
                vec1 = Vector128.LoadUnsafe(ref source, i);
                vec2 = Vector128.LoadUnsafe(ref prefix, i);

                if (!Utf8Utility.AllBytesInVector128AreAscii(vec1 | vec2))
                {
                    goto NON_ASCII;
                }

                if (!Utf8Utility.Vector128OrdinalIgnoreCaseAscii(vec1, vec2))
                {
                    return false;
                }

                i += (nuint)Vector128<byte>.Count;
            }
            while (i <= lengthToExamine);

            if (i == (uint)prefixLength)
            {
                // success if we reached the end of the prefix
                return true;
            }

            // Use scalar path for trailing elements
            return StartsWithIgnoreCaseUtf8_Scalar(ref Unsafe.Add(ref source, i), (int)(lengthU - i), ref Unsafe.Add(ref prefix, i), (int)(lengthU - i));

        NON_ASCII:
            if (Utf8Utility.AllBytesInVector128AreAscii(vec1) || Utf8Utility.AllBytesInVector128AreAscii(vec2))
            {
                // No need to use the fallback if one of the inputs is full-ASCII
                return false;
            }

            // Fallback for Non-ASCII inputs
            return StartsWithStringIgnoreCaseUtf8(
                ref Unsafe.Add(ref source, i), sourceLength - (int)i,
                ref Unsafe.Add(ref prefix, i), prefixLength - (int)i
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool StartsWithIgnoreCaseUtf8(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
        {
            if (!Vector128.IsHardwareAccelerated || (sourceLength < Vector128<byte>.Count) || (prefixLength < Vector128<byte>.Count))
            {
                return StartsWithIgnoreCaseUtf8_Scalar(ref source, sourceLength, ref prefix, prefixLength);
            }

            return StartsWithIgnoreCaseUtf8_Vector128(ref source, sourceLength, ref prefix, prefixLength);
        }

        internal static bool StartsWithIgnoreCaseUtf8_Scalar(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
        {
            IntPtr byteOffset = IntPtr.Zero;

            int length = Math.Min(sourceLength, prefixLength);
            int range = length;

#if TARGET_64BIT
            ulong valueAu64 = 0;
            ulong valueBu64 = 0;

            // Read 8 chars (64 bits) at a time from each string
            while ((uint)length >= 8)
            {
                valueAu64 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref source, byteOffset));
                valueBu64 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref prefix, byteOffset));

                // A 32-bit test - even with the bit-twiddling here - is more efficient than a 64-bit test.
                ulong temp = valueAu64 | valueBu64;

                if (!Utf8Utility.AllBytesInUInt32AreAscii((uint)temp | (uint)(temp >> 32)))
                {
                    // one of the inputs contains non-ASCII data
                    goto NonAscii64;
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
                valueAu32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref source, byteOffset));
                valueBu32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref prefix, byteOffset));

                if (!Utf8Utility.AllBytesInUInt32AreAscii(valueAu32 | valueBu32))
                {
                    // one of the inputs contains non-ASCII data
                    goto NonAscii32;
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
                // We have 1, 2, or 3 bytes remaining. We can't do anything fancy
                // like backtracking since we could have only had 1-3 bytes. So,
                // instead we'll do 1 or 2 reads to get all 3 bytes. Endianness
                // doesn't matter here since we only compare if all bytes are ascii
                // and the ordering will be consistent between the two comparisons

                if (length == 3)
                {
                    valueAu32 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref source, byteOffset));
                    valueBu32 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref prefix, byteOffset));

                    byteOffset += 2;

                    valueAu32 |= (uint)(Unsafe.AddByteOffset(ref source, byteOffset) << 16);
                    valueBu32 |= (uint)(Unsafe.AddByteOffset(ref prefix, byteOffset) << 16);
                }
                else if (length == 2)
                {
                    valueAu32 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref source, byteOffset));
                    valueBu32 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref prefix, byteOffset));
                }
                else
                {
                    Debug.Assert(length == 1);

                    valueAu32 = Unsafe.AddByteOffset(ref source, byteOffset);
                    valueBu32 = Unsafe.AddByteOffset(ref prefix, byteOffset);
                }

                if (!Utf8Utility.AllBytesInUInt32AreAscii(valueAu32 | valueBu32))
                {
                    goto NonAscii32; // one of the inputs contains non-ASCII data
                }

                if (range != prefixLength)
                {
                    // Failure if we didn't reach the end of the prefix
                    return false;
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
            return prefixLength <= sourceLength;

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
            range -= length;

            // The non-ASCII case is factored out into its own helper method so that the JIT
            // doesn't need to emit a complex prolog for its caller (this method).
            return StartsWithStringIgnoreCaseUtf8(ref Unsafe.AddByteOffset(ref source, byteOffset), sourceLength - range, ref Unsafe.AddByteOffset(ref prefix, byteOffset), prefixLength - range);
        }
    }
}
