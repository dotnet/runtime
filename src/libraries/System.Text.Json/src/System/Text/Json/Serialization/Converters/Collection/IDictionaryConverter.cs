// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.IDictionary</cref> that (de)serializes as a JSON object with properties
    /// representing the dictionary element key and value.
    /// </summary>
    internal sealed class IDictionaryConverter<TCollection>
        : DictionaryDefaultConverter<TCollection, object?>
        where TCollection : IDictionary
    {
        protected override void Add(object? value, JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(state.Current.ReturnValue is IDictionary);

            string key = state.Current.JsonPropertyNameAsString!;
            ((IDictionary)state.Current.ReturnValue!)[key] = value;
        }

        protected override void CreateCollection(ref ReadStack state)
        {
            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            if (TypeToConvert.IsInterface || TypeToConvert.IsAbstract)
            {
                if (!TypeToConvert.IsAssignableFrom(RuntimeType))
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoDeserializationConstructor(TypeToConvert);
                }

                state.Current.ReturnValue = new Dictionary<string, object>();
            }
            else
            {
                if (classInfo.CreateObject == null)
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoDeserializationConstructor(TypeToConvert);
                }

                TCollection returnValue = (TCollection)classInfo.CreateObject()!;

                if (returnValue.IsReadOnly)
                {
                    ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(TypeToConvert);
                }

                state.Current.ReturnValue = returnValue;
            }
        }

        protected internal override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
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
                Debug.Assert(state.Current.CollectionEnumerator is IDictionaryEnumerator);
                enumerator = (IDictionaryEnumerator)state.Current.CollectionEnumerator;
            }

            JsonConverter<object?> converter = GetValueConverter(ref state);
            do
            {
                if (enumerator.Key is string key)
                {
                    key = GetKeyName(key, ref state, options);
                    writer.WritePropertyName(key);

                    object? element = enumerator.Value;
                    if (!converter.TryWrite(writer, element, options, ref state))
                    {
                        state.Current.CollectionEnumerator = enumerator;
                        return false;
                    }

                    state.Current.EndDictionaryElement();
                }
                else
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(state.Current.DeclaredJsonPropertyInfo!.RuntimePropertyType!);
                }
            } while (enumerator.MoveNext());

            return true;
        }

        internal override Type RuntimeType
        {
            get
            {
                if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
                {
                    return typeof(Dictionary<string, object>);
                }

                return TypeToConvert;
            }
        }
    }
}
