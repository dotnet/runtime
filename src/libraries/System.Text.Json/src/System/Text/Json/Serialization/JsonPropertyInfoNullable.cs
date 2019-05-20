// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

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

        public override void Read(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            Debug.Assert(ElementClassInfo == null);
            Debug.Assert(ShouldDeserialize);

            if (ValueConverter != null && ValueConverter.TryRead(s_underlyingType, ref reader, out TProperty value))
            {
                if (state.Current.ReturnValue == null)
                {
                    state.Current.ReturnValue = value;
                }
                else
                {
                    Set(state.Current.ReturnValue, value);
                }

                return;
            }

            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state.PropertyPath);
        }

        public override void ReadEnumerable(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            Debug.Assert(ShouldDeserialize);

            if (ValueConverter == null || !ValueConverter.TryRead(typeof(TProperty), ref reader, out TProperty value))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(RuntimePropertyType, reader, state.PropertyPath);
                return;
            }

            TProperty? nullableValue = new TProperty?(value);
            JsonSerializer.ApplyValueToEnumerable(ref nullableValue, ref state, ref reader);
        }

        public override void Write(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            Debug.Assert(ShouldSerialize);

            if (current.Enumerator != null)
            {
                // Forward the setter to the value-based JsonPropertyInfo.
                JsonPropertyInfo propertyInfo = ElementClassInfo.GetPolicyProperty();
                propertyInfo.WriteEnumerable(options, ref current, writer);
            }
            else
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
                    Debug.Assert(EscapedName != null);

                    if (!IgnoreNullValues)
                    {
                        writer.WriteNull(EscapedName);
                    }
                }
                else if (ValueConverter != null)
                {
                    if (EscapedName != null)
                    {
                        ValueConverter.Write(EscapedName, value.GetValueOrDefault(), writer);
                    }
                    else
                    {
                        ValueConverter.Write(value.GetValueOrDefault(), writer);
                    }
                }
            }
        }

        public override void WriteEnumerable(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer)
        {
            Debug.Assert(ShouldSerialize);

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
