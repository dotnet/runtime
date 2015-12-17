// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// LowLevelComparer emulates the desktop type System.Collections.Comparer.
//
// This type is not part of the Win8P surface area but it is used by some code inside System.Private.CoreLib as well as the
// the implementation of Comparer<T> in System.Collections.
//
//

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
