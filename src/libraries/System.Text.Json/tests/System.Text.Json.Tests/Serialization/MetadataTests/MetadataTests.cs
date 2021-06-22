// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.Tests.Serialization
{
    public sealed class MetadataTests_Span : MetadataTests
    {
        public MetadataTests_Span() : base(SerializationWrapper.SpanSerializer) { }
    }

    public sealed class MetadataTests_String : MetadataTests
    {
        public MetadataTests_String() : base(SerializationWrapper.StringSerializer) { }
    }

    public sealed class MetadataTests_AsyncStream : MetadataTests
    {
        public MetadataTests_AsyncStream() : base(SerializationWrapper.AsyncStreamSerializer) { }
    }

    public sealed class MetadataTests_SyncStream : MetadataTests
    {
        public MetadataTests_SyncStream() : base(SerializationWrapper.SyncStreamSerializer) { }
    }

    public sealed class MetadataTests_LowLevel : MetadataTests
    {
        public MetadataTests_LowLevel() : base(SerializationWrapper.ReaderWriterSerializer) { }
    }

    public abstract partial class MetadataTests
    {
        protected SerializationWrapper Serializer { get; }

        public MetadataTests(SerializationWrapper serializer)
        {
            Serializer = serializer;
        }
    }

    internal class WeatherForecastWithPOCOs
    {
        public DateTimeOffset Date { get; set; }
        public int TemperatureCelsius { get; set; }
        public string Summary { get; set; }
        public string SummaryField;
        public List<DateTimeOffset> DatesAvailable { get; set; }
        public Dictionary<string, HighLowTemps> TemperatureRanges { get; set; }
        public string[] SummaryWords { get; set; }
    }

    public class HighLowTemps
    {
        public int High { get; set; }
        public int Low { get; set; }
    }
}
