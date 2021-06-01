// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.SourceGeneration.Tests;
using System.Text.Json.SourceGeneration.Tests.JsonSourceGeneration;
using Xunit;

[assembly: JsonSerializable(typeof(JsonSerializerSourceGeneratorTests.MyNestedClass))]
[assembly: JsonSerializable(typeof(JsonSerializerSourceGeneratorTests.MyNestedClass.MyNestedNestedClass))]
[assembly: JsonSerializable(typeof(object[]))]
[assembly: JsonSerializable(typeof(string))]
[assembly: JsonSerializable(typeof(JsonSerializerSourceGeneratorTests.ClassWithEnumAndNullable))]

namespace System.Text.Json.SourceGeneration.Tests
{
    public static class JsonSerializerSourceGeneratorTests
    {
        [Fact]
        public static void RoundTripLocation()
        {
            Location expected = CreateLocation();

            string json = JsonSerializer.Serialize(expected, JsonContext.Default.Location);
            Location obj = JsonSerializer.Deserialize(json, JsonContext.Default.Location);
            VerifyLocation(expected, obj);
        }

        [Fact]
        public static void RoundTripIndexViewModel()
        {
            IndexViewModel expected = CreateIndexViewModel();

            string json = JsonSerializer.Serialize(expected, JsonContext.Default.IndexViewModel);
            IndexViewModel obj = JsonSerializer.Deserialize(json, JsonContext.Default.IndexViewModel);

            VerifyIndexViewModel(expected, obj);
        }

        [Fact]
        public static void RoundTripCampaignSummaryViewModel()
        {
            CampaignSummaryViewModel expected = CreateCampaignSummaryViewModel();

            string json = JsonSerializer.Serialize(expected, JsonContext.Default.CampaignSummaryViewModel);
            CampaignSummaryViewModel obj = JsonSerializer.Deserialize(json, JsonContext.Default.CampaignSummaryViewModel);

            VerifyCampaignSummaryViewModel(expected, obj);
        }

        [Fact]
        public static void RoundTripActiveOrUpcomingEvent()
        {
            ActiveOrUpcomingEvent expected = CreateActiveOrUpcomingEvent();

            string json = JsonSerializer.Serialize(expected, JsonContext.Default.ActiveOrUpcomingEvent);
            ActiveOrUpcomingEvent obj = JsonSerializer.Deserialize(json, JsonContext.Default.ActiveOrUpcomingEvent);

            VerifyActiveOrUpcomingEvent(expected, obj);
        }

        [Fact]
        public static void RoundTripCollectionsDictionary()
        {
            WeatherForecastWithPOCOs expected = CreateWeatherForecastWithPOCOs();

            string json = JsonSerializer.Serialize(expected, JsonContext.Default.WeatherForecastWithPOCOs);
            WeatherForecastWithPOCOs obj = JsonSerializer.Deserialize(json, JsonContext.Default.WeatherForecastWithPOCOs);

            VerifyWeatherForecastWithPOCOs(expected, obj);
        }

        [Fact]
        public static void RoundTripEmptyPoco()
        {
            EmptyPoco expected = CreateEmptyPoco();

            string json = JsonSerializer.Serialize(expected, JsonContext.Default.EmptyPoco);
            EmptyPoco obj = JsonSerializer.Deserialize(json, JsonContext.Default.EmptyPoco);

            VerifyEmptyPoco(expected, obj);
        }

        [Fact]
        public static void RoundTripTypeNameClash()
        {
            RepeatedTypes.Location expected = CreateRepeatedLocation();

            string json = JsonSerializer.Serialize(expected, JsonContext.Default.RepeatedLocation);
            RepeatedTypes.Location obj = JsonSerializer.Deserialize(json, JsonContext.Default.RepeatedLocation);

            VerifyRepeatedLocation(expected, obj);
        }

        private static Location CreateLocation()
        {
            return new Location
            {
                Id = 1234,
                Address1 = "The Street Name",
                Address2 = "20/11",
                City = "The City",
                State = "The State",
                PostalCode = "abc-12",
                Name = "Nonexisting",
                PhoneNumber = "+0 11 222 333 44",
                Country = "The Greatest"
            };
        }

        private static void VerifyLocation(Location expected, Location obj)
        {
            Assert.Equal(expected.Address1, obj.Address1);
            Assert.Equal(expected.Address2, obj.Address2);
            Assert.Equal(expected.City, obj.City);
            Assert.Equal(expected.State, obj.State);
            Assert.Equal(expected.PostalCode, obj.PostalCode);
            Assert.Equal(expected.Name, obj.Name);
            Assert.Equal(expected.PhoneNumber, obj.PhoneNumber);
            Assert.Equal(expected.Country, obj.Country);
        }

