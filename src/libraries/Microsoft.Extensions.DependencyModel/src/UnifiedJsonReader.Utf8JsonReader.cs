// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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

        public void Skip() => _reader.Skip();

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
            // Replace with public API once https://github.com/dotnet/runtime/issues/28482 is fixed
            object boxedState = reader.CurrentState;
            long lineNumber = (long)(typeof(JsonReaderState).GetField("_lineNumber", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(boxedState) ?? -1);
            long bytePositionInLine = (long)(typeof(JsonReaderState).GetField("_bytePositionInLine", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(boxedState) ?? -1);

            return new FormatException($"Unexpected character encountered, excepted '{expected}' " +
                                       $"at line {lineNumber} position {bytePositionInLine}");
        }
    }
}
