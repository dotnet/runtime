// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Primitives;

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
        public string? SummaryField;
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

    public record HighLowTempsRecord(int High, int Low);

    public class EmptyPoco
    {
    }

    public class MyType
    {
        public MyType? Type;
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

    public class AllocatingOnPropertyAccess 
    {
        public int WhenWritingNullAccessCounter = 0;
        public int WhenWritingDefaultAccessCounter = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string SomeAllocatingProperty => $"Current Value: {++WhenWritingNullAccessCounter}";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string SomeAllocatingProperty2 => $"Current Value: {++WhenWritingDefaultAccessCounter}";
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

    public class MyContainingClass
    {
        public class MyNestedClass
        {
            public class MyNestedNestedClass { }
            public class MyNestedNestedGenericClass<T1> { }
        }
        public class MyNestedGenericClass<T1>
        {
            public class MyNestedGenericNestedClass { }
            public class MyNestedGenericNestedGenericClass<T2> { }
        }
    }

    public class MyContainingGenericClass<T>
    {
        public class MyNestedClass
        {
            public class MyNestedNestedClass { }
            public class MyNestedNestedGenericClass<T1> { }
        }
        public class MyNestedGenericClass<T1>
        {
            public class MyNestedGenericNestedClass { }
            public class MyNestedGenericNestedGenericClass<T2>
            {
                public T DataT { get; set; }
                public T1 DataT1 { get; set; }
                public T2 DataT2 { get; set; }
            }
        }
    }

    public class PublicClassWithDifferentAccessibilitiesProperties
    {
        public PublicTestClass PublicProperty { get; set; }
        internal PublicTestClass.InternalNestedClass InternalProperty1 { get; set; }
        protected ProtectedClass ProtectedProperty1 { get; set; }
        protected ProtectedInternalClass ProtectedProperty2 { get; set; }
        internal InternalTestClass InternalProperty2 { get; set; }
        internal InternalTestClass.PublicClass InternalProperty3 { get; set; }
        internal InternalTestClass.ProtectedInternalClass InternalProperty4 { get; set; }
        private InternalTestClass PrivateProperty1 { get; set; }
        private PrivateClass PrivateProperty2 { get; set; }
        private PrivateClass2 PrivateProperty3 { get; set; }
        PrivateClass2 PrivateProperty4 { get; set; }
        private PrivateProtectedClass PrivateProperty5 { get; set; }

        public PublicTestClass PublicField;

#pragma warning disable CS0414 // The field ... is assigned but its value is never used.
        internal PublicTestClass.InternalNestedClass InternalField1 = null;
        protected ProtectedClass ProtectedField1 = null;
        protected ProtectedInternalClass ProtectedField2 = null;
        internal InternalTestClass InternalField2 = null;
        internal InternalTestClass.PublicClass InternalField3 = null;
        internal InternalTestClass.ProtectedInternalClass InternalField4 = null;
        private InternalTestClass PrivateField1 = null;
        private PrivateClass PrivateField2 = null;
        private PrivateClass2 PrivateField3 = null;
        PrivateClass2 PrivateField4 = null;
        private PrivateProtectedClass PrivateField5 = null;
#pragma warning restore

        private class PrivateClass { }
        protected class ProtectedClass { }
        protected internal class ProtectedInternalClass { }
        private protected class PrivateProtectedClass { }
        class PrivateClass2 { }
    }

    internal class InternalTestClass
    {
        public class PublicClass { }
        protected internal class ProtectedInternalClass { }
    }

    public class PublicTestClass
    {
        internal class InternalNestedClass { }
    }

    public sealed class ClassWithStringValues
    {
        public StringValues StringValuesProperty { get; set; }
    }

    public class ClassWithDictionaryProperty
    {
        public ClassWithDictionaryProperty(Dictionary<string, object?> property) => DictionaryProperty = property;
        public Dictionary<string, object?> DictionaryProperty { get; }
    }

    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public class PocoWithNumberHandlingAttr
    {
        public int Id { get; set; }
    }

    public class PocoWithMixedVisibilityMembersBase
    {
        public string BaseProperty { get; set; }
        public string ShadowProperty { get; set; }
    }

    public class PocoWithMixedVisibilityMembers : PocoWithMixedVisibilityMembersBase
    {
        public string PublicProperty { get; set; }

        [JsonInclude]
        public string PublicField;

        [JsonInclude]
        internal int InternalProperty { get; set; }

        [JsonInclude]
        internal int InternalField;

        [JsonPropertyName("customProp")]
        public string PropertyWithCustomName { get; set; }

        [JsonInclude, JsonPropertyName("customField")]
        public string FieldWithCustomName;

        public new int ShadowProperty { get; set; }
    }

    public sealed class ClassWithConflictingIgnoredProperties
    {
        [JsonIgnore]
        public List<string>? UserList { get; set; }

        [JsonPropertyName("userlist")]
        public List<string>? SystemTextJsonUserList { get; set; }

        [JsonIgnore]
        public List<string>? UserGroupsList { get; set; }

        [JsonPropertyName("usergroupslist")]
        public List<string>? SystemTextJsonUserGroupsList { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<string>? SystemTextJsonIPAddresses { get; set; }

        [JsonIgnore]
        public List<object>? QueryParams { get; set; }

        [JsonPropertyName("queryparams")]
        public List<object>? SystemTextJsonQueryParams { get; set; }
    }
}
