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

    public class CovariantBaseNotIgnored
    {
        public virtual object Id { get; } = nameof(CovariantBaseNotIgnored);
    }

    public class CovariantBaseNotIgnored_CovariantDerivedNotIgnored : CovariantBaseNotIgnored
    {
        public override string Id { get; } = nameof(CovariantBaseNotIgnored_CovariantDerivedNotIgnored);
    }

    public class CovariantBaseNotIgnored_CovariantDerivedGenericNotIgnored<T> : CovariantBaseNotIgnored
        where T : class
    {
        public override T Id { get; } = (T)(object)nameof(CovariantBaseNotIgnored_CovariantDerivedGenericNotIgnored<T>);
    }

    public class CovariantBaseNotIgnored_CovariantDerivedIgnored : CovariantBaseNotIgnored
    {
        [JsonIgnore]
        public override string Id { get; } = nameof(CovariantBaseNotIgnored_CovariantDerivedIgnored);
    }

    public class CovariantBaseNotIgnored_CovariantDerivedGenericIgnored<T> : CovariantBaseNotIgnored
        where T : class
    {
        [JsonIgnore]
        public override T Id { get; } = (T)(object)nameof(CovariantBaseNotIgnored_CovariantDerivedGenericIgnored<T>);
    }

    public class CovariantBaseIgnored
    {
        [JsonIgnore]
        public virtual object Id { get; } = nameof(CovariantBaseIgnored);
    }

    public class CovariantBaseIgnored_CovariantDerivedNotIgnored : CovariantBaseIgnored
    {
        public override string Id { get; } = nameof(CovariantBaseIgnored_CovariantDerivedNotIgnored);
    }

    public class CovariantBaseIgnored_CovariantDerivedGenericNotIgnored<T> : CovariantBaseIgnored
        where T : class
    {
        public override T Id { get; } = (T)(object)nameof(CovariantBaseIgnored_CovariantDerivedGenericNotIgnored<T>);
    }

    public class CovariantBaseIgnored_CovariantDerivedIgnored : CovariantBaseIgnored
    {
        [JsonIgnore]
        public override string Id { get; } = nameof(CovariantBaseIgnored_CovariantDerivedIgnored);
    }

    public class CovariantBaseIgnored_CovariantDerivedGenericIgnored<T> : CovariantBaseIgnored
        where T : class
    {
        [JsonIgnore]
        public override T Id { get; } = (T)(object)nameof(CovariantBaseIgnored_CovariantDerivedGenericIgnored<T>);
    }

    public class NotIgnoredPropertyBase
    {
        public string Id { get; set; } = nameof(NotIgnoredPropertyBase);
    }

    public class NotIgnoredPropertyBase_NotIgnoredPropertyDerived : NotIgnoredPropertyBase
    {
        public new string Id { get; set; } = nameof(NotIgnoredPropertyBase_NotIgnoredPropertyDerived);
    }

    public class NotIgnoredPropertyBase_IgnoredPropertyDerived : NotIgnoredPropertyBase
    {
        [JsonIgnore]
        public new string Id { get; set; } = nameof(NotIgnoredPropertyBase_IgnoredPropertyDerived);
    }

    public class IgnoredPropertyBase
    {
        [JsonIgnore]
        public string Id { get; set; } = nameof(IgnoredPropertyBase);
    }

    public class IgnoredPropertyBase_NotIgnoredPropertyDerived : IgnoredPropertyBase
    {
        public new string Id { get; set; } = nameof(IgnoredPropertyBase_NotIgnoredPropertyDerived);
    }

    public class IgnoredPropertyBase_IgnoredPropertyDerived : IgnoredPropertyBase
    {
        [JsonIgnore]
        public new string Id { get; set; } = nameof(IgnoredPropertyBase_IgnoredPropertyDerived);
    }

    public class NotIgnoredVirtualPropertyBase
    {
        public virtual string Id { get; set; } = nameof(NotIgnoredVirtualPropertyBase);
    }

    public class NotIgnoredVirtualPropertyBase_NotIgnoredOverriddenPropertyDerived : NotIgnoredVirtualPropertyBase
    {
        public override string Id { get; set; } = nameof(NotIgnoredVirtualPropertyBase_NotIgnoredOverriddenPropertyDerived);
    }

    public class NotIgnoredVirtualPropertyBase_IgnoredOverriddenPropertyDerived : NotIgnoredVirtualPropertyBase
    {
        [JsonIgnore]
        public override string Id { get; set; } = nameof(NotIgnoredVirtualPropertyBase_IgnoredOverriddenPropertyDerived);
    }

    public class IgnoredVirtualPropertyBase
    {
        [JsonIgnore]
        public virtual string Id { get; set; } = nameof(IgnoredVirtualPropertyBase);
    }

    public class IgnoredVirtualPropertyBase_NotIgnoredOverriddenPropertyDerived : IgnoredVirtualPropertyBase
    {
        public override string Id { get; set; } = nameof(IgnoredVirtualPropertyBase_NotIgnoredOverriddenPropertyDerived);
    }

    public class IgnoredVirtualPropertyBase_IgnoredOverriddenPropertyDerived : IgnoredVirtualPropertyBase
    {
        [JsonIgnore]
        public override string Id { get; set; } = nameof(IgnoredVirtualPropertyBase_IgnoredOverriddenPropertyDerived);
    }
}
