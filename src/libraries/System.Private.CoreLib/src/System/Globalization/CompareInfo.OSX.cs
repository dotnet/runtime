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
        private void InitNative(string interopCultureName)
        {
            _isAsciiEqualityOrdinal = GetIsAsciiEqualityOrdinal(interopCultureName);
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

            Debug.Assert(result != -2);

            return result;
        }

        private unsafe int IndexOfCoreNative(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(target.Length != 0);

            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                System.Diagnostics.Debug.WriteLine("Collation function IndexOfCoreNative if case is callled from CompareInfo.OSX.cs");
                if ((options & CompareOptions.IgnoreCase) != 0)
                    return IndexOfOrdinalIgnoreCaseHelper(source, target, options, matchLengthPtr, fromBeginning);
                else
                    return IndexOfOrdinalHelper(source, target, options, matchLengthPtr, fromBeginning);
            }
            else
            {
                // GetReference may return nullptr if the input span is defaulted. The native layer handles
                // this appropriately; no workaround is needed on the managed side.

                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                fixed (char* pTarget = &MemoryMarshal.GetReference(target))
                {
                    NSRange result = Interop.Globalization.IndexOfNative(m_name, m_name.Length, pTarget, target.Length, pSource, source.Length, options, fromBeginning);
                    if (matchLengthPtr == null)
                        matchLengthPtr = &result.Length;
                    *matchLengthPtr = result.Length;
                    return result.Location;
                }
            }
        }

        private unsafe bool NativeStartsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            fixed (char* pSource = &MemoryMarshal.GetReference(source)) // could be null (or otherwise unable to be dereferenced)
            fixed (char* pPrefix = &MemoryMarshal.GetReference(prefix))
            {
                return Interop.Globalization.StartsWithNative(m_name, m_name.Length, pPrefix, prefix.Length, pSource, source.Length, options);
            }
        }

        private unsafe bool NativeEndsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            fixed (char* pSource = &MemoryMarshal.GetReference(source)) // could be null (or otherwise unable to be dereferenced)
            fixed (char* pSuffix = &MemoryMarshal.GetReference(suffix))
            {
                return Interop.Globalization.EndsWithNative(m_name, m_name.Length, pSuffix, suffix.Length, pSource, source.Length, options);
            }
        }

        private static void AssertComparisonSupported(CompareOptions options)
        {
            if ((options | SupportedCompareOptions) != SupportedCompareOptions)
                throw new PlatformNotSupportedException(GetPNSE(options));
        }

        private const CompareOptions SupportedCompareOptions = CompareOptions.None | CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace |
                                                               CompareOptions.IgnoreWidth | CompareOptions.StringSort;

        private static string GetPNSE(CompareOptions options) =>
            SR.Format(SR.PlatformNotSupported_HybridGlobalizationWithCompareOptions, options);
    }
}
