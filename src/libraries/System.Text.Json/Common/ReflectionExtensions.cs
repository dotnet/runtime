// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
#if !BUILDING_SOURCE_GENERATOR
using System.Diagnostics.CodeAnalysis;
#endif

namespace System.Text.Json.Reflection
{
    internal static partial class ReflectionExtensions
    {
        // Immutable collection types.
        private const string ImmutableArrayGenericTypeName = "System.Collections.Immutable.ImmutableArray`1";
        private const string ImmutableListGenericTypeName = "System.Collections.Immutable.ImmutableList`1";
        private const string ImmutableListGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableList`1";
        private const string ImmutableStackGenericTypeName = "System.Collections.Immutable.ImmutableStack`1";
        private const string ImmutableStackGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableStack`1";
        private const string ImmutableQueueGenericTypeName = "System.Collections.Immutable.ImmutableQueue`1";
        private const string ImmutableQueueGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableQueue`1";
        private const string ImmutableSortedSetGenericTypeName = "System.Collections.Immutable.ImmutableSortedSet`1";
        private const string ImmutableHashSetGenericTypeName = "System.Collections.Immutable.ImmutableHashSet`1";
        private const string ImmutableSetGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableSet`1";
        private const string ImmutableDictionaryGenericTypeName = "System.Collections.Immutable.ImmutableDictionary`2";
        private const string ImmutableDictionaryGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableDictionary`2";
        private const string ImmutableSortedDictionaryGenericTypeName = "System.Collections.Immutable.ImmutableSortedDictionary`2";

        // Immutable collection builder types.
        private const string ImmutableArrayTypeName = "System.Collections.Immutable.ImmutableArray";
        private const string ImmutableListTypeName = "System.Collections.Immutable.ImmutableList";
        private const string ImmutableStackTypeName = "System.Collections.Immutable.ImmutableStack";
        private const string ImmutableQueueTypeName = "System.Collections.Immutable.ImmutableQueue";
        private const string ImmutableSortedSetTypeName = "System.Collections.Immutable.ImmutableSortedSet";
        private const string ImmutableHashSetTypeName = "System.Collections.Immutable.ImmutableHashSet";
        private const string ImmutableDictionaryTypeName = "System.Collections.Immutable.ImmutableDictionary";
        private const string ImmutableSortedDictionaryTypeName = "System.Collections.Immutable.ImmutableSortedDictionary";

        public const string CreateRangeMethodName = "CreateRange";

        public static Type? GetCompatibleGenericBaseClass(
            this Type type,
            Type baseType,
            bool sourceGenType = false)
        {
            Debug.Assert(baseType.IsGenericType);
            Debug.Assert(!baseType.IsInterface);
            Debug.Assert(baseType == baseType.GetGenericTypeDefinition());

            Type? baseTypeToCheck = type;

            while (baseTypeToCheck != null && baseTypeToCheck != typeof(object))
            {
                if (baseTypeToCheck.IsGenericType)
                {
                    Type genericTypeToCheck = baseTypeToCheck.GetGenericTypeDefinition();
                    if (genericTypeToCheck == baseType ||
                        (sourceGenType && (OpenGenericTypesHaveSamePrefix(baseType, genericTypeToCheck))))
                    {
                        return baseTypeToCheck;
                    }
                }

                baseTypeToCheck = baseTypeToCheck.BaseType;
            }

            return null;
        }

#if !BUILDING_SOURCE_GENERATOR
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The 'interfaceType' must exist and so trimmer kept it. In which case " +
                "It also kept it on any type which implements it. The below call to GetInterfaces " +
                "may return fewer results when trimmed but it will return the 'interfaceType' " +
                "if the type implemented it, even after trimming.")]
