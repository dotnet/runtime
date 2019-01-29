// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyModel
{
    internal ref struct UnifiedJsonReader
    {
        private Utf8JsonReader _reader;

        public UnifiedJsonReader(Utf8JsonReader reader)
        {
            _reader = reader;
        }

        public bool Read() => _reader.Read();

        public string GetStringValue() => _reader.GetString();

        public bool IsTokenTypeProperty()
            => _reader.TokenType == JsonTokenType.PropertyName;

        public bool TryReadStringProperty(out string name, out string value)
        {
            name = null;
            value = null;
            if (_reader.Read() && IsTokenTypeProperty())
            {
                name = GetStringValue();

                if (_reader.Read())
                {
                    if (_reader.TokenType == JsonTokenType.String)
                    {
                        value = GetStringValue();
                    }
                    else
                    {
                        Skip();
                    }
                }

                return true;
            }

            return false;
        }

        public void ReadStartObject()
        {
            _reader.Read();
            CheckStartObject();
        }

        public void CheckStartObject()
        {
            if (_reader.TokenType != JsonTokenType.StartObject)
            {
                throw CreateUnexpectedException(ref _reader, "{");
            }
        }

        public void CheckEndObject()
        {
            if (_reader.TokenType != JsonTokenType.EndObject)
            {
                throw CreateUnexpectedException(ref _reader, "}");
            }
        }

        public string[] ReadStringArray()
        {
            _reader.Read();
            if (_reader.TokenType != JsonTokenType.StartArray)
            {
                throw CreateUnexpectedException(ref _reader, "[");
            }

            var items = new List<string>();

            while (_reader.Read() && _reader.TokenType == JsonTokenType.String)
            {
                items.Add(GetStringValue());
            }

            if (_reader.TokenType != JsonTokenType.EndArray)
            {
                throw CreateUnexpectedException(ref _reader, "]");
            }

            return items.ToArray();
        }

        public void Skip()
        {
            if (IsTokenTypeProperty())
            {
                _reader.Read();
            }

            if (_reader.TokenType == JsonTokenType.StartObject || _reader.TokenType == JsonTokenType.StartArray)
            {
                int depth = _reader.CurrentDepth;
                while (_reader.Read() && depth <= _reader.CurrentDepth)
                {
                }
            }
        }

        public string ReadAsString()
        {
            Debug.Assert(IsTokenTypeProperty());
            _reader.Read();
            if (_reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (_reader.TokenType != JsonTokenType.String)
            {
                throw CreateUnexpectedException(ref _reader, "a JSON string token");
            }
            Debug.Assert(!IsTokenTypeProperty());
            return GetStringValue();
        }

        public bool? ReadAsNullableBoolean()
        {
            Debug.Assert(IsTokenTypeProperty());
            _reader.Read();
            if (_reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (_reader.TokenType != JsonTokenType.True && _reader.TokenType != JsonTokenType.False)
            {
                throw CreateUnexpectedException(ref _reader, "a JSON true or false literal token");
            }
            return _reader.GetBoolean();
        }

        public bool ReadAsBoolean(bool defaultValue)
        {
            Debug.Assert(IsTokenTypeProperty());
            _reader.Read();
            if (_reader.TokenType == JsonTokenType.Null)
            {
                return defaultValue;
            }
            if (_reader.TokenType != JsonTokenType.True && _reader.TokenType != JsonTokenType.False)
            {
                throw CreateUnexpectedException(ref _reader, "a JSON true or false literal token");
            }
            return _reader.GetBoolean();
        }

        private static Exception CreateUnexpectedException(ref Utf8JsonReader reader, string expected)
        {
            return new FormatException($"Unexpected character encountered, excepted '{expected}' " +
                                       $"at line {reader.CurrentState._lineNumber} position {reader.CurrentState._bytePositionInLine}");
        }
    }
}
