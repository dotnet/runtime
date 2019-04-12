// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

#if BIT64
using nint = System.Int64;
using nuint = System.UInt64;
#else // BIT64
using nint = System.Int32;
using nuint = System.UInt32;
#endif // BIT64

namespace System.Text.Unicode
{
    internal static unsafe partial class Utf8Utility
    {
#if DEBUG
        static Utf8Utility()
        {
            Debug.Assert(sizeof(nint) == IntPtr.Size && nint.MinValue < 0, "nint is defined incorrectly.");
            Debug.Assert(sizeof(nuint) == IntPtr.Size && nuint.MinValue == 0, "nuint is defined incorrectly.");

            _ValidateAdditionalNIntDefinitions();
        }
#endif // DEBUG

        // On method return, pInputBufferRemaining and pOutputBufferRemaining will both point to where
        // the next byte would have been consumed from / the next char would have been written to.
        // inputLength in bytes, outputCharsRemaining in chars.
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static OperationStatus TranscodeToUtf16(byte* pInputBuffer, int inputLength, char* pOutputBuffer, int outputCharsRemaining, out byte* pInputBufferRemaining, out char* pOutputBufferRemaining)
        {
            Debug.Assert(inputLength >= 0, "Input length must not be negative.");
            Debug.Assert(pInputBuffer != null || inputLength == 0, "Input length must be zero if input buffer pointer is null.");

            Debug.Assert(outputCharsRemaining >= 0, "Destination length must not be negative.");
            Debug.Assert(pOutputBuffer != null || outputCharsRemaining == 0, "Destination length must be zero if destination buffer pointer is null.");

            // First, try vectorized conversion.

            {
                nuint numElementsConverted = ASCIIUtility.WidenAsciiToUtf16(pInputBuffer, pOutputBuffer, (uint)Math.Min(inputLength, outputCharsRemaining));

                pInputBuffer += numElementsConverted;
                pOutputBuffer += numElementsConverted;

                // Quick check - did we just end up consuming the entire input buffer?
                // If so, short-circuit the remainder of the method.

                if ((int)numElementsConverted == inputLength)
                {
                    pInputBufferRemaining = pInputBuffer;
                    pOutputBufferRemaining = pOutputBuffer;
                    return OperationStatus.Done;
                }

                inputLength -= (int)numElementsConverted;
                outputCharsRemaining -= (int)numElementsConverted;
            }

            if (inputLength < sizeof(uint))
            {
                goto ProcessInputOfLessThanDWordSize;
            }

            byte* pFinalPosWhereCanReadDWordFromInputBuffer = pInputBuffer + (uint)inputLength - 4;

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

                if (ASCIIUtility.AllBytesInUInt32AreAscii(thisDWord))
                {
                    // We read an all-ASCII sequence.

                    if (outputCharsRemaining < sizeof(uint))
                    {
                        goto ProcessRemainingBytesSlow; // running out of space, but may be able to write some data
                    }

                    Widen4AsciiBytesToCharsAndWrite(ref *pOutputBuffer, thisDWord);
                    pInputBuffer += 4;
                    pOutputBuffer += 4;
                    outputCharsRemaining -= 4;

                    // If we saw a sequence of all ASCII, there's a good chance a significant amount of following data is also ASCII.
                    // Below is basically unrolled loops with poor man's vectorization.

                    uint remainingInputBytes = (uint)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) + 4;
                    uint maxIters = Math.Min(remainingInputBytes, (uint)outputCharsRemaining) / (2 * sizeof(uint));
                    uint secondDWord;
                    int i;
                    for (i = 0; (uint)i < maxIters; i++)
                    {
                        // Reading two DWORDs in parallel benchmarked faster than reading a single QWORD.

                        thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                        secondDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer + sizeof(uint));

                        if (!ASCIIUtility.AllBytesInUInt32AreAscii(thisDWord | secondDWord))
                        {
                            goto LoopTerminatedEarlyDueToNonAsciiData;
                        }

                        pInputBuffer += 8;

                        Widen4AsciiBytesToCharsAndWrite(ref pOutputBuffer[0], thisDWord);
                        Widen4AsciiBytesToCharsAndWrite(ref pOutputBuffer[4], secondDWord);

                        pOutputBuffer += 8;
                    }

                    outputCharsRemaining -= 8 * i;

                    continue; // need to perform a bounds check because we might be running out of data

                LoopTerminatedEarlyDueToNonAsciiData:

                    if (ASCIIUtility.AllBytesInUInt32AreAscii(thisDWord))
                    {
                        // The first DWORD contained all-ASCII bytes, so expand it.

                        Widen4AsciiBytesToCharsAndWrite(ref *pOutputBuffer, thisDWord);

                        // continue the outer loop from the second DWORD

                        Debug.Assert(!ASCIIUtility.AllBytesInUInt32AreAscii(secondDWord));
                        thisDWord = secondDWord;

                        pInputBuffer += 4;
                        pOutputBuffer += 4;
                        outputCharsRemaining -= 4;
                    }

                    outputCharsRemaining -= 8 * i;

                    // We know that there's *at least* one DWORD of data remaining in the buffer.
                    // We also know that it's not all-ASCII. We can skip the logic at the beginning of the main loop.

                    goto AfterReadDWordSkipAllBytesAsciiCheck;
                }

            AfterReadDWordSkipAllBytesAsciiCheck:

                Debug.Assert(!ASCIIUtility.AllBytesInUInt32AreAscii(thisDWord)); // this should have been handled earlier

                // Next, try stripping off ASCII bytes one at a time.
                // We only handle up to three ASCII bytes here since we handled the four ASCII byte case above.

