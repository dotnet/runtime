// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Tests
{
    public abstract class EnumerableTests
    {
        protected class TestCollection<T> : ICollection<T>
        {
            public T[] Items = new T[0];
            public int CountTouched = 0;
            public int CopyToTouched = 0;
            public TestCollection(T[] items) { Items = items; }

            public virtual int Count { get { CountTouched++; return Items.Length; } }
            public bool IsReadOnly => false;
            public void Add(T item) { throw new NotImplementedException(); }
            public void Clear() { throw new NotImplementedException(); }
            public bool Contains(T item) => Items.Contains(item);
            public bool Remove(T item) { throw new NotImplementedException(); }
            public void CopyTo(T[] array, int arrayIndex) { CopyToTouched++; Items.CopyTo(array, arrayIndex); }
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
        }

        protected class TestEnumerable<T> : IEnumerable<T>
        {
            public T[] Items = new T[0];
            public TestEnumerable(T[] items) { Items = items; }

            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
        }

        protected class TestReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            public T[] Items = new T[0];
            public int CountTouched = 0;
            public TestReadOnlyCollection(T[] items) { Items = items; }

            public int Count { get { CountTouched++; return Items.Length; } }
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
        }

        protected sealed class FastInfiniteEnumerator<T> : IEnumerable<T>, IEnumerator<T>
        {
            public IEnumerator<T> GetEnumerator() => this;

            IEnumerator IEnumerable.GetEnumerator() => this;

            public bool MoveNext() => true;

            public void Reset() { }

            object IEnumerator.Current => default(T);

            public void Dispose() { }

            public T Current => default(T);
        }

        protected static bool IsEven(int num) => num % 2 == 0;

        protected class AnagramEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null | y == null) return false;
                int length = x.Length;
                if (length != y.Length) return false;
                using (var en = x.OrderBy(i => i).GetEnumerator())
                {
                    foreach (char c in y.OrderBy(i => i))
                    {
                        en.MoveNext();
                        if (c != en.Current) return false;
                    }
                }
                return true;
            }

            public int GetHashCode(string obj)
            {
                if (obj == null) return 0;
                int hash = obj.Length;
                foreach (char c in obj)
                    hash ^= c;
                return hash;
            }
        }

        protected static IEnumerable<int> RepeatedNumberGuaranteedNotCollectionType(int num, long count)
        {
            for (long i = 0; i < count; i++) yield return num;
        }

        protected static IEnumerable<int> NumberRangeGuaranteedNotCollectionType(int num, int count)
        {
            for (int i = 0; i < count; i++) yield return num + i;
        }

        protected static IEnumerable<int?> NullableNumberRangeGuaranteedNotCollectionType(int num, int count)
        {
            for (int i = 0; i < count; i++) yield return num + i;
        }

        protected static IEnumerable<int?> RepeatedNullableNumberGuaranteedNotCollectionType(int? num, long count)
        {
            for (long i = 0; i < count; i++) yield return num;
        }

        protected class ThrowsOnMatchEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _data;
            private readonly T _thrownOn;

            public ThrowsOnMatchEnumerable(IEnumerable<T> source, T thrownOn)
            {
                _data = source;
                _thrownOn = thrownOn;
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var datum in _data)
                {
                    if (datum.Equals(_thrownOn)) throw new Exception();
                    yield return datum;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Test enumerator - returns int values from 1 to 5 inclusive.
        /// </summary>
        protected class TestEnumerator : IEnumerable<int>, IEnumerator<int>
        {
            private int _current = 0;

            public virtual int Current => _current;

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public virtual IEnumerator<int> GetEnumerator() => this;

            public virtual bool MoveNext() => _current++ < 5;

            public void Reset()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// A test enumerator that throws an InvalidOperationException when invoking Current after MoveNext has been called exactly once.
        /// </summary>
        protected class ThrowsOnCurrentEnumerator : TestEnumerator
        {
            public override int Current
            {
                get
                {
                    var current = base.Current;
                    if (current == 1)
                    {
                        throw new InvalidOperationException();
                    }
                    return current;
                }
            }
        }

        /// <summary>
        /// A test enumerator that throws an InvalidOperationException when invoking MoveNext after MoveNext has been called exactly once.
        /// </summary>
        protected class ThrowsOnMoveNext : TestEnumerator
        {
            public override bool MoveNext()
            {
                bool baseReturn = base.MoveNext();
                if (base.Current == 1)
                {
                    throw new InvalidOperationException();
                }

                return baseReturn;
            }
        }

        /// <summary>
        /// A test enumerator that throws an InvalidOperationException when GetEnumerator is called for the first time.
        /// </summary>
        protected class ThrowsOnGetEnumerator : TestEnumerator
        {
            private int getEnumeratorCallCount;

            public override IEnumerator<int> GetEnumerator()
            {
                if (getEnumeratorCallCount++ == 0)
                {
                    throw new InvalidOperationException();
                }

                return base.GetEnumerator();
            }
        }

        protected static IEnumerable<T> ForceNotCollection<T>(IEnumerable<T> source)
        {
            foreach (T item in source) yield return item;
        }

        protected static IEnumerable<T> FlipIsCollection<T>(IEnumerable<T> source)
        {
            return source is ICollection<T> ? ForceNotCollection(source) : new List<T>(source);
        }

        protected struct StringWithIntArray
        {
            public string name { get; set; }
            public int?[] total { get; set; }
        }

        protected class DelegateBasedCollection<T> : ICollection<T>
        {
            public Func<int> CountWorker { get; set; }
            public Func<bool> IsReadOnlyWorker { get; set; }
            public Action<T> AddWorker { get; set; }
            public Action ClearWorker { get; set; }
            public Func<T, bool> ContainsWorker { get; set; }
            public Func<T, bool> RemoveWorker { get; set; }
            public Action<T[], int> CopyToWorker { get; set; }
            public Func<IEnumerator<T>> GetEnumeratorWorker { get; set; }
            public Func<IEnumerator> NonGenericGetEnumeratorWorker { get; set; }

            public DelegateBasedCollection()
            {
                CountWorker = () => 0;
                IsReadOnlyWorker = () => false;
                AddWorker = item => { };
                ClearWorker = () => { };
                ContainsWorker = item => false;
                RemoveWorker = item => false;
                CopyToWorker = (array, arrayIndex) => { };
                GetEnumeratorWorker = () => Enumerable.Empty<T>().GetEnumerator();
                NonGenericGetEnumeratorWorker = () => GetEnumeratorWorker();
            }

            public int Count => CountWorker();
            public bool IsReadOnly => IsReadOnlyWorker();
            public void Add(T item) => AddWorker(item);
            public void Clear() => ClearWorker();
            public bool Contains(T item) => ContainsWorker(item);
            public bool Remove(T item) => RemoveWorker(item);
            public void CopyTo(T[] array, int arrayIndex) => CopyToWorker(array, arrayIndex);
            public IEnumerator<T> GetEnumerator() => GetEnumeratorWorker();
            IEnumerator IEnumerable.GetEnumerator() => NonGenericGetEnumeratorWorker();
        }

        protected static List<Func<IEnumerable<T>, IEnumerable<T>>> IdentityTransforms<T>()
        {
            // All of these transforms should take an enumerable and produce
            // another enumerable with the same contents.
            return new List<Func<IEnumerable<T>, IEnumerable<T>>>
            {
                e => e,
                e => e.ToArray(),
                e => e.ToList(),
                e => e.Select(i => i),
                e => e.Concat(Array.Empty<T>()),
                e => ForceNotCollection(e),
                e => e.Concat(ForceNotCollection(Array.Empty<T>())),
                e => e.Where(i => true)
            };
        }

        protected sealed class DelegateIterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
        {
            private readonly Func<IEnumerator<TSource>> _getEnumerator;
            private readonly Func<bool> _moveNext;
            private readonly Func<TSource> _current;
            private readonly Func<IEnumerator> _explicitGetEnumerator;
            private readonly Func<object> _explicitCurrent;
            private readonly Action _reset;
            private readonly Action _dispose;

            public DelegateIterator(
                Func<IEnumerator<TSource>> getEnumerator = null,
                Func<bool> moveNext = null,
                Func<TSource> current = null,
                Func<IEnumerator> explicitGetEnumerator = null,
                Func<object> explicitCurrent = null,
                Action reset = null,
                Action dispose = null)
            {
                _getEnumerator = getEnumerator ?? (() => this);
                _moveNext = moveNext ?? (() => { throw new NotImplementedException(); });
                _current = current ?? (() => { throw new NotImplementedException(); });
                _explicitGetEnumerator = explicitGetEnumerator ?? (() => { throw new NotImplementedException(); });
                _explicitCurrent = explicitCurrent ?? (() => { throw new NotImplementedException(); });
                _reset = reset ?? (() => { throw new NotImplementedException(); });
                _dispose = dispose ?? (() => { throw new NotImplementedException(); });
            }

            public IEnumerator<TSource> GetEnumerator() => _getEnumerator();

            public bool MoveNext() => _moveNext();

            public TSource Current => _current();

            IEnumerator IEnumerable.GetEnumerator() => _explicitGetEnumerator();

            object IEnumerator.Current => _explicitCurrent();

            void IEnumerator.Reset() => _reset();

            void IDisposable.Dispose() => _dispose();
        }
    }
}
