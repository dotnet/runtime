// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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

        private unsafe int CompareStringNative(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            AssertComparisonSupported(options);

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                using SymbolFilterHelper filter1 = new SymbolFilterHelper(string1, needsIndexMap: false);
                using SymbolFilterHelper filter2 = new SymbolFilterHelper(string2, needsIndexMap: false);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;

                string1 = filter1.FilteredSpan;
                string2 = filter2.FilteredSpan;
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

        private unsafe int IndexOfCoreNative(ReadOnlySpan<char> target, ReadOnlySpan<char> source, CompareOptions options, bool fromBeginning, int* matchLengthPtr)
        {
            AssertComparisonSupported(options);

            SymbolFilterHelper targetFilter = default;
            SymbolFilterHelper sourceFilter = default;
            bool ignoreSymbols = (options & CompareOptions.IgnoreSymbols) != 0;

            // If we are ignoring symbols, preprocess the strings by removing specified Unicode categories.
            if (ignoreSymbols)
            {
                targetFilter = new SymbolFilterHelper(target, needsIndexMap: false);
                sourceFilter = new SymbolFilterHelper(source, needsIndexMap: true);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;

                target = targetFilter.FilteredSpan;
                source = sourceFilter.FilteredSpan;
            }

            try
            {
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

                // If not ignoring symbols / nothing found / an error code / filtered length is same as original (no ignorable symbols in source string), just propagate.
                if (!ignoreSymbols || nativeLocation < 0 || sourceFilter.FilteredLength == sourceFilter.OriginalLength)
                {
                    if (matchLengthPtr != null)
                        *matchLengthPtr = nativeLength;
                    return nativeLocation;
                }

                // If ignoring symbols, map filtered indices back to original indices, expanding match length to include removed symbol chars inside the span.
                ReadOnlySpan<int> sourceIndexMap = sourceFilter.IndexMap;
                int originalStart = sourceIndexMap[nativeLocation];
                int filteredEnd = nativeLocation + nativeLength - 1;

                Debug.Assert(filteredEnd < sourceFilter.FilteredLength,
                    $"Filtered end index {filteredEnd} should not exceed the length of the filtered string {sourceFilter.FilteredLength}. nativeLocation={nativeLocation}, nativeLength={nativeLength}");

                // Find the end position of the character at filteredEnd in the original string.
                int endCharStartPos = sourceIndexMap[filteredEnd];

                // Check if the previous position belongs to the same character (first unit of a surrogate pair)
                int firstUnit = (filteredEnd > 0 && sourceIndexMap[filteredEnd - 1] == endCharStartPos)
                    ? filteredEnd - 1
                    : filteredEnd;

                // Check if the next position belongs to the same character (second unit of a surrogate pair)
                int lastUnit = (filteredEnd + 1 < sourceFilter.FilteredLength && sourceIndexMap[filteredEnd + 1] == endCharStartPos)
                    ? filteredEnd + 1
                    : filteredEnd;

                int endCharWidth = lastUnit - firstUnit + 1;
                int originalEnd = endCharStartPos + endCharWidth;
                int originalLength = originalEnd - originalStart;

                if (matchLengthPtr != null)
                    *matchLengthPtr = originalLength;
                return originalStart;
            }
            finally
            {
                if (ignoreSymbols)
                {
                    targetFilter.Dispose();
                    sourceFilter.Dispose();
                }
            }
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

        private unsafe bool NativeStartsWith(ReadOnlySpan<char> prefix, ReadOnlySpan<char> source, CompareOptions options)
        {
            AssertComparisonSupported(options);

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                using SymbolFilterHelper prefixFilter = new SymbolFilterHelper(prefix, needsIndexMap: false);
                using SymbolFilterHelper sourceFilter = new SymbolFilterHelper(source, needsIndexMap: false);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;

                prefix = prefixFilter.FilteredSpan;
                source = sourceFilter.FilteredSpan;
            }

            fixed (char* pPrefix = &MemoryMarshal.GetReference(prefix))
            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            {
                int result = Interop.Globalization.StartsWithNative(m_name, m_name.Length, pPrefix, prefix.Length, pSource, source.Length, options);
                Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
                return result > 0;
            }
        }

        private unsafe bool NativeEndsWith(ReadOnlySpan<char> suffix, ReadOnlySpan<char> source, CompareOptions options)
        {
            AssertComparisonSupported(options);

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                using SymbolFilterHelper suffixFilter = new SymbolFilterHelper(suffix, needsIndexMap: false);
                using SymbolFilterHelper sourceFilter = new SymbolFilterHelper(source, needsIndexMap: false);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;

                suffix = suffixFilter.FilteredSpan;
                source = sourceFilter.FilteredSpan;
            }

            fixed (char* pSuffix = &MemoryMarshal.GetReference(suffix))
            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            {
                int result = Interop.Globalization.EndsWithNative(m_name, m_name.Length, pSuffix, suffix.Length, pSource, source.Length, options);
                Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
                return result > 0;
            }
        }

        /// <summary>
        /// Filters out ignorable symbol characters from the input span.
        /// </summary>
        /// <param name="input">The input span to filter.</param>
        /// <param name="destination">The destination span to write filtered characters to. Must be at least as large as input.</param>
        /// <param name="indexMap">
        /// Optional array to store the mapping where each index in the filtered output maps to the corresponding
        /// character start position in the original input span. Pass null if mapping is not needed.
        /// Must be at least as large as input if provided.
        /// </param>
        /// <returns>
        /// The number of characters written to the destination span after filtering.
        /// </returns>
        private static int FilterSymbolsFromSpan(ReadOnlySpan<char> input, Span<char> destination, int[]? indexMap)
        {
            Debug.Assert(destination.Length >= input.Length, "Destination buffer must be at least as large as input");
            Debug.Assert(indexMap is null || indexMap.Length >= input.Length, "Index map buffer must be at least as large as input if provided");

            int length = input.Length;
            int writeIndex = 0;

            for (int i = 0; i < length;)
            {
                Rune.DecodeFromUtf16(input.Slice(i), out Rune rune, out int consumed);

                if (!IsIgnorableSymbol(rune))
                {
                    // Copy the UTF-16 units and map each filtered position to the start of the original character
                    for (int j = 0; j < consumed; j++)
                    {
                        destination[writeIndex] = input[i + j];
                        if (indexMap is not null)
                        {
                            indexMap[writeIndex] = i;
                        }
                        writeIndex++;
                    }
                }

                i += consumed;
            }

            return writeIndex;
        }

        /// <summary>
        /// Helper struct that manages buffers for filtering symbols from a span.
        /// Uses ArrayPool to rent temporary buffers.
        /// Implements IDisposable to return rented arrays.
        /// </summary>
        private ref struct SymbolFilterHelper
        {
            private char[]? _rentedCharArray;
            private int[]? _rentedIntArray;

            public ReadOnlySpan<char> FilteredSpan { get; }
            public ReadOnlySpan<int> IndexMap { get; }
            public int FilteredLength { get; }
            public int OriginalLength { get; }

            public SymbolFilterHelper(ReadOnlySpan<char> input, bool needsIndexMap)
            {
                OriginalLength = input.Length;
                _rentedCharArray = ArrayPool<char>.Shared.Rent(input.Length);
                _rentedIntArray = needsIndexMap ? ArrayPool<int>.Shared.Rent(input.Length) : null;

                Span<char> charBuffer = _rentedCharArray.AsSpan(0, input.Length);

                FilteredLength = FilterSymbolsFromSpan(input, charBuffer, _rentedIntArray);
                FilteredSpan = charBuffer.Slice(0, FilteredLength);
                IndexMap = _rentedIntArray is not null ? _rentedIntArray.AsSpan(0, FilteredLength) : default;
            }

            public void Dispose()
            {
                if (_rentedCharArray is not null)
                {
                    ArrayPool<char>.Shared.Return(_rentedCharArray);
                    _rentedCharArray = null;
                }
                if (_rentedIntArray is not null)
                {
                    ArrayPool<int>.Shared.Return(_rentedIntArray);
                    _rentedIntArray = null;
                }
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
}
