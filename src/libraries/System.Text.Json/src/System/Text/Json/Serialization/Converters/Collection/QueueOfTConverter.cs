// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class QueueOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement>
        where TCollection : Queue<TElement>
    {
        /// <summary>Lazily initialized singleton for hardcoding by the IAsyncEnumerable streaming deserializer.</summary>
        internal static QueueOfTConverter<TCollection, TElement> Instance => _instance ??= new();
        private static QueueOfTConverter<TCollection, TElement>? _instance;

        protected override void Add(in TElement value, ref ReadStack state)
        {
            ((TCollection)state.Current.ReturnValue!).Enqueue(value);
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
        {
            if (state.Current.JsonTypeInfo.CreateObject == null)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(state.Current.JsonTypeInfo.Type);
            }

            state.Current.ReturnValue = state.Current.JsonTypeInfo.CreateObject();
        }
    }
}
