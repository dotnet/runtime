// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class CustomTypeDescriptorTests
    {
        [Fact]
        public void ReturnsDefaultValues()
        {
            var defaultCustomTypeDescriptor = new CallEmptyConstructor();

            Assert.Same(AttributeCollection.Empty, defaultCustomTypeDescriptor.GetAttributes());
            Assert.Null(defaultCustomTypeDescriptor.GetClassName());
            Assert.Null(defaultCustomTypeDescriptor.GetComponentName());
            Assert.NotNull(defaultCustomTypeDescriptor.GetConverter());
            Assert.Null(defaultCustomTypeDescriptor.GetDefaultEvent());
            Assert.Null(defaultCustomTypeDescriptor.GetDefaultProperty());
            Assert.Null(defaultCustomTypeDescriptor.GetEditor(typeof(int)));
            Assert.Same(EventDescriptorCollection.Empty, defaultCustomTypeDescriptor.GetEvents());
            Assert.Same(EventDescriptorCollection.Empty, defaultCustomTypeDescriptor.GetEvents(new Attribute[0]));
            Assert.Same(PropertyDescriptorCollection.Empty, defaultCustomTypeDescriptor.GetProperties());
            Assert.Same(PropertyDescriptorCollection.Empty, defaultCustomTypeDescriptor.GetProperties(new Attribute[0]));
            Assert.Null(defaultCustomTypeDescriptor.GetPropertyOwner(null));
        }

        [Fact]
        public void ReturnsParentValues()
        {
            var parent = new ParentCustomTypeDescriptor();
            var customTypeDescriptor = new InjectsParent(parent);

            Assert.Same(parent.GetAttributes(), customTypeDescriptor.GetAttributes());
            Assert.Same(parent.GetClassName(), customTypeDescriptor.GetClassName());
            Assert.Same(parent.GetComponentName(), customTypeDescriptor.GetComponentName());
            Assert.Same(parent.GetConverter(), customTypeDescriptor.GetConverter());
            Assert.Same(parent.GetDefaultEvent(), customTypeDescriptor.GetDefaultEvent());
            Assert.Same(parent.GetDefaultProperty(), customTypeDescriptor.GetDefaultProperty());
            Assert.Same(parent.GetEditor(/* any value */ null), customTypeDescriptor.GetEditor(/* any value */ null));
            Assert.Same(parent.GetEvents(), customTypeDescriptor.GetEvents());
            Assert.Same(parent.GetEvents(/* any value */ null), customTypeDescriptor.GetEvents(/* any value */ null));
            Assert.Same(parent.GetProperties(), customTypeDescriptor.GetProperties());
            Assert.Same(parent.GetProperties(/* any value */ null), customTypeDescriptor.GetProperties(/* any value */ null));
            Assert.Same(parent.GetPropertyOwner(/* any value */ null), customTypeDescriptor.GetPropertyOwner(/* any value */ null));
        }

        private class CallEmptyConstructor : CustomTypeDescriptor
        {
        }

        private class InjectsParent : CustomTypeDescriptor
        {
            public InjectsParent(ICustomTypeDescriptor parent)
                : base(parent)
            { }
        }

        /// <summary>
        /// An implementation of ICustomTypeDescriptor to be used to mock the parent injected into
        /// CustomTypeDescriptor. All of the items it returns are cached so they are the same instance
        /// each time the method is called. This way, the tests can verify that the parent instance is
        /// being returned by CustomTypeDescriptor for each of the methods.
        /// </summary>
        private class ParentCustomTypeDescriptor : ICustomTypeDescriptor
        {
            private readonly TypeConverter _converter = new TypeConverter();
            private readonly AttributeCollection _attributes = new AttributeCollection(null);
            private readonly EventDescriptor _defaultEvent = new MockEventDescriptor();
            private readonly PropertyDescriptor _defaultProperty = new MockPropertyDescriptor();
            private readonly EventDescriptorCollection _events1 = new EventDescriptorCollection(null);
            private readonly EventDescriptorCollection _events2 = new EventDescriptorCollection(null);
            private readonly PropertyDescriptorCollection _properties1 = new PropertyDescriptorCollection(null);
            private readonly PropertyDescriptorCollection _properties2 = new PropertyDescriptorCollection(null);
            private readonly object _editor = new object();
            private readonly object _propertyOwner = new object();

            public AttributeCollection GetAttributes() => _attributes;

            public string GetClassName() => "ClassName";

            public string GetComponentName() => "ComponentName";

            public TypeConverter GetConverter() => _converter;

            public EventDescriptor GetDefaultEvent() => _defaultEvent;

            public PropertyDescriptor GetDefaultProperty() => _defaultProperty;

            public object GetEditor(Type editorBaseType) => _editor;

            public EventDescriptorCollection GetEvents() => _events1;

            public EventDescriptorCollection GetEvents(Attribute[] attributes) => _events2;

            public PropertyDescriptorCollection GetProperties() => _properties1;

            public PropertyDescriptorCollection GetProperties(Attribute[] attributes) => _properties2;

            public object GetPropertyOwner(PropertyDescriptor pd) => _propertyOwner;
        }
    }
}
