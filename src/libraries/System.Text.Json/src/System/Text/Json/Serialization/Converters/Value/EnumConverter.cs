// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class EnumConverter<T> : JsonPrimitiveConverter<T>
        where T : struct, Enum
    {
        private static readonly TypeCode s_enumTypeCode = Type.GetTypeCode(typeof(T));

        private static readonly char[] s_specialChars = new[] { ',', ' ' };

        // Odd type codes are conveniently signed types (for enum backing types).
        private static readonly bool s_isSignedEnum = ((int)s_enumTypeCode % 2) == 1;

        private const string ValueSeparator = ", ";

        private readonly EnumConverterOptions _converterOptions;

        private readonly JsonNamingPolicy? _namingPolicy;

        /// <summary>
        /// Holds a mapping from enum value to text that might be formatted with <see cref="_namingPolicy" />.
        /// <see cref="ulong"/> is as the key used rather than <typeparamref name="T"/> given measurements that
        /// show private memory savings when a single type is used https://github.com/dotnet/runtime/pull/36726#discussion_r428868336.
        /// </summary>
        private readonly ConcurrentDictionary<ulong, JsonEncodedText> _nameCacheForWriting;

        /// <summary>
        /// Holds a mapping from text that might be formatted with <see cref="_namingPolicy" /> to enum value.
        /// </summary>
        private readonly ConcurrentDictionary<string, T>? _nameCacheForReading;

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
            _nameCacheForWriting = new ConcurrentDictionary<ulong, JsonEncodedText>();

            if (namingPolicy != null)
            {
                _nameCacheForReading = new ConcurrentDictionary<string, T>();
            }

#if NETCOREAPP
            string[] names = Enum.GetNames<T>();
            T[] values = Enum.GetValues<T>();
#else
            string[] names = Enum.GetNames(TypeToConvert);
            Array values = Enum.GetValues(TypeToConvert);
#endif
            Debug.Assert(names.Length == values.Length);

            JavaScriptEncoder? encoder = serializerOptions.Encoder;

            for (int i = 0; i < names.Length; i++)
            {
#if NETCOREAPP
                T value = values[i];
#else
                T value = (T)values.GetValue(i)!;
#endif
                ulong key = ConvertToUInt64(value);
                string name = names[i];

                string jsonName = FormatJsonName(name, namingPolicy);
                _nameCacheForWriting.TryAdd(key, JsonEncodedText.Encode(jsonName, encoder));
                _nameCacheForReading?.TryAdd(jsonName, value);

                // If enum contains special char, make it failed to serialize or deserialize.
                if (name.IndexOfAny(s_specialChars) != -1)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidEnumTypeWithSpecialChar(typeof(T), name);
                }
            }
        }

