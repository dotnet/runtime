// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class IReadOnlySetOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement>
        where TCollection : IReadOnlySet<TElement>
    {
        // TODO: Set to false because I think IReadOnlySet<T> is always read-only.
        // However, because we use HashSet<T> as the concrete type, we can still add items to it.
        // So I'm unsure if this should be true or false.
        internal override bool CanPopulate => false;

        protected override void Add(in TElement value, ref ReadStack state)
        {
            // Directly convert to HashSet<TElement> since IReadOnlySet<T> does not have an Add method.
            HashSet<TElement> collection = (HashSet<TElement>)state.Current.ReturnValue!;
            collection.Add(value);
            if (IsValueType)
            {
                state.Current.ReturnValue = collection;
            }
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
        {
            state.Current.ReturnValue = new HashSet<TElement>();
        }

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            // Deserialize as HashSet<TElement> for interface types that support it.
            if (jsonTypeInfo.CreateObject is null && Type.IsAssignableFrom(typeof(HashSet<TElement>)))
            {
                Debug.Assert(Type.IsInterface);
                jsonTypeInfo.CreateObject = () => new HashSet<TElement>();
            }
        }
    }
}
