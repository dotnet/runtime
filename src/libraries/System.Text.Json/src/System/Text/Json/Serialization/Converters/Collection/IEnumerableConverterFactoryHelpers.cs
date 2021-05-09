// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    internal static class IEnumerableConverterFactoryHelpers
    {
        // System.Text.Json doesn't take a direct reference to System.Collections.Immutable so
        // any netstandard2.0 consumers don't need to reference System.Collections.Immutable.
        // So instead, implement a "weak reference" by using strings to check for Immutable types.

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

        private const string CreateRangeMethodName = "CreateRange";
        private const string CreateRangeMethodNameForEnumerable = "CreateRange`1";
        private const string CreateRangeMethodNameForDictionary = "CreateRange`2";

        private const string ImmutableCollectionsAssembly = "System.Collections.Immutable";

        internal static Type? GetCompatibleGenericBaseClass(this Type type, Type baseType)
        {
            Debug.Assert(baseType.IsGenericType);
            Debug.Assert(!baseType.IsInterface);
            Debug.Assert(baseType == baseType.GetGenericTypeDefinition());

            Type? baseTypeToCheck = type;

            while (baseTypeToCheck != null && baseTypeToCheck != JsonTypeInfo.ObjectType)
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

        internal static Type? GetCompatibleGenericInterface(this Type type, Type interfaceType)
        {
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
            if (!type.IsGenericType || !type.Assembly.FullName!.StartsWith("System.Collections.Immutable,", StringComparison.Ordinal))
            {
                return false;
            }

            switch (type.GetGenericTypeDefinition().FullName)
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
            if (!type.IsGenericType|| !type.Assembly.FullName!.StartsWith("System.Collections.Immutable,", StringComparison.Ordinal))
            {
                return false;
            }

            switch (type.GetGenericTypeDefinition().FullName)
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

        [DynamicDependency(CreateRangeMethodNameForEnumerable, ImmutableArrayTypeName, ImmutableCollectionsAssembly)]
        [DynamicDependency(CreateRangeMethodNameForEnumerable, ImmutableListTypeName, ImmutableCollectionsAssembly)]
        [DynamicDependency(CreateRangeMethodNameForEnumerable, ImmutableStackTypeName, ImmutableCollectionsAssembly)]
        [DynamicDependency(CreateRangeMethodNameForEnumerable, ImmutableQueueTypeName, ImmutableCollectionsAssembly)]
        [DynamicDependency(CreateRangeMethodNameForEnumerable, ImmutableSortedSetTypeName, ImmutableCollectionsAssembly)]
        [DynamicDependency(CreateRangeMethodNameForEnumerable, ImmutableHashSetTypeName, ImmutableCollectionsAssembly)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "The CreateRange method is preserved by the DynamicDependency attribute.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "Immutable collections don't have trimming annotations.")]
        public static MethodInfo GetImmutableEnumerableCreateRangeMethod(this Type type, Type elementType)
        {
            Type? constructingType = GetImmutableEnumerableConstructingType(type);
            if (constructingType != null)
            {
                MethodInfo[] constructingTypeMethods = constructingType.GetMethods();
                foreach (MethodInfo method in constructingTypeMethods)
                {
                    if (method.Name == CreateRangeMethodName &&
                        method.GetParameters().Length == 1 &&
                        method.IsGenericMethod &&
                        method.GetGenericArguments().Length == 1)
                    {
                        return method.MakeGenericMethod(elementType);
                    }
                }
            }

            ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
            return null!;
        }

        [DynamicDependency(CreateRangeMethodNameForDictionary, ImmutableDictionaryTypeName, ImmutableCollectionsAssembly)]
        [DynamicDependency(CreateRangeMethodNameForDictionary, ImmutableSortedDictionaryTypeName, ImmutableCollectionsAssembly)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "The CreateRange method is preserved by the DynamicDependency attribute.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "Immutable collections don't have trimming annotations.")]
        public static MethodInfo GetImmutableDictionaryCreateRangeMethod(this Type type, Type keyType, Type valueType)
        {
            Type? constructingType = GetImmutableDictionaryConstructingType(type);
            if (constructingType != null)
            {
                MethodInfo[] constructingTypeMethods = constructingType.GetMethods();
                foreach (MethodInfo method in constructingTypeMethods)
                {
                    if (method.Name == CreateRangeMethodName &&
                        method.GetParameters().Length == 1 &&
                        method.IsGenericMethod &&
                        method.GetGenericArguments().Length == 2)
                    {
                        return method.MakeGenericMethod(keyType, valueType);
                    }
                }
            }

            ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
            return null!;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Json can't take a direct dependency on Collections.Immutable." +
            "The types used here will be preserved by the DynamicDependency attributes on the only caller - GetImmutableEnumerableCreateRangeMethod.")]
        private static Type? GetImmutableEnumerableConstructingType(Type type)
        {
            Debug.Assert(type.IsImmutableEnumerableType());

            // Use the generic type definition of the immutable collection to determine
            // an appropriate constructing type, i.e. a type that we can invoke the
            // `CreateRange<T>` method on, which returns the desired immutable collection.
            Type underlyingType = type.GetGenericTypeDefinition();
            string constructingTypeName;

            switch (underlyingType.FullName)
            {
                case ImmutableArrayGenericTypeName:
                    constructingTypeName = ImmutableArrayTypeName;
                    break;
                case ImmutableListGenericTypeName:
                case ImmutableListGenericInterfaceTypeName:
                    constructingTypeName = ImmutableListTypeName;
                    break;
                case ImmutableStackGenericTypeName:
                case ImmutableStackGenericInterfaceTypeName:
                    constructingTypeName = ImmutableStackTypeName;
                    break;
                case ImmutableQueueGenericTypeName:
                case ImmutableQueueGenericInterfaceTypeName:
                    constructingTypeName = ImmutableQueueTypeName;
                    break;
                case ImmutableSortedSetGenericTypeName:
                    constructingTypeName = ImmutableSortedSetTypeName;
                    break;
                case ImmutableHashSetGenericTypeName:
                case ImmutableSetGenericInterfaceTypeName:
                    constructingTypeName = ImmutableHashSetTypeName;
                    break;
                default:
                    // We verified that the type is an immutable collection, so the
                    // generic definition is one of the above.
                    return null;
            }

            return underlyingType.Assembly.GetType(constructingTypeName);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Json can't take a direct dependency on Collections.Immutable." +
            "The types used here will be preserved by the DynamicDependency attributes on the only caller - GetImmutableDictionaryCreateRangeMethod.")]
        private static Type? GetImmutableDictionaryConstructingType(Type type)
        {
            Debug.Assert(type.IsImmutableDictionaryType());

            // Use the generic type definition of the immutable collection to determine
            // an appropriate constructing type, i.e. a type that we can invoke the
            // `CreateRange<T>` method on, which returns the desired immutable collection.
            Type underlyingType = type.GetGenericTypeDefinition();
            string constructingTypeName;

            switch (underlyingType.FullName)
            {
                case ImmutableDictionaryGenericTypeName:
                case ImmutableDictionaryGenericInterfaceTypeName:
                    constructingTypeName = ImmutableDictionaryTypeName;
                    break;
                case ImmutableSortedDictionaryGenericTypeName:
                    constructingTypeName = ImmutableSortedDictionaryTypeName;
                    break;
                default:
                    // We verified that the type is an immutable collection, so the
                    // generic definition is one of the above.
                    return null;
            }

            return underlyingType.Assembly.GetType(constructingTypeName);
        }

        public static bool IsNonGenericStackOrQueue(this Type type)
        {
#if BUILDING_INBOX_LIBRARY
            // Optimize for linking scenarios where mscorlib is trimmed out.
            const string stackTypeName = "System.Collections.Stack, System.Collections.NonGeneric";
            const string queueTypeName = "System.Collections.Queue, System.Collections.NonGeneric";
#else
            const string stackTypeName = "System.Collections.Stack, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            const string queueTypeName = "System.Collections.Queue, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
#endif

            Type? stackType = GetTypeIfExists(stackTypeName);
            if (stackType?.IsAssignableFrom(type) == true)
            {
                return true;
            }

            Type? queueType = GetTypeIfExists(queueTypeName);
            if (queueType?.IsAssignableFrom(type) == true)
            {
                return true;
            }

            return false;
        }

        // This method takes an unannotated string which makes linker reflection analysis lose track of the type we are
        // looking for. This indirection allows the removal of the type if it is not used in the calling application.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:TypeGetType",
            Justification = "This method exists to allow for 'weak references' to the Stack and Queue types. If those types are used in the app, " +
            "they will be preserved by the app and Type.GetType will return them. If those types are not used in the app, we don't want to preserve them here.")]
        private static Type? GetTypeIfExists(string name) => Type.GetType(name, false);
    }
}
