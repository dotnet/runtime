// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DefaultEnumConverter<TValue> : JsonValueConverter<TValue>
    {
        public bool TreatAsString { get; private set; }

        internal DefaultEnumConverter(bool treatAsString)
        {
            TreatAsString = treatAsString;
        }

        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out TValue value)
        {
            if (TreatAsString)
            {
                // Assume the token is a string
                if (reader.TokenType != JsonTokenType.String)
                {
                    value = default;
                    return false;
                }

#if !BUILDING_INBOX_LIBRARY 
                // todo: add code here to handle NS2.0
                throw new NotImplementedException(SR.EnumConverterNotImplemented);
#else
                string enumString = reader.GetString();
                if (!Enum.TryParse(valueType, enumString, out object objValue))
                {
                    value = default;
                    return false;
                }

                value = (TValue)objValue;
                return true;
#endif
            }

            if (reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetUInt64(out ulong ulongValue))
            {
                value = default;
                return false;
            }

            value = (TValue)Enum.ToObject(valueType, ulongValue);
            return true;
        }

        public override void Write(TValue value, ref Utf8JsonWriter writer)
        {
            if (TreatAsString)
            {
                writer.WriteStringValue(value.ToString());
            }
            else
            {
                Type underlyingType = Enum.GetUnderlyingType(value.GetType());

                if (underlyingType == typeof(ulong))
                {
                    // Keep +sign
                    ulong ulongValue = Convert.ToUInt64(value);
                    writer.WriteNumberValue(ulongValue);
                }
                else
                {
                    // long can hold the signed\unsigned values of other integer types
                    long longValue = Convert.ToInt64(value);
                    writer.WriteNumberValue(longValue);
                }
            }
        }

        public override void Write(Span<byte> escapedPropertyName, TValue value, ref Utf8JsonWriter writer)
        {
            if (TreatAsString)
            {
                writer.WriteString(escapedPropertyName, value.ToString());
            }
            else
            {
                Type underlyingType = Enum.GetUnderlyingType(value.GetType());

                if (underlyingType == typeof(ulong))
                {
                    // Use the ulong converter to prevent conversion into a signed\long value.
                    ulong ulongValue = Convert.ToUInt64(value);
                    writer.WriteNumber(escapedPropertyName, ulongValue);
                }
                else
                {
                    // long can hold the signed\unsigned values of other integer types.
                    long longValue = Convert.ToInt64(value);
                    writer.WriteNumber(escapedPropertyName, longValue);
                }
            }
        }
    }
}
