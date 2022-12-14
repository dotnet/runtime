// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.ICollection{TElement}</cref>.
    /// </summary>
    internal sealed class ICollectionOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement, TCollection>
        where TCollection : ICollection<TElement>
    {
        private protected sealed override bool IsReadOnly(object obj)
            => ((TCollection)obj).IsReadOnly;

        private protected override void Add(ref TCollection collection, in TElement value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Add(value);
        }

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            // Deserialize as List<T> for interface types that support it.
            if (jsonTypeInfo.CreateObject is null && TypeToConvert.IsAssignableFrom(typeof(List<TElement>)))
            {
                Debug.Assert(TypeToConvert.IsInterface);
                jsonTypeInfo.CreateObject = () => new List<TElement>();
            }
        }
    }
}
