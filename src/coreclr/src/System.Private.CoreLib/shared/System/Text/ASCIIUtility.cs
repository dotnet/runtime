// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

#if BIT64
using nint = System.Int64;
using nuint = System.UInt64;
#else // BIT64
using nint = System.Int32;
using nuint = System.UInt32;
#endif // BIT64

namespace System.Text
{
    internal static partial class ASCIIUtility
    {
#if DEBUG
        static ASCIIUtility()
        {
            Debug.Assert(sizeof(nint) == IntPtr.Size && nint.MinValue < 0, "nint is defined incorrectly.");
            Debug.Assert(sizeof(nuint) == IntPtr.Size && nuint.MinValue == 0, "nuint is defined incorrectly.");
        }
#endif // DEBUG

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AllBytesInUInt64AreAscii(ulong value)
        {
            // If the high bit of any byte is set, that byte is non-ASCII.

            return ((value & UInt64HighBitsOnlyMask) == 0);
        }

        /// <summary>
        /// Returns <see langword="true"/> iff all chars in <paramref name="value"/> are ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AllCharsInUInt32AreAscii(uint value)
        {
            return ((value & ~0x007F007Fu) == 0);
        }

        /// <summary>
        /// Returns <see langword="true"/> iff all chars in <paramref name="value"/> are ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AllCharsInUInt64AreAscii(ulong value)
        {
            return ((value & ~0x007F007F_007F007Ful) == 0);
        }

        /// <summary>
        /// Given a DWORD which represents two packed chars in machine-endian order,
        /// <see langword="true"/> iff the first char (in machine-endian order) is ASCII.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool FirstCharInUInt32IsAscii(uint value)
        {
            return (BitConverter.IsLittleEndian && (value & 0xFF80u) == 0)
                || (!BitConverter.IsLittleEndian && (value & 0xFF800000u) == 0);
        }

        /// <summary>
        /// Returns the index in <paramref name="pBuffer"/> where the first non-ASCII byte is found.
        /// Returns <paramref name="bufferLength"/> if the buffer is empty or all-ASCII.
        /// </summary>
        /// <returns>An ASCII byte is defined as 0x00 - 0x7F, inclusive.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe nuint GetIndexOfFirstNonAsciiByte(byte* pBuffer, nuint bufferLength)
        {
            // If SSE2 is supported, use those specific intrinsics instead of the generic vectorized
            // code below. This has two benefits: (a) we can take advantage of specific instructions like
            // pmovmskb which we know are optimized, and (b) we can avoid downclocking the processor while
            // this method is running.

            return (Sse2.IsSupported)
                ? GetIndexOfFirstNonAsciiByte_Sse2(pBuffer, bufferLength)
                : GetIndexOfFirstNonAsciiByte_Default(pBuffer, bufferLength);
        }

        private static unsafe nuint GetIndexOfFirstNonAsciiByte_Default(byte* pBuffer, nuint bufferLength)
        {
            // Squirrel away the original buffer reference. This method works by determining the exact
            // byte reference where non-ASCII data begins, so we need this base value to perform the
            // final subtraction at the end of the method to get the index into the original buffer.

            byte* pOriginalBuffer = pBuffer;

            // Before we drain off byte-by-byte, try a generic vectorized loop.
            // Only run the loop if we have at least two vectors we can pull out.
            // Note use of SBYTE instead of BYTE below; we're using the two's-complement
            // representation of negative integers to act as a surrogate for "is ASCII?".

            if (Vector.IsHardwareAccelerated && bufferLength >= 2 * (uint)Vector<sbyte>.Count)
            {
                uint SizeOfVectorInBytes = (uint)Vector<sbyte>.Count; // JIT will make this a const

                if (Vector.GreaterThanOrEqualAll(Unsafe.ReadUnaligned<Vector<sbyte>>(pBuffer), Vector<sbyte>.Zero))
                {
                    // The first several elements of the input buffer were ASCII. Bump up the pointer to the
                    // next aligned boundary, then perform aligned reads from here on out until we find non-ASCII
                    // data or we approach the end of the buffer. It's possible we'll reread data; this is ok.

                    byte* pFinalVectorReadPos = pBuffer + bufferLength - SizeOfVectorInBytes;
                    pBuffer = (byte*)(((nuint)pBuffer + SizeOfVectorInBytes) & ~(nuint)(SizeOfVectorInBytes - 1));

#if DEBUG
                    long numBytesRead = pBuffer - pOriginalBuffer;
                    Debug.Assert(0 < numBytesRead && numBytesRead <= SizeOfVectorInBytes, "We should've made forward progress of at least one byte.");
                    Debug.Assert((nuint)numBytesRead <= bufferLength, "We shouldn't have read past the end of the input buffer.");
#endif

                    Debug.Assert(pBuffer <= pFinalVectorReadPos, "Should be able to read at least one vector.");

                    do
                    {
                        Debug.Assert((nuint)pBuffer % SizeOfVectorInBytes == 0, "Vector read should be aligned.");
                        if (Vector.LessThanAny(Unsafe.Read<Vector<sbyte>>(pBuffer), Vector<sbyte>.Zero))
                        {
                            break; // found non-ASCII data
                        }

                        pBuffer += SizeOfVectorInBytes;
                    } while (pBuffer <= pFinalVectorReadPos);

                    // Adjust the remaining buffer length for the number of elements we just consumed.

                    bufferLength -= (nuint)pBuffer;
                    bufferLength += (nuint)pOriginalBuffer;
                }
            }

            // At this point, the buffer length wasn't enough to perform a vectorized search, or we did perform
            // a vectorized search and encountered non-ASCII data. In either case go down a non-vectorized code
            // path to drain any remaining ASCII bytes.
            //
            // We're going to perform unaligned reads, so prefer 32-bit reads instead of 64-bit reads.
            // This also allows us to perform more optimized bit twiddling tricks to count the number of ASCII bytes.

            uint currentUInt32;

            // Try reading 64 bits at a time in a loop.

            for (; bufferLength >= 8; bufferLength -= 8)
            {
                currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
                uint nextUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer + 4);

                if (!AllBytesInUInt32AreAscii(currentUInt32 | nextUInt32))
                {
                    // One of these two values contains non-ASCII bytes.
                    // Figure out which one it is, then put it in 'current' so that we can drain the ASCII bytes.

                    if (AllBytesInUInt32AreAscii(currentUInt32))
                    {
                        currentUInt32 = nextUInt32;
                        pBuffer += 4;
                    }

                    goto FoundNonAsciiData;
                }

                pBuffer += 8; // consumed 8 ASCII bytes
            }

            // From this point forward we don't need to update bufferLength.
            // Try reading 32 bits.

            if ((bufferLength & 4) != 0)
            {
                currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
                if (!AllBytesInUInt32AreAscii(currentUInt32))
                {
                    goto FoundNonAsciiData;
                }

                pBuffer += 4;
            }

            // Try reading 16 bits.

            if ((bufferLength & 2) != 0)
            {
                currentUInt32 = Unsafe.ReadUnaligned<ushort>(pBuffer);
                if (!AllBytesInUInt32AreAscii(currentUInt32))
                {
                    goto FoundNonAsciiData;
                }

                pBuffer += 2;
            }

            // Try reading 8 bits

            if ((bufferLength & 1) != 0)
            {
                // If the buffer contains non-ASCII data, the comparison below will fail, and
                // we'll end up not incrementing the buffer reference.

                if (*(sbyte*)pBuffer >= 0)
                {
                    pBuffer++;
                }
            }

