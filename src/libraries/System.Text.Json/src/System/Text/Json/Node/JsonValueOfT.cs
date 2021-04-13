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

        public override T GetValue<[DynamicallyAccessedMembers(MembersAccessedOnRead)] T>()
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

        public override bool TryGetValue<[DynamicallyAccessedMembers(MembersAccessedOnRead)] T>([NotNullWhen(true)] out T value)
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:ystem.Text.Json.Node.JsonValue<TValue>.WriteTo(Utf8JsonWriter,JsonSerializerOptions): 'inputType' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicFields', 'DynamicallyAccessedMemberTypes.PublicProperties' in call to 'System.Text.Json.JsonSerializer.Serialize(Utf8JsonWriter,Object,Type,JsonSerializerOptions)'. The return value of method 'System.Object.GetType()' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.",
            Justification = "The 'inputType' parameter if obtained by calling System.Object.GetType().")]
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
            Type returnType = typeof(TypeToConvert);
            Type? underlyingType = Nullable.GetUnderlyingType(returnType);
            returnType = underlyingType ?? returnType;

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (returnType == typeof(int))
                    {
                        return (TypeToConvert)(object)element.GetInt32();
                    }

                    if (returnType == typeof(long))
                    {
                        return (TypeToConvert)(object)element.GetInt64();
                    }

                    if (returnType == typeof(double))
                    {
                        return (TypeToConvert)(object)element.GetDouble();
                    }

                    if (returnType == typeof(short))
                    {
                        return (TypeToConvert)(object)element.GetInt16();
                    }

                    if (returnType == typeof(decimal))
                    {
                        return (TypeToConvert)(object)element.GetDecimal();
                    }

                    if (returnType == typeof(byte))
                    {
                        return (TypeToConvert)(object)element.GetByte();
                    }

                    if (returnType == typeof(float))
                    {
                        return (TypeToConvert)(object)element.GetSingle();
                    }

                    else if (returnType == typeof(uint))
                    {
                        return (TypeToConvert)(object)element.GetUInt32();
                    }

                    if (returnType == typeof(ushort))
                    {
                        return (TypeToConvert)(object)element.GetUInt16();
                    }

                    if (returnType == typeof(ulong))
                    {
                        return (TypeToConvert)(object)element.GetUInt64();
                    }

                    if (returnType == typeof(sbyte))
                    {
                        return (TypeToConvert)(object)element.GetSByte();
                    }
                    break;

                case JsonValueKind.String:
                    if (returnType == typeof(string))
                    {
                        return (TypeToConvert)(object)element.GetString()!;
                    }

                    if (returnType == typeof(DateTime))
                    {
                        return (TypeToConvert)(object)element.GetDateTime();
                    }

                    if (returnType == typeof(DateTimeOffset))
                    {
                        return (TypeToConvert)(object)element.GetDateTimeOffset();
                    }

                    if (returnType == typeof(Guid))
                    {
                        return (TypeToConvert)(object)element.GetGuid();
                    }

                    if (returnType == typeof(char))
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
                    if (returnType == typeof(bool))
                    {
                        return (TypeToConvert)(object)element.GetBoolean();
                    }
                    break;
            }

            throw new InvalidOperationException(SR.Format(SR.NodeUnableToConvertElement,
                element.ValueKind,
                 typeof(TypeToConvert))
            );
        }

        internal bool TryConvertJsonElement<TypeToConvert>([NotNullWhen(true)] out TypeToConvert result)
        {
            bool success;

            JsonElement element = (JsonElement)(object)_value!;
            Type returnType = typeof(TypeToConvert);
            Type? underlyingType = Nullable.GetUnderlyingType(returnType);
            returnType = underlyingType ?? returnType;

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (returnType == typeof(int))
                    {
                        success = element.TryGetInt32(out int value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(long))
                    {
                        success = element.TryGetInt64(out long value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(double))
                    {
                        success = element.TryGetDouble(out double value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(short))
                    {
                        success = element.TryGetInt16(out short value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(decimal))
                    {
                        success = element.TryGetDecimal(out decimal value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(byte))
                    {
                        success = element.TryGetByte(out byte value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(float))
                    {
                        success = element.TryGetSingle(out float value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    else if (returnType == typeof(uint))
                    {
                        success = element.TryGetUInt32(out uint value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(ushort))
                    {
                        success = element.TryGetUInt16(out ushort value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(ulong))
                    {
                        success = element.TryGetUInt64(out ulong value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(sbyte))
                    {
                        success = element.TryGetSByte(out sbyte value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }
                    break;

                case JsonValueKind.String:
                    if (returnType == typeof(string))
                    {
                        string? strResult = element.GetString();
                        Debug.Assert(strResult != null);
                        result = (TypeToConvert)(object)strResult;
                        return true;
                    }

                    if (returnType == typeof(DateTime))
                    {
                        success = element.TryGetDateTime(out DateTime value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(DateTimeOffset))
                    {
                        success = element.TryGetDateTimeOffset(out DateTimeOffset value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(Guid))
                    {
                        success = element.TryGetGuid(out Guid value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(char))
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
                    if (returnType == typeof(bool))
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
