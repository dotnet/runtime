// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Node
{
    [DebuggerDisplay("{ToJsonString(),nq}")]
    [DebuggerTypeProxy(typeof(JsonValue<>.DebugView))]
    internal sealed partial class JsonValue<TValue> : JsonValue
    {
        internal readonly TValue _value; // keep as a field for direct access to avoid copies

        public JsonValue(TValue value, JsonNodeOptions? options = null) : base(options)
        {
            Debug.Assert(value != null);
            Debug.Assert(!(value is JsonElement) || ((JsonElement)(object)value).ValueKind != JsonValueKind.Null);

            if (value is JsonNode)
            {
                ThrowHelper.ThrowArgumentException_NodeValueNotAllowed(nameof(value));
            }

            _value = value;
        }

        public TValue Value
        {
            get
            {
                return _value;
            }
        }

        public override T GetValue<[DynamicallyAccessedMembers(JsonHelpers.MembersAccessedOnRead)] T>()
        {
            // If no conversion is needed, just return the raw value.
            if (_value is T returnValue)
            {
                return returnValue;
            }

            if (_value is JsonElement)
            {
                return ConvertJsonElement<T>();
            }

            // Currently we do not support other conversions.
            // Generics (and also boxing) do not support standard cast operators say from 'long' to 'int',
            //  so attempting to cast here would throw InvalidCastException.
            throw new InvalidOperationException(SR.Format(SR.NodeUnableToConvert, _value!.GetType(), typeof(T)));
        }

        public override bool TryGetValue<T>([NotNullWhen(true)] out T value)
        {
            // If no conversion is needed, just return the raw value.
            if (_value is T returnValue)
            {
                value = returnValue;
                return true;
            }

            if (_value is JsonElement jsonElement)
            {
                return TryConvertJsonElement<T>(out value);
            }

            // Currently we do not support other conversions.
            // Generics (and also boxing) do not support standard cast operators say from 'long' to 'int',
            //  so attempting to cast here would throw InvalidCastException.
            value = default!;
            return false;
        }

        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_value is JsonElement jsonElement)
            {
                jsonElement.WriteTo(writer);
            }
            else
            {
                JsonSerializer.Serialize(writer, _value, _value!.GetType(), options);
            }
        }

        internal TypeToConvert ConvertJsonElement<TypeToConvert>()
        {
            JsonElement element = (JsonElement)(object)_value!;

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (typeof(TypeToConvert) == typeof(int) || typeof(TypeToConvert) == typeof(int?))
                    {
                        return (TypeToConvert)(object)element.GetInt32();
                    }

                    if (typeof(TypeToConvert) == typeof(long) || typeof(TypeToConvert) == typeof(long?))
                    {
                        return (TypeToConvert)(object)element.GetInt64();
                    }

                    if (typeof(TypeToConvert) == typeof(double) || typeof(TypeToConvert) == typeof(double?))
                    {
                        return (TypeToConvert)(object)element.GetDouble();
                    }

                    if (typeof(TypeToConvert) == typeof(short) || typeof(TypeToConvert) == typeof(short?))
                    {
                        return (TypeToConvert)(object)element.GetInt16();
                    }

                    if (typeof(TypeToConvert) == typeof(decimal) || typeof(TypeToConvert) == typeof(decimal?))
                    {
                        return (TypeToConvert)(object)element.GetDecimal();
                    }

                    if (typeof(TypeToConvert) == typeof(byte) || typeof(TypeToConvert) == typeof(byte?))
                    {
                        return (TypeToConvert)(object)element.GetByte();
                    }

                    if (typeof(TypeToConvert) == typeof(float) || typeof(TypeToConvert) == typeof(float?))
                    {
                        return (TypeToConvert)(object)element.GetSingle();
                    }

                    if (typeof(TypeToConvert) == typeof(uint) || typeof(TypeToConvert) == typeof(uint?))
                    {
                        return (TypeToConvert)(object)element.GetUInt32();
                    }

                    if (typeof(TypeToConvert) == typeof(ushort) || typeof(TypeToConvert) == typeof(ushort?))
                    {
                        return (TypeToConvert)(object)element.GetUInt16();
                    }

                    if (typeof(TypeToConvert) == typeof(ulong) || typeof(TypeToConvert) == typeof(ulong?))
                    {
                        return (TypeToConvert)(object)element.GetUInt64();
                    }

                    if (typeof(TypeToConvert) == typeof(sbyte) || typeof(TypeToConvert) == typeof(sbyte?))
                    {
                        return (TypeToConvert)(object)element.GetSByte();
                    }
                    break;

                case JsonValueKind.String:
                    if (typeof(TypeToConvert) == typeof(string))
                    {
                        return (TypeToConvert)(object)element.GetString()!;
                    }

                    if (typeof(TypeToConvert) == typeof(DateTime) || typeof(TypeToConvert) == typeof(DateTime?))
                    {
                        return (TypeToConvert)(object)element.GetDateTime();
                    }

                    if (typeof(TypeToConvert) == typeof(DateTimeOffset) || typeof(TypeToConvert) == typeof(DateTimeOffset?))
                    {
                        return (TypeToConvert)(object)element.GetDateTimeOffset();
                    }

                    if (typeof(TypeToConvert) == typeof(Guid) || typeof(TypeToConvert) == typeof(Guid?))
                    {
                        return (TypeToConvert)(object)element.GetGuid();
                    }

                    if (typeof(TypeToConvert) == typeof(char) || typeof(TypeToConvert) == typeof(char?))
                    {
                        string? str = element.GetString();
                        Debug.Assert(str != null);
                        if (str.Length == 1)
                        {
                            return (TypeToConvert)(object)str[0];
                        }
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (typeof(TypeToConvert) == typeof(bool) || typeof(TypeToConvert) == typeof(bool?))
                    {
                        return (TypeToConvert)(object)element.GetBoolean();
                    }
                    break;
            }

            throw new InvalidOperationException(SR.Format(SR.NodeUnableToConvertElement,
                element.ValueKind,
                typeof(TypeToConvert)));
        }

        internal bool TryConvertJsonElement<TypeToConvert>([NotNullWhen(true)] out TypeToConvert result)
        {
            bool success;

            JsonElement element = (JsonElement)(object)_value!;

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (typeof(TypeToConvert) == typeof(int) || typeof(TypeToConvert) == typeof(int?))
                    {
                        success = element.TryGetInt32(out int value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(long) || typeof(TypeToConvert) == typeof(long?))
                    {
                        success = element.TryGetInt64(out long value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(double) || typeof(TypeToConvert) == typeof(double?))
                    {
                        success = element.TryGetDouble(out double value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(short) || typeof(TypeToConvert) == typeof(short?))
                    {
                        success = element.TryGetInt16(out short value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(decimal) || typeof(TypeToConvert) == typeof(decimal?))
                    {
                        success = element.TryGetDecimal(out decimal value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(byte) || typeof(TypeToConvert) == typeof(byte?))
                    {
                        success = element.TryGetByte(out byte value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(float) || typeof(TypeToConvert) == typeof(float?))
                    {
                        success = element.TryGetSingle(out float value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(uint) || typeof(TypeToConvert) == typeof(uint?))
                    {
                        success = element.TryGetUInt32(out uint value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(ushort) || typeof(TypeToConvert) == typeof(ushort?))
                    {
                        success = element.TryGetUInt16(out ushort value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(ulong) || typeof(TypeToConvert) == typeof(ulong?))
                    {
                        success = element.TryGetUInt64(out ulong value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(sbyte) || typeof(TypeToConvert) == typeof(sbyte?))
                    {
                        success = element.TryGetSByte(out sbyte value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }
                    break;

                case JsonValueKind.String:
                    if (typeof(TypeToConvert) == typeof(string))
                    {
                        string? strResult = element.GetString();
                        Debug.Assert(strResult != null);
                        result = (TypeToConvert)(object)strResult;
                        return true;
                    }

                    if (typeof(TypeToConvert) == typeof(DateTime) || typeof(TypeToConvert) == typeof(DateTime?))
                    {
                        success = element.TryGetDateTime(out DateTime value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(DateTimeOffset) || typeof(TypeToConvert) == typeof(DateTimeOffset?))
                    {
                        success = element.TryGetDateTimeOffset(out DateTimeOffset value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(Guid) || typeof(TypeToConvert) == typeof(Guid?))
                    {
                        success = element.TryGetGuid(out Guid value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (typeof(TypeToConvert) == typeof(char) || typeof(TypeToConvert) == typeof(char?))
                    {
                        string? str = element.GetString();
                        Debug.Assert(str != null);
                        if (str.Length == 1)
                        {
                            result = (TypeToConvert)(object)str[0];
                            return true;
                        }
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (typeof(TypeToConvert) == typeof(bool) || typeof(TypeToConvert) == typeof(bool?))
                    {
                        result = (TypeToConvert)(object)element.GetBoolean();
                        return true;
                    }
                    break;
            }

            result = default!;
            return false;
        }

        [ExcludeFromCodeCoverage] // Justification = "Design-time"
        [DebuggerDisplay("{Json,nq}")]
        private class DebugView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public JsonValue<TValue> _node;

            public DebugView(JsonValue<TValue> node)
            {
                _node = node;
            }

            public string Json => _node.ToJsonString();
            public string Path => _node.GetPath();
            public TValue? Value => _node.Value;
        }
    }
}
