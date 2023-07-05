// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.IEnumerable</cref>.
    /// </summary>
    /// <typeparam name="TCollection"></typeparam>
    internal sealed class IEnumerableConverter<TCollection>
        : JsonCollectionConverter<TCollection, object?>
        where TCollection : IEnumerable
    {
        private readonly bool _isDeserializable = typeof(TCollection).IsAssignableFrom(typeof(List<object?>));

        protected override void Add(in object? value, ref ReadStack state)
        {
            ((List<object?>)state.Current.ReturnValue!).Add(value);
        }

        internal override bool SupportsCreateObjectDelegate => false;
        protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
        {
            if (!_isDeserializable)
            {
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(Type, ref reader, ref state);
            }

            state.Current.ReturnValue = new List<object?>();
        }

        // Consider overriding ConvertCollection to convert the list to an array since a List is mutable.
        // However, converting from the temporary list to an array will be slower.

        protected override bool OnWriteResume(
            Utf8JsonWriter writer,
            TCollection value,
            JsonSerializerOptions options,
            ref WriteStack state)
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
