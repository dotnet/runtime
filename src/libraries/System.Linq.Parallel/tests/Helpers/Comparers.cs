// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Linq.Parallel.Tests
{
    internal class FailingEqualityComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T x, T y)
        {
            throw new DeliberateTestException();
        }

        public int GetHashCode(T obj)
        {
            throw new DeliberateTestException();
        }
    }

    internal class ModularCongruenceComparer : IEqualityComparer<int>, IComparer<int>
    {
        private int _mod;

        public ModularCongruenceComparer(int mod)
        {
            _mod = Math.Max(1, mod);
        }

        private int leastPositiveResidue(int x)
        {
            return ((x % _mod) + _mod) % _mod;
        }

        public bool Equals(int x, int y)
        {
            return leastPositiveResidue(x) == leastPositiveResidue(y);
        }

        public int GetHashCode(int x)
        {
            return leastPositiveResidue(x).GetHashCode();
        }

        public int GetHashCode(object obj)
        {
            return GetHashCode((int)obj);
        }

        public int Compare(int x, int y)
        {
            return leastPositiveResidue(x).CompareTo(leastPositiveResidue(y));
        }
    }

    internal class ReverseComparer : IComparer<int>
    {
        public static readonly ReverseComparer Instance = new ReverseComparer();

        public int Compare(int x, int y)
        {
            return y.CompareTo(x);
        }
    }

    internal class FailingComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            throw new DeliberateTestException();
        }
    }

    /// <summary>
    /// Returns an extreme value from non-equal comparisons.
    /// </summary>
    /// <remarks>Helper for regression test against PLINQ's version of #2239 .</remarks>
    /// <typeparam name="T">The type being compared.</typeparam>
    internal class ExtremeComparer<T> : IComparer<T>
    {
        private IComparer<T> _def = Comparer<T>.Default;

        public int Compare(T x, T y)
        {
            int direction = _def.Compare(x, y);
            return direction == 0 ? 0 :
                direction > 0 ? int.MaxValue :
                int.MinValue;
        }
    }

    internal static class DelgatedComparable
    {
        public static DelegatedComparable<T> Delegate<T>(T value, IComparer<T> comparer) where T : IComparable<T>
        {
            return new DelegatedComparable<T>(value, comparer);
        }
    }

    internal class DelegatedComparable<T> : IComparable<DelegatedComparable<T>> where T : IComparable<T>
    {
        private T _value;
        private IComparer<T> _comparer;

        public T Value
        {
            get
            {
                return _value;
            }
        }

        public DelegatedComparable(T value, IComparer<T> comparer)
        {
            _value = value;
            _comparer = comparer;
        }

        public int CompareTo(DelegatedComparable<T> other)
        {
            return _comparer.Compare(Value, other.Value);
        }
    }

    internal struct NotComparable
    {
        private readonly int _value;

        public int Value
        {
            get
            {
                return _value;
            }
        }

        public NotComparable(int x)
        {
            _value = x;
        }
    }
}
