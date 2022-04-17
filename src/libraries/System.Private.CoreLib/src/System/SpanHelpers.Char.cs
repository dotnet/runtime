// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
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
                return IndexOf(ref searchSpace, value, searchSpaceLength);
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
                int relativeIndex = IndexOf(ref Unsafe.Add(ref searchSpace, offset), valueHead, remainingSearchSpaceLength);
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

            // Based on http://0x80.pl/articles/simd-strfind.html#algorithm-1-generic-simd "Algorithm 1: Generic SIMD" by Wojciech Muła
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

                    Vector256<ushort> cmpCh2 = Vector256.Equals(ch2, LoadVector256(ref searchSpace, offset + ch1ch2Distance));
                    Vector256<ushort> cmpCh1 = Vector256.Equals(ch1, LoadVector256(ref searchSpace, offset));
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

                    Vector128<ushort> cmpCh2 = Vector128.Equals(ch2, LoadVector128(ref searchSpace, offset + ch1ch2Distance));
                    Vector128<ushort> cmpCh1 = Vector128.Equals(ch1, LoadVector128(ref searchSpace, offset));
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
                return LastIndexOf(ref searchSpace, value, searchSpaceLength); // for single-char values use plain LastIndexOf

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
                int relativeIndex = LastIndexOf(ref searchSpace, valueHead, remainingSearchSpaceLength);
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

            // Based on http://0x80.pl/articles/simd-strfind.html#algorithm-1-generic-simd "Algorithm 1: Generic SIMD" by Wojciech Muła
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

                    Vector256<ushort> cmpCh1 = Vector256.Equals(ch1, LoadVector256(ref searchSpace, (nuint)offset));
                    Vector256<ushort> cmpCh2 = Vector256.Equals(ch2, LoadVector256(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
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
                    Vector128<ushort> cmpCh1 = Vector128.Equals(ch1, LoadVector128(ref searchSpace, (nuint)offset));
                    Vector128<ushort> cmpCh2 = Vector128.Equals(ch2, LoadVector128(ref searchSpace, (nuint)(offset + ch1ch2Distance)));
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
                    if (Unsafe.ReadUnaligned<nuint> (ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)i))) !=
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

        // Adapted from IndexOf(...)
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe bool Contains(ref char searchSpace, char value, int length)
        {
            Debug.Assert(length >= 0);

            fixed (char* pChars = &searchSpace)
            {
                char* pCh = pChars;
                char* pEndCh = pCh + length;

                if (Vector.IsHardwareAccelerated && length >= Vector<ushort>.Count * 2)
                {
                    // Figure out how many characters to read sequentially until we are vector aligned
                    // This is equivalent to:
                    //         unaligned = ((int)pCh % Unsafe.SizeOf<Vector<ushort>>()) / elementsPerByte
                    //         length = (Vector<ushort>.Count - unaligned) % Vector<ushort>.Count
                    const int elementsPerByte = sizeof(ushort) / sizeof(byte);
                    int unaligned = ((int)pCh & (Unsafe.SizeOf<Vector<ushort>>() - 1)) / elementsPerByte;
                    length = (Vector<ushort>.Count - unaligned) & (Vector<ushort>.Count - 1);
                }

        SequentialScan:
                while (length >= 4)
                {
                    length -= 4;

                    if (value == *pCh ||
                        value == *(pCh + 1) ||
                        value == *(pCh + 2) ||
                        value == *(pCh + 3))
                    {
                        goto Found;
                    }

                    pCh += 4;
                }

                while (length > 0)
                {
                    length--;

                    if (value == *pCh)
                        goto Found;

                    pCh++;
                }

                // We get past SequentialScan only if IsHardwareAccelerated is true. However, we still have the redundant check to allow
                // the JIT to see that the code is unreachable and eliminate it when the platform does not have hardware accelerated.
                if (Vector.IsHardwareAccelerated && pCh < pEndCh)
                {
                    // Get the highest multiple of Vector<ushort>.Count that is within the search space.
                    // That will be how many times we iterate in the loop below.
                    // This is equivalent to: length = Vector<ushort>.Count * ((int)(pEndCh - pCh) / Vector<ushort>.Count)
                    length = (int)((pEndCh - pCh) & ~(Vector<ushort>.Count - 1));

                    // Get comparison Vector
                    Vector<ushort> vComparison = new Vector<ushort>(value);

                    while (length > 0)
                    {
                        // Using Unsafe.Read instead of ReadUnaligned since the search space is pinned and pCh is always vector aligned
                        Debug.Assert(((int)pCh & (Unsafe.SizeOf<Vector<ushort>>() - 1)) == 0);
                        Vector<ushort> vMatches = Vector.Equals(vComparison, Unsafe.Read<Vector<ushort>>(pCh));
                        if (Vector<ushort>.Zero.Equals(vMatches))
                        {
                            pCh += Vector<ushort>.Count;
                            length -= Vector<ushort>.Count;
                            continue;
                        }

                        goto Found;
                    }

                    if (pCh < pEndCh)
                    {
                        length = (int)(pEndCh - pCh);
                        goto SequentialScan;
                    }
                }

                return false;

        Found:
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe int IndexOf(ref char searchSpace, char value, int length)
        {
            Debug.Assert(length >= 0);

            nint offset = 0;
            nint lengthToExamine = length;

            if (((int)Unsafe.AsPointer(ref searchSpace) & 1) != 0)
            {
                // Input isn't char aligned, we won't be able to align it to a Vector
            }
            else if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported)
            {
                // Avx2 branch also operates on Sse2 sizes, so check is combined.
                // Needs to be double length to allow us to align the data first.
                if (length >= Vector128<ushort>.Count * 2)
                {
                    lengthToExamine = UnalignedCountVector128(ref searchSpace);
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Needs to be double length to allow us to align the data first.
                if (length >= Vector<ushort>.Count * 2)
                {
                    lengthToExamine = UnalignedCountVector(ref searchSpace);
                }
            }

        SequentialScan:
            // In the non-vector case lengthToExamine is the total length.
            // In the vector case lengthToExamine first aligns to Vector,
            // then in a second pass after the Vector lengths is the
            // remaining data that is shorter than a Vector length.
            while (lengthToExamine >= 4)
            {
                ref char current = ref Unsafe.Add(ref searchSpace, offset);

                if (value == current)
                    goto Found;
                if (value == Unsafe.Add(ref current, 1))
                    goto Found1;
                if (value == Unsafe.Add(ref current, 2))
                    goto Found2;
                if (value == Unsafe.Add(ref current, 3))
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                if (value == Unsafe.Add(ref searchSpace, offset))
                    goto Found;

                offset++;
                lengthToExamine--;
            }

            // We get past SequentialScan only if IsHardwareAccelerated or intrinsic .IsSupported is true. However, we still have the redundant check to allow
            // the JIT to see that the code is unreachable and eliminate it when the platform does not have hardware accelerated.
            if (Avx2.IsSupported)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector128<ushort>.Count);
                    if (((nint)Unsafe.AsPointer(ref Unsafe.Add(ref searchSpace, (nint)offset)) & (nint)(Vector256<byte>.Count - 1)) != 0)
                    {
                        // Not currently aligned to Vector256 (is aligned to Vector128); this can cause a problem for searches
                        // with no upper bound e.g. String.wcslen. Start with a check on Vector128 to align to Vector256,
                        // before moving to processing Vector256.

                        // If the input searchSpan has been fixed or pinned, this ensures we do not fault across memory pages
                        // while searching for an end of string. Specifically that this assumes that the length is either correct
                        // or that the data is pinned otherwise it may cause an AccessViolation from crossing a page boundary into an
                        // unowned page. If the search is unbounded (e.g. null terminator in wcslen) and the search value is not found,
                        // again this will likely cause an AccessViolation. However, correctly bounded searches will return -1 rather
                        // than ever causing an AV.

                        // If the searchSpan has not been fixed or pinned the GC can relocate it during the execution of this
                        // method, so the alignment only acts as best endeavour. The GC cost is likely to dominate over
                        // the misalignment that may occur after; to we default to giving the GC a free hand to relocate and
                        // its up to the caller whether they are operating over fixed data.
                        Vector128<ushort> values = Vector128.Create((ushort)value);
                        Vector128<ushort> search = LoadVector128(ref searchSpace, offset);

                        // Same method as below
                        int matches = Sse2.MoveMask(Sse2.CompareEqual(values, search).AsByte());
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
                        Vector256<ushort> values = Vector256.Create((ushort)value);
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector256<ushort>.Count);

                            Vector256<ushort> search = LoadVector256(ref searchSpace, offset);
                            int matches = Avx2.MoveMask(Avx2.CompareEqual(values, search).AsByte());
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

                        Vector128<ushort> values = Vector128.Create((ushort)value);
                        Vector128<ushort> search = LoadVector128(ref searchSpace, offset);

                        // Same method as above
                        int matches = Sse2.MoveMask(Sse2.CompareEqual(values, search).AsByte());
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
            else if (Sse2.IsSupported)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector128<ushort>.Count);

                    lengthToExamine = GetCharVector128SpanLength(offset, length);
                    if (lengthToExamine > 0)
                    {
                        Vector128<ushort> values = Vector128.Create((ushort)value);
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector128<ushort>.Count);

                            Vector128<ushort> search = LoadVector128(ref searchSpace, offset);

                            // Same method as above
                            int matches = Sse2.MoveMask(Sse2.CompareEqual(values, search).AsByte());
                            if (matches == 0)
                            {
                                // Zero flags set so no matches
                                offset += Vector128<ushort>.Count;
                                lengthToExamine -= Vector128<ushort>.Count;
                                continue;
                            }

                            // Find bitflag offset of first match and add to current offset,
                            // flags are in bytes so divide for chars
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
            else if (AdvSimd.Arm64.IsSupported)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector128<ushort>.Count);

                    lengthToExamine = GetCharVector128SpanLength(offset, length);
                    if (lengthToExamine > 0)
                    {
                        Vector128<ushort> values = Vector128.Create((ushort)value);
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector128<ushort>.Count);

                            Vector128<ushort> search = LoadVector128(ref searchSpace, offset);
                            Vector128<ushort> compareResult = AdvSimd.CompareEqual(values, search);

                            if (compareResult == Vector128<ushort>.Zero)
                            {
                                offset += Vector128<ushort>.Count;
                                lengthToExamine -= Vector128<ushort>.Count;
                                continue;
                            }

                            return (int)(offset + FindFirstMatchedLane(compareResult));
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
                        Vector<ushort> values = new Vector<ushort>((ushort)value);
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector<ushort>.Count);

                            var matches = Vector.Equals(values, LoadVector(ref searchSpace, offset));
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
            return -1;
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)(offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe int IndexOfAny(ref char searchStart, char value0, char value1, int length)
        {
            Debug.Assert(length >= 0);

            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Sse2.IsSupported)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector128<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // >= Sse2 intrinsics are supported and length is enough to use them, so use that path.
                    // We jump forward to the intrinsics at the end of them method so a naive branch predict
                    // will choose the non-intrinsic path so short lengths which don't gain anything aren't
                    // overly disadvantaged by having to jump over a lot of code. Whereas the longer lengths
                    // more than make this back from the intrinsics.
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // Similar as above for Vector version
                    lengthToExamine = (nuint)vectorDiff;
                    goto VectorCompare;
                }
            }

            int lookUp;
            while (lengthToExamine >= 4)
            {
                ref char current = ref Add(ref searchStart, offset);

                lookUp = current;
                if (value0 == lookUp || value1 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref current, 1);
                if (value0 == lookUp || value1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref current, 2);
                if (value0 == lookUp || value1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref current, 3);
                if (value0 == lookUp || value1 == lookUp)
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = Add(ref searchStart, offset);
                if (value0 == lookUp || value1 == lookUp)
                    goto Found;

                offset += 1;
                lengthToExamine -= 1;
            }

        NotFound:
            return -1;
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)offset;

        IntrinsicsCompare:
            // When we move into a Vectorized block, we process everything of Vector size;
            // and then for any remainder we do a final compare of Vector size but starting at
            // the end and forwards, which may overlap on an earlier compare.

            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (Sse2.IsSupported)
            {
                int matches;
                if (Avx2.IsSupported)
                {
                    Vector256<ushort> search;
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<ushort>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<ushort>.Count
                    if (lengthToExamine >= (nuint)Vector128<ushort>.Count)
                    {
                        Vector256<ushort> values0 = Vector256.Create((ushort)value0);
                        Vector256<ushort> values1 = Vector256.Create((ushort)value1);

                        // Subtract Vector128<ushort>.Count so we have now subtracted Vector256<ushort>.Count
                        lengthToExamine -= (nuint)Vector128<ushort>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            search = LoadVector256(ref searchStart, offset);
                            // Bitwise Or to combine the flagged matches for the second value to our match flags
                            matches = Avx2.MoveMask(
                                            Avx2.Or(
                                                Avx2.CompareEqual(values0, search),
                                                Avx2.CompareEqual(values1, search))
                                            .AsByte());
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // None matched
                                offset += (nuint)Vector256<ushort>.Count;
                                continue;
                            }

                            goto IntrinsicsMatch;
                        }

                        // Move to Vector length from end for final compare
                        search = LoadVector256(ref searchStart, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Avx2.MoveMask(
                                    Avx2.Or(
                                        Avx2.CompareEqual(values0, search),
                                        Avx2.CompareEqual(values1, search))
                                    .AsByte());
                        if (matches == 0)
                        {
                            // None matched
                            goto NotFound;
                        }

                        goto IntrinsicsMatch;
                    }
                }

                // Initial size check was done on method entry.
                Debug.Assert(length >= Vector128<ushort>.Count);
                {
                    Vector128<ushort> search;
                    Vector128<ushort> values0 = Vector128.Create((ushort)value0);
                    Vector128<ushort> values1 = Vector128.Create((ushort)value1);
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        search = LoadVector128(ref searchStart, offset);

                        matches = Sse2.MoveMask(
                            Sse2.Or(
                                Sse2.CompareEqual(values0, search),
                                Sse2.CompareEqual(values1, search))
                            .AsByte());
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // None matched
                            offset += (nuint)Vector128<ushort>.Count;
                            continue;
                        }

                        goto IntrinsicsMatch;
                    }
                    // Move to Vector length from end for final compare
                    search = LoadVector128(ref searchStart, lengthToExamine);
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Sse2.MoveMask(
                        Sse2.Or(
                            Sse2.CompareEqual(values0, search),
                            Sse2.CompareEqual(values1, search))
                        .AsByte());
                    if (matches == 0)
                    {
                        // None matched
                        goto NotFound;
                    }
                }

            IntrinsicsMatch:
                // Find bitflag offset of first difference and add to current offset,
                // flags are in bytes so divide by 2 for chars (shift right by 1)
                offset += (nuint)(uint)BitOperations.TrailingZeroCount(matches) >> 1;
                goto Found;
            }

        VectorCompare:
            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
            {
                Vector<ushort> values0 = new Vector<ushort>(value0);
                Vector<ushort> values1 = new Vector<ushort>(value1);

                Vector<ushort> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchStart, offset);
                    search = Vector.BitwiseOr(
                                    Vector.Equals(search, values0),
                                    Vector.Equals(search, values1));
                    if (Vector<ushort>.Zero.Equals(search))
                    {
                        // None matched
                        offset += (nuint)Vector<ushort>.Count;
                        continue;
                    }

                    goto VectorMatch;
                }

                // Move to Vector length from end for final compare
                search = LoadVector(ref searchStart, lengthToExamine);
                offset = lengthToExamine;
                search = Vector.BitwiseOr(
                                Vector.Equals(search, values0),
                                Vector.Equals(search, values1));
                if (Vector<ushort>.Zero.Equals(search))
                {
                    // None matched
                    goto NotFound;
                }

            VectorMatch:
                offset += (nuint)(uint)LocateFirstFoundChar(search);
                goto Found;
            }

            Debug.Fail("Unreachable");
            goto NotFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe int IndexOfAny(ref char searchStart, char value0, char value1, char value2, int length)
        {
            Debug.Assert(length >= 0);

            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Sse2.IsSupported)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector128<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // >= Sse2 intrinsics are supported and length is enough to use them, so use that path.
                    // We jump forward to the intrinsics at the end of them method so a naive branch predict
                    // will choose the non-intrinsic path so short lengths which don't gain anything aren't
                    // overly disadvantaged by having to jump over a lot of code. Whereas the longer lengths
                    // more than make this back from the intrinsics.
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // Similar as above for Vector version
                    lengthToExamine = (nuint)vectorDiff;
                    goto VectorCompare;
                }
            }

            int lookUp;
            while (lengthToExamine >= 4)
            {
                ref char current = ref Add(ref searchStart, offset);

                lookUp = current;
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref current, 1);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref current, 2);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref current, 3);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = Add(ref searchStart, offset);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found;

                offset += 1;
                lengthToExamine -= 1;
            }

        NotFound:
            return -1;
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)offset;

        IntrinsicsCompare:
            // When we move into a Vectorized block, we process everything of Vector size;
            // and then for any remainder we do a final compare of Vector size but starting at
            // the end and forwards, which may overlap on an earlier compare.

            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (Sse2.IsSupported)
            {
                int matches;
                if (Avx2.IsSupported)
                {
                    Vector256<ushort> search;
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<ushort>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<ushort>.Count
                    if (lengthToExamine >= (nuint)Vector128<ushort>.Count)
                    {
                        Vector256<ushort> values0 = Vector256.Create((ushort)value0);
                        Vector256<ushort> values1 = Vector256.Create((ushort)value1);
                        Vector256<ushort> values2 = Vector256.Create((ushort)value2);

                        // Subtract Vector128<ushort>.Count so we have now subtracted Vector256<ushort>.Count
                        lengthToExamine -= (nuint)Vector128<ushort>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            search = LoadVector256(ref searchStart, offset);
                            // Bitwise Or to combine the flagged matches for the second value to our match flags
                            matches = Avx2.MoveMask(
                                            Avx2.Or(
                                                Avx2.Or(
                                                    Avx2.CompareEqual(values0, search),
                                                    Avx2.CompareEqual(values1, search)),
                                                Avx2.CompareEqual(values2, search))
                                            .AsByte());
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // None matched
                                offset += (nuint)Vector256<ushort>.Count;
                                continue;
                            }

                            goto IntrinsicsMatch;
                        }

                        // Move to Vector length from end for final compare
                        search = LoadVector256(ref searchStart, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Avx2.MoveMask(
                                    Avx2.Or(
                                        Avx2.Or(
                                            Avx2.CompareEqual(values0, search),
                                            Avx2.CompareEqual(values1, search)),
                                        Avx2.CompareEqual(values2, search))
                                    .AsByte());
                        if (matches == 0)
                        {
                            // None matched
                            goto NotFound;
                        }

                        goto IntrinsicsMatch;
                    }
                }

                // Initial size check was done on method entry.
                Debug.Assert(length >= Vector128<ushort>.Count);
                {
                    Vector128<ushort> search;
                    Vector128<ushort> values0 = Vector128.Create((ushort)value0);
                    Vector128<ushort> values1 = Vector128.Create((ushort)value1);
                    Vector128<ushort> values2 = Vector128.Create((ushort)value2);
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        search = LoadVector128(ref searchStart, offset);

                        matches = Sse2.MoveMask(
                                    Sse2.Or(
                                        Sse2.Or(
                                            Sse2.CompareEqual(values0, search),
                                            Sse2.CompareEqual(values1, search)),
                                        Sse2.CompareEqual(values2, search))
                                    .AsByte());
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // None matched
                            offset += (nuint)Vector128<ushort>.Count;
                            continue;
                        }

                        goto IntrinsicsMatch;
                    }
                    // Move to Vector length from end for final compare
                    search = LoadVector128(ref searchStart, lengthToExamine);
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Sse2.MoveMask(
                                    Sse2.Or(
                                        Sse2.Or(
                                            Sse2.CompareEqual(values0, search),
                                            Sse2.CompareEqual(values1, search)),
                                        Sse2.CompareEqual(values2, search))
                                    .AsByte());
                    if (matches == 0)
                    {
                        // None matched
                        goto NotFound;
                    }
                }

            IntrinsicsMatch:
                // Find bitflag offset of first difference and add to current offset,
                // flags are in bytes so divide by 2 for chars (shift right by 1)
                offset += (nuint)(uint)BitOperations.TrailingZeroCount(matches) >> 1;
                goto Found;
            }

        VectorCompare:
            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
            {
                Vector<ushort> values0 = new Vector<ushort>(value0);
                Vector<ushort> values1 = new Vector<ushort>(value1);
                Vector<ushort> values2 = new Vector<ushort>(value2);

                Vector<ushort> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchStart, offset);
                    search = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(search, values0),
                                        Vector.Equals(search, values1)),
                                    Vector.Equals(search, values2));
                    if (Vector<ushort>.Zero.Equals(search))
                    {
                        // None matched
                        offset += (nuint)Vector<ushort>.Count;
                        continue;
                    }

                    goto VectorMatch;
                }

                // Move to Vector length from end for final compare
                search = LoadVector(ref searchStart, lengthToExamine);
                offset = lengthToExamine;
                search = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(search, values0),
                                        Vector.Equals(search, values1)),
                                    Vector.Equals(search, values2));
                if (Vector<ushort>.Zero.Equals(search))
                {
                    // None matched
                    goto NotFound;
                }

            VectorMatch:
                offset += (nuint)(uint)LocateFirstFoundChar(search);
                goto Found;
            }

            Debug.Fail("Unreachable");
            goto NotFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe int IndexOfAny(ref char searchStart, char value0, char value1, char value2, char value3, int length)
        {
            Debug.Assert(length >= 0);

            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Sse2.IsSupported)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector128<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // >= Sse2 intrinsics are supported and length is enough to use them, so use that path.
                    // We jump forward to the intrinsics at the end of them method so a naive branch predict
                    // will choose the non-intrinsic path so short lengths which don't gain anything aren't
                    // overly disadvantaged by having to jump over a lot of code. Whereas the longer lengths
                    // more than make this back from the intrinsics.
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // Similar as above for Vector version
                    lengthToExamine = (nuint)vectorDiff;
                    goto VectorCompare;
                }
            }

            int lookUp;
            while (lengthToExamine >= 4)
            {
                ref char current = ref Add(ref searchStart, offset);

                lookUp = current;
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref current, 1);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref current, 2);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref current, 3);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = Add(ref searchStart, offset);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found;

                offset += 1;
                lengthToExamine -= 1;
            }

        NotFound:
            return -1;
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)offset;

        IntrinsicsCompare:
            // When we move into a Vectorized block, we process everything of Vector size;
            // and then for any remainder we do a final compare of Vector size but starting at
            // the end and forwards, which may overlap on an earlier compare.

            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (Sse2.IsSupported)
            {
                int matches;
                if (Avx2.IsSupported)
                {
                    Vector256<ushort> search;
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<ushort>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<ushort>.Count
                    if (lengthToExamine >= (nuint)Vector128<ushort>.Count)
                    {
                        Vector256<ushort> values0 = Vector256.Create((ushort)value0);
                        Vector256<ushort> values1 = Vector256.Create((ushort)value1);
                        Vector256<ushort> values2 = Vector256.Create((ushort)value2);
                        Vector256<ushort> values3 = Vector256.Create((ushort)value3);

                        // Subtract Vector128<ushort>.Count so we have now subtracted Vector256<ushort>.Count
                        lengthToExamine -= (nuint)Vector128<ushort>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            search = LoadVector256(ref searchStart, offset);
                            // We preform the Or at non-Vector level as we are using the maximum number of non-preserved registers,
                            // and more causes them first to be pushed to stack and then popped on exit to preseve their values.
                            matches = Avx2.MoveMask(Avx2.CompareEqual(values0, search).AsByte());
                            // Bitwise Or to combine the flagged matches for the second, third and fourth values to our match flags
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values1, search).AsByte());
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values2, search).AsByte());
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values3, search).AsByte());
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // None matched
                                offset += (nuint)Vector256<ushort>.Count;
                                continue;
                            }

                            goto IntrinsicsMatch;
                        }

                        // Move to Vector length from end for final compare
                        search = LoadVector256(ref searchStart, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Avx2.MoveMask(Avx2.CompareEqual(values0, search).AsByte());
                        // Bitwise Or to combine the flagged matches for the second, third and fourth values to our match flags
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values1, search).AsByte());
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values2, search).AsByte());
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values3, search).AsByte());
                        if (matches == 0)
                        {
                            // None matched
                            goto NotFound;
                        }

                        goto IntrinsicsMatch;
                    }
                }

                // Initial size check was done on method entry.
                Debug.Assert(length >= Vector128<ushort>.Count);
                {
                    Vector128<ushort> search;
                    Vector128<ushort> values0 = Vector128.Create((ushort)value0);
                    Vector128<ushort> values1 = Vector128.Create((ushort)value1);
                    Vector128<ushort> values2 = Vector128.Create((ushort)value2);
                    Vector128<ushort> values3 = Vector128.Create((ushort)value3);
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        search = LoadVector128(ref searchStart, offset);

                        matches = Sse2.MoveMask(Sse2.CompareEqual(values0, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values1, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values2, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values3, search).AsByte());
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // None matched
                            offset += (nuint)Vector128<ushort>.Count;
                            continue;
                        }

                        goto IntrinsicsMatch;
                    }
                    // Move to Vector length from end for final compare
                    search = LoadVector128(ref searchStart, lengthToExamine);
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Sse2.MoveMask(Sse2.CompareEqual(values0, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values1, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values2, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values3, search).AsByte());
                    if (matches == 0)
                    {
                        // None matched
                        goto NotFound;
                    }
                }

            IntrinsicsMatch:
                // Find bitflag offset of first difference and add to current offset,
                // flags are in bytes so divide by 2 for chars (shift right by 1)
                offset += (nuint)(uint)BitOperations.TrailingZeroCount(matches) >> 1;
                goto Found;
            }

        VectorCompare:
            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
            {
                Vector<ushort> values0 = new Vector<ushort>(value0);
                Vector<ushort> values1 = new Vector<ushort>(value1);
                Vector<ushort> values2 = new Vector<ushort>(value2);
                Vector<ushort> values3 = new Vector<ushort>(value3);

                Vector<ushort> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchStart, offset);
                    search = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.BitwiseOr(
                                            Vector.Equals(search, values0),
                                            Vector.Equals(search, values1)),
                                        Vector.Equals(search, values2)),
                                    Vector.Equals(search, values3));
                    if (Vector<ushort>.Zero.Equals(search))
                    {
                        // None matched
                        offset += (nuint)Vector<ushort>.Count;
                        continue;
                    }

                    goto VectorMatch;
                }

                // Move to Vector length from end for final compare
                search = LoadVector(ref searchStart, lengthToExamine);
                offset = lengthToExamine;
                search = Vector.BitwiseOr(
                                Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(search, values0),
                                        Vector.Equals(search, values1)),
                                    Vector.Equals(search, values2)),
                                Vector.Equals(search, values3));
                if (Vector<ushort>.Zero.Equals(search))
                {
                    // None matched
                    goto NotFound;
                }

            VectorMatch:
                offset += (nuint)(uint)LocateFirstFoundChar(search);
                goto Found;
            }

            Debug.Fail("Unreachable");
            goto NotFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe int IndexOfAny(ref char searchStart, char value0, char value1, char value2, char value3, char value4, int length)
        {
            Debug.Assert(length >= 0);

            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Sse2.IsSupported)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector128<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // >= Sse2 intrinsics are supported and length is enough to use them, so use that path.
                    // We jump forward to the intrinsics at the end of them method so a naive branch predict
                    // will choose the non-intrinsic path so short lengths which don't gain anything aren't
                    // overly disadvantaged by having to jump over a lot of code. Whereas the longer lengths
                    // more than make this back from the intrinsics.
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // Similar as above for Vector version
                    lengthToExamine = (nuint)vectorDiff;
                    goto VectorCompare;
                }
            }

            int lookUp;
            while (lengthToExamine >= 4)
            {
                ref char current = ref Add(ref searchStart, offset);

                lookUp = current;
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp || value4 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref current, 1);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp || value4 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref current, 2);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp || value4 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref current, 3);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp || value4 == lookUp)
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = Add(ref searchStart, offset);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp || value4 == lookUp)
                    goto Found;

                offset += 1;
                lengthToExamine -= 1;
            }

        NotFound:
            return -1;
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)offset;

        IntrinsicsCompare:
            // When we move into a Vectorized block, we process everything of Vector size;
            // and then for any remainder we do a final compare of Vector size but starting at
            // the end and forwards, which may overlap on an earlier compare.

            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (Sse2.IsSupported)
            {
                int matches;
                if (Avx2.IsSupported)
                {
                    Vector256<ushort> search;
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<ushort>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<ushort>.Count
                    if (lengthToExamine >= (nuint)Vector128<ushort>.Count)
                    {
                        Vector256<ushort> values0 = Vector256.Create((ushort)value0);
                        Vector256<ushort> values1 = Vector256.Create((ushort)value1);
                        Vector256<ushort> values2 = Vector256.Create((ushort)value2);
                        Vector256<ushort> values3 = Vector256.Create((ushort)value3);
                        Vector256<ushort> values4 = Vector256.Create((ushort)value4);

                        // Subtract Vector128<ushort>.Count so we have now subtracted Vector256<ushort>.Count
                        lengthToExamine -= (nuint)Vector128<ushort>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            search = LoadVector256(ref searchStart, offset);
                            // We preform the Or at non-Vector level as we are using the maximum number of non-preserved registers (+ 1),
                            // and more causes them first to be pushed to stack and then popped on exit to preseve their values.
                            matches = Avx2.MoveMask(Avx2.CompareEqual(values0, search).AsByte());
                            // Bitwise Or to combine the flagged matches for the second, third and fourth values to our match flags
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values1, search).AsByte());
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values2, search).AsByte());
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values3, search).AsByte());
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values4, search).AsByte());
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // None matched
                                offset += (nuint)Vector256<ushort>.Count;
                                continue;
                            }

                            goto IntrinsicsMatch;
                        }

                        // Move to Vector length from end for final compare
                        search = LoadVector256(ref searchStart, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Avx2.MoveMask(Avx2.CompareEqual(values0, search).AsByte());
                        // Bitwise Or to combine the flagged matches for the second, third and fourth values to our match flags
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values1, search).AsByte());
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values2, search).AsByte());
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values3, search).AsByte());
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values4, search).AsByte());
                        if (matches == 0)
                        {
                            // None matched
                            goto NotFound;
                        }

                        goto IntrinsicsMatch;
                    }
                }

                // Initial size check was done on method entry.
                Debug.Assert(length >= Vector128<ushort>.Count);
                {
                    Vector128<ushort> search;
                    Vector128<ushort> values0 = Vector128.Create((ushort)value0);
                    Vector128<ushort> values1 = Vector128.Create((ushort)value1);
                    Vector128<ushort> values2 = Vector128.Create((ushort)value2);
                    Vector128<ushort> values3 = Vector128.Create((ushort)value3);
                    Vector128<ushort> values4 = Vector128.Create((ushort)value4);
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        search = LoadVector128(ref searchStart, offset);

                        matches = Sse2.MoveMask(Sse2.CompareEqual(values0, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values1, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values2, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values3, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values4, search).AsByte());
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // None matched
                            offset += (nuint)Vector128<ushort>.Count;
                            continue;
                        }

                        goto IntrinsicsMatch;
                    }
                    // Move to Vector length from end for final compare
                    search = LoadVector128(ref searchStart, lengthToExamine);
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Sse2.MoveMask(Sse2.CompareEqual(values0, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values1, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values2, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values3, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values4, search).AsByte());
                    if (matches == 0)
                    {
                        // None matched
                        goto NotFound;
                    }
                }

            IntrinsicsMatch:
                // Find bitflag offset of first difference and add to current offset,
                // flags are in bytes so divide by 2 for chars (shift right by 1)
                offset += (nuint)(uint)BitOperations.TrailingZeroCount(matches) >> 1;
                goto Found;
            }

        VectorCompare:
            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
            {
                Vector<ushort> values0 = new Vector<ushort>(value0);
                Vector<ushort> values1 = new Vector<ushort>(value1);
                Vector<ushort> values2 = new Vector<ushort>(value2);
                Vector<ushort> values3 = new Vector<ushort>(value3);
                Vector<ushort> values4 = new Vector<ushort>(value4);

                Vector<ushort> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchStart, offset);
                    search = Vector.BitwiseOr(
                                Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.BitwiseOr(
                                            Vector.Equals(search, values0),
                                            Vector.Equals(search, values1)),
                                        Vector.Equals(search, values2)),
                                    Vector.Equals(search, values3)),
                                Vector.Equals(search, values4));
                    if (Vector<ushort>.Zero.Equals(search))
                    {
                        // None matched
                        offset += (nuint)Vector<ushort>.Count;
                        continue;
                    }

                    goto VectorMatch;
                }

                // Move to Vector length from end for final compare
                search = LoadVector(ref searchStart, lengthToExamine);
                offset = lengthToExamine;
                search = Vector.BitwiseOr(
                            Vector.BitwiseOr(
                                Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(search, values0),
                                        Vector.Equals(search, values1)),
                                    Vector.Equals(search, values2)),
                                Vector.Equals(search, values3)),
                            Vector.Equals(search, values4));
                if (Vector<ushort>.Zero.Equals(search))
                {
                    // None matched
                    goto NotFound;
                }

            VectorMatch:
                offset += (nuint)(uint)LocateFirstFoundChar(search);
                goto Found;
            }

            Debug.Fail("Unreachable");
            goto NotFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe int LastIndexOf(ref char searchSpace, char value, int length)
        {
            Debug.Assert(length >= 0);

            fixed (char* pChars = &searchSpace)
            {
                char* pCh = pChars + length;
                char* pEndCh = pChars;

                if (Vector.IsHardwareAccelerated && length >= Vector<ushort>.Count * 2)
                {
                    // Figure out how many characters to read sequentially from the end until we are vector aligned
                    // This is equivalent to: length = ((int)pCh % Unsafe.SizeOf<Vector<ushort>>()) / elementsPerByte
                    const int elementsPerByte = sizeof(ushort) / sizeof(byte);
                    length = ((int)pCh & (Unsafe.SizeOf<Vector<ushort>>() - 1)) / elementsPerByte;
                }

            SequentialScan:
                while (length >= 4)
                {
                    length -= 4;
                    pCh -= 4;

                    if (*(pCh + 3) == value)
                        goto Found3;
                    if (*(pCh + 2) == value)
                        goto Found2;
                    if (*(pCh + 1) == value)
                        goto Found1;
                    if (*pCh == value)
                        goto Found;
                }

                while (length > 0)
                {
                    length--;
                    pCh--;

                    if (*pCh == value)
                        goto Found;
                }

                // We get past SequentialScan only if IsHardwareAccelerated is true. However, we still have the redundant check to allow
                // the JIT to see that the code is unreachable and eliminate it when the platform does not have hardware accelerated.
                if (Vector.IsHardwareAccelerated && pCh > pEndCh)
                {
                    // Get the highest multiple of Vector<ushort>.Count that is within the search space.
                    // That will be how many times we iterate in the loop below.
                    // This is equivalent to: length = Vector<ushort>.Count * ((int)(pCh - pEndCh) / Vector<ushort>.Count)
                    length = (int)((pCh - pEndCh) & ~(Vector<ushort>.Count - 1));

                    // Get comparison Vector
                    Vector<ushort> vComparison = new Vector<ushort>(value);

                    while (length > 0)
                    {
                        char* pStart = pCh - Vector<ushort>.Count;
                        // Using Unsafe.Read instead of ReadUnaligned since the search space is pinned and pCh (and hence pSart) is always vector aligned
                        Debug.Assert(((int)pStart & (Unsafe.SizeOf<Vector<ushort>>() - 1)) == 0);
                        Vector<ushort> vMatches = Vector.Equals(vComparison, Unsafe.Read<Vector<ushort>>(pStart));
                        if (Vector<ushort>.Zero.Equals(vMatches))
                        {
                            pCh -= Vector<ushort>.Count;
                            length -= Vector<ushort>.Count;
                            continue;
                        }
                        // Find offset of last match
                        return (int)(pStart - pEndCh) + LocateLastFoundChar(vMatches);
                    }

                    if (pCh > pEndCh)
                    {
                        length = (int)(pCh - pEndCh);
                        goto SequentialScan;
                    }
                }

                return -1;
            Found:
                return (int)(pCh - pEndCh);
            Found1:
                return (int)(pCh - pEndCh) + 1;
            Found2:
                return (int)(pCh - pEndCh) + 2;
            Found3:
                return (int)(pCh - pEndCh) + 3;
            }
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

        // Vector sub-search adapted from https://github.com/aspnet/KestrelHttpServer/pull/1138
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateLastFoundChar(Vector<ushort> match)
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
            return i * 4 + LocateLastFoundChar(candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateLastFoundChar(ulong match)
            => BitOperations.Log2(match) >> 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<ushort> LoadVector(ref char start, nint offset)
            => Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<ushort> LoadVector(ref char start, nuint offset)
            => Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, (nint)offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ushort> LoadVector128(ref char start, nint offset)
            => Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ushort> LoadVector128(ref char start, nuint offset)
            => Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, (nint)offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ushort> LoadVector256(ref char start, nint offset)
            => Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ushort> LoadVector256(ref char start, nuint offset)
            => Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, (nint)offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref char Add(ref char start, nuint offset) => ref Unsafe.Add(ref start, (nint)offset);

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
        private static unsafe nint UnalignedCountVector(ref char searchSpace)
        {
            const int ElementsPerByte = sizeof(ushort) / sizeof(byte);
            // Figure out how many characters to read sequentially until we are vector aligned
            // This is equivalent to:
            //         unaligned = ((int)pCh % Unsafe.SizeOf<Vector<ushort>>()) / ElementsPerByte
            //         length = (Vector<ushort>.Count - unaligned) % Vector<ushort>.Count

            // This alignment is only valid if the GC does not relocate; so we use ReadUnaligned to get the data.
            // If a GC does occur and alignment is lost, the GC cost will outweigh any gains from alignment so it
            // isn't too important to pin to maintain the alignment.
            return (nint)(uint)(-(int)Unsafe.AsPointer(ref searchSpace) / ElementsPerByte) & (Vector<ushort>.Count - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe nint UnalignedCountVector128(ref char searchSpace)
        {
            const int ElementsPerByte = sizeof(ushort) / sizeof(byte);
            // This alignment is only valid if the GC does not relocate; so we use ReadUnaligned to get the data.
            // If a GC does occur and alignment is lost, the GC cost will outweigh any gains from alignment so it
            // isn't too important to pin to maintain the alignment.
            return (nint)(uint)(-(int)Unsafe.AsPointer(ref searchSpace) / ElementsPerByte) & (Vector128<ushort>.Count - 1);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindFirstMatchedLane(Vector128<ushort> compareResult)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);

            Vector128<byte> pairwiseSelectedLane = AdvSimd.Arm64.AddPairwise(compareResult.AsByte(), compareResult.AsByte());
            ulong selectedLanes = pairwiseSelectedLane.AsUInt64().ToScalar();

            // It should be handled by compareResult != Vector.Zero
            Debug.Assert(selectedLanes != 0);

            return BitOperations.TrailingZeroCount(selectedLanes) >> 3;
        }
    }
}
