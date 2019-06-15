// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DefaultImmutableDictionaryConverter : JsonDictionaryConverter
    {
        private const string ImmutableDictionaryGenericTypeName = "System.Collections.Immutable.ImmutableDictionary`2";
        private const string ImmutableDictionaryGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableDictionary`2";
        private const string ImmutableSortedDictionaryGenericTypeName = "System.Collections.Immutable.ImmutableSortedDictionary`2";

        public static void RegisterImmutableDictionary(Type immutableCollectionType, Type elementType, JsonSerializerOptions options)
        {
            // Get a unique identifier for a delegate which will point to the appropiate CreateRange method.
            string delegateKey = DefaultImmutableEnumerableConverter.GetDelegateKey(immutableCollectionType, elementType, out Type underlyingType, out string constructingTypeName);

            // Exit if we have registered this immutable dictionary type.
            if (options.CreateRangeDelegatesContainsKey(delegateKey))
            {
                return;
            }

            // Get the constructing type.
            Type constructingType = underlyingType.Assembly.GetType(constructingTypeName);

            // Create a delegate which will point to the CreateRange method.
            object createRangeDelegate;
            createRangeDelegate = options.ClassMaterializerStrategy.ImmutableDictionaryCreateRange(constructingType, elementType);

            // Cache the delegate
            options.TryAddCreateRangeDelegate(delegateKey, createRangeDelegate);
        }

        public static bool IsImmutableDictionary(Type type)
        {
            if (!type.IsGenericType)
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

        public override IDictionary CreateFromDictionary(ref ReadStack state, IDictionary sourceDictionary, JsonSerializerOptions options)
        {
            Type immutableCollectionType = state.Current.JsonPropertyInfo.RuntimePropertyType;
            Type elementType = state.Current.GetElementType();

            string delegateKey = DefaultImmutableEnumerableConverter.GetDelegateKey(immutableCollectionType, elementType, out _, out _);

            JsonClassInfo elementClassInfo = state.Current.JsonPropertyInfo.ElementClassInfo;
            JsonPropertyInfo propertyInfo = options.GetJsonPropertyInfoFromClassInfo(elementClassInfo, options);
            return propertyInfo.CreateImmutableDictionaryInstance(immutableCollectionType, delegateKey, sourceDictionary, state.JsonPath, options);
        }
    }
}
