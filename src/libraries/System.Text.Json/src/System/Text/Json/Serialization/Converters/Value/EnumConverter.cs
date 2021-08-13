// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class EnumConverter<T> : JsonConverter<T>
        where T : struct, Enum
    {
        private static readonly TypeCode s_enumTypeCode = Type.GetTypeCode(typeof(T));

        // Odd type codes are conveniently signed types (for enum backing types).
        private static readonly string? s_negativeSign = ((int)s_enumTypeCode % 2) == 0 ? null : NumberFormatInfo.CurrentInfo.NegativeSign;

        private const string ValueSeparator = ", ";

        private readonly EnumConverterOptions _converterOptions;

        private readonly JsonNamingPolicy? _namingPolicy;

        private readonly ConcurrentDictionary<ulong, JsonEncodedText> _nameCache;

        private ConcurrentDictionary<ulong, JsonEncodedText>? _dictionaryKeyPolicyCache;

        // This is used to prevent flooding the cache due to exponential bitwise combinations of flags.
        // Since multiple threads can add to the cache, a few more values might be added.
        private const int NameCacheSizeSoftLimit = 64;

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
            Debug.Assert(names.Length == values.Length);

            JavaScriptEncoder? encoder = serializerOptions.Encoder;

            for (int i = 0; i < names.Length; i++)
            {
                if (_nameCache.Count >= NameCacheSizeSoftLimit)
                {
                    break;
                }

                T value = (T)values.GetValue(i)!;
                ulong key = ConvertToUInt64(value);
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

                return ReadAsPropertyName(ref reader, typeToConvert, options);
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
                case TypeCode.SByte:
                    if (reader.TryGetSByte(out sbyte byte8))
                    {
                        return Unsafe.As<sbyte, T>(ref byte8);
                    }
                    break;
                case TypeCode.Byte:
                    if (reader.TryGetByte(out byte ubyte8))
                    {
                        return Unsafe.As<byte, T>(ref ubyte8);
                    }
                    break;
                case TypeCode.Int16:
                    if (reader.TryGetInt16(out short int16))
                    {
                        return Unsafe.As<short, T>(ref int16);
                    }
                    break;
                case TypeCode.UInt16:
                    if (reader.TryGetUInt16(out ushort uint16))
                    {
                        return Unsafe.As<ushort, T>(ref uint16);
                    }
                    break;
            }

            ThrowHelper.ThrowJsonException();
            return default;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // If strings are allowed, attempt to write it out as a string value
            if (_converterOptions.HasFlag(EnumConverterOptions.AllowStrings))
            {
                ulong key = ConvertToUInt64(value);

                if (_nameCache.TryGetValue(key, out JsonEncodedText formatted))
                {
                    writer.WriteStringValue(formatted);
                    return;
                }

                string original = value.ToString();
                if (IsValidIdentifier(original))
                {
                    // We are dealing with a combination of flag constants since
                    // all constant values were cached during warm-up.
                    JavaScriptEncoder? encoder = options.Encoder;

                    if (_nameCache.Count < NameCacheSizeSoftLimit)
                    {
                        formatted = _namingPolicy == null
                            ? JsonEncodedText.Encode(original, encoder)
                            : FormatEnumValue(original, encoder);

                        writer.WriteStringValue(formatted);

                        _nameCache.TryAdd(key, formatted);
                    }
                    else
                    {
                        // We also do not create a JsonEncodedText instance here because passing the string
                        // directly to the writer is cheaper than creating one and not caching it for reuse.
                        writer.WriteStringValue(
                            _namingPolicy == null
                            ? original
                            : FormatEnumValueToString(original, encoder));
                    }

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

        // This method is adapted from Enum.ToUInt64 (an internal method):
        // https://github.com/dotnet/runtime/blob/bd6cbe3642f51d70839912a6a666e5de747ad581/src/libraries/System.Private.CoreLib/src/System/Enum.cs#L240-L260
        private static ulong ConvertToUInt64(object value)
        {
            Debug.Assert(value is T);
            ulong result = s_enumTypeCode switch
            {
                TypeCode.Int32 => (ulong)(int)value,
                TypeCode.UInt32 => (uint)value,
                TypeCode.UInt64 => (ulong)value,
                TypeCode.Int64 => (ulong)(long)value,
                TypeCode.SByte => (ulong)(sbyte)value,
                TypeCode.Byte => (byte)value,
                TypeCode.Int16 => (ulong)(short)value,
                TypeCode.UInt16 => (ushort)value,
                _ => throw new InvalidOperationException(),
            };
            return result;
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

        private JsonEncodedText FormatEnumValue(string value, JavaScriptEncoder? encoder)
        {
            Debug.Assert(_namingPolicy != null);
            string formatted = FormatEnumValueToString(value, encoder);
            return JsonEncodedText.Encode(formatted, encoder);
        }

        private string FormatEnumValueToString(string value, JavaScriptEncoder? encoder)
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

            return converted;
        }

        internal override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? enumString = reader.GetString();

            // Try parsing case sensitive first
            if (!Enum.TryParse(enumString, out T value)
                && !Enum.TryParse(enumString, ignoreCase: true, out value))
            {
                ThrowHelper.ThrowJsonException();
            }

            return value;
        }

        internal override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            // An EnumConverter that invokes this method
            // can only be created by JsonSerializerOptions.GetDictionaryKeyConverter
            // hence no naming policy is expected.
            Debug.Assert(_namingPolicy == null);

            ulong key = ConvertToUInt64(value);

            // Try to obtain values from caches
            if (options.DictionaryKeyPolicy != null)
            {
                Debug.Assert(!state.Current.IgnoreDictionaryKeyPolicy);

                if (_dictionaryKeyPolicyCache != null && _dictionaryKeyPolicyCache.TryGetValue(key, out JsonEncodedText formatted))
                {
                    writer.WritePropertyName(formatted);
                    return;
                }
            }
            else if (_nameCache.TryGetValue(key, out JsonEncodedText formatted))
            {
                writer.WritePropertyName(formatted);
                return;
            }


            // if there are not cached values
            string original = value.ToString();
            if (IsValidIdentifier(original))
            {
                if (options.DictionaryKeyPolicy != null)
                {
                    original = options.DictionaryKeyPolicy.ConvertName(original);

                    if (original == null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(options.DictionaryKeyPolicy);
                    }

                    _dictionaryKeyPolicyCache ??= new ConcurrentDictionary<ulong, JsonEncodedText>();

                    if (_dictionaryKeyPolicyCache.Count < NameCacheSizeSoftLimit)
                    {
                        JavaScriptEncoder? encoder = options.Encoder;

                        JsonEncodedText formatted = JsonEncodedText.Encode(original, encoder);

                        writer.WritePropertyName(formatted);

                        _dictionaryKeyPolicyCache.TryAdd(key, formatted);
                    }
                    else
                    {
                        // We also do not create a JsonEncodedText instance here because passing the string
                        // directly to the writer is cheaper than creating one and not caching it for reuse.
                        writer.WritePropertyName(original);
                    }

                    return;
                }
                else
                {
                    // We might be dealing with a combination of flag constants since all constant values were
                    // likely cached during warm - up(assuming the number of constants <= NameCacheSizeSoftLimit).

                    JavaScriptEncoder? encoder = options.Encoder;

                    if (_nameCache.Count < NameCacheSizeSoftLimit)
                    {
                        JsonEncodedText formatted = JsonEncodedText.Encode(original, encoder);

                        writer.WritePropertyName(formatted);

                        _nameCache.TryAdd(key, formatted);
                    }
                    else
                    {
                        // We also do not create a JsonEncodedText instance here because passing the string
                        // directly to the writer is cheaper than creating one and not caching it for reuse.
                        writer.WritePropertyName(original);
                    }

                    return;
                }
            }

            switch (s_enumTypeCode)
            {
                case TypeCode.Int32:
                    writer.WritePropertyName(Unsafe.As<T, int>(ref value));
                    break;
                case TypeCode.UInt32:
                    writer.WritePropertyName(Unsafe.As<T, uint>(ref value));
                    break;
                case TypeCode.UInt64:
                    writer.WritePropertyName(Unsafe.As<T, ulong>(ref value));
                    break;
                case TypeCode.Int64:
                    writer.WritePropertyName(Unsafe.As<T, long>(ref value));
                    break;
                case TypeCode.Int16:
                    writer.WritePropertyName(Unsafe.As<T, short>(ref value));
                    break;
                case TypeCode.UInt16:
                    writer.WritePropertyName(Unsafe.As<T, ushort>(ref value));
                    break;
                case TypeCode.Byte:
                    writer.WritePropertyName(Unsafe.As<T, byte>(ref value));
                    break;
                case TypeCode.SByte:
                    writer.WritePropertyName(Unsafe.As<T, sbyte>(ref value));
                    break;
                default:
                    ThrowHelper.ThrowJsonException();
                    break;
            }
        }
    }
}
