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
    /// <typeparam name="IntermediateType"></typeparam>
    internal abstract class IEnumerableConverterBase<TCollection, IntermediateType>
        : JsonCollectionConverter<TCollection, object?, IntermediateType>
        where TCollection : IEnumerable
    {
        internal sealed override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            IEnumerator enumerator;
            if (state.Current.CollectionEnumerator == null)
            {
                enumerator = value.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    return true;
                }
            }
            else
            {
                enumerator = state.Current.CollectionEnumerator;
            }

            JsonConverter<object?> converter = GetElementConverter(ref state);
            do
            {
                if (ShouldFlush(writer, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                object? element = enumerator.Current;
                if (!converter.TryWrite(writer, element, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                state.Current.EndCollectionElement();
            } while (enumerator.MoveNext());

            return true;
        }
    }
}
