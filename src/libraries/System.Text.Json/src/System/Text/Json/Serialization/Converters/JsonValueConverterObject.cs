// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterObject : JsonValueConverter<object>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out object value)
        {
            if (JsonDocument.TryParseValue(ref reader, out JsonDocument document))
            {
                value = document.RootElement.Clone();
                document.Dispose();
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public override void Write(object value, Utf8JsonWriter writer)
        {
            throw new InvalidOperationException();
        }

        public override void Write(Span<byte> escapedPropertyName, object value, Utf8JsonWriter writer)
        {
            throw new InvalidOperationException();
        }
    }
}
