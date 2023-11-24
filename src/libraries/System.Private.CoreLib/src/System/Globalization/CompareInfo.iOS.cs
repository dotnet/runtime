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

        private const CompareOptions SupportedCompareOptions = CompareOptions.None | CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreNonSpace |
                                                               CompareOptions.IgnoreWidth | CompareOptions.StringSort;

        private static string GetPNSE(CompareOptions options) =>
            SR.Format(SR.PlatformNotSupported_HybridGlobalizationWithCompareOptions, options);

        private unsafe SortKey CreateSortKeyNative(string source, CompareOptions options)
        {
            ArgumentNullException.ThrowIfNull(source);

            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            byte[] keyData;
            fixed (char* pSource = source)
            {
                int sortKeyLength = Interop.Globalization.GetSortKeyNative(m_name, m_name.Length, pSource, source.Length, null, 0, options);
                keyData = new byte[sortKeyLength];

                fixed (byte* pSortKey = keyData)
                {
                    if (Interop.Globalization.GetSortKeyNative(m_name, m_name.Length, pSource, source.Length, pSortKey, sortKeyLength, options) != sortKeyLength)
                    {
                        throw new ArgumentException(SR.Arg_ExternalException);
                    }
                }
            }

            return new SortKey(this, source, options, keyData);
        }

        private unsafe int GetSortKeyNative(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            // It's ok to pass nullptr (for empty buffers) to ICU's sort key routines.

            int actualSortKeyLength;

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (byte* pDest = &MemoryMarshal.GetReference(destination))
            {
                actualSortKeyLength = Interop.Globalization.GetSortKeyNative(m_name, m_name.Length, pSource, source.Length, pDest, destination.Length, options);
            }

            // The check below also handles errors due to negative values / overflow being returned.

            if ((uint)actualSortKeyLength > (uint)destination.Length)
            {
                if (actualSortKeyLength > destination.Length)
                {
                    ThrowHelper.ThrowArgumentException_DestinationTooShort();
                }
                else
                {
                    throw new ArgumentException(SR.Arg_ExternalException);
                }
            }

            return actualSortKeyLength;
        }

        private unsafe int GetSortKeyLengthNative(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            // It's ok to pass nullptr (for empty buffers) to ICU's sort key routines.

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            {
                return Interop.Globalization.GetSortKeyNative(m_name, m_name.Length, pSource, source.Length, null, 0, options);
            }
        }

        private unsafe int GetHashCodeOfStringNative(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            // according to ICU User Guide the performance of ucol_getSortKey is worse when it is called with null output buffer
            // the solution is to try to fill the sort key in a temporary buffer of size equal 4 x string length
            // (The ArrayPool used to have a limit on the length of buffers it would cache; this code was avoiding
            // exceeding that limit to avoid a per-operation allocation, and the performance implications here
            // were not re-evaluated when the limit was lifted.)
            int sortKeyLength = (source.Length > 1024 * 1024 / 4) ? 0 : 4 * source.Length;

            byte[]? borrowedArray = null;
            Span<byte> sortKey = sortKeyLength <= 1024
                ? stackalloc byte[1024]
                : (borrowedArray = ArrayPool<byte>.Shared.Rent(sortKeyLength));

            fixed (char* pSource = &MemoryMarshal.GetNonNullPinnableReference(source))
            {
                fixed (byte* pSortKey = &MemoryMarshal.GetReference(sortKey))
                {
                    System.Diagnostics.Debug.Write("Interop.Globalization.GetSortKeyNative before sortKeyLength is " + sortKeyLength + " for string length " + source.Length + " and sortKey.Length " + sortKey.Length + " and options " + options);
                    sortKeyLength = Interop.Globalization.GetSortKeyNative(m_name, m_name.Length, pSource, source.Length, pSortKey, sortKey.Length, options);
                    System.Diagnostics.Debug.Write("Interop.Globalization.GetSortKeyNative after sortKeyLength is " + sortKeyLength + " for string length " + source.Length + " and sortKey.Length " + sortKey.Length + " and options " + options);

                }

                if (sortKeyLength > sortKey.Length) // slow path for big strings
                {
                    if (borrowedArray != null)
                    {
                        ArrayPool<byte>.Shared.Return(borrowedArray);
                    }

                    sortKey = (borrowedArray = ArrayPool<byte>.Shared.Rent(sortKeyLength));

                    fixed (byte* pSortKey = &MemoryMarshal.GetReference(sortKey))
                    {
                        sortKeyLength = Interop.Globalization.GetSortKeyNative(m_name, m_name.Length, pSource, source.Length, pSortKey, sortKey.Length, options);
                        System.Diagnostics.Debug.Write("Interop.Globalization.GetSortKeyNative sortKeyLength is " + sortKeyLength + " for string length " + source.Length);
                    }
                }
            }

            if (sortKeyLength == 0 || sortKeyLength > sortKey.Length) // internal error (0) or a bug (2nd call failed) in ucol_getSortKey
            {
                System.Diagnostics.Debug.Write("Interop.Globalization.GetSortKeyNative returned an invalid value.");
                throw new ArgumentException(SR.Arg_ExternalException);
            }

            int hash = Marvin.ComputeHash32(sortKey.Slice(0, sortKeyLength), Marvin.DefaultSeed);

            if (borrowedArray != null)
            {
                ArrayPool<byte>.Shared.Return(borrowedArray);
            }

            return hash;
        }
    }
}
