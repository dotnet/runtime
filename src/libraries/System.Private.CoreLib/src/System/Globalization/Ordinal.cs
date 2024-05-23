// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Unicode;

namespace System.Globalization
{
    internal static partial class Ordinal
    {
        internal static int CompareStringIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB)
        {
            int length = Math.Min(lengthA, lengthB);
            int range = length;

            ref char charA = ref strA;
            ref char charB = ref strB;

            const char maxChar = (char)0x7F;

            while (length != 0 && charA <= maxChar && charB <= maxChar)
            {
                // Ordinal equals or lowercase equals if the result ends up in the a-z range
                if (charA == charB ||
                    ((charA | 0x20) == (charB | 0x20) && char.IsAsciiLetter(charA)))
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
                    if (char.IsAsciiLetterLower(charA))
                    {
                        currentA -= 0x20;
                    }
                    if (char.IsAsciiLetterLower(charB))
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

            return CompareStringIgnoreCaseNonAscii(ref charA, lengthA - range, ref charB, lengthB - range);
        }

        internal static int CompareStringIgnoreCaseNonAscii(ref char strA, int lengthA, ref char strB, int lengthB)
        {
            if (GlobalizationMode.Invariant)
            {
                return InvariantModeCasing.CompareStringIgnoreCase(ref strA, lengthA, ref strB, lengthB);
            }

            if (GlobalizationMode.UseNls)
            {
                return CompareInfo.NlsCompareStringOrdinalIgnoreCase(ref strA, lengthA, ref strB, lengthB);
            }

            return OrdinalCasing.CompareStringIgnoreCase(ref strA, lengthA, ref strB, lengthB);
        }

        private static bool EqualsIgnoreCase_Vector<TVector>(ref char charA, ref char charB, int length)
            where TVector : struct, ISimdVector<TVector, ushort>
        {
            Debug.Assert(length >= TVector.Count);

            nuint lengthU = (nuint)length;
            nuint lengthToExamine = lengthU - (nuint)TVector.Count;
            nuint i = 0;
            TVector vec1;
            TVector vec2;
            TVector loweringMask = TVector.Create(0x20);
            TVector vecA = TVector.Create('a');
            TVector vecZMinusA = TVector.Create('z' - 'a');
            do
            {
                vec1 = TVector.LoadUnsafe(ref Unsafe.As<char, ushort>(ref charA), i);
                vec2 = TVector.LoadUnsafe(ref Unsafe.As<char, ushort>(ref charB), i);

                if (!Utf16Utility.AllCharsInVectorAreAscii(vec1 | vec2))
                {
                    goto NON_ASCII;
                }

                TVector notEquals = ~TVector.Equals(vec1, vec2);
                if (!notEquals.Equals(TVector.Zero))
                {
                    // not exact match

                    vec1 |= loweringMask;
                    vec2 |= loweringMask;
                    if (TVector.GreaterThanAny((vec1 - vecA) & notEquals, vecZMinusA) || !vec1.Equals(vec2))
                    {
                        return false; // first input isn't in [A-Za-z], and not exact match of lowered
                    }
                }
                i += (nuint)TVector.Count;
            } while (i <= lengthToExamine);

            // Handle trailing elements
            if (i != lengthU)
            {
                i = lengthU - (nuint)TVector.Count;
                vec1 = TVector.LoadUnsafe(ref Unsafe.As<char, ushort>(ref charA), i);
                vec2 = TVector.LoadUnsafe(ref Unsafe.As<char, ushort>(ref charB), i);

                if (!Utf16Utility.AllCharsInVectorAreAscii(vec1 | vec2))
                {
                    goto NON_ASCII;
                }

                TVector notEquals = ~TVector.Equals(vec1, vec2);
                if (!notEquals.Equals(TVector.Zero))
                {
                    // not exact match

                    vec1 |= loweringMask;
                    vec2 |= loweringMask;
                    if (TVector.GreaterThanAny((vec1 - vecA) & notEquals, vecZMinusA) || !vec1.Equals(vec2))
                    {
                        return false; // first input isn't in [A-Za-z], and not exact match of lowered
                    }
                }
            }
            return true;

        NON_ASCII:
            if (Utf16Utility.AllCharsInVectorAreAscii(vec1) || Utf16Utility.AllCharsInVectorAreAscii(vec2))
            {
                // No need to use the fallback if one of the inputs is full-ASCII
                return false;
            }

            // Fallback for Non-ASCII inputs
            return CompareStringIgnoreCase(
                ref Unsafe.Add(ref charA, i), (int)(lengthU - i),
                ref Unsafe.Add(ref charB, i), (int)(lengthU - i)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsIgnoreCase(ref char charA, ref char charB, int length)
        {
            if (!Vector128.IsHardwareAccelerated || length < Vector128<ushort>.Count)
            {
                return EqualsIgnoreCase_Scalar(ref charA, ref charB, length);
            }
            if (Vector512.IsHardwareAccelerated && length >= Vector512<ushort>.Count)
            {
                return EqualsIgnoreCase_Vector<Vector512<ushort>>(ref charA, ref charB, length);
            }
            if (Vector256.IsHardwareAccelerated && length >= Vector256<ushort>.Count)
            {
                return EqualsIgnoreCase_Vector<Vector256<ushort>>(ref charA, ref charB, length);
            }
            return EqualsIgnoreCase_Vector<Vector128<ushort>>(ref charA, ref charB, length);
        }

        internal static bool EqualsIgnoreCase_Scalar(ref char charA, ref char charB, int length)
        {
            IntPtr byteOffset = IntPtr.Zero;

#if TARGET_64BIT
            ulong valueAu64 = 0;
            ulong valueBu64 = 0;
            // Read 4 chars (64 bits) at a time from each string
            while ((uint)length >= 4)
            {
                valueAu64 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charA, byteOffset)));
                valueBu64 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charB, byteOffset)));

                // A 32-bit test - even with the bit-twiddling here - is more efficient than a 64-bit test.
                ulong temp = valueAu64 | valueBu64;
                if (!Utf16Utility.AllCharsInUInt32AreAscii((uint)temp | (uint)(temp >> 32)))
                {
                    goto NonAscii64; // one of the inputs contains non-ASCII data
                }

                // Generally, the caller has likely performed a first-pass check that the input strings
                // are likely equal. Consider a dictionary which computes the hash code of its key before
                // performing a proper deep equality check of the string contents. We want to optimize for
                // the case where the equality check is likely to succeed, which means that we want to avoid
                // branching within this loop unless we're about to exit the loop, either due to failure or
                // due to us running out of input data.

                if (!Utf16Utility.UInt64OrdinalIgnoreCaseAscii(valueAu64, valueBu64))
                {
                    return false;
                }

                byteOffset += 8;
                length -= 4;
            }
