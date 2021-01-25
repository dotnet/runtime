// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Text.Unicode
{
    /// <summary>
    /// Provides helper utilities for computing text segmentation boundaries
    /// as specified in UAX#29 (https://www.unicode.org/reports/tr29/).
    /// </summary>
    /// <remarks>
    /// The current implementation is compliant per Rev. 35, https://www.unicode.org/reports/tr29/tr29-35.html.
    /// </remarks>
    internal static class TextSegmentationUtility
    {
        private delegate OperationStatus DecodeFirstRune<T>(ReadOnlySpan<T> input, out Rune rune, out int elementsConsumed);

        private static readonly DecodeFirstRune<char> _utf16Decoder = Rune.DecodeFromUtf16;

        private static int GetLengthOfFirstExtendedGraphemeCluster<T>(ReadOnlySpan<T> input, DecodeFirstRune<T> decoder)
        {
            // Algorithm given at https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules.

            Processor<T> processor = new Processor<T>(input, decoder);
            processor.MoveNext();

            // First, consume as many Prepend scalars as we can (rule GB9b).

            while (processor.CurrentType == GraphemeClusterBreakType.Prepend)
            {
                processor.MoveNext();
            }

            // Next, make sure we're not about to violate control character restrictions.
            // Essentially, if we saw Prepend data, we can't have Control | CR | LF data afterward (rule GB5).

            if (processor.CurrentCodeUnitOffset > 0)
            {
                if (processor.CurrentType == GraphemeClusterBreakType.Control
                    || processor.CurrentType == GraphemeClusterBreakType.CR
                    || processor.CurrentType == GraphemeClusterBreakType.LF)
                {
                    goto Return;
                }
            }

            // Now begin the main state machine.

            GraphemeClusterBreakType previousClusterBreakType = processor.CurrentType;
            processor.MoveNext();

            switch (previousClusterBreakType)
            {
                case GraphemeClusterBreakType.CR:
                    if (processor.CurrentType != GraphemeClusterBreakType.LF)
                    {
                        goto Return; // rules GB3 & GB4 (only <LF> can follow <CR>)
                    }

                    processor.MoveNext();
                    goto case GraphemeClusterBreakType.LF;

                case GraphemeClusterBreakType.Control:
                case GraphemeClusterBreakType.LF:
                    goto Return; // rule GB4 (no data after Control | LF)

                case GraphemeClusterBreakType.L:
                    if (processor.CurrentType == GraphemeClusterBreakType.L)
                    {
                        processor.MoveNext(); // rule GB6 (L x L)
                        goto case GraphemeClusterBreakType.L;
                    }
                    else if (processor.CurrentType == GraphemeClusterBreakType.V)
                    {
                        processor.MoveNext(); // rule GB6 (L x V)
                        goto case GraphemeClusterBreakType.V;
                    }
                    else if (processor.CurrentType == GraphemeClusterBreakType.LV)
                    {
                        processor.MoveNext(); // rule GB6 (L x LV)
                        goto case GraphemeClusterBreakType.LV;
                    }
                    else if (processor.CurrentType == GraphemeClusterBreakType.LVT)
                    {
                        processor.MoveNext(); // rule GB6 (L x LVT)
                        goto case GraphemeClusterBreakType.LVT;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeClusterBreakType.LV:
                case GraphemeClusterBreakType.V:
                    if (processor.CurrentType == GraphemeClusterBreakType.V)
                    {
                        processor.MoveNext(); // rule GB7 (LV | V x V)
                        goto case GraphemeClusterBreakType.V;
                    }
                    else if (processor.CurrentType == GraphemeClusterBreakType.T)
                    {
                        processor.MoveNext(); // rule GB7 (LV | V x T)
                        goto case GraphemeClusterBreakType.T;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeClusterBreakType.LVT:
                case GraphemeClusterBreakType.T:
                    if (processor.CurrentType == GraphemeClusterBreakType.T)
                    {
                        processor.MoveNext(); // rule GB8 (LVT | T x T)
                        goto case GraphemeClusterBreakType.T;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeClusterBreakType.Extended_Pictograph:
                    // Attempt processing extended pictographic (rules GB11, GB9).
                    // First, drain any Extend scalars that might exist

                    while (processor.CurrentType == GraphemeClusterBreakType.Extend)
                    {
                        processor.MoveNext();
                    }

                    // Now see if there's a ZWJ + extended pictograph again.

                    if (processor.CurrentType != GraphemeClusterBreakType.ZWJ)
                    {
                        break;
                    }

                    processor.MoveNext();
                    if (processor.CurrentType != GraphemeClusterBreakType.Extended_Pictograph)
                    {
                        break;
                    }

                    processor.MoveNext();
                    goto case GraphemeClusterBreakType.Extended_Pictograph;

                case GraphemeClusterBreakType.Regional_Indicator:
                    // We've consumed a single RI scalar. Try to consume another (to make it a pair).

                    if (processor.CurrentType == GraphemeClusterBreakType.Regional_Indicator)
                    {
                        processor.MoveNext();
                    }

                    // Standlone RI scalars (or a single pair of RI scalars) can only be followed by trailers.

                    break; // nothing but trailers after the final RI

                default:
                    break;
            }

            // rules GB9, GB9a
            while (processor.CurrentType == GraphemeClusterBreakType.Extend
                || processor.CurrentType == GraphemeClusterBreakType.ZWJ
                || processor.CurrentType == GraphemeClusterBreakType.SpacingMark)
            {
                processor.MoveNext();
            }

        Return:

            return processor.CurrentCodeUnitOffset; // rules GB2, GB999
        }

        /// <summary>
        /// Given UTF-16 input text, returns the length (in chars) of the first extended grapheme cluster.
        /// The slice [0..length] represents the first standalone extended grapheme cluster in the text.
        /// If the input is empty, returns 0.
        /// </summary>
        public static int GetLengthOfFirstUtf16ExtendedGraphemeCluster(ReadOnlySpan<char> input)
        {
            return GetLengthOfFirstExtendedGraphemeCluster(input, _utf16Decoder);
        }

        [StructLayout(LayoutKind.Auto)]
        private ref struct Processor<T>
        {
            private readonly ReadOnlySpan<T> _buffer;
            private readonly DecodeFirstRune<T> _decoder;
            private int _codeUnitLengthOfCurrentScalar;

            internal Processor(ReadOnlySpan<T> buffer, DecodeFirstRune<T> decoder)
            {
                _buffer = buffer;
                _decoder = decoder;
                _codeUnitLengthOfCurrentScalar = 0;
                CurrentType = GraphemeClusterBreakType.Other;
                CurrentCodeUnitOffset = 0;
            }

            public int CurrentCodeUnitOffset { get; private set; }

            /// <summary>
            /// Will be <see cref="GraphemeClusterBreakType.Other"/> if invalid data or EOF reached.
            /// Caller shouldn't need to special-case this since the normal rules will halt on this condition.
            /// </summary>
            public GraphemeClusterBreakType CurrentType { get; private set; }

            public void MoveNext()
            {
                // For ill-formed subsequences (like unpaired UTF-16 surrogate code points), we rely on
                // the decoder's default behavior of interpreting these ill-formed subsequences as
                // equivalent to U+FFFD REPLACEMENT CHARACTER. This code point has a boundary property
                // of Other (XX), which matches the modifications made to UAX#29, Rev. 35.
                // See: https://www.unicode.org/reports/tr29/tr29-35.html#Modifications
                // This change is also reflected in the UCD files. For example, Unicode 11.0's UCD file
                // https://www.unicode.org/Public/11.0.0/ucd/auxiliary/GraphemeBreakProperty.txt
                // has the line "D800..DFFF    ; Control # Cs [2048] <surrogate-D800>..<surrogate-DFFF>",
                // but starting with Unicode 12.0 that line has been removed.
                //
                // If a later version of the Unicode Standard further modifies this guidance we should reflect
                // that here.

                CurrentCodeUnitOffset += _codeUnitLengthOfCurrentScalar;
                _decoder(_buffer.Slice(CurrentCodeUnitOffset), out Rune thisRune, out _codeUnitLengthOfCurrentScalar);
                CurrentType = CharUnicodeInfo.GetGraphemeClusterBreakType(thisRune);
            }
        }
    }
}
