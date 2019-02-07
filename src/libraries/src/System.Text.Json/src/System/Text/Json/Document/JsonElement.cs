// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    public readonly partial struct JsonElement
    {
        private readonly JsonDocument _parent;
        private readonly int _idx;

        internal JsonElement(JsonDocument parent, int idx)
        {
            // parent is usually not null, but the Current property
            // on the enumerators (when initialized as `default`) can
            // get here with a null.
            Debug.Assert(idx >= 0);

            _parent = parent;
            _idx = idx;
        }

        internal JsonTokenType TokenType => _parent?.GetJsonTokenType(_idx) ?? JsonTokenType.None;

        public JsonValueType Type => TokenType.ToValueType();

        public JsonElement this[int index]
        {
            get
            {
                CheckValidInstance();

                return _parent.GetArrayIndexElement(_idx, index);
            }
        }

        public int GetArrayLength()
        {
            CheckValidInstance();

            return _parent.GetArrayLength(_idx);
        }

        public JsonElement GetProperty(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            if (TryGetProperty(propertyName, out JsonElement property))
            {
                return property;
            }

            throw new KeyNotFoundException();
        }

        public JsonElement GetProperty(ReadOnlySpan<char> propertyName)
        {
            if (TryGetProperty(propertyName, out JsonElement property))
            {
                return property;
            }

            throw new KeyNotFoundException();
        }

        public JsonElement GetProperty(ReadOnlySpan<byte> utf8PropertyName)
        {
            if (TryGetProperty(utf8PropertyName, out JsonElement property))
            {
                return property;
            }

            throw new KeyNotFoundException();
        }

        public bool TryGetProperty(string propertyName, out JsonElement value)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            return TryGetProperty(propertyName.AsSpan(), out value);
        }

        public bool TryGetProperty(ReadOnlySpan<char> propertyName, out JsonElement value)
        {
            CheckValidInstance();

            return _parent.TryGetNamedPropertyValue(_idx, propertyName, out value);
        }

        public bool TryGetProperty(ReadOnlySpan<byte> utf8PropertyName, out JsonElement value)
        {
            CheckValidInstance();

            return _parent.TryGetNamedPropertyValue(_idx, utf8PropertyName, out value);
        }

        public bool GetBoolean()
        {
            // CheckValidInstance is redundant.  Asking for the type will
            // return None, which then throws the same exception in the return statement.

            JsonTokenType type = TokenType;

            return
                type == JsonTokenType.True ? true :
                type == JsonTokenType.False ? false :
                throw ThrowHelper.GetJsonElementWrongTypeException(nameof(Boolean), type);
        }

        public string GetString()
        {
            CheckValidInstance();

            return _parent.GetString(_idx, JsonTokenType.String);
        }

        public bool TryGetInt32(out int value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        public int GetInt32()
        {
            if (TryGetInt32(out int value))
            {
                return value;
            }

            throw new FormatException();
        }

        [CLSCompliant(false)]
        public bool TryGetUInt32(out uint value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        [CLSCompliant(false)]
        public uint GetUInt32()
        {
            if (TryGetUInt32(out uint value))
            {
                return value;
            }

            throw new FormatException();
        }

        public bool TryGetInt64(out long value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        public long GetInt64()
        {
            if (TryGetInt64(out long value))
            {
                return value;
            }

            throw new FormatException();
        }

        [CLSCompliant(false)]
        public bool TryGetUInt64(out ulong value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        [CLSCompliant(false)]
        public ulong GetUInt64()
        {
            if (TryGetUInt64(out ulong value))
            {
                return value;
            }

            throw new FormatException();
        }

        public bool TryGetDouble(out double value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        public double GetDouble()
        {
            if (TryGetDouble(out double value))
            {
                return value;
            }

            throw new FormatException();
        }

        public bool TryGetSingle(out float value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        public float GetSingle()
        {
            if (TryGetSingle(out float value))
            {
                return value;
            }

            throw new FormatException();
        }

        public bool TryGetDecimal(out decimal value)
        {
            CheckValidInstance();

            return _parent.TryGetValue(_idx, out value);
        }

        public decimal GetDecimal()
        {
            if (TryGetDecimal(out decimal value))
            {
                return value;
            }

            throw new FormatException();
        }

        internal string GetPropertyName()
        {
            CheckValidInstance();

            return _parent.GetNameOfPropertyValue(_idx);
        }

        public string GetRawText()
        {
            CheckValidInstance();

            return _parent.GetRawValueAsString(_idx);
        }

        internal string GetPropertyRawText()
        {
            CheckValidInstance();

            return _parent.GetPropertyRawValueAsString(_idx);
        }

        public ArrayEnumerator EnumerateArray()
        {
            CheckValidInstance();

            JsonTokenType tokenType = TokenType;

            if (tokenType != JsonTokenType.StartArray)
            {
                throw ThrowHelper.GetJsonElementWrongTypeException(JsonTokenType.StartArray, tokenType);
            }

            return new ArrayEnumerator(this);
        }

        public ObjectEnumerator EnumerateObject()
        {
            CheckValidInstance();

            JsonTokenType tokenType = TokenType;

            if (tokenType != JsonTokenType.StartObject)
            {
                throw ThrowHelper.GetJsonElementWrongTypeException(JsonTokenType.StartObject, tokenType);
            }

            return new ObjectEnumerator(this);
        }

        public override string ToString()
        {
            switch (TokenType)
            {
                case JsonTokenType.None:
                case JsonTokenType.Null:
                    return string.Empty;
                case JsonTokenType.True:
                    return bool.TrueString;
                case JsonTokenType.False:
                    return bool.FalseString;
                case JsonTokenType.Number:
                case JsonTokenType.StartArray:
                case JsonTokenType.StartObject:
                {
                    // null parent should have hit the None case
                    Debug.Assert(_parent != null);
                    return _parent.GetRawValueAsString(_idx);
                }
                case JsonTokenType.String:
                    return GetString();
                case JsonTokenType.Comment:
                case JsonTokenType.EndArray:
                case JsonTokenType.EndObject:
                default:
                    Debug.Fail($"No handler for {nameof(JsonTokenType)}.{TokenType}");
                    return string.Empty;
            }
        }

        private void CheckValidInstance()
        {
            if (_parent == null)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