        Finish:

            nuint totalNumBytesRead = (nuint)pBuffer - (nuint)pOriginalBuffer;
            return totalNumBytesRead;

        FoundNonAsciiData:

            Debug.Assert(!AllBytesInUInt32AreAscii(currentUInt32), "Shouldn't have reached this point if we have an all-ASCII input.");

            // The method being called doesn't bother looking at whether the high byte is ASCII. There are only
            // two scenarios: (a) either one of the earlier bytes is not ASCII and the search terminates before
            // we get to the high byte; or (b) all of the earlier bytes are ASCII, so the high byte must be
            // non-ASCII. In both cases we only care about the low 24 bits.

            pBuffer += CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(currentUInt32);
            goto Finish;
        }

        private static unsafe nuint GetIndexOfFirstNonAsciiByte_Sse2(byte* pBuffer, nuint bufferLength)
        {
            // JIT turns the below into constants

            uint SizeOfVector128 = (uint)Unsafe.SizeOf<Vector128<byte>>();
            nuint MaskOfAllBitsInVector128 = (nuint)(SizeOfVector128 - 1);

            Debug.Assert(Sse2.IsSupported, "Should've been checked by caller.");
            Debug.Assert(BitConverter.IsLittleEndian, "SSE2 assumes little-endian.");

            uint currentMask, secondMask;
            byte* pOriginalBuffer = pBuffer;

            // This method is written such that control generally flows top-to-bottom, avoiding
            // jumps as much as possible in the optimistic case of a large enough buffer and
            // "all ASCII". If we see non-ASCII data, we jump out of the hot paths to targets
            // after all the main logic.

            if (bufferLength < SizeOfVector128)
            {
                goto InputBufferLessThanOneVectorInLength; // can't vectorize; drain primitives instead
            }

            // Read the first vector unaligned.

            currentMask = (uint)Sse2.MoveMask(Sse2.LoadVector128(pBuffer)); // unaligned load

            if (currentMask != 0)
            {
                goto FoundNonAsciiDataInCurrentMask;
            }

            // If we have less than 32 bytes to process, just go straight to the final unaligned
            // read. There's no need to mess with the loop logic in the middle of this method.

            if (bufferLength < 2 * SizeOfVector128)
            {
                goto IncrementCurrentOffsetBeforeFinalUnalignedVectorRead;
            }

            // Now adjust the read pointer so that future reads are aligned.

            pBuffer = (byte*)(((nuint)pBuffer + SizeOfVector128) & ~(nuint)MaskOfAllBitsInVector128);

#if DEBUG
            long numBytesRead = pBuffer - pOriginalBuffer;
            Debug.Assert(0 < numBytesRead && numBytesRead <= SizeOfVector128, "We should've made forward progress of at least one byte.");
            Debug.Assert((nuint)numBytesRead <= bufferLength, "We shouldn't have read past the end of the input buffer.");
#endif

            // Adjust the remaining length to account for what we just read.

            bufferLength += (nuint)pOriginalBuffer;
            bufferLength -= (nuint)pBuffer;

            // The buffer is now properly aligned.
            // Read 2 vectors at a time if possible.

            if (bufferLength >= 2 * SizeOfVector128)
            {
                byte* pFinalVectorReadPos = (byte*)((nuint)pBuffer + bufferLength - 2 * SizeOfVector128);

                // After this point, we no longer need to update the bufferLength value.

                do
                {
                    Vector128<byte> firstVector = Sse2.LoadAlignedVector128(pBuffer);
                    Vector128<byte> secondVector = Sse2.LoadAlignedVector128(pBuffer + SizeOfVector128);

                    currentMask = (uint)Sse2.MoveMask(firstVector);
                    secondMask = (uint)Sse2.MoveMask(secondVector);

                    if ((currentMask | secondMask) != 0)
                    {
                        goto FoundNonAsciiDataInInnerLoop;
                    }

                    pBuffer += 2 * SizeOfVector128;
                } while (pBuffer <= pFinalVectorReadPos);
            }

            // We have somewhere between 0 and (2 * vector length) - 1 bytes remaining to read from.
            // Since the above loop doesn't update bufferLength, we can't rely on its absolute value.
            // But we _can_ rely on it to tell us how much remaining data must be drained by looking
            // at what bits of it are set. This works because had we updated it within the loop above,
            // we would've been adding 2 * SizeOfVector128 on each iteration, but we only care about
            // bits which are less significant than those that the addition would've acted on.

            // If there is fewer than one vector length remaining, skip the next aligned read.

            if ((bufferLength & SizeOfVector128) == 0)
            {
                goto DoFinalUnalignedVectorRead;
            }

            // At least one full vector's worth of data remains, so we can safely read it.
            // Remember, at this point pBuffer is still aligned.

            currentMask = (uint)Sse2.MoveMask(Sse2.LoadAlignedVector128(pBuffer));
            if (currentMask != 0)
            {
                goto FoundNonAsciiDataInCurrentMask;
            }

        IncrementCurrentOffsetBeforeFinalUnalignedVectorRead:

            pBuffer += SizeOfVector128;

        DoFinalUnalignedVectorRead:

            if (((byte)bufferLength & MaskOfAllBitsInVector128) != 0)
            {
                // Perform an unaligned read of the last vector.
                // We need to adjust the pointer because we're re-reading data.

                pBuffer += (bufferLength & MaskOfAllBitsInVector128) - SizeOfVector128;

                currentMask = (uint)Sse2.MoveMask(Sse2.LoadVector128(pBuffer)); // unaligned load
                if (currentMask != 0)
                {
                    goto FoundNonAsciiDataInCurrentMask;
                }

                pBuffer += SizeOfVector128;
            }

        Finish:

            return (nuint)pBuffer - (nuint)pOriginalBuffer; // and we're done!

        FoundNonAsciiDataInInnerLoop:

            // If the current (first) mask isn't the mask that contains non-ASCII data, then it must
            // instead be the second mask. If so, skip the entire first mask and drain ASCII bytes
            // from the second mask.

            if (currentMask == 0)
            {
                pBuffer += SizeOfVector128;
                currentMask = secondMask;
            }

        FoundNonAsciiDataInCurrentMask:

            // The mask contains - from the LSB - a 0 for each ASCII byte we saw, and a 1 for each non-ASCII byte.
            // Tzcnt is the correct operation to count the number of zero bits quickly. If this instruction isn't
            // available, we'll fall back to a normal loop.

            Debug.Assert(currentMask != 0, "Shouldn't be here unless we see non-ASCII data.");
            pBuffer += (uint)BitOperations.TrailingZeroCount(currentMask);

            goto Finish;

        FoundNonAsciiDataInCurrentDWord:

            uint currentDWord;
            Debug.Assert(!AllBytesInUInt32AreAscii(currentDWord), "Shouldn't be here unless we see non-ASCII data.");
            pBuffer += CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(currentDWord);

            goto Finish;

        InputBufferLessThanOneVectorInLength:

            // These code paths get hit if the original input length was less than one vector in size.
            // We can't perform vectorized reads at this point, so we'll fall back to reading primitives
            // directly. Note that all of these reads are unaligned.

            Debug.Assert(bufferLength < SizeOfVector128);

            // QWORD drain

            if ((bufferLength & 8) != 0)
            {
                if (Bmi1.X64.IsSupported)
                {
                    // If we can use 64-bit tzcnt to count the number of leading ASCII bytes, prefer it.

                    ulong candidateUInt64 = Unsafe.ReadUnaligned<ulong>(pBuffer);
                    if (!AllBytesInUInt64AreAscii(candidateUInt64))
                    {
                        // Clear everything but the high bit of each byte, then tzcnt.
                        // Remember the / 8 at the end to convert bit count to byte count.

                        candidateUInt64 &= UInt64HighBitsOnlyMask;
                        pBuffer += (nuint)(Bmi1.X64.TrailingZeroCount(candidateUInt64) / 8);
                        goto Finish;
                    }
                }
                else
                {
                    // If we can't use 64-bit tzcnt, no worries. We'll just do 2x 32-bit reads instead.

                    currentDWord = Unsafe.ReadUnaligned<uint>(pBuffer);
                    uint nextDWord = Unsafe.ReadUnaligned<uint>(pBuffer + 4);

                    if (!AllBytesInUInt32AreAscii(currentDWord | nextDWord))
                    {
                        // At least one of the values wasn't all-ASCII.
                        // We need to figure out which one it was and stick it in the currentMask local.

                        if (AllBytesInUInt32AreAscii(currentDWord))
                        {
                            currentDWord = nextDWord; // this one is the culprit
                            pBuffer += 4;
                        }

                        goto FoundNonAsciiDataInCurrentDWord;
                    }
                }

                pBuffer += 8; // successfully consumed 8 ASCII bytes
            }

            // DWORD drain

            if ((bufferLength & 4) != 0)
            {
                currentDWord = Unsafe.ReadUnaligned<uint>(pBuffer);

                if (!AllBytesInUInt32AreAscii(currentDWord))
                {
                    goto FoundNonAsciiDataInCurrentDWord;
                }

                pBuffer += 4; // successfully consumed 4 ASCII bytes
            }

            // WORD drain
            // (We movzx to a DWORD for ease of manipulation.)

            if ((bufferLength & 2) != 0)
            {
                currentDWord = Unsafe.ReadUnaligned<ushort>(pBuffer);

                if (!AllBytesInUInt32AreAscii(currentDWord))
                {
                    // We only care about the 0x0080 bit of the value. If it's not set, then we
                    // increment currentOffset by 1. If it's set, we don't increment it at all.

                    pBuffer += (nuint)((nint)(sbyte)currentDWord >> 7) + 1;
                    goto Finish;
                }

                pBuffer += 2; // successfully consumed 2 ASCII bytes
            }

            // BYTE drain

            if ((bufferLength & 1) != 0)
            {
                // sbyte has non-negative value if byte is ASCII.

                if (*(sbyte*)(pBuffer) >= 0)
                {
                    pBuffer++; // successfully consumed a single byte
                }
            }

            goto Finish;
        }

