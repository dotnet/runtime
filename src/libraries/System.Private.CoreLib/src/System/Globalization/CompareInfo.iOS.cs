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

            // GetReference may return nullptr if the input span is defaulted. The native layer handles
            // this appropriately; no workaround is needed on the managed side.
            int result;
            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &MemoryMarshal.GetReference(string2))
            {
                result = Interop.Globalization.CompareStringNative(m_name, m_name.Length, pString1, string1.Length, pString2, string2.Length, options);
            }

            Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);

            return result;
        }

        private unsafe int IndexOfCoreNative(char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, bool fromBeginning, int* matchLengthPtr)
        {
            AssertComparisonSupported(options);

            Interop.Range result = Interop.Globalization.IndexOfNative(m_name, m_name.Length, target, cwTargetLength, pSource, cwSourceLength, options, fromBeginning);
            Debug.Assert(result.Location != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);
            if (result.Location == (int)ErrorCodes.ERROR_MIXED_COMPOSITION_NOT_FOUND)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_HybridGlobalizationWithMixedCompositions);
            if (matchLengthPtr != null)
                *matchLengthPtr = result.Length;

            return result.Location;
        }

        private unsafe bool NativeStartsWith(char* pPrefix, int cwPrefixLength, char* pSource, int cwSourceLength, CompareOptions options)
        {
            AssertComparisonSupported(options);

            int result = Interop.Globalization.StartsWithNative(m_name, m_name.Length, pPrefix, cwPrefixLength, pSource, cwSourceLength, options);
            Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);

            return result > 0 ? true : false;
        }

        private unsafe bool NativeEndsWith(char* pSuffix, int cwSuffixLength, char* pSource, int cwSourceLength, CompareOptions options)
        {
            AssertComparisonSupported(options);

            int result = Interop.Globalization.EndsWithNative(m_name, m_name.Length, pSuffix, cwSuffixLength, pSource, cwSourceLength, options);
            Debug.Assert(result != (int)ErrorCodes.ERROR_COMPARISON_OPTIONS_NOT_FOUND);

            return result > 0 ? true : false;
        }

        private static void AssertComparisonSupported(CompareOptions options)
        {
            if ((options | SupportedCompareOptions) != SupportedCompareOptions)
                throw new PlatformNotSupportedException(GetPNSE(options));
        }

        private const CompareOptions SupportedCompareOptions = CompareOptions.None | CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace |
                                                               CompareOptions.IgnoreWidth | CompareOptions.StringSort | CompareOptions.IgnoreKanaType;

        private static string GetPNSE(CompareOptions options) =>
            SR.Format(SR.PlatformNotSupported_HybridGlobalizationWithCompareOptions, options);
    }
}
