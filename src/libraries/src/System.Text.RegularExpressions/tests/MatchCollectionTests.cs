// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class MatchCollectionTests
    {
        [Fact]
        public void GetEnumerator()
        {
            Regex regex = new Regex("e");
            MatchCollection collection = regex.Matches("dotnet");
            IEnumerator enumerator = collection.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Same(collection[counter], enumerator.Current);
                    counter++;
                }
                Assert.Equal(collection.Count, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        public void GetEnumerator_Invalid()
        {
            Regex regex = new Regex("e");
            MatchCollection collection = regex.Matches("dotnet");
            IEnumerator enumerator = collection.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Reset();
            Assert.True(enumerator.MoveNext());
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }
    }
}
