// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for Dictionary{string, TValue} that (de)serializes as a JSON object with properties
    /// representing the dictionary element key and value.
    /// </summary>
    internal sealed class DictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>
        : DictionaryDefaultConverter<TCollection, TKey, TValue>
        where TCollection : Dictionary<TKey, TValue>
        where TKey : notnull
    {
        protected override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
        {
            ((TCollection)state.Current.ReturnValue!)[key] = value;
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state)
        {
            if (state.Current.JsonTypeInfo.CreateObject == null)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(state.Current.JsonTypeInfo.Type);
            }

            state.Current.ReturnValue = state.Current.JsonTypeInfo.CreateObject();
        }

        protected internal override bool OnWriteResume(
            Utf8JsonWriter writer,
            TCollection value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            Dictionary<TKey, TValue>.Enumerator enumerator;
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
                enumerator = (Dictionary<TKey, TValue>.Enumerator)state.Current.CollectionEnumerator;
            }

            JsonTypeInfo typeInfo = state.Current.JsonTypeInfo;
            _keyConverter ??= GetConverter<TKey>(typeInfo.KeyTypeInfo!);
            _valueConverter ??= GetConverter<TValue>(typeInfo.ElementTypeInfo!);

            if (!state.SupportContinuation && _valueConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
            {
                // Fast path that avoids validation and extra indirection.
                do
                {
                    TKey key = enumerator.Current.Key;
                    _keyConverter.WriteWithQuotes(writer, key, options, ref state);
                    _valueConverter.Write(writer, enumerator.Current.Value, options);
                } while (enumerator.MoveNext());
            }
            else
            {
                do
                {
                    if (ShouldFlush(writer, ref state))
                    {
                        state.Current.CollectionEnumerator = enumerator;
                        return false;
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        state.Current.PropertyState = StackFramePropertyState.Name;

                        TKey key = enumerator.Current.Key;
                        _keyConverter.WriteWithQuotes(writer, key, options, ref state);
                    }

                    TValue element = enumerator.Current.Value;
                    if (!_valueConverter.TryWrite(writer, element, options, ref state))
                    {
                        state.Current.CollectionEnumerator = enumerator;
                        return false;
                    }

                    state.Current.EndDictionaryElement();
                } while (enumerator.MoveNext());
            }

            enumerator.Dispose();
            return true;
        }
    }
}
