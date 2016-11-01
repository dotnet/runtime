// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///     Provides access to get and set option values for a designer.
    /// </summary>
    public abstract class DesignerOptionService : IDesignerOptionService
    {
        private DesignerOptionCollection _options;

        /// <summary>
        ///     Returns the options collection for this service.  There is 
        ///     always a global options collection that contains child collections.
        /// </summary>
        public DesignerOptionCollection Options
        {
            get
            {
                if (_options == null)
                {
                    _options = new DesignerOptionCollection(this, null, string.Empty, null);
                }
                return _options;
            }
        }

        /// <summary>
        ///     Creates a new DesignerOptionCollection with the given name, and adds it to 
        ///     the given parent.  The "value" parameter specifies an object whose public 
        ///     properties will be used in the Propeties collection of the option collection.  
        ///     The value parameter can be null if this options collection does not offer 
        ///     any properties.  Properties will be wrapped in such a way that passing 
        ///     anything into the component parameter of the property descriptor will be 
        ///     ignored and the value object will be substituted.
        /// </summary>
        protected DesignerOptionCollection CreateOptionCollection(DesignerOptionCollection parent, string name, object value)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.InvalidArgument, name.Length.ToString(CultureInfo.CurrentCulture), (0).ToString(CultureInfo.CurrentCulture)), "name.Length");
            }

            return new DesignerOptionCollection(this, parent, name, value);
        }

        /// <summary>
        ///     Retrieves the property descriptor for the given page / value name.  Returns
        ///     null if the property couldn't be found.
        /// </summary>
        private PropertyDescriptor GetOptionProperty(string pageName, string valueName)
        {
            if (pageName == null)
            {
                throw new ArgumentNullException(nameof(pageName));
            }

            if (valueName == null)
            {
                throw new ArgumentNullException(nameof(valueName));
            }

            string[] optionNames = pageName.Split(new char[] { '\\' });

            DesignerOptionCollection options = Options;
            foreach (string optionName in optionNames)
            {
                options = options[optionName];
                if (options == null)
                {
                    return null;
                }
            }

            return options.Properties[valueName];
        }

        /// <summary>
        ///     This method is called on demand the first time a user asks for child 
        ///     options or properties of an options collection. 
        /// </summary>
        protected virtual void PopulateOptionCollection(DesignerOptionCollection options)
        {
        }

        /// <summary>
        ///     This method must be implemented to show the options dialog UI for the given object.  
        /// </summary>
        protected virtual bool ShowDialog(DesignerOptionCollection options, object optionObject)
        {
            return false;
        }


        /// <internalonly/>
        /// <summary>
        /// Gets the value of an option defined in this package.
        /// </summary>
        object IDesignerOptionService.GetOptionValue(string pageName, string valueName)
        {
            PropertyDescriptor optionProp = GetOptionProperty(pageName, valueName);
            if (optionProp != null)
            {
                return optionProp.GetValue(null);
            }
            return null;
        }

        /// <internalonly/>
        /// <summary>
        /// Sets the value of an option defined in this package.
        /// </summary>
        void IDesignerOptionService.SetOptionValue(string pageName, string valueName, object value)
        {
            PropertyDescriptor optionProp = GetOptionProperty(pageName, valueName);
            if (optionProp != null)
            {
                optionProp.SetValue(null, value);
            }
        }

        /// <summary>
        ///     The DesignerOptionCollection class is a collection that contains 
        ///     other DesignerOptionCollection objects.  This forms a tree of options, 
        ///     with each branch of the tree having a name and a possible collection of 
        ///     properties.  Each parent branch of the tree contains a union of the 
        ///     properties if all the branch's children.  
        /// </summary>
        [TypeConverter(typeof(DesignerOptionConverter))]
        public sealed class DesignerOptionCollection : IList
        {
            private DesignerOptionService _service;
            private DesignerOptionCollection _parent;
            private string _name;
            private object _value;
            private ArrayList _children;
            private PropertyDescriptorCollection _properties;

            /// <summary>
            ///     Creates a new DesignerOptionCollection.
            /// </summary>
            internal DesignerOptionCollection(DesignerOptionService service, DesignerOptionCollection parent, string name, object value)
            {
                _service = service;
                _parent = parent;
                _name = name;
                _value = value;

                if (_parent != null)
                {
                    if (_parent._children == null)
                    {
                        _parent._children = new ArrayList(1);
                    }
                    _parent._children.Add(this);
                }
            }

            /// <summary>
            ///     The count of child options collections this collection contains.
            /// </summary>
            public int Count
            {
                get
                {
                    EnsurePopulated();
                    return _children.Count;
                }
            }

            /// <summary>
            ///     The name of this collection.  Names are programmatic names and are not 
            ///     localized.  A name search is case insensitive.
            /// </summary>
            public string Name
            {
                get
                {
                    return _name;
                }
            }

            /// <summary>
            ///     Returns the parent collection object, or null if there is no parent.
            /// </summary>
            public DesignerOptionCollection Parent
            {
                get
                {
                    return _parent;
                }
            }

            /// <summary>
            ///     The collection of properties that this OptionCollection, along with all of 
            ///     its children, offers.  PropertyDescriptors are taken directly from the 
            ///     value passed to CreateObjectCollection and wrapped in an additional property 
            ///     descriptor that hides the value object from the user.  This means that any 
            ///     value may be passed into the "component" parameter of the various 
            ///     PropertyDescriptor methods.  The value is ignored and is replaced with 
            ///     the correct value internally.
            /// </summary>
            public PropertyDescriptorCollection Properties
            {
                get
                {
                    if (_properties == null)
                    {
                        ArrayList propList;

                        if (_value != null)
                        {
                            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(_value);
                            propList = new ArrayList(props.Count);
                            foreach (PropertyDescriptor prop in props)
                            {
                                propList.Add(new WrappedPropertyDescriptor(prop, _value));
                            }
                        }
                        else
                        {
                            propList = new ArrayList(1);
                        }

                        EnsurePopulated();
                        foreach (DesignerOptionCollection child in _children)
                        {
                            propList.AddRange(child.Properties);
                        }

                        PropertyDescriptor[] propArray = (PropertyDescriptor[])propList.ToArray(typeof(PropertyDescriptor));
                        _properties = new PropertyDescriptorCollection(propArray, true);
                    }

                    return _properties;
                }
            }

            /// <summary>
            ///     Retrieves the child collection at the given index.
            /// </summary>
            public DesignerOptionCollection this[int index]
            {
                [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
                get
                {
                    EnsurePopulated();
                    if (index < 0 || index >= _children.Count)
                    {
                        throw new IndexOutOfRangeException(nameof(index));
                    }
                    return (DesignerOptionCollection)_children[index];
                }
            }

            /// <summary>
            ///     Retrieves the child collection at the given name.  The name search is case 
            ///     insensitive.
            /// </summary>
            public DesignerOptionCollection this[string name]
            {
                get
                {
                    EnsurePopulated();
                    foreach (DesignerOptionCollection child in _children)
                    {
                        if (string.Compare(child.Name, name, true, CultureInfo.InvariantCulture) == 0)
                        {
                            return child;
                        }
                    }
                    return null;
                }
            }

            /// <summary>
            ///     Copies this collection to an array.
            /// </summary>
            public void CopyTo(Array array, int index)
            {
                EnsurePopulated();
                _children.CopyTo(array, index);
            }

            /// <summary>
            ///     Called before any access to our collection to force it to become populated.
            /// </summary>
            private void EnsurePopulated()
            {
                if (_children == null)
                {
                    _service.PopulateOptionCollection(this);
                    if (_children == null)
                    {
                        _children = new ArrayList(1);
                    }
                }
            }

            /// <summary>
            ///     Returns an enumerator that can be used to iterate this collection.
            /// </summary>
            public IEnumerator GetEnumerator()
            {
                EnsurePopulated();
                return _children.GetEnumerator();
            }

            /// <summary>
            ///     Returns the numerical index of the given value.
            /// </summary>
            public int IndexOf(DesignerOptionCollection value)
            {
                EnsurePopulated();
                return _children.IndexOf(value);
            }

            /// <summary>
            ///     Locates the value object to use for getting or setting a property.
            /// </summary>
            private static object RecurseFindValue(DesignerOptionCollection options)
            {
                if (options._value != null)
                {
                    return options._value;
                }

                foreach (DesignerOptionCollection child in options)
                {
                    object value = RecurseFindValue(child);
                    if (value != null)
                    {
                        return value;
                    }
                }

                return null;
            }

            /// <summary>
            ///     Displays a dialog-based user interface that allows the user to 
            ///     configure the various options.  
            /// </summary>
            public bool ShowDialog()
            {
                object value = RecurseFindValue(this);

                if (value == null)
                {
                    return false;
                }

                return _service.ShowDialog(this, value);
            }

            /// <internalonly/>
            /// <summary>
            /// Private ICollection implementation.
            /// </summary>
            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            /// <internalonly/>
            /// <summary>
            /// Private ICollection implementation.
            /// </summary>
            object ICollection.SyncRoot
            {
                get
                {
                    return this;
                }
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            bool IList.IsFixedSize
            {
                get
                {
                    return true;
                }
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            bool IList.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            object IList.this[int index]
            {
                get
                {
                    return this[index];
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            int IList.Add(object value)
            {
                throw new NotSupportedException();
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            void IList.Clear()
            {
                throw new NotSupportedException();
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            bool IList.Contains(object value)
            {
                EnsurePopulated();
                return _children.Contains(value);
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            int IList.IndexOf(object value)
            {
                EnsurePopulated();
                return _children.IndexOf(value);
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            void IList.Insert(int index, object value)
            {
                throw new NotSupportedException();
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            void IList.Remove(object value)
            {
                throw new NotSupportedException();
            }

            /// <internalonly/>
            /// <summary>
            /// Private IList implementation.
            /// </summary>
            void IList.RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            ///     A special property descriptor that forwards onto a base
            ///     property descriptor but allows any value to be used for the
            ///     "component" parameter.
            /// </summary>
            private sealed class WrappedPropertyDescriptor : PropertyDescriptor
            {
                private object _target;
                private PropertyDescriptor _property;

                internal WrappedPropertyDescriptor(PropertyDescriptor property, object target) : base(property.Name, null)
                {
                    _property = property;
                    _target = target;
                }

                public override AttributeCollection Attributes
                {
                    get
                    {
                        return _property.Attributes;
                    }
                }

                public override Type ComponentType
                {
                    get
                    {
                        return _property.ComponentType;
                    }
                }

                public override bool IsReadOnly
                {
                    get
                    {
                        return _property.IsReadOnly;
                    }
                }

                public override Type PropertyType
                {
                    get
                    {
                        return _property.PropertyType;
                    }
                }

                public override bool CanResetValue(object component)
                {
                    return _property.CanResetValue(_target);
                }

                public override object GetValue(object component)
                {
                    return _property.GetValue(_target);
                }

                public override void ResetValue(object component)
                {
                    _property.ResetValue(_target);
                }

                public override void SetValue(object component, object value)
                {
                    _property.SetValue(_target, value);
                }

                public override bool ShouldSerializeValue(object component)
                {
                    return _property.ShouldSerializeValue(_target);
                }
            }
        }

        /// <summary>
        ///     The type converter for the designer option collection.
        /// </summary>
        internal sealed class DesignerOptionConverter : TypeConverter
        {
            public override bool GetPropertiesSupported(ITypeDescriptorContext cxt)
            {
                return true;
            }

            public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext cxt, object value, Attribute[] attributes)
            {
                PropertyDescriptorCollection props = new PropertyDescriptorCollection(null);
                DesignerOptionCollection options = value as DesignerOptionCollection;
                if (options == null)
                {
                    return props;
                }

                foreach (DesignerOptionCollection option in options)
                {
                    props.Add(new OptionPropertyDescriptor(option));
                }

                foreach (PropertyDescriptor p in options.Properties)
                {
                    props.Add(p);
                }
                return props;
            }

            public override object ConvertTo(ITypeDescriptorContext cxt, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                {
                    return SR.CollectionConverterText;
                }
                return base.ConvertTo(cxt, culture, value, destinationType);
            }

            private class OptionPropertyDescriptor : PropertyDescriptor
            {
                private DesignerOptionCollection _option;

                internal OptionPropertyDescriptor(DesignerOptionCollection option) : base(option.Name, null)
                {
                    _option = option;
                }

                public override Type ComponentType
                {
                    get
                    {
                        return _option.GetType();
                    }
                }

                public override bool IsReadOnly
                {
                    get
                    {
                        return true;
                    }
                }

                public override Type PropertyType
                {
                    get
                    {
                        return _option.GetType();
                    }
                }

                public override bool CanResetValue(object component)
                {
                    return false;
                }

                public override object GetValue(object component)
                {
                    return _option;
                }

                public override void ResetValue(object component)
                {
                }

                public override void SetValue(object component, object value)
                {
                }

                public override bool ShouldSerializeValue(object component)
                {
                    return false;
                }
            }
        }
    }
}

