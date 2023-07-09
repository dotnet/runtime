// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers
{
    /// <summary>
    /// An implementation of the Aho-Corasick algorithm we use as a fallback when we can't use Teddy
    /// (either due to missing hardware intrinsics, or due to characteristics of the values used).
    /// https://en.wikipedia.org/wiki/Aho%E2%80%93Corasick_algorithm
    /// </summary>
    internal readonly struct AhoCorasick
    {
        private readonly AhoCorasickNode[] _nodes;
        private readonly Vector256<byte> _startingCharsAsciiBitmap;

        public AhoCorasick(AhoCorasickNode[] nodes, Vector256<byte> startingAsciiBitmap)
        {
            _nodes = nodes;
            _startingCharsAsciiBitmap = startingAsciiBitmap;
        }

        public readonly bool ShouldUseAsciiFastScan
        {
            get
            {
                Vector256<byte> bitmap = _startingCharsAsciiBitmap;

                if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && bitmap != default)
                {
                    // If there are a lot of starting characters such that we often find one early,
                    // the ASCII fast scan may end up performing worse than checking one character at a time.
                    // Avoid using this optimization if the combined frequency of starting chars is too high.

                    // Combined frequency of characters based on CharacterFrequencyHelper.AsciiFrequency:
                    // - All digits is ~ 5 %
                    // - All lowercase letters is ~ 57.2 %
                    // - All uppercase letters is ~ 7.4 %
                    const float MaxCombinedFrequency = 50f;

                    float frequency = 0;

                    for (int i = 0; i < 128; i++)
                    {
                        if (IndexOfAnyAsciiSearcher.BitmapContains(ref bitmap, (char)i))
                        {
                            frequency += CharacterFrequencyHelper.AsciiFrequency[i];
                        }
                    }

                    return frequency < MaxCombinedFrequency;
                }

                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOfAny<TCaseSensitivity, TFastScanVariant>(ReadOnlySpan<char> span)
            where TCaseSensitivity : struct, StringSearchValuesHelper.ICaseSensitivity
            where TFastScanVariant : struct, IFastScan
        {
            return typeof(TCaseSensitivity) == typeof(StringSearchValuesHelper.CaseInsensitiveUnicode)
                ? IndexOfAnyCaseInsensitiveUnicode<TFastScanVariant>(span)
                : IndexOfAnyCore<TCaseSensitivity, TFastScanVariant>(span);
        }

        private readonly int IndexOfAnyCore<TCaseSensitivity, TFastScanVariant>(ReadOnlySpan<char> span)
            where TCaseSensitivity : struct, StringSearchValuesHelper.ICaseSensitivity
            where TFastScanVariant : struct, IFastScan
        {
            Debug.Assert(typeof(TCaseSensitivity) != typeof(StringSearchValuesHelper.CaseInsensitiveUnicode));

            ref AhoCorasickNode nodes = ref MemoryMarshal.GetArrayDataReference(_nodes);
            int nodeIndex = 0;
            int result = -1;
            int i = 0;

        FastScan:
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && typeof(TFastScanVariant) == typeof(IndexOfAnyAsciiFastScan))
            {
                int remainingLength = span.Length - i;

                if (remainingLength >= Vector128<ushort>.Count)
                {
                    // If '\0' is one of the starting chars and we're running on Ssse3 hardware, this may return false-positives.
                    // False-positives here are okay, we'll just rule them out below. While we could flow the Ssse3AndWasmHandleZeroInNeedle
                    // generic through, we expect such values to be rare enough that introducing more code is not worth it.
                    int offset = IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<IndexOfAnyAsciiSearcher.DontNegate, IndexOfAnyAsciiSearcher.Default>(
                        ref Unsafe.As<char, short>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), i)),
                        remainingLength,
                        ref Unsafe.AsRef(_startingCharsAsciiBitmap));

                    if (offset < 0)
                    {
                        goto Return;
                    }

                    i += offset;
                    goto LoopWithoutRangeCheck;
                }
            }

        Loop:
            if ((uint)i >= (uint)span.Length)
            {
                goto Return;
            }

        LoopWithoutRangeCheck:
            Debug.Assert(i < span.Length);
            char c = TCaseSensitivity.TransformInput(Unsafe.Add(ref MemoryMarshal.GetReference(span), i));

            while (true)
            {
                Debug.Assert((uint)nodeIndex < (uint)_nodes.Length);
                ref AhoCorasickNode node = ref Unsafe.Add(ref nodes, (uint)nodeIndex);

                if (node.TryGetChild(c, out int childIndex))
                {
                    nodeIndex = childIndex;

                    int matchLength = Unsafe.Add(ref nodes, (uint)nodeIndex).MatchLength;
                    if (matchLength != 0)
                    {
                        result = i + 1 - matchLength;
                    }

                    i++;
                    goto Loop;
                }

                if (nodeIndex == 0)
                {
                    if (result >= 0)
                    {
                        goto Return;
                    }

                    i++;
                    goto FastScan;
                }

                nodeIndex = node.SuffixLink;

                if (nodeIndex < 0)
                {
                    Debug.Assert(nodeIndex == -1);
                    Debug.Assert(result >= 0);
                    goto Return;
                }
            }

        Return:
            return result;
        }

        // Mostly a copy of IndexOfAnyCore, but we may read two characters at a time in the case of surrogate pairs.
        private readonly int IndexOfAnyCaseInsensitiveUnicode<TFastScanVariant>(ReadOnlySpan<char> span)
            where TFastScanVariant : struct, IFastScan
        {
            const char LowSurrogateNotSet = '\0';

            ref AhoCorasickNode nodes = ref MemoryMarshal.GetArrayDataReference(_nodes);
            int nodeIndex = 0;
            int result = -1;
            int i = 0;
            char lowSurrogateUpper = LowSurrogateNotSet;

        FastScan:
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && typeof(TFastScanVariant) == typeof(IndexOfAnyAsciiFastScan))
            {
                if (lowSurrogateUpper != LowSurrogateNotSet)
                {
                    goto LoopWithoutRangeCheck;
                }

                int remainingLength = span.Length - i;

                if (remainingLength >= Vector128<ushort>.Count)
                {
                    int offset = IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<IndexOfAnyAsciiSearcher.DontNegate, IndexOfAnyAsciiSearcher.Default>(
                        ref Unsafe.As<char, short>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), i)),
                        remainingLength,
                        ref Unsafe.AsRef(_startingCharsAsciiBitmap));

                    if (offset < 0)
                    {
                        goto Return;
                    }

                    i += offset;
                    goto LoopWithoutRangeCheck;
                }
            }

        Loop:
            if ((uint)i >= (uint)span.Length)
            {
                goto Return;
            }

        LoopWithoutRangeCheck:
            Debug.Assert(i < span.Length);
            char c;
            if (lowSurrogateUpper != LowSurrogateNotSet)
            {
                c = lowSurrogateUpper;
                lowSurrogateUpper = LowSurrogateNotSet;
            }
            else
            {
                c = Unsafe.Add(ref MemoryMarshal.GetReference(span), i);
                char lowSurrogate;

                if (char.IsHighSurrogate(c) &&
                    (uint)(i + 1) < (uint)span.Length &&
                    char.IsLowSurrogate(lowSurrogate = Unsafe.Add(ref MemoryMarshal.GetReference(span), i + 1)))
                {
                    if (GlobalizationMode.UseNls)
                    {
                        SurrogateToUpperNLS(c, lowSurrogate, out c, out lowSurrogateUpper);
                    }
                    else
                    {
                        SurrogateCasing.ToUpper(c, lowSurrogate, out c, out lowSurrogateUpper);
                    }

                    Debug.Assert(lowSurrogateUpper != LowSurrogateNotSet);
                }
                else
                {
                    c = TextInfo.ToUpperOrdinal(c);
                }

#if DEBUG
                // This logic must match Ordinal.ToUpperOrdinal exactly.
                Span<char> destination = new char[2]; // Avoid stackalloc in a loop
                Ordinal.ToUpperOrdinal(span.Slice(i, i + 1 == span.Length ? 1 : 2), destination);
                Debug.Assert(c == destination[0]);
                Debug.Assert(lowSurrogateUpper == LowSurrogateNotSet || lowSurrogateUpper == destination[1]);
#endif
            }

            while (true)
            {
                Debug.Assert((uint)nodeIndex < (uint)_nodes.Length);
                ref AhoCorasickNode node = ref Unsafe.Add(ref nodes, (uint)nodeIndex);

                if (node.TryGetChild(c, out int childIndex))
                {
                    nodeIndex = childIndex;

                    int matchLength = Unsafe.Add(ref nodes, (uint)nodeIndex).MatchLength;
                    if (matchLength != 0)
                    {
                        result = i + 1 - matchLength;
                    }

                    i++;
                    goto Loop;
                }

                if (nodeIndex == 0)
                {
                    if (result >= 0)
                    {
                        goto Return;
                    }

                    i++;
                    goto FastScan;
                }

                nodeIndex = node.SuffixLink;

                if (nodeIndex < 0)
                {
                    Debug.Assert(nodeIndex == -1);
                    Debug.Assert(result >= 0);
                    goto Return;
                }
            }

        Return:
            return result;
        }

        private static void SurrogateToUpperNLS(char h, char l, out char hr, out char lr)
        {
            Debug.Assert(char.IsHighSurrogate(h));
            Debug.Assert(char.IsLowSurrogate(l));

            Span<char> chars = stackalloc char[] { h, l };
            Span<char> destination = stackalloc char[2];

            int written = Ordinal.ToUpperOrdinal(chars, destination);
            Debug.Assert(written == 2);

            hr = destination[0];
            lr = destination[1];
        }

        public interface IFastScan { }

        public readonly struct IndexOfAnyAsciiFastScan : IFastScan { }

        public readonly struct NoFastScan : IFastScan { }
    }
}
