// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    // This converter returns enumerables in the System.Collections.Immutable namespace.
    internal sealed class DefaultImmutableEnumerableConverter : JsonEnumerableConverter
    {
        public const string ImmutableArrayTypeName = "System.Collections.Immutable.ImmutableArray";
        public const string ImmutableArrayGenericTypeName = "System.Collections.Immutable.ImmutableArray`1";

        private const string ImmutableListTypeName = "System.Collections.Immutable.ImmutableList";
        public const string ImmutableListGenericTypeName = "System.Collections.Immutable.ImmutableList`1";
        public const string ImmutableListGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableList`1";

        private const string ImmutableStackTypeName = "System.Collections.Immutable.ImmutableStack";
        public const string ImmutableStackGenericTypeName = "System.Collections.Immutable.ImmutableStack`1";
        public const string ImmutableStackGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableStack`1";

        private const string ImmutableQueueTypeName = "System.Collections.Immutable.ImmutableQueue";
        public const string ImmutableQueueGenericTypeName = "System.Collections.Immutable.ImmutableQueue`1";
        public const string ImmutableQueueGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableQueue`1";

        public const string ImmutableSortedSetTypeName = "System.Collections.Immutable.ImmutableSortedSet";
        public const string ImmutableSortedSetGenericTypeName = "System.Collections.Immutable.ImmutableSortedSet`1";

        private const string ImmutableHashSetTypeName = "System.Collections.Immutable.ImmutableHashSet";
        public const string ImmutableHashSetGenericTypeName = "System.Collections.Immutable.ImmutableHashSet`1";
        public const string ImmutableSetGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableSet`1";

        public static string GetDelegateKey(
            Type immutableCollectionType,
            Type elementType,
            out Type underlyingType,
            out string constructingTypeName)
        {
            // Use the generic type definition of the immutable collection to determine an appropriate constructing type,
            // i.e. a type that we can invoke the `CreateRange<elementType>` method on, which returns an assignable immutable collection.
            underlyingType = immutableCollectionType.GetGenericTypeDefinition();

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
                case DefaultImmutableDictionaryConverter.ImmutableDictionaryGenericTypeName:
                case DefaultImmutableDictionaryConverter.ImmutableDictionaryGenericInterfaceTypeName:
                    constructingTypeName = DefaultImmutableDictionaryConverter.ImmutableDictionaryTypeName;
                    break;
                case DefaultImmutableDictionaryConverter.ImmutableSortedDictionaryGenericTypeName:
                    constructingTypeName = DefaultImmutableDictionaryConverter.ImmutableSortedDictionaryTypeName;
                    break;
                default:
                    throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(immutableCollectionType, null, null);
            }

            return $"{constructingTypeName}:{elementType.FullName}";
        }

        public static void RegisterImmutableCollection(Type immutableCollectionType, Type elementType, JsonSerializerOptions options)
        {
            // Get a unique identifier for a delegate which will point to the appropiate CreateRange method.
            string delegateKey = GetDelegateKey(immutableCollectionType, elementType, out Type underlyingType, out string constructingTypeName);

            // Exit if we have registered this immutable collection type.
            if (options.CreateRangeDelegatesContainsKey(delegateKey))
            {
                return;
            }

            // Get the constructing type.
            Type constructingType = underlyingType.Assembly.GetType(constructingTypeName);

            // Create a delegate which will point to the CreateRange method.
            ImmutableCollectionCreator createRangeDelegate;
            createRangeDelegate = options.MemberAccessorStrategy.ImmutableCollectionCreateRange(constructingType, immutableCollectionType, elementType);

            // Cache the delegate
            options.TryAddCreateRangeDelegate(delegateKey, createRangeDelegate);
        }

        public override IEnumerable CreateFromList(ref ReadStack state, IList sourceList, JsonSerializerOptions options)
        {
            Type immutableCollectionType = state.Current.JsonPropertyInfo.RuntimePropertyType;
            Type elementType = state.Current.GetElementType();

            string delegateKey = GetDelegateKey(immutableCollectionType, elementType, out _, out _);

            JsonPropertyInfo propertyInfo = options.GetJsonPropertyInfoFromClassInfo(elementType, options);
            return propertyInfo.CreateImmutableCollectionInstance(ref state, immutableCollectionType, delegateKey, sourceList, options);
        }
    }
}
