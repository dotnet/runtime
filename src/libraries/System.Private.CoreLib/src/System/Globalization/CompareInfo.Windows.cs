// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        internal static unsafe IntPtr GetSortHandle(string cultureName)
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

        private void InitSort(CultureInfo culture)
        {
            _sortName = culture.SortName;
            _sortHandle = GetSortHandle(_sortName);
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
                Debug.Assert(ret >= 0 || Marshal.GetLastWin32Error() == Interop.Errors.ERROR_SUCCESS);

                return ret;
            }
        }

        internal static int IndexOfOrdinalCore(ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool ignoreCase, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(source.Length != 0);
            Debug.Assert(value.Length != 0);

            uint positionFlag = fromBeginning ? (uint)FIND_FROMSTART : FIND_FROMEND;
            return FindStringOrdinal(positionFlag, source, value, ignoreCase);
        }

        internal static int LastIndexOfOrdinalCore(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

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

        private unsafe int GetHashCodeOfStringCore(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

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

        private static unsafe int CompareStringOrdinalIgnoreCase(ref char string1, int count1, ref char string2, int count2)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

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

        // TODO https://github.com/dotnet/runtime/issues/8890:
        // This method shouldn't be necessary, as we should be able to just use the overload
        // that takes two spans.  But due to this issue, that's adding significant overhead.
        private unsafe int CompareString(ReadOnlySpan<char> string1, string string2, CompareOptions options)
        {
            Debug.Assert(string2 != null);
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            string? localeName = _sortHandle != IntPtr.Zero ? null : _sortName;

            // CompareStringEx may try to dereference the first character of its input, even if an explicit
            // length of 0 is specified. To work around potential AVs we'll always ensure zero-length inputs
            // are normalized to a null-terminated empty string.

            if (string1.IsEmpty)
            {
                string1 = string.Empty;
            }

            fixed (char* pLocaleName = localeName)
            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &string2.GetPinnableReference())
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

        private unsafe int CompareString(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
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
                Debug.Assert(result >= 0 || Marshal.GetLastWin32Error() == Interop.Errors.ERROR_SUCCESS);

                return result;
            }
        }

        internal unsafe int IndexOfCore(string source, string target, int startIndex, int count, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(target != null);
            Debug.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);
            Debug.Assert((options & CompareOptions.Ordinal) == 0);

            int retValue = FindString(FIND_FROMSTART | (uint)GetNativeCompareFlags(options), source.AsSpan(startIndex, count), target, matchLengthPtr);
            if (retValue >= 0)
            {
                return retValue + startIndex;
            }

            return -1;
        }

        internal unsafe int IndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(target.Length != 0);
            Debug.Assert(options == CompareOptions.None || options == CompareOptions.IgnoreCase);

            uint positionFlag = fromBeginning ? (uint)FIND_FROMSTART : FIND_FROMEND;
            return FindString(positionFlag | (uint)GetNativeCompareFlags(options), source, target, matchLengthPtr);
        }

        private unsafe int LastIndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(target != null);
            Debug.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            // startIndex points to the final char to include in the search space.
            // empty target strings trivially occur at the end of the search space.

            if (target.Length == 0)
                return startIndex + 1;

            if ((options & CompareOptions.Ordinal) != 0)
            {
                return FastLastIndexOfString(source, target, startIndex, count, target.Length);
            }
            else
            {
                int retValue = FindString(FIND_FROMEND | (uint)GetNativeCompareFlags(options), source.AsSpan(startIndex - count + 1, count), target, null);

                if (retValue >= 0)
                {
                    return retValue + startIndex - (count - 1);
                }
            }

            return -1;
        }

        private unsafe bool StartsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return FindString(FIND_STARTSWITH | (uint)GetNativeCompareFlags(options), source, prefix, null) >= 0;
        }

        private unsafe bool EndsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return FindString(FIND_ENDSWITH | (uint)GetNativeCompareFlags(options), source, suffix, null) >= 0;
        }

        // PAL ends here
        [NonSerialized]
        private IntPtr _sortHandle;

        private const uint LCMAP_SORTKEY = 0x00000400;

        private const int FIND_STARTSWITH = 0x00100000;
        private const int FIND_ENDSWITH = 0x00200000;
        private const int FIND_FROMSTART = 0x00400000;
        private const int FIND_FROMEND = 0x00800000;

        // TODO: Instead of this method could we just have upstack code call LastIndexOfOrdinal with ignoreCase = false?
        private static unsafe int FastLastIndexOfString(string source, string target, int startIndex, int sourceCount, int targetCount)
        {
            int retValue = -1;

            int sourceStartIndex = startIndex - sourceCount + 1;

            fixed (char* pSource = source, spTarget = target)
            {
                char* spSubSource = pSource + sourceStartIndex;

                int endPattern = sourceCount - targetCount;
                if (endPattern < 0)
                    return -1;

                Debug.Assert(target.Length >= 1);
                char patternChar0 = spTarget[0];
                for (int ctrSrc = endPattern; ctrSrc >= 0; ctrSrc--)
                {
                    if (spSubSource[ctrSrc] != patternChar0)
                        continue;

                    int ctrPat;
                    for (ctrPat = 1; ctrPat < targetCount; ctrPat++)
                    {
                        if (spSubSource[ctrSrc + ctrPat] != spTarget[ctrPat])
                            break;
                    }
                    if (ctrPat == targetCount)
                    {
                        retValue = ctrSrc;
                        break;
                    }
                }

                if (retValue >= 0)
                {
                    retValue += startIndex - sourceCount + 1;
                }
            }

            return retValue;
        }

        private unsafe SortKey CreateSortKey(string source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

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

        private static unsafe bool IsSortable(char* text, int length)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(text != null);

            return Interop.Kernel32.IsNLSDefinedString(Interop.Kernel32.COMPARE_STRING, 0, IntPtr.Zero, text, length);
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

        private unsafe SortVersion GetSortVersion()
        {
            Debug.Assert(!GlobalizationMode.Invariant);

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
