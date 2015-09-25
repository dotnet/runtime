// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Linq.Tests
{
    public class QueryableTests
    {
        [Fact]
        public void AsQueryable()
        {
            Assert.NotNull(((IEnumerable)(new int[] { })).AsQueryable());
        }

        [Fact]
        public void AsQueryableT()
        {
            Assert.NotNull((new int[] { }).AsQueryable());
        }

        [Fact]
        public void NullAsQueryableT()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).AsQueryable());
        }

        [Fact]
        public void NullAsQueryable()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable)null).AsQueryable());
        }

        private class NonGenericEnumerableSoWeDontNeedADependencyOnTheAssemblyWithNonGeneric : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                yield break;
            }
        }

        [Fact]
        public void NonGenericToQueryable()
        {
            Assert.Throws<ArgumentException>(() => new NonGenericEnumerableSoWeDontNeedADependencyOnTheAssemblyWithNonGeneric().AsQueryable());
        }

        [Fact]
        public void ReturnsSelfIfPossible()
        {
            IEnumerable<int> query = Enumerable.Repeat(1, 2).AsQueryable();
            Assert.Same(query, query.AsQueryable());
        }

        [Fact]
        public void ReturnsSelfIfPossibleNonGeneric()
        {
            IEnumerable query = Enumerable.Repeat(1, 2).AsQueryable();
            Assert.Same(query, query.AsQueryable());
        }
    }
}


