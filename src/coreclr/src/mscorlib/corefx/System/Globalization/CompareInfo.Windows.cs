// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        internal unsafe CompareInfo(CultureInfo culture)
        {
            _name = culture._name;
            InitSort(culture);
        }

        private void InitSort(CultureInfo culture)
        {
            _sortName = culture.SortName;

            const uint LCMAP_SORTHANDLE = 0x20000000;

            _name = culture._name;
            _sortName = culture.SortName;

            IntPtr handle;
            int ret = Interop.mincore.LCMapStringEx(_sortName, LCMAP_SORTHANDLE, null, 0, &handle, IntPtr.Size, null, null, IntPtr.Zero);
            _sortHandle = ret > 0 ? handle : IntPtr.Zero;
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
            fixed (char* pSource = stringSource)
            fixed (char* pValue = value)
            {
                int ret = Interop.mincore.FindStringOrdinal(
                            dwFindStringOrdinalFlags,
                            pSource + offset,
                            cchSource,
                            pValue,
                            cchValue,
                            bIgnoreCase ? 1 : 0);
                return ret < 0 ? ret : ret + offset;
            }
        }

        internal static int IndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(source != null);
            Debug.Assert(value != null);

            return FindStringOrdinal(FIND_FROMSTART, source, startIndex, count, value, value.Length, ignoreCase);
        }

        internal static int LastIndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(source != null);
            Debug.Assert(value != null);

            return FindStringOrdinal(FIND_FROMEND, source, startIndex - count + 1, count, value, value.Length, ignoreCase);
        }

        private unsafe int GetHashCodeOfStringCore(string source, CompareOptions options)
        {
            Debug.Assert(source != null);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            if (source.Length == 0)
            {
                return 0;
            }

            int tmpHash = 0;

            fixed (char* pSource = source)
            {
                if (Interop.mincore.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _sortName,
                                                  LCMAP_HASH | (uint)GetNativeCompareFlags(options),
                                                  pSource, source.Length,
                                                  &tmpHash, sizeof(int),
                                                  null, null, _sortHandle) == 0)
                {
                    Environment.FailFast("LCMapStringEx failed!");
                }
            }

            return tmpHash;
        }

        private static unsafe int CompareStringOrdinalIgnoreCase(char* string1, int count1, char* string2, int count2)
        {
            // Use the OS to compare and then convert the result to expected value by subtracting 2 
            return Interop.mincore.CompareStringOrdinal(string1, count1, string2, count2, true) - 2;
        }

        private unsafe int CompareString(string string1, int offset1, int length1, string string2, int offset2, int length2, CompareOptions options)
        {
            Debug.Assert(string1 != null);
            Debug.Assert(string2 != null);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            string localeName = _sortHandle != IntPtr.Zero ? null : _sortName;

            fixed (char* pLocaleName = localeName)
            fixed (char* pString1 = string1)
            fixed (char* pString2 = string2)
            {
                int result = Interop.mincore.CompareStringEx(
                                    pLocaleName,
                                    (uint)GetNativeCompareFlags(options),
                                    pString1 + offset1,
                                    length1,
                                    pString2 + offset2,
                                    length2,
                                    null,
                                    null,
                                    _sortHandle);

                if (result == 0)
                {
                    Environment.FailFast("CompareStringEx failed");
                }

                // Map CompareStringEx return value to -1, 0, 1.
                return result - 2;
            }
        }

        private unsafe int FindString(
                    uint dwFindNLSStringFlags,
                    string lpStringSource,
                    int startSource,
                    int cchSource,
                    string lpStringValue,
                    int startValue,
                    int cchValue)
        {
            string localeName = _sortHandle != IntPtr.Zero ? null : _sortName;

            fixed (char* pLocaleName = localeName)
            fixed (char* pSource = lpStringSource)
            fixed (char* pValue = lpStringValue)
            {
                char* pS = pSource + startSource;
                char* pV = pValue + startValue;

                return Interop.mincore.FindNLSStringEx(
                                    pLocaleName,
                                    dwFindNLSStringFlags,
                                    pS,
                                    cchSource,
                                    pV,
                                    cchValue,
                                    null,
                                    null,
                                    null,
                                    _sortHandle);
            }
        }

        private int IndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(target != null);
            Debug.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            // TODO: Consider moving this up to the relevent APIs we need to ensure this behavior for
            // and add a precondition that target is not empty. 
            if (target.Length == 0)
                return startIndex;       // keep Whidbey compatibility

            if ((options & CompareOptions.Ordinal) != 0)
            {
                return FastIndexOfString(source, target, startIndex, count, target.Length, findLastIndex: false);
            }
            else
            {
                int retValue = FindString(FIND_FROMSTART | (uint)GetNativeCompareFlags(options),
                                                               source,
                                                               startIndex,
                                                               count,
                                                               target,
                                                               0,
                                                               target.Length);
                if (retValue >= 0)
                {
                    return retValue + startIndex;
                }
            }

            return -1;
        }

        private int LastIndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(target != null);
            Debug.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            // TODO: Consider moving this up to the relevent APIs we need to ensure this behavior for
            // and add a precondition that target is not empty. 
            if (target.Length == 0)
                return startIndex;       // keep Whidbey compatibility

            if ((options & CompareOptions.Ordinal) != 0)
            {
                return FastIndexOfString(source, target, startIndex, count, target.Length, findLastIndex: true);
            }
            else
            {
                int retValue = FindString(FIND_FROMEND | (uint)GetNativeCompareFlags(options),
                                                               source,
                                                               startIndex - count + 1,
                                                               count,
                                                               target,
                                                               0,
                                                               target.Length);

                if (retValue >= 0)
                {
                    return retValue + startIndex - (count - 1);
                }
            }

            return -1;
        }

        private bool StartsWith(string source, string prefix, CompareOptions options)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(prefix));
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return FindString(FIND_STARTSWITH | (uint)GetNativeCompareFlags(options),
                                                   source,
                                                   0,
                                                   source.Length,
                                                   prefix,
                                                   0,
                                                   prefix.Length) >= 0;
        }

        private bool EndsWith(string source, string suffix, CompareOptions options)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(suffix));
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return FindString(FIND_ENDSWITH | (uint)GetNativeCompareFlags(options),
                                                   source,
                                                   0,
                                                   source.Length,
                                                   suffix,
                                                   0,
                                                   suffix.Length) >= 0;
        }

        // PAL ends here
        [NonSerialized]
        private readonly IntPtr _sortHandle;

        private const uint LCMAP_HASH = 0x00040000;

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
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            Contract.EndContractBlock();

            if ((options & ValidSortkeyCtorMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            throw new NotImplementedException();
        }

        private static unsafe bool IsSortable(char* text, int length)
        {
            // CompareInfo c = CultureInfo.InvariantCulture.CompareInfo;
            // return (InternalIsSortable(c.m_dataHandle, c.m_handleOrigin, c.m_sortName, text, text.Length));
            throw new NotImplementedException();
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

        private SortVersion GetSortVersion()
        {
            throw new NotImplementedException();
        }
    }
}
