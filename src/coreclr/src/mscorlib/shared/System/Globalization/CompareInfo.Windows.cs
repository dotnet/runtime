// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        private unsafe void InitSort(CultureInfo culture)
        {
            _sortName = culture.SortName;

            if (_invariantMode)
            {
                _sortHandle = IntPtr.Zero;
            }
            else
            {
                const uint LCMAP_SORTHANDLE = 0x20000000;

                IntPtr handle;
                int ret = Interop.Kernel32.LCMapStringEx(_sortName, LCMAP_SORTHANDLE, null, 0, &handle, IntPtr.Size, null, null, IntPtr.Zero);
                _sortHandle = ret > 0 ? handle : IntPtr.Zero;
            }
        }

        private static unsafe int FindStringOrdinal(
            uint dwFindStringOrdinalFlags,
            string stringSource,
            int offset,
            int cchSource,
            string value,
            int cchValue,
            bool bIgnoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(stringSource != null);
            Debug.Assert(value != null);

            fixed (char* pSource = stringSource)
            fixed (char* pValue = value)
            {
                int ret = Interop.Kernel32.FindStringOrdinal(
                            dwFindStringOrdinalFlags,
                            pSource + offset,
                            cchSource,
                            pValue,
                            cchValue,
                            bIgnoreCase ? 1 : 0);
                return ret < 0 ? ret : ret + offset;
            }
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
                int ret = Interop.Kernel32.FindStringOrdinal(
                            dwFindStringOrdinalFlags,
                            pSource,
                            source.Length,
                            pValue,
                            value.Length,
                            bIgnoreCase ? 1 : 0);
                return ret;
            }
        }

        internal static int IndexOfOrdinalCore(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(source != null);
            Debug.Assert(value != null);

            return FindStringOrdinal(FIND_FROMSTART, source, startIndex, count, value, value.Length, ignoreCase);
        }

        internal static int IndexOfOrdinalCore(ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool ignoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(source.Length != 0);
            Debug.Assert(value.Length != 0);

            return FindStringOrdinal(FIND_FROMSTART, source, value, ignoreCase);
        }

        internal static int LastIndexOfOrdinalCore(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(source != null);
            Debug.Assert(value != null);

            return FindStringOrdinal(FIND_FROMEND, source, startIndex - count + 1, count, value, value.Length, ignoreCase);
        }

        private unsafe int GetHashCodeOfStringCore(string source, CompareOptions options)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(source != null);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            if (source.Length == 0)
            {
                return 0;
            }

            uint flags = LCMAP_SORTKEY | (uint)GetNativeCompareFlags(options);

            fixed (char* pSource = source)
            {
                int sortKeyLength = Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                  flags,
                                                  pSource, source.Length,
                                                  null, 0,
                                                  null, null, _sortHandle);
                if (sortKeyLength == 0)
                {
                    throw new ArgumentException(SR.Arg_ExternalException);
                }

                byte[] borrowedArr = null;
                Span<byte> span = sortKeyLength <= 512 ?
                    stackalloc byte[512] :
                    (borrowedArr = ArrayPool<byte>.Shared.Rent(sortKeyLength));

                fixed (byte* pSortKey = &MemoryMarshal.GetReference(span))
                {
                    if (Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                      flags,
                                                      pSource, source.Length,
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

        private static unsafe int CompareStringOrdinalIgnoreCase(char* string1, int count1, char* string2, int count2)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(string1 != null);
            Debug.Assert(string2 != null);

            // Use the OS to compare and then convert the result to expected value by subtracting 2 
            return Interop.Kernel32.CompareStringOrdinal(string1, count1, string2, count2, true) - 2;
        }

        // TODO https://github.com/dotnet/coreclr/issues/13827:
        // This method shouldn't be necessary, as we should be able to just use the overload
        // that takes two spans.  But due to this issue, that's adding significant overhead.
        private unsafe int CompareString(ReadOnlySpan<char> string1, string string2, CompareOptions options)
        {
            Debug.Assert(string2 != null);
            Debug.Assert(!_invariantMode);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            string localeName = _sortHandle != IntPtr.Zero ? null : _sortName;

            fixed (char* pLocaleName = localeName)
            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &string2.GetRawStringData())
            {
                Debug.Assert(pString1 != null);
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
            Debug.Assert(!_invariantMode);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            string localeName = _sortHandle != IntPtr.Zero ? null : _sortName;

            fixed (char* pLocaleName = localeName)
            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &MemoryMarshal.GetReference(string2))
            {
                Debug.Assert(pString1 != null);
                Debug.Assert(pString2 != null);
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
            Debug.Assert(!_invariantMode);
            Debug.Assert(!lpStringSource.IsEmpty);
            Debug.Assert(!lpStringValue.IsEmpty);

            string localeName = _sortHandle != IntPtr.Zero ? null : _sortName;

            fixed (char* pLocaleName = localeName)
            fixed (char* pSource = &MemoryMarshal.GetReference(lpStringSource))
            fixed (char* pValue = &MemoryMarshal.GetReference(lpStringValue))
            {
                return Interop.Kernel32.FindNLSStringEx(
                                    pLocaleName,
                                    dwFindNLSStringFlags,
                                    pSource,
                                    lpStringSource.Length,
                                    pValue,
                                    lpStringValue.Length,
                                    pcchFound,
                                    null,
                                    null,
                                    _sortHandle);
            }
        }
        
        private unsafe int FindString(
            uint dwFindNLSStringFlags,
            string lpStringSource,
            int startSource,
            int cchSource,
            string lpStringValue,
            int startValue,
            int cchValue,
            int* pcchFound)
        {
            Debug.Assert(!_invariantMode);
            Debug.Assert(lpStringSource != null);
            Debug.Assert(lpStringValue != null);

            string localeName = _sortHandle != IntPtr.Zero ? null : _sortName;

            fixed (char* pLocaleName = localeName)
            fixed (char* pSource = lpStringSource)
            fixed (char* pValue = lpStringValue)
            {
                char* pS = pSource + startSource;
                char* pV = pValue + startValue;

                return Interop.Kernel32.FindNLSStringEx(
                                    pLocaleName,
                                    dwFindNLSStringFlags,
                                    pS,
                                    cchSource,
                                    pV,
                                    cchValue,
                                    pcchFound,
                                    null,
                                    null,
                                    _sortHandle);
            }
        }

        internal unsafe int IndexOfCore(String source, String target, int startIndex, int count, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(source != null);
            Debug.Assert(target != null);
            Debug.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            if (target.Length == 0)
            {
                if (matchLengthPtr != null)
                    *matchLengthPtr = 0;
                return startIndex;
            }

            if (source.Length == 0)
            {
                return -1;
            }

            if ((options & CompareOptions.Ordinal) != 0)
            {
                int retValue = FastIndexOfString(source, target, startIndex, count, target.Length, findLastIndex: false);
                if (retValue >= 0)
                {
                    if (matchLengthPtr != null)
                        *matchLengthPtr = target.Length;
                }
                return retValue;
            }
            else
            {
                int retValue = FindString(FIND_FROMSTART | (uint)GetNativeCompareFlags(options), source, startIndex, count,
                                                               target, 0, target.Length, matchLengthPtr);
                if (retValue >= 0)
                {
                    return retValue + startIndex;
                }
            }

            return -1;
        }

        internal unsafe int IndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(source.Length != 0);
            Debug.Assert(target.Length != 0);
            Debug.Assert((options == CompareOptions.None || options == CompareOptions.IgnoreCase));

            int retValue = FindString(FIND_FROMSTART | (uint)GetNativeCompareFlags(options), source, target, matchLengthPtr);
            return retValue;
        }

        private unsafe int LastIndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(target != null);
            Debug.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            if (target.Length == 0)
                return startIndex;

            if ((options & CompareOptions.Ordinal) != 0)
            {
                return FastIndexOfString(source, target, startIndex, count, target.Length, findLastIndex: true);
            }
            else
            {
                int retValue = FindString(FIND_FROMEND | (uint)GetNativeCompareFlags(options), source, startIndex - count + 1,
                                                               count, target, 0, target.Length, null);

                if (retValue >= 0)
                {
                    return retValue + startIndex - (count - 1);
                }
            }

            return -1;
        }

        private unsafe bool StartsWith(string source, string prefix, CompareOptions options)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(prefix));
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return FindString(FIND_STARTSWITH | (uint)GetNativeCompareFlags(options), source, 0, source.Length,
                                                   prefix, 0, prefix.Length, null) >= 0;
        }

        private unsafe bool StartsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return FindString(FIND_STARTSWITH | (uint)GetNativeCompareFlags(options), source, prefix, null) >= 0;
        }

        private unsafe bool EndsWith(string source, string suffix, CompareOptions options)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(suffix));
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return FindString(FIND_ENDSWITH | (uint)GetNativeCompareFlags(options), source, 0, source.Length,
                                                   suffix, 0, suffix.Length, null) >= 0;
        }

        private unsafe bool EndsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return FindString(FIND_ENDSWITH | (uint)GetNativeCompareFlags(options), source, suffix, null) >= 0;
        }

        // PAL ends here
        [NonSerialized]
        private IntPtr _sortHandle;

        private const uint LCMAP_SORTKEY = 0x00000400;
        private const uint LCMAP_HASH    = 0x00040000;

        private const int FIND_STARTSWITH = 0x00100000;
        private const int FIND_ENDSWITH = 0x00200000;
        private const int FIND_FROMSTART = 0x00400000;
        private const int FIND_FROMEND = 0x00800000;

        // TODO: Instead of this method could we just have upstack code call IndexOfOrdinal with ignoreCase = false?
        private static unsafe int FastIndexOfString(string source, string target, int startIndex, int sourceCount, int targetCount, bool findLastIndex)
        {
            int retValue = -1;

            int sourceStartIndex = findLastIndex ? startIndex - sourceCount + 1 : startIndex;

            fixed (char* pSource = source, spTarget = target)
            {
                char* spSubSource = pSource + sourceStartIndex;

                if (findLastIndex)
                {
                    int startPattern = (sourceCount - 1) - targetCount + 1;
                    if (startPattern < 0)
                        return -1;

                    char patternChar0 = spTarget[0];
                    for (int ctrSrc = startPattern; ctrSrc >= 0; ctrSrc--)
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
                else
                {
                    int endPattern = (sourceCount - 1) - targetCount + 1;
                    if (endPattern < 0)
                        return -1;

                    char patternChar0 = spTarget[0];
                    for (int ctrSrc = 0; ctrSrc <= endPattern; ctrSrc++)
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
                        retValue += startIndex;
                    }
                }
            }

            return retValue;
        }

        private unsafe SortKey CreateSortKey(String source, CompareOptions options)
        {
            Debug.Assert(!_invariantMode);

            if (source == null) { throw new ArgumentNullException(nameof(source)); }

            if ((options & ValidSortkeyCtorMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            byte [] keyData = null;
            if (source.Length == 0)
            { 
                keyData = Array.Empty<byte>();
            }
            else
            {
                uint flags = LCMAP_SORTKEY | (uint)GetNativeCompareFlags(options);

                fixed (char *pSource = source)
                {
                    int sortKeyLength = Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                flags,
                                                pSource, source.Length,
                                                null, 0,
                                                null, null, _sortHandle);
                    if (sortKeyLength == 0)
                    {
                        throw new ArgumentException(SR.Arg_ExternalException);
                    }

                    keyData = new byte[sortKeyLength];

                    fixed (byte* pBytes =  keyData)
                    {
                        if (Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                flags,
                                                pSource, source.Length,
                                                pBytes, keyData.Length,
                                                null, null, _sortHandle) != sortKeyLength)
                        {
                            throw new ArgumentException(SR.Arg_ExternalException);
                        }
                    }
                }
            }

            return new SortKey(Name, source, options, keyData);
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
            Debug.Assert(!_invariantMode);

            Interop.Kernel32.NlsVersionInfoEx nlsVersion = new Interop.Kernel32.NlsVersionInfoEx();
            nlsVersion.dwNLSVersionInfoSize = sizeof(Interop.Kernel32.NlsVersionInfoEx);
            Interop.Kernel32.GetNLSVersionEx(Interop.Kernel32.COMPARE_STRING, _sortName, &nlsVersion);
            return new SortVersion(
                        nlsVersion.dwNLSVersion,
                        nlsVersion.dwEffectiveId == 0 ? LCID : nlsVersion.dwEffectiveId,
                        nlsVersion.guidCustomVersion);
        }
    }
}
