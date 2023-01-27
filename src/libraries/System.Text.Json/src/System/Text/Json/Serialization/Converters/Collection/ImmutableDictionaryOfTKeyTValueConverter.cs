// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal class ImmutableDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue>
        : DictionaryDefaultConverter<TDictionary, TKey, TValue, Dictionary<TKey, TValue>>
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        protected sealed override void Add(ref Dictionary<TKey, TValue> collection, TKey key, in TValue value, JsonSerializerOptions options)
        {
            collection[key] = value;
        }

        internal sealed override bool CanHaveMetadata => false;

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            jsonTypeInfo.CreateObject ??= () => new Dictionary<TKey, TValue>();
            jsonTypeInfo.NotSupportedExtensionDataProperty = true;
        }

        private protected override bool TryConvert(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, Dictionary<TKey, TValue> obj, out TDictionary value)
        {
            Func<IEnumerable<KeyValuePair<TKey, TValue>>, TDictionary>? creator =
                (Func<IEnumerable<KeyValuePair<TKey, TValue>>, TDictionary>?)jsonTypeInfo.CreateObjectWithArgs;
            Debug.Assert(creator != null);
            value = creator(obj);
            return true;
        }
    }
}
