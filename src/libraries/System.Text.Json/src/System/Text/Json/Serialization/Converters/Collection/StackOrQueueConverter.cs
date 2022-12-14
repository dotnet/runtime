// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal class StackOrQueueConverter<TCollection>
        : IEnumerableConverterBase<TCollection, TCollection>
        where TCollection : IEnumerable
    {
        private protected sealed override void Add(ref TCollection collection, in object? value, JsonTypeInfo collectionTypeInfo)
        {
            var addMethodDelegate = ((Action<TCollection, object?>?)collectionTypeInfo.AddMethodDelegate);
            Debug.Assert(addMethodDelegate != null);
            addMethodDelegate(collection, value);
        }
    }
}
