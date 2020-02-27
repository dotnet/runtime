// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for Dictionary{TKey, TValue} that (de)serializes as a JSON object with properties
    /// representing the dictionary element key and value.
    /// </summary>
    internal sealed class DictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>
        : DictionaryDefaultConverter<TCollection, TKey, TValue>
        where TCollection : Dictionary<TKey, TValue>
        where TKey : notnull
    {
        protected override void Add(TKey key, TValue value, JsonSerializerOptions options, ref ReadStack state)
        {
            ((TCollection)state.Current.ReturnValue!)[key] = value;
        }

        protected override void CreateCollection(ref ReadStack state)
        {
            if (state.Current.JsonClassInfo.CreateObject == null)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(state.Current.JsonClassInfo.Type);
            }

            state.Current.ReturnValue = state.Current.JsonClassInfo.CreateObject();
        }

        protected internal override bool OnWriteResume(
            Utf8JsonWriter writer,
            TCollection value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            // Get the key converter at the very beginning; will throw NSE if there is no converter for TKey.
            // This is performed at the very beginning to avoid processing unsupported types.
            KeyConverter<TKey> keyConverter = GetKeyConverter(state.Current.JsonClassInfo);

            Dictionary<TKey, TValue>.Enumerator enumerator;

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
                enumerator = (Dictionary<TKey, TValue>.Enumerator)state.Current.CollectionEnumerator;
            }

            JsonConverter<TValue> valueConverter = GetValueConverter(ref state);

            if (!state.SupportContinuation && valueConverter.CanUseDirectReadOrWrite)
            {
                // Fast path that avoids validation and extra indirection.
                do
                {
                    keyConverter.OnTryWrite(writer, enumerator.Current.Key, options, ref state);
                    // TODO: https://github.com/dotnet/runtime/issues/32523
                    valueConverter.Write(writer, enumerator.Current.Value!, options);
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

                    TValue element = enumerator.Current.Value;

                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        state.Current.PropertyState = StackFramePropertyState.Name;
                        keyConverter.OnTryWrite(writer, enumerator.Current.Key, options, ref state);
                    }

                    if (!valueConverter.TryWrite(writer, element, options, ref state))
                    {
                        state.Current.CollectionEnumerator = enumerator;
                        return false;
                    }

                    state.Current.EndDictionaryElement();
                } while (enumerator.MoveNext());
            }

            return true;
        }
    }
}