#endif
            uint valueAu32 = 0;
            uint valueBu32 = 0;
            // Read 2 chars (32 bits) at a time from each string
#if TARGET_64BIT
            if ((uint)length >= 2)
#else
            while ((uint)length >= 2)
#endif
            {
                valueAu32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charA, byteOffset)));
                valueBu32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charB, byteOffset)));

                if (!Utf16Utility.AllCharsInUInt32AreAscii(valueAu32 | valueBu32))
                {
                    goto NonAscii32; // one of the inputs contains non-ASCII data
                }

                // Generally, the caller has likely performed a first-pass check that the input strings
                // are likely equal. Consider a dictionary which computes the hash code of its key before
                // performing a proper deep equality check of the string contents. We want to optimize for
                // the case where the equality check is likely to succeed, which means that we want to avoid
                // branching within this loop unless we're about to exit the loop, either due to failure or
                // due to us running out of input data.

                if (!Utf16Utility.UInt32OrdinalIgnoreCaseAscii(valueAu32, valueBu32))
                {
                    return false;
                }

                byteOffset += 4;
                length -= 2;
            }

            if (length != 0)
            {
                Debug.Assert(length == 1);

                valueAu32 = Unsafe.AddByteOffset(ref charA, byteOffset);
                valueBu32 = Unsafe.AddByteOffset(ref charB, byteOffset);

                if ((valueAu32 | valueBu32) > 0x7Fu)
                {
                    goto NonAscii32; // one of the inputs contains non-ASCII data
                }

                if (valueAu32 == valueBu32)
                {
                    return true; // exact match
                }

                valueAu32 |= 0x20u;
                if ((uint)(valueAu32 - 'a') > (uint)('z' - 'a'))
                {
                    return false; // not exact match, and first input isn't in [A-Za-z]
                }

                return valueAu32 == (valueBu32 | 0x20u);
            }

            Debug.Assert(length == 0);
            return true;

        NonAscii32:
            // Both values have to be non-ASCII to use the slow fallback, in case if one of them is not we return false
            if (Utf16Utility.AllCharsInUInt32AreAscii(valueAu32) || Utf16Utility.AllCharsInUInt32AreAscii(valueBu32))
            {
                return false;
            }
            goto NonAscii;

