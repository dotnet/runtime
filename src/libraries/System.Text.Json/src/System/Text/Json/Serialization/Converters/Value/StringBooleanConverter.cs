// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class StringBooleanConverter : JsonConverter<bool>
    {
        private readonly bool _convertBooleanToString;

        public StringBooleanConverter(bool convertBooleanToString)
        {
            _convertBooleanToString = convertBooleanToString;
        }

        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var incomingValue = reader.GetString();
            if (incomingValue.ToLowerInvariant() == bool.TrueString.ToLowerInvariant())
                return true;
            if (incomingValue.ToLowerInvariant() == bool.FalseString.ToLowerInvariant())
                return false;
            throw new JsonException("The string value could not be converted to System.Boolean");
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            if (_convertBooleanToString)
                writer.WriteStringValue(value.ToString());
            else
                writer.WriteBooleanValue(value);
        }
    }
}
