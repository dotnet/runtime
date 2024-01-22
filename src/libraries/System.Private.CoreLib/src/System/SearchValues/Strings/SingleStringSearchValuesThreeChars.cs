// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using static System.Buffers.StringSearchValuesHelper;

namespace System.Buffers
{
    // Based on SpanHelpers.IndexOf(ref char, int, ref char, int)
    // This implementation uses 3 precomputed anchor points when searching.
    // This implementation may also be used for length=2 values, in which case two anchors point at the same position.
    // Has an O(i * m) worst-case, with the expected time closer to O(n) for most inputs.
    internal sealed class SingleStringSearchValuesThreeChars<TValueLength, TCaseSensitivity> : StringSearchValuesBase
        where TValueLength : struct, IValueLength
        where TCaseSensitivity : struct, ICaseSensitivity
    {
        private const ushort CaseConversionMask = unchecked((ushort)~0x20);

        private readonly string _value;
        private readonly nint _minusValueTailLength;
        private readonly nuint _ch2ByteOffset;
        private readonly nuint _ch3ByteOffset;
        private readonly ushort _ch1;
        private readonly ushort _ch2;
        private readonly ushort _ch3;

        private static bool IgnoreCase => typeof(TCaseSensitivity) != typeof(CaseSensitive);

        public SingleStringSearchValuesThreeChars(HashSet<string>? uniqueValues, string value) : base(uniqueValues)
        {
            // We could have more than one entry in 'uniqueValues' if this value is an exact prefix of all the others.
            Debug.Assert(value.Length > 1);
            Debug.Assert((value.Length >= 8) == TValueLength.AtLeast8CharsOrUnknown);

            CharacterFrequencyHelper.GetSingleStringMultiCharacterOffsets(value, IgnoreCase, out int ch2Offset, out int ch3Offset);

            Debug.Assert(ch3Offset == 0 || ch3Offset > ch2Offset);

            _value = value;
            _minusValueTailLength = -(value.Length - 1);

            _ch1 = value[0];
            _ch2 = value[ch2Offset];
            _ch3 = value[ch3Offset];

            if (IgnoreCase)
            {
                _ch1 &= CaseConversionMask;
                _ch2 &= CaseConversionMask;
                _ch3 &= CaseConversionMask;
            }

            _ch2ByteOffset = (nuint)ch2Offset * 2;
            _ch3ByteOffset = (nuint)ch3Offset * 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyMultiString(ReadOnlySpan<char> span) =>
            IndexOf(ref MemoryMarshal.GetReference(span), span.Length);

        private int IndexOf(ref char searchSpace, int searchSpaceLength)
        {
            ref char searchSpaceStart = ref searchSpace;

            nint searchSpaceMinusValueTailLength = searchSpaceLength + _minusValueTailLength;

            if (!Vector128.IsHardwareAccelerated || searchSpaceMinusValueTailLength < Vector128<ushort>.Count)
            {
                goto ShortInput;
            }

            nuint ch2ByteOffset = _ch2ByteOffset;
            nuint ch3ByteOffset = _ch3ByteOffset;

            if (Vector512.IsHardwareAccelerated && searchSpaceMinusValueTailLength - Vector512<ushort>.Count >= 0)
            {
                Vector512<ushort> ch1 = Vector512.Create(_ch1);
                Vector512<ushort> ch2 = Vector512.Create(_ch2);
                Vector512<ushort> ch3 = Vector512.Create(_ch3);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector512<ushort>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<ushort>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<ushort>.Count + (int)(_ch2ByteOffset / 2));
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<ushort>.Count + (int)(_ch3ByteOffset / 2));

                    // Find which starting positions likely contain a match (likely match all 3 anchor characters).
                    Vector512<byte> result = GetComparisonResult(ref searchSpace, ch2ByteOffset, ch3ByteOffset, ch1, ch2, ch3);

                    if (result != Vector512<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    // We haven't found a match. Update the input position and check if we've reached the end.
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector512<ushort>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector512<ushort>.Count)))
                        {
                            return -1;
                        }

