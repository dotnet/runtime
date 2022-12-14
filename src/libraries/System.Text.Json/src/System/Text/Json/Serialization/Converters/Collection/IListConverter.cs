// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// Converter for <cref>System.Collections.IList</cref>.
    internal sealed class IListConverter<TCollection>
        : JsonCollectionConverter<TCollection, object?, TCollection>
        where TCollection : IList
    {
        private protected sealed override bool IsReadOnly(object obj)
            => ((TCollection)obj).IsReadOnly;

        private protected override void Add(ref TCollection collection, in object? value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Add(value);
        }

        internal override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            IList list = value;

            // Using an index is 2x faster than using an enumerator.
            int index = state.Current.EnumeratorIndex;
            JsonConverter<object?> elementConverter = GetElementConverter(ref state);

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
                    object? element = list[index];
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

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            // Deserialize as List<object?> for interface types that support it.
            if (jsonTypeInfo.CreateObject is null && TypeToConvert.IsAssignableFrom(typeof(List<object?>)))
            {
                Debug.Assert(TypeToConvert.IsInterface);
                jsonTypeInfo.CreateObject = () => new List<object?>();
            }
        }
    }
}
