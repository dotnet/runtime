// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace System.ComponentModel
{
    /// <devdoc>
    ///     This type description provider provides type information through 
    ///     reflection.  Unless someone has provided a custom type description
    ///     provider for a type or instance, or unless an instance implements
    ///     ICustomTypeDescriptor, any query for type information will go through
    ///     this class.  There should be a single instance of this class associated
    ///     with "object", as it can provide all type information for any type.
    /// </devdoc>
    internal sealed class ReflectTypeDescriptionProvider
    {
        // This is where we store the various converters for the intrinsic types.
        //
        private static volatile Dictionary<object, object> s_intrinsicConverters;

        // For converters, etc that are bound to class attribute data, rather than a class
        // type, we have special key sentinel values that we put into the hash table.
        //
        private static object s_intrinsicNullableKey = new object();

        private static object s_syncObject = new object();

        /// <devdoc>
        ///     Creates a new ReflectTypeDescriptionProvider.  The type is the
        ///     type we will obtain type information for.
        /// </devdoc>
        internal ReflectTypeDescriptionProvider()
        {
        }

        /// <devdoc> 
        ///      This is a table we create for intrinsic types. 
        ///      There should be entries here ONLY for intrinsic 
        ///      types, as all other types we should be able to 
        ///      add attributes directly as metadata. 
        /// </devdoc> 
        private static Dictionary<object, object> IntrinsicTypeConverters
        {
            get
            {
                // It is not worth taking a lock for this -- worst case of a collision
                // would build two tables, one that garbage collects very quickly.
                //
                if (ReflectTypeDescriptionProvider.s_intrinsicConverters == null)
                {
                    Dictionary<object, object> temp = new Dictionary<object, object>();

                    // Add the intrinsics
                    //
                    temp[typeof(bool)] = typeof(BooleanConverter);
                    temp[typeof(byte)] = typeof(ByteConverter);
                    temp[typeof(SByte)] = typeof(SByteConverter);
                    temp[typeof(char)] = typeof(CharConverter);
                    temp[typeof(double)] = typeof(DoubleConverter);
                    temp[typeof(string)] = typeof(StringConverter);
                    temp[typeof(int)] = typeof(Int32Converter);
                    temp[typeof(short)] = typeof(Int16Converter);
                    temp[typeof(long)] = typeof(Int64Converter);
                    temp[typeof(float)] = typeof(SingleConverter);
                    temp[typeof(UInt16)] = typeof(UInt16Converter);
                    temp[typeof(UInt32)] = typeof(UInt32Converter);
                    temp[typeof(UInt64)] = typeof(UInt64Converter);
                    temp[typeof(object)] = typeof(TypeConverter);
                    temp[typeof(void)] = typeof(TypeConverter);
                    temp[typeof(DateTime)] = typeof(DateTimeConverter);
                    temp[typeof(DateTimeOffset)] = typeof(DateTimeOffsetConverter);
                    temp[typeof(Decimal)] = typeof(DecimalConverter);
                    temp[typeof(TimeSpan)] = typeof(TimeSpanConverter);
                    temp[typeof(Guid)] = typeof(GuidConverter);
                    temp[typeof(Array)] = typeof(ArrayConverter);
                    temp[typeof(ICollection)] = typeof(CollectionConverter);
                    temp[typeof(Enum)] = typeof(EnumConverter);

                    // Special cases for things that are not bound to a specific type
                    //
                    temp[ReflectTypeDescriptionProvider.s_intrinsicNullableKey] = typeof(NullableConverter);

                    ReflectTypeDescriptionProvider.s_intrinsicConverters = temp;
                }
                return ReflectTypeDescriptionProvider.s_intrinsicConverters;
            }
        }

        /// <devdoc> 
        ///     Helper method to create type converters. This checks to see if the
        ///     type implements a Type constructor, and if it does it invokes that ctor. 
        ///     Otherwise, it just tries to create the type.
        /// </devdoc> 
        private static object CreateInstance(Type objectType, Type parameterType, ref bool noTypeConstructor)
        {
            ConstructorInfo typeConstructor = null;
            noTypeConstructor = true;

            foreach (ConstructorInfo constructor in objectType.GetTypeInfo().DeclaredConstructors)
            {
                if (!constructor.IsPublic)
                {
                    continue;
                }

                // This is the signature we look for when creating types that are generic, but
                // want to know what type they are dealing with.  Enums are a good example of this;
                // there is one enum converter that can work with all enums, but it needs to know
                // the type of enum it is dealing with.
                //
                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length != 1 || !parameters[0].ParameterType.Equals(typeof(Type)))
                {
                    continue;
                }
                typeConstructor = constructor;
                break;
            }

            if (typeConstructor != null)
            {
                noTypeConstructor = false;
                return typeConstructor.Invoke(new object[] { parameterType });
            }

            return Activator.CreateInstance(objectType);
        }

        private static TypeConverterAttribute GetTypeConverterAttributeIfAny(Type type)
        {
            foreach (TypeConverterAttribute attribute in type.GetTypeInfo().GetCustomAttributes<TypeConverterAttribute>(false))
            {
                return attribute;
            }
            return null;
        }

        /// <devdoc>
        ///    Gets a type converter for the specified type.
        /// </devdoc>
        internal static TypeConverter GetConverter(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            // Check the cached TypeConverter dictionary for an exact match of the given type.
            object ans = SearchIntrinsicTable_ExactTypeMatch(type);
            if (ans != null)
                return (TypeConverter)ans;

            // Obtaining attributes follows a very critical order: we must take care that
            // we merge attributes the right way.  Consider this:
            //
            // [A4]
            // interface IBase;
            //
            // [A3]
            // interface IDerived;
            //
            // [A2]
            // class Base : IBase;
            //
            // [A1]
            // class Derived : Base, IDerived
            //
            // We are retreving attributes in the following order:  A1 - A4.
            // Interfaces always lose to types, and interfaces and types
            // must be looked up in the same order.
            TypeConverterAttribute converterAttribute = ReflectTypeDescriptionProvider.GetTypeConverterAttributeIfAny(type);
            if (converterAttribute == null)
            {
                Type baseType = type.GetTypeInfo().BaseType;

                while (baseType != null && baseType != typeof(object))
                {
                    converterAttribute = ReflectTypeDescriptionProvider.GetTypeConverterAttributeIfAny(baseType);
                    if (converterAttribute != null)
                    {
                        break;
                    }
                    baseType = baseType.GetTypeInfo().BaseType;
                }
            }

            if (converterAttribute == null)
            {
                IEnumerable<Type> interfaces = type.GetTypeInfo().ImplementedInterfaces;
                foreach (Type iface in interfaces)
                {
                    // only do this for public interfaces.
                    //
                    if ((iface.GetTypeInfo().Attributes & (TypeAttributes.Public | TypeAttributes.NestedPublic)) != 0)
                    {
                        converterAttribute = GetTypeConverterAttributeIfAny(iface);
                        if (converterAttribute != null)
                        {
                            break;
                        }
                    }
                }
            }

            if (converterAttribute != null)
            {
                Type converterType = ReflectTypeDescriptionProvider.GetTypeFromName(converterAttribute.ConverterTypeName, type);
                if (converterType != null && typeof(TypeConverter).GetTypeInfo().IsAssignableFrom(converterType.GetTypeInfo()))
                {
                    bool noTypeConstructor = true;
                    object instance = (TypeConverter)ReflectTypeDescriptionProvider.CreateInstance(converterType, type, ref noTypeConstructor);
                    if (noTypeConstructor)
                        ReflectTypeDescriptionProvider.IntrinsicTypeConverters[type] = instance;
                    return (TypeConverter)instance;
                }
            }

            // We did not get a converter. Traverse up the base class chain until
            // we find one in the stock hashtable.
            //
            return (TypeConverter)ReflectTypeDescriptionProvider.SearchIntrinsicTable(type);
        }

        /// <devdoc>
        ///     Retrieve a type from a name, if the name is not a fully qualified assembly name, then 
        ///     look for this type name in the same assembly as the "type" parameter is defined in.
        /// </devdoc>
        private static Type GetTypeFromName(string typeName, Type type)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            int commaIndex = typeName.IndexOf(',');
            Type t = null;

            if (commaIndex == -1)
            {
                t = type.GetTypeInfo().Assembly.GetType(typeName);
            }

            if (t == null)
            {
                t = Type.GetType(typeName);
            }

            return t;
        }

        /// <devdoc> 
        ///      Searches the provided intrinsic hashtable for a match with the object type. 
        ///      At the beginning, the hashtable contains types for the various converters. 
        ///      As this table is searched, the types for these objects 
        ///      are replaced with instances, so we only create as needed.  This method 
        ///      does the search up the base class hierarchy and will create instances 
        ///      for types as needed.  These instances are stored back into the table 
        ///      for the base type, and for the original component type, for fast access. 
        /// </devdoc> 
        private static object SearchIntrinsicTable(Type callingType)
        {
            object hashEntry = null;

            // We take a lock on this table.  Nothing in this code calls out to
            // other methods that lock, so it should be fairly safe to grab this
            // lock.  Also, this allows multiple intrinsic tables to be searched
            // at once.
            //
            lock (ReflectTypeDescriptionProvider.s_syncObject)
            {
                Type baseType = callingType;
                while (baseType != null && baseType != typeof(object))
                {
                    if (ReflectTypeDescriptionProvider.IntrinsicTypeConverters.TryGetValue(baseType, out hashEntry) && hashEntry != null)
                    {
                        break;
                    }

                    baseType = baseType.GetTypeInfo().BaseType;
                }

                TypeInfo callingTypeInfo = callingType.GetTypeInfo();

                // Now make a scan through each value in the table, looking for interfaces.
                // If we find one, see if the object implements the interface.
                //
                if (hashEntry == null)
                {
                    foreach (object key in ReflectTypeDescriptionProvider.IntrinsicTypeConverters.Keys)
                    {
                        Type keyType = key as Type;

                        if (keyType != null)
                        {
                            TypeInfo keyTypeInfo = keyType.GetTypeInfo();
                            if (keyTypeInfo.IsInterface && keyTypeInfo.IsAssignableFrom(callingTypeInfo))
                            {
                                ReflectTypeDescriptionProvider.IntrinsicTypeConverters.TryGetValue(key, out hashEntry);
                                string converterTypeString = hashEntry as string;

                                if (converterTypeString != null)
                                {
                                    hashEntry = Type.GetType(converterTypeString);
                                    if (hashEntry != null)
                                    {
                                        ReflectTypeDescriptionProvider.IntrinsicTypeConverters[callingType] = hashEntry;
                                    }
                                }

                                if (hashEntry != null)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                // Special case converter
                //
                if (hashEntry == null)
                {
                    if (callingTypeInfo.IsGenericType && callingTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        // Check if it is a nullable value
                        ReflectTypeDescriptionProvider.IntrinsicTypeConverters.TryGetValue(ReflectTypeDescriptionProvider.s_intrinsicNullableKey, out hashEntry);
                    }
                }

                // Interfaces do not derive from object, so we
                // must handle the case of no hash entry here.
                //
                if (hashEntry == null)
                {
                    ReflectTypeDescriptionProvider.IntrinsicTypeConverters.TryGetValue(typeof(object), out hashEntry);
                }

                // If the entry is a type, create an instance of it and then
                // replace the entry.  This way we only need to create once.
                // We can only do this if the object doesn't want a type
                // in its constructor.
                //
                Type type = hashEntry as Type;

                if (type != null)
                {
                    bool noTypeConstructor = true;
                    hashEntry = ReflectTypeDescriptionProvider.CreateInstance(type, callingType, ref noTypeConstructor);
                    if (noTypeConstructor)
                    {
                        ReflectTypeDescriptionProvider.IntrinsicTypeConverters[callingType] = hashEntry;
                    }
                }
            }
            return hashEntry;
        }

        private static object SearchIntrinsicTable_ExactTypeMatch(Type callingType)
        {
            object hashEntry = null;

            // We take a lock on this table.  Nothing in this code calls out to
            // other methods that lock, so it should be fairly safe to grab this
            // lock.  Also, this allows multiple intrinsic tables to be searched
            // at once.
            //
            lock (s_syncObject)
            {
                if (callingType != null && !IntrinsicTypeConverters.TryGetValue(callingType, out hashEntry))
                    return null;

                // If the entry is a type, create an instance of it and then
                // replace the entry.  This way we only need to create once.
                // We can only do this if the object doesn't want a type
                // in its constructor.
                Type type = hashEntry as Type;
                if (type != null)
                {
                    bool noTypeConstructor = true;
                    hashEntry = CreateInstance(type, callingType, ref noTypeConstructor);
                    if (noTypeConstructor)
                        IntrinsicTypeConverters[callingType] = hashEntry;
                }
            }
            return hashEntry;
        }
    }
}
