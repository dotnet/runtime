// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System
{
    internal static partial class SpanHelpers // .Byte
    {
        public static int IndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return 0;  // A zero-length sequence is always treated as "found" at the start of the search space.

            int valueTailLength = valueLength - 1;
            if (valueTailLength == 0)
                return IndexOfValueType(ref searchSpace, value, searchSpaceLength); // for single-byte values use plain IndexOf

            nint offset = 0;
            byte valueHead = value;
            int searchSpaceMinusValueTailLength = searchSpaceLength - valueTailLength;
            if (Vector128.IsHardwareAccelerated && searchSpaceMinusValueTailLength >= Vector128<byte>.Count)
            {
                goto SEARCH_TWO_BYTES;
            }

            ref byte valueTail = ref Unsafe.Add(ref value, 1);
            int remainingSearchSpaceLength = searchSpaceMinusValueTailLength;

            while (remainingSearchSpaceLength > 0)
            {
                // Do a quick search for the first element of "value".
                int relativeIndex = IndexOfValueType(ref Unsafe.Add(ref searchSpace, offset), valueHead, remainingSearchSpaceLength);
                if (relativeIndex < 0)
                    break;

                remainingSearchSpaceLength -= relativeIndex;
                offset += relativeIndex;

                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Found the first element of "value". See if the tail matches.
                if (SequenceEqual(
                        ref Unsafe.Add(ref searchSpace, offset + 1),
                        ref valueTail, (nuint)(uint)valueTailLength))  // The (nuint)-cast is necessary to pick the correct overload
                    return (int)offset;  // The tail matched. Return a successful find.

                remainingSearchSpaceLength--;
                offset++;
            }
            return -1;

            // Based on http://0x80.pl/articles/simd-strfind.html#algorithm-1-generic-simd "Algorithm 1: Generic SIMD" by Wojciech Mula
            // Some details about the implementation can also be found in https://github.com/dotnet/runtime/pull/63285
        SEARCH_TWO_BYTES:
            if (Vector256.IsHardwareAccelerated && searchSpaceMinusValueTailLength - Vector256<byte>.Count >= 0)
            {
                // Find the last unique (which is not equal to ch1) byte
                // the algorithm is fine if both are equal, just a little bit less efficient
                byte ch2Val = Unsafe.Add(ref value, valueTailLength);
                nint ch1ch2Distance = valueTailLength;
                while (ch2Val == value && ch1ch2Distance > 1)
                    ch2Val = Unsafe.Add(ref value, --ch1ch2Distance);

                Vector256<byte> ch1 = Vector256.Create(value);
                Vector256<byte> ch2 = Vector256.Create(ch2Val);

                nint searchSpaceMinusValueTailLengthAndVector =
                    searchSpaceMinusValueTailLength - (nint)Vector256<byte>.Count;

                do
                {
                    Debug.Assert(offset >= 0);
                    // Make sure we don't go out of bounds
                    Debug.Assert(offset + ch1ch2Distance + Vector256<byte>.Count <= searchSpaceLength);

                    Vector256<byte> cmpCh2 = Vector256.Equals(ch2, Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
                    Vector256<byte> cmpCh1 = Vector256.Equals(ch1, Vector256.LoadUnsafe(ref searchSpace, (nuint)offset));
                    Vector256<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();

                    // Early out: cmpAnd is all zeros
                    if (cmpAnd != Vector256<byte>.Zero)
                    {
                        goto CANDIDATE_FOUND;
                    }

                LOOP_FOOTER:
                    offset += Vector256<byte>.Count;

                    if (offset == searchSpaceMinusValueTailLength)
                        return -1;

                    // Overlap with the current chunk for trailing elements
                    if (offset > searchSpaceMinusValueTailLengthAndVector)
                        offset = searchSpaceMinusValueTailLengthAndVector;

                    continue;

                CANDIDATE_FOUND:
                    uint mask = cmpAnd.ExtractMostSignificantBits();
                    do
                    {
                        int bitPos = BitOperations.TrailingZeroCount(mask);
                        if (valueLength == 2 || // we already matched two bytes
                            SequenceEqual(
                                ref Unsafe.Add(ref searchSpace, offset + bitPos),
                                ref value, (nuint)(uint)valueLength)) // The (nuint)-cast is necessary to pick the correct overload
                        {
                            return (int)(offset + bitPos);
                        }
                        mask = BitOperations.ResetLowestSetBit(mask); // Clear the lowest set bit
                    } while (mask != 0);
                    goto LOOP_FOOTER;

                } while (true);
            }
            else // 128bit vector path (SSE2 or AdvSimd)
            {
                // Find the last unique (which is not equal to ch1) byte
                // the algorithm is fine if both are equal, just a little bit less efficient
                byte ch2Val = Unsafe.Add(ref value, valueTailLength);
                int ch1ch2Distance = valueTailLength;
                while (ch2Val == value && ch1ch2Distance > 1)
                    ch2Val = Unsafe.Add(ref value, --ch1ch2Distance);

                Vector128<byte> ch1 = Vector128.Create(value);
                Vector128<byte> ch2 = Vector128.Create(ch2Val);

                nint searchSpaceMinusValueTailLengthAndVector =
                    searchSpaceMinusValueTailLength - (nint)Vector128<byte>.Count;

                do
                {
                    Debug.Assert(offset >= 0);
                    // Make sure we don't go out of bounds
                    Debug.Assert(offset + ch1ch2Distance + Vector128<byte>.Count <= searchSpaceLength);

                    Vector128<byte> cmpCh2 = Vector128.Equals(ch2, Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
                    Vector128<byte> cmpCh1 = Vector128.Equals(ch1, Vector128.LoadUnsafe(ref searchSpace, (nuint)offset));
                    Vector128<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();

                    // Early out: cmpAnd is all zeros
                    if (cmpAnd != Vector128<byte>.Zero)
                    {
                        goto CANDIDATE_FOUND;
                    }

                LOOP_FOOTER:
                    offset += Vector128<byte>.Count;

                    if (offset == searchSpaceMinusValueTailLength)
                        return -1;

                    // Overlap with the current chunk for trailing elements
                    if (offset > searchSpaceMinusValueTailLengthAndVector)
                        offset = searchSpaceMinusValueTailLengthAndVector;

                    continue;

                CANDIDATE_FOUND:
                    uint mask = cmpAnd.ExtractMostSignificantBits();
                    do
                    {
                        int bitPos = BitOperations.TrailingZeroCount(mask);
                        if (valueLength == 2 || // we already matched two bytes
                            SequenceEqual(
                                ref Unsafe.Add(ref searchSpace, offset + bitPos),
                                ref value, (nuint)(uint)valueLength)) // The (nuint)-cast is necessary to pick the correct overload
                        {
                            return (int)(offset + bitPos);
                        }
                        // Clear the lowest set bit
                        mask = BitOperations.ResetLowestSetBit(mask);
                    } while (mask != 0);
                    goto LOOP_FOOTER;

                } while (true);
            }
        }

        public static int LastIndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return searchSpaceLength;  // A zero-length sequence is always treated as "found" at the end of the search space.

            int valueTailLength = valueLength - 1;
            if (valueTailLength == 0)
                return LastIndexOfValueType(ref searchSpace, value, searchSpaceLength); // for single-byte values use plain LastIndexOf

            int offset = 0;
            byte valueHead = value;
            int searchSpaceMinusValueTailLength = searchSpaceLength - valueTailLength;
            if (Vector128.IsHardwareAccelerated && searchSpaceMinusValueTailLength >= Vector128<byte>.Count)
            {
                goto SEARCH_TWO_BYTES;
            }

            ref byte valueTail = ref Unsafe.Add(ref value, 1);

            while (true)
            {
                Debug.Assert(0 <= offset && offset <= searchSpaceLength); // Ensures no deceptive underflows in the computation of "remainingSearchSpaceLength".
                int remainingSearchSpaceLength = searchSpaceLength - offset - valueTailLength;
                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Do a quick search for the first element of "value".
                int relativeIndex = LastIndexOfValueType(ref searchSpace, valueHead, remainingSearchSpaceLength);
                if (relativeIndex < 0)
                    break;

                // Found the first element of "value". See if the tail matches.
                if (SequenceEqual(
                        ref Unsafe.Add(ref searchSpace, relativeIndex + 1),
                        ref valueTail, (nuint)(uint)valueTailLength)) // The (nuint)-cast is necessary to pick the correct overload
                    return relativeIndex;  // The tail matched. Return a successful find.

                offset += remainingSearchSpaceLength - relativeIndex;
            }
            return -1;

        // Based on http://0x80.pl/articles/simd-strfind.html#algorithm-1-generic-simd "Algorithm 1: Generic SIMD" by Wojciech Mula
        // Some details about the implementation can also be found in https://github.com/dotnet/runtime/pull/63285
        SEARCH_TWO_BYTES:
            if (Vector256.IsHardwareAccelerated && searchSpaceMinusValueTailLength >= Vector256<byte>.Count)
            {
                offset = searchSpaceMinusValueTailLength - Vector256<byte>.Count;

                // Find the last unique (which is not equal to ch1) byte
                // the algorithm is fine if both are equal, just a little bit less efficient
                byte ch2Val = Unsafe.Add(ref value, valueTailLength);
                int ch1ch2Distance = valueTailLength;
                while (ch2Val == value && ch1ch2Distance > 1)
                    ch2Val = Unsafe.Add(ref value, --ch1ch2Distance);

                Vector256<byte> ch1 = Vector256.Create(value);
                Vector256<byte> ch2 = Vector256.Create(ch2Val);
                do
                {
                    Vector256<byte> cmpCh1 = Vector256.Equals(ch1, Vector256.LoadUnsafe(ref searchSpace, (nuint)offset));
                    Vector256<byte> cmpCh2 = Vector256.Equals(ch2, Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
                    Vector256<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();

                    // Early out: cmpAnd is all zeros
                    if (cmpAnd != Vector256<byte>.Zero)
                    {
                        uint mask = cmpAnd.ExtractMostSignificantBits();
                        do
                        {
                            // unlike IndexOf, here we use LZCNT to process matches starting from the end
                            int bitPos = 31 - BitOperations.LeadingZeroCount(mask);
                            if (valueLength == 2 || // we already matched two bytes
                                SequenceEqual(
                                    ref Unsafe.Add(ref searchSpace, offset + bitPos),
                                    ref value, (nuint)(uint)valueLength)) // The (nuint)-cast is necessary to pick the correct overload
                            {
                                return bitPos + offset;
                            }
                            // Clear the highest set bit.
                            mask = BitOperations.ResetBit(mask, bitPos);
                        } while (mask != 0);
                    }

                    offset -= Vector256<byte>.Count;
                    if (offset == -Vector256<byte>.Count)
                        return -1;
                    // Overlap with the current chunk if there is not enough room for the next one
                    if (offset < 0)
                        offset = 0;
                } while (true);
            }
            else // 128bit vector path (SSE2 or AdvSimd)
            {
                offset = searchSpaceMinusValueTailLength - Vector128<byte>.Count;

                // Find the last unique (which is not equal to ch1) byte
                // the algorithm is fine if both are equal, just a little bit less efficient
                byte ch2Val = Unsafe.Add(ref value, valueTailLength);
                int ch1ch2Distance = valueTailLength;
                while (ch2Val == value && ch1ch2Distance > 1)
                    ch2Val = Unsafe.Add(ref value, --ch1ch2Distance);

                Vector128<byte> ch1 = Vector128.Create(value);
                Vector128<byte> ch2 = Vector128.Create(ch2Val);

                do
                {
                    Vector128<byte> cmpCh1 = Vector128.Equals(ch1, Vector128.LoadUnsafe(ref searchSpace, (nuint)offset));
                    Vector128<byte> cmpCh2 = Vector128.Equals(ch2, Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
                    Vector128<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();

                    // Early out: cmpAnd is all zeros
                    // it's especially important for ARM where ExtractMostSignificantBits is not cheap
                    if (cmpAnd != Vector128<byte>.Zero)
                    {
                        uint mask = cmpAnd.ExtractMostSignificantBits();
                        do
                        {
                            // unlike IndexOf, here we use LZCNT to process matches starting from the end
                            int bitPos = 31 - BitOperations.LeadingZeroCount(mask);
                            if (valueLength == 2 || // we already matched two bytes
                                SequenceEqual(
                                    ref Unsafe.Add(ref searchSpace, offset + bitPos),
                                    ref value, (nuint)(uint)valueLength)) // The (nuint)-cast is necessary to pick the correct overload
                            {
                                return bitPos + offset;
                            }
                            // Clear the highest set bit.
                            mask = BitOperations.ResetBit(mask, bitPos);
                        } while (mask != 0);
                    }

                    offset -= Vector128<byte>.Count;
                    if (offset == -Vector128<byte>.Count)
                        return -1;
                    // Overlap with the current chunk if there is not enough room for the next one
                    if (offset < 0)
                        offset = 0;

                } while (true);
            }
        }

        [DoesNotReturn]
        private static void ThrowMustBeNullTerminatedString()
        {
            throw new ArgumentException(SR.Arg_MustBeNullTerminatedString);
        }

        // IndexOfNullByte processes memory in aligned chunks, and thus it won't crash even if it accesses memory beyond the null terminator.
        // This behavior is an implementation detail of the runtime and callers outside System.Private.CoreLib must not depend on it.
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe int IndexOfNullByte(byte* searchSpace)
        {
            const int Length = int.MaxValue;

            const uint uValue = 0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)Length;

            if (Vector128.IsHardwareAccelerated)
            {
                // Avx2 branch also operates on Sse2 sizes, so check is combined.
                lengthToExamine = UnalignedCountVector128(searchSpace);
            }
            else if (Vector.IsHardwareAccelerated)
            {
                lengthToExamine = UnalignedCountVector(searchSpace);
            }
        SequentialScan:
            while (lengthToExamine >= 8)
            {
                lengthToExamine -= 8;

                if (uValue == searchSpace[offset])
                    goto Found;
                if (uValue == searchSpace[offset + 1])
                    goto Found1;
                if (uValue == searchSpace[offset + 2])
                    goto Found2;
                if (uValue == searchSpace[offset + 3])
                    goto Found3;
                if (uValue == searchSpace[offset + 4])
                    goto Found4;
                if (uValue == searchSpace[offset + 5])
                    goto Found5;
                if (uValue == searchSpace[offset + 6])
                    goto Found6;
                if (uValue == searchSpace[offset + 7])
                    goto Found7;

                offset += 8;
            }

            if (lengthToExamine >= 4)
            {
                lengthToExamine -= 4;

                if (uValue == searchSpace[offset])
                    goto Found;
                if (uValue == searchSpace[offset + 1])
                    goto Found1;
                if (uValue == searchSpace[offset + 2])
                    goto Found2;
                if (uValue == searchSpace[offset + 3])
                    goto Found3;

                offset += 4;
            }

            while (lengthToExamine > 0)
            {
                lengthToExamine -= 1;

                if (uValue == searchSpace[offset])
                    goto Found;

                offset += 1;
            }

            // We get past SequentialScan only if IsHardwareAccelerated is true; and remain length is greater than Vector length.
            // However, we still have the redundant check to allow the JIT to see that the code is unreachable and eliminate it when the platform does not
            // have hardware accelerated. After processing Vector lengths we return to SequentialScan to finish any remaining.
            if (Vector256.IsHardwareAccelerated)
            {
                if (offset < (nuint)(uint)Length)
                {
                    if ((((nuint)(uint)searchSpace + offset) & (nuint)(Vector256<byte>.Count - 1)) != 0)
                    {
                        // Not currently aligned to Vector256 (is aligned to Vector128); this can cause a problem for searches
                        // with no upper bound e.g. String.strlen.
                        // Start with a check on Vector128 to align to Vector256, before moving to processing Vector256.
                        // This ensures we do not fault across memory pages while searching for an end of string.
                        Vector128<byte> search = Vector128.Load(searchSpace + offset);

                        // Same method as below
                        uint matches = Vector128.Equals(Vector128<byte>.Zero, search).ExtractMostSignificantBits();
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += (nuint)Vector128<byte>.Count;
                        }
                        else
                        {
                            // Find bitflag offset of first match and add to current offset
                            return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                        }
                    }

                    lengthToExamine = GetByteVector256SpanLength(offset, Length);
                    if (lengthToExamine > offset)
                    {
                        do
                        {
                            Vector256<byte> search = Vector256.Load(searchSpace + offset);
                            uint matches = Vector256.Equals(Vector256<byte>.Zero, search).ExtractMostSignificantBits();
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // Zero flags set so no matches
                                offset += (nuint)Vector256<byte>.Count;
                                continue;
                            }

                            // Find bitflag offset of first match and add to current offset
                            return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                        } while (lengthToExamine > offset);
                    }

                    lengthToExamine = GetByteVector128SpanLength(offset, Length);
                    if (lengthToExamine > offset)
                    {
                        Vector128<byte> search = Vector128.Load(searchSpace + offset);

                        // Same method as above
                        uint matches = Vector128.Equals(Vector128<byte>.Zero, search).ExtractMostSignificantBits();
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += (nuint)Vector128<byte>.Count;
                        }
                        else
                        {
                            // Find bitflag offset of first match and add to current offset
                            return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                        }
                    }

                    if (offset < (nuint)(uint)Length)
                    {
                        lengthToExamine = ((nuint)(uint)Length - offset);
                        goto SequentialScan;
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                if (offset < (nuint)(uint)Length)
                {
                    lengthToExamine = GetByteVector128SpanLength(offset, Length);

                    while (lengthToExamine > offset)
                    {
                        Vector128<byte> search = Vector128.Load(searchSpace + offset);

                        // Same method as above
                        Vector128<byte> compareResult = Vector128.Equals(Vector128<byte>.Zero, search);
                        if (compareResult == Vector128<byte>.Zero)
                        {
                            // Zero flags set so no matches
                            offset += (nuint)Vector128<byte>.Count;
                            continue;
                        }

                        // Find bitflag offset of first match and add to current offset
                        uint matches = compareResult.ExtractMostSignificantBits();
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    }

                    if (offset < (nuint)(uint)Length)
                    {
                        lengthToExamine = ((nuint)(uint)Length - offset);
                        goto SequentialScan;
                    }
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                if (offset < (nuint)(uint)Length)
                {
                    lengthToExamine = GetByteVectorSpanLength(offset, Length);

                    while (lengthToExamine > offset)
                    {
                        Vector<byte> matches = Vector.Equals(Vector<byte>.Zero, Vector.Load(searchSpace + offset));
                        if (Vector<byte>.Zero.Equals(matches))
                        {
                            offset += (nuint)Vector<byte>.Count;
                            continue;
                        }

                        // Find offset of first match and add to current offset
                        return (int)offset + LocateFirstFoundByte(matches);
                    }

                    if (offset < (nuint)(uint)Length)
                    {
                        lengthToExamine = ((nuint)(uint)Length - offset);
                        goto SequentialScan;
                    }
                }
            }

            ThrowMustBeNullTerminatedString();
        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return (int)offset;
        Found1:
            return (int)(offset + 1);
        Found2:
            return (int)(offset + 2);
        Found3:
            return (int)(offset + 3);
        Found4:
            return (int)(offset + 4);
        Found5:
            return (int)(offset + 5);
        Found6:
            return (int)(offset + 6);
        Found7:
            return (int)(offset + 7);
        }

        // Optimized byte-based SequenceEquals. The "length" parameter for this one is declared a nuint rather than int as we also use it for types other than byte
        // where the length can exceed 2Gb once scaled by sizeof(T).
        [Intrinsic] // Unrolled for constant length
        public static unsafe bool SequenceEqual(ref byte first, ref byte second, nuint length)
        {
            bool result;
            // Use nint for arithmetic to avoid unnecessary 64->32->64 truncations
            if (length >= (nuint)sizeof(nuint))
            {
                // Conditional jmp forward to favor shorter lengths. (See comment at "Equal:" label)
                // The longer lengths can make back the time due to branch misprediction
                // better than shorter lengths.
                goto Longer;
            }

#if TARGET_64BIT
            // On 32-bit, this will always be true since sizeof(nuint) == 4
            if (length < sizeof(uint))
#endif
            {
                uint differentBits = 0;
                nuint offset = (length & 2);
                if (offset != 0)
                {
                    differentBits = LoadUShort(ref first);
                    differentBits -= LoadUShort(ref second);
                }
                if ((length & 1) != 0)
                {
                    differentBits |= (uint)Unsafe.AddByteOffset(ref first, offset) - (uint)Unsafe.AddByteOffset(ref second, offset);
                }
                result = (differentBits == 0);
                goto Result;
            }
#if TARGET_64BIT
            else
            {
                nuint offset = length - sizeof(uint);
                uint differentBits = LoadUInt(ref first) - LoadUInt(ref second);
                differentBits |= LoadUInt(ref first, offset) - LoadUInt(ref second, offset);
                result = (differentBits == 0);
                goto Result;
            }
#endif
        Longer:
            // Only check that the ref is the same if buffers are large,
            // and hence its worth avoiding doing unnecessary comparisons
            if (!Unsafe.AreSame(ref first, ref second))
            {
                // C# compiler inverts this test, making the outer goto the conditional jmp.
                goto Vector;
            }

            // This becomes a conditional jmp forward to not favor it.
            goto Equal;

        Result:
            return result;
        // When the sequence is equal; which is the longest execution, we want it to determine that
        // as fast as possible so we do not want the early outs to be "predicted not taken" branches.
        Equal:
            return true;

        Vector:
            if (Vector128.IsHardwareAccelerated)
            {
                if (Vector256.IsHardwareAccelerated && length >= (nuint)Vector256<byte>.Count)
                {
                    nuint offset = 0;
                    nuint lengthToExamine = length - (nuint)Vector256<byte>.Count;
                    // Unsigned, so it shouldn't have overflowed larger than length (rather than negative)
                    Debug.Assert(lengthToExamine < length);
                    if (lengthToExamine != 0)
                    {
                        do
                        {
                            if (Vector256.LoadUnsafe(ref first, offset) !=
                                Vector256.LoadUnsafe(ref second, offset))
                            {
                                goto NotEqual;
                            }
                            offset += (nuint)Vector256<byte>.Count;
                        } while (lengthToExamine > offset);
                    }

                    // Do final compare as Vector256<byte>.Count from end rather than start
                    if (Vector256.LoadUnsafe(ref first, lengthToExamine) ==
                        Vector256.LoadUnsafe(ref second, lengthToExamine))
                    {
                        // C# compiler inverts this test, making the outer goto the conditional jmp.
                        goto Equal;
                    }

                    // This becomes a conditional jmp forward to not favor it.
                    goto NotEqual;
                }
                else if (length >= (nuint)Vector128<byte>.Count)
                {
                    nuint offset = 0;
                    nuint lengthToExamine = length - (nuint)Vector128<byte>.Count;
                    // Unsigned, so it shouldn't have overflowed larger than length (rather than negative)
                    Debug.Assert(lengthToExamine < length);
                    if (lengthToExamine != 0)
                    {
                        do
                        {
                            if (Vector128.LoadUnsafe(ref first, offset) !=
                                Vector128.LoadUnsafe(ref second, offset))
                            {
                                goto NotEqual;
                            }
                            offset += (nuint)Vector128<byte>.Count;
                        } while (lengthToExamine > offset);
                    }

                    // Do final compare as Vector128<byte>.Count from end rather than start
                    if (Vector128.LoadUnsafe(ref first, lengthToExamine) ==
                        Vector128.LoadUnsafe(ref second, lengthToExamine))
                    {
                        // C# compiler inverts this test, making the outer goto the conditional jmp.
                        goto Equal;
                    }

                    // This becomes a conditional jmp forward to not favor it.
                    goto NotEqual;
                }
            }
            else if (Vector.IsHardwareAccelerated && length >= (nuint)Vector<byte>.Count)
            {
                nuint offset = 0;
                nuint lengthToExamine = length - (nuint)Vector<byte>.Count;
                // Unsigned, so it shouldn't have overflowed larger than length (rather than negative)
                Debug.Assert(lengthToExamine < length);
                if (lengthToExamine > 0)
                {
                    do
                    {
                        if (LoadVector(ref first, offset) != LoadVector(ref second, offset))
                        {
                            goto NotEqual;
                        }
                        offset += (nuint)Vector<byte>.Count;
                    } while (lengthToExamine > offset);
                }

                // Do final compare as Vector<byte>.Count from end rather than start
                if (LoadVector(ref first, lengthToExamine) == LoadVector(ref second, lengthToExamine))
                {
                    // C# compiler inverts this test, making the outer goto the conditional jmp.
                    goto Equal;
                }

                // This becomes a conditional jmp forward to not favor it.
                goto NotEqual;
            }

#if TARGET_64BIT
            if (Vector128.IsHardwareAccelerated)
            {
                Debug.Assert(length <= (nuint)sizeof(nuint) * 2);

                nuint offset = length - (nuint)sizeof(nuint);
                nuint differentBits = LoadNUInt(ref first) - LoadNUInt(ref second);
                differentBits |= LoadNUInt(ref first, offset) - LoadNUInt(ref second, offset);
                result = (differentBits == 0);
                goto Result;
            }
            else
#endif
            {
                Debug.Assert(length >= (nuint)sizeof(nuint));
                {
                    nuint offset = 0;
                    nuint lengthToExamine = length - (nuint)sizeof(nuint);
                    // Unsigned, so it shouldn't have overflowed larger than length (rather than negative)
                    Debug.Assert(lengthToExamine < length);
                    if (lengthToExamine > 0)
                    {
                        do
                        {
                            // Compare unsigned so not do a sign extend mov on 64 bit
                            if (LoadNUInt(ref first, offset) != LoadNUInt(ref second, offset))
                            {
                                goto NotEqual;
                            }
                            offset += (nuint)sizeof(nuint);
                        } while (lengthToExamine > offset);
                    }

                    // Do final compare as sizeof(nuint) from end rather than start
                    result = (LoadNUInt(ref first, lengthToExamine) == LoadNUInt(ref second, lengthToExamine));
                    goto Result;
                }
            }

            // As there are so many true/false exit points the Jit will coalesce them to one location.
            // We want them at the end so the conditional early exit jmps are all jmp forwards so the
            // branch predictor in a uninitialized state will not take them e.g.
            // - loops are conditional jmps backwards and predicted
            // - exceptions are conditional forwards jmps and not predicted
        NotEqual:
            return false;
        }

        // Vector sub-search adapted from https://github.com/aspnet/KestrelHttpServer/pull/1138
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundByte(Vector<byte> match)
        {
            var vector64 = Vector.AsVectorUInt64(match);
            ulong candidate = 0;
            int i = 0;
            // Pattern unrolled by jit https://github.com/dotnet/coreclr/pull/8001
            for (; i < Vector<ulong>.Count; i++)
            {
                candidate = vector64[i];
                if (candidate != 0)
                {
                    break;
                }
            }

            // Single LEA instruction with jitted const (using function result)
            return i * 8 + LocateFirstFoundByte(candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe int SequenceCompareTo(ref byte first, int firstLength, ref byte second, int secondLength)
        {
            Debug.Assert(firstLength >= 0);
            Debug.Assert(secondLength >= 0);

            if (Unsafe.AreSame(ref first, ref second))
                goto Equal;

            nuint minLength = (nuint)(((uint)firstLength < (uint)secondLength) ? (uint)firstLength : (uint)secondLength);

            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = minLength;

            if (Vector256.IsHardwareAccelerated)
            {
                if (lengthToExamine >= (nuint)Vector256<byte>.Count)
                {
                    lengthToExamine -= (nuint)Vector256<byte>.Count;
                    uint matches;
                    while (lengthToExamine > offset)
                    {
                        matches = Vector256.Equals(Vector256.LoadUnsafe(ref first, offset), Vector256.LoadUnsafe(ref second, offset)).ExtractMostSignificantBits();
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.

                        // 32 elements in Vector256<byte> so we compare to uint.MaxValue to check if everything matched
                        if (matches == uint.MaxValue)
                        {
                            // All matched
                            offset += (nuint)Vector256<byte>.Count;
                            continue;
                        }

                        goto Difference;
                    }
                    // Move to Vector length from end for final compare
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Vector256.Equals(Vector256.LoadUnsafe(ref first, offset), Vector256.LoadUnsafe(ref second, offset)).ExtractMostSignificantBits();
                    if (matches == uint.MaxValue)
                    {
                        // All matched
                        goto Equal;
                    }
                Difference:
                    // Invert matches to find differences
                    uint differences = ~matches;
                    // Find bitflag offset of first difference and add to current offset
                    offset += (uint)BitOperations.TrailingZeroCount(differences);

                    int result = Unsafe.AddByteOffset(ref first, offset).CompareTo(Unsafe.AddByteOffset(ref second, offset));
                    Debug.Assert(result != 0);

                    return result;
                }

                if (lengthToExamine >= (nuint)Vector128<byte>.Count)
                {
                    lengthToExamine -= (nuint)Vector128<byte>.Count;
                    uint matches;
                    if (lengthToExamine > offset)
                    {
                        matches = Vector128.Equals(Vector128.LoadUnsafe(ref first, offset), Vector128.LoadUnsafe(ref second, offset)).ExtractMostSignificantBits();
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.

                        // 16 elements in Vector128<byte> so we compare to ushort.MaxValue to check if everything matched
                        if (matches != ushort.MaxValue)
                        {
                            goto Difference;
                        }
                    }
                    // Move to Vector length from end for final compare
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Vector128.Equals(Vector128.LoadUnsafe(ref first, offset), Vector128.LoadUnsafe(ref second, offset)).ExtractMostSignificantBits();
                    if (matches == ushort.MaxValue)
                    {
                        // All matched
                        goto Equal;
                    }
                Difference:
                    // Invert matches to find differences
                    uint differences = ~matches;
                    // Find bitflag offset of first difference and add to current offset
                    offset += (uint)BitOperations.TrailingZeroCount(differences);

                    int result = Unsafe.AddByteOffset(ref first, offset).CompareTo(Unsafe.AddByteOffset(ref second, offset));
                    Debug.Assert(result != 0);

                    return result;
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                if (lengthToExamine >= (nuint)Vector128<byte>.Count)
                {
                    lengthToExamine -= (nuint)Vector128<byte>.Count;
                    while (lengthToExamine > offset)
                    {
                        if (Vector128.LoadUnsafe(ref first, offset) == Vector128.LoadUnsafe(ref second, offset))
                        {
                            // All matched
                            offset += (nuint)Vector128<byte>.Count;
                            continue;
                        }

                        goto BytewiseCheck;
                    }
                    // Move to Vector length from end for final compare
                    offset = lengthToExamine;
                    if (Vector128.LoadUnsafe(ref first, offset) == Vector128.LoadUnsafe(ref second, offset))
                    {
                        // All matched
                        goto Equal;
                    }
                    goto BytewiseCheck;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                if (lengthToExamine > (nuint)Vector<byte>.Count)
                {
                    lengthToExamine -= (nuint)Vector<byte>.Count;
                    while (lengthToExamine > offset)
                    {
                        if (LoadVector(ref first, offset) != LoadVector(ref second, offset))
                        {
                            goto BytewiseCheck;
                        }
                        offset += (nuint)Vector<byte>.Count;
                    }
                    goto BytewiseCheck;
                }
            }

            if (lengthToExamine > (nuint)sizeof(nuint))
            {
                lengthToExamine -= (nuint)sizeof(nuint);
                while (lengthToExamine > offset)
                {
                    if (LoadNUInt(ref first, offset) != LoadNUInt(ref second, offset))
                    {
                        goto BytewiseCheck;
                    }
                    offset += (nuint)sizeof(nuint);
                }
            }

        BytewiseCheck:  // Workaround for https://github.com/dotnet/runtime/issues/8795
            while (minLength > offset)
            {
                int result = Unsafe.AddByteOffset(ref first, offset).CompareTo(Unsafe.AddByteOffset(ref second, offset));
                if (result != 0)
                    return result;
                offset += 1;
            }

        Equal:
            return firstLength - secondLength;
        }

        public static nuint CommonPrefixLength(ref byte first, ref byte second, nuint length)
        {
            nuint i;

            // It is ordered this way to match the default branch predictor rules, to don't have too much
            // overhead for short input-lengths.
            if (!Vector128.IsHardwareAccelerated || length < (nuint)Vector128<byte>.Count)
            {
                // To have kind of fast path for small inputs, we handle as much elements needed
                // so that either we are done or can use the unrolled loop below.
                i = length % 4;

                if (i > 0)
                {
                    if (first != second)
                    {
                        return 0;
                    }

                    if (i > 1)
                    {
                        if (Unsafe.Add(ref first, 1) != Unsafe.Add(ref second, 1))
                        {
                            return 1;
                        }

                        if (i > 2 && Unsafe.Add(ref first, 2) != Unsafe.Add(ref second, 2))
                        {
                            return 2;
                        }
                    }
                }

                for (; (nint)i <= (nint)length - 4; i += 4)
                {
                    if (Unsafe.Add(ref first, i + 0) != Unsafe.Add(ref second, i + 0)) goto Found0;
                    if (Unsafe.Add(ref first, i + 1) != Unsafe.Add(ref second, i + 1)) goto Found1;
                    if (Unsafe.Add(ref first, i + 2) != Unsafe.Add(ref second, i + 2)) goto Found2;
                    if (Unsafe.Add(ref first, i + 3) != Unsafe.Add(ref second, i + 3)) goto Found3;
                }

                return length;
            Found0:
                return i;
            Found1:
                return i + 1;
            Found2:
                return i + 2;
            Found3:
                return i + 3;
            }

            Debug.Assert(length >= (uint)Vector128<byte>.Count);

            uint mask;
            nuint lengthToExamine = length - (nuint)Vector128<byte>.Count;

            Vector128<byte> maskVec;
            i = 0;

            while (i < lengthToExamine)
            {
                maskVec = Vector128.Equals(
                    Vector128.LoadUnsafe(ref first, i),
                    Vector128.LoadUnsafe(ref second, i));

                mask = maskVec.ExtractMostSignificantBits();
                if (mask != 0xFFFF)
                {
                    goto Found;
                }

                i += (nuint)Vector128<byte>.Count;
            }

            // Do final compare as Vector128<byte>.Count from end rather than start
            i = lengthToExamine;
            maskVec = Vector128.Equals(
                Vector128.LoadUnsafe(ref first, i),
                Vector128.LoadUnsafe(ref second, i));

            mask = maskVec.ExtractMostSignificantBits();
            if (mask != 0xFFFF)
            {
                goto Found;
            }

            return length;

        Found:
            mask = ~mask;
            return i + uint.TrailingZeroCount(mask);
        }

        // Vector sub-search adapted from https://github.com/aspnet/KestrelHttpServer/pull/1138
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateLastFoundByte(Vector<byte> match)
        {
            var vector64 = Vector.AsVectorUInt64(match);
            ulong candidate = 0;
            int i = Vector<ulong>.Count - 1;

            // This pattern is only unrolled by the Jit if the limit is Vector<T>.Count
            // As such, we need a dummy iteration variable for that condition to be satisfied
            for (int j = 0; j < Vector<ulong>.Count; j++)
            {
                candidate = vector64[i];
                if (candidate != 0)
                {
                    break;
                }

                i--;
            }

            // Single LEA instruction with jitted const (using function result)
            return i * 8 + LocateLastFoundByte(candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundByte(ulong match)
            => BitOperations.TrailingZeroCount(match) >> 3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateLastFoundByte(ulong match)
            => BitOperations.Log2(match) >> 3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort LoadUShort(ref byte start)
            => Unsafe.ReadUnaligned<ushort>(ref start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint LoadUInt(ref byte start)
            => Unsafe.ReadUnaligned<uint>(ref start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint LoadUInt(ref byte start, nuint offset)
            => Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref start, offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint LoadNUInt(ref byte start)
            => Unsafe.ReadUnaligned<nuint>(ref start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint LoadNUInt(ref byte start, nuint offset)
            => Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref start, offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<byte> LoadVector(ref byte start, nuint offset)
            => Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref start, offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> LoadVector128(ref byte start, nuint offset)
            => Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.AddByteOffset(ref start, offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<byte> LoadVector256(ref byte start, nuint offset)
            => Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.AddByteOffset(ref start, offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint GetByteVectorSpanLength(nuint offset, int length)
            => (nuint)(uint)((length - (int)offset) & ~(Vector<byte>.Count - 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint GetByteVector128SpanLength(nuint offset, int length)
            => (nuint)(uint)((length - (int)offset) & ~(Vector128<byte>.Count - 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint GetByteVector256SpanLength(nuint offset, int length)
            => (nuint)(uint)((length - (int)offset) & ~(Vector256<byte>.Count - 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe nuint UnalignedCountVector(byte* searchSpace)
        {
            nint unaligned = (nint)searchSpace & (Vector<byte>.Count - 1);
            return (nuint)((Vector<byte>.Count - unaligned) & (Vector<byte>.Count - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe nuint UnalignedCountVector128(byte* searchSpace)
        {
            nint unaligned = (nint)searchSpace & (Vector128<byte>.Count - 1);
            return (nuint)(uint)((Vector128<byte>.Count - unaligned) & (Vector128<byte>.Count - 1));
        }

        public static void Reverse(ref byte buf, nuint length)
        {
            Debug.Assert(length > 1);

            nint remainder = (nint)length;
            nint offset = 0;

            // overlapping has a positive performance benefit around 48 elements
            if (Avx2.IsSupported && remainder >= (nint)(Vector256<byte>.Count * 1.5))
            {
                Vector256<byte> reverseMask = Vector256.Create(
                    (byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, // first 128-bit lane
                    15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0); // second 128-bit lane

                nint lastOffset = remainder - Vector256<byte>.Count;
                do
                {
                    // Load the values into vectors
                    Vector256<byte> tempFirst = Vector256.LoadUnsafe(ref buf, (nuint)offset);
                    Vector256<byte> tempLast = Vector256.LoadUnsafe(ref buf, (nuint)lastOffset);

                    // Avx2 operates on two 128-bit lanes rather than the full 256-bit vector.
                    // Perform a shuffle to reverse each 128-bit lane, then permute to finish reversing the vector:
                    //     +-------------------------------------------------------------------------------+
                    //     | A1 | B1 | C1 | D1 | E1 | F1 | G1 | H1 | I1 | J1 | K1 | L1 | M1 | N1 | O1 | P1 |
                    //     +-------------------------------------------------------------------------------+
                    //     | A2 | B2 | C2 | D2 | E2 | F2 | G2 | H2 | I2 | J2 | K2 | L2 | M2 | N2 | O2 | P2 |
                    //     +-------------------------------------------------------------------------------+
                    //         Shuffle --->
                    //     +-------------------------------------------------------------------------------+
                    //     | P1 | O1 | N1 | M1 | L1 | K1 | J1 | I1 | H1 | G1 | F1 | E1 | D1 | C1 | B1 | A1 |
                    //     +-------------------------------------------------------------------------------+
                    //     | P2 | O2 | N2 | M2 | L2 | K2 | J2 | I2 | H2 | G2 | F2 | E2 | D2 | C2 | B2 | A2 |
                    //     +-------------------------------------------------------------------------------+
                    //         Permute --->
                    //     +-------------------------------------------------------------------------------+
                    //     | P2 | O2 | N2 | M2 | L2 | K2 | J2 | I2 | H2 | G2 | F2 | E2 | D2 | C2 | B2 | A2 |
                    //     +-------------------------------------------------------------------------------+
                    //     | P1 | O1 | N1 | M1 | L1 | K1 | J1 | I1 | H1 | G1 | F1 | E1 | D1 | C1 | B1 | A1 |
                    //     +-------------------------------------------------------------------------------+
                    tempFirst = Avx2.Shuffle(tempFirst, reverseMask);
                    tempFirst = Avx2.Permute2x128(tempFirst, tempFirst, 0b00_01);
                    tempLast = Avx2.Shuffle(tempLast, reverseMask);
                    tempLast = Avx2.Permute2x128(tempLast, tempLast, 0b00_01);

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref buf, (nuint)offset);
                    tempFirst.StoreUnsafe(ref buf, (nuint)lastOffset);

                    offset += Vector256<byte>.Count;
                    lastOffset -= Vector256<byte>.Count;
                } while (lastOffset >= offset);

                remainder = lastOffset + Vector256<byte>.Count - offset;
            }
            else if (Vector128.IsHardwareAccelerated && remainder >= Vector128<byte>.Count * 2)
            {
                nint lastOffset = remainder - Vector128<byte>.Count;
                do
                {
                    // Load the values into vectors
                    Vector128<byte> tempFirst = Vector128.LoadUnsafe(ref buf, (nuint)offset);
                    Vector128<byte> tempLast = Vector128.LoadUnsafe(ref buf, (nuint)lastOffset);

                    // Shuffle to reverse each vector:
                    //     +---------------------------------------------------------------+
                    //     | A | B | C | D | E | F | G | H | I | J | K | L | M | N | O | P |
                    //     +---------------------------------------------------------------+
                    //          --->
                    //     +---------------------------------------------------------------+
                    //     | P | O | N | M | L | K | J | I | H | G | F | E | D | C | B | A |
                    //     +---------------------------------------------------------------+
                    tempFirst = Vector128.Shuffle(tempFirst, Vector128.Create(
                        (byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));
                    tempLast = Vector128.Shuffle(tempLast, Vector128.Create(
                        (byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref buf, (nuint)offset);
                    tempFirst.StoreUnsafe(ref buf, (nuint)lastOffset);

                    offset += Vector128<byte>.Count;
                    lastOffset -= Vector128<byte>.Count;
                } while (lastOffset >= offset);

                remainder = lastOffset + Vector128<byte>.Count - offset;
            }

            if (remainder >= sizeof(long))
            {
                nint lastOffset = (nint)length - offset - sizeof(long);
                do
                {
                    long tempFirst = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref buf, offset));
                    long tempLast = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref buf, lastOffset));

                    // swap and store in reversed position
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref buf, offset), BinaryPrimitives.ReverseEndianness(tempLast));
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref buf, lastOffset), BinaryPrimitives.ReverseEndianness(tempFirst));

                    offset += sizeof(long);
                    lastOffset -= sizeof(long);
                } while (lastOffset >= offset);

                remainder = lastOffset + sizeof(long) - offset;
            }

            if (remainder >= sizeof(int))
            {
                nint lastOffset = (nint)length - offset - sizeof(int);
                do
                {
                    int tempFirst = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref buf, offset));
                    int tempLast = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref buf, lastOffset));

                    // swap and store in reversed position
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref buf, offset), BinaryPrimitives.ReverseEndianness(tempLast));
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref buf, lastOffset), BinaryPrimitives.ReverseEndianness(tempFirst));

                    offset += sizeof(int);
                    lastOffset -= sizeof(int);
                } while (lastOffset >= offset);

                remainder = lastOffset + sizeof(int) - offset;
            }

            if (remainder > 1)
            {
                ReverseInner(ref Unsafe.Add(ref buf, offset), (nuint)remainder);
            }
        }
    }
}
