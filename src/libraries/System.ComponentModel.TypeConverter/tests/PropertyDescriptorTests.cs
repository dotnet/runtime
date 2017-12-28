// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