        private static ActiveOrUpcomingEvent CreateActiveOrUpcomingEvent()
        {
            return new ActiveOrUpcomingEvent
            {
                Id = 10,
                CampaignManagedOrganizerName = "Name FamilyName",
                CampaignName = "The very new campaign",
                Description = "The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities.",
                EndDate = DateTime.UtcNow.AddYears(1),
                Name = "Just a name",
                ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                StartDate = DateTime.UtcNow
            };
        }

        private static void VerifyActiveOrUpcomingEvent(ActiveOrUpcomingEvent expected, ActiveOrUpcomingEvent obj)
        {
            Assert.Equal(expected.CampaignManagedOrganizerName, obj.CampaignManagedOrganizerName);
            Assert.Equal(expected.CampaignName, obj.CampaignName);
            Assert.Equal(expected.Description, obj.Description);
            Assert.Equal(expected.EndDate, obj.EndDate);
            Assert.Equal(expected.Id, obj.Id);
            Assert.Equal(expected.ImageUrl, obj.ImageUrl);
            Assert.Equal(expected.Name, obj.Name);
            Assert.Equal(expected.StartDate, obj.StartDate);
        }

        private static CampaignSummaryViewModel CreateCampaignSummaryViewModel()
        {
            return new CampaignSummaryViewModel
            {
                Description = "Very nice campaign",
                Headline = "The Headline",
                Id = 234235,
                OrganizationName = "The Company XYZ",
                ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                Title = "Promoting Open Source"
            };
        }

        private static void VerifyCampaignSummaryViewModel(CampaignSummaryViewModel expected, CampaignSummaryViewModel obj)
        {
            Assert.Equal(expected.Description, obj.Description);
            Assert.Equal(expected.Headline, obj.Headline);
            Assert.Equal(expected.Id, obj.Id);
            Assert.Equal(expected.ImageUrl, obj.ImageUrl);
            Assert.Equal(expected.OrganizationName, obj.OrganizationName);
            Assert.Equal(expected.Title, obj.Title);
        }

        private static IndexViewModel CreateIndexViewModel()
        {
            return new IndexViewModel
            {
                IsNewAccount = false,
                FeaturedCampaign = new CampaignSummaryViewModel
                {
                    Description = "Very nice campaign",
                    Headline = "The Headline",
                    Id = 234235,
                    OrganizationName = "The Company XYZ",
                    ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                    Title = "Promoting Open Source"
                },
                ActiveOrUpcomingEvents = Enumerable.Repeat(
                    new ActiveOrUpcomingEvent
                    {
                        Id = 10,
                        CampaignManagedOrganizerName = "Name FamilyName",
                        CampaignName = "The very new campaign",
                        Description = "The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities.",
                        EndDate = DateTime.UtcNow.AddYears(1),
                        Name = "Just a name",
                        ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                        StartDate = DateTime.UtcNow
                    },
                    count: 20).ToList()
            };
        }

        private static void VerifyIndexViewModel(IndexViewModel expected, IndexViewModel obj)
        {
            Assert.Equal(expected.ActiveOrUpcomingEvents.Count, obj.ActiveOrUpcomingEvents.Count);
            for (int i = 0; i < expected.ActiveOrUpcomingEvents.Count; i++)
            {
                VerifyActiveOrUpcomingEvent(expected.ActiveOrUpcomingEvents[i], obj.ActiveOrUpcomingEvents[i]);
            }

            VerifyCampaignSummaryViewModel(expected.FeaturedCampaign, obj.FeaturedCampaign);
            Assert.Equal(expected.HasFeaturedCampaign, obj.HasFeaturedCampaign);
            Assert.Equal(expected.IsNewAccount, obj.IsNewAccount);
        }

        private static WeatherForecastWithPOCOs CreateWeatherForecastWithPOCOs()
        {
            return new WeatherForecastWithPOCOs
            {
                Date = DateTime.Parse("2019-08-01T00:00:00-07:00"),
                TemperatureCelsius = 25,
                Summary = "Hot",
                DatesAvailable = new List<DateTimeOffset>
                {
                    DateTimeOffset.Parse("2019-08-01T00:00:00-07:00"),
                    DateTimeOffset.Parse("2019-08-02T00:00:00-07:00"),
                },
                TemperatureRanges = new Dictionary<string, HighLowTemps> {
                    {
                        "Cold",
                        new HighLowTemps
                        {
                            High = 20,
                            Low = -10,
                        }
                    },
                    {
                        "Hot",
                        new HighLowTemps
                        {
                            High = 60,
                            Low = 20,
                        }
                    },
                },
                SummaryWords = new string[] { "Cool", "Windy", "Humid" },
            };
        }

