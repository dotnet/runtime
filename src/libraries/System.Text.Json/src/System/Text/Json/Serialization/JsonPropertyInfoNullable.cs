// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Represents a strongly-typed property that is a <see cref="Nullable{T}"/>.
    /// </summary>
    internal sealed class JsonPropertyInfoNullable<TClass, TProperty>
        : JsonPropertyInfoCommon<TClass, TProperty?, TProperty>
        where TProperty : struct
    {
        // should this be cached somewhere else so that it's not populated per TClass as well as TProperty?
        private static readonly Type s_underlyingType = typeof(TProperty);

        public JsonPropertyInfoNullable(
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
                    if (ValueConverter.TryRead(s_underlyingType, ref reader, out TProperty value))
                    {
                        if (state.Current.ReturnValue == null)
                        {
                            state.Current.ReturnValue = value;
                        }
                        else
                        {
                            Set((TClass)state.Current.ReturnValue, value);
                        }

                        return;
                    }
                }

                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state.PropertyPath);
            }
        }

        public override void ReadEnumerable(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (ValueConverter == null || !ValueConverter.TryRead(typeof(TProperty), ref reader, out TProperty value))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state.PropertyPath);
                return;
            }

            // Converting to TProperty? here lets us share a common ApplyValue() with ApplyNullValue().
            TProperty? nullableValue = new TProperty?(value);
            JsonSerializer.ApplyValueToEnumerable(ref nullableValue, options, ref state, ref reader);
        }

        public override void ApplyNullValue(JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            TProperty? nullableValue = null;
            JsonSerializer.ApplyValueToEnumerable(ref nullableValue, options, ref state, ref reader);
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
                TProperty? value;
                if (_isPropertyPolicy)
                {
                    value = (TProperty?)current.CurrentValue;
                }
                else
                {
                    value = Get((TClass)current.CurrentValue);
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
                        ValueConverter.Write(_escapedName, value.GetValueOrDefault(), writer);
                    }
                    else
                    {
                        ValueConverter.Write(value.GetValueOrDefault(), writer);
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

                TProperty? value;
                if (current.Enumerator is IEnumerator<TProperty?> enumerator)
                {
                    // Avoid boxing for strongly-typed enumerators such as returned from IList<T>.
                    value = enumerator.Current;
                }
                else
                {
                    value = (TProperty?)current.Enumerator.Current;
                }

                if (value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    ValueConverter.Write(value.GetValueOrDefault(), writer);
                }
            }
        }
    }
}
