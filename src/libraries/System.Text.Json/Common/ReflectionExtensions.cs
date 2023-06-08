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

        public static Type? GetCompatibleGenericBaseClass(this Type type, Type? baseType)
        {
            if (baseType is null)
            {
                return null;
            }

            Debug.Assert(baseType.IsGenericType);
            Debug.Assert(!baseType.IsInterface);
            Debug.Assert(baseType == baseType.GetGenericTypeDefinition());

            Type? baseTypeToCheck = type;

            while (baseTypeToCheck != null && baseTypeToCheck != typeof(object))
            {
                if (baseTypeToCheck.IsGenericType)
                {
                    Type genericTypeToCheck = baseTypeToCheck.GetGenericTypeDefinition();
                    if (genericTypeToCheck == baseType)
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

        public static bool IsImmutableDictionaryType(this Type type)
        {
            if (!type.IsGenericType || !type.Assembly.FullName!.StartsWith("System.Collections.Immutable", StringComparison.Ordinal))
            {
                return false;
            }

            switch (GetBaseNameFromGenericType(type))
            {
                case ImmutableDictionaryGenericTypeName:
                case ImmutableDictionaryGenericInterfaceTypeName:
                case ImmutableSortedDictionaryGenericTypeName:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsImmutableEnumerableType(this Type type)
        {
            if (!type.IsGenericType || !type.Assembly.FullName!.StartsWith("System.Collections.Immutable", StringComparison.Ordinal))
            {
                return false;
            }

            switch (GetBaseNameFromGenericType(type))
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

        public static string? GetImmutableDictionaryConstructingTypeName(this Type type)
        {
            Debug.Assert(type.IsImmutableDictionaryType());

            // Use the generic type definition of the immutable collection to determine
            // an appropriate constructing type, i.e. a type that we can invoke the
            // `CreateRange<T>` method on, which returns the desired immutable collection.
            switch (GetBaseNameFromGenericType(type))
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

        public static string? GetImmutableEnumerableConstructingTypeName(this Type type)
        {
            Debug.Assert(type.IsImmutableEnumerableType());

            // Use the generic type definition of the immutable collection to determine
            // an appropriate constructing type, i.e. a type that we can invoke the
            // `CreateRange<T>` method on, which returns the desired immutable collection.
            switch (GetBaseNameFromGenericType(type))
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

        private static string GetBaseNameFromGenericType(Type genericType)
        {
            Type genericTypeDef = genericType.GetGenericTypeDefinition();
            return genericTypeDef.FullName!;
        }

        public static bool IsVirtual(this PropertyInfo propertyInfo)
        {
            return propertyInfo.GetMethod?.IsVirtual == true || propertyInfo.SetMethod?.IsVirtual == true;
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
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
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
            Type parameterType = parameterInfo.ParameterType;
            object? defaultValue = parameterInfo.DefaultValue;

            if (defaultValue is null)
            {
                return null;
            }

            // DBNull.Value is sometimes used as the default value (returned by reflection) of nullable params in place of null.
            if (defaultValue == DBNull.Value && parameterType != typeof(DBNull))
            {
                return null;
            }

            // Default values of enums or nullable enums are represented using the underlying type and need to be cast explicitly
            // cf. https://github.com/dotnet/runtime/issues/68647
            if (parameterType.IsEnum)
            {
                return Enum.ToObject(parameterType, defaultValue);
            }

            if (Nullable.GetUnderlyingType(parameterType) is Type underlyingType && underlyingType.IsEnum)
            {
                return Enum.ToObject(underlyingType, defaultValue);
            }

            return defaultValue;
        }

        /// <summary>
        /// Returns the type hierarchy for the given type, starting from the current type up to the base type(s) in the hierarchy.
        /// Interface hierarchies with multiple inheritance will return results using topological sorting.
        /// </summary>
        [RequiresUnreferencedCode("Should only be used by the reflection-based serializer.")]
        public static Type[] GetSortedTypeHierarchy(this Type type)
        {
            if (!type.IsInterface)
            {
                // Non-interface hierarchies are linear, just walk up to the earliest ancestor.

                var results = new List<Type>();
                for (Type? current = type; current != null; current = current.BaseType)
                {
                    results.Add(current);
                }

                return results.ToArray();
            }
            else
            {
                // Interface hierarchies support multiple inheritance.
                // For consistency with class hierarchy resolution order,
                // sort topologically from most derived to least derived.
                return JsonHelpers.TraverseGraphWithTopologicalSort(type, static t => t.GetInterfaces());
            }
        }
    }
}
