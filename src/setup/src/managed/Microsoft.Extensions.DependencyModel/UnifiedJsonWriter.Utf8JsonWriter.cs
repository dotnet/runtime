// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            => _writer.WriteStartObject(propertyName, escape);

        public void WriteStartArray(string propertyName, bool escape = true)
            => _writer.WriteStartArray(propertyName, escape);

        public void WriteString(string propertyName, string value, bool escape = true)
            => _writer.WriteString(propertyName, value, escape);

        public void WriteBoolean(string propertyName, bool value, bool escape = true)
            => _writer.WriteBoolean(propertyName, value, escape);

        public void WriteStringValue(string value, bool escape = true)
            => _writer.WriteStringValue(value, escape);
    }
}
