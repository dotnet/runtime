// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel
{
    internal sealed class XTypeDescriptionProvider<T> : TypeDescriptionProvider
    {
        public XTypeDescriptionProvider() : base(TypeDescriptor.GetProvider(typeof(T)))
        {
        }

        public override ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, object? instance)
        {
            return new XTypeDescriptor<T>(base.GetTypeDescriptor(type, instance));
        }
    }

    internal sealed class XTypeDescriptor<T> : CustomTypeDescriptor
    {
        public XTypeDescriptor(ICustomTypeDescriptor? parent) : base(parent)
        {
        }

        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
        public override PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(null);
        }

        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
        {
            PropertyDescriptorCollection properties = new PropertyDescriptorCollection(null);
            if (attributes == null)
            {
                if (typeof(T) == typeof(XElement))
                {
                    properties.Add(new XElementAttributePropertyDescriptor());
                    properties.Add(new XElementDescendantsPropertyDescriptor());
                    properties.Add(new XElementElementPropertyDescriptor());
                    properties.Add(new XElementElementsPropertyDescriptor());
                    properties.Add(new XElementValuePropertyDescriptor());
                    properties.Add(new XElementXmlPropertyDescriptor());
                }
                else if (typeof(T) == typeof(XAttribute))
                {
                    properties.Add(new XAttributeValuePropertyDescriptor());
                }
            }
            foreach (PropertyDescriptor property in base.GetProperties(attributes))
            {
                properties.Add(property);
            }
            return properties;
        }
    }

    internal abstract class XPropertyDescriptor<T, TProperty> : PropertyDescriptor where T : XObject
    {
        public XPropertyDescriptor(string name) : base(name, null)
        {
        }

        public override Type ComponentType
        {
            get { return typeof(T); }
        }

        public override bool IsReadOnly
        {
            get { return true; }
        }

        public override Type PropertyType
        {
            get { return typeof(TProperty); }
        }

        public override bool SupportsChangeEvents
        {
            get { return true; }
        }

        public override void AddValueChanged(object component, EventHandler handler)
        {
            bool hasValueChangedHandler = GetValueChangedHandler(component) != null;
            base.AddValueChanged(component, handler);
            if (hasValueChangedHandler)
                return;
            T? c = component as T;
            if (c != null && GetValueChangedHandler(component) != null)
            {
                c.Changing += new EventHandler<XObjectChangeEventArgs>(OnChanging);
                c.Changed += new EventHandler<XObjectChangeEventArgs>(OnChanged);
            }
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override void RemoveValueChanged(object component, EventHandler handler)
        {
            base.RemoveValueChanged(component, handler);
            T? c = component as T;
            if (c != null && GetValueChangedHandler(component) == null)
            {
                c.Changing -= new EventHandler<XObjectChangeEventArgs>(OnChanging);
                c.Changed -= new EventHandler<XObjectChangeEventArgs>(OnChanged);
            }
        }

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object? component, object? value)
        {
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        protected virtual void OnChanged(object? sender, XObjectChangeEventArgs args)
        {
        }

        protected virtual void OnChanging(object? sender, XObjectChangeEventArgs args)
        {
        }
    }

    internal sealed class XElementAttributePropertyDescriptor : XPropertyDescriptor<XElement, object>
    {
        private XDeferredSingleton<XAttribute>? _value;
        private XAttribute? _changeState;

        public XElementAttributePropertyDescriptor() : base("Attribute")
        {
        }

        public override object GetValue(object? component)
        {
            return _value = new XDeferredSingleton<XAttribute>((e, n) => e.Attribute(n), (component as XElement)!, null);
        }

        protected override void OnChanged(object? sender, XObjectChangeEventArgs args)
        {
            if (_value == null)
                return;
            switch (args.ObjectChange)
            {
                case XObjectChange.Add:
                    XAttribute? a = sender as XAttribute;
                    if (a != null && _value.element == a.Parent && _value.name == a.Name)
                    {
                        OnValueChanged(_value.element, EventArgs.Empty);
                    }
                    break;
                case XObjectChange.Remove:
                    a = sender as XAttribute;
                    if (a != null && _changeState == a)
                    {
                        _changeState = null;
                        OnValueChanged(_value.element, EventArgs.Empty);
                    }
                    break;
            }
        }

        protected override void OnChanging(object? sender, XObjectChangeEventArgs args)
        {
            if (_value == null)
                return;
            switch (args.ObjectChange)
            {
                case XObjectChange.Remove:
                    XAttribute? a = sender as XAttribute;
                    _changeState = a != null && _value.element == a.Parent && _value.name == a.Name ? a : null;
                    break;
            }
        }
    }

    internal sealed class XElementDescendantsPropertyDescriptor : XPropertyDescriptor<XElement, IEnumerable<XElement>>
    {
        private XDeferredAxis<XElement>? _value;
        private XName? _changeState;

        public XElementDescendantsPropertyDescriptor() : base("Descendants")
        {
        }

        public override object GetValue(object? component)
        {
            return _value = new XDeferredAxis<XElement>((e, n) => n != null ? e.Descendants(n) : e.Descendants(), (component as XElement)!, null);
        }

        protected override void OnChanged(object? sender, XObjectChangeEventArgs args)
        {
            if (_value == null)
                return;
            switch (args.ObjectChange)
            {
                case XObjectChange.Add:
                case XObjectChange.Remove:
                    XElement? e = sender as XElement;
                    if (e != null && (_value.name == e.Name || _value.name == null))
                    {
                        OnValueChanged(_value.element, EventArgs.Empty);
                    }
                    break;
                case XObjectChange.Name:
                    e = sender as XElement;
                    if (e != null && _value.element != e && _value.name != null && (_value.name == e.Name || _value.name == _changeState))
                    {
                        _changeState = null;
                        OnValueChanged(_value.element, EventArgs.Empty);
                    }
                    break;
            }
        }

        protected override void OnChanging(object? sender, XObjectChangeEventArgs args)
        {
            if (_value == null)
                return;
            switch (args.ObjectChange)
            {
                case XObjectChange.Name:
                    XElement? e = sender as XElement;
                    _changeState = e != null ? e.Name : null;
                    break;
            }
        }
    }

    internal sealed class XElementElementPropertyDescriptor : XPropertyDescriptor<XElement, object>
    {
        private XDeferredSingleton<XElement>? _value;
        private XElement? _changeState;

        public XElementElementPropertyDescriptor() : base("Element")
        {
        }

        public override object GetValue(object? component)
        {
            return _value = new XDeferredSingleton<XElement>((e, n) => e.Element(n), (component as XElement)!, null);
        }

        protected override void OnChanged(object? sender, XObjectChangeEventArgs args)
        {
            if (_value == null)
                return;
            switch (args.ObjectChange)
            {
                case XObjectChange.Add:
                    XElement? e = sender as XElement;
                    if (e != null && _value.element == e.Parent && _value.name == e.Name && _value.element.Element(_value.name) == e)
                    {
                        OnValueChanged(_value.element, EventArgs.Empty);
                    }
                    break;
                case XObjectChange.Remove:
                    e = sender as XElement;
                    if (e != null && _changeState == e)
                    {
                        _changeState = null;
                        OnValueChanged(_value.element, EventArgs.Empty);
                    }
                    break;
                case XObjectChange.Name:
                    e = sender as XElement;
                    if (e != null)
                    {
                        if (_value.element == e.Parent && _value.name == e.Name && _value.element.Element(_value.name) == e)
                        {
                            OnValueChanged(_value.element, EventArgs.Empty);
                        }
                        else if (_changeState == e)
                        {
                            _changeState = null;
                            OnValueChanged(_value.element, EventArgs.Empty);
                        }
                    }
                    break;
            }
        }

        protected override void OnChanging(object? sender, XObjectChangeEventArgs args)
        {
            if (_value == null)
                return;
            switch (args.ObjectChange)
            {
                case XObjectChange.Remove:
                case XObjectChange.Name:
                    XElement? e = sender as XElement;
                    _changeState = e != null && _value.element == e.Parent && _value.name == e.Name && _value.element.Element(_value.name) == e ? e : null;
                    break;
            }
        }
    }

    internal sealed class XElementElementsPropertyDescriptor : XPropertyDescriptor<XElement, IEnumerable<XElement>>
    {
        private XDeferredAxis<XElement>? _value;
        private object? _changeState;

        public XElementElementsPropertyDescriptor() : base("Elements")
        {
        }

        public override object GetValue(object? component)
        {
            return _value = new XDeferredAxis<XElement>((e, n) => n != null ? e.Elements(n) : e.Elements(), (component as XElement)!, null);
        }

        protected override void OnChanged(object? sender, XObjectChangeEventArgs args)
        {
            if (_value == null)
                return;
            switch (args.ObjectChange)
            {
                case XObjectChange.Add:
                    XElement? e = sender as XElement;
                    if (e != null && _value.element == e.Parent && (_value.name == e.Name || _value.name == null))
                    {
                        OnValueChanged(_value.element, EventArgs.Empty);
                    }
                    break;
                case XObjectChange.Remove:
                    e = sender as XElement;
                    if (e != null && _value.element == (_changeState as XContainer) && (_value.name == e.Name || _value.name == null))
                    {
                        _changeState = null;
                        OnValueChanged(_value.element, EventArgs.Empty);
                    }
                    break;
                case XObjectChange.Name:
                    e = sender as XElement;
                    if (e != null && _value.element == e.Parent && _value.name != null && (_value.name == e.Name || _value.name == (_changeState as XName)))
                    {
                        _changeState = null;
                        OnValueChanged(_value.element, EventArgs.Empty);
                    }
                    break;
            }
        }

        protected override void OnChanging(object? sender, XObjectChangeEventArgs args)
        {
            if (_value == null)
                return;
            switch (args.ObjectChange)
            {
                case XObjectChange.Remove:
                    XElement? e = sender as XElement;
                    _changeState = e != null ? e.Parent : null;
                    break;
                case XObjectChange.Name:
                    e = sender as XElement;
                    _changeState = e != null ? e.Name : null;
                    break;
            }
        }
    }

    internal sealed class XElementValuePropertyDescriptor : XPropertyDescriptor<XElement, string>
    {
        private XElement? _element;

        public XElementValuePropertyDescriptor() : base("Value")
        {
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override object GetValue(object? component)
        {
            _element = component as XElement;
            if (_element == null)
                return string.Empty;
            return _element.Value;
        }

        public override void SetValue(object? component, object? value)
        {
            _element = component as XElement;
            if (_element == null)
                return;
            _element.Value = (value as string)!;
        }

        protected override void OnChanged(object? sender, XObjectChangeEventArgs args)
        {
            if (_element == null)
                return;
            switch (args.ObjectChange)
            {
                case XObjectChange.Add:
                case XObjectChange.Remove:
                    if (sender is XElement || sender is XText)
                    {
                        OnValueChanged(_element, EventArgs.Empty);
                    }
                    break;
                case XObjectChange.Value:
                    if (sender is XText)
                    {
                        OnValueChanged(_element, EventArgs.Empty);
                    }
                    break;
            }
        }
    }

    internal sealed class XElementXmlPropertyDescriptor : XPropertyDescriptor<XElement, string>
    {
        private XElement? _element;

        public XElementXmlPropertyDescriptor() : base("Xml")
        {
        }

        public override object GetValue(object? component)
        {
            _element = component as XElement;
            if (_element == null)
                return string.Empty;
            return _element.ToString(SaveOptions.DisableFormatting);
        }

        protected override void OnChanged(object? sender, XObjectChangeEventArgs args)
        {
            if (_element == null)
                return;
            OnValueChanged(_element, EventArgs.Empty);
        }
    }

    internal sealed class XAttributeValuePropertyDescriptor : XPropertyDescriptor<XAttribute, string>
    {
        private XAttribute? _attribute;

        public XAttributeValuePropertyDescriptor() : base("Value")
        {
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override object GetValue(object? component)
        {
            _attribute = component as XAttribute;
            if (_attribute == null)
                return string.Empty;
            return _attribute.Value;
        }

        public override void SetValue(object? component, object? value)
        {
            _attribute = component as XAttribute;
            if (_attribute == null)
                return;
            _attribute.Value = (value as string)!;
        }

        protected override void OnChanged(object? sender, XObjectChangeEventArgs args)
        {
            if (_attribute == null)
                return;
            if (args.ObjectChange == XObjectChange.Value)
            {
                OnValueChanged(_attribute, EventArgs.Empty);
            }
        }
    }

    internal sealed class XDeferredAxis<T> : IEnumerable<T>, IEnumerable where T : XObject
    {
        private readonly Func<XElement, XName?, IEnumerable<T>> _func;
        internal XElement element;
        internal XName? name;

        public XDeferredAxis(Func<XElement, XName?, IEnumerable<T>> func, XElement element, XName? name)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            _func = func;
            this.element = element;
            this.name = name;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _func(element, name).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<T> this[string expandedName]
        {
            get
            {
                if (expandedName == null)
                    throw new ArgumentNullException(nameof(expandedName));
                if (name == null)
                {
                    name = expandedName;
                }
                else if (name != expandedName)
                {
                    return Enumerable.Empty<T>();
                }
                return this;
            }
        }
    }

    internal sealed class XDeferredSingleton<T> where T : XObject
    {
        private readonly Func<XElement, XName, T?> _func;
        internal XElement element;
        internal XName? name;

        public XDeferredSingleton(Func<XElement, XName, T?> func, XElement element, XName? name)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            _func = func;
            this.element = element;
            this.name = name;
        }

        public T? this[string expandedName]
        {
            get
            {
                if (expandedName == null)
                    throw new ArgumentNullException(nameof(expandedName));
                if (name == null)
                {
                    name = expandedName;
                }
                else if (name != expandedName)
                {
                    return null;
                }
                return _func(element, name);
            }
        }
    }
}
