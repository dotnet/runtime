// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text
{
    internal static partial class Latin1Utility
    {
        /// <summary>
        /// Returns the index in <paramref name="pBuffer"/> where the first non-Latin1 char is found.
        /// Returns <paramref name="bufferLength"/> if the buffer is empty or all-Latin1.
        /// </summary>
        /// <returns>A Latin-1 char is defined as 0x0000 - 0x00FF, inclusive.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe nuint GetIndexOfFirstNonLatin1Char(char* pBuffer, nuint bufferLength /* in chars */)
        {
            // If SSE2 is supported, use those specific intrinsics instead of the generic vectorized
            // code below. This has two benefits: (a) we can take advantage of specific instructions like
            // pmovmskb which we know are optimized, and (b) we can avoid downclocking the processor while
            // this method is running.

            return (Sse2.IsSupported)
                ? GetIndexOfFirstNonLatin1Char_Sse2(pBuffer, bufferLength)
                : GetIndexOfFirstNonLatin1Char_Default(pBuffer, bufferLength);
        }

        private static unsafe nuint GetIndexOfFirstNonLatin1Char_Default(char* pBuffer, nuint bufferLength /* in chars */)
        {
            // Squirrel away the original buffer reference.This method works by determining the exact
            // char reference where non-Latin1 data begins, so we need this base value to perform the
            // final subtraction at the end of the method to get the index into the original buffer.

            char* pOriginalBuffer = pBuffer;

            Debug.Assert(bufferLength <= nuint.MaxValue / sizeof(char));

            // Before we drain off char-by-char, try a generic vectorized loop.
            // Only run the loop if we have at least two vectors we can pull out.

            if (Vector.IsHardwareAccelerated && bufferLength >= 2 * (uint)Vector<ushort>.Count)
            {
                uint SizeOfVectorInChars = (uint)Vector<ushort>.Count; // JIT will make this a const
                uint SizeOfVectorInBytes = (uint)Vector<byte>.Count; // JIT will make this a const

                Vector<ushort> maxLatin1 = new Vector<ushort>(0x00FF);

                if (Vector.LessThanOrEqualAll(Unsafe.ReadUnaligned<Vector<ushort>>(pBuffer), maxLatin1))
                {
                    // The first several elements of the input buffer were Latin-1. Bump up the pointer to the
                    // next aligned boundary, then perform aligned reads from here on out until we find non-Latin-1
                    // data or we approach the end of the buffer. It's possible we'll reread data; this is ok.

                    char* pFinalVectorReadPos = pBuffer + bufferLength - SizeOfVectorInChars;
                    pBuffer = (char*)(((nuint)pBuffer + SizeOfVectorInBytes) & ~(nuint)(SizeOfVectorInBytes - 1));

#if DEBUG
                    long numCharsRead = pBuffer - pOriginalBuffer;
                    Debug.Assert(0 < numCharsRead && numCharsRead <= SizeOfVectorInChars, "We should've made forward progress of at least one char.");
                    Debug.Assert((nuint)numCharsRead <= bufferLength, "We shouldn't have read past the end of the input buffer.");
#endif

                    Debug.Assert(pBuffer <= pFinalVectorReadPos, "Should be able to read at least one vector.");

                    do
                    {
                        Debug.Assert((nuint)pBuffer % SizeOfVectorInChars == 0, "Vector read should be aligned.");
                        if (Vector.GreaterThanAny(Unsafe.Read<Vector<ushort>>(pBuffer), maxLatin1))
                        {
                            break; // found non-Latin-1 data
                        }
                        pBuffer += SizeOfVectorInChars;
                    } while (pBuffer <= pFinalVectorReadPos);

                    // Adjust the remaining buffer length for the number of elements we just consumed.

                    bufferLength -= ((nuint)pBuffer - (nuint)pOriginalBuffer) / sizeof(char);
                }
            }

            // At this point, the buffer length wasn't enough to perform a vectorized search, or we did perform
            // a vectorized search and encountered non-Latin-1 data. In either case go down a non-vectorized code
            // path to drain any remaining Latin-1 chars.
            //
            // We're going to perform unaligned reads, so prefer 32-bit reads instead of 64-bit reads.
            // This also allows us to perform more optimized bit twiddling tricks to count the number of Latin-1 chars.

            uint currentUInt32;

            // Try reading 64 bits at a time in a loop.

            for (; bufferLength >= 4; bufferLength -= 4) // 64 bits = 4 * 16-bit chars
            {
                currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
                uint nextUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer + 4 / sizeof(char));

                if (!AllCharsInUInt32AreLatin1(currentUInt32 | nextUInt32))
                {
                    // One of these two values contains non-Latin-1 chars.
                    // Figure out which one it is, then put it in 'current' so that we can drain the Latin-1 chars.

                    if (AllCharsInUInt32AreLatin1(currentUInt32))
                    {
                        currentUInt32 = nextUInt32;
                        pBuffer += 2;
                    }

                    goto FoundNonLatin1Data;
                }

                pBuffer += 4; // consumed 4 Latin-1 chars
            }

            // From this point forward we don't need to keep track of the remaining buffer length.
            // Try reading 32 bits.

            if ((bufferLength & 2) != 0) // 32 bits = 2 * 16-bit chars
            {
                currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
                if (!AllCharsInUInt32AreLatin1(currentUInt32))
                {
                    goto FoundNonLatin1Data;
                }

                pBuffer += 2;
            }

            // Try reading 16 bits.
            // No need to try an 8-bit read after this since we're working with chars.

            if ((bufferLength & 1) != 0)
            {
                // If the buffer contains non-Latin-1 data, the comparison below will fail, and
                // we'll end up not incrementing the buffer reference.

                if (*pBuffer <= byte.MaxValue)
                {
                    pBuffer++;
                }
            }

        Finish:

            nuint totalNumBytesRead = (nuint)pBuffer - (nuint)pOriginalBuffer;
            Debug.Assert(totalNumBytesRead % sizeof(char) == 0, "Total number of bytes read should be even since we're working with chars.");
            return totalNumBytesRead / sizeof(char); // convert byte count -> char count before returning

        FoundNonLatin1Data:

            Debug.Assert(!AllCharsInUInt32AreLatin1(currentUInt32), "Shouldn't have reached this point if we have an all-Latin-1 input.");

            // We don't bother looking at the second char - only the first char.

            if (FirstCharInUInt32IsLatin1(currentUInt32))
            {
                pBuffer++;
            }

            goto Finish;
        }

        [CompExactlyDependsOn(typeof(Sse2))]
        private static unsafe nuint GetIndexOfFirstNonLatin1Char_Sse2(char* pBuffer, nuint bufferLength /* in chars */)
        {
            // This method contains logic optimized for both SSE2 and SSE41. Much of the logic in this method
            // will be elided by JIT once we determine which specific ISAs we support.

            // Quick check for empty inputs.

            if (bufferLength == 0)
            {
                return 0;
            }

            // JIT turns the below into constants

            uint SizeOfVector128InBytes = (uint)sizeof(Vector128<byte>);
            uint SizeOfVector128InChars = SizeOfVector128InBytes / sizeof(char);

            Debug.Assert(Sse2.IsSupported, "Should've been checked by caller.");
            Debug.Assert(BitConverter.IsLittleEndian, "SSE2 assumes little-endian.");

            Vector128<ushort> firstVector, secondVector;
            uint currentMask;
            char* pOriginalBuffer = pBuffer;

            if (bufferLength < SizeOfVector128InChars)
            {
                goto InputBufferLessThanOneVectorInLength; // can't vectorize; drain primitives instead
            }

            // This method is written such that control generally flows top-to-bottom, avoiding
            // jumps as much as possible in the optimistic case of "all Latin-1". If we see non-Latin-1
            // data, we jump out of the hot paths to targets at the end of the method.

            Vector128<ushort> latin1MaskForTestZ = Vector128.Create((ushort)0xFF00); // used for PTEST on supported hardware
            Vector128<ushort> latin1MaskForAddSaturate = Vector128.Create((ushort)0x7F00); // used for PADDUSW
            const uint NonLatin1DataSeenMask = 0b_1010_1010_1010_1010; // used for determining whether 'currentMask' contains non-Latin-1 data

            Debug.Assert(bufferLength <= nuint.MaxValue / sizeof(char));

            // Read the first vector unaligned.

            firstVector = Sse2.LoadVector128((ushort*)pBuffer); // unaligned load

            // The operation below forces the 0x8000 bit of each WORD to be set iff the WORD element
            // has value >= 0x0100 (non-Latin-1). Then we'll treat the vector as a BYTE vector in order
            // to extract the mask. Reminder: the 0x0080 bit of each WORD should be ignored.

            currentMask = (uint)Sse2.MoveMask(Sse2.AddSaturate(firstVector, latin1MaskForAddSaturate).AsByte());

            if ((currentMask & NonLatin1DataSeenMask) != 0)
            {
                goto FoundNonLatin1DataInCurrentMask;
            }

            // If we have less than 32 bytes to process, just go straight to the final unaligned
            // read. There's no need to mess with the loop logic in the middle of this method.

            // Adjust the remaining length to account for what we just read.
            // For the remainder of this code path, bufferLength will be in bytes, not chars.

            bufferLength <<= 1; // chars to bytes

            if (bufferLength < 2 * SizeOfVector128InBytes)
            {
                goto IncrementCurrentOffsetBeforeFinalUnalignedVectorRead;
            }

            // Now adjust the read pointer so that future reads are aligned.

            pBuffer = (char*)(((nuint)pBuffer + SizeOfVector128InBytes) & ~(nuint)(SizeOfVector128InBytes - 1));

#if DEBUG
            long numCharsRead = pBuffer - pOriginalBuffer;
            Debug.Assert(0 < numCharsRead && numCharsRead <= SizeOfVector128InChars, "We should've made forward progress of at least one char.");
            Debug.Assert((nuint)numCharsRead <= bufferLength, "We shouldn't have read past the end of the input buffer.");
#endif

            // Adjust remaining buffer length.

            bufferLength += (nuint)pOriginalBuffer;
            bufferLength -= (nuint)pBuffer;

            // The buffer is now properly aligned.
            // Read 2 vectors at a time if possible.

            if (bufferLength >= 2 * SizeOfVector128InBytes)
            {
                char* pFinalVectorReadPos = (char*)((nuint)pBuffer + bufferLength - 2 * SizeOfVector128InBytes);

                // After this point, we no longer need to update the bufferLength value.

                do
                {
                    firstVector = Sse2.LoadAlignedVector128((ushort*)pBuffer);
                    secondVector = Sse2.LoadAlignedVector128((ushort*)pBuffer + SizeOfVector128InChars);
                    Vector128<ushort> combinedVector = Sse2.Or(firstVector, secondVector);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Sse41 is considered supported or unsupported
                    if (Sse41.IsSupported)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                    {
                        // If a non-Latin-1 bit is set in any WORD of the combined vector, we have seen non-Latin-1 data.
                        // Jump to the non-Latin-1 handler to figure out which particular vector contained non-Latin-1 data.
                        if (!Sse41.TestZ(combinedVector, latin1MaskForTestZ))
                        {
                            goto FoundNonLatin1DataInFirstOrSecondVector;
                        }
                    }
                    else
                    {
                        // See comment earlier in the method for an explanation of how the below logic works.
                        currentMask = (uint)Sse2.MoveMask(Sse2.AddSaturate(combinedVector, latin1MaskForAddSaturate).AsByte());
                        if ((currentMask & NonLatin1DataSeenMask) != 0)
                        {
                            goto FoundNonLatin1DataInFirstOrSecondVector;
                        }
                    }

                    pBuffer += 2 * SizeOfVector128InChars;
                } while (pBuffer <= pFinalVectorReadPos);
            }

            // We have somewhere between 0 and (2 * vector length) - 1 bytes remaining to read from.
            // Since the above loop doesn't update bufferLength, we can't rely on its absolute value.
            // But we _can_ rely on it to tell us how much remaining data must be drained by looking
            // at what bits of it are set. This works because had we updated it within the loop above,
            // we would've been adding 2 * SizeOfVector128 on each iteration, but we only care about
            // bits which are less significant than those that the addition would've acted on.

            // If there is fewer than one vector length remaining, skip the next aligned read.
            // Remember, at this point bufferLength is measured in bytes, not chars.

            if ((bufferLength & SizeOfVector128InBytes) == 0)
            {
                goto DoFinalUnalignedVectorRead;
            }

            // At least one full vector's worth of data remains, so we can safely read it.
            // Remember, at this point pBuffer is still aligned.

            firstVector = Sse2.LoadAlignedVector128((ushort*)pBuffer);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Sse41 is considered supported or unsupported
            if (Sse41.IsSupported)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                // If a non-Latin-1 bit is set in any WORD of the combined vector, we have seen non-Latin-1 data.
                // Jump to the non-Latin-1 handler to figure out which particular vector contained non-Latin-1 data.
                if (!Sse41.TestZ(firstVector, latin1MaskForTestZ))
                {
                    goto FoundNonLatin1DataInFirstVector;
                }
            }
            else
            {
                // See comment earlier in the method for an explanation of how the below logic works.
                currentMask = (uint)Sse2.MoveMask(Sse2.AddSaturate(firstVector, latin1MaskForAddSaturate).AsByte());
                if ((currentMask & NonLatin1DataSeenMask) != 0)
                {
                    goto FoundNonLatin1DataInCurrentMask;
                }
            }

        IncrementCurrentOffsetBeforeFinalUnalignedVectorRead:

            pBuffer += SizeOfVector128InChars;

        DoFinalUnalignedVectorRead:

            if (((byte)bufferLength & (SizeOfVector128InBytes - 1)) != 0)
            {
                // Perform an unaligned read of the last vector.
                // We need to adjust the pointer because we're re-reading data.

                pBuffer = (char*)((byte*)pBuffer + (bufferLength & (SizeOfVector128InBytes - 1)) - SizeOfVector128InBytes);
                firstVector = Sse2.LoadVector128((ushort*)pBuffer); // unaligned load

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Sse41 is considered supported or unsupported
                if (Sse41.IsSupported)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    // If a non-Latin-1 bit is set in any WORD of the combined vector, we have seen non-Latin-1 data.
                    // Jump to the non-Latin-1 handler to figure out which particular vector contained non-Latin-1 data.
                    if (!Sse41.TestZ(firstVector, latin1MaskForTestZ))
                    {
                        goto FoundNonLatin1DataInFirstVector;
                    }
                }
                else
                {
                    // See comment earlier in the method for an explanation of how the below logic works.
                    currentMask = (uint)Sse2.MoveMask(Sse2.AddSaturate(firstVector, latin1MaskForAddSaturate).AsByte());
                    if ((currentMask & NonLatin1DataSeenMask) != 0)
                    {
                        goto FoundNonLatin1DataInCurrentMask;
                    }
                }

                pBuffer += SizeOfVector128InChars;
            }

        Finish:

            Debug.Assert(((nuint)pBuffer - (nuint)pOriginalBuffer) % 2 == 0, "Shouldn't have incremented any pointer by an odd byte count.");
            return ((nuint)pBuffer - (nuint)pOriginalBuffer) / sizeof(char); // and we're done! (remember to adjust for char count)

        FoundNonLatin1DataInFirstOrSecondVector:

            // We don't know if the first or the second vector contains non-Latin-1 data. Check the first
            // vector, and if that's all-Latin-1 then the second vector must be the culprit. Either way
            // we'll make sure the first vector local is the one that contains the non-Latin-1 data.

            // See comment earlier in the method for an explanation of how the below logic works.
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Sse41 is considered supported or unsupported
            if (Sse41.IsSupported)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                if (!Sse41.TestZ(firstVector, latin1MaskForTestZ))
                {
                    goto FoundNonLatin1DataInFirstVector;
                }
            }
            else
            {
                currentMask = (uint)Sse2.MoveMask(Sse2.AddSaturate(firstVector, latin1MaskForAddSaturate).AsByte());
                if ((currentMask & NonLatin1DataSeenMask) != 0)
                {
                    goto FoundNonLatin1DataInCurrentMask;
                }
            }

            // Wasn't the first vector; must be the second.

            pBuffer += SizeOfVector128InChars;
            firstVector = secondVector;

        FoundNonLatin1DataInFirstVector:

            // See comment earlier in the method for an explanation of how the below logic works.
            currentMask = (uint)Sse2.MoveMask(Sse2.AddSaturate(firstVector, latin1MaskForAddSaturate).AsByte());

        FoundNonLatin1DataInCurrentMask:

            // See comment earlier in the method accounting for the 0x8000 and 0x0080 bits set after the WORD-sized operations.

            currentMask &= NonLatin1DataSeenMask;

            // Now, the mask contains - from the LSB - a 0b00 pair for each Latin-1 char we saw, and a 0b10 pair for each non-Latin-1 char.
            //
            // (Keep endianness in mind in the below examples.)
            // A non-Latin-1 char followed by two Latin-1 chars is 0b..._00_00_10. (tzcnt = 1)
            // A Latin-1 char followed by two non-Latin-1 chars is 0b..._10_10_00. (tzcnt = 3)
            // Two Latin-1 chars followed by a non-Latin-1 char is 0b..._10_00_00. (tzcnt = 5)
            //
            // This means tzcnt = 2 * numLeadingLatin1Chars + 1. We can conveniently take advantage of the fact
            // that the 2x multiplier already matches the char* stride length, then just subtract 1 at the end to
            // compute the correct final ending pointer value.

            Debug.Assert(currentMask != 0, "Shouldn't be here unless we see non-Latin-1 data.");
            pBuffer = (char*)((byte*)pBuffer + (uint)BitOperations.TrailingZeroCount(currentMask) - 1);

            goto Finish;

        FoundNonLatin1DataInCurrentDWord:

            uint currentDWord;
            Debug.Assert(!AllCharsInUInt32AreLatin1(currentDWord), "Shouldn't be here unless we see non-Latin-1 data.");

            if (FirstCharInUInt32IsLatin1(currentDWord))
            {
                pBuffer++; // skip past the Latin-1 char
            }

            goto Finish;

        InputBufferLessThanOneVectorInLength:

            // These code paths get hit if the original input length was less than one vector in size.
            // We can't perform vectorized reads at this point, so we'll fall back to reading primitives
            // directly. Note that all of these reads are unaligned.

            // Reminder: If this code path is hit, bufferLength is still a char count, not a byte count.
            // We skipped the code path that multiplied the count by sizeof(char).

            Debug.Assert(bufferLength < SizeOfVector128InChars);

            // QWORD drain

            if ((bufferLength & 4) != 0)
            {
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Bmi1.X64 is considered supported or unsupported
                if (Bmi1.X64.IsSupported)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    // If we can use 64-bit tzcnt to count the number of leading Latin-1 chars, prefer it.

                    ulong candidateUInt64 = Unsafe.ReadUnaligned<ulong>(pBuffer);
                    if (!AllCharsInUInt64AreLatin1(candidateUInt64))
                    {
                        // Clear the low 8 bits (the Latin-1 bits) of each char, then tzcnt.
                        // Remember the / 8 at the end to convert bit count to byte count,
                        // then the & ~1 at the end to treat a match in the high byte of
                        // any char the same as a match in the low byte of that same char.

                        candidateUInt64 &= 0xFF00FF00_FF00FF00ul;
                        pBuffer = (char*)((byte*)pBuffer + ((nuint)(Bmi1.X64.TrailingZeroCount(candidateUInt64) / 8) & ~(nuint)1));
                        goto Finish;
                    }
                }
                else
                {
                    // If we can't use 64-bit tzcnt, no worries. We'll just do 2x 32-bit reads instead.

                    currentDWord = Unsafe.ReadUnaligned<uint>(pBuffer);
                    uint nextDWord = Unsafe.ReadUnaligned<uint>(pBuffer + 4 / sizeof(char));

                    if (!AllCharsInUInt32AreLatin1(currentDWord | nextDWord))
                    {
                        // At least one of the values wasn't all-Latin-1.
                        // We need to figure out which one it was and stick it in the currentMask local.

                        if (AllCharsInUInt32AreLatin1(currentDWord))
                        {
                            currentDWord = nextDWord; // this one is the culprit
                            pBuffer += 4 / sizeof(char);
                        }

                        goto FoundNonLatin1DataInCurrentDWord;
                    }
                }

                pBuffer += 4; // successfully consumed 4 Latin-1 chars
            }

            // DWORD drain

            if ((bufferLength & 2) != 0)
            {
                currentDWord = Unsafe.ReadUnaligned<uint>(pBuffer);

                if (!AllCharsInUInt32AreLatin1(currentDWord))
                {
                    goto FoundNonLatin1DataInCurrentDWord;
                }

                pBuffer += 2; // successfully consumed 2 Latin-1 chars
            }

            // WORD drain
            // This is the final drain; there's no need for a BYTE drain since our elemental type is 16-bit char.

            if ((bufferLength & 1) != 0)
            {
                if (*pBuffer <= byte.MaxValue)
                {
                    pBuffer++; // successfully consumed a single char
                }
            }

            goto Finish;
        }


        /// <summary>
        /// Copies as many Latin-1 characters (U+0000..U+00FF) as possible from <paramref name="pUtf16Buffer"/>
        /// to <paramref name="pLatin1Buffer"/>, stopping when the first non-Latin-1 character is encountered
        /// or once <paramref name="elementCount"/> elements have been converted. Returns the total number
        /// of elements that were able to be converted.
        /// </summary>
        public static unsafe nuint NarrowUtf16ToLatin1(char* pUtf16Buffer, byte* pLatin1Buffer, nuint elementCount)
        {
            nuint currentOffset = 0;

            uint utf16Data32BitsHigh = 0, utf16Data32BitsLow = 0;
            ulong utf16Data64Bits = 0;

            // If SSE2 is supported, use those specific intrinsics instead of the generic vectorized
            // code below. This has two benefits: (a) we can take advantage of specific instructions like
            // pmovmskb, ptest, vpminuw which we know are optimized, and (b) we can avoid downclocking the
            // processor while this method is running.

            if (Sse2.IsSupported)
            {
                Debug.Assert(BitConverter.IsLittleEndian, "Assume little endian if SSE2 is supported.");

                if (elementCount >= 2 * (uint)sizeof(Vector128<byte>))
                {
                    // Since there's overhead to setting up the vectorized code path, we only want to
                    // call into it after a quick probe to ensure the next immediate characters really are Latin-1.
                    // If we see non-Latin-1 data, we'll jump immediately to the draining logic at the end of the method.

                    if (IntPtr.Size >= 8)
                    {
                        utf16Data64Bits = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer);
                        if (!AllCharsInUInt64AreLatin1(utf16Data64Bits))
                        {
                            goto FoundNonLatin1DataIn64BitRead;
                        }
                    }
                    else
                    {
                        utf16Data32BitsHigh = Unsafe.ReadUnaligned<uint>(pUtf16Buffer);
                        utf16Data32BitsLow = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + 4 / sizeof(char));
                        if (!AllCharsInUInt32AreLatin1(utf16Data32BitsHigh | utf16Data32BitsLow))
                        {
                            goto FoundNonLatin1DataIn64BitRead;
                        }
                    }

                    currentOffset = NarrowUtf16ToLatin1_Sse2(pUtf16Buffer, pLatin1Buffer, elementCount);
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                uint SizeOfVector = (uint)sizeof(Vector<byte>); // JIT will make this a const

                // Only bother vectorizing if we have enough data to do so.
                if (elementCount >= 2 * SizeOfVector)
                {
                    // Since there's overhead to setting up the vectorized code path, we only want to
                    // call into it after a quick probe to ensure the next immediate characters really are Latin-1.
                    // If we see non-Latin-1 data, we'll jump immediately to the draining logic at the end of the method.

                    if (IntPtr.Size >= 8)
                    {
                        utf16Data64Bits = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer);
                        if (!AllCharsInUInt64AreLatin1(utf16Data64Bits))
                        {
                            goto FoundNonLatin1DataIn64BitRead;
                        }
                    }
                    else
                    {
                        utf16Data32BitsHigh = Unsafe.ReadUnaligned<uint>(pUtf16Buffer);
                        utf16Data32BitsLow = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + 4 / sizeof(char));
                        if (!AllCharsInUInt32AreLatin1(utf16Data32BitsHigh | utf16Data32BitsLow))
                        {
                            goto FoundNonLatin1DataIn64BitRead;
                        }
                    }

                    Vector<ushort> maxLatin1 = new Vector<ushort>(0x00FF);

                    nuint finalOffsetWhereCanLoop = elementCount - 2 * SizeOfVector;
                    do
                    {
                        Vector<ushort> utf16VectorHigh = Unsafe.ReadUnaligned<Vector<ushort>>(pUtf16Buffer + currentOffset);
                        Vector<ushort> utf16VectorLow = Unsafe.ReadUnaligned<Vector<ushort>>(pUtf16Buffer + currentOffset + Vector<ushort>.Count);

                        if (Vector.GreaterThanAny(Vector.BitwiseOr(utf16VectorHigh, utf16VectorLow), maxLatin1))
                        {
                            break; // found non-Latin-1 data
                        }

                        // TODO: Is the below logic also valid for big-endian platforms?
                        Vector<byte> latin1Vector = Vector.Narrow(utf16VectorHigh, utf16VectorLow);
                        Unsafe.WriteUnaligned<Vector<byte>>(pLatin1Buffer + currentOffset, latin1Vector);

                        currentOffset += SizeOfVector;
                    } while (currentOffset <= finalOffsetWhereCanLoop);
                }
            }

            Debug.Assert(currentOffset <= elementCount);
            nuint remainingElementCount = elementCount - currentOffset;

            // Try to narrow 64 bits -> 32 bits at a time.
            // We needn't update remainingElementCount after this point.

            if (remainingElementCount >= 4)
            {
                nuint finalOffsetWhereCanLoop = currentOffset + remainingElementCount - 4;
                do
                {
                    if (IntPtr.Size >= 8)
                    {
                        // Only perform QWORD reads on a 64-bit platform.
                        utf16Data64Bits = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer + currentOffset);
                        if (!AllCharsInUInt64AreLatin1(utf16Data64Bits))
                        {
                            goto FoundNonLatin1DataIn64BitRead;
                        }

                        NarrowFourUtf16CharsToLatin1AndWriteToBuffer(ref pLatin1Buffer[currentOffset], utf16Data64Bits);
                    }
                    else
                    {
                        utf16Data32BitsHigh = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + currentOffset);
                        utf16Data32BitsLow = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + currentOffset + 4 / sizeof(char));
                        if (!AllCharsInUInt32AreLatin1(utf16Data32BitsHigh | utf16Data32BitsLow))
                        {
                            goto FoundNonLatin1DataIn64BitRead;
                        }

                        NarrowTwoUtf16CharsToLatin1AndWriteToBuffer(ref pLatin1Buffer[currentOffset], utf16Data32BitsHigh);
                        NarrowTwoUtf16CharsToLatin1AndWriteToBuffer(ref pLatin1Buffer[currentOffset + 2], utf16Data32BitsLow);
                    }

                    currentOffset += 4;
                } while (currentOffset <= finalOffsetWhereCanLoop);
            }

            // Try to narrow 32 bits -> 16 bits.

            if (((uint)remainingElementCount & 2) != 0)
            {
                utf16Data32BitsHigh = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + currentOffset);
                if (!AllCharsInUInt32AreLatin1(utf16Data32BitsHigh))
                {
                    goto FoundNonLatin1DataInHigh32Bits;
                }

                NarrowTwoUtf16CharsToLatin1AndWriteToBuffer(ref pLatin1Buffer[currentOffset], utf16Data32BitsHigh);
                currentOffset += 2;
            }

            // Try to narrow 16 bits -> 8 bits.

            if (((uint)remainingElementCount & 1) != 0)
            {
                utf16Data32BitsHigh = pUtf16Buffer[currentOffset];
                if (utf16Data32BitsHigh <= byte.MaxValue)
                {
                    pLatin1Buffer[currentOffset] = (byte)utf16Data32BitsHigh;
                    currentOffset++;
                }
            }

        Finish:

            return currentOffset;

        FoundNonLatin1DataIn64BitRead:

            if (IntPtr.Size >= 8)
            {
                // Try checking the first 32 bits of the buffer for non-Latin-1 data.
                // Regardless, we'll move the non-Latin-1 data into the utf16Data32BitsHigh local.

                if (BitConverter.IsLittleEndian)
                {
                    utf16Data32BitsHigh = (uint)utf16Data64Bits;
                }
                else
                {
                    utf16Data32BitsHigh = (uint)(utf16Data64Bits >> 32);
                }

                if (AllCharsInUInt32AreLatin1(utf16Data32BitsHigh))
                {
                    NarrowTwoUtf16CharsToLatin1AndWriteToBuffer(ref pLatin1Buffer[currentOffset], utf16Data32BitsHigh);

                    if (BitConverter.IsLittleEndian)
                    {
                        utf16Data32BitsHigh = (uint)(utf16Data64Bits >> 32);
                    }
                    else
                    {
                        utf16Data32BitsHigh = (uint)utf16Data64Bits;
                    }

                    currentOffset += 2;
                }
            }
            else
            {
                // Need to determine if the high or the low 32-bit value contained non-Latin-1 data.
                // Regardless, we'll move the non-Latin-1 data into the utf16Data32BitsHigh local.

                if (AllCharsInUInt32AreLatin1(utf16Data32BitsHigh))
                {
                    NarrowTwoUtf16CharsToLatin1AndWriteToBuffer(ref pLatin1Buffer[currentOffset], utf16Data32BitsHigh);
                    utf16Data32BitsHigh = utf16Data32BitsLow;
                    currentOffset += 2;
                }
            }

        FoundNonLatin1DataInHigh32Bits:

            Debug.Assert(!AllCharsInUInt32AreLatin1(utf16Data32BitsHigh), "Shouldn't have reached this point if we have an all-Latin-1 input.");

            // There's at most one char that needs to be drained.

            if (FirstCharInUInt32IsLatin1(utf16Data32BitsHigh))
            {
                if (!BitConverter.IsLittleEndian)
                {
                    utf16Data32BitsHigh >>= 16; // move high char down to low char
                }

                pLatin1Buffer[currentOffset] = (byte)utf16Data32BitsHigh;
                currentOffset++;
            }

            goto Finish;
        }

        [CompExactlyDependsOn(typeof(Sse2))]
        private static unsafe nuint NarrowUtf16ToLatin1_Sse2(char* pUtf16Buffer, byte* pLatin1Buffer, nuint elementCount)
        {
            // This method contains logic optimized for both SSE2 and SSE41. Much of the logic in this method
            // will be elided by JIT once we determine which specific ISAs we support.

            // JIT turns the below into constants

            uint SizeOfVector128 = (uint)sizeof(Vector128<byte>);
            nuint MaskOfAllBitsInVector128 = SizeOfVector128 - 1;

            // This method is written such that control generally flows top-to-bottom, avoiding
            // jumps as much as possible in the optimistic case of "all Latin-1". If we see non-Latin-1
            // data, we jump out of the hot paths to targets at the end of the method.

            Debug.Assert(Sse2.IsSupported);
            Debug.Assert(BitConverter.IsLittleEndian);
            Debug.Assert(elementCount >= 2 * SizeOfVector128);

            Vector128<short> latin1MaskForTestZ = Vector128.Create(unchecked((short)0xFF00)); // used for PTEST on supported hardware
            Vector128<ushort> latin1MaskForAddSaturate = Vector128.Create((ushort)0x7F00); // used for PADDUSW
            const int NonLatin1DataSeenMask = 0b_1010_1010_1010_1010; // used for determining whether the pmovmskb operation saw non-Latin-1 chars

            // First, perform an unaligned read of the first part of the input buffer.

            Vector128<short> utf16VectorFirst = Sse2.LoadVector128((short*)pUtf16Buffer); // unaligned load

            // If there's non-Latin-1 data in the first 8 elements of the vector, there's nothing we can do.
            // See comments in GetIndexOfFirstNonLatin1Char_Sse2 for information about how this works.

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Sse41 is considered supported or unsupported
            if (Sse41.IsSupported)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                if (!Sse41.TestZ(utf16VectorFirst, latin1MaskForTestZ))
                {
                    return 0;
                }
            }
            else
            {
                if ((Sse2.MoveMask(Sse2.AddSaturate(utf16VectorFirst.AsUInt16(), latin1MaskForAddSaturate).AsByte()) & NonLatin1DataSeenMask) != 0)
                {
                    return 0;
                }
            }

            // Turn the 8 Latin-1 chars we just read into 8 Latin-1 bytes, then copy it to the destination.

            Vector128<byte> latin1Vector = Sse2.PackUnsignedSaturate(utf16VectorFirst, utf16VectorFirst);
            Sse2.StoreScalar((ulong*)pLatin1Buffer, latin1Vector.AsUInt64()); // ulong* calculated here is UNALIGNED

            nuint currentOffsetInElements = SizeOfVector128 / 2; // we processed 8 elements so far

            // We're going to get the best performance when we have aligned writes, so we'll take the
            // hit of potentially unaligned reads in order to hit this sweet spot.

            // pLatin1Buffer points to the start of the destination buffer, immediately before where we wrote
            // the 8 bytes previously. If the 0x08 bit is set at the pinned address, then the 8 bytes we wrote
            // previously mean that the 0x08 bit is *not* set at address &pLatin1Buffer[SizeOfVector128 / 2]. In
            // that case we can immediately back up to the previous aligned boundary and start the main loop.
            // If the 0x08 bit is *not* set at the pinned address, then it means the 0x08 bit *is* set at
            // address &pLatin1Buffer[SizeOfVector128 / 2], and we should perform one more 8-byte write to bump
            // just past the next aligned boundary address.

            if (((uint)pLatin1Buffer & (SizeOfVector128 / 2)) == 0)
            {
                // We need to perform one more partial vector write before we can get the alignment we want.

                utf16VectorFirst = Sse2.LoadVector128((short*)pUtf16Buffer + currentOffsetInElements); // unaligned load

                // See comments earlier in this method for information about how this works.
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Sse41 is considered supported or unsupported
                if (Sse41.IsSupported)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    if (!Sse41.TestZ(utf16VectorFirst, latin1MaskForTestZ))
                    {
                        goto Finish;
                    }
                }
                else
                {
                    if ((Sse2.MoveMask(Sse2.AddSaturate(utf16VectorFirst.AsUInt16(), latin1MaskForAddSaturate).AsByte()) & NonLatin1DataSeenMask) != 0)
                    {
                        goto Finish;
                    }
                }

                // Turn the 8 Latin-1 chars we just read into 8 Latin-1 bytes, then copy it to the destination.
                latin1Vector = Sse2.PackUnsignedSaturate(utf16VectorFirst, utf16VectorFirst);
                Sse2.StoreScalar((ulong*)(pLatin1Buffer + currentOffsetInElements), latin1Vector.AsUInt64()); // ulong* calculated here is UNALIGNED
            }

            // Calculate how many elements we wrote in order to get pLatin1Buffer to its next alignment
            // point, then use that as the base offset going forward.

            currentOffsetInElements = SizeOfVector128 - ((nuint)pLatin1Buffer & MaskOfAllBitsInVector128);
            Debug.Assert(0 < currentOffsetInElements && currentOffsetInElements <= SizeOfVector128, "We wrote at least 1 byte but no more than a whole vector.");

            Debug.Assert(currentOffsetInElements <= elementCount, "Shouldn't have overrun the destination buffer.");
            Debug.Assert(elementCount - currentOffsetInElements >= SizeOfVector128, "We should be able to run at least one whole vector.");

            nuint finalOffsetWhereCanRunLoop = elementCount - SizeOfVector128;
            do
            {
                // In a loop, perform two unaligned reads, narrow to a single vector, then aligned write one vector.

                utf16VectorFirst = Sse2.LoadVector128((short*)pUtf16Buffer + currentOffsetInElements); // unaligned load
                Vector128<short> utf16VectorSecond = Sse2.LoadVector128((short*)pUtf16Buffer + currentOffsetInElements + SizeOfVector128 / sizeof(short)); // unaligned load
                Vector128<short> combinedVector = Sse2.Or(utf16VectorFirst, utf16VectorSecond);

                // See comments in GetIndexOfFirstNonLatin1Char_Sse2 for information about how this works.
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Sse41 is considered supported or unsupported
                if (Sse41.IsSupported)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    if (!Sse41.TestZ(combinedVector, latin1MaskForTestZ))
                    {
                        goto FoundNonLatin1DataInLoop;
                    }
                }
                else
                {
                    if ((Sse2.MoveMask(Sse2.AddSaturate(combinedVector.AsUInt16(), latin1MaskForAddSaturate).AsByte()) & NonLatin1DataSeenMask) != 0)
                    {
                        goto FoundNonLatin1DataInLoop;
                    }
                }

                // Build up the Latin-1 vector and perform the store.

                latin1Vector = Sse2.PackUnsignedSaturate(utf16VectorFirst, utf16VectorSecond);

                Debug.Assert(((nuint)pLatin1Buffer + currentOffsetInElements) % SizeOfVector128 == 0, "Write should be aligned.");
                Sse2.StoreAligned(pLatin1Buffer + currentOffsetInElements, latin1Vector); // aligned

                currentOffsetInElements += SizeOfVector128;
            } while (currentOffsetInElements <= finalOffsetWhereCanRunLoop);

        Finish:

            // There might be some Latin-1 data left over. That's fine - we'll let our caller handle the final drain.
            return currentOffsetInElements;

        FoundNonLatin1DataInLoop:

            // Can we at least narrow the high vector?
            // See comments in GetIndexOfFirstNonLatin1Char_Sse2 for information about how this works.
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Sse41 is considered supported or unsupported
            if (Sse41.IsSupported)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                if (!Sse41.TestZ(utf16VectorFirst, latin1MaskForTestZ))
                {
                    goto Finish; // found non-Latin-1 data
                }
            }
            else
            {
                if ((Sse2.MoveMask(Sse2.AddSaturate(utf16VectorFirst.AsUInt16(), latin1MaskForAddSaturate).AsByte()) & NonLatin1DataSeenMask) != 0)
                {
                    goto Finish; // found non-Latin-1 data
                }
            }

            // First part was all Latin-1, narrow and aligned write. Note we're only filling in the low half of the vector.
            latin1Vector = Sse2.PackUnsignedSaturate(utf16VectorFirst, utf16VectorFirst);

            Debug.Assert(((nuint)pLatin1Buffer + currentOffsetInElements) % sizeof(ulong) == 0, "Destination should be ulong-aligned.");

            Sse2.StoreScalar((ulong*)(pLatin1Buffer + currentOffsetInElements), latin1Vector.AsUInt64()); // ulong* calculated here is aligned
            currentOffsetInElements += SizeOfVector128 / 2;

            goto Finish;
        }

        /// <summary>
        /// Copies Latin-1 (narrow character) data from <paramref name="pLatin1Buffer"/> to the UTF-16 (wide character)
        /// buffer <paramref name="pUtf16Buffer"/>, widening data while copying. <paramref name="elementCount"/>
        /// specifies the element count of both the source and destination buffers.
        /// </summary>
        public static unsafe void WidenLatin1ToUtf16(byte* pLatin1Buffer, char* pUtf16Buffer, nuint elementCount)
        {
            // If SSE2 is supported, use those specific intrinsics instead of the generic vectorized
            // code below. This has two benefits: (a) we can take advantage of specific instructions like
            // punpcklbw which we know are optimized, and (b) we can avoid downclocking the processor while
            // this method is running.

            if (Sse2.IsSupported)
            {
                WidenLatin1ToUtf16_Sse2(pLatin1Buffer, pUtf16Buffer, elementCount);
            }
            else
            {
                WidenLatin1ToUtf16_Fallback(pLatin1Buffer, pUtf16Buffer, elementCount);
            }
        }

        [CompExactlyDependsOn(typeof(Sse2))]
        private static unsafe void WidenLatin1ToUtf16_Sse2(byte* pLatin1Buffer, char* pUtf16Buffer, nuint elementCount)
        {
            // JIT turns the below into constants

            uint SizeOfVector128 = (uint)sizeof(Vector128<byte>);
            nuint MaskOfAllBitsInVector128 = SizeOfVector128 - 1;

            Debug.Assert(Sse2.IsSupported);
            Debug.Assert(BitConverter.IsLittleEndian);

            nuint currentOffset = 0;
            Vector128<byte> zeroVector = Vector128<byte>.Zero;
            Vector128<byte> latin1Vector;

            // We're going to get the best performance when we have aligned writes, so we'll take the
            // hit of potentially unaligned reads in order to hit this sweet spot. Our central loop
            // will perform 1x 128-bit reads followed by 2x 128-bit writes, so we want to make sure
            // we actually have 128 bits of input data before entering the loop.

            if (elementCount >= SizeOfVector128)
            {
                // First, perform an unaligned 1x 64-bit read from the input buffer and an unaligned
                // 1x 128-bit write to the destination buffer.

                latin1Vector = Sse2.LoadScalarVector128((ulong*)pLatin1Buffer).AsByte(); // unaligned load
                Sse2.Store((byte*)pUtf16Buffer, Sse2.UnpackLow(latin1Vector, zeroVector)); // unaligned write

                // Calculate how many elements we wrote in order to get pOutputBuffer to its next alignment
                // point, then use that as the base offset going forward. Remember the >> 1 to account for
                // that we wrote chars, not bytes. This means we may re-read data in the next iteration of
                // the loop, but this is ok.

                currentOffset = (SizeOfVector128 >> 1) - (((nuint)pUtf16Buffer >> 1) & (MaskOfAllBitsInVector128 >> 1));
                Debug.Assert(0 < currentOffset && currentOffset <= SizeOfVector128 / sizeof(char));

                // Calculating the destination address outside the loop results in significant
                // perf wins vs. relying on the JIT to fold memory addressing logic into the
                // write instructions. See: https://github.com/dotnet/runtime/issues/33002

                char* pCurrentWriteAddress = pUtf16Buffer + currentOffset;

                // Now run the main 1x 128-bit read + 2x 128-bit write loop.

                nuint finalOffsetWhereCanIterateLoop = elementCount - SizeOfVector128;
                while (currentOffset <= finalOffsetWhereCanIterateLoop)
                {
                    latin1Vector = Sse2.LoadVector128(pLatin1Buffer + currentOffset); // unaligned load

                    // Calculating the destination address in the below manner results in significant
                    // performance wins vs. other patterns. See for more information:
                    // https://github.com/dotnet/runtime/issues/33002

                    Vector128<byte> low = Sse2.UnpackLow(latin1Vector, zeroVector);
                    Sse2.StoreAligned((byte*)pCurrentWriteAddress, low);

                    Vector128<byte> high = Sse2.UnpackHigh(latin1Vector, zeroVector);
                    Sse2.StoreAligned((byte*)pCurrentWriteAddress + SizeOfVector128, high);

                    currentOffset += SizeOfVector128;
                    pCurrentWriteAddress += SizeOfVector128;
                }
            }

            Debug.Assert(elementCount - currentOffset < SizeOfVector128, "Case where 2 vectors remained should've been in the hot loop.");
            uint remaining = (uint)elementCount - (uint)currentOffset;

            // Now handle cases where we can't process two vectors at a time.

            if ((remaining & 8) != 0)
            {
                // Read a single 64-bit vector; write a single 128-bit vector.

                latin1Vector = Sse2.LoadScalarVector128((ulong*)(pLatin1Buffer + currentOffset)).AsByte(); // unaligned load
                Sse2.Store((byte*)(pUtf16Buffer + currentOffset), Sse2.UnpackLow(latin1Vector, zeroVector)); // unaligned write
                currentOffset += 8;
            }

            if ((remaining & 4) != 0)
            {
                // Read a single 32-bit vector; write a single 64-bit vector.

                latin1Vector = Sse2.LoadScalarVector128((uint*)(pLatin1Buffer + currentOffset)).AsByte(); // unaligned load
                Sse2.StoreScalar((ulong*)(pUtf16Buffer + currentOffset), Sse2.UnpackLow(latin1Vector, zeroVector).AsUInt64()); // unaligned write
                currentOffset += 4;
            }

            if ((remaining & 3) != 0)
            {
                // 1, 2, or 3 bytes were left over
                pUtf16Buffer[currentOffset] = (char)pLatin1Buffer[currentOffset];

                if ((remaining & 2) != 0)
                {
                    // 2 or 3 bytes were left over
                    pUtf16Buffer[currentOffset + 1] = (char)pLatin1Buffer[currentOffset + 1];

                    if ((remaining & 1) != 0)
                    {
                        // 1 or 3 bytes were left over (and since '1' doesn't go down this branch, we know it was actually '3')
                        pUtf16Buffer[currentOffset + 2] = (char)pLatin1Buffer[currentOffset + 2];
                    }
                }
            }
        }

        private static unsafe void WidenLatin1ToUtf16_Fallback(byte* pLatin1Buffer, char* pUtf16Buffer, nuint elementCount)
        {
            Debug.Assert(!Sse2.IsSupported);

            nuint currentOffset = 0;

            if (Vector.IsHardwareAccelerated)
            {
                // In a loop, read 1x vector (unaligned) and write 2x vectors (unaligned).

                uint SizeOfVector = (uint)Vector<byte>.Count; // JIT will make this a const

                // Only bother vectorizing if we have enough data to do so.
                if (elementCount >= SizeOfVector)
                {
                    nuint finalOffsetWhereCanIterate = elementCount - SizeOfVector;
                    do
                    {
                        Vector<byte> latin1Vector = Unsafe.ReadUnaligned<Vector<byte>>(pLatin1Buffer + currentOffset);
                        Vector.Widen(Vector.AsVectorByte(latin1Vector), out Vector<ushort> utf16LowVector, out Vector<ushort> utf16HighVector);

                        // TODO: Is the below logic also valid for big-endian platforms?
                        Unsafe.WriteUnaligned<Vector<ushort>>(pUtf16Buffer + currentOffset, utf16LowVector);
                        Unsafe.WriteUnaligned<Vector<ushort>>(pUtf16Buffer + currentOffset + Vector<ushort>.Count, utf16HighVector);

                        currentOffset += SizeOfVector;
                    } while (currentOffset <= finalOffsetWhereCanIterate);
                }

                Debug.Assert(elementCount - currentOffset < SizeOfVector, "Vectorized logic should result in less than a vector's length of data remaining.");
            }

            // Flush any remaining data.

            while (currentOffset < elementCount)
            {
                pUtf16Buffer[currentOffset] = (char)pLatin1Buffer[currentOffset];
                currentOffset++;
            }
        }
    }
}
