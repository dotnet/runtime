// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    internal sealed class ProbabilisticWithAsciiCharSearchValues<TOptimizations> : SearchValues<char>
        where TOptimizations : struct, IndexOfAnyAsciiSearcher.IOptimizations
    {
        private Vector256<byte> _asciiBitmap;
        private Vector256<byte> _inverseAsciiBitmap;
        private ProbabilisticMap _map;
        private readonly string _values;

        public ProbabilisticWithAsciiCharSearchValues(scoped ReadOnlySpan<char> values)
        {
            Debug.Assert(IndexOfAnyAsciiSearcher.IsVectorizationSupported);
            Debug.Assert(values.ContainsAnyInRange((char)0, (char)127));

            IndexOfAnyAsciiSearcher.ComputeBitmap(values, out _asciiBitmap, out _);
            _inverseAsciiBitmap = ~_asciiBitmap;

            _values = new string(values);
            _map = new ProbabilisticMap(_values);
        }

        internal override char[] GetValues() => _values.ToCharArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            ProbabilisticMap.Contains(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), _values, value);

        internal override int IndexOfAny(ReadOnlySpan<char> span)
        {
            int offset = 0;

            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && span.Length >= Vector128<short>.Count)
            {
                // We are using IndexOfAnyAsciiSearcher to search for the first ASCII character in the set, or any non-ASCII character.
                // We do this by inverting the bitmap and using the opposite search function (Negate instead of DontNegate).

                // As we're using the inverse bitmap, we have to make sure to use the correct IOptimizations implementation
                if ((Ssse3.IsSupported || PackedSimd.IsSupported) && typeof(TOptimizations) == typeof(IndexOfAnyAsciiSearcher.Default))
                {
                    offset = IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(
                        ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref _inverseAsciiBitmap);
                }
                else
                {
                    offset = IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Default>(
                        ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref _inverseAsciiBitmap);
                }

                // If we've reached the end of the span or stopped at an ASCII character, we've found the result.
                if ((uint)offset >= (uint)span.Length || char.IsAscii(span[offset]))
                {
                    return offset;
                }

                // Fall back to using the ProbabilisticMap.
                span = span.Slice(offset);
            }

            int index = ProbabilisticMap.IndexOfAny(
                ref Unsafe.As<ProbabilisticMap, uint>(ref _map),
                ref MemoryMarshal.GetReference(span),
                span.Length,
                _values);

            if (index >= 0)
            {
                // We found a match. Account for the number of ASCII characters we've skipped previously.
                index += offset;
            }

            return index;
        }

        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span)
        {
            int offset = 0;

            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && span.Length >= Vector128<short>.Count)
            {
                // Do a regular IndexOfAnyExcept for the ASCII characters. The search will stop if we encounter a non-ASCII char.
                offset = IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, TOptimizations>(
                    ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref _asciiBitmap);

                // If we've reached the end of the span or stopped at an ASCII character, we've found the result.
                if ((uint)offset >= (uint)span.Length || char.IsAscii(span[offset]))
                {
                    return offset;
                }

                // Fall back to a simple char-by-char search.
                span = span.Slice(offset);
            }

            int index = ProbabilisticMap.IndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(
                ref MemoryMarshal.GetReference(span),
                span.Length,
                _values);

            if (index >= 0)
            {
                // We found a match. Account for the number of ASCII characters we've skipped previously.
                index += offset;
            }

            return index;
        }

        internal override int LastIndexOfAny(ReadOnlySpan<char> span)
        {
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && span.Length >= Vector128<short>.Count)
            {
                // We are using IndexOfAnyAsciiSearcher to search for the last ASCII character in the set, or any non-ASCII character.
                // We do this by inverting the bitmap and using the opposite search function (Negate instead of DontNegate).

                int offset;

                // As we're using the inverse bitmap, we have to make sure to use the correct IOptimizations implementation
                if ((Ssse3.IsSupported || PackedSimd.IsSupported) && typeof(TOptimizations) == typeof(IndexOfAnyAsciiSearcher.Default))
                {
                    offset = IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(
                        ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref _inverseAsciiBitmap);
                }
                else
                {
                    offset = IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Default>(
                        ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                        span.Length,
                        ref _inverseAsciiBitmap);
                }

                // If we've reached the end of the span or stopped at an ASCII character, we've found the result.
                if ((uint)offset >= (uint)span.Length || char.IsAscii(span[offset]))
                {
                    return offset;
                }

                // Fall back to using the ProbabilisticMap.
                span = span.Slice(0, offset + 1);
            }

            return ProbabilisticMap.LastIndexOfAny(
                ref Unsafe.As<ProbabilisticMap, uint>(ref _map),
                ref MemoryMarshal.GetReference(span),
                span.Length,
                _values);
        }

        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span)
        {
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && span.Length >= Vector128<short>.Count)
            {
                // Do a regular LastIndexOfAnyExcept for the ASCII characters. The search will stop if we encounter a non-ASCII char.
                int offset = IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, TOptimizations>(
                    ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)),
                    span.Length,
                    ref _asciiBitmap);

                // If we've reached the end of the span or stopped at an ASCII character, we've found the result.
                if ((uint)offset >= (uint)span.Length || char.IsAscii(span[offset]))
                {
                    return offset;
                }

                // Fall back to a simple char-by-char search.
                span = span.Slice(0, offset + 1);
            }

            return ProbabilisticMap.LastIndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(
                ref MemoryMarshal.GetReference(span),
                span.Length,
                _values);
        }
    }
}
