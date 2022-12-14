// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.IEnumerable{TElement}</cref>.
    /// </summary>
    internal sealed class IEnumerableOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement, List<TElement>>
        where TCollection : IEnumerable<TElement>
    {
        private readonly bool _isDeserializable = typeof(TCollection).IsAssignableFrom(typeof(List<TElement>));

        private protected sealed override void Add(ref List<TElement> collection, in TElement value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Add(value);
        }

        internal override bool SupportsCreateObjectDelegate => false;

        private protected override bool TryCreateObject(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, [NotNullWhen(true)] out List<TElement>? obj)
        {
            // TODO: IsReadOnly
            if (!_isDeserializable)
            {
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
            }

            // TODO: ConfigureJsonTypeInfo + remove this method
            obj = new List<TElement>();
            return true;
        }
    }
}
