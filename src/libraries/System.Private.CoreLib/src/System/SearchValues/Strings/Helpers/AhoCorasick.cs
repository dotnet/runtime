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
    /// Works in O(n).
    /// </summary>
    internal readonly struct AhoCorasick
    {
        private readonly AhoCorasickNode[] _nodes;
        private readonly IndexOfAnyAsciiSearcher.AsciiState _startingAsciiChars;

        public AhoCorasick(AhoCorasickNode[] nodes, IndexOfAnyAsciiSearcher.AsciiState startingAsciiChars)
        {
            _nodes = nodes;
            _startingAsciiChars = startingAsciiChars;
        }

        public readonly bool ShouldUseAsciiFastScan
        {
            get
            {
                if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && _startingAsciiChars.Bitmap != default)
                {
                    // If there are a lot of starting characters such that we often find one early,
                    // the ASCII fast scan may end up performing worse than checking one character at a time.
                    // Avoid using this optimization if the combined frequency of starting chars is too high.
                    //
                    // For reference, the combined frequency of characters based on CharacterFrequencyHelper.AsciiFrequency:
                    // - All digits is ~ 5 %
                    // - All lowercase letters is ~ 57.2 %
                    // - All uppercase letters is ~ 7.4 %
                    //
                    // This limit is based on experimentation with different texts and sets of values.
                    // Above ~50 %, the cost of calling into the vectorized helper is higher than checking char by char on average.
                    const float MaxCombinedFrequency = 50f;

                    float frequency = 0;

                    for (int i = 0; i < 128; i++)
                    {
                        if (_startingAsciiChars.Lookup.Contains128((char)i))
                        {
                            frequency += CharacterFrequencyHelper.AsciiFrequency[i];
                        }
                    }

                    return frequency <= MaxCombinedFrequency;
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
            Debug.Assert(nodeIndex == 0);
            // We are currently in the root node and trying to find the next position of any starting character.
            // If all the values start with an ASCII character, use a vectorized helper to quickly skip over characters that can't start a match.
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && typeof(TFastScanVariant) == typeof(IndexOfAnyAsciiFastScan))
            {
                int remainingLength = span.Length - i;

                if (remainingLength >= Vector128<ushort>.Count)
                {
                    // If '\0' is one of the starting chars and we're running on Ssse3 hardware, this may return false-positives.
                    // False-positives here are okay, we'll just rule them out below. While we could flow the Ssse3AndWasmHandleZeroInNeedle
                    // generic through, we expect such values to be rare enough that introducing more code is not worth it.
                    int offset = IndexOfAnyAsciiSearcher.IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate, IndexOfAnyAsciiSearcher.Default>(
                        ref Unsafe.As<char, short>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), i)),
                        remainingLength,
                        ref Unsafe.AsRef(in _startingAsciiChars));

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
            // Read the next input character and either find the next potential match prefix or transition back to the root node.
            Debug.Assert((uint)i < (uint)span.Length);
            char c = TCaseSensitivity.TransformInput(Unsafe.Add(ref MemoryMarshal.GetReference(span), i));

            while (true)
            {
                Debug.Assert((uint)nodeIndex < (uint)_nodes.Length);
                ref AhoCorasickNode node = ref Unsafe.Add(ref nodes, (uint)nodeIndex);

                if (node.TryGetChild(c, out int childIndex))
                {
                    // We were able to extend the current match. If this node contains a potential match, remember that.
                    nodeIndex = childIndex;

                    Debug.Assert((uint)nodeIndex < (uint)_nodes.Length);
                    int matchLength = Unsafe.Add(ref nodes, (uint)nodeIndex).MatchLength;
                    if (matchLength != 0)
                    {
                        // Any result we find from here on out may only be lower (longer match with a start closer to the beginning of the input).
                        Debug.Assert(result == -1 || result >= i + 1 - matchLength);
                        result = i + 1 - matchLength;
                    }

                    i++;
                    goto Loop;
                }

                if (nodeIndex == 0)
                {
                    // We are back at the root node and none of the values start with the current character.
                    if (result >= 0)
                    {
                        // If we've already found a match, we can't find an earlier one anymore. This is the result
                        goto Return;
                    }

                    // Go back to searching for the next possible starting character.
                    i++;
                    goto FastScan;
                }

                // Follow the next suffix link.
                nodeIndex = node.SuffixLink;

                if (nodeIndex < 0)
                {
                    // A node with a suffix link of -1 indicates a match, see AhoCorasickBuilder.AddSuffixLinks.
                    Debug.Assert(nodeIndex == -1);
                    Debug.Assert(result >= 0);
                    goto Return;
                }

                // Try to match the current character again at the suffix link node.
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
            // We are currently in the root node and trying to find the next position of any starting character.
            // If all the values start with an ASCII character, use a vectorized helper to quickly skip over characters that can't start a match.
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && typeof(TFastScanVariant) == typeof(IndexOfAnyAsciiFastScan))
            {
                if (lowSurrogateUpper != LowSurrogateNotSet)
                {
                    // We read a surrogate pair in the previous loop iteration and processed the high surrogate.
                    // Continue with the stored low surrogate.
                    goto LoopWithoutRangeCheck;
                }

                int remainingLength = span.Length - i;

                if (remainingLength >= Vector128<ushort>.Count)
                {
                    int offset = IndexOfAnyAsciiSearcher.IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate, IndexOfAnyAsciiSearcher.Default>(
                        ref Unsafe.As<char, short>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), i)),
                        remainingLength,
                        ref Unsafe.AsRef(in _startingAsciiChars));

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
            // Read the next input character and either find the next potential match prefix or transition back to the root node.
            Debug.Assert((uint)i < (uint)span.Length);
            char c;
            if (lowSurrogateUpper != LowSurrogateNotSet)
            {
                // We have just processed the high surrogate. Continue with the low surrogate we read in the previous iteration.
                c = lowSurrogateUpper;
                lowSurrogateUpper = LowSurrogateNotSet;
            }
            else
            {
                // Read the next character, check if it's a high surrogate, and transform it to its Ordinal uppercase representation.
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
                // The above logic must match Ordinal.ToUpperOrdinal exactly.
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
                    // We were able to extend the current match. If this node contains a potential match, remember that.
                    nodeIndex = childIndex;

                    Debug.Assert((uint)nodeIndex < (uint)_nodes.Length);
                    int matchLength = Unsafe.Add(ref nodes, (uint)nodeIndex).MatchLength;
                    if (matchLength != 0)
                    {
                        // Any result we find from here on out may only be lower (longer match with a start closer to the beginning of the input).
                        Debug.Assert(result == -1 || result >= i + 1 - matchLength);
                        result = i + 1 - matchLength;
                    }

                    i++;
                    goto Loop;
                }

                if (nodeIndex == 0)
                {
                    // We are back at the root node and none of the values start with the current character.
                    if (result >= 0)
                    {
                        // If we've already found a match, we can't find an earlier one anymore. This is the result
                        goto Return;
                    }

                    // Go back to searching for the next possible starting character.
                    i++;
                    goto FastScan;
                }

                // Follow the next suffix link.
                nodeIndex = node.SuffixLink;

                if (nodeIndex < 0)
                {
                    // A node with a suffix link of -1 indicates a match, see AhoCorasickBuilder.AddSuffixLinks.
                    Debug.Assert(nodeIndex == -1);
                    Debug.Assert(result >= 0);
                    goto Return;
                }

                // Try to match the current character again at the suffix link node.
            }

        Return:
            return result;
        }

        private static void SurrogateToUpperNLS(char h, char l, out char hr, out char lr)
        {
            Debug.Assert(char.IsHighSurrogate(h));
            Debug.Assert(char.IsLowSurrogate(l));

            ReadOnlySpan<char> chars = [h, l];
            Span<char> destination = stackalloc char[2];

            int written = Ordinal.ToUpperOrdinal(chars, destination);
            Debug.Assert(written == 2);

            hr = destination[0];
            lr = destination[1];

            Debug.Assert(char.IsHighSurrogate(hr));
            Debug.Assert(char.IsLowSurrogate(lr));
        }

        public interface IFastScan { }

        public readonly struct IndexOfAnyAsciiFastScan : IFastScan { }

        public readonly struct NoFastScan : IFastScan { }
    }
}
