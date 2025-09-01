// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using static System.Buffers.StringSearchValuesHelper;

namespace System.Buffers
{
    /// <summary>
    /// Same as <see cref="SingleStringSearchValuesThreeChars{TValueLength, TCaseSensitivity}"/>, but using packed comparisons similar to <see cref="PackedSpanHelpers"/>.
    /// </summary>
    internal sealed class SingleStringSearchValuesPackedThreeChars<TValueLength, TCaseSensitivity> : StringSearchValuesBase
        where TValueLength : struct, IValueLength
        where TCaseSensitivity : struct, ICaseSensitivity
    {
        private const byte CaseConversionMask = unchecked((byte)~0x20);

        private readonly SingleValueState _valueState;
        private readonly nint _minusValueTailLength;
        private readonly nuint _ch2ByteOffset;
        private readonly nuint _ch3ByteOffset;
        private readonly byte _ch1;
        private readonly byte _ch2;
        private readonly byte _ch3;

        private static bool IgnoreCase => typeof(TCaseSensitivity) != typeof(CaseSensitive);

        // If the value is short (ValueLengthLessThan4 => 2 or 3 characters), the anchors already represent the whole value.
        // With case-sensitive comparisons, we've therefore already confirmed the match, so we can skip doing so here.
        // With case-insensitive comparisons, we applied a mask to the input, so while the anchors likely matched, we can't be sure.
        // If the value is composed of only ASCII letters, masking the input can't produce false positives, so we can also skip the verification step.
        // We only do this when running on X86 and not ARM64, as the latter uses UnzipEven when packing inputs, which may produce false positive anchor matches.
        // We use that instead of ExtractNarrowingSaturate because it allows for higher searching throughput.
        private static bool CanSkipAnchorMatchVerification
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                Sse2.IsSupported &&
                typeof(TValueLength) == typeof(ValueLengthLessThan4) &&
                (typeof(TCaseSensitivity) == typeof(CaseSensitive) || typeof(TCaseSensitivity) == typeof(CaseInsensitiveAsciiLetters));
        }

        public SingleStringSearchValuesPackedThreeChars(HashSet<string>? uniqueValues, string value, int ch2Offset, int ch3Offset) : base(uniqueValues)
        {
            Debug.Assert(Sse2.IsSupported || AdvSimd.Arm64.IsSupported);

            // We could have more than one entry in 'uniqueValues' if this value is an exact prefix of all the others.
            Debug.Assert(value.Length > 1);
            Debug.Assert(ch3Offset == 0 || ch3Offset > ch2Offset);
            Debug.Assert(value[0] <= byte.MaxValue && value[ch2Offset] <= byte.MaxValue && value[ch3Offset] <= byte.MaxValue);

            _valueState = new SingleValueState(value, IgnoreCase);
            _minusValueTailLength = -(value.Length - 1);

            _ch1 = (byte)value[0];
            _ch2 = (byte)value[ch2Offset];
            _ch3 = (byte)value[ch3Offset];

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

            nuint ch2ByteOffset = _ch2ByteOffset;
            nuint ch3ByteOffset = _ch3ByteOffset;

            if (Vector512.IsHardwareAccelerated && Avx512BW.IsSupported && searchSpaceMinusValueTailLength - Vector512<byte>.Count >= 0)
            {
                Vector512<byte> ch1 = Vector512.Create(_ch1);
                Vector512<byte> ch2 = Vector512.Create(_ch2);
                Vector512<byte> ch3 = Vector512.Create(_ch3);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector512<byte>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<byte>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<byte>.Count + (int)(_ch2ByteOffset / sizeof(char)));
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<byte>.Count + (int)(_ch3ByteOffset / sizeof(char)));

                    // Find which starting positions likely contain a match (likely match all 3 anchor characters).
                    Vector512<byte> result = GetComparisonResult(ref searchSpace, ch2ByteOffset, ch3ByteOffset, ch1, ch2, ch3);

