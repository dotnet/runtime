// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics.Contracts;

namespace System.Collections.Generic
{
    public abstract class Comparer<T> : IComparer, IComparer<T>
    {
        protected Comparer()
        {
        }

        // .NET Native for UWP toolchain overwrites the Default property with optimized 
        // instantiation-specific implementation. It depends on subtle implementation details of this
        // class to do so. Once the packaging infrastructure allows it, the implementation 
        // of Comparer<T> should be moved to CoreRT repo to avoid the fragile dependency.
        // Until that happens, nothing in this class can change.

        // TODO: Initialize the _default field via implicit static constructor for better performance
        // (https://github.com/dotnet/coreclr/pull/4340).

        public static Comparer<T> Default
        {
            get
            {
                if (_default == null)
                    _default = CreateComparer();
                return _default;
            }
        }

        public abstract int Compare(T x, T y);

        public static Comparer<T> Create(Comparison<T> comparison)
        {
            Contract.Ensures(Contract.Result<Comparer<T>>() != null);

            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));

            return new ComparisonComparer<T>(comparison);
        }

        int System.Collections.IComparer.Compare(Object x, Object y)
        {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;
            if (x is T && y is T) return Compare((T)x, (T)y);
            throw new ArgumentException(SR.Argument_InvalidArgumentForComparison);
        }

        private static Comparer<T> CreateComparer()
        {
            // NUTC compiler optimization see comments in EqualityComparer.cs
            if (typeof(T) == typeof(Int32))
                return (Comparer<T>)(Object)(new Int32Comparer());

            return new DefaultComparer<T>();
        }

        // The following field is required for interop with the VS Debugger
        // Prior to making any changes to this field, please reach out to the VS Debugger 
        // team to make sure that your changes are not going to prevent the debugger
        // from working.
        private static Comparer<T> _default;
    }


    internal class DefaultComparer<T> : Comparer<T>
    {
        public override int Compare(T x, T y)
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

            return CompareUsingIComparable(x, y);
        }

        private int CompareUsingIComparable(Object a, Object b)
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

    internal class ComparisonComparer<T> : Comparer<T>
    {
        private readonly Comparison<T> _comparison;

        public ComparisonComparer(Comparison<T> comparison)
        {
            _comparison = comparison;
        }

        public override int Compare(T x, T y)
        {
            return _comparison(x, y);
        }
    }

    internal class Int32Comparer : Comparer<Int32>
    {
        public override int Compare(Int32 x, Int32 y)
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
