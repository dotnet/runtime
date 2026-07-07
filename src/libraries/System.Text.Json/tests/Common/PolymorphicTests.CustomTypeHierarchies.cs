// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class PolymorphicTests
    {
        private readonly JsonSerializerOptions s_optionsWithAllowOutOfOrderMetadata = new() { AllowOutOfOrderMetadataProperties = true };

        #region Polymorphic Class
        [Theory]
        [MemberData(nameof(Get_PolymorphicClass_TestData_Serialization))]
        public Task PolymorphicClass_TestData_Serialization(PolymorphicClass.TestData testData)
            => TestMultiContextSerialization(testData.Value, testData.ExpectedJson, testData.ExpectedSerializationException, contexts: ~SerializedValueContext.BoxedValue);

        public static IEnumerable<object[]> Get_PolymorphicClass_TestData_Serialization()
            => PolymorphicClass.GetSerializeTestData().Select(entry => new object[] { entry });

        [Theory]
        [MemberData(nameof(Get_PolymorphicClass_TestData_Deserialization))]
        public Task PolymorphicClass_TestData_Deserialization(PolymorphicClass.TestData testData)
            => TestMultiContextDeserialization<PolymorphicClass>(
                testData.ExpectedJson,
                testData.ExpectedRoundtripValue,
                testData.ExpectedDeserializationException,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicClass>());

        public static IEnumerable<object[]> Get_PolymorphicClass_TestData_Deserialization()
            => PolymorphicClass.GetSerializeTestData().Where(entry => entry.ExpectedJson != null).Select(entry => new object[] { entry });

        [Fact]
        public async Task PolymorphicClass_TestDataArray_Serialization()
        {
            IEnumerable<(PolymorphicClass Value, string ExpectedJson)> inputs =
                PolymorphicClass.GetSerializeTestData()
                    .Where(entry => entry.ExpectedSerializationException is null)
                    .Select(entry => (entry.Value, entry.ExpectedJson));

            await TestMultiContextSerialization(inputs);
        }

        [Fact]
        public async Task PolymorphicClass_TestDataArray_Deserialization()
        {
            IEnumerable<(string ExpectedJson, PolymorphicClass ExpectedRoundtripValue)> inputs =
                PolymorphicClass.GetSerializeTestData()
                    .Where(entry => entry.ExpectedRoundtripValue is not null)
                    .Select(entry => (entry.ExpectedJson, entry.ExpectedRoundtripValue));

            await TestMultiContextDeserialization(inputs, equalityComparer: CreateJsonEqualityComparer<PolymorphicClass>());
        }

        [Theory]
        [InlineData("$['$type']", """{ "$type" : "derivedClass1", "$type" : "derivedClass1", "Number" : 42 }""")]
        [InlineData("$['$type']", """{ "$type" : "derivedClass1", "Number" : 42, "$type" : "derivedClass1"}""")]
        [InlineData("$['$id']", """{ "$type" : "derivedClass1", "Number" : 42, "$id" : "referenceId"}""")]
        [InlineData("$['$id']", """{ "$type" : "derivedClass1", "" : 42, "$id" : "referenceId"}""")]
        [InlineData("$['$values']", """{ "Number" : 42, "$values" : [] }""")]
        [InlineData("$['$type']", """{ "Number" : 42, "$type" : "derivedClass" }""")]
        [InlineData("$", """{ "$type" : "invalidDiscriminator", "Number" : 42 }""")]
        [InlineData("$", """{ "$type" : 0, "Number" : 42 }""")]
        [InlineData("$['$type']", """{ "$type" : false, "Number" : 42 }""")]
        [InlineData("$['$type']", """{ "$type" : {}, "Number" : 42 }""")]
        [InlineData("$['$type']", """{ "$type" : [], "Number" : 42 }""")]
        [InlineData("$['$id']", """{ "$id" : "1", "Number" : 42 }""")]
        [InlineData("$['$ref']", """{ "$ref" : "1" }""")]
        public async Task PolymorphicClass_InvalidTypeDiscriminatorMetadata_ShouldThrowJsonException(string expectedJsonPath, string json)
        {
            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<PolymorphicClass>(json));
            Assert.Equal(expectedJsonPath, exception.Path);
        }

        [Theory]
        [MemberData(nameof(Get_PolymorphicClass_TestData_Deserialization))]
        public Task PolymorphicClass_TestData_AllowOutOfOrderMetadata_Deserialization(PolymorphicClass.TestData testData)
            => TestMultiContextDeserialization<PolymorphicClass>(
                testData.ExpectedJson,
                testData.ExpectedRoundtripValue,
                testData.ExpectedDeserializationException,
                options: s_optionsWithAllowOutOfOrderMetadata,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicClass>());

        [Theory]
        [InlineData("""{"Number":42, "$type":"derivedClass1", "String": "str"}""", typeof(PolymorphicClass.DerivedClass1_TypeDiscriminator))]
        [InlineData("""{"Number":42, "String": "str", "$type":"derivedClass1"}""", typeof(PolymorphicClass.DerivedClass1_TypeDiscriminator))]
        [InlineData("""{"Number":42, "$type": -1, "String": "str" }""", typeof(PolymorphicClass.DerivedClass_IntegerTypeDiscriminator))]
        [InlineData("""{"Number":42, "\u0024type": -1, "String": "str" }""", typeof(PolymorphicClass.DerivedClass_IntegerTypeDiscriminator))]
        [InlineData("""{"Number":42, "String": "str", "$type": -1 }""", typeof(PolymorphicClass.DerivedClass_IntegerTypeDiscriminator))]
        [InlineData("""{"$values": [42,42,42], "$type":"derivedCollection"}""", typeof(PolymorphicClass.DerivedCollection_TypeDiscriminator))]
        [InlineData("""{"$values": [42,42,42], "$type":"derivedCollectionOfDerivedCollection"}""", typeof(PolymorphicClass.DerivedCollection_TypeDiscriminator.DerivedClass))]
        [InlineData("""{"dictionaryKey" : 42, "$type":"derivedDictionary"}""", typeof(PolymorphicClass.DerivedDictionary_TypeDiscriminator))]
        [InlineData("""{"dictionaryKey" : 42, "$type":"derivedDictionaryOfDerivedDictionary"}""", typeof(PolymorphicClass.DerivedDictionary_TypeDiscriminator.DerivedClass))]
        [InlineData("""{"Number":42, "$type":"derivedClassWithCtor"}""", typeof(PolymorphicClass.DerivedClassWithConstructor_TypeDiscriminator))]
        [InlineData("""{"Number":42, "$type":"derivedClassOfDerivedClassWithCtor"}""", typeof(PolymorphicClass.DerivedClassWithConstructor_TypeDiscriminator.DerivedClass))]
        public async Task PolymorphicClass_AllowOutOfOrderMetadata_AcceptsOutOfOrderInputs(string json, Type expectedResultType)
        {
            PolymorphicClass? result = await Serializer.DeserializeWrapper<PolymorphicClass>(json, s_optionsWithAllowOutOfOrderMetadata);
            Assert.IsType(expectedResultType, result);

            if (result is IEnumerable collection)
            {
                Assert.NotEmpty(collection);
            }
        }

        [Theory]
        [InlineData("$['$type']", """{"Number":42, "$type":"derivedClass1", "String": "str", "$type":"derivedClass1"}""")]
        [InlineData("$['$type']", """{"$type":"derivedCollection", "$values": [42,42,42], "$type":"derivedCollection"}""")]
        [InlineData("$['$values']", """{"$type":"derivedCollection", "NonMetadataProp": {}, "$values": [42,42,42]}""")]
        [InlineData("$.NonMetadataProp", """{"$type":"derivedCollection", "$values": [42,42,42], "NonMetadataProp": {}}""")]
        [InlineData("$.NonMetadataProp", """{"$values": [42,42,42], "$type":"derivedCollection", "NonMetadataProp": {}}""")]
        [InlineData("$['$values']", """{"$type":"derivedCollection", "$values": [42,42,42], "$values": [42,42,42]}""")]
        public async Task PolymorphicClass_AllowOutOfOrderMetadata_RejectsInvalidInputs(string expectedJsonPath, string json)
        {
            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<PolymorphicClass>(json, s_optionsWithAllowOutOfOrderMetadata));
            Assert.Equal(expectedJsonPath, exception.Path);
        }

        //--

        [Theory]
        [MemberData(nameof(Get_PolymorphicClass_CustomConfigWithBaseTypeFallback_TestData_Serialization))]
        public Task PolymorphicClass_CustomConfigWithBaseTypeFallback_TestData_Serialization(PolymorphicClass.TestData testData)
            => TestMultiContextSerialization(
                testData.Value,
                testData.ExpectedJson,
                testData.ExpectedSerializationException,
                options: PolymorphicClass.CustomConfigWithBaseTypeFallback,
                contexts: ~SerializedValueContext.BoxedValue);

        public static IEnumerable<object[]> Get_PolymorphicClass_CustomConfigWithBaseTypeFallback_TestData_Serialization()
            => PolymorphicClass.GetSerializeTestData_CustomConfigWithBaseTypeFallback().Select(entry => new object[] { entry });

        [Theory]
        [MemberData(nameof(Get_PolymorphicClass_CustomConfigWithBaseTypeFallback_TestData_Deserialization))]
        public Task PolymorphicClass_CustomConfigWithBaseTypeFallback_TestData_Deserialization(PolymorphicClass.TestData testData)
            => TestMultiContextDeserialization<PolymorphicClass>(
                testData.ExpectedJson,
                testData.ExpectedRoundtripValue,
                testData.ExpectedSerializationException,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicClass>(),
                options: PolymorphicClass.CustomConfigWithBaseTypeFallback);

        public static IEnumerable<object[]> Get_PolymorphicClass_CustomConfigWithBaseTypeFallback_TestData_Deserialization()
            => PolymorphicClass.GetSerializeTestData_CustomConfigWithBaseTypeFallback()
                .Where(entry => entry.ExpectedJson != null)
                .Select(entry => new object[] { entry });

        [Fact]
        public async Task PolymorphicClass_CustomConfigWithBaseTypeFallback_TestDataArray_Serialization()
        {
            IEnumerable<(PolymorphicClass Value, string ExpectedJson)> inputs =
                PolymorphicClass.GetSerializeTestData_CustomConfigWithBaseTypeFallback()
                    .Where(entry => entry.ExpectedSerializationException is null)
                    .Select(entry => (entry.Value, entry.ExpectedJson));

            await TestMultiContextSerialization(inputs, options: PolymorphicClass.CustomConfigWithBaseTypeFallback);
        }

        [Fact]
        public async Task PolymorphicClass_CustomConfigWithBaseTypeFallbacks_TestDataArray_Deserialization()
        {
            IEnumerable<(string ExpectedJson, PolymorphicClass ExpectedRoundtripValue)> inputs =
                PolymorphicClass.GetSerializeTestData_CustomConfigWithBaseTypeFallback()
                .Where(entry => entry.ExpectedRoundtripValue is not null)
                .Select(entry => (entry.ExpectedJson, entry.ExpectedRoundtripValue));

            await TestMultiContextDeserialization(
                inputs,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicClass>(),
                options: PolymorphicClass.CustomConfigWithBaseTypeFallback);
        }

        [Theory]
        [InlineData("$['$type']", """{ "$type" : "derivedClass1", "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : "derivedClass1", "_case" : "derivedClass1", "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : "derivedClass1", "Number" : 42, "_case" : "derivedClass1"}""")]
        [InlineData("$['$type']", """{ "_case" : "derivedClass1", "Number" : 42, "$type" : "derivedClass1"}""")]
        [InlineData("$['$id']", """{ "_case" : "derivedClass1", "Number" : 42, "$id" : "referenceId"}""")]
        [InlineData("$['$id']", """{ "_case" : "derivedClass1", "" : 42, "$id" : "referenceId"}""")]
        [InlineData("$['$values']", """{ "_case" : "derivedClass1", "Number" : 42, "$values" : [] }""")]
        [InlineData("$._case", """{ "Number" : 42, "_case" : "derivedClass1" }""")]
        [InlineData("$", """{ "_case" : "invalidDiscriminator", "Number" : 42 }""")]
        [InlineData("$", """{ "_case" : 0, "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : false, "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : {}, "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : [], "Number" : 42 }""")]
        [InlineData("$['$id']", """{ "$id" : "1", "Number" : 42 }""")]
        [InlineData("$['$ref']", """{ "$ref" : "1" }""")]
        public async Task PolymorphicClass_CustomConfigWithBaseTypeFallback_InvalidTypeDiscriminatorMetadata_ShouldThrowJsonException(string expectedJsonPath, string json)
        {
            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<PolymorphicClass>(json, PolymorphicClass.CustomConfigWithBaseTypeFallback));
            Assert.Equal(expectedJsonPath, exception.Path);
        }

        //---

        [Theory]
        [MemberData(nameof(Get_PolymorphicClass_CustomConfigWithNearestAncestorFallback_TestData_Serialization))]
        public Task PolymorphicClass_CustomConfigWithNearestAncestorFallback_TestData_Serialization(PolymorphicClass.TestData testData)
            => TestMultiContextSerialization(
                testData.Value,
                testData.ExpectedJson,
                testData.ExpectedSerializationException,
                options: PolymorphicClass.CustomConfigWithNearestAncestorFallback,
                contexts: ~SerializedValueContext.BoxedValue);

        public static IEnumerable<object[]> Get_PolymorphicClass_CustomConfigWithNearestAncestorFallback_TestData_Serialization()
            => PolymorphicClass.GetSerializeTestData_CustomConfigWithNearestAncestorFallback().Select(entry => new object[] { entry });

        [Theory]
        [MemberData(nameof(Get_PolymorphicClass_CustomConfigWithNearestAncestorFallback_TestData_Deserialization))]
        public Task PolymorphicClass_CustomConfigWithNearestAncestorFallback_TestData_Deserialization(PolymorphicClass.TestData testData)
            => TestMultiContextDeserialization<PolymorphicClass>(
                testData.ExpectedJson,
                testData.ExpectedRoundtripValue,
                testData.ExpectedDeserializationException,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicClass>(),
                options: PolymorphicClass.CustomConfigWithNearestAncestorFallback);

        public static IEnumerable<object[]> Get_PolymorphicClass_CustomConfigWithNearestAncestorFallback_TestData_Deserialization()
            => PolymorphicClass.GetSerializeTestData_CustomConfigWithNearestAncestorFallback()
                    .Where(entry => entry.ExpectedJson != null)
                    .Select(entry => new object[] { entry });

        [Fact]
        public async Task PolymorphicClass_CustomConfigWithNearestAncestorFallback_TestDataArray_Serialization()
        {
            IEnumerable<(PolymorphicClass Value, string ExpectedJson)> inputs =
                PolymorphicClass.GetSerializeTestData_CustomConfigWithNearestAncestorFallback()
                    .Where(entry => entry.ExpectedSerializationException is null)
                    .Select(entry => (entry.Value, entry.ExpectedJson));

            await TestMultiContextSerialization(inputs, options: PolymorphicClass.CustomConfigWithNearestAncestorFallback);
        }

        [Fact]
        public async Task PolymorphicClass_CustomConfigWithNearestAncestorFallback_TestDataArray_Deserialization()
        {
            IEnumerable<(string ExpectedJson, PolymorphicClass ExpectedRoundtripValue)> inputs =
                PolymorphicClass.GetSerializeTestData_CustomConfigWithNearestAncestorFallback()
                .Where(entry => entry.ExpectedRoundtripValue is not null)
                .Select(entry => (entry.ExpectedJson, entry.ExpectedRoundtripValue));

            await TestMultiContextDeserialization(
                inputs,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicClass>(),
                options: PolymorphicClass.CustomConfigWithNearestAncestorFallback);
        }

        [Theory]
        [InlineData("$['$type']", """{ "$type" : "derivedClass1", "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : "derivedClass1", "_case" : "derivedClass1", "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : "derivedClass1", "Number" : 42, "_case" : "derivedClass1"}""")]
        [InlineData("$['$type']", """{ "_case" : "derivedClass1", "Number" : 42, "$type" : "derivedClass1"}""")]
        [InlineData("$['$id']", """{ "_case" : "derivedClass1", "Number" : 42, "$id" : "referenceId"}""")]
        [InlineData("$['$id']", """{ "_case" : "derivedClass1", "" : 42, "$id" : "referenceId"}""")]
        [InlineData("$['$values']", """{ "_case" : "derivedClass1", "Number" : 42, "$values" : [] }""")]
        [InlineData("$._case", """{ "Number" : 42, "_case" : "derivedClass1" }""")]
        [InlineData("$", """{ "_case" : "invalidDiscriminator", "Number" : 42 }""")]
        [InlineData("$", """{ "_case" : 0, "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : false, "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : {}, "Number" : 42 }""")]
        [InlineData("$._case", """{ "_case" : [], "Number" : 42 }""")]
        [InlineData("$['$id']", """{ "$id" : "1", "Number" : 42 }""")]
        [InlineData("$['$ref']", """{ "$ref" : "1" }""")]
        public async Task PolymorphicClass_CustomConfigWithNearestAncestorFallback_InvalidTypeDiscriminatorMetadata_ShouldThrowJsonException(string expectedJsonPath, string json)
        {
            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<PolymorphicClass>(json, PolymorphicClass.CustomConfigWithBaseTypeFallback));
            Assert.Equal(expectedJsonPath, exception.Path);
        }

        [Theory]
        [InlineData(JsonUnknownDerivedTypeHandling.FailSerialization)]
        [InlineData(JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
        public async Task PolymorphicClass_ConfigWithAbstractClass_ShouldThrowNotSupportedException(JsonUnknownDerivedTypeHandling jsonUnknownDerivedTypeHandling)
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new CustomPolymorphismResolver<PolymorphicClass>
                {
                    UnknownDerivedTypeHandling = jsonUnknownDerivedTypeHandling,
                }
                .WithDerivedType<PolymorphicClass.DerivedAbstractClass>()
            };

            PolymorphicClass value = new PolymorphicClass.DerivedAbstractClass.DerivedClass();
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(value));
        }

        [Fact]
        public async Task PolymorphicClass_ClearPolymorphismOptions_DoesNotUsePolymorphism()
        {
            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(static jsonTypeInfo =>
            {
                if (jsonTypeInfo.Type == typeof(PolymorphicClass))
                {
                    jsonTypeInfo.PolymorphismOptions = null;
                }
            });

            PolymorphicClass value = new PolymorphicClass.DerivedAbstractClass.DerivedClass { Number = 42, Boolean = true };
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("""{"Number":42}""", json);
        }

        [JsonDerivedType(typeof(DerivedClass1_NoTypeDiscriminator))]
        [JsonDerivedType(typeof(DerivedClass1_NoTypeDiscriminator.DerivedClass))]
        [JsonDerivedType(typeof(DerivedClass1_TypeDiscriminator), "derivedClass1")]
        [JsonDerivedType(typeof(DerivedClass1_TypeDiscriminator.DerivedClass), "derivedClassOfDerivedClass1")]
        [JsonDerivedType(typeof(DerivedClass2_NoTypeDiscriminator))]
        [JsonDerivedType(typeof(DerivedClass2_TypeDiscriminator), "derivedClass2")]
        [JsonDerivedType(typeof(DerivedClass_IntegerTypeDiscriminator), typeDiscriminator: -1)]
        [JsonDerivedType(typeof(DerivedCollection_NoTypeDiscriminator))]
        [JsonDerivedType(typeof(DerivedCollection_TypeDiscriminator), "derivedCollection")]
        [JsonDerivedType(typeof(DerivedCollection_TypeDiscriminator.DerivedClass), "derivedCollectionOfDerivedCollection")]
        [JsonDerivedType(typeof(DerivedDictionary_NoTypeDiscriminator))]
        [JsonDerivedType(typeof(DerivedDictionary_TypeDiscriminator), "derivedDictionary")]
        [JsonDerivedType(typeof(DerivedDictionary_TypeDiscriminator.DerivedClass), "derivedDictionaryOfDerivedDictionary")]
        [JsonDerivedType(typeof(DerivedClassWithConstructor_TypeDiscriminator), "derivedClassWithCtor")]
        [JsonDerivedType(typeof(DerivedClassWithConstructor_TypeDiscriminator.DerivedClass), "derivedClassOfDerivedClassWithCtor")]
        [JsonDerivedType(typeof(DerivedClassWithCustomConverter_NoTypeDiscriminator))]
        [JsonDerivedType(typeof(DerivedClassWithCustomConverter_TypeDiscriminator), "derivedClassWithCustomConverter")]
        public class PolymorphicClass
        {
            public int Number { get; set; }

            public class DerivedClass1_NoTypeDiscriminator : PolymorphicClass
            {
                public string String { get; set; }

                public class DerivedClass : DerivedClass1_NoTypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            // Ensure derived class polymorphic configuration is not inherited by base class polymorphic configuration
            [JsonPolymorphic(TypeDiscriminatorPropertyName = "$case")]
            [JsonDerivedType(typeof(DerivedClass), "derivedClassOfDerivedClass1")]
            public class DerivedClass1_TypeDiscriminator : PolymorphicClass
            {
                public string String { get; set; }

                [JsonExtensionData]
                public Dictionary<string, object>? ExtensionData { get; set; }

                public class DerivedClass : DerivedClass1_TypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public class DerivedClass2_NoTypeDiscriminator : PolymorphicClass
            {
                public bool Boolean { get; set; }

                public class DerivedClass : DerivedClass2_NoTypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public class DerivedClass2_TypeDiscriminator : PolymorphicClass
            {
                public bool Boolean { get; set; }

                public class DerivedClass : DerivedClass2_TypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public class DerivedClass_IntegerTypeDiscriminator : PolymorphicClass
            {
                public string String { get; set; }
            }

            public abstract class DerivedAbstractClass : PolymorphicClass
            {
                public abstract bool Boolean { get; set; }

                public class DerivedClass : DerivedAbstractClass
                {
                    public override bool Boolean { get; set; }
                }
            }

            public class DerivedCollection_NoTypeDiscriminator : PolymorphicClass, ICollection<int>
            {
                // Minimal ICollection implementation meant to enable collection deserialization
                bool ICollection<int>.IsReadOnly => false;
                void ICollection<int>.Add(int item) => Number = item;
                public IEnumerator<int> GetEnumerator() => Enumerable.Repeat(Number, 3).GetEnumerator();

                int ICollection<int>.Count => throw new NotImplementedException();
                void ICollection<int>.Clear() => throw new NotImplementedException();
                bool ICollection<int>.Contains(int item) => throw new NotImplementedException();
                void ICollection<int>.CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                bool ICollection<int>.Remove(int item) => throw new NotImplementedException();

                public class DerivedClass : DerivedCollection_NoTypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public class DerivedCollection_TypeDiscriminator : PolymorphicClass, ICollection<int>
            {
                // Minimal ICollection implementation meant to enable collection deserialization
                bool ICollection<int>.IsReadOnly => false;
                void ICollection<int>.Add(int item) => Number = item;
                public IEnumerator<int> GetEnumerator() => Enumerable.Repeat(Number, 3).GetEnumerator();

                int ICollection<int>.Count => throw new NotImplementedException();
                void ICollection<int>.Clear() => throw new NotImplementedException();
                bool ICollection<int>.Contains(int item) => throw new NotImplementedException();
                void ICollection<int>.CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                bool ICollection<int>.Remove(int item) => throw new NotImplementedException();

                public class DerivedClass : DerivedCollection_TypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public class DerivedDictionary_NoTypeDiscriminator : PolymorphicClass, IDictionary<string, int>
            {
                // Minimal IDictionary implementation meant to enable serialization
                bool ICollection<KeyValuePair<string, int>>.IsReadOnly => false;
                public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => Enumerable.Repeat(new KeyValuePair<string, int>("dictionaryKey", Number), 1).GetEnumerator();
                int IDictionary<string, int>.this[string key] { get => throw new NotImplementedException(); set { if (key == "dictionaryKey") Number = value; } }

                void IDictionary<string, int>.Add(string key, int value) => throw new NotImplementedException();
                ICollection<string> IDictionary<string, int>.Keys => throw new NotImplementedException();
                ICollection<int> IDictionary<string, int>.Values => throw new NotImplementedException();
                int ICollection<KeyValuePair<string, int>>.Count => throw new NotImplementedException();
                void ICollection<KeyValuePair<string, int>>.Add(KeyValuePair<string, int> item) => throw new NotImplementedException();
                void ICollection<KeyValuePair<string, int>>.Clear() => throw new NotImplementedException();
                bool ICollection<KeyValuePair<string, int>>.Contains(KeyValuePair<string, int> item) => throw new NotImplementedException();
                bool IDictionary<string, int>.ContainsKey(string key) => throw new NotImplementedException();
                void ICollection<KeyValuePair<string, int>>.CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
                bool IDictionary<string, int>.Remove(string key) => throw new NotImplementedException();
                bool ICollection<KeyValuePair<string, int>>.Remove(KeyValuePair<string, int> item) => throw new NotImplementedException();
                bool IDictionary<string, int>.TryGetValue(string key, out int value) => throw new NotImplementedException();

                public class DerivedClass : DerivedDictionary_NoTypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public class DerivedDictionary_TypeDiscriminator : PolymorphicClass, IDictionary<string, int>
            {
                // Minimal IDictionary implementation meant to enable serialization
                bool ICollection<KeyValuePair<string, int>>.IsReadOnly => false;
                public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => Enumerable.Repeat(new KeyValuePair<string, int>("dictionaryKey", Number), 1).GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                int IDictionary<string, int>.this[string key] { get => throw new NotImplementedException(); set { if (key == "dictionaryKey") Number = value; } }

                void IDictionary<string, int>.Add(string key, int value) => throw new NotImplementedException();
                ICollection<string> IDictionary<string, int>.Keys => throw new NotImplementedException();
                ICollection<int> IDictionary<string, int>.Values => throw new NotImplementedException();
                int ICollection<KeyValuePair<string, int>>.Count => throw new NotImplementedException();
                void ICollection<KeyValuePair<string, int>>.Add(KeyValuePair<string, int> item) => throw new NotImplementedException();
                void ICollection<KeyValuePair<string, int>>.Clear() => throw new NotImplementedException();
                bool ICollection<KeyValuePair<string, int>>.Contains(KeyValuePair<string, int> item) => throw new NotImplementedException();
                bool IDictionary<string, int>.ContainsKey(string key) => throw new NotImplementedException();
                void ICollection<KeyValuePair<string, int>>.CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => throw new NotImplementedException();
                bool IDictionary<string, int>.Remove(string key) => throw new NotImplementedException();
                bool ICollection<KeyValuePair<string, int>>.Remove(KeyValuePair<string, int> item) => throw new NotImplementedException();
                bool IDictionary<string, int>.TryGetValue(string key, out int value) => throw new NotImplementedException();

                public class DerivedClass : DerivedDictionary_TypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public class DerivedClassWithConstructor_TypeDiscriminator : PolymorphicClass
            {
                [JsonConstructor]
                public DerivedClassWithConstructor_TypeDiscriminator(int number)
                {
                    Number = number;
                }

                public class DerivedClass : DerivedClassWithConstructor_TypeDiscriminator
                {
                    [JsonConstructor]
                    public DerivedClass(int number, string extraProperty)
                        : base(number)
                    {
                        ExtraProperty = extraProperty;
                    }

                    public string ExtraProperty { get; set; }
                }
            }

            [JsonConverter(typeof(CustomConverter))]
            public class DerivedClassWithCustomConverter_NoTypeDiscriminator : PolymorphicClass
            {
                public class CustomConverter : JsonConverter<DerivedClassWithCustomConverter_NoTypeDiscriminator>
                {
                    public override DerivedClassWithCustomConverter_NoTypeDiscriminator Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                        => throw new NotSupportedException();

                    public override void Write(Utf8JsonWriter writer, DerivedClassWithCustomConverter_NoTypeDiscriminator value, JsonSerializerOptions options)
                        => writer.WriteNumberValue(value.Number);
                }

                public class DerivedClass : DerivedClassWithCustomConverter_NoTypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            [JsonConverter(typeof(CustomConverter))]
            public class DerivedClassWithCustomConverter_TypeDiscriminator : PolymorphicClass
            {
                public class CustomConverter : JsonConverter<DerivedClassWithCustomConverter_TypeDiscriminator>
                {
                    public override DerivedClassWithCustomConverter_TypeDiscriminator Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                        => throw new NotSupportedException();

                    public override void Write(Utf8JsonWriter writer, DerivedClassWithCustomConverter_TypeDiscriminator value, JsonSerializerOptions options)
                        => writer.WriteNumberValue(value.Number);
                }

                public class DerivedClass : DerivedClassWithCustomConverter_TypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public static IEnumerable<TestData> GetSerializeTestData()
            {
                yield return new TestData(
                    Value: new PolymorphicClass { Number = 42 },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: """{ "Number" : 42, "String" : "str" }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator.DerivedClass(),
                    ExpectedSerializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: """{ "Number" : 42, "String" : "str", "ExtraProperty" : "extra" }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: """{ "$type" : "derivedClass1", "Number" : 42, "String" : "str" }""",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: """{ "$type" : "derivedClassOfDerivedClass1", "Number" : 42, "String" : "str", "ExtraProperty" : "extra" }""",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: """{ "Number" : 42, "Boolean" : true }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator.DerivedClass(),
                    ExpectedSerializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: """{ "$type" : "derivedClass2", "Number" : 42, "Boolean" : true }""",
                    ExpectedRoundtripValue: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true });

                yield return new TestData(
                    Value: new DerivedClass_IntegerTypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: """{ "$type" : -1, "Number" : 42, "String" : "str" }""",
                    ExpectedRoundtripValue: new DerivedClass_IntegerTypeDiscriminator { Number = 42, String = "str" });

                yield return new TestData(
                    Value: new DerivedCollection_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: "[42,42,42]",
                    ExpectedDeserializationException: typeof(JsonException));

                yield return new TestData(
                    Value: new DerivedCollection_NoTypeDiscriminator.DerivedClass(),
                    ExpectedSerializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "$type" : "derivedCollection", "$values" : [42,42,42] }""",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: """{ "$type" : "derivedCollectionOfDerivedCollection", "$values" : [42,42,42] }""",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator.DerivedClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "dictionaryKey" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass());

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "$type":"derivedDictionary", "dictionaryKey" : 42 }""",
                    ExpectedRoundtripValue: new DerivedDictionary_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: """{ "$type" : "derivedDictionaryOfDerivedDictionary", "dictionaryKey" : 42 }""",
                    ExpectedRoundtripValue: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator(42),
                    ExpectedJson: """{ "$type" : "derivedClassWithCtor", "Number" : 42 }""",
                    ExpectedRoundtripValue: new DerivedClassWithConstructor_TypeDiscriminator(42));

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"),
                    ExpectedJson: """{ "$type" : "derivedClassOfDerivedClassWithCtor", "Number" : 42, "ExtraProperty" : "extra" }""",
                    ExpectedRoundtripValue: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"));

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: "42",
                    ExpectedDeserializationException: typeof(JsonException));

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_NoTypeDiscriminator.DerivedClass(),
                    ExpectedSerializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_TypeDiscriminator(), // TODO special unit test for type discriminators with custom converters
                    ExpectedSerializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_TypeDiscriminator.DerivedClass(),
                    ExpectedSerializationException: typeof(NotSupportedException));
            }

            public static JsonSerializerOptions CustomConfigWithBaseTypeFallback { get; } =
                new JsonSerializerOptions
                {
                    TypeInfoResolver = new CustomPolymorphismResolver<PolymorphicClass>
                    {
                        TypeDiscriminatorPropertyName = "_case",
                        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType
                    }
                    .WithDerivedType<DerivedClass1_NoTypeDiscriminator>()
                    .WithDerivedType<DerivedClass1_TypeDiscriminator>("derivedClass1")
                    .WithDerivedType<DerivedClass1_TypeDiscriminator.DerivedClass>("derivedClassOfDerivedClass1")
                    .WithDerivedType<DerivedClass2_TypeDiscriminator>("derivedClass2")
                    .WithDerivedType<DerivedCollection_TypeDiscriminator>("derivedCollection")
                    .WithDerivedType<DerivedDictionary_NoTypeDiscriminator>()
                    .WithDerivedType<DerivedDictionary_TypeDiscriminator.DerivedClass>("derivedDictionaryOfDerivedDictionary")
                    .WithDerivedType<DerivedClassWithConstructor_TypeDiscriminator.DerivedClass>("derivedClassOfDerivedClassWithCtor")
                    .WithDerivedType<DerivedClassWithCustomConverter_TypeDiscriminator>("derivedClassWithCustomConverter")
                };

            public static IEnumerable<TestData> GetSerializeTestData_CustomConfigWithBaseTypeFallback()
            {
                yield return new TestData(
                    Value: new PolymorphicClass { Number = 42 },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: """{ "Number" : 42, "String" : "str" }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: """{ "_case" : "derivedClass1", "Number" : 42, "String" : "str" }""",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: """{ "_case" : "derivedClassOfDerivedClass1", "Number" : 42, "String" : "str", "ExtraProperty" : "extra" }""",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator.DerivedClass { Number = 42, Boolean = true },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: """{ "_case" : "derivedClass2", "Number" : 42, "Boolean" : true }""",
                    ExpectedRoundtripValue: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator.DerivedClass { Number = 42, Boolean = true, ExtraProperty = "extra" },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "_case" : "derivedCollection", "$values" : [42,42,42] }""",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "dictionaryKey" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass());

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: """{ "_case" : "derivedDictionaryOfDerivedDictionary", "dictionaryKey" : 42 }""",
                    ExpectedRoundtripValue: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator(42),
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"),
                    ExpectedJson: """{ "_case" : "derivedClassOfDerivedClassWithCtor", "Number" : 42, "ExtraProperty" : "extra" }""",
                    ExpectedRoundtripValue: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"));

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_TypeDiscriminator(),
                    ExpectedSerializationException: typeof(NotSupportedException));
            }

            public static JsonSerializerOptions CustomConfigWithNearestAncestorFallback { get; } =
                new JsonSerializerOptions
                {
                    TypeInfoResolver = new CustomPolymorphismResolver<PolymorphicClass>
                    {
                        TypeDiscriminatorPropertyName = "_case",
                        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
                    }
                    .WithDerivedType<DerivedClass1_NoTypeDiscriminator>()
                    .WithDerivedType<DerivedClass1_TypeDiscriminator>("derivedClass1")
                    .WithDerivedType<DerivedClass1_TypeDiscriminator.DerivedClass>("derivedClassOfDerivedClass1")
                    .WithDerivedType<DerivedClass2_TypeDiscriminator>("derivedClass2")
                    .WithDerivedType<DerivedAbstractClass>("derivedAbstractClass")
                    .WithDerivedType<DerivedCollection_TypeDiscriminator>("derivedCollection")
                    .WithDerivedType<DerivedDictionary_NoTypeDiscriminator>()
                    .WithDerivedType<DerivedDictionary_TypeDiscriminator.DerivedClass>("derivedDictionaryOfDerivedDictionary")
                    .WithDerivedType<DerivedClassWithConstructor_TypeDiscriminator.DerivedClass>("derivedClassOfDerivedClassWithCtor")
                    .WithDerivedType<DerivedClassWithCustomConverter_TypeDiscriminator>("derivedClassWithCustomConverter")
                };

            public static IEnumerable<TestData> GetSerializeTestData_CustomConfigWithNearestAncestorFallback()
            {
                yield return new TestData(
                    Value: new PolymorphicClass { Number = 42 },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: """{ "Number" : 42, "String" : "str" }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: """{ "Number" : 42, "String" : "str" }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: """{ "_case" : "derivedClass1", "Number" : 42, "String" : "str" }""",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: """{ "_case" : "derivedClassOfDerivedClass1", "Number" : 42, "String" : "str", "ExtraProperty" : "extra" }""",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator.DerivedClass { Number = 42, Boolean = true },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: """{ "_case" : "derivedClass2", "Number" : 42, "Boolean" : true }""",
                    ExpectedRoundtripValue: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator.DerivedClass { Number = 42, Boolean = true, ExtraProperty = "extra" },
                    ExpectedJson: """{ "_case" : "derivedClass2", "Number" : 42, "Boolean" : true }""",
                    ExpectedRoundtripValue: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true });

                yield return new TestData(
                    Value: new DerivedAbstractClass.DerivedClass { Number = 42, Boolean = true },
                    ExpectedJson: """{ "_case" : "derivedAbstractClass", "Number" : 42, "Boolean" : true }""",
                    ExpectedDeserializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedCollection_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "_case" : "derivedCollection", "$values" : [42,42,42] }""",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: """{ "_case" : "derivedCollection", "$values" : [42,42,42] }""",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "dictionaryKey" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass());

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: """{ "_case" : "derivedDictionaryOfDerivedDictionary", "dictionaryKey" : 42 }""",
                    ExpectedRoundtripValue: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator(42),
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"),
                    ExpectedJson: """{ "_case" : "derivedClassOfDerivedClassWithCtor", "Number" : 42, "ExtraProperty" : "extra" }""",
                    ExpectedRoundtripValue: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"));

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_TypeDiscriminator(),
                    ExpectedSerializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_TypeDiscriminator.DerivedClass(),
                    ExpectedSerializationException: typeof(NotSupportedException));
            }

            public record TestData(
                PolymorphicClass Value,
                string? ExpectedJson = null,
                Type? ExpectedSerializationException = null,
                PolymorphicClass? ExpectedRoundtripValue = null,
                Type? ExpectedDeserializationException = null);
        }

        [Fact]
        public async Task PolymorphicClass_NoTypeDiscriminators_Deserialization_IgnoresTypeMetadata()
        {
            string json = """{"$type" : "derivedClass"}""";
            PolymorphicClass_NoTypeDiscriminators result = await Serializer.DeserializeWrapper<PolymorphicClass_NoTypeDiscriminators>(json);
            Assert.IsType<PolymorphicClass_NoTypeDiscriminators>(result);
            Assert.True(result.ExtensionData?.ContainsKey("$type") == true);
        }

        [JsonDerivedType(typeof(DerivedClass1))]
        [JsonDerivedType(typeof(DerivedClass2))]
        public class PolymorphicClass_NoTypeDiscriminators
        {
            [JsonExtensionData]
            public Dictionary<string, object?>? ExtensionData { get; set; }

            public class DerivedClass1 : PolymorphicClass_NoTypeDiscriminators { }
            public class DerivedClass2 : PolymorphicClass_NoTypeDiscriminators { }
        }

        [Fact]
        public async Task PolymorphicClass_WithDerivedPolymorphicClass_Serialization_ShouldUseBaseTypeContract()
        {
            string expectedJson = """{"$type":"derivedClass"}""";
            PolymorphicClass_WithDerivedPolymorphicClass value = new PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass();
            await TestMultiContextSerialization(value, expectedJson, contexts: ~SerializedValueContext.BoxedValue);
        }

        [Fact]
        public async Task PolymorphicClass_WithDerivedPolymorphicClass_Deserialization_ShouldUseBaseTypeContract()
        {
            string json = """{"$type":"derivedClass"}""";

            var expectedValueUsingBaseContract = new PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass();
            await TestMultiContextDeserialization<PolymorphicClass_WithDerivedPolymorphicClass>(
                json,
                expectedValueUsingBaseContract,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicClass_WithDerivedPolymorphicClass>());

            var expectedValueUsingDerivedContract = new PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass.DerivedClass2();
            await TestMultiContextDeserialization<PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass>(
                json,
                expectedValueUsingDerivedContract,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass>());
        }

        [JsonDerivedType(typeof(DerivedClass), "derivedClass")]
        [JsonDerivedType(typeof(DerivedClass.DerivedClass2), "derivedClass2")]
        public class PolymorphicClass_WithDerivedPolymorphicClass
        {
            // Derived class with conflicting configuration
            [JsonDerivedType(typeof(DerivedClass), "baseClass")]
            [JsonDerivedType(typeof(DerivedClass2), "derivedClass")]
            public class DerivedClass : PolymorphicClass_WithDerivedPolymorphicClass
            {
                public class DerivedClass2 : DerivedClass
                {
                }
            }
        }

        [Theory]
        [MemberData(nameof(PolymorphicClass_WithBaseTypeDiscriminator.GetTestData), MemberType = typeof(PolymorphicClass_WithBaseTypeDiscriminator))]
        public async Task PolymorphicClass_BoxedSerialization_UsesTypeDiscriminators(PolymorphicClass_WithBaseTypeDiscriminator value, string expectedJson)
        {
            await TestMultiContextSerialization<object>(value, expectedJson);
        }

        [JsonDerivedType(typeof(PolymorphicClass_WithBaseTypeDiscriminator), "baseType")]
        [JsonDerivedType(typeof(DerivedClass), "derivedType")]
        public class PolymorphicClass_WithBaseTypeDiscriminator
        {
            public int Number { get; set; }

            public class DerivedClass : PolymorphicClass_WithBaseTypeDiscriminator
            {
                public string String { get; set; }
            }

            public static IEnumerable<object[]> GetTestData()
            {
                yield return WrapArgs(new PolymorphicClass_WithBaseTypeDiscriminator { Number = 42 }, """{ "$type" : "baseType", "Number" : 42 }""");
                yield return WrapArgs(new DerivedClass { Number = 42, String = "str" }, """{ "$type" : "derivedType", "Number" : 42, "String" : "str" }""");

                static object[] WrapArgs(PolymorphicClass_WithBaseTypeDiscriminator value, string expectedJson)
                    => new object[] { value, expectedJson };
            }
        }

        [Fact]
        public async Task NestedPolymorphicClassesIncreaseReadAndWriteStackWhenNeeded()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/71994
            TestNode obj = new TestNodeList
            {
                Name = "testName",
                Info = "1",
                List = new List<TestNode>
                {
                    new TestLeaf
                    {
                        Name = "testName2"
                    },

                    new TestNodeList
                    {
                        Name = "testName3",
                        Info = "2",
                        List = new List<TestNode>
                        {
                            new TestNodeList
                            {
                                Name = "testName4",
                                Info = "1"
                            }
                        }
                    }
                }
            };

            string json = await Serializer.SerializeWrapper(obj);
            JsonTestHelper.AssertJsonEqual(
                """
                        {
                                           "$type": "NodeList",
                                           "Info": "1",
                                           "List": [
                                             {
                                               "$type": "Leaf",
                                               "Test": null,
                                               "Name": "testName2"
                                             },
                                             {
                                               "$type": "NodeList",
                                               "Info": "2",
                                               "List": [
                                                 {
                                                   "$type": "NodeList",
                                                   "Info": "1",
                                                   "List": null,
                                                   "Name": "testName4"
                                                 }
                                               ],
                                               "Name": "testName3"
                                             }
                                           ],
                                           "Name": "testName"}
                    """, json);

            TestNode deserialized = await Serializer.DeserializeWrapper<TestNode>(json);
            obj.AssertEqualTo(deserialized);
        }

        [JsonDerivedType(typeof(TestNodeList), "NodeList")]
        [JsonDerivedType(typeof(TestLeaf), "Leaf")]
        public abstract class TestNode
        {
            public string Name { get; set; }

            public abstract void AssertEqualTo(TestNode other);
        }

        public class TestNodeList : TestNode
        {
            public string Info { get; set; }

            public IEnumerable<TestNode>? List { get; set; }

            public override void AssertEqualTo(TestNode other)
            {
                Assert.Equal(Name, other.Name);
                Assert.IsType<TestNodeList>(other);
                TestNodeList typedOther = (TestNodeList)other;
                Assert.Equal(Info, typedOther.Info);
                Assert.Equal(List is null, typedOther.List is null);

                if (List is not null)
                {
                    using IEnumerator<TestNode> thisEnumerator = List.GetEnumerator();
                    using IEnumerator<TestNode> otherEnumerator = typedOther.List.GetEnumerator();

                    while (true)
                    {
                        bool hasNext = thisEnumerator.MoveNext();
                        Assert.Equal(hasNext, otherEnumerator.MoveNext());

                        if (!hasNext)
                            break;

                        thisEnumerator.Current.AssertEqualTo(otherEnumerator.Current);
                    }
                }
            }
        }

        public class TestLeaf : TestNode
        {
            public string? Test { get; set; }

            public override void AssertEqualTo(TestNode other)
            {
                Assert.Equal(Name, other.Name);
                Assert.IsType<TestLeaf>(other);
                TestLeaf typedOther = (TestLeaf)other;
                Assert.Equal(Test, typedOther.Test);
            }
        }
        #endregion

        #region Polymorphic Class with Constructor

        [Theory]
        [MemberData(nameof(Get_PolymorphicClassWithConstructor_TestData_Serialization))]
        public Task PolymorphicClassWithConstructor_TestData_Serialization(PolymorphicClassWithConstructor.TestData testData)
            => TestMultiContextSerialization(testData.Value, testData.ExpectedJson);

        public static IEnumerable<object[]> Get_PolymorphicClassWithConstructor_TestData_Serialization()
            => PolymorphicClassWithConstructor.GetSerializeTestData().Select(entry => new object[] { entry });

        [Theory]
        [MemberData(nameof(Get_PolymorphicClassWithConstructor_TestData_Deserialization))]
        public Task PolymorphicClassWithConstructor_TestData_Deserialization(PolymorphicClassWithConstructor.TestData testData)
            => TestMultiContextDeserialization<PolymorphicClassWithConstructor>(
                testData.ExpectedJson,
                testData.ExpectedRoundtripValue,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicClassWithConstructor>());

        public static IEnumerable<object[]> Get_PolymorphicClassWithConstructor_TestData_Deserialization()
            => PolymorphicClassWithConstructor.GetSerializeTestData()
                .Where(entry => entry.ExpectedJson != null)
                .Select(entry => new object[] { entry });

        [Fact]
        public async Task PolymorphicClassWithConstructor_TestDataArray_Serialization()
        {
            IEnumerable<(PolymorphicClassWithConstructor Value, string ExpectedJson)> inputs =
                PolymorphicClassWithConstructor.GetSerializeTestData().Select(entry => (entry.Value, entry.ExpectedJson));

            await TestMultiContextSerialization(inputs);
        }

        [Fact]
        public async Task PolymorphicClassWithConstructor_TestDataArray_Deserialization()
        {
            IEnumerable<(string ExpectedJson, PolymorphicClassWithConstructor ExpectedRoundtripValue)> inputs =
                PolymorphicClassWithConstructor.GetSerializeTestData().Select(entry => (entry.ExpectedJson, entry.ExpectedRoundtripValue));

            await TestMultiContextDeserialization(inputs, equalityComparer: CreateJsonEqualityComparer<PolymorphicClassWithConstructor>());
        }

        [JsonPolymorphic]
        [JsonDerivedType(typeof(DerivedClass), "derivedClass")]
        [JsonDerivedType(typeof(DerivedClassWithConstructor), "derivedClassWithCtor")]
        [JsonDerivedType(typeof(DerivedCollection), "derivedCollection")]
        [JsonDerivedType(typeof(DerivedDictionary), "derivedDictionary")]
        public class PolymorphicClassWithConstructor
        {
            [JsonConstructor]
            public PolymorphicClassWithConstructor(int number) => Number = number;

            public int Number { get; }

            public class DerivedClass : PolymorphicClassWithConstructor
            {
                public DerivedClass() : base(0) { }
                public string String { get; set; }
            }

            public class DerivedClassWithConstructor : PolymorphicClassWithConstructor
            {
                [JsonConstructor]
                public DerivedClassWithConstructor(int number, bool boolean)
                    : base(number)
                {
                    Boolean = boolean;
                }

                public bool Boolean { get; }
            }

            public class DerivedCollection : PolymorphicClassWithConstructor, ICollection<int>
            {
                private List<int> _list = new();

                public DerivedCollection() : base(0)
                {
                }

                bool ICollection<int>.IsReadOnly => false;
                public void Add(int item) => _list.Add(item);
                IEnumerator<int> IEnumerable<int>.GetEnumerator() => _list.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                int ICollection<int>.Count => _list.Count;
                void ICollection<int>.Clear() => throw new NotImplementedException();
                bool ICollection<int>.Contains(int item) => throw new NotImplementedException();
                void ICollection<int>.CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();
                bool ICollection<int>.Remove(int item) => throw new NotImplementedException();
            }

            public class DerivedDictionary : PolymorphicClassWithConstructor, IDictionary<string, int>
            {
                private Dictionary<string, int> _dict = new();

                public DerivedDictionary() : base(0)
                {
                }

                public int this[string key] { get => _dict[key]; set => _dict[key] = value; }
                bool ICollection<KeyValuePair<string, int>>.IsReadOnly => false;
                IEnumerator<KeyValuePair<string, int>> IEnumerable<KeyValuePair<string, int>>.GetEnumerator() => _dict.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();


                ICollection<string> IDictionary<string, int>.Keys => throw new NotImplementedException();
                ICollection<int> IDictionary<string, int>.Values => throw new NotImplementedException();
                int ICollection<KeyValuePair<string, int>>.Count => throw new NotImplementedException();
                void IDictionary<string, int>.Add(string key, int value) => throw new NotImplementedException();
                void ICollection<KeyValuePair<string, int>>.Add(KeyValuePair<string, int> item) => throw new NotImplementedException();
                void ICollection<KeyValuePair<string, int>>.Clear() => throw new NotImplementedException();
                bool ICollection<KeyValuePair<string, int>>.Contains(KeyValuePair<string, int> item) => throw new NotImplementedException();
                bool IDictionary<string, int>.ContainsKey(string key) => throw new NotImplementedException();
                void ICollection<KeyValuePair<string, int>>.CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => throw new NotImplementedException();
                bool IDictionary<string, int>.Remove(string key) => throw new NotImplementedException();
                bool ICollection<KeyValuePair<string, int>>.Remove(KeyValuePair<string, int> item) => throw new NotImplementedException();
                bool IDictionary<string, int>.TryGetValue(string key, out int value) => throw new NotImplementedException();
            }

            public static IEnumerable<TestData> GetSerializeTestData()
            {
                yield return new TestData(
                    Value: new PolymorphicClassWithConstructor(42),
                    ExpectedJson: """{ "Number" : 42 }""",
                    ExpectedRoundtripValue: new PolymorphicClassWithConstructor(42));

                yield return new TestData(
                    Value: new DerivedClass { String = "str" },
                    ExpectedJson: """{ "$type" : "derivedClass", "Number" : 0, "String" : "str" }""",
                    ExpectedRoundtripValue: new DerivedClass { String = "str" });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor(42, true),
                    ExpectedJson: """{ "$type" : "derivedClassWithCtor", "Number" : 42, "Boolean" : true }""",
                    ExpectedRoundtripValue: new DerivedClassWithConstructor(42, true));

                yield return new TestData(
                    Value: new DerivedCollection { 1, 2, 3 },
                    ExpectedJson: """{ "$type" : "derivedCollection", "$values" : [1,2,3]}""",
                    ExpectedRoundtripValue: new DerivedCollection { 1, 2, 3 });

                yield return new TestData(
                    Value: new DerivedDictionary { ["key1"] = 42, ["key2"] = -1 },
                    ExpectedJson: """{ "$type" : "derivedDictionary", "key1" : 42, "key2" : -1 }""",
                    ExpectedRoundtripValue: new DerivedDictionary { ["key1"] = 42, ["key2"] = -1 });
            }

            public record TestData(PolymorphicClassWithConstructor Value, string ExpectedJson, PolymorphicClassWithConstructor ExpectedRoundtripValue);
        }

        #endregion

        #region Polymorphic Interface
        [Theory]
        [MemberData(nameof(Get_PolymorphicInterface_TestData_Serialization))]
        public Task PolymorphicInterface_TestData_Serialization(PolymorphicInterface.TestData testData)
            => TestMultiContextSerialization(testData.Value, testData.ExpectedJson, testData.ExpectedSerializationException);

        public static IEnumerable<object[]> Get_PolymorphicInterface_TestData_Serialization()
            => PolymorphicInterface.Helpers.GetSerializeTestData().Select(entry => new object[] { entry });

        [Theory]
        [MemberData(nameof(Get_PolymorphicInterface_TestData_Deserialization))]
        public Task PolymorphicInterface_TestData_Deserialization(PolymorphicInterface.TestData testData)
            => TestMultiContextDeserialization<PolymorphicInterface>(
                testData.ExpectedJson,
                testData.ExpectedRoundtripValue,
                testData.ExpectedDeserializationException,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicInterface>());

        public static IEnumerable<object[]> Get_PolymorphicInterface_TestData_Deserialization()
            => PolymorphicInterface.Helpers.GetSerializeTestData()
                .Where(entry => entry.ExpectedJson != null)
                .Select(entry => new object[] { entry });

        [Fact]
        public async Task PolymorphicInterface_TestDataArray_Serialization()
        {
            IEnumerable<(PolymorphicInterface Value, string ExpectedJson)> inputs =
                PolymorphicInterface.Helpers.GetSerializeTestData()
                    .Where(entry => entry.ExpectedSerializationException is null)
                    .Select(entry => (entry.Value, entry.ExpectedJson));

            await TestMultiContextSerialization(inputs);
        }

        [Fact]
        public async Task PolymorphicInterface_TestDataArray_Deserialization()
        {
            IEnumerable<(string ExpectedJson, PolymorphicInterface ExpectedRoundtripValue)> inputs =
                PolymorphicInterface.Helpers.GetSerializeTestData()
                .Where(entry => entry.ExpectedRoundtripValue is not null)
                .Select(entry => (entry.ExpectedJson, entry.ExpectedRoundtripValue));

            await TestMultiContextDeserialization(inputs, equalityComparer: CreateJsonEqualityComparer<PolymorphicInterface>());
        }

        [Theory]
        [InlineData("$['$type']", """{ "$type" : "derivedClass", "$type" : "derivedClass", "Number" : 42 }""")]
        [InlineData("$['$type']", """{ "$type" : "derivedClass", "Number" : 42, "$type" : "derivedClass"}""")]
        [InlineData("$['$id']", """{ "$type" : "derivedClass", "Number" : 42, "$id" : "referenceId"}""")]
        [InlineData("$['$values']", """{ "$type" : "derivedClass", "Number" : 42, "$values" : [] }""")]
        [InlineData("$", """{ "$type" : "invalidDiscriminator", "Number" : 42 }""")]
        [InlineData("$", """{ "$type" : 0, "Number" : 42 }""")]
        [InlineData("$['$type']", """{ "$type" : false, "Number" : 42 }""")]
        [InlineData("$['$type']", """{ "$type" : {}, "Number" : 42 }""")]
        [InlineData("$['$type']", """{ "$type" : [], "Number" : 42 }""")]
        [InlineData("$['$id']", """{ "$id" : "1", "Number" : 42 }""")]
        [InlineData("$['$ref']", """{ "$ref" : "1" }""")]
        public async Task PolymorphicInterface_InvalidTypeDiscriminatorMetadata_ShouldThrowJsonException(string expectedJsonPath, string json)
        {
            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<PolymorphicInterface>(json, PolymorphicClass.CustomConfigWithBaseTypeFallback));
            Assert.Equal(expectedJsonPath, exception.Path);
        }

        // --

        [Theory]
        [MemberData(nameof(Get_PolymorphicInterface_CustomConfigWithNearestAncestorFallback_TestData_Serialization))]
        public Task PolymorphicInterface_CustomConfigWithNearestAncestorFallback_TestData_Serialization(PolymorphicInterface.TestData testData)
            => TestMultiContextSerialization(
                testData.Value,
                testData.ExpectedJson,
                testData.ExpectedSerializationException,
                options: PolymorphicInterface.Helpers.CustomConfigWithNearestAncestorFallback);

        public static IEnumerable<object[]> Get_PolymorphicInterface_CustomConfigWithNearestAncestorFallback_TestData_Serialization()
            => PolymorphicInterface.Helpers.GetSerializeTestData_CustomConfigWithNearestAncestorFallback().Select(entry => new object[] { entry });

        [Theory]
        [MemberData(nameof(Get_PolymorphicInterface_CustomConfigWithNearestAncestorFallback_TestData_Deserialization))]
        public Task PolymorphicInterface_CustomConfigWithNearestAncestorFallback_TestData_Deserialization(PolymorphicInterface.TestData testData)
            => TestMultiContextDeserialization<PolymorphicInterface>(
                testData.ExpectedJson,
                testData.ExpectedRoundtripValue,
                testData.ExpectedDeserializationException,
                options: PolymorphicInterface.Helpers.CustomConfigWithNearestAncestorFallback,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicInterface>());

        public static IEnumerable<object[]> Get_PolymorphicInterface_CustomConfigWithNearestAncestorFallback_TestData_Deserialization()
            => PolymorphicInterface.Helpers.GetSerializeTestData_CustomConfigWithNearestAncestorFallback()
                .Where(entry => entry.ExpectedJson != null)
                .Select(entry => new object[] { entry });

        [Fact]
        public async Task PolymorphicInterface_CustomConfigWithNearestAncestorFallback_TestDataArray_Serialization()
        {
            IEnumerable<(PolymorphicInterface Value, string ExpectedJson)> inputs =
                PolymorphicInterface.Helpers.GetSerializeTestData_CustomConfigWithNearestAncestorFallback()
                    .Where(entry => entry.ExpectedSerializationException is null)
                    .Select(entry => (entry.Value, entry.ExpectedJson));

            await TestMultiContextSerialization(inputs, options: PolymorphicInterface.Helpers.CustomConfigWithNearestAncestorFallback);
        }

        [Fact]
        public async Task PolymorphicInterface_CustomConfigWithNearestAncestorFallback_TestDataArray_Deserialization()
        {
            IEnumerable<(string ExpectedJson, PolymorphicInterface ExpectedRoundtripValue)> inputs =
                PolymorphicInterface.Helpers.GetSerializeTestData_CustomConfigWithNearestAncestorFallback()
                .Where(entry => entry.ExpectedRoundtripValue is not null)
                .Select(entry => (entry.ExpectedJson, entry.ExpectedRoundtripValue));

            await TestMultiContextDeserialization(
                inputs,
                options: PolymorphicInterface.Helpers.CustomConfigWithNearestAncestorFallback,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicInterface>());
        }

        // --

        [Theory]
        [MemberData(nameof(Get_PolymorphicInterface_DiamondInducingConfigurations_ShouldThrowNotSupportedException))]
        public async Task PolymorphicInterface_DiamondInducingConfigurations_ShouldThrowNotSupportedException(PolymorphicInterface value, CustomPolymorphismResolver configuration)
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = configuration };
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(value, options));
        }

        public static IEnumerable<object[]> Get_PolymorphicInterface_DiamondInducingConfigurations_ShouldThrowNotSupportedException()
            => PolymorphicInterface.Helpers.GetDiamondInducingConfigurations().Select(entry => new object[] { entry.diamondValue, entry.configuration });


        [JsonDerivedType(typeof(DerivedClass_NoTypeDiscriminator))]
        [JsonDerivedType(typeof(DerivedClass_TypeDiscriminator), "derivedClass")]
        [JsonDerivedType(typeof(DerivedStruct_NoTypeDiscriminator))]
        [JsonDerivedType(typeof(DerivedStruct_TypeDiscriminator), "derivedStruct")]
        [JsonDerivedType(typeof(DerivedInterface1.ImplementingClass), "implementingClassOfDerivedInterface")]
        public interface PolymorphicInterface
        {
            public int Number { get; set; }

            public class DerivedClass_NoTypeDiscriminator : PolymorphicInterface
            {
                public int Number { get; set; }
                public string String { get; set; }

                public class DerivedClass : DerivedClass_NoTypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public class DerivedClass_TypeDiscriminator : PolymorphicInterface
            {
                public int Number { get; set; }
                public string String { get; set; }

                public class DerivedClass : DerivedClass_TypeDiscriminator
                {
                    public string ExtraProperty { get; set; }
                }
            }

            public struct DerivedStruct_NoTypeDiscriminator : PolymorphicInterface
            {
                public int Number { get; set; }
                public string String { get; set; }
            }

            public struct DerivedStruct_TypeDiscriminator : PolymorphicInterface
            {
                public int Number { get; set; }
                public string String { get; set; }
            }

            public interface DerivedInterface1 : PolymorphicInterface
            {
                public string String { get; set; }

                public class ImplementingClass : DerivedInterface1
                {
                    public int Number { get; set; }
                    public string String { get; set; }
                }
            }

            public interface DerivedInterface2 : PolymorphicInterface
            {
                public bool Boolean { get; set; }
            }

            public class DiamondKind1 : DerivedInterface1, DerivedInterface2
            {
                public int Number { get; set; }
                public string String { get; set; }
                public bool Boolean { get; set; }
            }

            public class DiamondKind2 : DerivedClass_TypeDiscriminator, DerivedInterface1
            {
                public bool Boolean { get; set; }
            }

            public static class Helpers
            {
                public static IEnumerable<TestData> GetSerializeTestData()
                {
                    yield return new TestData(
                        Value: new DerivedClass_NoTypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: """{ "Number" : 42, "String" : "str" }""",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        new DerivedClass_NoTypeDiscriminator.DerivedClass(),
                        ExpectedSerializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: """{ "$type" : "derivedClass", "Number" : 42, "String" : "str" }""",
                        ExpectedRoundtripValue: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" });

                    yield return new TestData(
                        new DerivedClass_TypeDiscriminator.DerivedClass(),
                        ExpectedSerializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedStruct_NoTypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: """{ "Number" : 42, "String" : "str" }""",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedStruct_TypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: """{ "$type" : "derivedStruct", "Number" : 42, "String" : "str" }""",
                        ExpectedRoundtripValue: new DerivedStruct_TypeDiscriminator { Number = 42, String = "str" });
                }

                public static JsonSerializerOptions CustomConfigWithNearestAncestorFallback { get; } =
                    new JsonSerializerOptions
                    {
                        TypeInfoResolver = new CustomPolymorphismResolver<PolymorphicInterface>
                        {
                            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
                        }
                        .WithDerivedType<DerivedClass_TypeDiscriminator>("derivedClass")
                        .WithDerivedType<DerivedStruct_NoTypeDiscriminator>()
                        .WithDerivedType<DerivedInterface1>()
                        .WithDerivedType<DerivedInterface2>()
                    };

                public static IEnumerable<TestData> GetSerializeTestData_CustomConfigWithNearestAncestorFallback()
                {
                    yield return new TestData(
                        Value: new DerivedClass_NoTypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: """{ "Number" : 42 }""",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        new DerivedClass_NoTypeDiscriminator.DerivedClass { Number = 42 },
                        ExpectedJson: """{ "Number" : 42 }""",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: """{ "$type" : "derivedClass", "Number" : 42, "String" : "str" }""",
                        ExpectedRoundtripValue: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" });

                    yield return new TestData(
                        new DerivedClass_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                        ExpectedJson: """{ "$type" : "derivedClass", "Number" : 42, "String" : "str" }""",
                        ExpectedRoundtripValue: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" });

                    yield return new TestData(
                        Value: new DerivedStruct_NoTypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: """{ "Number" : 42, "String" : "str" }""",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedStruct_TypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: """{ "Number" : 42 }""",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DiamondKind1(),
                        ExpectedSerializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DiamondKind2(),
                        ExpectedSerializationException: typeof(NotSupportedException));

                }

                public static IEnumerable<(PolymorphicInterface diamondValue, CustomPolymorphismResolver configuration)> GetDiamondInducingConfigurations()
                {
                    yield return (
                        new DiamondKind1(),
                        new CustomPolymorphismResolver<PolymorphicInterface> { UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor }
                            .WithDerivedType<DerivedInterface1>()
                            .WithDerivedType<DerivedInterface2>());

                    yield return (
                        new DiamondKind2(),
                        new CustomPolymorphismResolver<PolymorphicInterface> { UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor }
                            .WithDerivedType<DerivedInterface1>()
                            .WithDerivedType<DerivedClass_TypeDiscriminator>());
                }
            }

            public record TestData(
                PolymorphicInterface Value = null,
                string? ExpectedJson = null,
                PolymorphicInterface? ExpectedRoundtripValue = null,
                Type? ExpectedSerializationException = null,
                Type? ExpectedDeserializationException = null);
        }
        #endregion

        #region Polymorphic List

        [Theory]
        [MemberData(nameof(Get_PolymorphicList_TestData_Serialization))]
        public Task PolymorphicList_TestData_Serialization(PolymorphicList.TestData testData)
            => TestMultiContextSerialization(testData.Value, testData.ExpectedJson);

        public static IEnumerable<object[]> Get_PolymorphicList_TestData_Serialization()
            => PolymorphicList.GetSerializeTestData().Select(entry => new object[] { entry });

        [Theory]
        [MemberData(nameof(Get_PolymorphicList_TestData_Serialization))]
        public Task PolymorphicList_TestData_Deserialization(PolymorphicList.TestData testData)
            => TestMultiContextDeserialization(
                testData.ExpectedJson,
                testData.ExpectedRoundtripValue,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicList>());

        public static IEnumerable<object[]> Get_PolymorphicList_TestData_Deserialization()
            => PolymorphicList.GetSerializeTestData().Select(entry => new object[] { entry });

        [Fact]
        public async Task PolymorphicList_TestDataArray_Serialization()
        {
            IEnumerable<(PolymorphicList Value, string ExpectedJson)> inputs =
                PolymorphicList.GetSerializeTestData().Select(entry => (entry.Value, entry.ExpectedJson));

            await TestMultiContextSerialization(inputs);
        }

        [Fact]
        public async Task PolymorphicList_TestDataArray_Deserialization()
        {
            IEnumerable<(string ExpectedJson, PolymorphicList ExpectedRoundtripValue)> inputs =
                PolymorphicList.GetSerializeTestData().Select(entry => (entry.ExpectedJson, entry.ExpectedRoundtripValue));

            await TestMultiContextDeserialization(inputs, equalityComparer: CreateJsonEqualityComparer<PolymorphicList>());
        }

        [Fact]
        public async Task PolymorphicList_UnrecognizedTypeDiscriminators_ShouldSucceedDeserialization()
        {
            string json = """{ "$type" : "invalidTypeDiscriminator", "$values" : [42,42,42] }""";
            PolymorphicList result = await Serializer.DeserializeWrapper<PolymorphicList>(json);
            Assert.IsType<PolymorphicList>(result);
            Assert.Equal(Enumerable.Repeat(42, 3), result);
        }

        [Theory]
        [InlineData("$.UnsupportedProperty", """{ "$type" : "derivedList", "UnsupportedProperty" : 42 }""")]
        [InlineData("$.UnsupportedProperty", """{ "$type" : "derivedList", "$values" : [], "UnsupportedProperty" : 42 }""")]
        [InlineData("$['$id']", """{ "$id" : 42, "$values" : [] }""")]
        [InlineData("$['$ref']", """{ "$ref" : 42 }""")]
        public async Task PolymorphicList_InvalidTypeDiscriminatorMetadata_ShouldThrowJsonException(string expectedJsonPath, string json)
        {
            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<PolymorphicList>(json));
            Assert.Equal(expectedJsonPath, exception.Path);
        }

        [JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor, IgnoreUnrecognizedTypeDiscriminators = true)]
        [JsonDerivedType(typeof(PolymorphicList), "baseList")]
        [JsonDerivedType(typeof(DerivedList1), "derivedList")]
        public class PolymorphicList : List<int>
        {
            public class DerivedList1 : PolymorphicList
            {
            }

            public class DerivedList2 : PolymorphicList
            {
            }

            public static IEnumerable<TestData> GetSerializeTestData()
            {
                yield return new TestData(
                    Value: new PolymorphicList { 42 },
                    ExpectedJson: """{ "$type" : "baseList", "$values" : [42]}""",
                    ExpectedRoundtripValue:  new PolymorphicList { 42 });

                yield return new TestData(
                    Value: new DerivedList1 { 42 },
                    ExpectedJson: """{ "$type" : "derivedList", "$values" : [42]}""",
                    ExpectedRoundtripValue: new DerivedList1 { 42 });

                yield return new TestData(
                    Value: new DerivedList2 { 42 },
                    ExpectedJson: """{ "$type" : "baseList", "$values" : [42]}""",
                    ExpectedRoundtripValue: new PolymorphicList { 42 });
            }

            public record TestData(PolymorphicList Value, string ExpectedJson, PolymorphicList ExpectedRoundtripValue);
        }

        [Fact]
        public async Task PolymorphicCollectionInterface_Serialization()
        {
            var source = new int[] { 1, 2, 3 };
            var values = new IEnumerable<int>[]
            {
                source,
                new List<int>(source),
                new Queue<int>(source),
                new HashSet<int>(source)
            };

            string expectedJson =
                """
                        [ [1,2,3],
                                            { "$type":"list" , "$values":[1,2,3] },
                                            { "$type":"queue", "$values":[1,2,3] },
                                            { "$type":"set"  , "$values":[1,2,3] }]
                    """;

            string actualJson = await Serializer.SerializeWrapper(values, s_optionsWithPolymorphicCollectionInterface);

            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Fact]
        public async Task PolymorphicCollectionInterface_Deserialization()
        {
            var source = new int[] { 1, 2, 3 };
            var expectedValues = new IEnumerable<int>[]
            {
                new List<int>(source),
                new List<int>(source),
                new Queue<int>(source),
                new HashSet<int>(source)
            };

            string json =
                """
                        [ [1,2,3],
                                            { "$type":"list" , "$values":[1,2,3] },
                                            { "$type":"queue", "$values":[1,2,3] },
                                            { "$type":"set"  , "$values":[1,2,3] }]
                    """;

            var actualValues = await Serializer.DeserializeWrapper<IEnumerable<int>[]>(json, s_optionsWithPolymorphicCollectionInterface);
            Assert.Equal(expectedValues.Length, actualValues.Length);
            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], actualValues[i]);
                Assert.IsType(expectedValues[i].GetType(), actualValues[i]);
            }
        }

        private readonly static JsonSerializerOptions s_optionsWithPolymorphicCollectionInterface = new JsonSerializerOptions
        {
            TypeInfoResolver = new CustomPolymorphismResolver<IEnumerable<int>>
            {
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
            }
            .WithDerivedType<List<int>>("list")
            .WithDerivedType<Queue<int>>("queue")
            .WithDerivedType<ISet<int>>("set")
        };

        [Fact]
        public async Task PolymorphicClassWithDerivedCollection_Collection_RoundtripsAsExpected()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/85934

            PolymorphicClassWithDerivedCollections value = new PolymorphicClassWithDerivedCollections.List
            {
                new PolymorphicClassWithDerivedCollections.List(),
                new PolymorphicClassWithDerivedCollections.Dictionary(),
            };

            string expectedJson = """{"$type":"list","$values":[{"$type":"list","$values":[]},{"$type":"dictionary"}]}""";

            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal(expectedJson, json);

            PolymorphicClassWithDerivedCollections deserializedValue = await Serializer.DeserializeWrapper<PolymorphicClassWithDerivedCollections>(json);

            PolymorphicClassWithDerivedCollections.List list = Assert.IsType<PolymorphicClassWithDerivedCollections.List>(deserializedValue);
            Assert.Equal(2, list.Count);
            Assert.IsType<PolymorphicClassWithDerivedCollections.List>(list[0]);
            Assert.IsType<PolymorphicClassWithDerivedCollections.Dictionary>(list[1]);
        }

        [Fact]
        public async Task PolymorphicClassWithDerivedCollection_Dictionary_RoundtripsAsExpected()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/85934

            PolymorphicClassWithDerivedCollections value = new PolymorphicClassWithDerivedCollections.Dictionary
            {
                ["key1"] = new PolymorphicClassWithDerivedCollections.Dictionary(),
                ["key2"] = new PolymorphicClassWithDerivedCollections.List(),
            };

            string expectedJson = """{"$type":"dictionary","key1":{"$type":"dictionary"},"key2":{"$type":"list","$values":[]}}""";

            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal(expectedJson, json);

            PolymorphicClassWithDerivedCollections deserializedValue = await Serializer.DeserializeWrapper<PolymorphicClassWithDerivedCollections>(json);
            PolymorphicClassWithDerivedCollections.Dictionary dictionary = Assert.IsType<PolymorphicClassWithDerivedCollections.Dictionary>(deserializedValue);
            Assert.Equal(2, dictionary.Count);
            Assert.IsType<PolymorphicClassWithDerivedCollections.Dictionary>(dictionary["key1"]);
            Assert.IsType<PolymorphicClassWithDerivedCollections.List>(dictionary["key2"]);
        }

        [JsonDerivedType(typeof(List), "list")]
        [JsonDerivedType(typeof(Dictionary), "dictionary")]
        public abstract class PolymorphicClassWithDerivedCollections
        {
            public class List : PolymorphicClassWithDerivedCollections, IList<PolymorphicClassWithDerivedCollections>
            {
                private readonly IList<PolymorphicClassWithDerivedCollections> _items = new List<PolymorphicClassWithDerivedCollections>();
                public PolymorphicClassWithDerivedCollections this[int index] { get => _items[index]; set => _items[index] = value; }
                public void Add(PolymorphicClassWithDerivedCollections item) => _items.Add(item);
                public bool Contains(PolymorphicClassWithDerivedCollections item) => _items.Contains(item);
                public void CopyTo(PolymorphicClassWithDerivedCollections[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
                public bool Remove(PolymorphicClassWithDerivedCollections item) => _items.Remove(item);
                public int IndexOf(PolymorphicClassWithDerivedCollections item) => _items.IndexOf(item);
                public void Insert(int index, PolymorphicClassWithDerivedCollections item) => _items.Insert(index, item);
                public void RemoveAt(int index) => _items.RemoveAt(index);
                public void Clear() => _items.Clear();
                public int Count => _items.Count;
                public bool IsReadOnly => false;
                IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
                public IEnumerator<PolymorphicClassWithDerivedCollections> GetEnumerator() => _items.GetEnumerator();

            }

            public class Dictionary : PolymorphicClassWithDerivedCollections, IDictionary<string, PolymorphicClassWithDerivedCollections>
            {
                private readonly IDictionary<string, PolymorphicClassWithDerivedCollections> _items = new Dictionary<string, PolymorphicClassWithDerivedCollections>();
                public PolymorphicClassWithDerivedCollections this[string key] { get => _items[key]; set => _items[key] = value; }
                public ICollection<string> Keys => _items.Keys;
                public ICollection<PolymorphicClassWithDerivedCollections> Values => _items.Values;
                public int Count => _items.Count;
                public bool IsReadOnly => false;
                public void Add(string key, PolymorphicClassWithDerivedCollections value) => _items.Add(key, value);
                public void Add(KeyValuePair<string, PolymorphicClassWithDerivedCollections> item) => _items.Add(item);
                public void Clear() => _items.Clear();
                public bool Contains(KeyValuePair<string, PolymorphicClassWithDerivedCollections> item) => _items.Contains(item);
                public bool ContainsKey(string key) => _items.ContainsKey(key);
                public void CopyTo(KeyValuePair<string, PolymorphicClassWithDerivedCollections>[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
                public bool Remove(string key) => _items.Remove(key);
                public bool Remove(KeyValuePair<string, PolymorphicClassWithDerivedCollections> item) => _items.Remove(item);
                public bool TryGetValue(string key, [MaybeNullWhen(false)] out PolymorphicClassWithDerivedCollections value) => _items.TryGetValue(key, out value);
                IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
                public IEnumerator<KeyValuePair<string, PolymorphicClassWithDerivedCollections>> GetEnumerator() => _items.GetEnumerator();
            }
        }
        #endregion

        #region Polymorphic Dictionary
        [Theory]
        [MemberData(nameof(Get_PolymorphicDictionary_TestData_Serialization))]
        public Task PolymorphicDictionary_TestData_Serialization(PolymorphicDictionary.TestData testData)
            => TestMultiContextSerialization(testData.Value, testData.ExpectedJson);

        public static IEnumerable<object[]> Get_PolymorphicDictionary_TestData_Serialization()
            => PolymorphicDictionary.GetSerializeTestData().Select(entry => new object[] { entry });

        [Theory]
        [MemberData(nameof(Get_PolymorphicDictionary_TestData_Serialization))]
        public Task PolymorphicDictionary_TestData_Deserialization(PolymorphicDictionary.TestData testData)
            => TestMultiContextDeserialization(
                testData.ExpectedJson,
                testData.ExpectedRoundtripValue,
                equalityComparer: CreateJsonEqualityComparer<PolymorphicDictionary>());

        public static IEnumerable<object[]> Get_PolymorphicDictionary_TestData_Deserialization()
            => PolymorphicDictionary.GetSerializeTestData().Select(entry => new object[] { entry });

        [Fact]
        public async Task PolymorphicDictionary_TestDataArray_Serialization()
        {
            IEnumerable<(PolymorphicDictionary Value, string ExpectedJson)> inputs =
                PolymorphicDictionary.GetSerializeTestData().Select(entry => (entry.Value, entry.ExpectedJson));

            await TestMultiContextSerialization(inputs);
        }

        [Fact]
        public async Task PolymorphicDictionary_TestDataArray_Deserialization()
        {
            IEnumerable<(string ExpectedJson, PolymorphicDictionary ExpectedRoundtripValue)> inputs =
                PolymorphicDictionary.GetSerializeTestData().Select(entry => (entry.ExpectedJson, entry.ExpectedRoundtripValue));

            await TestMultiContextDeserialization(inputs, equalityComparer: CreateJsonEqualityComparer<PolymorphicDictionary>());
        }

        [Fact]
        public async Task PolymorphicDictionary_UnrecognizedTypeDiscriminators_ShouldSucceedDeserialization()
        {
            string json = """{ "$type" : "invalidTypeDiscriminator", "key" : 42 }""";
            PolymorphicDictionary result = await Serializer.DeserializeWrapper<PolymorphicDictionary>(json);
            Assert.IsType<PolymorphicDictionary>(result);
            Assert.Equal(new PolymorphicDictionary { ["key"] = 42 }, result);
        }

        [Theory]
        [InlineData("$['$ref']", """{ "$type" : "derivedList", "UserProperty" : 42, "$ref" : "42" }""")]
        [InlineData("$['$type']", """{ "$type" : "derivedList", "UserProperty" : 42, "$type" : "derivedDictionary" }""")]
        [InlineData("$['$type']", """{ "UserProperty" : 42, "$type" : "derivedDictionary" }""")]
        [InlineData("$['$values']", """{ "$type" : "derivedDictionary", "$values" : [] }""")]
        [InlineData("$['$id']", """{ "$id" : 42, "UserProperty" : 42 }""")]
        [InlineData("$['$ref']", """{ "$ref" : 42 }""")]
        public async Task PolymorphicDictionary_InvalidTypeDiscriminatorMetadata_ShouldThrowJsonException(string expectedJsonPath, string json)
        {
            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<PolymorphicDictionary>(json));
            Assert.Equal(expectedJsonPath, exception.Path);
        }

        [JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor, IgnoreUnrecognizedTypeDiscriminators = true)]
        [JsonDerivedType(typeof(PolymorphicDictionary), "baseDictionary")]
        [JsonDerivedType(typeof(DerivedDictionary1), "derivedDictionary")]
        public class PolymorphicDictionary : Dictionary<string, int>
        {
            public class DerivedDictionary1 : PolymorphicDictionary
            {
            }

            public class DerivedDictionary2 : PolymorphicDictionary
            {
            }

            public static IEnumerable<TestData> GetSerializeTestData()
            {
                yield return new TestData(
                    Value: new PolymorphicDictionary { ["key1"] = 42 , ["key2"] = -1 },
                    ExpectedJson: """{ "$type" : "baseDictionary", "key1" : 42, "key2" : -1 }""",
                    ExpectedRoundtripValue: new PolymorphicDictionary { ["key1"] = 42, ["key2"] = -1 });

                yield return new TestData(
                    Value: new DerivedDictionary1 { ["key1"] = 42, ["key2"] = -1 },
                    ExpectedJson: """{ "$type" : "derivedDictionary", "key1" : 42, "key2" : -1 }""",
                    ExpectedRoundtripValue: new DerivedDictionary1 { ["key1"] = 42, ["key2"] = -1 });

                yield return new TestData(
                    Value: new DerivedDictionary2 { ["key1"] = 42, ["key2"] = -1 },
                    ExpectedJson: """{ "$type" : "baseDictionary", "key1" : 42, "key2" : -1 }""",
                    ExpectedRoundtripValue: new PolymorphicDictionary { ["key1"] = 42, ["key2"] = -1 });
            }

            public record TestData(PolymorphicDictionary Value, string ExpectedJson, PolymorphicDictionary ExpectedRoundtripValue);
        }

        [Fact]
        public async Task PolymorphicDictionaryInterface_Serialization()
        {
            var values = new IEnumerable<KeyValuePair<int, object>>[]
            {
                new List<KeyValuePair<int, object>> { new KeyValuePair<int, object>(0, 0) },
                new Dictionary<int, object> { [42] = false },
                new SortedDictionary<int, object> { [0] = 1, [1] = 42 },
                ImmutableDictionary.Create<int, object>()
            };

            string expectedJson =
                """
                        [ [ { "Key":0, "Value":0 } ],
                                            { "$type" : "dictionary", "42" : false },
                                            { "$type" : "sortedDictionary", "0" : 1, "1" : 42 },
                                            { "$type" : "readOnlyDictionary" } ]
                    """;

            string actualJson = await Serializer.SerializeWrapper(values, s_optionsWithPolymorphicDictionaryInterface);

            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Fact]
        public async Task PolymorphicDictionaryInterface_Deserialization()
        {
            string json =
                """
                        [ [ { "Key":0, "Value":0 } ],
                                            { "$type" : "dictionary", "42" : false },
                                            { "$type" : "sortedDictionary", "0" : 1, "1" : 42 },
                                            { "$type" : "readOnlyDictionary" } ]
                    """;

            var expectedValues = new IEnumerable<KeyValuePair<int, object>>[]
            {
                new List<KeyValuePair<int, object>> { new KeyValuePair<int, object>(0, 0) },
                new Dictionary<int, object> { [42] = false },
                new SortedDictionary<int, object> { [0] = 1, [1] = 42 },
                new Dictionary<int, object>()
            };

            var actualValues = await Serializer.DeserializeWrapper<IEnumerable<KeyValuePair<int, object>>[]>(json, s_optionsWithPolymorphicDictionaryInterface);

            Assert.Equal(expectedValues.Length, actualValues.Length);
            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.Equal(expectedValues[i].Select(x => x.Key), actualValues[i].Select(x => x.Key));
                Assert.Equal(expectedValues[i].Select(x => x.Value.ToString()), actualValues[i].Select(x => x.Value.ToString()));
                Assert.IsType(expectedValues[i].GetType(), actualValues[i]);
            }
        }

        private readonly static JsonSerializerOptions s_optionsWithPolymorphicDictionaryInterface = new JsonSerializerOptions
        {
            TypeInfoResolver = new CustomPolymorphismResolver<IEnumerable<KeyValuePair<int, object>>>
            {
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
            }
            .WithDerivedType<Dictionary<int, object>>("dictionary")
            .WithDerivedType<SortedDictionary<int, object>>("sortedDictionary")
            .WithDerivedType<IReadOnlyDictionary<int, object>>("readOnlyDictionary")
        };
        #endregion

        #region Polymorphic Record Types
        [Theory]
        [InlineData(0, """{"$type":"zero"}""")]
        [InlineData(1, """{"$type":"succ", "value":{"$type":"zero"}}""")]
        [InlineData(3, """{"$type":"succ", "value":{"$type":"succ","value":{"$type":"succ","value":{"$type":"zero"}}}}""")]
        public async Task Peano_Serialization(int size, string expectedJson)
        {
            Peano peano = Peano.FromInteger(size);
            await TestMultiContextSerialization(peano, expectedJson);
        }

        [Theory]
        [InlineData(0, """{"$type":"zero"}""")]
        [InlineData(1, """{"$type":"succ", "value":{"$type":"zero"}}""")]
        [InlineData(3, """{"$type":"succ", "value":{"$type":"succ","value":{"$type":"succ","value":{"$type":"zero"}}}}""")]
        public async Task Peano_Deserialization(int expectedSize, string json)
        {
            Peano expected = Peano.FromInteger(expectedSize);
            await TestMultiContextDeserialization<Peano>(json, expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)] // initial Write/ReadStack size
        [InlineData(5)] // 1st Write/ReadStack resize
        [InlineData(17)] // 2nd Write/ReadStack resize
        [InlineData(33)] // 3rd Write/ReadStack resize
        [InlineData(65)] // 4th Write/ReadStack resize
        [InlineData(80)]
        public async Task Peano_Roundtrip(int number)
        {
            JsonSerializerOptions options = new();
            options.MaxDepth = number + 1;
            Peano obj = Peano.FromInteger(number);
            string json = await Serializer.SerializeWrapper(obj, options);
            Peano deserialized = await Serializer.DeserializeWrapper<Peano>(json, options);
            Assert.Equal(obj, deserialized);
        }

        // A Peano representation for natural numbers
        [JsonDerivedType(typeof(Zero), "zero")]
        [JsonDerivedType(typeof(Succ), "succ")]
        public abstract record Peano
        {
            public static Peano FromInteger(int value) => value == 0 ? new Zero() : new Succ(FromInteger(value - 1));
            public record Zero : Peano;
            public record Succ(Peano value) : Peano;
        }

        [Theory]
        [MemberData(nameof(BinaryTree.GetTestData), MemberType = typeof(BinaryTree))]
        public async Task BinaryTree_TestData_Serialization(BinaryTree tree, string expectedJson)
        {
            string actualJson = await Serializer.SerializeWrapper(tree);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Theory]
        [MemberData(nameof(BinaryTree.GetTestData), MemberType = typeof(BinaryTree))]
        public async Task BinaryTree_TestData_Deserialization(BinaryTree expected, string json)
        {
            BinaryTree actual = await Serializer.DeserializeWrapper<BinaryTree>(json);
            Assert.Equal(expected, actual);
        }

        [JsonDerivedType(typeof(Leaf), "leaf")]
        [JsonDerivedType(typeof(Node), "node")]
        public abstract record BinaryTree
        {
            public record Leaf : BinaryTree;
            public record Node(int value, BinaryTree left, BinaryTree right) : BinaryTree;

            public static IEnumerable<object[]> GetTestData()
            {
                yield return WrapArgs(new Leaf(), """{"$type":"leaf"}""");
                yield return WrapArgs(
                    new Node(-1,
                        new Leaf(),
                        new Leaf()),
                    """{"$type":"node","value":-1,"left":{"$type":"leaf"},"right":{"$type":"leaf"}}""");

                yield return WrapArgs(
                    new Node(12,
                        new Leaf(),
                        new Node(24,
                            new Leaf(),
                            new Leaf())),
                    """
                            {"$type":"node", "value":12,
                                                        "left":{"$type":"leaf"},
                                                        "right":{"$type":"node", "value":24,
                                                                  "left":{"$type":"leaf"},
                                                                  "right":{"$type":"leaf"}}}
                        """);

                static object[] WrapArgs(BinaryTree value, string expectedJson) => new object[] { value, expectedJson };
            }
        }

        #endregion

        #region Polymorphism/Reference Preservation

        [Theory]
        [MemberData(nameof(Get_ReferencePreservation_TestData_Boxed))]
        public async Task ReferencePreservation_SingleValue_Serialization(PolymorphicClass value, Func<string, string> jsonTemplate)
        {
            string expectedJson = jsonTemplate("1"); // root values have reference id "1"
            string actualJson = await Serializer.SerializeWrapper(value, s_jsonSerializerOptionsPreserveRefs);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Theory]
        [MemberData(nameof(Get_ReferencePreservation_TestData_Boxed))]
        public async Task ReferencePreservation_SingleValue_Deserialization(PolymorphicClass expectedValue, Func<string, string> jsonTemplate)
        {
            string json = jsonTemplate("1"); // root values have reference id "1"
            PolymorphicClass actualValue = await Serializer.DeserializeWrapper<PolymorphicClass>(json, s_jsonSerializerOptionsPreserveRefs);
            Assert.Equal(expectedValue, actualValue, CreateJsonEqualityComparer<PolymorphicClass>());
        }

        [Theory]
        [MemberData(nameof(Get_ReferencePreservation_TestData_Boxed))]
        public async Task ReferencePreservation_RepeatingValue_Serialization(PolymorphicClass value, Func<string, string> jsonTemplate)
        {
            List<PolymorphicClass> input = new() { value, value };
            string expectedJson =
                $@"{{""$id"":""1"",
                     ""$values"":[
                          {jsonTemplate("2")},
                          {{""$ref"":""2""}} ]
                }}";

            string actualJson = await Serializer.SerializeWrapper(input, s_jsonSerializerOptionsPreserveRefs);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Theory]
        [MemberData(nameof(Get_ReferencePreservation_TestData_Boxed))]
        public async Task ReferencePreservation_RepeatingValue_Deserialization(PolymorphicClass expectedValue, Func<string, string> jsonTemplate)
        {
            string json =
                $@"{{""$id"":""1"",
                     ""$values"":[
                          {jsonTemplate("2")},
                          {{""$ref"":""2""}} ]
                }}";

            var result = await Serializer.DeserializeWrapper<List<PolymorphicClass>>(json, s_jsonSerializerOptionsPreserveRefs);

            Assert.Equal(2, result.Count);
            Assert.Equal(expectedValue, result[0], CreateJsonEqualityComparer<PolymorphicClass>());
            Assert.Same(result[0], result[1]);
        }

        [Fact]
        public async Task ReferencePreservation_MultipleRepeatingValues_Serialization()
        {
            (PolymorphicClass Value, Func<string, string> JsonTemplate)[] data = Get_ReferencePreservation_TestData().ToArray();
            PolymorphicClass[] values = data.Select(entry => entry.Value).Concat(data.Select(entry => entry.Value)).ToArray();

            IEnumerable<string> idValues = data.Select((entry, i) => entry.JsonTemplate((i + 1).ToString()));
            IEnumerable<string> refValues = Enumerable.Range(1, data.Length).Select(x => $@"{{ ""$ref"" : ""{x}""}}");
            string expectedJson = "[" + string.Join(", ", idValues.Concat(refValues)) + "]";

            string actualJson = await Serializer.SerializeWrapper(values, s_jsonSerializerOptionsPreserveRefs);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Fact]
        public async Task ReferencePreservation_MultipleRepeatingValues_Deserialization()
        {
            (PolymorphicClass Value, Func<string, string> JsonTemplate)[] data = Get_ReferencePreservation_TestData().ToArray();
            PolymorphicClass[] expectedValues = data.Select(entry => entry.Value).Concat(data.Select(entry => entry.Value)).ToArray();

            IEnumerable<string> idValues = data.Select((entry, i) => entry.JsonTemplate((i + 1).ToString()));
            IEnumerable<string> refValues = Enumerable.Range(1, data.Length).Select(x => $@"{{ ""$ref"" : ""{x}""}}");
            string json = "[" + string.Join(", ", idValues.Concat(refValues)) + "]";

            PolymorphicClass[] result = await Serializer.DeserializeWrapper<PolymorphicClass[]>(json, s_jsonSerializerOptionsPreserveRefs);
            Assert.Equal(expectedValues, result, CreateJsonEqualityComparer<PolymorphicClass>());
        }

        public static IEnumerable<(PolymorphicClass Value, Func<string, string> JsonTemplate)> Get_ReferencePreservation_TestData()
        {
            yield return (
                Value: new PolymorphicClass.DerivedClass1_TypeDiscriminator { Number = 42, String = "str" },
                JsonTemplate: id => $@"{{""$id"":""{id}"",""$type"":""derivedClass1"",""Number"":42,""String"":""str""}}");

            yield return (
                Value: new PolymorphicClass.DerivedClassWithConstructor_TypeDiscriminator(42),
                JsonTemplate: id => $@"{{""$id"":""{id}"",""$type"":""derivedClassWithCtor"",""Number"":42}}");

            yield return (
                Value: new PolymorphicClass.DerivedCollection_TypeDiscriminator { Number = 42 },
                JsonTemplate: id => $@"{{""$id"":""{id}"",""$type"":""derivedCollection"",""$values"":[42,42,42]}}");

            yield return (
                Value: new PolymorphicClass.DerivedDictionary_TypeDiscriminator { Number = 42 },
                JsonTemplate: id => $@"{{""$id"":""{id}"",""$type"":""derivedDictionary"",""dictionaryKey"":42}}");
        }

        public static IEnumerable<object[]> Get_ReferencePreservation_TestData_Boxed()
            => Get_ReferencePreservation_TestData().Select(entry => new object[] { entry.Value, entry.JsonTemplate });

        [Theory]
        [MemberData(nameof(PolymorphicClassWithCustomTypeDiscriminator.GetTestData_Boxed), MemberType = typeof(PolymorphicClassWithCustomTypeDiscriminator))]
        public async Task ReferencePreservation_CustomTypeDiscriminator_SingleValue_Serialization(PolymorphicClassWithCustomTypeDiscriminator value, Func<string, string> jsonTemplate)
        {
            string expectedJson = jsonTemplate("1"); // root values have reference id "1"
            string actualJson = await Serializer.SerializeWrapper(value, s_jsonSerializerOptionsPreserveRefs);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Theory]
        [MemberData(nameof(PolymorphicClassWithCustomTypeDiscriminator.GetTestData_Boxed), MemberType = typeof(PolymorphicClassWithCustomTypeDiscriminator))]
        public async Task ReferencePreservation_CustomTypeDiscriminator_SingleValue_Deserialization(PolymorphicClassWithCustomTypeDiscriminator expectedValue, Func<string, string> jsonTemplate)
        {
            string json = jsonTemplate("1"); // root values have reference id "1"
            PolymorphicClassWithCustomTypeDiscriminator actualValue = await Serializer.DeserializeWrapper<PolymorphicClassWithCustomTypeDiscriminator>(json, s_jsonSerializerOptionsPreserveRefs);
            Assert.Equal(expectedValue, actualValue, CreateJsonEqualityComparer<PolymorphicClassWithCustomTypeDiscriminator>());
        }

        [Theory]
        [MemberData(nameof(PolymorphicClassWithCustomTypeDiscriminator.GetTestData_Boxed), MemberType = typeof(PolymorphicClassWithCustomTypeDiscriminator))]
        public async Task ReferencePreservation_CustomTypeDiscriminator_RepeatingValue_Serialization(PolymorphicClassWithCustomTypeDiscriminator value, Func<string, string> jsonTemplate)
        {
            List<PolymorphicClassWithCustomTypeDiscriminator> input = new() { value, value };
            string expectedJson =
                $@"{{""$id"":""1"",
                     ""$values"":[
                          {jsonTemplate("2")},
                          {{""$ref"":""2""}} ]
                }}";

            string actualJson = await Serializer.SerializeWrapper(input, s_jsonSerializerOptionsPreserveRefs);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Theory]
        [MemberData(nameof(PolymorphicClassWithCustomTypeDiscriminator.GetTestData_Boxed), MemberType = typeof(PolymorphicClassWithCustomTypeDiscriminator))]
        public async Task ReferencePreservation_CustomTypeDiscriminator_RepeatingValue_Deserialization(PolymorphicClassWithCustomTypeDiscriminator expectedValue, Func<string, string> jsonTemplate)
        {
            string json =
                $@"{{""$id"":""1"",
                     ""$values"":[
                          {jsonTemplate("2")},
                          {{""$ref"":""2""}} ]
                }}";

            var result = await Serializer.DeserializeWrapper<List<PolymorphicClassWithCustomTypeDiscriminator>>(json, s_jsonSerializerOptionsPreserveRefs);

            Assert.Equal(2, result.Count);
            Assert.Equal(expectedValue, result[0], CreateJsonEqualityComparer<PolymorphicClassWithCustomTypeDiscriminator>());
            Assert.Same(result[0], result[1]);
        }

        [Fact]
        public async Task ReferencePreservation_CustomTypeDiscriminator_MultipleRepeatingValues_Serialization()
        {
            (PolymorphicClassWithCustomTypeDiscriminator Value, Func<string, string> JsonTemplate)[] data = PolymorphicClassWithCustomTypeDiscriminator.GetTestData().ToArray();
            PolymorphicClassWithCustomTypeDiscriminator[] values = data.Select(entry => entry.Value).Concat(data.Select(entry => entry.Value)).ToArray();

            IEnumerable<string> idValues = data.Select((entry, i) => entry.JsonTemplate((i + 1).ToString()));
            IEnumerable<string> refValues = Enumerable.Range(1, data.Length).Select(x => $@"{{ ""$ref"" : ""{x}""}}");
            string expectedJson = "[" + string.Join(", ", idValues.Concat(refValues)) + "]";

            string actualJson = await Serializer.SerializeWrapper(values, s_jsonSerializerOptionsPreserveRefs);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Fact]
        public async Task ReferencePreservation_CustomTypeDiscriminator_MultipleRepeatingValues_Deserialization()
        {
            (PolymorphicClassWithCustomTypeDiscriminator Value, Func<string, string> JsonTemplate)[] data = PolymorphicClassWithCustomTypeDiscriminator.GetTestData().ToArray();
            PolymorphicClassWithCustomTypeDiscriminator[] expectedValues = data.Select(entry => entry.Value).Concat(data.Select(entry => entry.Value)).ToArray();

            IEnumerable<string> idValues = data.Select((entry, i) => entry.JsonTemplate((i + 1).ToString()));
            IEnumerable<string> refValues = Enumerable.Range(1, data.Length).Select(x => $@"{{ ""$ref"" : ""{x}""}}");
            string json = "[" + string.Join(", ", idValues.Concat(refValues)) + "]";

            PolymorphicClassWithCustomTypeDiscriminator[] result = await Serializer.DeserializeWrapper<PolymorphicClassWithCustomTypeDiscriminator[]>(json, s_jsonSerializerOptionsPreserveRefs);
            Assert.Equal(expectedValues, result, CreateJsonEqualityComparer<PolymorphicClassWithCustomTypeDiscriminator>());
        }

        [Theory]
        [MemberData(nameof(Get_ReferencePreservation_TestData_Boxed))]
        public async Task ReferencePreservation_AllowOutOfOrderMetadata_SingleValue_Deserialization(PolymorphicClass expectedValue, Func<string, string> jsonTemplate)
        {
            string json = jsonTemplate("1"); // root values have reference id "1"
            PolymorphicClass actualValue = await Serializer.DeserializeWrapper<PolymorphicClass>(json, s_jsonSerializerOptionsPreserveRefsAndAllowReadAhead);
            Assert.Equal(expectedValue, actualValue, CreateJsonEqualityComparer<PolymorphicClass>());
        }

        [Theory]
        [MemberData(nameof(Get_ReferencePreservation_TestData_Boxed))]
        public async Task ReferencePreservation_AllowOutOfOrderMetadata_RepeatingValue_Deserialization(PolymorphicClass expectedValue, Func<string, string> jsonTemplate)
        {
            string json =
                $@"{{""$id"":""1"",
                     ""$values"":[
                          {jsonTemplate("2")},
                          {{""$ref"":""2""}} ]
                }}";

            var result = await Serializer.DeserializeWrapper<List<PolymorphicClass>>(json, s_jsonSerializerOptionsPreserveRefsAndAllowReadAhead);

            Assert.Equal(2, result.Count);
            Assert.Equal(expectedValue, result[0], CreateJsonEqualityComparer<PolymorphicClass>());
            Assert.Same(result[0], result[1]);
        }

        [Fact]
        public async Task ReferencePreservation_AllowOutOfOrderMetadata_MultipleRepeatingValues_Deserialization()
        {
            (PolymorphicClass Value, Func<string, string> JsonTemplate)[] data = Get_ReferencePreservation_TestData().ToArray();
            PolymorphicClass[] expectedValues = data.Select(entry => entry.Value).Concat(data.Select(entry => entry.Value)).ToArray();

            IEnumerable<string> idValues = data.Select((entry, i) => entry.JsonTemplate((i + 1).ToString()));
            IEnumerable<string> refValues = Enumerable.Range(1, data.Length).Select(x => $@"{{ ""$ref"" : ""{x}""}}");
            string json = "[" + string.Join(", ", idValues.Concat(refValues)) + "]";

            PolymorphicClass[] result = await Serializer.DeserializeWrapper<PolymorphicClass[]>(json, s_jsonSerializerOptionsPreserveRefsAndAllowReadAhead);
            Assert.Equal(expectedValues, result, CreateJsonEqualityComparer<PolymorphicClass>());
        }

        [Theory]
        [InlineData("""[{ "$type" : "derivedClass1", "Number" : 42, "$id" : "1", "String" : "str" }, { "$ref" : "1" }]""", typeof(PolymorphicClass.DerivedClass1_TypeDiscriminator))]
        [InlineData("""[{ "$id" : "1", "Number" : 42, "$type" : "derivedClass1", "String" : "str" }, { "$ref" : "1" }]""", typeof(PolymorphicClass.DerivedClass1_TypeDiscriminator))]
        [InlineData("""[{ "$type" : "derivedClass1", "Number" : 42, "String" : "str", "$id" : "1" }, { "$ref" : "1" }]""", typeof(PolymorphicClass.DerivedClass1_TypeDiscriminator))]
        [InlineData("""[{ "$values": [42,42,42], "$type" : "derivedCollection", "$id" : "1" }, { "$ref" : "1" }]""", typeof(PolymorphicClass.DerivedCollection_TypeDiscriminator))]
        [InlineData("""[{ "$type" : "derivedCollection", "$values": [42,42,42], "$id" : "1" }, { "$ref" : "1" }]""", typeof(PolymorphicClass.DerivedCollection_TypeDiscriminator))]
        public async Task ReferencePreservation_AllowOutOfOrderMetadata_AcceptsOutOfOrderMetadata(string json, Type expectedType)
        {
            PolymorphicClass[] result = await Serializer.DeserializeWrapper<PolymorphicClass[]>(json, s_jsonSerializerOptionsPreserveRefsAndAllowReadAhead);
            Assert.Equal(2, result.Length);
            Assert.IsType(expectedType, result[0]);
            Assert.Same(result[0], result[1]);
        }

        [Theory]
        [InlineData("$[1]['$ref']", """[{ "$id" : "1" }, { "NonMetadataProperty": [1,2,3], "$ref" : "1" }]""")]
        [InlineData("$[1].NonMetadataProperty", """[{ "$id" : "1" }, { "$ref" : "1", "NonMetadataProperty": [1,2,3] }]""")]
        [InlineData("$[1]['$ref']", """[{ "$id" : "1" }, { "$type": "derivedClass1", "$ref" : "1" }]""")]
        [InlineData("$[1]['$type']", """[{ "$id" : "1" }, { "$ref" : "1", "$type": "derivedClass1" }]""")]
        [InlineData("$[1]['$id']", """[{ "$id" : "1" }, { "$ref" : "1", "$id": "1" }]""")]
        [InlineData("$[1]['$ref']", """[{ "$id" : "1" }, { "$id": "1", "$ref" : "1" }]""")]
        [InlineData("$[1]['$ref']", """[{ "$id" : "1" }, { "$values": [1, 2, 3], "$ref" : "1" }]""")]
        [InlineData("$[1]['$values']", """[{ "$id" : "1" }, { "$ref" : "1", "$values": [1, 2, 3] }]""")]
        [InlineData("$[0].NonMetadataProperty", """[{ "$type" : "derivedCollection", "$values": [42,42,42], "$id" : "1", "NonMetadataProperty": {}}, { "$ref" : "1" }]""")]
        [InlineData("$[0]['$values']", """[{ "$type" : "derivedCollection", "$id" : "1", "NonMetadataProperty": {}, "$values": [42,42,42]}, { "$ref" : "1" }]""")]
        [InlineData("$[1]['$ref']", """[{ "$type" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$type" : "derivedCollection", "$ref" : "1" }]""")]
        [InlineData("$[1]['$values']", """[{ "$type" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$ref" : "1", "$values" : [1,2,3] }]""")]
        [InlineData("$[1]['$ref']", """[{ "$type" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$values" : [1,2,3], "$ref" : "1" }]""")]
        public async Task ReferencePreservation_AllowOutOfOrderMetadata_RejectsInvalidMetadata(string expectedJsonPath, string json)
        {
            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<PolymorphicClass[]>(json, s_jsonSerializerOptionsPreserveRefsAndAllowReadAhead));
            Assert.Equal(expectedJsonPath, exception.Path);
        }

        [Theory]
        [InlineData("""[{ "case" : "derivedClass", "Number" : 42, "$id" : "1", "String" : "str" }, { "$ref" : "1" }]""", typeof(PolymorphicClassWithCustomTypeDiscriminator.DerivedClass))]
        [InlineData("""[{ "$id" : "1", "Number" : 42, "case" : "derivedClass", "String" : "str" }, { "$ref" : "1" }]""", typeof(PolymorphicClassWithCustomTypeDiscriminator.DerivedClass))]
        [InlineData("""[{ "case" : "derivedClass", "Number" : 42, "String" : "str", "$id" : "1" }, { "$ref" : "1" }]""", typeof(PolymorphicClassWithCustomTypeDiscriminator.DerivedClass))]
        [InlineData("""[{ "$values": [42,42,42], "case" : "derivedCollection", "$id" : "1" }, { "$ref" : "1" }]""", typeof(PolymorphicClassWithCustomTypeDiscriminator.DerivedCollection))]
        [InlineData("""[{ "case" : "derivedCollection", "$values": [42,42,42], "$id" : "1" }, { "$ref" : "1" }]""", typeof(PolymorphicClassWithCustomTypeDiscriminator.DerivedCollection))]
        public async Task ReferencePreservation_CustomTypeDiscriminator_AllowOutOfOrderMetadata_AcceptsOutOfOrderMetadata(string json, Type expectedType)
        {
            PolymorphicClassWithCustomTypeDiscriminator[] result = await Serializer.DeserializeWrapper<PolymorphicClassWithCustomTypeDiscriminator[]>(json, s_jsonSerializerOptionsPreserveRefsAndAllowReadAhead);
            Assert.Equal(2, result.Length);
            Assert.IsType(expectedType, result[0]);
            Assert.Same(result[0], result[1]);
        }

        [Theory]
        [InlineData("$[1]['$ref']", """[{ "$id" : "1" }, { "NonMetadataProperty": [1,2,3], "$ref" : "1" }]""")]
        [InlineData("$[1].NonMetadataProperty", """[{ "$id" : "1" }, { "$ref" : "1", "NonMetadataProperty": [1,2,3] }]""")]
        [InlineData("$[1]['$ref']", """[{ "$id" : "1" }, { "case": "derivedClass", "$ref" : "1" }]""")]
        [InlineData("$[1].case", """[{ "$id" : "1" }, { "$ref" : "1", "case": "derivedClass" }]""")]
        [InlineData("$[1]['$id']", """[{ "$id" : "1" }, { "$ref" : "1", "$id": "1" }]""")]
        [InlineData("$[1]['$ref']", """[{ "$id" : "1" }, { "$id": "1", "$ref" : "1" }]""")]
        [InlineData("$[1]['$ref']", """[{ "$id" : "1" }, { "$values": [1, 2, 3], "$ref" : "1" }]""")]
        [InlineData("$[1]['$values']", """[{ "$id" : "1" }, { "$ref" : "1", "$values": [1, 2, 3] }]""")]
        [InlineData("$[0].NonMetadataProperty", """[{ "case" : "derivedCollection", "$values": [42,42,42], "$id" : "1", "NonMetadataProperty": {}}, { "$ref" : "1" }]""")]
        [InlineData("$[0]['$values']", """[{ "case" : "derivedCollection", "$id" : "1", "NonMetadataProperty": {}, "$values": [42,42,42]}, { "$ref" : "1" }]""")]
        [InlineData("$[1]['$ref']", """[{ "case" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "case" : "derivedCollection", "$ref" : "1" }]""")]
        [InlineData("$[1]['$values']", """[{ "case" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$ref" : "1", "$values" : [1,2,3] }]""")]
        [InlineData("$[1]['$ref']", """[{ "case" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$values" : [1,2,3], "$ref" : "1" }]""")]
        public async Task ReferencePreservation_CustomTypeDiscriminator_AllowOutOfOrderMetadata_RejectsInvalidMetadata(string expectedJsonPath, string json)
        {
            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<PolymorphicClassWithCustomTypeDiscriminator[]>(json, s_jsonSerializerOptionsPreserveRefsAndAllowReadAhead));
            Assert.Equal(expectedJsonPath, exception.Path);
        }

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "case")]
        [JsonDerivedType(typeof(PolymorphicClassWithCustomTypeDiscriminator), "baseClass")]
        [JsonDerivedType(typeof(DerivedClass), "derivedClass")]
        [JsonDerivedType(typeof(DerivedCollection), "derivedCollection")]
        public class PolymorphicClassWithCustomTypeDiscriminator
        {
            public int Number { get; set; }

            public class DerivedClass : PolymorphicClassWithCustomTypeDiscriminator
            {
                public string String { get; set; }
            }

            public class DerivedCollection : PolymorphicClassWithCustomTypeDiscriminator, ICollection<int>
            {
                public bool IsReadOnly => false;
                public void Add(int item) => Number = item;
                public IEnumerator<int> GetEnumerator() => Enumerable.Repeat(Number, 3).GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                public int Count => throw new NotImplementedException();
                public void Clear() => throw new NotImplementedException();
                public bool Contains(int item) => throw new NotImplementedException();
                public void CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();
                public bool Remove(int item) => throw new NotImplementedException();
            }

            public static IEnumerable<(PolymorphicClassWithCustomTypeDiscriminator Value, Func<string, string> JsonTemplate)> GetTestData()
            {
                yield return (
                    Value: new PolymorphicClassWithCustomTypeDiscriminator { Number = 42 },
                    JsonTemplate: id => $@"{{""$id"":""{id}"",""case"":""baseClass"",""Number"":42}}");

                yield return (
                    Value: new DerivedClass { Number = 42, String = "str" },
                    JsonTemplate: id => $@"{{""case"":""derivedClass"",""$id"":""{id}"",""Number"":42,""String"":""str""}}");

                yield return (
                    Value: new DerivedCollection { 42 },
                    JsonTemplate: id => $@"{{""case"":""derivedCollection"",""$id"":""{id}"",""$values"":[42,42,42]}}");
            }

            public static IEnumerable<object[]> GetTestData_Boxed()
                => GetTestData().Select(entry => new object[] { entry.Value, entry.JsonTemplate });
        }

        private readonly static JsonSerializerOptions s_jsonSerializerOptionsPreserveRefs = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve
        };

        private readonly static JsonSerializerOptions s_jsonSerializerOptionsPreserveRefsAndAllowReadAhead = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            AllowOutOfOrderMetadataProperties = true,
        };
        #endregion

        #region Attribute Negative Tests

        [Fact]
        public async Task PolymorphicClassWithoutDerivedTypeAttribute_ThrowsInvalidOperationException()
        {
            var value = new PolymorphicClassWithoutDerivedTypeAttribute();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonPolymorphic]
        public class PolymorphicClassWithoutDerivedTypeAttribute
        {
        }

        [ConditionalFact]
        public async Task PolymorphicClassWithNullDerivedTypeAttribute_ThrowsInvalidOperationException()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                // The JsonDerivedType ctor arg is dereferenced without a null check in
                // JsonSourceGenerator.Parser.cs, so the generator throws NRE rather than emitting
                // a SYSLIB diagnostic. Pending a generator fix, the runtime InvalidOperationException
                // is validated under reflection only.
                throw new SkipTestException("Generator NRE on [JsonDerivedType(null)] prevents reaching the runtime path.");
            }

            var value = new PolymorphicClassWithNullDerivedTypeAttribute();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(derivedType: null)]
        public class PolymorphicClassWithNullDerivedTypeAttribute
        {
        }

        [Fact]
        public async Task PolymorphicClassWithStructDerivedTypeAttribute_ThrowsInvalidOperationException()
        {
            var value = new PolymorphicClassWithStructDerivedTypeAttribute();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(Guid))]
        public class PolymorphicClassWithStructDerivedTypeAttribute
        {
        }

        [Fact]
        public async Task PolymorphicClassWithObjectDerivedTypeAttribute_ThrowsInvalidOperationException()
        {
            var value = new PolymorphicClassWithObjectDerivedTypeAttribute();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(object), "object")]
        public class PolymorphicClassWithObjectDerivedTypeAttribute
        {
        }

        [Fact]
        public async Task PolymorphicClassWithNonAssignableDerivedTypeAttribute_ThrowsInvalidOperationException()
        {
            var value = new PolymorphicClassWithNonAssignableDerivedTypeAttribute();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(object))]
        public class PolymorphicClassWithNonAssignableDerivedTypeAttribute
        {
        }


        [Fact]
        public async Task PolymorphicInterfaceWithInterfaceDerivedType_Serialization_ThrowsInvalidOperationException()
        {
            PolymorphicInterfaceWithInterfaceDerivedType value = new PolymorphicInterfaceWithInterfaceDerivedType.DerivedClass();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [Fact]
        public async Task PolymorphicInterfaceWithInterfaceDerivedType_Deserialization_ThrowsInvalidOperationException()
        {
            string json = """{"$type":"derivedInterface"}""";
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.DeserializeWrapper<PolymorphicInterfaceWithInterfaceDerivedType>(json));
        }

        [Fact]
        public async Task PolymorphicInterfaceWithInterfaceDerivedType_FallbackToNearestAncestor_Serialization()
        {
            PolymorphicInterfaceWithInterfaceDerivedType value = new PolymorphicInterfaceWithInterfaceDerivedType.DerivedInterface.ImplementingClass();
            string expectedJson = """{"$type":"derivedInterface"}""";
            string actualJson = await Serializer.SerializeWrapper(value, PolymorphicInterfaceWithInterfaceDerivedType_OptionsWithFallbackToNearestAncestor);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Fact]
        public async Task PolymorphicInterfaceWithInterfaceDerivedType_FallbackToNearestAncestor_Deserialization_ThrowsNotSupportedException()
        {
            string json = """{"$type":"derivedInterface"}""";
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                Serializer.DeserializeWrapper<PolymorphicInterfaceWithInterfaceDerivedType>(json,
                    PolymorphicInterfaceWithInterfaceDerivedType_OptionsWithFallbackToNearestAncestor));
        }

        [JsonDerivedType(typeof(DerivedInterface), "derivedInterface")]
        [JsonDerivedType(typeof(DerivedClass), "derivedClass")]
        public interface PolymorphicInterfaceWithInterfaceDerivedType
        {
            public interface DerivedInterface : PolymorphicInterfaceWithInterfaceDerivedType
            {
                public class ImplementingClass : DerivedInterface
                {
                }
            }

            public class DerivedClass : PolymorphicInterfaceWithInterfaceDerivedType
            {
            }

        }

        public static JsonSerializerOptions PolymorphicInterfaceWithInterfaceDerivedType_OptionsWithFallbackToNearestAncestor { get; } =
            new JsonSerializerOptions
            {
                TypeInfoResolver = new CustomPolymorphismResolver<PolymorphicInterfaceWithInterfaceDerivedType>()
                {
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
                }
                .WithDerivedType<PolymorphicInterfaceWithInterfaceDerivedType.DerivedInterface>("derivedInterface")
                .WithDerivedType<PolymorphicInterfaceWithInterfaceDerivedType.DerivedClass>("derivedClass")
            };

        [Fact]
        public async Task PolymorphicAbstractClassWithAbstractClassDerivedType_ThrowsInvalidOperationException()
        {
            PolymorphicAbstractClassWithAbstractClassDerivedType value = new PolymorphicAbstractClassWithAbstractClassDerivedType.DerivedClass();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(DerivedAbstractClass))]
        [JsonDerivedType(typeof(DerivedClass))]
        public abstract class PolymorphicAbstractClassWithAbstractClassDerivedType
        {
            public abstract class DerivedAbstractClass : PolymorphicAbstractClassWithAbstractClassDerivedType
            {
            }

            public class DerivedClass : PolymorphicAbstractClassWithAbstractClassDerivedType
            {
            }
        }

        [Fact]
        public async Task PolymorphicClassWithDuplicateDerivedTypeRegistrations_ThrowsInvalidOperationException()
        {
            PolymorphicClassWithDuplicateDerivedTypeRegistrations value = new PolymorphicClassWithDuplicateDerivedTypeRegistrations.DerivedClass();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(DerivedClass))]
        [JsonDerivedType(typeof(DerivedClass), "id")]
        public class PolymorphicClassWithDuplicateDerivedTypeRegistrations
        {
            public class DerivedClass : PolymorphicClassWithDuplicateDerivedTypeRegistrations
            {
            }
        }

        [Fact]
        public async Task PolymorphicClasWithDuplicateTypeDiscriminators_ThrowsInvalidOperationException()
        {
            var value = new PolymorphicClasWithDuplicateTypeDiscriminators();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(A), "duplicateId")]
        [JsonDerivedType(typeof(B), "duplicateId")]
        public class PolymorphicClasWithDuplicateTypeDiscriminators
        {
            public class A : PolymorphicClasWithDuplicateTypeDiscriminators { }
            public class B : PolymorphicClasWithDuplicateTypeDiscriminators { }
        }

        [Fact]
        public async Task PolymorphicGenericClass_SupportsOpenGenericDerivedType()
        {
            PolymorphicGenericClass<int> value = new PolymorphicGenericClass<int>.DerivedClass { BaseValue = 1, DerivedValue = 2 };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"BaseValue":1,"DerivedValue":2}""", json);
        }

        [JsonDerivedType(typeof(PolymorphicGenericClass<>.DerivedClass))]
        public class PolymorphicGenericClass<T>
        {
            public T? BaseValue { get; set; }

            public class DerivedClass : PolymorphicGenericClass<T>
            {
                public T? DerivedValue { get; set; }
            }
        }

        [ConditionalFact]
        public async Task PolymorphicDerivedGenericClass_ThrowsInvalidOperationException()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                throw new SkipTestException("Source generator rejects this invalid polymorphic configuration at build time (SYSLIB diagnostic); the runtime InvalidOperationException is validated under reflection only.");
            }

            PolymorphicDerivedGenericClass value = new PolymorphicDerivedGenericClass.DerivedClass<int>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(DerivedClass<>))]
        public class PolymorphicDerivedGenericClass
        {
            public class DerivedClass<T> : PolymorphicDerivedGenericClass
            {
            }
        }

        #region Open Generic Polymorphism Tests

        [Fact]
        public async Task OpenGenericDerivedType_WithStringDiscriminator_SerializationWorks()
        {
            OpenGenericBase_StringDisc<int> value = new OpenGenericDerived_StringDisc<int> { Value = 42 };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Value":42}""", json);
        }

        [Fact]
        public async Task OpenGenericDerivedType_WithStringDiscriminator_DeserializationWorks()
        {
            string json = """{"$type":"derived","Value":42}""";
            var result = await Serializer.DeserializeWrapper<OpenGenericBase_StringDisc<int>>(json);
            Assert.IsType<OpenGenericDerived_StringDisc<int>>(result);
            Assert.Equal(42, ((OpenGenericDerived_StringDisc<int>)result).Value);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_StringDisc<>), "derived")]
        public class OpenGenericBase_StringDisc<T>
        {
            public T? Value { get; set; }
        }

        public class OpenGenericDerived_StringDisc<T> : OpenGenericBase_StringDisc<T>;

        [Fact]
        public async Task OpenGenericDerivedType_WithIntDiscriminator_SerializationWorks()
        {
            OpenGenericBase_IntDisc<string> value = new OpenGenericDerived_IntDisc<string> { Value = "hello" };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":1,"Value":"hello"}""", json);
        }

        [Fact]
        public async Task OpenGenericDerivedType_WithIntDiscriminator_DeserializationWorks()
        {
            string json = """{"$type":1,"Value":"hello"}""";
            var result = await Serializer.DeserializeWrapper<OpenGenericBase_IntDisc<string>>(json);
            Assert.IsType<OpenGenericDerived_IntDisc<string>>(result);
            Assert.Equal("hello", ((OpenGenericDerived_IntDisc<string>)result).Value);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_IntDisc<>), 1)]
        public class OpenGenericBase_IntDisc<T>
        {
            public T? Value { get; set; }
        }

        public class OpenGenericDerived_IntDisc<T> : OpenGenericBase_IntDisc<T>;

        [Fact]
        public async Task OpenGenericDerivedType_MultipleDerivedTypes_Work()
        {
            OpenGenericBase_Multi<int> valueA = new OpenGenericDerivedA_Multi<int> { ValueA = 1 };
            OpenGenericBase_Multi<int> valueB = new OpenGenericDerivedB_Multi<int> { ValueB = 2 };

            string jsonA = await Serializer.SerializeWrapper(valueA);
            string jsonB = await Serializer.SerializeWrapper(valueB);

            JsonTestHelper.AssertJsonEqual("""{"$type":"a","ValueA":1}""", jsonA);
            JsonTestHelper.AssertJsonEqual("""{"$type":"b","ValueB":2}""", jsonB);

            var resultA = await Serializer.DeserializeWrapper<OpenGenericBase_Multi<int>>(jsonA);
            var resultB = await Serializer.DeserializeWrapper<OpenGenericBase_Multi<int>>(jsonB);

            Assert.IsType<OpenGenericDerivedA_Multi<int>>(resultA);
            Assert.IsType<OpenGenericDerivedB_Multi<int>>(resultB);
        }

        [JsonDerivedType(typeof(OpenGenericDerivedA_Multi<>), "a")]
        [JsonDerivedType(typeof(OpenGenericDerivedB_Multi<>), "b")]
        public class OpenGenericBase_Multi<T>;

        public class OpenGenericDerivedA_Multi<T> : OpenGenericBase_Multi<T>
        {
            public int ValueA { get; set; }
        }

        public class OpenGenericDerivedB_Multi<T> : OpenGenericBase_Multi<T>
        {
            public int ValueB { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_NestedClass_Works()
        {
            OpenGenericBase_Nested<int> value = new OpenGenericBase_Nested<int>.Derived();
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"nested"}""", json);
        }

        [JsonDerivedType(typeof(OpenGenericBase_Nested<>.Derived), "nested")]
        public class OpenGenericBase_Nested<T>
        {
            public class Derived : OpenGenericBase_Nested<T>;
        }

        [Fact]
        public async Task OpenGenericDerivedType_ComplexTypeArg_Works()
        {
            OpenGenericBase_ComplexArg<List<int>> value = new OpenGenericDerived_ComplexArg<List<int>> { Data = [1, 2, 3] };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Data":[1,2,3]}""", json);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_ComplexArg<>), "derived")]
        public class OpenGenericBase_ComplexArg<T>
        {
            public T? Data { get; set; }
        }

        public class OpenGenericDerived_ComplexArg<T> : OpenGenericBase_ComplexArg<T>;

        [Fact]
        public async Task OpenGenericDerivedType_WrappedTypeArg_Works()
        {
            // Derived<T> : Base<List<T>> registered on Base<List<string>> unifies to Derived<string>.
            OpenGenericBase_Wrapped<List<string>> value = new OpenGenericDerived_Wrapped<string> { Data = ["a", "b"] };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Data":["a","b"]}""", json);

            var result = await Serializer.DeserializeWrapper<OpenGenericBase_Wrapped<List<string>>>(json);
            Assert.IsType<OpenGenericDerived_Wrapped<string>>(result);
            Assert.Equal(new[] { "a", "b" }, ((OpenGenericDerived_Wrapped<string>)result).Data);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_Wrapped<>), "derived")]
        public class OpenGenericBase_Wrapped<T>
        {
            public T? Data { get; set; }
        }

        public class OpenGenericDerived_Wrapped<T> : OpenGenericBase_Wrapped<List<T>>;

        [Fact]
        public async Task OpenGenericDerivedType_Interface_Works()
        {
            IOpenGenericBase<int> value = new OpenGenericInterfaceImpl<int> { Value = 42 };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"impl","Value":42}""", json);

            var result = await Serializer.DeserializeWrapper<IOpenGenericBase<int>>(json);
            Assert.IsType<OpenGenericInterfaceImpl<int>>(result);
        }

        [JsonDerivedType(typeof(OpenGenericInterfaceImpl<>), "impl")]
        public interface IOpenGenericBase<T>
        {
            T? Value { get; set; }
        }

        public class OpenGenericInterfaceImpl<T> : IOpenGenericBase<T>
        {
            public T? Value { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_DifferentTypeArguments_ProduceDifferentResults()
        {
            OpenGenericBase_StringDisc<int> intValue = new OpenGenericDerived_StringDisc<int> { Value = 42 };
            OpenGenericBase_StringDisc<string> strValue = new OpenGenericDerived_StringDisc<string> { Value = "hello" };

            string intJson = await Serializer.SerializeWrapper(intValue);
            string strJson = await Serializer.SerializeWrapper(strValue);

            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Value":42}""", intJson);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Value":"hello"}""", strJson);
        }

        [ConditionalFact]
        public async Task OpenGenericDerivedType_NonGenericBase_ThrowsInvalidOperationException()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                throw new SkipTestException("Source generator rejects this invalid polymorphic configuration at build time (SYSLIB diagnostic); the runtime InvalidOperationException is validated under reflection only.");
            }

            var value = new NonGenericBaseWithOpenGenericDerived();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(NonGenericBaseWithOpenGenericDerived.OpenDerived<>), "derived")]
        public class NonGenericBaseWithOpenGenericDerived
        {
            public class OpenDerived<T> : NonGenericBaseWithOpenGenericDerived;
        }

        [ConditionalFact]
        public async Task OpenGenericDerivedType_TypeArgsNotResolvable_ThrowsInvalidOperationException()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                throw new SkipTestException("Source generator rejects this invalid polymorphic configuration at build time (SYSLIB diagnostic); the runtime InvalidOperationException is validated under reflection only.");
            }

            // Derived<T> : Base<int> - T cannot be determined from Base<int>
            var value = new OpenGenericBase_Unresolvable<int>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(OpenGenericDerived_Unresolvable<>), "derived")]
        public class OpenGenericBase_Unresolvable<T>
        {
            public T? Value { get; set; }
        }

        public class OpenGenericDerived_Unresolvable<T> : OpenGenericBase_Unresolvable<int>;

        [ConditionalFact]
        public async Task OpenGenericDerivedType_GroundMismatchAgainstClosedBase_ThrowsInvalidOperationException()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                throw new SkipTestException("Source generator rejects this invalid polymorphic configuration at build time (SYSLIB diagnostic); the runtime InvalidOperationException is validated under reflection only.");
            }

            // OpenGenericDerived_GroundMismatch<T> : OpenGenericBase_GroundMismatch<T, int>
            // registered on OpenGenericBase_GroundMismatch<int, string>.
            // Position 0 (T) unifies with int, but position 1 (concrete int in derived's base
            // spec) contradicts string in the closed base. The derived type is well-formed in
            // isolation but does not apply to this particular closed base, so the resolver
            // surfaces a loud diagnostic rather than silently dropping the registration.
            var value = new OpenGenericBase_GroundMismatch<int, string>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(OpenGenericDerived_GroundMismatch<>), "derived")]
        public class OpenGenericBase_GroundMismatch<T1, T2>;

        public class OpenGenericDerived_GroundMismatch<T> : OpenGenericBase_GroundMismatch<T, int>;

        [Fact]
        public async Task OpenGenericDerivedType_PartiallyConcrete_Works()
        {
            // Derived<T> : Base<T, int> registered on Base<string, int>:
            // position 0 (T) unifies with string, position 1 (concrete int) matches.
            // Expected: closed derived is OpenGenericDerived_PartiallyConcrete<string>, and
            // round-trip serialization emits and reads the $type discriminator.
            OpenGenericBase_PartiallyConcrete<string, int> value = new OpenGenericDerived_PartiallyConcrete<string> { Extra = "hello" };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Extra":"hello","Value1":null,"Value2":0}""", json);

            var result = await Serializer.DeserializeWrapper<OpenGenericBase_PartiallyConcrete<string, int>>(json);
            var derived = Assert.IsType<OpenGenericDerived_PartiallyConcrete<string>>(result);
            Assert.Equal("hello", derived.Extra);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_PartiallyConcrete<>), "derived")]
        public class OpenGenericBase_PartiallyConcrete<T1, T2>
        {
            public T1? Value1 { get; set; }
            public T2? Value2 { get; set; }
        }

        public class OpenGenericDerived_PartiallyConcrete<T> : OpenGenericBase_PartiallyConcrete<T, int>
        {
            public T? Extra { get; set; }
        }

        // Validates the runtime programmatic API by adding polymorphism to OpenGenericBase_Programmatic<int>
        // via a resolver modifier. The type is deliberately not registered with a JsonSerializerContext
        // (no [JsonDerivedType]), so it relies on the reflection-based DefaultJsonTypeInfoResolver and only
        // runs where runtime code generation is available; OpenGenericDerivedType_MixedWithRegularDerivedType_Works
        // covers the attribute-based equivalent that also runs under source-gen.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [RequiresUnreferencedCode("Uses DefaultJsonTypeInfoResolver and reflection-based serialization.")]
        [RequiresDynamicCode("Uses DefaultJsonTypeInfoResolver and reflection-based serialization.")]
        public async Task OpenGenericDerivedType_ProgrammaticApi_Works()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        static typeInfo =>
                        {
                            if (typeInfo.Type == typeof(OpenGenericBase_Programmatic<int>))
                            {
                                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                                {
                                    DerivedTypes =
                                    {
                                        new JsonDerivedType(typeof(OpenGenericDerived_Programmatic<int>), "derived"),
                                    }
                                };
                            }
                        }
                    }
                }
            };

            OpenGenericBase_Programmatic<int> value = new OpenGenericDerived_Programmatic<int> { Value = 99 };
            string json = JsonSerializer.Serialize(value, options);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Value":99}""", json);

            var result = JsonSerializer.Deserialize<OpenGenericBase_Programmatic<int>>(json, options);
            Assert.IsType<OpenGenericDerived_Programmatic<int>>(result);
            Assert.Equal(99, ((OpenGenericDerived_Programmatic<int>)result).Value);
        }

        public class OpenGenericBase_Programmatic<T>
        {
            public T? Value { get; set; }
        }

        public class OpenGenericDerived_Programmatic<T> : OpenGenericBase_Programmatic<T>;

        [Fact]
        public async Task OpenGenericDerivedType_MixedWithRegularDerivedType_Works()
        {
            // Validates that both regular and open generic derived types coexist.
            OpenGenericBase_Mixed<int> openValue = new OpenGenericDerived_Mixed<int> { Value = 1 };
            OpenGenericBase_Mixed<int> regularValue = new RegularDerived_Mixed { Value = 2, Extra = "extra" };

            string openJson = await Serializer.SerializeWrapper(openValue);
            string regularJson = await Serializer.SerializeWrapper(regularValue);

            JsonTestHelper.AssertJsonEqual("""{"$type":"open","Value":1}""", openJson);
            JsonTestHelper.AssertJsonEqual("""{"$type":"regular","Value":2,"Extra":"extra"}""", regularJson);

            var openResult = await Serializer.DeserializeWrapper<OpenGenericBase_Mixed<int>>(openJson);
            var regularResult = await Serializer.DeserializeWrapper<OpenGenericBase_Mixed<int>>(regularJson);

            Assert.IsType<OpenGenericDerived_Mixed<int>>(openResult);
            Assert.IsType<RegularDerived_Mixed>(regularResult);
            Assert.Equal(1, openResult.Value);
            Assert.Equal("extra", ((RegularDerived_Mixed)regularResult).Extra);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_Mixed<>), "open")]
        [JsonDerivedType(typeof(RegularDerived_Mixed), "regular")]
        public class OpenGenericBase_Mixed<T>
        {
            public T? Value { get; set; }
        }

        public class OpenGenericDerived_Mixed<T> : OpenGenericBase_Mixed<T>;

        public class RegularDerived_Mixed : OpenGenericBase_Mixed<int>
        {
            public string? Extra { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_InterfaceHierarchy_Works()
        {
            // Tests unification through a chain of generic interfaces:
            // IDerived<T> extends IBase<T>, and we serialize through IBase<int>.
            IOpenGenericBase_InterfaceHierarchy<int> value = new OpenGenericImpl_InterfaceHierarchy<int> { Value = 42, Extra = "extra" };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"impl","Value":42,"Extra":"extra"}""", json);

            var result = await Serializer.DeserializeWrapper<IOpenGenericBase_InterfaceHierarchy<int>>(json);
            Assert.IsType<OpenGenericImpl_InterfaceHierarchy<int>>(result);
            Assert.Equal(42, result.Value);
        }

        [JsonDerivedType(typeof(OpenGenericImpl_InterfaceHierarchy<>), "impl")]
        public interface IOpenGenericBase_InterfaceHierarchy<T>
        {
            T? Value { get; set; }
        }

        public interface IOpenGenericDerived_InterfaceHierarchy<T> : IOpenGenericBase_InterfaceHierarchy<T>;

        public class OpenGenericImpl_InterfaceHierarchy<T> : IOpenGenericDerived_InterfaceHierarchy<T>
        {
            public T? Value { get; set; }
            public string? Extra { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_InterfaceBaseWithWrappedTypeArg_Works()
        {
            // Impl<T> implements IBase<List<T>> registered on IBase<List<string>> unifies to Impl<string>.
            IOpenGenericBase_InterfaceWrapped<List<string>> value = new OpenGenericImpl_InterfaceWrapped<string> { Data = ["a", "b"] };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"impl","Data":["a","b"]}""", json);

            var result = await Serializer.DeserializeWrapper<IOpenGenericBase_InterfaceWrapped<List<string>>>(json);
            Assert.IsType<OpenGenericImpl_InterfaceWrapped<string>>(result);
            Assert.Equal(new[] { "a", "b" }, ((OpenGenericImpl_InterfaceWrapped<string>)result).Data);
        }

        [JsonDerivedType(typeof(OpenGenericImpl_InterfaceWrapped<>), "impl")]
        public interface IOpenGenericBase_InterfaceWrapped<T>
        {
            T? Data { get; set; }
        }

        public class OpenGenericImpl_InterfaceWrapped<T> : IOpenGenericBase_InterfaceWrapped<List<T>>
        {
            public List<T>? Data { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_ArrayTypeArg_Works()
        {
            // Derived<T> : Base<T[]> registered on Base<int[]> unifies to Derived<int>.
            OpenGenericBase_ArrayArg<int[]> value = new OpenGenericDerived_ArrayArg<int> { Values = [1, 2, 3] };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Values":[1,2,3]}""", json);

            var result = await Serializer.DeserializeWrapper<OpenGenericBase_ArrayArg<int[]>>(json);
            Assert.IsType<OpenGenericDerived_ArrayArg<int>>(result);
            Assert.Equal(new[] { 1, 2, 3 }, ((OpenGenericDerived_ArrayArg<int>)result).Values);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_ArrayArg<>), "derived")]
        public class OpenGenericBase_ArrayArg<T>
        {
            public T? Values { get; set; }
        }

        public class OpenGenericDerived_ArrayArg<T> : OpenGenericBase_ArrayArg<T[]>
        {
            public new T[]? Values { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_ReorderedParameters_Works()
        {
            // Derived<T1, T2> : Base<T2, T1> registered on Base<int, string> unifies to Derived<string, int>.
            OpenGenericBase_Reordered<int, string> value = new OpenGenericDerived_Reordered<string, int> { Left = "left", Right = 42 };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Left":"left","Right":42}""", json);

            var result = await Serializer.DeserializeWrapper<OpenGenericBase_Reordered<int, string>>(json);
            Assert.IsType<OpenGenericDerived_Reordered<string, int>>(result);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_Reordered<,>), "derived")]
        public class OpenGenericBase_Reordered<T1, T2>;

        public class OpenGenericDerived_Reordered<T1, T2> : OpenGenericBase_Reordered<T2, T1>
        {
            public T1? Left { get; set; }
            public T2? Right { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_PartialConcretization_Works()
        {
            // Derived<T> : Base<T, int> registered on Base<string, int> unifies to Derived<string>.
            OpenGenericBase_Partial<string, int> value = new OpenGenericDerived_Partial<string> { Value = "hello" };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Value":"hello"}""", json);

            var result = await Serializer.DeserializeWrapper<OpenGenericBase_Partial<string, int>>(json);
            Assert.IsType<OpenGenericDerived_Partial<string>>(result);
            Assert.Equal("hello", ((OpenGenericDerived_Partial<string>)result).Value);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_Partial<>), "derived")]
        public class OpenGenericBase_Partial<T1, T2>;

        public class OpenGenericDerived_Partial<T> : OpenGenericBase_Partial<T, int>
        {
            public T? Value { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_KeyValuePairArg_Works()
        {
            // Derived<T> : Base<KeyValuePair<string, T>> registered on Base<KeyValuePair<string, int>> unifies to Derived<int>.
            OpenGenericBase_KvpArg<KeyValuePair<string, int>> value = new OpenGenericDerived_KvpArg<int> { Pair = new KeyValuePair<string, int>("k", 99) };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Pair":{"Key":"k","Value":99}}""", json);

            var result = await Serializer.DeserializeWrapper<OpenGenericBase_KvpArg<KeyValuePair<string, int>>>(json);
            Assert.IsType<OpenGenericDerived_KvpArg<int>>(result);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_KvpArg<>), "derived")]
        public class OpenGenericBase_KvpArg<T>;

        public class OpenGenericDerived_KvpArg<T> : OpenGenericBase_KvpArg<KeyValuePair<string, T>>
        {
            public KeyValuePair<string, T> Pair { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_MultiLevelInheritance_Works()
        {
            // Mid<T> : Base<List<T>>, Leaf<T> : Mid<T>; registered on Base<List<int>> unifies to Leaf<int>.
            OpenGenericBase_MultiLevel<List<int>> value = new OpenGenericLeaf_MultiLevel<int> { Items = [10, 20] };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"leaf","Items":[10,20]}""", json);

            var result = await Serializer.DeserializeWrapper<OpenGenericBase_MultiLevel<List<int>>>(json);
            Assert.IsType<OpenGenericLeaf_MultiLevel<int>>(result);
        }

        [JsonDerivedType(typeof(OpenGenericLeaf_MultiLevel<>), "leaf")]
        public class OpenGenericBase_MultiLevel<T>;

        public class OpenGenericMid_MultiLevel<T> : OpenGenericBase_MultiLevel<List<T>>;

        public class OpenGenericLeaf_MultiLevel<T> : OpenGenericMid_MultiLevel<T>
        {
            public List<T>? Items { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_TupleSyntax_Works()
        {
            // Derived<T1, T2> : Base<(T1, T2)> registered on Base<(int, string)> unifies to Derived<int, string>.
            OpenGenericBase_Tuple<(int, string)> value = new OpenGenericDerived_Tuple<int, string> { Pair = (5, "x") };
            string json = await Serializer.SerializeWrapper(value);

            var result = await Serializer.DeserializeWrapper<OpenGenericBase_Tuple<(int, string)>>(json);
            Assert.IsType<OpenGenericDerived_Tuple<int, string>>(result);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_Tuple<,>), "derived")]
        public class OpenGenericBase_Tuple<T>;

        public class OpenGenericDerived_Tuple<T1, T2> : OpenGenericBase_Tuple<(T1, T2)>
        {
            public (T1, T2) Pair { get; set; }
        }

        [ConditionalFact]
        public async Task OpenGenericDerivedType_AmbiguousInterfaceMatch_ThrowsInvalidOperationException()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                throw new SkipTestException("Source generator rejects this invalid polymorphic configuration at build time (SYSLIB diagnostic); the runtime InvalidOperationException is validated under reflection only.");
            }

            // Impl<T> : IBase<T>, IBase<List<T>> registered on IBase<List<int>>.
            // Both ancestors unify (T=List<int> via the first interface, T=int via the second).
            // Result: ambiguous, throws.
            IOpenGenericBase_Ambiguous<List<int>> value = new OpenGenericImpl_Ambiguous<int>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(OpenGenericImpl_Ambiguous<>), "impl")]
        public interface IOpenGenericBase_Ambiguous<T>;

        public class OpenGenericImpl_Ambiguous<T> : IOpenGenericBase_Ambiguous<T>, IOpenGenericBase_Ambiguous<List<T>>;

        [ConditionalFact]
        public async Task OpenGenericDerivedType_UnboundParameter_ThrowsInvalidOperationException()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                throw new SkipTestException("Source generator rejects this invalid polymorphic configuration at build time (SYSLIB diagnostic); the runtime InvalidOperationException is validated under reflection only.");
            }

            // Derived<T1, T2> : Base<T1> — T2 is unspeakable (not bound by the base type's args).
            var value = new OpenGenericBase_Unbound<int>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(OpenGenericDerived_Unbound<,>), "derived")]
        public class OpenGenericBase_Unbound<T>;

        public class OpenGenericDerived_Unbound<T1, T2> : OpenGenericBase_Unbound<T1>;

        [ConditionalFact]
        public async Task OpenGenericDerivedType_ConstraintViolation_ThrowsInvalidOperationException()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                throw new SkipTestException("Source generator rejects this invalid polymorphic configuration at build time (SYSLIB diagnostic); the runtime InvalidOperationException is validated under reflection only.");
            }

            // Derived<T> : Base<T> where T : struct, registered on Base<string>.
            // Constraint fails → InvalidOperationException.
            var value = new OpenGenericBase_StructConstraint<string>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(OpenGenericDerived_StructConstraint<>), "derived")]
        public class OpenGenericBase_StructConstraint<T>;

        public class OpenGenericDerived_StructConstraint<T> : OpenGenericBase_StructConstraint<T>
            where T : struct;

        [Fact]
        public async Task OpenGenericDerivedType_NullableAnnotationOnTypeArg_Works()
        {
            // Reflection does not preserve nullable annotations; Derived<T> : Base<T> on Base<string> just works.
            // (We verify the absence of any nullable-annotation-related rejection.)
            OpenGenericBase_NullableArg<string> value = new OpenGenericDerived_NullableArg<string> { Value = "hello" };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"derived","Value":"hello"}""", json);
        }

        [JsonDerivedType(typeof(OpenGenericDerived_NullableArg<>), "derived")]
        public class OpenGenericBase_NullableArg<T>
        {
            public T? Value { get; set; }
        }

        public class OpenGenericDerived_NullableArg<T> : OpenGenericBase_NullableArg<T>;

        [Fact]
        public async Task OpenGenericDerivedType_DuplicateClosedAndOpenRegistration_ThrowsInvalidOperationException()
        {
            // Base<int> has BOTH a closed-form Derived<int> registration AND an open-form
            // Derived<> registration. The open form closes to Derived<int>, producing a
            // duplicate derived-type registration. The existing dup-detection in
            // PolymorphicTypeResolver must surface this as InvalidOperationException.
            // (Source generator accepts the configuration without warning; the runtime resolver
            // catches the duplicate, so this scenario validates the same runtime path under both engines.)
            OpenGenericBase_DuplicateDerivedRegistrations<int> value = new OpenGenericDerived_DuplicateDerivedRegistrations<int>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(OpenGenericDerived_DuplicateDerivedRegistrations<int>), "closed")]
        [JsonDerivedType(typeof(OpenGenericDerived_DuplicateDerivedRegistrations<>), "open")]
        public class OpenGenericBase_DuplicateDerivedRegistrations<T>;

        public class OpenGenericDerived_DuplicateDerivedRegistrations<T> : OpenGenericBase_DuplicateDerivedRegistrations<T>;

        [Fact]
        public async Task OpenGenericDerivedType_MultipleGenericInterfaceBases_EachResolvesIndependently_Works()
        {
            // Impl<T> implements two unrelated generic interfaces, each carrying its own
            // open-generic [JsonDerivedType(typeof(Impl<>))]. Serializing through either
            // interface base resolves the same closed Impl<T> independently.
            IOpenGenericBase_MultiBaseA<int> viaA = new OpenGenericImpl_MultiBase<int> { ValueA = 1, ValueB = "x" };
            IOpenGenericBase_MultiBaseB<int> viaB = (OpenGenericImpl_MultiBase<int>)viaA;

            string jsonA = await Serializer.SerializeWrapper(viaA);
            string jsonB = await Serializer.SerializeWrapper(viaB);

            JsonTestHelper.AssertJsonEqual("""{"$type":"impl","ValueA":1,"ValueB":"x"}""", jsonA);
            JsonTestHelper.AssertJsonEqual("""{"$type":"impl","ValueA":1,"ValueB":"x"}""", jsonB);

            var roundA = await Serializer.DeserializeWrapper<IOpenGenericBase_MultiBaseA<int>>(jsonA);
            var roundB = await Serializer.DeserializeWrapper<IOpenGenericBase_MultiBaseB<int>>(jsonB);

            var implA = Assert.IsType<OpenGenericImpl_MultiBase<int>>(roundA);
            var implB = Assert.IsType<OpenGenericImpl_MultiBase<int>>(roundB);
            Assert.Equal(1, implA.ValueA);
            Assert.Equal(1, implB.ValueA);
        }

        [JsonDerivedType(typeof(OpenGenericImpl_MultiBase<>), "impl")]
        public interface IOpenGenericBase_MultiBaseA<T>
        {
            T? ValueA { get; set; }
        }

        [JsonDerivedType(typeof(OpenGenericImpl_MultiBase<>), "impl")]
        public interface IOpenGenericBase_MultiBaseB<T>
        {
            string? ValueB { get; set; }
        }

        public class OpenGenericImpl_MultiBase<T> : IOpenGenericBase_MultiBaseA<T>, IOpenGenericBase_MultiBaseB<T>
        {
            public T? ValueA { get; set; }
            public string? ValueB { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_GenericInterfaceDiamond_Works()
        {
            // Diamond inheritance: IDerived<T> extends both IBaseA<T> and IBaseB<T>, and
            // Impl<T> implements IDerived<T>. Open-generic [JsonDerivedType(typeof(Impl<>))]
            // is declared on each of the two diamond legs. Serializing through either leg
            // must independently resolve Impl<T> through the diamond.
            IOpenGenericBase_DiamondA<int> viaA = new OpenGenericImpl_Diamond<int> { Common = 7 };
            IOpenGenericBase_DiamondB<int> viaB = (OpenGenericImpl_Diamond<int>)viaA;

            string jsonA = await Serializer.SerializeWrapper(viaA);
            string jsonB = await Serializer.SerializeWrapper(viaB);

            JsonTestHelper.AssertJsonEqual("""{"$type":"impl","Common":7}""", jsonA);
            JsonTestHelper.AssertJsonEqual("""{"$type":"impl","Common":7}""", jsonB);

            var roundA = await Serializer.DeserializeWrapper<IOpenGenericBase_DiamondA<int>>(jsonA);
            var roundB = await Serializer.DeserializeWrapper<IOpenGenericBase_DiamondB<int>>(jsonB);

            Assert.IsType<OpenGenericImpl_Diamond<int>>(roundA);
            Assert.IsType<OpenGenericImpl_Diamond<int>>(roundB);
        }

        [JsonDerivedType(typeof(OpenGenericImpl_Diamond<>), "impl")]
        public interface IOpenGenericBase_DiamondA<T>
        {
            T? Common { get; set; }
        }

        [JsonDerivedType(typeof(OpenGenericImpl_Diamond<>), "impl")]
        public interface IOpenGenericBase_DiamondB<T>
        {
            T? Common { get; set; }
        }

        public interface IOpenGenericDerived_Diamond<T> : IOpenGenericBase_DiamondA<T>, IOpenGenericBase_DiamondB<T>;

        public class OpenGenericImpl_Diamond<T> : IOpenGenericDerived_Diamond<T>
        {
            public T? Common { get; set; }
        }

        [Fact]
        public async Task OpenGenericDerivedType_MultipleInterfaceConstructions_NonAmbiguousResolution_Works()
        {
            // Impl<T> reaches IBase<> twice: once via its own type-parameterized interface
            // (IBase<T>) and once via inheritance from the non-generic IntBase (IBase<int>).
            // When the closed base is IBase<string>, only the IBase<T> ancestor unifies
            // (T=string); the IBase<int> ancestor is incompatible. Resolution must succeed
            // and produce Impl<string>. (The both-legs-match scenario is the ambiguous one,
            // covered separately.) Indirecting the IBase<int> leg through a non-generic base
            // class avoids C# CS0695 -- a class cannot directly declare two constructions of
            // the same generic interface that could unify under any substitution.
            IOpenGenericBase_MultiCtor<string> value = new OpenGenericImpl_MultiCtor<string> { Item = "hello" };
            string json = await Serializer.SerializeWrapper(value);

            JsonTestHelper.AssertJsonEqual("""{"$type":"impl","Item":"hello"}""", json);

            var result = await Serializer.DeserializeWrapper<IOpenGenericBase_MultiCtor<string>>(json);
            var impl = Assert.IsType<OpenGenericImpl_MultiCtor<string>>(result);
            Assert.Equal("hello", impl.Item);
        }

        [JsonDerivedType(typeof(OpenGenericImpl_MultiCtor<>), "impl")]
        public interface IOpenGenericBase_MultiCtor<T>;

        public class OpenGenericImpl_MultiCtor_IntBase : IOpenGenericBase_MultiCtor<int>;

        public class OpenGenericImpl_MultiCtor<T> : OpenGenericImpl_MultiCtor_IntBase, IOpenGenericBase_MultiCtor<T>
        {
            public T? Item { get; set; }
        }

        #endregion

        #region Generic Variance Tests

        // Shared fixtures for variance scenarios.
        public class VarAnimal { public string? Name { get; set; } }
        public sealed class VarDog : VarAnimal { public string? Breed { get; set; } }

        // Covariant interface base. Closing T to VarAnimal still admits VarCovImpl<VarDog>
        // at the IS-A level via the 'out' modifier, but the resolver registers only the
        // closed VarCovImpl<VarAnimal> and a runtime VarCovImpl<VarDog> is not a key in
        // the discriminator dictionary.
        [JsonDerivedType(typeof(VarCovImpl<>), "covImpl")]
        public interface IVarCovBase<out T> { }
        public class VarCovImpl<T> : IVarCovBase<T> { public T? Value { get; set; } }

        // Contravariant interface base. Mirror story for 'in' on the negative side.
        [JsonDerivedType(typeof(VarContraImpl<>), "contraImpl")]
        public interface IVarContraBase<in T> { }
        public class VarContraImpl<T> : IVarContraBase<T> { public string? Marker { get; set; } }

        // Mixed-variance interface (in TIn, out TOut).
        [JsonDerivedType(typeof(VarBivariantImpl<,>), "bvImpl")]
        public interface IVarBivariantBase<in TIn, out TOut> { }
        public class VarBivariantImpl<TIn, TOut> : IVarBivariantBase<TIn, TOut> { public TOut? Out { get; set; } }

        [Fact]
        public async Task Variance_CovariantInterface_ExactMatch_EmitsDiscriminator()
        {
            // Exact-type match: runtime VarCovImpl<VarAnimal> is in the discriminator dictionary.
            IVarCovBase<VarAnimal> value = new VarCovImpl<VarAnimal> { Value = new VarAnimal { Name = "Rex" } };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"covImpl","Value":{"Name":"Rex"}}""", json);

            var result = await Serializer.DeserializeWrapper<IVarCovBase<VarAnimal>>(json);
            var impl = Assert.IsType<VarCovImpl<VarAnimal>>(result);
            Assert.Equal("Rex", impl.Value!.Name);
        }

        [Fact]
        public async Task Variance_CovariantInterface_VarianceOnlyAssignment_DefaultThrows()
        {
            // Pranav's exact scenario: runtime value VarCovImpl<VarDog> is assignable to the
            // closed base IVarCovBase<VarAnimal> only via covariance on 'out T'. The unification
            // step is purely structural and closes the open generic to VarCovImpl<VarAnimal>;
            // the runtime type VarCovImpl<VarDog> is therefore NOT in the discriminator dict.
            // Default UnknownDerivedTypeHandling = FailSerialization -> NotSupportedException.
            IVarCovBase<VarAnimal> value = new VarCovImpl<VarDog> { Value = new VarDog { Name = "Rex", Breed = "Labrador" } };
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(value));
        }

        [Fact]
        public async Task Variance_CovariantInterface_VarianceOnlyAssignment_FallBackToBaseType_SerializesAsBase()
        {
            // Same as above with explicit FallBackToBaseType. The resolver falls through to the
            // base contract (no discriminator emitted) because the runtime type is not registered.
            // This matches the previously-described "serializes as base contract" behavior.
            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(IVarCovBase<VarAnimal>) && typeInfo.PolymorphismOptions is { } pOpts)
                {
                    pOpts.UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType;
                }
            });

            IVarCovBase<VarAnimal> value = new VarCovImpl<VarDog> { Value = new VarDog { Name = "Rex", Breed = "Labrador" } };
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("{}", json);
        }

        [Fact]
        public async Task Variance_ContravariantInterface_ExactMatch_EmitsDiscriminator()
        {
            // Exact-type match for the contravariant base: VarContraImpl<VarDog> with property
            // typed IVarContraBase<VarDog>. Discriminator should be emitted.
            IVarContraBase<VarDog> value = new VarContraImpl<VarDog> { Marker = "dog-impl" };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"contraImpl","Marker":"dog-impl"}""", json);
        }

        [Fact]
        public async Task Variance_ContravariantInterface_VarianceOnlyAssignment_DefaultThrows()
        {
            // Contravariant analogue of Pranav's case: VarContraImpl<VarAnimal> assigned to
            // IVarContraBase<VarDog> through 'in T' contravariance. Resolved derived type
            // closes to VarContraImpl<VarDog>; runtime type VarContraImpl<VarAnimal> is not
            // in the discriminator dict; default mode throws.
            IVarContraBase<VarDog> value = new VarContraImpl<VarAnimal> { Marker = "animal-impl" };
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(value));
        }

        [Fact]
        public async Task Variance_BivariantInterface_ExactMatch_EmitsDiscriminator()
        {
            // Both TIn and TOut bound exactly.
            IVarBivariantBase<VarDog, VarAnimal> value = new VarBivariantImpl<VarDog, VarAnimal> { Out = new VarAnimal { Name = "Charlie" } };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"bvImpl","Out":{"Name":"Charlie"}}""", json);
        }

        [Fact]
        public async Task Variance_BivariantInterface_BothViaVariance_DefaultThrows()
        {
            // TIn satisfied via contravariance (Animal -> Dog), TOut via covariance (Dog -> Animal).
            // Both axes engage variance; resolved derived closes to VarBivariantImpl<VarDog, VarAnimal>;
            // runtime VarBivariantImpl<VarAnimal, VarDog> is not in the dict; default mode throws.
            IVarBivariantBase<VarDog, VarAnimal> value = new VarBivariantImpl<VarAnimal, VarDog> { Out = new VarDog { Name = "Buddy", Breed = "Beagle" } };
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(value));
        }

        // Nested-generic ContainingType tests (matching Tarek's B2 concern). The source-gen
        // unification helper now walks ContainingType to match reflection's flattened-args
        // behavior. The reflection side has always handled these cases correctly because
        // Type.GetGenericArguments() returns enclosing+leaf args together.

        [ConditionalFact]
        public async Task NestedGeneric_EnclosingMismatch_ThrowsInvalidOperationException()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                throw new SkipTestException("Source generator rejects this invalid polymorphic configuration at build time (SYSLIB diagnostic); the runtime InvalidOperationException is validated under reflection only.");
            }

            // Pattern: NestedDerivedEnclosingMismatch<T> : NestedBase<NestedOuter<int>.NestedBox<T>>.
            // Target: NestedBase<NestedOuter<string>.NestedBox<int>>.
            // The enclosing argument differs (int vs string) so unification MUST fail. The
            // resolver runs at first-use of the base type; serializing any value typed as the
            // closed base surfaces the failure.
            // Pre-B2-fix source-gen would have false-accepted (T=int) by ignoring the enclosing
            // arg mismatch; reflection has always rejected.
            var value = new NestedBase<NestedOuter<string>.NestedBox<int>>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(NestedDerivedEnclosingMismatch<>), "nested")]
        public class NestedBase<T> { public T? Item { get; set; } }
        public class NestedOuter<TOuter> { public class NestedBox<TInner> { public TInner? Inner { get; set; } } }
        public class NestedDerivedEnclosingMismatch<T> : NestedBase<NestedOuter<int>.NestedBox<T>>;

        [Fact]
        public async Task NestedGeneric_TypeParameterInEnclosing_Resolves()
        {
            // Pattern: NestedDerivedParamInEnclosing<T> : NestedBaseB<NestedOuterB<T>.NestedBoxB<int>>.
            // Target: NestedBaseB<NestedOuterB<string>.NestedBoxB<int>>.
            // T appears only in the ENCLOSING type's argument list. Reflection has always
            // resolved this correctly because GetGenericArguments() flattens. Pre-B2-fix
            // source-gen would have false-rejected because TryUnifyWith only walked leaf
            // TypeArguments (T was never bound).
            NestedBaseB<NestedOuterB<string>.NestedBoxB<int>> value =
                new NestedDerivedParamInEnclosing<string> { Item = new NestedOuterB<string>.NestedBoxB<int> { Inner = 42 } };

            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"nestedB","Item":{"Inner":42}}""", json);
        }

        [JsonDerivedType(typeof(NestedDerivedParamInEnclosing<>), "nestedB")]
        public class NestedBaseB<T> { public T? Item { get; set; } }
        public class NestedOuterB<TOuter> { public class NestedBoxB<TInner> { public TInner? Inner { get; set; } } }
        public class NestedDerivedParamInEnclosing<T> : NestedBaseB<NestedOuterB<T>.NestedBoxB<int>>;

        // Variance + constraint test (Tarek's B3 concern). Reflection has always handled
        // variance-satisfying constraints correctly because Type.MakeGenericType respects
        // implicit conversions including interface variance. The source-gen mirror was
        // changed to use Compilation.HasImplicitConversion for the same parity.

        [Fact]
        public async Task Variance_CovariantInterfaceConstraintSatisfied_Resolves()
        {
            // Constraint: where T : IEnumerable<object>. Closing T to List<string> satisfies
            // the constraint ONLY via IEnumerable<out T> covariance (IEnumerable<string> is
            // assignable to IEnumerable<object> only by virtue of 'out T'). Reflection
            // resolves successfully; serialization emits the discriminator.
            ConstraintBase<List<string>> value = new ConstraintImpl<List<string>> { Items = new List<string> { "hello" } };
            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"$type":"impl","Items":["hello"]}""", json);
        }

        [JsonDerivedType(typeof(ConstraintImpl<>), "impl")]
        public class ConstraintBase<T> { public T? Items { get; set; } }
        public class ConstraintImpl<T> : ConstraintBase<T> where T : IEnumerable<object> { }

        #endregion

        [Fact]
        public async Task PolymorphicClass_CustomConverter_TypeDiscriminator_Serialization_ThrowsNotSupportedException()
        {
            PolymorphicClass_CustomConverter_TypeDiscriminator value = new PolymorphicClass_CustomConverter_TypeDiscriminator.DerivedClass();
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(value));
        }

        [Fact]
        public async Task PolymorphicClass_CustomConverter_TypeDiscriminator_Deserialization_ThrowsNotSupportedException()
        {
            string json = """{ "$type" : "derivedClass" }""";
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<PolymorphicClass_CustomConverter_TypeDiscriminator>(json));
        }

        [JsonConverter(typeof(CustomConverter))]
        [JsonDerivedType(typeof(DerivedClass), "derivedClass")]
        public class PolymorphicClass_CustomConverter_TypeDiscriminator
        {
            public class DerivedClass : PolymorphicClass_CustomConverter_TypeDiscriminator
            {
            }

            public class CustomConverter : JsonConverter<PolymorphicClass_CustomConverter_TypeDiscriminator>
            {
                public override PolymorphicClass_CustomConverter_TypeDiscriminator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    reader.TrySkip();
                    return null;
                }

                public override void Write(Utf8JsonWriter writer, PolymorphicClass_CustomConverter_TypeDiscriminator value, JsonSerializerOptions options)
                    => writer.WriteNullValue();
            }
        }

        [Fact]
        public async Task PolymorphicAbstractClass_NoDiscriminatorInPayload_ThrowsNotSupportedException()
        {
            string json = """{"Value" : 42}""";
            NotSupportedException exn = await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<PolymorphicAbstractClass>(json));
            Assert.Contains($"The JSON payload for polymorphic interface or abstract type '{typeof(PolymorphicAbstractClass)}' must specify a type discriminator.", exn.Message);
        }

        [JsonDerivedType(typeof(Derived), "derived")]
        public abstract class PolymorphicAbstractClass
        {
            public int Value { get; set; }

            public class Derived : PolymorphicAbstractClass;
        }

        [Fact]
        public async Task PolymorphicClass_CustomConverter_NoTypeDiscriminator_Serialization()
        {
            var value = new PolymorphicClass_CustomConverter_NoTypeDiscriminator.DerivedClass { Number = 42 };
            string expectedJson = """{ "Number" : 42 }""";
            string actualJson = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Fact]
        public async Task PolymorphicClass_CustomConverter_NoTypeDiscriminator_Deserialization()
        {
            string json = """{ "Number" : 42 }""";
            PolymorphicClass_CustomConverter_NoTypeDiscriminator result = await Serializer.DeserializeWrapper<PolymorphicClass_CustomConverter_NoTypeDiscriminator>(json);
            Assert.Null(result);
        }

        [JsonConverter(typeof(CustomConverter))]
        [JsonDerivedType(typeof(DerivedClass))]
        public class PolymorphicClass_CustomConverter_NoTypeDiscriminator
        {
            public class DerivedClass : PolymorphicClass_CustomConverter_NoTypeDiscriminator
            {
                public int Number { get; set; }
            }

            public class CustomConverter : JsonConverter<PolymorphicClass_CustomConverter_NoTypeDiscriminator>
            {
                public override PolymorphicClass_CustomConverter_NoTypeDiscriminator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    reader.TrySkip();
                    return null;
                }

                public override void Write(Utf8JsonWriter writer, PolymorphicClass_CustomConverter_NoTypeDiscriminator value, JsonSerializerOptions options)
                    => writer.WriteNullValue();
            }
        }

        [Theory]
        [InlineData("$id")]
        [InlineData("$ref")]
        [InlineData("$values")]
        public async Task PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName_ThrowsInvalidOperationException(string invalidPropertyName)
        {
            JsonSerializerOptions? options = PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName.CreatePolymorphicConfigurationWithCustomPropertyName(invalidPropertyName);
            PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName value = new PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName.DerivedClass();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value, options));
        }

        [Fact]
        public async Task PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName_PassingDefaultPropertyNameAsCustomParameter_ShouldSucceed()
        {
            JsonSerializerOptions? options = PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName.CreatePolymorphicConfigurationWithCustomPropertyName("$type");
            PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName value = new PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName.DerivedClass { Number = 42 };

            string expectedJson = """{ "$type" : "derivedClass", "Number" : 42 }""";
            string actualJson = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Theory]
        [InlineData(@"")]
        [InlineData(@" ")]
        [InlineData(@"\t")]
        [InlineData(@"\r\n")]
        [InlineData("""{ "lol" : true }""")]
        public async Task PolymorphicClass_DegenerateCustomPropertyNames_ShouldSucceed(string propertyName)
        {
            JsonSerializerOptions? options = PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName.CreatePolymorphicConfigurationWithCustomPropertyName(propertyName);
            PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName value = new PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName.DerivedClass { Number = 42 };

            string expectedJson = @$"{{ ""{JavaScriptEncoder.Default.Encode(propertyName)}"" : ""derivedClass"", ""Number"" : 42 }}";
            string actualJson = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);

            PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName deserializeResult = await Serializer.DeserializeWrapper<PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName>(actualJson, options);
            Assert.IsType<PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName.DerivedClass>(deserializeResult);
        }

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "$id")]
        [JsonDerivedType(typeof(DerivedClass), "derivedClass")]
        public class PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName
        {
            public int Number { get; set; }

            public class DerivedClass : PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName
            {
            }

            public static JsonSerializerOptions? CreatePolymorphicConfigurationWithCustomPropertyName(string customPropertyName)
            {
                if (customPropertyName == "$id")
                {
                    // revert to attribute configuration
                    return null;
                }

                return new JsonSerializerOptions
                {
                    TypeInfoResolver = new CustomPolymorphismResolver<PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName>
                    {
                        TypeDiscriminatorPropertyName = customPropertyName
                    }
                    .WithDerivedType<DerivedClass>("derivedClass")
                };
            }
        }

        #endregion

        #region Regression Tests

        [Fact]
        public async Task PolymorphicClassWithEscapedTypeDiscriminator_RoundtripsCorrectly()
        {
            PolymorphicClassWithEscapedTypeDiscriminator value = new PolymorphicClassWithEscapedTypeDiscriminator.Derived();
            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal("""{"cat\u00E9gorie":"derived"}""", json);

            PolymorphicClassWithEscapedTypeDiscriminator result = await Serializer.DeserializeWrapper<PolymorphicClassWithEscapedTypeDiscriminator>(json);
            Assert.IsType<PolymorphicClassWithEscapedTypeDiscriminator.Derived>(result);
        }

        [Fact]
        public async Task PolymorphicClassWithEscapedTypeDiscriminator_ReadsEscapedValues()
        {
            string json = """{"\u0063\u0061\u0074\u00e9\u0067\u006f\u0072\u0069\u0065":"derived"}""";
            PolymorphicClassWithEscapedTypeDiscriminator result = await Serializer.DeserializeWrapper<PolymorphicClassWithEscapedTypeDiscriminator>(json);
            Assert.IsType<PolymorphicClassWithEscapedTypeDiscriminator.Derived>(result);
        }

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "catégorie")]
        [JsonDerivedType(typeof(Derived), "derived")]
        public class PolymorphicClassWithEscapedTypeDiscriminator
        {
            public class Derived : PolymorphicClassWithEscapedTypeDiscriminator;
        }
        #endregion

        #region Test Helpers

        public class CustomPolymorphismResolver : IJsonTypeInfoResolver
        {
            #if BUILDING_SOURCE_GENERATOR_TESTS
            private readonly IJsonTypeInfoResolver _inner = System.Text.Json.SourceGeneration.Tests.PolymorphicTests_Metadata.PolymorphicTestsContext_Metadata.Default;
            #else
            private readonly DefaultJsonTypeInfoResolver _inner = new();
            #endif
            private readonly List<JsonDerivedType> _jsonDerivedTypes = new();

            public CustomPolymorphismResolver(Type baseType)
            {
                BaseType = baseType;
            }

            public Type BaseType { get; }
            public bool IgnoreUnrecognizedTypeDiscriminators { get; set; }
            public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; set; }
            public string? TypeDiscriminatorPropertyName { get; set; }

            public CustomPolymorphismResolver WithDerivedType(JsonDerivedType jsonDerivedType)
            {
                _jsonDerivedTypes.Add(jsonDerivedType);
                return this;
            }

            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                JsonTypeInfo? jsonTypeInfo = _inner.GetTypeInfo(type, options);
                if (jsonTypeInfo is not null && jsonTypeInfo.Type == BaseType)
                {
                    jsonTypeInfo.PolymorphismOptions = new()
                    {
                        IgnoreUnrecognizedTypeDiscriminators = IgnoreUnrecognizedTypeDiscriminators,
                        UnknownDerivedTypeHandling = UnknownDerivedTypeHandling,
                        TypeDiscriminatorPropertyName = TypeDiscriminatorPropertyName,
                    };

                    foreach (JsonDerivedType derivedType in _jsonDerivedTypes)
                    {
                        jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(derivedType);
                    }
                }

                return jsonTypeInfo;
            }
        }

        public class CustomPolymorphismResolver<TBaseType> : CustomPolymorphismResolver
            where TBaseType : class
        {
            public CustomPolymorphismResolver() : base(typeof(TBaseType))
            {
            }

            public CustomPolymorphismResolver<TBaseType> WithDerivedType<TDerivedType>() where TDerivedType : TBaseType
            {
                WithDerivedType(new JsonDerivedType(typeof(TDerivedType)));
                return this;
            }

            public CustomPolymorphismResolver<TBaseType> WithDerivedType<TDerivedType>(int typeDiscriminatorId) where TDerivedType : TBaseType
            {
                WithDerivedType(new JsonDerivedType(typeof(TDerivedType), typeDiscriminatorId));
                return this;
            }

            public CustomPolymorphismResolver<TBaseType> WithDerivedType<TDerivedType>(string typeDiscriminatorId) where TDerivedType : TBaseType
            {
                WithDerivedType(new JsonDerivedType(typeof(TDerivedType), typeDiscriminatorId));
                return this;
            }
        }
        #endregion
    }
}
