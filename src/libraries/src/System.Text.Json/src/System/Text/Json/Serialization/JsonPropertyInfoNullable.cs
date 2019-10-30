// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    /// Represents a strongly-typed property that is a <see cref="Nullable{T}"/>.
    /// </summary>
    internal sealed class JsonPropertyInfoNullable<TClass, TProperty>
        : JsonPropertyInfoCommon<TClass, TProperty?, TProperty, TProperty>
        where TProperty : struct
    {
        private static readonly Type s_underlyingType = typeof(TProperty);

        protected override void OnRead(ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (Converter == null)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType);
            }

            TProperty value = Converter.Read(ref reader, s_underlyingType, Options);

            if (state.Current.ReturnValue == null)
            {
                state.Current.ReturnValue = value;
            }
            else
            {
                Set(state.Current.ReturnValue, value);
            }
        }

        protected override void OnReadEnumerable(ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (Converter == null)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType);
            }

            TProperty value = Converter.Read(ref reader, s_underlyingType, Options);
            TProperty? nullableValue = new TProperty?(value);
            JsonSerializer.ApplyValueToEnumerable(ref nullableValue, ref state);
        }

        protected override void OnWrite(ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            TProperty? value;
            if (IsPropertyPolicy)
            {
                value = (TProperty?)current.CurrentValue;
            }
            else
            {
                value = Get(current.CurrentValue);
            }

            if (value == null)
            {
                Debug.Assert(EscapedName.HasValue);

                if (!IgnoreNullValues)
                {
                    writer.WriteNull(EscapedName.Value);
                }
            }
            else if (Converter != null)
            {
                if (EscapedName.HasValue)
                {
                    writer.WritePropertyName(EscapedName.Value);
                }

                Converter.Write(writer, value.GetValueOrDefault(), Options);
            }
        }

        protected override void OnWriteDictionary(ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            Debug.Assert(Converter != null && current.CollectionEnumerator != null);

            string key = null;
            TProperty? value = null;
            if (current.CollectionEnumerator is IEnumerator<KeyValuePair<string, TProperty?>> enumerator)
            {
                key = enumerator.Current.Key;
                value = enumerator.Current.Value;
            }
            else
            {
                if (((DictionaryEntry)current.CollectionEnumerator.Current).Key is string keyAsString)
                {
                    key = keyAsString;
                    value = (TProperty?)((DictionaryEntry)current.CollectionEnumerator.Current).Value;
                }
                else
                {
                    throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                        current.JsonPropertyInfo.DeclaredPropertyType,
                        current.JsonPropertyInfo.ParentClassType,
                        current.JsonPropertyInfo.PropertyInfo);
                }
            }

            Debug.Assert(key != null);

            if (value == null)
            {
                writer.WriteNull(key);
            }
            else
            {
                if (Options.DictionaryKeyPolicy != null)
                {
                    key = Options.DictionaryKeyPolicy.ConvertName(key);

                    if (key == null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializerDictionaryKeyNull(Options.DictionaryKeyPolicy.GetType());
                    }
                }

                writer.WritePropertyName(key);
                Converter.Write(writer, value.GetValueOrDefault(), Options);
            }
        }

        protected override void OnWriteEnumerable(ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            if (Converter != null)
            {
                Debug.Assert(current.CollectionEnumerator != null);

                TProperty? value;
                if (current.CollectionEnumerator is IEnumerator<TProperty?> enumerator)
                {
                    // Avoid boxing for strongly-typed enumerators such as returned from IList<T>.
                    value = enumerator.Current;
                }
                else
                {
                    value = (TProperty?)current.CollectionEnumerator.Current;
                }

                if (value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    Converter.Write(writer, value.GetValueOrDefault(), Options);
                }
            }
        }

        public override Type GetDictionaryConcreteType()
        {
            return typeof(Dictionary<string, TProperty?>);
        }

        public override void GetDictionaryKeyAndValueFromGenericDictionary(ref WriteStackFrame writeStackFrame, out string key, out object value)
        {
            if (writeStackFrame.CollectionEnumerator is IEnumerator<KeyValuePair<string, TProperty?>> genericEnumerator)
            {
                key = genericEnumerator.Current.Key;
                value = genericEnumerator.Current.Value;
            }
            else
            {
                throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                    writeStackFrame.JsonPropertyInfo.DeclaredPropertyType,
                    writeStackFrame.JsonPropertyInfo.ParentClassType,
                    writeStackFrame.JsonPropertyInfo.PropertyInfo);
            }
        }
    }
}
