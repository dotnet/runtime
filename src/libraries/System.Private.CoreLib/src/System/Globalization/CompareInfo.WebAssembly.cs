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
        // Control: 59 (out of 1105)
        // Format: 43 (out of 731)
        // NonSpacingMark: 195 (out of 18105)
        // EnclosingMark: 5 (out of 221) // 0488, 0489, A670, A671, A672
        // ModifierLetter: 2 (out of 4012) // 0640, 07FA
        // SpacingCombiningMark: 4 (out of 4420) // 0F3E, 0F3F, 1CE1, 1CF7
        // OtherPunctuation: 4 (out of 7004) // 180A, 1CD3, 10F86 (\uD803\uDC00), 10F87 (\uD803\uDF87)
        // OtherLetter: 683 (out of 784142)
        // OtherNotAssigned: 3581 (out of 24718)
        // UppercaseLetter: 4 (out of 19159) // 10591 (\uD801\uDC91), 10592 (\uD801\uDC92), 10594 (\uD801\uDC94), 10595 (\uD801\uDC95)
        // LowercaseLetter: 24 (out of 24565) // 10597 -  105AF
        // OtherNumber: 1 (out of 5100) // 10FC6 (\uD843\uDFC6)
        // PrivateUse: 614 (out of 108800)
        // total: 5219 chars
        // skipping more characters than ICU would lead to hashes with smaller distribution and more collisions in hash tables
        // but it makes the behavior correct and consistent with locale-aware equals, which is acceptable tradeoff
        private static bool ShouldBeSkipped(UnicodeCategory category, char value)
        {
            switch (category)
            {
                case UnicodeCategory.Control:
                case UnicodeCategory.Format:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.OtherNotAssigned:
                case UnicodeCategory.PrivateUse:
                {
                    return true;
                }
                case UnicodeCategory.LowercaseLetter:
                {
                    // some skipped unicodes, e.g. from Elbasan script, are surrogates
                    int codePoint = char.ConvertToUtf32(value.ToString(), 0);
                    return 0x10597 <= codePoint && codePoint <= 0x105AF;
                }
                case UnicodeCategory.UppercaseLetter:
                {
                    int codePoint = char.ConvertToUtf32(value.ToString(), 0);
                    return 0x10591 <= codePoint && codePoint <= 0x10595;
                }
                case UnicodeCategory.OtherNumber:
                {
                    int codePoint = char.ConvertToUtf32(value.ToString(), 0);
                    return codePoint == 0x10FC6;
                }
                case UnicodeCategory.OtherPunctuation:
                {
                    if (value == '\u180A' || value == '\u1CD3')
                        return true;
                    int codePoint = char.ConvertToUtf32(value.ToString(), 0);
                    return codePoint == 0x10F86 || codePoint == 0x10F87;
                }
                case UnicodeCategory.EnclosingMark:
                {
                    return value == '\u0488' || value == '\u0489' || value == '\uA670' || value == '\uA671' || value == '\uA672';
                }
                case UnicodeCategory.ModifierLetter:
                {
                    return value == '\u0640' || value == '\u07FA';
                }
                case UnicodeCategory.SpacingCombiningMark:
                {
                    return value == '\u0F3E' || value == '\u0F3F' || value == '\u1CE1' || value == '\u1CF7';
                }
                default:
                    return false;
            }
        }

        private ReadOnlySpan<char> SanitizeForInvariantHash(ReadOnlySpan<char> source, CompareOptions options)
        {
            char[] result = new char[source.Length];
            int resultIndex = 0;
            foreach (char c in source)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (ShouldBeSkipped(category, c))
                {
                    continue;
                }
                result[resultIndex++] = c;
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
