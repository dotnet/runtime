// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.IEnumerable</cref>.
    /// </summary>
    /// <typeparam name="TCollection"></typeparam>
    internal sealed class IEnumerableConverter<TCollection>
        : IEnumerableConverterBase<TCollection, List<object?>>
        where TCollection : IEnumerable
    {
        private readonly bool _isDeserializable = typeof(TCollection).IsAssignableFrom(typeof(List<object?>));

        private protected override void Add(ref List<object?> collection, in object? value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Add(value);
        }

        private protected override bool TryCreateObject(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, [NotNullWhen(true)] out List<object?>? obj)
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
                jsonTypeInfo.CreateObject = () => new List<object?>();
            }
        }
    }
}
