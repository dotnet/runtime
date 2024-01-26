// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Moq;
using Xunit;

namespace System.ComponentModel.Tests
{
    [Collection(nameof(DisableParallelization))] // manipulates cache
    public class TypeDescriptorTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
        public void AddProvider_NullProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.AddProvider(null, new object()));
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.AddProvider(null, typeof(int)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
        public void AddProvider_NullInstance_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("instance", () => TypeDescriptor.AddProvider(mockProvider.Object, (object)null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
        public void AddProvider_NullType_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("type", () => TypeDescriptor.AddProvider(mockProvider.Object, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
        public void AddProviderTransparent_NullProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.AddProviderTransparent(null, new object()));
            Assert.Throws<ArgumentNullException>("provider", () => TypeDescriptor.AddProviderTransparent(null, typeof(int)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
        public void AddProviderTransparent_NullInstance_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("instance", () => TypeDescriptor.AddProviderTransparent(mockProvider.Object, (object)null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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
        [InlineData(typeof(Int128), typeof(Int128Converter))]
        [InlineData(typeof(short), typeof(Int16Converter))]
        [InlineData(typeof(int), typeof(Int32Converter))]
        [InlineData(typeof(long), typeof(Int64Converter))]
        [InlineData(typeof(float), typeof(SingleConverter))]
        [InlineData(typeof(Half), typeof(HalfConverter))]
        [InlineData(typeof(UInt128), typeof(UInt128Converter))]
        [InlineData(typeof(ushort), typeof(UInt16Converter))]
        [InlineData(typeof(uint), typeof(UInt32Converter))]
        [InlineData(typeof(ulong), typeof(UInt64Converter))]
        [InlineData(typeof(object), typeof(TypeConverter))]
        [InlineData(typeof(void), typeof(TypeConverter))]
        [InlineData(typeof(DateOnly), typeof(DateOnlyConverter))]
        [InlineData(typeof(DateTime), typeof(DateTimeConverter))]
        [InlineData(typeof(DateTimeOffset), typeof(DateTimeOffsetConverter))]
        [InlineData(typeof(decimal), typeof(DecimalConverter))]
        [InlineData(typeof(TimeOnly), typeof(TimeOnlyConverter))]
        [InlineData(typeof(TimeSpan), typeof(TimeSpanConverter))]
        [InlineData(typeof(Guid), typeof(GuidConverter))]
        [InlineData(typeof(Array), typeof(ArrayConverter))]
        [InlineData(typeof(int[]), typeof(ArrayConverter))]
        [InlineData(typeof(ICollection), typeof(CollectionConverter))]
        [InlineData(typeof(Stack), typeof(CollectionConverter))]
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
        [InlineData(typeof(DerivedUri), typeof(UriTypeConverter))]
        [InlineData(typeof(TwiceDerivedUri), typeof(UriTypeConverter))]
        [InlineData(typeof(CultureInfo), typeof(CultureInfoConverter))]
        [InlineData(typeof(DerivedCultureInfo), typeof(CultureInfoConverter))]
        [InlineData(typeof(TwiceDerivedCultureInfo), typeof(CultureInfoConverter))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
        public void RemoveProvider_NullInstance_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("instance", () => TypeDescriptor.RemoveProvider(mockProvider.Object, (object)null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
        public void RemoveProvider_NullType_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("type", () => TypeDescriptor.RemoveProvider(mockProvider.Object, null));
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
        public void RemoveProviderTransparent_NullInstance_ThrowsArgumentNullException()
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            Assert.Throws<ArgumentNullException>("instance", () => TypeDescriptor.RemoveProviderTransparent(mockProvider.Object, (object)null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Mock will try to JIT
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

        [Fact]
        public void PropertyFilterAttributeMatch()
        {
            Assert.Equal(3, TypeDescriptor.GetProperties(typeof(PropertyFilterAttributeMatchPoco), new[] { new PropertyFilterAttribute() }).Count);
        }

        public class PropertyFilterAttribute : Attribute
        {
            public override bool Equals(object value) => ReferenceEquals(this, value);

            public override int GetHashCode() => throw new NotImplementedException();

            public override bool Match(object value)
            {
                Attribute attr = (Attribute)value;
                if (attr.GetType().IsSubclassOf(this.GetType()))
                    return attr.Match(value);

                return true;
            }

            public static readonly PropertyFilterAttribute Default = new PropertyFilterAttribute();
        }

        public sealed class PropertyFilterFalseMatchAttribute : PropertyFilterAttribute
        {
            public override bool Match(object value) => false;
        }

        public class PropertyFilterAttributeMatchPoco
        {
            public string StringProp { get; set; }
            public int IntProp { get; set; }
            public double DoubleProp { get; set; }
            public ClassWithFilterAttribute FilterProp { get; set; }
        }

        [PropertyFilterFalseMatch]
        public class ClassWithFilterAttribute { }

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

        class DerivedUri : Uri
        {
            protected DerivedUri() : base("https://hello")
            {
            }
        }

        class TwiceDerivedUri : DerivedUri
        {
        }

        class DerivedCultureInfo : CultureInfo
        {
            protected DerivedCultureInfo() : base("hello")
            {
            }
        }

        class TwiceDerivedCultureInfo : DerivedCultureInfo
        {
        }

        private long _concurrentError = 0;
        private bool ConcurrentError
        {
            get => Interlocked.Read(ref _concurrentError) == 1;
            set => Interlocked.Exchange(ref _concurrentError, value ? 1 : 0);
        }

        private void ConcurrentTest(TypeWithProperty instance)
        {
            var properties = TypeDescriptor.GetProperties(instance);
            Thread.Sleep(10);
            if (properties.Count > 0)
            {
                ConcurrentError = true;
            }
        }

        [SkipOnPlatform(TestPlatforms.Browser, "Thread.Start is not supported on browsers.")]
        [Fact]
        public void ConcurrentGetProperties_ReturnsExpected()
        {
            const int Timeout = 60000;
            int concurrentCount = Environment.ProcessorCount * 2;

            using var finished = new CountdownEvent(concurrentCount);

            var instances = new TypeWithProperty[concurrentCount];
            for (int i = 0; i < concurrentCount; i++)
            {
                instances[i] = new TypeWithProperty();
            }

            for (int i = 0; i < concurrentCount; i++)
            {
                int i2 = i;
                new Thread(() =>
                {
                    ConcurrentTest(instances[i2]);
                    finished.Signal();
                }).Start();
            }

            finished.Wait(Timeout);

            if (finished.CurrentCount != 0)
            {
                Assert.Fail("Timeout. Possible deadlock.");
            }
            else
            {
                Assert.False(ConcurrentError, "Fallback type descriptor is used. Possible race condition.");
            }
        }

        [SkipOnPlatform(TestPlatforms.Browser, "Thread.Start is not supported on browsers.")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void ConcurrentAddProviderAndGetProvider()
        {
            // Use a timeout value lower than RemoteExecutor in order to get a nice Fail message.
            const int Timeout = 50000;

            RemoteInvokeOptions options = new()
            {
                TimeOut = 60000
            };

            RemoteExecutor.Invoke(() =>
            {
                using var finished = new CountdownEvent(2);

                Thread t1 = new Thread(() =>
                {
                    ConcurrentAddProvider();
                    finished.Signal();
                });

                Thread t2 = new Thread(() =>
                {
                    ConcurrentGetProvider();
                    finished.Signal();
                });

                t1.Start();
                t2.Start();
                finished.Wait(Timeout);

                if (finished.CurrentCount != 0)
                {
                    Assert.Fail("Timeout. Possible deadlock.");
                }
            }, options).Dispose();

            static void ConcurrentAddProvider()
            {
                var provider = new EmptyPropertiesTypeProvider();
                TypeDescriptor.AddProvider(provider, typeof(MyClass));

                // This test primarily verifies no deadlock, but verify the values anyway.
                Assert.True(TypeDescriptor.GetProvider(typeof(MyClass)).IsSupportedType(typeof(MyClass)));
            }

            static void ConcurrentGetProvider()
            {
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(TypeWithProperty));

                // This test primarily verifies no deadlock, but verify the values anyway.
                Assert.True(provider.IsSupportedType(typeof(TypeWithProperty)));
            }
        }

        public sealed class EmptyPropertiesTypeProvider : TypeDescriptionProvider
        {
            private sealed class EmptyPropertyListDescriptor : ICustomTypeDescriptor
            {
                public AttributeCollection GetAttributes() => AttributeCollection.Empty;

                public string? GetClassName() => null;

                public string? GetComponentName() => null;

                public TypeConverter? GetConverter() => new TypeConverter();

                public EventDescriptor? GetDefaultEvent() => null;

                public PropertyDescriptor? GetDefaultProperty() => null;

                public object? GetEditor(Type editorBaseType) => null;

                public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;

                public EventDescriptorCollection GetEvents(Attribute[]? attributes) => GetEvents();

                public PropertyDescriptorCollection GetProperties() => PropertyDescriptorCollection.Empty;

                public PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => GetProperties();

                public object? GetPropertyOwner(PropertyDescriptor? pd) => null;
            }
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
            {
                return new EmptyPropertyListDescriptor();
            }
        }

        [TypeDescriptionProvider(typeof(EmptyPropertiesTypeProvider))]
        public sealed class TypeWithProperty
        {
            public int OneProperty { get; set; }
        }

        public static IEnumerable<object[]> GetConverter_ByMultithread_ReturnsExpected_TestData()
        {
            yield return new object[] { typeof(MyClass), typeof(MyTypeConverter) };
            yield return new object[] { typeof(MyInheritedClassWithCustomTypeDescriptionProvider), typeof(MyInheritedClassWithCustomTypeDescriptionProviderConverter) };
            yield return new object[] { typeof(MyInheritedClassWithInheritedTypeDescriptionProvider), typeof(MyTypeConverter) };
        }

        [Theory]
        [MemberData(nameof(GetConverter_ByMultithread_ReturnsExpected_TestData))]
        public async void GetConverter_ByMultithread_ReturnsExpected(Type typeForGetConverter, Type expectedConverterType)
        {
            TypeConverter[] actualConverters = await Task.WhenAll(
                Enumerable.Range(0, 100).Select(_ =>
                    Task.Run(() => TypeDescriptor.GetConverter(typeForGetConverter))));

            Assert.All(actualConverters,
                currentConverter => Assert.IsType(expectedConverterType, currentConverter));
        }

        public static IEnumerable<object[]> GetConverterWithAddProvider_ByMultithread_Success_TestData()
        {
            foreach (object[] currentTestCase in GetConverter_ByMultithread_ReturnsExpected_TestData())
            {
                yield return currentTestCase;
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]  // Mock will try to JIT
        [MemberData(nameof(GetConverterWithAddProvider_ByMultithread_Success_TestData))]
        public async void GetConverterWithAddProvider_ByMultithread_Success(Type typeForGetConverter, Type expectedConverterType)
        {
            TypeConverter[] actualConverters = await Task.WhenAll(
                Enumerable.Range(0, 200).Select(_ =>
                    Task.Run(() =>
                    {
                        var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
                        var someInstance = new object();
                        TypeDescriptor.AddProvider(mockProvider.Object, someInstance);
                        return TypeDescriptor.GetConverter(typeForGetConverter);
                    })));
            Assert.All(actualConverters,
                currentConverter => Assert.IsType(expectedConverterType, currentConverter));
        }

        [TypeDescriptionProvider(typeof(MyClassTypeDescriptionProvider))]
        public class MyClass
        {
        }

        [TypeDescriptionProvider(typeof(MyInheritedClassWithCustomTypeDescriptionProviderTypeDescriptionProvider))]
        public class MyInheritedClassWithCustomTypeDescriptionProvider : MyClass
        {
        }

        public class MyInheritedClassWithInheritedTypeDescriptionProvider : MyClass
        {
        }

        public class MyClassTypeDescriptionProvider : TypeDescriptionProvider
        {
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            {
                return new MyClassTypeDescriptor();
            }
        }

        public class MyInheritedClassWithCustomTypeDescriptionProviderTypeDescriptionProvider : TypeDescriptionProvider
        {
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            {
                return new MyInheritedClassWithCustomTypeDescriptionProviderTypeDescriptor();
            }
        }

        public class MyClassTypeDescriptor : CustomTypeDescriptor
        {
            public override TypeConverter GetConverter()
            {
                return new MyTypeConverter();
            }
        }

        public class MyInheritedClassWithCustomTypeDescriptionProviderTypeDescriptor : CustomTypeDescriptor
        {
            public override TypeConverter GetConverter()
            {
                return new MyInheritedClassWithCustomTypeDescriptionProviderConverter();
            }
        }

        public class MyTypeConverter : TypeConverter
        {
        }

        public class MyInheritedClassWithCustomTypeDescriptionProviderConverter : TypeConverter
        {
        }
    }
}
