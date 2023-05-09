// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System
{
    internal static partial class SpanHelpers // .Char
    {
        public static int IndexOf(ref char searchSpace, int searchSpaceLength, ref char value, int valueLength)
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return 0;  // A zero-length sequence is always treated as "found" at the start of the search space.

            int valueTailLength = valueLength - 1;
            if (valueTailLength == 0)
            {
                // for single-char values use plain IndexOf
                return IndexOfChar(ref searchSpace, value, searchSpaceLength);
            }

            nint offset = 0;
            char valueHead = value;
            int searchSpaceMinusValueTailLength = searchSpaceLength - valueTailLength;
            if (Vector128.IsHardwareAccelerated && searchSpaceMinusValueTailLength >= Vector128<ushort>.Count)
            {
                goto SEARCH_TWO_CHARS;
            }

            ref byte valueTail = ref Unsafe.As<char, byte>(ref Unsafe.Add(ref value, 1));
            int remainingSearchSpaceLength = searchSpaceMinusValueTailLength;

            while (remainingSearchSpaceLength > 0)
            {
                // Do a quick search for the first element of "value".
                // Using the non-packed variant as the input is short and would not benefit from the packed implementation.
                int relativeIndex = NonPackedIndexOfChar(ref Unsafe.Add(ref searchSpace, offset), valueHead, remainingSearchSpaceLength);
                if (relativeIndex < 0)
                    break;

                remainingSearchSpaceLength -= relativeIndex;
                offset += relativeIndex;

                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Found the first element of "value". See if the tail matches.
                if (SequenceEqual(
                        ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, offset + 1)),
                        ref valueTail,
                        (nuint)(uint)valueTailLength * 2))
                {
                    return (int)offset;  // The tail matched. Return a successful find.
                }

                remainingSearchSpaceLength--;
                offset++;
            }
            return -1;

            // Based on http://0x80.pl/articles/simd-strfind.html#algorithm-1-generic-simd "Algorithm 1: Generic SIMD" by Wojciech Mula
            // Some details about the implementation can also be found in https://github.com/dotnet/runtime/pull/63285
        SEARCH_TWO_CHARS:
            if (Vector256.IsHardwareAccelerated && searchSpaceMinusValueTailLength - Vector256<ushort>.Count >= 0)
            {
                // Find the last unique (which is not equal to ch1) character
                // the algorithm is fine if both are equal, just a little bit less efficient
                ushort ch2Val = Unsafe.Add(ref value, valueTailLength);
                nint ch1ch2Distance = valueTailLength;
                while (ch2Val == valueHead && ch1ch2Distance > 1)
                    ch2Val = Unsafe.Add(ref value, --ch1ch2Distance);

                Vector256<ushort> ch1 = Vector256.Create((ushort)valueHead);
                Vector256<ushort> ch2 = Vector256.Create(ch2Val);

                nint searchSpaceMinusValueTailLengthAndVector =
                    searchSpaceMinusValueTailLength - (nint)Vector256<ushort>.Count;

                do
                {
                    // Make sure we don't go out of bounds
                    Debug.Assert(offset + ch1ch2Distance + Vector256<ushort>.Count <= searchSpaceLength);

                    Vector256<ushort> cmpCh2 = Vector256.Equals(ch2, Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
                    Vector256<ushort> cmpCh1 = Vector256.Equals(ch1, Vector256.LoadUnsafe(ref searchSpace, (nuint)offset));
                    Vector256<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();

                    // Early out: cmpAnd is all zeros
                    if (cmpAnd != Vector256<byte>.Zero)
                    {
                        goto CANDIDATE_FOUND;
                    }

                LOOP_FOOTER:
                    offset += Vector256<ushort>.Count;

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
                        // div by 2 (shr) because we work with 2-byte chars
                        nint charPos = (nint)((uint)bitPos / 2);
                        if (valueLength == 2 || // we already matched two chars
                            SequenceEqual(
                                ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, offset + charPos)),
                                ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * 2))
                        {
                            return (int)(offset + charPos);
                        }

                        // Clear two the lowest set bits
                        if (Bmi1.IsSupported)
                            mask = Bmi1.ResetLowestSetBit(Bmi1.ResetLowestSetBit(mask));
                        else
                            mask &= ~(uint)(0b11 << bitPos);
                    } while (mask != 0);
                    goto LOOP_FOOTER;

                } while (true);
            }
            else // 128bit vector path (SSE2 or AdvSimd)
            {
                // Find the last unique (which is not equal to ch1) character
                // the algorithm is fine if both are equal, just a little bit less efficient
                ushort ch2Val = Unsafe.Add(ref value, valueTailLength);
                nint ch1ch2Distance = valueTailLength;
                while (ch2Val == valueHead && ch1ch2Distance > 1)
                    ch2Val = Unsafe.Add(ref value, --ch1ch2Distance);

                Vector128<ushort> ch1 = Vector128.Create((ushort)valueHead);
                Vector128<ushort> ch2 = Vector128.Create(ch2Val);

                nint searchSpaceMinusValueTailLengthAndVector =
                    searchSpaceMinusValueTailLength - (nint)Vector128<ushort>.Count;

                do
                {
                    // Make sure we don't go out of bounds
                    Debug.Assert(offset + ch1ch2Distance + Vector128<ushort>.Count <= searchSpaceLength);

                    Vector128<ushort> cmpCh2 = Vector128.Equals(ch2, Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
                    Vector128<ushort> cmpCh1 = Vector128.Equals(ch1, Vector128.LoadUnsafe(ref searchSpace, (nuint)offset));
                    Vector128<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();

                    // Early out: cmpAnd is all zeros
                    if (cmpAnd != Vector128<byte>.Zero)
                    {
                        goto CANDIDATE_FOUND;
                    }

                LOOP_FOOTER:
                    offset += Vector128<ushort>.Count;

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
                        // div by 2 (shr) because we work with 2-byte chars
                        int charPos = (int)((uint)bitPos / 2);
                        if (valueLength == 2 || // we already matched two chars
                            SequenceEqual(
                                ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, offset + charPos)),
                                ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * 2))
                        {
                            return (int)(offset + charPos);
                        }

                        // Clear two lowest set bits
                        if (Bmi1.IsSupported)
                            mask = Bmi1.ResetLowestSetBit(Bmi1.ResetLowestSetBit(mask));
                        else
                            mask &= ~(uint)(0b11 << bitPos);
                    } while (mask != 0);
                    goto LOOP_FOOTER;

                } while (true);
            }
        }

        public static int LastIndexOf(ref char searchSpace, int searchSpaceLength, ref char value, int valueLength)
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return searchSpaceLength;  // A zero-length sequence is always treated as "found" at the end of the search space.

            int valueTailLength = valueLength - 1;
            if (valueTailLength == 0)
                return LastIndexOfValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)value, searchSpaceLength); // for single-char values use plain LastIndexOf

            int offset = 0;
            char valueHead = value;
            int searchSpaceMinusValueTailLength = searchSpaceLength - valueTailLength;
            if (Vector128.IsHardwareAccelerated && searchSpaceMinusValueTailLength >= Vector128<ushort>.Count)
            {
                goto SEARCH_TWO_CHARS;
            }

            ref byte valueTail = ref Unsafe.As<char, byte>(ref Unsafe.Add(ref value, 1));

            while (true)
            {
                Debug.Assert(0 <= offset && offset <= searchSpaceLength); // Ensures no deceptive underflows in the computation of "remainingSearchSpaceLength".
                int remainingSearchSpaceLength = searchSpaceLength - offset - valueTailLength;
                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Do a quick search for the first element of "value".
                int relativeIndex = LastIndexOfValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)valueHead, remainingSearchSpaceLength);
                if (relativeIndex == -1)
                    break;

                // Found the first element of "value". See if the tail matches.
                if (SequenceEqual(
                        ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, relativeIndex + 1)),
                        ref valueTail, (nuint)(uint)valueTailLength * 2))
                {
                    return relativeIndex; // The tail matched. Return a successful find.
                }

                offset += remainingSearchSpaceLength - relativeIndex;
            }
            return -1;

            // Based on http://0x80.pl/articles/simd-strfind.html#algorithm-1-generic-simd "Algorithm 1: Generic SIMD" by Wojciech Mula
            // Some details about the implementation can also be found in https://github.com/dotnet/runtime/pull/63285
        SEARCH_TWO_CHARS:
            if (Vector256.IsHardwareAccelerated && searchSpaceMinusValueTailLength >= Vector256<ushort>.Count)
            {
                offset = searchSpaceMinusValueTailLength - Vector256<ushort>.Count;

                // Find the last unique (which is not equal to ch1) char
                // the algorithm is fine if both are equal, just a little bit less efficient
                char ch2Val = Unsafe.Add(ref value, valueTailLength);
                int ch1ch2Distance = valueTailLength;
                while (ch2Val == valueHead && ch1ch2Distance > 1)
                    ch2Val = Unsafe.Add(ref value, --ch1ch2Distance);

                Vector256<ushort> ch1 = Vector256.Create((ushort)valueHead);
                Vector256<ushort> ch2 = Vector256.Create((ushort)ch2Val);

                do
                {

                    Vector256<ushort> cmpCh1 = Vector256.Equals(ch1, Vector256.LoadUnsafe(ref searchSpace, (nuint)offset));
                    Vector256<ushort> cmpCh2 = Vector256.Equals(ch2, Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
                    Vector256<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();

                    // Early out: cmpAnd is all zeros
                    if (cmpAnd != Vector256<byte>.Zero)
                    {
                        uint mask = cmpAnd.ExtractMostSignificantBits();
                        do
                        {
                            // unlike IndexOf, here we use LZCNT to process matches starting from the end
                            int bitPos = 30 - BitOperations.LeadingZeroCount(mask);
                            int charPos = (int)((uint)bitPos / 2);

                            if (valueLength == 2 || // we already matched two chars
                                SequenceEqual(
                                    ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, offset + charPos)),
                                    ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * 2))
                            {
                                return charPos + offset;
                            }
                            mask &= ~(uint)(0b11 << bitPos); // clear two highest set bits.
                        } while (mask != 0);
                    }

                    offset -= Vector256<ushort>.Count;
                    if (offset == -Vector256<ushort>.Count)
                        return -1;
                    // Overlap with the current chunk if there is not enough room for the next one
                    if (offset < 0)
                        offset = 0;
                } while (true);
            }
            else // 128bit vector path (SSE2 or AdvSimd)
            {
                offset = searchSpaceMinusValueTailLength - Vector128<ushort>.Count;

                // Find the last unique (which is not equal to ch1) char
                // the algorithm is fine if both are equal, just a little bit less efficient
                char ch2Val = Unsafe.Add(ref value, valueTailLength);
                int ch1ch2Distance = valueTailLength;
                while (ch2Val == value && ch1ch2Distance > 1)
                    ch2Val = Unsafe.Add(ref value, --ch1ch2Distance);

                Vector128<ushort> ch1 = Vector128.Create((ushort)value);
                Vector128<ushort> ch2 = Vector128.Create((ushort)ch2Val);

                do
                {
                    Vector128<ushort> cmpCh1 = Vector128.Equals(ch1, Vector128.LoadUnsafe(ref searchSpace, (nuint)offset));
                    Vector128<ushort> cmpCh2 = Vector128.Equals(ch2, Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
                    Vector128<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();

                    // Early out: cmpAnd is all zeros
                    // it's especially important for ARM where ExtractMostSignificantBits is not cheap
                    if (cmpAnd != Vector128<byte>.Zero)
                    {
                        uint mask = cmpAnd.ExtractMostSignificantBits();
                        do
                        {
                            // unlike IndexOf, here we use LZCNT to process matches starting from the end
                            int bitPos = 30 - BitOperations.LeadingZeroCount(mask);
                            int charPos = (int)((uint)bitPos / 2);

                            if (valueLength == 2 || // we already matched two chars
                                SequenceEqual(
                                    ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, offset + charPos)),
                                    ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * 2))
                            {
                                return charPos + offset;
                            }
                            mask &= ~(uint)(0b11 << bitPos); // clear two the highest set bits.
                        } while (mask != 0);
                    }

                    offset -= Vector128<ushort>.Count;
                    if (offset == -Vector128<ushort>.Count)
                        return -1;
                    // Overlap with the current chunk if there is not enough room for the next one
                    if (offset < 0)
                        offset = 0;
                } while (true);
            }
        }

        public static unsafe int SequenceCompareTo(ref char first, int firstLength, ref char second, int secondLength)
        {
            Debug.Assert(firstLength >= 0);
            Debug.Assert(secondLength >= 0);

            int lengthDelta = firstLength - secondLength;

            if (Unsafe.AreSame(ref first, ref second))
                goto Equal;

            nuint minLength = (nuint)(((uint)firstLength < (uint)secondLength) ? (uint)firstLength : (uint)secondLength);
            nuint i = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations

            if (minLength >= (nuint)(sizeof(nuint) / sizeof(char)))
            {
                if (Vector.IsHardwareAccelerated && minLength >= (nuint)Vector<ushort>.Count)
                {
                    nuint nLength = minLength - (nuint)Vector<ushort>.Count;
                    do
                    {
                        if (Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)i))) !=
                            Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, (nint)i))))
                        {
                            break;
                        }
                        i += (nuint)Vector<ushort>.Count;
                    }
                    while (nLength >= i);
                }

                while (minLength >= (i + (nuint)(sizeof(nuint) / sizeof(char))))
                {
                    if (Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)i))) !=
                        Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, (nint)i))))
                    {
                        break;
                    }
                    i += (nuint)(sizeof(nuint) / sizeof(char));
                }
            }

