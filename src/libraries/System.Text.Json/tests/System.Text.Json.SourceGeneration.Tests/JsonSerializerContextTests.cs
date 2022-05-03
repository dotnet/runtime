// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public static partial class JsonSerializerContextTests
    {
        [Fact]
        public static void VariousNestingAndVisibilityLevelsAreSupported()
        {
            Assert.NotNull(PublicContext.Default);
            Assert.NotNull(NestedContext.Default);
            Assert.NotNull(NestedPublicContext.Default);
            Assert.NotNull(NestedPublicContext.NestedProtectedInternalClass.Default);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/63802", TargetFrameworkMonikers.NetFramework)]
        public static void Converters_AndTypeInfoCreator_NotRooted_WhenMetadataNotPresent()
        {
            RemoteExecutor.Invoke(
                static () =>
                {
                    object[] objArr = new object[] { new MyStruct() };

                    // Metadata not generated for MyStruct without JsonSerializableAttribute.
                    NotSupportedException ex = Assert.Throws<NotSupportedException>(
                        () => JsonSerializer.Serialize(objArr, MetadataContext.Default.ObjectArray));
                    string exAsStr = ex.ToString();
                    Assert.Contains(typeof(MyStruct).ToString(), exAsStr);
                    Assert.Contains("JsonSerializerOptions", exAsStr);

                    // This test uses reflection to:
                    // - Access JsonSerializerOptions.s_defaultSimpleConverters
                    // - Access JsonSerializerOptions.s_defaultFactoryConverters
                    // - Access JsonSerializerOptions.s_typeInfoCreationFunc
                    //
                    // If any of them changes, this test will need to be kept in sync.

                    // Confirm built-in converters not set.
                    AssertFieldNull("s_defaultSimpleConverters", optionsInstance: null);
                    AssertFieldNull("s_defaultFactoryConverters", optionsInstance: null);

                    // Confirm type info dynamic creator not set.
                    AssertFieldNull("s_typeInfoCreationFunc", optionsInstance: null);

                    static void AssertFieldNull(string fieldName, JsonSerializerOptions? optionsInstance)
                    {
                        BindingFlags bindingFlags = BindingFlags.NonPublic | (optionsInstance == null ? BindingFlags.Static : BindingFlags.Instance);
                        FieldInfo fieldInfo = typeof(JsonSerializerOptions).GetField(fieldName, bindingFlags);
                        Assert.NotNull(fieldInfo);
                        Assert.Null(fieldInfo.GetValue(optionsInstance));
                    }
                }).Dispose();
        }

        [Fact]
        public static void SupportsPositionalRecords()
        {
            Person person = new(FirstName: "Jane", LastName: "Doe");

            byte[] utf8Json = JsonSerializer.SerializeToUtf8Bytes(person, PersonJsonContext.Default.Person);

            person = JsonSerializer.Deserialize<Person>(utf8Json, PersonJsonContext.Default.Person);
            Assert.Equal("Jane", person.FirstName);
            Assert.Equal("Doe", person.LastName);
        }

        [JsonSerializable(typeof(JsonMessage))]
        internal partial class NestedContext : JsonSerializerContext { }

        [JsonSerializable(typeof(JsonMessage))]
        public partial class NestedPublicContext : JsonSerializerContext
        {
            [JsonSerializable(typeof(JsonMessage))]
            protected internal partial class NestedProtectedInternalClass : JsonSerializerContext { }
        }

        internal record Person(string FirstName, string LastName);

        [JsonSourceGenerationOptions(
            PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
        [JsonSerializable(typeof(Person))]
        internal partial class PersonJsonContext : JsonSerializerContext
        {
        }

        // Regression test for https://github.com/dotnet/runtime/issues/62079
        [Fact]
        public static void SupportsPropertiesWithCustomConverterFactory()
        {
            var value = new ClassWithCustomConverterFactoryProperty { MyEnum = Serialization.Tests.SampleEnum.MinZero };
            string json = JsonSerializer.Serialize(value, SingleClassWithCustomConverterFactoryPropertyContext.Default.ClassWithCustomConverterFactoryProperty);
            Assert.Equal(@"{""MyEnum"":""MinZero""}", json);
        }

        public class ParentClass
        {
            public ClassWithCustomConverterFactoryProperty? Child { get; set; }
        }

        [JsonSerializable(typeof(ParentClass))]
        internal partial class SingleClassWithCustomConverterFactoryPropertyContext : JsonSerializerContext
        {
        }

        // Regression test for https://github.com/dotnet/runtime/issues/61860
        [Fact]
        public static void SupportsGenericParameterWithCustomConverterFactory()
        {
            var value = new List<TestEnum> { TestEnum.Cee };
            string json = JsonSerializer.Serialize(value, GenericParameterWithCustomConverterFactoryContext.Default.ListTestEnum);
            Assert.Equal(@"[""Cee""]", json);
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum TestEnum
        {
            Aye, Bee, Cee
        }

        [JsonSerializable(typeof(List<TestEnum>))]
        internal partial class GenericParameterWithCustomConverterFactoryContext : JsonSerializerContext
        {
        }
    }
}
