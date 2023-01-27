// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal class ImmutableEnumerableOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement, List<TElement>>
        where TCollection : IEnumerable<TElement>
    {
        private protected sealed override void Add(ref List<TElement> collection, in TElement value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Add(value);
        }

        internal sealed override bool CanHaveMetadata => false;

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            jsonTypeInfo.CreateObject ??= () => new List<TElement>();
        }

        private protected override bool TryConvert(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, List<TElement> obj, out TCollection value)
        {
            Func<IEnumerable<TElement>, TCollection>? creator = (Func<IEnumerable<TElement>, TCollection>?)jsonTypeInfo.CreateObjectWithArgs;
            Debug.Assert(creator != null);
            value = creator(obj);
            return true;
        }
    }
}
