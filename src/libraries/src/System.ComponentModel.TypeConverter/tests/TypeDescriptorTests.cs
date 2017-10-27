// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeDescriptorTests
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

        [Fact]
        public static void GetConverter()
        {
            foreach (Tuple<Type, Type> pair in s_typesWithConverters)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(pair.Item1);
                Assert.NotNull(converter);
                Assert.Equal(pair.Item2, converter.GetType());
                Assert.True(converter.CanConvertTo(typeof(string)));
            }
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

        private static Tuple<Type, Type>[] s_typesWithConverters =
        {
            new Tuple<Type, Type> (typeof(bool), typeof(BooleanConverter)),
            new Tuple<Type, Type> (typeof(byte), typeof(ByteConverter)),
            new Tuple<Type, Type> (typeof(SByte), typeof(SByteConverter)),
            new Tuple<Type, Type> (typeof(char), typeof(CharConverter)),
            new Tuple<Type, Type> (typeof(double), typeof(DoubleConverter)),
            new Tuple<Type, Type> (typeof(string), typeof(StringConverter)),
            new Tuple<Type, Type> (typeof(short), typeof(Int16Converter)),
            new Tuple<Type, Type> (typeof(int), typeof(Int32Converter)),
            new Tuple<Type, Type> (typeof(long), typeof(Int64Converter)),
            new Tuple<Type, Type> (typeof(float), typeof(SingleConverter)),
            new Tuple<Type, Type> (typeof(UInt16), typeof(UInt16Converter)),
            new Tuple<Type, Type> (typeof(UInt32), typeof(UInt32Converter)),
            new Tuple<Type, Type> (typeof(UInt64), typeof(UInt64Converter)),
            new Tuple<Type, Type> (typeof(object), typeof(TypeConverter)),
            new Tuple<Type, Type> (typeof(void), typeof(TypeConverter)),
            new Tuple<Type, Type> (typeof(DateTime), typeof(DateTimeConverter)),
            new Tuple<Type, Type> (typeof(DateTimeOffset), typeof(DateTimeOffsetConverter)),
            new Tuple<Type, Type> (typeof(Decimal), typeof(DecimalConverter)),
            new Tuple<Type, Type> (typeof(TimeSpan), typeof(TimeSpanConverter)),
            new Tuple<Type, Type> (typeof(Guid), typeof(GuidConverter)),
            new Tuple<Type, Type> (typeof(Array), typeof(ArrayConverter)),
            new Tuple<Type, Type> (typeof(ICollection), typeof(CollectionConverter)),
            new Tuple<Type, Type> (typeof(Enum), typeof(EnumConverter)),
            new Tuple<Type, Type> (typeof(SomeEnum), typeof(EnumConverter)),
            new Tuple<Type, Type> (typeof(SomeValueType?), typeof(NullableConverter)),
            new Tuple<Type, Type> (typeof(int?), typeof(NullableConverter)),
            new Tuple<Type, Type> (typeof(ClassWithNoConverter), typeof(TypeConverter)),
            new Tuple<Type, Type> (typeof(BaseClass), typeof(BaseClassConverter)),
            new Tuple<Type, Type> (typeof(DerivedClass), typeof(DerivedClassConverter)),
            new Tuple<Type, Type> (typeof(IBase), typeof(IBaseConverter)),
            new Tuple<Type, Type> (typeof(IDerived), typeof(IBaseConverter)),
            new Tuple<Type, Type> (typeof(ClassIBase), typeof(IBaseConverter)),
            new Tuple<Type, Type> (typeof(ClassIDerived), typeof(IBaseConverter)),
            new Tuple<Type, Type> (typeof(Uri), typeof(UriTypeConverter)),
            new Tuple<Type, Type> (typeof(CultureInfo), typeof(CultureInfoConverter))
        };
    }
}
