// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.IDictionary{TKey, TValue}</cref> that
    /// (de)serializes as a JSON object with properties representing the dictionary element key and value.
    /// </summary>
    internal sealed class IDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue>
        : DictionaryDefaultConverter<TDictionary, TKey, TValue>
        where TDictionary : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        internal override bool CanPopulate => true;

        protected override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
        {
            TDictionary collection = (TDictionary)state.Current.ReturnValue!;
            collection[key] = value;
            if (IsValueType)
            {
                state.Current.ReturnValue = collection;
            };
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state)
        {
            base.CreateCollection(ref reader, ref state);
            TDictionary returnValue = (TDictionary)state.Current.ReturnValue!;
            if (returnValue.IsReadOnly)
            {
                state.Current.ReturnValue = null; // clear out for more accurate JsonPath reporting.
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
            }
        }

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            // Deserialize as Dictionary<TKey,TValue> for interface types that support it.
            if (jsonTypeInfo.CreateObject is null && TypeToConvert.IsAssignableFrom(typeof(Dictionary<TKey, TValue>)))
            {
                Debug.Assert(TypeToConvert.IsInterface);
                jsonTypeInfo.CreateObject = () => new Dictionary<TKey, TValue>();
            }
        }
    }
}
