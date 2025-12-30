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
    /// Specialized <see cref="StringSearchValuesBase"/> implementation for exactly two strings.
    /// Uses packed byte comparisons similar to <see cref="SingleStringSearchValuesPackedThreeChars{TValueLength, TCaseSensitivity}"/>
    /// to process twice as many characters per iteration.
    /// </summary>
    /// <remarks>
    /// This implementation packs two consecutive Vector&lt;ushort&gt; inputs into one Vector&lt;byte&gt;,
    /// allowing it to compare 16/32/64 character positions per iteration.
    /// It uses a shared second character offset for both values (4 comparisons total: v0Ch1, v0Ch2, v1Ch1, v1Ch2),
    /// but only requires 2 vector loads per iteration (input at offset 0 and input at the shared offset).
    /// This reduces memory bandwidth compared to using separate offsets per value.
    /// The <c>ThreeChars</c> suffix in the type name is retained for consistency with the single-string variant and to reflect
    /// the algorithm family it belongs to; it does not mean that this type uses three anchor characters per string.
    /// </remarks>
    internal sealed class TwoStringSearchValuesPackedThreeChars<TCaseSensitivity> : StringSearchValuesBase
        where TCaseSensitivity : struct, ICaseSensitivity
    {
        private const byte CaseConversionMask = unchecked((byte)~0x20);

        private readonly string _value0;
        private readonly string _value1;
        private readonly nint _minusValueTailLength;
        private readonly int _minValueLength;

        // First character anchors (at offset 0) for each value
        private readonly byte _v0Ch1;
        private readonly byte _v1Ch1;

        // Second character anchors at shared offset
        private readonly nuint _ch2ByteOffset;
        private readonly byte _v0Ch2;
        private readonly byte _v1Ch2;

        private static bool IgnoreCase => typeof(TCaseSensitivity) != typeof(CaseSensitive);

        public TwoStringSearchValuesPackedThreeChars(HashSet<string> uniqueValues, string value0, string value1, int ch2Offset)
            : base(uniqueValues)
        {
            Debug.Assert(Sse2.IsSupported || AdvSimd.Arm64.IsSupported);
            Debug.Assert(value0.Length > 1);
            Debug.Assert(value1.Length > 1);
            Debug.Assert(ch2Offset > 0);
            Debug.Assert(ch2Offset < Math.Min(value0.Length, value1.Length));
            Debug.Assert(value0[0] <= byte.MaxValue && value0[ch2Offset] <= byte.MaxValue);
            Debug.Assert(value1[0] <= byte.MaxValue && value1[ch2Offset] <= byte.MaxValue);

            _value0 = value0;
            _value1 = value1;

            int minLength = Math.Min(value0.Length, value1.Length);
            _minValueLength = minLength;

            // We need to reserve space for reading at ch2Offset from the starting position,
            // and for verifying the full value (minLength - 1 chars after the starting position for the shorter value)
            _minusValueTailLength = -Math.Max(minLength - 1, ch2Offset);

            _v0Ch1 = (byte)value0[0];
            _v1Ch1 = (byte)value1[0];
            _v0Ch2 = (byte)value0[ch2Offset];
            _v1Ch2 = (byte)value1[ch2Offset];

            if (IgnoreCase)
            {
                _v0Ch1 &= CaseConversionMask;
                _v1Ch1 &= CaseConversionMask;
                _v0Ch2 &= CaseConversionMask;
                _v1Ch2 &= CaseConversionMask;
            }

            _ch2ByteOffset = (nuint)ch2Offset * sizeof(char);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyMultiString(ReadOnlySpan<char> span) =>
            IndexOf(ref MemoryMarshal.GetReference(span), span.Length);

        private int IndexOf(ref char searchSpace, int searchSpaceLength)
        {
            ref char searchSpaceStart = ref searchSpace;

            // Calculate how many positions we can safely search (accounting for max offset needed)
            nint searchSpaceMinusValueTailLength = searchSpaceLength + _minusValueTailLength;

            nuint ch2ByteOffset = _ch2ByteOffset;

            // Packed variant processes Vector<byte>.Count characters at a time
            if (Vector512.IsHardwareAccelerated && Avx512BW.IsSupported && searchSpaceMinusValueTailLength - Vector512<byte>.Count >= 0)
            {
                Vector512<byte> v0Ch1Vec = Vector512.Create(_v0Ch1);
                Vector512<byte> v0Ch2Vec = Vector512.Create(_v0Ch2);
                Vector512<byte> v1Ch1Vec = Vector512.Create(_v1Ch1);
                Vector512<byte> v1Ch2Vec = Vector512.Create(_v1Ch2);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector512<byte>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<byte>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector512<byte>.Count + (int)(ch2ByteOffset / sizeof(char)));

                    Vector512<byte> result = GetComparisonResult(ref searchSpace, ch2ByteOffset, v0Ch1Vec, v0Ch2Vec, v1Ch1Vec, v1Ch2Vec);

                    if (result != Vector512<byte>.Zero)
                    {
                        goto CandidateFound512;
                    }

                LoopFooter512:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector512<byte>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector512<byte>.Count)))
                        {
                            return -1;
                        }

                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound512:
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, PackedSpanHelpers.FixUpPackedVector512Result(result).ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter512;
                }
            }
            else if (Vector256.IsHardwareAccelerated && Avx2.IsSupported && searchSpaceMinusValueTailLength - Vector256<byte>.Count >= 0)
            {
                Vector256<byte> v0Ch1Vec = Vector256.Create(_v0Ch1);
                Vector256<byte> v0Ch2Vec = Vector256.Create(_v0Ch2);
                Vector256<byte> v1Ch1Vec = Vector256.Create(_v1Ch1);
                Vector256<byte> v1Ch2Vec = Vector256.Create(_v1Ch2);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector256<byte>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<byte>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector256<byte>.Count + (int)(ch2ByteOffset / sizeof(char)));

                    Vector256<byte> result = GetComparisonResult(ref searchSpace, ch2ByteOffset, v0Ch1Vec, v0Ch2Vec, v1Ch1Vec, v1Ch2Vec);

                    if (result != Vector256<byte>.Zero)
                    {
                        goto CandidateFound256;
                    }

                LoopFooter256:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector256<byte>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector256<byte>.Count)))
                        {
                            return -1;
                        }

                        searchSpace = ref lastSearchSpace;
                    }

                    continue;

                CandidateFound256:
                    if (TryMatch(ref searchSpaceStart, searchSpaceLength, ref searchSpace, PackedSpanHelpers.FixUpPackedVector256Result(result).ExtractMostSignificantBits(), out int offset))
                    {
                        return offset;
                    }
                    goto LoopFooter256;
                }
            }
            else if ((Sse2.IsSupported || AdvSimd.Arm64.IsSupported) && searchSpaceMinusValueTailLength - Vector128<byte>.Count >= 0)
            {
                Vector128<byte> v0Ch1Vec = Vector128.Create(_v0Ch1);
                Vector128<byte> v0Ch2Vec = Vector128.Create(_v0Ch2);
                Vector128<byte> v1Ch1Vec = Vector128.Create(_v1Ch1);
                Vector128<byte> v1Ch2Vec = Vector128.Create(_v1Ch2);

                ref char lastSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceMinusValueTailLength - Vector128<byte>.Count);

                while (true)
                {
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<byte>.Count);
                    ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref searchSpace, Vector128<byte>.Count + (int)(ch2ByteOffset / sizeof(char)));

                    Vector128<byte> result = GetComparisonResult(ref searchSpace, ch2ByteOffset, v0Ch1Vec, v0Ch2Vec, v1Ch1Vec, v1Ch2Vec);

                    if (result != Vector128<byte>.Zero)
                    {
                        goto CandidateFound128;
                    }

                LoopFooter128:
                    searchSpace = ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count);

                    if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpace))
                    {
                        if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpace, Vector128<byte>.Count)))
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

            // Fallback: Iterate through all valid starting positions
            nint shortInputEnd = searchSpaceLength - _minValueLength + 1;
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
        [CompExactlyDependsOn(typeof(Sse2))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static Vector128<byte> GetComparisonResult(
            ref char searchSpace,
            nuint ch2ByteOffset,
            Vector128<byte> v0Ch1, Vector128<byte> v0Ch2,
            Vector128<byte> v1Ch1, Vector128<byte> v1Ch2)
        {
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                // Load packed input at position 0 and at the shared second character offset
                Vector128<byte> input0 = LoadPacked128(ref searchSpace, 0);
                Vector128<byte> inputCh2 = LoadPacked128(ref searchSpace, ch2ByteOffset);

                // Compare first characters of both values
                Vector128<byte> cmpV0Ch1 = Vector128.Equals(v0Ch1, input0);
                Vector128<byte> cmpV1Ch1 = Vector128.Equals(v1Ch1, input0);

                // Compare second characters at the shared offset
                Vector128<byte> cmpV0Ch2 = Vector128.Equals(v0Ch2, inputCh2);
                Vector128<byte> cmpV1Ch2 = Vector128.Equals(v1Ch2, inputCh2);

                // A position matches if (value0's ch1 AND ch2 match) OR (value1's ch1 AND ch2 match)
                Vector128<byte> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector128<byte> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return matchV0 | matchV1;
            }
            else
            {
                Vector128<byte> caseConversion = Vector128.Create(CaseConversionMask);

                // Load packed input at position 0 and at the shared second character offset, applying case conversion
                Vector128<byte> input0 = LoadPacked128(ref searchSpace, 0) & caseConversion;
                Vector128<byte> inputCh2 = LoadPacked128(ref searchSpace, ch2ByteOffset) & caseConversion;

                // Compare first characters of both values
                Vector128<byte> cmpV0Ch1 = Vector128.Equals(v0Ch1, input0);
                Vector128<byte> cmpV1Ch1 = Vector128.Equals(v1Ch1, input0);

                // Compare second characters at the shared offset
                Vector128<byte> cmpV0Ch2 = Vector128.Equals(v0Ch2, inputCh2);
                Vector128<byte> cmpV1Ch2 = Vector128.Equals(v1Ch2, inputCh2);

                // A position matches if (value0's ch1 AND ch2 match) OR (value1's ch1 AND ch2 match)
                Vector128<byte> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector128<byte> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return matchV0 | matchV1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> GetComparisonResult(
            ref char searchSpace,
            nuint ch2ByteOffset,
            Vector256<byte> v0Ch1, Vector256<byte> v0Ch2,
            Vector256<byte> v1Ch1, Vector256<byte> v1Ch2)
        {
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector256<byte> input0 = LoadPacked256(ref searchSpace, 0);
                Vector256<byte> inputCh2 = LoadPacked256(ref searchSpace, ch2ByteOffset);

                Vector256<byte> cmpV0Ch1 = Vector256.Equals(v0Ch1, input0);
                Vector256<byte> cmpV1Ch1 = Vector256.Equals(v1Ch1, input0);
                Vector256<byte> cmpV0Ch2 = Vector256.Equals(v0Ch2, inputCh2);
                Vector256<byte> cmpV1Ch2 = Vector256.Equals(v1Ch2, inputCh2);

                Vector256<byte> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector256<byte> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return matchV0 | matchV1;
            }
            else
            {
                Vector256<byte> caseConversion = Vector256.Create(CaseConversionMask);

                Vector256<byte> input0 = LoadPacked256(ref searchSpace, 0) & caseConversion;
                Vector256<byte> inputCh2 = LoadPacked256(ref searchSpace, ch2ByteOffset) & caseConversion;

                Vector256<byte> cmpV0Ch1 = Vector256.Equals(v0Ch1, input0);
                Vector256<byte> cmpV1Ch1 = Vector256.Equals(v1Ch1, input0);
                Vector256<byte> cmpV0Ch2 = Vector256.Equals(v0Ch2, inputCh2);
                Vector256<byte> cmpV1Ch2 = Vector256.Equals(v1Ch2, inputCh2);

                Vector256<byte> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector256<byte> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return matchV0 | matchV1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        private static Vector512<byte> GetComparisonResult(
            ref char searchSpace,
            nuint ch2ByteOffset,
            Vector512<byte> v0Ch1, Vector512<byte> v0Ch2,
            Vector512<byte> v1Ch1, Vector512<byte> v1Ch2)
        {
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                Vector512<byte> input0 = LoadPacked512(ref searchSpace, 0);
                Vector512<byte> inputCh2 = LoadPacked512(ref searchSpace, ch2ByteOffset);

                Vector512<byte> cmpV0Ch1 = Vector512.Equals(v0Ch1, input0);
                Vector512<byte> cmpV1Ch1 = Vector512.Equals(v1Ch1, input0);
                Vector512<byte> cmpV0Ch2 = Vector512.Equals(v0Ch2, inputCh2);
                Vector512<byte> cmpV1Ch2 = Vector512.Equals(v1Ch2, inputCh2);

                Vector512<byte> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector512<byte> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return matchV0 | matchV1;
            }
            else
            {
                Vector512<byte> caseConversion = Vector512.Create(CaseConversionMask);

                Vector512<byte> input0 = LoadPacked512(ref searchSpace, 0) & caseConversion;
                Vector512<byte> inputCh2 = LoadPacked512(ref searchSpace, ch2ByteOffset) & caseConversion;

                Vector512<byte> cmpV0Ch1 = Vector512.Equals(v0Ch1, input0);
                Vector512<byte> cmpV1Ch1 = Vector512.Equals(v1Ch1, input0);
                Vector512<byte> cmpV0Ch2 = Vector512.Equals(v0Ch2, inputCh2);
                Vector512<byte> cmpV1Ch2 = Vector512.Equals(v1Ch2, inputCh2);

                Vector512<byte> matchV0 = cmpV0Ch1 & cmpV0Ch2;
                Vector512<byte> matchV1 = cmpV1Ch1 & cmpV1Ch2;

                return matchV0 | matchV1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryMatch(ref char searchSpaceStart, int searchSpaceLength, ref char searchSpace, uint mask, out int offsetFromStart)
        {
            do
            {
                int bitPos = BitOperations.TrailingZeroCount(mask);

                ref char matchRef = ref Unsafe.Add(ref searchSpace, bitPos);
                int lengthRemaining = searchSpaceLength - (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / sizeof(char));

                ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref matchRef, Math.Min(_value0.Length, lengthRemaining));

                // Check both values - return the one that matches
                if (StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, _value0) ||
                    StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, _value1))
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
            do
            {
                int bitPos = BitOperations.TrailingZeroCount(mask);

                ref char matchRef = ref Unsafe.Add(ref searchSpace, bitPos);
                int lengthRemaining = searchSpaceLength - (int)((nuint)Unsafe.ByteOffset(ref searchSpaceStart, ref matchRef) / sizeof(char));

                ValidateReadPosition(ref searchSpaceStart, searchSpaceLength, ref matchRef, Math.Min(_value0.Length, lengthRemaining));

                // Check both values - return the one that matches
                if (StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, _value0) ||
                    StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, _value1))
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
