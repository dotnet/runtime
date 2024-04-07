// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameObjectCollectionBaseRemoveAtTests
    {
        [Fact]
        public void RemoveAt()
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(10);
            nameObjectCollection.Add(null, new Foo("null-value"));
            nameObjectCollection.Add(null, null);

            nameObjectCollection.Add("repeated-name", null);
            nameObjectCollection.Add("repeated-name", new Foo("repeated-name-value"));

            // Remove from the middle
            nameObjectCollection.RemoveAt(3);
            Assert.Equal(13, nameObjectCollection.Count);
            Assert.Null(nameObjectCollection["Name_3"]);
            Assert.Equal("Name_4", nameObjectCollection.GetKey(3));

            // Remove the first
            nameObjectCollection.RemoveAt(0);
            Assert.Equal(12, nameObjectCollection.Count);
            Assert.Null(nameObjectCollection["Name_0"]);
            Assert.Equal("Name_1", nameObjectCollection.GetKey(0));

            // Remove the last
            nameObjectCollection.RemoveAt(nameObjectCollection.Count - 1);
            Assert.Equal(11, nameObjectCollection.Count);
            Assert.Equal("repeated-name", nameObjectCollection.GetKey(nameObjectCollection.Count - 1));
            Assert.Null(nameObjectCollection["repeated-name"]);

            // Remove all
            int count = nameObjectCollection.Count;
            for (int i = 0; i < count; i++)
            {
                int newCount = 11 - i - 1;
                string key = nameObjectCollection.GetKey(0);

                string nextKey = null;
                if (newCount != 0)
                {
                    nextKey = nameObjectCollection.GetKey(1);
                }

                nameObjectCollection.RemoveAt(0);
                Assert.Equal(newCount, nameObjectCollection.Count);
                Assert.Null(nameObjectCollection[key]);

                if (newCount != 0)
                {
                    Assert.Equal(nextKey, nameObjectCollection.GetKey(0));
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameObjectCollection.RemoveAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameObjectCollection.RemoveAt(count));
        }

        [Fact]
        public void RemoveAt_ReadOnly_ThrowsNotSupportedException()
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(1);
            nameObjectCollection.IsReadOnly = true;
            Assert.Throws<NotSupportedException>(() => nameObjectCollection.RemoveAt(0));
        }
    }
}
