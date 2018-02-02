// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using AlphaVantageDataParser;
//
//    var data = TimeSeriesDaily.FromJson(jsonString);

namespace AlphaVantageDataParser
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public partial class TimeSeriesDaily
    {
        [JsonProperty("Meta Data")]
        public MetaData MetaData { get; set; }

        [JsonProperty("Time Series (Daily)")]
        public Dictionary<string, TimeSeriesDailyItem> TimeSeriesDailyItem { get; set; }
    }

    public partial class MetaData
    {
        [JsonProperty("1. Information")]
        public string Information { get; set; }

        [JsonProperty("2. Symbol")]
        public string Symbol { get; set; }

        [JsonProperty("3. Last Refreshed")]
        public System.DateTime LastRefreshed { get; set; }

        [JsonProperty("4. Output Size")]
        public string OutputSize { get; set; }

        [JsonProperty("5. Time Zone")]
        public string TimeZone { get; set; }
    }

    public partial class TimeSeriesDailyItem
    {
        [JsonProperty("1. open")]
        public string Open { get; set; }

        [JsonProperty("2. high")]
        public string High { get; set; }

        [JsonProperty("3. low")]
        public string Low { get; set; }

        [JsonProperty("4. close")]
        public string Close { get; set; }

        [JsonProperty("5. volume")]
        public string Volume { get; set; }
    }

    public partial class TimeSeriesDaily
    {
        public static TimeSeriesDaily FromJson(string json) => JsonConvert.DeserializeObject<TimeSeriesDaily>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this TimeSeriesDaily self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };
    }
}
