// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Moq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class CustomTypeDescriptorTests
    {
        [Fact]
        public void GetAttributes_InvokeWithoutParent_ReturnsEmpty()
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Same(AttributeCollection.Empty, descriptor.GetAttributes());

            // Call again.
            Assert.Same(AttributeCollection.Empty, descriptor.GetAttributes());
        }

        public static IEnumerable<object[]> GetAttributes_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new AttributeCollection(new EditorBrowsableAttribute()) };
        }

        [Theory]
        [MemberData(nameof(GetAttributes_TestData))]
        public void GetAttributes_InvokeWithParent_ReturnsExpected(AttributeCollection result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetAttributes())
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetAttributes());
            mockParentDescriptor.Verify(d => d.GetAttributes(), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetAttributes());
            mockParentDescriptor.Verify(d => d.GetAttributes(), Times.Exactly(2));
        }

        [Fact]
        public void GetClassName_InvokeWithoutParent_ReturnsNull()
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Null(descriptor.GetClassName());

            // Call again.
            Assert.Null(descriptor.GetClassName());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void GetClassName_InvokeWithParent_ReturnsExpected(string result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetClassName())
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetClassName());
            mockParentDescriptor.Verify(d => d.GetClassName(), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetClassName());
            mockParentDescriptor.Verify(d => d.GetClassName(), Times.Exactly(2));
        }

        [Fact]
        public void GetComponentName_InvokeWithoutParent_ReturnsNull()
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Null(descriptor.GetComponentName());

            // Call again.
            Assert.Null(descriptor.GetComponentName());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void GetComponentName_InvokeWithParent_ReturnsExpected(string result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetComponentName())
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetComponentName());
            mockParentDescriptor.Verify(d => d.GetComponentName(), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetComponentName());
            mockParentDescriptor.Verify(d => d.GetComponentName(), Times.Exactly(2));
        }

        [Fact]
        public void GetConverter_InvokeWithoutParent_ReturnsExpected()
        {
            var descriptor = new SubCustomTypeDescriptor();
            TypeConverter result1 = Assert.IsType<TypeConverter>(descriptor.GetConverter());
            Assert.NotNull(result1);

            // Call again.
            TypeConverter result2 = Assert.IsType<TypeConverter>(descriptor.GetConverter());
            Assert.NotSame(result1, result2);
        }

        public static IEnumerable<object[]> GetConverter_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Int32Converter() };
        }

        [Theory]
        [MemberData(nameof(GetConverter_TestData))]
        public void GetConverter_InvokeWithParent_ReturnsExpected(TypeConverter result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetConverter())
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetConverter());
            mockParentDescriptor.Verify(d => d.GetConverter(), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetConverter());
            mockParentDescriptor.Verify(d => d.GetConverter(), Times.Exactly(2));
        }

        [Fact]
        public void GetDefaultEvent_InvokeWithoutParent_ReturnsNull()
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Null(descriptor.GetDefaultEvent());

            // Call again.
            Assert.Null(descriptor.GetDefaultEvent());
        }

        public static IEnumerable<object[]> GetDefaultEvent_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Mock<EventDescriptor>(MockBehavior.Strict, "Event", new Attribute[0]).Object };
        }

        [Theory]
        [MemberData(nameof(GetDefaultEvent_TestData))]
        public void GetDefaultEvent_InvokeWithParent_ReturnsExpected(EventDescriptor result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetDefaultEvent())
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetDefaultEvent());
            mockParentDescriptor.Verify(d => d.GetDefaultEvent(), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetDefaultEvent());
            mockParentDescriptor.Verify(d => d.GetDefaultEvent(), Times.Exactly(2));
        }

        [Fact]
        public void GetDefaultProperty_InvokeWithoutParent_ReturnsNull()
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Null(descriptor.GetDefaultProperty());

            // Call again.
            Assert.Null(descriptor.GetDefaultProperty());
        }

        public static IEnumerable<object[]> GetDefaultProperty_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Mock<PropertyDescriptor>(MockBehavior.Strict, "Property", new Attribute[0]).Object };
        }

        [Theory]
        [MemberData(nameof(GetDefaultProperty_TestData))]
        public void GetDefaultProperty_InvokeWithParent_ReturnsExpected(PropertyDescriptor result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetDefaultProperty())
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetDefaultProperty());
            mockParentDescriptor.Verify(d => d.GetDefaultProperty(), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetDefaultProperty());
            mockParentDescriptor.Verify(d => d.GetDefaultProperty(), Times.Exactly(2));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void GetEditor_InvokeWithoutParent_ReturnsNull(Type editorBaseType)
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Null(descriptor.GetEditor(editorBaseType));

            // Call again.
            Assert.Null(descriptor.GetEditor(editorBaseType));
        }

        public static IEnumerable<object[]> GetEditor_TestData()
        {
            foreach (Type result in new Type[] { null, typeof(int) })
            {
                yield return new object[] { null, result };
                yield return new object[] { typeof(object), result };
            }
        }

        [Theory]
        [MemberData(nameof(GetEditor_TestData))]
        public void GetEditor_InvokeWithParent_ReturnsExpected(Type editorBaseType, Type result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetEditor(editorBaseType))
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetEditor(editorBaseType));
            mockParentDescriptor.Verify(d => d.GetEditor(editorBaseType), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetEditor(editorBaseType));
            mockParentDescriptor.Verify(d => d.GetEditor(editorBaseType), Times.Exactly(2));
        }

        [Fact]
        public void GetEvents_InvokeWithoutParent_ReturnsEmpty()
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Same(EventDescriptorCollection.Empty, descriptor.GetEvents());

            // Call again.
            Assert.Same(EventDescriptorCollection.Empty, descriptor.GetEvents());
        }

        public static IEnumerable<object[]> GetEvents_WithParent_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new EventDescriptorCollection(new EventDescriptor[] { null }) };
        }

        [Theory]
        [MemberData(nameof(GetEvents_WithParent_TestData))]
        public void GetEvents_InvokeWithParent_ReturnsExpected(EventDescriptorCollection result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetEvents())
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetEvents());
            mockParentDescriptor.Verify(d => d.GetEvents(), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetEvents());
            mockParentDescriptor.Verify(d => d.GetEvents(), Times.Exactly(2));
        }

        public static IEnumerable<object[]> GetEvents_AttributesWithoutParent_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Attribute[] { new EditorBrowsableAttribute() } };
        }

        [Theory]
        [MemberData(nameof(GetEvents_AttributesWithoutParent_TestData))]
        public void GetEvents_InvokeAttributesWithoutParent_ReturnsEmpty(Attribute[] attributes)
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Same(EventDescriptorCollection.Empty, descriptor.GetEvents(attributes));

            // Call again.
            Assert.Same(EventDescriptorCollection.Empty, descriptor.GetEvents(attributes));
        }

        public static IEnumerable<object[]> GetEvents_AttributesWithParent_TestData()
        {
            foreach (EventDescriptorCollection result in new EventDescriptorCollection[] { null, new EventDescriptorCollection(new EventDescriptor[] { null }) })
            {
                yield return new object[] { null, result };
                yield return new object[] { new Attribute[] { new EditorBrowsableAttribute() }, result };
            }
        }

        [Theory]
        [MemberData(nameof(GetEvents_AttributesWithParent_TestData))]
        public void GetEvents_InvokeAttributesWithParent_ReturnsExpected(Attribute[] attributes, EventDescriptorCollection result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetEvents(attributes))
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetEvents(attributes));
            mockParentDescriptor.Verify(d => d.GetEvents(attributes), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetEvents(attributes));
            mockParentDescriptor.Verify(d => d.GetEvents(attributes), Times.Exactly(2));
        }

        [Fact]
        public void GetProperties_InvokeWithoutParent_ReturnsEmpty()
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Same(PropertyDescriptorCollection.Empty, descriptor.GetProperties());

            // Call again.
            Assert.Same(PropertyDescriptorCollection.Empty, descriptor.GetProperties());
        }

        public static IEnumerable<object[]> GetProperties_WithParent_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new PropertyDescriptorCollection(new PropertyDescriptor[] { null }) };
        }

        [Theory]
        [MemberData(nameof(GetProperties_WithParent_TestData))]
        public void GetProperties_InvokeWithParent_ReturnsExpected(PropertyDescriptorCollection result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetProperties())
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetProperties());
            mockParentDescriptor.Verify(d => d.GetProperties(), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetProperties());
            mockParentDescriptor.Verify(d => d.GetProperties(), Times.Exactly(2));
        }

        public static IEnumerable<object[]> GetProperties_AttributesWithoutParent_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Attribute[] { new EditorBrowsableAttribute() } };
        }

        [Theory]
        [MemberData(nameof(GetProperties_AttributesWithoutParent_TestData))]
        public void GetProperties_InvokeAttributesWithoutParent_ReturnsEmpty(Attribute[] attributes)
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Same(PropertyDescriptorCollection.Empty, descriptor.GetProperties(attributes));

            // Call again.
            Assert.Same(PropertyDescriptorCollection.Empty, descriptor.GetProperties(attributes));
        }

        public static IEnumerable<object[]> GetProperties_AttributesWithParent_TestData()
        {
            foreach (PropertyDescriptorCollection result in new PropertyDescriptorCollection[] { null, new PropertyDescriptorCollection(new PropertyDescriptor[] { null }) })
            {
                yield return new object[] { null, result };
                yield return new object[] { new Attribute[] { new EditorBrowsableAttribute() }, result };
            }
        }

        [Theory]
        [MemberData(nameof(GetProperties_AttributesWithParent_TestData))]
        public void GetProperties_InvokeAttributesWithParent_ReturnsExpected(Attribute[] attributes, PropertyDescriptorCollection result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetProperties(attributes))
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetProperties(attributes));
            mockParentDescriptor.Verify(d => d.GetProperties(attributes), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetProperties(attributes));
            mockParentDescriptor.Verify(d => d.GetProperties(attributes), Times.Exactly(2));
        }

        public static IEnumerable<object[]> GetPropertyOwner_WithoutParent_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Mock<PropertyDescriptor>(MockBehavior.Strict, "Name", new Attribute[0]).Object };
        }

        [Theory]
        [MemberData(nameof(GetPropertyOwner_WithoutParent_TestData))]
        public void GetPropertyOwner_InvokeWithoutParent_ReturnsNull(PropertyDescriptor pd)
        {
            var descriptor = new SubCustomTypeDescriptor();
            Assert.Null(descriptor.GetPropertyOwner(pd));

            // Call again.
            Assert.Null(descriptor.GetPropertyOwner(pd));
        }

        public static IEnumerable<object[]> GetPropertyOwner_WithParent_TestData()
        {
            foreach (object result in new object[] { null, new object() })
            {
                yield return new object[] { null, result };
                yield return new object[] { new Mock<PropertyDescriptor>(MockBehavior.Strict, "Name", new Attribute[0]).Object, result };
            }
        }

        [Theory]
        [MemberData(nameof(GetPropertyOwner_WithParent_TestData))]
        public void GetPropertyOwner_InvokeWithParent_ReturnsExpected(PropertyDescriptor pd, object result)
        {
            var mockParentDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockParentDescriptor
                .Setup(d => d.GetPropertyOwner(pd))
                .Returns(result)
                .Verifiable();
            var descriptor = new SubCustomTypeDescriptor(mockParentDescriptor.Object);
            Assert.Same(result, descriptor.GetPropertyOwner(pd));
            mockParentDescriptor.Verify(d => d.GetPropertyOwner(pd), Times.Once());

            // Call again.
            Assert.Same(result, descriptor.GetPropertyOwner(pd));
            mockParentDescriptor.Verify(d => d.GetPropertyOwner(pd), Times.Exactly(2));
        }

        private class SubCustomTypeDescriptor : CustomTypeDescriptor
        {
            public SubCustomTypeDescriptor() : base()
            {
            }

            public SubCustomTypeDescriptor(ICustomTypeDescriptor parent) : base(parent)
            {
            }
        }
    }
}