                    if (result != Vector512<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    // We haven't found a match. Update the input position and check if we've reached the end.
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector512<byte>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector512<byte>.Count)))
                        {
                            return -1;
                        }

                        // We have fewer than 64 characters remaining. Adjust the input position such that we will do one last loop iteration.
                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound:
                    // We found potential matches, but they may be false-positives, so we must verify each one.
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, PackedSpanHelpers.FixUpPackedVector512Result(result).ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter;
                }
            }
            else if (Vector256.IsHardwareAccelerated && Avx2.IsSupported && searchSpaceMinusValueTailLength - Vector256<byte>.Count >= 0)
            {
                Vector256<byte> ch1 = Vector256.Create(_ch1);
                Vector256<byte> ch2 = Vector256.Create(_ch2);
                Vector256<byte> ch3 = Vector256.Create(_ch3);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector256<byte>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<byte>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<byte>.Count + (int)(_ch2ByteOffset / sizeof(char)));
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<byte>.Count + (int)(_ch3ByteOffset / sizeof(char)));

                    // Find which starting positions likely contain a match (likely match all 3 anchor characters).
                    Vector256<byte> result = GetComparisonResult(ref searchSpace, ch2ByteOffset, ch3ByteOffset, ch1, ch2, ch3);

                    if (result != Vector256<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector256<byte>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector256<byte>.Count)))
                        {
                            return -1;
                        }

                        // We have fewer than 32 characters remaining. Adjust the input position such that we will do one last loop iteration.
                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound:
                    // We found potential matches, but they may be false-positives, so we must verify each one.
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, PackedSpanHelpers.FixUpPackedVector256Result(result).ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter;
                }
            }
            else if ((Sse2.IsSupported || AdvSimd.Arm64.IsSupported) && searchSpaceMinusValueTailLength - Vector128<byte>.Count >= 0)
            {
                Vector128<byte> ch1 = Vector128.Create(_ch1);
                Vector128<byte> ch2 = Vector128.Create(_ch2);
                Vector128<byte> ch3 = Vector128.Create(_ch3);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector128<byte>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<byte>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<byte>.Count + (int)(_ch2ByteOffset / sizeof(char)));
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<byte>.Count + (int)(_ch3ByteOffset / sizeof(char)));

                    // Find which starting positions likely contain a match (likely match all 3 anchor characters).
                    Vector128<byte> result = GetComparisonResult(ref searchSpace, ch2ByteOffset, ch3ByteOffset, ch1, ch2, ch3);

                    if (result != Vector128<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector128<byte>.Count)))
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

            char valueHead = _valueState.Value.GetRawStringData();

            for (nint i = 0; i < searchSpaceMinusValueTailLength; i++)
            {
                ref char cur = ref Unsafe.Add(ref searchSpace, i);

                // CaseInsensitiveUnicode doesn't support single-character transformations, so we skip checking the first character first.
                if ((typeof(TCaseSensitivity) == typeof(CaseInsensitiveUnicode) || TCaseSensitivity.TransformInput(cur) == valueHead) &&
                    TCaseSensitivity.Equals<TValueLength>(ref cur, in _valueState))
                {
                    return (int)i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static Vector128<byte> GetComparisonResult(ref char searchSpace, nuint ch2ByteOffset, nuint ch3ByteOffset, Vector128<byte> ch1, Vector128<byte> ch2, Vector128<byte> ch3)
        {
            // Load 3 vectors from the input.
            // One from the current search space, the other two at an offset based on the distance of those characters from the first one.
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector128<byte> cmpCh1 = Vector128.Equals(ch1, LoadPacked128(ref searchSpace, 0));
                Vector128<byte> cmpCh2 = Vector128.Equals(ch2, LoadPacked128(ref searchSpace, ch2ByteOffset));
                Vector128<byte> cmpCh3 = Vector128.Equals(ch3, LoadPacked128(ref searchSpace, ch3ByteOffset));
                // AND all 3 together to get a mask of possible match positions that match in at least 3 places.
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
            else
            {
                // For each, AND the value with ~0x20 so that letters are uppercased.
                // For characters that aren't ASCII letters, this may produce wrong results, but only false-positives.
                // We will take care of those in the verification step if the other characters also indicate a possible match.
                Vector128<byte> caseConversion = Vector128.Create(CaseConversionMask);

                Vector128<byte> cmpCh1 = Vector128.Equals(ch1, LoadPacked128(ref searchSpace, 0) & caseConversion);
                Vector128<byte> cmpCh2 = Vector128.Equals(ch2, LoadPacked128(ref searchSpace, ch2ByteOffset) & caseConversion);
                Vector128<byte> cmpCh3 = Vector128.Equals(ch3, LoadPacked128(ref searchSpace, ch3ByteOffset) & caseConversion);
                // AND all 3 together to get a mask of possible match positions that likely match in at least 3 places.
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> GetComparisonResult(ref char searchSpace, nuint ch2ByteOffset, nuint ch3ByteOffset, Vector256<byte> ch1, Vector256<byte> ch2, Vector256<byte> ch3)
        {
            // See comments in 'GetComparisonResult' for Vector128<byte> above.
            // This method is the same, but operates on 32 input characters at a time.
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector256<byte> cmpCh1 = Vector256.Equals(ch1, LoadPacked256(ref searchSpace, 0));
                Vector256<byte> cmpCh2 = Vector256.Equals(ch2, LoadPacked256(ref searchSpace, ch2ByteOffset));
                Vector256<byte> cmpCh3 = Vector256.Equals(ch3, LoadPacked256(ref searchSpace, ch3ByteOffset));
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
            else
            {
                Vector256<byte> caseConversion = Vector256.Create(CaseConversionMask);

                Vector256<byte> cmpCh1 = Vector256.Equals(ch1, LoadPacked256(ref searchSpace, 0) & caseConversion);
                Vector256<byte> cmpCh2 = Vector256.Equals(ch2, LoadPacked256(ref searchSpace, ch2ByteOffset) & caseConversion);
                Vector256<byte> cmpCh3 = Vector256.Equals(ch3, LoadPacked256(ref searchSpace, ch3ByteOffset) & caseConversion);
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        private static Vector512<byte> GetComparisonResult(ref char searchSpace, nuint ch2ByteOffset, nuint ch3ByteOffset, Vector512<byte> ch1, Vector512<byte> ch2, Vector512<byte> ch3)
        {
            // See comments in 'GetComparisonResult' for Vector128<byte> above.
            // This method is the same, but operates on 64 input characters at a time.
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector512<byte> cmpCh1 = Vector512.Equals(ch1, LoadPacked512(ref searchSpace, 0));
                Vector512<byte> cmpCh2 = Vector512.Equals(ch2, LoadPacked512(ref searchSpace, ch2ByteOffset));
                Vector512<byte> cmpCh3 = Vector512.Equals(ch3, LoadPacked512(ref searchSpace, ch3ByteOffset));
                return (cmpCh1 & cmpCh2 & cmpCh3).AsByte();
            }
            else
            {
                Vector512<byte> caseConversion = Vector512.Create(CaseConversionMask);

                Vector512<byte> cmpCh1 = Vector512.Equals(ch1, LoadPacked512(ref searchSpace, 0) & caseConversion);
                Vector512<byte> cmpCh2 = Vector512.Equals(ch2, LoadPacked512(ref searchSpace, ch2ByteOffset) & caseConversion);
                Vector512<byte> cmpCh3 = Vector512.Equals(ch3, LoadPacked512(ref searchSpace, ch3ByteOffset) & caseConversion);
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

                ref char matchRef = ref Unsafe.Add(ref searchSpace, bitPos);

                ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref matchRef, _valueState.Value.Length);

                if (CanSkipAnchorMatchVerification || TCaseSensitivity.Equals<TValueLength>(ref matchRef, in _valueState))
                {
                    offsetFromStart = (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / sizeof(char));
                    return true;
                }

                mask = BitOperations.ResetLowestSetBit(mask);
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

                ref char matchRef = ref Unsafe.Add(ref searchSpace, bitPos);

                ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref matchRef, _valueState.Value.Length);

                if (CanSkipAnchorMatchVerification || TCaseSensitivity.Equals<TValueLength>(ref matchRef, in _valueState))
                {
                    offsetFromStart = (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / sizeof(char));
                    return true;
                }

                mask = BitOperations.ResetLowestSetBit(mask);
            }
            while (mask != 0);

            offsetFromStart = 0;
            return false;
        }

        internal override bool ContainsCore(string value) => HasUniqueValues
            ? base.ContainsCore(value)
            : _valueState.Value.Equals(value, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        internal override string[] GetValues() => HasUniqueValues
            ? base.GetValues()
            : [_valueState.Value];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static Vector128<byte> LoadPacked128(ref char searchSpace, nuint byteOffset)
        {
            Vector128<ushort> input0 = Vector128.LoadUnsafe(ref Unsafe.AddByteOffset(ref searchSpace, byteOffset));
            Vector128<ushort> input1 = Vector128.LoadUnsafe(ref Unsafe.AddByteOffset(ref searchSpace, byteOffset + (uint)Vector128<byte>.Count));

            return Sse2.IsSupported
                ? Sse2.PackUnsignedSaturate(input0.AsInt16(), input1.AsInt16())
                : AdvSimd.Arm64.UnzipEven(input0.AsByte(), input1.AsByte());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> LoadPacked256(ref char searchSpace, nuint byteOffset) =>
            Avx2.PackUnsignedSaturate(
                Vector256.LoadUnsafe(ref Unsafe.AddByteOffset(ref searchSpace, byteOffset)).AsInt16(),
                Vector256.LoadUnsafe(ref Unsafe.AddByteOffset(ref searchSpace, byteOffset + (uint)Vector256<byte>.Count)).AsInt16());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        private static Vector512<byte> LoadPacked512(ref char searchSpace, nuint byteOffset) =>
            Avx512BW.PackUnsignedSaturate(
                Vector512.LoadUnsafe(ref Unsafe.AddByteOffset(ref searchSpace, byteOffset)).AsInt16(),
                Vector512.LoadUnsafe(ref Unsafe.AddByteOffset(ref searchSpace, byteOffset + (uint)Vector512<byte>.Count)).AsInt16());
    }
}
