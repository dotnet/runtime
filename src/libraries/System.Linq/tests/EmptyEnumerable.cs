// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class EmptyEnumerableTest : EnumerableTests
    {
        private void TestEmptyCached<T>()
        {
            var enumerable1 = Enumerable.Empty<T>();
            var enumerable2 = Enumerable.Empty<T>();

            Assert.Same(enumerable1, enumerable2);
        }

        [Fact]
        public void EmptyEnumerableCachedTest()
        {
            TestEmptyCached<int>();
            TestEmptyCached<string>();
            TestEmptyCached<object>();
            TestEmptyCached<EmptyEnumerableTest>();
        }

        private void TestEmptyEmpty<T>()
        {
            Assert.Equal(new T[0], Enumerable.Empty<T>());
            Assert.Equal(0, Enumerable.Empty<T>().Count());
            Assert.Same(Enumerable.Empty<T>().GetEnumerator(), Enumerable.Empty<T>().GetEnumerator());
        }

        [Fact]
        public void EmptyEnumerableIsIndeedEmpty()
        {
            TestEmptyEmpty<int>();
            TestEmptyEmpty<string>();
            TestEmptyEmpty<object>();
            TestEmptyEmpty<EmptyEnumerableTest>();
        }

        [Fact]
        public void IListImplementationIsValid()
        {
            IList<int> list = Assert.IsAssignableFrom<IList<int>>(Enumerable.Empty<int>());
            IReadOnlyList<int> roList = Assert.IsAssignableFrom<IReadOnlyList<int>>(Enumerable.Empty<int>());

            Assert.Throws<NotSupportedException>(() => list.Add(42));
            Assert.Throws<NotSupportedException>(() => list.Insert(0, 42));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Remove(42));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[0] = 42);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[0]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => roList[0]);

            Assert.True(list.IsReadOnly);
            Assert.Equal(0, list.Count);
            Assert.Equal(0, roList.Count);

            Assert.False(list.Contains(42));
            Assert.Equal(-1, list.IndexOf(42));

            list.CopyTo(Array.Empty<int>(), 0);
            AssertExtensions.Throws<ArgumentException>("destinationArray", () => list.CopyTo(Array.Empty<int>(), 1));
            int[] array = [42];
            list.CopyTo(array, 0);
            Assert.Equal(42, array[0]);
        }
    }
}
