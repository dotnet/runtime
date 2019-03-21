// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Represents a strongly-typed property that is not a <see cref="Nullable{T}"/>.
    /// </summary>
    internal sealed class JsonPropertyInfoNotNullable<TClass, TProperty> : JsonPropertyInfo<TClass, TProperty, TProperty>
    {
        // Constructor used for internal identifiers
        internal JsonPropertyInfoNotNullable() { }

        internal JsonPropertyInfoNotNullable(Type classType, Type propertyType, PropertyInfo propertyInfo, Type elementType, JsonSerializerOptions options) :
            base(classType, propertyType, propertyInfo, elementType, options)
        {
        }

        internal override void Read(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (ElementClassInfo != null)
            {
                // Forward the setter to the value-based JsonPropertyInfo.
                JsonPropertyInfo propertyInfo = ElementClassInfo.GetPolicyProperty();
                propertyInfo.ReadEnumerable(tokenType, options, ref state, ref reader);
            }
            else if (HasSetter)
            {
                if (ValueConverter != null)
                {
                    if (ValueConverter.TryRead(PropertyType, ref reader, out TProperty value))
                    {
                        if (state.Current.ReturnValue == null)
                        {
                            state.Current.ReturnValue = value;
                        }
                        else
                        {
                            if (value != null || !IgnoreNullPropertyValueOnRead(options))
                            {
                                Set((TClass)state.Current.ReturnValue, value);
                            }
                        }

                        return;
                    }
                }

                ThrowHelper.ThrowJsonReaderException_DeserializeUnableToConvertValue(PropertyType, reader, state);
            }
        }

        internal override void ReadEnumerable(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (ValueConverter != null)
            {
                if (ValueConverter.TryRead(PropertyType, ref reader, out TProperty value))
                {
                    ReadStackFrame.SetReturnValue(value, options, ref state.Current);
                    return;
                }
            }

            ThrowHelper.ThrowJsonReaderException_DeserializeUnableToConvertValue(PropertyType, reader, state);
        }

        // todo: have the caller check if current.Enumerator != null and call WriteEnumerable of the underlying property directly to avoid an extra virtual call.
        internal override void Write(JsonSerializerOptions options, ref WriteStackFrame current, ref Utf8JsonWriter writer)
        {
            if (current.Enumerator != null)
            {
                // Forward the setter to the value-based JsonPropertyInfo.
                JsonPropertyInfo propertyInfo = ElementClassInfo.GetPolicyProperty();
                propertyInfo.WriteEnumerable(options, ref current, ref writer);
            }
            else if (HasGetter)
            {
                TProperty value;
                if (_isPropertyPolicy)
                {
                    value = (TProperty)current.CurrentValue;
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
                    else if (!IgnoreNullPropertyValueOnWrite(options))
                    {
                        writer.WriteNull(_escapedName);
                    }
                }
                else if (ValueConverter != null)
                {
                    if (_escapedName != null)
                    {
                        ValueConverter.Write(_escapedName, value, ref writer);
                    }
                    else
                    {
                        ValueConverter.Write(value, ref writer);
                    }
                }
            }
        }

        internal override void WriteEnumerable(JsonSerializerOptions options, ref WriteStackFrame current, ref Utf8JsonWriter writer)
        {
            if (ValueConverter != null)
            {
                Debug.Assert(current.Enumerator != null);
                TProperty value = (TProperty)current.Enumerator.Current;
                if (value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    ValueConverter.Write(value, ref writer);
                }
            }
        }
    }
}
