// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public static class TestExtensions
    {
        public static IEnumerable<T> RunOnce<T>(this IEnumerable<T> source) => 
            source == null ? null : (source as IList<T>)?.RunOnce() ?? new RunOnceEnumerable<T>(source);

        public static IEnumerable<T> RunOnce<T>(this IList<T> source)
            => source == null ? null : new RunOnceList<T>(source);

        private class RunOnceEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _source;
            private bool _called;

            public RunOnceEnumerable(IEnumerable<T> source)
            {
                _source = source;
            }

            public IEnumerator<T> GetEnumerator()
            {
                Assert.False(_called);
                _called = true;
                return _source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class RunOnceList<T> : IList<T>
        {
            private readonly IList<T> _source;
            private readonly HashSet<int> _called = new HashSet<int>();

            private void AssertAll()
            {
                Assert.Empty(_called);
                _called.Add(-1);
            }

            private void AssertIndex(int index)
            {
                Assert.False(_called.Contains(-1));
                Assert.True(_called.Add(index));
            }

            public RunOnceList(IList<T> source)
            {
                _source = source;
            }

            public IEnumerator<T> GetEnumerator()
            {
                AssertAll();
                return _source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                AssertAll();
                return _source.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                AssertAll();
                _source.CopyTo(array, arrayIndex);
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public int Count => _source.Count;

            public bool IsReadOnly => true;

            public int IndexOf(T item)
            {
                AssertAll();
                return _source.IndexOf(item);
            }

            public void Insert(int index, T item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            public T this[int index]
            {
                get
                {
                    AssertIndex(index);
                    return _source[index];
                }
                set { throw new NotSupportedException(); }
            }
        }
    }
}
