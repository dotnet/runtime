// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            var collection = new SubAttributeCollection();
            Assert.Empty(collection.Attributes);
            Assert.Equal(0, collection.Count);
            Assert.Empty(collection);
        }

        public static IEnumerable<object[]> Ctor_Attributes_TestData()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            yield return new object[] { new Attribute[] { attribute1, attribute2 }, new Attribute[] { attribute1, attribute2 } };
            yield return new object[] { new Attribute[] { attribute1 }, new Attribute[] { attribute1 } };
            yield return new object[] { new Attribute[0], new Attribute[0] };
            yield return new object[] { null, new Attribute[0] };
        }

        [Theory]
        [MemberData(nameof(Ctor_Attributes_TestData))]
        public void Ctor_Attributes(Attribute[] attributes, Attribute[] expected)
        {
            var collection = new SubAttributeCollection(attributes);
            Assert.Equal(expected, collection.Attributes);
            Assert.Equal(expected.Length, collection.Count);
            Assert.Equal(expected, collection.Cast<Attribute>());
        }

        [Fact]
        public void Ctor_ModifyAttributes_UpdatesInnerArray()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var attributes = new Attribute[] { attribute1 };
            var collection = new SubAttributeCollection(attributes);
            Assert.Equal(attributes, collection.Attributes);
            Assert.Equal(new Attribute[] { attribute1 }, collection.Cast<Attribute>());

            // Change.
            attributes[0] = attribute2;
            Assert.Equal(new Attribute[] { attribute2 }, collection.Cast<Attribute>());
        }

        [Fact]
        public void Ctor_NullAttributeInAttributes_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("attributes", () => new AttributeCollection(new Attribute[] { null }));
        }

        [Fact]
        public void ICollection_GetProperties_ReturnsExpected()
        {
            var collection = new AttributeCollection(null);
            ICollection iCollection = collection;
            Assert.Equal(0, iCollection.Count);
            Assert.False(iCollection.IsSynchronized);
            Assert.Same(collection, iCollection.SyncRoot);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void CopyTo_InvokeEmpty_Success(int index)
        {
            var collection = new SubAttributeCollection();
            var array = new object[] { 1, 2, 3 };
            collection.CopyTo(array, index);
            Assert.Equal(new object[] { 1, 2, 3 }, array);
        }

        [Fact]
        public void CopyTo_InvokeNotEmpty_Success()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var collection = new SubAttributeCollection(attribute1, attribute2);

            var array = new object[] { 1, 2, 3 };
            collection.CopyTo(array, 0);
            Assert.Equal(new object[] { attribute1, attribute2, 3 }, array);

            array = new object[] { 1, 2, 3 };
            collection.CopyTo(array, 1);
            Assert.Equal(new object[] { 1, attribute1, attribute2 }, array);
        }

        [Fact]
        public void CopyTo_NullArray_ThrowsArgumentNullException()
        {
            var collection = new SubAttributeCollection();
            Assert.Throws<ArgumentNullException>("destinationArray", () => collection.CopyTo(null, 0));
        }

        [Fact]
        public void Contains_InvokeAttributeEmpty_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            Assert.False(collection.Contains(new BrowsableAttribute(false)));
            Assert.False(collection.Contains((Attribute)null));
        }

        [Fact]
        public void Contains_InvokeAttributeWithAttributes_ReturnsExpected()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var collection = new AttributeCollection(attribute1, attribute2);
            Assert.True(collection.Contains(attribute1));
            Assert.False(collection.Contains(new BrowsableAttribute(false)));
            Assert.True(collection.Contains(attribute2));
            Assert.False(collection.Contains(new EditorBrowsableAttribute()));
            Assert.False(collection.Contains((Attribute)null));
        }

        [Fact]
        public void Contains_InvokeAttributesEmpty_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            Assert.False(collection.Contains(new Attribute[] { new BrowsableAttribute(false) }));
            Assert.False(collection.Contains(new Attribute[] { null }));
            Assert.True(collection.Contains(new Attribute[0]));
            Assert.True(collection.Contains((Attribute[])null));
        }

        [Fact]
        public void Contains_InvokeAttributesWithAttributes_ReturnsExpected()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var collection = new AttributeCollection(attribute1, attribute2);
            Assert.True(collection.Contains(new Attribute[] { attribute1 }));
            Assert.False(collection.Contains(new Attribute[] { new BrowsableAttribute(false) }));
            Assert.True(collection.Contains(new Attribute[] { attribute1, attribute2 }));
            Assert.False(collection.Contains(new Attribute[] { attribute1, attribute2, new EditorBrowsableAttribute() }));
            Assert.False(collection.Contains(new Attribute[] { new EditorBrowsableAttribute() }));
            Assert.False(collection.Contains(new Attribute[] { null }));
            Assert.True(collection.Contains(new Attribute[0]));
            Assert.True(collection.Contains((Attribute[])null));
        }

        [Fact]
        public void GetDefaultAttribute_InvokeCustom_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            BrowsableAttribute result = Assert.IsType<BrowsableAttribute>(collection.GetDefaultAttribute(typeof(BrowsableAttribute)));
            Assert.True(result.Browsable);

            // Call again.
            Assert.Same(result, collection.GetDefaultAttribute(typeof(BrowsableAttribute)));
        }

        [Fact]
        public void GetDefaultAttribute_InvokeDefaultField_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            AttributeWithDefaultField result = Assert.IsType<AttributeWithDefaultField>(collection.GetDefaultAttribute(typeof(AttributeWithDefaultField)));
            Assert.Same(AttributeWithDefaultField.Default, result);

            // Call again.
            Assert.Same(result, collection.GetDefaultAttribute(typeof(AttributeWithDefaultField)));
        }

        [Fact]
        public void GetDefaultAttribute_InvokeDefaultFieldNotDefault_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            AttributeWithDefaultFieldNotDefault result = Assert.IsType<AttributeWithDefaultFieldNotDefault>(collection.GetDefaultAttribute(typeof(AttributeWithDefaultFieldNotDefault)));
            Assert.Same(AttributeWithDefaultFieldNotDefault.Default, result);

            // Call again.
            Assert.Same(result, collection.GetDefaultAttribute(typeof(AttributeWithDefaultFieldNotDefault)));
        }

        [Fact]
        public void GetDefaultAttribute_InvokeParameterlessConstructorDefault_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            AttributeWithDefaultConstructor result = Assert.IsType<AttributeWithDefaultConstructor>(collection.GetDefaultAttribute(typeof(AttributeWithDefaultConstructor)));

            // Call again.
            Assert.Same(result, collection.GetDefaultAttribute(typeof(AttributeWithDefaultConstructor)));
        }

        [Theory]
        [InlineData(typeof(Attribute))]
        [InlineData(typeof(int))]
        [InlineData(typeof(AttributeWithDefaultProperty))]
        [InlineData(typeof(AttributeWithPrivateDefaultField))]
        [InlineData(typeof(AttributeWithDefaultConstructorNotDefault))]
        public void GetDefaultAttribute_NoDefault_ReturnsNull(Type attributeType)
        {
            var collection = new SubAttributeCollection();
            Assert.Null(collection.GetDefaultAttribute(attributeType));

            // Call again.
            Assert.Null(collection.GetDefaultAttribute(attributeType));
        }

        [Fact]
        public void GetDefaultAttribute_NullAttributeType_ThrowsArgumentNullException()
        {
            var collection = new SubAttributeCollection();
            Assert.Throws<ArgumentNullException>("attributeType", () => collection.GetDefaultAttribute(null));
        }

        [Fact]
        public void GetDefaultAttribute_InvalidType_ReturnsNull()
        {
            var collection = new SubAttributeCollection();
            Assert.Throws<InvalidCastException>(() => collection.GetDefaultAttribute(typeof(AttributeCollectionTests)));

            // Call again.
            Assert.Throws<InvalidCastException>(() => collection.GetDefaultAttribute(typeof(AttributeCollectionTests)));
        }

        [Fact]
        public void GetEnumerator_Invoke_Success()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var collection = new AttributeCollection(attribute1, attribute2);

            IEnumerator enumerator = collection.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                Assert.True(enumerator.MoveNext());
                Assert.Equal(attribute1, enumerator.Current);

                Assert.True(enumerator.MoveNext());
                Assert.Equal(attribute2, enumerator.Current);

                Assert.False(enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                enumerator.Reset();
            }
        }

        [Fact]
        public void FromExisting_NullNewAttributes_Success()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var existingAttributes = new Attribute[] { attribute1, attribute2 };
            var existing = new AttributeCollection(existingAttributes);

            var collection = AttributeCollection.FromExisting(existing, null);
            Assert.Equal(existingAttributes, collection.Cast<Attribute>());
        }

        [Fact]
        public void FromExisting_DifferentNewAttributes_Success()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var attribute3 = new EditorBrowsableAttribute(EditorBrowsableState.Never);
            var attribute4 = new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content);
            var existingAttributes = new Attribute[] { attribute1, attribute2 };
            var newAttributes = new Attribute[] { attribute3, attribute4 };
            var existing = new AttributeCollection(existingAttributes);

            var collection = AttributeCollection.FromExisting(existing, newAttributes);
            Assert.Equal(existingAttributes.Concat(newAttributes), collection.Cast<Attribute>());
        }

        [Fact]
        public void FromExisting_SameNewAttributes_Success()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var attribute3 = new EditorBrowsableAttribute(EditorBrowsableState.Never);
            var existingAttributes = new Attribute[] { attribute1, attribute2 };
            var newAttributes = new Attribute[] { attribute2, attribute3 };
            var existing = new AttributeCollection(existingAttributes);

            var collection = AttributeCollection.FromExisting(existing, newAttributes);
            Assert.Equal(new Attribute[] { existingAttributes[0], newAttributes[0], newAttributes[1] }, collection.Cast<Attribute>());
        }

        [Fact]
        public void FromExisting_NullExisting_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("existing", () => AttributeCollection.FromExisting(null, new Attribute[0]));
        }

        [Fact]
        public void FromExisting_NullAttributeInNewAttributes_ThrowsArgumentNullException()
        {
            var existing = new AttributeCollection();
            AssertExtensions.Throws<ArgumentNullException>("newAttributes", () => AttributeCollection.FromExisting(existing, new Attribute[] { null }));
        }

        [Fact]
        public void Item_GetInt_ReturnsExpected()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var collection = new AttributeCollection(attribute1, attribute2);
            Assert.Same(attribute1, collection[0]);
            Assert.Same(attribute2, collection[1]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void Item_GetInvalidIndex_ThrowsIndexOutOfRangeException(int index)
        {
            var attribute = new ReadOnlyAttribute(true);
            var collection = new AttributeCollection(attribute);
            Assert.Throws<IndexOutOfRangeException>(() => collection[index]);
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
        public void Item_GetTypeCustom_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            BrowsableAttribute result = Assert.IsType<BrowsableAttribute>(collection[typeof(BrowsableAttribute)]);
            Assert.True(result.Browsable);

            // Call again.
            Assert.Same(result, collection[typeof(BrowsableAttribute)]);
        }

        [Fact]
        public void Item_GetTypeDefaultField_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            AttributeWithDefaultField result = Assert.IsType<AttributeWithDefaultField>(collection[typeof(AttributeWithDefaultField)]);
            Assert.Same(AttributeWithDefaultField.Default, result);

            // Call again.
            Assert.Same(result, collection[typeof(AttributeWithDefaultField)]);
        }

        [Fact]
        public void Item_GetTypeDefaultFieldNotDefault_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            AttributeWithDefaultFieldNotDefault result = Assert.IsType<AttributeWithDefaultFieldNotDefault>(collection[typeof(AttributeWithDefaultFieldNotDefault)]);
            Assert.Same(AttributeWithDefaultFieldNotDefault.Default, result);

            // Call again.
            Assert.Same(result, collection[typeof(AttributeWithDefaultFieldNotDefault)]);
        }

        [Fact]
        public void Item_GetTypeParameterlessConstructorDefault_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            AttributeWithDefaultConstructor result = Assert.IsType<AttributeWithDefaultConstructor>(collection[typeof(AttributeWithDefaultConstructor)]);

            // Call again.
            Assert.Same(result, collection[typeof(AttributeWithDefaultConstructor)]);
        }

        [Theory]
        [InlineData(typeof(Attribute))]
        [InlineData(typeof(int))]
        [InlineData(typeof(AttributeWithDefaultProperty))]
        [InlineData(typeof(AttributeWithPrivateDefaultField))]
        [InlineData(typeof(AttributeWithDefaultConstructorNotDefault))]
        public void Item_GetTypeNoDefault_ReturnsNull(Type attributeType)
        {
            var collection = new SubAttributeCollection();
            Assert.Null(collection[attributeType]);

            // Call again.
            Assert.Null(collection[attributeType]);
        }

        [Fact]
        public void Item_GetNullAttributeType_ThrowsArgumentNullException()
        {
            var collection = new AttributeCollection();
            Assert.Throws<ArgumentNullException>("attributeType", () => collection[null]);
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
        public void Matches_InvokeAttributeEmpty_ReturnsExpected()
        {
            var collection = new AttributeCollection();
            Assert.False(collection.Matches(new TestAttribute4()));
            Assert.False(collection.Matches(new TestAttribute5a()));
            Assert.False(collection.Matches(new TestAttribute5b()));
            Assert.False(collection.Matches((Attribute)null));
        }

        [Fact]
        public void Matches_InvokeAttributeWithAttributes_ReturnsExpected()
        {
            var attribute1 = new TestAttribute1();
            var attribute2 = new TestAttribute2();
            var attribute3 = new TestAttribute3();
            var collection = new AttributeCollection(attribute1, attribute2, attribute3);
            Assert.True(collection.Matches(attribute1));
            Assert.True(collection.Matches(attribute2));
            Assert.True(collection.Matches(attribute3));
            Assert.False(collection.Matches(new TestAttribute4()));
            Assert.False(collection.Matches(new TestAttribute5a()));
            Assert.False(collection.Matches(new TestAttribute5b()));
            Assert.False(collection.Matches((Attribute)null));
        }

        [Fact]
        public void Matches_InvokeAttributesEmpty_ReturnsExpected()
        {
            var collection = new SubAttributeCollection();
            Assert.False(collection.Matches(new Attribute[] { new BrowsableAttribute(false) }));
            Assert.False(collection.Matches(new Attribute[] { null }));
            Assert.True(collection.Matches(new Attribute[0]));
            Assert.True(collection.Matches((Attribute[])null));
        }

        [Fact]
        public void Matches_InvokeAttributesWithAttributes_ReturnsExpected()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var collection = new AttributeCollection(attribute1, attribute2);
            Assert.True(collection.Matches(new Attribute[] { attribute1 }));
            Assert.False(collection.Matches(new Attribute[] { new BrowsableAttribute(false) }));
            Assert.True(collection.Matches(new Attribute[] { attribute1, attribute2 }));
            Assert.False(collection.Matches(new Attribute[] { attribute1, attribute2, new EditorBrowsableAttribute() }));
            Assert.False(collection.Matches(new Attribute[] { new EditorBrowsableAttribute() }));
            Assert.False(collection.Matches(new Attribute[] { null }));
            Assert.True(collection.Matches(new Attribute[0]));
            Assert.True(collection.Matches((Attribute[])null));
        }

        [Fact]
        public void IEnumerableGetEnumerator_Invoke_Success()
        {
            var attribute1 = new BrowsableAttribute(true);
            var attribute2 = new ReadOnlyAttribute(true);
            var collection = new AttributeCollection(attribute1, attribute2);

            IEnumerable iEnumerable = collection;
            IEnumerator enumerator = iEnumerable.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                Assert.True(enumerator.MoveNext());
                Assert.Equal(attribute1, enumerator.Current);

                Assert.True(enumerator.MoveNext());
                Assert.Equal(attribute2, enumerator.Current);

                Assert.False(enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                enumerator.Reset();
            }
        }

        public class TestAttribute1 : Attribute { }
        public class TestAttribute2 : Attribute { }
        public class TestAttribute3 : Attribute { }
        public class TestAttribute4 : Attribute { }
        public class TestAttribute5a : Attribute { }
        public class TestAttribute5b : TestAttribute5a { }
        public class TestAttribute6 : Attribute { }

        private class AttributeWithDefaultField : Attribute
        {
            public static readonly AttributeWithDefaultField Default = new AttributeWithDefaultField();

            public override bool IsDefaultAttribute() => true;
        }

        private class AttributeWithDefaultFieldNotDefault : Attribute
        {
            public static readonly AttributeWithDefaultFieldNotDefault Default = new AttributeWithDefaultFieldNotDefault();

            public override bool IsDefaultAttribute() => false;
        }

        private class AttributeWithDefaultConstructor : Attribute
        {
            public AttributeWithDefaultConstructor()
            {
            }

            public override bool IsDefaultAttribute() => true;
        }

        private class AttributeWithDefaultConstructorNotDefault : Attribute
        {
            public AttributeWithDefaultConstructorNotDefault()
            {
            }

            public override bool IsDefaultAttribute() => false;
        }

        private class AttributeWithDefaultProperty : Attribute
        {
            public static AttributeWithDefaultProperty Default { get; } = new AttributeWithDefaultProperty();
        }

        private class AttributeWithPrivateDefaultField : Attribute
        {
            private static readonly AttributeWithPrivateDefaultField Default = new AttributeWithPrivateDefaultField();
        }

        public class SubAttributeCollection : AttributeCollection
        {
            public SubAttributeCollection() : base()
            {
            }

            public SubAttributeCollection(params Attribute[] attributes) : base(attributes)
            {
            }

            public new Attribute[] Attributes => base.Attributes;

            public new Attribute GetDefaultAttribute(Type attributeType) => base.GetDefaultAttribute(attributeType);
        }
    }
}
