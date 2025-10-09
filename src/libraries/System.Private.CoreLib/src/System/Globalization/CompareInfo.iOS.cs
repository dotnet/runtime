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

            using SymbolFilterHelper helper1 = new SymbolFilterHelper();
            using SymbolFilterHelper helper2 = new SymbolFilterHelper();

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                string1 = helper1.GetFilteredString(string1, generateIndexMap: false);
                string2 = helper2.GetFilteredString(string2, generateIndexMap: false);

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

        private unsafe int IndexOfCoreNative(ReadOnlySpan<char> target, ReadOnlySpan<char> source, CompareOptions options, bool fromBeginning, int* matchLengthPtr)
        {
            AssertComparisonSupported(options);

            using SymbolFilterHelper targetHelper = new SymbolFilterHelper();
            using SymbolFilterHelper sourceHelper = new SymbolFilterHelper();
            bool ignoreSymbols = (options & CompareOptions.IgnoreSymbols) != 0;

            // If we are ignoring symbols, preprocess the strings by removing specified Unicode categories.
            if (ignoreSymbols)
            {
                target = targetHelper.GetFilteredString(target, generateIndexMap: false);
                source = sourceHelper.GetFilteredString(source, generateIndexMap: true);

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
            if (!ignoreSymbols || nativeLocation < 0 || sourceHelper.IndexMap is null)
            {
                if (matchLengthPtr != null)
                    *matchLengthPtr = nativeLength;
                return nativeLocation;
            }

            // If ignoring symbols, map filtered indices back to original indices, expanding match length to include removed symbol chars inside the span.
            int originalStart = sourceHelper.IndexMap[nativeLocation];
            int filteredEnd = nativeLocation + nativeLength - 1;

            Debug.Assert(filteredEnd < source.Length,
                $"Filtered end index {filteredEnd} should not exceed the length of the filtered string {source.Length}. nativeLocation={nativeLocation}, nativeLength={nativeLength}");

            // Find the end position of the character at filteredEnd in the original string.
            int endCharStartPos = sourceHelper.IndexMap[filteredEnd];

            // Check if the previous position belongs to the same character (first unit of a surrogate pair)
            int firstUnit = (filteredEnd > 0 && sourceHelper.IndexMap[filteredEnd - 1] == endCharStartPos)
                ? filteredEnd - 1
                : filteredEnd;

            // Check if the next position belongs to the same character (second unit of a surrogate pair)
            int lastUnit = (filteredEnd + 1 < source.Length && sourceHelper.IndexMap[filteredEnd + 1] == endCharStartPos)
                ? filteredEnd + 1
                : filteredEnd;

            int endCharWidth = lastUnit - firstUnit + 1;
            int originalEnd = endCharStartPos + endCharWidth;
            int originalLength = originalEnd - originalStart;

            if (matchLengthPtr != null)
                *matchLengthPtr = originalLength;
            return originalStart;
        }

        private unsafe bool NativeStartsWith(ReadOnlySpan<char> prefix, ReadOnlySpan<char> source, CompareOptions options)
        {
            AssertComparisonSupported(options);

            using SymbolFilterHelper prefixHelper = new SymbolFilterHelper();
            using SymbolFilterHelper sourceHelper = new SymbolFilterHelper();

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                prefix = prefixHelper.GetFilteredString(prefix, generateIndexMap: false);
                source = sourceHelper.GetFilteredString(source, generateIndexMap: false);

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

        private unsafe bool NativeEndsWith(ReadOnlySpan<char> suffix, ReadOnlySpan<char> source, CompareOptions options)
        {
            AssertComparisonSupported(options);

            using SymbolFilterHelper suffixHelper = new SymbolFilterHelper();
            using SymbolFilterHelper sourceHelper = new SymbolFilterHelper();

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                suffix = suffixHelper.GetFilteredString(suffix, generateIndexMap: false);
                source = sourceHelper.GetFilteredString(source, generateIndexMap: false);

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

        /// <summary>
        /// Helper struct that manages buffers for filtering symbols from a span.
        /// Uses ArrayPool for memory allocation.
        /// Implements IDisposable to return rented arrays.
        /// </summary>
        private ref struct SymbolFilterHelper
        {
            private char[]? _arrayToReturnToPool;

            public SymbolFilterHelper() { }

            /// <summary>
            /// Gets the index map array, or null if no index map was generated.
            /// </summary>
            public int[]? IndexMap { get; private set; }

            /// <summary>
            /// Filters ignorable symbols from the input span and optionally generates an index map.
            /// </summary>
            /// <param name="input">The input span to filter.</param>
            /// <param name="generateIndexMap">
            /// If <c>true</c>, generates an index map that tracks the position of each character in the filtered
            /// output back to its original position in the input. The map is accessible via <see cref="IndexMap"/>.
            /// </param>
            /// <returns>
            /// A span containing the filtered string with ignorable symbols removed. If no symbols are found,
            /// returns the original input span without allocating. Otherwise, returns a span backed by
            /// pooled memory that will be returned when <see cref="Dispose"/> is called.
            /// </returns>
            public ReadOnlySpan<char> GetFilteredString(ReadOnlySpan<char> input, bool generateIndexMap = false)
            {
                if (_arrayToReturnToPool is not null || IndexMap is not null)
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

                // If we scanned the entire input without finding any ignorable symbols, return original input
                if (i >= input.Length)
                {
                    return input;
                }

                // Rent buffers from ArrayPool
                _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(input.Length);
                Span<char> outSpan = _arrayToReturnToPool;

                // Copy the initial segment that contains no ignorable symbols (positions 0 to i-1)
                input.Slice(0, i).CopyTo(outSpan);
                if (generateIndexMap)
                {
                    // Initialize the index map for the initial segment with identity mapping
                    IndexMap = ArrayPool<int>.Shared.Rent(input.Length);
                    for (int j = 0; j < i; j++)
                    {
                        IndexMap![j] = j;
                    }
                }

                int writeIndex = i;
                i += consumed; // skip the ignorable symbol we just found

                for (; i < input.Length;)
                {
                    Rune.DecodeFromUtf16(input.Slice(i), out Rune rune, out consumed);
                    if (!IsIgnorableSymbol(rune))
                    {
                        // Copy the UTF-16 units and map each filtered position to the start of the original character
                        outSpan[writeIndex] = input[i];
                        IndexMap?[writeIndex] = i;
                        writeIndex++;

                        if (consumed > 1)
                        {
                            outSpan[writeIndex] = input[i + 1];
                            IndexMap?[writeIndex] = i + 1;
                            writeIndex++;
                        }
                    }
                    i += consumed;
                }

                return outSpan.Slice(0, writeIndex);
            }

            public void Dispose()
            {
                if (_arrayToReturnToPool is not null)
                {
                    ArrayPool<char>.Shared.Return(_arrayToReturnToPool);
                    _arrayToReturnToPool = null;
                }
                if (IndexMap is not null)
                {
                    ArrayPool<int>.Shared.Return(IndexMap);
                    IndexMap = null;
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
