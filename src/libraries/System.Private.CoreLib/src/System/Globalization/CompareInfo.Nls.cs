// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        private void NlsInitSortHandle()
        {
            Debug.Assert(GlobalizationMode.UseNls);
            _sortHandle = NlsGetSortHandle(_sortName);
        }

        internal static unsafe IntPtr NlsGetSortHandle(string cultureName)
        {
            if (GlobalizationMode.Invariant)
            {
                return IntPtr.Zero;
            }

            IntPtr handle;
            int ret = Interop.Kernel32.LCMapStringEx(cultureName, Interop.Kernel32.LCMAP_SORTHANDLE, null, 0, &handle, IntPtr.Size, null, null, IntPtr.Zero);
            if (ret > 0)
            {
                // Even if we can get the sort handle, it is not guaranteed to work when Windows compatibility shim is applied
                // e.g. Windows 7 compatibility mode. We need to ensure it is working before using it.
                // otherwise the whole framework app will not start.
                int hashValue = 0;
                char a = 'a';
                ret = Interop.Kernel32.LCMapStringEx(null, Interop.Kernel32.LCMAP_HASH, &a, 1, &hashValue, sizeof(int), null, null, handle);
                if (ret > 1)
                {
                    return handle;
                }
            }

            return IntPtr.Zero;
        }

        private static unsafe int FindStringOrdinal(
            uint dwFindStringOrdinalFlags,
            ReadOnlySpan<char> source,
            ReadOnlySpan<char> value,
            bool bIgnoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!value.IsEmpty);

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (char* pValue = &MemoryMarshal.GetReference(value))
            {
                Debug.Assert(pSource != null);
                Debug.Assert(pValue != null);

                int ret = Interop.Kernel32.FindStringOrdinal(
                            dwFindStringOrdinalFlags,
                            pSource,
                            source.Length,
                            pValue,
                            value.Length,
                            bIgnoreCase ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);

                Debug.Assert(ret >= -1 && ret <= source.Length);

                // SetLastError is only performed under debug builds.
                Debug.Assert(ret >= 0 || Marshal.GetLastPInvokeError() == Interop.Errors.ERROR_SUCCESS);

                return ret;
            }
        }

        internal static int NlsIndexOfOrdinalCore(ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool ignoreCase, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            Debug.Assert(source.Length != 0);
            Debug.Assert(value.Length != 0);

            uint positionFlag = fromBeginning ? (uint)FIND_FROMSTART : FIND_FROMEND;
            return FindStringOrdinal(positionFlag, source, value, ignoreCase);
        }

        internal static int NlsLastIndexOfOrdinalCore(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            Debug.Assert(source != null);
            Debug.Assert(value != null);

            int offset = startIndex - count + 1;
            int result = FindStringOrdinal(FIND_FROMEND, source.AsSpan(offset, count), value, ignoreCase);
            if (result >= 0)
            {
                result += offset;
            }
            return result;
        }

        private unsafe int NlsGetHashCodeOfString(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

#if TARGET_WINDOWS
            if (!Environment.IsWindows8OrAbove)
            {
                // On Windows 7 / Server 2008, LCMapStringEx exhibits strange behaviors if the destination
                // buffer is both non-null and too small for the required output. To prevent this from
                // causing issues for us, we need to make an immutable copy of the input buffer so that
                // its contents can't change between when we calculate the required sort key length and
                // when we populate the sort key buffer.

                source = source.ToString();
            }
#endif

            // LCMapStringEx doesn't support passing cchSrc = 0, so if given a null or empty input
            // we'll normalize it to an empty null-terminated string and pass -1 to indicate that
            // the underlying OS function should read until it encounters the null terminator.

            int sourceLength = source.Length;
            if (sourceLength == 0)
            {
                source = string.Empty;
                sourceLength = -1;
            }

            uint flags = LCMAP_SORTKEY | (uint)GetNativeCompareFlags(options);

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            {
                int sortKeyLength = Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                  flags,
                                                  pSource, sourceLength /* in chars */,
                                                  null, 0,
                                                  null, null, _sortHandle);
                if (sortKeyLength == 0)
                {
                    throw new ArgumentException(SR.Arg_ExternalException);
                }

                // Note in calls to LCMapStringEx below, the input buffer is specified in wchars (and wchar count),
                // but the output buffer is specified in bytes (and byte count). This is because when generating
                // sort keys, LCMapStringEx treats the output buffer as containing opaque binary data.
                // See https://docs.microsoft.com/en-us/windows/desktop/api/winnls/nf-winnls-lcmapstringex.

                byte[]? borrowedArr = null;
                Span<byte> span = sortKeyLength <= 512 ?
                    stackalloc byte[512] :
                    (borrowedArr = ArrayPool<byte>.Shared.Rent(sortKeyLength));

                fixed (byte* pSortKey = &MemoryMarshal.GetReference(span))
                {
                    if (Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                      flags,
                                                      pSource, sourceLength /* in chars */,
                                                      pSortKey, sortKeyLength,
                                                      null, null, _sortHandle) != sortKeyLength)
                    {
                        throw new ArgumentException(SR.Arg_ExternalException);
                    }
                }

                int hash = Marvin.ComputeHash32(span.Slice(0, sortKeyLength), Marvin.DefaultSeed);

                // Return the borrowed array if necessary.
                if (borrowedArr != null)
                {
                    ArrayPool<byte>.Shared.Return(borrowedArr);
                }

                return hash;
            }
        }

        internal static unsafe int NlsCompareStringOrdinalIgnoreCase(ref char string1, int count1, ref char string2, int count2)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            Debug.Assert(count1 > 0);
            Debug.Assert(count2 > 0);

            fixed (char* char1 = &string1)
            fixed (char* char2 = &string2)
            {
                Debug.Assert(char1 != null);
                Debug.Assert(char2 != null);

                // Use the OS to compare and then convert the result to expected value by subtracting 2
                int result = Interop.Kernel32.CompareStringOrdinal(char1, count1, char2, count2, bIgnoreCase: true);
                if (result == 0)
                {
                    throw new ArgumentException(SR.Arg_ExternalException);
                }
                return result - 2;
            }
        }

        private unsafe int NlsCompareString(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            string? localeName = _sortHandle != IntPtr.Zero ? null : _sortName;

            // CompareStringEx may try to dereference the first character of its input, even if an explicit
            // length of 0 is specified. To work around potential AVs we'll always ensure zero-length inputs
            // are normalized to a null-terminated empty string.

            if (string1.IsEmpty)
            {
                string1 = string.Empty;
            }

            if (string2.IsEmpty)
            {
                string2 = string.Empty;
            }

            fixed (char* pLocaleName = localeName)
            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &MemoryMarshal.GetReference(string2))
            {
                Debug.Assert(*pString1 >= 0); // assert that we can always dereference this
                Debug.Assert(*pString2 >= 0); // assert that we can always dereference this

                int result = Interop.Kernel32.CompareStringEx(
                                    pLocaleName,
                                    (uint)GetNativeCompareFlags(options),
                                    pString1,
                                    string1.Length,
                                    pString2,
                                    string2.Length,
                                    null,
                                    null,
                                    _sortHandle);

                if (result == 0)
                {
                    throw new ArgumentException(SR.Arg_ExternalException);
                }

                // Map CompareStringEx return value to -1, 0, 1.
                return result - 2;
            }
        }

        private unsafe int FindString(
                    uint dwFindNLSStringFlags,
                    ReadOnlySpan<char> lpStringSource,
                    ReadOnlySpan<char> lpStringValue,
                    int* pcchFound)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!lpStringValue.IsEmpty);

            string? localeName = _sortHandle != IntPtr.Zero ? null : _sortName;

            // FindNLSStringEx disallows passing an explicit 0 for cchSource or cchValue.
            // The caller should've already checked that 'lpStringValue' isn't empty,
            // but it's possible for 'lpStringSource' to be empty. In this case we'll
            // substitute an empty null-terminated string and pass -1 so that the NLS
            // function uses the implicit string length.

            int lpStringSourceLength = lpStringSource.Length;
            if (lpStringSourceLength == 0)
            {
                lpStringSource = string.Empty;
                lpStringSourceLength = -1;
            }

            fixed (char* pLocaleName = localeName)
            fixed (char* pSource = &MemoryMarshal.GetReference(lpStringSource))
            fixed (char* pValue = &MemoryMarshal.GetReference(lpStringValue))
            {
                Debug.Assert(pSource != null && pValue != null);

                int result = Interop.Kernel32.FindNLSStringEx(
                                    pLocaleName,
                                    dwFindNLSStringFlags,
                                    pSource,
                                    lpStringSourceLength,
                                    pValue,
                                    lpStringValue.Length,
                                    pcchFound,
                                    null,
                                    null,
                                    _sortHandle);

                Debug.Assert(result >= -1 && result <= lpStringSource.Length);

                // SetLastError is only performed under debug builds.
                Debug.Assert(result >= 0 || Marshal.GetLastPInvokeError() == Interop.Errors.ERROR_SUCCESS);

                return result;
            }
        }

        private unsafe int NlsIndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            Debug.Assert(target.Length != 0);

            uint positionFlag = fromBeginning ? (uint)FIND_FROMSTART : FIND_FROMEND;
            return FindString(positionFlag | (uint)GetNativeCompareFlags(options), source, target, matchLengthPtr);
        }

        private unsafe bool NlsStartsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            int idx = FindString(FIND_STARTSWITH | (uint)GetNativeCompareFlags(options), source, prefix, matchLengthPtr);
            if (idx >= 0)
            {
                if (matchLengthPtr != null)
                {
                    *matchLengthPtr += idx; // account for chars we skipped at the front of the string
                }
                return true;
            }

            return false;
        }

        private unsafe bool NlsEndsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            int idx = FindString(FIND_ENDSWITH | (uint)GetNativeCompareFlags(options), source, suffix, pcchFound: null);
            if (idx >= 0)
            {
                if (matchLengthPtr != null)
                {
                    *matchLengthPtr = source.Length - idx; // all chars from idx to the end of the string are consumed
                }
                return true;
            }

            return false;
        }

        private const uint LCMAP_SORTKEY = 0x00000400;

        private const int FIND_STARTSWITH = 0x00100000;
        private const int FIND_ENDSWITH = 0x00200000;
        private const int FIND_FROMSTART = 0x00400000;
        private const int FIND_FROMEND = 0x00800000;

        private unsafe SortKey NlsCreateSortKey(string source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            if (source == null) { throw new ArgumentNullException(nameof(source)); }

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            byte[] keyData;
            uint flags = LCMAP_SORTKEY | (uint)GetNativeCompareFlags(options);

            // LCMapStringEx doesn't support passing cchSrc = 0, so if given an empty string
            // we'll instead pass -1 to indicate a null-terminated empty string.

            int sourceLength = source.Length;
            if (sourceLength == 0)
            {
                sourceLength = -1;
            }

            fixed (char* pSource = source)
            {
                int sortKeyLength = Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                            flags,
                                            pSource, sourceLength,
                                            null, 0,
                                            null, null, _sortHandle);
                if (sortKeyLength == 0)
                {
                    throw new ArgumentException(SR.Arg_ExternalException);
                }

                keyData = new byte[sortKeyLength];

                fixed (byte* pBytes = keyData)
                {
                    if (Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                            flags,
                                            pSource, sourceLength,
                                            pBytes, keyData.Length,
                                            null, null, _sortHandle) != sortKeyLength)
                    {
                        throw new ArgumentException(SR.Arg_ExternalException);
                    }
                }
            }

            return new SortKey(this, source, options, keyData);
        }

        private unsafe int NlsGetSortKey(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            // LCMapStringEx doesn't allow cchDest = 0 unless we're trying to query
            // the total number of bytes necessary.

            if (destination.IsEmpty)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

#if TARGET_WINDOWS
            if (!Environment.IsWindows8OrAbove)
            {
                // On Windows 7 / Server 2008, LCMapStringEx exhibits strange behaviors if the destination
                // buffer is both non-null and too small for the required output. To prevent this from
                // causing issues for us, we need to make an immutable copy of the input buffer so that
                // its contents can't change between when we calculate the required sort key length and
                // when we populate the sort key buffer.

                source = source.ToString();
            }
#endif

            uint flags = LCMAP_SORTKEY | (uint)GetNativeCompareFlags(options);

            // LCMapStringEx doesn't support passing cchSrc = 0, so if given an empty span
            // we'll instead normalize to a null-terminated empty string and pass -1 as
            // the length to indicate that the implicit null terminator should be used.

            int sourceLength = source.Length;
            if (sourceLength == 0)
            {
                source = string.Empty;
                sourceLength = -1;
            }

            int actualSortKeyLength;

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (byte* pSortKey = &MemoryMarshal.GetReference(destination))
            {
                Debug.Assert(pSource != null);
                Debug.Assert(pSortKey != null);

#if TARGET_WINDOWS
                if (!Environment.IsWindows8OrAbove)
                {
                    // Manually check that the destination buffer is large enough to hold the full output.
                    // See earlier comment for reasoning.

                    int requiredSortKeyLength = Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                                               flags,
                                                                               pSource, sourceLength,
                                                                               null, 0,
                                                                               null, null, _sortHandle);

                    if (requiredSortKeyLength > destination.Length)
                    {
                        ThrowHelper.ThrowArgumentException_DestinationTooShort();
                    }

                    if (requiredSortKeyLength <= 0)
                    {
                        throw new ArgumentException(SR.Arg_ExternalException);
                    }
                }
#endif

                actualSortKeyLength = Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                                     flags,
                                                                     pSource, sourceLength,
                                                                     pSortKey, destination.Length,
                                                                     null, null, _sortHandle);
            }

            if (actualSortKeyLength <= 0)
            {
                Debug.Assert(actualSortKeyLength == 0, "LCMapStringEx should never return a negative value.");

                // This could fail for a variety of reasons, including NLS being unable
                // to allocate a temporary buffer large enough to hold intermediate state,
                // or the destination buffer being too small.

                if (Marshal.GetLastPInvokeError() == Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                {
                    ThrowHelper.ThrowArgumentException_DestinationTooShort();
                }
                else
                {
                    throw new ArgumentException(SR.Arg_ExternalException);
                }
            }

            Debug.Assert(actualSortKeyLength <= destination.Length);
            return actualSortKeyLength;
        }

        private unsafe int NlsGetSortKeyLength(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            uint flags = LCMAP_SORTKEY | (uint)GetNativeCompareFlags(options);

            // LCMapStringEx doesn't support passing cchSrc = 0, so if given an empty span
            // we'll instead normalize to a null-terminated empty string and pass -1 as
            // the length to indicate that the implicit null terminator should be used.

            int sourceLength = source.Length;
            if (sourceLength == 0)
            {
                source = string.Empty;
                sourceLength = -1;
            }

            int sortKeyLength;

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            {
                Debug.Assert(pSource != null);
                sortKeyLength = Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                               flags,
                                                               pSource, sourceLength,
                                                               null, 0,
                                                               null, null, _sortHandle);
            }

            if (sortKeyLength <= 0)
            {
                Debug.Assert(sortKeyLength == 0, "LCMapStringEx should never return a negative value.");

                // This could fail for a variety of reasons, including NLS being unable
                // to allocate a temporary buffer large enough to hold intermediate state.

                throw new ArgumentException(SR.Arg_ExternalException);
            }

            return sortKeyLength;
        }

        private static unsafe bool NlsIsSortable(ReadOnlySpan<char> text)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);
            Debug.Assert(!text.IsEmpty);

            fixed (char* pText = &MemoryMarshal.GetReference(text))
            {
                return Interop.Kernel32.IsNLSDefinedString(Interop.Kernel32.COMPARE_STRING, 0, IntPtr.Zero, pText, text.Length);
            }
        }

        private const int COMPARE_OPTIONS_ORDINAL = 0x40000000;       // Ordinal
        private const int NORM_IGNORECASE = 0x00000001;       // Ignores case.  (use LINGUISTIC_IGNORECASE instead)
        private const int NORM_IGNOREKANATYPE = 0x00010000;       // Does not differentiate between Hiragana and Katakana characters. Corresponding Hiragana and Katakana will compare as equal.
        private const int NORM_IGNORENONSPACE = 0x00000002;       // Ignores nonspacing. This flag also removes Japanese accent characters.  (use LINGUISTIC_IGNOREDIACRITIC instead)
        private const int NORM_IGNORESYMBOLS = 0x00000004;       // Ignores symbols.
        private const int NORM_IGNOREWIDTH = 0x00020000;       // Does not differentiate between a single-byte character and the same character as a double-byte character.
        private const int NORM_LINGUISTIC_CASING = 0x08000000;       // use linguistic rules for casing
        private const int SORT_STRINGSORT = 0x00001000;       // Treats punctuation the same as symbols.

        private static int GetNativeCompareFlags(CompareOptions options)
        {
            // Use "linguistic casing" by default (load the culture's casing exception tables)
            int nativeCompareFlags = NORM_LINGUISTIC_CASING;

            if ((options & CompareOptions.IgnoreCase) != 0) { nativeCompareFlags |= NORM_IGNORECASE; }
            if ((options & CompareOptions.IgnoreKanaType) != 0) { nativeCompareFlags |= NORM_IGNOREKANATYPE; }
            if ((options & CompareOptions.IgnoreNonSpace) != 0) { nativeCompareFlags |= NORM_IGNORENONSPACE; }
            if ((options & CompareOptions.IgnoreSymbols) != 0) { nativeCompareFlags |= NORM_IGNORESYMBOLS; }
            if ((options & CompareOptions.IgnoreWidth) != 0) { nativeCompareFlags |= NORM_IGNOREWIDTH; }
            if ((options & CompareOptions.StringSort) != 0) { nativeCompareFlags |= SORT_STRINGSORT; }

            // TODO: Can we try for GetNativeCompareFlags to never
            // take Ordinal or OrdinalIgnoreCase.  This value is not part of Win32, we just handle it special
            // in some places.
            // Suffix & Prefix shouldn't use this, make sure to turn off the NORM_LINGUISTIC_CASING flag
            if (options == CompareOptions.Ordinal) { nativeCompareFlags = COMPARE_OPTIONS_ORDINAL; }

            Debug.Assert(((options & ~(CompareOptions.IgnoreCase |
                                          CompareOptions.IgnoreKanaType |
                                          CompareOptions.IgnoreNonSpace |
                                          CompareOptions.IgnoreSymbols |
                                          CompareOptions.IgnoreWidth |
                                          CompareOptions.StringSort)) == 0) ||
                             (options == CompareOptions.Ordinal), "[CompareInfo.GetNativeCompareFlags]Expected all flags to be handled");

            return nativeCompareFlags;
        }

        private unsafe SortVersion NlsGetSortVersion()
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            Interop.Kernel32.NlsVersionInfoEx nlsVersion = default;
            nlsVersion.dwNLSVersionInfoSize = sizeof(Interop.Kernel32.NlsVersionInfoEx);
            Interop.Kernel32.GetNLSVersionEx(Interop.Kernel32.COMPARE_STRING, _sortName, &nlsVersion);
            return new SortVersion(
                        nlsVersion.dwNLSVersion,
                        nlsVersion.dwEffectiveId == 0 ? LCID : nlsVersion.dwEffectiveId,
                        nlsVersion.guidCustomVersion);
        }
    }
}
