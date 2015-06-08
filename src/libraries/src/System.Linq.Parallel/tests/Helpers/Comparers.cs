// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Test
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

    internal class ModularCongruenceComparer : IEqualityComparer<int>
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
    }

    internal class ReverseComparer : IComparer<int>
    {
        public static readonly ReverseComparer Instance = new ReverseComparer();

        public int Compare(int x, int y)
        {
            return -x.CompareTo(y);
        }
    }

    internal class FailingComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            throw new DeliberateTestException();
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
}
