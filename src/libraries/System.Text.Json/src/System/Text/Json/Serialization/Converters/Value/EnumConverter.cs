// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class EnumConverter<T> : JsonPrimitiveConverter<T>
        where T : struct, Enum
    {
        private static readonly TypeCode s_enumTypeCode = Type.GetTypeCode(typeof(T));
        private static readonly bool s_isFlagsEnum = typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false);

        private readonly EnumConverterOptions _converterOptions;

        private readonly JsonNamingPolicy? _namingPolicy;

        /// <summary>
        /// Whether either of the enum fields have been overridden with <see cref="JsonStringEnumMemberNameAttribute"/>.
        /// </summary>
        private readonly bool _containsNameAttributes;

        /// <summary>
        /// Stores metadata for the individual fields declared on the enum.
        /// </summary>
        private readonly EnumFieldInfo[] _enumFieldInfo;

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

        public EnumConverter(EnumConverterOptions converterOptions, JsonNamingPolicy? namingPolicy, JsonSerializerOptions options)
        {
            Debug.Assert(EnumConverterFactory.IsSupportedTypeCode(s_enumTypeCode));

            _converterOptions = converterOptions;
            _namingPolicy = namingPolicy;
            _enumFieldInfo = ResolveEnumFields(namingPolicy, out _containsNameAttributes);

            _nameCacheForWriting = new();
            if (namingPolicy != null || _containsNameAttributes)
            {
                // We can't rely on the built-in enum parser since custom names are used.
                _nameCacheForReading = new(StringComparer.Ordinal);
            }

            JavaScriptEncoder? encoder = options.Encoder;
            foreach (EnumFieldInfo fieldInfo in _enumFieldInfo)
            {
                JsonEncodedText encodedName = JsonEncodedText.Encode(fieldInfo.JsonName, encoder);
                _nameCacheForWriting.TryAdd(fieldInfo.Key, encodedName);
                _nameCacheForReading?.TryAdd(fieldInfo.JsonName, fieldInfo.Value);
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonTokenType token = reader.TokenType;

            if (token is JsonTokenType.String &&
                (_converterOptions & EnumConverterOptions.AllowStrings) != 0 &&
                TryParseEnumFromString(ref reader, out T value))
            {
                return value;
            }

            if (token != JsonTokenType.Number || (_converterOptions & EnumConverterOptions.AllowNumbers) == 0)
            {
                ThrowHelper.ThrowJsonException();
            }

            switch (s_enumTypeCode)
            {
                // Switch cases ordered by expected frequency

                case TypeCode.Int32:
                    if (reader.TryGetInt32(out int int32))
                    {
                        // Use Unsafe.As instead of raw pointers for .NET Standard support.
                        // https://github.com/dotnet/runtime/issues/84895
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
            if ((_converterOptions & EnumConverterOptions.AllowStrings) != 0)
            {
                ulong key = ConvertToUInt64(value);

                if (_nameCacheForWriting.TryGetValue(key, out JsonEncodedText formatted))
                {
                    writer.WriteStringValue(formatted);
                    return;
                }

                if (TryFormatEnumAsString(key, value, dictionaryKeyPolicy: null, out string? stringValue))
                {
                    if (_nameCacheForWriting.Count < NameCacheSizeSoftLimit)
                    {
                        formatted = JsonEncodedText.Encode(stringValue, options.Encoder);
                        writer.WriteStringValue(formatted);
                        _nameCacheForWriting.TryAdd(key, formatted);
                    }
                    else
                    {
                        // We also do not create a JsonEncodedText instance here because passing the string
                        // directly to the writer is cheaper than creating one and not caching it for reuse.
                        writer.WriteStringValue(stringValue);
                    }

                    return;
                }
            }

            if ((_converterOptions & EnumConverterOptions.AllowNumbers) == 0)
            {
                ThrowHelper.ThrowJsonException();
            }

            switch (s_enumTypeCode)
            {
                case TypeCode.Int32:
                    // Use Unsafe.As instead of raw pointers for .NET Standard support.
                    // https://github.com/dotnet/runtime/issues/84895
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
                    Debug.Fail("Should not be reached");
                    break;
            }
        }

        internal override T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // NB JsonSerializerOptions.DictionaryKeyPolicy is ignored on deserialization.
            // This is true for all converters that implement dictionary key serialization.

            if (!TryParseEnumFromString(ref reader, out T value))
            {
                ThrowHelper.ThrowJsonException();
            }

            return value;
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            JsonNamingPolicy? dictionaryKeyPolicy = options.DictionaryKeyPolicy is { } dkp && dkp != _namingPolicy ? dkp : null;
            ulong key = ConvertToUInt64(value);

            if (dictionaryKeyPolicy is null && _nameCacheForWriting.TryGetValue(key, out JsonEncodedText formatted))
            {
                writer.WritePropertyName(formatted);
                return;
            }

            if (TryFormatEnumAsString(key, value, dictionaryKeyPolicy, out string? stringEnum))
            {
                if (dictionaryKeyPolicy is null && _nameCacheForWriting.Count < NameCacheSizeSoftLimit)
                {
                    // Only attempt to cache if there is no dictionary key policy.
                    formatted = JsonEncodedText.Encode(stringEnum, options.Encoder);
                    writer.WritePropertyName(formatted);
                    _nameCacheForWriting.TryAdd(key, formatted);
                }
                else
                {
                    // We also do not create a JsonEncodedText instance here because passing the string
                    // directly to the writer is cheaper than creating one and not caching it for reuse.
                    writer.WritePropertyName(stringEnum);
                }

                return;
            }

            switch (s_enumTypeCode)
            {
                // Use Unsafe.As instead of raw pointers for .NET Standard support.
                // https://github.com/dotnet/runtime/issues/84895

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
                    Debug.Fail("Should not be reached");
                    break;
            }
        }

        private bool TryParseEnumFromString(ref Utf8JsonReader reader, out T value)
        {
            bool success;
#if NET
            char[]? rentedBuffer = null;
            int bufferLength = reader.ValueLength;

            Span<char> charBuffer = bufferLength <= JsonConstants.StackallocCharThreshold
                ? stackalloc char[JsonConstants.StackallocCharThreshold]
                : (rentedBuffer = ArrayPool<char>.Shared.Rent(bufferLength));

            int charsWritten = reader.CopyString(charBuffer);
            Span<char> source = charBuffer.Slice(0, charsWritten);
#else
            string source = reader.GetString();
#endif
            // Skip the built-in enum parser and go directly to the read cache if either:
            //
            // 1. one of the enum fields have had their names overridden OR
            // 2. the source string represents a number when numbers are not permitted.
            //
            // For backward compatibility reasons the built-in parser is not skipped if a naming policy is specified.
            bool skipEnumParser = _containsNameAttributes ||
                ((_converterOptions & EnumConverterOptions.AllowNumbers) is 0 && JsonHelpers.IntegerRegex.IsMatch(source));

            if (!skipEnumParser && Enum.TryParse(source, ignoreCase: true, out value))
            {
                success = true;
                goto End;
            }

            Debug.Assert(_nameCacheForReading is null == (_namingPolicy is null && !_containsNameAttributes),
                         "A read cache should only be populated if we have a naming policy or name attributes.");

            if (_nameCacheForReading is null)
            {
                value = default;
                success = false;
                goto End;
            }

            success = TryParseCommaSeparatedEnumValues(source, out value);

        End:
#if NET
            if (rentedBuffer != null)
            {
                source.Clear();
                ArrayPool<char>.Shared.Return(rentedBuffer);
            }
#endif
            return success;
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling numberHandling)
        {
            if ((_converterOptions & EnumConverterOptions.AllowStrings) != 0)
            {
                // This explicitly ignores the integer component in converters configured as AllowNumbers | AllowStrings
                // which is the default for JsonStringEnumConverter. This sacrifices some precision in the schema for simplicity.

                if (s_isFlagsEnum)
                {
                    // Do not report enum values in case of flags.
                    return new() { Type = JsonSchemaType.String };
                }

                JsonArray enumValues = [];
                foreach (EnumFieldInfo fieldInfo in _enumFieldInfo)
                {
                    enumValues.Add((JsonNode)fieldInfo.JsonName);
                }

                return new() { Enum = enumValues };
            }

            return new() { Type = JsonSchemaType.Integer };
        }

        private bool TryParseCommaSeparatedEnumValues(
#if NET
            ReadOnlySpan<char> source,
#else
            string sourceString,
#endif
            out T value)
        {
            Debug.Assert(_nameCacheForReading != null);
            ConcurrentDictionary<string, T> nameCacheForReading = _nameCacheForReading;
#if NET9_0_OR_GREATER
            ConcurrentDictionary<string, T>.AlternateLookup<ReadOnlySpan<char>> alternateLookup = nameCacheForReading.GetAlternateLookup<ReadOnlySpan<char>>();
            if (alternateLookup.TryGetValue(source, out value))
            {
                return true;
            }

            ReadOnlySpan<char> rest = source;
#elif NET
            string sourceString = source.ToString();
            if (nameCacheForReading.TryGetValue(sourceString, out value))
            {
                return true;
            }

            ReadOnlySpan<char> rest = source;
#else
            if (nameCacheForReading.TryGetValue(sourceString, out value))
            {
                return true;
            }

            ReadOnlySpan<char> rest = sourceString.AsSpan();
#endif
            ulong result = 0;

            do
            {
                ReadOnlySpan<char> next;
                int i = rest.IndexOf(',');
                if (i == -1)
                {
                    next = rest;
                    rest = default;
                }
                else
                {
                    next = rest.Slice(0, i);
                    rest = rest.Slice(i + 1);
                }

                next = next.Trim(' ');

#if NET9_0_OR_GREATER
                if (!alternateLookup.TryGetValue(next, out value))
#else
                if (!nameCacheForReading.TryGetValue(next.ToString(), out value))
#endif
                {
                    return false;
                }

                result |= ConvertToUInt64(value);

            } while (!rest.IsEmpty);

            value = ConvertFromUInt64(result);

            if (nameCacheForReading.Count < NameCacheSizeSoftLimit)
            {
#if NET9_0_OR_GREATER
                alternateLookup[source] = value;
#else
                nameCacheForReading[sourceString] = value;
#endif
            }

            return true;
        }

        private static ulong ConvertToUInt64(T value)
        {
            switch (s_enumTypeCode)
            {
                case TypeCode.Int32 or TypeCode.UInt32: return Unsafe.As<T, uint>(ref value);
                case TypeCode.Int64 or TypeCode.UInt64: return Unsafe.As<T, ulong>(ref value);
                case TypeCode.Int16 or TypeCode.UInt16: return Unsafe.As<T, ushort>(ref value);
                default:
                    Debug.Assert(s_enumTypeCode is TypeCode.SByte or TypeCode.Byte);
                    return Unsafe.As<T, byte>(ref value);
            };
        }

        private static T ConvertFromUInt64(ulong value)
        {
            switch (s_enumTypeCode)
            {
                case TypeCode.Int32 or TypeCode.UInt32:
                    uint uintValue = (uint)value;
                    return Unsafe.As<uint, T>(ref uintValue);

                case TypeCode.Int64 or TypeCode.UInt64:
                    ulong ulongValue = value;
                    return Unsafe.As<ulong, T>(ref ulongValue);

                case TypeCode.Int16 or TypeCode.UInt16:
                    ushort ushortValue = (ushort)value;
                    return Unsafe.As<ushort, T>(ref ushortValue);

                default:
                    Debug.Assert(s_enumTypeCode is TypeCode.SByte or TypeCode.Byte);
                    byte byteValue = (byte)value;
                    return Unsafe.As<byte, T>(ref byteValue);
            };
        }

        /// <summary>
        /// Attempt to format the enum value as a comma-separated string of flag values, or returns false if not a valid flag combination.
        /// </summary>
        private bool TryFormatEnumAsString(ulong key, T value, JsonNamingPolicy? dictionaryKeyPolicy, [NotNullWhen(true)] out string? stringValue)
        {
            Debug.Assert(!Enum.IsDefined(typeof(T), value) || dictionaryKeyPolicy != null, "Must either be used on undefined values or with a key policy.");

            if (s_isFlagsEnum)
            {
                using ValueStringBuilder sb = new(stackalloc char[JsonConstants.StackallocCharThreshold]);
                ulong remainingBits = key;

                foreach (EnumFieldInfo enumField in _enumFieldInfo)
                {
                    ulong fieldKey = enumField.Key;
                    if (fieldKey == 0 ? key == 0 : (remainingBits & fieldKey) == fieldKey)
                    {
                        string name = dictionaryKeyPolicy is not null
                            ? ResolveAndValidateJsonName(enumField.Name, dictionaryKeyPolicy, enumField.IsNameFromAttribute)
                            : enumField.JsonName;

                        if (sb.Length > 0)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(name);
                        remainingBits &= ~fieldKey;

                        if (fieldKey == 0)
                        {
                            // Do not process further fields if the value equals zero.
                            Debug.Assert(key == 0);
                            break;
                        }
                    }
                }

                if (remainingBits == 0 && sb.Length > 0)
                {
                    // The value is a valid combination of flags.
                    stringValue = sb.ToString();
                    return true;
                }
            }
            else if (dictionaryKeyPolicy != null)
            {
                foreach (EnumFieldInfo enumField in _enumFieldInfo)
                {
                    // Search for an exact match and apply the key policy.
                    if (enumField.Key == key)
                    {
                        stringValue = ResolveAndValidateJsonName(enumField.Name, dictionaryKeyPolicy, enumField.IsNameFromAttribute);
                        return true;
                    }
                }
            }

            stringValue = null;
            return false;
        }

        private static EnumFieldInfo[] ResolveEnumFields(JsonNamingPolicy? namingPolicy, out bool containsNameAttributes)
        {
            containsNameAttributes = false;
#if NET
            string[] names = Enum.GetNames<T>();
            T[] values = Enum.GetValues<T>();
#else
            string[] names = Enum.GetNames(typeof(T));
            T[] values = (T[])Enum.GetValues(typeof(T));
#endif
            Debug.Assert(names.Length == values.Length);

            Dictionary<string, string>? enumMemberAttributes = null;
            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>() is { } attribute)
                {
                    (enumMemberAttributes ??= new(StringComparer.Ordinal)).Add(field.Name, attribute.Name);
                }
            }

            var enumFields = new EnumFieldInfo[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                T value = values[i];
                ulong key = ConvertToUInt64(value);
                bool isNameFromAttribute = false;

                if (enumMemberAttributes != null && enumMemberAttributes.TryGetValue(name, out string? attributeName))
                {
                    name = attributeName;
                    containsNameAttributes = isNameFromAttribute = true;
                }

                string jsonName = ResolveAndValidateJsonName(name, namingPolicy, isNameFromAttribute);
                enumFields[i] = new EnumFieldInfo(key, value, name, jsonName, isNameFromAttribute);
            }

            return enumFields;
        }

        private static string ResolveAndValidateJsonName(string name, JsonNamingPolicy? namingPolicy, bool isNameFromAttribute)
        {
            if (!isNameFromAttribute && namingPolicy is not null)
            {
                // Do not apply a naming policy to names that are explicitly set via attributes.
                // This is consistent with JsonPropertyNameAttribute semantics.
                name = namingPolicy.ConvertName(name);
            }

            if (name is null || (s_isFlagsEnum && (name is "" || name.AsSpan().IndexOfAny(' ', ',') >= 0)))
            {
                // Reject null strings and in the case of flags additionally reject empty strings or names containing spaces or commas.
                ThrowHelper.ThrowInvalidOperationException_UnsupportedEnumIdentifier(typeof(T), name);
            }

            return name;
        }

        private sealed class EnumFieldInfo(ulong key, T value, string name, string jsonName, bool isNameFromAttribute)
        {
            public ulong Key { get; } = key;
            public T Value { get; } = value;
            public string Name { get; } = name;
            public string JsonName { get; } = jsonName;
            public bool IsNameFromAttribute { get; } = isNameFromAttribute;
        }
    }
}
