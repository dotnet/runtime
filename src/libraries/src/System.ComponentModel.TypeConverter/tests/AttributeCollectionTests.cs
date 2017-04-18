// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class AttributeCollectionTests
    {
        [Fact]
        public void CreateEmptyCollectionWithNull()
        {
            AttributeCollection collection = new AttributeCollection(null);

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void CollectionSyncProperties()
        {
            AttributeCollection collection = new AttributeCollection(null);

            Assert.Null(((ICollection)collection).SyncRoot);
            Assert.False(((ICollection)collection).IsSynchronized);
        }

        [Fact]
        public void VerifyThrowsIfMultipleAttributesArePassedButOneNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("attributes", () => new AttributeCollection(new TestAttribute1(), null));
        }

        [InlineData(20)]
        [InlineData(1000)]
        [InlineData(1)]
        [InlineData(0)]
        [Theory]
        public void CreateCollectionAndVerify(int count)
        {
            var attributes = GetAttributes().Take(count).ToArray();
            var attributeCollection = new AttributeCollection(attributes);

            Assert.Equal(count, attributes.Length);
            Assert.Equal(count, attributeCollection.Count);

            foreach (var item in attributeCollection)
            {
                Assert.NotNull(item);
                Assert.NotEqual(string.Empty, item.GetType().Name);
            }
        }

        [InlineData(20, 0)]
        [InlineData(20, 1)]
        [InlineData(1000, 0)]
        [InlineData(1000, 1)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(0, 0)]
        [Theory]
        public void CopyTest(int count, int index)
        {
            var attributes = GetAttributes().Take(count).ToArray();
            var attributeCollection = new AttributeCollection(attributes);

            var array = new Attribute[count + index];
            attributeCollection.CopyTo(array, index);

            Assert.Equal(attributeCollection.Cast<Attribute>(), array.Cast<Attribute>().Skip(index));
        }

        [InlineData(20)]
        [InlineData(1000)]
        [InlineData(1)]
        [InlineData(0)]
        [Theory]
        public void ContainsTest(int count)
        {
            var attributes = GetAttributes().Take(count).ToArray();
            var attributeCollection = new AttributeCollection(attributes);

            foreach (Attribute attribute in attributes)
            {
                Assert.True(attributeCollection.Contains(attribute));
            }
        }

        [InlineData(20)]
        [InlineData(1000)]
        [InlineData(1)]
        [InlineData(0)]
        [Theory]
        public void CountTests(int count)
        {
            var attributes = GetAttributes().Take(count).ToArray();
            var attributeCollection = new AttributeCollection(attributes);

            Assert.Equal(count, attributeCollection.Count);
            Assert.Equal(count, ((ICollection)attributeCollection).Count);
        }

        [InlineData(20)]
        [InlineData(1000)]
        [InlineData(1)]
        [InlineData(0)]
        [Theory]
        public void ItemIndex(int count)
        {
            var attributes = GetAttributes().Take(count).ToArray();
            var collection = new AttributeCollection(attributes);

            for (int i = 0; i < attributes.Length; i++)
            {
                Assert.Equal(attributes[i], collection[i]);
            }
        }

        [Theory]
        [InlineData(typeof(TestAttribute1), true)]
        [InlineData(typeof(TestAttribute2), false)]
        public void ItemIndexByType(Type type, bool isInCollection)
        {
            var attributes = new Attribute[]
            {
                new TestAttribute1(),
                new TestAttribute3(),
                new TestAttribute4(),
                new TestAttribute1(),
                new TestAttribute5b()
            };

            var collection = new AttributeCollection(attributes);

            Assert.Equal(isInCollection, collection[type] != null);
        }

        [Fact]
        public void ItemIndexByTypeWithDefault()
        {
            var collection = new AttributeCollection();

            // ReadOnlyAttribute is used as an example of an attribute that has a default value
            Assert.Same(ReadOnlyAttribute.No, collection[typeof(ReadOnlyAttribute)]);
        }

        [Fact]
        public void ItemIndexByTypeWithDefaultFieldButNotDefault()
        {
            var collection = new AttributeCollection();

            Assert.Same(TestAttributeWithDefaultFieldButNotDefault.Default, collection[typeof(TestAttributeWithDefaultFieldButNotDefault)]);
        }

        [Fact]
        public void ItemIndexByTypeCacheTest()
        {
            var attributes = new Attribute[]
            {
                new TestAttribute1(),
                new TestAttribute2(),
                new TestAttribute3(),
                new TestAttribute4(),
                new TestAttribute1(),
                new TestAttribute5b()
            };

            var collection = new AttributeCollection(attributes);

            // Run this multiple times as a cache is made of the lookup and this test
            // can verify that as the cache is filled, the lookup still succeeds
            for (int loop = 0; loop < 5; loop++)
            {
                Assert.Same(attributes[0], collection[typeof(TestAttribute1)]);
                Assert.Same(attributes[1], collection[typeof(TestAttribute2)]);
                Assert.Same(attributes[2], collection[typeof(TestAttribute3)]);
                Assert.Same(attributes[3], collection[typeof(TestAttribute4)]);

                // Search for TestAttribute5a even though we included TestAttribute5b as the index search
                // will look up the inheritance hierarchy if needed
                Assert.Same(attributes[5], collection[typeof(TestAttribute5a)]);

                // This attribute is not available, so we expect a null to be returned
                Assert.Null(collection[typeof(TestAttribute6)]);
            }
        }

        [Fact]
        public void Matches()
        {
            var attributes = new Attribute[]
            {
                new TestAttribute1(),
                new TestAttribute2(),
                new TestAttribute3()
            };

            var notInCollection = new Attribute[]
            {
                new TestAttribute4(),
                new TestAttribute5a(),
                new TestAttribute5b()
            };

            var collection = new AttributeCollection(attributes);

            foreach (Attribute attribute in attributes)
            {
                Assert.True(collection.Matches(attribute));
            }

            foreach (Attribute attribute in notInCollection)
            {
                Assert.False(collection.Matches(attribute));
            }
        }

        [Fact]
        public void MatchesCollection()
        {
            var attributes = new Attribute[]
            {
                new TestAttribute1(),
                new TestAttribute2(),
                new TestAttribute3()
            };

            var notInCollection = new Attribute[]
            {
                new TestAttribute4(),
                new TestAttribute5a(),
                new TestAttribute5b()
            };

            var collection = new AttributeCollection(attributes);

            Assert.True(collection.Matches(attributes));
            Assert.False(collection.Matches(notInCollection));
        }

        private IEnumerable<Attribute> GetAttributes()
        {
            while (true)
            {
                yield return new TestAttribute1();
                yield return new TestAttribute2();
                yield return new TestAttribute3();
                yield return new TestAttribute4();
            }
        }

        private class TestAttribute1 : Attribute { }
        private class TestAttribute2 : Attribute { }
        private class TestAttribute3 : Attribute { }
        private class TestAttribute4 : Attribute { }
        private class TestAttribute5a : Attribute { }
        private class TestAttribute5b : TestAttribute5a { }
        private class TestAttribute6 : Attribute { }

        private class TestAttributeWithDefaultFieldButNotDefault : Attribute
        {
            public static readonly TestAttributeWithDefaultFieldButNotDefault Default = new TestAttributeWithDefaultFieldButNotDefault();
        }
    }
}
