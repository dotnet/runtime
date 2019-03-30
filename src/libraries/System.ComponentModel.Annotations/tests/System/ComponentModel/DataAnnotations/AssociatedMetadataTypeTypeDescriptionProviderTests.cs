// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class AssociatedMetadataTypeTypeDescriptionProviderTests
    {
        [Fact]
        public void Ctor_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => new AssociatedMetadataTypeTypeDescriptionProvider(null));
        }

        [Fact]
        public void Ctor_NullAssociatedMetadataType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("associatedMetadataType", () => new AssociatedMetadataTypeTypeDescriptionProvider(typeof(string), null));
        }

        [Fact]
        public void GetTypeDescriptor_MetadataHasFieldsNotPresentOnClass_ThrowsInvalidOperationException()
        {
            var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithInvalidMetadata));
            Assert.Throws<InvalidOperationException>(() => provider.GetTypeDescriptor(typeof(ClassWithInvalidMetadata), null));
        }

        [Fact]
        public void GetTypeDescriptorGetAttributes_NoAssociatedMetadataTypeWithoutMetadataTypeAttribute_ReturnsExpected()
        {
            // Perform multiple times to test static caching behaviour.
            for (int i = 0; i < 2; i++)
            {
                var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadata));
                ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(ClassWithMetadata), null);
                AttributeCollection attributes = typeDescriptor.GetAttributes();
                Assert.Equal(1, attributes.Count);
                Assert.Equal("typeName", Assert.IsType<TypeConverterAttribute>(attributes[typeof(TypeConverterAttribute)]).ConverterTypeName);
            }
        }

        [Fact]
        public void GetTypeDescriptorGetAttributes_NoAssociatedMetadataTypeWithMetadataTypeAttribute_ReturnsExpected()
        {
            // Perform multiple times to test static caching behaviour.
            for (int i = 0; i < 2; i++)
            {
                var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass));
                ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null);
                AttributeCollection attributes = typeDescriptor.GetAttributes();
                Assert.Equal(2, attributes.Count);
                Assert.Equal(typeof(ClassWithMetadata), Assert.IsType<MetadataTypeAttribute>(attributes[typeof(MetadataTypeAttribute)]).MetadataClassType);
                Assert.Equal("typeName", Assert.IsType<TypeConverterAttribute>(attributes[typeof(TypeConverterAttribute)]).ConverterTypeName);
            }
        }

        [Fact]
        public void GetTypeDescriptorGetAttributes_WithAssociatedMetadataType_ReturnsExpected()
        {
            // Perform multiple times to test static caching behaviour.
            for (int i = 0; i < 2; i++)
            {
                var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass), typeof(ClassWithAttributes));
                ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null);
                AttributeCollection attributes = typeDescriptor.GetAttributes();
                Assert.Equal(2, attributes.Count);
                Assert.Equal(typeof(ClassWithMetadata), Assert.IsType<MetadataTypeAttribute>(attributes[typeof(MetadataTypeAttribute)]).MetadataClassType);
                Assert.Equal(EditorBrowsableState.Always, Assert.IsType<EditorBrowsableAttribute>(attributes[typeof(EditorBrowsableAttribute)]).State);
            }
        }

        [Fact]
        public void GetTypeDescriptorGetAttributes_SameAssociatedMetadataType_ReturnsExpected()
        {
            // Perform multiple times to test static caching behaviour.
            for (int i = 0; i < 2; i++)
            {
                var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadata), typeof(ClassWithMetadata));
                ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(ClassWithMetadata));
                AttributeCollection attributes = typeDescriptor.GetAttributes();
                Assert.Equal("typeName", Assert.IsType<TypeConverterAttribute>(attributes[typeof(TypeConverterAttribute)]).ConverterTypeName);
            }
        }

        [Fact]
        public void GetTypeDescriptorGetAttributes_SelfAssociatedMetadataType_ReturnsExpected()
        {
            // Perform multiple times to test static caching behaviour.
            for (int i = 0; i < 2; i++)
            {
                var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithSelfAssociatedMetadata));
                ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(ClassWithSelfAssociatedMetadata));
                AttributeCollection attributes = typeDescriptor.GetAttributes();
                Assert.Equal("typeName", Assert.IsType<TypeConverterAttribute>(attributes[typeof(TypeConverterAttribute)]).ConverterTypeName);
            }
        }

        [Fact]
        public void GetTypeDescriptorGetProperties_NoAssociatedMetadataTypeWithoutMetadataTypeAttribute_ReturnsExpected()
        {
            // Perform multiple times to test static caching behaviour.
            for (int i = 0; i < 2; i++)
            {
                var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadata));
                ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(ClassWithMetadata), null);
                PropertyDescriptorCollection properties = typeDescriptor.GetProperties();
                PropertyDescriptor firstNameProperty = properties[nameof(ClassWithMetadata.FirstName)];
                PropertyDescriptor lastNameProperty = properties[nameof(ClassWithMetadata.LastName)];

                Assert.Equal("First name", firstNameProperty.DisplayName);
                Assert.Equal(typeof(ClassWithMetadata), firstNameProperty.ComponentType);
                Assert.Equal(typeof(string), firstNameProperty.PropertyType);
                Assert.True(firstNameProperty.IsReadOnly);
                Assert.False(lastNameProperty.SupportsChangeEvents);

                Assert.Equal("LastName", lastNameProperty.DisplayName);
                Assert.Equal(typeof(ClassWithMetadata), lastNameProperty.ComponentType);
                Assert.Equal(typeof(string), lastNameProperty.PropertyType);
                Assert.False(lastNameProperty.IsReadOnly);
                Assert.False(lastNameProperty.SupportsChangeEvents);
            }
        }

        [Fact]
        public void GetTypeDescriptorGetProperties_NoAssociatedMetadataTypeWithMetadataTypeAttribute_ReturnsExpected()
        {
            // Perform multiple times to test static caching behaviour.
            for (int i = 0; i < 2; i++)
            {
                var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadata));
                ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(ClassWithMetadata), null);
                PropertyDescriptorCollection properties = typeDescriptor.GetProperties(new Attribute[] { new RequiredAttribute() });
                PropertyDescriptor firstNameProperty = properties[nameof(ClassWithMetadata.FirstName)];
                PropertyDescriptor lastNameProperty = properties[nameof(ClassWithMetadata.LastName)];

                Assert.Equal("First name", firstNameProperty.DisplayName);
                Assert.Equal(typeof(ClassWithMetadata), firstNameProperty.ComponentType);
                Assert.Equal(typeof(string), firstNameProperty.PropertyType);
                Assert.True(firstNameProperty.IsReadOnly);
                Assert.False(firstNameProperty.SupportsChangeEvents);
                
                Assert.Equal("LastName", lastNameProperty.DisplayName);
                Assert.Equal(typeof(ClassWithMetadata), lastNameProperty.ComponentType);
                Assert.Equal(typeof(string), lastNameProperty.PropertyType);
                Assert.False(lastNameProperty.IsReadOnly);
                Assert.False(lastNameProperty.SupportsChangeEvents);
            }
        }

        [Theory]
        [InlineData(typeof(ClassWithSelfAssociatedMetadata), "Last name")]
        [InlineData(typeof(ClassWithMetadata), "LastName")]
        [InlineData(typeof(EmptyClass), "LastName")]
        public void GetTypeDescriptorGetProperties_WithAssociatedMetadataType_ReturnsExpected(Type associatedMetadataType, string expectedLastName)
        {
            // Perform multiple times to test static caching behaviour.
            for (int i = 0; i < 2; i++)
            {
                var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadata), associatedMetadataType);
                ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(ClassWithMetadata), null);
                PropertyDescriptorCollection properties = typeDescriptor.GetProperties();
                PropertyDescriptor firstNameProperty = properties[nameof(ClassWithMetadata.FirstName)];
                PropertyDescriptor lastNameProperty = properties[nameof(ClassWithMetadata.LastName)];

                Assert.Equal("First name", firstNameProperty.DisplayName);
                Assert.Equal(typeof(ClassWithMetadata), firstNameProperty.ComponentType);
                Assert.Equal(typeof(string), firstNameProperty.PropertyType);
                Assert.True(firstNameProperty.IsReadOnly);
                Assert.False(firstNameProperty.SupportsChangeEvents);

                Assert.Equal(expectedLastName, lastNameProperty.DisplayName);
                Assert.Equal(typeof(ClassWithMetadata), lastNameProperty.ComponentType);
                Assert.Equal(typeof(string), lastNameProperty.PropertyType);
                Assert.False(lastNameProperty.IsReadOnly);
                Assert.False(lastNameProperty.SupportsChangeEvents);
            }
        }

        [Fact]
        public void GetTypeDescriptorGetProperties_WithMetadataTypeAttribute_ReturnsExpected()
        {
            // Perform multiple times to test static caching behaviour.
            for (int i = 0; i < 2; i++)
            {
                var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass));
                ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null);
                PropertyDescriptorCollection properties = typeDescriptor.GetProperties();
                PropertyDescriptor firstNameProperty = properties[nameof(ClassWithMetadataOnAnotherClass.FirstName)];
                PropertyDescriptor lastNameProperty = properties[nameof(ClassWithMetadataOnAnotherClass.LastName)];

                Assert.Equal("First name", firstNameProperty.DisplayName);
                Assert.Equal(typeof(ClassWithMetadataOnAnotherClass), firstNameProperty.ComponentType);
                Assert.Equal(typeof(string), firstNameProperty.PropertyType);
                Assert.True(firstNameProperty.IsReadOnly);
                Assert.False(firstNameProperty.SupportsChangeEvents);
                
                Assert.Equal("Last name", lastNameProperty.DisplayName);
                Assert.Equal(typeof(ClassWithMetadataOnAnotherClass), lastNameProperty.ComponentType);
                Assert.Equal(typeof(string), lastNameProperty.PropertyType);
                Assert.False(lastNameProperty.IsReadOnly);
                Assert.False(lastNameProperty.SupportsChangeEvents);
            }
        }

        [Fact]
        public void GetTypeDescriptorGetPropertiesWrappedPropertyDescriptor_GetValue_Success()
        {
            var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass));
            PropertyDescriptorCollection properties = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null).GetProperties();
            PropertyDescriptor descriptor = properties[nameof(ClassWithMetadataOnAnotherClass.FirstName)];

            var component = new ClassWithMetadataOnAnotherClass
            {
                FirstName = "value"
            };
            Assert.Equal("value", descriptor.GetValue(component));
        }

        [Fact]
        public void GetTypeDescriptorGetPropertiesWrappedPropertyDescriptor_SetValue_Success()
        {
            var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass));
            PropertyDescriptorCollection properties = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null).GetProperties();
            PropertyDescriptor descriptor = properties[nameof(ClassWithMetadataOnAnotherClass.FirstName)];

            var component = new ClassWithMetadataOnAnotherClass();
            descriptor.SetValue(component, "value");
            Assert.Equal("value", descriptor.GetValue(component));
            Assert.Equal("value", component.FirstName);
        }

        [Fact]
        public void GetTypeDescriptorGetPropertiesWrappedPropertyDescriptor_ResetValue_Success()
        {
            var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass));
            PropertyDescriptorCollection properties = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null).GetProperties();
            PropertyDescriptor descriptor = properties[nameof(ClassWithMetadataOnAnotherClass.FirstName)];

            var component = new ClassWithMetadataOnAnotherClass
            {
                FirstName = "value"
            };
            descriptor.ResetValue(component);
            Assert.Equal("value", descriptor.GetValue(component));
            Assert.Equal("value", component.FirstName);
        }

        [Fact]
        public void GetTypeDescriptorGetPropertiesWrappedPropertyDescriptor_CanResetValue_Success()
        {
            var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass));
            PropertyDescriptorCollection properties = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null).GetProperties();
            PropertyDescriptor descriptor = properties[nameof(ClassWithMetadataOnAnotherClass.FirstName)];

            var component = new ClassWithMetadataOnAnotherClass();
            Assert.False(descriptor.CanResetValue(component));
        }

        [Fact]
        public void GetTypeDescriptorGetPropertiesWrappedPropertyDescriptor_ShouldSerializeValue_Success()
        {
            var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass));
            PropertyDescriptorCollection properties = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null).GetProperties();
            PropertyDescriptor descriptor = properties[nameof(ClassWithMetadataOnAnotherClass.FirstName)];

            var component = new ClassWithMetadataOnAnotherClass();
            Assert.True(descriptor.ShouldSerializeValue(component));
        }

        [Fact]
        public void GetTypeDescriptorGetPropertiesWrappedPropertyDescriptor_AddValueChanged_Success()
        {
            var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass));
            PropertyDescriptorCollection properties = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null).GetProperties();
            PropertyDescriptor descriptor = properties[nameof(ClassWithMetadataOnAnotherClass.FirstName)];

            var component = new ClassWithMetadataOnAnotherClass();
            int callCount = 0;
            EventHandler handler = (sender, e) => callCount++;
            descriptor.AddValueChanged(component, handler);
            descriptor.SetValue(component, "value");
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GetTypeDescriptorGetPropertiesWrappedPropertyDescriptor_RemoveValueChanged_Success()
        {
            var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(ClassWithMetadataOnAnotherClass));
            PropertyDescriptorCollection properties = provider.GetTypeDescriptor(typeof(ClassWithMetadataOnAnotherClass), null).GetProperties();
            PropertyDescriptor descriptor = properties[nameof(ClassWithMetadataOnAnotherClass.FirstName)];

            var component = new ClassWithMetadataOnAnotherClass();
            int callCount = 0;
            EventHandler handler = (sender, e) => callCount++;
            descriptor.AddValueChanged(component, handler);
            descriptor.RemoveValueChanged(component, handler);
            descriptor.SetValue(component, "value");
            Assert.Equal(0, callCount);
        }

        [TypeConverter("typeName")]
        public class ClassWithMetadata
        {
            [ReadOnly(true)]
            [DisplayName("First name")]
            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [MetadataType(typeof(ClassWithMetadata))]
        public class ClassWithMetadataOnAnotherClass
        {
            public string FirstName { get; set; }

            [DisplayName("Last name")]
            public string LastName { get; set; }
        }

        [MetadataType(typeof(ClassWithMetadata))]
        public class ClassWithInvalidMetadata
        {
            public string FirstName { get; set; }
        }

        [TypeConverter("typeName")]
        [MetadataType(typeof(ClassWithSelfAssociatedMetadata))]
        public class ClassWithSelfAssociatedMetadata
        {
            [DisplayName("Last name")]
            public string LastName { get; set; }
        }

        public class EmptyClass
        {
        }

        [EditorBrowsable(EditorBrowsableState.Always)]
        public class ClassWithAttributes
        {
        }
    }
}