#pragma warning disable 8500 // address of managed types
        public override unsafe T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonTokenType token = reader.TokenType;

            if (token == JsonTokenType.String)
            {
                if (!_converterOptions.HasFlag(EnumConverterOptions.AllowStrings))
                {
                    ThrowHelper.ThrowJsonException();
                    return default;
                }

#if NETCOREAPP
                if (TryParseEnumCore(ref reader, options, out T value))
#else
                string? enumString = reader.GetString();
                if (TryParseEnumCore(enumString, options, out T value))
#endif
                {
                    return value;
                }
#if NETCOREAPP
                return ReadEnumUsingNamingPolicy(reader.GetString());
#else
                return ReadEnumUsingNamingPolicy(enumString);
#endif
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
                        return *(T*)&int32;
                    }
                    break;
                case TypeCode.UInt32:
                    if (reader.TryGetUInt32(out uint uint32))
                    {
                        return *(T*)&uint32;
                    }
                    break;
                case TypeCode.UInt64:
                    if (reader.TryGetUInt64(out ulong uint64))
                    {
                        return *(T*)&uint64;
                    }
                    break;
                case TypeCode.Int64:
                    if (reader.TryGetInt64(out long int64))
                    {
                        return *(T*)&int64;
                    }
                    break;
                case TypeCode.SByte:
                    if (reader.TryGetSByte(out sbyte byte8))
                    {
                        return *(T*)&byte8;
                    }
                    break;
                case TypeCode.Byte:
                    if (reader.TryGetByte(out byte ubyte8))
                    {
                        return *(T*)&ubyte8;
                    }
                    break;
                case TypeCode.Int16:
                    if (reader.TryGetInt16(out short int16))
                    {
                        return *(T*)&int16;
                    }
                    break;
                case TypeCode.UInt16:
                    if (reader.TryGetUInt16(out ushort uint16))
                    {
                        return *(T*)&uint16;
                    }
                    break;
            }

            ThrowHelper.ThrowJsonException();
            return default;
        }

        public override unsafe void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // If strings are allowed, attempt to write it out as a string value
            if (_converterOptions.HasFlag(EnumConverterOptions.AllowStrings))
            {
                ulong key = ConvertToUInt64(value);

                if (_nameCacheForWriting.TryGetValue(key, out JsonEncodedText formatted))
                {
                    writer.WriteStringValue(formatted);
                    return;
                }

                string original = value.ToString();

                if (IsValidIdentifier(original))
                {
                    // We are dealing with a combination of flag constants since
                    // all constant values were cached during warm-up.
                    Debug.Assert(original.Contains(ValueSeparator));

                    original = FormatJsonName(original, _namingPolicy);

                    if (_nameCacheForWriting.Count < NameCacheSizeSoftLimit)
                    {
                        formatted = JsonEncodedText.Encode(original, options.Encoder);
                        writer.WriteStringValue(formatted);
                        _nameCacheForWriting.TryAdd(key, formatted);
                    }
                    else
                    {
                        // We also do not create a JsonEncodedText instance here because passing the string
                        // directly to the writer is cheaper than creating one and not caching it for reuse.
                        writer.WriteStringValue(original);
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
                    writer.WriteNumberValue(*(int*)&value);
                    break;
                case TypeCode.UInt32:
                    writer.WriteNumberValue(*(uint*)&value);
                    break;
                case TypeCode.UInt64:
                    writer.WriteNumberValue(*(ulong*)&value);
                    break;
                case TypeCode.Int64:
                    writer.WriteNumberValue(*(long*)&value);
                    break;
                case TypeCode.Int16:
                    writer.WriteNumberValue(*(short*)&value);
                    break;
                case TypeCode.UInt16:
                    writer.WriteNumberValue(*(ushort*)&value);
                    break;
                case TypeCode.Byte:
                    writer.WriteNumberValue(*(byte*)&value);
                    break;
                case TypeCode.SByte:
                    writer.WriteNumberValue(*(sbyte*)&value);
                    break;
                default:
                    ThrowHelper.ThrowJsonException();
                    break;
            }
        }
#pragma warning restore 8500

        internal override T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
#if NETCOREAPP
            bool success = TryParseEnumCore(ref reader, options, out T value);
#else
            bool success = TryParseEnumCore(reader.GetString(), options, out T value);
#endif

            if (!success)
            {
                ThrowHelper.ThrowJsonException();
            }

            return value;
        }

        internal override unsafe void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            ulong key = ConvertToUInt64(value);

            if (options.DictionaryKeyPolicy == null && _nameCacheForWriting.TryGetValue(key, out JsonEncodedText formatted))
            {
                writer.WritePropertyName(formatted);
                return;
            }

            string original = value.ToString();

            if (IsValidIdentifier(original))
            {
                if (options.DictionaryKeyPolicy != null)
                {
                    original = FormatJsonName(original, options.DictionaryKeyPolicy);
                    writer.WritePropertyName(original);
                    return;
                }

                original = FormatJsonName(original, _namingPolicy);

                if (_nameCacheForWriting.Count < NameCacheSizeSoftLimit)
                {
                    formatted = JsonEncodedText.Encode(original, options.Encoder);
                    writer.WritePropertyName(formatted);
                    _nameCacheForWriting.TryAdd(key, formatted);
                }
                else
                {
                    // We also do not create a JsonEncodedText instance here because passing the string
                    // directly to the writer is cheaper than creating one and not caching it for reuse.
                    writer.WritePropertyName(original);
                }

                return;
            }

