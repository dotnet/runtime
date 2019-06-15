// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json
{
    /// <summary>
    /// Represents a strongly-typed property that is not a <see cref="Nullable{T}"/>.
    /// </summary>
    internal sealed class JsonPropertyInfoNotNullable<TClass, TDeclaredProperty, TRuntimeProperty> :
        JsonPropertyInfoCommon<TClass, TDeclaredProperty, TRuntimeProperty>
        where TRuntimeProperty : TDeclaredProperty
    {
        public override void Read(JsonTokenType tokenType, ref ReadStack state, ref Utf8JsonReader reader)
        {
            Debug.Assert(ShouldDeserialize);

            if (ElementClassInfo != null)
            {
                // Forward the setter to the value-based JsonPropertyInfo.
                JsonPropertyInfo propertyInfo = ElementClassInfo.GetPolicyProperty();
                propertyInfo.ReadEnumerable(tokenType, ref state, ref reader);
            }
            else
            {
                if (ValueConverter != null && ValueConverter.TryRead(RuntimePropertyType, ref reader, out TRuntimeProperty value))
                {
                    if (state.Current.ReturnValue == null)
                    {
                        state.Current.ReturnValue = value;
                    }
                    else
                    {
                        // Null values were already handled.
                        Debug.Assert(value != null);

                            Set(state.Current.ReturnValue, value);
                        }

                    return;
                }

                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state.JsonPath);
            }
        }

        // If this method is changed, also change JsonPropertyInfoNullable.ReadEnumerable and JsonSerializer.ApplyObjectToEnumerable
        public override void ReadEnumerable(JsonTokenType tokenType, ref ReadStack state, ref Utf8JsonReader reader)
        {
            Debug.Assert(ShouldDeserialize);

            if (state.Current.KeyName == null && (state.Current.IsProcessingDictionary || state.Current.IsProcessingIDictionaryConstructibleOrKeyValuePair))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state.JsonPath);
                return;
            }

            // We need an initialized array in order to store the values.
            if (state.Current.IsProcessingEnumerable && state.Current.TempEnumerableValues == null && state.Current.ReturnValue == null)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state.JsonPath);
                return;
            }

            if (ValueConverter == null || !ValueConverter.TryRead(RuntimePropertyType, ref reader, out TRuntimeProperty value))
            {
                if (state.Current.IsProcessingKeyValuePair && state.Current.KeyName == "Key")
                {
                    // Handle the special case where the input KeyValuePair is of form {"Key": "MyKey", "Value": 1}
                    // (as opposed to form {"MyKey": 1}) and the value type is not string.
                    // If we have one, the ValueConverter failed to read the current token because it should be of type string
                    // (we only support string keys) but we initially tried to read it as type TRuntimeProperty.
                    // We have TRuntimeProperty not string because for deserialization, we parse the KeyValuePair as a
                    // dictionary before creating a KeyValuePair instance in a converter-like manner with the parsed values.
                    // Because it's dictionary-like parsing, we set the element type of the dictionary earlier on to the KeyValuePair's value
                    // type, which led us here.
                    // If there's no ValueConverter, the runtime type of the KeyValuePair's value is probably an object, dictionary or enumerable.
                    JsonValueConverter<string> stringConverter = DefaultConverters<string>.s_converter;
                    if (!stringConverter.TryRead(typeof(string), ref reader, out string strValue))
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(typeof(string), reader, state.JsonPath);
                    }

                    object objValue = strValue;

                    JsonSerializer.ApplyValueToEnumerable(ref objValue, ref state, ref reader);
                    return;
                }

                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state.JsonPath);
                return;
            }

            if (state.Current.IsProcessingKeyValuePair)
            {
                // The value is being applied to a Dictionary<string, object>, so we need to cast to object here.
                object objValue = value;
                JsonSerializer.ApplyValueToEnumerable(ref objValue, ref state, ref reader);
                return;
            }

            JsonSerializer.ApplyValueToEnumerable(ref value, ref state, ref reader);
        }

        public override void Write(ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            Debug.Assert(current.Enumerator == null);
            Debug.Assert(ShouldSerialize);

            TRuntimeProperty value;
            if (IsPropertyPolicy)
            {
                value = (TRuntimeProperty)current.CurrentValue;
            }
            else
            {
                value = (TRuntimeProperty)Get(current.CurrentValue);
            }

            if (value == null)
            {
                Debug.Assert(EscapedName.HasValue);

                if (!IgnoreNullValues)
                {
                    writer.WriteNull(EscapedName.Value);
                }
            }
            else if (ValueConverter != null)
            {
                if (EscapedName.HasValue)
                {
                    ValueConverter.Write(EscapedName.Value, value, writer);
                }
                else
                {
                    ValueConverter.Write(value, writer);
                }
            }
        }

        public override void WriteDictionary(ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            Debug.Assert(ShouldSerialize);
            JsonSerializer.WriteDictionary(ValueConverter, Options, ref current, writer);
        }

        public override void WriteEnumerable(ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            Debug.Assert(ShouldSerialize);

            if (ValueConverter != null)
            {
                Debug.Assert(current.Enumerator != null);

                TRuntimeProperty value;
                if (current.Enumerator is IEnumerator<TRuntimeProperty> enumerator)
                {
                    // Avoid boxing for strongly-typed enumerators such as returned from IList<T>.
                    value = enumerator.Current;
                }
                else
                {
                    value = (TRuntimeProperty)current.Enumerator.Current;
                }

                if (value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    ValueConverter.Write(value, writer);
                }
            }
        }
    }
}
