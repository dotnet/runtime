// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal sealed class CancelingEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Action _canceler;

        public CancelingEqualityComparer(Action canceler)
        {
            _canceler = canceler;
        }

        public bool Equals(T x, T y)
        {
            _canceler();
            return EqualityComparer<T>.Default.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            _canceler();
            return obj.GetHashCode();
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

    internal sealed class CancelingComparer : IComparer<int>
    {
        private readonly Action _canceler;

        public CancelingComparer(Action canceler)
        {
            _canceler = canceler;
        }

        public int Compare(int x, int y)
        {
            _canceler();
            return Comparer<int>.Default.Compare(x, y);
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

    /// <summary>
    /// All funcs to be used as comparers by wrapping and delegating.
    /// </summary>
    internal static class DelegatingComparer
    {
        public static IComparer<T> Create<T>(Func<T, T, int> comparer)
        {
            return new DelegatingOrderingComparer<T>(comparer);
        }

        public static IEqualityComparer<T> Create<T>(Func<T, T, bool> comparer, Func<T, int> hashcode)
        {
            return new DelegatingEqualityComparer<T>(comparer, hashcode);
        }

        private class DelegatingOrderingComparer<T> : IComparer<T>
        {
            private readonly Func<T, T, int> _comparer;

            public DelegatingOrderingComparer(Func<T, T, int> comparer)
            {
                _comparer = comparer;
            }

            public int Compare(T left, T right)
            {
                return _comparer(left, right);
            }
        }

        private class DelegatingEqualityComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> _comparer;
            private readonly Func<T, int> _hashcode;

            public DelegatingEqualityComparer(Func<T, T, bool> comparer, Func<T, int> hashcode)
            {
                _comparer = comparer;
                _hashcode = hashcode;
            }

            public bool Equals(T left, T right)
            {
                return _comparer(left, right);
            }

            public int GetHashCode(T item)
            {
                return _hashcode(item);
            }
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
