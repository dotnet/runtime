// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

#pragma warning disable 8500 // sizeof of managed types
#pragma warning disable IDE0060 // https://github.com/dotnet/roslyn-analyzers/issues/6228

namespace System.Buffers
{
    internal static class IndexOfAnyAsciiSearcher
    {
        internal static bool IsVectorizationSupported => Ssse3.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported;

        internal static unsafe void ComputeBitmap256(ReadOnlySpan<byte> values, out Vector256<byte> bitmap0, out Vector256<byte> bitmap1, out BitVector256 lookup)
        {
            // The exact format of these bitmaps differs from the other ComputeBitmap overloads as it's meant for the full [0, 255] range algorithm.
            // See http://0x80.pl/articles/simd-byte-lookup.html#universal-algorithm

            Vector128<byte> bitmapSpace0 = default;
            Vector128<byte> bitmapSpace1 = default;
            byte* bitmapLocal0 = (byte*)&bitmapSpace0;
            byte* bitmapLocal1 = (byte*)&bitmapSpace1;
            BitVector256 lookupLocal = default;

            foreach (byte b in values)
            {
                lookupLocal.Set(b);

                int highNibble = b >> 4;
                int lowNibble = b & 0xF;

                if (highNibble < 8)
                {
                    bitmapLocal0[(uint)lowNibble] |= (byte)(1 << highNibble);
                }
                else
                {
                    bitmapLocal1[(uint)lowNibble] |= (byte)(1 << (highNibble - 8));
                }
            }

            bitmap0 = Vector256.Create(bitmapSpace0, bitmapSpace0);
            bitmap1 = Vector256.Create(bitmapSpace1, bitmapSpace1);
            lookup = lookupLocal;
        }

        internal static unsafe void ComputeBitmap<T>(ReadOnlySpan<T> values, out Vector256<byte> bitmap, out BitVector256 lookup)
            where T : struct, IUnsignedNumber<T>
        {
            Debug.Assert(typeof(T) == typeof(byte) || typeof(T) == typeof(char));

            Vector128<byte> bitmapSpace = default;
            byte* bitmapLocal = (byte*)&bitmapSpace;
            BitVector256 lookupLocal = default;

            foreach (T tValue in values)
            {
                int value = int.CreateChecked(tValue);

                if (value > 127)
                {
                    continue;
                }

                lookupLocal.Set(value);

                int highNibble = value >> 4;
                int lowNibble = value & 0xF;

                bitmapLocal[(uint)lowNibble] |= (byte)(1 << highNibble);
            }

            bitmap = Vector256.Create(bitmapSpace, bitmapSpace);
            lookup = lookupLocal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool TryComputeBitmap(ReadOnlySpan<char> values, byte* bitmap, out bool needleContainsZero)
        {
            byte* bitmapLocal = bitmap; // https://github.com/dotnet/runtime/issues/9040

            foreach (char c in values)
            {
                if (c > 127)
                {
                    needleContainsZero = false;
                    return false;
                }

                int highNibble = c >> 4;
                int lowNibble = c & 0xF;

                bitmapLocal[(uint)lowNibble] |= (byte)(1 << highNibble);
            }

            needleContainsZero = (bitmap[0] & 1) != 0;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryIndexOfAny(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index) =>
            TryIndexOfAny<DontNegate>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, asciiValues, out index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryIndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index) =>
            TryIndexOfAny<Negate>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, asciiValues, out index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryLastIndexOfAny(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index) =>
            TryLastIndexOfAny<DontNegate>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, asciiValues, out index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryLastIndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index) =>
            TryLastIndexOfAny<Negate>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, asciiValues, out index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool TryIndexOfAny<TNegator>(ref short searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index)
            where TNegator : struct, INegator
        {
            Debug.Assert(searchSpaceLength >= Vector128<short>.Count);

            if (IsVectorizationSupported)
            {
                Vector128<byte> bitmap = default;
                if (TryComputeBitmap(asciiValues, (byte*)&bitmap, out bool needleContainsZero))
                {
                    Vector256<byte> bitmap256 = Vector256.Create(bitmap, bitmap);

                    index = (Ssse3.IsSupported || PackedSimd.IsSupported) && needleContainsZero
                        ? IndexOfAnyVectorized<TNegator, Ssse3AndWasmHandleZeroInNeedle>(ref searchSpace, searchSpaceLength, ref bitmap256)
                        : IndexOfAnyVectorized<TNegator, Default>(ref searchSpace, searchSpaceLength, ref bitmap256);
                    return true;
                }
            }

            index = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool TryLastIndexOfAny<TNegator>(ref short searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index)
            where TNegator : struct, INegator
        {
            Debug.Assert(searchSpaceLength >= Vector128<short>.Count);

            if (IsVectorizationSupported)
            {
                Vector128<byte> bitmap = default;
                if (TryComputeBitmap(asciiValues, (byte*)&bitmap, out bool needleContainsZero))
                {
                    Vector256<byte> bitmap256 = Vector256.Create(bitmap, bitmap);

                    index = (Ssse3.IsSupported || PackedSimd.IsSupported) && needleContainsZero
                        ? LastIndexOfAnyVectorized<TNegator, Ssse3AndWasmHandleZeroInNeedle>(ref searchSpace, searchSpaceLength, ref bitmap256)
                        : LastIndexOfAnyVectorized<TNegator, Default>(ref searchSpace, searchSpaceLength, ref bitmap256);
                    return true;
                }
            }

            index = default;
            return false;
        }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal static int IndexOfAnyVectorized<TNegator, TOptimizations>(ref short searchSpace, int searchSpaceLength, ref Vector256<byte> bitmapRef)
            where TNegator : struct, INegator
            where TOptimizations : struct, IOptimizations
        {
            ref short currentSearchSpace = ref searchSpace;

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The behavior of the rest of the function remains the same if Avx2.IsSupported is false
            if (Avx2.IsSupported && searchSpaceLength > 2 * Vector128<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                Vector256<byte> bitmap256 = bitmapRef;

                if (searchSpaceLength > 2 * Vector256<short>.Count)
                {
                    // Process the input in chunks of 32 characters (2 * Vector256<short>).
                    // We're mainly interested in a single byte of each character, and the core lookup operates on a Vector256<byte>.
                    // As packing two Vector256<short>s into a Vector256<byte> is cheap compared to the lookup, we can effectively double the throughput.
                    // If the input length is a multiple of 32, don't consume the last 32 characters in this loop.
                    // Let the fallback below handle it instead. This is why the condition is
                    // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                    ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - (2 * Vector256<short>.Count));

                    do
                    {
                        Vector256<short> source0 = Vector256.LoadUnsafe(ref currentSearchSpace);
                        Vector256<short> source1 = Vector256.LoadUnsafe(ref currentSearchSpace, (nuint)Vector256<short>.Count);

                        Vector256<byte> result = IndexOfAnyLookup<TNegator, TOptimizations>(source0, source1, bitmap256);
                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeFirstIndex<short, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                        }

                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector256<short>.Count);
                    }
                    while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                }

                // We have 1-32 characters remaining. Process the first and last vector in the search space.
                // They may overlap, but we'll handle that in the index calculation if we do get a match.
                Debug.Assert(searchSpaceLength >= Vector256<short>.Count, "We expect that the input is long enough for us to load a whole vector.");
                {
                    ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector256<short>.Count);

                    ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                        ? ref oneVectorAwayFromEnd
                        : ref currentSearchSpace;

                    Vector256<short> source0 = Vector256.LoadUnsafe(ref firstVector);
                    Vector256<short> source1 = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);

