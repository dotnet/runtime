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
using static System.Buffers.TeddyHelper;

namespace System.Buffers
{
    internal abstract class AsciiStringSearchValuesTeddyBase<TBucketized, TStartCaseSensitivity, TCaseSensitivity> : StringSearchValuesRabinKarp<TCaseSensitivity>
        where TBucketized : struct, SearchValues.IRuntimeConst
        where TStartCaseSensitivity : struct, ICaseSensitivity
        where TCaseSensitivity : struct, ICaseSensitivity
    {
        private const int MatchStartOffsetN2 = 1;
        private const int MatchStartOffsetN3 = 2;
        private const int CharsPerIterationVector128 = 16;
        private const int CharsPerIterationAvx2 = 32;
        private const int CharsPerIterationAvx512 = 64;

        private readonly EightPackedReferences _buckets;

        private readonly Vector512<byte>
            _n0Low, _n0High,
            _n1Low, _n1High,
            _n2Low, _n2High;

        protected AsciiStringSearchValuesTeddyBase(ReadOnlySpan<string> values, HashSet<string> uniqueValues, int n) : base(values, uniqueValues)
        {
            Debug.Assert(!TBucketized.Value);
            Debug.Assert(n is 2 or 3);

            _buckets = new EightPackedReferences(MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<string, object>(ref MemoryMarshal.GetReference(values)),
                values.Length));

            (_n0Low, _n0High) = TeddyBucketizer.GenerateNonBucketizedFingerprint(values, offset: 0);
            (_n1Low, _n1High) = TeddyBucketizer.GenerateNonBucketizedFingerprint(values, offset: 1);

            if (n == 3)
            {
                (_n2Low, _n2High) = TeddyBucketizer.GenerateNonBucketizedFingerprint(values, offset: 2);
            }
        }

        protected AsciiStringSearchValuesTeddyBase(string[][] buckets, ReadOnlySpan<string> values, HashSet<string> uniqueValues, int n) : base(values, uniqueValues)
        {
            Debug.Assert(TBucketized.Value);
            Debug.Assert(n is 2 or 3);

            _buckets = new EightPackedReferences(buckets);

            (_n0Low, _n0High) = TeddyBucketizer.GenerateBucketizedFingerprint(buckets, offset: 0);
            (_n1Low, _n1High) = TeddyBucketizer.GenerateBucketizedFingerprint(buckets, offset: 1);

            if (n == 3)
            {
                (_n2Low, _n2High) = TeddyBucketizer.GenerateBucketizedFingerprint(buckets, offset: 2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        protected int IndexOfAnyN2(ReadOnlySpan<char> span)
        {
            // The behavior of the rest of the function remains the same if Avx2 or Avx512BW aren't supported
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            if (Vector512.IsHardwareAccelerated && Avx512BW.IsSupported && span.Length >= CharsPerIterationAvx512 + MatchStartOffsetN2)
            {
                return IndexOfAnyN2Avx512(span);
            }

            if (Avx2.IsSupported && span.Length >= CharsPerIterationAvx2 + MatchStartOffsetN2)
            {
                return IndexOfAnyN2Avx2(span);
            }
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough

            return IndexOfAnyN2Vector128(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        protected int IndexOfAnyN3(ReadOnlySpan<char> span)
        {
            // The behavior of the rest of the function remains the same if Avx2 or Avx512BW aren't supported
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            if (Vector512.IsHardwareAccelerated && Avx512BW.IsSupported && span.Length >= CharsPerIterationAvx512 + MatchStartOffsetN3)
            {
                return IndexOfAnyN3Avx512(span);
            }

            if (Avx2.IsSupported && span.Length >= CharsPerIterationAvx2 + MatchStartOffsetN3)
            {
                return IndexOfAnyN3Avx2(span);
            }
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough

            return IndexOfAnyN3Vector128(span);
        }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private int IndexOfAnyN2Vector128(ReadOnlySpan<char> span)
        {
            if (span.Length < CharsPerIterationVector128 + MatchStartOffsetN2)
            {
                return ShortInputFallback(span);
            }

            ref char searchSpace = ref MemoryMarshal.GetReference(span);
            ref char lastSearchSpaceStart = ref Unsafe.Add(ref searchSpace, span.Length - CharsPerIterationVector128);

            searchSpace = ref Unsafe.Add(ref searchSpace, MatchStartOffsetN2);

            Vector128<byte> n0Low = _n0Low._lower._lower, n0High = _n0High._lower._lower;
            Vector128<byte> n1Low = _n1Low._lower._lower, n1High = _n1High._lower._lower;
            Vector128<byte> prev0 = Vector128<byte>.AllBitsSet;

        Loop:
            Vector128<byte> input = TStartCaseSensitivity.TransformInput(LoadAndPack16AsciiChars(ref searchSpace));

            (Vector128<byte> result, prev0) = ProcessInputN2(input, prev0, n0Low, n0High, n1Low, n1High);

            if (result != Vector128<byte>.Zero)
            {
                goto CandidateFound;
            }

        ContinueLoop:
            searchSpace = ref Unsafe.Add(ref searchSpace, CharsPerIterationVector128);

            if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpaceStart))
            {
                if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpaceStart, CharsPerIterationVector128)))
                {
                    return -1;
                }

                // We're switching which characters we will process in the next iteration.
                // prev0 no longer points to the characters just before the current input, so we must reset it.
                prev0 = Vector128<byte>.AllBitsSet;
                searchSpace = ref lastSearchSpaceStart;
            }
            goto Loop;

        CandidateFound:
            if (TryFindMatch(span, ref searchSpace, result, MatchStartOffsetN2, out int offset))
            {
                return offset;
            }
            goto ContinueLoop;
        }

