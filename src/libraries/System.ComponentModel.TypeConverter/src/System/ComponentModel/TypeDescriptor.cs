// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides information about the properties and events for a component.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class TypeDescriptor
    {
        internal const DynamicallyAccessedMemberTypes ReflectTypesDynamicallyAccessedMembers = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields;
        internal const string DesignTimeAttributeTrimmed = "Design-time attributes are not preserved when trimming. Types referenced by attributes like EditorAttribute and DesignerAttribute may not be available after trimming.";

        // Note: this is initialized at class load because we
        // lock on it for thread safety. It is used from nearly
        // every call to this class, so it will be created soon after
        // class load anyway.
        private static readonly WeakHashtable s_providerTable = new WeakHashtable();     // mapping of type or object hash to a provider list
        private static readonly Hashtable s_providerTypeTable = new Hashtable();         // A direct mapping from type to provider.

        private static readonly Hashtable s_defaultProviderInitialized = new Hashtable(); // A table of type -> object to track DefaultTypeDescriptionProviderAttributes.
                                                                                          // A value of `null` indicates initialization is in progress.
                                                                                          // A value of s_initializedDefaultProvider indicates the provider is initialized.
        private static readonly object s_initializedDefaultProvider = new object();

        private static WeakHashtable? s_associationTable;
        private static int s_metadataVersion;                          // a version stamp for our metadata. Used by property descriptors to know when to rebuild attributes.

        // This is an index that we use to create a unique name for a property in the
        // event of a name collision. The only time we should use this is when
        // a name collision happened on an extender property that has no site or
        // no name on its site. Should be very rare.
        private static int s_collisionIndex;

        // For each stage of our filtering pipeline, the pipeline needs to know what it is filtering.
        private const int PIPELINE_ATTRIBUTES = 0x00;
        private const int PIPELINE_PROPERTIES = 0x01;
        private const int PIPELINE_EVENTS = 0x02;

        // And each stage of the pipeline needs to have its own
        // keys for its cache table. We use guids because they
        // are unique and fast to compare. The order for each of
        // these keys must match the Id's of the filter type above.
        private static readonly Guid[] s_pipelineInitializeKeys = new Guid[]
        {
            Guid.NewGuid(), // attributes
            Guid.NewGuid(), // properties
            Guid.NewGuid()  // events
        };

        private static readonly Guid[] s_pipelineMergeKeys = new Guid[]
        {
            Guid.NewGuid(), // attributes
            Guid.NewGuid(), // properties
            Guid.NewGuid()  // events
        };

        private static readonly Guid[] s_pipelineFilterKeys = new Guid[]
        {
            Guid.NewGuid(), // attributes
            Guid.NewGuid(), // properties
            Guid.NewGuid()  // events
        };

        private static readonly Guid[] s_pipelineAttributeFilterKeys = new Guid[]
        {
            Guid.NewGuid(), // attributes
            Guid.NewGuid(), // properties
            Guid.NewGuid()  // events
        };

        private TypeDescriptor()
        {
        }

        /// <summary>
        /// This property returns a Type object that can be passed to the various
        /// AddProvider methods to define a type description provider for interface types.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Type InterfaceType
        {
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            get => typeof(TypeDescriptorInterface);
        }

        /// <summary>
        /// This value increments each time someone refreshes or changes metadata.
        /// </summary>
        internal static int MetadataVersion => s_metadataVersion;

        private static WeakHashtable AssociationTable => LazyInitializer.EnsureInitialized(ref s_associationTable, () => new WeakHashtable());

        /// <summary>
        /// Occurs when Refreshed is raised for a component.
        /// </summary>
        public static event RefreshEventHandler? Refreshed;

        /// <summary>
        /// The AddAttributes method allows you to add class-level attributes for a
        /// type or an instance. This method simply implements a type description provider
        /// that merges the provided attributes with the attributes that already exist on
        /// the class. This is a short cut for such a behavior. Adding additional
        /// attributes is common need for applications using the Windows Forms property
        /// window. The return value form AddAttributes is the TypeDescriptionProvider
        /// that was used to add the attributes. This provider can later be passed to
        /// RemoveProvider if the added attributes are no longer needed.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static TypeDescriptionProvider AddAttributes(Type type, params Attribute[] attributes)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(attributes);

            TypeDescriptionProvider existingProvider = GetProvider(type);
            TypeDescriptionProvider provider = new AttributeProvider(existingProvider, attributes);
            TypeDescriptor.AddProvider(provider, type);
            return provider;
        }

        /// <summary>
        /// The AddAttributes method allows you to add class-level attributes for a
        /// type or an instance. This method simply implements a type description provider
        /// that merges the provided attributes with the attributes that already exist on
        /// the class. This is a short cut for such a behavior. Adding additional
        /// attributes is common need for applications using the Windows Forms property
        /// window. The return value form AddAttributes is the TypeDescriptionProvider
        /// that was used to add the attributes. This provider can later be passed to
        /// RemoveProvider if the added attributes are no longer needed.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static TypeDescriptionProvider AddAttributes(object instance, params Attribute[] attributes)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(attributes);

            TypeDescriptionProvider existingProvider = GetProvider(instance);
            TypeDescriptionProvider provider = new AttributeProvider(existingProvider, attributes);
            AddProvider(provider, instance);
            return provider;
        }

        /// <summary>
        /// Adds an editor table for the given editor base type. Typically, editors are
        /// specified as metadata on an object. If no metadata for a requested editor
        /// base type can be found on an object, however, the TypeDescriptor will search
        /// an editor table for the editor type, if one can be found.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode("The Types specified in table may be trimmed, or have their static construtors trimmed.")]
        public static void AddEditorTable(Type editorBaseType, Hashtable table)
        {
            ReflectTypeDescriptionProvider.AddEditorTable(editorBaseType, table);
        }

        /// <summary>
        /// Adds a type description provider that will be called on to provide
        /// type and instance information for any object that is of, or a subtype
        /// of, the provided type. Type can be any type, including interfaces.
        /// For example, to provide custom type and instance information for all
        /// components, you would pass typeof(IComponent). Passing typeof(object)
        /// will cause the provider to be called to provide type information for
        /// all types.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void AddProvider(TypeDescriptionProvider provider, Type type)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(type);

            lock (s_providerTable)
            {
                // Get the root node, hook it up, and stuff it back into
                // the provider cache.
                TypeDescriptionNode node = NodeFor(type, true);
                var head = new TypeDescriptionNode(provider) { Next = node };
                s_providerTable[type] = head;
                s_providerTypeTable.Clear();
            }

            Refresh(type);
        }

        /// <summary>
        /// Adds a type description provider that will be called on to provide
        /// type information for a single object instance. A provider added
        /// using this method will never have its CreateInstance method called
        /// because the instance already exists. This method does not prevent
        /// the object from finalizing.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void AddProvider(TypeDescriptionProvider provider, object instance)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(instance);

            bool refreshNeeded;

            // Get the root node, hook it up, and stuff it back into
            // the provider cache.
            lock (s_providerTable)
            {
                refreshNeeded = s_providerTable.ContainsKey(instance);
                TypeDescriptionNode node = NodeFor(instance, true);
                var head = new TypeDescriptionNode(provider) { Next = node };
                s_providerTable.SetWeak(instance, head);
                s_providerTypeTable.Clear();
            }

            if (refreshNeeded)
            {
                Refresh(instance, false);
            }
        }

        /// <summary>
        /// Adds a type description provider that will be called on to provide
        /// type and instance information for any object that is of, or a subtype
        /// of, the provided type. Type can be any type, including interfaces.
        /// For example, to provide custom type and instance information for all
        /// components, you would pass typeof(IComponent). Passing typeof(object)
        /// will cause the provider to be called to provide type information for
        /// all types.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void AddProviderTransparent(TypeDescriptionProvider provider, Type type)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(type);

            AddProvider(provider, type);
        }

        /// <summary>
        /// Adds a type description provider that will be called on to provide
        /// type information for a single object instance. A provider added
        /// using this method will never have its CreateInstance method called
        /// because the instance already exists. This method does not prevent
        /// the object from finalizing.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void AddProviderTransparent(TypeDescriptionProvider provider, object instance)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(instance);

            AddProvider(provider, instance);
        }

        /// <summary>
        /// This method verifies that we have checked for the presence
        /// of a default type description provider attribute for the
        /// given type.
        /// </summary>
        private static void CheckDefaultProvider(Type type)
        {
            if (s_defaultProviderInitialized[type] == s_initializedDefaultProvider)
            {
                return;
            }

            // Lock on s_providerTable even though s_providerTable is not modified here.
            // Using a single lock prevents deadlocks since other methods that call into or are called
            // by this method also lock on s_providerTable and the ordering of the locks may be different.
            lock (s_providerTable)
            {
                AddDefaultProvider(type);
            }
        }

        /// <summary>
        /// Add the default provider, if it exists.
        /// For threading, this is always called under a 'lock (s_providerTable)'.
        /// </summary>
        private static void AddDefaultProvider(Type type)
        {
            bool providerAdded = false;

            if (s_defaultProviderInitialized.ContainsKey(type))
            {
                // Either another thread finished initializing for this type, or we are recursing on the same thread.
                return;
            }

            // Immediately set this to null to indicate we are in progress setting the default provider for a type.
            // This prevents re-entrance to this method.
            s_defaultProviderInitialized[type] = null;

            // Always use core reflection when checking for the default provider attribute.
            // If there is a provider, we probably don't want to build up our own cache state against the type.
            // There shouldn't be more than one of these, but walk anyway.
            // Walk in reverse order so that the most derived takes precedence.
            object[] attrs = type.GetCustomAttributes(typeof(TypeDescriptionProviderAttribute), false);
            for (int idx = attrs.Length - 1; idx >= 0; idx--)
            {
                TypeDescriptionProviderAttribute pa = (TypeDescriptionProviderAttribute)attrs[idx];
                Type? providerType = Type.GetType(pa.TypeName);
                if (providerType != null && typeof(TypeDescriptionProvider).IsAssignableFrom(providerType))
                {
                    TypeDescriptionProvider prov = (TypeDescriptionProvider)Activator.CreateInstance(providerType)!;
                    AddProvider(prov, type);
                    providerAdded = true;
                }
            }

            // If we did not add a provider, check the base class.
            if (!providerAdded)
            {
                Type? baseType = type.BaseType;
                if (baseType != null && baseType != type)
                {
                    AddDefaultProvider(baseType);
                }
            }

            s_defaultProviderInitialized[type] = s_initializedDefaultProvider;
        }

        /// <summary>
        /// The CreateAssociation method creates an association between two objects.
        /// Once an association is created, a designer or other filtering mechanism
        /// can add properties that route to either object into the primary object's
        /// property set. When a property invocation is made against the primary
        /// object, GetAssociation will be called to resolve the actual object
        /// instance that is related to its type parameter.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void CreateAssociation(object primary, object secondary)
        {
            ArgumentNullException.ThrowIfNull(primary);
            ArgumentNullException.ThrowIfNull(secondary);

            if (primary == secondary)
            {
                throw new ArgumentException(SR.TypeDescriptorSameAssociation);
            }

            WeakHashtable associationTable = AssociationTable;
            IList? associations = (IList?)associationTable[primary];

            if (associations == null)
            {
                lock (associationTable)
                {
                    associations = (IList?)associationTable[primary];
                    if (associations == null)
                    {
                        associations = new ArrayList(4);
                        associationTable.SetWeak(primary, associations);
                    }
                }
            }
            else
            {
                for (int idx = associations.Count - 1; idx >= 0; idx--)
                {
                    WeakReference r = (WeakReference)associations[idx]!;
                    if (r.IsAlive && r.Target == secondary)
                    {
                        throw new ArgumentException(SR.TypeDescriptorAlreadyAssociated);
                    }
                }
            }

            lock (associations)
            {
                associations.Add(new WeakReference(secondary));
            }
        }

        /// <summary>
        /// This dynamically binds an EventDescriptor to a type.
        /// </summary>
        public static EventDescriptor CreateEvent(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
            string name,
            Type type,
            params Attribute[] attributes)
        {
            return new ReflectEventDescriptor(componentType, name, type, attributes);
        }

        /// <summary>
        /// This creates a new event descriptor identical to an existing event descriptor. The new event descriptor
        /// has the specified metadata attributes merged with the existing metadata attributes.
        /// </summary>
        public static EventDescriptor CreateEvent(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
            EventDescriptor oldEventDescriptor,
            params Attribute[] attributes)
        {
            return new ReflectEventDescriptor(componentType, oldEventDescriptor, attributes);
        }

        /// <summary>
        /// This method will search internal tables within TypeDescriptor for
        /// a TypeDescriptionProvider object that is associated with the given
        /// data type. If it finds one, it will delegate the call to that object.
        /// </summary>
        public static object? CreateInstance(
            IServiceProvider? provider,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type objectType,
            Type[]? argTypes,
            object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(objectType);

            if (argTypes != null)
            {
                ArgumentNullException.ThrowIfNull(args);

                if (argTypes.Length != args.Length)
                {
                    throw new ArgumentException(SR.TypeDescriptorArgsCountMismatch);
                }
            }

            object? instance = null;

            // See if the provider wants to offer a TypeDescriptionProvider to delegate to. This allows
            // a caller to have complete control over all object instantiation.
            if (provider?.GetService(typeof(TypeDescriptionProvider)) is TypeDescriptionProvider p)
            {
                instance = p.CreateInstance(provider, objectType, argTypes, args);
            }

            return instance ?? NodeFor(objectType).CreateInstance(provider, objectType, argTypes, args);
        }

        /// <summary>
        /// This dynamically binds a PropertyDescriptor to a type.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
        public static PropertyDescriptor CreateProperty(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
            string name,
            Type type,
            params Attribute[] attributes)
        {
            return new ReflectPropertyDescriptor(componentType, name, type, attributes);
        }

        /// <summary>
        /// This creates a new property descriptor identical to an existing property descriptor. The new property descriptor
        /// has the specified metadata attributes merged with the existing metadata attributes.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
        public static PropertyDescriptor CreateProperty(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
            PropertyDescriptor oldPropertyDescriptor,
            params Attribute[] attributes)
        {
            // We must do some special case work here for extended properties. If the old property descriptor is really
            // an extender property that is being surfaced on a component as a normal property, then we must
            // do work here or else ReflectPropertyDescriptor will fail to resolve the get and set methods. We check
            // for the necessary ExtenderProvidedPropertyAttribute and if we find it, we create an
            // ExtendedPropertyDescriptor instead. We only do this if the component class is the same, since the user
            // may want to re-route the property to a different target.
            //
            if (componentType == oldPropertyDescriptor.ComponentType)
            {
                ExtenderProvidedPropertyAttribute attr = (ExtenderProvidedPropertyAttribute)
                                                         oldPropertyDescriptor.Attributes[
                                                         typeof(ExtenderProvidedPropertyAttribute)]!;

                if (attr.ExtenderProperty is ReflectPropertyDescriptor)
                {
                    return new ExtendedPropertyDescriptor(oldPropertyDescriptor, attributes);
                }
            }

            // This is either a normal prop or the caller has changed target classes.
            return new ReflectPropertyDescriptor(componentType, oldPropertyDescriptor, attributes);
        }

        /// <summary>
        /// This  API is used to remove any members from the given
        /// collection that do not match the attribute array. If members
        /// need to be removed, a new ArrayList wil be created that
        /// contains only the remaining members. The API returns
        /// NULL if it did not need to filter any members.
        /// </summary>
        [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        private static ArrayList? FilterMembers(IList members, Attribute[] attributes)
        {
            ArrayList? newMembers = null;
            int memberCount = members.Count;

            for (int idx = 0; idx < memberCount; idx++)
            {
                bool hide = false;

                for (int attrIdx = 0; attrIdx < attributes.Length; attrIdx++)
                {
                    if (ShouldHideMember((MemberDescriptor?)members[idx], attributes[attrIdx]))
                    {
                        hide = true;
                        break;
                    }
                }

                if (hide)
                {
                    // We have to hide. If this is the first time, we need to init
                    // newMembers to have all the valid members we have previously
                    // hit.
                    if (newMembers == null)
                    {
                        newMembers = new ArrayList(memberCount);
                        for (int validIdx = 0; validIdx < idx; validIdx++)
                        {
                            newMembers.Add(members[validIdx]);
                        }
                    }
                }
                else
                {
                    newMembers?.Add(members[idx]);
                }
            }

            return newMembers;
        }

        /// <summary>
        /// The GetAssociation method returns the correct object to invoke
        /// for the requested type. It never returns null.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static object GetAssociation(Type type, object primary)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(primary);

            object associatedObject = primary;

            if (!type.IsInstanceOfType(primary))
            {
                // Check our association table for a match.
                Hashtable assocTable = AssociationTable;
                IList? associations = (IList?)assocTable?[primary];
                if (associations != null)
                {
                    lock (associations)
                    {
                        for (int idx = associations.Count - 1; idx >= 0; idx--)
                        {
                            // Look for an associated object that has a type that
                            // matches the given type.
                            WeakReference weakRef = (WeakReference)associations[idx]!;
                            object? secondary = weakRef.Target;
                            if (secondary == null)
                            {
                                associations.RemoveAt(idx);
                            }
                            else if (type.IsInstanceOfType(secondary))
                            {
                                associatedObject = secondary;
                            }
                        }
                    }
                }

                // Not in our table. We have a default association with a designer
                // if that designer is a component.
                if (associatedObject == primary)
                {
                    IComponent? component = primary as IComponent;
                    if (component != null)
                    {
                        ISite? site = component.Site;

                        if (site != null && site.DesignMode)
                        {
                            IDesignerHost? host = site.GetService(typeof(IDesignerHost)) as IDesignerHost;
                            if (host != null)
                            {
                                object? designer = host.GetDesigner(component);

                                // We only use the designer if it has a compatible class. If we
                                // got here, we're probably hosed because the user just passed in
                                // an object that this PropertyDescriptor can't munch on, but it's
                                // clearer to use that object instance instead of it's designer.
                                if (designer != null && type.IsInstanceOfType(designer))
                                {
                                    associatedObject = designer;
                                }
                            }
                        }
                    }
                }
            }

            return associatedObject;
        }

        /// <summary>
        /// Gets a collection of attributes for the specified type of component.
        /// </summary>
        public static AttributeCollection GetAttributes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
        {
            if (componentType == null)
            {
                Debug.Fail("COMPAT:  Returning an empty collection, but you should not pass null here");
                return new AttributeCollection((Attribute[])null);
            }

            AttributeCollection attributes = GetDescriptor(componentType, nameof(componentType)).GetAttributes();
            return attributes;
        }

        /// <summary>
        /// Gets a collection of attributes for the specified component.
        /// </summary>
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public static AttributeCollection GetAttributes(object component)
        {
            return GetAttributes(component, false);
        }

        /// <summary>
        /// Gets a collection of attributes for the specified component.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public static AttributeCollection GetAttributes(object component, bool noCustomTypeDesc)
        {
            if (component == null)
            {
                Debug.Fail("COMPAT:  Returning an empty collection, but you should not pass null here");
                return new AttributeCollection(null);
            }

            // We create a sort of pipeline for mucking with metadata. The pipeline
            // goes through the following process:
            //
            // 1. Merge metadata from extenders.
            // 2. Allow services to filter the metadata
            // 3. If an attribute filter was specified, apply that.
            //
            // The goal here is speed. We get speed by not copying or
            // allocating memory. We do this by allowing each phase of the
            // pipeline to cache its data in the object cache. If
            // a phase makes a change to the results, this change must cause
            // successive phases to recompute their results as well. "Results" is
            // always a collection, and the various stages of the pipeline may
            // replace or modify this collection (depending on if it's a
            // read-only IList or not). It is possible for the original
            // descriptor or attribute collection to pass through the entire
            // pipeline without modification.
            //
            ICustomTypeDescriptor typeDesc = GetDescriptor(component, noCustomTypeDesc)!;
            ICollection results = typeDesc.GetAttributes();

            // If we are handed a custom type descriptor we have several choices of action
            // we can take. If noCustomTypeDesc is true, it means that the custom type
            // descriptor is trying to find a baseline set of properties. In this case
            // we should merge in extended properties, but we do not let designers filter
            // because we're not done with the property set yet. If noCustomTypeDesc
            // is false, we don't do extender properties because the custom type descriptor
            // has already added them. In this case, we are doing a final pass so we
            // want to apply filtering. Finally, if the incoming object is not a custom
            // type descriptor, we do extenders and the filter.
            //
            if (component is ICustomTypeDescriptor)
            {
                if (noCustomTypeDesc)
                {
                    ICustomTypeDescriptor extDesc = GetExtendedDescriptor(component);
                    if (extDesc != null)
                    {
                        ICollection extResults = extDesc.GetAttributes();
                        results = PipelineMerge(PIPELINE_ATTRIBUTES, results, extResults, null);
                    }
                }
                else
                {
                    results = PipelineFilter(PIPELINE_ATTRIBUTES, results, component, null);
                }
            }
            else
            {
                IDictionary? cache = GetCache(component);

                results = PipelineInitialize(PIPELINE_ATTRIBUTES, results, cache);

                ICustomTypeDescriptor extDesc = GetExtendedDescriptor(component);
                if (extDesc != null)
                {
                    ICollection extResults = extDesc.GetAttributes();
                    results = PipelineMerge(PIPELINE_ATTRIBUTES, results, extResults, cache);
                }

                results = PipelineFilter(PIPELINE_ATTRIBUTES, results, component, cache);
            }

            if (!(results is AttributeCollection attrs))
            {
                Attribute[] attrArray = new Attribute[results.Count];
                results.CopyTo(attrArray, 0);
                attrs = new AttributeCollection(attrArray);
            }

            return attrs;
        }

        /// <summary>
        /// Helper function to obtain a cache for the given object.
        /// </summary>
        internal static IDictionary? GetCache(object instance) => NodeFor(instance).GetCache(instance);

        /// <summary>
        /// Gets the name of the class for the specified component.
        /// </summary>
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public static string? GetClassName(object component) => GetClassName(component, false);

        /// <summary>
        /// Gets the name of the class for the specified component.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public static string? GetClassName(object component, bool noCustomTypeDesc)
        {
            return GetDescriptor(component, noCustomTypeDesc)!.GetClassName();
        }

        /// <summary>
        /// Gets the name of the class for the specified type.
        /// </summary>
        public static string? GetClassName(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
        {
            return GetDescriptor(componentType, nameof(componentType)).GetClassName();
        }

        /// <summary>
        /// The name of the class for the specified component.
        /// </summary>
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public static string? GetComponentName(object component) => GetComponentName(component, false);

        /// <summary>
        /// Gets the name of the class for the specified component.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public static string? GetComponentName(object component, bool noCustomTypeDesc)
        {
            return GetDescriptor(component, noCustomTypeDesc)!.GetComponentName();
        }

        /// <summary>
        /// Gets a type converter for the type of the specified component.
        /// </summary>
        [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage + " The Type of component cannot be statically discovered.")]
        public static TypeConverter GetConverter(object component) => GetConverter(component, false);

        /// <summary>
        /// Gets a type converter for the type of the specified component.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage + " The Type of component cannot be statically discovered.")]
        public static TypeConverter GetConverter(object component, bool noCustomTypeDesc)
        {
            TypeConverter? converter = GetDescriptor(component, noCustomTypeDesc)!.GetConverter();
            // GetDescriptor will only return DefaultTypeDescriptor, or MergedTypeDescriptor with DefaultTypeDescriptor as the secondary,
            // which will always return a non-null TypeConverter.
            Debug.Assert(converter != null, "Unexpected null TypeConverter.");
            return converter;
        }

        /// <summary>
        /// Gets a type converter for the specified type.
        /// </summary>
        [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage)]
        public static TypeConverter GetConverter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        {
            return GetDescriptor(type, nameof(type)).GetConverter();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The callers of this method ensure getting the converter is trim compatible - i.e. the type is not Nullable<T>.")]
        internal static TypeConverter GetConverterTrimUnsafe([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type) =>
            GetConverter(type);

        // This is called by System.ComponentModel.DefaultValueAttribute via reflection.
        [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage)]
        private static object? ConvertFromInvariantString([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string stringValue)
        {
            return GetConverter(type).ConvertFromInvariantString(stringValue);
        }

        /// <summary>
        /// Gets the default event for the specified type of component.
        /// </summary>
        [RequiresUnreferencedCode(EventDescriptor.RequiresUnreferencedCodeMessage)]
        public static EventDescriptor? GetDefaultEvent(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
        {
            if (componentType == null)
            {
                Debug.Fail("COMPAT:  Returning null, but you should not pass null here");
                return null;
            }

            return GetDescriptor(componentType, nameof(componentType)).GetDefaultEvent();
        }

        /// <summary>
        /// Gets the default event for the specified component.
        /// </summary>
        [RequiresUnreferencedCode(EventDescriptor.RequiresUnreferencedCodeMessage + " The Type of component cannot be statically discovered.")]
        public static EventDescriptor? GetDefaultEvent(object component) => GetDefaultEvent(component, false);

        /// <summary>
        /// Gets the default event for a component.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode(EventDescriptor.RequiresUnreferencedCodeMessage + " The Type of component cannot be statically discovered.")]
        public static EventDescriptor? GetDefaultEvent(object component, bool noCustomTypeDesc)
        {
            if (component == null)
            {
                Debug.Fail("COMPAT:  Returning null, but you should not pass null here");
                return null;
            }

            return GetDescriptor(component, noCustomTypeDesc)!.GetDefaultEvent();
        }

        /// <summary>
        /// Gets the default property for the specified type of component.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
        public static PropertyDescriptor? GetDefaultProperty(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
        {
            if (componentType == null)
            {
                Debug.Fail("COMPAT:  Returning an empty collection, but you should not pass null here");
                return null;
            }

            return GetDescriptor(componentType, nameof(componentType)).GetDefaultProperty();
        }

        /// <summary>
        /// Gets the default property for the specified component.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " The Type of component cannot be statically discovered.")]
        public static PropertyDescriptor? GetDefaultProperty(object component) => GetDefaultProperty(component, false);

        /// <summary>
        /// Gets the default property for the specified component.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " The Type of component cannot be statically discovered.")]
        public static PropertyDescriptor? GetDefaultProperty(object component, bool noCustomTypeDesc)
        {
            if (component == null)
            {
                Debug.Fail("COMPAT:  Returning null, but you should not pass null here");
                return null;
            }

            return GetDescriptor(component, noCustomTypeDesc)!.GetDefaultProperty();
        }

        /// <summary>
        /// Returns a custom type descriptor for the given type.
        /// Performs arg checking so callers don't have to.
        /// </summary>
        private static DefaultTypeDescriptor GetDescriptor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
            string typeName)
        {
            ArgumentNullException.ThrowIfNull(type, typeName);

            return NodeFor(type).GetDefaultTypeDescriptor(type);
        }

        /// <summary>
        /// Returns a custom type descriptor for the given instance.
        /// Performs arg checking so callers don't have to. This
        /// will call through to instance if it is a custom type
        /// descriptor.
        /// </summary>
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        internal static ICustomTypeDescriptor? GetDescriptor(object component, bool noCustomTypeDesc)
        {
            if (component == null)
            {
                throw new ArgumentException(nameof(component));
            }

            ICustomTypeDescriptor? desc = NodeFor(component).GetTypeDescriptor(component);
            ICustomTypeDescriptor? d = component as ICustomTypeDescriptor;
            if (!noCustomTypeDesc && d != null)
            {
                desc = new MergedTypeDescriptor(d, desc!);
            }

            return desc;
        }

        /// <summary>
        /// Returns an extended custom type descriptor for the given instance.
        /// </summary>
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        internal static ICustomTypeDescriptor GetExtendedDescriptor(object component)
        {
            if (component == null)
            {
                throw new ArgumentException(nameof(component));
            }

            return NodeFor(component).GetExtendedTypeDescriptor(component);
        }

        /// <summary>
        /// Gets an editor with the specified base type for the
        /// specified component.
        /// </summary>
        [RequiresUnreferencedCode(DesignTimeAttributeTrimmed + " The Type of component cannot be statically discovered.")]
        public static object? GetEditor(object component, Type editorBaseType)
        {
            return GetEditor(component, editorBaseType, false);
        }

        /// <summary>
        /// Gets an editor with the specified base type for the
        /// specified component.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode(DesignTimeAttributeTrimmed + " The Type of component cannot be statically discovered.")]
        public static object? GetEditor(object component, Type editorBaseType, bool noCustomTypeDesc)
        {
            ArgumentNullException.ThrowIfNull(editorBaseType);

            return GetDescriptor(component, noCustomTypeDesc)!.GetEditor(editorBaseType);
        }

        /// <summary>
        /// Gets an editor with the specified base type for the specified type.
        /// </summary>
        [RequiresUnreferencedCode(DesignTimeAttributeTrimmed)]
        public static object? GetEditor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
            Type editorBaseType)
        {
            ArgumentNullException.ThrowIfNull(editorBaseType);

            return GetDescriptor(type, nameof(type)).GetEditor(editorBaseType);
        }

        /// <summary>
        /// Gets a collection of events for a specified type of component.
        /// </summary>
        public static EventDescriptorCollection GetEvents(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
        {
            if (componentType == null)
            {
                Debug.Fail("COMPAT:  Returning an empty collection, but you should not pass null here");
                return new EventDescriptorCollection(null, true);
            }

            return GetDescriptor(componentType, nameof(componentType))!.GetEvents();
        }

        /// <summary>
        /// Gets a collection of events for a specified type of
        /// component using a specified array of attributes as a filter.
        /// </summary>
        [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public static EventDescriptorCollection GetEvents(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
            Attribute[] attributes)
        {
            if (componentType == null)
            {
                Debug.Fail("COMPAT:  Returning an empty collection, but you should not pass null here");
                return new EventDescriptorCollection(null, true);
            }

            EventDescriptorCollection events = GetDescriptor(componentType, nameof(componentType)).GetEvents(attributes);

            if (attributes != null && attributes.Length > 0)
            {
                ArrayList? filteredEvents = FilterMembers(events, attributes);
                if (filteredEvents != null)
                {
                    var descriptors = new EventDescriptor[filteredEvents.Count];
                    filteredEvents.CopyTo(descriptors);
                    events = new EventDescriptorCollection(descriptors, true);
                }
            }

            return events;
        }

        /// <summary>
        /// Gets a collection of events for a specified component.
        /// </summary>
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public static EventDescriptorCollection GetEvents(object component)
        {
            return GetEvents(component, null, false);
        }

        /// <summary>
        /// Gets a collection of events for a specified component.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public static EventDescriptorCollection GetEvents(object component, bool noCustomTypeDesc)
        {
            return GetEvents(component, null, noCustomTypeDesc);
        }

        /// <summary>
        /// Gets a collection of events for a specified component
        /// using a specified array of attributes as a filter.
        /// </summary>
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public static EventDescriptorCollection GetEvents(object component, Attribute[] attributes)
        {
            return GetEvents(component, attributes, false);
        }

        /// <summary>
        /// Gets a collection of events for a specified component
        /// using a specified array of attributes as a filter.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public static EventDescriptorCollection GetEvents(object component, Attribute[]? attributes, bool noCustomTypeDesc)
        {
            if (component == null)
            {
                Debug.Fail("COMPAT:  Returning an empty collection, but you should not pass null here");
                return new EventDescriptorCollection(null, true);
            }

            // We create a sort of pipeline for mucking with metadata. The pipeline
            // goes through the following process:
            //
            // 1. Merge metadata from extenders.
            // 2. Allow services to filter the metadata
            // 3. If an attribute filter was specified, apply that.
            //
            // The goal here is speed. We get speed by not copying or
            // allocating memory. We do this by allowing each phase of the
            // pipeline to cache its data in the object cache. If
            // a phase makes a change to the results, this change must cause
            // successive phases to recompute their results as well. "Results" is
            // always a collection, and the various stages of the pipeline may
            // replace or modify this collection (depending on if it's a
            // read-only IList or not). It is possible for the original
            // descriptor or attribute collection to pass through the entire
            // pipeline without modification.
            //
            ICustomTypeDescriptor typeDesc = GetDescriptor(component, noCustomTypeDesc)!;
            ICollection results;

            // If we are handed a custom type descriptor we have several choices of action
            // we can take. If noCustomTypeDesc is true, it means that the custom type
            // descriptor is trying to find a baseline set of events. In this case
            // we should merge in extended events, but we do not let designers filter
            // because we're not done with the event set yet. If noCustomTypeDesc
            // is false, we don't do extender events because the custom type descriptor
            // has already added them. In this case, we are doing a final pass so we
            // want to apply filtering. Finally, if the incoming object is not a custom
            // type descriptor, we do extenders and the filter.
            //
            if (component is ICustomTypeDescriptor)
            {
                results = typeDesc.GetEvents(attributes);
                if (noCustomTypeDesc)
                {
                    ICustomTypeDescriptor extDesc = GetExtendedDescriptor(component);
                    if (extDesc != null)
                    {
                        ICollection extResults = extDesc.GetEvents(attributes);
                        results = PipelineMerge(PIPELINE_EVENTS, results, extResults, null);
                    }
                }
                else
                {
                    results = PipelineFilter(PIPELINE_EVENTS, results, component, null);
                    results = PipelineAttributeFilter(PIPELINE_EVENTS, results, attributes, null);
                }
            }
            else
            {
                IDictionary? cache = GetCache(component);
                results = typeDesc.GetEvents(attributes);
                results = PipelineInitialize(PIPELINE_EVENTS, results, cache);
                ICustomTypeDescriptor extDesc = GetExtendedDescriptor(component);
                if (extDesc != null)
                {
                    ICollection extResults = extDesc.GetEvents(attributes);
                    results = PipelineMerge(PIPELINE_EVENTS, results, extResults, cache);
                }

                results = PipelineFilter(PIPELINE_EVENTS, results, component, cache);
                results = PipelineAttributeFilter(PIPELINE_EVENTS, results, attributes, cache);
            }

            if (!(results is EventDescriptorCollection evts))
            {
                EventDescriptor[] eventArray = new EventDescriptor[results.Count];
                results.CopyTo(eventArray, 0);
                evts = new EventDescriptorCollection(eventArray, true);
            }

            return evts;
        }

        /// <summary>
        /// This method is invoked during filtering when a name
        /// collision is encountered between two properties or events. This returns
        /// a suffix that can be appended to the name to make
        /// it unique. This will first attempt ot use the name of the
        /// extender. Failing that it will fall back to a static
        /// index that is continually incremented.
        /// </summary>
        private static string? GetExtenderCollisionSuffix(MemberDescriptor member)
        {
            string? suffix = null;

            ExtenderProvidedPropertyAttribute? exAttr = member.Attributes[typeof(ExtenderProvidedPropertyAttribute)] as ExtenderProvidedPropertyAttribute;
            IExtenderProvider? prov = exAttr?.Provider;

            if (prov != null)
            {
                string? name = null;

                if (prov is IComponent component && component.Site != null)
                {
                    name = component.Site.Name;
                }

                if (string.IsNullOrEmpty(name))
                {
                    int ci = System.Threading.Interlocked.Increment(ref s_collisionIndex) - 1;
                    name = ci.ToString(CultureInfo.InvariantCulture);
                }

                suffix = "_" + name;
            }

            return suffix;
        }

        /// <summary>
        /// The name of the specified component, or null if the component has no name.
        /// In many cases this will return the same value as GetComponentName. If the
        /// component resides in a nested container or has other nested semantics, it may
        /// return a different fully qualified name.
        /// </summary>
        [RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
        public static string? GetFullComponentName(object component)
        {
            ArgumentNullException.ThrowIfNull(component);

            return GetProvider(component).GetFullComponentName(component);
        }

        private static Type? GetNodeForBaseType(Type searchType)
        {
            if (searchType.IsInterface)
            {
                return InterfaceType;
            }
            else if (searchType == InterfaceType)
            {
                return null;
            }
            return searchType.BaseType;
        }

        /// <summary>
        /// Gets a collection of properties for a specified type of component.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
        public static PropertyDescriptorCollection GetProperties(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
        {
            if (componentType == null)
            {
                Debug.Fail("COMPAT:  Returning an empty collection, but you should not pass null here");
                return new PropertyDescriptorCollection(null, true);
            }

            return GetDescriptor(componentType, nameof(componentType)).GetProperties();
        }

        /// <summary>
        /// Gets a collection of properties for a specified type of
        /// component using a specified array of attributes as a filter.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public static PropertyDescriptorCollection GetProperties(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
            Attribute[]? attributes)
        {
            if (componentType == null)
            {
                Debug.Fail("COMPAT:  Returning an empty collection, but you should not pass null here");
                return new PropertyDescriptorCollection(null, true);
            }

            PropertyDescriptorCollection properties = GetDescriptor(componentType, nameof(componentType)).GetProperties(attributes);

            if (attributes != null && attributes.Length > 0)
            {
                ArrayList? filteredProperties = FilterMembers(properties, attributes);
                if (filteredProperties != null)
                {
                    var descriptors = new PropertyDescriptor[filteredProperties.Count];
                    filteredProperties.CopyTo(descriptors);
                    properties = new PropertyDescriptorCollection(descriptors, true);
                }
            }

            return properties;
        }

        /// <summary>
        /// Gets a collection of properties for a specified component.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " The Type of component cannot be statically discovered.")]
        public static PropertyDescriptorCollection GetProperties(object component)
        {
            return GetProperties(component, false);
        }

        /// <summary>
        /// Gets a collection of properties for a specified component.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " The Type of component cannot be statically discovered.")]
        public static PropertyDescriptorCollection GetProperties(object component, bool noCustomTypeDesc)
        {
            return GetPropertiesImpl(component, null, noCustomTypeDesc, true);
        }

        /// <summary>
        /// Gets a collection of properties for a specified
        /// component using a specified array of attributes
        /// as a filter.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " The Type of component cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public static PropertyDescriptorCollection GetProperties(object component, Attribute[]? attributes)
        {
            return GetProperties(component, attributes, false);
        }

        /// <summary>
        /// Gets a collection of properties for a specified
        /// component using a specified array of attributes
        /// as a filter.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " The Type of component cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public static PropertyDescriptorCollection GetProperties(object component, Attribute[]? attributes, bool noCustomTypeDesc)
        {
            return GetPropertiesImpl(component, attributes, noCustomTypeDesc, false);
        }

        /// <summary>
        /// Gets a collection of properties for a specified component. Uses the attribute filter
        /// only if noAttributes is false. This is to preserve backward compat for the case when
        /// no attribute filter was passed in (as against passing in null).
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " The Type of component cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        private static PropertyDescriptorCollection GetPropertiesImpl(object component, Attribute[]? attributes, bool noCustomTypeDesc, bool noAttributes)
        {
            if (component == null)
            {
                Debug.Fail("COMPAT:  Returning an empty collection, but you should not pass null here");
                return new PropertyDescriptorCollection(null, true);
            }

            // We create a sort of pipeline for mucking with metadata. The pipeline
            // goes through the following process:
            //
            // 1. Merge metadata from extenders.
            // 2. Allow services to filter the metadata
            // 3. If an attribute filter was specified, apply that.
            //
            // The goal here is speed. We get speed by not copying or
            // allocating memory. We do this by allowing each phase of the
            // pipeline to cache its data in the object cache. If
            // a phase makes a change to the results, this change must cause
            // successive phases to recompute their results as well. "Results" is
            // always a collection, and the various stages of the pipeline may
            // replace or modify this collection (depending on if it's a
            // read-only IList or not). It is possible for the original
            // descriptor or attribute collection to pass through the entire
            // pipeline without modification.
            //
            ICustomTypeDescriptor typeDesc = GetDescriptor(component, noCustomTypeDesc)!;
            ICollection results;

            // If we are handed a custom type descriptor we have several choices of action
            // we can take. If noCustomTypeDesc is true, it means that the custom type
            // descriptor is trying to find a baseline set of properties. In this case
            // we should merge in extended properties, but we do not let designers filter
            // because we're not done with the property set yet. If noCustomTypeDesc
            // is false, we don't do extender properties because the custom type descriptor
            // has already added them. In this case, we are doing a final pass so we
            // want to apply filtering. Finally, if the incoming object is not a custom
            // type descriptor, we do extenders and the filter.
            //
            if (component is ICustomTypeDescriptor)
            {
                results = noAttributes ? typeDesc.GetProperties() : typeDesc.GetProperties(attributes);
                if (noCustomTypeDesc)
                {
                    ICustomTypeDescriptor extDesc = GetExtendedDescriptor(component);
                    if (extDesc != null)
                    {
                        ICollection extResults = noAttributes ? extDesc.GetProperties() : extDesc.GetProperties(attributes);
                        results = PipelineMerge(PIPELINE_PROPERTIES, results, extResults, null);
                    }
                }
                else
                {
                    results = PipelineFilter(PIPELINE_PROPERTIES, results, component, null);
                    results = PipelineAttributeFilter(PIPELINE_PROPERTIES, results, attributes, null);
                }
            }
            else
            {
                IDictionary? cache = GetCache(component);
                results = noAttributes ? typeDesc.GetProperties() : typeDesc.GetProperties(attributes);
                results = PipelineInitialize(PIPELINE_PROPERTIES, results, cache);
                ICustomTypeDescriptor extDesc = GetExtendedDescriptor(component);
                if (extDesc != null)
                {
                    ICollection extResults = noAttributes ? extDesc.GetProperties() : extDesc.GetProperties(attributes);
                    results = PipelineMerge(PIPELINE_PROPERTIES, results, extResults, cache);
                }

                results = PipelineFilter(PIPELINE_PROPERTIES, results, component, cache);
                results = PipelineAttributeFilter(PIPELINE_PROPERTIES, results, attributes, cache);
            }

            if (!(results is PropertyDescriptorCollection props))
            {
                PropertyDescriptor[] propArray = new PropertyDescriptor[results.Count];
                results.CopyTo(propArray, 0);
                props = new PropertyDescriptorCollection(propArray, true);
            }

            return props;
        }

        /// <summary>
        /// The GetProvider method returns a type description provider for
        /// the given object or type. This will always return a type description
        /// provider. Even the default TypeDescriptor implementation is built on
        /// a TypeDescriptionProvider, and this will be returned unless there is
        /// another provider that someone else has added.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static TypeDescriptionProvider GetProvider(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return NodeFor(type, true);
        }

        /// <summary>
        /// The GetProvider method returns a type description provider for
        /// the given object or type. This will always return a type description
        /// provider. Even the default TypeDescriptor implementation is built on
        /// a TypeDescriptionProvider, and this will be returned unless there is
        /// another provider that someone else has added.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static TypeDescriptionProvider GetProvider(object instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            return NodeFor(instance, true);
        }

        /// <summary>
        /// This method returns a type description provider, but instead of creating
        /// a delegating provider for the type, this will walk all base types until
        /// it locates a provider. The provider returned cannot be cached. This
        /// method is used by the DelegatingTypeDescriptionProvider to efficiently
        /// locate the provider to delegate to.
        /// </summary>
        internal static TypeDescriptionProvider GetProviderRecursive(Type type)
        {
            return NodeFor(type, false);
        }

        /// <summary>
        /// Returns an Type instance that can be used to perform reflection.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [return: DynamicallyAccessedMembers(ReflectTypesDynamicallyAccessedMembers)]
        public static Type GetReflectionType([DynamicallyAccessedMembers(ReflectTypesDynamicallyAccessedMembers)] Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return NodeFor(type).GetReflectionType(type);
        }

        /// <summary>
        /// Returns an Type instance that can be used to perform reflection.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [RequiresUnreferencedCode("GetReflectionType is not trim compatible because the Type of object cannot be statically discovered.")]
        public static Type GetReflectionType(object instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            return NodeFor(instance).GetReflectionType(instance);
        }

        /// <summary>
        /// Retrieves the head type description node for a type.
        /// A head node pointing to a reflection based type description
        /// provider will be created on demand. This does not create
        /// a delegator, in which case the node returned may be
        /// a base type node.
        /// </summary>
        private static TypeDescriptionNode NodeFor(Type type) => NodeFor(type, false);

        /// <summary>
        /// Retrieves the head type description node for a type.
        /// A head node pointing to a reflection based type description
        /// provider will be created on demand.
        ///
        /// If createDelegator is true, this method will create a delegation
        /// node for a type if the type has no node of its own. Delegation
        /// nodes should be created if you are going to hand this node
        /// out to a user. Without a delegation node, user code could
        /// skip providers that are added after their call. Delegation
        /// nodes solve that problem.
        ///
        /// If createDelegator is false, this method will recurse up the
        /// base type chain looking for nodes.
        /// </summary>
        private static TypeDescriptionNode NodeFor(Type type, bool createDelegator)
        {
            Debug.Assert(type != null, "Caller should validate");
            CheckDefaultProvider(type);

            // First, check our provider type table to see if we have a matching
            // provider for this type. The provider type table is a cache that
            // matches types to providers. When a new provider is added or
            // an existing one removed, the provider type table is torn
            // down and automatically rebuilt on demand.
            //
            TypeDescriptionNode? node = null;
            Type searchType = type;

            while (node == null)
            {
                node = (TypeDescriptionNode?)s_providerTypeTable[searchType] ??
                       (TypeDescriptionNode?)s_providerTable[searchType];

                if (node == null)
                {
                    Type? baseType = GetNodeForBaseType(searchType);

                    if (searchType == typeof(object) || baseType == null)
                    {
                        lock (s_providerTable)
                        {
                            node = (TypeDescriptionNode?)s_providerTable[searchType];

                            if (node == null)
                            {
                                // The reflect type description provider is a default provider that
                                // can provide type information for all objects.
                                node = new TypeDescriptionNode(new ReflectTypeDescriptionProvider());
                                s_providerTable[searchType] = node;
                            }
                        }
                    }
                    else if (createDelegator)
                    {
                        node = new TypeDescriptionNode(new DelegatingTypeDescriptionProvider(baseType));
                        lock (s_providerTable)
                        {
                            s_providerTypeTable[searchType] = node;
                        }
                    }
                    else
                    {
                        // Continue our search
                        searchType = baseType;
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// Retrieves the head type description node for an instance.
        /// Instance-based node lists are rare. If a node list is not
        /// available for a given instance, this will return the head node
        /// for the instance's type.
        /// </summary>
        private static TypeDescriptionNode NodeFor(object instance) => NodeFor(instance, false);

        /// <summary>
        /// Retrieves the head type description node for an instance.
        /// Instance-based node lists are rare. If a node list is not
        /// available for a given instance, this will return the head node
        /// for the instance's type. This variation offers a bool called
        /// createDelegator. If true and there is no node list for this
        /// instance, NodeFor will create a temporary "delegator node" that,
        /// when queried, will delegate to the type stored in the instance.
        /// This is done on demand, which means if someone else added a
        /// type description provider for the instance's type the delegator
        /// would pick up the new type. If a query is being made that does
        /// not involve publicly exposing the type description provider for
        /// the instance, the query should pass in false (the default) for
        /// createDelegator because no object will be created.
        /// </summary>
        private static TypeDescriptionNode NodeFor(object instance, bool createDelegator)
        {
            // For object instances, the provider cache key is not the object (that
            // would keep it in memory). Instead, it is a subclass of WeakReference
            // that overrides GetHashCode and Equals to make it appear to be the
            // object it is wrapping. A GC'd object causes WeakReference to return
            // false for all .Equals, but it always returns a valid hash code.

            Debug.Assert(instance != null, "Caller should validate");

            TypeDescriptionNode? node = (TypeDescriptionNode?)s_providerTable[instance];
            if (node == null)
            {
                Type type = instance.GetType();

                if (type.IsCOMObject)
                {
                    type = ComObjectType;
                }
                else if (OperatingSystem.IsWindows()
                    && ComWrappers.TryGetComInstance(instance, out nint unknown))
                {
                    // ComObjectType uses the Windows Forms provided ComNativeDescriptor. It currently has hard Win32
                    // API dependencies. Even though ComWrappers work with other platforms, restricting to Windows until
                    // such time that the ComNativeDescriptor can handle basic COM types on other platforms.
                    //
                    // Tracked with https://github.com/dotnet/winforms/issues/9291
                    Marshal.Release(unknown);
                    type = ComObjectType;
                }

                if (createDelegator)
                {
                    node = new TypeDescriptionNode(new DelegatingTypeDescriptionProvider(type));
                }
                else
                {
                    node = NodeFor(type);
                }
            }

            return node;
        }

        /// <summary>
        /// Simple linked list code to remove an element
        /// from the list. Returns the new head to the
        /// list. If the head points to an instance of
        /// DelegatingTypeDescriptionProvider, we clear the
        /// node because all it is doing is delegating elsewhere.
        ///
        /// Note that this behaves a little differently from normal
        /// linked list code. In a normal linked list, you remove
        /// then target node and fixup the links. In this linked
        /// list, we remove the node AFTER the target node, fixup
        /// the links, and fixup the underlying providers that each
        /// node references. The reason for this is that most
        /// providers keep a reference to the previous provider,
        /// which is exposed as one of these nodes. Therefore,
        /// to remove a provider the node following is most likely
        /// referenced by that provider
        /// </summary>
        private static void NodeRemove(object key, TypeDescriptionProvider provider)
        {
            lock (s_providerTable)
            {
                TypeDescriptionNode? head = (TypeDescriptionNode?)s_providerTable[key];
                TypeDescriptionNode? target = head;

                while (target != null && target.Provider != provider)
                {
                    target = target.Next;
                }

                if (target != null)
                {
                    // We have our target node. There are three cases
                    // to consider:  the target is in the middle, the head,
                    // or the end.

                    if (target.Next != null)
                    {
                        // If there is a node after the target node,
                        // steal the node's provider and store it
                        // at the target location. This removes
                        // the provider at the target location without
                        // the need to modify providers which may be
                        // pointing to "target".
                        target.Provider = target.Next.Provider;

                        // Now remove target.Next from the list
                        target.Next = target.Next.Next;

                        // If the new provider we got is a delegating
                        // provider, we can remove this node from
                        // the list. The delegating provider should
                        // always be at the end of the node list.
                        if (target == head && target.Provider is DelegatingTypeDescriptionProvider)
                        {
                            Debug.Assert(target.Next == null, "Delegating provider should always be the last provider in the chain.");
                            s_providerTable.Remove(key);
                        }
                    }
                    else if (target != head)
                    {
                        // If target is the last node, we can't
                        // assign a new provider over to it. What
                        // we can do, however, is assign a delegating
                        // provider into the target node. This routes
                        // requests from the previous provider into
                        // the next base type provider list.

                        // We don't do this if the target is the head.
                        // In that case, we can remove the node
                        // altogether since no one is pointing to it.

                        Type keyType = key as Type ?? key.GetType();

                        target.Provider = new DelegatingTypeDescriptionProvider(keyType.BaseType!);
                    }
                    else
                    {
                        s_providerTable.Remove(key);
                    }

                    // Finally, clear our cache of provider types; it might be invalid
                    // now.
                    s_providerTypeTable.Clear();
                }
            }
        }

        /// <summary>
        /// This is the last stage in our filtering pipeline. Here, we apply any
        /// user-defined filter.
        /// </summary>
        [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        private static ICollection PipelineAttributeFilter(int pipelineType, ICollection members, Attribute[]? filter, IDictionary? cache)
        {
            Debug.Assert(pipelineType != PIPELINE_ATTRIBUTES, "PipelineAttributeFilter is not supported for attributes");

            IList? list = members as ArrayList;

            if (filter == null || filter.Length == 0)
            {
                return members;
            }

            // Now, check our cache. The cache state is only valid
            // if the data coming into us is read-only. If it is read-write,
            // that means something higher in the pipeline has already changed
            // it so we must recompute anyway.
            //
            if (cache != null && (list == null || list.IsReadOnly))
            {
                if (cache[s_pipelineAttributeFilterKeys[pipelineType]] is AttributeFilterCacheItem filterCache && filterCache.IsValid(filter))
                {
                    return filterCache.FilteredMembers;
                }
            }

            // Our cache did not contain the correct state, so generate it.
            //
            if (list == null || list.IsReadOnly)
            {
                list = new ArrayList(members);
            }

            ArrayList? filterResult = FilterMembers(list, filter);
            if (filterResult != null) list = filterResult;

            // And, if we have a cache, store the updated state into it for future reference.
            //
            if (cache != null)
            {
                ICollection cacheValue;

                switch (pipelineType)
                {
                    case PIPELINE_PROPERTIES:
                        PropertyDescriptor[] propArray = new PropertyDescriptor[list.Count];
                        list.CopyTo(propArray, 0);
                        cacheValue = new PropertyDescriptorCollection(propArray, true);
                        break;

                    case PIPELINE_EVENTS:
                        EventDescriptor[] eventArray = new EventDescriptor[list.Count];
                        list.CopyTo(eventArray, 0);
                        cacheValue = new EventDescriptorCollection(eventArray, true);
                        break;

                    default:
                        Debug.Fail("unknown pipeline type");
                        cacheValue = null;
                        break;
                }

                AttributeFilterCacheItem filterCache = new AttributeFilterCacheItem(filter, cacheValue);
                cache[s_pipelineAttributeFilterKeys[pipelineType]] = filterCache;
            }

            return list;
        }

        /// <summary>
        /// Metdata filtering is the third stage of our pipeline.
        /// In this stage we check to see if the given object is a
        /// sited component that provides the ITypeDescriptorFilterService
        /// object. If it does, we allow the TDS to filter the metadata.
        /// This will use the cache, if available, to store filtered
        /// metdata.
        /// </summary>
        private static ICollection PipelineFilter(int pipelineType, ICollection members, object instance, IDictionary? cache)
        {
            IComponent? component = instance as IComponent;
            ITypeDescriptorFilterService? componentFilter = null;

            ISite? site = component?.Site;
            if (site != null)
            {
                componentFilter = site.GetService(typeof(ITypeDescriptorFilterService)) as ITypeDescriptorFilterService;
            }

            // If we have no filter, there is nothing for us to do.
            //
            IList? list = members as ArrayList;

            if (componentFilter == null)
            {
                Debug.Assert(cache == null || list == null || !cache.Contains(s_pipelineFilterKeys[pipelineType]), "Earlier pipeline stage should have removed our cache");
                return members;
            }

            // Now, check our cache. The cache state is only valid
            // if the data coming into us is read-only. If it is read-write,
            // that means something higher in the pipeline has already changed
            // it so we must recompute anyway.
            //
            if (cache != null && (list == null || list.IsReadOnly))
            {
                if (cache[s_pipelineFilterKeys[pipelineType]] is FilterCacheItem cacheItem && cacheItem.IsValid(componentFilter))
                {
                    return cacheItem.FilteredMembers;
                }
            }

            // Cache either is dirty or doesn't exist. Re-filter the members.
            // We need to build an IDictionary of key->value pairs and invoke
            // Filter* on the filter service.
            //
            OrderedDictionary filterTable = new OrderedDictionary(members.Count);
            bool cacheResults;

            switch (pipelineType)
            {
                case PIPELINE_ATTRIBUTES:
                    foreach (Attribute attr in members)
                    {
                        filterTable[attr.TypeId] = attr;
                    }
                    cacheResults = componentFilter.FilterAttributes(component!, filterTable);
                    break;

                case PIPELINE_PROPERTIES:
                case PIPELINE_EVENTS:
                    foreach (MemberDescriptor desc in members)
                    {
                        string descName = desc.Name;
                        // We must handle the case of duplicate property names
                        // because extender providers can provide any arbitrary
                        // name. Our rule for this is simple:  If we find a
                        // duplicate name, resolve it back to the extender
                        // provider that offered it and append "_" + the
                        // provider name. If the provider has no name,
                        // then append the object hash code.
                        //
                        if (filterTable.Contains(descName))
                        {
                            // First, handle the new property. Because
                            // of the order in which we added extended
                            // properties earlier in the pipeline, we can be
                            // sure that the new property is an extender. We
                            // cannot be sure that the existing property
                            // in the table is an extender, so we will
                            // have to check.
                            //
                            string? suffix = GetExtenderCollisionSuffix(desc);
                            Debug.Assert(suffix != null, "Name collision with non-extender property.");
                            if (suffix != null)
                            {
                                filterTable[descName + suffix] = desc;
                            }

                            // Now, handle the original property.
                            //
                            MemberDescriptor origDesc = (MemberDescriptor)filterTable[descName]!;
                            suffix = GetExtenderCollisionSuffix(origDesc);
                            if (suffix != null)
                            {
                                filterTable.Remove(descName);
                                filterTable[origDesc.Name + suffix] = origDesc;
                            }
                        }
                        else
                        {
                            filterTable[descName] = desc;
                        }
                    }
                    if (pipelineType == PIPELINE_PROPERTIES)
                    {
                        cacheResults = componentFilter.FilterProperties(component!, filterTable);
                    }
                    else
                    {
                        cacheResults = componentFilter.FilterEvents(component!, filterTable);
                    }
                    break;

                default:
                    Debug.Fail("unknown pipeline type");
                    cacheResults = false;
                    break;
            }

            // See if we can re-use the IList that was passed. If we can,
            // it is more efficient to re-use its slots than to generate new ones.
            if (list == null || list.IsReadOnly)
            {
                list = new ArrayList(filterTable.Values);
            }
            else
            {
                list.Clear();
                foreach (object obj in filterTable.Values)
                {
                    list.Add(obj);
                }
            }

            // Component filter has requested that we cache these
            // new changes. We store them as a correctly typed collection
            // so on successive invocations we can simply return. Note that
            // we always return the IList so that successive stages in the
            // pipeline can modify it.
            if (cacheResults && cache != null)
            {
                ICollection cacheValue;

                switch (pipelineType)
                {
                    case PIPELINE_ATTRIBUTES:
                        Attribute[] attrArray = new Attribute[list.Count];
                        try
                        {
                            list.CopyTo(attrArray, 0);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(SR.Format(SR.TypeDescriptorExpectedElementType, typeof(Attribute).FullName));
                        }
                        cacheValue = new AttributeCollection(attrArray);
                        break;

                    case PIPELINE_PROPERTIES:
                        PropertyDescriptor[] propArray = new PropertyDescriptor[list.Count];
                        try
                        {
                            list.CopyTo(propArray, 0);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(SR.Format(SR.TypeDescriptorExpectedElementType, typeof(PropertyDescriptor).FullName));
                        }
                        cacheValue = new PropertyDescriptorCollection(propArray, true);
                        break;

                    case PIPELINE_EVENTS:
                        EventDescriptor[] eventArray = new EventDescriptor[list.Count];
                        try
                        {
                            list.CopyTo(eventArray, 0);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(SR.Format(SR.TypeDescriptorExpectedElementType, typeof(EventDescriptor).FullName));
                        }
                        cacheValue = new EventDescriptorCollection(eventArray, true);
                        break;

                    default:
                        Debug.Fail("unknown pipeline type");
                        cacheValue = null;
                        break;
                }

                FilterCacheItem cacheItem = new FilterCacheItem(componentFilter, cacheValue);
                cache[s_pipelineFilterKeys[pipelineType]] = cacheItem;
                cache.Remove(s_pipelineAttributeFilterKeys[pipelineType]);
            }

            return list;
        }

        /// <summary>
        /// This is the first stage in the pipeline. This checks the incoming member collection and if it
        /// differs from what we have seen in the past, it invalidates all successive pipelines.
        /// </summary>
        private static ICollection PipelineInitialize(int pipelineType, ICollection members, IDictionary? cache)
        {
            if (cache != null)
            {
                bool cacheValid = true;

                if (cache[s_pipelineInitializeKeys[pipelineType]] is ICollection cachedMembers && cachedMembers.Count == members.Count)
                {
                    IEnumerator cacheEnum = cachedMembers.GetEnumerator();
                    IEnumerator memberEnum = members.GetEnumerator();

                    while (cacheEnum.MoveNext() && memberEnum.MoveNext())
                    {
                        if (cacheEnum.Current != memberEnum.Current)
                        {
                            cacheValid = false;
                            break;
                        }
                    }
                }

                if (!cacheValid)
                {
                    // The cache wasn't valid. Remove all subsequent cache layers
                    // and then save off new data.
                    cache.Remove(s_pipelineMergeKeys[pipelineType]);
                    cache.Remove(s_pipelineFilterKeys[pipelineType]);
                    cache.Remove(s_pipelineAttributeFilterKeys[pipelineType]);
                    cache[s_pipelineInitializeKeys[pipelineType]] = members;
                }
            }

            return members;
        }

        /// <summary>
        /// Metadata merging is the second stage of our metadata pipeline. This stage
        /// merges extended metdata with primary metadata, and stores it in
        /// the cache if it is available.
        /// </summary>
        private static ICollection PipelineMerge(int pipelineType, ICollection primary, ICollection secondary, IDictionary? cache)
        {
            // If there is no secondary collection, there is nothing to merge.
            if (secondary == null || secondary.Count == 0)
            {
                return primary;
            }

            // Next, if we were given a cache, see if it has accurate data.
            if (cache?[s_pipelineMergeKeys[pipelineType]] is ICollection mergeCache && mergeCache.Count == (primary.Count + secondary.Count))
            {
                // Walk the merge cache.
                IEnumerator mergeEnum = mergeCache.GetEnumerator();
                IEnumerator primaryEnum = primary.GetEnumerator();
                bool match = true;

                while (primaryEnum.MoveNext() && mergeEnum.MoveNext())
                {
                    if (primaryEnum.Current != mergeEnum.Current)
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    IEnumerator secondaryEnum = secondary.GetEnumerator();

                    while (secondaryEnum.MoveNext() && mergeEnum.MoveNext())
                    {
                        if (secondaryEnum.Current != mergeEnum.Current)
                        {
                            match = false;
                            break;
                        }
                    }
                }

                if (match)
                {
                    return mergeCache;
                }
            }

            // Our cache didn't match. We need to merge metadata and return
            // the merged copy. We create an array list here, rather than
            // an array, because we want successive sections of the
            // pipeline to be able to modify it.
            ArrayList list = new ArrayList(primary.Count + secondary.Count);
            foreach (object obj in primary)
            {
                list.Add(obj);
            }
            foreach (object obj in secondary)
            {
                list.Add(obj);
            }

            if (cache != null)
            {
                ICollection cacheValue;

                switch (pipelineType)
                {
                    case PIPELINE_ATTRIBUTES:
                        Attribute[] attrArray = new Attribute[list.Count];
                        list.CopyTo(attrArray, 0);
                        cacheValue = new AttributeCollection(attrArray);
                        break;

                    case PIPELINE_PROPERTIES:
                        PropertyDescriptor[] propArray = new PropertyDescriptor[list.Count];
                        list.CopyTo(propArray, 0);
                        cacheValue = new PropertyDescriptorCollection(propArray, true);
                        break;

                    case PIPELINE_EVENTS:
                        EventDescriptor[] eventArray = new EventDescriptor[list.Count];
                        list.CopyTo(eventArray, 0);
                        cacheValue = new EventDescriptorCollection(eventArray, true);
                        break;

                    default:
                        Debug.Fail("unknown pipeline type");
                        cacheValue = null;
                        break;
                }

                cache[s_pipelineMergeKeys[pipelineType]] = cacheValue;
                cache.Remove(s_pipelineFilterKeys[pipelineType]);
                cache.Remove(s_pipelineAttributeFilterKeys[pipelineType]);
            }

            return list;
        }

        private static void RaiseRefresh(object component)
        {
            // This volatility prevents the JIT from making certain optimizations
            // that could cause this firing pattern to break. Although the likelihood
            // the JIT makes those changes is mostly theoretical
            RefreshEventHandler? handler = Volatile.Read(ref Refreshed);

            handler?.Invoke(new RefreshEventArgs(component));
        }

        private static void RaiseRefresh(Type type)
        {
            RefreshEventHandler? handler = Volatile.Read(ref Refreshed);

            handler?.Invoke(new RefreshEventArgs(type));
        }

        /// <summary>
        /// Clears the properties and events for the specified
        /// component from the cache.
        /// </summary>
        public static void Refresh(object component) => Refresh(component, refreshReflectionProvider: true);

        private static void Refresh(object component, bool refreshReflectionProvider)
        {
            if (component == null)
            {
                Debug.Fail("COMPAT:  Returning, but you should not pass null here");
                return;
            }

            // Build up a list of type description providers for
            // each type that is a derived type of the given
            // object. We will invalidate the metadata at
            // each of these levels.
            bool found = false;

            if (refreshReflectionProvider)
            {
                Type type = component.GetType();

                lock (s_providerTable)
                {
                    // ReflectTypeDescritionProvider is only bound to object, but we
                    // need go to through the entire table to try to find custom
                    // providers. If we find one, will clear our cache.
                    // Manual use of IDictionaryEnumerator instead of foreach to avoid
                    // DictionaryEntry box allocations.
                    IDictionaryEnumerator e = s_providerTable.GetEnumerator();
                    while (e.MoveNext())
                    {
                        DictionaryEntry de = e.Entry;
                        Type? nodeType = de.Key as Type;
                        if (nodeType != null && type.IsAssignableFrom(nodeType) || nodeType == typeof(object))
                        {
                            TypeDescriptionNode? node = (TypeDescriptionNode?)de.Value;
                            while (node != null && !(node.Provider is ReflectTypeDescriptionProvider))
                            {
                                found = true;
                                node = node.Next;
                            }

                            if (node != null)
                            {
                                ReflectTypeDescriptionProvider provider = (ReflectTypeDescriptionProvider)node.Provider;
                                if (provider.IsPopulated(type))
                                {
                                    found = true;
                                    provider.Refresh(type);
                                }
                            }
                        }
                    }
                }
            }

            // We need to clear our filter even if no typedescriptionprovider had data.
            // This is because if you call Refresh(instance1) and Refresh(instance2)
            // and instance1 and instance2 are of the same type, you will end up not
            // actually deleting the dictionary cache on instance2 if you skip this
            // when you don't find a typedescriptionprovider.
            // However, we do not need to fire the event if we did not find any loaded
            // typedescriptionprovider AND the cache is empty (if someone repeatedly calls
            // Refresh on an instance).

            // Now, clear any cached data for the instance.
            IDictionary? cache = GetCache(component);
            if (found || cache != null)
            {
                if (cache != null)
                {
                    for (int idx = 0; idx < s_pipelineFilterKeys.Length; idx++)
                    {
                        cache.Remove(s_pipelineFilterKeys[idx]);
                        cache.Remove(s_pipelineMergeKeys[idx]);
                        cache.Remove(s_pipelineAttributeFilterKeys[idx]);
                    }
                }

                Interlocked.Increment(ref s_metadataVersion);

                // And raise the event.
                RaiseRefresh(component);
            }
        }

        /// <summary>
        /// Clears the properties and events for the specified type
        /// of component from the cache.
        /// </summary>
        public static void Refresh(Type type)
        {
            if (type == null)
            {
                Debug.Fail("COMPAT:  Returning, but you should not pass null here");
                return;
            }

            // Build up a list of type description providers for
            // each type that is a derived type of the given
            // type. We will invalidate the metadata at
            // each of these levels.

            bool found = false;

            lock (s_providerTable)
            {
                // ReflectTypeDescritionProvider is only bound to object, but we
                // need go to through the entire table to try to find custom
                // providers. If we find one, will clear our cache.
                // Manual use of IDictionaryEnumerator instead of foreach to avoid
                // DictionaryEntry box allocations.
                IDictionaryEnumerator e = s_providerTable.GetEnumerator();
                while (e.MoveNext())
                {
                    DictionaryEntry de = e.Entry;
                    Type? nodeType = de.Key as Type;
                    if (nodeType != null && type.IsAssignableFrom(nodeType) || nodeType == typeof(object))
                    {
                        TypeDescriptionNode? node = (TypeDescriptionNode?)de.Value;
                        while (node != null && !(node.Provider is ReflectTypeDescriptionProvider))
                        {
                            found = true;
                            node = node.Next;
                        }

                        if (node != null)
                        {
                            ReflectTypeDescriptionProvider provider = (ReflectTypeDescriptionProvider)node.Provider;
                            if (provider.IsPopulated(type))
                            {
                                found = true;
                                provider.Refresh(type);
                            }
                        }
                    }
                }
            }

            // We only clear our filter and fire the refresh event if there was one or
            // more type description providers that were populated with metdata.
            // This prevents us from doing a lot of extra work and raising
            // a ton more events than we need to.
            if (found)
            {
                Interlocked.Increment(ref s_metadataVersion);

                // And raise the event.
                RaiseRefresh(type);
            }
        }

        /// <summary>
        /// Clears the properties and events for the specified
        /// module from the cache.
        /// </summary>
        public static void Refresh(Module module)
        {
            if (module == null)
            {
                Debug.Fail("COMPAT:  Returning, but you should not pass null here");
                return;
            }

            // Build up a list of type description providers for
            // each type that is a derived type of the given
            // object. We will invalidate the metadata at
            // each of these levels.
            Hashtable? refreshedTypes = null;

            lock (s_providerTable)
            {
                // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                IDictionaryEnumerator e = s_providerTable.GetEnumerator();
                while (e.MoveNext())
                {
                    DictionaryEntry de = e.Entry;
                    Type? nodeType = de.Key as Type;
                    if (nodeType != null && nodeType.Module.Equals(module) || nodeType == typeof(object))
                    {
                        TypeDescriptionNode? node = (TypeDescriptionNode?)de.Value;
                        while (node != null && !(node.Provider is ReflectTypeDescriptionProvider))
                        {
                            refreshedTypes ??= new Hashtable();
                            refreshedTypes[nodeType] = nodeType;
                            node = node.Next;
                        }

                        if (node != null)
                        {
                            ReflectTypeDescriptionProvider provider = (ReflectTypeDescriptionProvider)node.Provider;
                            Type[] populatedTypes = provider.GetPopulatedTypes(module);

                            foreach (Type populatedType in populatedTypes)
                            {
                                provider.Refresh(populatedType);
                                refreshedTypes ??= new Hashtable();
                                refreshedTypes[populatedType] = populatedType;
                            }
                        }
                    }
                }
            }

            // And raise the event if types were refresh and handlers are attached.
            if (refreshedTypes != null && Refreshed != null)
            {
                foreach (Type t in refreshedTypes.Keys)
                {
                    RaiseRefresh(t);
                }
            }
        }

        /// <summary>
        /// Clears the properties and events for the specified
        /// assembly from the cache.
        /// </summary>
        public static void Refresh(Assembly assembly)
        {
            if (assembly == null)
            {
                Debug.Fail("COMPAT:  Returning, but you should not pass null here");
                return;
            }

            foreach (Module mod in assembly.GetModules())
            {
                Refresh(mod);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Type ComObjectType
        {
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            get => typeof(TypeDescriptorComObject);
        }

        [RequiresUnreferencedCode(DesignTimeAttributeTrimmed)]
        public static IDesigner? CreateDesigner(IComponent component, Type designerBaseType)
        {
            Type? type = null;
            IDesigner? result = null;
            AttributeCollection attributes = GetAttributes(component);
            for (int i = 0; i < attributes.Count; i++)
            {
                if (attributes[i] is DesignerAttribute designerAttribute)
                {
                    Type? type2 = Type.GetType(designerAttribute.DesignerBaseTypeName);
                    if (type2 != null && type2 == designerBaseType)
                    {
                        ISite? site = component.Site;
                        bool flag = false;
                        ITypeResolutionService? typeResolutionService = (ITypeResolutionService?)site?.GetService(typeof(ITypeResolutionService));
                        if (typeResolutionService != null)
                        {
                            flag = true;
                            type = typeResolutionService.GetType(designerAttribute.DesignerTypeName);
                        }
                        if (!flag)
                        {
                            type = Type.GetType(designerAttribute.DesignerTypeName);
                        }
                        if (type != null)
                        {
                            break;
                        }
                    }
                }
            }
            if (type != null)
            {
                result = (IDesigner?)Activator.CreateInstance(type);
            }
            return result;
        }

        [Obsolete("TypeDescriptor.ComNativeDescriptorHandler has been deprecated. Use a type description provider to supply type information for COM types instead.")]
        [DisallowNull]
        public static IComNativeDescriptorHandler? ComNativeDescriptorHandler
        {
            get
            {
                TypeDescriptionNode? typeDescriptionNode = NodeFor(ComObjectType);
                ComNativeDescriptionProvider? comNativeDescriptionProvider;
                do
                {
                    comNativeDescriptionProvider = (typeDescriptionNode.Provider as ComNativeDescriptionProvider);
                    typeDescriptionNode = typeDescriptionNode.Next;
                }
                while (typeDescriptionNode != null && comNativeDescriptionProvider == null);
                return comNativeDescriptionProvider?.Handler;
            }
            set
            {
                TypeDescriptionNode? typeDescriptionNode = NodeFor(ComObjectType);
                while (typeDescriptionNode != null && !(typeDescriptionNode.Provider is ComNativeDescriptionProvider))
                {
                    typeDescriptionNode = typeDescriptionNode.Next;
                }
                if (typeDescriptionNode == null)
                {
                    AddProvider(new ComNativeDescriptionProvider(value), ComObjectType);
                    return;
                }
                ((ComNativeDescriptionProvider)typeDescriptionNode.Provider).Handler = value;
            }
        }

        /// <summary>
        /// The RemoveAssociation method removes an association with an object.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void RemoveAssociation(object primary, object secondary)
        {
            ArgumentNullException.ThrowIfNull(primary);
            ArgumentNullException.ThrowIfNull(secondary);

            Hashtable assocTable = AssociationTable;
            IList? associations = (IList?)assocTable?[primary];
            if (associations != null)
            {
                lock (associations)
                {
                    for (int idx = associations.Count - 1; idx >= 0; idx--)
                    {
                        // Look for an associated object that has a type that
                        // matches the given type.
                        WeakReference weakRef = (WeakReference)associations[idx]!;
                        object? secondaryItem = weakRef.Target;
                        if (secondaryItem == null || secondaryItem == secondary)
                        {
                            associations.RemoveAt(idx);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The RemoveAssociations method removes all associations for a primary object.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void RemoveAssociations(object primary)
        {
            ArgumentNullException.ThrowIfNull(primary);

            AssociationTable?.Remove(primary);
        }

        /// <summary>
        /// The RemoveProvider method removes a previously added type
        /// description provider. Removing a provider causes a Refresh
        /// event to be raised for the object or type the provider is
        /// associated with.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void RemoveProvider(TypeDescriptionProvider provider, Type type)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(type);

            // Walk the nodes until we find the right one, and then remove it.
            NodeRemove(type, provider);
            RaiseRefresh(type);
        }

        /// <summary>
        /// The RemoveProvider method removes a previously added type
        /// description provider. Removing a provider causes a Refresh
        /// event to be raised for the object or type the provider is
        /// associated with.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void RemoveProvider(TypeDescriptionProvider provider, object instance)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(instance);

            // Walk the nodes until we find the right one, and then remove it.
            NodeRemove(instance, provider);
            RaiseRefresh(instance);
        }


        /// <summary>
        /// The RemoveProvider method removes a previously added type
        /// description provider. Removing a provider causes a Refresh
        /// event to be raised for the object or type the provider is
        /// associated with.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void RemoveProviderTransparent(TypeDescriptionProvider provider, Type type)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(type);

            RemoveProvider(provider, type);
        }

        /// <summary>
        /// The RemoveProvider method removes a previously added type
        /// description provider. Removing a provider causes a Refresh
        /// event to be raised for the object or type the provider is
        /// associated with.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static void RemoveProviderTransparent(TypeDescriptionProvider provider, object instance)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(instance);

            RemoveProvider(provider, instance);
        }

        /// <summary>
        /// This function takes a member descriptor and an attribute and determines whether
        /// the member satisfies the particular attribute. This either means that the member
        /// contains the attribute or the member does not contain the attribute and the default
        /// for the attribute matches the passed in attribute.
        /// </summary>
        [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        private static bool ShouldHideMember(MemberDescriptor? member, Attribute? attribute)
        {
            if (member == null || attribute == null)
            {
                return true;
            }

            Attribute? memberAttribute = member.Attributes[attribute.GetType()];
            if (memberAttribute == null)
            {
                return !attribute.IsDefaultAttribute();
            }
            else
            {
                return !attribute.Match(memberAttribute);
            }
        }

        /// <summary>
        /// Sorts descriptors by name of the descriptor.
        /// </summary>
        public static void SortDescriptorArray(IList infos)
        {
            ArgumentNullException.ThrowIfNull(infos);

            ArrayList.Adapter(infos).Sort(MemberDescriptorComparer.Instance);
        }

        /// <summary>
        /// This class is a type description provider that works with the IComNativeDescriptorHandler
        /// interface.
        /// </summary>
        private sealed class ComNativeDescriptionProvider : TypeDescriptionProvider
        {
#pragma warning disable 618
            internal ComNativeDescriptionProvider(IComNativeDescriptorHandler handler)
            {
                Handler = handler;
            }

            /// <summary>
            /// Returns the COM handler object.
            /// </summary>
            internal IComNativeDescriptorHandler Handler { get; set; }
#pragma warning restore 618

            /// <summary>
            /// Implements GetTypeDescriptor. This creates a custom type
            /// descriptor that walks the linked list for each of its calls.
            /// </summary>
            [return: NotNullIfNotNull(nameof(instance))]
            public override ICustomTypeDescriptor? GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object? instance)
            {
                ArgumentNullException.ThrowIfNull(objectType);

                if (instance == null)
                {
                    return null;
                }

                if (!objectType.IsInstanceOfType(instance))
                {
                    throw new ArgumentException(SR.Format(SR.ConvertToException, nameof(objectType), instance.GetType()), nameof(instance));
                }

                return new ComNativeTypeDescriptor(Handler, instance);
            }

            /// <summary>
            /// This type descriptor sits on top of a native
            /// descriptor handler.
            /// </summary>
            private sealed class ComNativeTypeDescriptor : ICustomTypeDescriptor
            {
#pragma warning disable 618
                private readonly IComNativeDescriptorHandler _handler;
                private readonly object _instance;

                /// <summary>
                /// Creates a new ComNativeTypeDescriptor.
                /// </summary>
                internal ComNativeTypeDescriptor(IComNativeDescriptorHandler handler, object instance)
                {
                    _handler = handler;
                    _instance = instance;
                }
#pragma warning restore 618

                AttributeCollection ICustomTypeDescriptor.GetAttributes() => _handler.GetAttributes(_instance);

                string ICustomTypeDescriptor.GetClassName() => _handler.GetClassName(_instance);

                string? ICustomTypeDescriptor.GetComponentName() => null;

                [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage)]
                TypeConverter ICustomTypeDescriptor.GetConverter() => _handler.GetConverter(_instance);

                [RequiresUnreferencedCode(EventDescriptor.RequiresUnreferencedCodeMessage)]
                EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
                {
                    return _handler.GetDefaultEvent(_instance);
                }

                [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
                PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
                {
                    return _handler.GetDefaultProperty(_instance);
                }

                [RequiresUnreferencedCode(DesignTimeAttributeTrimmed)]
                object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
                {
                    return _handler.GetEditor(_instance, editorBaseType);
                }

                EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
                {
                    return _handler.GetEvents(_instance);
                }

                [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
                EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes)
                {
                    return _handler.GetEvents(_instance, attributes);
                }

                [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
                {
                    return _handler.GetProperties(_instance, null);
                }

                [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes)
                {
                    return _handler.GetProperties(_instance, attributes);
                }

                object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => _instance;
            }
        }

        /// <summary>
        /// This is a type description provider that adds the given
        /// array of attributes to a class or instance, preserving the rest
        /// of the metadata in the process.
        /// </summary>
        private sealed class AttributeProvider : TypeDescriptionProvider
        {
            private readonly Attribute[] _attrs;

            /// <summary>
            /// Creates a new attribute provider.
            /// </summary>
            internal AttributeProvider(TypeDescriptionProvider existingProvider, params Attribute[] attrs) : base(existingProvider)
            {
                _attrs = attrs;
            }

            /// <summary>
            /// Creates a custom type descriptor that replaces the attributes.
            /// </summary>
            public override ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object? instance)
            {
                return new AttributeTypeDescriptor(_attrs, base.GetTypeDescriptor(objectType, instance));
            }

            /// <summary>
            /// Our custom type descriptor.
            /// </summary>
            private sealed class AttributeTypeDescriptor : CustomTypeDescriptor
            {
                private readonly Attribute[] _attributeArray;

                /// <summary>
                /// Creates a new custom type descriptor that can merge
                /// the provided set of attributes with the existing set.
                /// </summary>
                internal AttributeTypeDescriptor(Attribute[] attrs, ICustomTypeDescriptor? parent) : base(parent)
                {
                    _attributeArray = attrs;
                }

                /// <summary>
                /// Retrieves the merged set of attributes. We do not cache
                /// this because there is always the possibility that someone
                /// changed our parent provider's metadata. TypeDescriptor
                /// will cache this for us anyhow.
                /// </summary>
                public override AttributeCollection GetAttributes()
                {
                    Attribute[]? finalAttr;
                    AttributeCollection existing = base.GetAttributes();
                    Attribute[] newAttrs = _attributeArray;
                    Attribute[] newArray = new Attribute[existing.Count + newAttrs.Length];
                    int actualCount = existing.Count;
                    existing.CopyTo(newArray, 0);

                    for (int idx = 0; idx < newAttrs.Length; idx++)
                    {
                        Debug.Assert(newAttrs[idx] != null, "_attributes contains a null member");

                        // We must see if this attribute is already in the existing
                        // array. If it is, we replace it.
                        bool match = false;
                        for (int existingIdx = 0; existingIdx < existing.Count; existingIdx++)
                        {
                            if (newArray[existingIdx].TypeId.Equals(newAttrs[idx].TypeId))
                            {
                                match = true;
                                newArray[existingIdx] = newAttrs[idx];
                                break;
                            }
                        }

                        if (!match)
                        {
                            newArray[actualCount++] = newAttrs[idx];
                        }
                    }

                    // Now, if we collapsed some attributes, create a new array.
                    if (actualCount < newArray.Length)
                    {
                        finalAttr = new Attribute[actualCount];
                        Array.Copy(newArray, finalAttr, actualCount);
                    }
                    else
                    {
                        finalAttr = newArray;
                    }

                    return new AttributeCollection(finalAttr);
                }
            }
        }

        /// <summary>
        /// This is a simple class that is used to store a filtered
        /// set of members in an object's dictionary cache. It is
        /// used by the PipelineAttributeFilter method.
        /// </summary>
        private sealed class AttributeFilterCacheItem
        {
            private readonly Attribute[] _filter;
            internal readonly ICollection FilteredMembers;

            internal AttributeFilterCacheItem(Attribute[] filter, ICollection filteredMembers)
            {
                _filter = filter;
                FilteredMembers = filteredMembers;
            }

            internal bool IsValid(Attribute[] filter)
            {
                if (_filter.Length != filter.Length) return false;

                for (int idx = 0; idx < filter.Length; idx++)
                {
                    if (_filter[idx] != filter[idx])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// This small class contains cache information for the filter stage of our
        /// caching algorithm. It is used by the PipelineFilter method.
        /// </summary>
        private sealed class FilterCacheItem
        {
            private readonly ITypeDescriptorFilterService _filterService;
            internal readonly ICollection FilteredMembers;

            internal FilterCacheItem(ITypeDescriptorFilterService filterService, ICollection filteredMembers)
            {
                _filterService = filterService;
                FilteredMembers = filteredMembers;
            }

            internal bool IsValid(ITypeDescriptorFilterService filterService)
            {
                if (!ReferenceEquals(_filterService, filterService))
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// This comparer compares member descriptors for sorting.
        /// </summary>
        private sealed class MemberDescriptorComparer : IComparer
        {
            public static readonly MemberDescriptorComparer Instance = new MemberDescriptorComparer();

            public int Compare(object? left, object? right)
            {
                MemberDescriptor? leftMember = left as MemberDescriptor;
                MemberDescriptor? rightMember = right as MemberDescriptor;
                return CultureInfo.InvariantCulture.CompareInfo.Compare(leftMember?.Name, rightMember?.Name);
            }
        }

        [TypeDescriptionProvider(typeof(ComNativeDescriptorProxy))]
        private sealed class TypeDescriptorComObject
        {
        }

        // This class is being used to aid in diagnosability. The alternative to having this proxy would be
        // to set the fully qualified type name in the TypeDescriptionProvider attribute. The issue with the
        // string method is the failure is silent during type load making diagnosing the issue difficult.
        private sealed class ComNativeDescriptorProxy : TypeDescriptionProvider
        {
            private readonly TypeDescriptionProvider _comNativeDescriptor;

            public ComNativeDescriptorProxy()
            {
                Type realComNativeDescriptor = Type.GetType("System.Windows.Forms.ComponentModel.Com2Interop.ComNativeDescriptor, System.Windows.Forms", throwOnError: true)!;
                _comNativeDescriptor = (TypeDescriptionProvider)Activator.CreateInstance(realComNativeDescriptor)!;
            }

            [return: NotNullIfNotNull(nameof(instance))]
            public override ICustomTypeDescriptor? GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object? instance)
            {
                return _comNativeDescriptor.GetTypeDescriptor(objectType, instance);
            }
        }

        /// <summary>
        /// This is a merged type descriptor that can merge the output of
        /// a primary and secondary type descriptor. If the primary doesn't
        /// provide the needed information, the request is passed on to the
        /// secondary.
        /// </summary>
        private sealed class MergedTypeDescriptor : ICustomTypeDescriptor
        {
            private readonly ICustomTypeDescriptor _primary;
            private readonly ICustomTypeDescriptor _secondary;

            /// <summary>
            /// Creates a new MergedTypeDescriptor.
            /// </summary>
            internal MergedTypeDescriptor(ICustomTypeDescriptor primary, ICustomTypeDescriptor secondary)
            {
                _primary = primary;
                _secondary = secondary;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            AttributeCollection ICustomTypeDescriptor.GetAttributes()
            {
                AttributeCollection attrs = _primary.GetAttributes() ?? _secondary.GetAttributes();

                Debug.Assert(attrs != null, "Someone should have handled this");
                return attrs;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            string ICustomTypeDescriptor.GetClassName()
            {
                string? className = _primary.GetClassName() ?? _secondary.GetClassName();

                Debug.Assert(className != null, "Someone should have handled this");
                return className;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            string? ICustomTypeDescriptor.GetComponentName()
            {
                return _primary.GetComponentName() ?? _secondary.GetComponentName();
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage)]
            TypeConverter ICustomTypeDescriptor.GetConverter()
            {
                TypeConverter? converter = _primary.GetConverter() ?? _secondary.GetConverter();

                Debug.Assert(converter != null, "Someone should have handled this");
                return converter;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(EventDescriptor.RequiresUnreferencedCodeMessage)]
            EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent()
            {
                return _primary.GetDefaultEvent() ?? _secondary.GetDefaultEvent();
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
            PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty()
            {
                return _primary.GetDefaultProperty() ?? _secondary.GetDefaultProperty();
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(DesignTimeAttributeTrimmed)]
            object? ICustomTypeDescriptor.GetEditor(Type editorBaseType)
            {
                ArgumentNullException.ThrowIfNull(editorBaseType);

                object? editor = _primary.GetEditor(editorBaseType) ?? _secondary.GetEditor(editorBaseType);

                return editor;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
            {
                EventDescriptorCollection events = _primary.GetEvents() ?? _secondary.GetEvents();

                Debug.Assert(events != null, "Someone should have handled this");
                return events;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
            EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes)
            {
                EventDescriptorCollection events = _primary.GetEvents(attributes) ?? _secondary.GetEvents(attributes);

                Debug.Assert(events != null, "Someone should have handled this");
                return events;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
            PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
            {
                PropertyDescriptorCollection properties = _primary.GetProperties() ?? _secondary.GetProperties();

                Debug.Assert(properties != null, "Someone should have handled this");
                return properties;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
            PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes)
            {
                PropertyDescriptorCollection properties =
                    _primary.GetProperties(attributes) ??
                    _secondary.GetProperties(attributes);

                Debug.Assert(properties != null, "Someone should have handled this");
                return properties;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            object? ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd)
            {
                return _primary.GetPropertyOwner(pd) ?? _secondary.GetPropertyOwner(pd);
            }
        }

        /// <summary>
        /// This is a linked list node that is comprised of a type
        /// description provider. Each node contains a Next pointer
        /// to the next node in the list and also a Provider pointer
        /// which contains the type description provider this node
        /// represents. The implementation of TypeDescriptionProvider
        /// that the node provides simply invokes the corresponding
        /// method on the node's provider.
        /// </summary>
        private sealed class TypeDescriptionNode : TypeDescriptionProvider
        {
            internal TypeDescriptionNode? Next;
            internal TypeDescriptionProvider Provider;

            /// <summary>
            /// Creates a new type description node.
            /// </summary>
            internal TypeDescriptionNode(TypeDescriptionProvider provider)
            {
                Provider = provider;
            }

            /// <summary>
            /// Implements CreateInstance. This just walks the linked list
            /// looking for someone who implements the call.
            /// </summary>
            public override object? CreateInstance(
                IServiceProvider? provider,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type objectType,
                Type[]? argTypes,
                object?[]? args)
            {
                ArgumentNullException.ThrowIfNull(objectType);

                if (argTypes != null)
                {
                    ArgumentNullException.ThrowIfNull(args);

                    if (argTypes.Length != args.Length)
                    {
                        throw new ArgumentException(SR.TypeDescriptorArgsCountMismatch);
                    }
                }

                return Provider.CreateInstance(provider, objectType, argTypes, args);
            }

            /// <summary>
            /// Implements GetCache. This just walks the linked
            /// list looking for someone who implements the call.
            /// </summary>
            public override IDictionary? GetCache(object instance)
            {
                ArgumentNullException.ThrowIfNull(instance);

                return Provider.GetCache(instance);
            }

            /// <summary>
            /// Implements GetExtendedTypeDescriptor. This creates a custom type
            /// descriptor that walks the linked list for each of its calls.
            /// </summary>
            [RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
            public override ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
            {
                ArgumentNullException.ThrowIfNull(instance);

                return new DefaultExtendedTypeDescriptor(this, instance);
            }

            protected internal override IExtenderProvider[] GetExtenderProviders(object instance)
            {
                ArgumentNullException.ThrowIfNull(instance);

                return Provider.GetExtenderProviders(instance);
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
            public override string? GetFullComponentName(object component)
            {
                ArgumentNullException.ThrowIfNull(component);

                return Provider.GetFullComponentName(component);
            }

            /// <summary>
            /// Implements GetReflectionType. This just walks the linked list
            /// looking for someone who implements the call.
            /// </summary>
            [return: DynamicallyAccessedMembers(ReflectTypesDynamicallyAccessedMembers)]
            public override Type GetReflectionType(
                [DynamicallyAccessedMembers(ReflectTypesDynamicallyAccessedMembers)] Type objectType,
                object? instance)
            {
                ArgumentNullException.ThrowIfNull(objectType);

                return Provider.GetReflectionType(objectType, instance);
            }

            public override Type GetRuntimeType(Type objectType)
            {
                ArgumentNullException.ThrowIfNull(objectType);

                return Provider.GetRuntimeType(objectType);
            }

            /// <summary>
            /// Implements GetTypeDescriptor. This creates a custom type
            /// descriptor that walks the linked list for each of its calls.
            /// </summary>
            public override ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object? instance)
            {
                ArgumentNullException.ThrowIfNull(objectType);

                if (instance != null && !objectType.IsInstanceOfType(instance))
                {
                    throw new ArgumentException(nameof(instance));
                }

                return new DefaultTypeDescriptor(this, objectType, instance);
            }

            internal DefaultTypeDescriptor GetDefaultTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType)
            {
                return new DefaultTypeDescriptor(this, objectType, instance: null);
            }

            public override bool IsSupportedType(Type type)
            {
                ArgumentNullException.ThrowIfNull(type);

                return Provider.IsSupportedType(type);
            }

            /// <summary>
            /// A type descriptor for extended types. This type descriptor
            /// looks at the head node in the linked list.
            /// </summary>
            private readonly struct DefaultExtendedTypeDescriptor : ICustomTypeDescriptor
            {
                private readonly TypeDescriptionNode _node;
                private readonly object _instance;

                /// <summary>
                /// Creates a new WalkingExtendedTypeDescriptor.
                /// </summary>
                [RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
                internal DefaultExtendedTypeDescriptor(TypeDescriptionNode node, object instance)
                {
                    _node = node;
                    _instance = instance;
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
                AttributeCollection ICustomTypeDescriptor.GetAttributes()
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor

                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider)
                    {
                        return ReflectTypeDescriptionProvider.GetExtendedAttributes();
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    AttributeCollection attrs = desc.GetAttributes();
                    if (attrs == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetAttributes"));
                    return attrs;
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
                string? ICustomTypeDescriptor.GetClassName()
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor

                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider rp)
                    {
                        return rp.GetExtendedClassName(_instance);
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    string? name = desc.GetClassName() ?? _instance.GetType().FullName;
                    return name;
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
                string? ICustomTypeDescriptor.GetComponentName()
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor

                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider)
                    {
                        return ReflectTypeDescriptionProvider.GetExtendedComponentName(_instance);
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    return desc.GetComponentName();
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage)]
                TypeConverter ICustomTypeDescriptor.GetConverter()
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor

                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider rp)
                    {
                        return rp.GetExtendedConverter(_instance);
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    TypeConverter? converter = desc.GetConverter();
                    if (converter == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetConverter"));
                    return converter;
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [RequiresUnreferencedCode(EventDescriptor.RequiresUnreferencedCodeMessage)]
                EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent()
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor

                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider)
                    {
                        return ReflectTypeDescriptionProvider.GetExtendedDefaultEvent();
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    return desc.GetDefaultEvent();
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
                PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty()
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor
                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider)
                    {
                        return ReflectTypeDescriptionProvider.GetExtendedDefaultProperty();
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    return desc.GetDefaultProperty();
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [RequiresUnreferencedCode(DesignTimeAttributeTrimmed)]
                object? ICustomTypeDescriptor.GetEditor(Type editorBaseType)
                {
                    ArgumentNullException.ThrowIfNull(editorBaseType);

                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor
                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider rp)
                    {
                        return rp.GetExtendedEditor(_instance, editorBaseType);
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    return desc.GetEditor(editorBaseType);
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
                EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor
                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider)
                    {
                        return ReflectTypeDescriptionProvider.GetExtendedEvents();
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    EventDescriptorCollection events = desc.GetEvents();
                    if (events == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetEvents"));
                    return events;
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
                EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes)
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor
                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider)
                    {
                        // There is no need to filter these events. For extended objects, they
                        // are accessed through our pipeline code, which always filters before
                        // returning. So any filter we do here is redundant. Note that we do
                        // pass a valid filter to a custom descriptor so it can optimize if it wants.
                        EventDescriptorCollection events = ReflectTypeDescriptionProvider.GetExtendedEvents();
                        return events;
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    EventDescriptorCollection evts = desc.GetEvents(attributes);
                    if (evts == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetEvents"));
                    return evts;
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor
                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider rp)
                    {
                        return rp.GetExtendedProperties(_instance);
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    PropertyDescriptorCollection properties = desc.GetProperties();
                    if (properties == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetProperties"));
                    return properties;
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes)
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor
                    TypeDescriptionProvider p = _node.Provider;
                    if (p is ReflectTypeDescriptionProvider rp)
                    {
                        // There is no need to filter these properties. For extended objects, they
                        // are accessed through our pipeline code, which always filters before
                        // returning. So any filter we do here is redundant. Note that we do
                        // pass a valid filter to a custom descriptor so it can optimize if it wants.
                        PropertyDescriptorCollection props = rp.GetExtendedProperties(_instance);
                        return props;
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    PropertyDescriptorCollection properties = desc.GetProperties(attributes);
                    if (properties == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetProperties"));
                    return properties;
                }

                /// <summary>
                /// ICustomTypeDescriptor implementation.
                /// </summary>
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
                object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd)
                {
                    // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                    // If so, we can call on it directly rather than creating another
                    // custom type descriptor

                    TypeDescriptionProvider p = _node.Provider;

                    if (p is ReflectTypeDescriptionProvider)
                    {
                        return ReflectTypeDescriptionProvider.GetExtendedPropertyOwner(_instance);
                    }

                    ICustomTypeDescriptor desc = p.GetExtendedTypeDescriptor(_instance);
                    if (desc == null) throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
                    object owner = desc.GetPropertyOwner(pd) ?? _instance;
                    return owner;
                }
            }
        }

        /// <summary>
        /// The default type descriptor.
        /// </summary>
        private readonly struct DefaultTypeDescriptor : ICustomTypeDescriptor
        {
            private readonly TypeDescriptionNode _node;
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            private readonly Type _objectType;
            private readonly object? _instance;

            /// <summary>
            /// Creates a new WalkingTypeDescriptor.
            /// </summary>
            internal DefaultTypeDescriptor(
                TypeDescriptionNode node,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType,
                object? instance)
            {
                _node = node;
                _objectType = objectType;
                _instance = instance;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            public AttributeCollection GetAttributes()
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                AttributeCollection attrs;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    attrs = rp.GetAttributes(_objectType);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    attrs = desc.GetAttributes();
                    if (attrs == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetAttributes"));
                }

                return attrs;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            public string? GetClassName()
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                string? name;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    name = rp.GetClassName(_objectType);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    name = desc.GetClassName() ?? _objectType.FullName;
                }

                return name;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            string? ICustomTypeDescriptor.GetComponentName()
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                string? name;
                if (p is ReflectTypeDescriptionProvider)
                {
                    name = ReflectTypeDescriptionProvider.GetComponentName(_instance);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    name = desc.GetComponentName();
                }

                return name;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage)]
            public TypeConverter GetConverter()
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                TypeConverter? converter;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    converter = rp.GetConverter(_objectType, _instance);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    converter = desc.GetConverter();
                    if (converter == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetConverter"));
                }

                return converter;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(EventDescriptor.RequiresUnreferencedCodeMessage)]
            public EventDescriptor? GetDefaultEvent()
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                EventDescriptor? defaultEvent;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    defaultEvent = rp.GetDefaultEvent(_objectType, _instance);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    defaultEvent = desc.GetDefaultEvent();
                }

                return defaultEvent;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
            public PropertyDescriptor? GetDefaultProperty()
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                PropertyDescriptor? defaultProperty;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    defaultProperty = rp.GetDefaultProperty(_objectType, _instance);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    defaultProperty = desc.GetDefaultProperty();
                }

                return defaultProperty;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(DesignTimeAttributeTrimmed)]
            public object? GetEditor(Type editorBaseType)
            {
                ArgumentNullException.ThrowIfNull(editorBaseType);

                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                object? editor;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    editor = rp.GetEditor(_objectType, _instance, editorBaseType);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    editor = desc.GetEditor(editorBaseType);
                }

                return editor;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            public EventDescriptorCollection GetEvents()
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                EventDescriptorCollection events;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    events = rp.GetEvents(_objectType);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    events = desc.GetEvents();
                    if (events == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetEvents"));
                }

                return events;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
            public EventDescriptorCollection GetEvents(Attribute[]? attributes)
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                EventDescriptorCollection events;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    events = rp.GetEvents(_objectType);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    events = desc.GetEvents(attributes);
                    if (events == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetEvents"));
                }

                return events;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
            public PropertyDescriptorCollection GetProperties()
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                PropertyDescriptorCollection properties;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    properties = rp.GetProperties(_objectType);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    properties = desc.GetProperties();
                    if (properties == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetProperties"));
                }

                return properties;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
            public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                PropertyDescriptorCollection properties;
                if (p is ReflectTypeDescriptionProvider rp)
                {
                    properties = rp.GetProperties(_objectType);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    properties = desc.GetProperties(attributes);
                    if (properties == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetProperties"));
                }

                return properties;
            }

            /// <summary>
            /// ICustomTypeDescriptor implementation.
            /// </summary>
            object? ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd)
            {
                // Check to see if the provider we get is a ReflectTypeDescriptionProvider.
                // If so, we can call on it directly rather than creating another
                // custom type descriptor
                TypeDescriptionProvider p = _node.Provider;
                object? owner;
                if (p is ReflectTypeDescriptionProvider)
                {
                    owner = ReflectTypeDescriptionProvider.GetPropertyOwner(_objectType, _instance!);
                }
                else
                {
                    ICustomTypeDescriptor? desc = p.GetTypeDescriptor(_objectType, _instance);
                    if (desc == null)
                        throw new InvalidOperationException(SR.Format(SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
                    owner = desc.GetPropertyOwner(pd) ?? _instance;
                }

                return owner;
            }
        }

        /// <summary>
        /// This is a simple internal type that allows external parties to
        /// register a custom type description provider for all interface types.
        /// </summary>
        private sealed class TypeDescriptorInterface
        {
        }
    }
}
