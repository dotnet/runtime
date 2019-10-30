// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameObjectCollectionBaseGetEnumeratorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void GetEnumerator(int count)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            Assert.NotSame(nameObjectCollection.GetEnumerator(), nameObjectCollection.GetEnumerator());

            IEnumerator enumerator = nameObjectCollection.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(nameObjectCollection.GetKey(counter), enumerator.Current);
                    counter++;
                }
                Assert.Equal(count, nameObjectCollection.Count);
                enumerator.Reset();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void GetEnumerator_Invalid(int count)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            IEnumerator enumerator = nameObjectCollection.GetEnumerator();

            // Has not started enumerating
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Has finished enumerating
            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Has reset enumerating
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Modify collection
            enumerator.MoveNext();
            nameObjectCollection.Add("new-name", new Foo("new-value"));
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
            if (count > 0)
            {
                Assert.NotNull(enumerator.Current);
            }

            // Modified read only collection still throws
            nameObjectCollection.IsReadOnly = true;
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

            // Clear collection
            nameObjectCollection.IsReadOnly = false;
            enumerator = nameObjectCollection.GetEnumerator();
            enumerator.MoveNext();
            nameObjectCollection.Clear();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        }
    }
}
