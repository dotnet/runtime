// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.Serialization.Tests
{
    public sealed class MetadataTests_Span : MetadataTests
    {
        public MetadataTests_Span() : base(JsonSerializerWrapper.SpanSerializer) { }
    }

    public sealed class MetadataTests_String : MetadataTests
    {
        public MetadataTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public sealed class MetadataTests_AsyncStream : MetadataTests
    {
        public MetadataTests_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public sealed class MetadataTests_SyncStream : MetadataTests
    {
        public MetadataTests_SyncStream() : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }

    public sealed class MetadataTests_LowLevel : MetadataTests
    {
        public MetadataTests_LowLevel() : base(JsonSerializerWrapper.ReaderWriterSerializer) { }
    }

    public class MetadataTests_Document : MetadataTests
    {
        public MetadataTests_Document() : base(JsonSerializerWrapper.DocumentSerializer) { }
    }

    public class MetadataTests_Element : MetadataTests
    {
        public MetadataTests_Element() : base(JsonSerializerWrapper.ElementSerializer) { }
    }

    public class MetadataTests_Node : MetadataTests
    {
        public MetadataTests_Node() : base(JsonSerializerWrapper.NodeSerializer) { }
    }

    public abstract partial class MetadataTests
    {
        protected JsonSerializerWrapper Serializer { get; }

        public MetadataTests(JsonSerializerWrapper serializer)
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

    [JsonSerializable(typeof(WeatherForecastWithPOCOs))]
    internal sealed partial class JsonContext : JsonSerializerContext
    {
    }
}
