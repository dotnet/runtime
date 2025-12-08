// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Text.Unicode
{
    internal static unsafe partial class Utf16Utility
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint GetSurrogateMask(Vector128<ushort> cmp)
        {
            // Convert the comparison result to a scalar surrogate mask.
            // The elements in 'cmp' should be either all bits set or zero.

            if (AdvSimd.Arm64.IsSupported)
            {
                // Since ExtractMostSignificantBits is very slow on AdvSimd,
                // we use a 64-bit value to encode the mask, where each byte represents one element:
                //   0x01 for all bits set, 0x00 for zero.
                ulong mask = AdvSimd.Arm64.UnzipOdd(cmp.AsByte(), cmp.AsByte()).AsUInt64().ToScalar();
                return (nuint)(mask & 0x0101010101010101u);
            }

            // Otherwise, encode the mask with 8-bits (one byte), where each bit represents one element.
            return cmp.ExtractMostSignificantBits();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSurrogatesMatch(nuint maskHigh, nuint maskLow)
        {
            // Make sure that each high surrogate is followed by a low surrogate character,
            // and each low surrogate follows a high surrogate character.
            // The last character is discarded as it will be checked by 'IsLastCharHighSurrogate'.
            // The first character must not be a low surrogate. This is checked by matching
            // 'maskLow' aganist the zeros inserted after shifting 'maskHigh' to the left.

            if (AdvSimd.Arm64.IsSupported)
            {
                // Each surrogate character is 8 bits apart.
                return (maskHigh << 8) == maskLow;
            }
            // Each surrogate character is 1 bit apart.
            return (byte)(maskHigh << 1) == (byte)maskLow;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLastCharHighSurrogate(nuint maskHigh)
        {
            if (AdvSimd.Arm64.IsSupported)
            {
                // Check if the top byte is not zero.
                return (maskHigh >>> 56) != 0;
            }
            // Check if the top bit (of a byte) is not zero.
            return ((byte)maskHigh >>> 7) != 0;
        }

        // Returns &inputBuffer[inputLength] if the input buffer is valid.
        /// <summary>
        /// Given an input buffer <paramref name="pInputBuffer"/> of char length <paramref name="inputLength"/>,
        /// returns a pointer to where the first invalid data appears in <paramref name="pInputBuffer"/>.
        /// </summary>
        /// <remarks>
        /// Returns a pointer to the end of <paramref name="pInputBuffer"/> if the buffer is well-formed.
        /// </remarks>
        public static char* GetPointerToFirstInvalidChar(char* pInputBuffer, int inputLength, out long utf8CodeUnitCountAdjustment, out int scalarCountAdjustment)
        {
            Debug.Assert(inputLength >= 0, "Input length must not be negative.");
            Debug.Assert(pInputBuffer != null || inputLength == 0, "Input length must be zero if input buffer pointer is null.");

            // First, we'll handle the common case of all-ASCII. If this is able to
            // consume the entire buffer, we'll skip the remainder of this method's logic.

            int numAsciiCharsConsumedJustNow = (int)Ascii.GetIndexOfFirstNonAsciiChar(pInputBuffer, (uint)inputLength);
            Debug.Assert(0 <= numAsciiCharsConsumedJustNow && numAsciiCharsConsumedJustNow <= inputLength);

            pInputBuffer += (uint)numAsciiCharsConsumedJustNow;
            inputLength -= numAsciiCharsConsumedJustNow;

            if (inputLength == 0)
            {
                utf8CodeUnitCountAdjustment = 0;
                scalarCountAdjustment = 0;
                return pInputBuffer;
            }

            // If we got here, it means we saw some non-ASCII data, so within our
            // vectorized code paths below we'll handle all non-surrogate UTF-16
            // code points branchlessly. We'll only branch if we see surrogates.
            //
            // We still optimistically assume the data is mostly ASCII. This means that the
            // number of UTF-8 code units and the number of scalars almost matches the number
            // of UTF-16 code units. As we go through the input and find non-ASCII
            // characters, we'll keep track of these "adjustment" fixups. To get the
            // total number of UTF-8 code units required to encode the input data, add
            // the UTF-8 code unit count adjustment to the number of UTF-16 code units
            // seen.  To get the total number of scalars present in the input data,
            // add the scalar count adjustment to the number of UTF-16 code units seen.

            long tempUtf8CodeUnitCountAdjustment = 0;
            int tempScalarCountAdjustment = 0;
            char* pEndOfInputBuffer = pInputBuffer + (uint)inputLength;

            if (Vector128.IsHardwareAccelerated)
            {
                if (inputLength >= Vector128<ushort>.Count)
                {
                    Vector128<ushort> vector0080 = Vector128.Create<ushort>(0x0080);
                    Vector128<ushort> vector0400 = Vector128.Create<ushort>(0x0400);
                    Vector128<ushort> vector0800 = Vector128.Create<ushort>(0x0800);
                    Vector128<ushort> vectorD800 = Vector128.Create<ushort>(0xD800);

                    char* pHighestAddressWhereCanReadOneVector = pEndOfInputBuffer - Vector128<ushort>.Count;
                    Debug.Assert(pHighestAddressWhereCanReadOneVector >= pInputBuffer);

                    do
                    {
                        Vector128<ushort> utf16Data = Vector128.Load((ushort*)pInputBuffer);

                        // Calculate the popcnt for UTF-8 adjustments, which is the number of *additional*
                        // UTF-8 bytes that each UTF-16 code unit requires as it expands.
                        // This results in the wrong count for UTF-16 surrogate code units (we just counted
                        // that each individual code unit expands to 3 bytes, but in reality a well-formed
                        // UTF-16 surrogate pair expands to 4 bytes). We'll handle this in just a moment.
                        //
                        // For now, compute the popcnt but squirrel it away. We'll fold it in to the
                        // cumulative UTF-8 adjustment factor once we determine that there are no
                        // unpaired surrogates in our data. (Unpaired surrogates would invalidate
                        // our computed result and we'd have to throw it away.)

                        uint popcnt;

                        // On AdvSimd ExtractMostSignificantBits is very slow, so a different algorithm is used to avoid
                        // the poor performance.

                        if (AdvSimd.Arm64.IsSupported)
                        {
                            // The 'twoOrMoreUtf8Bytes' and 'threeOrMoreUtf8Bytes' vectors will contain
                            // elements whose values are 0xFFFF (-1 as signed word) iff the corresponding
                            // UTF-16 code unit was >= 0x0080 and >= 0x0800, respectively. By summing these
                            // vectors, each element of the sum will contain one of three values:
                            //
                            // 0x0000 ( 0) = original char was 0000..007F
                            // 0xFFFF (-1) = original char was 0080..07FF
                            // 0xFFFE (-2) = original char was 0800..FFFF
                            //
                            // We'll negate them to produce a value 0..2 for each element, then sum all the
                            // elements together to produce the number of *additional* UTF-8 code units
                            // required to represent this UTF-16 data.

                            Vector128<ushort> twoOrMoreUtf8Bytes = Vector128.GreaterThanOrEqual(utf16Data, vector0080);
                            Vector128<ushort> threeOrMoreUtf8Bytes = Vector128.GreaterThanOrEqual(utf16Data, vector0800);
                            Vector128<ushort> sumVector = Vector128<ushort>.Zero - twoOrMoreUtf8Bytes - threeOrMoreUtf8Bytes;
                            popcnt = Vector128.Sum(sumVector);
                        }
                        else
                        {
                            Vector128<ushort> vector7800 = Vector128.Create<ushort>(0x7800);

                            // Sets the 0x0080 bit of each element in 'charIsNonAscii' if the corresponding
                            // input was 0x0080 <= [value]. (i.e., [value] is non-ASCII.)

                            Vector128<ushort> charIsNonAscii = Vector128.Min(utf16Data, vector0080);

#if DEBUG
                            // Quick check to ensure we didn't accidentally set the 0x8000 bit of any element.
                            uint debugMask = charIsNonAscii.AsByte().ExtractMostSignificantBits();
                            Debug.Assert((debugMask & 0b_1010_1010_1010_1010) == 0, "Shouldn't have set the 0x8000 bit of any element in 'charIsNonAscii'.");
#endif // DEBUG

                            // Sets the 0x8080 bits of each element in 'charIsNonAscii' if the corresponding
                            // input was 0x0800 <= [value]. This also handles the missing range a few lines above.
                            // Since 3-byte elements have a value >= 0x0800, we'll perform a saturating add of 0x7800 in order to
                            // get all 3-byte elements to have their 0x8000 bits set. A saturating add will not set the 0x8000
                            // bit for 1-byte or 2-byte elements. The 0x0080 bit will already have been set for non-ASCII (2-byte
                            // and 3-byte) elements.

                            Vector128<ushort> charIsThreeByteUtf8Encoded = Vector128.AddSaturate(utf16Data, vector7800);

                            // Each even bit of mask will be 1 only if the char was >= 0x0080,
                            // and each odd bit of mask will be 1 only if the char was >= 0x0800.
                            //
                            // Example for UTF-16 input "[ 0123 ] [ 1234 ] ...":
                            //
                            //            ,-- set if char[1] is >= 0x0800
                            //            |   ,-- set if char[0] is >= 0x0800
                            //            v   v
                            // mask = ... 1 1 0 1
                            //              ^   ^-- set if char[0] is non-ASCII
                            //              `-- set if char[1] is non-ASCII

                            uint mask = (charIsNonAscii | charIsThreeByteUtf8Encoded).AsByte().ExtractMostSignificantBits();
                            popcnt = (uint)BitOperations.PopCount(mask); // on x64, perform zero-extension for free
                        }

                        // Now check for surrogates.

                        utf16Data -= vectorD800;
                        nuint maskSurr = GetSurrogateMask(Vector128.LessThan(utf16Data, vector0800));
                        if (maskSurr != 0)
                        {
                            // Get the surrogate masks for high and low surrogates.
                            // A high surrogate will be less than 0x0400 after subtracting by 0xD800.
                            // A low surrogate is a surrogate that is not a high surrogate.

                            nuint maskHigh = GetSurrogateMask(Vector128.LessThan(utf16Data, vector0400));
                            nuint maskLow  = ~maskHigh & maskSurr;

                            if (!IsSurrogatesMatch(maskHigh, maskLow))
                            {
                                break; // error: mismatched surrogate pair; break out of vectorized logic
                            }

                            if (IsLastCharHighSurrogate(maskHigh))
                            {
                                // There was a standalone high surrogate at the end of the vector.
                                // We'll adjust our counters so that we don't consider this char consumed.

                                pInputBuffer--;
                                popcnt -= 2;
                            }

                            // If all the surrogate pairs are valid, then the number of surrogate pairs
                            // is equal to the number of low surrogates.

                            nint surrogatePairsCountNint = (nint)BitOperations.PopCount(maskLow);

                            // 2 UTF-16 chars become 1 Unicode scalar

                            tempScalarCountAdjustment -= (int)surrogatePairsCountNint;

                            // Since each surrogate code unit was >= 0x0800, we eagerly assumed
                            // it'd be encoded as 3 UTF-8 code units. Each surrogate half is only
                            // encoded as 2 UTF-8 code units (for 4 UTF-8 code units total),
                            // so we'll adjust this now.

                            tempUtf8CodeUnitCountAdjustment -= surrogatePairsCountNint;
                            tempUtf8CodeUnitCountAdjustment -= surrogatePairsCountNint;
                        }

                        tempUtf8CodeUnitCountAdjustment += popcnt;
                        pInputBuffer += Vector128<ushort>.Count;
                    } while (pInputBuffer <= pHighestAddressWhereCanReadOneVector);
                }
            }

            // Vectorization isn't supported on our current platform, or the input was too small to benefit
            // from vectorization, or we saw invalid UTF-16 data in the vectorized code paths and need to
            // drain remaining valid chars before we report failure.

            for (; pInputBuffer < pEndOfInputBuffer; pInputBuffer++)
            {
                uint thisChar = pInputBuffer[0];
                if (thisChar <= 0x7F)
                {
                    continue;
                }

                // Bump adjustment by +1 for U+0080..U+07FF; by +2 for U+0800..U+FFFF.
                // This optimistically assumes no surrogates, which we'll handle shortly.

                tempUtf8CodeUnitCountAdjustment += (thisChar + 0x0001_F800u) >> 16;

                if (!UnicodeUtility.IsSurrogateCodePoint(thisChar))
                {
                    continue;
                }

                // Found a surrogate char. Back out the adjustment we made above, then
                // try to consume the entire surrogate pair all at once. We won't bother
                // trying to interpret the surrogate pair as a scalar value; we'll only
                // validate that its bit pattern matches what's expected for a surrogate pair.

                tempUtf8CodeUnitCountAdjustment -= 2;

                if ((nuint)pEndOfInputBuffer - (nuint)pInputBuffer < sizeof(uint))
                {
                    goto Error; // input buffer too small to read a surrogate pair
                }

                thisChar = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                if (((thisChar - (BitConverter.IsLittleEndian ? 0xDC00_D800u : 0xD800_DC00u)) & 0xFC00_FC00u) != 0)
                {
                    goto Error; // not a well-formed surrogate pair
                }

                tempScalarCountAdjustment--; // 2 UTF-16 code units -> 1 scalar
                tempUtf8CodeUnitCountAdjustment += 2; // 2 UTF-16 code units -> 4 UTF-8 code units

                pInputBuffer++; // consumed one extra char
            }

        Error:

            // Also used for normal return.

            utf8CodeUnitCountAdjustment = tempUtf8CodeUnitCountAdjustment;
            scalarCountAdjustment = tempScalarCountAdjustment;
            return pInputBuffer;
        }
    }
}
