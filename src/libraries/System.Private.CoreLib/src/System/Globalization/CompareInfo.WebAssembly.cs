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

        // there are chars that are ignored by ICU hashing algorithm but not ignored by invariant hashing
        // Control: 1105 (out of 1105)
        // Format: 697 (out of 731)
        // OtherPunctuation: 6919 (out of 7004)
        // SpaceSeparator: 289 (out of 289)
        // OpenPunctuation: 1275 (out of 1343)
        // ClosePunctuation: 1241 (out of 1309)
        // DashPunctuation: 408 (out of 425)
        // ConnectorPunctuation: 170 (out of 170)
        // InitialQuotePunctuation: 204 (out of 204)
        // FinalQuotePunctuation: 170 (out of 170)
        // LineSeparator: 17 (out of 17)
        // ParagraphSeparator: 17 (out of 17)
        // OtherLetter: 34 (out of 784142)
        // SpacingCombiningMark: 68 (out of 4420)
        // ModifierLetter: 51 (out of 4012)
        // EnclosingMark: 85 (out of 221)
        // NonSpacingMark: 3281 (out of 18105)
        // we can skip them all (~1027k chars) by checking for the remaining UnicodeCategories (~291k chars)
        // skipping more characters than ICU would lead to hashes with smaller distribution and more collisions in hash tables
        // but it makes the behavior correct and consistent with locale-aware equals, which is acceptable tradeoff
        private static bool ShouldNotBeSkipped(UnicodeCategory category) =>
            category == UnicodeCategory.LowercaseLetter ||
            category == UnicodeCategory.UppercaseLetter ||
            category == UnicodeCategory.TitlecaseLetter ||
            category == UnicodeCategory.LetterNumber ||
            category == UnicodeCategory.OtherNumber ||
            category == UnicodeCategory.Surrogate ||
            category == UnicodeCategory.PrivateUse ||
            category == UnicodeCategory.MathSymbol ||
            category == UnicodeCategory.CurrencySymbol ||
            category == UnicodeCategory.ModifierSymbol ||
            category == UnicodeCategory.OtherSymbol ||
            category == UnicodeCategory.OtherNotAssigned;

        private ReadOnlySpan<char> SanitizeForInvariantHash(ReadOnlySpan<char> source, CompareOptions options)
        {
            char[] result = new char[source.Length];
            int resultIndex = 0;
            foreach (char c in source)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (ShouldNotBeSkipped(category))
                {
                    result[resultIndex++] = c;
                }
            }
            if ((options & CompareOptions.IgnoreCase) != 0)
            {
                string resultStr = new string(result, 0, resultIndex);
                // JS-based ToUpper, to keep cases like Turkish I working
                resultStr = thisTextInfo.ToUpper(resultStr);
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
