// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.IDictionary</cref> that (de)serializes as a JSON object with properties
    /// representing the dictionary element key and value.
    /// </summary>
    internal sealed class IDictionaryConverter<TDictionary>
        : JsonDictionaryConverter<TDictionary, string, object?>
        where TDictionary : IDictionary
    {
        internal override bool CanPopulate => true;

        protected override void Add(string key, in object? value, JsonSerializerOptions options, ref ReadStack state)
        {
            TDictionary collection = (TDictionary)state.Current.ReturnValue!;
            collection[key] = value;
            if (IsValueType)
            {
                state.Current.ReturnValue = collection;
            }
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state)
        {
            base.CreateCollection(ref reader, ref state);
            TDictionary returnValue = (TDictionary)state.Current.ReturnValue!;
            if (returnValue.IsReadOnly)
            {
                state.Current.ReturnValue = null; // clear out for more accurate JsonPath reporting.
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
            }
        }

        protected internal override bool OnWriteResume(Utf8JsonWriter writer, TDictionary value, JsonSerializerOptions options, ref WriteStack state)
        {
            IDictionaryEnumerator enumerator;
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
                enumerator = (IDictionaryEnumerator)state.Current.CollectionEnumerator;
            }

            JsonTypeInfo typeInfo = state.Current.JsonTypeInfo;
            _valueConverter ??= GetConverter<object?>(typeInfo.ElementTypeInfo!);

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
                    object key = enumerator.Key;
                    // Optimize for string since that's the hot path.
                    if (key is string keyString)
                    {
                        _keyConverter ??= GetConverter<string>(typeInfo.KeyTypeInfo!);
                        _keyConverter.WriteAsPropertyNameCore(writer, keyString, options, state.Current.IsWritingExtensionDataProperty);
                    }
                    else
                    {
                        // IDictionary is a special case since it has polymorphic object semantics on serialization
                        // but needs to use JsonConverter<string> on deserialization.
                        _valueConverter.WriteAsPropertyNameCore(writer, key, options, state.Current.IsWritingExtensionDataProperty);
                    }
                }

                object? element = enumerator.Value;
                if (!_valueConverter.TryWrite(writer, element, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                state.Current.EndDictionaryEntry();
            } while (enumerator.MoveNext());

            return true;
        }

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            // Deserialize as Dictionary<TKey,TValue> for interface types that support it.
            if (jsonTypeInfo.CreateObject is null && TypeToConvert.IsAssignableFrom(typeof(Dictionary<string, object?>)))
            {
                Debug.Assert(TypeToConvert.IsInterface);
                jsonTypeInfo.CreateObject = () => new Dictionary<string, object?>();
            }
        }
    }
}
