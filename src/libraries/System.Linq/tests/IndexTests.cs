// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class IndexTests : EnumerableTests
    {
        [Fact]
        public void Index_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> source = null;
            
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Index());
        }

        [ConditionalFact(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        public void IndexOverflows()
        {
            var infiniteWhere = new FastInfiniteEnumerator<int>().Index();
            using (var en = infiniteWhere.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while (en.MoveNext())
                    {
                    }
                });
        }

        [Fact]
        public void Index()
        {
            string[] source = ["a", "b"];
            (int Index, string Item)[] actual = source.Index().ToArray();
            (int Index, string Item)[] expected = [(0, "a"), (1, "b")];
            AssertExtensions.SequenceEqual<(int Index, string Item)>(expected, actual);
        }
    }
}
