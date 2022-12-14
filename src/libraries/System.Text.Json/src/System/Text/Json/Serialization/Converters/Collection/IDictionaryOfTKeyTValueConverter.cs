// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.IDictionary{TKey, TValue}</cref> that
    /// (de)serializes as a JSON object with properties representing the dictionary element key and value.
    /// </summary>
    internal sealed class IDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue>
        : DictionaryDefaultConverter<TDictionary, TKey, TValue, TDictionary>
        where TDictionary : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        private protected sealed override bool IsReadOnly(object obj)
            => ((TDictionary)obj).IsReadOnly;

        protected override void Add(ref TDictionary collection, TKey key, in TValue value, JsonSerializerOptions options)
        {
            collection[key] = value;
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