        private static void VerifyWeatherForecastWithPOCOs(WeatherForecastWithPOCOs expected, WeatherForecastWithPOCOs obj)
        {
            Assert.Equal(expected.Date, obj.Date);
            Assert.Equal(expected.TemperatureCelsius, obj.TemperatureCelsius);
            Assert.Equal(expected.Summary, obj.Summary);
            Assert.Equal(expected.DatesAvailable.Count, obj.DatesAvailable.Count);
            for (int i = 0; i < expected.DatesAvailable.Count; i++)
            {
                Assert.Equal(expected.DatesAvailable[i], obj.DatesAvailable[i]);
            }
            List<KeyValuePair<string, HighLowTemps>> expectedTemperatureRanges = expected.TemperatureRanges.OrderBy(kv => kv.Key).ToList();
            List<KeyValuePair<string, HighLowTemps>> objTemperatureRanges = obj.TemperatureRanges.OrderBy(kv => kv.Key).ToList();
            Assert.Equal(expectedTemperatureRanges.Count, objTemperatureRanges.Count);
            for (int i = 0; i < expectedTemperatureRanges.Count; i++)
            {
                Assert.Equal(expectedTemperatureRanges[i].Key, objTemperatureRanges[i].Key);
                Assert.Equal(expectedTemperatureRanges[i].Value.Low, objTemperatureRanges[i].Value.Low);
                Assert.Equal(expectedTemperatureRanges[i].Value.High, objTemperatureRanges[i].Value.High);
            }
            Assert.Equal(expected.SummaryWords.Length, obj.SummaryWords.Length);
            for (int i = 0; i < expected.SummaryWords.Length; i++)
            {
                Assert.Equal(expected.SummaryWords[i], obj.SummaryWords[i]);
            }
        }

        private static RepeatedTypes.Location CreateRepeatedLocation()
        {
            return new RepeatedTypes.Location
            {
                FakeId = 1234,
                FakeAddress1 = "The Street Name",
                FakeAddress2 = "20/11",
                FakeCity = "The City",
                FakeState = "The State",
                FakePostalCode = "abc-12",
                FakeName = "Nonexisting",
                FakePhoneNumber = "+0 11 222 333 44",
                FakeCountry = "The Greatest"
            };
        }

        private static void VerifyRepeatedLocation(RepeatedTypes.Location expected, RepeatedTypes.Location obj)
        {
            Assert.Equal(expected.FakeAddress1, obj.FakeAddress1);
            Assert.Equal(expected.FakeAddress2, obj.FakeAddress2);
            Assert.Equal(expected.FakeCity, obj.FakeCity);
            Assert.Equal(expected.FakeState, obj.FakeState);
            Assert.Equal(expected.FakePostalCode, obj.FakePostalCode);
            Assert.Equal(expected.FakeName, obj.FakeName);
            Assert.Equal(expected.FakePhoneNumber, obj.FakePhoneNumber);
            Assert.Equal(expected.FakeCountry, obj.FakeCountry);
        }

        private static EmptyPoco CreateEmptyPoco() => new EmptyPoco();

        private static void VerifyEmptyPoco(EmptyPoco expected, EmptyPoco obj)
        {
            Assert.NotNull(expected);
            Assert.NotNull(obj);
        }

        [Fact]
        public static void NestedSameTypeWorks()
        {
            MyType myType = new() { Type = new() };
            string json = JsonSerializer.Serialize(myType, JsonContext.Default.MyType);
            myType = JsonSerializer.Deserialize(json, JsonContext.Default.MyType);
            Assert.Equal(json, JsonSerializer.Serialize(myType, JsonContext.Default.MyType));

            MyType2 myType2 = new() { Type = new MyIntermediateType() { Type = myType } };
            json = JsonSerializer.Serialize(myType2, JsonContext.Default.MyType2);
            myType2 = JsonSerializer.Deserialize(json, JsonContext.Default.MyType2);
            Assert.Equal(json, JsonSerializer.Serialize(myType2, JsonContext.Default.MyType2));
        }

