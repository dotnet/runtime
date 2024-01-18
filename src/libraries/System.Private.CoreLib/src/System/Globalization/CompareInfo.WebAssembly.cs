// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        // invariant culture has empty CultureInfo.ToString() and
        // m_name == CultureInfo._name == CultureInfo.ToString()
        private bool _isInvariantCulture => string.IsNullOrEmpty(m_name);

        private TextInfo? _thisTextInfo;

        private TextInfo thisTextInfo => _thisTextInfo ??= new CultureInfo(m_name).TextInfo;

        private static bool LocalizedHashCodeSupportsCompareOptions(CompareOptions options) =>
            options == CompareOptions.IgnoreCase || options == CompareOptions.None;
        private static void AssertHybridOnWasm(CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(GlobalizationMode.Hybrid);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);
        }

        private static void AssertComparisonSupported(CompareOptions options, string cultureName)
        {
            if (CompareOptionsNotSupported(options))
                throw new PlatformNotSupportedException(GetPNSE(options));

            if (CompareOptionsNotSupportedForCulture(options, cultureName))
                throw new PlatformNotSupportedException(GetPNSEForCulture(options, cultureName));
        }

        private static void AssertIndexingSupported(CompareOptions options, string cultureName)
        {
            if (IndexingOptionsNotSupported(options) || CompareOptionsNotSupported(options))
                throw new PlatformNotSupportedException(GetPNSE(options));

            if (CompareOptionsNotSupportedForCulture(options, cultureName))
                throw new PlatformNotSupportedException(GetPNSEForCulture(options, cultureName));
        }

        private unsafe int JsCompareString(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
        {
            AssertHybridOnWasm(options);
            string cultureName = m_name;
            AssertComparisonSupported(options, cultureName);

            int cmpResult;
            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &MemoryMarshal.GetReference(string2))
            {
                cmpResult = Interop.JsGlobalization.CompareString(cultureName, pString1, string1.Length, pString2, string2.Length, options, out int exception, out object ex_result);
                if (exception != 0)
                    throw new Exception((string)ex_result);
            }

            return cmpResult;
        }

        private unsafe bool JsStartsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options)
        {
            AssertHybridOnWasm(options);
            Debug.Assert(!prefix.IsEmpty);
            string cultureName = m_name;
            AssertIndexingSupported(options, cultureName);

            bool result;
            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (char* pPrefix = &MemoryMarshal.GetReference(prefix))
            {
                result = Interop.JsGlobalization.StartsWith(cultureName, pSource, source.Length, pPrefix, prefix.Length, options, out int exception, out object ex_result);
                if (exception != 0)
                    throw new Exception((string)ex_result);
            }


            return result;
        }

        private unsafe bool JsEndsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options)
        {
            AssertHybridOnWasm(options);
            Debug.Assert(!prefix.IsEmpty);
            string cultureName = m_name;
            AssertIndexingSupported(options, cultureName);

            bool result;
            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (char* pPrefix = &MemoryMarshal.GetReference(prefix))
            {
                result = Interop.JsGlobalization.EndsWith(cultureName, pSource, source.Length, pPrefix, prefix.Length, options, out int exception, out object ex_result);
                if (exception != 0)
                    throw new Exception((string)ex_result);
            }

            return result;
        }

        private unsafe int JsIndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            AssertHybridOnWasm(options);
            Debug.Assert(!target.IsEmpty);
            string cultureName = m_name;
            AssertIndexingSupported(options, cultureName);

            int idx;
            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                idx = (options & CompareOptions.IgnoreCase) != 0 ?
                    IndexOfOrdinalIgnoreCaseHelper(source, target, options, matchLengthPtr, fromBeginning) :
                    IndexOfOrdinalHelper(source, target, options, matchLengthPtr, fromBeginning);
            }
            else
            {
                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                fixed (char* pTarget = &MemoryMarshal.GetReference(target))
                {
                    idx = Interop.JsGlobalization.IndexOf(m_name, pTarget, target.Length, pSource, source.Length, options, fromBeginning, out int exception, out object ex_result);
                    if (exception != 0)
                        throw new Exception((string)ex_result);
                }
            }

            return idx;
        }

        // chars that are ignored by ICU hashing algorithm but not ignored by invariant hashing
        private char[] emptyCharsToRemove = {
            '\u200d', '\u200b', '\u200c', '\uFEFF', '\u200E', '\u200F',
            '\u2060', '\u2063', '\u2061', '\u2062', '\u2064', '\u180E',
            '\u202A', '\u202B', '\u202D', '\u202E', '\u2066', '\u2067',
            '\u2068', '\u2069', '\u202C'
        };

        private ReadOnlySpan<char> SanitizeForInvariantHash(ReadOnlySpan<char> source, CompareOptions options)
        {
            char[] result = new char[source.Length];
            int resultIndex = 0;
            foreach (char c in source)
            {
                if (Array.IndexOf(emptyCharsToRemove, c) == -1)
                {
                    result[resultIndex++] = c;
                }
            }
            if ((options & CompareOptions.IgnoreCase) != 0)
            {
                string? resultStr = new string(result, 0, resultIndex);
                // JS-based ToLower, to keep cases like Turkish I working
                resultStr = _thisTextInfo?.ToLower(resultStr);
                return resultStr.AsSpan();
            }
            return result.AsSpan(0, resultIndex);
        }

        private static bool IndexingOptionsNotSupported(CompareOptions options) =>
            (options & CompareOptions.IgnoreSymbols) == CompareOptions.IgnoreSymbols;

        private static bool CompareOptionsNotSupported(CompareOptions options) =>
            (options & CompareOptions.IgnoreWidth) == CompareOptions.IgnoreWidth ||
            ((options & CompareOptions.IgnoreNonSpace) == CompareOptions.IgnoreNonSpace && (options & CompareOptions.IgnoreKanaType) != CompareOptions.IgnoreKanaType);

        private static string GetPNSE(CompareOptions options) =>
            SR.Format(SR.PlatformNotSupported_HybridGlobalizationWithCompareOptions, options);

        private static bool CompareOptionsNotSupportedForCulture(CompareOptions options, string cultureName) =>
            (options == CompareOptions.IgnoreKanaType &&
            (string.IsNullOrEmpty(cultureName) || cultureName.Split('-')[0] != "ja")) ||
            (options == CompareOptions.None &&
            (cultureName.Split('-')[0] == "ja"));

        private static string GetPNSEForCulture(CompareOptions options, string cultureName) =>
            SR.Format(SR.PlatformNotSupported_HybridGlobalizationWithCompareOptions, options, cultureName);
    }
}