        [CompExactlyDependsOn(typeof(Avx2))]
        private int IndexOfAnyN2Avx2(ReadOnlySpan<char> span)
        {
            Debug.Assert(span.Length >= CharsPerIterationAvx2 + MatchStartOffsetN2);

            ref char searchSpace = ref MemoryMarshal.GetReference(span);
            ref char lastSearchSpaceStart = ref Unsafe.Add(ref searchSpace, span.Length - CharsPerIterationAvx2);

            searchSpace = ref Unsafe.Add(ref searchSpace, MatchStartOffsetN2);

            Vector256<byte> n0Low = _n0Low._lower, n0High = _n0High._lower;
            Vector256<byte> n1Low = _n1Low._lower, n1High = _n1High._lower;
            Vector256<byte> prev0 = Vector256<byte>.AllBitsSet;

        Loop:
            Vector256<byte> input = TStartCaseSensitivity.TransformInput(LoadAndPack32AsciiChars(ref searchSpace));

            (Vector256<byte> result, prev0) = ProcessInputN2(input, prev0, n0Low, n0High, n1Low, n1High);

            if (result != Vector256<byte>.Zero)
            {
                goto CandidateFound;
            }

        ContinueLoop:
            searchSpace = ref Unsafe.Add(ref searchSpace, CharsPerIterationAvx2);

            if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpaceStart))
            {
                if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpaceStart, CharsPerIterationAvx2)))
                {
                    return -1;
                }

                // We're switching which characters we will process in the next iteration.
                // prev0 no longer points to the characters just before the current input, so we must reset it.
                prev0 = Vector256<byte>.AllBitsSet;
                searchSpace = ref lastSearchSpaceStart;
            }
            goto Loop;

        CandidateFound:
            if (TryFindMatch(span, ref searchSpace, result, MatchStartOffsetN2, out int offset))
            {
                return offset;
            }
            goto ContinueLoop;
        }

        [CompExactlyDependsOn(typeof(Avx512BW))]
        private int IndexOfAnyN2Avx512(ReadOnlySpan<char> span)
        {
            Debug.Assert(span.Length >= CharsPerIterationAvx512 + MatchStartOffsetN2);

            ref char searchSpace = ref MemoryMarshal.GetReference(span);
            ref char lastSearchSpaceStart = ref Unsafe.Add(ref searchSpace, span.Length - CharsPerIterationAvx512);

            searchSpace = ref Unsafe.Add(ref searchSpace, MatchStartOffsetN2);

            Vector512<byte> n0Low = _n0Low, n0High = _n0High;
            Vector512<byte> n1Low = _n1Low, n1High = _n1High;
            Vector512<byte> prev0 = Vector512<byte>.AllBitsSet;

        Loop:
            Vector512<byte> input = TStartCaseSensitivity.TransformInput(LoadAndPack64AsciiChars(ref searchSpace));

            (Vector512<byte> result, prev0) = ProcessInputN2(input, prev0, n0Low, n0High, n1Low, n1High);

            if (result != Vector512<byte>.Zero)
            {
                goto CandidateFound;
            }

        ContinueLoop:
            searchSpace = ref Unsafe.Add(ref searchSpace, CharsPerIterationAvx512);

            if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpaceStart))
            {
                if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpaceStart, CharsPerIterationAvx512)))
                {
                    return -1;
                }

                // We're switching which characters we will process in the next iteration.
                // prev0 no longer points to the characters just before the current input, so we must reset it.
                prev0 = Vector512<byte>.AllBitsSet;
                searchSpace = ref lastSearchSpaceStart;
            }
            goto Loop;

        CandidateFound:
            if (TryFindMatch(span, ref searchSpace, result, MatchStartOffsetN2, out int offset))
            {
                return offset;
            }
            goto ContinueLoop;
        }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private int IndexOfAnyN3Vector128(ReadOnlySpan<char> span)
        {
            if (span.Length < CharsPerIterationVector128 + MatchStartOffsetN3)
            {
                return ShortInputFallback(span);
            }

            ref char searchSpace = ref MemoryMarshal.GetReference(span);
            ref char lastSearchSpaceStart = ref Unsafe.Add(ref searchSpace, span.Length - CharsPerIterationVector128);

            searchSpace = ref Unsafe.Add(ref searchSpace, MatchStartOffsetN3);

            Vector128<byte> n0Low = _n0Low._lower._lower, n0High = _n0High._lower._lower;
            Vector128<byte> n1Low = _n1Low._lower._lower, n1High = _n1High._lower._lower;
            Vector128<byte> n2Low = _n2Low._lower._lower, n2High = _n2High._lower._lower;
            Vector128<byte> prev0 = Vector128<byte>.AllBitsSet;
            Vector128<byte> prev1 = Vector128<byte>.AllBitsSet;

        Loop:
            Vector128<byte> input = TStartCaseSensitivity.TransformInput(LoadAndPack16AsciiChars(ref searchSpace));

            (Vector128<byte> result, prev0, prev1) = ProcessInputN3(input, prev0, prev1, n0Low, n0High, n1Low, n1High, n2Low, n2High);

            if (result != Vector128<byte>.Zero)
            {
                goto CandidateFound;
            }

        ContinueLoop:
            searchSpace = ref Unsafe.Add(ref searchSpace, CharsPerIterationVector128);

            if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpaceStart))
            {
                if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpaceStart, CharsPerIterationVector128)))
                {
                    return -1;
                }

                // We're switching which characters we will process in the next iteration.
                // prev0 and prev1 no longer point to the characters just before the current input, so we must reset them.
                prev0 = Vector128<byte>.AllBitsSet;
                prev1 = Vector128<byte>.AllBitsSet;
                searchSpace = ref lastSearchSpaceStart;
            }
            goto Loop;

        CandidateFound:
            if (TryFindMatch(span, ref searchSpace, result, MatchStartOffsetN3, out int offset))
            {
                return offset;
            }
            goto ContinueLoop;
        }

        [CompExactlyDependsOn(typeof(Avx2))]
        private int IndexOfAnyN3Avx2(ReadOnlySpan<char> span)
        {
            Debug.Assert(span.Length >= CharsPerIterationAvx2 + MatchStartOffsetN3);

            ref char searchSpace = ref MemoryMarshal.GetReference(span);
            ref char lastSearchSpaceStart = ref Unsafe.Add(ref searchSpace, span.Length - CharsPerIterationAvx2);

            searchSpace = ref Unsafe.Add(ref searchSpace, MatchStartOffsetN3);

            Vector256<byte> n0Low = _n0Low._lower, n0High = _n0High._lower;
            Vector256<byte> n1Low = _n1Low._lower, n1High = _n1High._lower;
            Vector256<byte> n2Low = _n2Low._lower, n2High = _n2High._lower;
            Vector256<byte> prev0 = Vector256<byte>.AllBitsSet;
            Vector256<byte> prev1 = Vector256<byte>.AllBitsSet;

        Loop:
            Vector256<byte> input = TStartCaseSensitivity.TransformInput(LoadAndPack32AsciiChars(ref searchSpace));

            (Vector256<byte> result, prev0, prev1) = ProcessInputN3(input, prev0, prev1, n0Low, n0High, n1Low, n1High, n2Low, n2High);

            if (result != Vector256<byte>.Zero)
            {
                goto CandidateFound;
            }

        ContinueLoop:
            searchSpace = ref Unsafe.Add(ref searchSpace, CharsPerIterationAvx2);

            if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpaceStart))
            {
                if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpaceStart, CharsPerIterationAvx2)))
                {
                    return -1;
                }

                // We're switching which characters we will process in the next iteration.
                // prev0 and prev1 no longer point to the characters just before the current input, so we must reset them.
                prev0 = Vector256<byte>.AllBitsSet;
                prev1 = Vector256<byte>.AllBitsSet;
                searchSpace = ref lastSearchSpaceStart;
            }
            goto Loop;

        CandidateFound:
            if (TryFindMatch(span, ref searchSpace, result, MatchStartOffsetN3, out int offset))
            {
                return offset;
            }
            goto ContinueLoop;
        }

        [CompExactlyDependsOn(typeof(Avx512BW))]
        private int IndexOfAnyN3Avx512(ReadOnlySpan<char> span)
        {
            Debug.Assert(span.Length >= CharsPerIterationAvx512 + MatchStartOffsetN3);

            ref char searchSpace = ref MemoryMarshal.GetReference(span);
            ref char lastSearchSpaceStart = ref Unsafe.Add(ref searchSpace, span.Length - CharsPerIterationAvx512);

            searchSpace = ref Unsafe.Add(ref searchSpace, MatchStartOffsetN3);

            Vector512<byte> n0Low = _n0Low, n0High = _n0High;
            Vector512<byte> n1Low = _n1Low, n1High = _n1High;
            Vector512<byte> n2Low = _n2Low, n2High = _n2High;
            Vector512<byte> prev0 = Vector512<byte>.AllBitsSet;
            Vector512<byte> prev1 = Vector512<byte>.AllBitsSet;

        Loop:
            Vector512<byte> input = TStartCaseSensitivity.TransformInput(LoadAndPack64AsciiChars(ref searchSpace));

            (Vector512<byte> result, prev0, prev1) = ProcessInputN3(input, prev0, prev1, n0Low, n0High, n1Low, n1High, n2Low, n2High);

            if (result != Vector512<byte>.Zero)
            {
                goto CandidateFound;
            }

        ContinueLoop:
            searchSpace = ref Unsafe.Add(ref searchSpace, CharsPerIterationAvx512);

            if (Unsafe.IsAddressGreaterThan(ref searchSpace, ref lastSearchSpaceStart))
            {
                if (Unsafe.AreSame(ref searchSpace, ref Unsafe.Add(ref lastSearchSpaceStart, CharsPerIterationAvx512)))
                {
                    return -1;
                }

                // We're switching which characters we will process in the next iteration.
                // prev0 and prev1 no longer point to the characters just before the current input, so we must reset them.
                prev0 = Vector512<byte>.AllBitsSet;
                prev1 = Vector512<byte>.AllBitsSet;
                searchSpace = ref lastSearchSpaceStart;
            }
            goto Loop;

        CandidateFound:
            if (TryFindMatch(span, ref searchSpace, result, MatchStartOffsetN3, out int offset))
            {
                return offset;
            }
            goto ContinueLoop;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindMatch(ReadOnlySpan<char> span, ref char searchSpace, Vector128<byte> result, int matchStartOffset, out int offsetFromStart)
        {
            uint resultMask = (~Vector128.Equals(result, Vector128<byte>.Zero)).ExtractMostSignificantBits();

            do
            {
                int matchOffset = BitOperations.TrailingZeroCount(resultMask);

                ref char matchRef = ref Unsafe.Add(ref searchSpace, matchOffset - matchStartOffset);
                offsetFromStart = (int)((nuint)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref matchRef) / 2);
                int lengthRemaining = span.Length - offsetFromStart;

                uint candidateMask = result.GetElementUnsafe(matchOffset);

                do
                {
                    int candidateOffset = BitOperations.TrailingZeroCount(candidateMask);

                    object? bucket = _buckets[candidateOffset];
                    Debug.Assert(bucket is not null);

                    if (TBucketized.Value
                        ? StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, Unsafe.As<string[]>(bucket))
                        : StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, Unsafe.As<string>(bucket)))
                    {
                        return true;
                    }

                    candidateMask = BitOperations.ResetLowestSetBit(candidateMask);
                }
                while (candidateMask != 0);

                resultMask = BitOperations.ResetLowestSetBit(resultMask);
            }
            while (resultMask != 0);

            offsetFromStart = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindMatch(ReadOnlySpan<char> span, ref char searchSpace, Vector256<byte> result, int matchStartOffset, out int offsetFromStart)
        {
            uint resultMask = (~Vector256.Equals(result, Vector256<byte>.Zero)).ExtractMostSignificantBits();

            do
            {
                int matchOffset = BitOperations.TrailingZeroCount(resultMask);

                ref char matchRef = ref Unsafe.Add(ref searchSpace, matchOffset - matchStartOffset);
                offsetFromStart = (int)((nuint)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref matchRef) / 2);
                int lengthRemaining = span.Length - offsetFromStart;

                uint candidateMask = result.GetElementUnsafe(matchOffset);

                do
                {
                    int candidateOffset = BitOperations.TrailingZeroCount(candidateMask);

                    object? bucket = _buckets[candidateOffset];
                    Debug.Assert(bucket is not null);

                    if (TBucketized.Value
                        ? StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, Unsafe.As<string[]>(bucket))
                        : StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, Unsafe.As<string>(bucket)))
                    {
                        return true;
                    }

                    candidateMask = BitOperations.ResetLowestSetBit(candidateMask);
                }
                while (candidateMask != 0);

                resultMask = BitOperations.ResetLowestSetBit(resultMask);
            }
            while (resultMask != 0);

            offsetFromStart = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindMatch(ReadOnlySpan<char> span, ref char searchSpace, Vector512<byte> result, int matchStartOffset, out int offsetFromStart)
        {
            ulong resultMask = (~Vector512.Equals(result, Vector512<byte>.Zero)).ExtractMostSignificantBits();

            do
            {
                int matchOffset = BitOperations.TrailingZeroCount(resultMask);

                ref char matchRef = ref Unsafe.Add(ref searchSpace, matchOffset - matchStartOffset);
                offsetFromStart = (int)((nuint)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref matchRef) / 2);
                int lengthRemaining = span.Length - offsetFromStart;

                uint candidateMask = result.GetElementUnsafe(matchOffset);

                do
                {
                    int candidateOffset = BitOperations.TrailingZeroCount(candidateMask);

                    object? bucket = _buckets[candidateOffset];
                    Debug.Assert(bucket is not null);

                    if (TBucketized.Value
                        ? StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, Unsafe.As<string[]>(bucket))
                        : StartsWith<TCaseSensitivity>(ref matchRef, lengthRemaining, Unsafe.As<string>(bucket)))
                    {
                        return true;
                    }

                    candidateMask = BitOperations.ResetLowestSetBit(candidateMask);
                }
                while (candidateMask != 0);

                resultMask = BitOperations.ResetLowestSetBit(resultMask);
            }
            while (resultMask != 0);

            offsetFromStart = 0;
            return false;
        }
    }
}
