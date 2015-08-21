// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public partial class EmptyEnumerableTest
    {
        private void TestEmptyCached<T>()
        {
            var enumerable1 = Enumerable.Empty<T>();
            var enumerable2 = Enumerable.Empty<T>();

            Assert.Same(enumerable1, enumerable2); // Enumerable.Empty is not cached if not the same.
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
        public void CastToIList()
        {
            var emptyEnumerable = Enumerable.Empty<object>();
            Assert.Same(emptyEnumerable, (IList)emptyEnumerable);
        }

        [Fact]
        public void CastToIListGeneric()
        {
            var emptyEnumerable = Enumerable.Empty<object>();
            Assert.Same(emptyEnumerable, (IList<object>)emptyEnumerable);
        }

        [Fact]
        public void CastToArray()
        {
            var emptyEnumerable = Enumerable.Empty<object>();
            Assert.Same(emptyEnumerable, (object[])emptyEnumerable);
        }
    }
}
