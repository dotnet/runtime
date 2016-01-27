// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace System.Collections.Generic
{
    internal sealed class LowLevelComparer<T> : IComparer<T>
    {
        internal static IComparer<T> Default
        {
            get
            {
                if (_default == null)
                    _default = CreateComparer();
                return _default;
            }
        }

        private LowLevelComparer()
        {
        }

        public int Compare(T x, T y)
        {
            return DefaultCompareImpl(x, y);
        }

        //
        // Implements both Comparer<T>.Default.Compare(x,y) and LowLevelComparer<T>.Default.Compare(x,y)
        //
        // FxOverRh port notes:
        //
        // - Compat note:
        //
        //   If T does not implement IComparable<> but the specific instance (x or y) does, we behave differently
        //   from the desktop:
        //
        //      - We do the comparison using IComparable<>.
        //      - The desktop does the comparison using IComparable, or throws if at least one of x and y does not implement IComparable.
        //
        //   This is a consequence of the fact that the desktop only exploits IComparable<> if T implements IComparable<>.
        //   We have no way to perform this check using Win8P's reduced Type surface area. Thus, we check the instance object for IComparable<> instead.
        //
        //
        // - Nullables
        //
        //   Currently, Comparer<Nullable<T>> is broken due to the fact that the special rules for boxing Nullables are implemented yet.
        //   Once that's fixed, this code should work correctly,
        //
        internal static int DefaultCompareImpl(T x, T y)
        {
            // Desktop compat note: If either x or y are null, this api must not invoke either IComparable.Compare or IComparable<T>.Compare on either
            // x or y.
            if (x == null)
            {
                if (y == null)
                    return 0;
                else
                    return -1;
            }
            if (y == null)
                return 1;

            IComparable<T> igcx = x as IComparable<T>;
            if (igcx != null)
                return igcx.CompareTo(y);
            IComparable<T> igcy = y as IComparable<T>;
            if (igcy != null)
                return -igcy.CompareTo(x);

            return LowLevelComparer.Default.Compare(x, y);
        }

        private static IComparer<T> CreateComparer()
        {
            // NUTC compiler optimization see comments in EqualityComparer.cs
            if (typeof(T) == typeof(Int32))
                return (IComparer<T>)(Object)(new LowLevelInt32Comparer());

            return new LowLevelComparer<T>();
        }

        private static IComparer<T> _default;
    }

    internal class LowLevelInt32Comparer : IComparer<Int32>
    {
        public int Compare(Int32 x, Int32 y)
        {
            if (x < y)
                return -1;
            else if (x > y)
                return 1;
            else
                return 0;
        }
    }
}
