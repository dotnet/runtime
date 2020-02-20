// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.IDictionary{string, TValue}</cref> that
    /// (de)serializes as a JSON object with properties representing the dictionary element key and value.
    /// </summary>
    internal sealed class IDictionaryOfStringTValueConverter<TCollection, TValue>
        : DictionaryDefaultConverter<TCollection, TValue>
        where TCollection : IDictionary<string, TValue>
    {
        protected override void Add(TValue value, JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(state.Current.ReturnValue is TCollection);

            string key = state.Current.JsonPropertyNameAsString!;
            ((TCollection)state.Current.ReturnValue!)[key] = value;
        }

        protected override void CreateCollection(ref ReadStack state)
        {
            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            if (TypeToConvert.IsInterface || TypeToConvert.IsAbstract)
            {
                if (!TypeToConvert.IsAssignableFrom(RuntimeType))
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoParameterlessConstructor(TypeToConvert);
                }

                state.Current.ReturnValue = new Dictionary<string, TValue>();
            }
            else
            {
                if (classInfo.CreateObject == null)
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoParameterlessConstructor(TypeToConvert);
                }

                TCollection returnValue = (TCollection)classInfo.CreateObject()!;

                if (returnValue.IsReadOnly)
                {
                    ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(TypeToConvert);
                }

                state.Current.ReturnValue = returnValue;
            }
        }

        protected internal override bool OnWriteResume(
            Utf8JsonWriter writer,
            TCollection value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            IEnumerator<KeyValuePair<string, TValue>> enumerator;
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
                Debug.Assert(state.Current.CollectionEnumerator is IEnumerator<KeyValuePair<string, TValue>>);
                enumerator = (IEnumerator<KeyValuePair<string, TValue>>)state.Current.CollectionEnumerator;
            }

            JsonConverter<TValue> converter = GetValueConverter(ref state);
            do
            {
                if (ShouldFlush(writer, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                string key = GetKeyName(enumerator.Current.Key, ref state, options);
                writer.WritePropertyName(key);

                TValue element = enumerator.Current.Value;
                if (!converter.TryWrite(writer, element, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                state.Current.EndDictionaryElement();
            } while (enumerator.MoveNext());

            return true;
        }

        internal override Type RuntimeType
        {
            get
            {
                if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
                {
                    return typeof(Dictionary<string, TValue>);
                }

                return TypeToConvert;
            }
        }
    }
}
