// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    internal class JsonConverterEnum<T> : JsonConverter<T>
        where T : struct, Enum
    {
        private static readonly TypeCode s_enumTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T)));

        public bool TreatAsString { get; private set; }

        public override bool CanConvert(Type type)
        {
            return type.IsEnum;
        }

        public JsonConverterEnum(bool treatAsString)
        {
            TreatAsString = treatAsString;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (TreatAsString)
            {
                // Assume the token is a string
                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException();
                    return default;
                }

                string enumString = reader.GetString();
                if (!Enum.TryParse(enumString, out T value))
                {
                    ThrowHelper.ThrowJsonException();
                    return default;
                }
            }

            if (reader.TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowJsonException();
                return default;
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
                            return Unsafe.As<sbyte, T>(ref byte8Value);
                        }
                        break;
                    }
                case TypeCode.Byte:
                    {
                        if (reader.TryGetUInt32(out uint ubyte8) && JsonHelpers.IsInRangeInclusive(ubyte8, byte.MinValue, byte.MaxValue))
                        {
                            byte ubyte8Value = (byte)ubyte8;
                            return Unsafe.As<byte, T>(ref ubyte8Value);
                        }
                        break;
                    }
                case TypeCode.Int16:
                    {
                        if (reader.TryGetInt32(out int int16) && JsonHelpers.IsInRangeInclusive(int16, short.MinValue, short.MaxValue))
                        {
                            short shortValue = (short)int16;
                            return Unsafe.As<short, T>(ref shortValue);
                        }
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        if (reader.TryGetUInt32(out uint uint16) && JsonHelpers.IsInRangeInclusive(uint16, ushort.MinValue, ushort.MaxValue))
                        {
                            ushort ushortValue = (ushort)uint16;
                            return Unsafe.As<ushort, T>(ref ushortValue);
                        }
                        break;
                    }
                case TypeCode.Int32:
                    {
                        if (reader.TryGetInt32(out int int32))
                        {
                            return Unsafe.As<int, T>(ref int32);
                        }
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        if (reader.TryGetUInt32(out uint uint32))
                        {
                            return Unsafe.As<uint, T>(ref uint32);
                        }
                        break;
                    }
                case TypeCode.Int64:
                    {
                        if (reader.TryGetInt64(out long int64))
                        {
                            return Unsafe.As<long, T>(ref int64);
                        }
                        break;
                    }
                case TypeCode.UInt64:
                    {
                        if (reader.TryGetUInt64(out ulong uint64))
                        {
                            return Unsafe.As<ulong, T>(ref uint64);
                        }
                        break;
                    }
            }

            ThrowHelper.ThrowJsonException();
            return default;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
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

        public override void Write(Utf8JsonWriter writer, T value, JsonEncodedText propertyName, JsonSerializerOptions options)
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
