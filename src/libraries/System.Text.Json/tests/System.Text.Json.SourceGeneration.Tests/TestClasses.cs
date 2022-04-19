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
        public TimeSpan Offset { get; set; }
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

    public struct PersonStruct
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class TypeWithValidationAttributes
    {
        [ComponentModel.DataAnnotations.Required(ErrorMessage = "Name is required")]
        [ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Name must not be longer than 100 characters")]
        public string Name { get; set; }

        [ComponentModel.DataAnnotations.Required]
        public string Email { get; set; }
    }

    public class BaseAttribute : Attribute
    {
        public string TestProperty { get; set; }
    }

    public class DerivedAttribute : BaseAttribute
    { }

    [Derived(TestProperty = "Test")]
    public class TypeWithDerivedAttribute
    { }

    [JsonDerivedType(typeof(DerivedClass), "derivedClass")]
    public class PolymorphicClass
    {
        public int Number { get; set; }

        public class DerivedClass : PolymorphicClass
        {
            public bool Boolean { get; set; }
        }
    }
}