        [Fact]
        public static void SerializeObjectArray()
        {
            IndexViewModel index = CreateIndexViewModel();
            CampaignSummaryViewModel campaignSummary = CreateCampaignSummaryViewModel();

            string json = JsonSerializer.Serialize(new object[] { index, campaignSummary }, JsonContext.Default.ObjectArray);
            object[] arr = JsonSerializer.Deserialize(json, JsonContext.Default.ObjectArray);

            JsonElement indexAsJsonElement = (JsonElement)arr[0];
            JsonElement campaignSummeryAsJsonElement = (JsonElement)arr[1];
            VerifyIndexViewModel(index, JsonSerializer.Deserialize(indexAsJsonElement.GetRawText(), JsonContext.Default.IndexViewModel));
            VerifyCampaignSummaryViewModel(campaignSummary, JsonSerializer.Deserialize(campaignSummeryAsJsonElement.GetRawText(), JsonContext.Default.CampaignSummaryViewModel));
        }

        [Fact]
        public static void SerializeObjectArray_WithCustomOptions()
        {
            IndexViewModel index = CreateIndexViewModel();
            CampaignSummaryViewModel campaignSummary = CreateCampaignSummaryViewModel();

            JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            JsonContext context = new(options);

            string json = JsonSerializer.Serialize(new object[] { index, campaignSummary }, context.ObjectArray);
            object[] arr = JsonSerializer.Deserialize(json, context.ObjectArray);

            JsonElement indexAsJsonElement = (JsonElement)arr[0];
            JsonElement campaignSummeryAsJsonElement = (JsonElement)arr[1];
            VerifyIndexViewModel(index, JsonSerializer.Deserialize(indexAsJsonElement.GetRawText(), context.IndexViewModel));
            VerifyCampaignSummaryViewModel(campaignSummary, JsonSerializer.Deserialize(campaignSummeryAsJsonElement.GetRawText(), context.CampaignSummaryViewModel));
        }

        [Fact]
        public static void SerializeObjectArray_SimpleTypes_WithCustomOptions()
        {
            JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            JsonContext context = new JsonContext(options);

            string json = JsonSerializer.Serialize(new object[] { "Hello", "World" }, typeof(object[]), context);
            object[] arr = (object[])JsonSerializer.Deserialize(json, typeof(object[]), context);

            JsonElement hello = (JsonElement)arr[0];
            JsonElement world = (JsonElement)arr[1];
            Assert.Equal("\"Hello\"", hello.GetRawText());
            Assert.Equal("\"World\"", world.GetRawText());
        }

        [Fact]
        public static void HandlesNestedTypes()
        {
            string json = @"{""MyInt"":5}";
            MyNestedClass obj = JsonSerializer.Deserialize<MyNestedClass>(json, JsonContext.Default.MyNestedClass);
            Assert.Equal(5, obj.MyInt);
            Assert.Equal(json, JsonSerializer.Serialize(obj, JsonContext.Default.MyNestedClass));

            MyNestedClass.MyNestedNestedClass obj2 = JsonSerializer.Deserialize<MyNestedClass.MyNestedNestedClass>(json, JsonContext.Default.MyNestedNestedClass);
            Assert.Equal(5, obj2.MyInt);
            Assert.Equal(json, JsonSerializer.Serialize(obj2, JsonContext.Default.MyNestedNestedClass));
        }

        public class MyNestedClass
        {
            public int MyInt { get; set; }

            public class MyNestedNestedClass
            {
                public int MyInt { get; set; }
            }
        }

        [Fact]
        public static void ConstructingFromOptionsKeepsReference()
        {
            JsonStringEnumConverter converter = new();
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                Converters = { converter }
            };

            JsonContext context = new(options);
            Assert.Same(options, context.Options);
            Assert.Equal(options.PropertyNameCaseInsensitive, context.Options.PropertyNameCaseInsensitive);
            Assert.Same(converter, context.Options.Converters[0]);
        }

        [Fact]
        public static void JsonContextDefaultClonesDefaultOptions()
        {
            JsonContext context = JsonContext.Default;
            Assert.Equal(0, context.Options.Converters.Count);
        }

        [Fact]
        public static void JsonContextOptionsNotMutableAfterConstruction()
        {
            JsonContext context = JsonContext.Default;
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => context.Options.PropertyNameCaseInsensitive = true);
            string exAsStr = ex.ToString();
            Assert.Contains("JsonSerializerOptions", exAsStr);
            Assert.Contains("JsonSerializerContext", exAsStr);

