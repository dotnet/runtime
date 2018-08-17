// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public partial class TypeDescriptorTests
    {
        [Fact]
        public void AddAndRemoveProvider()
        {
            var provider = new InvocationRecordingTypeDescriptionProvider();
            var component = new DescriptorTestComponent();
            TypeDescriptor.AddProvider(provider, component);

            var retrievedProvider = TypeDescriptor.GetProvider(component);
            retrievedProvider.GetCache(component);

            Assert.True(provider.ReceivedCall);

            provider.Reset();
            TypeDescriptor.RemoveProvider(provider, component);
            retrievedProvider = TypeDescriptor.GetProvider(component);
            retrievedProvider.GetCache(component);

            Assert.False(provider.ReceivedCall);
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
            Assert.IsType(typeof(DescriptorTestComponent), component);
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
        public void DerivedPropertyAttribute() {
            PropertyDescriptor property = TypeDescriptor.GetProperties(typeof(FooBarDerived))["Value"];
            var descriptionAttribute = (DescriptionAttribute)property.Attributes[typeof(DescriptionAttribute)];
            Assert.Equal("Derived", descriptionAttribute.Description);
        }

        private class InvocationRecordingTypeDescriptionProvider : TypeDescriptionProvider
        {
            public bool ReceivedCall { get; private set; } = false;

            public void Reset() => ReceivedCall = false;

            public override object CreateInstance(IServiceProvider provider, Type objectType, Type[] argTypes, object[] args)
            {
                ReceivedCall = true;
                return base.CreateInstance(provider, objectType, argTypes, args);
            }

            public override IDictionary GetCache(object instance)
            {
                ReceivedCall = true;
                return base.GetCache(instance);
            }

            public override ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
            {
                ReceivedCall = true;
                return base.GetExtendedTypeDescriptor(instance);
            }

            public override string GetFullComponentName(object component)
            {
                ReceivedCall = true;
                return base.GetFullComponentName(component);
            }

            public override Type GetReflectionType(Type objectType, object instance)
            {
                ReceivedCall = true;
                return base.GetReflectionType(objectType, instance);
            }

            protected override IExtenderProvider[] GetExtenderProviders(object instance)
            {
                ReceivedCall = true;
                return base.GetExtenderProviders(instance);
            }

            public override Type GetRuntimeType(Type reflectionType)
            {
                ReceivedCall = true;
                return base.GetRuntimeType(reflectionType);
            }

            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            {
                ReceivedCall = true;
                return base.GetTypeDescriptor(objectType, instance);
            }

            public override bool IsSupportedType(Type type)
            {
                ReceivedCall = true;
                return base.IsSupportedType(type);
            }
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
    }
}
