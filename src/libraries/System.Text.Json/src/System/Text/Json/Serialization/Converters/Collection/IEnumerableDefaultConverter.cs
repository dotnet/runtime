// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Default base class implementation of <cref>JsonIEnumerableConverter{TCollection, TElement}</cref>.
    /// </summary>
    internal abstract class IEnumerableDefaultConverter<TCollection, TElement> : JsonCollectionConverter<TCollection, TElement>
        where TCollection : IEnumerable<TElement>
    {
        internal override bool CanHaveMetadata => true;

        protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            Debug.Assert(value is not null);

            IEnumerator<TElement>? enumerator = default;
            try
            {
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
                    Debug.Assert(state.Current.CollectionEnumerator is IEnumerator<TElement>);
                    enumerator = (IEnumerator<TElement>)state.Current.CollectionEnumerator;
                }

                JsonConverter<TElement> converter = GetElementConverter(ref state);
                do
                {
                    if (ShouldFlush(writer, ref state))
                    {
                        state.Current.CollectionEnumerator = enumerator;
                        enumerator = default;
                        return false;
                    }

                    TElement element = enumerator.Current;
                    if (!converter.TryWrite(writer, element, options, ref state))
                    {
                        state.Current.CollectionEnumerator = enumerator;
                        enumerator = default;
                        return false;
                    }

                    state.Current.EndCollectionElement();
                } while (enumerator.MoveNext());

                return true;
            }
            finally
            {
                enumerator?.Dispose();
            }
        }
    }
}
