// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a description of a property.
    /// </summary>
    public abstract class PropertyDescriptor : MemberDescriptor
    {
        internal const string PropertyDescriptorPropertyTypeMessage = "PropertyDescriptor's PropertyType cannot be statically discovered.";

        private TypeConverter? _converter;
        private Dictionary<object, EventHandler?>? _valueChangedHandlers;
        private object?[]? _editors;
        private Type[]? _editorTypes;
        private int _editorCount;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.PropertyDescriptor'/> class with the specified name and
        /// attributes.
        /// </summary>
        protected PropertyDescriptor(string name, Attribute[]? attrs) : base(name, attrs)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.PropertyDescriptor'/> class with
        /// the name and attributes in the specified <see cref='System.ComponentModel.MemberDescriptor'/>.
        /// </summary>
        protected PropertyDescriptor(MemberDescriptor descr) : base(descr)
        {
        }

        /// <summary>
        ///
        /// Initializes a new instance of the <see cref='System.ComponentModel.PropertyDescriptor'/> class with
        /// the name in the specified <see cref='System.ComponentModel.MemberDescriptor'/> and the
        /// attributes in both the <see cref='System.ComponentModel.MemberDescriptor'/> and the
        /// <see cref='System.Attribute'/> array.
        ///
        /// </summary>
        protected PropertyDescriptor(MemberDescriptor descr, Attribute[]? attrs) : base(descr, attrs)
        {
        }

        /// <summary>
        /// When overridden in a derived class, gets the type of the
        /// component this property is bound to.
        /// </summary>
        public abstract Type ComponentType { get; }

        /// <summary>
        /// Gets the type converter for this property.
        /// </summary>
        public virtual TypeConverter Converter
        {
            [RequiresUnreferencedCode(PropertyDescriptorPropertyTypeMessage)]
            get
            {
                // Always grab the attribute collection first here, because if the metadata version
                // changes it will invalidate our type converter cache.
                AttributeCollection attrs = Attributes;

                if (_converter == null)
                {
                    TypeConverterAttribute attr = (TypeConverterAttribute)attrs[typeof(TypeConverterAttribute)]!;
                    if (attr.ConverterTypeName != null && attr.ConverterTypeName.Length > 0)
                    {
                        Type? converterType = GetTypeFromName(attr.ConverterTypeName);
                        if (converterType != null && typeof(TypeConverter).IsAssignableFrom(converterType))
                        {
                            _converter = (TypeConverter)CreateInstance(converterType)!;
                        }
                    }

                    _converter ??= TypeDescriptor.GetConverter(PropertyType);
                }
                return _converter;
            }
        }

        /// <summary>
        /// Gets a value
        /// indicating whether this property should be localized, as
        /// specified in the <see cref='System.ComponentModel.LocalizableAttribute'/>.
        /// </summary>
        public virtual bool IsLocalizable => (LocalizableAttribute.Yes.Equals(Attributes[typeof(LocalizableAttribute)]));

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether this
        /// property is read-only.
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Gets a value indicating whether this property should be serialized as specified
        /// in the <see cref='System.ComponentModel.DesignerSerializationVisibilityAttribute'/>.
        /// </summary>
        public DesignerSerializationVisibility SerializationVisibility
        {
            get
            {
                DesignerSerializationVisibilityAttribute attr = (DesignerSerializationVisibilityAttribute)Attributes[typeof(DesignerSerializationVisibilityAttribute)]!;
                return attr.Visibility;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets the type of the property.
        /// </summary>
        public abstract Type PropertyType { get; }

        /// <summary>
        /// Allows interested objects to be notified when this property changes.
        /// </summary>
        public virtual void AddValueChanged(object component, EventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(component);
            ArgumentNullException.ThrowIfNull(handler);

            _valueChangedHandlers ??= new Dictionary<object, EventHandler?>();

            EventHandler? h = _valueChangedHandlers.GetValueOrDefault(component, defaultValue: null);
            _valueChangedHandlers[component] = (EventHandler?)Delegate.Combine(h, handler);
        }

        /// <summary>
        /// When overridden in a derived class, indicates whether
        /// resetting the <paramref name="component "/>will change the value of the
        /// <paramref name="component"/>.
        /// </summary>
        public abstract bool CanResetValue(object component);

        /// <summary>
        /// Compares this to another <see cref='System.ComponentModel.PropertyDescriptor'/>
        /// to see if they are equivalent.
        /// NOTE: If you make a change here, you likely need to change GetHashCode() as well.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            try
            {
                if (obj == this)
                {
                    return true;
                }

                if (obj == null)
                {
                    return false;
                }

                // Assume that 90% of the time we will only do a .Equals(...) for
                // propertydescriptor vs. propertydescriptor... avoid the overhead
                // of an instanceof call.

                if (obj is PropertyDescriptor pd && pd.NameHashCode == NameHashCode
                    && pd.PropertyType == PropertyType
                    && pd.Name.Equals(Name))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        protected object? CreateInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            Type[] typeArgs = new Type[] { typeof(Type) };
            ConstructorInfo? ctor = type.GetConstructor(typeArgs);
            if (ctor != null)
            {
                return TypeDescriptor.CreateInstance(null, type, typeArgs, new object[] { PropertyType });
            }

            return TypeDescriptor.CreateInstance(null, type, null, null);
        }

        /// <summary>
        /// In an inheriting class, adds the attributes of the inheriting class to the
        /// specified list of attributes in the parent class. For duplicate attributes,
        /// the last one added to the list will be kept.
        /// </summary>
        protected override void FillAttributes(IList attributeList)
        {
            // Each time we fill our attributes, we should clear our cached
            // stuff.
            _converter = null;
            _editors = null;
            _editorTypes = null;
            _editorCount = 0;

            base.FillAttributes(attributeList);
        }

        [RequiresUnreferencedCode(PropertyDescriptorPropertyTypeMessage)]
        public PropertyDescriptorCollection GetChildProperties() => GetChildProperties(null, null);

        [RequiresUnreferencedCode(PropertyDescriptorPropertyTypeMessage + " " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public PropertyDescriptorCollection GetChildProperties(Attribute[] filter) => GetChildProperties(null, filter);

        [RequiresUnreferencedCode(PropertyDescriptorPropertyTypeMessage + " The Type of instance cannot be statically discovered.")]
        public PropertyDescriptorCollection GetChildProperties(object instance) => GetChildProperties(instance, null);

        /// <summary>
        /// Retrieves the properties
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptorPropertyTypeMessage + " The Type of instance cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public virtual PropertyDescriptorCollection GetChildProperties(object? instance, Attribute[]? filter)
        {
            if (instance == null)
            {
                return TypeDescriptor.GetProperties(PropertyType, filter);
            }
            else
            {
                return TypeDescriptor.GetProperties(instance, filter);
            }
        }

        /// <summary>
        /// Gets an editor of the specified type.
        /// </summary>
        [RequiresUnreferencedCode(TypeDescriptor.DesignTimeAttributeTrimmed + " " + PropertyDescriptorPropertyTypeMessage)]
        public virtual object? GetEditor(Type editorBaseType)
        {
            object? editor = null;

            // Always grab the attribute collection first here, because if the metadata version
            // changes it will invalidate our editor cache.
            AttributeCollection attrs = Attributes;

            // Check the editors we've already created for this type.
            if (_editorTypes != null)
            {
                for (int i = 0; i < _editorCount; i++)
                {
                    if (_editorTypes[i] == editorBaseType)
                    {
                        return _editors![i];
                    }
                }
            }

            // If one wasn't found, then we must go through the attributes.
            if (editor == null)
            {
                for (int i = 0; i < attrs.Count; i++)
                {
                    if (!(attrs[i] is EditorAttribute attr))
                    {
                        continue;
                    }

                    Type? editorType = GetTypeFromName(attr.EditorBaseTypeName);

                    if (editorBaseType == editorType)
                    {
                        Type? type = GetTypeFromName(attr.EditorTypeName);
                        if (type != null)
                        {
                            editor = CreateInstance(type);
                            break;
                        }
                    }
                }

                // Now, if we failed to find it in our own attributes, go to the
                // component descriptor.
                editor ??= TypeDescriptor.GetEditor(PropertyType, editorBaseType);

                // Now, another slot in our editor cache for next time
                if (_editorTypes == null)
                {
                    _editorTypes = new Type[5];
                    _editors = new object[5];
                }

                if (_editorCount >= _editorTypes.Length)
                {
                    Type[] newTypes = new Type[_editorTypes.Length * 2];
                    object[] newEditors = new object[_editors!.Length * 2];
                    Array.Copy(_editorTypes, newTypes, _editorTypes.Length);
                    Array.Copy(_editors, newEditors, _editors.Length);
                    _editorTypes = newTypes;
                    _editors = newEditors;
                }

                _editorTypes[_editorCount] = editorBaseType;
                _editors![_editorCount++] = editor;
            }

            return editor;
        }

        /// <summary>
        /// Try to keep this reasonable in [....] with Equals(). Specifically,
        /// if A.Equals(B) returns true, A &amp; B should have the same hash code.
        /// </summary>
        public override int GetHashCode() => NameHashCode ^ PropertyType.GetHashCode();

        /// <summary>
        /// This method returns the object that should be used during invocation of members.
        /// Normally the return value will be the same as the instance passed in. If
        /// someone associated another object with this instance, or if the instance is a
        /// custom type descriptor, GetInvocationTarget may return a different value.
        /// </summary>
        protected override object? GetInvocationTarget(Type type, object instance)
        {
            object? target = base.GetInvocationTarget(type, instance);
            if (target is ICustomTypeDescriptor td)
            {
                target = td.GetPropertyOwner(this);
            }

            return target;
        }

        /// <summary>
        /// Gets a type using its name.
        /// </summary>
        [RequiresUnreferencedCode("Calls ComponentType.Assembly.GetType on the non-fully qualified typeName, which the trimmer cannot recognize.")]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        protected Type? GetTypeFromName(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] string? typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            //  try the generic method.
            Type? typeFromGetType = Type.GetType(typeName);

            // If we didn't get a type from the generic method, or if the assembly we found the type
            // in is the same as our Component's assembly, use or Component's assembly instead. This is
            // because the CLR may have cached an older version if the assembly's version number didn't change
            Type? typeFromComponent = null;
            if (ComponentType != null)
            {
                if ((typeFromGetType == null) ||
                    (ComponentType.Assembly.FullName!.Equals(typeFromGetType.Assembly.FullName)))
                {
                    int comma = typeName.IndexOf(',');

                    if (comma != -1)
                        typeName = typeName.Substring(0, comma);

                    typeFromComponent = ComponentType.Assembly.GetType(typeName);
                }
            }

            return typeFromComponent ?? typeFromGetType;
        }

        /// <summary>
        /// When overridden in a derived class, gets the current value of the property on a component.
        /// </summary>
        public abstract object? GetValue(object? component);

        /// <summary>
        /// This should be called by your property descriptor implementation
        /// when the property value has changed.
        /// </summary>
        protected virtual void OnValueChanged(object? component, EventArgs e)
        {
            if (component != null)
            {
                _valueChangedHandlers?.GetValueOrDefault(component, defaultValue: null)?.Invoke(component, e);
            }
        }

        /// <summary>
        /// Allows interested objects to be notified when this property changes.
        /// </summary>
        public virtual void RemoveValueChanged(object component, EventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(component);
            ArgumentNullException.ThrowIfNull(handler);

            if (_valueChangedHandlers != null)
            {
                EventHandler? h = _valueChangedHandlers.GetValueOrDefault(component, defaultValue: null);
                h = (EventHandler?)Delegate.Remove(h, handler);
                if (h != null)
                {
                    _valueChangedHandlers[component] = h;
                }
                else
                {
                    _valueChangedHandlers.Remove(component);
                }
            }
        }

        /// <summary>
        /// Return current set of ValueChanged event handlers for a specific
        /// component, in the form of a combined multicast event handler.
        /// Returns null if no event handlers currently assigned to component.
        /// </summary>
        protected internal EventHandler? GetValueChangedHandler(object component)
        {
            if (component != null && _valueChangedHandlers != null)
            {
                return _valueChangedHandlers.GetValueOrDefault(component, defaultValue: null);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// When overridden in a derived class, resets the value for this property of the component.
        /// </summary>
        public abstract void ResetValue(object component);

        /// <summary>
        /// When overridden in a derived class, sets the value of
        /// the component to a different value.
        /// </summary>
        public abstract void SetValue(object? component, object? value);

        /// <summary>
        /// When overridden in a derived class, indicates whether the
        /// value of this property needs to be persisted.
        /// </summary>
        public abstract bool ShouldSerializeValue(object component);

        /// <summary>
        /// Indicates whether value change notifications for this property may originate from outside the property
        /// descriptor, such as from the component itself (value=true), or whether notifications will only originate
        /// from direct calls made to PropertyDescriptor.SetValue (value=false). For example, the component may
        /// implement the INotifyPropertyChanged interface, or may have an explicit '{name}Changed' event for this property.
        /// </summary>
        public virtual bool SupportsChangeEvents => false;
    }
}
