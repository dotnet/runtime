// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        private static readonly TimeSpanConverter s_timeSpanConverter = new TimeSpanConverter();
        private static readonly TimeSpan s_timeOnlyMaxValue = TimeOnly.MaxValue.ToTimeSpan();

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            TimeSpan timespan = s_timeSpanConverter.Read(ref reader, typeToConvert, options);

            if (timespan < TimeSpan.Zero || timespan > s_timeOnlyMaxValue)
            {
                ThrowHelper.ThrowJsonException();
            }

            return TimeOnly.FromTimeSpan(timespan);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            s_timeSpanConverter.Write(writer, value.ToTimeSpan(), options);
        }
    }
}