#if TARGET_64BIT
        NonAscii64:
            // Both values have to be non-ASCII to use the slow fallback, in case if one of them is not we return false
            if (Utf16Utility.AllCharsInUInt64AreAscii(valueAu64) || Utf16Utility.AllCharsInUInt64AreAscii(valueBu64))
            {
                return false;
            }
#endif
        NonAscii:
            // The non-ASCII case is factored out into its own helper method so that the JIT
            // doesn't need to emit a complex prolog for its caller (this method).
            return CompareStringIgnoreCase(ref Unsafe.AddByteOffset(ref charA, byteOffset), length, ref Unsafe.AddByteOffset(ref charB, byteOffset), length) == 0;
        }

        internal static unsafe int IndexOf(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if (!source.TryGetSpan(startIndex, count, out ReadOnlySpan<char> sourceSpan))
            {
                // Bounds check failed - figure out exactly what went wrong so that we can
                // surface the correct argument exception.

                if ((uint)startIndex > (uint)source.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
                }
                else
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
                }
            }

            int result = ignoreCase ? IndexOfOrdinalIgnoreCase(sourceSpan, value) : sourceSpan.IndexOf(value);

            return result >= 0 ? result + startIndex : result;
        }

        internal static int IndexOfOrdinalIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
        {
            if (value.Length == 0)
            {
                return 0;
            }

            if (value.Length > source.Length)
            {
                // A non-linguistic search compares chars directly against one another, so large
                // target strings can never be found inside small search spaces. This check also
                // handles empty 'source' spans.
                return -1;
            }

            if (GlobalizationMode.Invariant)
            {
                return InvariantModeCasing.IndexOfIgnoreCase(source, value);
            }

            if (GlobalizationMode.UseNls)
            {
                return CompareInfo.NlsIndexOfOrdinalCore(source, value, ignoreCase: true, fromBeginning: true);
            }

            // If value doesn't start with ASCII, fall back to a non-vectorized non-ASCII friendly version.
            ref char valueRef = ref MemoryMarshal.GetReference(value);
            char valueChar = valueRef;
            if (!char.IsAscii(valueChar))
            {
                return OrdinalCasing.IndexOf(source, value);
            }

            // Hoist some expressions from the loop
            int valueTailLength = value.Length - 1;
            int searchSpaceMinusValueTailLength = source.Length - valueTailLength;
            ref char searchSpace = ref MemoryMarshal.GetReference(source);
            char valueCharU = default;
            char valueCharL = default;
            nint offset = 0;
            bool isLetter = false;

            // If the input is long enough and the value ends with ASCII and is at least two characters,
            // we can take a special vectorized path that compares both the beginning and the end at the same time.
            if (Vector128.IsHardwareAccelerated &&
                valueTailLength != 0 &&
                searchSpaceMinusValueTailLength >= Vector128<ushort>.Count)
            {
                valueCharU = Unsafe.Add(ref valueRef, valueTailLength);
                if (char.IsAscii(valueCharU))
                {
                    goto SearchTwoChars;
                }
            }

            // We're searching for the first character and it's known to be ASCII. If it's not a letter,
            // then IgnoreCase doesn't impact what it matches and we just need to do a normal search
            // for that single character. If it is a letter, then we need to search for both its upper
            // and lower-case variants.
            if (char.IsAsciiLetter(valueChar))
            {
                valueCharU = (char)(valueChar & ~0x20);
                valueCharL = (char)(valueChar | 0x20);
                isLetter = true;
            }

            do
            {
                // Do a quick search for the first element of "value".
                int relativeIndex = isLetter ?
                    PackedSpanHelpers.PackedIndexOfIsSupported
                        ? PackedSpanHelpers.IndexOfAnyIgnoreCase(ref Unsafe.Add(ref searchSpace, offset), valueCharL, searchSpaceMinusValueTailLength)
                        : SpanHelpers.IndexOfAnyChar(ref Unsafe.Add(ref searchSpace, offset), valueCharU, valueCharL, searchSpaceMinusValueTailLength) :
                    SpanHelpers.IndexOfChar(ref Unsafe.Add(ref searchSpace, offset), valueChar, searchSpaceMinusValueTailLength);
                if (relativeIndex < 0)
                {
                    break;
                }

                searchSpaceMinusValueTailLength -= relativeIndex;
                if (searchSpaceMinusValueTailLength <= 0)
                {
                    break;
                }
                offset += relativeIndex;

                // Found the first element of "value". See if the tail matches.
                if (valueTailLength == 0 || // for single-char values we already matched first chars
                    EqualsIgnoreCase(
                        ref Unsafe.Add(ref searchSpace, (nuint)(offset + 1)),
                        ref Unsafe.Add(ref valueRef, 1), valueTailLength))
                {
                    return (int)offset;  // The tail matched. Return a successful find.
                }

                searchSpaceMinusValueTailLength--;
                offset++;
            }
            while (searchSpaceMinusValueTailLength > 0);

            return -1;

        // Based on SpanHelpers.IndexOf(ref char, int, ref char, int), which was in turn based on
        // http://0x80.pl/articles/simd-strfind.html#algorithm-1-generic-simd. This version has additional
        // modifications to support case-insensitive searches.
        SearchTwoChars:
            // Both the first character in value (valueChar) and the last character in value (valueCharU) are ASCII. Get their lowercase variants.
            valueChar = (char)(valueChar | 0x20);
            valueCharU = (char)(valueCharU | 0x20);

            // The search is more efficient if the two characters being searched for are different. As long as they are equal, walk backwards
            // from the last character in the search value until we find a character that's different. Since we're dealing with IgnoreCase,
            // we compare the lowercase variants, as that's what we'll be comparing against in the main loop.
            nint ch1ch2Distance = valueTailLength;
            while (valueCharU == valueChar && ch1ch2Distance > 1)
            {
                char tmp = Unsafe.Add(ref valueRef, ch1ch2Distance - 1);
                if (!char.IsAscii(tmp))
                {
                    break;
                }
                --ch1ch2Distance;
                valueCharU = (char)(tmp | 0x20);
            }

            // Use Vector256 if the input is long enough.
            if (Vector256.IsHardwareAccelerated && searchSpaceMinusValueTailLength - Vector256<ushort>.Count >= 0)
            {
                // Create a vector for each of the lowercase ASCII characters we're searching for.
                Vector256<ushort> ch1 = Vector256.Create((ushort)valueChar);
                Vector256<ushort> ch2 = Vector256.Create((ushort)valueCharU);

                nint searchSpaceMinusValueTailLengthAndVector = searchSpaceMinusValueTailLength - (nint)Vector256<ushort>.Count;
                do
                {
                    // Make sure we don't go out of bounds.
                    Debug.Assert(offset + ch1ch2Distance + Vector256<ushort>.Count <= source.Length);

                    // Load a vector from the current search space offset and another from the offset plus the distance between the two characters.
                    // For each, | with 0x20 so that letters are lowercased, then & those together to get a mask. If the mask is all zeros, there
                    // was no match.  If it wasn't, we have to do more work to check for a match.
                    Vector256<ushort> cmpCh2 = Vector256.Equals(ch2, Vector256.BitwiseOr(Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)), Vector256.Create((ushort)0x20)));
                    Vector256<ushort> cmpCh1 = Vector256.Equals(ch1, Vector256.BitwiseOr(Vector256.LoadUnsafe(ref searchSpace, (nuint)offset), Vector256.Create((ushort)0x20)));
                    Vector256<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();
                    if (cmpAnd != Vector256<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    // No match. Advance to the next vector.
                    offset += Vector256<ushort>.Count;

                    // If we've reached the end of the search space, bail.
                    if (offset == searchSpaceMinusValueTailLength)
                    {
                        return -1;
                    }

                    // If we're within a vector's length of the end of the search space, adjust the offset
                    // to point to the last vector so that our next iteration will process it.
                    if (offset > searchSpaceMinusValueTailLengthAndVector)
                    {
                        offset = searchSpaceMinusValueTailLengthAndVector;
                    }

                    continue;

                CandidateFound:
                    // Possible matches at the current location. Extract the bits for each element.
                    // For each set bits, we'll check if it's a match at that location.
                    uint mask = cmpAnd.ExtractMostSignificantBits();
                    do
                    {
                        // Do a full IgnoreCase equality comparison. SpanHelpers.IndexOf skips comparing the two characters in some cases,
                        // but we don't actually know that the two characters are equal, since we compared with | 0x20. So we just compare
                        // the full string always.
                        nint charPos = (nint)(uint.TrailingZeroCount(mask) / sizeof(ushort));
                        if (EqualsIgnoreCase(ref Unsafe.Add(ref searchSpace, offset + charPos), ref valueRef, value.Length))
                        {
                            // Match! Return the index.
                            return (int)(offset + charPos);
                        }

                        // Clear the two lowest set bits in the mask. If there are no more set bits, we're done.
                        // If any remain, we loop around to do the next comparison.
                        mask = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(mask));
                    } while (mask != 0);
                    goto LoopFooter;

                } while (true);
            }
            else // 128bit vector path (SSE2 or AdvSimd)
            {
                // Create a vector for each of the lowercase ASCII characters we're searching for.
                Vector128<ushort> ch1 = Vector128.Create((ushort)valueChar);
                Vector128<ushort> ch2 = Vector128.Create((ushort)valueCharU);

                nint searchSpaceMinusValueTailLengthAndVector = searchSpaceMinusValueTailLength - (nint)Vector128<ushort>.Count;
                do
                {
                    // Make sure we don't go out of bounds.
                    Debug.Assert(offset + ch1ch2Distance + Vector128<ushort>.Count <= source.Length);

                    // Load a vector from the current search space offset and another from the offset plus the distance between the two characters.
                    // For each, | with 0x20 so that letters are lowercased, then & those together to get a mask. If the mask is all zeros, there
                    // was no match.  If it wasn't, we have to do more work to check for a match.
                    Vector128<ushort> cmpCh2 = Vector128.Equals(ch2, Vector128.BitwiseOr(Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)), Vector128.Create((ushort)0x20)));
                    Vector128<ushort> cmpCh1 = Vector128.Equals(ch1, Vector128.BitwiseOr(Vector128.LoadUnsafe(ref searchSpace, (nuint)offset), Vector128.Create((ushort)0x20)));
                    Vector128<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();
                    if (cmpAnd != Vector128<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    // No match. Advance to the next vector.
                    offset += Vector128<ushort>.Count;

                    // If we've reached the end of the search space, bail.
                    if (offset == searchSpaceMinusValueTailLength)
                    {
                        return -1;
                    }

                    // If we're within a vector's length of the end of the search space, adjust the offset
                    // to point to the last vector so that our next iteration will process it.
                    if (offset > searchSpaceMinusValueTailLengthAndVector)
                    {
                        offset = searchSpaceMinusValueTailLengthAndVector;
                    }

                    continue;

                CandidateFound:
                    // Possible matches at the current location. Extract the bits for each element.
                    // For each set bits, we'll check if it's a match at that location.
                    uint mask = cmpAnd.ExtractMostSignificantBits();
                    do
                    {
                        // Do a full IgnoreCase equality comparison. SpanHelpers.IndexOf skips comparing the two characters in some cases,
                        // but we don't actually know that the two characters are equal, since we compared with | 0x20. So we just compare
                        // the full string always.
                        nint charPos = (nint)(uint.TrailingZeroCount(mask) / sizeof(ushort));
                        if (EqualsIgnoreCase(ref Unsafe.Add(ref searchSpace, offset + charPos), ref valueRef, value.Length))
                        {
                            // Match! Return the index.
                            return (int)(offset + charPos);
                        }

                        // Clear the two lowest set bits in the mask. If there are no more set bits, we're done.
                        // If any remain, we loop around to do the next comparison.
                        mask = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(mask));
                    } while (mask != 0);
                    goto LoopFooter;

                } while (true);
            }
        }

        internal static int LastIndexOf(string source, string value, int startIndex, int count)
        {
            int result = source.AsSpan(startIndex, count).LastIndexOf(value);
            if (result >= 0) { result += startIndex; } // if match found, adjust 'result' by the actual start position
            return result;
        }

        internal static unsafe int LastIndexOf(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if (value.Length == 0)
            {
                return startIndex + 1; // startIndex is the index of the last char to include in the search space
            }

            if (count == 0)
            {
                return -1;
            }

            if (GlobalizationMode.Invariant)
            {
                return ignoreCase ? InvariantModeCasing.LastIndexOfIgnoreCase(source.AsSpan(startIndex, count), value) : LastIndexOf(source, value, startIndex, count);
            }

            if (GlobalizationMode.UseNls)
            {
                return CompareInfo.NlsLastIndexOfOrdinalCore(source, value, startIndex, count, ignoreCase);
            }

            if (!ignoreCase)
            {
                LastIndexOf(source, value, startIndex, count);
            }

            if (!source.TryGetSpan(startIndex, count, out ReadOnlySpan<char> sourceSpan))
            {
                // Bounds check failed - figure out exactly what went wrong so that we can
                // surface the correct argument exception.

                if ((uint)startIndex > (uint)source.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
                }
                else
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
                }
            }

            int result = OrdinalCasing.LastIndexOf(sourceSpan, value);

            if (result >= 0)
            {
                result += startIndex;
            }
            return result;
        }

        internal static int LastIndexOfOrdinalIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
        {
            if (value.Length == 0)
            {
                return source.Length;
            }

            if (value.Length > source.Length)
            {
                // A non-linguistic search compares chars directly against one another, so large
                // target strings can never be found inside small search spaces. This check also
                // handles empty 'source' spans.

                return -1;
            }

            if (GlobalizationMode.Invariant)
            {
                return InvariantModeCasing.LastIndexOfIgnoreCase(source, value);
            }

            if (GlobalizationMode.UseNls)
            {
                return CompareInfo.NlsIndexOfOrdinalCore(source, value, ignoreCase: true, fromBeginning: false);
            }

            return OrdinalCasing.LastIndexOf(source, value);
        }

        internal static int ToUpperOrdinal(ReadOnlySpan<char> source, Span<char> destination)
        {
            if (source.Overlaps(destination))
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_SpanOverlappedOperation);

            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            if (GlobalizationMode.Invariant)
            {
                InvariantModeCasing.ToUpper(source, destination);
                return source.Length;
            }

            if (GlobalizationMode.UseNls)
            {
                TextInfo.Invariant.ChangeCaseToUpper(source, destination); // this is the best so far for NLS.
                return source.Length;
            }

            OrdinalCasing.ToUpperOrdinal(source, destination);
            return source.Length;
        }
    }
}
