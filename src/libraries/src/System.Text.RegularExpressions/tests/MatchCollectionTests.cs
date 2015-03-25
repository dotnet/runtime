// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

namespace Test
{
    public class MatchCollectionTests
    {
        [Fact]
        public static void EnumeratorTest1()
        {
            var regex = new Regex("e");
            var collection = regex.Matches("dotnet");
            var enumerator = collection.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.IsAssignableFrom<Match>(enumerator.Current);
            Assert.Equal(4, ((Match)enumerator.Current).Index);
            Assert.Equal("e", ((Match)enumerator.Current).Value);
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.IsAssignableFrom<Match>(enumerator.Current);
            Assert.Equal(4, ((Match)enumerator.Current).Index);
            Assert.Equal("e", ((Match)enumerator.Current).Value);
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static void EnumeratorTest2()
        {
            var regex = new Regex("t");
            var collection = regex.Matches("dotnet");
            var enumerator = collection.GetEnumerator();

            for (int i = 0; i < collection.Count; i++)
            {
                enumerator.MoveNext();

                Assert.Equal(enumerator.Current, collection[i]);
            }

            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Reset();

            for (int i = 0; i < collection.Count; i++)
            {
                enumerator.MoveNext();

                Assert.Equal(enumerator.Current, collection[i]);
            }

            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }
    }
}
