// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal class StackOrQueueConverter<TCollection>
        : IEnumerableDefaultConverter<TCollection, object?>
        where TCollection : IEnumerable
    {
        protected sealed override void Add(in object? value, ref ReadStack state)
        {
            var addMethodDelegate = ((Action<TCollection, object?>?)state.Current.JsonTypeInfo.AddMethodDelegate);
            Debug.Assert(addMethodDelegate != null);
            addMethodDelegate((TCollection)state.Current.ReturnValue!, value);
        }

        protected sealed override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
        {
            JsonTypeInfo typeInfo = state.Current.JsonTypeInfo;
            JsonTypeInfo.ConstructorDelegate? constructorDelegate = typeInfo.CreateObject;

            if (constructorDelegate == null)
            {
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
            }

            state.Current.ReturnValue = constructorDelegate();

            Debug.Assert(typeInfo.AddMethodDelegate != null);
        }

        protected sealed override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
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
            } while (enumerator.MoveNext());

            return true;
        }
    }
}
