// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public partial class ImmutableSortedSetTest : ImmutableSetTest
    {
        [Fact]
        [ActiveIssue("Sporadic failure, needs a port of https://github.com/dotnet/coreclr/pull/4340", TargetFrameworkMonikers.NetFramework)]
        public void EmptyTest()
        {
            this.EmptyTestHelper(Empty<int>(), 5, null);
            this.EmptyTestHelper(Empty<string>().ToImmutableSortedSet(StringComparer.OrdinalIgnoreCase), "a", StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void TryGetValueTest()
        {
            this.TryGetValueTestHelper(ImmutableSortedSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase));
        }

        internal override IBinaryTree GetRootNode<T>(IImmutableSet<T> set)
        {
            return ((ImmutableSortedSet<T>)set).Root;
        }

        /// <summary>
        /// Tests various aspects of a sorted set.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the set.</typeparam>
        /// <param name="emptySet">The empty set.</param>
        /// <param name="value">A value that could be placed in the set.</param>
        /// <param name="comparer">The comparer used to obtain the empty set, if any.</param>
        private void EmptyTestHelper<T>(IImmutableSet<T> emptySet, T value, IComparer<T> comparer)
        {
            Assert.NotNull(emptySet);

            this.EmptyTestHelper(emptySet);
            Assert.Same(emptySet, emptySet.ToImmutableSortedSet(comparer));
            Assert.Same(comparer ?? Comparer<T>.Default, ((ISortKeyCollection<T>)emptySet).KeyComparer);

            var reemptied = emptySet.Add(value).Clear();
            Assert.Same(reemptied, reemptied.ToImmutableSortedSet(comparer)); //, "Getting the empty set from a non-empty instance did not preserve the comparer.");
        }
    }
}
