// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public partial class CompareInfo
    {      
        internal unsafe CompareInfo(CultureInfo culture)
        {
            // TODO: Implement This Fully.
        }

        internal static int IndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Contract.Assert(source != null);
            Contract.Assert(value != null);

            // TODO: Implement This Fully.
            if (ignoreCase)
            {
                source = source.ToUpper(CultureInfo.InvariantCulture);
                value = value.ToUpper(CultureInfo.InvariantCulture);
            }

            source = source.Substring(startIndex, count);

            if (value.Length > source.Length) return -1;

            for (int i = 0; i < source.Length; i++)
            {
                for (int j = 0; j < value.Length; j++) {
                   if (source[i + j] != value[j]) {
                       break;
                   }

                   if (j == value.Length - 1) {
                       return i + startIndex;
                   }
                }
            }

            return -1;
        }

        internal static int LastIndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Contract.Assert(source != null);
            Contract.Assert(value != null);

            // TODO: Implement This Fully.
            if (ignoreCase)
            {
                source = source.ToUpper(CultureInfo.InvariantCulture);
                value = value.ToUpper(CultureInfo.InvariantCulture);
            }

            source = source.Substring(startIndex, count);

            int last = -1;
            int cur = 0;

            while((cur = IndexOfOrdinal(source, value, 0, source.Length, false)) != -1)
            {
                last = cur;
                source = source.Substring(last + value.Length);
            }

            return last;
        }

        private unsafe int GetHashCodeOfStringCore(string source, CompareOptions options)
        {
            Contract.Assert(source != null);
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            // TODO: Implement This Fully.
            int hash = 5381;

            unchecked
            {
                for (int i = 0; i < source.Length; i++)
                {
                    hash = ((hash << 5) + hash) + TextInfo.ChangeCaseAscii(source[i]);
                }
            }

            return hash;
        }

        [System.Security.SecuritySafeCritical]
        private static unsafe int CompareStringOrdinalIgnoreCase(char* string1, int count1, char* string2, int count2)
        {
            // TODO: Implement This Fully.            
            return CompareStringOrdinalAscii(string1, count1, string2, count2, ignoreCase: true);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe int CompareString(string string1, int offset1, int length1, string string2, int offset2, int length2, CompareOptions options)
        {
            Contract.Assert(string1 != null);
            Contract.Assert(string2 != null);
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            // TODO: Implement This Fully.
            string s1 = string1.Substring(offset1, length1);
            string s2 = string2.Substring(offset2, length2);

            fixed (char* c1 = s1)
            {
                fixed (char* c2 = s2)
                {
                    return CompareStringOrdinalAscii(c1, s1.Length, c2, s2.Length, IgnoreCase(options));
                }
            }
        }

        private int IndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(target != null);
            Contract.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            // TODO: Implement This Fully.
            return IndexOfOrdinal(source, target, startIndex, count, IgnoreCase(options));
        }

        private int LastIndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(target != null);
            Contract.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            // TODO: Implement This Fully.
            return LastIndexOfOrdinal(source, target, startIndex, count, IgnoreCase(options));
        }

        private bool StartsWith(string source, string prefix, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(!string.IsNullOrEmpty(prefix));
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            // TODO: Implement This Fully.
            if(prefix.Length > source.Length) return false;

            return StringEqualsAscii(source.Substring(0, prefix.Length), prefix, IgnoreCase(options));
        }

        private bool EndsWith(string source, string suffix, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(!string.IsNullOrEmpty(suffix));
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            // TODO: Implement This Fully.
            if(suffix.Length > source.Length) return false;

            return StringEqualsAscii(source.Substring(source.Length - suffix.Length), suffix, IgnoreCase(options));
        }

        // PAL ends here

        private static bool StringEqualsAscii(string s1, string s2, bool ignoreCase = true)
        {
            if (s1.Length != s2.Length) return false;

            for (int i = 0; i < s1.Length; i++)
            {
                char c1 = ignoreCase ? TextInfo.ChangeCaseAscii(s1[i]) : s1[i];
                char c2 = ignoreCase ? TextInfo.ChangeCaseAscii(s2[i]) : s2[i];

                if (c1 != c2) return false;
            }

            return true;
        }

        [System.Security.SecuritySafeCritical]
        private static unsafe int CompareStringOrdinalAscii(char* s1, int count1, char* s2, int count2, bool ignoreCase)
        {
            int countMin = Math.Min(count1, count2);
            {
                for (int i = 0; i < countMin; i++)
                {
                    char c1 = ignoreCase ? TextInfo.ChangeCaseAscii(s1[i]) : s1[i];
                    char c2 = ignoreCase ? TextInfo.ChangeCaseAscii(s2[i]) : s2[i];

                    if (TextInfo.ChangeCaseAscii(s1[i]) < TextInfo.ChangeCaseAscii(s2[i]))
                    {
                        return -1;
                    }
                    else if (TextInfo.ChangeCaseAscii(s1[i]) > TextInfo.ChangeCaseAscii(s2[i]))
                    {
                        return 1;
                    }
                }
            }

            if (count1 == count2) return 0;
            if (count1 > count2) return 1;

            return -1;
        }

        private static bool IgnoreCase(CompareOptions options)
        {
            return ((options & CompareOptions.IgnoreCase) == CompareOptions.IgnoreCase);
        }
    }
}