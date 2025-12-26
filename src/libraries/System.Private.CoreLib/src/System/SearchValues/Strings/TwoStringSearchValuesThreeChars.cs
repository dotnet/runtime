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
    /// <summary>
    /// Specialized SearchValues implementation for exactly two strings.
    /// Uses the same approach as <see cref="SingleStringSearchValuesThreeChars{TValueLength, TCaseSensitivity}"/>
    /// but searches for both strings simultaneously by comparing anchor characters from both strings.
    /// </summary>
    /// <remarks>
    /// For each of the two strings, we pick 2 anchor characters (just like the single-string implementation picks 3).
    /// The inner loop compares vectors for each of these characters at their respective offsets.
    /// When we find a potential match, we verify which of the two strings actually matched.
    /// </remarks>
    internal sealed class TwoStringSearchValuesThreeChars<TCaseSensitivity> : StringSearchValuesBase
        where TCaseSensitivity : struct, ICaseSensitivity
    {
        private const ushort CaseConversionMask = unchecked((ushort)~0x20);

        private readonly string _value0;
        private readonly string _value1;
        private readonly nint _minusValueTailLength;

        // Anchor characters and their offsets for value0 (first character is at offset 0)
        private readonly nuint _v0Ch2ByteOffset;
        private readonly ushort _v0Ch1;
        private readonly ushort _v0Ch2;

        // Anchor characters and their offsets for value1 (first character is at offset 0)
        private readonly nuint _v1Ch2ByteOffset;
        private readonly ushort _v1Ch1;
        private readonly ushort _v1Ch2;

        private static bool IgnoreCase => typeof(TCaseSensitivity) != typeof(CaseSensitive);

        public TwoStringSearchValuesThreeChars(HashSet<string> uniqueValues, string value0, string value1, int v0Ch2Offset, int v1Ch2Offset)
            : base(uniqueValues)
        {
            Debug.Assert(value0.Length > 1);
            Debug.Assert(value1.Length > 1);
            Debug.Assert(value0.Length == value1.Length);
            Debug.Assert(v0Ch2Offset > 0);
            Debug.Assert(v1Ch2Offset > 0);

            _value0 = value0;
            _value1 = value1;

            // Since both values have the same length, we just need to account for that length
            // and the maximum ch2 offset for safe reads
            int valueLength = value0.Length;
            int maxCh2Offset = Math.Max(v0Ch2Offset, v1Ch2Offset);

            // We need to reserve space for reading at offset maxCh2Offset from the starting position,
            // and for verifying the full value (valueLength - 1 chars after the starting position)
            _minusValueTailLength = -Math.Max(valueLength - 1, maxCh2Offset);

            _v0Ch1 = value0[0];
            _v0Ch2 = value0[v0Ch2Offset];
            _v1Ch1 = value1[0];
            _v1Ch2 = value1[v1Ch2Offset];

            if (IgnoreCase)
            {
                Debug.Assert(char.IsAscii((char)_v0Ch1) && char.IsAscii((char)_v0Ch2));
                Debug.Assert(char.IsAscii((char)_v1Ch1) && char.IsAscii((char)_v1Ch2));

                _v0Ch1 &= CaseConversionMask;
                _v0Ch2 &= CaseConversionMask;
                _v1Ch1 &= CaseConversionMask;
                _v1Ch2 &= CaseConversionMask;
            }

            _v0Ch2ByteOffset = (nuint)v0Ch2Offset * sizeof(char);
            _v1Ch2ByteOffset = (nuint)v1Ch2Offset * sizeof(char);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyMultiString(ReadOnlySpan<char> span) =>
            IndexOf(ref MemoryMarshal.GetReference(span), span.Length);

        private int IndexOf(ref char searchSpace, int searchSpaceLength)
        {
            ref char searchSpaceStart = ref searchSpace;

            // Calculate how many positions we can safely search (accounting for max offset needed)
            nint searchSpaceMinusValueTailLength = searchSpaceLength + _minusValueTailLength;

            if (!Vector128.IsHardwareAccelerated || searchSpaceMinusValueTailLength < Vector128<ushort>.Count)
            {
                goto ShortInput;
            }

            nuint v0Ch2ByteOffset = _v0Ch2ByteOffset;
            nuint v1Ch2ByteOffset = _v1Ch2ByteOffset;

            if (Vector512.IsHardwareAccelerated && searchSpaceMinusValueTailLength - Vector512<ushort>.Count >= 0)
            {
                Vector512<ushort> v0Ch1Vec = Vector512.Create(_v0Ch1);
                Vector512<ushort> v0Ch2Vec = Vector512.Create(_v0Ch2);
                Vector512<ushort> v1Ch1Vec = Vector512.Create(_v1Ch1);
                Vector512<ushort> v1Ch2Vec = Vector512.Create(_v1Ch2);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector512<ushort>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<ushort>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<ushort>.Count + (int)(v0Ch2ByteOffset / sizeof(char)));
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<ushort>.Count + (int)(v1Ch2ByteOffset / sizeof(char)));

                    Vector512<byte> result = GetComparisonResult(
                        ref searchSpace, v0Ch2ByteOffset, v1Ch2ByteOffset,
                        v0Ch1Vec, v0Ch2Vec, v1Ch1Vec, v1Ch2Vec);

                    if (result != Vector512<byte>.Zero)
                    {
                        goto CandidateFound512;
                    }

                LoopFooter512:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector512<ushort>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector512<ushort>.Count)))
                        {
                            return -1;
                        }

                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound512:
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, result.ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter512;
                }
            }
            else if (Vector256.IsHardwareAccelerated && searchSpaceMinusValueTailLength - Vector256<ushort>.Count >= 0)
            {
                Vector256<ushort> v0Ch1Vec = Vector256.Create(_v0Ch1);
                Vector256<ushort> v0Ch2Vec = Vector256.Create(_v0Ch2);
                Vector256<ushort> v1Ch1Vec = Vector256.Create(_v1Ch1);
                Vector256<ushort> v1Ch2Vec = Vector256.Create(_v1Ch2);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector256<ushort>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<ushort>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<ushort>.Count + (int)(v0Ch2ByteOffset / sizeof(char)));
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<ushort>.Count + (int)(v1Ch2ByteOffset / sizeof(char)));

                    Vector256<byte> result = GetComparisonResult(
                        ref searchSpace, v0Ch2ByteOffset, v1Ch2ByteOffset,
                        v0Ch1Vec, v0Ch2Vec, v1Ch1Vec, v1Ch2Vec);

                    if (result != Vector256<byte>.Zero)
                    {
                        goto CandidateFound256;
                    }

                LoopFooter256:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector256<ushort>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector256<ushort>.Count)))
                        {
                            return -1;
                        }

                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound256:
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, result.ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter256;
                }
            }
            else
            {
                Vector128<ushort> v0Ch1Vec = Vector128.Create(_v0Ch1);
                Vector128<ushort> v0Ch2Vec = Vector128.Create(_v0Ch2);
                Vector128<ushort> v1Ch1Vec = Vector128.Create(_v1Ch1);
                Vector128<ushort> v1Ch2Vec = Vector128.Create(_v1Ch2);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector128<ushort>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<ushort>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<ushort>.Count + (int)(v0Ch2ByteOffset / sizeof(char)));
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<ushort>.Count + (int)(v1Ch2ByteOffset / sizeof(char)));

                    Vector128<byte> result = GetComparisonResult(
                        ref searchSpace, v0Ch2ByteOffset, v1Ch2ByteOffset,
                        v0Ch1Vec, v0Ch2Vec, v1Ch1Vec, v1Ch2Vec);

                    if (result != Vector128<byte>.Zero)
                    {
                        goto CandidateFound128;
                    }

                LoopFooter128:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector128<ushort>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector128<ushort>.Count)))
                        {
                            return -1;
                        }

                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound128:
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, result.ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter128;
                }
            }

        ShortInput:
            // Both values have the same length, iterate through all valid starting positions
            nint shortInputEnd = searchSpaceLength - _value0.Length + 1;
            for (nint i = 0; i < shortInputEnd; i++)
            {
                ref char cur = ref Unsafe.Add(ref searchSpace, i);

                if (StartsWith<TCaseSensitivity>(ref cur, searchSpaceLength - (int)i, _value0) ||
                    StartsWith<TCaseSensitivity>(ref cur, searchSpaceLength - (int)i, _value1))
                {
                    return (int)i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> GetComparisonResult(
            ref char searchSpace,
            nuint v0Ch2ByteOffset, nuint v1Ch2ByteOffset,
            Vector128<ushort> v0Ch1, Vector128<ushort> v0Ch2,
            Vector128<ushort> v1Ch1, Vector128<ushort> v1Ch2)
        {
            // Load input at the current position
            Vector128<ushort> input0 = Vector128.LoadUnsafe(ref searchSpace);

            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                // Check both strings' first character
                Vector128<ushort> cmpV0Ch1 = Vector128.Equals(v0Ch1, input0);
                Vector128<ushort> cmpV1Ch1 = Vector128.Equals(v1Ch1, input0);

                // Load and compare second characters at their respective offsets
                Vector128<ushort> cmpV0Ch2 = Vector128.Equals(v0Ch2, Vector128.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v0Ch2ByteOffset).AsUInt16());
                Vector128<ushort> cmpV1Ch2 = Vector128.Equals(v1Ch2, Vector128.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v1Ch2ByteOffset).AsUInt16());

                // A position matches if (value0's ch1 AND ch2 match) OR (value1's ch1 AND ch2 match)
                Vector128<ushort> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector128<ushort> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return (matchV0 | matchV1).AsByte();
            }
            else
            {
                Vector128<ushort> caseConversion = Vector128.Create(CaseConversionMask);
                Vector128<ushort> input0Masked = input0 & caseConversion;

                // Check both strings' first character
                Vector128<ushort> cmpV0Ch1 = Vector128.Equals(v0Ch1, input0Masked);
                Vector128<ushort> cmpV1Ch1 = Vector128.Equals(v1Ch1, input0Masked);

                // Load and compare second characters at their respective offsets
                Vector128<ushort> cmpV0Ch2 = Vector128.Equals(v0Ch2, Vector128.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v0Ch2ByteOffset).AsUInt16() & caseConversion);
                Vector128<ushort> cmpV1Ch2 = Vector128.Equals(v1Ch2, Vector128.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v1Ch2ByteOffset).AsUInt16() & caseConversion);

                // A position matches if (value0's ch1 AND ch2 match) OR (value1's ch1 AND ch2 match)
                Vector128<ushort> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector128<ushort> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return (matchV0 | matchV1).AsByte();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<byte> GetComparisonResult(
            ref char searchSpace,
            nuint v0Ch2ByteOffset, nuint v1Ch2ByteOffset,
            Vector256<ushort> v0Ch1, Vector256<ushort> v0Ch2,
            Vector256<ushort> v1Ch1, Vector256<ushort> v1Ch2)
        {
            Vector256<ushort> input0 = Vector256.LoadUnsafe(ref searchSpace);

            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector256<ushort> cmpV0Ch1 = Vector256.Equals(v0Ch1, input0);
                Vector256<ushort> cmpV1Ch1 = Vector256.Equals(v1Ch1, input0);
                Vector256<ushort> cmpV0Ch2 = Vector256.Equals(v0Ch2, Vector256.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v0Ch2ByteOffset).AsUInt16());
                Vector256<ushort> cmpV1Ch2 = Vector256.Equals(v1Ch2, Vector256.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v1Ch2ByteOffset).AsUInt16());

                Vector256<ushort> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector256<ushort> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return (matchV0 | matchV1).AsByte();
            }
            else
            {
                Vector256<ushort> caseConversion = Vector256.Create(CaseConversionMask);
                Vector256<ushort> input0Masked = input0 & caseConversion;

                Vector256<ushort> cmpV0Ch1 = Vector256.Equals(v0Ch1, input0Masked);
                Vector256<ushort> cmpV1Ch1 = Vector256.Equals(v1Ch1, input0Masked);
                Vector256<ushort> cmpV0Ch2 = Vector256.Equals(v0Ch2, Vector256.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v0Ch2ByteOffset).AsUInt16() & caseConversion);
                Vector256<ushort> cmpV1Ch2 = Vector256.Equals(v1Ch2, Vector256.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v1Ch2ByteOffset).AsUInt16() & caseConversion);

                Vector256<ushort> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector256<ushort> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return (matchV0 | matchV1).AsByte();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<byte> GetComparisonResult(
            ref char searchSpace,
            nuint v0Ch2ByteOffset, nuint v1Ch2ByteOffset,
            Vector512<ushort> v0Ch1, Vector512<ushort> v0Ch2,
            Vector512<ushort> v1Ch1, Vector512<ushort> v1Ch2)
        {
            Vector512<ushort> input0 = Vector512.LoadUnsafe(ref searchSpace);

            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector512<ushort> cmpV0Ch1 = Vector512.Equals(v0Ch1, input0);
                Vector512<ushort> cmpV1Ch1 = Vector512.Equals(v1Ch1, input0);
                Vector512<ushort> cmpV0Ch2 = Vector512.Equals(v0Ch2, Vector512.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v0Ch2ByteOffset).AsUInt16());
                Vector512<ushort> cmpV1Ch2 = Vector512.Equals(v1Ch2, Vector512.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v1Ch2ByteOffset).AsUInt16());

                Vector512<ushort> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector512<ushort> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return (matchV0 | matchV1).AsByte();
            }
            else
            {
                Vector512<ushort> caseConversion = Vector512.Create(CaseConversionMask);
                Vector512<ushort> input0Masked = input0 & caseConversion;

                Vector512<ushort> cmpV0Ch1 = Vector512.Equals(v0Ch1, input0Masked);
                Vector512<ushort> cmpV1Ch1 = Vector512.Equals(v1Ch1, input0Masked);
                Vector512<ushort> cmpV0Ch2 = Vector512.Equals(v0Ch2, Vector512.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v0Ch2ByteOffset).AsUInt16() & caseConversion);
                Vector512<ushort> cmpV1Ch2 = Vector512.Equals(v1Ch2, Vector512.LoadUnsafe(ref Unsafe.As<char, byte>(ref searchSpace), v1Ch2ByteOffset).AsUInt16() & caseConversion);

                Vector512<ushort> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector512<ushort> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return (matchV0 | matchV1).AsByte();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryMatch(ref char searchSpaceStart, int searchSpaceLength, ref char searchSpace, uint mask, out int offsetFromStart)
        {
            do
            {
                int bitPos = BitOperations.TrailingZeroCount(mask);
                Debug.Assert(bitPos % 2 == 0);

                ref char matchRef = ref Unsafe.AddByteOffset(ref searchSpace, bitPos);
                int lengthRemaining = searchSpaceLength - (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / sizeof(char));

                ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref matchRef, Math.Min(_value0.Length, lengthRemaining));

                // Check both values - return the one that matches
                if (StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, _value0) ||
                    StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, _value1))
                {
                    offsetFromStart = (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / sizeof(char));
                    return true;
                }

                // Reset 2 bits because each ushort match sets 2 consecutive bits in the byte mask
                mask = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(mask));
            }
            while (mask != 0);

            offsetFromStart = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryMatch(ref char searchSpaceStart, int searchSpaceLength, ref char searchSpace, ulong mask, out int offsetFromStart)
        {
            do
            {
                int bitPos = BitOperations.TrailingZeroCount(mask);
                Debug.Assert(bitPos % 2 == 0);

                ref char matchRef = ref Unsafe.AddByteOffset(ref searchSpace, bitPos);
                int lengthRemaining = searchSpaceLength - (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / sizeof(char));

                ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref matchRef, Math.Min(_value0.Length, lengthRemaining));

                // Check both values - return the one that matches
                if (StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, _value0) ||
                    StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, _value1))
                {
                    offsetFromStart = (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / sizeof(char));
                    return true;
                }

                // Reset 2 bits because each ushort match sets 2 consecutive bits in the byte mask
                mask = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(mask));
            }
            while (mask != 0);

            offsetFromStart = 0;
            return false;
        }
    }
}
