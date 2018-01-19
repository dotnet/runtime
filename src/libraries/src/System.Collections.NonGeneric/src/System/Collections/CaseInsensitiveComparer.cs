// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class: CaseInsensitiveComparer
**
** Purpose: Compares two objects for equivalence,
**          ignoring the case of strings.
**
============================================================*/

using System.Globalization;

namespace System.Collections
{
    public class CaseInsensitiveComparer : IComparer
    {
        private CompareInfo _compareInfo;
        private static volatile CaseInsensitiveComparer s_InvariantCaseInsensitiveComparer;

        public CaseInsensitiveComparer()
        {
            _compareInfo = CultureInfo.CurrentCulture.CompareInfo;
        }

        public CaseInsensitiveComparer(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }
            _compareInfo = culture.CompareInfo;
        }

        public static CaseInsensitiveComparer Default
        {
            get
            {
                return new CaseInsensitiveComparer(CultureInfo.CurrentCulture);
            }
        }

        public static CaseInsensitiveComparer DefaultInvariant
        {
            get
            {
                if (s_InvariantCaseInsensitiveComparer == null)
                {
                    s_InvariantCaseInsensitiveComparer = new CaseInsensitiveComparer(CultureInfo.InvariantCulture);
                }
                return s_InvariantCaseInsensitiveComparer;
            }
        }

        // Behaves exactly like Comparer.Default.Compare except that the comparison is case insensitive
        // Compares two Objects by calling CompareTo.
        // If a == b, 0 is returned.
        // If a implements IComparable, a.CompareTo(b) is returned.
        // If a doesn't implement IComparable and b does, -(b.CompareTo(a)) is returned.
        // Otherwise an exception is thrown.
        // 
        public int Compare(Object a, Object b)
        {
            String sa = a as String;
            String sb = b as String;
            if (sa != null && sb != null)
                return _compareInfo.Compare(sa, sb, CompareOptions.IgnoreCase);
            else
                return Comparer.Default.Compare(a, b);
        }
    }
}
