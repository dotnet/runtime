// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration.Tests.RepeatedTypes
{
    public class Location
    {
        public int FakeId { get; set; }
        public string FakeAddress1 { get; set; }
        public string FakeAddress2 { get; set; }
        public string FakeCity { get; set; }
        public string FakeState { get; set; }
        public string FakePostalCode { get; set; }
        public string FakeName { get; set; }
        public string FakePhoneNumber { get; set; }
        public string FakeCountry { get; set; }
    }
}

namespace System.Text.Json.SourceGeneration.Tests
{
    public class Location
    {
        public int Id { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Country { get; set; }
    }

    public class NumberTypes
    {
        public float Single { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public sbyte SByte { get; set; }
        public byte Byte { get; set; }
        public ushort UShort { get; set; }
        public short Short { get; set; }
        public uint UInt { get; set; }
        public int Int { get; set; }
        public ulong ULong { get; set; }
        public long Long { get; set; }
    }

    public class ActiveOrUpcomingEvent
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public string Name { get; set; }
        public string CampaignName { get; set; }
        public string CampaignManagedOrganizerName { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }

    public class CampaignSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string OrganizationName { get; set; }
        public string Headline { get; set; }
    }

    public class IndexViewModel
    {
        public List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
        public CampaignSummaryViewModel FeaturedCampaign { get; set; }
        public bool IsNewAccount { get; set; }
        public bool HasFeaturedCampaign => FeaturedCampaign != null;
    }

    public class WeatherForecastWithPOCOs
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

    public class HighLowTempsImmutable
    {
        public int High { get; }
        public int Low { get; }

        public HighLowTempsImmutable(int high, int low) => (High, Low) = (high, low);
    }

    public class EmptyPoco
    {
    }

    public class MyType
    {
        public MyType Type;
    }

    public class MyType2
    {
        public MyIntermediateType Type = new();
    }

    public class MyIntermediateType
    {
        public MyType Type = new();
    }

    public class MyTypeWithCallbacks : IJsonOnSerializing, IJsonOnSerialized
    {
        public string MyProperty { get; set; }

        public void OnSerializing() => MyProperty = "Before";
        void IJsonOnSerialized.OnSerialized() => MyProperty = "After";
    }

    public class MyTypeWithPropertyOrdering
    {
        public int B { get; set; }

        [JsonPropertyOrder(1)]
        public int A { get; set; }

        [JsonPropertyOrder(-1)]
        [JsonInclude]
        public int C = 0;
    }

    public class JsonMessage
    {
        public string Message { get; set; }
        public int Length => Message?.Length ?? 0; // Read-only property
    }

    internal struct MyStruct { }

    /// <summary>
    /// Custom converter that adds\substract 100 from MyIntProperty.
    /// </summary>
    public class CustomConverterForClass : JsonConverter<ClassWithCustomConverter>
    {
        public override ClassWithCustomConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("No StartObject");
            }

            ClassWithCustomConverter obj = new();

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName &&
                reader.GetString() != "MyInt")
            {
                throw new JsonException("Wrong property name");
            }

            reader.Read();
            obj.MyInt = reader.GetInt32() - 100;

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("No EndObject");
            }

            return obj;
        }

        public override void Write(Utf8JsonWriter writer, ClassWithCustomConverter value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(ClassWithCustomConverter.MyInt), value.MyInt + 100);
            writer.WriteEndObject();
        }
    }

    [JsonConverter(typeof(CustomConverterForClass))]
    public class ClassWithCustomConverter
    {
        public int MyInt { get; set; }
    }

    [JsonConverter(typeof(CustomConverterForStruct))] // Invalid
    public struct ClassWithBadCustomConverter
    {
        public int MyInt { get; set; }
    }

    /// <summary>
    /// Custom converter that adds\substract 100 from MyIntProperty.
    /// </summary>
    public class CustomConverterForStruct : JsonConverter<StructWithCustomConverter>
    {
        public override StructWithCustomConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("No StartObject");
            }

            StructWithCustomConverter obj = new();

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName &&
                reader.GetString() != "MyInt")
            {
                throw new JsonException("Wrong property name");
            }

            reader.Read();
            obj.MyInt = reader.GetInt32() - 100;

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("No EndObject");
            }

            return obj;
        }

        public override void Write(Utf8JsonWriter writer, StructWithCustomConverter value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(StructWithCustomConverter.MyInt), value.MyInt + 100);
            writer.WriteEndObject();
        }
    }

    [JsonConverter(typeof(CustomConverterForStruct))]
    public struct StructWithCustomConverter
    {
        public int MyInt { get; set; }
    }

    [JsonConverter(typeof(CustomConverterForClass))] // Invalid
    public struct StructWithBadCustomConverter
    {
        public int MyInt { get; set; }
    }
}
