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
            bool ignoreSymbols = (options & CompareOptions.IgnoreSymbols) != 0;
            ReadOnlySpan<char> filteredString1 = string1;
            ReadOnlySpan<char> filteredString2 = string2;

            if (ignoreSymbols)
            {
                filteredString1 = FilterSymbolsFromSpan(string1, out _);
                filteredString2 = FilterSymbolsFromSpan(string2, out _);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            // GetReference may return nullptr if the input span is defaulted. The native layer handles
            // this appropriately; no workaround is needed on the managed side.
            int result;
            fixed (char* pString1 = &MemoryMarshal.GetReference(filteredString1))
            fixed (char* pString2 = &MemoryMarshal.GetReference(filteredString2))
            {
                result = Interop.Globalization.CompareStringNative(m_name, m_name.Length, pString1, filteredString1.Length, pString2, filteredString2.Length, options);
            }

            Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);

            return result;
        }

        private unsafe int IndexOfCoreNative(ReadOnlySpan<char> target, ReadOnlySpan<char> source, CompareOptions options, bool fromBeginning, int* matchLengthPtr)
        {
            AssertComparisonSupported(options);
            // We only implement managed preprocessing for IgnoreSymbols.
            bool ignoreSymbols = (options & CompareOptions.IgnoreSymbols) != 0;
            ReadOnlySpan<char> filteredTarget = target;
            ReadOnlySpan<char> filteredSource = source;
            int[]? sourceIndexMap = null; // maps each char index in filteredSource to original source char index

            // If we are ignoring symbols, preprocess the strings by removing specified Unicode categories.
            if (ignoreSymbols)
            {
                filteredTarget = FilterSymbolsFromSpan(target, out _);
                filteredSource = FilterSymbolsFromSpan(source, out sourceIndexMap);

                // Remove the flag before passing to native since we handled it here.
                options &= ~CompareOptions.IgnoreSymbols;
            }

            int nativeLocation;
            int nativeLength;
            fixed (char* pTarget = &MemoryMarshal.GetReference(filteredTarget))
            fixed (char* pSource = &MemoryMarshal.GetReference(filteredSource))
            {
                Interop.Range result = Interop.Globalization.IndexOfNative(m_name, m_name.Length, pTarget, filteredTarget.Length, pSource, filteredSource.Length, options, fromBeginning);
                Debug.Assert(result.Location != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
                if (result.Location == (int)ErrorCodes.ERROR_MIXED_COMPOSITION_NOT_FOUND)
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_HybridGlobalizationWithMixedCompositions);
                nativeLocation = result.Location;
                nativeLength = result.Length;
            }

            if (!ignoreSymbols)
            {
                if (matchLengthPtr != null)
                    *matchLengthPtr = nativeLength;
                return nativeLocation;
            }

            // If ignoring symbols and nothing found, or an error code, or no source index map, just propagate.
            if (nativeLocation < 0 || sourceIndexMap == null)
            {
                if (matchLengthPtr != null)
                    *matchLengthPtr = nativeLength;
                return nativeLocation;
            }

            // Map filtered indices back to original indices, expanding match length to include removed symbol chars inside the span.
            // nativeLocation is index into filteredSource; nativeLength is length in filteredSource UTF-16 code units.
            int originalStart = sourceIndexMap[nativeLocation];
            int filteredEnd = nativeLocation + nativeLength - 1;

            Debug.Assert(filteredEnd < sourceIndexMap.Length,
                $"Filtered end index {filteredEnd} should not exceed the length of the filtered string {sourceIndexMap.Length}. nativeLocation={nativeLocation}, nativeLength={nativeLength}");

            int originalEnd = sourceIndexMap[filteredEnd];
            int originalLength = originalEnd - originalStart + 1;

            if (matchLengthPtr != null)
                *matchLengthPtr = originalLength;
            return originalStart;
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
            bool ignoreSymbols = (options & CompareOptions.IgnoreSymbols) != 0;
            ReadOnlySpan<char> filteredPrefix = prefix;
            ReadOnlySpan<char> filteredSource = source;

            if (ignoreSymbols)
            {
                filteredPrefix = FilterSymbolsFromSpan(prefix, out _);
                filteredSource = FilterSymbolsFromSpan(source, out _);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            int result;
            fixed (char* pPrefix = &MemoryMarshal.GetReference(filteredPrefix))
            fixed (char* pSource = &MemoryMarshal.GetReference(filteredSource))
            {
                result = Interop.Globalization.StartsWithNative(m_name, m_name.Length, pPrefix, filteredPrefix.Length, pSource, filteredSource.Length, options);
            }
            Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);

            return result > 0;
        }

        private unsafe bool NativeEndsWith(ReadOnlySpan<char> suffix, ReadOnlySpan<char> source, CompareOptions options)
        {
            AssertComparisonSupported(options);

            // Handle IgnoreSymbols preprocessing
            bool ignoreSymbols = (options & CompareOptions.IgnoreSymbols) != 0;
            ReadOnlySpan<char> filteredSuffix = suffix;
            ReadOnlySpan<char> filteredSource = source;

            if (ignoreSymbols)
            {
                filteredSuffix = FilterSymbolsFromSpan(suffix, out _);
                filteredSource = FilterSymbolsFromSpan(source, out _);

                // Remove the flag before passing to native since we handled it here
                options &= ~CompareOptions.IgnoreSymbols;
            }

            int result;
            fixed (char* pSuffix = &MemoryMarshal.GetReference(filteredSuffix))
            fixed (char* pSource = &MemoryMarshal.GetReference(filteredSource))
            {
                result = Interop.Globalization.EndsWithNative(m_name, m_name.Length, pSuffix, filteredSuffix.Length, pSource, filteredSource.Length, options);
            }
            Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);

            return result > 0;
        }

        /// <summary>
        /// Filters out ignorable symbol characters from the input span.
        /// </summary>
        /// <param name="input">The input span to filter.</param>
        /// <param name="indexMap">
        /// When this method returns, contains a mapping array where each index in the filtered output
        /// maps to the corresponding character index in the original input span. This parameter is
        /// passed uninitialized and will be null if no symbols were removed.
        /// </param>
        /// <returns>
        /// A read-only span with ignorable symbols removed. If no symbols were found, returns the
        /// original input span unchanged.
        /// </returns>
        private static ReadOnlySpan<char> FilterSymbolsFromSpan(ReadOnlySpan<char> input, out int[]? indexMap)
        {
            int length = input.Length;
            bool hasSymbols = false;
            List<char> keptChars = new List<char>(length);
            List<int> mapping = new List<int>(length);
            // TODO: Use ArrayPool<char> for keptChars and mapping to avoid allocations.
            for (int i = 0; i < length;)
            {
                Rune.DecodeFromUtf16(input.Slice(i), out Rune rune, out int consumed);
                bool remove = IsIgnorableSymbol(rune);

                if (!remove)
                {
                    // Copy the UTF-16 units and map each filtered position to the start of the original character
                    for (int j = 0; j < consumed; j++)
                    {
                        keptChars.Add(input[i + j]);
                        mapping.Add(i);
                    }
                }
                else
                {
                    hasSymbols = true;
                }

                i += consumed;
            }

            if (!hasSymbols)
            {
                // No symbols removed; return original span and no mapping.
                indexMap = null;
                return input;
            }
            else
            {
                indexMap = mapping.ToArray();
                return keptChars.ToArray();
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
