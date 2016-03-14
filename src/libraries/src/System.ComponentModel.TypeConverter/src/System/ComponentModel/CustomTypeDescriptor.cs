// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    public abstract class CustomTypeDescriptor : ICustomTypeDescriptor
    {
        private ICustomTypeDescriptor _parent;

        /// <devdoc>
        ///     Creates a new CustomTypeDescriptor object.  There are two versions 
        ///     of this constructor. The version that takes no parameters simply 
        ///     calls the version that takes a parent and passes null as the 
        ///     parent value.  If the parent is null, CustomTypeDescriptor returns 
        ///     the defined default values for each method.  If the parent is 
        ///     non-null, CustomTypeDescriptor calls the parent's version of 
        ///     the method.
        /// </devdoc>
        protected CustomTypeDescriptor()
        {
        }

        /// <devdoc>
        ///     Creates a new CustomTypeDescriptor object.  There are two versions 
        ///     of this constructor. The version that takes no parameters simply 
        ///     calls the version that takes a parent and passes null as the 
        ///     parent value.  If the parent is null, CustomTypeDescriptor returns 
        ///     the defined default values for each method.  If the parent is 
        ///     non-null, CustomTypeDescriptor calls the parent's version of 
        ///     the method.
        /// </devdoc>
        protected CustomTypeDescriptor(ICustomTypeDescriptor parent)
        {
            _parent = parent;
        }

        /// <devdoc>
        ///     The GetAttributes method returns the type-level attributes for 
        ///     the type this custom type descriptor is providing information for.  
        ///     You must always return a valid collection from this method.
        /// </devdoc>
        public virtual AttributeCollection GetAttributes()
        {
            if (_parent != null)
            {
                return _parent.GetAttributes();
            }

            return AttributeCollection.Empty;
        }

        /// <devdoc>
        ///     The GetClassName method returns the fully qualified name of the 
        ///     class this type descriptor is representing.  Returning null from 
        ///     this method causes the TypeDescriptor object to return the
        ///     default class name.
        /// </devdoc>
        public virtual string GetClassName()
        {
            if (_parent != null)
            {
                return _parent.GetClassName();
            }

            return null;
        }

        /// <devdoc>
        ///     The GetComponentName method returns the name of the component instance 
        ///     this type descriptor is describing.
        /// </devdoc>
        public virtual string GetComponentName()
        {
            if (_parent != null)
            {
                return _parent.GetComponentName();
            }

            return null;
        }

        /// <devdoc>
        ///     The GetConverter method returns a type converter for the type this type 
        ///     descriptor is representing.
        /// </devdoc>
        public virtual TypeConverter GetConverter()
        {
            if (_parent != null)
            {
                return _parent.GetConverter();
            }

            return new TypeConverter();
        }

        /// <devdoc>
        ///     The GetDefaultEvent method returns the event descriptor for the default 
        ///     event on the object this type descriptor is representing.
        /// </devdoc>
        public virtual EventDescriptor GetDefaultEvent()
        {
            if (_parent != null)
            {
                return _parent.GetDefaultEvent();
            }

            return null;
        }

        /// <devdoc>
        ///     The GetDefaultProperty method returns the property descriptor for the 
        ///     default property on the object this type descriptor is representing.  
        /// </devdoc>
        public virtual PropertyDescriptor GetDefaultProperty()
        {
            if (_parent != null)
            {
                return _parent.GetDefaultProperty();
            }

            return null;
        }

        /// <devdoc>
        ///     The GetEditor method returns an editor of the given type that is 
        ///     to be associated with the class this type descriptor is representing.  
        /// </devdoc>
        public virtual object GetEditor(Type editorBaseType)
        {
            if (_parent != null)
            {
                return _parent.GetEditor(editorBaseType);
            }

            return null;
        }

        /// <devdoc>
        ///     The GetEvents method returns a collection of event descriptors 
        ///     for the object this type descriptor is representing.  An optional 
        ///     attribute array may be provided to filter the collection that is 
        ///     returned.  If no parent is provided,this will return an empty
        ///     event collection.
        /// </devdoc>
        public virtual EventDescriptorCollection GetEvents()
        {
            if (_parent != null)
            {
                return _parent.GetEvents();
            }

            return EventDescriptorCollection.Empty;
        }

        /// <devdoc>
        ///     The GetEvents method returns a collection of event descriptors 
        ///     for the object this type descriptor is representing.  An optional 
        ///     attribute array may be provided to filter the collection that is 
        ///     returned.  If no parent is provided,this will return an empty
        ///     event collection.
        /// </devdoc>
        public virtual EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            if (_parent != null)
            {
                return _parent.GetEvents(attributes);
            }

            return EventDescriptorCollection.Empty;
        }

        /// <devdoc>
        ///     The GetProperties method returns a collection of property descriptors 
        ///     for the object this type descriptor is representing.  An optional 
        ///     attribute array may be provided to filter the collection that is returned.  
        ///     If no parent is provided,this will return an empty
        ///     property collection.
        /// </devdoc>
        public virtual PropertyDescriptorCollection GetProperties()
        {
            if (_parent != null)
            {
                return _parent.GetProperties();
            }

            return PropertyDescriptorCollection.Empty;
        }

        /// <devdoc>
        ///     The GetProperties method returns a collection of property descriptors 
        ///     for the object this type descriptor is representing.  An optional 
        ///     attribute array may be provided to filter the collection that is returned.  
        ///     If no parent is provided,this will return an empty
        ///     property collection.
        /// </devdoc>
        public virtual PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            if (_parent != null)
            {
                return _parent.GetProperties(attributes);
            }

            return PropertyDescriptorCollection.Empty;
        }

        /// <devdoc>
        ///     The GetPropertyOwner method returns an instance of an object that 
        ///     owns the given property for the object this type descriptor is representing.  
        ///     An optional attribute array may be provided to filter the collection that is 
        ///     returned.  Returning null from this method causes the TypeDescriptor object 
        ///     to use its default type description services.
        /// </devdoc>
        public virtual object GetPropertyOwner(PropertyDescriptor pd)
        {
            if (_parent != null)
            {
                return _parent.GetPropertyOwner(pd);
            }

            return null;
        }
    }
}

