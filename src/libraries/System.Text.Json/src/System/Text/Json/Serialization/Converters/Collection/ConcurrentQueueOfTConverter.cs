// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ConcurrentQueueOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement>
        where TCollection : ConcurrentQueue<TElement>
    {
        protected override void Add(in TElement value, ref ReadStack state)
        {
            ((TCollection)state.Current.ReturnValue!).Enqueue(value);
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
        {
            if (state.Current.JsonClassInfo.CreateObject == null)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(state.Current.JsonClassInfo.Type);
            }

            state.Current.ReturnValue = state.Current.JsonClassInfo.CreateObject();
        }

        protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            IEnumerator<TElement> enumerator;
            if (state.Current.CollectionEnumerator == null)
            {
                enumerator = value.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    enumerator.Dispose();
                    return true;
                }
            }
            else
            {
                enumerator = (IEnumerator<TElement>)state.Current.CollectionEnumerator;
            }

            JsonConverter<TElement> converter = GetElementConverter(ref state);
            do
            {
                if (ShouldFlush(writer, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                TElement element = enumerator.Current;
                if (!converter.TryWrite(writer, element, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }
            } while (enumerator.MoveNext());

            enumerator.Dispose();
            return true;
        }
    }
}