#if TARGET_64BIT
            if (minLength >= (i + sizeof(int) / sizeof(char)))
            {
                if (Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)i))) ==
                    Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, (nint)i))))
                {
                    i += sizeof(int) / sizeof(char);
                }
            }
#endif

            while (i < minLength)
            {
                int result = Unsafe.Add(ref first, (nint)i).CompareTo(Unsafe.Add(ref second, (nint)i));
                if (result != 0)
                    return result;
                i += 1;
            }

        Equal:
            return lengthDelta;
        }

        // IndexOfNullCharacter processes memory in aligned chunks, and thus it won't crash even if it accesses memory beyond the null terminator.
        // This behavior is an implementation detail of the runtime and callers outside System.Private.CoreLib must not depend on it.
        public static unsafe int IndexOfNullCharacter(char* searchSpace)
        {
            const char value = '\0';
            const int length = int.MaxValue;

            nint offset = 0;
            nint lengthToExamine = length;

            if (((int)searchSpace & 1) != 0)
            {
                // Input isn't char aligned, we won't be able to align it to a Vector
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                // Avx2 branch also operates on Sse2 sizes, so check is combined.
                // Needs to be double length to allow us to align the data first.
                lengthToExamine = UnalignedCountVector128(searchSpace);
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Needs to be double length to allow us to align the data first.
                lengthToExamine = UnalignedCountVector(searchSpace);
            }

        SequentialScan:
            // In the non-vector case lengthToExamine is the total length.
            // In the vector case lengthToExamine first aligns to Vector,
            // then in a second pass after the Vector lengths is the
            // remaining data that is shorter than a Vector length.
            while (lengthToExamine >= 4)
            {
                if (value == searchSpace[offset])
                    goto Found;
                if (value == searchSpace[offset + 1])
                    goto Found1;
                if (value == searchSpace[offset + 2])
                    goto Found2;
                if (value == searchSpace[offset + 3])
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                if (value == searchSpace[offset])
                    goto Found;

                offset++;
                lengthToExamine--;
            }

            // We get past SequentialScan only if IsHardwareAccelerated is true. However, we still have the redundant check to allow
            // the JIT to see that the code is unreachable and eliminate it when the platform does not have hardware accelerated.
            if (Vector256.IsHardwareAccelerated)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector128<ushort>.Count);
                    if (((nint)(searchSpace + (nint)offset) & (nint)(Vector256<byte>.Count - 1)) != 0)
                    {
                        // Not currently aligned to Vector256 (is aligned to Vector128); this can cause a problem for searches
                        // with no upper bound e.g. String.wcslen. Start with a check on Vector128 to align to Vector256,
                        // before moving to processing Vector256.

                        // This ensures we do not fault across memory pages
                        // while searching for an end of string. Specifically that this assumes that the length is either correct
                        // or that the data is pinned otherwise it may cause an AccessViolation from crossing a page boundary into an
                        // unowned page. If the search is unbounded (e.g. null terminator in wcslen) and the search value is not found,
                        // again this will likely cause an AccessViolation. However, correctly bounded searches will return -1 rather
                        // than ever causing an AV.
                        Vector128<ushort> search = *(Vector128<ushort>*)(searchSpace + (nuint)offset);

                        // Same method as below
                        uint matches = Vector128.Equals(Vector128<ushort>.Zero, search).AsByte().ExtractMostSignificantBits();
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += Vector128<ushort>.Count;
                        }
                        else
                        {
                            // Find bitflag offset of first match and add to current offset
                            return (int)(offset + ((uint)BitOperations.TrailingZeroCount(matches) / sizeof(char)));
                        }
                    }

                    lengthToExamine = GetCharVector256SpanLength(offset, length);
                    if (lengthToExamine > 0)
                    {
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector256<ushort>.Count);

                            Vector256<ushort> search = *(Vector256<ushort>*)(searchSpace + (nuint)offset);
                            uint matches = Vector256.Equals(Vector256<ushort>.Zero, search).AsByte().ExtractMostSignificantBits();
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // Zero flags set so no matches
                                offset += Vector256<ushort>.Count;
                                lengthToExamine -= Vector256<ushort>.Count;
                                continue;
                            }

                            // Find bitflag offset of first match and add to current offset,
                            // flags are in bytes so divide for chars
                            return (int)(offset + ((uint)BitOperations.TrailingZeroCount(matches) / sizeof(char)));
                        } while (lengthToExamine > 0);
                    }

                    lengthToExamine = GetCharVector128SpanLength(offset, length);
                    if (lengthToExamine > 0)
                    {
                        Debug.Assert(lengthToExamine >= Vector128<ushort>.Count);

                        Vector128<ushort> search = *(Vector128<ushort>*)(searchSpace + (nuint)offset);

                        // Same method as above
                        uint matches = Vector128.Equals(Vector128<ushort>.Zero, search).AsByte().ExtractMostSignificantBits();
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += Vector128<ushort>.Count;
                            // Don't need to change lengthToExamine here as we don't use its current value again.
                        }
                        else
                        {
                            // Find bitflag offset of first match and add to current offset,
                            // flags are in bytes so divide for chars
                            return (int)(offset + ((uint)BitOperations.TrailingZeroCount(matches) / sizeof(char)));
                        }
                    }

                    if (offset < length)
                    {
                        lengthToExamine = length - offset;
                        goto SequentialScan;
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector128<ushort>.Count);

                    lengthToExamine = GetCharVector128SpanLength(offset, length);
                    if (lengthToExamine > 0)
                    {
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector128<ushort>.Count);

                            Vector128<ushort> search = *(Vector128<ushort>*)(searchSpace + (nuint)offset);

                            // Same method as above
                            Vector128<ushort> compareResult = Vector128.Equals(Vector128<ushort>.Zero, search);
                            if (compareResult == Vector128<ushort>.Zero)
                            {
                                // Zero flags set so no matches
                                offset += Vector128<ushort>.Count;
                                lengthToExamine -= Vector128<ushort>.Count;
                                continue;
                            }

                            // Find bitflag offset of first match and add to current offset,
                            // flags are in bytes so divide for chars
                            uint matches = compareResult.AsByte().ExtractMostSignificantBits();
                            return (int)(offset + ((uint)BitOperations.TrailingZeroCount(matches) / sizeof(char)));
                        } while (lengthToExamine > 0);
                    }

                    if (offset < length)
                    {
                        lengthToExamine = length - offset;
                        goto SequentialScan;
                    }
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector<ushort>.Count);

                    lengthToExamine = GetCharVectorSpanLength(offset, length);

                    if (lengthToExamine > 0)
                    {
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector<ushort>.Count);

                            var matches = Vector.Equals(Vector<ushort>.Zero, *(Vector<ushort>*)(searchSpace + (nuint)offset));
                            if (Vector<ushort>.Zero.Equals(matches))
                            {
                                offset += Vector<ushort>.Count;
                                lengthToExamine -= Vector<ushort>.Count;
                                continue;
                            }

                            // Find offset of first match
                            return (int)(offset + LocateFirstFoundChar(matches));
                        } while (lengthToExamine > 0);
                    }

                    if (offset < length)
                    {
                        lengthToExamine = length - offset;
                        goto SequentialScan;
                    }
                }
            }

            ThrowMustBeNullTerminatedString();
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)(offset);
        }

        // Vector sub-search adapted from https://github.com/aspnet/KestrelHttpServer/pull/1138
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundChar(Vector<ushort> match)
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
            return i * 4 + LocateFirstFoundChar(candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundChar(ulong match)
            => BitOperations.TrailingZeroCount(match) >> 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<ushort> LoadVector(ref char start, nint offset)
            => Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<ushort> LoadVector(ref char start, nuint offset)
            => Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, (nint)offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetCharVectorSpanLength(nint offset, nint length)
            => (length - offset) & ~(Vector<ushort>.Count - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetCharVector128SpanLength(nint offset, nint length)
            => (length - offset) & ~(Vector128<ushort>.Count - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetCharVector256SpanLength(nint offset, nint length)
            => (length - offset) & ~(Vector256<ushort>.Count - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe nint UnalignedCountVector(char* searchSpace)
        {
            const int ElementsPerByte = sizeof(ushort) / sizeof(byte);
            return (nint)(uint)(-(int)searchSpace / ElementsPerByte) & (Vector<ushort>.Count - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe nint UnalignedCountVector128(char* searchSpace)
        {
            const int ElementsPerByte = sizeof(ushort) / sizeof(byte);
            return (nint)(uint)(-(int)searchSpace / ElementsPerByte) & (Vector128<ushort>.Count - 1);
        }

        public static void Reverse(ref char buf, nuint length)
        {
            Debug.Assert(length > 1);

            nint remainder = (nint)length;
            nint offset = 0;

            // overlapping has a positive performance benefit around 24 elements
            if (Avx2.IsSupported && remainder >= (nint)(Vector256<ushort>.Count * 1.5))
            {
                Vector256<byte> reverseMask = Vector256.Create(
                    (byte)14, 15, 12, 13, 10, 11, 8, 9, 6, 7, 4, 5, 2, 3, 0, 1, // first 128-bit lane
                    14, 15, 12, 13, 10, 11, 8, 9, 6, 7, 4, 5, 2, 3, 0, 1); // second 128-bit lane

                nint lastOffset = remainder - Vector256<ushort>.Count;
                do
                {
                    ref byte first = ref Unsafe.As<char, byte>(ref Unsafe.Add(ref buf, offset));
                    ref byte last = ref Unsafe.As<char, byte>(ref Unsafe.Add(ref buf, lastOffset));

                    Vector256<byte> tempFirst = Vector256.LoadUnsafe(ref first);
                    Vector256<byte> tempLast = Vector256.LoadUnsafe(ref last);

                    // Avx2 operates on two 128-bit lanes rather than the full 256-bit vector.
                    // Perform a shuffle to reverse each 128-bit lane, then permute to finish reversing the vector:
                    //     +---------------------------------------------------------------+
                    //     | A | B | C | D | E | F | G | H | I | J | K | L | M | N | O | P |
                    //     +---------------------------------------------------------------+
                    //         Shuffle --->
                    //     +---------------------------------------------------------------+
                    //     | H | G | F | E | D | C | B | A | P | O | N | M | L | K | J | I |
                    //     +---------------------------------------------------------------+
                    //         Permute --->
                    //     +---------------------------------------------------------------+
                    //     | P | O | N | M | L | K | J | I | H | G | F | E | D | C | B | A |
                    //     +---------------------------------------------------------------+
                    tempFirst = Avx2.Shuffle(tempFirst, reverseMask);
                    tempFirst = Avx2.Permute2x128(tempFirst, tempFirst, 0b00_01);
                    tempLast = Avx2.Shuffle(tempLast, reverseMask);
                    tempLast = Avx2.Permute2x128(tempLast, tempLast, 0b00_01);

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref first);
                    tempFirst.StoreUnsafe(ref last);

                    offset += Vector256<ushort>.Count;
                    lastOffset -= Vector256<ushort>.Count;
                } while (lastOffset >= offset);

                remainder = (lastOffset + Vector256<ushort>.Count - offset);
            }
            else if (Vector128.IsHardwareAccelerated && remainder >= Vector128<ushort>.Count * 2)
            {
                nint lastOffset = remainder - Vector128<ushort>.Count;
                do
                {
                    ref ushort first = ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref buf, offset));
                    ref ushort last = ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref buf, lastOffset));

                    Vector128<ushort> tempFirst = Vector128.LoadUnsafe(ref first);
                    Vector128<ushort> tempLast = Vector128.LoadUnsafe(ref last);

                    // Shuffle to reverse each vector:
                    //     +-------------------------------+
                    //     | A | B | C | D | E | F | G | H |
                    //     +-------------------------------+
                    //          --->
                    //     +-------------------------------+
                    //     | H | G | F | E | D | C | B | A |
                    //     +-------------------------------+
                    tempFirst = Vector128.Shuffle(tempFirst, Vector128.Create((ushort)7, 6, 5, 4, 3, 2, 1, 0));
                    tempLast = Vector128.Shuffle(tempLast, Vector128.Create((ushort)7, 6, 5, 4, 3, 2, 1, 0));

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref first);
                    tempFirst.StoreUnsafe(ref last);

                    offset += Vector128<ushort>.Count;
                    lastOffset -= Vector128<ushort>.Count;
                } while (lastOffset >= offset);

                remainder = (lastOffset + Vector128<ushort>.Count - offset);
            }

            // Store any remaining values one-by-one
            if (remainder > 1)
            {
                ReverseInner(ref Unsafe.Add(ref buf, offset), (nuint)remainder);
            }
        }
    }
}