        /// <summary>
        /// Returns the index in <paramref name="pBuffer"/> where the first non-ASCII char is found.
        /// Returns <paramref name="bufferLength"/> if the buffer is empty or all-ASCII.
        /// </summary>
        /// <returns>An ASCII char is defined as 0x0000 - 0x007F, inclusive.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe nuint GetIndexOfFirstNonAsciiChar(char* pBuffer, nuint bufferLength /* in chars */)
        {
            // If SSE2 is supported, use those specific intrinsics instead of the generic vectorized
            // code below. This has two benefits: (a) we can take advantage of specific instructions like
            // pmovmskb which we know are optimized, and (b) we can avoid downclocking the processor while
            // this method is running.

            return (Sse2.IsSupported)
                ? GetIndexOfFirstNonAsciiChar_Sse2(pBuffer, bufferLength)
                : GetIndexOfFirstNonAsciiChar_Default(pBuffer, bufferLength);
        }

        private static unsafe nuint GetIndexOfFirstNonAsciiChar_Default(char* pBuffer, nuint bufferLength /* in chars */)
        {
            // Squirrel away the original buffer reference.This method works by determining the exact
            // char reference where non-ASCII data begins, so we need this base value to perform the
            // final subtraction at the end of the method to get the index into the original buffer.

            char* pOriginalBuffer = pBuffer;

            Debug.Assert(bufferLength <= nuint.MaxValue / sizeof(char));

            // Before we drain off char-by-char, try a generic vectorized loop.
            // Only run the loop if we have at least two vectors we can pull out.

            if (Vector.IsHardwareAccelerated && bufferLength >= 2 * (uint)Vector<ushort>.Count)
            {
                uint SizeOfVectorInChars = (uint)Vector<ushort>.Count; // JIT will make this a const
                uint SizeOfVectorInBytes = (uint)Vector<byte>.Count; // JIT will make this a const

                Vector<ushort> maxAscii = new Vector<ushort>(0x007F);

                if (Vector.LessThanOrEqualAll(Unsafe.ReadUnaligned<Vector<ushort>>(pBuffer), maxAscii))
                {
                    // The first several elements of the input buffer were ASCII. Bump up the pointer to the
                    // next aligned boundary, then perform aligned reads from here on out until we find non-ASCII
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
                        if (Vector.GreaterThanAny(Unsafe.Read<Vector<ushort>>(pBuffer), maxAscii))
                        {
                            break; // found non-ASCII data
                        }
                        pBuffer += SizeOfVectorInChars;
                    } while (pBuffer <= pFinalVectorReadPos);

                    // Adjust the remaining buffer length for the number of elements we just consumed.

                    bufferLength -= ((nuint)pBuffer - (nuint)pOriginalBuffer) / sizeof(char);
                }
            }

            // At this point, the buffer length wasn't enough to perform a vectorized search, or we did perform
            // a vectorized search and encountered non-ASCII data. In either case go down a non-vectorized code
            // path to drain any remaining ASCII chars.
            //
            // We're going to perform unaligned reads, so prefer 32-bit reads instead of 64-bit reads.
            // This also allows us to perform more optimized bit twiddling tricks to count the number of ASCII chars.

            uint currentUInt32;

            // Try reading 64 bits at a time in a loop.

            for (; bufferLength >= 4; bufferLength -= 4) // 64 bits = 4 * 16-bit chars
            {
                currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
                uint nextUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer + 4 / sizeof(char));

                if (!AllCharsInUInt32AreAscii(currentUInt32 | nextUInt32))
                {
                    // One of these two values contains non-ASCII chars.
                    // Figure out which one it is, then put it in 'current' so that we can drain the ASCII chars.

                    if (AllCharsInUInt32AreAscii(currentUInt32))
                    {
                        currentUInt32 = nextUInt32;
                        pBuffer += 2;
                    }

                    goto FoundNonAsciiData;
                }

