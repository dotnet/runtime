// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyModel
{
    internal ref struct UnifiedJsonReader
    {
        private JsonTextReader _reader;

        public UnifiedJsonReader(JsonTextReader reader)
        {
            _reader = reader;
        }

        public bool Read() => _reader.Read();

        public string GetStringValue() => (string)_reader.Value;

        public bool IsTokenTypeProperty()
            => _reader.TokenType == JsonToken.PropertyName;

        public bool TryReadStringProperty(out string name, out string value)
        {
            name = null;
            value = null;
            if (_reader.Read() && IsTokenTypeProperty())
            {
                name = GetStringValue();

                if (_reader.Read())
                {
                    if (_reader.TokenType == JsonToken.String)
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
            if (_reader.TokenType != JsonToken.StartObject)
            {
                throw CreateUnexpectedException(_reader, "{");
            }
        }

        public void CheckEndObject()
        {
            if (_reader.TokenType != JsonToken.EndObject)
            {
                throw CreateUnexpectedException(_reader, "}");
            }
        }

        public string[] ReadStringArray()
        {
            _reader.Read();
            if (_reader.TokenType != JsonToken.StartArray)
            {
                throw CreateUnexpectedException(_reader, "[");
            }

            var items = new List<string>();

            while (_reader.Read() && _reader.TokenType == JsonToken.String)
            {
                items.Add(GetStringValue());
            }

            if (_reader.TokenType != JsonToken.EndArray)
            {
                throw CreateUnexpectedException(_reader, "]");
            }

            return items.ToArray();
        }

        public void Skip() => _reader.Skip();

        public string ReadAsString()
        {
            Debug.Assert(IsTokenTypeProperty());
            return _reader.ReadAsString();
        }

        public bool? ReadAsNullableBoolean()
        {
            Debug.Assert(IsTokenTypeProperty());
            return _reader.ReadAsBoolean();
        }

        public bool ReadAsBoolean(bool defaultValue)
        {
            Debug.Assert(IsTokenTypeProperty());
            bool? nullableBool = _reader.ReadAsBoolean();
            return nullableBool ?? defaultValue;
        }

        private static Exception CreateUnexpectedException(JsonTextReader reader, string expected)
        {
            return new FormatException($"Unexpected character encountered, excepted '{expected}' " +
                                       $"at line {reader.LineNumber} position {reader.LinePosition} path {reader.Path}");
        }
    }
}
