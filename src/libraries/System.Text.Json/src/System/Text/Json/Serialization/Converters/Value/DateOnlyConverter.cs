// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DateOnlyConverter : JsonConverter<DateOnly>
    {
        private const int DateOnlyIsoFormatLength = 10; // YYYY-MM-DD

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            DateTime dateTime = reader.GetDateTime();
            return DateOnly.FromDateTime(dateTime);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            Span<char> buffer = stackalloc char[DateOnlyIsoFormatLength];
            bool formattedSuccessfully = value.TryFormat(buffer, out int charsWritten, "O", CultureInfo.InvariantCulture);
            Debug.Assert(formattedSuccessfully && charsWritten == DateOnlyIsoFormatLength);
            writer.WriteStringValue(buffer);
        }

        internal override DateOnly ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            DateTime dateTime = reader.GetDateTimeNoValidation();
            return DateOnly.FromDateTime(dateTime);
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            Span<char> buffer = stackalloc char[DateOnlyIsoFormatLength];
            bool formattedSuccessfully = value.TryFormat(buffer, out int charsWritten, "O", CultureInfo.InvariantCulture);
            Debug.Assert(formattedSuccessfully && charsWritten == DateOnlyIsoFormatLength);
            writer.WritePropertyName(buffer);
        }
    }
}
