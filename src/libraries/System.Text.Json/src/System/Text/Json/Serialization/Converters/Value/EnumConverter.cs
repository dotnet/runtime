// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace System.Text.Json.Serialization.Converters
{
    internal class EnumConverter<T> : JsonConverter<T>
        where T : struct, Enum
    {
        private static readonly TypeCode s_enumTypeCode = Type.GetTypeCode(typeof(T));

        // Odd type codes are conveniently signed types (for enum backing types).
        private static readonly string? s_negativeSign = ((int)s_enumTypeCode % 2) == 0 ? null : NumberFormatInfo.CurrentInfo.NegativeSign;

        private const string ValueSeparator = ", ";

        private readonly EnumConverterOptions _converterOptions;
        private readonly JsonNamingPolicy? _namingPolicy;
        private readonly ConcurrentDictionary<ulong, JsonEncodedText> _nameCache;

        public override bool CanConvert(Type type)
        {
            return type.IsEnum;
        }

        public EnumConverter(EnumConverterOptions converterOptions, JsonSerializerOptions serializerOptions)
            : this(converterOptions, namingPolicy: null, serializerOptions)
        {
        }

        public EnumConverter(EnumConverterOptions converterOptions, JsonNamingPolicy? namingPolicy, JsonSerializerOptions serializerOptions)
        {
            _converterOptions = converterOptions;
            _namingPolicy = namingPolicy;
            _nameCache = new ConcurrentDictionary<ulong, JsonEncodedText>();

            string[] names = Enum.GetNames(TypeToConvert);
            Array values = Enum.GetValues(TypeToConvert);
            Debug.Assert(names.Length > 0 && names.Length == values.Length);

            JavaScriptEncoder? encoder = serializerOptions.Encoder;

            for (int i = 0; i < names.Length; i++)
            {
                T value = (T)values.GetValue(i)!;
                // Enum values can be represented as ulong. Note that F# supports char as enum backing types.
                ulong key = Unsafe.As<T, ulong>(ref value);
                string name = names[i];

                _nameCache.TryAdd(
                    key,
                    namingPolicy == null
                        ? JsonEncodedText.Encode(name, encoder)
                        : FormatEnumValue(name, encoder));
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonTokenType token = reader.TokenType;

            if (token == JsonTokenType.String)
            {
                if (!_converterOptions.HasFlag(EnumConverterOptions.AllowStrings))
                {
                    ThrowHelper.ThrowJsonException();
                    return default;
                }

                // Try parsing case sensitive first
                string? enumString = reader.GetString();
                if (!Enum.TryParse(enumString, out T value)
                    && !Enum.TryParse(enumString, ignoreCase: true, out value))
                {
                    ThrowHelper.ThrowJsonException();
                    return default;
                }
                return value;
            }

            if (token != JsonTokenType.Number || !_converterOptions.HasFlag(EnumConverterOptions.AllowNumbers))
            {
                ThrowHelper.ThrowJsonException();
                return default;
            }

            switch (s_enumTypeCode)
            {
                // Switch cases ordered by expected frequency

                case TypeCode.Int32:
                    if (reader.TryGetInt32(out int int32))
                    {
                        return Unsafe.As<int, T>(ref int32);
                    }
                    break;
                case TypeCode.UInt32:
                    if (reader.TryGetUInt32(out uint uint32))
                    {
                        return Unsafe.As<uint, T>(ref uint32);
                    }
                    break;
                case TypeCode.UInt64:
                    if (reader.TryGetUInt64(out ulong uint64))
                    {
                        return Unsafe.As<ulong, T>(ref uint64);
                    }
                    break;
                case TypeCode.Int64:
                    if (reader.TryGetInt64(out long int64))
                    {
                        return Unsafe.As<long, T>(ref int64);
                    }
                    break;

                // When utf8reader/writer will support all primitive types we should remove custom bound checks
                // https://github.com/dotnet/runtime/issues/29000
                case TypeCode.SByte:
                    if (reader.TryGetInt32(out int byte8) && JsonHelpers.IsInRangeInclusive(byte8, sbyte.MinValue, sbyte.MaxValue))
                    {
                        sbyte byte8Value = (sbyte)byte8;
                        return Unsafe.As<sbyte, T>(ref byte8Value);
                    }
                    break;
                case TypeCode.Byte:
                    if (reader.TryGetUInt32(out uint ubyte8) && JsonHelpers.IsInRangeInclusive(ubyte8, byte.MinValue, byte.MaxValue))
                    {
                        byte ubyte8Value = (byte)ubyte8;
                        return Unsafe.As<byte, T>(ref ubyte8Value);
                    }
                    break;
                case TypeCode.Int16:
                    if (reader.TryGetInt32(out int int16) && JsonHelpers.IsInRangeInclusive(int16, short.MinValue, short.MaxValue))
                    {
                        short shortValue = (short)int16;
                        return Unsafe.As<short, T>(ref shortValue);
                    }
                    break;
                case TypeCode.UInt16:
                    if (reader.TryGetUInt32(out uint uint16) && JsonHelpers.IsInRangeInclusive(uint16, ushort.MinValue, ushort.MaxValue))
                    {
                        ushort ushortValue = (ushort)uint16;
                        return Unsafe.As<ushort, T>(ref ushortValue);
                    }
                    break;
            }

            ThrowHelper.ThrowJsonException();
            return default;
        }

        private static bool IsValidIdentifier(string value)
        {
            // Trying to do this check efficiently. When an enum is converted to
            // string the underlying value is given if it can't find a matching
            // identifier (or identifiers in the case of flags).
            //
            // The underlying value will be given back with a digit (e.g. 0-9) possibly
            // preceded by a negative sign. Identifiers have to start with a letter
            // so we'll just pick the first valid one and check for a negative sign
            // if needed.
            return (value[0] >= 'A' &&
                (s_negativeSign == null || !value.StartsWith(s_negativeSign)));
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // If strings are allowed, attempt to write it out as a string value
            if (_converterOptions.HasFlag(EnumConverterOptions.AllowStrings))
            {
                ulong key = Unsafe.As<T, ulong>(ref value);

                if (_nameCache.TryGetValue(key, out JsonEncodedText transformed))
                {
                    writer.WriteStringValue(transformed);
                    return;
                }

                string original = value.ToString();
                if (IsValidIdentifier(original))
                {
                    JavaScriptEncoder? encoder = options.Encoder;

                    // We are dealing with flags since all literal values were cached during warm-up.
                    transformed = _namingPolicy == null
                        ? JsonEncodedText.Encode(original, encoder)
                        : FormatEnumValue(original, encoder);

                    writer.WriteStringValue(transformed);

                    // Since the value represents a valid identifier, malicious user input is not added to the cache.
                    _nameCache.TryAdd(key, transformed);

                    return;
                }
            }

            if (!_converterOptions.HasFlag(EnumConverterOptions.AllowNumbers))
            {
                ThrowHelper.ThrowJsonException();
            }

            switch (s_enumTypeCode)
            {
                case TypeCode.Int32:
                    writer.WriteNumberValue(Unsafe.As<T, int>(ref value));
                    break;
                case TypeCode.UInt32:
                    writer.WriteNumberValue(Unsafe.As<T, uint>(ref value));
                    break;
                case TypeCode.UInt64:
                    writer.WriteNumberValue(Unsafe.As<T, ulong>(ref value));
                    break;
                case TypeCode.Int64:
                    writer.WriteNumberValue(Unsafe.As<T, long>(ref value));
                    break;
                case TypeCode.Int16:
                    writer.WriteNumberValue(Unsafe.As<T, short>(ref value));
                    break;
                case TypeCode.UInt16:
                    writer.WriteNumberValue(Unsafe.As<T, ushort>(ref value));
                    break;
                case TypeCode.Byte:
                    writer.WriteNumberValue(Unsafe.As<T, byte>(ref value));
                    break;
                case TypeCode.SByte:
                    writer.WriteNumberValue(Unsafe.As<T, sbyte>(ref value));
                    break;
                default:
                    ThrowHelper.ThrowJsonException();
                    break;
            }
        }

        private JsonEncodedText FormatEnumValue(string value, JavaScriptEncoder? encoder)
        {
            Debug.Assert(_namingPolicy != null);
            string converted;

            if (!value.Contains(ValueSeparator))
            {
                converted = _namingPolicy.ConvertName(value);
            }
            else
            {
                // todo: optimize implementation here by leveraging https://github.com/dotnet/runtime/issues/934.
                string[] enumValues = value.Split(
#if BUILDING_INBOX_LIBRARY
                    ValueSeparator
#else
                    new string[] { ValueSeparator }, StringSplitOptions.None
#endif
                    );

                for (int i = 0; i < enumValues.Length; i++)
                {
                    enumValues[i] = _namingPolicy.ConvertName(enumValues[i]);
                }

                converted = string.Join(ValueSeparator, enumValues);
            }

            return JsonEncodedText.Encode(converted, encoder);
        }
    }
}