                if (UInt32FirstByteIsAscii(thisDWord))
                {
                    if (outputCharsRemaining >= 3)
                    {
                        // Fast-track: we don't need to check the destination length for subsequent
                        // ASCII bytes since we know we can write them all now.

                        uint thisDWordLittleEndian = ToLittleEndian(thisDWord);

                        nuint adjustment = 1;
                        pOutputBuffer[0] = (char)(byte)thisDWordLittleEndian;

                        if (UInt32SecondByteIsAscii(thisDWord))
                        {
                            adjustment++;
                            thisDWordLittleEndian >>= 8;
                            pOutputBuffer[1] = (char)(byte)thisDWordLittleEndian;

                            if (UInt32ThirdByteIsAscii(thisDWord))
                            {
                                adjustment++;
                                thisDWordLittleEndian >>= 8;
                                pOutputBuffer[2] = (char)(byte)thisDWordLittleEndian;
                            }
                        }

                        pInputBuffer += adjustment;
                        pOutputBuffer += adjustment;
                        outputCharsRemaining -= (int)adjustment;
                    }
                    else
                    {
                        // Slow-track: we need to make sure each individual write has enough
                        // of a buffer so that we don't overrun the destination.

                        if (outputCharsRemaining == 0)
                        {
                            goto OutputBufferTooSmall;
                        }

                        uint thisDWordLittleEndian = ToLittleEndian(thisDWord);

                        pInputBuffer++;
                        *pOutputBuffer++ = (char)(byte)thisDWordLittleEndian;
                        outputCharsRemaining--;

                        if (UInt32SecondByteIsAscii(thisDWord))
                        {
                            if (outputCharsRemaining == 0)
                            {
                                goto OutputBufferTooSmall;
                            }

                            pInputBuffer++;
                            thisDWordLittleEndian >>= 8;
                            *pOutputBuffer++ = (char)(byte)thisDWordLittleEndian;

                            // We can perform a small optimization here. We know at this point that
                            // the output buffer is fully consumed (we read two ASCII bytes and wrote
                            // two ASCII chars, and we checked earlier that the destination buffer
                            // can't store a third byte). If the next byte is ASCII, we can jump straight
                            // to the return statement since the end-of-method logic only relies on the
                            // destination buffer pointer -- NOT the output chars remaining count -- being
                            // correct. If the next byte is not ASCII, we'll need to continue with the
                            // rest of the main loop, but we can set the buffer length directly to zero
                            // rather than decrementing it from 1 to 0.

                            Debug.Assert(outputCharsRemaining == 1);

                            if (UInt32ThirdByteIsAscii(thisDWord))
                            {
                                goto OutputBufferTooSmall;
                            }
                            else
                            {
                                outputCharsRemaining = 0;
                            }
                        }
                    }

                    if (pInputBuffer > pFinalPosWhereCanReadDWordFromInputBuffer)
                    {
                        goto ProcessRemainingBytesSlow; // input buffer doesn't contain enough data to read a DWORD
                    }
                    else
                    {
                        // The input buffer at the current offset contains a non-ASCII byte.
                        // Read an entire DWORD and fall through to multi-byte consumption logic.
                        thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                    }
                }

            BeforeProcessTwoByteSequence:

                // At this point, we know we're working with a multi-byte code unit,
                // but we haven't yet validated it.

                // The masks and comparands are derived from the Unicode Standard, Table 3-6.
                // Additionally, we need to check for valid byte sequences per Table 3-7.

                // Check the 2-byte case.

