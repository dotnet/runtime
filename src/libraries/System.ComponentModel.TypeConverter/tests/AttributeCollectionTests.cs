// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class AttributeCollectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var subAttributeCollection = new SubAttributeCollection();
            if (!PlatformDetection.IsFullFramework)
            {
                Assert.Equal(0, subAttributeCollection.Count);
                Assert.Empty(subAttributeCollection);
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => subAttributeCollection.Count);
                Assert.Throws<NullReferenceException>(() => subAttributeCollection.GetEnumerator());
            }
        }

        public static IEnumerable<object[]> Ctor_Attributes_TestData()
        {
            yield return new object[] { GetAttributes().Take(20).ToArray() };
            yield return new object[] { GetAttributes().Take(1).ToArray() };
            yield return new object[] { new Attribute[0] };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(Ctor_Attributes_TestData))]
        public void Ctor_Attributes(Attribute[] attributes)
        {
            var attributeCollection = new AttributeCollection(attributes);
            Assert.Equal(attributes?.Length ?? 0, attributeCollection.Count);
            Assert.Equal(attributes ?? new Attribute[0], attributeCollection.Cast<Attribute>());
        }

        [Fact]
        public void Ctor_NullAttributeInAttributes_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("attributes", () => new AttributeCollection(new TestAttribute1(), null));
        }

        [Fact]
        public void ICollection_SynchronizationProperties_ReturnsExpected()
        {
            AttributeCollection collection = new AttributeCollection(null);

            Assert.Null(((ICollection)collection).SyncRoot);
            Assert.False(((ICollection)collection).IsSynchronized);
        }

        [Theory]
        [InlineData(20, 0)]
        [InlineData(20, 1)]
        [InlineData(1000, 0)]
        [InlineData(1000, 1)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(0, 0)]
        public void CopyTo_ValidArray_Success(int count, int index)
        {
            Attribute[] attributes = GetAttributes().Take(count).ToArray();
            var attributeCollection = new AttributeCollection(attributes);

            var array = new Attribute[count + index];
            attributeCollection.CopyTo(array, index);

            Assert.Equal(attributeCollection.Cast<Attribute>(), array.Cast<Attribute>().Skip(index));
        }

        [Fact]
        public void CopyTo_Default_Nop()
        {
            var attributeCollection = new SubAttributeCollection();
            var array = new object[] { 1, 2, 3 };
            if (!PlatformDetection.IsFullFramework)
            {
                attributeCollection.CopyTo(array, 1);
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => attributeCollection.CopyTo(array, 1));
            }
            Assert.Equal(new object[] { 1, 2, 3}, array);
        }

        [Theory]
        [InlineData(20)]
        [InlineData(1000)]
        [InlineData(1)]
        [InlineData(0)]
        public void Contains_AttributeExists_ReturnsExpected(int count)
        {
            Attribute[] attributes = GetAttributes().Take(count).ToArray();
            var attributeCollection = new AttributeCollection(attributes);

            foreach (Attribute attribute in attributes)
            {
                Assert.True(attributeCollection.Contains(attribute));
            }
        }

        [Fact]
        public void Contains_Attributes_ReturnsExpected()
        {
            Attribute[] attributes = GetAttributes().Take(5).ToArray();
            var attributeCollection = new AttributeCollection(attributes);

            Assert.True(attributeCollection.Contains(attributes));
            Assert.True(attributeCollection.Contains(new Attribute[0]));
            Assert.True(attributeCollection.Contains((Attribute[])null));
            Assert.False(attributeCollection.Contains(new Attribute[] { new ReadOnlyAttribute(true) }));
        }

        [Fact]
        public void Contains_Default_ReturnsFalse()
        {
            var attributeCollection = new SubAttributeCollection();
            if (!PlatformDetection.IsFullFramework)
            {
                Assert.False(attributeCollection.Contains(new ReadOnlyAttribute(true)));
                Assert.False(attributeCollection.Contains(new Attribute[] { new ReadOnlyAttribute(true) }));
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => attributeCollection.Contains(new ReadOnlyAttribute(true)));
                Assert.Throws<NullReferenceException>(() => attributeCollection.Contains(new Attribute[] { new ReadOnlyAttribute(true) }));
            }
        }

        [Theory]
        [InlineData(20)]
        [InlineData(1000)]
        [InlineData(1)]
        [InlineData(0)]
        public void Count_Get_ReturnsExpected(int count)
        {
            Attribute[] attributes = GetAttributes().Take(count).ToArray();
            var attributeCollection = new AttributeCollection(attributes);

            Assert.Equal(count, attributeCollection.Count);
            Assert.Equal(count, ((ICollection)attributeCollection).Count);
        }

        [Fact]
        public void FromExisting_NullExisting_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("existing", () => AttributeCollection.FromExisting(null, new Attribute[0]));
        }

        [Fact]
        public void FromExisting_NullAttributeInNewAttributes_ThrowsArgumentNullException()
        {
            Attribute[] existingAttributes = GetAttributes().Take(5).ToArray();
            var existing = new AttributeCollection(existingAttributes);
            var newAttributes = new Attribute[] { null };

            AssertExtensions.Throws<ArgumentNullException>("newAttributes", () => AttributeCollection.FromExisting(existing, newAttributes));
        }

        [Fact]
        public void FromExisting_NullNewAttributes_Success()
        {
            Attribute[] existingAttributes = GetAttributes().Take(5).ToArray();
            var existing = new AttributeCollection(existingAttributes);

            AttributeCollection attributeCollection = AttributeCollection.FromExisting(existing, null);
            Assert.Equal(existingAttributes, attributeCollection.Cast<Attribute>());
        }

        [Fact]
        public void FromExisting_DifferentNewAttributes_Success()
        {
            Attribute[] existingAttributes = GetAttributes().Take(2).ToArray();
            Attribute[] newAttributes = GetAttributes().Skip(2).Take(2).ToArray();
            var existing = new AttributeCollection(existingAttributes);

            AttributeCollection attributeCollection = AttributeCollection.FromExisting(existing, newAttributes);
            Assert.Equal(existingAttributes.Concat(newAttributes), attributeCollection.Cast<Attribute>());
        }

        [Fact]
        public void FromExisting_SameNewAttributes_Success()
        {
            Attribute[] existingAttributes = GetAttributes().Take(2).ToArray();
            Attribute[] newAttributes = GetAttributes().Skip(1).Take(2).ToArray();
            var existing = new AttributeCollection(existingAttributes);

            AttributeCollection attributeCollection = AttributeCollection.FromExisting(existing, newAttributes);
            Assert.Equal(new Attribute[] { existingAttributes[0], newAttributes[0], newAttributes[1] }, attributeCollection.Cast<Attribute>());
        }

        [Theory]
        [InlineData(20)]
        [InlineData(1000)]
        [InlineData(1)]
        [InlineData(0)]
        public void ItemIndex_GetInt_ReturnsExpected(int count)
        {
            Attribute[] attributes = GetAttributes().Take(count).ToArray();
            var collection = new AttributeCollection(attributes);

            for (int i = 0; i < attributes.Length; i++)
            {
                Assert.Equal(attributes[i], collection[i]);
            }
        }

        [Theory]
        [InlineData(typeof(TestAttribute1), true)]
        [InlineData(typeof(TestAttribute2), false)]
        public void ItemIndex_GetType_ReturnsExpected(Type type, bool isInCollection)
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
        public void Matches_Attribute_ReturnsExpected()
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
        public void Matches_Attributes_ReturnsExpected()
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

        [Fact]
        public void Matches_Default_ReturnsFalse()
        {
            var attributeCollection = new SubAttributeCollection();
            if (!PlatformDetection.IsFullFramework)
            {
                Assert.False(attributeCollection.Matches(new ReadOnlyAttribute(true)));
                Assert.False(attributeCollection.Matches(new Attribute[] { new ReadOnlyAttribute(true) }));
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => attributeCollection.Matches(new ReadOnlyAttribute(true)));
                Assert.Throws<NullReferenceException>(() => attributeCollection.Matches(new Attribute[] { new ReadOnlyAttribute(true) }));
            }
        }

        private static IEnumerable<Attribute> GetAttributes()
        {
            while (true)
            {
                yield return new TestAttribute1();
                yield return new TestAttribute2();
                yield return new TestAttribute3();
                yield return new TestAttribute4();
            }
        }

        public class TestAttribute1 : Attribute { }
        public class TestAttribute2 : Attribute { }
        public class TestAttribute3 : Attribute { }
        public class TestAttribute4 : Attribute { }
        public class TestAttribute5a : Attribute { }
        public class TestAttribute5b : TestAttribute5a { }
        public class TestAttribute6 : Attribute { }

        private class TestAttributeWithDefaultFieldButNotDefault : Attribute
        {
            public static readonly TestAttributeWithDefaultFieldButNotDefault Default = new TestAttributeWithDefaultFieldButNotDefault();
        }

        public class SubAttributeCollection : AttributeCollection
        {
            public SubAttributeCollection() : base() { }
        }
    }
}
