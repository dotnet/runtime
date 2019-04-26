// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Represents a strongly-typed property that is not a <see cref="Nullable{T}"/>.
    /// </summary>
    internal sealed class JsonPropertyInfoNotNullable<TClass, TDeclaredProperty, TRuntimeProperty> :
        JsonPropertyInfoCommon<TClass, TDeclaredProperty, TRuntimeProperty>
        where TRuntimeProperty : TDeclaredProperty
    {
        // Constructor used for internal identifiers
        public JsonPropertyInfoNotNullable() { }

        public JsonPropertyInfoNotNullable(
            Type parentClassType,
            Type declaredPropertyType,
            Type runtimePropertyType,
            PropertyInfo propertyInfo,
            Type elementType,
            JsonSerializerOptions options) :
            base(parentClassType, declaredPropertyType, runtimePropertyType, propertyInfo, elementType, options)
        {
        }

        public override void Read(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (ElementClassInfo != null)
            {
                // Forward the setter to the value-based JsonPropertyInfo.
                JsonPropertyInfo propertyInfo = ElementClassInfo.GetPolicyProperty();
                propertyInfo.ReadEnumerable(tokenType, options, ref state, ref reader);
            }
            else if (ShouldDeserialize)
            {
                if (ValueConverter != null)
                {
                    if (ValueConverter.TryRead(RuntimePropertyType, ref reader, out TRuntimeProperty value))
                    {
                        if (state.Current.ReturnValue == null)
                        {
                            state.Current.ReturnValue = value;
                        }
                        else
                        {
                            // Null values were already handled.
                            Debug.Assert(value != null);

                            Set((TClass)state.Current.ReturnValue, value);
                        }

                        return;
                    }
                }

                ThrowHelper.ThrowJsonReaderException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state);
            }
        }

        // If this method is changed, also change JsonPropertyInfoNullable.ReadEnumerable and JsonSerializer.ApplyObjectToEnumerable
        public override void ReadEnumerable(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (ValueConverter == null || !ValueConverter.TryRead(RuntimePropertyType, ref reader, out TRuntimeProperty value))
            {
                ThrowHelper.ThrowJsonReaderException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state);
                return;
            }

            JsonSerializer.ApplyValueToEnumerable(ref value, options, ref state.Current);
        }

        public override void ApplyNullValue(JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(state.Current.JsonPropertyInfo != null);
            state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value : null);
        }

        // todo: have the caller check if current.Enumerator != null and call WriteEnumerable of the underlying property directly to avoid an extra virtual call.
        public override void Write(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            if (current.Enumerator != null)
            {
                // Forward the setter to the value-based JsonPropertyInfo.
                JsonPropertyInfo propertyInfo = ElementClassInfo.GetPolicyProperty();
                propertyInfo.WriteEnumerable(options, ref current, writer);
            }
            else if (ShouldSerialize)
            {
                TRuntimeProperty value;
                if (_isPropertyPolicy)
                {
                    value = (TRuntimeProperty)current.CurrentValue;
                }
                else
                {
                    value = (TRuntimeProperty)Get((TClass)current.CurrentValue);
                }

                if (value == null)
                {
                    if (_escapedName == null)
                    {
                        writer.WriteNullValue();
                    }
                    else if (!IgnoreNullValues)
                    {
                        writer.WriteNull(_escapedName);
                    }
                }
                else if (ValueConverter != null)
                {
                    if (_escapedName != null)
                    {
                        ValueConverter.Write(_escapedName, value, writer);
                    }
                    else
                    {
                        ValueConverter.Write(value, writer);
                    }
                }
            }
        }

        public override void WriteDictionary(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            JsonSerializer.WriteDictionary(ValueConverter, options, ref current, writer);
        }


        public override void WriteEnumerable(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer)
        {
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
