// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public partial class CompareInfo
    {      
        internal unsafe CompareInfo(CultureInfo culture)
        {
            this.m_name = culture.m_name;
            InitSort(culture);
        }

        private void InitSort(CultureInfo culture)
        {
            this.m_sortName = culture.SortName;

            const uint LCMAP_SORTHANDLE = 0x20000000;
            long handle;
            int ret = Interop.mincore.LCMapStringEx(m_sortName, LCMAP_SORTHANDLE, null, 0, (IntPtr)(&handle), IntPtr.Size, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            _sortHandle = ret > 0 ? (IntPtr)handle : IntPtr.Zero;
        }

        internal static int IndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Contract.Assert(source != null);
            Contract.Assert(value != null);

            return Interop.mincore.FindStringOrdinal(FIND_FROMSTART, source, startIndex, count, value, value.Length, ignoreCase);

        }

        internal static int LastIndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Contract.Assert(source != null);
            Contract.Assert(value != null);

            return Interop.mincore.FindStringOrdinal(FIND_FROMEND, source, startIndex - count + 1, count, value, value.Length, ignoreCase);
        }

        private unsafe int GetHashCodeOfStringCore(string source, CompareOptions options)
        {
            Contract.Assert(source != null);
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            if (source.Length == 0)
            {
                return 0;
            }

            int tmpHash = 0;
            
            if (Interop.mincore.LCMapStringEx(m_sortName, 
                                              LCMAP_HASH | (uint)GetNativeCompareFlags(options), 
                                              source, source.Length, 
                                              (IntPtr)(&tmpHash), sizeof(int), 
                                              IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) == 0)
            {
                Environment.FailFast("LCMapStringEx failed!");
            }

            return tmpHash;
        }

        private static unsafe int CompareStringOrdinalIgnoreCase(char* string1, int count1, char* string2, int count2)
        {
            // Use the OS to compare and then convert the result to expected value by subtracting 2 
            return Interop.mincore.CompareStringOrdinal(string1, count1, string2, count2, true) - 2;
        }

        private int CompareString(string string1, int offset1, int length1, string string2, int offset2, int length2, CompareOptions options)
        {
            Contract.Assert(string1 != null);
            Contract.Assert(string2 != null);
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            int result = Interop.mincore.CompareStringEx(_sortHandle != IntPtr.Zero ? null : m_sortName,
                                                         GetNativeCompareFlags(options),
                                                         string1, offset1, length1,
                                                         string2, offset2, length2,
                                                         _sortHandle);

            if (result == 0)
            {
                Environment.FailFast("CompareStringEx failed");
            }

            // Map CompareStringEx return value to -1, 0, 1.
            return result - 2;
        }

        private int IndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(target != null);
            Contract.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            if (target.Length == 0)
                return startIndex;       // keep Whidbey compatibility

            if ((options & CompareOptions.Ordinal) != 0)
            {
                return FastIndexOfString(source, target, startIndex, count, target.Length, findLastIndex: false);
            }
            else
            {
                int retValue = Interop.mincore.FindNLSStringEx(_sortHandle != IntPtr.Zero ? null : m_sortName,
                                                               FIND_FROMSTART | (uint)GetNativeCompareFlags(options),
                                                               source,
                                                               startIndex,
                                                               count,
                                                               target,
                                                               0,
                                                               target.Length,
                                                               _sortHandle);
                if (retValue >= 0)
                {
                    return retValue + startIndex;
                }
            }

            return -1;
        }

        private int LastIndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(target != null);
            Contract.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            if (target.Length == 0)
                return startIndex;       // keep Whidbey compatibility

            if ((options & CompareOptions.Ordinal) != 0)
            {
                return FastIndexOfString(source, target, startIndex, count, target.Length, findLastIndex: true);
            }
            else
            {
                int retValue = Interop.mincore.FindNLSStringEx(_sortHandle != IntPtr.Zero ? null : m_sortName,
                                                               FIND_FROMEND | (uint)GetNativeCompareFlags(options),
                                                               source,
                                                               startIndex - count + 1,
                                                               count,
                                                               target,
                                                               0,
                                                               target.Length,
                                                               _sortHandle);

                if (retValue >= 0)
                {
                    return retValue + startIndex - (count - 1);
                }
            }

            return -1;        
        }

        private bool StartsWith(string source, string prefix, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(!string.IsNullOrEmpty(prefix));
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return Interop.mincore.FindNLSStringEx(_sortHandle != IntPtr.Zero ? null : m_sortName,
                                                   FIND_STARTSWITH | (uint)GetNativeCompareFlags(options),
                                                   source,
                                                   0,
                                                   source.Length,
                                                   prefix,
                                                   0,
                                                   prefix.Length,
                                                   _sortHandle) >= 0;
        }

        private bool EndsWith(string source, string suffix, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(!string.IsNullOrEmpty(suffix));
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return Interop.mincore.FindNLSStringEx(_sortHandle != IntPtr.Zero ? null : m_sortName,
                                                   FIND_ENDSWITH | (uint)GetNativeCompareFlags(options),
                                                   source,
                                                   0,
                                                   source.Length,
                                                   suffix,
                                                   0,
                                                   suffix.Length,
                                                   _sortHandle) >= 0;

        }

        // PAL ends here
        [NonSerialized]
        private readonly IntPtr _sortHandle;

        private const uint LCMAP_HASH = 0x00040000;

        private const int FIND_STARTSWITH = 0x00100000;
        private const int FIND_ENDSWITH = 0x00200000;
        private const int FIND_FROMSTART = 0x00400000;
        private const int FIND_FROMEND = 0x00800000;

        private static unsafe int FastIndexOfString(string source, string target, int startIndex, int sourceCount, int targetCount, bool findLastIndex)
        {
            int retValue = -1;

            int sourceStartIndex = findLastIndex ? startIndex - sourceCount + 1 : startIndex;

#if !TEST_CODEGEN_OPTIMIZATION
            fixed (char* pSource = source, spTarget = target)
            {
                char* spSubSource = pSource + sourceStartIndex;
#else
                String.StringPointer spSubSource = source.GetStringPointer(sourceStartIndex);
                String.StringPointer spTarget = target.GetStringPointer();
#endif
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
#if !TEST_CODEGEN_OPTIMIZATION
            }

            return retValue;
#endif // TEST_CODEGEN_OPTIMIZATION
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

            // Suffix & Prefix shouldn't use this, make sure to turn off the NORM_LINGUISTIC_CASING flag
            if (options == CompareOptions.Ordinal) { nativeCompareFlags = COMPARE_OPTIONS_ORDINAL; }

            Contract.Assert(((options & ~(CompareOptions.IgnoreCase |
                                          CompareOptions.IgnoreKanaType |
                                          CompareOptions.IgnoreNonSpace |
                                          CompareOptions.IgnoreSymbols |
                                          CompareOptions.IgnoreWidth |
                                          CompareOptions.StringSort)) == 0) ||
                             (options == CompareOptions.Ordinal), "[CompareInfo.GetNativeCompareFlags]Expected all flags to be handled");

            return nativeCompareFlags;
        }
    }
}
