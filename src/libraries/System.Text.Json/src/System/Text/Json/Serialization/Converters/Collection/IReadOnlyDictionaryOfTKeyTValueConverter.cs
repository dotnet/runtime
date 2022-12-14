// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class IReadOnlyDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue>
        : DictionaryDefaultConverter<TDictionary, TKey, TValue, Dictionary<TKey, TValue>>
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly bool _isDeserializable = typeof(TDictionary).IsAssignableFrom(typeof(Dictionary<TKey, TValue>));

        protected override void Add(ref Dictionary<TKey, TValue> collection, TKey key, in TValue value, JsonSerializerOptions options)
        {
            collection[key] = value;
        }

        internal override bool SupportsCreateObjectDelegate => false;

        private protected override bool TryCreateObject(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, [NotNullWhen(true)] out Dictionary<TKey, TValue>? obj)
        {
            // TODO: IsReadOnly?
            if (!_isDeserializable)
            {
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
            }

            // TODO: should use default implementation and ConfigureJsonTypeInfo
            obj = new Dictionary<TKey, TValue>();
            return true;
        }
    }
}
