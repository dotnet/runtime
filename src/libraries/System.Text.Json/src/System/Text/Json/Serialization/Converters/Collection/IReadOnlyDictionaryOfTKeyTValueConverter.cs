// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class IReadOnlyDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue>
        : DictionaryDefaultConverter<TDictionary, TKey, TValue, IReadOnlyDictionary<TKey, TValue>>
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly bool _isDeserializable = typeof(TDictionary).IsAssignableFrom(typeof(Dictionary<TKey, TValue>));

        protected override void Add(ref IReadOnlyDictionary<TKey, TValue> collection, TKey key, in TValue value, JsonSerializerOptions options)
        {
            ((Dictionary<TKey, TValue>)collection)[key] = value;
        }

        private protected override bool TryCreateObject(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, [NotNullWhen(true)] out IReadOnlyDictionary<TKey, TValue>? obj)
        {
            if (!_isDeserializable)
            {
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
            }

            return base.TryCreateObject(ref reader, jsonTypeInfo, ref state, out obj);
        }

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            if (jsonTypeInfo.CreateObject == null && _isDeserializable)
            {
                jsonTypeInfo.CreateObject = () => new Dictionary<TKey, TValue>();
            }
        }
    }
}