                    Vector256<byte> result = IndexOfAnyLookup<TNegator, TOptimizations>(source0, source1, bitmap256);
                    if (result != Vector256<byte>.Zero)
                    {
                        return ComputeFirstIndexOverlapped<short, TNegator>(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                    }
                }

                return -1;
            }

            Vector128<byte> bitmap = bitmapRef._lower;

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The behavior of the rest of the function remains the same if Avx2.IsSupported is false
            if (!Avx2.IsSupported && searchSpaceLength > 2 * Vector128<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                // Process the input in chunks of 16 characters (2 * Vector128<short>).
                // We're mainly interested in a single byte of each character, and the core lookup operates on a Vector128<byte>.
                // As packing two Vector128<short>s into a Vector128<byte> is cheap compared to the lookup, we can effectively double the throughput.
                // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                // Let the fallback below handle it instead. This is why the condition is
                // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - (2 * Vector128<short>.Count));

                do
                {
                    Vector128<short> source0 = Vector128.LoadUnsafe(ref currentSearchSpace);
                    Vector128<short> source1 = Vector128.LoadUnsafe(ref currentSearchSpace, (nuint)Vector128<short>.Count);

                    Vector128<byte> result = IndexOfAnyLookup<TNegator, TOptimizations>(source0, source1, bitmap);
                    if (result != Vector128<byte>.Zero)
                    {
                        return ComputeFirstIndex<short, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                    }

                    currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector128<short>.Count);
                }
                while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
            }

            // We have 1-16 characters remaining. Process the first and last vector in the search space.
            // They may overlap, but we'll handle that in the index calculation if we do get a match.
            Debug.Assert(searchSpaceLength >= Vector128<short>.Count, "We expect that the input is long enough for us to load a whole vector.");
            {
                ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<short>.Count);

                ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                    ? ref oneVectorAwayFromEnd
                    : ref currentSearchSpace;

                Vector128<short> source0 = Vector128.LoadUnsafe(ref firstVector);
                Vector128<short> source1 = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);

                Vector128<byte> result = IndexOfAnyLookup<TNegator, TOptimizations>(source0, source1, bitmap);
                if (result != Vector128<byte>.Zero)
                {
                    return ComputeFirstIndexOverlapped<short, TNegator>(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal static int LastIndexOfAnyVectorized<TNegator, TOptimizations>(ref short searchSpace, int searchSpaceLength, ref Vector256<byte> bitmapRef)
            where TNegator : struct, INegator
            where TOptimizations : struct, IOptimizations
        {
            ref short currentSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceLength);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else clause is semantically equivalent
            if (Avx2.IsSupported && searchSpaceLength > 2 * Vector128<short>.Count)
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                Vector256<byte> bitmap256 = bitmapRef;

                if (searchSpaceLength > 2 * Vector256<short>.Count)
                {
                    // Process the input in chunks of 32 characters (2 * Vector256<short>).
                    // We're mainly interested in a single byte of each character, and the core lookup operates on a Vector256<byte>.
                    // As packing two Vector256<short>s into a Vector256<byte> is cheap compared to the lookup, we can effectively double the throughput.
                    // If the input length is a multiple of 32, don't consume the last 32 characters in this loop.
                    // Let the fallback below handle it instead. This is why the condition is
                    // ">" instead of ">=" above, and why "IsAddressGreaterThan" is used instead of "!IsAddressLessThan".
                    ref short twoVectorsAfterStart = ref Unsafe.Add(ref searchSpace, 2 * Vector256<short>.Count);

                    do
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, 2 * Vector256<short>.Count);

                        Vector256<short> source0 = Vector256.LoadUnsafe(ref currentSearchSpace);
                        Vector256<short> source1 = Vector256.LoadUnsafe(ref currentSearchSpace, (nuint)Vector256<short>.Count);

                        Vector256<byte> result = IndexOfAnyLookup<TNegator, TOptimizations>(source0, source1, bitmap256);
                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeLastIndex<short, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                        }
                    }
                    while (Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref twoVectorsAfterStart));
                }

                // We have 1-32 characters remaining. Process the first and last vector in the search space.
                // They may overlap, but we'll handle that in the index calculation if we do get a match.
                Debug.Assert(searchSpaceLength >= Vector256<short>.Count, "We expect that the input is long enough for us to load a whole vector.");
                {
                    ref short oneVectorAfterStart = ref Unsafe.Add(ref searchSpace, Vector256<short>.Count);

                    ref short secondVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAfterStart)
                        ? ref Unsafe.Subtract(ref currentSearchSpace, Vector256<short>.Count)
                        : ref searchSpace;

                    Vector256<short> source0 = Vector256.LoadUnsafe(ref searchSpace);
                    Vector256<short> source1 = Vector256.LoadUnsafe(ref secondVector);

                    Vector256<byte> result = IndexOfAnyLookup<TNegator, TOptimizations>(source0, source1, bitmap256);
                    if (result != Vector256<byte>.Zero)
                    {
                        return ComputeLastIndexOverlapped<short, TNegator>(ref searchSpace, ref secondVector, result);
                    }
                }

                return -1;
            }

            Vector128<byte> bitmap = bitmapRef._lower;

            if (!Avx2.IsSupported && searchSpaceLength > 2 * Vector128<short>.Count)
            {
                // Process the input in chunks of 16 characters (2 * Vector128<short>).
                // We're mainly interested in a single byte of each character, and the core lookup operates on a Vector128<byte>.
                // As packing two Vector128<short>s into a Vector128<byte> is cheap compared to the lookup, we can effectively double the throughput.
                // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                // Let the fallback below handle it instead. This is why the condition is
                // ">" instead of ">=" above, and why "IsAddressGreaterThan" is used instead of "!IsAddressLessThan".
                ref short twoVectorsAfterStart = ref Unsafe.Add(ref searchSpace, 2 * Vector128<short>.Count);

                do
                {
                    currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, 2 * Vector128<short>.Count);

                    Vector128<short> source0 = Vector128.LoadUnsafe(ref currentSearchSpace);
                    Vector128<short> source1 = Vector128.LoadUnsafe(ref currentSearchSpace, (nuint)Vector128<short>.Count);

                    Vector128<byte> result = IndexOfAnyLookup<TNegator, TOptimizations>(source0, source1, bitmap);
                    if (result != Vector128<byte>.Zero)
                    {
                        return ComputeLastIndex<short, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                    }
                }
                while (Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref twoVectorsAfterStart));
            }

            // We have 1-16 characters remaining. Process the first and last vector in the search space.
            // They may overlap, but we'll handle that in the index calculation if we do get a match.
            Debug.Assert(searchSpaceLength >= Vector128<short>.Count, "We expect that the input is long enough for us to load a whole vector.");
            {
                ref short oneVectorAfterStart = ref Unsafe.Add(ref searchSpace, Vector128<short>.Count);

                ref short secondVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAfterStart)
                    ? ref Unsafe.Subtract(ref currentSearchSpace, Vector128<short>.Count)
                    : ref searchSpace;

                Vector128<short> source0 = Vector128.LoadUnsafe(ref searchSpace);
                Vector128<short> source1 = Vector128.LoadUnsafe(ref secondVector);

                Vector128<byte> result = IndexOfAnyLookup<TNegator, TOptimizations>(source0, source1, bitmap);
                if (result != Vector128<byte>.Zero)
                {
                    return ComputeLastIndexOverlapped<short, TNegator>(ref searchSpace, ref secondVector, result);
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal static int IndexOfAnyVectorized<TNegator>(ref byte searchSpace, int searchSpaceLength, ref Vector256<byte> bitmapRef)
            where TNegator : struct, INegator
        {
            ref byte currentSearchSpace = ref searchSpace;

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The behavior of the rest of the function remains the same if Avx2.IsSupported is false
            if (Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                Vector256<byte> bitmap256 = bitmapRef;

                if (searchSpaceLength > Vector256<byte>.Count)
                {
                    // Process the input in chunks of 32 bytes.
                    // If the input length is a multiple of 32, don't consume the last 32 characters in this loop.
                    // Let the fallback below handle it instead. This is why the condition is
                    // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                    ref byte vectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector256<byte>.Count);

                    do
                    {
                        Vector256<byte> source = Vector256.LoadUnsafe(ref currentSearchSpace);

                        Vector256<byte> result = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmap256));
                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeFirstIndex<byte, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                        }

                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<byte>.Count);
                    }
                    while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref vectorAwayFromEnd));
                }

                // We have 1-32 bytes remaining. Process the first and last half vectors in the search space.
                // They may overlap, but we'll handle that in the index calculation if we do get a match.
                Debug.Assert(searchSpaceLength >= Vector128<byte>.Count, "We expect that the input is long enough for us to load a Vector128.");
                {
                    ref byte halfVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<byte>.Count);

                    ref byte firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref halfVectorAwayFromEnd)
                        ? ref halfVectorAwayFromEnd
                        : ref currentSearchSpace;

                    Vector128<byte> source0 = Vector128.LoadUnsafe(ref firstVector);
                    Vector128<byte> source1 = Vector128.LoadUnsafe(ref halfVectorAwayFromEnd);
                    Vector256<byte> source = Vector256.Create(source0, source1);

                    Vector256<byte> result = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmap256));
                    if (result != Vector256<byte>.Zero)
                    {
                        return ComputeFirstIndexOverlapped<byte, TNegator>(ref searchSpace, ref firstVector, ref halfVectorAwayFromEnd, result);
                    }
                }

                return -1;
            }

            Vector128<byte> bitmap = bitmapRef._lower;

            if (!Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
            {
                // Process the input in chunks of 16 bytes.
                // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                // Let the fallback below handle it instead. This is why the condition is
                // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                ref byte vectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<byte>.Count);

                do
                {
                    Vector128<byte> source = Vector128.LoadUnsafe(ref currentSearchSpace);

                    Vector128<byte> result = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmap));
                    if (result != Vector128<byte>.Zero)
                    {
                        return ComputeFirstIndex<byte, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                    }

                    currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<byte>.Count);
                }
                while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref vectorAwayFromEnd));
            }

            // We have 1-16 bytes remaining. Process the first and last half vectors in the search space.
            // They may overlap, but we'll handle that in the index calculation if we do get a match.
            Debug.Assert(searchSpaceLength >= sizeof(ulong), "We expect that the input is long enough for us to load a ulong.");
            {
                ref byte halfVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - sizeof(ulong));

                ref byte firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref halfVectorAwayFromEnd)
                    ? ref halfVectorAwayFromEnd
                    : ref currentSearchSpace;

                ulong source0 = Unsafe.ReadUnaligned<ulong>(ref firstVector);
                ulong source1 = Unsafe.ReadUnaligned<ulong>(ref halfVectorAwayFromEnd);
                Vector128<byte> source = Vector128.Create(source0, source1).AsByte();

                Vector128<byte> result = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmap));
                if (result != Vector128<byte>.Zero)
                {
                    return ComputeFirstIndexOverlapped<byte, TNegator>(ref searchSpace, ref firstVector, ref halfVectorAwayFromEnd, result);
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal static int LastIndexOfAnyVectorized<TNegator>(ref byte searchSpace, int searchSpaceLength, ref Vector256<byte> bitmapRef)
            where TNegator : struct, INegator
        {
            ref byte currentSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceLength);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The behavior of the rest of the function remains the same if Avx2.IsSupported is false
            if (Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                Vector256<byte> bitmap256 = bitmapRef;

                if (searchSpaceLength > Vector256<byte>.Count)
                {
                    // Process the input in chunks of 32 bytes.
                    // If the input length is a multiple of 32, don't consume the last 32 characters in this loop.
                    // Let the fallback below handle it instead. This is why the condition is
                    // ">" instead of ">=" above, and why "IsAddressGreaterThan" is used instead of "!IsAddressLessThan".
                    ref byte vectorAfterStart = ref Unsafe.Add(ref searchSpace, Vector256<byte>.Count);

                    do
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector256<byte>.Count);

                        Vector256<byte> source = Vector256.LoadUnsafe(ref currentSearchSpace);

                        Vector256<byte> result = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmap256));
                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeLastIndex<byte, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                        }
                    }
                    while (Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref vectorAfterStart));
                }

                // We have 1-32 bytes remaining. Process the first and last half vectors in the search space.
                // They may overlap, but we'll handle that in the index calculation if we do get a match.
                Debug.Assert(searchSpaceLength >= Vector128<byte>.Count, "We expect that the input is long enough for us to load a Vector128.");
                {
                    ref byte halfVectorAfterStart = ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count);

                    ref byte secondVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref halfVectorAfterStart)
                        ? ref Unsafe.Subtract(ref currentSearchSpace, Vector128<byte>.Count)
                        : ref searchSpace;

                    Vector128<byte> source0 = Vector128.LoadUnsafe(ref searchSpace);
                    Vector128<byte> source1 = Vector128.LoadUnsafe(ref secondVector);
                    Vector256<byte> source = Vector256.Create(source0, source1);

                    Vector256<byte> result = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmap256));
                    if (result != Vector256<byte>.Zero)
                    {
                        return ComputeLastIndexOverlapped<byte, TNegator>(ref searchSpace, ref secondVector, result);
                    }
                }

                return -1;
            }

            Vector128<byte> bitmap = bitmapRef._lower;

            if (!Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
            {
                // Process the input in chunks of 16 bytes.
                // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                // Let the fallback below handle it instead. This is why the condition is
                // ">" instead of ">=" above, and why "IsAddressGreaterThan" is used instead of "!IsAddressLessThan".
                ref byte vectorAfterStart = ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count);

                do
                {
                    currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<byte>.Count);

                    Vector128<byte> source = Vector128.LoadUnsafe(ref currentSearchSpace);

                    Vector128<byte> result = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmap));
                    if (result != Vector128<byte>.Zero)
                    {
                        return ComputeLastIndex<byte, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                    }
                }
                while (Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref vectorAfterStart));
            }

            // We have 1-16 bytes remaining. Process the first and last half vectors in the search space.
            // They may overlap, but we'll handle that in the index calculation if we do get a match.
            Debug.Assert(searchSpaceLength >= sizeof(ulong), "We expect that the input is long enough for us to load a ulong.");
            {
                ref byte halfVectorAfterStart = ref Unsafe.Add(ref searchSpace, sizeof(ulong));

                ref byte secondVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref halfVectorAfterStart)
                    ? ref Unsafe.Subtract(ref currentSearchSpace, sizeof(ulong))
                    : ref searchSpace;

                ulong source0 = Unsafe.ReadUnaligned<ulong>(ref searchSpace);
                ulong source1 = Unsafe.ReadUnaligned<ulong>(ref secondVector);
                Vector128<byte> source = Vector128.Create(source0, source1).AsByte();

                Vector128<byte> result = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmap));
                if (result != Vector128<byte>.Zero)
                {
                    return ComputeLastIndexOverlapped<byte, TNegator>(ref searchSpace, ref secondVector, result);
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal static int IndexOfAnyVectorizedAnyByte<TNegator>(ref byte searchSpace, int searchSpaceLength, ref Vector512<byte> bitmapsRef)
            where TNegator : struct, INegator
        {
            ref byte currentSearchSpace = ref searchSpace;

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The behavior of the rest of the function remains the same if Avx2.IsSupported is false
            if (Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                Vector256<byte> bitmap256_0 = bitmapsRef._lower;
                Vector256<byte> bitmap256_1 = bitmapsRef._upper;

                if (searchSpaceLength > Vector256<byte>.Count)
                {
                    // Process the input in chunks of 32 bytes.
                    // If the input length is a multiple of 32, don't consume the last 32 characters in this loop.
                    // Let the fallback below handle it instead. This is why the condition is
                    // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                    ref byte vectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector256<byte>.Count);

                    do
                    {
                        Vector256<byte> source = Vector256.LoadUnsafe(ref currentSearchSpace);

                        Vector256<byte> result = IndexOfAnyLookup<TNegator>(source, bitmap256_0, bitmap256_1);
                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeFirstIndex<byte, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                        }

                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<byte>.Count);
                    }
                    while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref vectorAwayFromEnd));
                }

                // We have 1-32 bytes remaining. Process the first and last half vectors in the search space.
                // They may overlap, but we'll handle that in the index calculation if we do get a match.
                Debug.Assert(searchSpaceLength >= Vector128<byte>.Count, "We expect that the input is long enough for us to load a Vector128.");
                {
                    ref byte halfVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<byte>.Count);

                    ref byte firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref halfVectorAwayFromEnd)
                        ? ref halfVectorAwayFromEnd
                        : ref currentSearchSpace;

                    Vector128<byte> source0 = Vector128.LoadUnsafe(ref firstVector);
                    Vector128<byte> source1 = Vector128.LoadUnsafe(ref halfVectorAwayFromEnd);
                    Vector256<byte> source = Vector256.Create(source0, source1);

                    Vector256<byte> result = IndexOfAnyLookup<TNegator>(source, bitmap256_0, bitmap256_1);
                    if (result != Vector256<byte>.Zero)
                    {
                        return ComputeFirstIndexOverlapped<byte, TNegator>(ref searchSpace, ref firstVector, ref halfVectorAwayFromEnd, result);
                    }
                }

                return -1;
            }

            Vector128<byte> bitmap0 = bitmapsRef._lower._lower;
            Vector128<byte> bitmap1 = bitmapsRef._upper._lower;

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The behavior of the rest of the function remains the same if Avx2.IsSupported is false
            if (!Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                // Process the input in chunks of 16 bytes.
                // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                // Let the fallback below handle it instead. This is why the condition is
                // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                ref byte vectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<byte>.Count);

                do
                {
                    Vector128<byte> source = Vector128.LoadUnsafe(ref currentSearchSpace);

                    Vector128<byte> result = IndexOfAnyLookup<TNegator>(source, bitmap0, bitmap1);
                    if (result != Vector128<byte>.Zero)
                    {
                        return ComputeFirstIndex<byte, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                    }

                    currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<byte>.Count);
                }
                while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref vectorAwayFromEnd));
            }

            // We have 1-16 bytes remaining. Process the first and last half vectors in the search space.
            // They may overlap, but we'll handle that in the index calculation if we do get a match.
            Debug.Assert(searchSpaceLength >= sizeof(ulong), "We expect that the input is long enough for us to load a ulong.");
            {
                ref byte halfVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength - sizeof(ulong));

                ref byte firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref halfVectorAwayFromEnd)
                    ? ref halfVectorAwayFromEnd
                    : ref currentSearchSpace;

                ulong source0 = Unsafe.ReadUnaligned<ulong>(ref firstVector);
                ulong source1 = Unsafe.ReadUnaligned<ulong>(ref halfVectorAwayFromEnd);
                Vector128<byte> source = Vector128.Create(source0, source1).AsByte();

                Vector128<byte> result = IndexOfAnyLookup<TNegator>(source, bitmap0, bitmap1);
                if (result != Vector128<byte>.Zero)
                {
                    return ComputeFirstIndexOverlapped<byte, TNegator>(ref searchSpace, ref firstVector, ref halfVectorAwayFromEnd, result);
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        internal static int LastIndexOfAnyVectorizedAnyByte<TNegator>(ref byte searchSpace, int searchSpaceLength, ref Vector512<byte> bitmapsRef)
            where TNegator : struct, INegator
        {
            ref byte currentSearchSpace = ref Unsafe.Add(ref searchSpace, searchSpaceLength);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The behavior of the rest of the function remains the same if Avx2.IsSupported is false
            if (Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
            {
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                Vector256<byte> bitmap256_0 = bitmapsRef._lower;
                Vector256<byte> bitmap256_1 = bitmapsRef._upper;

                if (searchSpaceLength > Vector256<byte>.Count)
                {
                    // Process the input in chunks of 32 bytes.
                    // If the input length is a multiple of 32, don't consume the last 32 characters in this loop.
                    // Let the fallback below handle it instead. This is why the condition is
                    // ">" instead of ">=" above, and why "IsAddressGreaterThan" is used instead of "!IsAddressLessThan".
                    ref byte vectorAfterStart = ref Unsafe.Add(ref searchSpace, Vector256<byte>.Count);

                    do
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector256<byte>.Count);

                        Vector256<byte> source = Vector256.LoadUnsafe(ref currentSearchSpace);

                        Vector256<byte> result = IndexOfAnyLookup<TNegator>(source, bitmap256_0, bitmap256_1);
                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeLastIndex<byte, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                        }
                    }
                    while (Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref vectorAfterStart));
                }

                // We have 1-32 bytes remaining. Process the first and last half vectors in the search space.
                // They may overlap, but we'll handle that in the index calculation if we do get a match.
                Debug.Assert(searchSpaceLength >= Vector128<byte>.Count, "We expect that the input is long enough for us to load a Vector128.");
                {
                    ref byte halfVectorAfterStart = ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count);

                    ref byte secondVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref halfVectorAfterStart)
                        ? ref Unsafe.Subtract(ref currentSearchSpace, Vector128<byte>.Count)
                        : ref searchSpace;

                    Vector128<byte> source0 = Vector128.LoadUnsafe(ref searchSpace);
                    Vector128<byte> source1 = Vector128.LoadUnsafe(ref secondVector);
                    Vector256<byte> source = Vector256.Create(source0, source1);

                    Vector256<byte> result = IndexOfAnyLookup<TNegator>(source, bitmap256_0, bitmap256_1);
                    if (result != Vector256<byte>.Zero)
                    {
                        return ComputeLastIndexOverlapped<byte, TNegator>(ref searchSpace, ref secondVector, result);
                    }
                }

                return -1;
            }

            Vector128<byte> bitmap0 = bitmapsRef._lower._lower;
            Vector128<byte> bitmap1 = bitmapsRef._upper._lower;

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The behavior of the rest of the function remains the same if Avx2.IsSupported is false
            if (!Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                // Process the input in chunks of 16 bytes.
                // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                // Let the fallback below handle it instead. This is why the condition is
                // ">" instead of ">=" above, and why "IsAddressGreaterThan" is used instead of "!IsAddressLessThan".
                ref byte vectorAfterStart = ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count);

                do
                {
                    currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<byte>.Count);

                    Vector128<byte> source = Vector128.LoadUnsafe(ref currentSearchSpace);

                    Vector128<byte> result = IndexOfAnyLookup<TNegator>(source, bitmap0, bitmap1);
                    if (result != Vector128<byte>.Zero)
                    {
                        return ComputeLastIndex<byte, TNegator>(ref searchSpace, ref currentSearchSpace, result);
                    }
                }
                while (Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref vectorAfterStart));
            }

            // We have 1-16 bytes remaining. Process the first and last half vectors in the search space.
            // They may overlap, but we'll handle that in the index calculation if we do get a match.
            Debug.Assert(searchSpaceLength >= sizeof(ulong), "We expect that the input is long enough for us to load a ulong.");
            {
                ref byte halfVectorAfterStart = ref Unsafe.Add(ref searchSpace, sizeof(ulong));

                ref byte secondVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref halfVectorAfterStart)
                    ? ref Unsafe.Subtract(ref currentSearchSpace, sizeof(ulong))
                    : ref searchSpace;

                ulong source0 = Unsafe.ReadUnaligned<ulong>(ref searchSpace);
                ulong source1 = Unsafe.ReadUnaligned<ulong>(ref secondVector);
                Vector128<byte> source = Vector128.Create(source0, source1).AsByte();

                Vector128<byte> result = IndexOfAnyLookup<TNegator>(source, bitmap0, bitmap1);
                if (result != Vector128<byte>.Zero)
                {
                    return ComputeLastIndexOverlapped<byte, TNegator>(ref searchSpace, ref secondVector, result);
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        private static Vector128<byte> IndexOfAnyLookup<TNegator, TOptimizations>(Vector128<short> source0, Vector128<short> source1, Vector128<byte> bitmapLookup)
            where TNegator : struct, INegator
            where TOptimizations : struct, IOptimizations
        {
            Vector128<byte> source = TOptimizations.PackSources(source0.AsUInt16(), source1.AsUInt16());

            Vector128<byte> result = IndexOfAnyLookupCore(source, bitmapLookup);

            return TNegator.NegateIfNeeded(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        private static Vector128<byte> IndexOfAnyLookupCore(Vector128<byte> source, Vector128<byte> bitmapLookup)
        {
            // On X86, the Ssse3.Shuffle instruction will already perform an implicit 'AND 0xF' on the indices, so we can skip it.
            // For values above 127, Ssse3.Shuffle will also set the result to 0. This is fine as we don't want non-ASCII values to match anyway.
            Vector128<byte> lowNibbles = Ssse3.IsSupported
                ? source
                : source & Vector128.Create((byte)0xF);

            // On ARM, we have an instruction for an arithmetic right shift of 1-byte signed values.
            // The shift will map values above 127 to values above 16, which the shuffle will then map to 0.
            // On X86 and WASM, use a logical right shift instead.
            Vector128<byte> highNibbles = AdvSimd.IsSupported
                ? AdvSimd.ShiftRightArithmetic(source.AsSByte(), 4).AsByte()
                : source >>> 4;

            // The bitmapLookup represents a 8x16 table of bits, indicating whether a character is present in the needle.
            // Lookup the rows via the lower nibble and the column via the higher nibble.
            Vector128<byte> bitMask = Vector128.ShuffleUnsafe(bitmapLookup, lowNibbles);

            // For values above 127, the high nibble will be above 7. We construct the positions vector for the shuffle such that those values map to 0.
            Vector128<byte> bitPositions = Vector128.ShuffleUnsafe(Vector128.Create(0x8040201008040201, 0).AsByte(), highNibbles);

            Vector128<byte> result = bitMask & bitPositions;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> IndexOfAnyLookup<TNegator, TOptimizations>(Vector256<short> source0, Vector256<short> source1, Vector256<byte> bitmapLookup)
            where TNegator : struct, INegator
            where TOptimizations : struct, IOptimizations
        {
            Vector256<byte> source = TOptimizations.PackSources(source0.AsUInt16(), source1.AsUInt16());

            Vector256<byte> result = IndexOfAnyLookupCore(source, bitmapLookup);

            return TNegator.NegateIfNeeded(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> IndexOfAnyLookupCore(Vector256<byte> source, Vector256<byte> bitmapLookup)
        {
            // See comments in IndexOfAnyLookupCore(Vector128<byte>) above for more details.
            Vector256<byte> highNibbles = source >>> 4;
            Vector256<byte> bitMask = Avx2.Shuffle(bitmapLookup, source);
            Vector256<byte> bitPositions = Avx2.Shuffle(Vector256.Create(0x8040201008040201).AsByte(), highNibbles);
            Vector256<byte> result = bitMask & bitPositions;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        private static Vector128<byte> IndexOfAnyLookup<TNegator>(Vector128<byte> source, Vector128<byte> bitmapLookup0, Vector128<byte> bitmapLookup1)
            where TNegator : struct, INegator
        {
            // http://0x80.pl/articles/simd-byte-lookup.html#universal-algorithm

            Vector128<byte> lowNibbles = source & Vector128.Create((byte)0xF);
            Vector128<byte> highNibbles = Vector128.ShiftRightLogical(source.AsInt32(), 4).AsByte() & Vector128.Create((byte)0xF);

            Vector128<byte> row0 = Vector128.ShuffleUnsafe(bitmapLookup0, lowNibbles);
            Vector128<byte> row1 = Vector128.ShuffleUnsafe(bitmapLookup1, lowNibbles);

            Vector128<byte> bitmask = Vector128.ShuffleUnsafe(Vector128.Create(0x8040201008040201).AsByte(), highNibbles);

            Vector128<byte> mask = Vector128.GreaterThan(highNibbles.AsSByte(), Vector128.Create((sbyte)0x7)).AsByte();
            Vector128<byte> bitsets = Vector128.ConditionalSelect(mask, row1, row0);

            Vector128<byte> result = Vector128.Equals(bitsets & bitmask, bitmask);

            return TNegator.NegateIfNeeded(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> IndexOfAnyLookup<TNegator>(Vector256<byte> source, Vector256<byte> bitmapLookup0, Vector256<byte> bitmapLookup1)
            where TNegator : struct, INegator
        {
            // http://0x80.pl/articles/simd-byte-lookup.html#universal-algorithm

            Vector256<byte> lowNibbles = source & Vector256.Create((byte)0xF);
            Vector256<byte> highNibbles = Vector256.ShiftRightLogical(source.AsInt32(), 4).AsByte() & Vector256.Create((byte)0xF);

            Vector256<byte> row0 = Avx2.Shuffle(bitmapLookup0, lowNibbles);
            Vector256<byte> row1 = Avx2.Shuffle(bitmapLookup1, lowNibbles);

            Vector256<byte> bitmask = Avx2.Shuffle(Vector256.Create(0x8040201008040201).AsByte(), highNibbles);

            Vector256<byte> mask = Vector256.GreaterThan(highNibbles.AsSByte(), Vector256.Create((sbyte)0x7)).AsByte();
            Vector256<byte> bitsets = Vector256.ConditionalSelect(mask, row1, row0);

            Vector256<byte> result = Vector256.Equals(bitsets & bitmask, bitmask);

            return TNegator.NegateIfNeeded(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndex<T, TNegator>(ref T searchSpace, ref T current, Vector128<byte> result)
            where TNegator : struct, INegator
        {
            uint mask = TNegator.ExtractMask(result);
            int offsetInVector = BitOperations.TrailingZeroCount(mask);
            return offsetInVector + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndexOverlapped<T, TNegator>(ref T searchSpace, ref T current0, ref T current1, Vector128<byte> result)
            where TNegator : struct, INegator
        {
            uint mask = TNegator.ExtractMask(result);
            int offsetInVector = BitOperations.TrailingZeroCount(mask);
            if (offsetInVector >= Vector128<short>.Count)
            {
                // We matched within the second vector
                current0 = ref current1;
                offsetInVector -= Vector128<short>.Count;
            }
            return offsetInVector + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeLastIndex<T, TNegator>(ref T searchSpace, ref T current, Vector128<byte> result)
            where TNegator : struct, INegator
        {
            uint mask = TNegator.ExtractMask(result) & 0xFFFF;
            int offsetInVector = 31 - BitOperations.LeadingZeroCount(mask);
            return offsetInVector + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeLastIndexOverlapped<T, TNegator>(ref T searchSpace, ref T secondVector, Vector128<byte> result)
            where TNegator : struct, INegator
        {
            uint mask = TNegator.ExtractMask(result) & 0xFFFF;
            int offsetInVector = 31 - BitOperations.LeadingZeroCount(mask);
            if (offsetInVector < Vector128<short>.Count)
            {
                return offsetInVector;
            }

            // We matched within the second vector
            return offsetInVector - Vector128<short>.Count + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref secondVector) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static unsafe int ComputeFirstIndex<T, TNegator>(ref T searchSpace, ref T current, Vector256<byte> result)
            where TNegator : struct, INegator
        {
            if (typeof(T) == typeof(short))
            {
                result = FixUpPackedVector256Result(result);
            }

            uint mask = TNegator.ExtractMask(result);

            int offsetInVector = BitOperations.TrailingZeroCount(mask);
            return offsetInVector + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static unsafe int ComputeFirstIndexOverlapped<T, TNegator>(ref T searchSpace, ref T current0, ref T current1, Vector256<byte> result)
            where TNegator : struct, INegator
        {
            if (typeof(T) == typeof(short))
            {
                result = FixUpPackedVector256Result(result);
            }

            uint mask = TNegator.ExtractMask(result);

            int offsetInVector = BitOperations.TrailingZeroCount(mask);
            if (offsetInVector >= Vector256<short>.Count)
            {
                // We matched within the second vector
                current0 = ref current1;
                offsetInVector -= Vector256<short>.Count;
            }
            return offsetInVector + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static unsafe int ComputeLastIndex<T, TNegator>(ref T searchSpace, ref T current, Vector256<byte> result)
            where TNegator : struct, INegator
        {
            if (typeof(T) == typeof(short))
            {
                result = FixUpPackedVector256Result(result);
            }

            uint mask = TNegator.ExtractMask(result);

            int offsetInVector = 31 - BitOperations.LeadingZeroCount(mask);
            return offsetInVector + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static unsafe int ComputeLastIndexOverlapped<T, TNegator>(ref T searchSpace, ref T secondVector, Vector256<byte> result)
            where TNegator : struct, INegator
        {
            if (typeof(T) == typeof(short))
            {
                result = FixUpPackedVector256Result(result);
            }

            uint mask = TNegator.ExtractMask(result);

            int offsetInVector = 31 - BitOperations.LeadingZeroCount(mask);
            if (offsetInVector < Vector256<short>.Count)
            {
                return offsetInVector;
            }

            // We matched within the second vector
            return offsetInVector - Vector256<short>.Count + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref secondVector) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> FixUpPackedVector256Result(Vector256<byte> result)
        {
            Debug.Assert(Avx2.IsSupported);
            // Avx2.PackUnsignedSaturate(Vector256.Create((short)1), Vector256.Create((short)2)) will result in
            // 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2
            // We want to swap the X and Y bits
            // 1, 1, 1, 1, 1, 1, 1, 1, X, X, X, X, X, X, X, X, Y, Y, Y, Y, Y, Y, Y, Y, 2, 2, 2, 2, 2, 2, 2, 2
            return Avx2.Permute4x64(result.AsInt64(), 0b_11_01_10_00).AsByte();
        }

        internal interface INegator
        {
            static abstract bool NegateIfNeeded(bool result);
            static abstract Vector128<byte> NegateIfNeeded(Vector128<byte> result);
            static abstract Vector256<byte> NegateIfNeeded(Vector256<byte> result);
            static abstract uint ExtractMask(Vector128<byte> result);
            static abstract uint ExtractMask(Vector256<byte> result);
        }

        internal readonly struct DontNegate : INegator
        {
            public static bool NegateIfNeeded(bool result) => result;
            public static Vector128<byte> NegateIfNeeded(Vector128<byte> result) => result;
            public static Vector256<byte> NegateIfNeeded(Vector256<byte> result) => result;
            public static uint ExtractMask(Vector128<byte> result) => ~Vector128.Equals(result, Vector128<byte>.Zero).ExtractMostSignificantBits();
            public static uint ExtractMask(Vector256<byte> result) => ~Vector256.Equals(result, Vector256<byte>.Zero).ExtractMostSignificantBits();
        }

        internal readonly struct Negate : INegator
        {
            public static bool NegateIfNeeded(bool result) => !result;
            // This is intentionally testing for equality with 0 instead of "~result".
            // We want to know if any character didn't match, as that means it should be treated as a match for the -Except method.
            public static Vector128<byte> NegateIfNeeded(Vector128<byte> result) => Vector128.Equals(result, Vector128<byte>.Zero);
            public static Vector256<byte> NegateIfNeeded(Vector256<byte> result) => Vector256.Equals(result, Vector256<byte>.Zero);
            public static uint ExtractMask(Vector128<byte> result) => result.ExtractMostSignificantBits();
            public static uint ExtractMask(Vector256<byte> result) => result.ExtractMostSignificantBits();
        }

        internal interface IOptimizations
        {
            // Pack two vectors of characters into bytes.
            // X86 and WASM when the needle does not contain 0:  Downcast every character using saturation.
            // - Values <= 32767 result in min(value, 255).
            // - Values  > 32767 result in 0. Because of this we must do more work to handle needles that contain 0.
            // Otherwise: Do narrowing saturation over unsigned values.
            // - All values result in min(value, 255)
            static abstract Vector128<byte> PackSources(Vector128<ushort> lower, Vector128<ushort> upper);
            static abstract Vector256<byte> PackSources(Vector256<ushort> lower, Vector256<ushort> upper);
        }

        internal readonly struct Ssse3AndWasmHandleZeroInNeedle : IOptimizations
        {
            // Replace with Vector128.NarrowWithSaturation once https://github.com/dotnet/runtime/issues/75724 is implemented.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Sse2))]
            [CompExactlyDependsOn(typeof(PackedSimd))]
            public static Vector128<byte> PackSources(Vector128<ushort> lower, Vector128<ushort> upper)
            {
                Vector128<short> lowerMin = Vector128.Min(lower, Vector128.Create((ushort)255)).AsInt16();
                Vector128<short> upperMin = Vector128.Min(upper, Vector128.Create((ushort)255)).AsInt16();

                return Sse2.IsSupported
                    ? Sse2.PackUnsignedSaturate(lowerMin, upperMin)
                    : PackedSimd.ConvertNarrowingSaturateUnsigned(lowerMin, upperMin);
            }

            // Replace with Vector256.NarrowWithSaturation once https://github.com/dotnet/runtime/issues/75724 is implemented.
            [CompExactlyDependsOn(typeof(Avx2))]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<byte> PackSources(Vector256<ushort> lower, Vector256<ushort> upper)
            {
                return Avx2.PackUnsignedSaturate(
                    Vector256.Min(lower, Vector256.Create((ushort)255)).AsInt16(),
                    Vector256.Min(upper, Vector256.Create((ushort)255)).AsInt16());
            }
        }

        internal readonly struct Default : IOptimizations
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Sse2))]
            [CompExactlyDependsOn(typeof(AdvSimd))]
            [CompExactlyDependsOn(typeof(PackedSimd))]
            public static Vector128<byte> PackSources(Vector128<ushort> lower, Vector128<ushort> upper)
            {
                return
                    Sse2.IsSupported ? Sse2.PackUnsignedSaturate(lower.AsInt16(), upper.AsInt16()) :
                    AdvSimd.IsSupported ? AdvSimd.ExtractNarrowingSaturateUpper(AdvSimd.ExtractNarrowingSaturateLower(lower), upper) :
                    PackedSimd.ConvertNarrowingSaturateUnsigned(lower.AsInt16(), upper.AsInt16());
            }

            [CompExactlyDependsOn(typeof(Avx2))]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<byte> PackSources(Vector256<ushort> lower, Vector256<ushort> upper)
            {
                return Avx2.PackUnsignedSaturate(lower.AsInt16(), upper.AsInt16());
            }
        }
    }
}
