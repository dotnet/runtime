// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                throw new ArgumentNullException("comparison");

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

        private static Comparer<T> _default;
    }


    internal class DefaultComparer<T> : Comparer<T>
    {
        public override int Compare(T x, T y)
        {
            return LowLevelComparer<T>.DefaultCompareImpl(x, y);
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
