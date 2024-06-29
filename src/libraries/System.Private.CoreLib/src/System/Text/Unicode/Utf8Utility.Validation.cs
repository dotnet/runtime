// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Text.Unicode
{
    internal static unsafe partial class Utf8Utility
    {
        // Returns &inputBuffer[inputLength] if the input buffer is valid.
        /// <summary>
        /// Given an input buffer <paramref name="pInputBuffer"/> of byte length <paramref name="inputLength"/>,
        /// returns a pointer to where the first invalid data appears in <paramref name="pInputBuffer"/>.
        /// </summary>
        /// <remarks>
        /// Returns a pointer to the end of <paramref name="pInputBuffer"/> if the buffer is well-formed.
        /// </remarks>
        public static byte* GetPointerToFirstInvalidByte(byte* pInputBuffer, int inputLength, out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment)
        {
            Debug.Assert(inputLength >= 0, "Input length must not be negative.");
            Debug.Assert(pInputBuffer != null || inputLength == 0, "Input length must be zero if input buffer pointer is null.");

            // First, try to drain off as many ASCII bytes as we can from the beginning.
            nuint numAsciiBytesCounted = Ascii.GetIndexOfFirstNonAsciiByte(pInputBuffer, (uint)inputLength);
            pInputBuffer += numAsciiBytesCounted;

            // Quick check - did we just end up consuming the entire input buffer?
            // If so, short-circuit the remainder of the method.

            inputLength -= (int)numAsciiBytesCounted;
            if (inputLength == 0)
            {
                utf16CodeUnitCountAdjustment = 0;
                scalarCountAdjustment = 0;
                return pInputBuffer;
            }

            if (BitConverter.IsLittleEndian && inputLength > 128)
            {
                if (AdvSimd.Arm64.IsSupported)
                {
                    return GetPointerToFirstInvalidByteArm64(pInputBuffer, inputLength, out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
                }
                if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
                {
                    return GetPointerToFirstInvalidByteAvx512(pInputBuffer, inputLength, out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
                }
                if (Avx2.IsSupported)
                {
                    return GetPointerToFirstInvalidByteAvx2(pInputBuffer, inputLength, out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
                }
                if (Sse41.IsSupported)
                {
                    return GetPointerToFirstInvalidByteSse(pInputBuffer, inputLength, out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
                }
            }
            return GetPointerToFirstInvalidByteFallback(pInputBuffer, inputLength, out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
        }

        private static byte* GetPointerToFirstInvalidByteFallback(byte* pInputBuffer, int inputLength, out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment)
        {
            Debug.Assert(inputLength >= 0, "Input length must not be negative.");
            Debug.Assert(pInputBuffer != null || inputLength == 0, "Input length must be zero if input buffer pointer is null.");

#if DEBUG
            // Keep these around for final validation at the end of the method.
            byte* pOriginalInputBuffer = pInputBuffer;
            int originalInputLength = inputLength;
#endif

            // Enregistered locals that we'll eventually out to our caller.

            int tempUtf16CodeUnitCountAdjustment = 0;
            int tempScalarCountAdjustment = 0;

            if (inputLength < sizeof(uint))
            {
                goto ProcessInputOfLessThanDWordSize;
            }

            byte* pFinalPosWhereCanReadDWordFromInputBuffer = pInputBuffer + (uint)inputLength - sizeof(uint);

            // Begin the main loop.

#if DEBUG
            byte* pLastBufferPosProcessed = null; // used for invariant checking in debug builds
#endif

            while (pInputBuffer <= pFinalPosWhereCanReadDWordFromInputBuffer)
            {
                // Read 32 bits at a time. This is enough to hold any possible UTF8-encoded scalar.

                uint thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);

            AfterReadDWord:

#if DEBUG
                Debug.Assert(pLastBufferPosProcessed < pInputBuffer, "Algorithm should've made forward progress since last read.");
                pLastBufferPosProcessed = pInputBuffer;
#endif

                // First, check for the common case of all-ASCII bytes.

                if (Ascii.AllBytesInUInt32AreAscii(thisDWord))
                {
                    // We read an all-ASCII sequence.

                    pInputBuffer += sizeof(uint);

                    // If we saw a sequence of all ASCII, there's a good chance a significant amount of following data is also ASCII.
                    // Below is basically unrolled loops with poor man's vectorization.

                    // Below check is "can I read at least five DWORDs from the input stream?"
                    // n.b. Since we incremented pInputBuffer above the below subtraction may result in a negative value,
                    // hence using nint instead of nuint.

                    if ((nint)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) >= 4 * sizeof(uint))
                    {
                        // We want reads in the inner loop to be aligned. So let's perform a quick
                        // ASCII check of the next 32 bits (4 bytes) now, and if that succeeds bump
                        // the read pointer up to the next aligned address.

                        thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                        if (!Ascii.AllBytesInUInt32AreAscii(thisDWord))
                        {
                            goto AfterReadDWordSkipAllBytesAsciiCheck;
                        }

                        pInputBuffer = (byte*)((nuint)(pInputBuffer + 4) & ~(nuint)3);

                        // At this point, the input buffer offset points to an aligned DWORD. We also know that there's
                        // enough room to read at least four DWORDs from the buffer. (Heed the comment a few lines above:
                        // the original 'if' check confirmed that there were 5 DWORDs before the alignment check, and
                        // the alignment check consumes at most a single DWORD.)

                        byte* pInputBufferFinalPosAtWhichCanSafelyLoop = pFinalPosWhereCanReadDWordFromInputBuffer - 3 * sizeof(uint); // can safely read 4 DWORDs here
                        nuint trailingZeroCount;

                        // pInputBuffer is 32-bit aligned but not necessary 128-bit aligned, so we're
                        // going to perform an unaligned load. We don't necessarily care about aligning
                        // this because we pessimistically assume we'll encounter non-ASCII data at some
                        // point in the not-too-distant future (otherwise we would've stayed entirely
                        // within the all-ASCII vectorized code at the entry to this method).
                        if (AdvSimd.Arm64.IsSupported && BitConverter.IsLittleEndian)
                        {
                            // declare bitMask128 inside of the AdvSimd.Arm64.IsSupported check
                            // so it gets removed on non-Arm64 builds.
                            Vector128<byte> bitMask128 = BitConverter.IsLittleEndian ?
                                Vector128.Create((ushort)0x1001).AsByte() :
                                Vector128.Create((ushort)0x0110).AsByte();
                            do
                            {
                                ulong mask = GetNonAsciiBytes(AdvSimd.LoadVector128(pInputBuffer), bitMask128);
                                if (mask != 0)
                                {
                                    trailingZeroCount = (nuint)BitOperations.TrailingZeroCount(mask) >> 2;
                                    goto LoopTerminatedEarlyDueToNonAsciiData;
                                }

                                pInputBuffer += 4 * sizeof(uint); // consumed 4 DWORDs
                            } while (pInputBuffer <= pInputBufferFinalPosAtWhichCanSafelyLoop);
                        }
                        else
                        {
                            do
                            {
                                if (Sse2.IsSupported)
                                {
                                    uint mask = (uint)Sse2.MoveMask(Sse2.LoadVector128(pInputBuffer));
                                    if (mask != 0)
                                    {
                                        trailingZeroCount = (nuint)BitOperations.TrailingZeroCount(mask);
                                        goto LoopTerminatedEarlyDueToNonAsciiData;
                                    }
                                }
                                else
                                {
                                    if (!Ascii.AllBytesInUInt32AreAscii(((uint*)pInputBuffer)[0] | ((uint*)pInputBuffer)[1]))
                                    {
                                        goto LoopTerminatedEarlyDueToNonAsciiDataInFirstPair;
                                    }

                                    if (!Ascii.AllBytesInUInt32AreAscii(((uint*)pInputBuffer)[2] | ((uint*)pInputBuffer)[3]))
                                    {
                                        goto LoopTerminatedEarlyDueToNonAsciiDataInSecondPair;
                                    }
                                }

                                pInputBuffer += 4 * sizeof(uint); // consumed 4 DWORDs
                            } while (pInputBuffer <= pInputBufferFinalPosAtWhichCanSafelyLoop);
                        }

                        continue; // need to perform a bounds check because we might be running out of data

                    LoopTerminatedEarlyDueToNonAsciiData:
                        // x86 can only be little endian, while ARM can be big or little endian
                        // so if we reached this label we need to check both combinations are supported
                        Debug.Assert((AdvSimd.Arm64.IsSupported && BitConverter.IsLittleEndian) || Sse2.IsSupported);


                        // The 'mask' value will have a 0 bit for each ASCII byte we saw and a 1 bit
                        // for each non-ASCII byte we saw. trailingZeroCount will count the number of ASCII bytes,
                        // bump our input counter by that amount, and resume processing from the
                        // "the first byte is no longer ASCII" portion of the main loop.
                        // We should not expect a total number of zeroes equal or larger than 16.
                        Debug.Assert(trailingZeroCount < 16);

                        pInputBuffer += trailingZeroCount;
                        if (pInputBuffer > pFinalPosWhereCanReadDWordFromInputBuffer)
                        {
                            goto ProcessRemainingBytesSlow;
                        }

                        thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer); // no longer guaranteed to be aligned
                        goto BeforeProcessTwoByteSequence;

                    LoopTerminatedEarlyDueToNonAsciiDataInSecondPair:

                        pInputBuffer += 2 * sizeof(uint); // consumed 2 DWORDs

                    LoopTerminatedEarlyDueToNonAsciiDataInFirstPair:

                        // We know that there's *at least* two DWORDs of data remaining in the buffer.
                        // We also know that one of them (or both of them) contains non-ASCII data somewhere.
                        // Let's perform a quick check here to bypass the logic at the beginning of the main loop.

                        thisDWord = *(uint*)pInputBuffer; // still aligned here
                        if (Ascii.AllBytesInUInt32AreAscii(thisDWord))
                        {
                            pInputBuffer += sizeof(uint); // consumed 1 more DWORD
                            thisDWord = *(uint*)pInputBuffer; // still aligned here
                        }

                        goto AfterReadDWordSkipAllBytesAsciiCheck;
                    }

                    continue; // not enough data remaining to unroll loop - go back to beginning with bounds checks
                }

            AfterReadDWordSkipAllBytesAsciiCheck:

                Debug.Assert(!Ascii.AllBytesInUInt32AreAscii(thisDWord)); // this should have been handled earlier

                // Next, try stripping off ASCII bytes one at a time.
                // We only handle up to three ASCII bytes here since we handled the four ASCII byte case above.

                {
                    uint numLeadingAsciiBytes = Ascii.CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(thisDWord);
                    pInputBuffer += numLeadingAsciiBytes;

                    if (pFinalPosWhereCanReadDWordFromInputBuffer < pInputBuffer)
                    {
                        goto ProcessRemainingBytesSlow; // Input buffer doesn't contain enough data to read a DWORD
                    }
                    else
                    {
                        // The input buffer at the current offset contains a non-ASCII byte.
                        // Read an entire DWORD and fall through to multi-byte consumption logic.
                        thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                    }
                }

            BeforeProcessTwoByteSequence:

                // At this point, we suspect we're working with a multi-byte code unit sequence,
                // but we haven't yet validated it for well-formedness.

                // The masks and comparands are derived from the Unicode Standard, Table 3-6.
                // Additionally, we need to check for valid byte sequences per Table 3-7.

                // Check the 2-byte case.

                thisDWord -= (BitConverter.IsLittleEndian) ? 0x0000_80C0u : 0xC080_0000u;
                if ((thisDWord & (BitConverter.IsLittleEndian ? 0x0000_C0E0u : 0xE0C0_0000u)) == 0)
                {
                    // Per Table 3-7, valid sequences are:
                    // [ C2..DF ] [ 80..BF ]
                    //
                    // Due to our modification of 'thisDWord' above, this becomes:
                    // [ 02..1F ] [ 00..3F ]
                    //
                    // We've already checked that the leading byte was originally in the range [ C0..DF ]
                    // and that the trailing byte was originally in the range [ 80..BF ], so now we only need
                    // to check that the modified leading byte is >= [ 02 ].

                    if ((BitConverter.IsLittleEndian && (byte)thisDWord < 0x02u)
                        || (!BitConverter.IsLittleEndian && thisDWord < 0x0200_0000u))
                    {
                        goto Error; // overlong form - leading byte was [ C0 ] or [ C1 ]
                    }

                ProcessTwoByteSequenceSkipOverlongFormCheck:

                    // Optimization: If this is a two-byte-per-character language like Cyrillic or Hebrew,
                    // there's a good chance that if we see one two-byte run then there's another two-byte
                    // run immediately after. Let's check that now.

                    // On little-endian platforms, we can check for the two-byte UTF8 mask *and* validate that
                    // the value isn't overlong using a single comparison. On big-endian platforms, we'll need
                    // to validate the mask and validate that the sequence isn't overlong as two separate comparisons.

                    if ((BitConverter.IsLittleEndian && UInt32EndsWithValidUtf8TwoByteSequenceLittleEndian(thisDWord))
                        || (!BitConverter.IsLittleEndian && (UInt32EndsWithUtf8TwoByteMask(thisDWord) && !UInt32EndsWithOverlongUtf8TwoByteSequence(thisDWord))))
                    {
                        // We have two runs of two bytes each.
                        pInputBuffer += 4;
                        tempUtf16CodeUnitCountAdjustment -= 2; // 4 UTF-8 code units -> 2 UTF-16 code units (and 2 scalars)

                        if (pInputBuffer <= pFinalPosWhereCanReadDWordFromInputBuffer)
                        {
                            // Optimization: If we read a long run of two-byte sequences, the next sequence is probably
                            // also two bytes. Check for that first before going back to the beginning of the loop.

                            thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);

                            if (BitConverter.IsLittleEndian)
                            {
                                if (UInt32BeginsWithValidUtf8TwoByteSequenceLittleEndian(thisDWord))
                                {
                                    // The next sequence is a valid two-byte sequence.
                                    goto ProcessTwoByteSequenceSkipOverlongFormCheck;
                                }
                            }
                            else
                            {
                                if (UInt32BeginsWithUtf8TwoByteMask(thisDWord))
                                {
                                    if (UInt32BeginsWithOverlongUtf8TwoByteSequence(thisDWord))
                                    {
                                        goto Error; // The next sequence purports to be a 2-byte sequence but is overlong.
                                    }

                                    goto ProcessTwoByteSequenceSkipOverlongFormCheck;
                                }
                            }

                            // If we reached this point, the next sequence is something other than a valid
                            // two-byte sequence, so go back to the beginning of the loop.
                            goto AfterReadDWord;
                        }
                        else
                        {
                            goto ProcessRemainingBytesSlow; // Running out of data - go down slow path
                        }
                    }

                    // The buffer contains a 2-byte sequence followed by 2 bytes that aren't a 2-byte sequence.
                    // Unlikely that a 3-byte sequence would follow a 2-byte sequence, so perhaps remaining
                    // bytes are ASCII?

                    tempUtf16CodeUnitCountAdjustment--; // 2-byte sequence + (some number of ASCII bytes) -> 1 UTF-16 code units (and 1 scalar) [+ trailing]

                    if (UInt32ThirdByteIsAscii(thisDWord))
                    {
                        if (UInt32FourthByteIsAscii(thisDWord))
                        {
                            pInputBuffer += 4;
                        }
                        else
                        {
                            pInputBuffer += 3;

                            // A two-byte sequence followed by an ASCII byte followed by a non-ASCII byte.
                            // Read in the next DWORD and jump directly to the start of the multi-byte processing block.

                            if (pInputBuffer <= pFinalPosWhereCanReadDWordFromInputBuffer)
                            {
                                thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                                goto BeforeProcessTwoByteSequence;
                            }
                        }
                    }
                    else
                    {
                        pInputBuffer += 2;
                    }

                    continue;
                }

                // Check the 3-byte case.
                // We need to restore the C0 leading byte we stripped out earlier, then we can strip out the expected E0 byte.

                thisDWord -= (BitConverter.IsLittleEndian) ? (0x0080_00E0u - 0x0000_00C0u) : (0xE000_8000u - 0xC000_0000u);
                if ((thisDWord & (BitConverter.IsLittleEndian ? 0x00C0_C0F0u : 0xF0C0_C000u)) == 0)
                {
                ProcessThreeByteSequenceWithCheck:

                    // We assume the caller has confirmed that the bit pattern is representative of a three-byte
                    // sequence, but it may still be overlong or surrogate. We need to check for these possibilities.
                    //
                    // Per Table 3-7, valid sequences are:
                    // [   E0   ] [ A0..BF ] [ 80..BF ]
                    // [ E1..EC ] [ 80..BF ] [ 80..BF ]
                    // [   ED   ] [ 80..9F ] [ 80..BF ]
                    // [ EE..EF ] [ 80..BF ] [ 80..BF ]
                    //
                    // Big-endian examples of using the above validation table:
                    // E0A0 = 1110 0000 1010 0000 => invalid (overlong ) patterns are 1110 0000 100# ####
                    // ED9F = 1110 1101 1001 1111 => invalid (surrogate) patterns are 1110 1101 101# ####
                    // If using the bitmask ......................................... 0000 1111 0010 0000 (=0F20),
                    // Then invalid (overlong) patterns match the comparand ......... 0000 0000 0000 0000 (=0000),
                    // And invalid (surrogate) patterns match the comparand ......... 0000 1101 0010 0000 (=0D20).
                    //
                    // It's ok if the caller has manipulated 'thisDWord' (e.g., by subtracting 0xE0 or 0x80)
                    // as long as they haven't touched the bits we're about to use in our mask checking below.

                    if (BitConverter.IsLittleEndian)
                    {
                        // The "overlong or surrogate" check can be implemented using a single jump, but there's
                        // some overhead to moving the bits into the correct locations in order to perform the
                        // correct comparison, and in practice the processor's branch prediction capability is
                        // good enough that we shouldn't bother. So we'll use two jumps instead.

                        // Can't extract this check into its own helper method because JITter produces suboptimal
                        // assembly, even with aggressive inlining.

                        // Code below becomes 5 instructions: test, jz, lea, test, jz

                        if (((thisDWord & 0x0000_200Fu) == 0) || (((thisDWord - 0x0000_200Du) & 0x0000_200Fu) == 0))
                        {
                            goto Error; // overlong or surrogate
                        }
                    }
                    else
                    {
                        if (((thisDWord & 0x0F20_0000u) == 0) || (((thisDWord - 0x0D20_0000u) & 0x0F20_0000u) == 0))
                        {
                            goto Error; // overlong or surrogate
                        }
                    }

                ProcessSingleThreeByteSequenceSkipOverlongAndSurrogateChecks:

                    // Occasionally one-off ASCII characters like spaces, periods, or newlines will make their way
                    // in to the text. If this happens strip it off now before seeing if the next character
                    // consists of three code units.

                    // Branchless: consume a 3-byte UTF-8 sequence and optionally an extra ASCII byte from the end.

                    nint asciiAdjustment;
                    if (BitConverter.IsLittleEndian)
                    {
                        asciiAdjustment = (int)thisDWord >> 31; // smear most significant bit across entire value
                    }
                    else
                    {
                        asciiAdjustment = (nint)(sbyte)thisDWord >> 7; // smear most significant bit of least significant byte across entire value
                    }

                    // asciiAdjustment = 0 if fourth byte is ASCII; -1 otherwise

                    // Please *DO NOT* reorder the below two lines. It provides extra defense in depth in case this method
                    // is ever changed such that pInputBuffer becomes a 'ref byte' instead of a simple 'byte*'. It's valid
                    // to add 4 before backing up since we already checked previously that the input buffer contains at
                    // least a DWORD's worth of data, so we're not going to run past the end of the buffer where the GC can
                    // no longer track the reference. However, we can't back up before adding 4, since we might back up to
                    // before the start of the buffer, and the GC isn't guaranteed to be able to track this.

                    pInputBuffer += 4; // optimistically, assume consumed a 3-byte UTF-8 sequence plus an extra ASCII byte
                    pInputBuffer += asciiAdjustment; // back up if we didn't actually consume an ASCII byte

                    tempUtf16CodeUnitCountAdjustment -= 2; // 3 (or 4) UTF-8 bytes -> 1 (or 2) UTF-16 code unit (and 1 [or 2] scalar)

                SuccessfullyProcessedThreeByteSequence:

                    if (IntPtr.Size >= 8 && BitConverter.IsLittleEndian)
                    {
                        // x64 little-endian optimization: A three-byte character could indicate CJK text,
                        // which makes it likely that the character following this one is also CJK.
                        // We'll try to process several three-byte sequences at a time.

                        // The check below is really "can we read 9 bytes from the input buffer?" since 'pFinalPos...' is already offset
                        // n.b. The subtraction below could result in a negative value (since we advanced pInputBuffer above), so
                        // use nint instead of nuint.

                        if ((nint)(pFinalPosWhereCanReadDWordFromInputBuffer - pInputBuffer) >= 5)
                        {
                            ulong thisQWord = Unsafe.ReadUnaligned<ulong>(pInputBuffer);

                            // Stage the next 32 bits into 'thisDWord' so that it's ready for us in case we need to jump backward
                            // to a previous location in the loop. This offers defense against reading main memory again (which may
                            // have been modified and could lead to a race condition).

                            thisDWord = (uint)thisQWord;

                            // Is this three 3-byte sequences in a row?
                            // thisQWord = [ 10yyyyyy 1110zzzz | 10xxxxxx 10yyyyyy 1110zzzz | 10xxxxxx 10yyyyyy 1110zzzz ] [ 10xxxxxx ]
                            //               ---- CHAR 3  ----   --------- CHAR 2 ---------   --------- CHAR 1 ---------     -CHAR 3-
                            if ((thisQWord & 0xC0F0_C0C0_F0C0_C0F0ul) == 0x80E0_8080_E080_80E0ul && IsUtf8ContinuationByte(in pInputBuffer[8]))
                            {
                                // Saw a proper bitmask for three incoming 3-byte sequences, perform the
                                // overlong and surrogate sequence checking now.

                                // Check the first character.
                                // If the first character is overlong or a surrogate, fail immediately.

                                if ((((uint)thisQWord & 0x200Fu) == 0) || ((((uint)thisQWord - 0x200Du) & 0x200Fu) == 0))
                                {
                                    goto Error;
                                }

                                // Check the second character.
                                // At this point, we now know the first three bytes represent a well-formed sequence.
                                // If there's an error beyond here, we'll jump back to the "process three known good bytes"
                                // logic.

                                thisQWord >>= 24;
                                if ((((uint)thisQWord & 0x200Fu) == 0) || ((((uint)thisQWord - 0x200Du) & 0x200Fu) == 0))
                                {
                                    goto ProcessSingleThreeByteSequenceSkipOverlongAndSurrogateChecks;
                                }

                                // Check the third character (we already checked that it's followed by a continuation byte).

                                thisQWord >>= 24;
                                if ((((uint)thisQWord & 0x200Fu) == 0) || ((((uint)thisQWord - 0x200Du) & 0x200Fu) == 0))
                                {
                                    goto ProcessSingleThreeByteSequenceSkipOverlongAndSurrogateChecks;
                                }

                                pInputBuffer += 9;
                                tempUtf16CodeUnitCountAdjustment -= 6; // 9 UTF-8 bytes -> 3 UTF-16 code units (and 3 scalars)

                                goto SuccessfullyProcessedThreeByteSequence;
                            }

                            // Is this two 3-byte sequences in a row?
                            // thisQWord = [ ######## ######## | 10xxxxxx 10yyyyyy 1110zzzz | 10xxxxxx 10yyyyyy 1110zzzz ]
                            //                                   --------- CHAR 2 ---------   --------- CHAR 1 ---------
                            if ((thisQWord & 0xC0C0_F0C0_C0F0ul) == 0x8080_E080_80E0ul)
                            {
                                // Saw a proper bitmask for two incoming 3-byte sequences, perform the
                                // overlong and surrogate sequence checking now.

                                // Check the first character.
                                // If the first character is overlong or a surrogate, fail immediately.

                                if ((((uint)thisQWord & 0x200Fu) == 0) || ((((uint)thisQWord - 0x200Du) & 0x200Fu) == 0))
                                {
                                    goto Error;
                                }

                                // Check the second character.
                                // At this point, we now know the first three bytes represent a well-formed sequence.
                                // If there's an error beyond here, we'll jump back to the "process three known good bytes"
                                // logic.

                                thisQWord >>= 24;
                                if ((((uint)thisQWord & 0x200Fu) == 0) || ((((uint)thisQWord - 0x200Du) & 0x200Fu) == 0))
                                {
                                    goto ProcessSingleThreeByteSequenceSkipOverlongAndSurrogateChecks;
                                }

                                pInputBuffer += 6;
                                tempUtf16CodeUnitCountAdjustment -= 4; // 6 UTF-8 bytes -> 2 UTF-16 code units (and 2 scalars)

                                // The next byte in the sequence didn't have a 3-byte marker, so it's probably
                                // an ASCII character. Jump back to the beginning of loop processing.

                                continue;
                            }

                            if (UInt32BeginsWithUtf8ThreeByteMask(thisDWord))
                            {
                                // A single three-byte sequence.
                                goto ProcessThreeByteSequenceWithCheck;
                            }
                            else
                            {
                                // Not a three-byte sequence; perhaps ASCII?
                                goto AfterReadDWord;
                            }
                        }
                    }

                    if (pInputBuffer <= pFinalPosWhereCanReadDWordFromInputBuffer)
                    {
                        thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);

                        // Optimization: A three-byte character could indicate CJK text, which makes it likely
                        // that the character following this one is also CJK. We'll check for a three-byte sequence
                        // marker now and jump directly to three-byte sequence processing if we see one, skipping
                        // all of the logic at the beginning of the loop.

                        if (UInt32BeginsWithUtf8ThreeByteMask(thisDWord))
                        {
                            goto ProcessThreeByteSequenceWithCheck; // Found another [not yet validated] three-byte sequence; process
                        }
                        else
                        {
                            goto AfterReadDWord; // Probably ASCII punctuation or whitespace; go back to start of loop
                        }
                    }
                    else
                    {
                        goto ProcessRemainingBytesSlow; // Running out of data
                    }
                }

                // Assume the 4-byte case, but we need to validate.

                if (BitConverter.IsLittleEndian)
                {
                    thisDWord &= 0xC0C0_FFFFu;

                    // After the above modifications earlier in this method, we expect 'thisDWord'
                    // to have the structure [ 10000000 00000000 00uuzzzz 00010uuu ]. We'll now
                    // perform two checks to confirm this. The first will verify the
                    // [ 10000000 00000000 00###### ######## ] structure by taking advantage of two's
                    // complement representation to perform a single *signed* integer check.

                    if ((int)thisDWord > unchecked((int)0x8000_3FFF))
                    {
                        goto Error; // didn't have three trailing bytes
                    }

                    // Now we want to confirm that 0x01 <= uuuuu (otherwise this is an overlong encoding)
                    // and that uuuuu <= 0x10 (otherwise this is an out-of-range encoding).

                    thisDWord = BitOperations.RotateRight(thisDWord, 8);

                    // Now, thisDWord = [ 00010uuu 10000000 00000000 00uuzzzz ].
                    // The check is now a simple add / cmp / jcc combo.

                    if (!UnicodeUtility.IsInRangeInclusive(thisDWord, 0x1080_0010u, 0x1480_000Fu))
                    {
                        goto Error; // overlong or out-of-range
                    }
                }
                else
                {
                    thisDWord -= 0x80u;

                    // After the above modifications earlier in this method, we expect 'thisDWord'
                    // to have the structure [ 00010uuu 00uuzzzz 00yyyyyy 00xxxxxx ]. We'll now
                    // perform two checks to confirm this. The first will verify the
                    // [ ######## 00###### 00###### 00###### ] structure.

                    if ((thisDWord & 0x00C0_C0C0u) != 0)
                    {
                        goto Error; // didn't have three trailing bytes
                    }

                    // Now we want to confirm that 0x01 <= uuuuu (otherwise this is an overlong encoding)
                    // and that uuuuu <= 0x10 (otherwise this is an out-of-range encoding).
                    // This is a simple range check. (We don't care about the low two bytes.)

                    if (!UnicodeUtility.IsInRangeInclusive(thisDWord, 0x1010_0000u, 0x140F_FFFFu))
                    {
                        goto Error; // overlong or out-of-range
                    }
                }

                // Validation of 4-byte case complete.

                pInputBuffer += 4;
                tempUtf16CodeUnitCountAdjustment -= 2; // 4 UTF-8 bytes -> 2 UTF-16 code units
                tempScalarCountAdjustment--; // 2 UTF-16 code units -> 1 scalar

                continue; // go back to beginning of loop for processing
            }

            goto ProcessRemainingBytesSlow;

        ProcessInputOfLessThanDWordSize:

            Debug.Assert(inputLength < 4);
            nuint inputBufferRemainingBytes = (uint)inputLength;
            goto ProcessSmallBufferCommon;

        ProcessRemainingBytesSlow:

            inputBufferRemainingBytes = (nuint)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) + 4;

        ProcessSmallBufferCommon:

            Debug.Assert(inputBufferRemainingBytes < 4);
            while (inputBufferRemainingBytes > 0)
            {
                uint firstByte = pInputBuffer[0];

                if ((byte)firstByte < 0x80u)
                {
                    // 1-byte (ASCII) case
                    pInputBuffer++;
                    inputBufferRemainingBytes--;
                    continue;
                }
                else if (inputBufferRemainingBytes >= 2)
                {
                    uint secondByte = pInputBuffer[1]; // typed as 32-bit since we perform arithmetic (not just comparisons) on this value
                    if ((byte)firstByte < 0xE0u)
                    {
                        // 2-byte case
                        if ((byte)firstByte >= 0xC2u && IsLowByteUtf8ContinuationByte(secondByte))
                        {
                            pInputBuffer += 2;
                            tempUtf16CodeUnitCountAdjustment--; // 2 UTF-8 bytes -> 1 UTF-16 code unit (and 1 scalar)
                            inputBufferRemainingBytes -= 2;
                            continue;
                        }
                    }
                    else if (inputBufferRemainingBytes >= 3)
                    {
                        if ((byte)firstByte < 0xF0u)
                        {
                            if ((byte)firstByte == 0xE0u)
                            {
                                if (!UnicodeUtility.IsInRangeInclusive(secondByte, 0xA0u, 0xBFu))
                                {
                                    goto Error; // overlong encoding
                                }
                            }
                            else if ((byte)firstByte == 0xEDu)
                            {
                                if (!UnicodeUtility.IsInRangeInclusive(secondByte, 0x80u, 0x9Fu))
                                {
                                    goto Error; // would be a UTF-16 surrogate code point
                                }
                            }
                            else
                            {
                                if (!IsLowByteUtf8ContinuationByte(secondByte))
                                {
                                    goto Error; // first trailing byte doesn't have proper continuation marker
                                }
                            }

                            if (IsUtf8ContinuationByte(in pInputBuffer[2]))
                            {
                                pInputBuffer += 3;
                                tempUtf16CodeUnitCountAdjustment -= 2; // 3 UTF-8 bytes -> 2 UTF-16 code units (and 2 scalars)
                                inputBufferRemainingBytes -= 3;
                                continue;
                            }
                        }
                    }
                }

                // Error - no match.

                goto Error;
            }

            // If we reached this point, we're out of data, and we saw no bad UTF8 sequence.

#if DEBUG
            // Quick check that for the success case we're going to fulfill our contract of returning &inputBuffer[inputLength].
            Debug.Assert(pOriginalInputBuffer + originalInputLength == pInputBuffer, "About to return an unexpected value.");
#endif

        Error:

            // Report back to our caller how far we got before seeing invalid data.
            // (Also used for normal termination when falling out of the loop above.)

            utf16CodeUnitCountAdjustment = tempUtf16CodeUnitCountAdjustment;
            scalarCountAdjustment = tempScalarCountAdjustment;
            return pInputBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static ulong GetNonAsciiBytes(Vector128<byte> value, Vector128<byte> bitMask128)
        {
            if (!AdvSimd.Arm64.IsSupported || !BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException();
            }

            Vector128<byte> mostSignificantBitIsSet = AdvSimd.ShiftRightArithmetic(value.AsSByte(), 7).AsByte();
            Vector128<byte> extractedBits = AdvSimd.And(mostSignificantBitIsSet, bitMask128);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            return extractedBits.AsUInt64().ToScalar();
        }


        // SimdUnicode:


        // We scan the input from buf to len, possibly going back howFarBack bytes, to find the end of
        // a valid UTF-8 sequence. We return buf + len if the buffer is valid, otherwise we return the
        // pointer to the first invalid byte.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe byte* SimpleRewindAndValidateWithErrors(int howFarBack, byte* buf, int len)
        {
            var extraLen = 0;
            var foundLeadingBytes = false;

            for (var i = 0; i <= howFarBack; i++)
            {
                var candidateByte = buf[0 - i];
                foundLeadingBytes = (candidateByte & 0b11000000) != 0b10000000;

                if (foundLeadingBytes)
                {
                    buf -= i;
                    extraLen = i;
                    break;
                }
            }

            if (!foundLeadingBytes) return buf - howFarBack;
            var pos = 0;
            int nextPos;
            uint codePoint = 0;

            len += extraLen;

            while (pos < len)
            {
                var firstByte = buf[pos];

                while (firstByte < 0b10000000)
                {
                    if (++pos == len) return buf + len;
                    firstByte = buf[pos];
                }

                if ((firstByte & 0b11100000) == 0b11000000)
                {
                    nextPos = pos + 2;
                    if (nextPos > len) return buf + pos; // Too short

                    if ((buf[pos + 1] & 0b11000000) != 0b10000000) return buf + pos; // Too short

                    // range check
                    codePoint = ((uint)(firstByte & 0b00011111) << 6) | (uint)(buf[pos + 1] & 0b00111111);
                    if (codePoint < 0x80 || 0x7ff < codePoint) return buf + pos; // Overlong
                }
                else if ((firstByte & 0b11110000) == 0b11100000)
                {
                    nextPos = pos + 3;
                    if (nextPos > len) return buf + pos; // Too short

                    // range check
                    codePoint = ((uint)(firstByte & 0b00001111) << 12) |
                                ((uint)(buf[pos + 1] & 0b00111111) << 6) |
                                (uint)(buf[pos + 2] & 0b00111111);
                    // Either overlong or too large:
                    if (codePoint < 0x800 || 0xffff < codePoint ||
                        (0xd7ff < codePoint && codePoint < 0xe000))
                        return buf + pos;
                    if ((buf[pos + 1] & 0b11000000) != 0b10000000) return buf + pos; // Too short

                    if ((buf[pos + 2] & 0b11000000) != 0b10000000) return buf + pos; // Too short
                }
                else if ((firstByte & 0b11111000) == 0b11110000)
                {
                    nextPos = pos + 4;
                    if (nextPos > len) return buf + pos;
                    if ((buf[pos + 1] & 0b11000000) != 0b10000000) return buf + pos;
                    if ((buf[pos + 2] & 0b11000000) != 0b10000000) return buf + pos;
                    if ((buf[pos + 3] & 0b11000000) != 0b10000000) return buf + pos;
                    // range check
                    codePoint =
                        ((uint)(firstByte & 0b00000111) << 18) | ((uint)(buf[pos + 1] & 0b00111111) << 12) |
                        ((uint)(buf[pos + 2] & 0b00111111) << 6) | (uint)(buf[pos + 3] & 0b00111111);
                    if (codePoint <= 0xffff || 0x10ffff < codePoint) return buf + pos;
                }
                else
                {
                    // we may have a continuation/too long error
                    return buf + pos;
                }

                pos = nextPos;
            }

            return buf + len; // no error
        }

        private const byte TOO_SHORT = 1 << 0;
        private const byte TOO_LONG = 1 << 1;
        private const byte OVERLONG_3 = 1 << 2;
        private const byte SURROGATE = 1 << 4;
        private const byte OVERLONG_2 = 1 << 5;
        private const byte TWO_CONTS = 1 << 7;
        private const byte TOO_LARGE = 1 << 3;
        private const byte TOO_LARGE_1000 = 1 << 6;
        private const byte OVERLONG_4 = 1 << 6;
        private const byte CARRY = TOO_SHORT | TOO_LONG | TWO_CONTS;

        private static (int utfadjust, int scalaradjust) CalculateN2N3FinalSIMDAdjustments(int n4, int contbytes)
        {
            var n3 = -2 * n4 + 2 * contbytes;
            var n2 = n4 - 3 * contbytes;
            var utfadjust = -2 * n4 - 2 * n3 - n2;
            var scalaradjust = -n4;

            return (utfadjust, scalaradjust);
        }

        [CompExactlyDependsOn(typeof(Sse41))]
        public static byte* GetPointerToFirstInvalidByteSse(byte* pInputBuffer, int inputLength,
            out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment)
        {
            var processedLength = 0;
            Debug.Assert(inputLength > 128);
            {
                if (processedLength + 16 < inputLength)
                {
                    var prevInputBlock = Vector128<byte>.Zero;
                    var maxValue = Vector128.Create(
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 0b11110000 - 1, 0b11100000 - 1, 0b11000000 - 1);
                    var prevIncomplete = Sse2.SubtractSaturate(prevInputBlock, maxValue);
                    var shuf1 = Vector128.Create(
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TWO_CONTS, TWO_CONTS, TWO_CONTS, TWO_CONTS,
                        TOO_SHORT | OVERLONG_2,
                        TOO_SHORT,
                        TOO_SHORT | OVERLONG_3 | SURROGATE,
                        TOO_SHORT | TOO_LARGE | TOO_LARGE_1000 | OVERLONG_4);
                    var shuf2 = Vector128.Create(
                        CARRY | OVERLONG_3 | OVERLONG_2 | OVERLONG_4,
                        CARRY | OVERLONG_2,
                        CARRY,
                        CARRY,
                        CARRY | TOO_LARGE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000 | SURROGATE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000);
                    var shuf3 = Vector128.Create(
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT);

                    var thirdByte = Vector128.Create((byte)(0b11100000u - 0x80));
                    var fourthByte = Vector128.Create((byte)(0b11110000u - 0x80));
                    var v0f = Vector128.Create((byte)0x0F);
                    var v80 = Vector128.Create((byte)0x80);
                    /****
                     * So we want to count the number of 4-byte sequences,
                     * the number of 4-byte sequences, 3-byte sequences, and
                     * the number of 2-byte sequences.
                     * We can do it indirectly. We know how many bytes in total
                     * we have (length). Let us assume that the length covers
                     * only complete sequences (we need to adjust otherwise).
                     * We have that
                     *   length = 4 * n4 + 3 * n3 + 2 * n2 + n1
                     * where n1 is the number of 1-byte sequences (ASCII),
                     * n2 is the number of 2-byte sequences, n3 is the number
                     * of 3-byte sequences, and n4 is the number of 4-byte sequences.
                     *
                     * Let ncon be the number of continuation bytes, then we have
                     *  length =  n4 + n3 + n2 + ncon + n1
                     *
                     * We can solve for n2 and n3 in terms of the other variables:
                     * n3 = n1 - 2 * n4 + 2 * ncon - length
                     * n2 = -2 * n1 + n4 - 4 * ncon + 2 * length
                     * Thus we only need to count the number of continuation bytes,
                     * the number of ASCII bytes and the number of 4-byte sequences.
                     * But we need even less because we compute
                     * utfadjust = -2 * n4 - 2 * n3 - n2
                     * so n1 and length cancel out in the end. Thus we only need to compute
                     * n3' =  - 2 * n4 + 2 * ncon
                     * n2' = n4 - 4 * ncon
                     */
                    ////////////
                    // The *block* here is what begins at processedLength and ends
                    // at processedLength/16*16 or when an error occurs.
                    ///////////

                    // The block goes from processedLength to processedLength/16*16.
                    var contbytes = 0; // number of continuation bytes in the block
                    var n4 = 0; // number of 4-byte sequences that start in this block
                    for (; processedLength + 16 <= inputLength; processedLength += 16)
                    {
                        var currentBlock = Vector128.Load(pInputBuffer + processedLength);
                        var mask = Sse2.MoveMask(currentBlock);
                        if (mask == 0)
                        {
                            // We have an ASCII block, no need to process it, but
                            // we need to check if the previous block was incomplete.
                            //
                            if (!Sse41.TestZ(prevIncomplete, prevIncomplete))
                            {
                                var invalidBytePointer = SimpleRewindAndValidateWithErrors(16 - 3,
                                    pInputBuffer + processedLength - 3, inputLength - processedLength + 3);
                                // So the code is correct up to invalidBytePointer
                                if (invalidBytePointer < pInputBuffer + processedLength)
                                    RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4,
                                        ref contbytes);
                                else
                                    AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                                (utf16CodeUnitCountAdjustment, scalarCountAdjustment) =
                                    CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                                return invalidBytePointer;
                            }

                            prevIncomplete = Vector128<byte>.Zero;

                            // Often, we have a lot of ASCII characters in a row.
                            var localasciirun = 16;
                            if (processedLength + localasciirun + 64 <= inputLength)
                            {
                                for (; processedLength + localasciirun + 64 <= inputLength; localasciirun += 64)
                                {
                                    var block1 = Vector128.Load(pInputBuffer + processedLength + localasciirun);
                                    var block2 = Vector128.Load(pInputBuffer + processedLength + localasciirun + 16);
                                    var block3 = Vector128.Load(pInputBuffer + processedLength + localasciirun + 32);
                                    var block4 = Vector128.Load(pInputBuffer + processedLength + localasciirun + 48);

                                    var or = Vector128.BitwiseOr(Vector128.BitwiseOr(block1, block2), Vector128.BitwiseOr(block3, block4));
                                    if (Sse2.MoveMask(or) != 0) break;
                                }

                                processedLength += localasciirun - 16;
                            }
                        }
                        else // Contains non-ASCII characters, we need to do non-trivial processing
                        {
                            // Use SubtractSaturate to effectively compare if bytes in block are greater than markers.
                            // Contains non-ASCII characters, we need to do non-trivial processing
                            var prev1 = Ssse3.AlignRight(currentBlock, prevInputBlock, 16 - 1);
                            var byte_1_high = Ssse3.Shuffle(shuf1,
                                Sse2.ShiftRightLogical(prev1.AsUInt16(), 4).AsByte() & v0f);
                            var byte_1_low = Ssse3.Shuffle(shuf2, prev1 & v0f);
                            var byte_2_high = Ssse3.Shuffle(shuf3,
                                Sse2.ShiftRightLogical(currentBlock.AsUInt16(), 4).AsByte() & v0f);
                            var sc = Vector128.BitwiseAnd(Vector128.BitwiseAnd(byte_1_high, byte_1_low), byte_2_high);
                            var prev2 = Ssse3.AlignRight(currentBlock, prevInputBlock, 16 - 2);
                            var prev3 = Ssse3.AlignRight(currentBlock, prevInputBlock, 16 - 3);
                            prevInputBlock = currentBlock;

                            var isThirdByte = Sse2.SubtractSaturate(prev2, thirdByte);
                            var isFourthByte = Sse2.SubtractSaturate(prev3, fourthByte);
                            var must23 = Vector128.BitwiseOr(isThirdByte, isFourthByte);
                            var must23As80 = Vector128.BitwiseAnd(must23, v80);
                            var error = Vector128.Xor(must23As80, sc);

                            if (!Sse41.TestZ(error, error))
                            {
                                byte* invalidBytePointer;
                                if (processedLength == 0)
                                    invalidBytePointer = SimpleRewindAndValidateWithErrors(0, pInputBuffer + processedLength, inputLength - processedLength);
                                else
                                    invalidBytePointer = SimpleRewindAndValidateWithErrors(3, pInputBuffer + processedLength - 3, inputLength - processedLength + 3);
                                if (invalidBytePointer < pInputBuffer + processedLength)
                                    RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4, ref contbytes);
                                else
                                    AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                                (utf16CodeUnitCountAdjustment, scalarCountAdjustment) = CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                                return invalidBytePointer;
                            }

                            prevIncomplete = Sse2.SubtractSaturate(currentBlock, maxValue);

                            contbytes += (int)BitOperations.PopCount((uint)Sse2.MoveMask(byte_2_high));
                            // We use two instructions (SubtractSaturate and MoveMask) to update n4, with one arithmetic operation.
                            n4 += (int)BitOperations.PopCount(
                                (uint)Sse2.MoveMask(Sse2.SubtractSaturate(currentBlock, fourthByte)));
                        }
                    }

                    // We may still have an error.
                    var hasIncompete = !Sse41.TestZ(prevIncomplete, prevIncomplete);
                    if (processedLength < inputLength || hasIncompete)
                    {
                        byte* invalidBytePointer;
                        if (processedLength == 0 || !hasIncompete)
                            invalidBytePointer = SimpleRewindAndValidateWithErrors(0, pInputBuffer + processedLength,
                                inputLength - processedLength);
                        else
                            invalidBytePointer = SimpleRewindAndValidateWithErrors(3, pInputBuffer + processedLength - 3,
                                inputLength - processedLength + 3);
                        if (invalidBytePointer != pInputBuffer + inputLength)
                        {
                            if (invalidBytePointer < pInputBuffer + processedLength)
                                RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4, ref contbytes);
                            else
                                AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                            (utf16CodeUnitCountAdjustment, scalarCountAdjustment) = CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                            return invalidBytePointer;
                        }

                        AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                    }

                    (utf16CodeUnitCountAdjustment, scalarCountAdjustment) = CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                    return pInputBuffer + inputLength;
                }
            }

            return GetPointerToFirstInvalidByteFallback(pInputBuffer + processedLength, inputLength - processedLength,
                out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
        }

        [CompExactlyDependsOn(typeof(Avx2))]
        public static byte* GetPointerToFirstInvalidByteAvx2(byte* pInputBuffer, int inputLength,
            out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment)
        {
            var processedLength = 0;
            Debug.Assert(inputLength > 128);
            {
                if (processedLength + 32 < inputLength)
                {
                    // We still have work to do!
                    var prevInputBlock = Vector256<byte>.Zero;
                    var maxValue = Vector256.Create(255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 0b11110000 - 1, 0b11100000 - 1, 0b11000000 - 1);
                    var prevIncomplete = Avx2.SubtractSaturate(prevInputBlock, maxValue);
                    var shuf1 = Vector256.Create(TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TWO_CONTS, TWO_CONTS, TWO_CONTS, TWO_CONTS,
                        TOO_SHORT | OVERLONG_2,
                        TOO_SHORT,
                        TOO_SHORT | OVERLONG_3 | SURROGATE,
                        TOO_SHORT | TOO_LARGE | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TWO_CONTS, TWO_CONTS, TWO_CONTS, TWO_CONTS,
                        TOO_SHORT | OVERLONG_2,
                        TOO_SHORT,
                        TOO_SHORT | OVERLONG_3 | SURROGATE,
                        TOO_SHORT | TOO_LARGE | TOO_LARGE_1000 | OVERLONG_4);

                    var shuf2 = Vector256.Create(CARRY | OVERLONG_3 | OVERLONG_2 | OVERLONG_4,
                        CARRY | OVERLONG_2,
                        CARRY,
                        CARRY,
                        CARRY | TOO_LARGE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000 | SURROGATE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | OVERLONG_3 | OVERLONG_2 | OVERLONG_4,
                        CARRY | OVERLONG_2,
                        CARRY,
                        CARRY,
                        CARRY | TOO_LARGE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000 | SURROGATE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000);
                    var shuf3 = Vector256.Create(TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT);

                    var thirdByte = Vector256.Create((byte)(0b11100000u - 0x80));
                    var fourthByte = Vector256.Create((byte)(0b11110000u - 0x80));
                    var v0f = Vector256.Create((byte)0x0F);
                    var v80 = Vector256.Create((byte)0x80);

                    // The block goes from processedLength to processedLength/16*16.
                    var contbytes = 0; // number of continuation bytes in the block
                    var n4 = 0; // number of 4-byte sequences that start in this block
                    for (; processedLength + 32 <= inputLength; processedLength += 32)
                    {
                        var currentBlock = Vector256.Load(pInputBuffer + processedLength);
                        var mask = Avx2.MoveMask(currentBlock);
                        if (mask == 0)
                        {
                            // We have an ASCII block, no need to process it, but
                            // we need to check if the previous block was incomplete.
                            if (!Avx.TestZ(prevIncomplete, prevIncomplete))
                            {
                                var invalidBytePointer = SimpleRewindAndValidateWithErrors(32 - 3,
                                    pInputBuffer + processedLength - 3, inputLength - processedLength + 3);
                                // So the code is correct up to invalidBytePointer
                                if (invalidBytePointer < pInputBuffer + processedLength)
                                    RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4,
                                        ref contbytes);
                                else
                                    AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                                (utf16CodeUnitCountAdjustment, scalarCountAdjustment) =
                                    CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                                return invalidBytePointer;
                            }

                            prevIncomplete = Vector256<byte>.Zero;

                            // Often, we have a lot of ASCII characters in a row.
                            var localasciirun = 32;
                            if (processedLength + localasciirun + 64 <= inputLength)
                            {
                                for (; processedLength + localasciirun + 64 <= inputLength; localasciirun += 64)
                                {
                                    var block1 = Vector256.Load(pInputBuffer + processedLength + localasciirun);
                                    var block2 = Vector256.Load(pInputBuffer + processedLength + localasciirun + 32);
                                    var or = Avx2.Or(block1, block2);
                                    if (Avx2.MoveMask(or) != 0) break;
                                }
                                processedLength += localasciirun - 32;
                            }
                        }
                        else // Contains non-ASCII characters, we need to do non-trivial processing
                        {
                            // Use SubtractSaturate to effectively compare if bytes in block are greater than markers.
                            var shuffled = Avx2.Permute2x128(prevInputBlock, currentBlock, 0x21);
                            prevInputBlock = currentBlock;
                            var prev1 = Avx2.AlignRight(prevInputBlock, shuffled, 16 - 1);
                            // Vector256.Shuffle vs Avx2.Shuffle
                            // https://github.com/dotnet/runtime/blob/1400c1e7a888ea1e710e5c08d55c800e0b04bf8a/docs/coding-guidelines/vectorization-guidelines.md#vector256shuffle-vs-avx2shuffle
                            var byte_1_high = Avx2.Shuffle(shuf1,
                                Avx2.ShiftRightLogical(prev1.AsUInt16(), 4).AsByte() &
                                v0f); // takes the XXXX 0000 part of the previous byte
                            var byte_1_low =
                                Avx2.Shuffle(shuf2, prev1 & v0f); // takes the 0000 XXXX part of the previous part
                            var byte_2_high = Avx2.Shuffle(shuf3,
                                Avx2.ShiftRightLogical(currentBlock.AsUInt16(), 4).AsByte() &
                                v0f); // takes the XXXX 0000 part of the current byte
                            var sc = Avx2.And(Avx2.And(byte_1_high, byte_1_low), byte_2_high);
                            var prev2 = Avx2.AlignRight(prevInputBlock, shuffled, 16 - 2);
                            var prev3 = Avx2.AlignRight(prevInputBlock, shuffled, 16 - 3);
                            var isThirdByte = Avx2.SubtractSaturate(prev2, thirdByte);
                            var isFourthByte = Avx2.SubtractSaturate(prev3, fourthByte);
                            var must23 = Avx2.Or(isThirdByte, isFourthByte);
                            var must23As80 = Avx2.And(must23, v80);
                            var error = Avx2.Xor(must23As80, sc);

                            if (!Avx.TestZ(error, error))
                            {
                                byte* invalidBytePointer;
                                if (processedLength == 0)
                                    invalidBytePointer = SimpleRewindAndValidateWithErrors(0, pInputBuffer + processedLength, inputLength - processedLength);
                                else
                                    invalidBytePointer = SimpleRewindAndValidateWithErrors(3, pInputBuffer + processedLength - 3, inputLength - processedLength + 3);
                                if (invalidBytePointer < pInputBuffer + processedLength)
                                    RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4, ref contbytes);
                                else
                                    AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                                (utf16CodeUnitCountAdjustment, scalarCountAdjustment) = CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                                return invalidBytePointer;
                            }

                            prevIncomplete = Avx2.SubtractSaturate(currentBlock, maxValue);
                            contbytes += (int)BitOperations.PopCount((uint)Avx2.MoveMask(byte_2_high));
                            // We use two instructions (SubtractSaturate and MoveMask) to update n4, with one arithmetic operation.
                            n4 += (int)BitOperations.PopCount(
                                (uint)Avx2.MoveMask(Avx2.SubtractSaturate(currentBlock, fourthByte)));
                        }
                    }

                    // We may still have an error.
                    var hasIncompete = !Avx.TestZ(prevIncomplete, prevIncomplete);
                    if (processedLength < inputLength || hasIncompete)
                    {
                        byte* invalidBytePointer;
                        if (processedLength == 0 || !hasIncompete)
                            invalidBytePointer = SimpleRewindAndValidateWithErrors(0, pInputBuffer + processedLength,
                                inputLength - processedLength);
                        else
                            invalidBytePointer = SimpleRewindAndValidateWithErrors(3, pInputBuffer + processedLength - 3,
                                inputLength - processedLength + 3);
                        if (invalidBytePointer != pInputBuffer + inputLength)
                        {
                            if (invalidBytePointer < pInputBuffer + processedLength)
                                RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4, ref contbytes);
                            else
                                AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                            (utf16CodeUnitCountAdjustment, scalarCountAdjustment) =
                                CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                            return invalidBytePointer;
                        }

                        AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                    }

                    (utf16CodeUnitCountAdjustment, scalarCountAdjustment) =
                        CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                    return pInputBuffer + inputLength;
                }
            }

            return GetPointerToFirstInvalidByteFallback(pInputBuffer + processedLength, inputLength - processedLength,
                out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
        }


        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        public static unsafe byte* GetPointerToFirstInvalidByteAvx512(byte* pInputBuffer, int inputLength,
            out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment)
        {
            var processedLength = 0;
            Debug.Assert(inputLength > 128);
            {
                if (processedLength + 64 < inputLength)
                {
                    var prevInputBlock = Vector512<byte>.Zero;
                    var maxValue = Vector512.Create(
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 0b11110000 - 1, 0b11100000 - 1, 0b11000000 - 1);
                    var prevIncomplete = Avx512BW.SubtractSaturate(prevInputBlock, maxValue);
                    var shuf1 = Vector512.Create(TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TWO_CONTS, TWO_CONTS, TWO_CONTS, TWO_CONTS,
                        TOO_SHORT | OVERLONG_2,
                        TOO_SHORT,
                        TOO_SHORT | OVERLONG_3 | SURROGATE,
                        TOO_SHORT | TOO_LARGE | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TWO_CONTS, TWO_CONTS, TWO_CONTS, TWO_CONTS,
                        TOO_SHORT | OVERLONG_2,
                        TOO_SHORT,
                        TOO_SHORT | OVERLONG_3 | SURROGATE,
                        TOO_SHORT | TOO_LARGE | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TWO_CONTS, TWO_CONTS, TWO_CONTS, TWO_CONTS,
                        TOO_SHORT | OVERLONG_2,
                        TOO_SHORT,
                        TOO_SHORT | OVERLONG_3 | SURROGATE,
                        TOO_SHORT | TOO_LARGE | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TWO_CONTS, TWO_CONTS, TWO_CONTS, TWO_CONTS,
                        TOO_SHORT | OVERLONG_2,
                        TOO_SHORT,
                        TOO_SHORT | OVERLONG_3 | SURROGATE,
                        TOO_SHORT | TOO_LARGE | TOO_LARGE_1000 | OVERLONG_4);

                    var shuf2 = Vector512.Create(
                        CARRY | OVERLONG_3 | OVERLONG_2 | OVERLONG_4,
                        CARRY | OVERLONG_2,
                        CARRY,
                        CARRY,
                        CARRY | TOO_LARGE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000 | SURROGATE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | OVERLONG_3 | OVERLONG_2 | OVERLONG_4,
                        CARRY | OVERLONG_2,
                        CARRY,
                        CARRY,
                        CARRY | TOO_LARGE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000 | SURROGATE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | OVERLONG_3 | OVERLONG_2 | OVERLONG_4,
                        CARRY | OVERLONG_2,
                        CARRY,
                        CARRY,
                        CARRY | TOO_LARGE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000 | SURROGATE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | OVERLONG_3 | OVERLONG_2 | OVERLONG_4,
                        CARRY | OVERLONG_2,
                        CARRY,
                        CARRY,
                        CARRY | TOO_LARGE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000 | SURROGATE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000);
                    var shuf3 = Vector512.Create(TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT);

                    var thirdByte = Vector512.Create((byte)(0b11100000u - 0x80));
                    var fourthByte = Vector512.Create((byte)(0b11110000u - 0x80));
                    var v0f = Vector512.Create((byte)0x0F);
                    var v80 = Vector512.Create((byte)0x80);

                    // The block goes from processedLength to processedLength/16*16.
                    var contbytes = 0; // number of continuation bytes in the block
                    var n4 = 0; // number of 4-byte sequences that start in this block
                    for (; processedLength + 64 <= inputLength; processedLength += 64)
                    {
                        var currentBlock = Vector512.Load(pInputBuffer + processedLength);
                        var mask = currentBlock.ExtractMostSignificantBits();
                        if (mask == 0)
                        {
                            // We have an ASCII block, no need to process it, but
                            // we need to check if the previous block was incomplete.
                            if (Avx512BW.CompareGreaterThan(prevIncomplete, Vector512<byte>.Zero)
                                    .ExtractMostSignificantBits() != 0)
                            {
                                var invalidBytePointer = SimpleRewindAndValidateWithErrors(16 - 3,
                                    pInputBuffer + processedLength - 3, inputLength - processedLength + 3);
                                // So the code is correct up to invalidBytePointer
                                if (invalidBytePointer < pInputBuffer + processedLength)
                                    RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4,
                                        ref contbytes);
                                else
                                    AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                                (utf16CodeUnitCountAdjustment, scalarCountAdjustment) =
                                    CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                                return invalidBytePointer;
                            }

                            prevIncomplete = Vector512<byte>.Zero;

                            // Often, we have a lot of ASCII characters in a row.
                            var localasciirun = 64;
                            if (processedLength + localasciirun + 64 <= inputLength)
                            {
                                for (; processedLength + localasciirun + 64 <= inputLength; localasciirun += 64)
                                {
                                    var block = Vector512.Load(pInputBuffer + processedLength + localasciirun);
                                    if (block.ExtractMostSignificantBits() != 0) break;
                                }
                                processedLength += localasciirun - 64;
                            }
                        }
                        else // Contains non-ASCII characters, we need to do non-trivial processing
                        {
                            // Use SubtractSaturate to effectively compare if bytes in block are greater than markers.
                            var movemask = Vector512.Create(28, 29, 30, 31, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
                            var shuffled = Avx512F
                                .PermuteVar16x32x2(currentBlock.AsInt32(), movemask, prevInputBlock.AsInt32()).AsByte();
                            prevInputBlock = currentBlock;

                            var prev1 = Avx512BW.AlignRight(prevInputBlock, shuffled, 16 - 1);
                            var byte_1_high = Avx512BW.Shuffle(shuf1,
                                Avx512BW.ShiftRightLogical(prev1.AsUInt16(), 4).AsByte() &
                                v0f); // takes the XXXX 0000 part of the previous byte
                            var byte_1_low =
                                Avx512BW.Shuffle(shuf2, prev1 & v0f); // takes the 0000 XXXX part of the previous part
                            var byte_2_high = Avx512BW.Shuffle(shuf3,
                                Avx512BW.ShiftRightLogical(currentBlock.AsUInt16(), 4).AsByte() &
                                v0f); // takes the XXXX 0000 part of the current byte
                            var sc = Avx512F.And(Avx512F.And(byte_1_high, byte_1_low), byte_2_high);
                            var prev2 = Avx512BW.AlignRight(prevInputBlock, shuffled, 16 - 2);
                            var prev3 = Avx512BW.AlignRight(prevInputBlock, shuffled, 16 - 3);
                            var isThirdByte = Avx512BW.SubtractSaturate(prev2, thirdByte);
                            var isFourthByte = Avx512BW.SubtractSaturate(prev3, fourthByte);
                            var must23 = Avx512F.Or(isThirdByte, isFourthByte);
                            var must23As80 = Avx512F.And(must23, v80);
                            var error = Avx512F.Xor(must23As80, sc);

                            if (Avx512BW.CompareGreaterThan(error, Vector512<byte>.Zero).ExtractMostSignificantBits() != 0)
                            {
                                byte* invalidBytePointer;
                                if (processedLength == 0)
                                    invalidBytePointer = SimpleRewindAndValidateWithErrors(0,
                                        pInputBuffer + processedLength, inputLength - processedLength);
                                else
                                    invalidBytePointer = SimpleRewindAndValidateWithErrors(3,
                                        pInputBuffer + processedLength - 3, inputLength - processedLength + 3);
                                if (invalidBytePointer < pInputBuffer + processedLength)
                                    RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4,
                                        ref contbytes);
                                else
                                    AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                                (utf16CodeUnitCountAdjustment, scalarCountAdjustment) =
                                    CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                                return invalidBytePointer;
                            }

                            prevIncomplete = Avx512BW.SubtractSaturate(currentBlock, maxValue);
                            contbytes += BitOperations.PopCount(byte_2_high.ExtractMostSignificantBits());
                            // We use two instructions (SubtractSaturate and ExtractMostSignificantBits) to update n4, with one arithmetic operation.
                            n4 += BitOperations.PopCount(Avx512BW.SubtractSaturate(currentBlock, fourthByte)
                                .ExtractMostSignificantBits());
                        }
                    }

                    // We may still have an error.
                    var hasIncompete = Avx512BW.CompareGreaterThan(prevIncomplete, Vector512<byte>.Zero)
                        .ExtractMostSignificantBits() != 0;
                    if (processedLength < inputLength || hasIncompete)
                    {
                        byte* invalidBytePointer;
                        if (processedLength == 0 || !hasIncompete)
                            invalidBytePointer = SimpleRewindAndValidateWithErrors(0, pInputBuffer + processedLength,
                                inputLength - processedLength);
                        else
                            invalidBytePointer = SimpleRewindAndValidateWithErrors(3, pInputBuffer + processedLength - 3,
                                inputLength - processedLength + 3);
                        if (invalidBytePointer != pInputBuffer + inputLength)
                        {
                            if (invalidBytePointer < pInputBuffer + processedLength)
                                RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4, ref contbytes);
                            else
                                AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                            (utf16CodeUnitCountAdjustment, scalarCountAdjustment) =
                                CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                            return invalidBytePointer;
                        }

                        AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                    }

                    (utf16CodeUnitCountAdjustment, scalarCountAdjustment) =
                        CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                    return pInputBuffer + inputLength;
                }
            }

            return GetPointerToFirstInvalidByteFallback(pInputBuffer + processedLength, inputLength - processedLength,
                out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
        }

        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static byte* GetPointerToFirstInvalidByteArm64(byte* pInputBuffer, int inputLength, out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment)
        {
            int processedLength = 0;
            if (processedLength + 32 < inputLength)
            {
                // We still have work to do!
                Vector128<byte> prevInputBlock = Vector128<byte>.Zero;
                Vector128<byte> maxValue = Vector128.Create(
                        255, 255, 255, 255, 255, 255, 255, 255,
                        255, 255, 255, 255, 255, 0b11110000 - 1, 0b11100000 - 1, 0b11000000 - 1);
                Vector128<byte> prevIncomplete = AdvSimd.SubtractSaturate(prevInputBlock, maxValue);
                Vector128<byte> shuf1 = Vector128.Create(TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TOO_LONG, TOO_LONG, TOO_LONG, TOO_LONG,
                        TWO_CONTS, TWO_CONTS, TWO_CONTS, TWO_CONTS,
                        TOO_SHORT | OVERLONG_2,
                        TOO_SHORT,
                        TOO_SHORT | OVERLONG_3 | SURROGATE,
                        TOO_SHORT | TOO_LARGE | TOO_LARGE_1000 | OVERLONG_4);
                Vector128<byte> shuf2 = Vector128.Create(CARRY | OVERLONG_3 | OVERLONG_2 | OVERLONG_4,
                        CARRY | OVERLONG_2,
                        CARRY,
                        CARRY,
                        CARRY | TOO_LARGE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000 | SURROGATE,
                        CARRY | TOO_LARGE | TOO_LARGE_1000,
                        CARRY | TOO_LARGE | TOO_LARGE_1000);
                Vector128<byte> shuf3 = Vector128.Create(TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE_1000 | OVERLONG_4,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | OVERLONG_3 | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_LONG | OVERLONG_2 | TWO_CONTS | SURROGATE | TOO_LARGE,
                        TOO_SHORT, TOO_SHORT, TOO_SHORT, TOO_SHORT);
                Vector128<byte> thirdByte = Vector128.Create((byte)(0b11100000u - 0x80));
                Vector128<byte> fourthByte = Vector128.Create((byte)(0b11110000u - 0x80));
                Vector128<byte> v0f = Vector128.Create((byte)0x0F);
                Vector128<byte> v80 = Vector128.Create((byte)0x80);
                Vector128<byte> fourthByteMinusOne = Vector128.Create((byte)(0b11110000u - 1));
                Vector128<sbyte> largestcont = Vector128.Create((sbyte)-65); // -65 => 0b10111111
                // Performance note: we could process 64 bytes at a time for better speed in some cases.

                // The block goes from processedLength to processedLength/16*16.
                int contbytes = 0; // number of continuation bytes in the block
                int n4 = 0; // number of 4-byte sequences that start in this block
                /////
                // Design:
                // Instead of updating n4 and contbytes continuously, we accumulate
                // the values in n4v and contv, while using overflowCounter to make
                // sure we do not overflow. This allows you to reach good performance
                // on systems where summing across vectors is slow.
                ////
                Vector128<sbyte> n4v = Vector128<sbyte>.Zero;
                Vector128<sbyte> contv = Vector128<sbyte>.Zero;
                int overflowCounter = 0;
                for (; processedLength + 16 <= inputLength; processedLength += 16)
                {

                    Vector128<byte> currentBlock = AdvSimd.LoadVector128(pInputBuffer + processedLength);
                    if ((currentBlock & v80) == Vector128<byte>.Zero)
                    {
                        // We have an ASCII block, no need to process it, but
                        // we need to check if the previous block was incomplete.
                        if (prevIncomplete != Vector128<byte>.Zero)
                        {
                            contbytes += -AdvSimd.Arm64.AddAcrossWidening(contv).ToScalar();
                            if (n4v != Vector128<sbyte>.Zero)
                            {
                                n4 += -AdvSimd.Arm64.AddAcrossWidening(n4v).ToScalar();
                            }
                            byte* invalidBytePointer = SimpleRewindAndValidateWithErrors(16 - 3, pInputBuffer + processedLength - 3, inputLength - processedLength + 3);
                            // So the code is correct up to invalidBytePointer
                            if (invalidBytePointer < pInputBuffer + processedLength)
                            {
                                RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4, ref contbytes);
                            }
                            else
                            {
                                AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                            }
                            (utf16CodeUnitCountAdjustment, scalarCountAdjustment) = CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                            return invalidBytePointer;
                        }
                        prevIncomplete = Vector128<byte>.Zero;
                        // Often, we have a lot of ASCII characters in a row.
                        int localasciirun = 16;
                        if (processedLength + localasciirun + 16 <= inputLength)
                        {
                            Vector128<byte> block = AdvSimd.LoadVector128(pInputBuffer + processedLength + localasciirun);
                            if (AdvSimd.Arm64.MaxAcross(Vector128.AsUInt32(AdvSimd.And(block, v80))).ToScalar() == 0)
                            {
                                localasciirun += 16;
                                for (; processedLength + localasciirun + 64 <= inputLength; localasciirun += 64)
                                {
                                    Vector128<byte> block1 = AdvSimd.LoadVector128(pInputBuffer + processedLength + localasciirun);
                                    Vector128<byte> block2 = AdvSimd.LoadVector128(pInputBuffer + processedLength + localasciirun + 16);
                                    Vector128<byte> block3 = AdvSimd.LoadVector128(pInputBuffer + processedLength + localasciirun + 32);
                                    Vector128<byte> block4 = AdvSimd.LoadVector128(pInputBuffer + processedLength + localasciirun + 48);
                                    Vector128<byte> or = AdvSimd.Or(AdvSimd.Or(block1, block2), AdvSimd.Or(block3, block4));

                                    if ((or & v80) != Vector128<byte>.Zero)
                                    {
                                        break;
                                    }
                                }

                            }

                            processedLength += localasciirun - 16;
                        }
                    }
                    else
                    {
                        // Contains non-ASCII characters, we need to do non-trivial processing
                        Vector128<byte> prev1 = AdvSimd.ExtractVector128(prevInputBlock, currentBlock, (byte)(16 - 1));
                        // Vector128.Shuffle vs AdvSimd.Arm64.VectorTableLookup: prefer the latter!!!
                        Vector128<byte> byte_1_high = AdvSimd.Arm64.VectorTableLookup(shuf1, AdvSimd.ShiftRightLogical(prev1.AsUInt16(), 4).AsByte() & v0f);
                        Vector128<byte> byte_1_low = AdvSimd.Arm64.VectorTableLookup(shuf2, (prev1 & v0f));
                        Vector128<byte> byte_2_high = AdvSimd.Arm64.VectorTableLookup(shuf3, AdvSimd.ShiftRightLogical(currentBlock.AsUInt16(), 4).AsByte() & v0f);
                        Vector128<byte> sc = AdvSimd.And(AdvSimd.And(byte_1_high, byte_1_low), byte_2_high);
                        Vector128<byte> prev2 = AdvSimd.ExtractVector128(prevInputBlock, currentBlock, (byte)(16 - 2));
                        Vector128<byte> prev3 = AdvSimd.ExtractVector128(prevInputBlock, currentBlock, (byte)(16 - 3));
                        prevInputBlock = currentBlock;
                        Vector128<byte> isThirdByte = AdvSimd.SubtractSaturate(prev2, thirdByte);
                        Vector128<byte> isFourthByte = AdvSimd.SubtractSaturate(prev3, fourthByte);
                        Vector128<byte> must23 = AdvSimd.Or(isThirdByte, isFourthByte);
                        Vector128<byte> must23As80 = AdvSimd.And(must23, v80);
                        Vector128<byte> error = AdvSimd.Xor(must23As80, sc);
                        if (error != Vector128<byte>.Zero)
                        {
                            contbytes += -AdvSimd.Arm64.AddAcrossWidening(contv).ToScalar();
                            if (n4v != Vector128<sbyte>.Zero)
                            {
                                n4 += -AdvSimd.Arm64.AddAcrossWidening(n4v).ToScalar();
                            }
                            byte* invalidBytePointer;
                            if (processedLength == 0)
                            {
                                invalidBytePointer = SimpleRewindAndValidateWithErrors(0, pInputBuffer + processedLength, inputLength - processedLength);
                            }
                            else
                            {
                                invalidBytePointer = SimpleRewindAndValidateWithErrors(3, pInputBuffer + processedLength - 3, inputLength - processedLength + 3);
                            }
                            if (invalidBytePointer < pInputBuffer + processedLength)
                            {
                                RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4, ref contbytes);
                            }
                            else
                            {
                                AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                            }
                            (utf16CodeUnitCountAdjustment, scalarCountAdjustment) = CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                            return invalidBytePointer;
                        }
                        prevIncomplete = AdvSimd.SubtractSaturate(currentBlock, maxValue);
                        contv += AdvSimd.CompareLessThanOrEqual(Vector128.AsSByte(currentBlock), largestcont);
                        n4v += AdvSimd.CompareGreaterThan(currentBlock, fourthByteMinusOne).AsSByte();
                        overflowCounter++;
                        // We have a risk of overflow if overflowCounter reaches 255,
                        // in which case, we empty contv and n4v, and update contbytes and
                        // n4.
                        if (overflowCounter == 0xff)
                        {
                            overflowCounter = 0;
                            contbytes += -AdvSimd.Arm64.AddAcrossWidening(contv).ToScalar();
                            contv = Vector128<sbyte>.Zero;
                            if (n4v != Vector128<sbyte>.Zero)
                            {
                                n4 += -AdvSimd.Arm64.AddAcrossWidening(n4v).ToScalar();
                                n4v = Vector128<sbyte>.Zero;
                            }
                        }
                    }
                }
                contbytes += -AdvSimd.Arm64.AddAcrossWidening(contv).ToScalar();
                if (n4v != Vector128<sbyte>.Zero)
                {
                    n4 += -AdvSimd.Arm64.AddAcrossWidening(n4v).ToScalar();
                }

                bool hasIncompete = (prevIncomplete != Vector128<byte>.Zero);
                if (processedLength < inputLength || hasIncompete)
                {
                    byte* invalidBytePointer;
                    if (processedLength == 0 || !hasIncompete)
                    {
                        invalidBytePointer = SimpleRewindAndValidateWithErrors(0, pInputBuffer + processedLength, inputLength - processedLength);
                    }
                    else
                    {
                        invalidBytePointer = SimpleRewindAndValidateWithErrors(3, pInputBuffer + processedLength - 3, inputLength - processedLength + 3);
                    }
                    if (invalidBytePointer != pInputBuffer + inputLength)
                    {
                        if (invalidBytePointer < pInputBuffer + processedLength)
                        {
                            RemoveCounters(invalidBytePointer, pInputBuffer + processedLength, ref n4, ref contbytes);
                        }
                        else
                        {
                            AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                        }
                        (utf16CodeUnitCountAdjustment, scalarCountAdjustment) = CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                        return invalidBytePointer;
                    }
                    else
                    {
                        AddCounters(pInputBuffer + processedLength, invalidBytePointer, ref n4, ref contbytes);
                    }
                }
                (utf16CodeUnitCountAdjustment, scalarCountAdjustment) = CalculateN2N3FinalSIMDAdjustments(n4, contbytes);
                return pInputBuffer + inputLength;
            }
            return GetPointerToFirstInvalidByteFallback(pInputBuffer + processedLength, inputLength - processedLength,
                out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void RemoveCounters(byte* start, byte* end, ref int n4, ref int contbytes)
        {
            for (var p = start; p < end; p++)
            {
                if ((*p & 0b11000000) == 0b10000000) contbytes -= 1;
                if ((*p & 0b11110000) == 0b11110000) n4 -= 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void AddCounters(byte* start, byte* end, ref int n4, ref int contbytes)
        {
            for (var p = start; p < end; p++)
            {
                if ((*p & 0b11000000) == 0b10000000) contbytes += 1;
                if ((*p & 0b11110000) == 0b11110000) n4 += 1;
            }
        }
    }
}
