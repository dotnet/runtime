// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        private unsafe int JsCompareString(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(GlobalizationMode.Hybrid);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);


            if (ComparisonOptionsNotSupported(options))
                throw new PlatformNotSupportedException(GetPNSE(options));

            string cultureName = m_name;

            if (ComparisonOptionsNotSupportedForCulture(options, cultureName))
                throw new PlatformNotSupportedException(GetPNSEForCulture(options, cultureName));

            string exceptionMessage;
            int cmpResult;
            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &MemoryMarshal.GetReference(string2))
            {
                cmpResult = Interop.JsGlobalization.CompareString(out exceptionMessage, cultureName, pString1, string1.Length, pString2, string2.Length, options);
            }

            if (!string.IsNullOrEmpty(exceptionMessage))
                throw new Exception(exceptionMessage);

            return cmpResult;
        }

        private void JsInit(string interopCultureName)
        {
            _isAsciiEqualityOrdinal = GetIsAsciiEqualityOrdinal(interopCultureName);
        }

        private unsafe int JsIndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(GlobalizationMode.Hybrid);
            Debug.Assert(target.Length != 0);

            if (IndexingOptionsNotSupported(options) || ComparisonOptionsNotSupported(options))
                throw new PlatformNotSupportedException(GetPNSE(options));

            string cultureName = m_name;

            if (ComparisonOptionsNotSupportedForCulture(options, cultureName))
                throw new PlatformNotSupportedException(GetPNSEForCulture(options, cultureName));

            string exceptionMessage;
            int idx;
            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                idx = (options & CompareOptions.IgnoreCase) != 0 ?
                    IndexOfOrdinalIgnoreCaseHelperJS(out exceptionMessage, source, target, options, matchLengthPtr, fromBeginning) :
                    IndexOfOrdinalHelperJS(out exceptionMessage, source, target, options, matchLengthPtr, fromBeginning);
            }
            else
            {
                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                fixed (char* pTarget = &MemoryMarshal.GetReference(target))
                {
                    idx = Interop.JsGlobalization.IndexOf(out exceptionMessage, m_name, pTarget, target.Length, pSource, source.Length, options, matchLengthPtr, fromBeginning);
                }
            }

            if (!string.IsNullOrEmpty(exceptionMessage))
                throw new Exception(exceptionMessage);

            return idx;
        }

        /// <summary>
        /// Duplicate of IndexOfOrdinalHelperJS that also handles ignore case. Can't converge both methods
        /// as the JIT wouldn't be able to optimize the ignoreCase path away.
        /// </summary>
        /// <returns></returns>
        // ToDo: clean up with IndexOfOrdinalHelper from .Icu
        private unsafe int IndexOfOrdinalIgnoreCaseHelperJS(out string  exceptionMessage, ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!target.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options));

            exceptionMessage = "";

            fixed (char* ap = &MemoryMarshal.GetReference(source))
            fixed (char* bp = &MemoryMarshal.GetReference(target))
            {
                char* a = ap;
                char* b = bp;

                for (int j = 0; j < target.Length; j++)
                {
                    char targetChar = *(b + j);
                    if (targetChar >= 0x80 || HighCharTable[targetChar])
                        goto InteropCall;
                }

                if (target.Length > source.Length)
                {
                    for (int k = 0; k < source.Length; k++)
                    {
                        char targetChar = *(a + k);
                        if (targetChar >= 0x80 || HighCharTable[targetChar])
                            goto InteropCall;
                    }
                    return -1;
                }

                int startIndex, endIndex, jump;
                if (fromBeginning)
                {
                    // Left to right, from zero to last possible index in the source string.
                    // Incrementing by one after each iteration. Stop condition is last possible index plus 1.
                    startIndex = 0;
                    endIndex = source.Length - target.Length + 1;
                    jump = 1;
                }
                else
                {
                    // Right to left, from first possible index in the source string to zero.
                    // Decrementing by one after each iteration. Stop condition is last possible index minus 1.
                    startIndex = source.Length - target.Length;
                    endIndex = -1;
                    jump = -1;
                }

                for (int i = startIndex; i != endIndex; i += jump)
                {
                    int targetIndex = 0;
                    int sourceIndex = i;

                    for (; targetIndex < target.Length; targetIndex++, sourceIndex++)
                    {
                        char valueChar = *(a + sourceIndex);
                        char targetChar = *(b + targetIndex);

                        if (valueChar >= 0x80 || HighCharTable[valueChar])
                            goto InteropCall;

                        if (valueChar == targetChar)
                        {
                            continue;
                        }

                        // uppercase both chars - notice that we need just one compare per char
                        if (char.IsAsciiLetterLower(valueChar))
                            valueChar = (char)(valueChar - 0x20);
                        if (char.IsAsciiLetterLower(targetChar))
                            targetChar = (char)(targetChar - 0x20);

                        if (valueChar == targetChar)
                        {
                            continue;
                        }

                        // The match may be affected by special character. Verify that the following character is regular ASCII.
                        if (sourceIndex < source.Length - 1 && *(a + sourceIndex + 1) >= 0x80)
                            goto InteropCall;
                        goto Next;
                    }

                    // The match may be affected by special character. Verify that the following character is regular ASCII.
                    if (sourceIndex < source.Length && *(a + sourceIndex) >= 0x80)
                        goto InteropCall;
                    if (matchLengthPtr != null)
                        *matchLengthPtr = target.Length;
                    return i;

                Next: ;
                }

                return -1;

            InteropCall:
                return Interop.JsGlobalization.IndexOf(out exceptionMessage, m_name, b, target.Length, a, source.Length, options, matchLengthPtr, fromBeginning);
            }
        }

        private unsafe int IndexOfOrdinalHelperJS(out string exceptionMessage, ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!target.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options));

            exceptionMessage = "";

            fixed (char* ap = &MemoryMarshal.GetReference(source))
            fixed (char* bp = &MemoryMarshal.GetReference(target))
            {
                char* a = ap;
                char* b = bp;

                for (int j = 0; j < target.Length; j++)
                {
                    char targetChar = *(b + j);
                    if (targetChar >= 0x80 || HighCharTable[targetChar])
                        goto InteropCall;
                }

                if (target.Length > source.Length)
                {
                    for (int k = 0; k < source.Length; k++)
                    {
                        char targetChar = *(a + k);
                        if (targetChar >= 0x80 || HighCharTable[targetChar])
                            goto InteropCall;
                    }
                    return -1;
                }

                int startIndex, endIndex, jump;
                if (fromBeginning)
                {
                    // Left to right, from zero to last possible index in the source string.
                    // Incrementing by one after each iteration. Stop condition is last possible index plus 1.
                    startIndex = 0;
                    endIndex = source.Length - target.Length + 1;
                    jump = 1;
                }
                else
                {
                    // Right to left, from first possible index in the source string to zero.
                    // Decrementing by one after each iteration. Stop condition is last possible index minus 1.
                    startIndex = source.Length - target.Length;
                    endIndex = -1;
                    jump = -1;
                }

                for (int i = startIndex; i != endIndex; i += jump)
                {
                    int targetIndex = 0;
                    int sourceIndex = i;

                    for (; targetIndex < target.Length; targetIndex++, sourceIndex++)
                    {
                        char valueChar = *(a + sourceIndex);
                        char targetChar = *(b + targetIndex);

                        if (valueChar >= 0x80 || HighCharTable[valueChar])
                            goto InteropCall;

                        if (valueChar == targetChar)
                        {
                            continue;
                        }

                        // The match may be affected by special character. Verify that the following character is regular ASCII.
                        if (sourceIndex < source.Length - 1 && *(a + sourceIndex + 1) >= 0x80)
                            goto InteropCall;
                        goto Next;
                    }

                    // The match may be affected by special character. Verify that the following character is regular ASCII.
                    if (sourceIndex < source.Length && *(a + sourceIndex) >= 0x80)
                        goto InteropCall;
                    if (matchLengthPtr != null)
                        *matchLengthPtr = target.Length;
                    return i;

                Next: ;
                }

                return -1;

            InteropCall:
                return Interop.JsGlobalization.IndexOf(out exceptionMessage, m_name, b, target.Length, a, source.Length, options, matchLengthPtr, fromBeginning);
            }
        }

        private static bool ComparisonOptionsNotSupported(CompareOptions options) =>
            (options & CompareOptions.IgnoreWidth) == CompareOptions.IgnoreWidth ||
            ((options & CompareOptions.IgnoreNonSpace) == CompareOptions.IgnoreNonSpace && (options & CompareOptions.IgnoreKanaType) != CompareOptions.IgnoreKanaType);

        private static bool IndexingOptionsNotSupported(CompareOptions options) =>
            (options & CompareOptions.IgnoreSymbols) == CompareOptions.IgnoreSymbols;

        private static string GetPNSE(CompareOptions options) =>
            $"CompareOptions = {options} are not supported when HybridGlobalization=true. Disable it to load larger ICU bundle, then use this option.";

        private static bool ComparisonOptionsNotSupportedForCulture(CompareOptions options, string cultureName) =>
            (options == CompareOptions.IgnoreKanaType &&
            (string.IsNullOrEmpty(cultureName) || cultureName.Split('-')[0] != "ja")) ||
            (options == CompareOptions.None &&
            (cultureName.Split('-')[0] == "ja"));

        private static string GetPNSEForCulture(CompareOptions options, string cultureName) =>
            $"CompareOptions = {options} are not supported for culture = {cultureName} when HybridGlobalization=true. Disable it to load larger ICU bundle, then use this option.";
    }
}
