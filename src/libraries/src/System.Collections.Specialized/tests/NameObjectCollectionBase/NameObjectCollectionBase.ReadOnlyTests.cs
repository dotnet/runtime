// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameObjectCollectionBaseReadOnlyTests
    {
        [Fact]
        public void IsReadOnly_Set()
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(10);
            Assert.False(nameObjectCollection.IsReadOnly);

            nameObjectCollection.IsReadOnly = true;
            Assert.True(nameObjectCollection.IsReadOnly);

            Assert.Throws<NotSupportedException>(() => nameObjectCollection.Add("name", null));
            Assert.Throws<NotSupportedException>(() => nameObjectCollection["name"] = null);
            Assert.Throws<NotSupportedException>(() => nameObjectCollection[0] = null);

            Assert.Throws<NotSupportedException>(() => nameObjectCollection.Remove("name"));
            Assert.Throws<NotSupportedException>(() => nameObjectCollection.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => nameObjectCollection.Clear());

            Assert.Equal("Name_0", nameObjectCollection.GetKey(0));
            Assert.Equal(new Foo("Value_0"), nameObjectCollection["Name_0"]);
            Assert.Equal(new Foo("Value_0"), nameObjectCollection[0]);
        }
    }
}
