// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public class XTypeDescriptionProviderTests
    {
        [Fact]
        public void XAttributeValuePropertyDescriptor()
        {
            var xatt = new XAttribute("someAttribute", "someValue");
            var props = TypeDescriptor.GetProperties(xatt);

            var xattrPD = props["Value"];
            Assert.NotNull(xattrPD);
            Assert.False(xattrPD.IsReadOnly);
            Assert.Equal(typeof(XAttribute), xattrPD.ComponentType);
            Assert.Equal(typeof(string), xattrPD.PropertyType);
            Assert.True(xattrPD.SupportsChangeEvents);
            Assert.False(xattrPD.CanResetValue(xatt));
            Assert.False(xattrPD.ShouldSerializeValue(xatt));

            Assert.Equal(xatt.Value, xattrPD.GetValue(xatt));

            bool valueChanged = false;
            xattrPD.AddValueChanged(xatt, (o, e) =>
            {
                valueChanged = true;
            });
            var newValue = "SomeNewValue";
            xattrPD.SetValue(xatt, newValue);

            Assert.True(valueChanged);
            Assert.Equal(newValue, xatt.Value);
        }

        [Fact]
        public void XElementAttributePropertyDescriptor()
        {
            var xel = new XElement("someElement");
            var props = TypeDescriptor.GetProperties(xel);

            var xelAttPD = props["Attribute"];
            Assert.NotNull(xelAttPD);
            Assert.True(xelAttPD.IsReadOnly);
            Assert.Equal(typeof(XElement), xelAttPD.ComponentType);
            Assert.Equal(typeof(object), xelAttPD.PropertyType);
            Assert.True(xelAttPD.SupportsChangeEvents);
            Assert.False(xelAttPD.CanResetValue(xel));
            Assert.False(xelAttPD.ShouldSerializeValue(xel));

            bool valueChanged = false;
            xelAttPD.AddValueChanged(xel, (o, e) =>
            {
                valueChanged = true;
            });

            var attr1 = new XAttribute("attr1", "value");
            xel.Add(attr1);
            Assert.False(valueChanged); // Cannot be triggered until one call to GetValue;

            // value here is a private object, it has a single item indexer that returns an XAttribute from a name.
            // once you call it once with a name it has the behavior of "binding" the value to that name
            // so that changes to that name will trigger changed events.
            // once bound it cannot be rebound to a different name.
            // only one value (the latest) is tracked by the property descriptor.
            object value = xelAttPD.GetValue(xel);

            // not exposed, must use reflection to get at it?!
            var getItemMethod = value.GetType().GetMethod("get_Item", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Func<string, XAttribute> getAttribute = (name) => (XAttribute)getItemMethod.Invoke(value, new string[] { name });

            Assert.Equal(attr1, getAttribute(attr1.Name.ToString()));

            attr1.Remove();
            Assert.True(valueChanged);

            // has been removed
            Assert.Null(getAttribute(attr1.Name.ToString()));

            valueChanged = false;
            var attr2 = new XAttribute("attr2", "value2");
            Assert.Null(getAttribute(attr2.Name.ToString()));
            Assert.False(valueChanged);
            xel.Add(attr2);
            Assert.False(valueChanged);  // value is bound to attr1
            xel.Add(attr1);
            Assert.True(valueChanged);

            attr2.Remove();
            attr1.Remove();
            valueChanged = false;

            // get a new value that hasn't be bound
            value = xelAttPD.GetValue(xel);
            // bind it to attr2, which hasn't been added
            Assert.Null(getAttribute(attr2.Name.ToString()));
            Assert.False(valueChanged);
            xel.Add(attr1);
            Assert.False(valueChanged);
            xel.Add(attr2);
            Assert.True(valueChanged);
            Assert.Equal(attr2, getAttribute(attr2.Name.ToString()));
            Assert.Null(getAttribute(attr1.Name.ToString()));

            valueChanged = false;
            attr1.Remove();
            Assert.False(valueChanged);
            attr2.Remove();
            Assert.True(valueChanged);
        }

        [Fact]
        public void XElementDescendantsPropertyDescriptor()
        {
            var xel = new XElement("someElement");
            var props = TypeDescriptor.GetProperties(xel);

            var xelDesPD = props["Descendants"];
            Assert.NotNull(xelDesPD);
            Assert.True(xelDesPD.IsReadOnly);
            Assert.Equal(typeof(XElement), xelDesPD.ComponentType);
            Assert.Equal(typeof(IEnumerable<XElement>), xelDesPD.PropertyType);
            Assert.True(xelDesPD.SupportsChangeEvents);
            Assert.False(xelDesPD.CanResetValue(xel));
            Assert.False(xelDesPD.ShouldSerializeValue(xel));

            var dess = (IEnumerable<XElement>)xelDesPD.GetValue(xel);

            bool valueChanged = false;
            xelDesPD.AddValueChanged(xel, (o, e) =>
            {
                valueChanged = true;
            });

            xel.Add(new XElement("c1", new XElement("gc1", new XElement("ggc1"))), new XElement("c2"));
            Assert.True(valueChanged);

            Assert.Equal(dess, xel.Descendants());

            valueChanged = false;
            xel.Element("c1").Remove();
            Assert.True(valueChanged);
        }

        [Fact]
        public void XElementElementPropertyDescriptor()
        {
            var xel = new XElement("someElement");
            var props = TypeDescriptor.GetProperties(xel);

            var xelElPD = props["Element"];
            Assert.True(xelElPD.IsReadOnly);
            Assert.Equal(typeof(XElement), xelElPD.ComponentType);
            Assert.Equal(typeof(object), xelElPD.PropertyType);
            Assert.True(xelElPD.SupportsChangeEvents);
            Assert.False(xelElPD.CanResetValue(xel));
            Assert.False(xelElPD.ShouldSerializeValue(xel));

            bool valueChanged = false;
            xelElPD.AddValueChanged(xel, (o, e) =>
            {
                valueChanged = true;
            });

            var el1 = new XElement("el1");
            xel.Add(el1);
            Assert.False(valueChanged); // Cannot be triggered until one call to GetValue;

            // value here is a private object, it has a single item indexer that returns an XAttribute from a name.
            // once you call it once with a name it has the behavior of "binding" the value to that name
            // so that changes to that name will trigger changed events.
            // once bound it cannot be rebound to a different name.
            // only one value (the latest) is tracked by the property descriptor.
            object value = xelElPD.GetValue(xel);

            // not exposed, must use reflection to get at it?!
            var getItemMethod = value.GetType().GetMethod("get_Item", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Func<string, XElement> getElement = (name) => (XElement)getItemMethod.Invoke(value, new string[] { name });

            Assert.Equal(el1, getElement(el1.Name.ToString()));

            el1.Remove();
            Assert.True(valueChanged);

            // has been removed
            Assert.Null(getElement(el1.Name.ToString()));

            valueChanged = false;
            var el2 = new XElement("el2");
            Assert.Null(getElement(el2.Name.ToString()));
            Assert.False(valueChanged);
            xel.Add(el2);
            Assert.False(valueChanged);  // value is bound to attr1
            xel.Add(el1);
            Assert.True(valueChanged);

            el2.Remove();
            el1.Remove();
            valueChanged = false;

            // get a new value that hasn't be bound
            value = xelElPD.GetValue(xel);
            // bind it to attr2, which hasn't been added
            Assert.Null(getElement(el2.Name.ToString()));
            Assert.False(valueChanged);
            xel.Add(el1);
            Assert.False(valueChanged);
            xel.Add(el2);
            Assert.True(valueChanged);
            Assert.Equal(el2, getElement(el2.Name.ToString()));
            Assert.Null(getElement(el1.Name.ToString()));

            valueChanged = false;
            el1.Remove();
            Assert.False(valueChanged);
            el2.Remove();
            Assert.True(valueChanged);
        }

        [Fact]
        public void XElementElementsPropertyDescriptor()
        {
            var xel = new XElement("someElement");
            var props = TypeDescriptor.GetProperties(xel);

            var xelElsPD = props["Elements"];
            Assert.NotNull(xelElsPD);
            Assert.True(xelElsPD.IsReadOnly);
            Assert.Equal(typeof(XElement), xelElsPD.ComponentType);
            Assert.Equal(typeof(IEnumerable<XElement>), xelElsPD.PropertyType);
            Assert.True(xelElsPD.SupportsChangeEvents);
            Assert.False(xelElsPD.CanResetValue(xel));
            Assert.False(xelElsPD.ShouldSerializeValue(xel));

            var els = (IEnumerable<XElement>)xelElsPD.GetValue(xel);

            bool valueChanged = false;
            xelElsPD.AddValueChanged(xel, (o, e) =>
            {
                valueChanged = true;
            });

            xel.Add(new XElement("c1"), new XElement("c2"));
            Assert.True(valueChanged);

            Assert.Equal(els, xel.Elements());

            valueChanged = false;
            xel.Element("c1").Remove();
            Assert.True(valueChanged);
        }

        [Fact]
        public void XElementValuePropertyDescriptor()
        {
            var xel = new XElement("someElement", "someValue");
            var props = TypeDescriptor.GetProperties(xel);

            var xelValPD = props["Value"];
            Assert.NotNull(xelValPD);
            Assert.False(xelValPD.IsReadOnly);
            Assert.Equal(typeof(XElement), xelValPD.ComponentType);
            Assert.Equal(typeof(string), xelValPD.PropertyType);
            Assert.True(xelValPD.SupportsChangeEvents);
            Assert.False(xelValPD.CanResetValue(xel));
            Assert.False(xelValPD.ShouldSerializeValue(xel));

            Assert.Equal(xel.Value, xelValPD.GetValue(xel));

            bool valueChanged = false;
            xelValPD.AddValueChanged(xel, (o, e) =>
            {
                valueChanged = true;
            });
            var newValue = "SomeNewValue";
            xelValPD.SetValue(xel, newValue);

            Assert.True(valueChanged);
            Assert.Equal(newValue, xel.Value);

            valueChanged = false;
            xel.Value = "AnotherValue";
            Assert.True(valueChanged);
            Assert.Equal(xel.Value, xelValPD.GetValue(xel));
        }

        [Fact]
        public void XElementXmlPropertyDescriptor()
        {
            var xel = new XElement("someElement");
            var props = TypeDescriptor.GetProperties(xel);

            var xelXmlPD = props["Xml"];
            Assert.NotNull(xelXmlPD);
            Assert.True(xelXmlPD.IsReadOnly);
            Assert.Equal(typeof(XElement), xelXmlPD.ComponentType);
            Assert.Equal(typeof(string), xelXmlPD.PropertyType);
            Assert.True(xelXmlPD.SupportsChangeEvents);
            Assert.False(xelXmlPD.CanResetValue(xel));
            Assert.False(xelXmlPD.ShouldSerializeValue(xel));

            Assert.Equal(xel.ToString(), xelXmlPD.GetValue(xel));

            bool valueChanged = false;
            xelXmlPD.AddValueChanged(xel, (o, e) =>
            {
                valueChanged = true;
            });
            xel.Value = "abc123";
            Assert.True(valueChanged);
            Assert.Equal(xel.ToString(), xelXmlPD.GetValue(xel));
        }
    }
}
