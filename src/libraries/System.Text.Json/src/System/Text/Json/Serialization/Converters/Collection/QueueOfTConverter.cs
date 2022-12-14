// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class QueueOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement, TCollection>
        where TCollection : Queue<TElement>
    {
        private protected sealed override void Add(ref TCollection collection, in TElement value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Enqueue(value);
        }
    }
}