#endif
        public static Type? GetCompatibleGenericInterface(this Type type, Type? interfaceType)
        {
            if (interfaceType is null)
            {
                return null;
            }

            Debug.Assert(interfaceType.IsGenericType);
            Debug.Assert(interfaceType.IsInterface);
            Debug.Assert(interfaceType == interfaceType.GetGenericTypeDefinition());

            Type interfaceToCheck = type;

            if (interfaceToCheck.IsGenericType)
            {
                interfaceToCheck = interfaceToCheck.GetGenericTypeDefinition();
            }

            if (interfaceToCheck == interfaceType)
            {
                return type;
            }

            foreach (Type typeToCheck in type.GetInterfaces())
            {
                if (typeToCheck.IsGenericType)
                {
                    Type genericInterfaceToCheck = typeToCheck.GetGenericTypeDefinition();
                    if (genericInterfaceToCheck == interfaceType)
                    {
                        return typeToCheck;
                    }
                }
            }

            return null;
        }

        public static bool IsImmutableDictionaryType(this Type type, bool sourceGenType = false)
        {
            if (!type.IsGenericType || !type.Assembly.FullName!.StartsWith("System.Collections.Immutable", StringComparison.Ordinal))
            {
                return false;
            }

            switch (GetBaseNameFromGenericType(type, sourceGenType))
            {
                case ImmutableDictionaryGenericTypeName:
                case ImmutableDictionaryGenericInterfaceTypeName:
                case ImmutableSortedDictionaryGenericTypeName:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsImmutableEnumerableType(this Type type, bool sourceGenType = false)
        {
            if (!type.IsGenericType || !type.Assembly.FullName!.StartsWith("System.Collections.Immutable", StringComparison.Ordinal))
            {
                return false;
            }

            switch (GetBaseNameFromGenericType(type, sourceGenType))
            {
                case ImmutableArrayGenericTypeName:
                case ImmutableListGenericTypeName:
                case ImmutableListGenericInterfaceTypeName:
                case ImmutableStackGenericTypeName:
                case ImmutableStackGenericInterfaceTypeName:
                case ImmutableQueueGenericTypeName:
                case ImmutableQueueGenericInterfaceTypeName:
                case ImmutableSortedSetGenericTypeName:
                case ImmutableHashSetGenericTypeName:
                case ImmutableSetGenericInterfaceTypeName:
                    return true;
                default:
                    return false;
            }
        }

        public static string? GetImmutableDictionaryConstructingTypeName(this Type type, bool sourceGenType = false)
        {
            Debug.Assert(type.IsImmutableDictionaryType(sourceGenType));

            // Use the generic type definition of the immutable collection to determine
            // an appropriate constructing type, i.e. a type that we can invoke the
            // `CreateRange<T>` method on, which returns the desired immutable collection.
            switch (GetBaseNameFromGenericType(type, sourceGenType))
            {
                case ImmutableDictionaryGenericTypeName:
                case ImmutableDictionaryGenericInterfaceTypeName:
                    return ImmutableDictionaryTypeName;
                case ImmutableSortedDictionaryGenericTypeName:
                    return ImmutableSortedDictionaryTypeName;
                default:
                    // We verified that the type is an immutable collection, so the
                    // generic definition is one of the above.
                    return null;
            }
        }

        public static string? GetImmutableEnumerableConstructingTypeName(this Type type, bool sourceGenType = false)
        {
            Debug.Assert(type.IsImmutableEnumerableType(sourceGenType));

            // Use the generic type definition of the immutable collection to determine
            // an appropriate constructing type, i.e. a type that we can invoke the
            // `CreateRange<T>` method on, which returns the desired immutable collection.
            switch (GetBaseNameFromGenericType(type, sourceGenType))
            {
                case ImmutableArrayGenericTypeName:
                    return ImmutableArrayTypeName;
                case ImmutableListGenericTypeName:
                case ImmutableListGenericInterfaceTypeName:
                    return ImmutableListTypeName;
                case ImmutableStackGenericTypeName:
                case ImmutableStackGenericInterfaceTypeName:
                    return ImmutableStackTypeName;
                case ImmutableQueueGenericTypeName:
                case ImmutableQueueGenericInterfaceTypeName:
                    return ImmutableQueueTypeName;
                case ImmutableSortedSetGenericTypeName:
                    return ImmutableSortedSetTypeName;
                case ImmutableHashSetGenericTypeName:
                case ImmutableSetGenericInterfaceTypeName:
                    return ImmutableHashSetTypeName;
                default:
                    // We verified that the type is an immutable collection, so the
                    // generic definition is one of the above.
                    return null;
            }
        }

        private static bool OpenGenericTypesHaveSamePrefix(Type t1, Type t2)
            => t1.FullName == GetBaseNameFromGenericTypeDef(t2);

        private static string GetBaseNameFromGenericType(Type genericType, bool sourceGenType)
        {
            Type genericTypeDef = genericType.GetGenericTypeDefinition();
            return sourceGenType ? GetBaseNameFromGenericTypeDef(genericTypeDef) : genericTypeDef.FullName!;
        }

        private static string GetBaseNameFromGenericTypeDef(Type genericTypeDef)
        {
            Debug.Assert(genericTypeDef.IsGenericType);
            string fullName = genericTypeDef.FullName!;
            int length = fullName.IndexOf("`") + 2;
            return fullName.Substring(0, length);
        }

        public static bool IsVirtual(this PropertyInfo? propertyInfo)
        {
            Debug.Assert(propertyInfo != null);
            return propertyInfo != null && (propertyInfo.GetMethod?.IsVirtual == true || propertyInfo.SetMethod?.IsVirtual == true);
        }

        public static bool IsKeyValuePair(this Type type, Type? keyValuePairType = null)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            // Work around not being able to use typeof(KeyValuePair<,>) directly during compile-time src gen type analysis.
            keyValuePairType ??= typeof(KeyValuePair<,>);

            Type generic = type.GetGenericTypeDefinition();
            return generic == keyValuePairType;
        }

        public static bool TryGetDeserializationConstructor(
#if !BUILDING_SOURCE_GENERATOR
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
            this Type type,
            bool useDefaultCtorInAnnotatedStructs,
            out ConstructorInfo? deserializationCtor)
        {
            ConstructorInfo? ctorWithAttribute = null;
            ConstructorInfo? publicParameterlessCtor = null;
            ConstructorInfo? lonePublicCtor = null;

            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            if (constructors.Length == 1)
            {
                lonePublicCtor = constructors[0];
            }

            foreach (ConstructorInfo constructor in constructors)
            {
                if (HasJsonConstructorAttribute(constructor))
                {
                    if (ctorWithAttribute != null)
                    {
                        deserializationCtor = null;
                        return false;
                    }

                    ctorWithAttribute = constructor;
                }
                else if (constructor.GetParameters().Length == 0)
                {
                    publicParameterlessCtor = constructor;
                }
            }

            // For correctness, throw if multiple ctors have [JsonConstructor], even if one or more are non-public.
            ConstructorInfo? dummyCtorWithAttribute = ctorWithAttribute;

            constructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (ConstructorInfo constructor in constructors)
            {
                if (HasJsonConstructorAttribute(constructor))
                {
                    if (dummyCtorWithAttribute != null)
                    {
                        deserializationCtor = null;
                        return false;
                    }

                    dummyCtorWithAttribute = constructor;
                }
            }

            // Structs will use default constructor if attribute isn't used.
            if (useDefaultCtorInAnnotatedStructs && type.IsValueType && ctorWithAttribute == null)
            {
                deserializationCtor = null;
                return true;
            }

            deserializationCtor = ctorWithAttribute ?? publicParameterlessCtor ?? lonePublicCtor;
            return true;
        }

        public static object? GetDefaultValue(this ParameterInfo parameterInfo)
        {
            object? defaultValue = parameterInfo.DefaultValue;

            // DBNull.Value is sometimes used as the default value (returned by reflection) of nullable params in place of null.
            if (defaultValue == DBNull.Value && parameterInfo.ParameterType != typeof(DBNull))
            {
                return null;
            }

            return defaultValue;
        }
    }
}
