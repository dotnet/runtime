// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Numerics;

#if SYSTEM_PRIVATE_CORELIB
using Internal.Runtime.CompilerServices;
#endif

namespace System.Text.Unicode
{
    internal static unsafe partial class Utf16Utility
    {
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

            int numAsciiCharsConsumedJustNow = (int)ASCIIUtility.GetIndexOfFirstNonAsciiChar(pInputBuffer, (uint)inputLength);
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

            // Per https://github.com/dotnet/runtime/issues/41699, temporarily disabling
            // ARM64-intrinsicified code paths. ARM64 platforms may still use the vectorized
            // non-intrinsicified 'else' block below.

            if (/* (AdvSimd.Arm64.IsSupported && BitConverter.IsLittleEndian) || */ Sse41.IsSupported)
            {
                if (inputLength >= Vector128<ushort>.Count)
                {
                    Vector128<ushort> vector0080 = Vector128.Create((ushort)0x0080);
                    Vector128<ushort> vector7800 = Vector128.Create((ushort)0x7800);
                    Vector128<ushort> vectorA000 = Vector128.Create((ushort)0xA000);

                    Vector128<byte> bitMask128 = BitConverter.IsLittleEndian ?
                        Vector128.Create(0x80402010_08040201).AsByte() :
                        Vector128.Create(0x01020408_10204080).AsByte();

                    char* pHighestAddressWhereCanReadOneVector = pEndOfInputBuffer - Vector128<ushort>.Count;
                    Debug.Assert(pHighestAddressWhereCanReadOneVector >= pInputBuffer);

                    do
                    {
                        Vector128<ushort> utf16Data;
                        if (AdvSimd.Arm64.IsSupported)
                        {
                            utf16Data = AdvSimd.LoadVector128((ushort*)pInputBuffer); // unaligned
                        }
                        else
                        {
                            utf16Data = Sse2.LoadVector128((ushort*)pInputBuffer); // unaligned
                        }

                        pInputBuffer += Vector128<ushort>.Count; // eagerly bump this now in preparation for next loop, will adjust later if necessary
                        Vector128<ushort> charIsNonAscii;

                        // Sets the 0x0080 bit of each element in 'charIsNonAscii' if the corresponding
                        // input was 0x0080 <= [value]. (i.e., [value] is non-ASCII.)

                        if (AdvSimd.Arm64.IsSupported)
                        {
                            charIsNonAscii = AdvSimd.Min(utf16Data, vector0080);
                        }
                        else
                        {
                            charIsNonAscii = Sse41.Min(utf16Data, vector0080);
                        }

#if DEBUG
                        // Quick check to ensure we didn't accidentally set the 0x8000 bit of any element.
                        uint debugMask;
                        if (AdvSimd.Arm64.IsSupported)
                        {
                            debugMask = GetNonAsciiBytes(charIsNonAscii.AsByte(), bitMask128);
                        }
                        else
                        {
                            debugMask = (uint)Sse2.MoveMask(charIsNonAscii.AsByte());
                        }
                        Debug.Assert((debugMask & 0b_1010_1010_1010_1010) == 0, "Shouldn't have set the 0x8000 bit of any element in 'charIsNonAscii'.");
#endif // DEBUG

                        // Sets the 0x8080 bits of each element in 'charIsNonAscii' if the corresponding
                        // input was 0x0800 <= [value]. This also handles the missing range a few lines above.

                        Vector128<ushort> charIsThreeByteUtf8Encoded;
                        uint mask;

                        // Since 3-byte elements have a value >= 0x0800, we'll perform a saturating add of 0x7800 in order to
                        // get all 3-byte elements to have their 0x8000 bits set. A saturating add will not set the 0x8000
                        // bit for 1-byte or 2-byte elements. The 0x0080 bit will already have been set for non-ASCII (2-byte
                        // and 3-byte) elements.

                        if (AdvSimd.IsSupported)
                        {
                            charIsThreeByteUtf8Encoded = AdvSimd.AddSaturate(utf16Data, vector7800);
                            mask = GetNonAsciiBytes(AdvSimd.Or(charIsNonAscii, charIsThreeByteUtf8Encoded).AsByte(), bitMask128);
                        }
                        else
                        {
                            charIsThreeByteUtf8Encoded = Sse2.AddSaturate(utf16Data, vector7800);
                            mask = (uint)Sse2.MoveMask(Sse2.Or(charIsNonAscii, charIsThreeByteUtf8Encoded).AsByte());
                        }

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
                        //
                        // This means we can popcnt the number of set bits, and the result is the
                        // number of *additional* UTF-8 bytes that each UTF-16 code unit requires as
                        // it expands. This results in the wrong count for UTF-16 surrogate code
                        // units (we just counted that each individual code unit expands to 3 bytes,
                        // but in reality a well-formed UTF-16 surrogate pair expands to 4 bytes).
                        // We'll handle this in just a moment.
                        //
                        // For now, compute the popcnt but squirrel it away. We'll fold it in to the
                        // cumulative UTF-8 adjustment factor once we determine that there are no
                        // unpaired surrogates in our data. (Unpaired surrogates would invalidate
                        // our computed result and we'd have to throw it away.)

                        nuint popcnt = (uint)BitOperations.PopCount(mask); // on x64, perform zero-extension for free

                        // Surrogates need to be special-cased for two reasons: (a) we need
                        // to account for the fact that we over-counted in the addition above;
                        // and (b) they require separate validation.
                        //
                        // Since surrogate code points are [D800..DFFF], adding {A000} to each element moves surrogate
                        // code points to [7800..7FFF], which allows performing a single signed comparison.
                        if (AdvSimd.Arm64.IsSupported)
                        {
                            mask = GetNonAsciiBytes(AdvSimd.CompareLessThan(AdvSimd.Add(utf16Data, vectorA000).AsInt16(), vector7800.AsInt16()).AsByte(), bitMask128);
                        }
                        else
                        {
                            mask = (uint)Sse2.MoveMask(Sse2.CompareLessThan(Sse2.Add(utf16Data, vectorA000).AsInt16(), vector7800.AsInt16()).AsByte());
                        }

                    FinishIteration:

                        // Note: mask bits are set when the corresponding element is NOT a surrogate.
                        // We'll invert this before entering the "validate surrogate pairs" logic below.

                        if (mask == 0xFFFF)
                        {
                            // Put this logic up top since it's predicted-taken (surrogate pairs are uncommon).
                            // Either we saw no surrogates or we already handled them below.

                            tempUtf8CodeUnitCountAdjustment += (long)popcnt;
                            if (pInputBuffer > pHighestAddressWhereCanReadOneVector)
                            {
                                goto NonVectorizedLoop; // can no longer read a vector's worth of data
                            }
                        }
                        else
                        {
                            mask = ~mask;

                            // There's at least one UTF-16 surrogate code unit present.
                            // Since we performed a pmovmskb operation on the result of a 16-bit pcmpgtw,
                            // the resulting bits of 'mask' will occur in pairs:
                            // - 00 if the corresponding UTF-16 char was not a surrogate code unit;
                            // - 11 if the corresponding UTF-16 char was a surrogate code unit.
                            //
                            // A UTF-16 high/low surrogate code unit has the bit pattern [ 11011q## ######## ],
                            // where # is any bit; q = 0 represents a high surrogate, and q = 1 represents
                            // a low surrogate. Right-shifting each surrogate char by 3 bits, we end up with
                            // [ 00011011 q####### ], which means that we can immediately use pmovmskb to
                            // determine whether a given char was a high or a low surrogate.
                            //
                            // Therefore the resulting bits of 'mask2' will occur in pairs:
                            // - 00 if the corresponding UTF-16 char was a high surrogate code unit;
                            // - 01 if the corresponding UTF-16 char was a low surrogate code unit;
                            // - ## (garbage) if the corresponding UTF-16 char was not a surrogate code unit.
                            //   Since 'mask' already has 00 in these positions (since the corresponding char
                            //   wasn't a surrogate), "mask AND mask2 == 00" holds for these positions.

                            uint mask2;
                            if (AdvSimd.Arm64.IsSupported)
                            {
                                mask2 = GetNonAsciiBytes(AdvSimd.ShiftRightLogical(utf16Data, 3).AsByte(), bitMask128);
                            }
                            else
                            {
                                mask2 = (uint)Sse2.MoveMask(Sse2.ShiftRightLogical(utf16Data, 3).AsByte());
                            }

                            // 'lowSurrogatesMask' has its bits occur in pairs:
                            // - 01 if the corresponding char was a low surrogate char,
                            // - 00 if the corresponding char was a high surrogate char or not a surrogate at all.

                            uint lowSurrogatesMask = mask2 & mask;

                            // 'highSurrogatesMask' has its bits occur in pairs:
                            // - 01 if the corresponding char was a high surrogate char,
                            // - 00 if the corresponding char was a low surrogate char or not a surrogate at all.

                            uint highSurrogatesMask = (mask2 ^ 0b_0101_0101_0101_0101u /* flip all even-numbered bits 00 <-> 01 */) & mask;

                            Debug.Assert((highSurrogatesMask & lowSurrogatesMask) == 0,
                                "A char cannot simultaneously be both a high and a low surrogate char.");

                            Debug.Assert(((highSurrogatesMask | lowSurrogatesMask) & 0b_1010_1010_1010_1010u) == 0,
                                "Only even bits (no odd bits) of the masks should be set.");

                            // Now check that each high surrogate is followed by a low surrogate and that each
                            // low surrogate follows a high surrogate. We make an exception for the case where
                            // the final char of the vector is a high surrogate, since we can't perform validation
                            // on it until the next iteration of the loop when we hope to consume the matching
                            // low surrogate.

                            highSurrogatesMask <<= 2;
                            if ((ushort)highSurrogatesMask != lowSurrogatesMask)
                            {
                                break; // error: mismatched surrogate pair; break out of vectorized logic
                            }

                            if (highSurrogatesMask > ushort.MaxValue)
                            {
                                // There was a standalone high surrogate at the end of the vector.
                                // We'll adjust our counters so that we don't consider this char consumed.

                                highSurrogatesMask = (ushort)highSurrogatesMask; // don't allow stray high surrogate to be consumed by popcnt
                                popcnt -= 2; // the '0xC000_0000' bits in the original mask are shifted out and discarded, so account for that here
                                pInputBuffer--; // don't consume this char (pointer has already been bumped at start of loop)
                            }

                            // If we're 64-bit, we can perform the zero-extension of the surrogate pairs count for
                            // free right now, saving the extension step a few lines below. If we're 32-bit, the
                            // convertion to nuint immediately below is a no-op, and we'll pay the cost of the real
                            // 64 -bit extension a few lines below.
                            nuint surrogatePairsCountNuint = (uint)BitOperations.PopCount(highSurrogatesMask);

                            // 2 UTF-16 chars become 1 Unicode scalar

                            tempScalarCountAdjustment -= (int)surrogatePairsCountNuint;

                            // Since each surrogate code unit was >= 0x0800, we eagerly assumed
                            // it'd be encoded as 3 UTF-8 code units, so our earlier popcnt computation
                            // assumes that the pair is encoded as 6 UTF-8 code units. Since each
                            // pair is in reality only encoded as 4 UTF-8 code units, we need to
                            // perform this adjustment now.

                            if (IntPtr.Size == 8)
                            {
                                // Since we've already zero-extended surrogatePairsCountNuint, we can directly
                                // sub + sub. It's more efficient than shl + sub.
                                tempUtf8CodeUnitCountAdjustment -= (long)surrogatePairsCountNuint;
                                tempUtf8CodeUnitCountAdjustment -= (long)surrogatePairsCountNuint;
                            }
                            else
                            {
                                // Take the hit of the 64-bit extension now.
                                tempUtf8CodeUnitCountAdjustment -= 2 * (uint)surrogatePairsCountNuint;
                            }

                            mask = 0xFFFF; // mark "no surrogates require processing"
                            goto FinishIteration; // jump backward to continue the main loop
                        }
                    } while (true);

                    // If we reached this point, we saw truly invalid data within the loop.
                    // Need to undo the eager "bump pInputBuffer" adjustment that took place at start of loop.

                    pInputBuffer -= Vector128<ushort>.Count;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                if (inputLength >= Vector<ushort>.Count)
                {
                    Vector<ushort> vector0080 = new Vector<ushort>(0x0080);
                    Vector<ushort> vector0400 = new Vector<ushort>(0x0400);
                    Vector<ushort> vector0800 = new Vector<ushort>(0x0800);
                    Vector<ushort> vectorD800 = new Vector<ushort>(0xD800);

                    char* pHighestAddressWhereCanReadOneVector = pEndOfInputBuffer - Vector<ushort>.Count;
                    Debug.Assert(pHighestAddressWhereCanReadOneVector >= pInputBuffer);

                    do
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
                        // required to represent this UTF-16 data. This is similar to the popcnt step
                        // performed by the SSE2 code path. This will overcount surrogates, but we'll
                        // handle that shortly.

                        Vector<ushort> utf16Data = Unsafe.ReadUnaligned<Vector<ushort>>(pInputBuffer);
                        Vector<ushort> twoOrMoreUtf8Bytes = Vector.GreaterThanOrEqual(utf16Data, vector0080);
                        Vector<ushort> threeOrMoreUtf8Bytes = Vector.GreaterThanOrEqual(utf16Data, vector0800);
                        Vector<nuint> sumVector = (Vector<nuint>)(Vector<ushort>.Zero - twoOrMoreUtf8Bytes - threeOrMoreUtf8Bytes);

                        // We'll try summing by a natural word (rather than a 16-bit word) at a time,
                        // which should halve the number of operations we must perform.

                        nuint popcnt = 0;
                        for (int i = 0; i < Vector<nuint>.Count; i++)
                        {
                            popcnt += (nuint)sumVector[i];
                        }

                        uint popcnt32 = (uint)popcnt;
                        if (IntPtr.Size == 8)
                        {
                            popcnt32 += (uint)(popcnt >> 32);
                        }

                        // As in the SSE4.1 paths, compute popcnt but don't fold it in until we
                        // know there aren't any unpaired surrogates in the input data.

                        popcnt32 = (ushort)popcnt32 + (popcnt32 >> 16);

                        // Now check for surrogates.

                        utf16Data -= vectorD800;
                        Vector<ushort> surrogateChars = Vector.LessThan(utf16Data, vector0800);
                        if (surrogateChars != Vector<ushort>.Zero)
                        {
                            // There's at least one surrogate (high or low) UTF-16 code unit in
                            // the vector. We'll build up additional vectors: 'highSurrogateChars'
                            // and 'lowSurrogateChars', where the elements are 0xFFFF iff the original
                            // UTF-16 code unit was a high or low surrogate, respectively.

                            Vector<ushort> highSurrogateChars = Vector.LessThan(utf16Data, vector0400);
                            Vector<ushort> lowSurrogateChars = Vector.AndNot(surrogateChars, highSurrogateChars);

                            // We want to make sure that each high surrogate code unit is followed by
                            // a low surrogate code unit and each low surrogate code unit follows a
                            // high surrogate code unit. Since we don't have an equivalent of pmovmskb
                            // or palignr available to us, we'll do this as a loop. We won't look at
                            // the very last high surrogate char element since we don't yet know if
                            // the next vector read will have a low surrogate char element.

                            if (lowSurrogateChars[0] != 0)
                            {
                                goto Error; // error: start of buffer contains standalone low surrogate char
                            }

                            ushort surrogatePairsCount = 0;
                            for (int i = 0; i < Vector<ushort>.Count - 1; i++)
                            {
                                surrogatePairsCount -= highSurrogateChars[i]; // turns into +1 or +0
                                if (highSurrogateChars[i] != lowSurrogateChars[i + 1])
                                {
                                    goto NonVectorizedLoop; // error: mismatched surrogate pair; break out of vectorized logic
                                }
                            }

                            if (highSurrogateChars[Vector<ushort>.Count - 1] != 0)
                            {
                                // There was a standalone high surrogate at the end of the vector.
                                // We'll adjust our counters so that we don't consider this char consumed.

                                pInputBuffer--;
                                popcnt32 -= 2;
                            }

                            nint surrogatePairsCountNint = (nint)surrogatePairsCount; // zero-extend to native int size

                            // 2 UTF-16 chars become 1 Unicode scalar

                            tempScalarCountAdjustment -= (int)surrogatePairsCountNint;

                            // Since each surrogate code unit was >= 0x0800, we eagerly assumed
                            // it'd be encoded as 3 UTF-8 code units. Each surrogate half is only
                            // encoded as 2 UTF-8 code units (for 4 UTF-8 code units total),
                            // so we'll adjust this now.

                            tempUtf8CodeUnitCountAdjustment -= surrogatePairsCountNint;
                            tempUtf8CodeUnitCountAdjustment -= surrogatePairsCountNint;
                        }

                        tempUtf8CodeUnitCountAdjustment += popcnt32;
                        pInputBuffer += Vector<ushort>.Count;
                    } while (pInputBuffer <= pHighestAddressWhereCanReadOneVector);
                }
            }

        NonVectorizedLoop:

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetNonAsciiBytes(Vector128<byte> value, Vector128<byte> bitMask128)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);

            Vector128<byte> mostSignificantBitIsSet = AdvSimd.ShiftRightArithmetic(value.AsSByte(), 7).AsByte();
            Vector128<byte> extractedBits = AdvSimd.And(mostSignificantBitIsSet, bitMask128);

            // self-pairwise add until all flags have moved to the first two bytes of the vector
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            return extractedBits.AsUInt16().ToScalar();
        }
    }
}
