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

        private const int StackAllocThreshold = 256;

        private unsafe int CompareStringNative(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            AssertComparisonSupported(options);

            char[]? rentedArray1 = null;
            char[]? rentedArray2 = null;

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                string1 = GetFilteredString(string1, stackalloc char[StackAllocThreshold], out rentedArray1, out _, generateIndexMap: false);
                string2 = GetFilteredString(string2, stackalloc char[StackAllocThreshold], out rentedArray2, out _, generateIndexMap: false);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            try
            {
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
            finally
            {
                ReturnBuffers(rentedArray1, rentedArray2);
            }
        }

        private unsafe int IndexOfCoreNative(ReadOnlySpan<char> target, ReadOnlySpan<char> source, CompareOptions options, bool fromBeginning, int* matchLengthPtr)
        {
            AssertComparisonSupported(options);

            char[]? rentedTargetArray = null;
            char[]? rentedSourceArray = null;
            int[]? rentedIndexMap = null;
            bool ignoreSymbols = (options & CompareOptions.IgnoreSymbols) != 0;

            // If we are ignoring symbols, preprocess the strings by removing specified Unicode categories.
            if (ignoreSymbols)
            {
                target = GetFilteredString(target, stackalloc char[StackAllocThreshold], out rentedTargetArray, out _, generateIndexMap: false);
                source = GetFilteredString(source, stackalloc char[StackAllocThreshold], out rentedSourceArray, out rentedIndexMap, generateIndexMap: true);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
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

                // If not ignoring symbols / nothing found / an error code / no index map (no symbols found in source), just propagate.
                if (!ignoreSymbols || nativeLocation < 0 || rentedIndexMap is null)
                {
                    if (matchLengthPtr != null)
                        *matchLengthPtr = nativeLength;
                    return nativeLocation;
                }

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
            finally
            {
                ReturnBuffers(rentedTargetArray, rentedSourceArray, rentedIndexMap);
            }
        }

        private unsafe bool NativeStartsWith(ReadOnlySpan<char> prefix, ReadOnlySpan<char> source, CompareOptions options)
        {
            AssertComparisonSupported(options);

            char[]? rentedPrefixArray = null;
            char[]? rentedSourceArray = null;

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                prefix = GetFilteredString(prefix, stackalloc char[StackAllocThreshold], out rentedPrefixArray, out _, generateIndexMap: false);
                source = GetFilteredString(source, stackalloc char[StackAllocThreshold], out rentedSourceArray, out _, generateIndexMap: false);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            try
            {
                fixed (char* pPrefix = &MemoryMarshal.GetReference(prefix))
                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                {
                    int result = Interop.Globalization.StartsWithNative(m_name, m_name.Length, pPrefix, prefix.Length, pSource, source.Length, options);
                    Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
                    return result > 0;
                }
            }
            finally
            {
                ReturnBuffers(rentedPrefixArray, rentedSourceArray);
            }
        }

        private unsafe bool NativeEndsWith(ReadOnlySpan<char> suffix, ReadOnlySpan<char> source, CompareOptions options)
        {
            AssertComparisonSupported(options);

            char[]? rentedSuffixArray = null;
            char[]? rentedSourceArray = null;

            // Handle IgnoreSymbols preprocessing
            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                suffix = GetFilteredString(suffix, stackalloc char[StackAllocThreshold], out rentedSuffixArray, out _, generateIndexMap: false);
                source = GetFilteredString(source, stackalloc char[StackAllocThreshold], out rentedSourceArray, out _, generateIndexMap: false);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            try
            {
                fixed (char* pSuffix = &MemoryMarshal.GetReference(suffix))
                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                {
                    int result = Interop.Globalization.EndsWithNative(m_name, m_name.Length, pSuffix, suffix.Length, pSource, source.Length, options);
                    Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
                    return result > 0;
                }
            }
            finally
            {
                ReturnBuffers(rentedSuffixArray, rentedSourceArray);
            }
        }

        /// <summary>
        /// Returns multiple rented buffers to their respective pools if they were allocated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnBuffers(char[]? charBuffer1 = null, char[]? charBuffer2 = null, int[]? indexMapBuffer = null)
        {
            if (charBuffer1 is not null)
                ArrayPool<char>.Shared.Return(charBuffer1);
            if (charBuffer2 is not null)
                ArrayPool<char>.Shared.Return(charBuffer2);
            if (indexMapBuffer is not null)
                ArrayPool<int>.Shared.Return(indexMapBuffer);
        }

        /// <summary>
        /// Filters ignorable symbols from the input span.
        /// </summary>
        /// <param name="input">The input span to filter.</param>
        /// <param name="stackBuffer">A stack-allocated buffer to use for small inputs or if no symbols are found.</param>
        /// <param name="rentedCharArray">Outputs a rented char array if input is too large for stackBuffer and contains symbols. Null if stackBuffer was sufficient.</param>
        /// <param name="rentedIndexMap">Outputs a rented int array for the index map if generateIndexMap is true and symbols were found. Null otherwise.</param>
        /// <param name="generateIndexMap">If true, generates an index map tracking each character's original position.</param>
        /// <returns>
        /// A span containing the filtered string with ignorable symbols removed. If no symbols are found,
        /// returns the original input span without allocating.
        /// </returns>
        private static ReadOnlySpan<char> GetFilteredString(
            ReadOnlySpan<char> input,
            scoped Span<char> stackBuffer,
            out char[]? rentedCharArray,
            out int[]? rentedIndexMap,
            bool generateIndexMap)
        {
            rentedCharArray = null;
            rentedIndexMap = null;

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

            // Found symbols - decide whether to use stack or heap allocation
            Span<char> outSpan = input.Length <= stackBuffer.Length ? stackBuffer : (rentedCharArray = ArrayPool<char>.Shared.Rent(input.Length));
            // Copy the initial segment that contains no ignorable symbols (positions 0 to i-1)
            input.Slice(0, i).CopyTo(outSpan);

            // Initialize the index map for the initial segment with identity mapping
            if (generateIndexMap)
            {
                rentedIndexMap = ArrayPool<int>.Shared.Rent(input.Length);
                for (int j = 0; j < i; j++)
                {
                    rentedIndexMap[j] = j;
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
                    rentedIndexMap?[writeIndex] = i;
                    writeIndex++;

                    if (consumed > 1)
                    {
                        outSpan[writeIndex] = input[i + 1];
                        rentedIndexMap?[writeIndex] = i;
                        writeIndex++;
                    }
                }
                i += consumed;
            }

            return outSpan.Slice(0, writeIndex);
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
