// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class IndexTests : EnumerableTests
    {
        [Fact]
        public void Empty()
        {
            Assert.Empty(Enumerable.Empty<int>().Index());
        }

        [Fact]
        public void Index_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> source = null;
            
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Index());
        }

        [ConditionalFact(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        public void LargeEnumerable_ThrowsOverflowException()
        {
            long maxInt = int.MaxValue;
            var overflowRange = RepeatedNumberGuaranteedNotCollectionType(num: 1, count: maxInt + 2).Index();
            using (var en = overflowRange.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while (en.MoveNext())
                    {
                    }
                });
        }

        [ConditionalFact(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        public void LargeEnumerable()
        {
            long maxInt = int.MaxValue;
            int index = -1;
            var range = RepeatedNumberGuaranteedNotCollectionType(num: 1, count: maxInt + 1).Index();
            foreach (var item in range)
            {
                ++index;
                Assert.Equal(index, item.Index);
            }
            Assert.Equal(int.MaxValue, index);
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
