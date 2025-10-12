// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        private enum ErrorCodes
        {
            ERROR_INDEX_NOT_FOUND = -1,
            ERROR_COMPARISON_OPTIONS_NOT_FOUND = -2,
            ERROR_MIXED_COMPOSITION_NOT_FOUND = -3,
        }

        private const int StackAllocThreshold = 150;

        private unsafe int CompareStringNative(scoped ReadOnlySpan<char> string1, scoped ReadOnlySpan<char> string2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            AssertComparisonSupported(options);

            using SymbolFilteringBuffer buffer1 = new SymbolFilteringBuffer();
            using SymbolFilteringBuffer buffer2 = new SymbolFilteringBuffer();

            Span<char> stackBuffer1 = stackalloc char[StackAllocThreshold];
            Span<char> stackBuffer2 = stackalloc char[StackAllocThreshold];

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                string1 = !buffer1.TryFilterString(string1, stackBuffer1, Span<int>.Empty) ? string1 :
                    buffer1.RentedFilteredBuffer is not null ?
                    buffer1.RentedFilteredBuffer.AsSpan(0, buffer1.FilteredLength) :
                    stackBuffer1.Slice(0, buffer1.FilteredLength);

                string2 = !buffer2.TryFilterString(string2, stackBuffer2, Span<int>.Empty) ? string2 :
                    buffer2.RentedFilteredBuffer is not null ?
                    buffer2.RentedFilteredBuffer.AsSpan(0, buffer2.FilteredLength) :
                    stackBuffer2.Slice(0, buffer2.FilteredLength);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            // GetReference may return nullptr if the input span is defaulted. The native layer handles
            // this appropriately; no workaround is needed on the managed side.
            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &MemoryMarshal.GetReference(string2))
            {
                int result = Interop.Globalization.CompareStringNative(m_name, m_name.Length, pString1, string1.Length, pString2, string2.Length, options);
                Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
                return result;
            }
        }

        private unsafe int IndexOfCoreNative(scoped ReadOnlySpan<char> target, scoped ReadOnlySpan<char> source, CompareOptions options, bool fromBeginning, int* matchLengthPtr)
        {
            AssertComparisonSupported(options);

            bool ignoreSymbols = (options & CompareOptions.IgnoreSymbols) != 0;

            using SymbolFilteringBuffer buffer1 = new SymbolFilteringBuffer();
            using SymbolFilteringBuffer buffer2 = new SymbolFilteringBuffer();

            Span<char> stackBuffer1 = stackalloc char[StackAllocThreshold];
            Span<char> stackBuffer2 = stackalloc char[StackAllocThreshold];
            Span<int> stackIndexMap = stackalloc int[StackAllocThreshold];

            // If we are ignoring symbols, preprocess the strings by removing specified Unicode categories.
            if (ignoreSymbols)
            {
                target = !buffer1.TryFilterString(target, stackBuffer1, Span<int>.Empty) ? target :
                    buffer1.RentedFilteredBuffer is not null ?
                    buffer1.RentedFilteredBuffer.AsSpan(0, buffer1.FilteredLength) :
                    stackBuffer1.Slice(0, buffer1.FilteredLength);

                source = !buffer2.TryFilterString(source, stackBuffer2, stackIndexMap) ? source :
                    buffer2.RentedFilteredBuffer is not null ?
                    buffer2.RentedFilteredBuffer.AsSpan(0, buffer2.FilteredLength) :
                    stackBuffer2.Slice(0, buffer2.FilteredLength);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            Interop.Range result;
            fixed (char* pTarget = &MemoryMarshal.GetReference(target))
            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            {
                result = Interop.Globalization.IndexOfNative(m_name, m_name.Length, pTarget, target.Length, pSource, source.Length, options, fromBeginning);
                Debug.Assert(result.Location != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
                if (result.Location == (int)ErrorCodes.ERROR_MIXED_COMPOSITION_NOT_FOUND)
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_HybridGlobalizationWithMixedCompositions);
            }

            int nativeLocation = result.Location;
            int nativeLength = result.Length;

            // If not ignoring symbols / nothing found / an error code / no index map (no symbols found in source), just propagate.
            // buffer2.FilteredLength == 0 means no symbols were found in source, so no index map was created.
            if (!ignoreSymbols || buffer2.FilteredLength == 0 || nativeLocation < 0)
            {
                if (matchLengthPtr != null)
                    *matchLengthPtr = nativeLength;
                return nativeLocation;
            }

            Span<int> rentedIndexMap = buffer2.RentedIndexMapBuffer is not null ?
                                        buffer2.RentedIndexMapBuffer.AsSpan(0, buffer2.FilteredLength) :
                                        stackIndexMap.Slice(0, buffer2.FilteredLength);

            // If ignoring symbols, map filtered indices back to original indices, expanding match length to include removed symbol chars inside the span.
            int originalStart = rentedIndexMap[nativeLocation];
            int filteredEnd = nativeLocation + nativeLength - 1;

            Debug.Assert(filteredEnd < source.Length,
                $"Filtered end index {filteredEnd} should not exceed the length of the filtered string {source.Length}. nativeLocation={nativeLocation}, nativeLength={nativeLength}");

            // Find the end position of the character at filteredEnd in the original string.
            int endCharStartPos = rentedIndexMap[filteredEnd];

            // Check if the previous position belongs to the same character (first unit of a surrogate pair)
            int firstUnit = (filteredEnd > 0 && rentedIndexMap[filteredEnd - 1] == endCharStartPos)
                ? filteredEnd - 1
                : filteredEnd;

            // Check if the next position belongs to the same character (second unit of a surrogate pair)
            int lastUnit = (filteredEnd + 1 < source.Length && rentedIndexMap[filteredEnd + 1] == endCharStartPos)
                ? filteredEnd + 1
                : filteredEnd;

            int endCharWidth = lastUnit - firstUnit + 1;
            int originalEnd = endCharStartPos + endCharWidth;
            int originalLength = originalEnd - originalStart;

            if (matchLengthPtr != null)
                *matchLengthPtr = originalLength;

            return originalStart;
        }

        private unsafe bool NativeStartsWith(scoped ReadOnlySpan<char> prefix, scoped ReadOnlySpan<char> source, CompareOptions options)
        {
            AssertComparisonSupported(options);

            using SymbolFilteringBuffer buffer1 = new SymbolFilteringBuffer();
            using SymbolFilteringBuffer buffer2 = new SymbolFilteringBuffer();

            Span<char> stackBuffer1 = stackalloc char[StackAllocThreshold];
            Span<char> stackBuffer2 = stackalloc char[StackAllocThreshold];

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                prefix = !buffer1.TryFilterString(prefix, stackBuffer1, Span<int>.Empty) ? prefix :
                    buffer1.RentedFilteredBuffer is not null ?
                    buffer1.RentedFilteredBuffer.AsSpan(0, buffer1.FilteredLength) :
                    stackBuffer1.Slice(0, buffer1.FilteredLength);

                source = !buffer2.TryFilterString(source, stackBuffer2, Span<int>.Empty) ? source :
                    buffer2.RentedFilteredBuffer is not null ?
                    buffer2.RentedFilteredBuffer.AsSpan(0, buffer2.FilteredLength) :
                    stackBuffer2.Slice(0, buffer2.FilteredLength);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            fixed (char* pPrefix = &MemoryMarshal.GetReference(prefix))
            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            {
                int result = Interop.Globalization.StartsWithNative(m_name, m_name.Length, pPrefix, prefix.Length, pSource, source.Length, options);
                Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
                return result > 0;
            }
        }

        private unsafe bool NativeEndsWith(scoped ReadOnlySpan<char> suffix, scoped ReadOnlySpan<char> source, CompareOptions options)
        {
            AssertComparisonSupported(options);

            using SymbolFilteringBuffer buffer1 = new SymbolFilteringBuffer();
            using SymbolFilteringBuffer buffer2 = new SymbolFilteringBuffer();

            Span<char> stackBuffer1 = stackalloc char[StackAllocThreshold];
            Span<char> stackBuffer2 = stackalloc char[StackAllocThreshold];

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                suffix = !buffer1.TryFilterString(suffix, stackBuffer1, Span<int>.Empty) ? suffix :
                    buffer1.RentedFilteredBuffer is not null ?
                    buffer1.RentedFilteredBuffer.AsSpan(0, buffer1.FilteredLength) :
                    stackBuffer1.Slice(0, buffer1.FilteredLength);

                source = !buffer2.TryFilterString(source, stackBuffer2, Span<int>.Empty) ? source :
                    buffer2.RentedFilteredBuffer is not null ?
                    buffer2.RentedFilteredBuffer.AsSpan(0, buffer2.FilteredLength) :
                    stackBuffer2.Slice(0, buffer2.FilteredLength);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            fixed (char* pSuffix = &MemoryMarshal.GetReference(suffix))
            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            {
                int result = Interop.Globalization.EndsWithNative(m_name, m_name.Length, pSuffix, suffix.Length, pSource, source.Length, options);
                Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
                return result > 0;
            }
        }

        private static void AssertComparisonSupported(CompareOptions options)
        {
            if ((options | SupportedCompareOptions) != SupportedCompareOptions)
                throw new PlatformNotSupportedException(GetPNSE(options));
        }

        private const CompareOptions SupportedCompareOptions = CompareOptions.None | CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace |
                                                               CompareOptions.IgnoreWidth | CompareOptions.StringSort | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreSymbols;

        private static string GetPNSE(CompareOptions options) =>
            SR.Format(SR.PlatformNotSupported_HybridGlobalizationWithCompareOptions, options);
    }

    internal struct SymbolFilteringBuffer : IDisposable
    {
        public SymbolFilteringBuffer()
        {
            RentedFilteredBuffer = null;
            RentedIndexMapBuffer = null;
            FilteredLength = 0;
        }

        public char[]? RentedFilteredBuffer { get; set; }
        public int[]?  RentedIndexMapBuffer { get; set; }
        public int FilteredLength { get; set; }

        /// <summary>
        /// Attempt to filter ignorable symbols from the input string.
        /// </summary>
        /// <remarks>If the input contains ignorable symbols and the stack buffers are not large enough to
        /// hold the filtered result, the method may fall back to heap allocation. The index mapping buffer is only
        /// populated if index mapping is needed. This method does not modify the original input.</remarks>
        /// <param name="input">The input string to be filtered, represented as a read-only span of UTF-16 characters.</param>
        /// <param name="filteredStackBuffer">A stack-allocated buffer to receive the filtered characters. If this buffer is not long enough to hold the filtered result,
        /// the method may fall to use the ArrayPool.
        /// </param>
        /// <param name="indexMapStackBuffer">A stack-allocated buffer to receive the mapping from filtered character positions to their original indices
        /// in the input. if this span is empty, means ni need to create the index mapping.</param>
        /// <returns>true if the string is filtered removing symbols from there. false if there is no symbols found in the input.</returns>
        internal bool TryFilterString(
            ReadOnlySpan<char> input,
            Span<char> filteredStackBuffer,
            Span<int>  indexMapStackBuffer)
        {
            if (RentedFilteredBuffer is not null || RentedIndexMapBuffer is not null)
            {
                Dispose();
            }

            int i = 0;
            int consumed = 0;

            // Fast path: scan through the input until we find the first ignorable symbol
            for (; i < input.Length;)
            {
                Rune.DecodeFromUtf16(input.Slice(i), out Rune rune, out consumed);
                if (IsIgnorableSymbol(rune))
                {
                    break;
                }
                i += consumed;
            }

            // If we scanned the entire input without finding any ignorable symbols, return false
            if (i >= input.Length)
            {
                return false;
            }

            // Found symbols - decide whether to use stack or heap allocation
            Span<char> outSpan = input.Length <= filteredStackBuffer.Length ? filteredStackBuffer : (RentedFilteredBuffer = ArrayPool<char>.Shared.Rent(input.Length));
            // Copy the initial segment that contains no ignorable symbols (positions 0 to i-1)
            input.Slice(0, i).CopyTo(outSpan);

            Span<int> indexMap = indexMapStackBuffer.IsEmpty ?
                                    Span<int>.Empty :
                                    (indexMapStackBuffer.Length >= input.Length ? indexMapStackBuffer : (RentedIndexMapBuffer = ArrayPool<int>.Shared.Rent(input.Length)));

            // Initialize the index map for the initial segment with identity mapping
            if (!indexMap.IsEmpty)
            {
                for (int j = 0; j < i; j++)
                {
                    indexMap[j] = j;
                }
            }

            FilteredLength = i;
            i += consumed; // skip the ignorable symbol we just found

            for (; i < input.Length;)
            {
                Rune.DecodeFromUtf16(input.Slice(i), out Rune rune, out consumed);
                if (!IsIgnorableSymbol(rune))
                {
                    // Copy the UTF-16 units and map each filtered position to the start of the original character
                    outSpan[FilteredLength] = input[i];
                    if (!indexMap.IsEmpty)
                    {
                        indexMap[FilteredLength] = i;
                    }
                    FilteredLength++;

                    if (consumed > 1)
                    {
                        outSpan[FilteredLength] = input[i + 1];
                        if (!indexMap.IsEmpty)
                        {
                            indexMap[FilteredLength] = i + 1;
                        }
                        FilteredLength++;
                    }
                }
                i += consumed;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified rune should be ignored when using CompareOptions.IgnoreSymbols.
        /// </summary>
        /// <param name="rune">The rune to check.</param>
        /// <returns>
        /// <c>true</c> if the rune should be ignored; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method returns <c>true</c> for:
        /// - All separator categories (SpaceSeparator, LineSeparator, ParagraphSeparator)
        /// - All punctuation categories (ConnectorPunctuation through OtherPunctuation)
        /// - All symbol categories (MathSymbol through ModifierSymbol)
        /// - Whitespace control characters (tab, line feed, vertical tab, form feed, carriage return, etc.)
        /// </remarks>
        private static bool IsIgnorableSymbol(Rune rune)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(rune.Value);

            // Check for separator categories (11-13)
            if (category >= UnicodeCategory.SpaceSeparator && category <= UnicodeCategory.ParagraphSeparator)
                return true;

            // Check for punctuation/symbol categories (18-27)
            if (category >= UnicodeCategory.ConnectorPunctuation && category <= UnicodeCategory.ModifierSymbol)
                return true;

            // For Control (14) and Format (15) categories, only include whitespace characters
            // This includes: tab (U+0009), LF (U+000A), VT (U+000B), FF (U+000C), CR (U+000D), NEL (U+0085)
            if (category == UnicodeCategory.Control || category == UnicodeCategory.Format)
                return Rune.IsWhiteSpace(rune);

            return false;
        }

        public void Dispose()
        {
            if (RentedFilteredBuffer is not null)
            {
                ArrayPool<char>.Shared.Return(RentedFilteredBuffer);
                RentedFilteredBuffer = null;
            }
            if (RentedIndexMapBuffer is not null)
            {
                ArrayPool<int>.Shared.Return(RentedIndexMapBuffer);
                RentedIndexMapBuffer = null;
            }

            FilteredLength = 0;
        }
    }
}