            context = new JsonContext(new JsonSerializerOptions());
            ex = Assert.Throws<InvalidOperationException>(() => context.Options.PropertyNameCaseInsensitive = true);
            exAsStr = ex.ToString();
            Assert.Contains("JsonSerializerOptions", exAsStr);
            Assert.Contains("JsonSerializerContext", exAsStr);
        }

        [Fact]
        public static void ParameterizedConstructor()
        {
            string json = JsonSerializer.Serialize(new HighLowTempsImmutable(1, 2), JsonContext.Default.HighLowTempsImmutable);
            Assert.Contains(@"""High"":1", json);
            Assert.Contains(@"""Low"":2", json);

            // Deserialization not supported for now.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(json, JsonContext.Default.HighLowTempsImmutable));
        }

        [Fact]
        public static void EnumAndNullable()
        {
            RunTest(new ClassWithEnumAndNullable() { Day = DayOfWeek.Monday, NullableDay = DayOfWeek.Tuesday });
            RunTest(new ClassWithEnumAndNullable());

            static void RunTest(ClassWithEnumAndNullable expected)
            {
                string json = JsonSerializer.Serialize(expected, JsonContext.Default.ClassWithEnumAndNullable);
                ClassWithEnumAndNullable actual = JsonSerializer.Deserialize(json, JsonContext.Default.ClassWithEnumAndNullable);
                Assert.Equal(expected.Day, actual.Day);
                Assert.Equal(expected.NullableDay, actual.NullableDay);
            }
        }

        public class ClassWithEnumAndNullable
        {
            public DayOfWeek Day { get; set; }
            public DayOfWeek? NullableDay { get; set; }
        }

        [Fact]
        public static void Converters_AndTypeInfoCreator_NotRooted_WhenMetadataNotPresent()
        {
            object[] objArr = new object[] { new MyStruct() };

            // Metadata not generated for MyStruct without JsonSerializableAttribute.
            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => JsonSerializer.Serialize(objArr, JsonContext.Default.ObjectArray));
            string exAsStr = ex.ToString();
            Assert.Contains(typeof(MyStruct).ToString(), exAsStr);
            Assert.Contains("JsonSerializerOptions", exAsStr);

            // This test uses reflection to:
            // - Access JsonSerializerOptions.s_defaultSimpleConverters
            // - Access JsonSerializerOptions.s_defaultFactoryConverters
            // - Access JsonSerializerOptions._typeInfoCreationFunc
            //
            // If any of them changes, this test will need to be kept in sync.

            // Confirm built-in converters not set.
            AssertFieldNull("s_defaultSimpleConverters", optionsInstance: null);
            AssertFieldNull("s_defaultFactoryConverters", optionsInstance: null);

            // Confirm type info dynamic creator not set.
            AssertFieldNull("_typeInfoCreationFunc", JsonContext.Default.Options);

            static void AssertFieldNull(string fieldName, JsonSerializerOptions? optionsInstance)
            {
                BindingFlags bindingFlags = BindingFlags.NonPublic | (optionsInstance == null ? BindingFlags.Static : BindingFlags.Instance);
                FieldInfo fieldInfo = typeof(JsonSerializerOptions).GetField(fieldName, bindingFlags);
                Assert.NotNull(fieldInfo);
                Assert.Null(fieldInfo.GetValue(optionsInstance));
            }
        }

        private const string ExceptionMessageFromCustomContext = "Exception thrown from custom context.";

        [Fact]
        public static void GetTypeInfoCalledDuringPolymorphicSerialization()
        {
            CustomContext context = new(new JsonSerializerOptions());

            // Empty array is fine since we don't need metadata for children.
            Assert.Equal("[]", JsonSerializer.Serialize(Array.Empty<object>(), context.ObjectArray));
            Assert.Equal("[]", JsonSerializer.Serialize(Array.Empty<object>(), typeof(object[]), context));

            // GetTypeInfo method called to get metadata for element run-time type.
            object[] objArr = new object[] { new MyStruct() };

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(objArr, context.ObjectArray));
            Assert.Contains(ExceptionMessageFromCustomContext, ex.ToString());

            ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(objArr, typeof(object[]), context));
            Assert.Contains(ExceptionMessageFromCustomContext, ex.ToString());
        }

        internal struct MyStruct { }

        internal class CustomContext : JsonSerializerContext
        {
            public CustomContext(JsonSerializerOptions options) : base(options) { }

            private JsonTypeInfo<object> _object;
            public JsonTypeInfo<object> Object => _object ??= JsonMetadataServices.CreateValueInfo<object>(Options, JsonMetadataServices.ObjectConverter);

            private JsonTypeInfo<object[]> _objectArray;
            public JsonTypeInfo<object[]> ObjectArray => _objectArray ??= JsonMetadataServices.CreateArrayInfo<object>(Options, Object, default);

            public override JsonTypeInfo GetTypeInfo(Type type)
            {
                if (type == typeof(object[]))
                {
                    return ObjectArray;
                }

                throw new InvalidOperationException(ExceptionMessageFromCustomContext);
            }
        }
    }
}
