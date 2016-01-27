// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class:  Comparer
**
** Purpose: Compares two objects for equivalence,
**          where string comparisons are case-sensitive.
**
===========================================================*/

using System;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System.Collections
{
    public sealed class Comparer : IComparer
    {
        private CompareInfo _compareInfo;
        public static readonly Comparer Default = new Comparer(CultureInfo.CurrentCulture);
        public static readonly Comparer DefaultInvariant = new Comparer(CultureInfo.InvariantCulture);

        private const String CompareInfoName = "CompareInfo";

        private Comparer()
        {
            _compareInfo = null;
        }

        public Comparer(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }
            Contract.EndContractBlock();
            _compareInfo = culture.CompareInfo;
        }

        // Compares two Objects by calling CompareTo.
        // If a == b, 0 is returned.
        // If a implements IComparable, a.CompareTo(b) is returned.
        // If a doesn't implement IComparable and b does, -(b.CompareTo(a)) is returned.
        // Otherwise an exception is thrown.
        // 
        public int Compare(Object a, Object b)
        {
            if (a == b) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            if (_compareInfo != null)
            {
                String sa = a as String;
                String sb = b as String;
                if (sa != null && sb != null)
                    return _compareInfo.Compare(sa, sb);
            }

            IComparable ia = a as IComparable;
            if (ia != null)
                return ia.CompareTo(b);

            IComparable ib = b as IComparable;
            if (ib != null)
                return -ib.CompareTo(a);

            throw new ArgumentException(SR.Argument_ImplementIComparable);
        }
    }
}
