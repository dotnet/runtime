// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// The TypeDescriptionProvider class can be thought of as a "plug-in" for
    /// TypeDescriptor. There can be multiple type description provider classes
    /// all offering metadata to TypeDescriptor
    /// </summary>
    public abstract class TypeDescriptionProvider
    {
        private readonly TypeDescriptionProvider _parent;
        private EmptyCustomTypeDescriptor _emptyDescriptor;

        /// <summary>
        /// There are two versions of the constructor for this class. The empty
        /// constructor is identical to using TypeDescriptionProvider(null).
        /// If a child type description provider is passed into the constructor,
        /// the "base" versions of all methods will call to this parent provider.
        /// If no such provider is given, the base versions of the methods will
        /// return empty, but valid values.
        /// </summary>
        protected TypeDescriptionProvider()
        {
        }

        /// <summary>
        /// There are two versions of the constructor for this class. The empty
        /// constructor is identical to using TypeDescriptionProvider(null).
        /// If a child type description provider is passed into the constructor,
        /// the "base" versions of all methods will call to this parent provider.
        /// If no such provider is given, the base versions of the methods will
        /// return empty, but valid values.
        /// </summary>
        protected TypeDescriptionProvider(TypeDescriptionProvider parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// This method is used to create an instance that can substitute for another
        /// data type. If the method is not interested in providing a substitute
        /// instance, it should call base.
        ///
        /// This method is prototyped as virtual, and by default returns null if no
        /// parent provider was passed. If a parent provider was passed, this
        /// method will invoke the parent provider's CreateInstance method.
        /// </summary>
        public virtual object CreateInstance(
            IServiceProvider provider,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type objectType,
            Type[] argTypes,
            object[] args)
        {
            if (_parent != null)
            {
                return _parent.CreateInstance(provider, objectType, argTypes, args);
            }

            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            return Activator.CreateInstance(objectType, args);
        }

        /// <summary>
        /// TypeDescriptor may need to perform complex operations on collections of metadata.
        /// Since types are not unloaded for the life of a domain, TypeDescriptor will
        /// automatically cache the results of these operations based on type. There are a
        /// number of operations that use live object instances, however. These operations
        /// cannot be cached within TypeDescriptor because caching them would prevent the
        /// object from garbage collecting. Instead, TypeDescriptor allows for a per-object
        /// cache, accessed as an IDictionary of key/value pairs, to exist on an object.
        /// The GetCache method returns an instance of this cache. GetCache will return
        /// null if there is no supported cache for an object.
        /// </summary>
        public virtual IDictionary GetCache(object instance) => _parent?.GetCache(instance);

        /// <summary>
        /// This method returns an extended custom type descriptor for the given object.
        /// An extended type descriptor is a custom type descriptor that offers properties
        /// that other objects have added to this object, but are not actually defined on
        /// the object. For example, in the .NET Framework Component Model, objects that
        /// implement the interface IExtenderProvider can "attach" properties to other
        /// objects that reside in the same logical container. The GetTypeDescriptor
        /// method does not return a type descriptor that provides these extra extended
        /// properties. GetExtendedTypeDescriptor returns the set of these extended
        /// properties. TypeDescriptor will automatically merge the results of these
        /// two property collections. Note that while the .NET Framework component
        /// model only supports extended properties this API can be used for extended
        /// attributes and events as well, if the type description provider supports it.
        /// </summary>
        [RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
        public virtual ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
        {
            if (_parent != null)
            {
                return _parent.GetExtendedTypeDescriptor(instance);
            }

            return _emptyDescriptor ?? (_emptyDescriptor = new EmptyCustomTypeDescriptor());
        }

        protected internal virtual IExtenderProvider[] GetExtenderProviders(object instance)
        {
            if (_parent != null)
            {
                return _parent.GetExtenderProviders(instance);
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return Array.Empty<IExtenderProvider>();
        }

        /// <summary>
        /// The name of the specified component, or null if the component has no name.
        /// In many cases this will return the same value as GetComponentName. If the
        /// component resides in a nested container or has other nested semantics, it may
        /// return a different fully qualified name.
        ///
        /// If not overridden, the default implementation of this method will call
        /// GetTypeDescriptor.GetComponentName.
        /// </summary>
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public virtual string GetFullComponentName(object component)
        {
            if (_parent != null)
            {
                return _parent.GetFullComponentName(component);
            }

            return GetTypeDescriptor(component)?.GetComponentName();
        }

        /// <summary>
        /// The GetReflection method is a lower level version of GetTypeDescriptor.
        /// If no custom type descriptor can be located for an object, GetReflectionType
        /// is called to perform normal reflection against the object.
        /// </summary>
        [return: DynamicallyAccessedMembers(TypeDescriptor.ReflectTypesDynamicallyAccessedMembers)]
        public Type GetReflectionType([DynamicallyAccessedMembers(TypeDescriptor.ReflectTypesDynamicallyAccessedMembers)] Type objectType) => GetReflectionType(objectType, null);

        /// <summary>
        /// The GetReflection method is a lower level version of GetTypeDescriptor.
        /// If no custom type descriptor can be located for an object, GetReflectionType
        /// is called to perform normal reflection against the object.
        ///
        /// This method is prototyped as virtual, and by default returns the
        /// object type if no parent provider was passed. If a parent provider was passed, this
        /// method will invoke the parent provider's GetReflectionType method.
        /// </summary>
        [RequiresUnreferencedCode("GetReflectionType is not trim compatible because the Type of object cannot be statically discovered.")]
        public Type GetReflectionType(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return GetReflectionType(instance.GetType(), instance);
        }

        /// <summary>
        /// The GetReflection method is a lower level version of GetTypeDescriptor.
        /// If no custom type descriptor can be located for an object, GetReflectionType
        /// is called to perform normal reflection against the object.
        ///
        /// This method is prototyped as virtual, and by default returns the
        /// object type if no parent provider was passed. If a parent provider was passed, this
        /// method will invoke the parent provider's GetReflectionType method.
        /// </summary>
        [return: DynamicallyAccessedMembers(TypeDescriptor.ReflectTypesDynamicallyAccessedMembers)]
        public virtual Type GetReflectionType(
            [DynamicallyAccessedMembers(TypeDescriptor.ReflectTypesDynamicallyAccessedMembers)] Type objectType,
            object instance)
        {
            if (_parent != null)
            {
                return _parent.GetReflectionType(objectType, instance);
            }

            return objectType;
        }

        /// <summary>
        /// The GetRuntimeType method reverses GetReflectionType to convert a reflection type
        /// back into a runtime type. Historically the Type.UnderlyingSystemType property has
        /// been used to return the runtime type. This isn't exactly correct, but it needs
        /// to be preserved unless all type description providers are revised.
        /// </summary>
        public virtual Type GetRuntimeType(Type reflectionType)
        {
            if (_parent != null)
            {
                return _parent.GetRuntimeType(reflectionType);
            }

            if (reflectionType == null)
            {
                throw new ArgumentNullException(nameof(reflectionType));
            }

            if (reflectionType.GetType().Assembly == typeof(object).Assembly)
            {
                return reflectionType;
            }

            return reflectionType.UnderlyingSystemType;
        }

        /// <summary>
        /// This method returns a custom type descriptor for the given type / object.
        /// The objectType parameter is always valid, but the instance parameter may
        /// be null if no instance was passed to TypeDescriptor. The method should
        /// return a custom type descriptor for the object. If the method is not
        /// interested in providing type information for the object it should
        /// return base.
        /// </summary>
        public ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType)
        {
            return GetTypeDescriptor(objectType, null);
        }

        /// <summary>
        /// This method returns a custom type descriptor for the given type / object.
        /// The objectType parameter is always valid, but the instance parameter may
        /// be null if no instance was passed to TypeDescriptor. The method should
        /// return a custom type descriptor for the object. If the method is not
        /// interested in providing type information for the object it should
        /// return base.
        /// </summary>
        [RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
        public ICustomTypeDescriptor GetTypeDescriptor(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return GetTypeDescriptor(instance.GetType(), instance);
        }

        /// <summary>
        /// This method returns a custom type descriptor for the given type / object.
        /// The objectType parameter is always valid, but the instance parameter may
        /// be null if no instance was passed to TypeDescriptor. The method should
        /// return a custom type descriptor for the object. If the method is not
        /// interested in providing type information for the object it should
        /// return base.
        ///
        /// This method is prototyped as virtual, and by default returns a
        /// custom type descriptor that returns empty collections for all values
        /// if no parent provider was passed. If a parent provider was passed,
        /// this method will invoke the parent provider's GetTypeDescriptor
        /// method.
        /// </summary>
        public virtual ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object instance)
        {
            if (_parent != null)
            {
                return _parent.GetTypeDescriptor(objectType, instance);
            }

            return _emptyDescriptor ?? (_emptyDescriptor = new EmptyCustomTypeDescriptor());
        }

        /// <summary>
        /// This method returns true if the type is "supported" by the type descriptor
        /// and its chain of type description providers.
        /// </summary>
        public virtual bool IsSupportedType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_parent != null)
            {
                return _parent.IsSupportedType(type);
            }

            return true;
        }

        /// <summary>
        /// A simple empty descriptor that is used as a placeholder for times
        /// when the user does not provide their own.
        /// </summary>
        private sealed class EmptyCustomTypeDescriptor : CustomTypeDescriptor
        {
        }
    }
}
