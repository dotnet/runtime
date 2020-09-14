// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace System.Text.Json.Serialization.Samples
{
    /// <summary>
    /// This class is intended as a sample for supporting the <see langword="dynamic"/> feature.
    /// </summary>
    /// <remarks>
    /// It requires a reference to the "System.Linq.Expressions" assembly.
    /// </remarks>
    public static class JsonSerializerExtensions
    {
        /// <summary>
        /// Enable support for the <see langword="dynamic"/> feature.
        /// Changes the default handling for types specified as <see cref="object"/> from deserializing as
        /// <see cref="System.Text.Json.JsonElement"/> to instead deserializing as the one of the
        /// <see cref="JsonDynamicType"/>-derived types including:
        /// <see cref="JsonDynamicObject"/>,
        /// <see cref="JsonDynamicArray"/>,
        /// <see cref="JsonDynamicString"/>,
        /// <see cref="JsonDynamicNumber"/> and
        /// <see cref="JsonDynamicBoolean"/>.
        /// </summary>
        /// <remarks>
        /// When deserializing <see cref="System.Text.Json.JsonTokenType.StartObject"/>, <see cref="JsonDynamicObject"/>
        /// is returned which implements <see cref="System.Collections.IDictionary{string, object}"/>.
        /// When deserializing <see cref="System.Text.Json.JsonTokenType.StartArray"/>, <see cref="System.Collections.IList{object}"/>
        /// is returned which implements <see cref="System.Collections.IList{object}"/>.
        /// When deserializing <see cref="System.Text.Json.JsonTokenType.String"/>, <see cref="JsonDynamicString"/>
        /// is returned and supports an implicit cast to <see cref="string"/>.
        /// An explicit cast or assignment to other types, such as <see cref="System.Text.Json.JsonTokenType.DateTime"/>,
        /// is supported provided there is a custom converter for that Type.
        /// When deserializing <see cref="System.Text.Json.JsonTokenType.Number"/>, <see cref="JsonDynamicNumber"/> is returned.
        /// An explicit cast or assignment is required to the appropriate number type, such as <see cref="decimal"/> or <see cref="long"/>.
        /// When deserializing <see cref="System.Text.Json.JsonTokenType.True"/> and <see cref="System.Text.Json.JsonTokenType.False"/>,
        /// <see cref="JsonDynamicBool"/> is returned and supports an implicit cast to <see cref="bool"/>.
        /// An explicit cast or assignment to other types is supported provided there is a custom converter for that type.
        /// When deserializing <see cref="System.Text.Json.JsonTokenType.Null"/>, <see langword="null"/> is returned.
        /// </remarks>
        public static void EnableDynamicTypes(this JsonSerializerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Converters.Add(new DynamicObjectConverter());
        }

        /// <summary>
        /// The base class for all dynamic types supported by the serializer.
        /// </summary>
        public abstract class JsonDynamicType : DynamicObject
        {
            public JsonSerializerOptions Options { get; private set; }

            internal JsonDynamicType(JsonSerializerOptions options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                Options = options;
            }

            public sealed override bool TryConvert(ConvertBinder binder, out object result)
            {
                return TryConvert(binder.ReturnType, out result);
            }

            protected abstract bool TryConvert(Type returnType, out object result);

            internal abstract object Value { get; }
        }

        /// <summary>
        /// Supports dynamic strings.
        /// </summary>
        public sealed class JsonDynamicString : JsonDynamicType
        {
            private object _value;
            private Type _type;

            public JsonDynamicString(string value, JsonSerializerOptions options) : base(options)
            {
                _value = value;
                _type = typeof(string);
            }

            internal override object Value => _value;

            protected override bool TryConvert(Type returnType, out object result)
            {
                if (returnType == _type)
                {
                    result = _value; // Return cached value, such as a DateTime.
                    return true;
                }

                result = _value = JsonSerializer.Deserialize($"\"{_value}\"", returnType, Options);
                _type = result?.GetType();
                return true;
            }

            public static implicit operator string(JsonDynamicString obj)
            {
                // Assume any type that is deserialized as a JSON string can be converted to a bool.
                return (string)obj._value;
            }
        }

        /// <summary>
        /// Supports dynamic numbers.
        /// </summary>
        public sealed class JsonDynamicNumber : JsonDynamicType
        {
            private JsonElement _jsonElement;
            private Type _type = null;
            private object _value = null;

            public JsonDynamicNumber(in JsonElement jsonElement, JsonSerializerOptions options) : base(options)
            {
                _jsonElement = jsonElement;
            }

            internal override object Value => _jsonElement;

            protected override bool TryConvert(Type returnType, out object result)
            {
                if (returnType == _type)
                {
                    result = _value; // Return cached value, such as a long or double.
                    return true;
                }

                bool success = false;
                result = null;

                if (returnType == typeof(long))
                {
                    success = _jsonElement.TryGetInt64(out long value);
                    result = value;
                }
                else if (returnType == typeof(double))
                {
                    success = _jsonElement.TryGetDouble(out double value);
                    result = value;
                }
                else if (returnType == typeof(int))
                {
                    success = _jsonElement.TryGetInt32(out int value);
                    result = value;
                }
                else if (returnType == typeof(short))
                {
                    success = _jsonElement.TryGetInt16(out short value);
                    result = value;
                }
                else if (returnType == typeof(decimal))
                {
                    success = _jsonElement.TryGetDecimal(out decimal value);
                    result = value;
                }
                else if (returnType == typeof(byte))
                {
                    success = _jsonElement.TryGetByte(out byte value);
                    result = value;
                }
                else if (returnType == typeof(float))
                {
                    success = _jsonElement.TryGetSingle(out float value);
                    result = value;
                }
                else if (returnType == typeof(uint))
                {
                    success = _jsonElement.TryGetUInt32(out uint value);
                    result = value;
                }
                else if (returnType == typeof(ushort))
                {
                    success = _jsonElement.TryGetUInt16(out ushort value);
                    result = value;
                }
                else if (returnType == typeof(ulong))
                {
                    success = _jsonElement.TryGetUInt64(out ulong value);
                    result = value;
                }
                else if (returnType == typeof(sbyte))
                {
                    success = _jsonElement.TryGetSByte(out sbyte value);
                    result = value;
                }

                if (!success)
                {
                    // Use the raw test which may be recognized by converters such as the Enum converter than can process numbers.
                    string rawText = _jsonElement.GetRawText();
                    result = JsonSerializer.Deserialize($"{rawText}", returnType, Options);
                }

                _value = result;
                _type = result?.GetType();
                return true;
            }
        }

        /// <summary>
        /// Supports dynamic booleans.
        /// </summary>
        public sealed class JsonDynamicBoolean : JsonDynamicType
        {
            private object _value;
            private Type _type;

            public JsonDynamicBoolean(bool value, JsonSerializerOptions options) : base(options)
            {
                _value = value;
                _type = typeof(bool);
            }

            internal override object Value => _value;

            protected override bool TryConvert(Type returnType, out object result)
            {
                if (returnType == _type)
                {
                    result = _value; // Return cached value.
                    return true;
                }

                result = _value = JsonSerializer.Deserialize($"\"{_value}\"", returnType, Options);
                _type = result?.GetType();
                return true;
            }

            public static implicit operator bool(JsonDynamicBoolean obj)
            {
                // Assume any type that handles True and False tokens can be converted to a bool.
                return (bool)obj._value;
            }
        }

        /// <summary>
        /// Supports dynamic objects.
        /// </summary>
        public sealed class JsonDynamicObject : JsonDynamicType, IDictionary<string, object>
        {
            private IDictionary<string, object> _value;

            public JsonDynamicObject(IDictionary<string, object> value, JsonSerializerOptions options)
                : base(options)
            {
                _value = value;
            }

            internal override object Value => _value;

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                if (_value.TryGetValue(binder.Name, out result))
                {
                    JsonDynamicObject dynamicObj = result as JsonDynamicObject;
                    if (dynamicObj != null)
                    {
                        return dynamicObj.TryConvert(binder.ReturnType, out result);
                    }

                    return true;
                }

                // Return null for missing properties.
                result = null;
                return true;
            }

            public bool TryGetValue(string key, out object value) => _value.TryGetValue(key, out value);

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                _value[binder.Name] = value;
                return true;
            }

            protected override bool TryConvert(Type returnType, out object result)
            {
                if (returnType.IsAssignableFrom(typeof(IDictionary<string, object>)))
                {
                    result = _value;
                    return true;
                }

                result = null;
                return false;
            }

            // IDictionary members.
            public void Add(string key, object value) => _value.Add(key, value);
            public void Add(KeyValuePair<string, object> item) => _value.Add(item);
            public void Clear() => _value.Clear();
            public bool Contains(KeyValuePair<string, object> item) => _value.Contains(item);
            public bool ContainsKey(string key) => _value.ContainsKey(key);
            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _value.CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _value.GetEnumerator();
            public bool Remove(string key) => _value.Remove(key);
            public bool Remove(KeyValuePair<string, object> item) => _value.Remove(item);
            public object this[string key] { get => _value[key]; set => _value[key] = value; }
            public ICollection<string> Keys => _value.Keys;
            public ICollection<object> Values => _value.Values;
            public int Count => _value.Count;
            public bool IsReadOnly => _value.IsReadOnly;
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_value).GetEnumerator();
        }

        /// <summary>
        /// Supports dynamic arrays.
        /// </summary>
        public sealed class JsonDynamicArray : JsonDynamicType, IList<object>
        {
            private IList<object> _value;

            public JsonDynamicArray(IList<object> value, JsonSerializerOptions options) : base(options)
            {
                _value = value;
            }

            internal override object Value => _value;

            protected override bool TryConvert(Type returnType, out object result)
            {
                if (returnType.IsAssignableFrom(typeof(IList<object>)))
                {
                    result = _value;
                    return true;
                }

                result = null;
                return false;
            }

            // IList members.
            public object this[int index] { get => _value[index]; set => _value[index] = value; }
            public int Count => _value.Count;
            public bool IsReadOnly => _value.IsReadOnly;
            public void Add(object item) => _value.Add(item);
            public void Clear() => _value.Clear();
            public bool Contains(object item) => _value.Contains(item);
            public void CopyTo(object[] array, int arrayIndex) => _value.CopyTo(array, arrayIndex);
            public IEnumerator<object> GetEnumerator() => _value.GetEnumerator();
            public int IndexOf(object item) => _value.IndexOf(item);
            public void Insert(int index, object item) => _value.Insert(index, item);
            public bool Remove(object item) => _value.Remove(item);
            public void RemoveAt(int index) => _value.RemoveAt(index);
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_value).GetEnumerator();
        }

        /// <summary>
        /// Supports deserialization of all <see cref="object"/>-declared types, supporting <see langword="dynamic"/>.
        /// supports serialization of all <see cref="JsonDynamicType"/>-derived types.
        /// </summary>
        private sealed class DynamicObjectConverter : JsonConverter<object>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                // For simplicity in adding the converter, we use a single converter instead of two.
                return typeToConvert == typeof(object) ||
                    typeof(JsonDynamicType).IsAssignableFrom(typeToConvert);
            }

            // Instead of re-implementing these converters, forward to them.
            // We don't forward to other converters at this time.
            private JsonConverter<IList<object>> _listConverter;
            private JsonConverter<JsonElement> _jsonElementConverter;

            public override sealed object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        return new JsonDynamicString(reader.GetString(), options);
                    case JsonTokenType.StartArray:
                        _listConverter ??= (JsonConverter<IList<object>>)options.GetConverter(typeof(IList<object>));
                        IList<object> list = _listConverter.Read(ref reader, typeof(IList<object>), options);
                        return new JsonDynamicArray(list, options);
                    case JsonTokenType.StartObject:
                        IDictionary<string, object> properties = ReadObject(ref reader, options);
                        return new JsonDynamicObject(properties, options);
                    case JsonTokenType.False:
                        return new JsonDynamicBoolean(false, options);
                    case JsonTokenType.True:
                        return new JsonDynamicBoolean(true, options);
                    case JsonTokenType.Number:
                        _jsonElementConverter ??= (JsonConverter<JsonElement>)options.GetConverter(typeof(JsonElement));
                        JsonElement jsonElement = _jsonElementConverter.Read(ref reader, typeof(JsonElement), options);
                        return new JsonDynamicNumber(jsonElement, options);
                    case JsonTokenType.Null:
                        return null;
                    default:
                        throw new JsonException("Unexpected token type.");
                }
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                JsonDynamicType dynamicType = value as JsonDynamicType;
                if (dynamicType != null)
                {
                    value = dynamicType.Value;
                }

                JsonSerializer.Serialize<object>(writer, value, options);
            }

            private IDictionary<string, object> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                var _properties = new Dictionary<string, object>(options.PropertyNameCaseInsensitive ?
                    StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

                while (true)
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException();
                    }

                    string key = reader.GetString();

                    reader.Read();
                    object element = Read(ref reader, typeof(object), options);
                    _properties.Add(key, element);
                }

                return _properties;
            }
        }
    }
}
