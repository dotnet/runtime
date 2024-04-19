// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    public abstract class CustomTypeDescriptor : ICustomTypeDescriptor
    {
        private readonly ICustomTypeDescriptor? _parent;

        /// <summary>
        /// Creates a new CustomTypeDescriptor object. There are two versions
        /// of this constructor. The version that takes no parameters simply
        /// calls the version that takes a parent and passes null as the
        /// parent value. If the parent is null, CustomTypeDescriptor returns
        /// the defined default values for each method. If the parent is
        /// non-null, CustomTypeDescriptor calls the parent's version of
        /// the method.
        /// </summary>
        protected CustomTypeDescriptor()
        {
        }

        /// <summary>
        /// Creates a new CustomTypeDescriptor object. There are two versions
        /// of this constructor. The version that takes no parameters simply
        /// calls the version that takes a parent and passes null as the
        /// parent value. If the parent is null, CustomTypeDescriptor returns
        /// the defined default values for each method. If the parent is
        /// non-null, CustomTypeDescriptor calls the parent's version of
        /// the method.
        /// </summary>
        protected CustomTypeDescriptor(ICustomTypeDescriptor? parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// The GetAttributes method returns the type-level attributes for
        /// the type this custom type descriptor is providing information for.
        /// You must always return a valid collection from this method.
        /// </summary>
        public virtual AttributeCollection GetAttributes()
        {
            if (_parent != null)
            {
                return _parent.GetAttributes();
            }

            return AttributeCollection.Empty;
        }

        /// <summary>
        /// The GetClassName method returns the fully qualified name of the
        /// class this type descriptor is representing. Returning null from
        /// this method causes the TypeDescriptor object to return the
        /// default class name.
        /// </summary>
        public virtual string? GetClassName() => _parent?.GetClassName();

        /// <summary>
        /// The GetComponentName method returns the name of the component instance
        /// this type descriptor is describing.
        /// </summary>
        public virtual string? GetComponentName() => _parent?.GetComponentName();

        /// <summary>
        /// The GetConverter method returns a type converter for the type this type
        /// descriptor is representing.
        /// </summary>
        [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage)]
        public virtual TypeConverter? GetConverter()
        {
            if (_parent != null)
            {
                return _parent.GetConverter();
            }

            return new TypeConverter();
        }

        /// <summary>
        /// The GetDefaultEvent method returns the event descriptor for the default
        /// event on the object this type descriptor is representing.
        /// </summary>
        [RequiresUnreferencedCode(EventDescriptor.RequiresUnreferencedCodeMessage)]
        public virtual EventDescriptor? GetDefaultEvent() => _parent?.GetDefaultEvent();

        /// <summary>
        /// The GetDefaultProperty method returns the property descriptor for the
        /// default property on the object this type descriptor is representing.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
        public virtual PropertyDescriptor? GetDefaultProperty() => _parent?.GetDefaultProperty();

        /// <summary>
        /// The GetEditor method returns an editor of the given type that is
        /// to be associated with the class this type descriptor is representing.
        /// </summary>
        [RequiresUnreferencedCode(TypeDescriptor.DesignTimeAttributeTrimmed)]
        public virtual object? GetEditor(Type editorBaseType) => _parent?.GetEditor(editorBaseType);

        /// <summary>
        /// The GetEvents method returns a collection of event descriptors
        /// for the object this type descriptor is representing. An optional
        /// attribute array may be provided to filter the collection that is
        /// returned. If no parent is provided,this will return an empty
        /// event collection.
        /// </summary>
        public virtual EventDescriptorCollection GetEvents()
        {
            if (_parent != null)
            {
                return _parent.GetEvents();
            }

            return EventDescriptorCollection.Empty;
        }

        /// <summary>
        /// The GetEvents method returns a collection of event descriptors
        /// for the object this type descriptor is representing. An optional
        /// attribute array may be provided to filter the collection that is
        /// returned. If no parent is provided,this will return an empty
        /// event collection.
        /// </summary>
        [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public virtual EventDescriptorCollection GetEvents(Attribute[]? attributes)
        {
            if (_parent != null)
            {
                return _parent.GetEvents(attributes);
            }

            return EventDescriptorCollection.Empty;
        }

        /// <summary>
        /// The GetProperties method returns a collection of property descriptors
        /// for the object this type descriptor is representing. An optional
        /// attribute array may be provided to filter the collection that is returned.
        /// If no parent is provided,this will return an empty
        /// property collection.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
        public virtual PropertyDescriptorCollection GetProperties()
        {
            if (_parent != null)
            {
                return _parent.GetProperties();
            }

            return PropertyDescriptorCollection.Empty;
        }

        /// <summary>
        /// The GetProperties method returns a collection of property descriptors
        /// for the object this type descriptor is representing. An optional
        /// attribute array may be provided to filter the collection that is returned.
        /// If no parent is provided,this will return an empty
        /// property collection.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public virtual PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
        {
            if (_parent != null)
            {
                return _parent.GetProperties(attributes);
            }

            return PropertyDescriptorCollection.Empty;
        }

        /// <summary>
        /// The GetPropertyOwner method returns an instance of an object that
        /// owns the given property for the object this type descriptor is representing.
        /// An optional attribute array may be provided to filter the collection that is
        /// returned. Returning null from this method causes the TypeDescriptor object
        /// to use its default type description services.
        /// </summary>
        public virtual object? GetPropertyOwner(PropertyDescriptor? pd) => _parent?.GetPropertyOwner(pd);
    }
}
