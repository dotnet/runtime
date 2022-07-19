// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private readonly CompareInfo _compareInfo;
        private static volatile CaseInsensitiveComparer? s_InvariantCaseInsensitiveComparer;

        public CaseInsensitiveComparer()
        {
            _compareInfo = CultureInfo.CurrentCulture.CompareInfo;
        }

        public CaseInsensitiveComparer(CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(culture);

            _compareInfo = culture.CompareInfo;
        }

        public static CaseInsensitiveComparer Default
        {
            get
            {
                return new CaseInsensitiveComparer(CultureInfo.CurrentCulture);
            }
        }

        public static CaseInsensitiveComparer DefaultInvariant =>
            s_InvariantCaseInsensitiveComparer ??= new CaseInsensitiveComparer(CultureInfo.InvariantCulture);

        // Behaves exactly like Comparer.Default.Compare except that the comparison is case insensitive
        // Compares two Objects by calling CompareTo.
        // If a == b, 0 is returned.
        // If a implements IComparable, a.CompareTo(b) is returned.
        // If a doesn't implement IComparable and b does, -(b.CompareTo(a)) is returned.
        // Otherwise an exception is thrown.
        //
        public int Compare(object? a, object? b)
        {
            string? sa = a as string;
            string? sb = b as string;
            if (sa != null && sb != null)
                return _compareInfo.Compare(sa, sb, CompareOptions.IgnoreCase);
            else
                return Comparer.Default.Compare(a, b);
        }
    }
}
