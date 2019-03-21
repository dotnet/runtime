// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyModel
{
    internal ref struct UnifiedJsonWriter
    {
        private readonly JsonTextWriter _writer;

        public UnifiedJsonWriter(JsonTextWriter writer)
        {
            _writer = writer;
        }

        public void WriteStartObject() => _writer.WriteStartObject();
        public void WriteEndObject() => _writer.WriteEndObject();
        public void WriteStartArray() => _writer.WriteStartArray();
        public void WriteEndArray() => _writer.WriteEndArray();

        public void Flush() => _writer.Flush();

        public void WriteStartObject(string propertyName, bool escape = true)
        {
            _writer.WritePropertyName(propertyName, escape);
            _writer.WriteStartObject();
        }

        public void WriteStartArray(string propertyName, bool escape = true)
        {
            _writer.WritePropertyName(propertyName, escape);
            _writer.WriteStartArray();
        }

        public void WriteString(string propertyName, string value, bool escape = true)
        {
            _writer.WritePropertyName(propertyName, escape);
            _writer.WriteValue(value);
        }

        public void WriteBoolean(string propertyName, bool value, bool escape = true)
        {
            _writer.WritePropertyName(propertyName, escape);
            _writer.WriteValue(value);
        }

        public void WriteStringValue(string value, bool escape = true)
            => _writer.WriteValue(value);
    }
}