                if (UInt32BeginsWithUtf8TwoByteMask(thisDWord))
                {
                    // Per Table 3-7, valid sequences are:
                    // [ C2..DF ] [ 80..BF ]

                    if (UInt32BeginsWithOverlongUtf8TwoByteSequence(thisDWord))
                    {
                        goto Error;
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

                        if (outputCharsRemaining < 2)
                        {
                            goto ProcessRemainingBytesSlow; // running out of output buffer
                        }

                        Unsafe.WriteUnaligned<uint>(pOutputBuffer, ExtractTwoCharsPackedFromTwoAdjacentTwoByteSequences(thisDWord));

                        pInputBuffer += 4;
                        pOutputBuffer += 2;
                        outputCharsRemaining -= 2;

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

                    uint charToWrite = ExtractCharFromFirstTwoByteSequence(thisDWord); // optimistically compute this now, but don't store until we know dest is large enough

                    if (UInt32ThirdByteIsAscii(thisDWord))
                    {
                        if (UInt32FourthByteIsAscii(thisDWord))
                        {
                            if (outputCharsRemaining < 3)
                            {
                                goto ProcessRemainingBytesSlow; // running out of output buffer
                            }

                            pOutputBuffer[0] = (char)charToWrite;
                            if (BitConverter.IsLittleEndian)
                            {
                                thisDWord >>= 16;
                                pOutputBuffer[1] = (char)(byte)thisDWord;
                                thisDWord >>= 8;
                                pOutputBuffer[2] = (char)thisDWord;
                            }
                            else
                            {
                                pOutputBuffer[2] = (char)(byte)thisDWord;
                                pOutputBuffer[1] = (char)(byte)(thisDWord >> 8);
                            }
                            pInputBuffer += 4;
                            pOutputBuffer += 3;
                            outputCharsRemaining -= 3;

                            continue; // go back to original bounds check and check for ASCII
                        }
                        else
                        {
                            if (outputCharsRemaining < 2)
                            {
                                goto ProcessRemainingBytesSlow; // running out of output buffer
                            }

                            pOutputBuffer[0] = (char)charToWrite;
                            pOutputBuffer[1] = (char)(byte)(thisDWord >> (BitConverter.IsLittleEndian ? 16 : 8));
                            pInputBuffer += 3;
                            pOutputBuffer += 2;
                            outputCharsRemaining -= 2;

                            // A two-byte sequence followed by an ASCII byte followed by a non-ASCII byte.
                            // Read in the next DWORD and jump directly to the start of the multi-byte processing block.

                            if (pFinalPosWhereCanReadDWordFromInputBuffer < pInputBuffer)
                            {
                                goto ProcessRemainingBytesSlow; // Running out of data - go down slow path
                            }
                            else
                            {
                                thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                                goto BeforeProcessTwoByteSequence;
                            }
                        }
                    }
                    else
                    {
                        if (outputCharsRemaining == 0)
                        {
                            goto ProcessRemainingBytesSlow; // running out of output buffer
                        }

                        pOutputBuffer[0] = (char)charToWrite;
                        pInputBuffer += 2;
                        pOutputBuffer += 1;
                        outputCharsRemaining--;

                        if (pFinalPosWhereCanReadDWordFromInputBuffer < pInputBuffer)
                        {
                            goto ProcessRemainingBytesSlow; // Running out of data - go down slow path
                        }
                        else
                        {
                            thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                            goto BeforeProcessThreeByteSequence; // we know the next byte isn't ASCII, and it's not the start of a 2-byte sequence (this was checked above)
                        }
                    }
                }

            // Check the 3-byte case.

            BeforeProcessThreeByteSequence:

                if (UInt32BeginsWithUtf8ThreeByteMask(thisDWord))
                {
                ProcessThreeByteSequenceWithCheck:

                    // We need to check for overlong or surrogate three-byte sequences.
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

                    // At this point, we know the incoming scalar is well-formed.

                    if (outputCharsRemaining == 0)
                    {
                        goto OutputBufferTooSmall; // not enough space in the destination buffer to write
                    }

                    // As an optimization, on compatible platforms check if a second three-byte sequence immediately
                    // follows the one we just read, and if so use BSWAP and BMI2 to extract them together.

                    if (Bmi2.X64.IsSupported)
                    {
                        Debug.Assert(BitConverter.IsLittleEndian, "BMI2 requires little-endian.");

                        // First, check that the leftover byte from the original DWORD is in the range [ E0..EF ], which
                        // would indicate the potential start of a second three-byte sequence.

                        if (((thisDWord - 0xE000_0000u) & 0xF000_0000u) == 0)
                        {
                            // The const '3' below is correct because pFinalPosWhereCanReadDWordFromInputBuffer represents
                            // the final place where we can safely perform a DWORD read, and we want to probe whether it's
                            // safe to read a DWORD beginning at address &pInputBuffer[3].

                            if (outputCharsRemaining > 1 && (nint)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) >= 3)
                            {
                                // We're going to attempt to read a second 3-byte sequence and write them both out simultaneously using PEXT.
                                // We need to check the continuation bit mask on the remaining two bytes (and we may as well check the leading
                                // byte mask again since it's free), then perform overlong + surrogate checks. If the overlong or surrogate
                                // checks fail, we'll fall through to the remainder of the logic which will transcode the original valid
                                // 3-byte UTF-8 sequence we read; and on the next iteration of the loop the validation routine will run again,
                                // fail, and redirect control flow to the error handling logic at the very end of this method.

                                uint secondDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer + 3);

                                if (UInt32BeginsWithUtf8ThreeByteMask(secondDWord)
                                    && ((secondDWord & 0x0000_200Fu) != 0)
                                    && (((secondDWord - 0x0000_200Du) & 0x0000_200Fu) != 0))
                                {
                                    // combinedQWord = [ 1110ZZZZ 10YYYYYY 10XXXXXX ######## | 1110zzzz 10yyyyyy 10xxxxxx ######## ], where xyz are from first DWORD, XYZ are from second DWORD
                                    ulong combinedQWord = ((ulong)BinaryPrimitives.ReverseEndianness(secondDWord) << 32) | BinaryPrimitives.ReverseEndianness(thisDWord);
                                    thisDWord = secondDWord; // store this value in the correct local for the ASCII drain logic

                                    // extractedQWord = [ 00000000 00000000 00000000 00000000 | ZZZZYYYYYYXXXXXX zzzzyyyyyyxxxxxx ]
                                    ulong extractedQWord = Bmi2.X64.ParallelBitExtract(combinedQWord, 0x0F3F3F00_0F3F3F00ul);

                                    Unsafe.WriteUnaligned<uint>(pOutputBuffer, (uint)extractedQWord);
                                    pInputBuffer += 6;
                                    pOutputBuffer += 2;
                                    outputCharsRemaining -= 2;

                                    // Drain any ASCII data following the second three-byte sequence.

                                    goto CheckForAsciiByteAfterThreeByteSequence;
                                }
                            }
                        }
                    }

                    // Couldn't extract 2x three-byte sequences together, just do this one by itself.

                    *pOutputBuffer = (char)ExtractCharFromFirstThreeByteSequence(thisDWord);
                    pInputBuffer += 3;
                    pOutputBuffer += 1;
                    outputCharsRemaining -= 1;

                CheckForAsciiByteAfterThreeByteSequence:

                    // Occasionally one-off ASCII characters like spaces, periods, or newlines will make their way
                    // in to the text. If this happens strip it off now before seeing if the next character
                    // consists of three code units.

