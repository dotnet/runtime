// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Diagnostics;

namespace System.Collections
{
    internal sealed class LowLevelComparer : IComparer
    {
        internal static readonly LowLevelComparer Default = new LowLevelComparer();

        private LowLevelComparer()
        {
        }

        public int Compare(Object a, Object b)
        {
            if (a == b) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

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
