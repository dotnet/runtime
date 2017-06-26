// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Drawing.Design.Tests
{
    public class CategoryNameCollectionTests
    {
        [Fact]
        public void Ctor_StringArray()
        {
            var value = new string[] { "1", "2", "3" };
            var collection = new CategoryNameCollection(value);
            Assert.Equal(value, collection.Cast<string>());
        }

        [Fact]
        public void Ctor_CategoryNameCollection()
        {
            var value = new string[] { "1", "2", "3" };
            var sourceCollection = new CategoryNameCollection(value);

            var collection = new CategoryNameCollection(sourceCollection);
            Assert.Equal(value, collection.Cast<string>());
        }
        
        [Fact]
        public void Indexer_Get_ReturnsExpected()
        {
            var value = new string[] { "1", "2", "3" };
            var sourceCollection = new CategoryNameCollection(value);

            for (int i = 0; i < sourceCollection.Count; i++)
            {
                string expectedValue = value[i];
                Assert.Equal(expectedValue, sourceCollection[i]);
                Assert.True(sourceCollection.Contains(expectedValue));
                Assert.Equal(i, sourceCollection.IndexOf(expectedValue));
            }
        }

        [Fact]
        public void CopyTo_Valid_Success()
        {
            var value = new string[] { "1", "2", "3" };
            var sourceCollection = new CategoryNameCollection(value);

            var destination = new string[5];
            sourceCollection.CopyTo(destination, 1);

            Assert.Equal(new string[] { null, "1", "2", "3", null }, destination);
        }
    }
}
