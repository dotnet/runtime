// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterEnum<TValue> : JsonValueConverter<TValue>
        where TValue : struct, Enum
    {
        private static readonly TypeCode s_enumTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(TValue)));

        public bool TreatAsString { get; private set; }

        internal JsonValueConverterEnum(bool treatAsString)
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

                string enumString = reader.GetString();
                return Enum.TryParse(enumString, out value);
            }

            if (reader.TokenType != JsonTokenType.Number)
            {
                value = default;
                return false;
            }

            // When utf8reader/writer will support all primitive types we should remove custom bound checks
            // https://github.com/dotnet/corefx/issues/36125

            switch (s_enumTypeCode)
            {
                case TypeCode.SByte:
                    {
                        if (reader.TryGetInt32(out int byte8) && JsonHelpers.IsInRangeInclusive(byte8, sbyte.MinValue, sbyte.MaxValue))
                        {
                            sbyte byte8Value = (sbyte)byte8;
                            value = Unsafe.As<sbyte, TValue>(ref byte8Value);
                            return true;
                        }
                        break;
                    }
                case TypeCode.Byte:
                    {
                        if (reader.TryGetUInt32(out uint ubyte8) && JsonHelpers.IsInRangeInclusive(ubyte8, byte.MinValue, byte.MaxValue))
                        {
                            byte ubyte8Value = (byte)ubyte8;
                            value = Unsafe.As<byte, TValue>(ref ubyte8Value);
                            return true;
                        }
                        break;
                    }
                case TypeCode.Int16:
                    {
                        if (reader.TryGetInt32(out int int16) && JsonHelpers.IsInRangeInclusive(int16, short.MinValue, short.MaxValue))
                        {
                            short shortValue = (short)int16;
                            value = Unsafe.As<short, TValue>(ref shortValue);
                            return true;
                        }
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        if (reader.TryGetUInt32(out uint uint16) && JsonHelpers.IsInRangeInclusive(uint16, ushort.MinValue, ushort.MaxValue))
                        {
                            ushort ushortValue = (ushort)uint16;
                            value = Unsafe.As<ushort, TValue>(ref ushortValue);
                            return true;
                        }
                        break;
                    }
                case TypeCode.Int32:
                    {
                        if (reader.TryGetInt32(out int int32))
                        {
                            value = Unsafe.As<int, TValue>(ref int32);
                            return true;
                        }
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        if (reader.TryGetUInt32(out uint uint32))
                        {
                            value = Unsafe.As<uint, TValue>(ref uint32);
                            return true;
                        }
                        break;
                    }
                case TypeCode.Int64:
                    {
                        if (reader.TryGetInt64(out long int64))
                        {
                            value = Unsafe.As<long, TValue>(ref int64);
                            return true;
                        }
                        break;
                    }
                case TypeCode.UInt64:
                    {
                        if (reader.TryGetUInt64(out ulong uint64))
                        {
                            value = Unsafe.As<ulong, TValue>(ref uint64);
                            return true;
                        }
                        break;
                    }
            }

            value = default;
            return false;
        }

        public override void Write(TValue value, Utf8JsonWriter writer)
        {
            if (TreatAsString)
            {
                writer.WriteStringValue(value.ToString());
            }
            else if (s_enumTypeCode == TypeCode.UInt64)
            {
                // Use the ulong converter to prevent conversion into a signed\long value.
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

        public override void Write(JsonEncodedText propertyName, TValue value, Utf8JsonWriter writer)
        {
            if (TreatAsString)
            {
                writer.WriteString(propertyName, value.ToString());
            }
            else if (s_enumTypeCode == TypeCode.UInt64)
            {
                // Use the ulong converter to prevent conversion into a signed\long value.
                ulong ulongValue = Convert.ToUInt64(value);
                writer.WriteNumber(propertyName, ulongValue);
            }
            else
            {
                // long can hold the signed\unsigned values of other integer types.
                long longValue = Convert.ToInt64(value);
                writer.WriteNumber(propertyName, longValue);
            }
        }
    }
}
