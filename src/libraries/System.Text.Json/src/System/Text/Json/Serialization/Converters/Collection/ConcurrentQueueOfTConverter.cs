// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ConcurrentQueueOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement, TCollection>
        where TCollection : ConcurrentQueue<TElement>
    {
        private protected override void Add(ref TCollection collection, in TElement value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Enqueue(value);
        }
    }
}