#pragma warning disable 8500 // address of managed type
            switch (s_enumTypeCode)
            {
                case TypeCode.Int32:
                    writer.WritePropertyName(*(int*)&value);
                    break;
                case TypeCode.UInt32:
                    writer.WritePropertyName(*(uint*)&value);
                    break;
                case TypeCode.UInt64:
                    writer.WritePropertyName(*(ulong*)&value);
                    break;
                case TypeCode.Int64:
                    writer.WritePropertyName(*(long*)&value);
                    break;
                case TypeCode.Int16:
                    writer.WritePropertyName(*(short*)&value);
                    break;
                case TypeCode.UInt16:
                    writer.WritePropertyName(*(ushort*)&value);
                    break;
                case TypeCode.Byte:
                    writer.WritePropertyName(*(byte*)&value);
                    break;
                case TypeCode.SByte:
                    writer.WritePropertyName(*(sbyte*)&value);
                    break;
                default:
                    ThrowHelper.ThrowJsonException();
                    break;
            }
#pragma warning restore 8500
        }

#if NETCOREAPP
        private static bool TryParseEnumCore(ref Utf8JsonReader reader, JsonSerializerOptions options, out T value)
        {
            char[]? rentedBuffer = null;
            int bufferLength = reader.ValueLength;

            Span<char> charBuffer = bufferLength <= JsonConstants.StackallocCharThreshold
                ? stackalloc char[JsonConstants.StackallocCharThreshold]
                : (rentedBuffer = ArrayPool<char>.Shared.Rent(bufferLength));

            int charsWritten = reader.CopyString(charBuffer);
            ReadOnlySpan<char> source = charBuffer.Slice(0, charsWritten);

            // Try parsing case sensitive first
            bool success = Enum.TryParse(source, out T result) || Enum.TryParse(source, ignoreCase: true, out result);

            if (rentedBuffer != null)
            {
                charBuffer.Slice(0, charsWritten).Clear();
                ArrayPool<char>.Shared.Return(rentedBuffer);
            }

            value = result;
            return success;
        }
#else
        private static bool TryParseEnumCore(string? enumString, JsonSerializerOptions _, out T value)
        {
            // Try parsing case sensitive first
            bool success = Enum.TryParse(enumString, out T result) || Enum.TryParse(enumString, ignoreCase: true, out result);
            value = result;
            return success;
        }
#endif

        private T ReadEnumUsingNamingPolicy(string? enumString)
        {
            if (_namingPolicy == null)
            {
                ThrowHelper.ThrowJsonException();
            }

            if (enumString == null)
            {
                ThrowHelper.ThrowJsonException();
            }

            Debug.Assert(_nameCacheForReading != null, "Enum value cache should be instantiated if a naming policy is specified.");

            bool success;

            if (!(success = _nameCacheForReading.TryGetValue(enumString, out T value)) && enumString.Contains(ValueSeparator))
            {
                string[] enumValues = SplitFlagsEnum(enumString);
                ulong result = 0;

                for (int i = 0; i < enumValues.Length; i++)
                {
                    success = _nameCacheForReading.TryGetValue(enumValues[i], out value);
                    if (!success)
                    {
                        break;
                    }

                    result |= ConvertToUInt64(value);
                }

                value = (T)Enum.ToObject(typeof(T), result);

                if (success && _nameCacheForReading.Count < NameCacheSizeSoftLimit)
                {
                    _nameCacheForReading[enumString] = value;
                }
            }

            if (!success)
            {
                ThrowHelper.ThrowJsonException();
            }

            return value;
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
                (!s_isSignedEnum || !value.StartsWith(NumberFormatInfo.CurrentInfo.NegativeSign)));
        }

        private static string FormatJsonName(string value, JsonNamingPolicy? namingPolicy)
        {
            if (namingPolicy is null)
            {
                return value;
            }

            string converted;
            if (!value.Contains(ValueSeparator))
            {
                converted = namingPolicy.ConvertName(value);
            }
            else
            {
                string[] enumValues = SplitFlagsEnum(value);

                for (int i = 0; i < enumValues.Length; i++)
                {
                    string name = namingPolicy.ConvertName(enumValues[i]);
                    if (name == null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(namingPolicy);
                    }
                    enumValues[i] = name;
                }

                converted = string.Join(ValueSeparator, enumValues);
            }

            return converted;
        }

        private static string[] SplitFlagsEnum(string value)
        {
            // todo: optimize implementation here by leveraging https://github.com/dotnet/runtime/issues/934.
            return value.Split(
#if NETCOREAPP
                ValueSeparator
#else
                new string[] { ValueSeparator }, StringSplitOptions.None
#endif
                );
        }
    }
}
