// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    internal partial class JsonClassInfo
    {
        public const string ImmutableNamespaceName = "System.Collections.Immutable";

        private const string EnumerableGenericInterfaceTypeName = "System.Collections.Generic.IEnumerable`1";
        private const string EnumerableInterfaceTypeName = "System.Collections.IEnumerable";

        private const string ListInterfaceTypeName = "System.Collections.IList";
        private const string ListGenericInterfaceTypeName = "System.Collections.Generic.IList`1";
        private const string ListGenericTypeName = "System.Collections.Generic.List`1";

        private const string CollectionGenericInterfaceTypeName = "System.Collections.Generic.ICollection`1";
        private const string CollectionInterfaceTypeName = "System.Collections.ICollection";

        private const string ReadOnlyListGenericInterfaceTypeName = "System.Collections.Generic.IReadOnlyList`1";

        private const string ReadOnlyCollectionGenericInterfaceTypeName = "System.Collections.Generic.IReadOnlyCollection`1";

        public const string HashtableTypeName = "System.Collections.Hashtable";
        public const string SortedListTypeName = "System.Collections.SortedList";

        public const string StackTypeName = "System.Collections.Stack";
        public const string StackGenericTypeName = "System.Collections.Generic.Stack`1";

        public const string QueueTypeName = "System.Collections.Queue";
        public const string QueueGenericTypeName = "System.Collections.Generic.Queue`1";

        public const string SetGenericInterfaceTypeName = "System.Collections.Generic.ISet`1";
        public const string SortedSetGenericTypeName = "System.Collections.Generic.SortedSet`1";
        public const string HashSetGenericTypeName = "System.Collections.Generic.HashSet`1";

        public const string LinkedListGenericTypeName = "System.Collections.Generic.LinkedList`1";

        public const string DictionaryInterfaceTypeName = "System.Collections.IDictionary";
        public const string DictionaryGenericTypeName = "System.Collections.Generic.Dictionary`2";
        public const string DictionaryGenericInterfaceTypeName = "System.Collections.Generic.IDictionary`2";
        public const string ReadOnlyDictionaryGenericInterfaceTypeName = "System.Collections.Generic.IReadOnlyDictionary`2";
        public const string SortedDictionaryGenericTypeName = "System.Collections.Generic.SortedDictionary`2";
        public const string KeyValuePairGenericTypeName = "System.Collections.Generic.KeyValuePair`2";

        public const string ArrayListTypeName = "System.Collections.ArrayList";

        // In the order we wish to detect a derived type.
        private static readonly Type[] s_genericInterfacesWithAddMethods = new Type[]
        {
            typeof(IDictionary<,>),
            typeof(ICollection<>),
        };

        // In the order we wish to detect a derived type.
        private static readonly Type[] s_nonGenericInterfacesWithAddMethods = new Type[]
        {
            typeof(IDictionary),
            typeof(IList),
        };

        // In the order we wish to detect a derived type.
        private static readonly Type[] s_genericInterfacesWithoutAddMethods = new Type[]
        {
            typeof(IReadOnlyDictionary<,>),
            typeof(IReadOnlyCollection<>),
            typeof(IReadOnlyList<>),
            typeof(IEnumerable<>),
        };

        // Any additional natively supported generic collection must be registered here.
        private static readonly HashSet<string> s_nativelySupportedGenericCollections = new HashSet<string>()
        {
            ListGenericTypeName,
            EnumerableGenericInterfaceTypeName,
            ListGenericInterfaceTypeName,
            CollectionGenericInterfaceTypeName,
            ReadOnlyListGenericInterfaceTypeName,
            ReadOnlyCollectionGenericInterfaceTypeName,
            SetGenericInterfaceTypeName,
            StackGenericTypeName,
            QueueGenericTypeName,
            HashSetGenericTypeName,
            LinkedListGenericTypeName,
            SortedSetGenericTypeName,
            DictionaryInterfaceTypeName,
            DictionaryGenericTypeName,
            DictionaryGenericInterfaceTypeName,
            ReadOnlyDictionaryGenericInterfaceTypeName,
            SortedDictionaryGenericTypeName,
            KeyValuePairGenericTypeName,
            DefaultImmutableEnumerableConverter.ImmutableArrayGenericTypeName,
            DefaultImmutableEnumerableConverter.ImmutableListGenericTypeName,
            DefaultImmutableEnumerableConverter.ImmutableListGenericInterfaceTypeName,
            DefaultImmutableEnumerableConverter.ImmutableStackGenericTypeName,
            DefaultImmutableEnumerableConverter.ImmutableStackGenericInterfaceTypeName,
            DefaultImmutableEnumerableConverter.ImmutableQueueGenericTypeName,
            DefaultImmutableEnumerableConverter.ImmutableQueueGenericInterfaceTypeName,
            DefaultImmutableEnumerableConverter.ImmutableSortedSetTypeName,
            DefaultImmutableEnumerableConverter.ImmutableSortedSetGenericTypeName,
            DefaultImmutableEnumerableConverter.ImmutableHashSetGenericTypeName,
            DefaultImmutableEnumerableConverter.ImmutableSetGenericInterfaceTypeName,
            DefaultImmutableDictionaryConverter.ImmutableDictionaryGenericTypeName,
            DefaultImmutableDictionaryConverter.ImmutableDictionaryGenericInterfaceTypeName,
            DefaultImmutableDictionaryConverter.ImmutableSortedDictionaryGenericTypeName,
        };

        // Any additional natively supported non-generic collection must be registered here.
        private static readonly HashSet<string> s_nativelySupportedNonGenericCollections = new HashSet<string>()
        {
            EnumerableInterfaceTypeName,
            CollectionInterfaceTypeName,
            ListInterfaceTypeName,
            DictionaryInterfaceTypeName,
            StackTypeName,
            QueueTypeName,
            HashtableTypeName,
            ArrayListTypeName,
            SortedListTypeName,
        };

        public static Type GetImplementedCollectionType(
            Type parentClassType,
            Type queryType,
            PropertyInfo propertyInfo,
            out JsonConverter converter,
            JsonSerializerOptions options)
        {
            Debug.Assert(queryType != null);

            if (!(typeof(IEnumerable).IsAssignableFrom(queryType)) ||
                queryType == typeof(string) ||
                queryType.IsInterface ||
                queryType.IsArray ||
                IsNativelySupportedCollection(queryType))
            {
                converter = null;
                return queryType;
            }

            // If a converter was provided, we should not detect implemented types and instead use the converter later.
            converter = options.DetermineConverterForProperty(parentClassType, queryType, propertyInfo);
            if (converter != null)
            {
                return queryType;
            }

            Type baseType = queryType.GetTypeInfo().BaseType;

            // Check if the base type is a supported concrete collection.
            if (IsNativelySupportedCollection(baseType))
            {
                return baseType;
            }

            // Try generic interfaces with add methods.
            foreach (Type candidate in s_genericInterfacesWithAddMethods)
            {
                Type derivedGeneric = ExtractGenericInterface(queryType, candidate);
                if (derivedGeneric != null)
                {
                    return derivedGeneric;
                }
            }

            // Try non-generic interfaces with add methods.
            foreach (Type candidate in s_nonGenericInterfacesWithAddMethods)
            {
                if (candidate.IsAssignableFrom(queryType))
                {
                    return candidate;
                }
            }

            // Try generic interfaces without add methods
            foreach (Type candidate in s_genericInterfacesWithoutAddMethods)
            {
                Type derivedGeneric = ExtractGenericInterface(queryType, candidate);
                if (derivedGeneric != null)
                {
                    return derivedGeneric;
                }
            }

            return typeof(IEnumerable);
        }

        public static bool IsDeserializedByAssigningFromList(Type type)
        {
            if (type.IsGenericType)
            {
                switch (type.GetGenericTypeDefinition().FullName)
                {
                    case EnumerableGenericInterfaceTypeName:
                    case ListGenericInterfaceTypeName:
                    case CollectionGenericInterfaceTypeName:
                    case ReadOnlyListGenericInterfaceTypeName:
                    case ReadOnlyCollectionGenericInterfaceTypeName:
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                switch (type.FullName)
                {
                    case EnumerableInterfaceTypeName:
                    case ListInterfaceTypeName:
                    case CollectionInterfaceTypeName:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static bool IsSetInterface(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISet<>);
        }

        public static bool HasConstructorThatTakesGenericIEnumerable(Type type, JsonSerializerOptions options)
        {
            Type elementType = GetElementType(type, parentType: null, memberInfo: null, options);
            return type.GetConstructor(new Type[] { typeof(List<>).MakeGenericType(elementType) }) != null;
        }

        public static bool IsDeserializedByConstructingWithIList(Type type)
        {
            switch (type.FullName)
            {
                case StackTypeName:
                case QueueTypeName:
                case ArrayListTypeName:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDeserializedByConstructingWithIDictionary(Type type)
        {
            switch (type.FullName)
            {
                case HashtableTypeName:
                case SortedListTypeName:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsNativelySupportedCollection(Type queryType)
        {
            Debug.Assert(queryType != null);

            if (queryType.IsGenericType)
            {
                return s_nativelySupportedGenericCollections.Contains(queryType.GetGenericTypeDefinition().FullName);
            }

            return s_nativelySupportedNonGenericCollections.Contains(queryType.FullName);
        }

        // The following methods were copied verbatim from AspNetCore:
        // https://github.com/aspnet/AspNetCore/blob/13ae0057fbb11fd84fcee8fca46ebc1b2d7c1e6a/src/Shared/ClosedGenericMatcher/ClosedGenericMatcher.cs.

        /// <summary>
        /// Determine whether <paramref name="queryType"/> is or implements a closed generic <see cref="Type"/>
        /// created from <paramref name="interfaceType"/>.
        /// </summary>
        /// <param name="queryType">The <see cref="Type"/> of interest.</param>
        /// <param name="interfaceType">The open generic <see cref="Type"/> to match. Usually an interface.</param>
        /// <returns>
        /// The closed generic <see cref="Type"/> created from <paramref name="interfaceType"/> that
        /// <paramref name="queryType"/> is or implements. <c>null</c> if the two <see cref="Type"/>s have no such
        /// relationship.
        /// </returns>
        /// <remarks>
        /// This method will return <paramref name="queryType"/> if <paramref name="interfaceType"/> is
        /// <c>typeof(KeyValuePair{,})</c>, and <paramref name="queryType"/> is
        /// <c>typeof(KeyValuePair{string, object})</c>.
        /// </remarks>
        public static Type ExtractGenericInterface(Type queryType, Type interfaceType)
        {
            if (queryType == null)
            {
                throw new ArgumentNullException(nameof(queryType));
            }

            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            if (IsGenericInstantiation(queryType, interfaceType))
            {
                // queryType matches (i.e. is a closed generic type created from) the open generic type.
                return queryType;
            }

            // Otherwise check all interfaces the type implements for a match.
            // - If multiple different generic instantiations exists, we want the most derived one.
            // - If that doesn't break the tie, then we sort alphabetically so that it's deterministic.
            //
            // We do this by looking at interfaces on the type, and recursing to the base type
            // if we don't find any matches.
            return GetGenericInstantiation(queryType, interfaceType);
        }

        private static bool IsGenericInstantiation(Type candidate, Type interfaceType)
        {
            return
                candidate.GetTypeInfo().IsGenericType &&
                candidate.GetGenericTypeDefinition() == interfaceType;
        }

        private static Type GetGenericInstantiation(Type queryType, Type interfaceType)
        {
            Type bestMatch = null;
            Type[] interfaces = queryType.GetInterfaces();
            foreach (Type @interface in interfaces)
            {
                if (IsGenericInstantiation(@interface, interfaceType))
                {
                    if (bestMatch == null)
                    {
                        bestMatch = @interface;
                    }
                    else if (StringComparer.Ordinal.Compare(@interface.FullName, bestMatch.FullName) < 0)
                    {
                        bestMatch = @interface;
                    }
                    else
                    {
                        // There are two matches at this level of the class hierarchy, but @interface is after
                        // bestMatch in the sort order.
                    }
                }
            }

            if (bestMatch != null)
            {
                return bestMatch;
            }

            // BaseType will be null for object and interfaces, which means we've reached 'bottom'.
            Type baseType = queryType?.GetTypeInfo().BaseType;
            if (baseType == null)
            {
                return null;
            }
            else
            {
                return GetGenericInstantiation(baseType, interfaceType);
            }
        }
    }
}
