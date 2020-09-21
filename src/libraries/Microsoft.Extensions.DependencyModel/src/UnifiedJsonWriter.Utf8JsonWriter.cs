// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.Extensions.DependencyModel
{
    internal ref struct UnifiedJsonWriter
    {
        private Utf8JsonWriter _writer;

        public UnifiedJsonWriter(Utf8JsonWriter writer)
        {
            _writer = writer;
        }

        public void WriteStartObject() => _writer.WriteStartObject();
        public void WriteEndObject() => _writer.WriteEndObject();
        public void WriteStartArray() => _writer.WriteStartArray();
        public void WriteEndArray() => _writer.WriteEndArray();

        public void Flush() => _writer.Flush();

        public void WriteStartObject(string propertyName, bool escape = true)
            => _writer.WriteStartObject(propertyName);

        public void WriteStartArray(string propertyName, bool escape = true)
            => _writer.WriteStartArray(propertyName);

        public void WriteString(string propertyName, string value, bool escape = true)
            => _writer.WriteString(propertyName, value);

        public void WriteBoolean(string propertyName, bool value, bool escape = true)
            => _writer.WriteBoolean(propertyName, value);

        public void WriteStringValue(string value, bool escape = true)
            => _writer.WriteStringValue(value);
    }
}