                pBuffer += 4; // consumed 4 ASCII chars
            }

            // From this point forward we don't need to keep track of the remaining buffer length.
            // Try reading 32 bits.

            if ((bufferLength & 2) != 0) // 32 bits = 2 * 16-bit chars
            {
                currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
                if (!AllCharsInUInt32AreAscii(currentUInt32))
                {
                    goto FoundNonAsciiData;
                }

                pBuffer += 2;
            }

            // Try reading 16 bits.
            // No need to try an 8-bit read after this since we're working with chars.

            if ((bufferLength & 1) != 0)
            {
                // If the buffer contains non-ASCII data, the comparison below will fail, and
                // we'll end up not incrementing the buffer reference.

                if (*pBuffer <= 0x007F)
                {
                    pBuffer++;
                }
            }

        Finish:

            nuint totalNumBytesRead = (nuint)pBuffer - (nuint)pOriginalBuffer;
            Debug.Assert(totalNumBytesRead % sizeof(char) == 0, "Total number of bytes read should be even since we're working with chars.");
            return totalNumBytesRead / sizeof(char); // convert byte count -> char count before returning

        FoundNonAsciiData:

            Debug.Assert(!AllCharsInUInt32AreAscii(currentUInt32), "Shouldn't have reached this point if we have an all-ASCII input.");

            // We don't bother looking at the second char - only the first char.

            if (FirstCharInUInt32IsAscii(currentUInt32))
            {
                pBuffer++;
            }

            goto Finish;
        }

        private static unsafe nuint GetIndexOfFirstNonAsciiChar_Sse2(char* pBuffer, nuint bufferLength /* in chars */)
        {
            // This method contains logic optimized for both SSE2 and SSE41. Much of the logic in this method
            // will be elided by JIT once we determine which specific ISAs we support.

            // Quick check for empty inputs.

            if (bufferLength == 0)
            {
                return 0;
            }

            // JIT turns the below into constants

            uint SizeOfVector128InBytes = (uint)Unsafe.SizeOf<Vector128<byte>>();
            uint SizeOfVector128InChars = SizeOfVector128InBytes / sizeof(char);

            Debug.Assert(Sse2.IsSupported, "Should've been checked by caller.");
            Debug.Assert(BitConverter.IsLittleEndian, "SSE2 assumes little-endian.");

            Vector128<short> firstVector, secondVector;
            uint currentMask;
            char* pOriginalBuffer = pBuffer;

            if (bufferLength < SizeOfVector128InChars)
            {
                goto InputBufferLessThanOneVectorInLength; // can't vectorize; drain primitives instead
            }

            // This method is written such that control generally flows top-to-bottom, avoiding
            // jumps as much as possible in the optimistic case of "all ASCII". If we see non-ASCII
            // data, we jump out of the hot paths to targets at the end of the method.

            Vector128<short> asciiMaskForPTEST = Vector128.Create(unchecked((short)0xFF80)); // used for PTEST on supported hardware
            Vector128<ushort> asciiMaskForPMINUW = Vector128.Create((ushort)0x0080); // used for PMINUW on supported hardware
            Vector128<short> asciiMaskForPXOR = Vector128.Create(unchecked((short)0x8000)); // used for PXOR
            Vector128<short> asciiMaskForPCMPGTW = Vector128.Create(unchecked((short)0x807F)); // used for PCMPGTW

            Debug.Assert(bufferLength <= nuint.MaxValue / sizeof(char));

            // Read the first vector unaligned.

            firstVector = Sse2.LoadVector128((short*)pBuffer); // unaligned load

            if (Sse41.IsSupported)
            {
                // The SSE41-optimized code path works by forcing the 0x0080 bit in each WORD of the vector to be
                // set iff the WORD element has value >= 0x0080 (non-ASCII). Then we'll treat it as a BYTE vector
                // in order to extract the mask.
                currentMask = (uint)Sse2.MoveMask(Sse41.Min(firstVector.AsUInt16(), asciiMaskForPMINUW).AsByte());
            }
            else
            {
                // The SSE2-optimized code path works by forcing each WORD of the vector to be 0xFFFF iff the WORD
                // element has value >= 0x0080 (non-ASCII). Then we'll treat it as a BYTE vector in order to extract
                // the mask.
                currentMask = (uint)Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(firstVector, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte());
            }

            if (currentMask != 0)
            {
                goto FoundNonAsciiDataInCurrentMask;
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
                    firstVector = Sse2.LoadAlignedVector128((short*)pBuffer);
                    secondVector = Sse2.LoadAlignedVector128((short*)pBuffer + SizeOfVector128InChars);
                    Vector128<short> combinedVector = Sse2.Or(firstVector, secondVector);

                    if (Sse41.IsSupported)
                    {
                        // If a non-ASCII bit is set in any WORD of the combined vector, we have seen non-ASCII data.
                        // Jump to the non-ASCII handler to figure out which particular vector contained non-ASCII data.
                        if (!Sse41.TestZ(combinedVector, asciiMaskForPTEST))
                        {
                            goto FoundNonAsciiDataInFirstOrSecondVector;
                        }
                    }
                    else
                    {
                        // See comment earlier in the method for an explanation of how the below logic works.
                        if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(combinedVector, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte()) != 0)
                        {
                            goto FoundNonAsciiDataInFirstOrSecondVector;
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

            firstVector = Sse2.LoadAlignedVector128((short*)pBuffer);

            if (Sse41.IsSupported)
            {
                // If a non-ASCII bit is set in any WORD of the combined vector, we have seen non-ASCII data.
                // Jump to the non-ASCII handler to figure out which particular vector contained non-ASCII data.
                if (!Sse41.TestZ(firstVector, asciiMaskForPTEST))
                {
                    goto FoundNonAsciiDataInFirstVector;
                }
            }
            else
            {
                // See comment earlier in the method for an explanation of how the below logic works.
                currentMask = (uint)Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(firstVector, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte());
                if (currentMask != 0)
                {
                    goto FoundNonAsciiDataInCurrentMask;
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
                firstVector = Sse2.LoadVector128((short*)pBuffer); // unaligned load

                if (Sse41.IsSupported)
                {
                    // If a non-ASCII bit is set in any WORD of the combined vector, we have seen non-ASCII data.
                    // Jump to the non-ASCII handler to figure out which particular vector contained non-ASCII data.
                    if (!Sse41.TestZ(firstVector, asciiMaskForPTEST))
                    {
                        goto FoundNonAsciiDataInFirstVector;
                    }
                }
                else
                {
                    // See comment earlier in the method for an explanation of how the below logic works.
                    currentMask = (uint)Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(firstVector, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte());
                    if (currentMask != 0)
                    {
                        goto FoundNonAsciiDataInCurrentMask;
                    }
                }

                pBuffer += SizeOfVector128InChars;
            }

        Finish:

            Debug.Assert(((nuint)pBuffer - (nuint)pOriginalBuffer) % 2 == 0, "Shouldn't have incremented any pointer by an odd byte count.");
            return ((nuint)pBuffer - (nuint)pOriginalBuffer) / sizeof(char); // and we're done! (remember to adjust for char count)

        FoundNonAsciiDataInFirstOrSecondVector:

            // We don't know if the first or the second vector contains non-ASCII data. Check the first
            // vector, and if that's all-ASCII then the second vector must be the culprit. Either way
            // we'll make sure the first vector local is the one that contains the non-ASCII data.

            // See comment earlier in the method for an explanation of how the below logic works.
            if (Sse41.IsSupported)
            {
                if (!Sse41.TestZ(firstVector, asciiMaskForPTEST))
                {
                    goto FoundNonAsciiDataInFirstVector;
                }
            }
            else
            {
                currentMask = (uint)Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(firstVector, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte());
                if (currentMask != 0)
                {
                    goto FoundNonAsciiDataInCurrentMask;
                }
            }

            // Wasn't the first vector; must be the second.

            pBuffer += SizeOfVector128InChars;
            firstVector = secondVector;

        FoundNonAsciiDataInFirstVector:

            // See comment earlier in the method for an explanation of how the below logic works.
            if (Sse41.IsSupported)
            {
                currentMask = (uint)Sse2.MoveMask(Sse41.Min(firstVector.AsUInt16(), asciiMaskForPMINUW).AsByte());
            }
            else
            {
                currentMask = (uint)Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(firstVector, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte());
            }

        FoundNonAsciiDataInCurrentMask:

            // The mask contains - from the LSB - a 0 for each ASCII byte we saw, and a 1 for each non-ASCII byte.
            // Tzcnt is the correct operation to count the number of zero bits quickly. If this instruction isn't
            // available, we'll fall back to a normal loop. (Even though the original vector used WORD elements,
            // masks work on BYTE elements, and we account for this in the final fixup.)

            Debug.Assert(currentMask != 0, "Shouldn't be here unless we see non-ASCII data.");
            pBuffer = (char*)((byte*)pBuffer + (uint)BitOperations.TrailingZeroCount(currentMask));

            goto Finish;

        FoundNonAsciiDataInCurrentDWord:

            uint currentDWord;
            Debug.Assert(!AllCharsInUInt32AreAscii(currentDWord), "Shouldn't be here unless we see non-ASCII data.");

            if (FirstCharInUInt32IsAscii(currentDWord))
            {
                pBuffer++; // skip past the ASCII char
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
                if (Bmi1.X64.IsSupported)
                {
                    // If we can use 64-bit tzcnt to count the number of leading ASCII chars, prefer it.

                    ulong candidateUInt64 = Unsafe.ReadUnaligned<ulong>(pBuffer);
                    if (!AllCharsInUInt64AreAscii(candidateUInt64))
                    {
                        // Clear the low 7 bits (the ASCII bits) of each char, then tzcnt.
                        // Remember the / 8 at the end to convert bit count to byte count,
                        // then the & ~1 at the end to treat a match in the high byte of
                        // any char the same as a match in the low byte of that same char.

                        candidateUInt64 &= 0xFF80FF80_FF80FF80ul;
                        pBuffer = (char*)((byte*)pBuffer + ((nuint)(Bmi1.X64.TrailingZeroCount(candidateUInt64) / 8) & ~(nuint)1));
                        goto Finish;
                    }
                }
                else
                {
                    // If we can't use 64-bit tzcnt, no worries. We'll just do 2x 32-bit reads instead.

                    currentDWord = Unsafe.ReadUnaligned<uint>(pBuffer);
                    uint nextDWord = Unsafe.ReadUnaligned<uint>(pBuffer + 4 / sizeof(char));

                    if (!AllCharsInUInt32AreAscii(currentDWord | nextDWord))
                    {
                        // At least one of the values wasn't all-ASCII.
                        // We need to figure out which one it was and stick it in the currentMask local.

                        if (AllCharsInUInt32AreAscii(currentDWord))
                        {
                            currentDWord = nextDWord; // this one is the culprit
                            pBuffer += 4 / sizeof(char);
                        }

                        goto FoundNonAsciiDataInCurrentDWord;
                    }
                }

                pBuffer += 4; // successfully consumed 4 ASCII chars
            }

            // DWORD drain

            if ((bufferLength & 2) != 0)
            {
                currentDWord = Unsafe.ReadUnaligned<uint>(pBuffer);

                if (!AllCharsInUInt32AreAscii(currentDWord))
                {
                    goto FoundNonAsciiDataInCurrentDWord;
                }

                pBuffer += 2; // successfully consumed 2 ASCII chars
            }

            // WORD drain
            // This is the final drain; there's no need for a BYTE drain since our elemental type is 16-bit char.

            if ((bufferLength & 1) != 0)
            {
                if (*pBuffer <= 0x007F)
                {
                    pBuffer++; // successfully consumed a single char
                }
            }

            goto Finish;
        }

        /// <summary>
        /// Given a QWORD which represents a buffer of 4 ASCII chars in machine-endian order,
        /// narrows each WORD to a BYTE, then writes the 4-byte result to the output buffer
        /// also in machine-endian order.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void NarrowFourUtf16CharsToAsciiAndWriteToBuffer(ref byte outputBuffer, ulong value)
        {
            Debug.Assert(AllCharsInUInt64AreAscii(value));

            if (Bmi2.X64.IsSupported)
            {
                // BMI2 will work regardless of the processor's endianness.
                Unsafe.WriteUnaligned(ref outputBuffer, (uint)Bmi2.X64.ParallelBitExtract(value, 0x00FF00FF_00FF00FFul));
            }
            else
            {
                if (BitConverter.IsLittleEndian)
                {
                    outputBuffer = (byte)value;
                    value >>= 16;
                    Unsafe.Add(ref outputBuffer, 1) = (byte)value;
                    value >>= 16;
                    Unsafe.Add(ref outputBuffer, 2) = (byte)value;
                    value >>= 16;
                    Unsafe.Add(ref outputBuffer, 3) = (byte)value;
                }
                else
                {
                    Unsafe.Add(ref outputBuffer, 3) = (byte)value;
                    value >>= 16;
                    Unsafe.Add(ref outputBuffer, 2) = (byte)value;
                    value >>= 16;
                    Unsafe.Add(ref outputBuffer, 1) = (byte)value;
                    value >>= 16;
                    outputBuffer = (byte)value;
                }
            }
        }

        /// <summary>
        /// Given a DWORD which represents a buffer of 2 ASCII chars in machine-endian order,
        /// narrows each WORD to a BYTE, then writes the 2-byte result to the output buffer also in
        /// machine-endian order.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref byte outputBuffer, uint value)
        {
            Debug.Assert(AllCharsInUInt32AreAscii(value));

            if (BitConverter.IsLittleEndian)
            {
                outputBuffer = (byte)value;
                Unsafe.Add(ref outputBuffer, 1) = (byte)(value >> 16);
            }
            else
            {
                Unsafe.Add(ref outputBuffer, 1) = (byte)value;
                outputBuffer = (byte)(value >> 16);
            }
        }

        /// <summary>
        /// Copies as many ASCII characters (U+0000..U+007F) as possible from <paramref name="pUtf16Buffer"/>
        /// to <paramref name="pAsciiBuffer"/>, stopping when the first non-ASCII character is encountered
        /// or once <paramref name="elementCount"/> elements have been converted. Returns the total number
        /// of elements that were able to be converted.
        /// </summary>
        public static unsafe nuint NarrowUtf16ToAscii(char* pUtf16Buffer, byte* pAsciiBuffer, nuint elementCount)
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

                if (elementCount >= 2 * (uint)Unsafe.SizeOf<Vector128<byte>>())
                {
                    // Since there's overhead to setting up the vectorized code path, we only want to
                    // call into it after a quick probe to ensure the next immediate characters really are ASCII.
                    // If we see non-ASCII data, we'll jump immediately to the draining logic at the end of the method.

                    if (IntPtr.Size >= 8)
                    {
                        utf16Data64Bits = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer);
                        if (!AllCharsInUInt64AreAscii(utf16Data64Bits))
                        {
                            goto FoundNonAsciiDataIn64BitRead;
                        }
                    }
                    else
                    {
                        utf16Data32BitsHigh = Unsafe.ReadUnaligned<uint>(pUtf16Buffer);
                        utf16Data32BitsLow = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + 4 / sizeof(char));
                        if (!AllCharsInUInt32AreAscii(utf16Data32BitsHigh | utf16Data32BitsLow))
                        {
                            goto FoundNonAsciiDataIn64BitRead;
                        }
                    }

                    currentOffset = NarrowUtf16ToAscii_Sse2(pUtf16Buffer, pAsciiBuffer, elementCount);
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                uint SizeOfVector = (uint)Unsafe.SizeOf<Vector<byte>>(); // JIT will make this a const

                // Only bother vectorizing if we have enough data to do so.
                if (elementCount >= 2 * SizeOfVector)
                {
                    // Since there's overhead to setting up the vectorized code path, we only want to
                    // call into it after a quick probe to ensure the next immediate characters really are ASCII.
                    // If we see non-ASCII data, we'll jump immediately to the draining logic at the end of the method.

                    if (IntPtr.Size >= 8)
                    {
                        utf16Data64Bits = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer);
                        if (!AllCharsInUInt64AreAscii(utf16Data64Bits))
                        {
                            goto FoundNonAsciiDataIn64BitRead;
                        }
                    }
                    else
                    {
                        utf16Data32BitsHigh = Unsafe.ReadUnaligned<uint>(pUtf16Buffer);
                        utf16Data32BitsLow = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + 4 / sizeof(char));
                        if (!AllCharsInUInt32AreAscii(utf16Data32BitsHigh | utf16Data32BitsLow))
                        {
                            goto FoundNonAsciiDataIn64BitRead;
                        }
                    }

                    Vector<ushort> maxAscii = new Vector<ushort>(0x007F);

                    nuint finalOffsetWhereCanLoop = elementCount - 2 * SizeOfVector;
                    do
                    {
                        Vector<ushort> utf16VectorHigh = Unsafe.ReadUnaligned<Vector<ushort>>(pUtf16Buffer + currentOffset);
                        Vector<ushort> utf16VectorLow = Unsafe.ReadUnaligned<Vector<ushort>>(pUtf16Buffer + currentOffset + Vector<ushort>.Count);

                        if (Vector.GreaterThanAny(Vector.BitwiseOr(utf16VectorHigh, utf16VectorLow), maxAscii))
                        {
                            break; // found non-ASCII data
                        }

                        // TODO: Is the below logic also valid for big-endian platforms?
                        Vector<byte> asciiVector = Vector.Narrow(utf16VectorHigh, utf16VectorLow);
                        Unsafe.WriteUnaligned<Vector<byte>>(pAsciiBuffer + currentOffset, asciiVector);

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
                        if (!AllCharsInUInt64AreAscii(utf16Data64Bits))
                        {
                            goto FoundNonAsciiDataIn64BitRead;
                        }

                        NarrowFourUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[currentOffset], utf16Data64Bits);
                    }
                    else
                    {
                        utf16Data32BitsHigh = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + currentOffset);
                        utf16Data32BitsLow = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + currentOffset + 4 / sizeof(char));
                        if (!AllCharsInUInt32AreAscii(utf16Data32BitsHigh | utf16Data32BitsLow))
                        {
                            goto FoundNonAsciiDataIn64BitRead;
                        }

                        NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[currentOffset], utf16Data32BitsHigh);
                        NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[currentOffset + 2], utf16Data32BitsLow);
                    }

                    currentOffset += 4;
                } while (currentOffset <= finalOffsetWhereCanLoop);
            }

            // Try to narrow 32 bits -> 16 bits.

            if (((uint)remainingElementCount & 2) != 0)
            {
                utf16Data32BitsHigh = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + currentOffset);
                if (!AllCharsInUInt32AreAscii(utf16Data32BitsHigh))
                {
                    goto FoundNonAsciiDataInHigh32Bits;
                }

                NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[currentOffset], utf16Data32BitsHigh);
                currentOffset += 2;
            }

            // Try to narrow 16 bits -> 8 bits.

            if (((uint)remainingElementCount & 1) != 0)
            {
                utf16Data32BitsHigh = pUtf16Buffer[currentOffset];
                if (utf16Data32BitsHigh <= 0x007Fu)
                {
                    pAsciiBuffer[currentOffset] = (byte)utf16Data32BitsHigh;
                    currentOffset++;
                }
            }

        Finish:

            return currentOffset;

        FoundNonAsciiDataIn64BitRead:

            if (IntPtr.Size >= 8)
            {
                // Try checking the first 32 bits of the buffer for non-ASCII data.
                // Regardless, we'll move the non-ASCII data into the utf16Data32BitsHigh local.

                if (BitConverter.IsLittleEndian)
                {
                    utf16Data32BitsHigh = (uint)utf16Data64Bits;
                }
                else
                {
                    utf16Data32BitsHigh = (uint)(utf16Data64Bits >> 32);
                }

                if (AllCharsInUInt32AreAscii(utf16Data32BitsHigh))
                {
                    NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[currentOffset], utf16Data32BitsHigh);

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
                // Need to determine if the high or the low 32-bit value contained non-ASCII data.
                // Regardless, we'll move the non-ASCII data into the utf16Data32BitsHigh local.

                if (AllCharsInUInt32AreAscii(utf16Data32BitsHigh))
                {
                    NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[currentOffset], utf16Data32BitsHigh);
                    utf16Data32BitsHigh = utf16Data32BitsLow;
                    currentOffset += 2;
                }
            }

        FoundNonAsciiDataInHigh32Bits:

            Debug.Assert(!AllCharsInUInt32AreAscii(utf16Data32BitsHigh), "Shouldn't have reached this point if we have an all-ASCII input.");

            // There's at most one char that needs to be drained.

            if (FirstCharInUInt32IsAscii(utf16Data32BitsHigh))
            {
                if (!BitConverter.IsLittleEndian)
                {
                    utf16Data32BitsHigh >>= 16; // move high char down to low char
                }

                pAsciiBuffer[currentOffset] = (byte)utf16Data32BitsHigh;
                currentOffset++;
            }

            goto Finish;
        }

        private static unsafe nuint NarrowUtf16ToAscii_Sse2(char* pUtf16Buffer, byte* pAsciiBuffer, nuint elementCount)
        {
            // This method contains logic optimized for both SSE2 and SSE41. Much of the logic in this method
            // will be elided by JIT once we determine which specific ISAs we support.

            // JIT turns the below into constants

            uint SizeOfVector128 = (uint)Unsafe.SizeOf<Vector128<byte>>();
            nuint MaskOfAllBitsInVector128 = (nuint)(SizeOfVector128 - 1);

            // This method is written such that control generally flows top-to-bottom, avoiding
            // jumps as much as possible in the optimistic case of "all ASCII". If we see non-ASCII
            // data, we jump out of the hot paths to targets at the end of the method.

            Debug.Assert(Sse2.IsSupported);
            Debug.Assert(BitConverter.IsLittleEndian);
            Debug.Assert(elementCount >= 2 * SizeOfVector128);

            Vector128<short> asciiMaskForPTEST = Vector128.Create(unchecked((short)0xFF80)); // used for PTEST on supported hardware
            Vector128<short> asciiMaskForPXOR = Vector128.Create(unchecked((short)0x8000)); // used for PXOR
            Vector128<short> asciiMaskForPCMPGTW = Vector128.Create(unchecked((short)0x807F)); // used for PCMPGTW

            // First, perform an unaligned read of the first part of the input buffer.

            Vector128<short> utf16VectorFirst = Sse2.LoadVector128((short*)pUtf16Buffer); // unaligned load

            // If there's non-ASCII data in the first 8 elements of the vector, there's nothing we can do.
            // See comments in GetIndexOfFirstNonAsciiChar_Sse2 for information about how this works.

            if (Sse41.IsSupported)
            {
                if (!Sse41.TestZ(utf16VectorFirst, asciiMaskForPTEST))
                {
                    return 0;
                }
            }
            else
            {
                if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(utf16VectorFirst, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte()) != 0)
                {
                    return 0;
                }
            }

            // Turn the 8 ASCII chars we just read into 8 ASCII bytes, then copy it to the destination.

            Vector128<byte> asciiVector = Sse2.PackUnsignedSaturate(utf16VectorFirst, utf16VectorFirst);
            Sse2.StoreScalar((ulong*)pAsciiBuffer, asciiVector.AsUInt64()); // ulong* calculated here is UNALIGNED

            nuint currentOffsetInElements = SizeOfVector128 / 2; // we processed 8 elements so far

            // We're going to get the best performance when we have aligned writes, so we'll take the
            // hit of potentially unaligned reads in order to hit this sweet spot.

            // pAsciiBuffer points to the start of the destination buffer, immediately before where we wrote
            // the 8 bytes previously. If the 0x08 bit is set at the pinned address, then the 8 bytes we wrote
            // previously mean that the 0x08 bit is *not* set at address &pAsciiBuffer[SizeOfVector128 / 2]. In
            // that case we can immediately back up to the previous aligned boundary and start the main loop.
            // If the 0x08 bit is *not* set at the pinned address, then it means the 0x08 bit *is* set at
            // address &pAsciiBuffer[SizeOfVector128 / 2], and we should perform one more 8-byte write to bump
            // just past the next aligned boundary address.

            if (((uint)pAsciiBuffer & (SizeOfVector128 / 2)) == 0)
            {
                // We need to perform one more partial vector write before we can get the alignment we want.

                utf16VectorFirst = Sse2.LoadVector128((short*)pUtf16Buffer + currentOffsetInElements); // unaligned load

                // See comments earlier in this method for information about how this works.
                if (Sse41.IsSupported)
                {
                    if (!Sse41.TestZ(utf16VectorFirst, asciiMaskForPTEST))
                    {
                        goto Finish;
                    }
                }
                else
                {
                    if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(utf16VectorFirst, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte()) != 0)
                    {
                        goto Finish;
                    }
                }

                // Turn the 8 ASCII chars we just read into 8 ASCII bytes, then copy it to the destination.
                asciiVector = Sse2.PackUnsignedSaturate(utf16VectorFirst, utf16VectorFirst);
                Sse2.StoreScalar((ulong*)(pAsciiBuffer + currentOffsetInElements), asciiVector.AsUInt64()); // ulong* calculated here is UNALIGNED
            }

            // Calculate how many elements we wrote in order to get pAsciiBuffer to its next alignment
            // point, then use that as the base offset going forward.

            currentOffsetInElements = SizeOfVector128 - ((nuint)pAsciiBuffer & MaskOfAllBitsInVector128);
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

                // See comments in GetIndexOfFirstNonAsciiChar_Sse2 for information about how this works.
                if (Sse41.IsSupported)
                {
                    if (!Sse41.TestZ(combinedVector, asciiMaskForPTEST))
                    {
                        goto FoundNonAsciiDataInLoop;
                    }
                }
                else
                {
                    if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(combinedVector, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte()) != 0)
                    {
                        goto FoundNonAsciiDataInLoop;
                    }
                }

                // Build up the UTF-8 vector and perform the store.

                asciiVector = Sse2.PackUnsignedSaturate(utf16VectorFirst, utf16VectorSecond);

                Debug.Assert(((nuint)pAsciiBuffer + currentOffsetInElements) % SizeOfVector128 == 0, "Write should be aligned.");
                Sse2.StoreAligned(pAsciiBuffer + currentOffsetInElements, asciiVector); // aligned

                currentOffsetInElements += SizeOfVector128;
            } while (currentOffsetInElements <= finalOffsetWhereCanRunLoop);

        Finish:

            // There might be some ASCII data left over. That's fine - we'll let our caller handle the final drain.
            return currentOffsetInElements;

        FoundNonAsciiDataInLoop:

            // Can we at least narrow the high vector?
            // See comments in GetIndexOfFirstNonAsciiChar_Sse2 for information about how this works.
            if (Sse41.IsSupported)
            {
                if (!Sse41.TestZ(utf16VectorFirst, asciiMaskForPTEST))
                {
                    goto Finish; // found non-ASCII data
                }
            }
            else
            {
                if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(utf16VectorFirst, asciiMaskForPXOR), asciiMaskForPCMPGTW).AsByte()) != 0)
                {
                    goto Finish; // found non-ASCII data
                }
            }

            // First part was all ASCII, narrow and aligned write. Note we're only filling in the low half of the vector.
            asciiVector = Sse2.PackUnsignedSaturate(utf16VectorFirst, utf16VectorFirst);

            Debug.Assert(((nuint)pAsciiBuffer + currentOffsetInElements) % sizeof(ulong) == 0, "Destination should be ulong-aligned.");

            Sse2.StoreScalar((ulong*)(pAsciiBuffer + currentOffsetInElements), asciiVector.AsUInt64()); // ulong* calculated here is aligned
            currentOffsetInElements += SizeOfVector128 / 2;

            goto Finish;
        }

        /// <summary>
        /// Copies as many ASCII bytes (00..7F) as possible from <paramref name="pAsciiBuffer"/>
        /// to <paramref name="pUtf16Buffer"/>, stopping when the first non-ASCII byte is encountered
        /// or once <paramref name="elementCount"/> elements have been converted. Returns the total number
        /// of elements that were able to be converted.
        /// </summary>
        public static unsafe nuint WidenAsciiToUtf16(byte* pAsciiBuffer, char* pUtf16Buffer, nuint elementCount)
        {
            nuint currentOffset = 0;

            // If SSE2 is supported, use those specific intrinsics instead of the generic vectorized
            // code below. This has two benefits: (a) we can take advantage of specific instructions like
            // pmovmskb which we know are optimized, and (b) we can avoid downclocking the processor while
            // this method is running.

            if (Sse2.IsSupported)
            {
                if (elementCount >= 2 * (uint)Unsafe.SizeOf<Vector128<byte>>())
                {
                    currentOffset = WidenAsciiToUtf16_Sse2(pAsciiBuffer, pUtf16Buffer, elementCount);
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                uint SizeOfVector = (uint)Unsafe.SizeOf<Vector<byte>>(); // JIT will make this a const

                // Only bother vectorizing if we have enough data to do so.
                if (elementCount >= SizeOfVector)
                {
                    // Note use of SBYTE instead of BYTE below; we're using the two's-complement
                    // representation of negative integers to act as a surrogate for "is ASCII?".

                    nuint finalOffsetWhereCanLoop = elementCount - SizeOfVector;
                    do
                    {
                        Vector<sbyte> asciiVector = Unsafe.ReadUnaligned<Vector<sbyte>>(pAsciiBuffer + currentOffset);
                        if (Vector.LessThanAny(asciiVector, Vector<sbyte>.Zero))
                        {
                            break; // found non-ASCII data
                        }

                        Vector.Widen(Vector.AsVectorByte(asciiVector), out Vector<ushort> utf16LowVector, out Vector<ushort> utf16HighVector);

                        // TODO: Is the below logic also valid for big-endian platforms?
                        Unsafe.WriteUnaligned<Vector<ushort>>(pUtf16Buffer + currentOffset, utf16LowVector);
                        Unsafe.WriteUnaligned<Vector<ushort>>(pUtf16Buffer + currentOffset + Vector<ushort>.Count, utf16HighVector);

                        currentOffset += SizeOfVector;
                    } while (currentOffset <= finalOffsetWhereCanLoop);
                }
            }

            Debug.Assert(currentOffset <= elementCount);
            nuint remainingElementCount = elementCount - currentOffset;

            // Try to widen 32 bits -> 64 bits at a time.
            // We needn't update remainingElementCount after this point.

            uint asciiData;

            if (remainingElementCount >= 4)
            {
                nuint finalOffsetWhereCanLoop = currentOffset + remainingElementCount - 4;
                do
                {
                    asciiData = Unsafe.ReadUnaligned<uint>(pAsciiBuffer + currentOffset);
                    if (!AllBytesInUInt32AreAscii(asciiData))
                    {
                        goto FoundNonAsciiData;
                    }

                    WidenFourAsciiBytesToUtf16AndWriteToBuffer(ref pUtf16Buffer[currentOffset], asciiData);
                    currentOffset += 4;
                } while (currentOffset <= finalOffsetWhereCanLoop);
            }

            // Try to widen 16 bits -> 32 bits.

            if (((uint)remainingElementCount & 2) != 0)
            {
                asciiData = Unsafe.ReadUnaligned<ushort>(pAsciiBuffer + currentOffset);
                if (!AllBytesInUInt32AreAscii(asciiData))
                {
                    goto FoundNonAsciiData;
                }

                if (BitConverter.IsLittleEndian)
                {
                    pUtf16Buffer[currentOffset] = (char)(byte)asciiData;
                    pUtf16Buffer[currentOffset + 1] = (char)(asciiData >> 8);
                }
                else
                {
                    pUtf16Buffer[currentOffset + 1] = (char)(byte)asciiData;
                    pUtf16Buffer[currentOffset] = (char)(asciiData >> 8);
                }

                currentOffset += 2;
            }

            // Try to widen 8 bits -> 16 bits.

            if (((uint)remainingElementCount & 1) != 0)
            {
                asciiData = pAsciiBuffer[currentOffset];
                if (((byte)asciiData & 0x80) != 0)
                {
                    goto Finish;
                }

                pUtf16Buffer[currentOffset] = (char)asciiData;
                currentOffset += 1;
            }

        Finish:

            return currentOffset;

        FoundNonAsciiData:

            Debug.Assert(!AllBytesInUInt32AreAscii(asciiData), "Shouldn't have reached this point if we have an all-ASCII input.");

            // Drain ASCII bytes one at a time.

            while (((byte)asciiData & 0x80) == 0)
            {
                pUtf16Buffer[currentOffset] = (char)(byte)asciiData;
                currentOffset += 1;
                asciiData >>= 8;
            }

            goto Finish;
        }

        private static unsafe nuint WidenAsciiToUtf16_Sse2(byte* pAsciiBuffer, char* pUtf16Buffer, nuint elementCount)
        {
            // JIT turns the below into constants

            uint SizeOfVector128 = (uint)Unsafe.SizeOf<Vector128<byte>>();
            nuint MaskOfAllBitsInVector128 = (nuint)(SizeOfVector128 - 1);

            // This method is written such that control generally flows top-to-bottom, avoiding
            // jumps as much as possible in the optimistic case of "all ASCII". If we see non-ASCII
            // data, we jump out of the hot paths to targets at the end of the method.

            Debug.Assert(Sse2.IsSupported);
            Debug.Assert(BitConverter.IsLittleEndian);
            Debug.Assert(elementCount >= 2 * SizeOfVector128);

            // We're going to get the best performance when we have aligned writes, so we'll take the
            // hit of potentially unaligned reads in order to hit this sweet spot.

            Vector128<byte> asciiVector;
            Vector128<byte> utf16FirstHalfVector;
            uint mask;

            // First, perform an unaligned read of the first part of the input buffer.

            asciiVector = Sse2.LoadVector128(pAsciiBuffer); // unaligned load
            mask = (uint)Sse2.MoveMask(asciiVector);

            // If there's non-ASCII data in the first 8 elements of the vector, there's nothing we can do.

            if ((byte)mask != 0)
            {
                return 0;
            }

            // Then perform an unaligned write of the first part of the input buffer.

            Vector128<byte> zeroVector = Vector128<byte>.Zero;

            utf16FirstHalfVector = Sse2.UnpackLow(asciiVector, zeroVector);
            Sse2.Store((byte*)pUtf16Buffer, utf16FirstHalfVector); // unaligned

            // Calculate how many elements we wrote in order to get pOutputBuffer to its next alignment
            // point, then use that as the base offset going forward. Remember the >> 1 to account for
            // that we wrote chars, not bytes. This means we may re-read data in the next iteration of
            // the loop, but this is ok.

            nuint currentOffset = (SizeOfVector128 >> 1) - (((nuint)pUtf16Buffer >> 1) & (MaskOfAllBitsInVector128 >> 1));
            Debug.Assert(0 < currentOffset && currentOffset <= SizeOfVector128 / sizeof(char));

            nuint finalOffsetWhereCanRunLoop = elementCount - SizeOfVector128;

            do
            {
                // In a loop, perform an unaligned read, widen to two vectors, then aligned write the two vectors.

                asciiVector = Sse2.LoadVector128(pAsciiBuffer + currentOffset); // unaligned load
                mask = (uint)Sse2.MoveMask(asciiVector);

                if (mask != 0)
                {
                    // non-ASCII byte somewhere
                    goto NonAsciiDataSeenInInnerLoop;
                }

                byte* pStore = (byte*)(pUtf16Buffer + currentOffset);
                Sse2.StoreAligned(pStore, Sse2.UnpackLow(asciiVector, zeroVector));

                pStore += SizeOfVector128;
                Sse2.StoreAligned(pStore, Sse2.UnpackHigh(asciiVector, zeroVector));

                currentOffset += SizeOfVector128;
            } while (currentOffset <= finalOffsetWhereCanRunLoop);

        Finish:

            return currentOffset;

        NonAsciiDataSeenInInnerLoop:

            // Can we at least widen the first part of the vector?

            if ((byte)mask == 0)
            {
                // First part was all ASCII, widen
                utf16FirstHalfVector = Sse2.UnpackLow(asciiVector, zeroVector);
                Sse2.StoreAligned((byte*)(pUtf16Buffer + currentOffset), utf16FirstHalfVector);
                currentOffset += SizeOfVector128 / 2;
            }

            goto Finish;
        }

        /// <summary>
        /// Given a DWORD which represents a buffer of 4 bytes, widens the buffer into 4 WORDs and
        /// writes them to the output buffer with machine endianness.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WidenFourAsciiBytesToUtf16AndWriteToBuffer(ref char outputBuffer, uint value)
        {
            Debug.Assert(AllBytesInUInt32AreAscii(value));

            if (Bmi2.X64.IsSupported)
            {
                // BMI2 will work regardless of the processor's endianness.
                Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref outputBuffer), Bmi2.X64.ParallelBitDeposit(value, 0x00FF00FF_00FF00FFul));
            }
            else
            {
                if (BitConverter.IsLittleEndian)
                {
                    outputBuffer = (char)(byte)value;
                    value >>= 8;
                    Unsafe.Add(ref outputBuffer, 1) = (char)(byte)value;
                    value >>= 8;
                    Unsafe.Add(ref outputBuffer, 2) = (char)(byte)value;
                    value >>= 8;
                    Unsafe.Add(ref outputBuffer, 3) = (char)value;
                }
                else
                {
                    Unsafe.Add(ref outputBuffer, 3) = (char)(byte)value;
                    value >>= 8;
                    Unsafe.Add(ref outputBuffer, 2) = (char)(byte)value;
                    value >>= 8;
                    Unsafe.Add(ref outputBuffer, 1) = (char)(byte)value;
                    value >>= 8;
                    outputBuffer = (char)value;
                }
            }
        }
    }
}