                        // We have fewer than 32 characters remaining. Adjust the input position such that we will do one last loop iteration.
                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound:
                    // We found potential matches, but they may be false-positives, so we must verify each one.
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, result.ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter;
                }
            }
            else if (Vector256.IsHardwareAccelerated && searchSpaceMinusValueTailLength - Vector256<ushort>.Count >= 0)
            {
                Vector256<ushort> ch1 = Vector256.Create(_ch1);
                Vector256<ushort> ch2 = Vector256.Create(_ch2);
                Vector256<ushort> ch3 = Vector256.Create(_ch3);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector256<ushort>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<ushort>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<ushort>.Count + (int)(_ch2ByteOffset / 2));
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<ushort>.Count + (int)(_ch3ByteOffset / 2));

                    // Find which starting positions likely contain a match (likely match all 3 anchor characters).
                    Vector256<byte> result = GetComparisonResult(ref searchSpace, ch2ByteOffset, ch3ByteOffset, ch1, ch2, ch3);

                    if (result != Vector256<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector256<ushort>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector256<ushort>.Count)))
                        {
                            return -1;
                        }

                        // We have fewer than 16 characters remaining. Adjust the input position such that we will do one last loop iteration.
                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound:
                    // We found potential matches, but they may be false-positives, so we must verify each one.
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, result.ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter;
                }
            }
            else
            {
                Vector128<ushort> ch1 = Vector128.Create(_ch1);
                Vector128<ushort> ch2 = Vector128.Create(_ch2);
                Vector128<ushort> ch3 = Vector128.Create(_ch3);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector128<ushort>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<ushort>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<ushort>.Count + (int)(_ch2ByteOffset / 2));
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<ushort>.Count + (int)(_ch3ByteOffset / 2));

                    // Find which starting positions likely contain a match (likely match all 3 anchor characters).
                    Vector128<byte> result = GetComparisonResult(ref searchSpace, ch2ByteOffset, ch3ByteOffset, ch1, ch2, ch3);

                    if (result != Vector128<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector128<ushort>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector128<ushort>.Count)))
                        {
                            return -1;
                        }

                        // We have fewer than 8 characters remaining. Adjust the input position such that we will do one last loop iteration.
                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound:
                    // We found potential matches, but they may be false-positives, so we must verify each one.
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, result.ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter;
                }
            }

        ShortInput:
            string value = _value;
            char valueHead = value.GetRawStringData();

            for (nint i = 0; i < searchSpaceMinusValueTailLength; i++)
            {
                ref char cur = ref Unsafe.Add(ref searchSpace, i);

                // CaseInsensitiveUnicode doesn't support single-character transformations, so we skip checking the first character first.
                if ((typeof(TCaseSensitivity) == typeof(CaseInsensitiveUnicode) || TCaseSensitivity.TransformInput(cur) == valueHead) &&
                    TCaseSensitivity.Equals<TValueLength>(ref cur, value))
                {
                    return (int)i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> GetComparisonResult(ref char searchSpace, nuint ch2ByteOffset, nuint ch3ByteOffset, Vector128<ushort> ch1, Vector128<ushort> ch2, Vector128<ushort> ch3)
        {
            // Load 3 vectors from the input.
            // One from the current search space, the other two at an offset based on the distance of those characters from the first one.
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector128<ushort> cmpCh1 = Vector128.Equals(ch1, Vector128.LoadUnsafe(ref searchSpace));
                Vector128<ushort> cmpCh2 = Vector128.Equals(ch2, Vector128.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch2ByteOffset).AsUInt16());
                Vector128<ushort> cmpCh3 = Vector128.Equals(ch3, Vector128.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch3ByteOffset).AsUInt16());
                // AND all 3 together to get a mask of possible match positions that match in at least 3 places.
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
            else
            {
                // For each, AND the value with ~0x20 so that letters are uppercased.
                // For characters that aren't ASCII letters, this may produce wrong results, but only false-positives.
                // We will take care of those in the verification step if the other characters also indicate a possible match.
                Vector128<ushort> caseConversion = Vector128.Create(CaseConversionMask);

                Vector128<ushort> cmpCh1 = Vector128.Equals(ch1, Vector128.LoadUnsafe(ref searchSpace) & caseConversion);
                Vector128<ushort> cmpCh2 = Vector128.Equals(ch2, Vector128.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch2ByteOffset).AsUInt16() & caseConversion);
                Vector128<ushort> cmpCh3 = Vector128.Equals(ch3, Vector128.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch3ByteOffset).AsUInt16() & caseConversion);
                // AND all 3 together to get a mask of possible match positions that likely match in at least 3 places.
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<byte> GetComparisonResult(ref char searchSpace, nuint ch2ByteOffset, nuint ch3ByteOffset, Vector256<ushort> ch1, Vector256<ushort> ch2, Vector256<ushort> ch3)
        {
            // See comments in 'GetComparisonResult' for Vector128<byte> above.
            // This method is the same, but operates on 32 input characters at a time.
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector256<ushort> cmpCh1 = Vector256.Equals(ch1, Vector256.LoadUnsafe(ref searchSpace));
                Vector256<ushort> cmpCh2 = Vector256.Equals(ch2, Vector256.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch2ByteOffset).AsUInt16());
                Vector256<ushort> cmpCh3 = Vector256.Equals(ch3, Vector256.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch3ByteOffset).AsUInt16());
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
            else
            {
                Vector256<ushort> caseConversion = Vector256.Create(CaseConversionMask);

                Vector256<ushort> cmpCh1 = Vector256.Equals(ch1, Vector256.LoadUnsafe(ref searchSpace) & caseConversion);
                Vector256<ushort> cmpCh2 = Vector256.Equals(ch2, Vector256.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch2ByteOffset).AsUInt16() & caseConversion);
                Vector256<ushort> cmpCh3 = Vector256.Equals(ch3, Vector256.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch3ByteOffset).AsUInt16() & caseConversion);
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<byte> GetComparisonResult(ref char searchSpace, nuint ch2ByteOffset, nuint ch3ByteOffset, Vector512<ushort> ch1, Vector512<ushort> ch2, Vector512<ushort> ch3)
        {
            // See comments in 'GetComparisonResult' for Vector128<byte> above.
            // This method is the same, but operates on 64 input characters at a time.
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector512<ushort> cmpCh1 = Vector512.Equals(ch1, Vector512.LoadUnsafe(ref searchSpace));
                Vector512<ushort> cmpCh2 = Vector512.Equals(ch2, Vector512.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch2ByteOffset).AsUInt16());
                Vector512<ushort> cmpCh3 = Vector512.Equals(ch3, Vector512.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch3ByteOffset).AsUInt16());
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
            else
            {
                Vector512<ushort> caseConversion = Vector512.Create(CaseConversionMask);

                Vector512<ushort> cmpCh1 = Vector512.Equals(ch1, Vector512.LoadUnsafe(ref searchSpace) & caseConversion);
                Vector512<ushort> cmpCh2 = Vector512.Equals(ch2, Vector512.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch2ByteOffset).AsUInt16() & caseConversion);
                Vector512<ushort> cmpCh3 = Vector512.Equals(ch3, Vector512.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), ch3ByteOffset).AsUInt16() & caseConversion);
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryMatch(ref char searchSpaceStart, int searchSpaceLength, ref char searchSpace, uint mask, out int offsetFromStart)
        {
            // 'mask' encodes the input positions where at least 3 characters likely matched.
            // Verify each one to see if we've found a match, otherwise return back to the vectorized loop.
            do
            {
                int bitPos = BitOperations.TrailingZeroCount(mask);
                Debug.Assert(bitPos % 2 == 0);

                ref char matchRef = ref Unsafe.AddByteOffset(ref searchSpace, bitPos);

                ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref matchRef, _value.Length);

                // If the value is short (!TValueLength.AtLeast4Chars => 2 or 3 characters), the anchors already represent the whole value.
                // With case-sensitive comparisons, we've therefore already confirmed the match, so we can skip doing so here.
                // With case-insensitive comparisons, we applied a mask to the input, so while the anchors likely matched, we can't be sure.
                if ((typeof(TCaseSensitivity) == typeof(CaseSensitive) && !TValueLength.AtLeast4Chars) ||
                    TCaseSensitivity.Equals<TValueLength>(ref matchRef, _value))
                {
                    offsetFromStart = (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / 2);
                    return true;
                }

                mask = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(mask));
            }
            while (mask != 0);

            offsetFromStart = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryMatch(ref char searchSpaceStart, int searchSpaceLength, ref char searchSpace, ulong mask, out int offsetFromStart)
        {
            // 'mask' encodes the input positions where at least 3 characters likely matched.
            // Verify each one to see if we've found a match, otherwise return back to the vectorized loop.
            do
            {
                int bitPos = BitOperations.TrailingZeroCount(mask);
                Debug.Assert(bitPos % 2 == 0);

                ref char matchRef = ref Unsafe.AddByteOffset(ref searchSpace, bitPos);

                ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref matchRef, _value.Length);

                // If the value is short (!TValueLength.AtLeast4Chars => 2 or 3 characters), the anchors already represent the whole value.
                // With case-sensitive comparisons, we've therefore already confirmed the match, so we can skip doing so here.
                // With case-insensitive comparisons, we applied a mask to the input, so while the anchors likely matched, we can't be sure.
                if ((typeof(TCaseSensitivity) == typeof(CaseSensitive) && !TValueLength.AtLeast4Chars) ||
                    TCaseSensitivity.Equals<TValueLength>(ref matchRef, _value))
                {
                    offsetFromStart = (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / 2);
                    return true;
                }

                mask = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(mask));
            }
            while (mask != 0);

            offsetFromStart = 0;
            return false;
        }


        internal override bool ContainsCore(string value) => HasUniqueValues
            ? base.ContainsCore(value)
            : _value.Equals(value, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        internal override string[] GetValues() => HasUniqueValues
            ? base.GetValues()
            : new string[] { _value };
    }
}
