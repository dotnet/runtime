// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// Converter for <cref>System.Collections.Generic.List{TElement}</cref>.
    internal sealed class ListOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement, TCollection>
        where TCollection : List<TElement>
    {
        private protected sealed override void Add(ref TCollection collection, in TElement value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Add(value);
        }

        internal override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            List<TElement> list = value;

            // Using an index is 2x faster than using an enumerator.
            int index = state.Current.EnumeratorIndex;
            JsonConverter<TElement> elementConverter = GetElementConverter(ref state);

            if (elementConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
            {
                // Fast path that avoids validation and extra indirection.
                for (; index < list.Count; index++)
                {
                    elementConverter.Write(writer, list[index], options);
                }
            }
            else
            {
                for (; index < list.Count; index++)
                {
                    TElement element = list[index];
                    if (!elementConverter.TryWrite(writer, element, options, ref state))
                    {
                        state.Current.EnumeratorIndex = index;
                        return false;
                    }

                    state.Current.EndCollectionElement();

                    if (ShouldFlush(writer, ref state))
                    {
                        state.Current.EnumeratorIndex = ++index;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
