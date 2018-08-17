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
    public partial class ImmutableSortedSetBuilderTest : ImmutablesTestBase
    {
        [Fact]
        public void ItemRef()
        {
            var array = new[] { 1, 2, 3 }.ToImmutableSortedSet();
            var builder = new ImmutableSortedSet<int>.Builder(array);

            ref readonly var safeRef = ref builder.ItemRef(1);
            ref var unsafeRef = ref Unsafe.AsRef(safeRef);

            Assert.Equal(2, builder.ItemRef(1));

            unsafeRef = 4;

            Assert.Equal(4, builder.ItemRef(1));
        }

        [Fact]
        public void ItemRef_OutOfBounds()
        {
            var array = new[] { 1, 2, 3 }.ToImmutableSortedSet();
            var builder = new ImmutableSortedSet<int>.Builder(array);

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ItemRef(5));
        }
    }
}
