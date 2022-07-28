// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class PropertyDescriptorTests
    {
        [Fact]
        public void CopyConstructorAddsAttribute()
        {
            var component = new DescriptorTestComponent();
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor oldPropertyDescriptor = properties.Find(nameof(component.Property), false);
            var newAttribute = new DescriptorTestAttribute();

            PropertyDescriptor newPropertyDescriptor = TypeDescriptor.CreateProperty(component.GetType(), oldPropertyDescriptor, newAttribute);

            Assert.True(newPropertyDescriptor.Attributes.Contains(newAttribute));
        }

        [Fact]
        public void RaiseAddedValueChangedHandler()
        {
            var component = new DescriptorTestComponent();
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.Property), false);
            var handlerWasCalled = false;
            EventHandler valueChangedHandler = (_, __) => handlerWasCalled = true;

            propertyDescriptor.AddValueChanged(component, valueChangedHandler);
            propertyDescriptor.SetValue(component, int.MaxValue);

            Assert.True(handlerWasCalled);
        }

        [Fact]
        public void RemoveAddedValueChangedHandler()
        {
            var component = new DescriptorTestComponent();
            var properties = TypeDescriptor.GetProperties(component.GetType());
            var handlerWasCalled = false;
            EventHandler valueChangedHandler = (_, __) => handlerWasCalled = true;
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.Property), false);

            propertyDescriptor.AddValueChanged(component, valueChangedHandler);
            propertyDescriptor.RemoveValueChanged(component, valueChangedHandler);
            propertyDescriptor.SetValue(component, int.MaxValue);

            Assert.False(handlerWasCalled);
        }

        [Fact]
        public void ResetValueReturnsFalseWhenValueEqualsDefault()
        {
            var component = new DescriptorTestComponent();
            component.Property = DescriptorTestComponent.DefaultPropertyValue;
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.Property), false);

            Assert.False(propertyDescriptor.CanResetValue(component));
        }

        [Fact]
        public void GetComponentType()
        {
            var component = new DescriptorTestComponent();
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.Property), false);

            Assert.Equal(component.GetType(), propertyDescriptor.ComponentType);
        }

        [Fact]
        public void GetPropertyType()
        {
            var component = new DescriptorTestComponent();
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.Property), false);

            Assert.Equal(component.Property.GetType(), propertyDescriptor.PropertyType);
        }

        [Fact]
        public void GetValue()
        {
            var component = new DescriptorTestComponent();
            component.Property = 37;
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.Property), false);

            var retrievedValue = propertyDescriptor.GetValue(component);

            Assert.Equal(component.Property, retrievedValue);
        }


        [Fact]
        public void GetValueDoesNotHandleExceptions()
        {
            var component = new DescriptorTestComponent();
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.PropertyWhichThrows), false);

            Assert.Throws<TargetInvocationException>(() => propertyDescriptor.GetValue(component));
        }

        [Fact]
        public void ResetValue()
        {
            var component = new DescriptorTestComponent();
            component.Property = DescriptorTestComponent.DefaultPropertyValue - 1;
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.Property), false);

            // this should set the property's value to that provided by the DefaultValueAttribute
            propertyDescriptor.ResetValue(component);

            Assert.Equal(DescriptorTestComponent.DefaultPropertyValue, component.Property);
        }

        [Fact]
        public void ShouldSerializeValueReturnsFalseWhenValueIsDefault()
        {
            var component = new DescriptorTestComponent();
            component.Property = DescriptorTestComponent.DefaultPropertyValue;
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.Property), false);

            Assert.False(propertyDescriptor.ShouldSerializeValue(component));
        }

        [Fact]
        public void ShouldSerializeValueReturnsTrueWhenValueIsNotDefault()
        {
            var component = new DescriptorTestComponent();
            component.Property = DescriptorTestComponent.DefaultPropertyValue - 1;
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.Property), false);

            Assert.True(propertyDescriptor.ShouldSerializeValue(component));
        }

        [Fact]
        public void ShouldSerializeValueHandlesEnumWithBackingTypeDefaultValue()
        {
            var component = new DescriptorTestComponent();
            component.EnumProperty = DescriptorTestEnum.Value1;
            var properties = TypeDescriptor.GetProperties(component.GetType());
            PropertyDescriptor propertyDescriptor = properties.Find(nameof(component.EnumProperty), false);

            Assert.True(propertyDescriptor.ShouldSerializeValue(component));
        }

        [Fact]
        public static void ReadOnlyPropertyReturnsTrue()
        {
            var foo = new ReadOnlyPropertyTestClass();
            PropertyDescriptor property = TypeDescriptor.GetProperties(foo).Find("BarReadOnly", true);
            Assert.True(property.IsReadOnly);
        }

        [Fact]
        public static void ReadOnlyPropertyReturnsFalse()
        {
            var foo = new ReadOnlyPropertyTestClass();
            PropertyDescriptor property = TypeDescriptor.GetProperties(foo).Find("BarNotReadOnly", true);
            Assert.False(property.IsReadOnly);
        }

        [Fact]
        public static void ReadOnlyVirtualPropertyReturnsTrue()
        {
            PropertyDescriptor property = TypeDescriptor.GetProperties(typeof(ReadOnlyPropertyTestClass), new[] { BrowsableAttribute.Yes }).Find("BarReadOnlyBaseClass", true);
            Assert.True(property.IsReadOnly);
        }

        class ReadOnlyPropertyTestBaseClass
        {
            public virtual bool BarReadOnlyBaseClass { get; }
        }

        class ReadOnlyPropertyTestClass : ReadOnlyPropertyTestBaseClass
        {
            public bool BarReadOnly { get; private set; }
            public bool BarNotReadOnly { get; set; }
            public override bool BarReadOnlyBaseClass { get; }
        }

        [Fact]
        public void PropertyDescriptorCanBeCreatedThatAccessesNonPublicProperty()
        {
            var component = new DescriptorTestComponent();

            // Create a new property that "exposes" a protected property on our component.
            PropertyDescriptor property = TypeDescriptor.CreateProperty(
                componentType: typeof(DescriptorTestComponent),
                name: DescriptorTestComponent.ProtectedStringPropertyName,
                type: typeof(string));

            const string PropertyValue = "Test";

            property.SetValue(component, PropertyValue);
            Assert.Equal(PropertyValue, property.GetValue(component));
            Assert.Equal(PropertyValue, component.ProtectedStringPropertyValue);
        }

        [Fact]
        public void PropertyDescriptorCanBeCreatedThatAccessesNonPublicPropertyOnAssociatedDesigner()
        {
            var designer = new MockDesigner();
            var designerHost = new MockDesignerHost();
            var component = new DescriptorTestComponent();

            designerHost.AddDesigner(component, designer);
            component.AddService(typeof(IDesignerHost), designerHost);

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(component);
            PropertyDescriptor stringProperty = properties[nameof(DescriptorTestComponent.StringProperty)];
            Assert.NotNull(stringProperty);

            // Create new property that "wraps" stringProperty and redirects to the designer.
            // Note that a ReadOnlyAttribute is added to ensure that we can set it
            PropertyDescriptor newStringProperty = TypeDescriptor.CreateProperty(
                componentType: typeof(MockDesigner),
                oldPropertyDescriptor: stringProperty,
                attributes: new[] { ReadOnlyAttribute.No });

            // The property descriptor should be redirected to reflect over the designer.
            const string PropertyValue = "Test";

            Assert.False(designer.StringPropertyHasBeenSet);
            newStringProperty.SetValue(component, PropertyValue);
            Assert.Empty(component.StringProperty);
            Assert.Equal(PropertyValue, newStringProperty.GetValue(component));
            Assert.True(designer.StringPropertyHasBeenSet);

            Assert.False(designer.ShouldSerializeStringPropertyCalled);
            Assert.True(newStringProperty.ShouldSerializeValue(component));
            Assert.True(designer.ShouldSerializeStringPropertyCalled);

            Assert.False(designer.ResetStringPropertyCalled);
            newStringProperty.ResetValue(component);
            Assert.True(designer.ResetStringPropertyCalled);
        }
    }
}
