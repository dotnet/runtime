// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace System.ComponentModel
{
    /// <summary>
    /// This type description provider provides type information through 
    /// reflection. Unless someone has provided a custom type description
    /// provider for a type or instance, or unless an instance implements
    /// ICustomTypeDescriptor, any query for type information will go through
    /// this class. There should be a single instance of this class associated
    /// with "object", as it can provide all type information for any type.
    /// </summary>
    internal sealed partial class ReflectTypeDescriptionProvider : TypeDescriptionProvider
    {
        // Hastable of Type -> ReflectedTypeData. ReflectedTypeData contains all
        // of the type information we have gathered for a given type.
        //
        private Hashtable _typeData;

        // This is the signature we look for when creating types that are generic, but
        // want to know what type they are dealing with. Enums are a good example of this;
        // there is one enum converter that can work with all enums, but it needs to know
        // the type of enum it is dealing with.
        //
        private static Type[] s_typeConstructor = new Type[] { typeof(Type) };

        // This is where we store the various converters, etc for the intrinsic types.
        //
        private static Hashtable s_editorTables;
        private static Hashtable s_intrinsicTypeConverters;

        // For converters, etc that are bound to class attribute data, rather than a class
        // type, we have special key sentinel values that we put into the hash table.
        //
        private static object s_intrinsicReferenceKey = new object();
        private static object s_intrinsicNullableKey = new object();

        // The key we put into IDictionaryService to store our cache dictionary.
        //
        private static object s_dictionaryKey = new object();

        // This is a cache on top of core reflection. The cache
        // builds itself recursively, so if you ask for the properties
        // on Control, Component and object are also automatically filled
        // in. The keys to the property and event caches are types.
        // The keys to the attribute cache are either MemberInfos or types.
        //
        private static Hashtable s_propertyCache;
        private static Hashtable s_eventCache;
        private static Hashtable s_attributeCache;
        private static Hashtable s_extendedPropertyCache;

        // These are keys we stuff into our object cache. We use this
        // cache data to store extender provider info for an object.
        //
        private static readonly Guid s_extenderPropertiesKey = Guid.NewGuid();
        private static readonly Guid s_extenderProviderPropertiesKey = Guid.NewGuid();

        // These are attributes that, when we discover them on interfaces, we do
        // not merge them into the attribute set for a class.
        private static readonly Type[] s_skipInterfaceAttributeList = new Type[]
        {
#if FEATURE_SKIP_INTERFACE
            typeof(System.Runtime.InteropServices.GuidAttribute),
            typeof(System.Runtime.InteropServices.InterfaceTypeAttribute)
#endif
            typeof(System.Runtime.InteropServices.ComVisibleAttribute),
        };


        internal static Guid ExtenderProviderKey { get; } = Guid.NewGuid();


        private static readonly object s_internalSyncObject = new object();
        /// <summary>
        /// Creates a new ReflectTypeDescriptionProvider. The type is the
        /// type we will obtain type information for.
        /// </summary>
        internal ReflectTypeDescriptionProvider()
        {
        }

        private static Hashtable EditorTables => LazyInitializer.EnsureInitialized(ref s_editorTables, () => new Hashtable(4));

        /// <summary> 
        /// This is a table we create for intrinsic types. 
        /// There should be entries here ONLY for intrinsic 
        /// types, as all other types we should be able to 
        /// add attributes directly as metadata. 
        /// </summary> 
        private static Hashtable IntrinsicTypeConverters => LazyInitializer.EnsureInitialized(ref s_intrinsicTypeConverters, () => new Hashtable
        {
            // Add the intrinsics
            //
            [typeof(bool)] = typeof(BooleanConverter),
            [typeof(byte)] = typeof(ByteConverter),
            [typeof(sbyte)] = typeof(SByteConverter),
            [typeof(char)] = typeof(CharConverter),
            [typeof(double)] = typeof(DoubleConverter),
            [typeof(string)] = typeof(StringConverter),
            [typeof(int)] = typeof(Int32Converter),
            [typeof(short)] = typeof(Int16Converter),
            [typeof(long)] = typeof(Int64Converter),
            [typeof(float)] = typeof(SingleConverter),
            [typeof(ushort)] = typeof(UInt16Converter),
            [typeof(uint)] = typeof(UInt32Converter),
            [typeof(ulong)] = typeof(UInt64Converter),
            [typeof(object)] = typeof(TypeConverter),
            [typeof(void)] = typeof(TypeConverter),
            [typeof(CultureInfo)] = typeof(CultureInfoConverter),
            [typeof(DateTime)] = typeof(DateTimeConverter),
            [typeof(DateTimeOffset)] = typeof(DateTimeOffsetConverter),
            [typeof(decimal)] = typeof(DecimalConverter),
            [typeof(TimeSpan)] = typeof(TimeSpanConverter),
            [typeof(Guid)] = typeof(GuidConverter),
            [typeof(Uri)] = typeof(UriTypeConverter),
            [typeof(Version)] = typeof(VersionConverter),
            [typeof(Color)] = typeof(ColorConverter),
            [typeof(Point)] = typeof(PointConverter),
            [typeof(Rectangle)] = typeof(RectangleConverter),
            [typeof(Size)] = typeof(SizeConverter),
            [typeof(SizeF)] = typeof(SizeFConverter),
            // Special cases for things that are not bound to a specific type
            //
            [typeof(Array)] = typeof(ArrayConverter),
            [typeof(ICollection)] = typeof(CollectionConverter),
            [typeof(Enum)] = typeof(EnumConverter),
            [s_intrinsicNullableKey] = typeof(NullableConverter),
        });

        private static Hashtable PropertyCache => LazyInitializer.EnsureInitialized(ref s_propertyCache, () => new Hashtable());

        private static Hashtable EventCache => LazyInitializer.EnsureInitialized(ref s_eventCache, () => new Hashtable());

        private static Hashtable AttributeCache => LazyInitializer.EnsureInitialized(ref s_attributeCache, () => new Hashtable());

        private static Hashtable ExtendedPropertyCache => LazyInitializer.EnsureInitialized(ref s_extendedPropertyCache, () => new Hashtable());

        /// <summary>
        /// Adds an editor table for the given editor base type.
        /// Typically, editors are specified as metadata on an object. If no metadata for a
        /// requested editor base type can be found on an object, however, the
        /// TypeDescriptor will search an editor
        /// table for the editor type, if one can be found.
        /// </summary>
        internal static void AddEditorTable(Type editorBaseType, Hashtable table)
        {
            if (editorBaseType == null)
            {
                throw new ArgumentNullException(nameof(editorBaseType));
            }

            if (table == null)
            {
                Debug.Fail("COMPAT: Editor table should not be null");
                // don't throw; RTM didn't so we can't do it either.
            }

            lock (s_internalSyncObject)
            {
                Hashtable editorTables = EditorTables;
                if (!editorTables.ContainsKey(editorBaseType))
                {
                    editorTables[editorBaseType] = table;
                }
            }
        }

        /// <summary>
        /// CreateInstance implementation. We delegate to Activator.
        /// </summary>
        public override object CreateInstance(IServiceProvider provider, Type objectType, Type[] argTypes, object[] args)
        {
            Debug.Assert(objectType != null, "Should have arg-checked before coming in here");

            object obj = null;

            if (argTypes != null)
            {
                obj = objectType.GetConstructor(argTypes)?.Invoke(args);
            }
            else
            {
                if (args != null)
                {
                    argTypes = new Type[args.Length];
                    for (int idx = 0; idx < args.Length; idx++)
                    {
                        if (args[idx] != null)
                        {
                            argTypes[idx] = args[idx].GetType();
                        }
                        else
                        {
                            argTypes[idx] = typeof(object);
                        }
                    }
                }
                else
                {
                    argTypes = Array.Empty<Type>();
                }

                obj = objectType.GetConstructor(argTypes)?.Invoke(args);
            }

            return obj ?? Activator.CreateInstance(objectType, args);
        }


        /// <summary> 
        /// Helper method to create editors and type converters. This checks to see if the
        /// type implements a Type constructor, and if it does it invokes that ctor. 
        /// Otherwise, it just tries to create the type.
        /// </summary> 
        private static object CreateInstance(Type objectType, Type callingType)
        {
            return objectType.GetConstructor(s_typeConstructor)?.Invoke(new object[] { callingType })
                ?? Activator.CreateInstance(objectType);
        }

        /// <summary>
        /// Retrieves custom attributes.
        /// </summary>
        internal AttributeCollection GetAttributes(Type type)
        {
            ReflectedTypeData td = GetTypeData(type, true);
            return td.GetAttributes();
        }

        /// <summary>
        /// Our implementation of GetCache sits on top of IDictionaryService.
        /// </summary>
        public override IDictionary GetCache(object instance)
        {
            IComponent comp = instance as IComponent;
            if (comp != null && comp.Site != null)
            {
                IDictionaryService ds = comp.Site.GetService(typeof(IDictionaryService)) as IDictionaryService;
                if (ds != null)
                {
                    IDictionary dict = ds.GetValue(s_dictionaryKey) as IDictionary;
                    if (dict == null)
                    {
                        dict = new Hashtable();
                        ds.SetValue(s_dictionaryKey, dict);
                    }
                    return dict;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the class name for our type.
        /// </summary>
        internal string GetClassName(Type type)
        {
            ReflectedTypeData td = GetTypeData(type, true);
            return td.GetClassName(null);
        }

        /// <summary>
        /// Retrieves the component name from the site.
        /// </summary>
        internal string GetComponentName(Type type, object instance)
        {
            ReflectedTypeData td = GetTypeData(type, true);
            return td.GetComponentName(instance);
        }

        /// <summary>
        /// Retrieves the type converter. If instance is non-null,
        /// it will be used to retrieve attributes. Otherwise, _type
        /// will be used.
        /// </summary>
        internal TypeConverter GetConverter(Type type, object instance)
        {
            ReflectedTypeData td = GetTypeData(type, true);
            return td.GetConverter(instance);
        }

        /// <summary>
        /// Return the default event. The default event is determined by the
        /// presence of a DefaultEventAttribute on the class.
        /// </summary>
        internal EventDescriptor GetDefaultEvent(Type type, object instance)
        {
            ReflectedTypeData td = GetTypeData(type, true);
            return td.GetDefaultEvent(instance);
        }

        /// <summary>
        /// Return the default property.
        /// </summary>
        internal PropertyDescriptor GetDefaultProperty(Type type, object instance)
        {
            ReflectedTypeData td = GetTypeData(type, true);
            return td.GetDefaultProperty(instance);
        }

        /// <summary>
        /// Retrieves the editor for the given base type.
        /// </summary>
        internal object GetEditor(Type type, object instance, Type editorBaseType)
        {
            ReflectedTypeData td = GetTypeData(type, true);
            return td.GetEditor(instance, editorBaseType);
        }

        /// <summary> 
        /// Retrieves a default editor table for the given editor base type. 
        /// </summary> 
        private static Hashtable GetEditorTable(Type editorBaseType)
        {
            Hashtable editorTables = EditorTables;
            object table = editorTables[editorBaseType];

            if (table == null)
            {
                // Before we give up, it is possible that the
                // class initializer for editorBaseType hasn't 
                // actually run.
                //
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(editorBaseType.TypeHandle);
                table = editorTables[editorBaseType];

                // If the table is still null, then throw a
                // sentinel in there so we don't
                // go through this again.
                //
                if (table == null)
                {
                    lock (s_internalSyncObject)
                    {
                        table = editorTables[editorBaseType];
                        if (table == null)
                        {
                            editorTables[editorBaseType] = editorTables;
                        }
                    }
                }
            }

            // Look for our sentinel value that indicates
            // we have already tried and failed to get
            // a table.
            //
            if (table == editorTables)
            {
                table = null;
            }

            return (Hashtable)table;
        }

        /// <summary>
        /// Retrieves the events for this type.
        /// </summary>
        internal EventDescriptorCollection GetEvents(Type type)
        {
            ReflectedTypeData td = GetTypeData(type, true);
            return td.GetEvents();
        }

        /// <summary>
        /// Retrieves custom extender attributes. We don't support
        /// extender attributes, so we always return an empty collection.
        /// </summary>
        internal AttributeCollection GetExtendedAttributes(object instance)
        {
            return AttributeCollection.Empty;
        }

        /// <summary>
        /// Retrieves the class name for our type.
        /// </summary>
        internal string GetExtendedClassName(object instance)
        {
            return GetClassName(instance.GetType());
        }

        /// <summary>
        /// Retrieves the component name from the site.
        /// </summary>
        internal string GetExtendedComponentName(object instance)
        {
            return GetComponentName(instance.GetType(), instance);
        }

        /// <summary>
        /// Retrieves the type converter. If instance is non-null,
        /// it will be used to retrieve attributes. Otherwise, _type
        /// will be used.
        /// </summary>
        internal TypeConverter GetExtendedConverter(object instance)
        {
            return GetConverter(instance.GetType(), instance);
        }

        /// <summary>
        /// Return the default event. The default event is determined by the
        /// presence of a DefaultEventAttribute on the class.
        /// </summary>
        internal EventDescriptor GetExtendedDefaultEvent(object instance)
        {
            return null; // we don't support extended events.
        }

        /// <summary>
        /// Return the default property.
        /// </summary>
        internal PropertyDescriptor GetExtendedDefaultProperty(object instance)
        {
            return null; // extender properties are never the default.
        }

        /// <summary>
        /// Retrieves the editor for the given base type.
        /// </summary>
        internal object GetExtendedEditor(object instance, Type editorBaseType)
        {
            return GetEditor(instance.GetType(), instance, editorBaseType);
        }

        /// <summary>
        /// Retrieves the events for this type.
        /// </summary>
        internal EventDescriptorCollection GetExtendedEvents(object instance)
        {
            return EventDescriptorCollection.Empty;
        }

        /// <summary>
        /// Retrieves the properties for this type.
        /// </summary>
        internal PropertyDescriptorCollection GetExtendedProperties(object instance)
        {
            // Is this object a sited component?  If not, then it
            // doesn't have any extender properties.
            //
            Type componentType = instance.GetType();

            // Check the component for extender providers. We prefer
            // IExtenderListService, but will use the container if that's
            // all we have. In either case, we check the list of extenders
            // against previously stored data in the object cache. If
            // the cache is up to date, we just return the extenders in the
            // cache.
            //
            IExtenderProvider[] extenders = GetExtenderProviders(instance);
            IDictionary cache = TypeDescriptor.GetCache(instance);

            if (extenders.Length == 0)
            {
                return PropertyDescriptorCollection.Empty;
            }

            // Ok, we have a set of extenders. Now, check to see if there
            // are properties already in our object cache. If there aren't,
            // then we will need to create them.
            //
            PropertyDescriptorCollection properties = null;

            if (cache != null)
            {
                properties = cache[s_extenderPropertiesKey] as PropertyDescriptorCollection;
            }

            if (properties != null)
            {
                return properties;
            }

            // Unlike normal properties, it is fine for there to be properties with
            // duplicate names here. 
            //
            List<PropertyDescriptor> propertyList = null;

            for (int idx = 0; idx < extenders.Length; idx++)
            {
                PropertyDescriptor[] propertyArray = ReflectGetExtendedProperties(extenders[idx]);

                if (propertyList == null)
                {
                    propertyList = new List<PropertyDescriptor>(propertyArray.Length * extenders.Length);
                }

                for (int propIdx = 0; propIdx < propertyArray.Length; propIdx++)
                {
                    PropertyDescriptor prop = propertyArray[propIdx];
                    ExtenderProvidedPropertyAttribute eppa = prop.Attributes[typeof(ExtenderProvidedPropertyAttribute)] as ExtenderProvidedPropertyAttribute;

                    Debug.Assert(eppa != null, $"Extender property {prop.Name} has no provider attribute. We will skip it.");
                    if (eppa != null)
                    {
                        Type receiverType = eppa.ReceiverType;
                        if (receiverType != null)
                        {
                            if (receiverType.IsAssignableFrom(componentType))
                            {
                                propertyList.Add(prop);
                            }
                        }
                    }
                }
            }

            // propertyHash now contains ExtendedPropertyDescriptor objects
            // for each extended property.
            //
            if (propertyList != null)
            {
                PropertyDescriptor[] fullArray = new PropertyDescriptor[propertyList.Count];
                propertyList.CopyTo(fullArray, 0);
                properties = new PropertyDescriptorCollection(fullArray, true);
            }
            else
            {
                properties = PropertyDescriptorCollection.Empty;
            }

            if (cache != null)
            {
                cache[s_extenderPropertiesKey] = properties;
            }

            return properties;
        }

        protected internal override IExtenderProvider[] GetExtenderProviders(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            IComponent component = instance as IComponent;
            if (component != null && component.Site != null)
            {
                IExtenderListService extenderList = component.Site.GetService(typeof(IExtenderListService)) as IExtenderListService;
                IDictionary cache = TypeDescriptor.GetCache(instance);

                if (extenderList != null)
                {
                    return GetExtenders(extenderList.GetExtenderProviders(), instance, cache);
                }
#if FEATURE_COMPONENT_COLLECTION
                else
                {
                    IContainer cont = component.Site.Container;
                    if (cont != null)
                    {
                        return GetExtenders(cont.Components, instance, cache);
                    }
                }
#endif
            }
            return Array.Empty<IExtenderProvider>();
        }

        /// <summary>
        /// GetExtenders builds a list of extender providers from
        /// a collection of components. It validates the extenders
        /// against any cached collection of extenders in the 
        /// cache. If there is a discrepancy, this will erase
        /// any cached extender properties from the cache and
        /// save the updated extender list. If there is no
        /// discrepancy this will simply return the cached list.
        /// </summary>
        private static IExtenderProvider[] GetExtenders(ICollection components, object instance, IDictionary cache)
        {
            bool newExtenders = false;
            int extenderCount = 0;
            IExtenderProvider[] existingExtenders = null;

            //CanExtend is expensive. We will remember results of CanExtend for the first 64 extenders and using "long canExtend" as a bit vector.
            // we want to avoid memory allocation as well so we don't use some more sophisticated data structure like an array of booleans
            ulong canExtend = 0;
            int maxCanExtendResults = 64;
            // currentExtenders is what we intend to return. If the caller passed us
            // the return value from IExtenderListService, components will already be
            // an IExtenderProvider[]. If not, then we must treat components as an
            // opaque collection. We spend a great deal of energy here to avoid
            // copying or allocating memory because this method is called every
            // time a component is asked for its properties.
            IExtenderProvider[] currentExtenders = components as IExtenderProvider[];

            if (cache != null)
            {
                existingExtenders = cache[ExtenderProviderKey] as IExtenderProvider[];
            }

            if (existingExtenders == null)
            {
                newExtenders = true;
            }

            int curIdx = 0;
            int idx = 0;

            if (currentExtenders != null)
            {
                for (curIdx = 0; curIdx < currentExtenders.Length; curIdx++)
                {
                    if (currentExtenders[curIdx].CanExtend(instance))
                    {
                        extenderCount++;
                        // Performance:We would like to call CanExtend as little as possible therefore we remember its result
                        if (curIdx < maxCanExtendResults)
                            canExtend |= (ulong)1 << curIdx;
                        if (!newExtenders && (idx >= existingExtenders.Length || currentExtenders[curIdx] != existingExtenders[idx++]))
                        {
                            newExtenders = true;
                        }
                    }
                }
            }
            else if (components != null)
            {
                foreach (object obj in components)
                {
                    IExtenderProvider prov = obj as IExtenderProvider;
                    if (prov != null && prov.CanExtend(instance))
                    {
                        extenderCount++;
                        if (curIdx < maxCanExtendResults)
                            canExtend |= (ulong)1 << curIdx;
                        if (!newExtenders && (idx >= existingExtenders.Length || prov != existingExtenders[idx++]))
                        {
                            newExtenders = true;
                        }
                    }
                    curIdx++;
                }
            }
            if (existingExtenders != null && extenderCount != existingExtenders.Length)
            {
                newExtenders = true;
            }
            if (newExtenders)
            {
                if (currentExtenders == null || extenderCount != currentExtenders.Length)
                {
                    IExtenderProvider[] newExtenderArray = new IExtenderProvider[extenderCount];

                    curIdx = 0;
                    idx = 0;

                    if (currentExtenders != null && extenderCount > 0)
                    {
                        while (curIdx < currentExtenders.Length)
                        {
                            if ((curIdx < maxCanExtendResults && (canExtend & ((ulong)1 << curIdx)) != 0) ||
                                            (curIdx >= maxCanExtendResults && currentExtenders[curIdx].CanExtend(instance)))
                            {
                                Debug.Assert(idx < extenderCount, "There are more extenders than we expect");
                                newExtenderArray[idx++] = currentExtenders[curIdx];
                            }
                            curIdx++;
                        }
                        Debug.Assert(idx == extenderCount, "Wrong number of extenders");
                    }
                    else if (extenderCount > 0)
                    {
                        foreach (var component in components)
                        {
                            IExtenderProvider p = component as IExtenderProvider;

                            if (p != null && ((curIdx < maxCanExtendResults && (canExtend & ((ulong)1 << curIdx)) != 0) ||
                                                (curIdx >= maxCanExtendResults && p.CanExtend(instance))))
                            {
                                Debug.Assert(idx < extenderCount, "There are more extenders than we expect");
                                newExtenderArray[idx++] = p;
                            }
                            curIdx++;
                        }
                        Debug.Assert(idx == extenderCount, "Wrong number of extenders");
                    }
                    currentExtenders = newExtenderArray;
                }

                if (cache != null)
                {
                    cache[ExtenderProviderKey] = currentExtenders;
                    cache.Remove(s_extenderPropertiesKey);
                }
            }
            else
            {
                currentExtenders = existingExtenders;
            }
            return currentExtenders;
        }

        /// <summary>
        /// Retrieves the owner for a property.
        /// </summary>
        internal object GetExtendedPropertyOwner(object instance, PropertyDescriptor pd)
        {
            return GetPropertyOwner(instance.GetType(), instance, pd);
        }

        //////////////////////////////////////////////////////////
        /// <summary>
        /// Provides a type descriptor for the given object. We only support this
        /// if the object is a component that 
        /// </summary>
        public override ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
        {
            Debug.Fail("This should never be invoked. TypeDescriptionNode should wrap for us.");
            return null;
        }

        /// <summary>
        /// The name of the specified component, or null if the component has no name.
        /// In many cases this will return the same value as GetComponentName. If the
        /// component resides in a nested container or has other nested semantics, it may
        /// return a different fully qualified name.
        ///
        /// If not overridden, the default implementation of this method will call
        /// GetComponentName.
        /// </summary>
        public override string GetFullComponentName(object component)
        {
            IComponent comp = component as IComponent;
            INestedSite ns = comp?.Site as INestedSite;
            if (ns != null)
            {
                return ns.FullName;
            }

            return TypeDescriptor.GetComponentName(component);
        }

        /// <summary>
        /// Returns an array of types we have populated metadata for that live
        /// in the current module.
        /// </summary>
        internal Type[] GetPopulatedTypes(Module module)
        {
            List<Type> typeList = new List<Type>();

            // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
            IDictionaryEnumerator e = _typeData.GetEnumerator();
            while (e.MoveNext())
            {
                DictionaryEntry de = e.Entry;
                Type type = (Type)de.Key;
                ReflectedTypeData typeData = (ReflectedTypeData)de.Value;

                if (type.Module == module && typeData.IsPopulated)
                {
                    typeList.Add(type);
                }
            }

            return typeList.ToArray();
        }

        /// <summary>
        /// Retrieves the properties for this type.
        /// </summary>
        internal PropertyDescriptorCollection GetProperties(Type type)
        {
            ReflectedTypeData td = GetTypeData(type, true);
            return td.GetProperties();
        }

        /// <summary>
        /// Retrieves the owner for a property.
        /// </summary>
        internal object GetPropertyOwner(Type type, object instance, PropertyDescriptor pd)
        {
            return TypeDescriptor.GetAssociation(type, instance);
        }

        /// <summary>
        /// Returns an Type for the given type. Since type implements IReflect,
        /// we just return objectType.
        /// </summary>
        public override Type GetReflectionType(Type objectType, object instance)
        {
            Debug.Assert(objectType != null, "Should have arg-checked before coming in here");
            return objectType;
        }

        /// <summary>
        /// Returns the type data for the given type, or
        /// null if there is no type data for the type yet and
        /// createIfNeeded is false.
        /// </summary>
        private ReflectedTypeData GetTypeData(Type type, bool createIfNeeded)
        {
            ReflectedTypeData td = null;

            if (_typeData != null)
            {
                td = (ReflectedTypeData)_typeData[type];
                if (td != null)
                {
                    return td;
                }
            }

            lock (s_internalSyncObject)
            {
                if (_typeData != null)
                {
                    td = (ReflectedTypeData)_typeData[type];
                }

                if (td == null && createIfNeeded)
                {
                    td = new ReflectedTypeData(type);
                    if (_typeData == null)
                    {
                        _typeData = new Hashtable();
                    }
                    _typeData[type] = td;
                }
            }

            return td;
        }

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
            Debug.Fail("This should never be invoked. TypeDescriptionNode should wrap for us.");
            return null;
        }

        /// <summary>
        /// Retrieves a type from a name.
        /// </summary>
        private static Type GetTypeFromName(string typeName)
        {
            Type t = Type.GetType(typeName);

            if (t == null)
            {
                int commaIndex = typeName.IndexOf(',');

                if (commaIndex != -1)
                {
                    // At design time, it's possible for us to reuse
                    // an assembly but add new types. The app domain
                    // will cache the assembly based on identity, however,
                    // so it could be looking in the previous version
                    // of the assembly and not finding the type. We work
                    // around this by looking for the non-assembly qualified
                    // name, which causes the domain to raise a type 
                    // resolve event.
                    //
                    t = Type.GetType(typeName.Substring(0, commaIndex));
                }
            }

            return t;
        }

        /// <summary>
        /// This method returns true if the data cache in this reflection 
        /// type descriptor has data in it.
        /// </summary>
        internal bool IsPopulated(Type type)
        {
            ReflectedTypeData td = GetTypeData(type, false);
            if (td != null)
            {
                return td.IsPopulated;
            }
            return false;
        }

        /// <summary>
        /// Static helper API around reflection to get and cache
        /// custom attributes. This does not recurse, but it will
        /// walk interfaces on the type. Interfaces are added 
        /// to the end, so merging should be done from length - 1
        /// to 0.
        /// </summary>
        internal static Attribute[] ReflectGetAttributes(Type type)
        {
            Hashtable attributeCache = AttributeCache;
            Attribute[] attrs = (Attribute[])attributeCache[type];
            if (attrs != null)
            {
                return attrs;
            }

            lock (s_internalSyncObject)
            {
                attrs = (Attribute[])attributeCache[type];
                if (attrs == null)
                {
                    // Get the type's attributes.
                    //
                    attrs = type.GetCustomAttributes(typeof(Attribute), false).OfType<Attribute>().ToArray();
                    attributeCache[type] = attrs;
                }
            }

            return attrs;
        }

        /// <summary>
        /// Static helper API around reflection to get and cache
        /// custom attributes. This does not recurse to the base class.
        /// </summary>
        internal static Attribute[] ReflectGetAttributes(MemberInfo member)
        {
            Hashtable attributeCache = AttributeCache;
            Attribute[] attrs = (Attribute[])attributeCache[member];
            if (attrs != null)
            {
                return attrs;
            }

            lock (s_internalSyncObject)
            {
                attrs = (Attribute[])attributeCache[member];
                if (attrs == null)
                {
                    // Get the member's attributes.
                    //
                    attrs = member.GetCustomAttributes(typeof(Attribute), false).OfType<Attribute>().ToArray();
                    attributeCache[member] = attrs;
                }
            }

            return attrs;
        }

        /// <summary>
        /// Static helper API around reflection to get and cache
        /// events. This does not recurse to the base class.
        /// </summary>
        private static EventDescriptor[] ReflectGetEvents(Type type)
        {
            Hashtable eventCache = EventCache;
            EventDescriptor[] events = (EventDescriptor[])eventCache[type];
            if (events != null)
            {
                return events;
            }

            lock (s_internalSyncObject)
            {
                events = (EventDescriptor[])eventCache[type];
                if (events == null)
                {
                    BindingFlags bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;

                    // Get the type's events. Events may have their add and
                    // remove methods individually overridden in a derived
                    // class, but at some point in the base class chain both
                    // methods must exist. If we find an event that doesn't
                    // have both add and remove, we skip it here, because it
                    // will be picked up in our base class scan.
                    //
                    EventInfo[] eventInfos = type.GetEvents(bindingFlags);
                    events = new EventDescriptor[eventInfos.Length];
                    int eventCount = 0;

                    for (int idx = 0; idx < eventInfos.Length; idx++)
                    {
                        EventInfo eventInfo = eventInfos[idx];

                        // GetEvents returns events that are on nonpublic types
                        // if those types are from our assembly. Screen these.
                        // 
                        if ((!(eventInfo.DeclaringType.IsPublic || eventInfo.DeclaringType.IsNestedPublic)) && (eventInfo.DeclaringType.Assembly == typeof(ReflectTypeDescriptionProvider).Assembly))
                        {
                            Debug.Fail("Hey, assumption holds true. Rip this assert.");
                            continue;
                        }

                        if (eventInfo.AddMethod != null && eventInfo.RemoveMethod != null)
                        {
                            events[eventCount++] = new ReflectEventDescriptor(type, eventInfo);
                        }
                    }

                    if (eventCount != events.Length)
                    {
                        EventDescriptor[] newEvents = new EventDescriptor[eventCount];
                        Array.Copy(events, 0, newEvents, 0, eventCount);
                        events = newEvents;
                    }

#if DEBUG
                    foreach (EventDescriptor dbgEvent in events)
                    {
                        Debug.Assert(dbgEvent != null, "Holes in event array for type " + type);
                    }
#endif
                    eventCache[type] = events;
                }
            }

            return events;
        }

        /// <summary>
        /// This performs the actual reflection needed to discover
        /// extender properties. If object caching is supported this
        /// will maintain a cache of property descriptors on the
        /// extender provider. Extender properties are actually two
        /// property descriptors in one. There is a chunk of per-class
        /// data in a ReflectPropertyDescriptor that defines the
        /// parameter types and get and set methods of the extended property,
        /// and there is an ExtendedPropertyDescriptor that combines this
        /// with an extender provider object to create what looks like a
        /// normal property. ReflectGetExtendedProperties maintains two 
        /// separate caches for these two sets:  a static one for the
        /// ReflectPropertyDescriptor values that don't change for each
        /// provider instance, and a per-provider cache that contains
        /// the ExtendedPropertyDescriptors.
        /// </summary>
        private static PropertyDescriptor[] ReflectGetExtendedProperties(IExtenderProvider provider)
        {
            IDictionary cache = TypeDescriptor.GetCache(provider);
            PropertyDescriptor[] properties;

            if (cache != null)
            {
                properties = cache[s_extenderProviderPropertiesKey] as PropertyDescriptor[];
                if (properties != null)
                {
                    return properties;
                }
            }

            // Our per-instance cache missed. We have never seen this instance of the
            // extender provider before. See if we can find our class-based
            // property store.
            //
            Type providerType = provider.GetType();
            Hashtable extendedPropertyCache = ExtendedPropertyCache;
            ReflectPropertyDescriptor[] extendedProperties = (ReflectPropertyDescriptor[])extendedPropertyCache[providerType];
            if (extendedProperties == null)
            {
                lock (s_internalSyncObject)
                {
                    extendedProperties = (ReflectPropertyDescriptor[])extendedPropertyCache[providerType];

                    // Our class-based property store failed as well, so we need to build up the set of
                    // extended properties here.
                    //
                    if (extendedProperties == null)
                    {
                        AttributeCollection attributes = TypeDescriptor.GetAttributes(providerType);
                        List<ReflectPropertyDescriptor> extendedList = new List<ReflectPropertyDescriptor>(attributes.Count);

                        foreach (Attribute attr in attributes)
                        {
                            ProvidePropertyAttribute provideAttr = attr as ProvidePropertyAttribute;

                            if (provideAttr != null)
                            {
                                Type receiverType = GetTypeFromName(provideAttr.ReceiverTypeName);

                                if (receiverType != null)
                                {
                                    MethodInfo getMethod = providerType.GetMethod("Get" + provideAttr.PropertyName, new Type[] { receiverType });

                                    if (getMethod != null && !getMethod.IsStatic && getMethod.IsPublic)
                                    {
                                        MethodInfo setMethod = providerType.GetMethod("Set" + provideAttr.PropertyName, new Type[] { receiverType, getMethod.ReturnType });

                                        if (setMethod != null && (setMethod.IsStatic || !setMethod.IsPublic))
                                        {
                                            setMethod = null;
                                        }

                                        extendedList.Add(new ReflectPropertyDescriptor(providerType, provideAttr.PropertyName, getMethod.ReturnType, receiverType, getMethod, setMethod, null));
                                    }
                                }
                            }
                        }

                        extendedProperties = new ReflectPropertyDescriptor[extendedList.Count];
                        extendedList.CopyTo(extendedProperties, 0);
                        extendedPropertyCache[providerType] = extendedProperties;
                    }
                }
            }

            // Now that we have our extended properties we can build up a list of callable properties. These can be 
            // returned to the user.
            //
            properties = new PropertyDescriptor[extendedProperties.Length];
            for (int idx = 0; idx < extendedProperties.Length; idx++)
            {
                ReflectPropertyDescriptor rpd = extendedProperties[idx];

                properties[idx] = new ExtendedPropertyDescriptor(rpd, rpd.ExtenderGetReceiverType(), provider, null);
            }

            if (cache != null)
            {
                cache[s_extenderProviderPropertiesKey] = properties;
            }

            return properties;
        }

        /// <summary>
        /// Static helper API around reflection to get and cache
        /// properties. This does not recurse to the base class.
        /// </summary>
        private static PropertyDescriptor[] ReflectGetProperties(Type type)
        {
            Hashtable propertyCache = PropertyCache;
            PropertyDescriptor[] properties = (PropertyDescriptor[])propertyCache[type];
            if (properties != null)
            {
                return properties;
            }

            lock (s_internalSyncObject)
            {
                properties = (PropertyDescriptor[])propertyCache[type];

                if (properties == null)
                {
                    BindingFlags bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;

                    // Get the type's properties. Properties may have their
                    // get and set methods individually overridden in a derived
                    // class, so if we find a missing method we need to walk
                    // down the base class chain to find it. We actually merge
                    // "new" properties of the same name, so we must preserve
                    // the member info for each method individually.
                    //
                    PropertyInfo[] propertyInfos = type.GetProperties(bindingFlags);
                    properties = new PropertyDescriptor[propertyInfos.Length];
                    int propertyCount = 0;


                    for (int idx = 0; idx < propertyInfos.Length; idx++)
                    {
                        PropertyInfo propertyInfo = propertyInfos[idx];

                        // Today we do not support parameterized properties.
                        // 
                        if (propertyInfo.GetIndexParameters().Length > 0)
                        {
                            continue;
                        }

                        MethodInfo getMethod = propertyInfo.GetGetMethod(nonPublic: false);
                        MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic: false);
                        string name = propertyInfo.Name;

                        // If the property only overrode "set", then we don't
                        // pick it up here. Rather, we just merge it in from
                        // the base class list.


                        // If a property had at least a get method, we consider it. We don't
                        // consider write-only properties.
                        //
                        if (getMethod != null)
                        {
                            properties[propertyCount++] = new ReflectPropertyDescriptor(type, name,
                                                                                    propertyInfo.PropertyType,
                                                                                    propertyInfo, getMethod,
                                                                                    setMethod, null);
                        }
                    }

                    if (propertyCount != properties.Length)
                    {
                        PropertyDescriptor[] newProperties = new PropertyDescriptor[propertyCount];
                        Array.Copy(properties, 0, newProperties, 0, propertyCount);
                        properties = newProperties;
                    }

                    Debug.Assert(!properties.Any(dbgProp => dbgProp == null), $"Holes in property array for type {type}");

                    propertyCache[type] = properties;
                }
            }

            return properties;
        }

        /// <summary>
        /// Refreshes the contents of this type descriptor. This does not
        /// actually requery, but it will clear our state so the next
        /// query re-populates.
        /// </summary>
        internal void Refresh(Type type)
        {
            ReflectedTypeData td = GetTypeData(type, false);
            td?.Refresh();
        }

        /// <summary> 
        /// Searches the provided intrinsic hashtable for a match with the object type. 
        /// At the beginning, the hashtable contains types for the various converters. 
        /// As this table is searched, the types for these objects 
        /// are replaced with instances, so we only create as needed. This method 
        /// does the search up the base class hierarchy and will create instances 
        /// for types as needed. These instances are stored back into the table 
        /// for the base type, and for the original component type, for fast access. 
        /// </summary> 
        private static object SearchIntrinsicTable(Hashtable table, Type callingType)
        {
            object hashEntry = null;

            // We take a lock on this table. Nothing in this code calls out to
            // other methods that lock, so it should be fairly safe to grab this
            // lock. Also, this allows multiple intrinsic tables to be searched
            // at once.
            //
            lock (table)
            {
                Type baseType = callingType;
                while (baseType != null && baseType != typeof(object))
                {
                    hashEntry = table[baseType];

                    // If the entry is a late-bound type, then try to
                    // resolve it.
                    //
                    string typeString = hashEntry as string;
                    if (typeString != null)
                    {
                        hashEntry = Type.GetType(typeString);
                        if (hashEntry != null)
                        {
                            table[baseType] = hashEntry;
                        }
                    }

                    if (hashEntry != null)
                    {
                        break;
                    }

                    baseType = baseType.BaseType;
                }

                // Now make a scan through each value in the table, looking for interfaces.
                // If we find one, see if the object implements the interface.
                //
                if (hashEntry == null)
                {
                    // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                    IDictionaryEnumerator e = table.GetEnumerator();
                    while (e.MoveNext())
                    {
                        DictionaryEntry de = e.Entry;
                        Type keyType = de.Key as Type;

                        if (keyType != null && keyType.IsInterface && keyType.IsAssignableFrom(callingType))
                        {
                            hashEntry = de.Value;
                            string typeString = hashEntry as string;

                            if (typeString != null)
                            {
                                hashEntry = Type.GetType(typeString);
                                if (hashEntry != null)
                                {
                                    table[callingType] = hashEntry;
                                }
                            }

                            if (hashEntry != null)
                            {
                                break;
                            }
                        }
                    }
                }

                // Special case converters
                //
                if (hashEntry == null)
                {
                    if (callingType.IsGenericType && callingType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        // Check if it is a nullable value
                        hashEntry = table[s_intrinsicNullableKey];
                    }
                    else if (callingType.IsInterface)
                    {
                        // Finally, check to see if the component type is some unknown interface.
                        // We have a custom converter for that.
                        hashEntry = table[s_intrinsicReferenceKey];
                    }
                }

                // Interfaces do not derive from object, so we
                // must handle the case of no hash entry here.
                //
                if (hashEntry == null)
                {
                    hashEntry = table[typeof(object)];
                }

                // If the entry is a type, create an instance of it and then
                // replace the entry. This way we only need to create once.
                // We can only do this if the object doesn't want a type
                // in its constructor.
                //
                Type type = hashEntry as Type;

                if (type != null)
                {
                    hashEntry = CreateInstance(type, callingType);
                    if (type.GetConstructor(s_typeConstructor) == null)
                    {
                        table[callingType] = hashEntry;
                    }
                }
            }

            return hashEntry;
        }
    }
}