                    if (UInt32FourthByteIsAscii(thisDWord))
                    {
                        if (outputCharsRemaining == 0)
                        {
                            goto OutputBufferTooSmall;
                        }

                        if (BitConverter.IsLittleEndian)
                        {
                            *pOutputBuffer = (char)(thisDWord >> 24);
                        }
                        else
                        {
                            *pOutputBuffer = (char)(byte)thisDWord;
                        }

                        pInputBuffer += 1;
                        pOutputBuffer += 1;
                        outputCharsRemaining -= 1;
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
                            goto ProcessThreeByteSequenceWithCheck; // found a three-byte sequence marker; validate and consume
                        }
                        else
                        {
                            goto AfterReadDWord; // probably ASCII punctuation or whitespace
                        }
                    }
                    else
                    {
                        goto ProcessRemainingBytesSlow; // Running out of data - go down slow path
                    }
                }

                // Assume the 4-byte case, but we need to validate.

                {
                    // We need to check for overlong or invalid (over U+10FFFF) four-byte sequences.
                    //
                    // Per Table 3-7, valid sequences are:
                    // [   F0   ] [ 90..BF ] [ 80..BF ] [ 80..BF ]
                    // [ F1..F3 ] [ 80..BF ] [ 80..BF ] [ 80..BF ]
                    // [   F4   ] [ 80..8F ] [ 80..BF ] [ 80..BF ]

                    if (!UInt32BeginsWithUtf8FourByteMask(thisDWord))
                    {
                        goto Error;
                    }

                    // Now check for overlong / out-of-range sequences.

                    if (BitConverter.IsLittleEndian)
                    {
                        // The DWORD we read is [ 10xxxxxx 10yyyyyy 10zzzzzz 11110www ].
                        // We want to get the 'w' byte in front of the 'z' byte so that we can perform
                        // a single range comparison. We'll take advantage of the fact that the JITter
                        // can detect a ROR / ROL operation, then we'll just zero out the bytes that
                        // aren't involved in the range check.

                        uint toCheck = thisDWord & 0x0000_FFFFu;

                        // At this point, toCheck = [ 00000000 00000000 10zzzzzz 11110www ].

                        toCheck = BitOperations.RotateRight(toCheck, 8);

                        // At this point, toCheck = [ 11110www 00000000 00000000 10zzzzzz ].

                        if (!UnicodeUtility.IsInRangeInclusive(toCheck, 0xF000_0090u, 0xF400_008Fu))
                        {
                            goto Error;
                        }
                    }
                    else
                    {
                        if (!UnicodeUtility.IsInRangeInclusive(thisDWord, 0xF090_0000u, 0xF48F_FFFFu))
                        {
                            goto Error;
                        }
                    }

                    // Validation complete.

                    if (outputCharsRemaining < 2)
                    {
                        // There's no point to falling back to the "drain the input buffer" logic, since we know
                        // we can't write anything to the destination. So we'll just exit immediately.
                        goto OutputBufferTooSmall;
                    }

                    Unsafe.WriteUnaligned<uint>(pOutputBuffer, ExtractCharsFromFourByteSequence(thisDWord));

                    pInputBuffer += 4;
                    pOutputBuffer += 2;
                    outputCharsRemaining -= 2;

                    continue; // go back to beginning of loop for processing
                }
            }

        ProcessRemainingBytesSlow:
            inputLength = (int)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) + 4;

        ProcessInputOfLessThanDWordSize:
            while (inputLength > 0)
            {
                uint firstByte = pInputBuffer[0];
                if (firstByte <= 0x7Fu)
                {
                    if (outputCharsRemaining == 0)
                    {
                        goto OutputBufferTooSmall; // we have no hope of writing anything to the output
                    }

                    // 1-byte (ASCII) case
                    *pOutputBuffer = (char)firstByte;

                    pInputBuffer += 1;
                    pOutputBuffer += 1;
                    inputLength -= 1;
                    outputCharsRemaining -= 1;
                    continue;
                }

                // Potentially the start of a multi-byte sequence?

                firstByte -= 0xC2u;
                if ((byte)firstByte <= (0xDFu - 0xC2u))
                {
                    // Potentially a 2-byte sequence?
                    if (inputLength < 2)
                    {
                        goto InputBufferTooSmall; // out of data
                    }

                    uint secondByte = pInputBuffer[1];
                    if (!IsLowByteUtf8ContinuationByte(secondByte))
                    {
                        goto Error; // 2-byte marker not followed by continuation byte
                    }

                    if (outputCharsRemaining == 0)
                    {
                        goto OutputBufferTooSmall; // we have no hope of writing anything to the output
                    }

                    uint asChar = (firstByte << 6) + secondByte + ((0xC2u - 0xC0u) << 6) - 0x80u; // remove UTF-8 markers from scalar
                    *pOutputBuffer = (char)asChar;

                    pInputBuffer += 2;
                    pOutputBuffer += 1;
                    inputLength -= 2;
                    outputCharsRemaining -= 1;
                    continue;
                }
                else if ((byte)firstByte <= (0xEFu - 0xC2u))
                {
                    // Potentially a 3-byte sequence?
                    if (inputLength >= 3)
                    {
                        uint secondByte = pInputBuffer[1];
                        uint thirdByte = pInputBuffer[2];
                        if (!IsLowByteUtf8ContinuationByte(secondByte) || !IsLowByteUtf8ContinuationByte(thirdByte))
                        {
                            goto Error; // 3-byte marker not followed by 2 continuation bytes
                        }

                        // To speed up the validation logic below, we're not going to remove the UTF-8 markers from the partial char just yet.
                        // We account for this in the comparisons below.

                        uint partialChar = (firstByte << 12) + (secondByte << 6);
                        if (partialChar < ((0xE0u - 0xC2u) << 12) + (0xA0u << 6))
                        {
                            goto Error; // this is an overlong encoding; fail
                        }

                        partialChar -= ((0xEDu - 0xC2u) << 12) + (0xA0u << 6); //if partialChar = 0, we're at beginning of UTF-16 surrogate code point range
                        if (partialChar < (0x0800u /* number of code points in UTF-16 surrogate code point range */))
                        {
                            goto Error; // attempted to encode a UTF-16 surrogate code point; fail
                        }

                        if (outputCharsRemaining == 0)
                        {
                            goto OutputBufferTooSmall; // we have no hope of writing anything to the output
                        }

                        // Now restore the full scalar value.

                        partialChar += thirdByte;
                        partialChar += 0xD800; // undo "move to beginning of UTF-16 surrogate code point range" from earlier, fold it with later adds
                        partialChar -= 0x80u; // remove third byte continuation marker

                        *pOutputBuffer = (char)partialChar;

                        pInputBuffer += 3;
                        pOutputBuffer += 1;
                        inputLength -= 3;
                        outputCharsRemaining -= 1;
                        continue;
                    }
                    else if (inputLength >= 2)
                    {
                        uint secondByte = pInputBuffer[1];
                        if (!IsLowByteUtf8ContinuationByte(secondByte))
                        {
                            goto Error; // 3-byte marker not followed by continuation byte
                        }

                        // We can't build up the entire scalar value now, but we can check for overlong / surrogate representations
                        // from just the first two bytes.

                        uint partialChar = (firstByte << 6) + secondByte; // don't worry about fixing up the UTF-8 markers; we'll account for it in the below comparison
                        if (partialChar < ((0xE0u - 0xC2u) << 6) + 0xA0u)
                        {
                            goto Error; // failed overlong check
                        }
                        if (UnicodeUtility.IsInRangeInclusive(partialChar, ((0xEDu - 0xC2u) << 6) + 0xA0u, ((0xEEu - 0xC2u) << 6) + 0x7Fu))
                        {
                            goto Error; // failed surrogate check
                        }
                    }

                    goto InputBufferTooSmall; // out of data
                }
                else if ((byte)firstByte <= (0xF4u - 0xC2u))
                {
                    // Potentially a 4-byte sequence?

                    if (inputLength < 2)
                    {
                        goto InputBufferTooSmall; // ran out of data
                    }

                    uint nextByte = pInputBuffer[1];
                    if (!IsLowByteUtf8ContinuationByte(nextByte))
                    {
                        goto Error; // 4-byte marker not followed by a continuation byte
                    }

                    uint asPartialChar = (firstByte << 6) + nextByte; // don't worry about fixing up the UTF-8 markers; we'll account for it in the below comparison
                    if (!UnicodeUtility.IsInRangeInclusive(asPartialChar, ((0xF0u - 0xC2u) << 6) + 0x90u, ((0xF4u - 0xC2u) << 6) + 0x8Fu))
                    {
                        goto Error; // failed overlong / out-of-range check
                    }

                    if (inputLength < 3)
                    {
                        goto InputBufferTooSmall; // ran out of data
                    }

                    if (!IsLowByteUtf8ContinuationByte(pInputBuffer[2]))
                    {
                        goto Error; // third byte in 4-byte sequence not a continuation byte
                    }

                    if (inputLength < 4)
                    {
                        goto InputBufferTooSmall; // ran out of data
                    }

                    if (!IsLowByteUtf8ContinuationByte(pInputBuffer[3]))
                    {
                        goto Error; // fourth byte in 4-byte sequence not a continuation byte
                    }

                    // If we read a valid astral scalar value, the only way we could've fallen down this code path
                    // is that we didn't have enough output buffer to write the result.

                    goto OutputBufferTooSmall;
                }
                else
                {
                    goto Error; // didn't begin with [ C2 .. F4 ], so invalid multi-byte sequence header byte
                }
            }

            OperationStatus retVal = OperationStatus.Done;
            goto ReturnCommon;

        InputBufferTooSmall:
            retVal = OperationStatus.NeedMoreData;
            goto ReturnCommon;

        OutputBufferTooSmall:
            retVal = OperationStatus.DestinationTooSmall;
            goto ReturnCommon;

        Error:
            retVal = OperationStatus.InvalidData;
            goto ReturnCommon;

        ReturnCommon:
            pInputBufferRemaining = pInputBuffer;
            pOutputBufferRemaining = pOutputBuffer;
            return retVal;
        }

        // On method return, pInputBufferRemaining and pOutputBufferRemaining will both point to where
        // the next char would have been consumed from / the next byte would have been written to.
        // inputLength in chars, outputBytesRemaining in bytes.
        public static OperationStatus TranscodeToUtf8(char* pInputBuffer, int inputLength, byte* pOutputBuffer, int outputBytesRemaining, out char* pInputBufferRemaining, out byte* pOutputBufferRemaining)
        {
            const int CharsPerDWord = sizeof(uint) / sizeof(char);

            Debug.Assert(inputLength >= 0, "Input length must not be negative.");
            Debug.Assert(pInputBuffer != null || inputLength == 0, "Input length must be zero if input buffer pointer is null.");

            Debug.Assert(outputBytesRemaining >= 0, "Destination length must not be negative.");
            Debug.Assert(pOutputBuffer != null || outputBytesRemaining == 0, "Destination length must be zero if destination buffer pointer is null.");

            // First, try vectorized conversion.

            {
                nuint numElementsConverted = ASCIIUtility.NarrowUtf16ToAscii(pInputBuffer, pOutputBuffer, (uint)Math.Min(inputLength, outputBytesRemaining));

                pInputBuffer += numElementsConverted;
                pOutputBuffer += numElementsConverted;

                // Quick check - did we just end up consuming the entire input buffer?
                // If so, short-circuit the remainder of the method.

                if ((int)numElementsConverted == inputLength)
                {
                    pInputBufferRemaining = pInputBuffer;
                    pOutputBufferRemaining = pOutputBuffer;
                    return OperationStatus.Done;
                }

                inputLength -= (int)numElementsConverted;
                outputBytesRemaining -= (int)numElementsConverted;
            }

            if (inputLength < CharsPerDWord)
            {
                goto ProcessInputOfLessThanDWordSize;
            }

            char* pFinalPosWhereCanReadDWordFromInputBuffer = pInputBuffer + (uint)inputLength - CharsPerDWord;

            // Begin the main loop.

#if DEBUG
            char* pLastBufferPosProcessed = null; // used for invariant checking in debug builds
#endif

            uint thisDWord;

            while (pInputBuffer <= pFinalPosWhereCanReadDWordFromInputBuffer)
            {
                // Read 32 bits at a time. This is enough to hold any possible UTF16-encoded scalar.

                thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);

            AfterReadDWord:

#if DEBUG
                Debug.Assert(pLastBufferPosProcessed < pInputBuffer, "Algorithm should've made forward progress since last read.");
                pLastBufferPosProcessed = pInputBuffer;
#endif

                // First, check for the common case of all-ASCII chars.

                if (Utf16Utility.AllCharsInUInt32AreAscii(thisDWord))
                {
                    // We read an all-ASCII sequence (2 chars).

                    if (outputBytesRemaining < 2)
                    {
                        goto ProcessOneCharFromCurrentDWordAndFinish; // running out of space, but may be able to write some data
                    }

                    // The high WORD of the local declared below might be populated with garbage
                    // as a result of our shifts below, but that's ok since we're only going to
                    // write the low WORD.
                    //
                    // [ 00000000 0bbbbbbb | 00000000 0aaaaaaa ] -> [ 00000000 0bbbbbbb | 0bbbbbbb 0aaaaaaa ]
                    // (Same logic works regardless of endianness.)
                    uint valueToWrite = thisDWord | (thisDWord >> 8);

                    Unsafe.WriteUnaligned<ushort>(pOutputBuffer, (ushort)valueToWrite);

                    pInputBuffer += 2;
                    pOutputBuffer += 2;
                    outputBytesRemaining -= 2;

                    // If we saw a sequence of all ASCII, there's a good chance a significant amount of following data is also ASCII.
                    // Below is basically unrolled loops with poor man's vectorization.

                    uint inputCharsRemaining = (uint)(pFinalPosWhereCanReadDWordFromInputBuffer - pInputBuffer) + 2;
                    uint minElementsRemaining = (uint)Math.Min(inputCharsRemaining, outputBytesRemaining);

                    if (Bmi2.X64.IsSupported)
                    {
                        Debug.Assert(BitConverter.IsLittleEndian, "BMI2 requires little-endian.");
                        const ulong PEXT_MASK = 0x00FF00FF_00FF00FFul;

                        // Try reading and writing 8 elements per iteration.
                        uint maxIters = minElementsRemaining / 8;
                        ulong firstQWord, secondQWord;
                        int i;
                        for (i = 0; (uint)i < maxIters; i++)
                        {
                            firstQWord = Unsafe.ReadUnaligned<ulong>(pInputBuffer);
                            secondQWord = Unsafe.ReadUnaligned<ulong>(pInputBuffer + 4);

                            if (!Utf16Utility.AllCharsInUInt64AreAscii(firstQWord | secondQWord))
                            {
                                goto LoopTerminatedDueToNonAsciiData;
                            }

                            Unsafe.WriteUnaligned<uint>(pOutputBuffer, (uint)Bmi2.X64.ParallelBitExtract(firstQWord, PEXT_MASK));
                            Unsafe.WriteUnaligned<uint>(pOutputBuffer + 4, (uint)Bmi2.X64.ParallelBitExtract(secondQWord, PEXT_MASK));

                            pInputBuffer += 8;
                            pOutputBuffer += 8;
                        }

                        outputBytesRemaining -= 8 * i;

                        // Can we perform one more iteration, but reading & writing 4 elements instead of 8?

                        if ((minElementsRemaining & 4) != 0)
                        {
                            secondQWord = Unsafe.ReadUnaligned<ulong>(pInputBuffer);

                            if (!Utf16Utility.AllCharsInUInt64AreAscii(secondQWord))
                            {
                                goto LoopTerminatedDueToNonAsciiDataInSecondQWord;
                            }

                            Unsafe.WriteUnaligned<uint>(pOutputBuffer, (uint)Bmi2.X64.ParallelBitExtract(secondQWord, PEXT_MASK));

                            pInputBuffer += 4;
                            pOutputBuffer += 4;
                            outputBytesRemaining -= 4;
                        }

                        continue; // Go back to beginning of main loop, read data, check for ASCII

                    LoopTerminatedDueToNonAsciiData:

                        outputBytesRemaining -= 8 * i;

                        // First, see if we can drain any ASCII data from the first QWORD.

                        if (Utf16Utility.AllCharsInUInt64AreAscii(firstQWord))
                        {
                            Unsafe.WriteUnaligned<uint>(pOutputBuffer, (uint)Bmi2.X64.ParallelBitExtract(firstQWord, PEXT_MASK));
                            pInputBuffer += 4;
                            pOutputBuffer += 4;
                            outputBytesRemaining -= 4;
                        }
                        else
                        {
                            secondQWord = firstQWord;
                        }

                    LoopTerminatedDueToNonAsciiDataInSecondQWord:

                        Debug.Assert(!Utf16Utility.AllCharsInUInt64AreAscii(secondQWord)); // this condition should've been checked earlier

                        thisDWord = (uint)secondQWord;
                        if (Utf16Utility.AllCharsInUInt32AreAscii(thisDWord))
                        {
                            // [ 00000000 0bbbbbbb | 00000000 0aaaaaaa ] -> [ 00000000 0bbbbbbb | 0bbbbbbb 0aaaaaaa ]
                            Unsafe.WriteUnaligned<ushort>(pOutputBuffer, (ushort)(thisDWord | (thisDWord >> 8)));
                            pInputBuffer += 2;
                            pOutputBuffer += 2;
                            outputBytesRemaining -= 2;
                            thisDWord = (uint)(secondQWord >> 32);
                        }

                        goto AfterReadDWordSkipAllCharsAsciiCheck;
                    }
                    else
                    {
                        // Can't use BMI2 x64, so we'll only read and write 4 elements per iteration.
                        uint maxIters = minElementsRemaining / 4;
                        uint secondDWord;
                        int i;
                        for (i = 0; (uint)i < maxIters; i++)
                        {
                            thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                            secondDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer + 2);

                            if (!Utf16Utility.AllCharsInUInt32AreAscii(thisDWord | secondDWord))
                            {
                                goto LoopTerminatedDueToNonAsciiData;
                            }

                            // [ 00000000 0bbbbbbb | 00000000 0aaaaaaa ] -> [ 00000000 0bbbbbbb | 0bbbbbbb 0aaaaaaa ]
                            // (Same logic works regardless of endianness.)
                            Unsafe.WriteUnaligned<ushort>(pOutputBuffer, (ushort)(thisDWord | (thisDWord >> 8)));
                            Unsafe.WriteUnaligned<ushort>(pOutputBuffer + 2, (ushort)(secondDWord | (secondDWord >> 8)));

                            pInputBuffer += 4;
                            pOutputBuffer += 4;
                        }

                        outputBytesRemaining -= 4 * i;

                        continue; // Go back to beginning of main loop, read data, check for ASCII

                    LoopTerminatedDueToNonAsciiData:

                        outputBytesRemaining -= 4 * i;

                        // First, see if we can drain any ASCII data from the first DWORD.

                        if (Utf16Utility.AllCharsInUInt32AreAscii(thisDWord))
                        {
                            // [ 00000000 0bbbbbbb | 00000000 0aaaaaaa ] -> [ 00000000 0bbbbbbb | 0bbbbbbb 0aaaaaaa ]
                            // (Same logic works regardless of endianness.)
                            Unsafe.WriteUnaligned<ushort>(pOutputBuffer, (ushort)(thisDWord | (thisDWord >> 8)));
                            pInputBuffer += 2;
                            pOutputBuffer += 2;
                            outputBytesRemaining -= 2;
                            thisDWord = secondDWord;
                        }

                        goto AfterReadDWordSkipAllCharsAsciiCheck;
                    }
                }

            AfterReadDWordSkipAllCharsAsciiCheck:

                Debug.Assert(!Utf16Utility.AllCharsInUInt32AreAscii(thisDWord)); // this should have been handled earlier

                // Next, try stripping off the first ASCII char if it exists.
                // We don't check for a second ASCII char since that should have been handled above.

                if (IsFirstCharAscii(thisDWord))
                {
                    if (outputBytesRemaining == 0)
                    {
                        goto OutputBufferTooSmall;
                    }

                    if (BitConverter.IsLittleEndian)
                    {
                        pOutputBuffer[0] = (byte)thisDWord; // extract [ ## ## 00 AA ]
                    }
                    else
                    {
                        pOutputBuffer[0] = (byte)(thisDWord >> 24); // extract [ AA 00 ## ## ]
                    }

                    pInputBuffer += 1;
                    pOutputBuffer += 1;
                    outputBytesRemaining -= 1;

                    if (pInputBuffer > pFinalPosWhereCanReadDWordFromInputBuffer)
                    {
                        goto ProcessNextCharAndFinish; // input buffer doesn't contain enough data to read a DWORD
                    }
                    else
                    {
                        // The input buffer at the current offset contains a non-ASCII char.
                        // Read an entire DWORD and fall through to non-ASCII consumption logic.
                        thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                    }
                }

                // At this point, we know the first char in the buffer is non-ASCII, but we haven't yet validated it.

                if (!IsFirstCharAtLeastThreeUtf8Bytes(thisDWord))
                {
                TryConsumeMultipleTwoByteSequences:

                    // For certain text (Greek, Cyrillic, ...), 2-byte sequences tend to be clustered. We'll try transcoding them in
                    // a tight loop without falling back to the main loop.

                    if (IsSecondCharTwoUtf8Bytes(thisDWord))
                    {
                        // We have two runs of two bytes each.

                        if (outputBytesRemaining < 4)
                        {
                            goto ProcessOneCharFromCurrentDWordAndFinish; // running out of output buffer
                        }

                        Unsafe.WriteUnaligned<uint>(pOutputBuffer, ExtractTwoUtf8TwoByteSequencesFromTwoPackedUtf16Chars(thisDWord));

                        pInputBuffer += 2;
                        pOutputBuffer += 4;
                        outputBytesRemaining -= 4;

                        if (pInputBuffer > pFinalPosWhereCanReadDWordFromInputBuffer)
                        {
                            goto ProcessNextCharAndFinish; // Running out of data - go down slow path
                        }
                        else
                        {
                            // Optimization: If we read a long run of two-byte sequences, the next sequence is probably
                            // also two bytes. Check for that first before going back to the beginning of the loop.

                            thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);

                            if (IsFirstCharTwoUtf8Bytes(thisDWord))
                            {
                                // Validated we have a two-byte sequence coming up
                                goto TryConsumeMultipleTwoByteSequences;
                            }

                            // If we reached this point, the next sequence is something other than a valid
                            // two-byte sequence, so go back to the beginning of the loop.
                            goto AfterReadDWord;
                        }
                    }

                    if (outputBytesRemaining < 2)
                    {
                        goto OutputBufferTooSmall;
                    }

                    Unsafe.WriteUnaligned<ushort>(pOutputBuffer, (ushort)ExtractUtf8TwoByteSequenceFromFirstUtf16Char(thisDWord));

                    // The buffer contains a 2-byte sequence followed by 2 bytes that aren't a 2-byte sequence.
                    // Unlikely that a 3-byte sequence would follow a 2-byte sequence, so perhaps remaining
                    // char is ASCII?

                    if (IsSecondCharAscii(thisDWord))
                    {
                        if (outputBytesRemaining >= 3)
                        {
                            if (BitConverter.IsLittleEndian)
                            {
                                thisDWord >>= 16;
                            }
                            pOutputBuffer[2] = (byte)thisDWord;

                            pInputBuffer += 2;
                            pOutputBuffer += 3;
                            outputBytesRemaining -= 3;

                            continue; // go back to original bounds check and check for ASCII
                        }
                        else
                        {
                            pInputBuffer += 1;
                            pOutputBuffer += 2;
                            goto OutputBufferTooSmall;
                        }
                    }
                    else
                    {
                        pInputBuffer += 1;
                        pOutputBuffer += 2;
                        outputBytesRemaining -= 2;

                        if (pInputBuffer > pFinalPosWhereCanReadDWordFromInputBuffer)
                        {
                            goto ProcessNextCharAndFinish; // Running out of data - go down slow path
                        }
                        else
                        {
                            thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                            goto BeforeProcessThreeByteSequence; // we know the next byte isn't ASCII, and it's not the start of a 2-byte sequence (this was checked above)
                        }
                    }
                }

            // Check the 3-byte case.

            BeforeProcessThreeByteSequence:

                if (!IsFirstCharSurrogate(thisDWord))
                {
                    // Optimization: A three-byte character could indicate CJK text, which makes it likely
                    // that the character following this one is also CJK. We'll perform the check now
                    // rather than jumping to the beginning of the main loop.

                    if (IsSecondCharAtLeastThreeUtf8Bytes(thisDWord))
                    {
                        if (!IsSecondCharSurrogate(thisDWord))
                        {
                            if (outputBytesRemaining < 6)
                            {
                                goto ConsumeSingleThreeByteRun; // not enough space - try consuming as much as we can
                            }

                            WriteTwoUtf16CharsAsTwoUtf8ThreeByteSequences(ref *pOutputBuffer, thisDWord);

                            pInputBuffer += 2;
                            pOutputBuffer += 6;
                            outputBytesRemaining -= 6;

                            // Try to remain in the 3-byte processing loop if at all possible.

                            if (pInputBuffer > pFinalPosWhereCanReadDWordFromInputBuffer)
                            {
                                goto ProcessNextCharAndFinish; // Running out of data - go down slow path
                            }
                            else
                            {
                                thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);

                                if (IsFirstCharAtLeastThreeUtf8Bytes(thisDWord))
                                {
                                    goto BeforeProcessThreeByteSequence;
                                }
                                else
                                {
                                    // Fall back to standard processing loop since we don't know how to optimize this.
                                    goto AfterReadDWord;
                                }
                            }
                        }
                    }

                ConsumeSingleThreeByteRun:

                    if (outputBytesRemaining < 3)
                    {
                        goto OutputBufferTooSmall;
                    }

                    WriteFirstUtf16CharAsUtf8ThreeByteSequence(ref *pOutputBuffer, thisDWord);

                    pInputBuffer += 1;
                    pOutputBuffer += 3;
                    outputBytesRemaining -= 3;

                    // Occasionally one-off ASCII characters like spaces, periods, or newlines will make their way
                    // in to the text. If this happens strip it off now before seeing if the next character
                    // consists of three code units.

                    if (IsSecondCharAscii(thisDWord))
                    {
                        if (outputBytesRemaining == 0)
                        {
                            goto OutputBufferTooSmall;
                        }

                        if (BitConverter.IsLittleEndian)
                        {
                            *pOutputBuffer = (byte)(thisDWord >> 16);
                        }
                        else
                        {
                            *pOutputBuffer = (byte)(thisDWord);
                        }

                        pInputBuffer += 1;
                        pOutputBuffer += 1;
                        outputBytesRemaining -= 1;

                        if (pInputBuffer > pFinalPosWhereCanReadDWordFromInputBuffer)
                        {
                            goto ProcessNextCharAndFinish; // Running out of data - go down slow path
                        }
                        else
                        {
                            thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);

                            if (IsFirstCharAtLeastThreeUtf8Bytes(thisDWord))
                            {
                                goto BeforeProcessThreeByteSequence;
                            }
                            else
                            {
                                // Fall back to standard processing loop since we don't know how to optimize this.
                                goto AfterReadDWord;
                            }
                        }
                    }

                    if (pInputBuffer > pFinalPosWhereCanReadDWordFromInputBuffer)
                    {
                        goto ProcessNextCharAndFinish; // Running out of data - go down slow path
                    }
                    else
                    {
                        thisDWord = Unsafe.ReadUnaligned<uint>(pInputBuffer);
                        goto AfterReadDWordSkipAllCharsAsciiCheck; // we just checked above that this value isn't ASCII
                    }
                }

                // Four byte sequence processing

                if (IsWellFormedUtf16SurrogatePair(thisDWord))
                {
                    if (outputBytesRemaining < 4)
                    {
                        goto OutputBufferTooSmall;
                    }

                    Unsafe.WriteUnaligned<uint>(pOutputBuffer, ExtractFourUtf8BytesFromSurrogatePair(thisDWord));

                    pInputBuffer += 2;
                    pOutputBuffer += 4;
                    outputBytesRemaining -= 4;

                    continue; // go back to beginning of loop for processing
                }

                goto Error; // an ill-formed surrogate sequence: high not followed by low, or low not preceded by high
            }

        ProcessNextCharAndFinish:
            inputLength = (int)(pFinalPosWhereCanReadDWordFromInputBuffer - pInputBuffer) + CharsPerDWord;

        ProcessInputOfLessThanDWordSize:
            Debug.Assert(inputLength < CharsPerDWord);

            if (inputLength == 0)
            {
                goto InputBufferFullyConsumed;
            }

            uint thisChar = *pInputBuffer;
            goto ProcessFinalChar;

        ProcessOneCharFromCurrentDWordAndFinish:
            if (BitConverter.IsLittleEndian)
            {
                thisChar = thisDWord & 0xFFFFu; // preserve only the first char
            }
            else
            {
                thisChar = thisDWord >> 16; // preserve only the first char
            }

        ProcessFinalChar:
            {
                if (thisChar <= 0x7Fu)
                {
                    if (outputBytesRemaining == 0)
                    {
                        goto OutputBufferTooSmall; // we have no hope of writing anything to the output
                    }

                    // 1-byte (ASCII) case
                    *pOutputBuffer = (byte)thisChar;

                    pInputBuffer += 1;
                    pOutputBuffer += 1;
                }
                else if (thisChar < 0x0800u)
                {
                    if (outputBytesRemaining < 2)
                    {
                        goto OutputBufferTooSmall; // we have no hope of writing anything to the output
                    }

                    // 2-byte case
                    pOutputBuffer[1] = (byte)((thisChar & 0x3Fu) | unchecked((uint)(sbyte)0x80)); // [ 10xxxxxx ]
                    pOutputBuffer[0] = (byte)((thisChar >> 6) | unchecked((uint)(sbyte)0xC0)); // [ 110yyyyy ]

                    pInputBuffer += 1;
                    pOutputBuffer += 2;
                }
                else if (!UnicodeUtility.IsSurrogateCodePoint(thisChar))
                {
                    if (outputBytesRemaining < 3)
                    {
                        goto OutputBufferTooSmall; // we have no hope of writing anything to the output
                    }

                    // 3-byte case
                    pOutputBuffer[2] = (byte)((thisChar & 0x3Fu) | unchecked((uint)(sbyte)0x80)); // [ 10xxxxxx ]
                    pOutputBuffer[1] = (byte)(((thisChar >> 6) & 0x3Fu) | unchecked((uint)(sbyte)0x80)); // [ 10yyyyyy ]
                    pOutputBuffer[0] = (byte)((thisChar >> 12) | unchecked((uint)(sbyte)0xE0)); // [ 1110zzzz ]

                    pInputBuffer += 1;
                    pOutputBuffer += 3;
                }
                else if (thisChar <= 0xDBFFu)
                {
                    // UTF-16 high surrogate code point with no trailing data, report incomplete input buffer
                    goto InputBufferTooSmall;
                }
                else
                {
                    // UTF-16 low surrogate code point with no leading data, report error
                    goto Error;
                }
            }

            // There are two ways we can end up here. Either we were running low on input data,
            // or we were running low on space in the destination buffer. If we're running low on
            // input data (label targets ProcessInputOfLessThanDWordSize and ProcessNextCharAndFinish),
            // then the inputLength value is guaranteed to be between 0 and 1, and we should return Done.
            // If we're running low on destination buffer space (label target ProcessOneCharFromCurrentDWordAndFinish),
            // then we didn't modify inputLength since entering the main loop, which means it should
            // still have a value of >= 2. So checking the value of inputLength is all we need to do to determine
            // which of the two scenarios we're in.

            if (inputLength > 1)
            {
                goto OutputBufferTooSmall;
            }

        InputBufferFullyConsumed:
            OperationStatus retVal = OperationStatus.Done;
            goto ReturnCommon;

        InputBufferTooSmall:
            retVal = OperationStatus.NeedMoreData;
            goto ReturnCommon;

        OutputBufferTooSmall:
            retVal = OperationStatus.DestinationTooSmall;
            goto ReturnCommon;

        Error:
            retVal = OperationStatus.InvalidData;
            goto ReturnCommon;

        ReturnCommon:
            pInputBufferRemaining = pInputBuffer;
            pOutputBufferRemaining = pOutputBuffer;
            return retVal;
        }
    }
}
