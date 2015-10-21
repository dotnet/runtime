// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        // ICU uses a char* (UTF-8) to represent a locale name.
        private readonly byte[] m_sortNameAsUtf8;

        internal unsafe CompareInfo(CultureInfo culture)
        {
            m_name = culture.m_name;
            m_sortName = culture.SortName;
            m_sortNameAsUtf8 = System.Text.Encoding.UTF8.GetBytes(m_sortName);
        }

        internal static unsafe int IndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Contract.Assert(source != null);
            Contract.Assert(value != null);

            if (value.Length == 0)
            {
                return startIndex;
            }

            if (count < value.Length)
            {
                return -1;
            }

            if (ignoreCase)
            {
                fixed (char* pSource = source)
                {
                    int index = Interop.GlobalizationInterop.IndexOfOrdinalIgnoreCase(value, value.Length, pSource + startIndex, count, findLast: false);
                    return index != -1 ?
                        startIndex + index :
                        -1;
                }
            }

            int endIndex = startIndex + (count - value.Length);
            for (int i = startIndex; i <= endIndex; i++)
            {
                int valueIndex, sourceIndex;

                for (valueIndex = 0, sourceIndex = i;
                     valueIndex < value.Length && source[sourceIndex] == value[valueIndex];
                     valueIndex++, sourceIndex++) ;

                if (valueIndex == value.Length)
                {
                    return i;
                }
            }

            return -1;
        }

        internal static unsafe int LastIndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Contract.Assert(source != null);
            Contract.Assert(value != null);

            if (value.Length == 0)
            {
                return startIndex;
            }

            if (count < value.Length)
            {
                return -1;
            }

            // startIndex is the index into source where we start search backwards from. 
            // leftStartIndex is the index into source of the start of the string that is 
            // count characters away from startIndex.
            int leftStartIndex = startIndex - count + 1;

            if (ignoreCase)
            {
                fixed (char* pSource = source)
                {
                    int lastIndex = Interop.GlobalizationInterop.IndexOfOrdinalIgnoreCase(value, value.Length, pSource + leftStartIndex, count, findLast: true);
                    return lastIndex != -1 ?
                        leftStartIndex + lastIndex :
                        -1;
                }
            }

            for (int i = startIndex - value.Length + 1; i >= leftStartIndex; i--)
            {
                int valueIndex, sourceIndex;

                for (valueIndex = 0, sourceIndex = i;
                     valueIndex < value.Length && source[sourceIndex] == value[valueIndex];
                     valueIndex++, sourceIndex++) ;

                if (valueIndex == value.Length) {
                    return i;
                }
            }

            return -1;
        }

        private unsafe int GetHashCodeOfStringCore(string source, CompareOptions options)
        {
            Contract.Assert(source != null);
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return GetHashCodeOfStringCore(source, options, forceRandomizedHashing: false, additionalEntropy: 0);
        }

        [System.Security.SecuritySafeCritical]
        private static unsafe int CompareStringOrdinalIgnoreCase(char* string1, int count1, char* string2, int count2)
        {
            return Interop.GlobalizationInterop.CompareStringOrdinalIgnoreCase(string1, count1, string2, count2);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe int CompareString(string string1, int offset1, int length1, string string2, int offset2, int length2, CompareOptions options)
        {
            Contract.Assert(string1 != null);
            Contract.Assert(string2 != null);
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            fixed (char* pString1 = string1)
            {
                fixed (char* pString2 = string2)
                {
                    return Interop.GlobalizationInterop.CompareString(m_sortNameAsUtf8, pString1 + offset1, length1, pString2 + offset2, length2, options);
                }
            }
        }

        [System.Security.SecuritySafeCritical]
        private unsafe int IndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(target != null);
            Contract.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            if (target.Length == 0)
            {
                return startIndex;
            }

            if (options == CompareOptions.Ordinal)
            {
                return IndexOfOrdinal(source, target, startIndex, count, ignoreCase: false);
            }

            fixed (char* pSource = source)
            {
                int index = Interop.GlobalizationInterop.IndexOf(m_sortNameAsUtf8, target, pSource + startIndex, count, options);

                return index != -1 ? index + startIndex : -1;
            }
        }

        private unsafe int LastIndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(target != null);
            Contract.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            if (target.Length == 0)
            {
                return startIndex;
            }
            
            if (options == CompareOptions.Ordinal)
            {
                return LastIndexOfOrdinal(source, target, startIndex, count, ignoreCase: false);
            }

            // startIndex is the index into source where we start search backwards from. leftStartIndex is the index into source
            // of the start of the string that is count characters away from startIndex.
            int leftStartIndex = (startIndex - count + 1);

            fixed (char* pSource = source)
            {
                int lastIndex = Interop.GlobalizationInterop.LastIndexOf(m_sortNameAsUtf8, target, pSource + (startIndex - count + 1), count, options);

                return lastIndex != -1 ? lastIndex + leftStartIndex : -1;
            }
        }

        private bool StartsWith(string source, string prefix, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(!string.IsNullOrEmpty(prefix));
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return Interop.GlobalizationInterop.StartsWith(m_sortNameAsUtf8, prefix, source, source.Length, options);
        }

        private bool EndsWith(string source, string suffix, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(!string.IsNullOrEmpty(suffix));
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return Interop.GlobalizationInterop.EndsWith(m_sortNameAsUtf8, suffix, source, source.Length, options);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        internal unsafe int GetHashCodeOfStringCore(string source, CompareOptions options, bool forceRandomizedHashing, long additionalEntropy)
        {
            Contract.Assert(source != null);
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            int sortKeyLength = Interop.GlobalizationInterop.GetSortKey(m_sortNameAsUtf8, source, source.Length, null, 0, options);

            // As an optimization, for small sort keys we allocate the buffer on the stack.
            if (sortKeyLength <= 256)
            {
                byte* pSortKey = stackalloc byte[sortKeyLength];
                Interop.GlobalizationInterop.GetSortKey(m_sortNameAsUtf8, source, source.Length, pSortKey, sortKeyLength, options);
                return InternalHashSortKey(pSortKey, sortKeyLength, false, additionalEntropy);
            }

            byte[] sortKey = new byte[sortKeyLength];

            fixed(byte* pSortKey = sortKey)
            {
                Interop.GlobalizationInterop.GetSortKey(m_sortNameAsUtf8, source, source.Length, pSortKey, sortKeyLength, options);
                return InternalHashSortKey(pSortKey, sortKeyLength, false, additionalEntropy);
            }
        }

        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall)]
        [SuppressUnmanagedCodeSecurity]
        private static unsafe extern int InternalHashSortKey(byte* sortKey, int sortKeyLength, [MarshalAs(UnmanagedType.Bool)] bool forceRandomizedHashing, long additionalEntropy);
    }
}
