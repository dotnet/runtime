// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

            public abstract T GetValue<T>();
            public abstract void SetValue(object value);
            protected abstract bool TryConvert(Type returnType, out object result);

            protected static bool TryConvertWithTypeConverter(object value, Type returnType, out object result)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(value.GetType());
                if (converter.CanConvertTo(returnType))
                {
                    result = converter.ConvertTo(value, returnType);
                    return true;
                }

                converter = TypeDescriptor.GetConverter(returnType);
                if (converter.CanConvertFrom(value.GetType()))
                {
                    result = converter.ConvertFrom(value);
                    return true;
                }

                result = null;
                return false;
            }

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

            public override T GetValue<T>()
            {
                bool success = TryConvert(typeof(T), out object result);
                Debug.Assert(success);
                return (T)result;
            }

            public override void SetValue(object value)
            {
                _value = value;
                _type = value?.GetType();
            }

            protected override bool TryConvert(Type returnType, out object result)
            {
                if (returnType == _type)
                {
                    result = _value; // Return cached value, such as a DateTime.
                    return true;
                }

                if (TryConvertWithTypeConverter(_value, returnType, out result))
                {
                    return true;
                }

                result = _value = JsonSerializer.Deserialize($"\"{_value}\"", returnType, Options);
                _type = result?.GetType();
                return true;
            }

            internal override object Value => _value;

            public static implicit operator string(JsonDynamicString obj)
            {
                bool success = obj.TryConvert(typeof(string), out object result);
                Debug.Assert(success);
                return (string)result;
            }
        }

        /// <summary>
        /// Supports dynamic numbers.
        /// </summary>
        public sealed class JsonDynamicNumber : JsonDynamicType
        {
            private Type _type = null;
            private object _value = null;
            private object _lastValue = null;

            public JsonDynamicNumber(object value, JsonSerializerOptions options) : base(options)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _value = value;
            }

            public override T GetValue<T>()
            {
                if (TryConvert(typeof(T), out object result))
                {
                    return (T)result;
                }

                throw new InvalidOperationException($"Cannot change type {_value.GetType()} to {typeof(T)}.");
            }

            public override void SetValue(object value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _value = _lastValue = value;
                _type = value.GetType();
            }

            protected override bool TryConvert(Type returnType, out object result)
            {
                if (returnType == _type)
                {
                    result = _lastValue; // Return cached value, such as a long or double.
                    return true;
                }

                bool success = false;
                result = null;

                if (!(_value is JsonElement jsonElement))
                {
                    return TryConvertWithTypeConverter(_value, returnType, out result);
                }

                if (returnType == typeof(long))
                {
                    success = jsonElement.TryGetInt64(out long value);
                    result = value;
                }
                else if (returnType == typeof(double))
                {
                    success = jsonElement.TryGetDouble(out double value);
                    result = value;
                }
                else if (returnType == typeof(int))
                {
                    success = jsonElement.TryGetInt32(out int value);
                    result = value;
                }
                else if (returnType == typeof(short))
                {
                    success = jsonElement.TryGetInt16(out short value);
                    result = value;
                }
                else if (returnType == typeof(decimal))
                {
                    success = jsonElement.TryGetDecimal(out decimal value);
                    result = value;
                }
                else if (returnType == typeof(byte))
                {
                    success = jsonElement.TryGetByte(out byte value);
                    result = value;
                }
                else if (returnType == typeof(float))
                {
                    success = jsonElement.TryGetSingle(out float value);
                    result = value;
                }
                else if (returnType == typeof(uint))
                {
                    success = jsonElement.TryGetUInt32(out uint value);
                    result = value;
                }
                else if (returnType == typeof(ushort))
                {
                    success = jsonElement.TryGetUInt16(out ushort value);
                    result = value;
                }
                else if (returnType == typeof(ulong))
                {
                    success = jsonElement.TryGetUInt64(out ulong value);
                    result = value;
                }
                else if (returnType == typeof(sbyte))
                {
                    success = jsonElement.TryGetSByte(out sbyte value);
                    result = value;
                }

                if (!success)
                {
                    // Use the raw test which may be recognized by converters such as the Enum converter than can process numbers.
                    string rawText = jsonElement.GetRawText();
                    result = JsonSerializer.Deserialize($"{rawText}", returnType, Options);
                }

                _lastValue = result;
                _type = result?.GetType();
                return true;
            }

            internal override object Value => _value;
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

            public override T GetValue<T>()
            {
                bool success = TryConvert(typeof(T), out object result);
                Debug.Assert(success);
                return (T)result;
            }

            public override void SetValue(object value)
            {
                _value = value;
                _type = value?.GetType();
            }

            protected override bool TryConvert(Type returnType, out object result)
            {
                if (returnType == _value.GetType())
                {
                    result = _value; // Return cached value.
                    return true;
                }

                if (TryConvertWithTypeConverter(_value, returnType, out result))
                {
                    return true;
                }

                result = _value = JsonSerializer.Deserialize($"\"{Value}\"", returnType, Options);
                _type = result?.GetType();
                return true;
            }

            internal override object Value => _value;

            public static implicit operator bool(JsonDynamicBoolean obj)
            {
                bool success = obj.TryConvert(typeof(bool), out object result);
                Debug.Assert(success);
                return (bool)result;
            }
        }

        /// <summary>
        /// Supports dynamic objects.
        /// </summary>
        public sealed class JsonDynamicObject : JsonDynamicType, IDictionary<string, object>
        {
            private IDictionary<string, object> _value;

            public JsonDynamicObject(JsonSerializerOptions options)
                : base(options)
            {
                _value = new Dictionary<string, object>(options.PropertyNameCaseInsensitive ?
                    StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            }

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

            protected override bool TryConvert(Type returnType, out object result)
            {
                if (returnType.IsAssignableFrom(typeof(IDictionary<string, object>)))
                {
                    result = this;
                    return true;
                }

                result = null;
                return false;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                _value[binder.Name] = value;
                return true;
            }

            internal override object Value => _value;

            public override T GetValue<T>() => throw new NotSupportedException();
            public override void SetValue(object value) => throw new NotSupportedException();

            // IDictionary members.
            public void Add(string key, object value) => _value.Add(key, value);
            void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => _value.Add(item);
            public void Clear() => _value.Clear();
            bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => _value.Contains(item);
            public bool ContainsKey(string key) => _value.ContainsKey(key);
            void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _value.CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _value.GetEnumerator();
            public bool Remove(string key) => _value.Remove(key);
            bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) => _value.Remove(item);
            public object this[string key] { get => _value[key]; set => _value[key] = value; }
            ICollection<string> IDictionary<string, object>.Keys => _value.Keys;
            ICollection<object> IDictionary<string, object>.Values => _value.Values;
            public int Count => _value.Count;
            bool ICollection<KeyValuePair<string, object>>.IsReadOnly => _value.IsReadOnly;
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_value).GetEnumerator();
            public bool TryGetValue(string key, out object value) => _value.TryGetValue(key, out value);
        }

        /// <summary>
        /// Supports dynamic arrays.
        /// </summary>
        public sealed class JsonDynamicArray : JsonDynamicType, IList<object>
        {
            private IList<object> _value;

            public JsonDynamicArray(JsonSerializerOptions options) : base(options)
            {
                _value = new List<object>();
            }

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

            internal override object Value => _value;

            public override T GetValue<T>() => throw new NotSupportedException();
            public override void SetValue(object value) => throw new NotSupportedException();

            // IList members.
            public object this[int index] { get => _value[index]; set => _value[index] = value; }
            public int Count => _value.Count;
            bool ICollection<object>.IsReadOnly => _value.IsReadOnly;
            public void Add(object item) => _value.Add(item);
            public void Clear() => _value.Clear();
            public bool Contains(object item) => _value.Contains(item);
            void ICollection<object>.CopyTo(object[] array, int arrayIndex) => _value.CopyTo(array, arrayIndex);
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

            public override sealed object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        return new JsonDynamicString(reader.GetString(), options);
                    case JsonTokenType.StartArray:
                        var dynamicArray = new JsonDynamicArray(options);
                        ReadList(dynamicArray, ref reader, options);
                        return dynamicArray;
                    case JsonTokenType.StartObject:
                        var dynamicObject = new JsonDynamicObject(options);
                        ReadObject(dynamicObject, ref reader, options);
                        return dynamicObject;
                    case JsonTokenType.False:
                        return new JsonDynamicBoolean(false, options);
                    case JsonTokenType.True:
                        return new JsonDynamicBoolean(true, options);
                    case JsonTokenType.Number:
                        JsonElement jsonElement;
                        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
                        {
                            jsonElement = document.RootElement.Clone();
                        }
                        // In 6.0, this can be used instead for increased performance:
                        //JsonElement jsonElement = JsonElement.ParseValue(ref reader);
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

            private void ReadList(JsonDynamicArray dynamicArray, ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                while (true)
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    object value = Read(ref reader, typeof(object), options);
                    dynamicArray.Add(value);
                }
            }

            private void ReadObject(JsonDynamicObject dynamicObject, ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
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
                    object value = Read(ref reader, typeof(object), options);
                    dynamicObject.Add(key, value);
                }
            }
        }
    }
}
