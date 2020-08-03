// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using Moq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeDescriptorTests
    {
        [Fact]
        public void AddProvider_InvokeObject_GetProviderReturnsExpected()
        {
            var instance = new object();
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            mockProvider2
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>());

            TypeDescriptor.AddProvider(mockProvider1.Object, instance);
            TypeDescriptionProvider actualProvider1 = TypeDescriptor.GetProvider(instance);
            Assert.NotSame(actualProvider1, mockProvider1.Object);
            actualProvider1.IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());

            // Add another.
            TypeDescriptor.AddProvider(mockProvider2.Object, instance);
            TypeDescriptionProvider actualProvider2 = TypeDescriptor.GetProvider(instance);
            Assert.NotSame(actualProvider1, actualProvider2);
            actualProvider2.IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
        }

        [Fact]
        public void AddProvider_InvokeObjectMultipleTimes_Refreshes()
        {
            var instance = new object();
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>())
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>())
                .Verifiable();
            
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.ComponentChanged == instance)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.AddProvider(mockProvider1.Object, instance);
                Assert.Equal(0, callCount);
                mockProvider1.Verify(p => p.GetCache(instance), Times.Never());
                mockProvider2.Verify(p => p.GetCache(instance), Times.Never());

                // Add again.
                TypeDescriptor.AddProvider(mockProvider1.Object, instance);
                Assert.Equal(1, callCount);
                mockProvider1.Verify(p => p.GetCache(instance), Times.Once());
                mockProvider2.Verify(p => p.GetCache(instance), Times.Never());

                // Add different.
                TypeDescriptor.AddProvider(mockProvider2.Object, instance);
                Assert.Equal(2, callCount);
                mockProvider1.Verify(p => p.GetCache(instance), Times.Once());
                mockProvider2.Verify(p => p.GetCache(instance), Times.Once());
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        [Fact]
        public void AddProvider_InvokeType_GetProviderReturnsExpected()
        {
            Type type = typeof(AddProvider_InvokeType_GetProviderReturnsExpectedType);
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            mockProvider2
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>());

            TypeDescriptor.AddProvider(mockProvider1.Object, type);
            TypeDescriptionProvider actualProvider1 = TypeDescriptor.GetProvider(type);
            Assert.NotSame(actualProvider1, mockProvider1.Object);
            actualProvider1.IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());

            // Add another.
            TypeDescriptor.AddProvider(mockProvider2.Object, type);
            TypeDescriptionProvider actualProvider2 = TypeDescriptor.GetProvider(type);
            Assert.NotSame(actualProvider1, actualProvider2);
            actualProvider2.IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
        }

        private class AddProvider_InvokeType_GetProviderReturnsExpectedType { }


        [Fact]
        public void AddProvider_InvokeTypeMultipleTimes_Refreshes()
        {
            var type = typeof(AddProvider_InvokeTypeMultipleTimes_RefreshesType);
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>())
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>())
                .Verifiable();
            
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.TypeChanged == type)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.AddProvider(mockProvider1.Object, type);
                Assert.Equal(1, callCount);
                mockProvider1.Verify(p => p.GetCache(type), Times.Never());
                mockProvider2.Verify(p => p.GetCache(type), Times.Never());

                // Add again.
                TypeDescriptor.AddProvider(mockProvider1.Object, type);
                Assert.Equal(2, callCount);
                mockProvider1.Verify(p => p.GetCache(type), Times.Never());
                mockProvider2.Verify(p => p.GetCache(type), Times.Never());

                // Add different.
                TypeDescriptor.AddProvider(mockProvider2.Object, type);
                Assert.Equal(3, callCount);
                mockProvider1.Verify(p => p.GetCache(type), Times.Never());
                mockProvider2.Verify(p => p.GetCache(type), Times.Never());
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        private class AddProvider_InvokeTypeMultipleTimes_RefreshesType { }

        [Fact]
        public void AddProvider_NullProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.AddProvider(null, new object()));
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.AddProvider(null, typeof(int)));
        }

        [Fact]
        public void AddProvider_NullInstance_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("instance", () => TypeDescriptor.AddProvider(mockProvider.Object, (object)null));
        }

        [Fact]
        public void AddProvider_NullType_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("type", () => TypeDescriptor.AddProvider(mockProvider.Object, null));
        }

        [Fact]
        public void AddProviderTransparent_InvokeObject_GetProviderReturnsExpected()
        {
            var instance = new object();
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            mockProvider2
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>());

            TypeDescriptor.AddProviderTransparent(mockProvider1.Object, instance);
            TypeDescriptionProvider actualProvider1 = TypeDescriptor.GetProvider(instance);
            Assert.NotSame(actualProvider1, mockProvider1.Object);
            actualProvider1.IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());

            // Add another.
            TypeDescriptor.AddProviderTransparent(mockProvider2.Object, instance);
            TypeDescriptionProvider actualProvider2 = TypeDescriptor.GetProvider(instance);
            Assert.NotSame(actualProvider1, actualProvider2);
            actualProvider2.IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
        }

        [Fact]
        public void AddProviderTransparent_InvokeObjectMultipleTimes_Refreshes()
        {
            var instance = new object();
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>())
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>())
                .Verifiable();
            
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.ComponentChanged == instance)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.AddProviderTransparent(mockProvider1.Object, instance);
                Assert.Equal(0, callCount);
                mockProvider1.Verify(p => p.GetCache(instance), Times.Never());
                mockProvider2.Verify(p => p.GetCache(instance), Times.Never());

                // Add again.
                TypeDescriptor.AddProviderTransparent(mockProvider1.Object, instance);
                Assert.Equal(1, callCount);
                mockProvider1.Verify(p => p.GetCache(instance), Times.Once());
                mockProvider2.Verify(p => p.GetCache(instance), Times.Never());

                // Add different.
                TypeDescriptor.AddProviderTransparent(mockProvider2.Object, instance);
                Assert.Equal(2, callCount);
                mockProvider1.Verify(p => p.GetCache(instance), Times.Once());
                mockProvider2.Verify(p => p.GetCache(instance), Times.Once());
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        [Fact]
        public void AddProviderTransparent_InvokeType_GetProviderReturnsExpected()
        {
            Type type = typeof(AddProviderTransparent_InvokeType_GetProviderReturnsExpectedType);
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            mockProvider2
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>());

            TypeDescriptor.AddProviderTransparent(mockProvider1.Object, type);
            TypeDescriptionProvider actualProvider1 = TypeDescriptor.GetProvider(type);
            Assert.NotSame(actualProvider1, mockProvider1.Object);
            actualProvider1.IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());

            // Add another.
            TypeDescriptor.AddProviderTransparent(mockProvider2.Object, type);
            TypeDescriptionProvider actualProvider2 = TypeDescriptor.GetProvider(type);
            Assert.NotSame(actualProvider1, actualProvider2);
            actualProvider2.IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
        }

        private class AddProviderTransparent_InvokeType_GetProviderReturnsExpectedType { }

        [Fact]
        public void AddProviderTransparent_InvokeTypeMultipleTimes_Refreshes()
        {
            var type = typeof(AddProviderTransparent_InvokeTypeMultipleTimes_RefreshesType);
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>())
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>())
                .Verifiable();
            
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.TypeChanged == type)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.AddProviderTransparent(mockProvider1.Object, type);
                Assert.Equal(1, callCount);
                mockProvider1.Verify(p => p.GetCache(type), Times.Never());
                mockProvider2.Verify(p => p.GetCache(type), Times.Never());

                // Add again.
                TypeDescriptor.AddProviderTransparent(mockProvider1.Object, type);
                Assert.Equal(2, callCount);
                mockProvider1.Verify(p => p.GetCache(type), Times.Never());
                mockProvider2.Verify(p => p.GetCache(type), Times.Never());

                // Add different.
                TypeDescriptor.AddProviderTransparent(mockProvider2.Object, type);
                Assert.Equal(3, callCount);
                mockProvider1.Verify(p => p.GetCache(type), Times.Never());
                mockProvider2.Verify(p => p.GetCache(type), Times.Never());
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        private class AddProviderTransparent_InvokeTypeMultipleTimes_RefreshesType { }

        [Fact]
        public void AddProviderTransparent_NullProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.AddProviderTransparent(null, new object()));
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.AddProviderTransparent(null, typeof(int)));
        }

        [Fact]
        public void AddProviderTransparent_NullInstance_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("instance", () => TypeDescriptor.AddProviderTransparent(mockProvider.Object, (object)null));
        }

        [Fact]
        public void AddProviderTransparent_NullType_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("type", () => TypeDescriptor.AddProviderTransparent(mockProvider.Object, null));
        }

        [Fact]
        public void AddAttribute()
        {
            var component = new DescriptorTestComponent();
            var addedAttribute = new DescriptorTestAttribute("expected string");

            TypeDescriptor.AddAttributes(component.GetType(), addedAttribute);

            AttributeCollection attributes = TypeDescriptor.GetAttributes(component);
            Assert.True(attributes.Contains(addedAttribute));
        }

        [Fact]
        public void CreateInstancePassesCtorParameters()
        {
            var expectedString = "expected string";
            var component = TypeDescriptor.CreateInstance(null, typeof(DescriptorTestComponent), new[] { expectedString.GetType() }, new[] { expectedString });

            Assert.NotNull(component);
            Assert.IsType<DescriptorTestComponent>(component);
            Assert.Equal(expectedString, (component as DescriptorTestComponent).StringProperty);
        }

        [Fact]
        public void GetAssociationReturnsExpectedObject()
        {
            var primaryObject = new DescriptorTestComponent();
            var secondaryObject = new MockEventDescriptor();
            TypeDescriptor.CreateAssociation(primaryObject, secondaryObject);

            var associatedObject = TypeDescriptor.GetAssociation(secondaryObject.GetType(), primaryObject);

            Assert.IsType(secondaryObject.GetType(), associatedObject);
            Assert.Equal(secondaryObject, associatedObject);
        }

        [Fact]
        public void GetAssociationReturnsDesigner()
        {
            var designer = new MockDesigner();
            var designerHost = new MockDesignerHost();
            var component = new DescriptorTestComponent();

            designerHost.AddDesigner(component, designer);
            component.AddService(typeof(IDesignerHost), designerHost);

            object associatedObject = TypeDescriptor.GetAssociation(designer.GetType(), component);

            Assert.IsType<MockDesigner>(associatedObject);
            Assert.Same(designer, associatedObject);
        }

        [Theory]
        [InlineData(typeof(bool), typeof(BooleanConverter))]
        [InlineData(typeof(byte), typeof(ByteConverter))]
        [InlineData(typeof(sbyte), typeof(SByteConverter))]
        [InlineData(typeof(char), typeof(CharConverter))]
        [InlineData(typeof(double), typeof(DoubleConverter))]
        [InlineData(typeof(string), typeof(StringConverter))]
        [InlineData(typeof(short), typeof(Int16Converter))]
        [InlineData(typeof(int), typeof(Int32Converter))]
        [InlineData(typeof(long), typeof(Int64Converter))]
        [InlineData(typeof(float), typeof(SingleConverter))]
        [InlineData(typeof(ushort), typeof(UInt16Converter))]
        [InlineData(typeof(uint), typeof(UInt32Converter))]
        [InlineData(typeof(ulong), typeof(UInt64Converter))]
        [InlineData(typeof(object), typeof(TypeConverter))]
        [InlineData(typeof(void), typeof(TypeConverter))]
        [InlineData(typeof(DateTime), typeof(DateTimeConverter))]
        [InlineData(typeof(DateTimeOffset), typeof(DateTimeOffsetConverter))]
        [InlineData(typeof(decimal), typeof(DecimalConverter))]
        [InlineData(typeof(TimeSpan), typeof(TimeSpanConverter))]
        [InlineData(typeof(Guid), typeof(GuidConverter))]
        [InlineData(typeof(Array), typeof(ArrayConverter))]
        [InlineData(typeof(ICollection), typeof(CollectionConverter))]
        [InlineData(typeof(Enum), typeof(EnumConverter))]
        [InlineData(typeof(SomeEnum), typeof(EnumConverter))]
        [InlineData(typeof(SomeValueType?), typeof(NullableConverter))]
        [InlineData(typeof(int?), typeof(NullableConverter))]
        [InlineData(typeof(ClassWithNoConverter), typeof(TypeConverter))]
        [InlineData(typeof(BaseClass), typeof(BaseClassConverter))]
        [InlineData(typeof(DerivedClass), typeof(DerivedClassConverter))]
        [InlineData(typeof(IBase), typeof(IBaseConverter))]
        [InlineData(typeof(IDerived), typeof(IBaseConverter))]
        [InlineData(typeof(ClassIBase), typeof(IBaseConverter))]
        [InlineData(typeof(ClassIDerived), typeof(IBaseConverter))]
        [InlineData(typeof(Uri), typeof(UriTypeConverter))]
        [InlineData(typeof(CultureInfo), typeof(CultureInfoConverter))]
        [InlineData(typeof(Version), typeof(VersionConverter))]
        [InlineData(typeof(IComponent), typeof(ComponentConverter))]
        [InlineData(typeof(IFooComponent), typeof(ReferenceConverter))]
        public static void GetConverter(Type targetType, Type resultConverterType)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            Assert.NotNull(converter);
            Assert.Equal(resultConverterType, converter.GetType());
            Assert.True(converter.CanConvertTo(typeof(string)));
        }

        [Fact]
        public static void GetConverter_null()
        {
            Assert.Throws<ArgumentNullException>(() => TypeDescriptor.GetConverter(null));
        }

        [Fact]
        public static void GetConverter_NotAvailable()
        {
            Assert.Throws<MissingMethodException>(
                 () => TypeDescriptor.GetConverter(typeof(ClassWithInvalidConverter)));
            // GetConverter should throw MissingMethodException because parameterless constructor is missing in the InvalidConverter class.
        }

        [Fact]
        public void GetEvents()
        {
            var component = new DescriptorTestComponent();

            EventDescriptorCollection events = TypeDescriptor.GetEvents(component);

            Assert.Equal(2, events.Count);
        }

        [Fact]
        public void GetEventsFiltersByAttribute()
        {
            var defaultValueAttribute = new DefaultValueAttribute(null);
            EventDescriptorCollection events = TypeDescriptor.GetEvents(typeof(DescriptorTestComponent), new[] { defaultValueAttribute });

            Assert.Equal(1, events.Count);
        }

        [Fact]
        public void GetPropertiesFiltersByAttribute()
        {
            var defaultValueAttribute = new DefaultValueAttribute(DescriptorTestComponent.DefaultPropertyValue);
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(DescriptorTestComponent), new[] { defaultValueAttribute });

            Assert.Equal(1, properties.Count);
        }

        [Fact]
        public void RemoveProvider_InvokeObject_RemovesProvider()
        {
            var instance = new object();
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>());
            mockProvider1
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>());
            mockProvider2
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider3 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider3
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>());
            mockProvider3
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();

            TypeDescriptor.AddProvider(mockProvider1.Object, instance);
            TypeDescriptor.AddProvider(mockProvider2.Object, instance);
            TypeDescriptor.AddProvider(mockProvider3.Object, instance);

            // Remove middle.
            TypeDescriptor.RemoveProvider(mockProvider2.Object, instance);
            TypeDescriptor.GetProvider(instance).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());

            // Remove end.
            TypeDescriptor.RemoveProvider(mockProvider3.Object, instance);
            TypeDescriptor.GetProvider(instance).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());

            // Remove start.
            TypeDescriptor.RemoveProvider(mockProvider1.Object, instance);
            TypeDescriptor.GetProvider(instance).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
        }

        [Fact]
        public void RemoveProvider_InvokeObjectWithProviders_Refreshes()
        {
            var instance = new object();
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.ComponentChanged == instance)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.AddProvider(mockProvider1.Object, instance);
                Assert.Equal(0, callCount);

                TypeDescriptor.RemoveProvider(mockProvider1.Object, instance);
                Assert.Equal(1, callCount);

                // Remove again.
                TypeDescriptor.RemoveProvider(mockProvider1.Object, instance);
                Assert.Equal(2, callCount);

                // Remove different.
                TypeDescriptor.RemoveProvider(mockProvider2.Object, instance);
                Assert.Equal(3, callCount);
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        [Fact]
        public void RemoveProvider_InvokeObjectWithoutProviders_Refreshes()
        {
            var instance = new object();
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.ComponentChanged == instance)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.RemoveProvider(mockProvider.Object, instance);
                Assert.Equal(1, callCount);
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        [Fact]
        public void RemoveProvider_InvokeType_RemovesProvider()
        {
            Type type = typeof(RemoveProvider_InvokeType_RemovesProviderType);
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>());
            mockProvider1
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>());
            mockProvider2
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider3 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider3
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>());
            mockProvider3
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();

            TypeDescriptor.AddProvider(mockProvider1.Object, type);
            TypeDescriptor.AddProvider(mockProvider2.Object, type);
            TypeDescriptor.AddProvider(mockProvider3.Object, type);

            // Remove middle.
            TypeDescriptor.RemoveProvider(mockProvider2.Object, type);
            TypeDescriptor.GetProvider(type).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());

            // Remove end.
            TypeDescriptor.RemoveProvider(mockProvider3.Object, type);
            TypeDescriptor.GetProvider(type).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());

            // Remove start.
            TypeDescriptor.RemoveProvider(mockProvider1.Object, type);
            TypeDescriptor.GetProvider(type).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
        }

        private class RemoveProvider_InvokeType_RemovesProviderType { }

        [Fact]
        public void RemoveProvider_InvokeTypeWithProviders_Refreshes()
        {
            Type type = typeof(RemoveProvider_InvokeObjectWithProviders_RefreshesType);
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.TypeChanged == type)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.AddProvider(mockProvider1.Object, type);
                Assert.Equal(1, callCount);

                TypeDescriptor.RemoveProvider(mockProvider1.Object, type);
                Assert.Equal(2, callCount);

                // Remove again.
                TypeDescriptor.RemoveProvider(mockProvider1.Object, type);
                Assert.Equal(3, callCount);

                // Remove different.
                TypeDescriptor.RemoveProvider(mockProvider2.Object, type);
                Assert.Equal(4, callCount);
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        private class RemoveProvider_InvokeObjectWithProviders_RefreshesType { }

        [Fact]
        public void RemoveProvider_InvokeTypeWithoutProviders_Refreshes()
        {
            Type type = typeof(RemoveProvider_InvokeTypeWithoutProviders_RefreshesType);
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.TypeChanged == type)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.RemoveProvider(mockProvider.Object, type);
                Assert.Equal(1, callCount);
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        private class RemoveProvider_InvokeTypeWithoutProviders_RefreshesType { }

        [Fact]
        public void RemoveProvider_NullProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.RemoveProvider(null, new object()));
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.RemoveProvider(null, typeof(int)));
        }

        [Fact]
        public void RemoveProvider_NullInstance_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("instance", () => TypeDescriptor.RemoveProvider(mockProvider.Object, (object)null));
        }

        [Fact]
        public void RemoveProvider_NullType_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("type", () => TypeDescriptor.RemoveProvider(mockProvider.Object, null));
        }


        [Fact]
        public void RemoveProviderTransparent_InvokeObject_RemovesProvider()
        {
            var instance = new object();
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>());
            mockProvider1
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>());
            mockProvider2
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider3 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider3
                .Setup(p => p.GetCache(instance))
                .Returns(new Dictionary<int, string>());
            mockProvider3
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();

            TypeDescriptor.AddProvider(mockProvider1.Object, instance);
            TypeDescriptor.AddProvider(mockProvider2.Object, instance);
            TypeDescriptor.AddProvider(mockProvider3.Object, instance);

            // Remove middle.
            TypeDescriptor.RemoveProviderTransparent(mockProvider2.Object, instance);
            TypeDescriptor.GetProvider(instance).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());

            // Remove end.
            TypeDescriptor.RemoveProviderTransparent(mockProvider3.Object, instance);
            TypeDescriptor.GetProvider(instance).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());

            // Remove start.
            TypeDescriptor.RemoveProviderTransparent(mockProvider1.Object, instance);
            TypeDescriptor.GetProvider(instance).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
        }

        [Fact]
        public void RemoveProviderTransparent_InvokeObjectWithProviders_Refreshes()
        {
            var instance = new object();
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.ComponentChanged == instance)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.AddProvider(mockProvider1.Object, instance);
                Assert.Equal(0, callCount);

                TypeDescriptor.RemoveProviderTransparent(mockProvider1.Object, instance);
                Assert.Equal(1, callCount);

                // Remove again.
                TypeDescriptor.RemoveProviderTransparent(mockProvider1.Object, instance);
                Assert.Equal(2, callCount);

                // Remove different.
                TypeDescriptor.RemoveProviderTransparent(mockProvider2.Object, instance);
                Assert.Equal(3, callCount);
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        [Fact]
        public void RemoveProviderTransparent_InvokeObjectWithoutProviders_Refreshes()
        {
            var instance = new object();
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.ComponentChanged == instance)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.RemoveProviderTransparent(mockProvider.Object, instance);
                Assert.Equal(1, callCount);
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        [Fact]
        public void RemoveProviderTransparent_InvokeType_RemovesProvider()
        {
            Type type = typeof(RemoveProviderTransparent_InvokeType_RemovesProviderType);
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider1
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>());
            mockProvider1
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider2
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>());
            mockProvider2
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();
            var mockProvider3 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider3
                .Setup(p => p.GetCache(type))
                .Returns(new Dictionary<int, string>());
            mockProvider3
                .Setup(p => p.IsSupportedType(typeof(int)))
                .Returns(true)
                .Verifiable();

            TypeDescriptor.AddProvider(mockProvider1.Object, type);
            TypeDescriptor.AddProvider(mockProvider2.Object, type);
            TypeDescriptor.AddProvider(mockProvider3.Object, type);

            // Remove middle.
            TypeDescriptor.RemoveProviderTransparent(mockProvider2.Object, type);
            TypeDescriptor.GetProvider(type).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());

            // Remove end.
            TypeDescriptor.RemoveProviderTransparent(mockProvider3.Object, type);
            TypeDescriptor.GetProvider(type).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());

            // Remove start.
            TypeDescriptor.RemoveProviderTransparent(mockProvider1.Object, type);
            TypeDescriptor.GetProvider(type).IsSupportedType(typeof(int));
            mockProvider1.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
            mockProvider2.Verify(p => p.IsSupportedType(typeof(int)), Times.Never());
            mockProvider3.Verify(p => p.IsSupportedType(typeof(int)), Times.Once());
        }

        private class RemoveProviderTransparent_InvokeType_RemovesProviderType { }

        [Fact]
        public void RemoveProviderTransparent_InvokeTypeWithProviders_Refreshes()
        {
            Type type = typeof(RemoveProviderTransparent_InvokeObjectWithProviders_RefreshesType);
            var mockProvider1 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            var mockProvider2 = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.TypeChanged == type)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.AddProvider(mockProvider1.Object, type);
                Assert.Equal(1, callCount);

                TypeDescriptor.RemoveProviderTransparent(mockProvider1.Object, type);
                Assert.Equal(2, callCount);

                // Remove again.
                TypeDescriptor.RemoveProviderTransparent(mockProvider1.Object, type);
                Assert.Equal(3, callCount);

                // Remove different.
                TypeDescriptor.RemoveProviderTransparent(mockProvider2.Object, type);
                Assert.Equal(4, callCount);
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        private class RemoveProviderTransparent_InvokeObjectWithProviders_RefreshesType { }

        [Fact]
        public void RemoveProviderTransparent_InvokeTypeWithoutProviders_Refreshes()
        {
            Type type = typeof(RemoveProviderTransparent_InvokeTypeWithoutProviders_RefreshesType);
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            int callCount = 0;
            RefreshEventHandler handler = (e) =>
            {
                if (e.TypeChanged == type)
                {
                    callCount++;
                }
            };
            TypeDescriptor.Refreshed += handler;
            try
            {
                TypeDescriptor.RemoveProviderTransparent(mockProvider.Object, type);
                Assert.Equal(1, callCount);
            }
            finally
            {
                TypeDescriptor.Refreshed -= handler;
            }
        }

        private class RemoveProviderTransparent_InvokeTypeWithoutProviders_RefreshesType { }

        [Fact]
        public void RemoveProviderTransparent_NullProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.RemoveProviderTransparent(null, new object()));
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.RemoveProviderTransparent(null, typeof(int)));
        }

        [Fact]
        public void RemoveProviderTransparent_NullInstance_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("instance", () => TypeDescriptor.RemoveProviderTransparent(mockProvider.Object, (object)null));
        }

        [Fact]
        public void RemoveProviderTransparent_NullType_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("type", () => TypeDescriptor.RemoveProviderTransparent(mockProvider.Object, null));
        }

        [Fact]
        public void RemoveAssociationsRemovesAllAssociations()
        {
            var primaryObject = new DescriptorTestComponent();
            var firstAssociatedObject = new MockEventDescriptor();
            var secondAssociatedObject = new MockPropertyDescriptor();
            TypeDescriptor.CreateAssociation(primaryObject, firstAssociatedObject);
            TypeDescriptor.CreateAssociation(primaryObject, secondAssociatedObject);

            TypeDescriptor.RemoveAssociations(primaryObject);

            // GetAssociation never returns null. The default implementation returns the
            // primary object when an association doesn't exist. This isn't documented,
            // however, so here we only verify that the formerly associated objects aren't returned.
            var firstAssociation = TypeDescriptor.GetAssociation(firstAssociatedObject.GetType(), primaryObject);
            Assert.NotEqual(firstAssociatedObject, firstAssociation);
            var secondAssociation = TypeDescriptor.GetAssociation(secondAssociatedObject.GetType(), primaryObject);
            Assert.NotEqual(secondAssociatedObject, secondAssociation);
        }

        [Fact]
        public void RemoveSingleAssociation()
        {
            var primaryObject = new DescriptorTestComponent();
            var firstAssociatedObject = new MockEventDescriptor();
            var secondAssociatedObject = new MockPropertyDescriptor();
            TypeDescriptor.CreateAssociation(primaryObject, firstAssociatedObject);
            TypeDescriptor.CreateAssociation(primaryObject, secondAssociatedObject);

            TypeDescriptor.RemoveAssociation(primaryObject, firstAssociatedObject);

            // the second association should remain
            var secondAssociation = TypeDescriptor.GetAssociation(secondAssociatedObject.GetType(), primaryObject);
            Assert.Equal(secondAssociatedObject, secondAssociation);

            // the first association should not
            var firstAssociation = TypeDescriptor.GetAssociation(firstAssociatedObject.GetType(), primaryObject);
            Assert.NotEqual(firstAssociatedObject, firstAssociation);
        }

        [Fact]
        public void SortDescriptorArray_Invoke_ReturnsExpected()
        {
            var notADescriptor1 = new object();
            var notADescriptor2 = new object();
            var mockDescriptor1 = new Mock<EventDescriptor>(MockBehavior.Strict, "Name1", new Attribute[0]);
            mockDescriptor1
                .Setup(d => d.Name)
                .Returns("Name1");
            var mockDescriptor2 = new Mock<EventDescriptor>(MockBehavior.Strict, "Name2", new Attribute[0]);
            mockDescriptor2
                .Setup(d => d.Name)
                .Returns("Name2");
            var mockDescriptor3 = new Mock<EventDescriptor>(MockBehavior.Strict, "Name3", new Attribute[0]);
            mockDescriptor3
                .Setup(d => d.Name)
                .Returns("Name3");
            var infos = new object[] { null, mockDescriptor3.Object, notADescriptor2, mockDescriptor1.Object, mockDescriptor2.Object, null, notADescriptor1 };
            TypeDescriptor.SortDescriptorArray(infos);
            Assert.True(infos[0] == null || infos[0] == notADescriptor1 || infos[0] == notADescriptor2);
            Assert.True(infos[1] == null || infos[1] == notADescriptor1 || infos[1] == notADescriptor2);
            Assert.True(infos[2] == null || infos[2] == notADescriptor1 || infos[2] == notADescriptor2);
            Assert.True(infos[3] == null || infos[3] == notADescriptor1 || infos[3] == notADescriptor2);
            Assert.Same(mockDescriptor1.Object, infos[4]);
            Assert.Same(mockDescriptor2.Object, infos[5]);
            Assert.Same(mockDescriptor3.Object, infos[6]);
        }

        [Fact]
        public void SortDescriptorArray_NullInfos_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("infos", () => TypeDescriptor.SortDescriptorArray(null));
        }

        [Fact]
        public void DerivedPropertyAttribute()
        {
            PropertyDescriptor property = TypeDescriptor.GetProperties(typeof(FooBarDerived))["Value"];
            var descriptionAttribute = (DescriptionAttribute)property.Attributes[typeof(DescriptionAttribute)];
            Assert.Equal("Derived", descriptionAttribute.Description);
        }

        class FooBarBase
        {
            [Description("Base")]
            public virtual int Value { get; set; }
        }

        class FooBarDerived : FooBarBase
        {
            [Description("Derived")]
            public override int Value { get; set; }
        }

        interface IFooComponent
        {
            bool Flag { get; set; }
        }
    }
}
