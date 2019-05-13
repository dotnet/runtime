// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    // This converter returns enumerables in the System.Collections.Immutable namespace.
    internal sealed class DefaultImmutableConverter : JsonEnumerableConverter
    {
        public const string ImmutableNamespace = "System.Collections.Immutable";

        private const string ImmutableListTypeName = "System.Collections.Immutable.ImmutableList";
        private const string ImmutableListGenericTypeName = "System.Collections.Immutable.ImmutableList`1";
        private const string ImmutableListGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableList`1";

        private const string ImmutableStackTypeName = "System.Collections.Immutable.ImmutableStack";
        private const string ImmutableStackGenericTypeName = "System.Collections.Immutable.ImmutableStack`1";
        private const string ImmutableStackGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableStack`1";

        private const string ImmutableQueueTypeName = "System.Collections.Immutable.ImmutableQueue";
        private const string ImmutableQueueGenericTypeName = "System.Collections.Immutable.ImmutableQueue`1";
        private const string ImmutableQueueGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableQueue`1";

        private const string ImmutableSortedSetTypeName = "System.Collections.Immutable.ImmutableSortedSet";
        private const string ImmutableSortedSetGenericTypeName = "System.Collections.Immutable.ImmutableSortedSet`1";

        private const string ImmutableHashSetTypeName = "System.Collections.Immutable.ImmutableHashSet";
        private const string ImmutableHashSetGenericTypeName = "System.Collections.Immutable.ImmutableHashSet`1";
        private const string ImmutableSetGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableSet`1";

        internal delegate object ImmutableCreateRangeDelegate<T>(IEnumerable<T> items);

        public static ConcurrentDictionary<string, object> CreateRangeDelegates = new ConcurrentDictionary<string, object>();

        private string GetConstructingTypeName(string immutableCollectionTypeName)
        {
            switch (immutableCollectionTypeName)
            {
                case ImmutableListGenericTypeName:
                case ImmutableListGenericInterfaceTypeName:
                    return ImmutableListTypeName;
                case ImmutableStackGenericTypeName:
                case ImmutableStackGenericInterfaceTypeName:
                    return  ImmutableStackTypeName;
                case ImmutableQueueGenericTypeName:
                case ImmutableQueueGenericInterfaceTypeName:
                    return  ImmutableQueueTypeName;
                case ImmutableSortedSetGenericTypeName:
                    return  ImmutableSortedSetTypeName;
                case ImmutableHashSetGenericTypeName:
                case ImmutableSetGenericInterfaceTypeName:
                    return  ImmutableHashSetTypeName;
                default:
                    // TODO: Refactor exception throw following serialization exception changes. 
                    throw new NotSupportedException(SR.Format(SR.DeserializeTypeNotSupported, immutableCollectionTypeName));
            }
        }

        private string GetDelegateKey(
            Type immutableCollectionType,
            Type elementType,
            out Type underlyingType,
            out string constructingTypeName)
        {
            // Use the generic type definition of the immutable collection to determine an appropriate constructing type,
            // i.e. a type that we can invoke the `CreateRange<elementType>` method on, which returns an assignable immutable collection.
            underlyingType = immutableCollectionType.GetGenericTypeDefinition();
            constructingTypeName = GetConstructingTypeName(underlyingType.FullName);

            return $"{constructingTypeName}:{elementType.FullName}";
        }

        public void RegisterImmutableCollectionType(Type immutableCollectionType, Type elementType, JsonSerializerOptions options)
        {
            // Get a unique identifier for a delegate which will point to the appropiate CreateRange method.
            string delegateKey = GetDelegateKey(immutableCollectionType, elementType, out Type underlyingType, out string constructingTypeName);

            // Exit if we have registered this immutable collection type.
            if (CreateRangeDelegates.ContainsKey(delegateKey))
            {
                return;
            }

            // Get the constructing type.
            Type constructingType = underlyingType.Assembly.GetType(constructingTypeName);

            // Create a delegate which will point to the CreateRange method.
            object createRangeDelegate = options.ClassMaterializerStrategy.ImmutableCreateRange(constructingType, elementType);
            Debug.Assert(createRangeDelegate != null);

            // Cache the delegate
            CreateRangeDelegates.TryAdd(delegateKey, createRangeDelegate);
        }

        public override IEnumerable CreateFromList(ref ReadStack state, IList sourceList, JsonSerializerOptions options)
        {
            Type immutableCollectionType = state.Current.JsonPropertyInfo.RuntimePropertyType;
            Type elementType = state.Current.GetElementType();

            string delegateKey = GetDelegateKey(immutableCollectionType, elementType, out _, out _);
            Debug.Assert(CreateRangeDelegates.ContainsKey(delegateKey));

            JsonClassInfo elementClassInfo = state.Current.JsonPropertyInfo.ElementClassInfo;

            JsonPropertyInfo propertyInfo;
            if (elementClassInfo.ClassType == ClassType.Object)
            {
                Type objectType = elementClassInfo.Type;

                if (DefaultIEnumerableConstructibleConverter.s_objectJsonProperties.ContainsKey(objectType))
                {
                    propertyInfo = DefaultIEnumerableConstructibleConverter.s_objectJsonProperties[objectType];
                }
                else
                {
                    propertyInfo = JsonClassInfo.CreateProperty(objectType, objectType, null, typeof(object), options);
                    DefaultIEnumerableConstructibleConverter.s_objectJsonProperties[objectType] = propertyInfo;
                }
            }
            else
            {
                propertyInfo = elementClassInfo.GetPolicyProperty();
            }

            return propertyInfo.CreateImmutableCollectionFromList(delegateKey, sourceList);
        }
    }
}
