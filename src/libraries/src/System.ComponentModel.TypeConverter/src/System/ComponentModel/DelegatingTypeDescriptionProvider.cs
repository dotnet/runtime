// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel
{
    /// <summary>
    /// This is a simple type description provider that, when invoked demand
    /// locates the correct type description provider for the given type and
    /// invokes it. This is used as the tail node for both type and instance
    /// based providers. Conceptually it "links" the provider list for one type
    /// or instance to its corresponding base type.
    /// </summary>
    internal sealed class DelegatingTypeDescriptionProvider : TypeDescriptionProvider
    {
        private readonly Type _type;

        /// <summary>
        /// Creates a new DelegatingTypeDescriptionProvider. The type is the type we will delegate to.
        /// </summary>
        internal DelegatingTypeDescriptionProvider(Type type)
        {
            _type = type;
        }

        internal TypeDescriptionProvider Provider => TypeDescriptor.GetProviderRecursive(_type);

        /// <summary>
        /// This method is used to create an instance that can substitute for another 
        /// data type. If the method is not interested in providing a substitute 
        /// instance, it should call base.
        /// </summary>
        public override object CreateInstance(IServiceProvider provider, Type objectType, Type[] argTypes, object[] args)
        {
            return Provider.CreateInstance(provider, objectType, argTypes, args);
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
        public override IDictionary GetCache(object instance) => Provider.GetCache(instance);

        /// <summary>
        /// The name of the specified component, or null if the component has no name.
        /// In many cases this will return the same value as GetComponentName. If the
        /// component resides in a nested container or has other nested semantics, it may
        /// return a different fully qualified name.
        ///
        /// If not overridden, the default implementation of this method will call
        /// GetTypeDescriptor.GetComponentName.
        /// </summary>
        public override string GetFullComponentName(object component) => Provider.GetFullComponentName(component);

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
        public override ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
        {
            return Provider.GetExtendedTypeDescriptor(instance);
        }

        protected internal override IExtenderProvider[] GetExtenderProviders(object instance)
        {
            return Provider.GetExtenderProviders(instance);
        }

        /// <summary>
        /// The GetReflection method is a lower level version of GetTypeDescriptor. 
        /// If no custom type descriptor can be located for an object, GetReflection 
        /// is called to perform normal reflection against the object.
        /// </summary>
        public override Type GetReflectionType(Type objectType, object instance)
        {
            return Provider.GetReflectionType(objectType, instance);
        }

        public override Type GetRuntimeType(Type objectType) => Provider.GetRuntimeType(objectType);

        /// <summary>
        /// This method returns a custom type descriptor for the given type / object. 
        /// The objectType parameter is always valid, but the instance parameter may 
        /// be null if no instance was passed to TypeDescriptor. The method should 
        /// return a custom type descriptor for the object. If the method is not 
        /// interested in providing type information for the object it should 
        /// return null.
        /// </summary>
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            return Provider.GetTypeDescriptor(objectType, instance);
        }

        public override bool IsSupportedType(Type type) => Provider.IsSupportedType(type);
    }
}
