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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicClass>.Instance);

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

            await TestMultiContextDeserialization(inputs, equalityComparer: PolymorphicEqualityComparer<PolymorphicClass>.Instance);
        }

        [Theory]
        [InlineData("$.$type", @"{ ""$type"" : ""derivedClass1"", ""$type"" : ""derivedClass1"", ""Number"" : 42 }")]
        [InlineData("$.$type", @"{ ""$type"" : ""derivedClass1"", ""Number"" : 42, ""$type"" : ""derivedClass1""}")]
        [InlineData("$.$id", @"{ ""$type"" : ""derivedClass1"", ""Number"" : 42, ""$id"" : ""referenceId""}")]
        [InlineData("$.$id", @"{ ""$type"" : ""derivedClass1"", """" : 42, ""$id"" : ""referenceId""}")]
        [InlineData("$.$values", @"{ ""Number"" : 42, ""$values"" : [] }")]
        [InlineData("$.$type", @"{ ""Number"" : 42, ""$type"" : ""derivedClass"" }")]
        [InlineData("$", @"{ ""$type"" : ""invalidDiscriminator"", ""Number"" : 42 }")]
        [InlineData("$", @"{ ""$type"" : 0, ""Number"" : 42 }")]
        [InlineData("$.$type", @"{ ""$type"" : false, ""Number"" : 42 }")]
        [InlineData("$.$type", @"{ ""$type"" : {}, ""Number"" : 42 }")]
        [InlineData("$.$type", @"{ ""$type"" : [], ""Number"" : 42 }")]
        [InlineData("$.$id", @"{ ""$id"" : ""1"", ""Number"" : 42 }")]
        [InlineData("$.$ref", @"{ ""$ref"" : ""1"" }")]
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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicClass>.Instance);

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
        [InlineData("$.$type", """{"Number":42, "$type":"derivedClass1", "String": "str", "$type":"derivedClass1"}""")]
        [InlineData("$.$type", """{"$type":"derivedCollection", "$values": [42,42,42], "$type":"derivedCollection"}""")]
        [InlineData("$.$values", """{"$type":"derivedCollection", "NonMetadataProp": {}, "$values": [42,42,42]}""")]
        [InlineData("$.NonMetadataProp", """{"$type":"derivedCollection", "$values": [42,42,42], "NonMetadataProp": {}}""")]
        [InlineData("$.NonMetadataProp", """{"$values": [42,42,42], "$type":"derivedCollection", "NonMetadataProp": {}}""")]
        [InlineData("$.$values", """{"$type":"derivedCollection", "$values": [42,42,42], "$values": [42,42,42]}""")]
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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicClass>.Instance,
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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicClass>.Instance,
                options: PolymorphicClass.CustomConfigWithBaseTypeFallback);
        }

        [Theory]
        [InlineData("$.$type", @"{ ""$type"" : ""derivedClass1"", ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : ""derivedClass1"", ""_case"" : ""derivedClass1"", ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""_case"" : ""derivedClass1""}")]
        [InlineData("$.$type", @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""$type"" : ""derivedClass1""}")]
        [InlineData("$.$id", @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""$id"" : ""referenceId""}")]
        [InlineData("$.$id", @"{ ""_case"" : ""derivedClass1"", """" : 42, ""$id"" : ""referenceId""}")]
        [InlineData("$.$values", @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""$values"" : [] }")]
        [InlineData("$._case", @"{ ""Number"" : 42, ""_case"" : ""derivedClass1"" }")]
        [InlineData("$", @"{ ""_case"" : ""invalidDiscriminator"", ""Number"" : 42 }")]
        [InlineData("$", @"{ ""_case"" : 0, ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : false, ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : {}, ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : [], ""Number"" : 42 }")]
        [InlineData("$.$id", @"{ ""$id"" : ""1"", ""Number"" : 42 }")]
        [InlineData("$.$ref", @"{ ""$ref"" : ""1"" }")]
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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicClass>.Instance,
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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicClass>.Instance,
                options: PolymorphicClass.CustomConfigWithNearestAncestorFallback);
        }

        [Theory]
        [InlineData("$.$type", @"{ ""$type"" : ""derivedClass1"", ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : ""derivedClass1"", ""_case"" : ""derivedClass1"", ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""_case"" : ""derivedClass1""}")]
        [InlineData("$.$type", @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""$type"" : ""derivedClass1""}")]
        [InlineData("$.$id", @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""$id"" : ""referenceId""}")]
        [InlineData("$.$id", @"{ ""_case"" : ""derivedClass1"", """" : 42, ""$id"" : ""referenceId""}")]
        [InlineData("$.$values", @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""$values"" : [] }")]
        [InlineData("$._case", @"{ ""Number"" : 42, ""_case"" : ""derivedClass1"" }")]
        [InlineData("$", @"{ ""_case"" : ""invalidDiscriminator"", ""Number"" : 42 }")]
        [InlineData("$", @"{ ""_case"" : 0, ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : false, ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : {}, ""Number"" : 42 }")]
        [InlineData("$._case", @"{ ""_case"" : [], ""Number"" : 42 }")]
        [InlineData("$.$id", @"{ ""$id"" : ""1"", ""Number"" : 42 }")]
        [InlineData("$.$ref", @"{ ""$ref"" : ""1"" }")]
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
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                {
                    Modifiers =
                    {
                        static jsonTypeInfo =>
                        {
                            if (jsonTypeInfo.Type == typeof(PolymorphicClass))
                            {
                                jsonTypeInfo.PolymorphismOptions = null;
                            }
                        }
                    }
                }
            };

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
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: @"{ ""Number"" : 42, ""String"" : ""str"" }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator.DerivedClass(),
                    ExpectedSerializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""Number"" : 42, ""String"" : ""str"", ""ExtraProperty"" : ""extra"" }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: @"{ ""$type"" : ""derivedClass1"", ""Number"" : 42, ""String"" : ""str"" }",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""$type"" : ""derivedClassOfDerivedClass1"", ""Number"" : 42, ""String"" : ""str"", ""ExtraProperty"" : ""extra"" }",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: @"{ ""Number"" : 42, ""Boolean"" : true }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator.DerivedClass(),
                    ExpectedSerializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: @"{ ""$type"" : ""derivedClass2"", ""Number"" : 42, ""Boolean"" : true }",
                    ExpectedRoundtripValue: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true });

                yield return new TestData(
                    Value: new DerivedClass_IntegerTypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: @"{ ""$type"" : -1, ""Number"" : 42, ""String"" : ""str"" }",
                    ExpectedRoundtripValue: new DerivedClass_IntegerTypeDiscriminator { Number = 42, String = "str" });

                yield return new TestData(
                    Value: new DerivedCollection_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: @"[42,42,42]",
                    ExpectedDeserializationException: typeof(JsonException));

                yield return new TestData(
                    Value: new DerivedCollection_NoTypeDiscriminator.DerivedClass(),
                    ExpectedSerializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""$type"" : ""derivedCollection"", ""$values"" : [42,42,42] }",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""$type"" : ""derivedCollectionOfDerivedCollection"", ""$values"" : [42,42,42] }",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator.DerivedClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""dictionaryKey"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass());

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""$type"":""derivedDictionary"", ""dictionaryKey"" : 42 }",
                    ExpectedRoundtripValue: new DerivedDictionary_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""$type"" : ""derivedDictionaryOfDerivedDictionary"", ""dictionaryKey"" : 42 }",
                    ExpectedRoundtripValue: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator(42),
                    ExpectedJson: @"{ ""$type"" : ""derivedClassWithCtor"", ""Number"" : 42 }",
                    ExpectedRoundtripValue: new DerivedClassWithConstructor_TypeDiscriminator(42));

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"),
                    ExpectedJson: @"{ ""$type"" : ""derivedClassOfDerivedClassWithCtor"", ""Number"" : 42, ""ExtraProperty"" : ""extra"" }",
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
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: @"{ ""Number"" : 42, ""String"" : ""str"" }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""String"" : ""str"" }",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""_case"" : ""derivedClassOfDerivedClass1"", ""Number"" : 42, ""String"" : ""str"", ""ExtraProperty"" : ""extra"" }",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator.DerivedClass { Number = 42, Boolean = true },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: @"{ ""_case"" : ""derivedClass2"", ""Number"" : 42, ""Boolean"" : true }",
                    ExpectedRoundtripValue: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator.DerivedClass { Number = 42, Boolean = true, ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""_case"" : ""derivedCollection"", ""$values"" : [42,42,42] }",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""dictionaryKey"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass());

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""_case"" : ""derivedDictionaryOfDerivedDictionary"", ""dictionaryKey"" : 42 }",
                    ExpectedRoundtripValue: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator(42),
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"),
                    ExpectedJson: @"{ ""_case"" : ""derivedClassOfDerivedClassWithCtor"", ""Number"" : 42, ""ExtraProperty"" : ""extra"" }",
                    ExpectedRoundtripValue: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"));

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""Number"" : 42 }",
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
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: @"{ ""Number"" : 42, ""String"" : ""str"" }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_NoTypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""Number"" : 42, ""String"" : ""str"" }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" },
                    ExpectedJson: @"{ ""_case"" : ""derivedClass1"", ""Number"" : 42, ""String"" : ""str"" }",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator { Number = 42, String = "str" });

                yield return new TestData(
                    Value: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""_case"" : ""derivedClassOfDerivedClass1"", ""Number"" : 42, ""String"" : ""str"", ""ExtraProperty"" : ""extra"" }",
                    ExpectedRoundtripValue: new DerivedClass1_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_NoTypeDiscriminator.DerivedClass { Number = 42, Boolean = true },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true },
                    ExpectedJson: @"{ ""_case"" : ""derivedClass2"", ""Number"" : 42, ""Boolean"" : true }",
                    ExpectedRoundtripValue: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true });

                yield return new TestData(
                    Value: new DerivedClass2_TypeDiscriminator.DerivedClass { Number = 42, Boolean = true, ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""_case"" : ""derivedClass2"", ""Number"" : 42, ""Boolean"" : true }",
                    ExpectedRoundtripValue: new DerivedClass2_TypeDiscriminator { Number = 42, Boolean = true });

                yield return new TestData(
                    Value: new DerivedAbstractClass.DerivedClass { Number = 42, Boolean = true },
                    ExpectedJson: @"{ ""_case"" : ""derivedAbstractClass"", ""Number"" : 42, ""Boolean"" : true }",
                    ExpectedDeserializationException: typeof(NotSupportedException));

                yield return new TestData(
                    Value: new DerivedCollection_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""_case"" : ""derivedCollection"", ""$values"" : [42,42,42] }",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedCollection_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""_case"" : ""derivedCollection"", ""$values"" : [42,42,42] }",
                    ExpectedRoundtripValue: new DerivedCollection_TypeDiscriminator { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""dictionaryKey"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass());

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42, ExtraProperty = "extra" },
                    ExpectedJson: @"{ ""_case"" : ""derivedDictionaryOfDerivedDictionary"", ""dictionaryKey"" : 42 }",
                    ExpectedRoundtripValue: new DerivedDictionary_TypeDiscriminator.DerivedClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator(42),
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClass { Number = 42 });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"),
                    ExpectedJson: @"{ ""_case"" : ""derivedClassOfDerivedClassWithCtor"", ""Number"" : 42, ""ExtraProperty"" : ""extra"" }",
                    ExpectedRoundtripValue: new DerivedClassWithConstructor_TypeDiscriminator.DerivedClass(42, "extra"));

                yield return new TestData(
                    Value: new DerivedClassWithCustomConverter_NoTypeDiscriminator { Number = 42 },
                    ExpectedJson: @"{ ""Number"" : 42 }",
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
            string json = @"{""$type"" : ""derivedClass""}";
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
            string expectedJson = @"{""$type"":""derivedClass""}";
            PolymorphicClass_WithDerivedPolymorphicClass value = new PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass();
            await TestMultiContextSerialization(value, expectedJson, contexts: ~SerializedValueContext.BoxedValue);
        }

        [Fact]
        public async Task PolymorphicClass_WithDerivedPolymorphicClass_Deserialization_ShouldUseBaseTypeContract()
        {
            string json = @"{""$type"":""derivedClass""}";

            var expectedValueUsingBaseContract = new PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass();
            await TestMultiContextDeserialization<PolymorphicClass_WithDerivedPolymorphicClass>(
                json,
                expectedValueUsingBaseContract,
                equalityComparer: PolymorphicEqualityComparer<PolymorphicClass_WithDerivedPolymorphicClass>.Instance);

            var expectedValueUsingDerivedContract = new PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass.DerivedClass2();
            await TestMultiContextDeserialization<PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass>(
                json,
                expectedValueUsingDerivedContract,
                equalityComparer: PolymorphicEqualityComparer<PolymorphicClass_WithDerivedPolymorphicClass.DerivedClass>.Instance);
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
                yield return WrapArgs(new PolymorphicClass_WithBaseTypeDiscriminator { Number = 42 }, @"{ ""$type"" : ""baseType"", ""Number"" : 42 }");
                yield return WrapArgs(new DerivedClass { Number = 42, String = "str" }, @"{ ""$type"" : ""derivedType"", ""Number"" : 42, ""String"" : ""str"" }");

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
                @"{
                   ""$type"": ""NodeList"",
                   ""Info"": ""1"",
                   ""List"": [
                     {
                       ""$type"": ""Leaf"",
                       ""Test"": null,
                       ""Name"": ""testName2""
                     },
                     {
                       ""$type"": ""NodeList"",
                       ""Info"": ""2"",
                       ""List"": [
                         {
                           ""$type"": ""NodeList"",
                           ""Info"": ""1"",
                           ""List"": null,
                           ""Name"": ""testName4""
                         }
                       ],
                       ""Name"": ""testName3""
                     }
                   ],
                   ""Name"": ""testName""}", json);

            TestNode deserialized = await Serializer.DeserializeWrapper<TestNode>(json);
            obj.AssertEqualTo(deserialized);
        }

        [JsonDerivedType(typeof(TestNodeList), "NodeList")]
        [JsonDerivedType(typeof(TestLeaf), "Leaf")]
        abstract class TestNode
        {
            public string Name { get; set; }

            public abstract void AssertEqualTo(TestNode other);
        }

        class TestNodeList : TestNode
        {
            public string Info { get; set; }

            public IEnumerable<TestNode> List { get; set; }

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

        class TestLeaf : TestNode
        {
            public string Test { get; set; }

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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicClassWithConstructor>.Instance);

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

            await TestMultiContextDeserialization(inputs, equalityComparer: PolymorphicEqualityComparer<PolymorphicClassWithConstructor>.Instance);
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
                    ExpectedJson: @"{ ""Number"" : 42 }",
                    ExpectedRoundtripValue: new PolymorphicClassWithConstructor(42));

                yield return new TestData(
                    Value: new DerivedClass { String = "str" },
                    ExpectedJson: @"{ ""$type"" : ""derivedClass"", ""Number"" : 0, ""String"" : ""str"" }",
                    ExpectedRoundtripValue: new DerivedClass { String = "str" });

                yield return new TestData(
                    Value: new DerivedClassWithConstructor(42, true),
                    ExpectedJson: @"{ ""$type"" : ""derivedClassWithCtor"", ""Number"" : 42, ""Boolean"" : true }",
                    ExpectedRoundtripValue: new DerivedClassWithConstructor(42, true));

                yield return new TestData(
                    Value: new DerivedCollection { 1, 2, 3 },
                    ExpectedJson: @"{ ""$type"" : ""derivedCollection"", ""$values"" : [1,2,3]}",
                    ExpectedRoundtripValue: new DerivedCollection { 1, 2, 3 });

                yield return new TestData(
                    Value: new DerivedDictionary { ["key1"] = 42, ["key2"] = -1 },
                    ExpectedJson: @"{ ""$type"" : ""derivedDictionary"", ""key1"" : 42, ""key2"" : -1 }",
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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicInterface>.Instance);

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

            await TestMultiContextDeserialization(inputs, equalityComparer: PolymorphicEqualityComparer<PolymorphicInterface>.Instance);
        }

        [Theory]
        [InlineData("$.$type", @"{ ""$type"" : ""derivedClass"", ""$type"" : ""derivedClass"", ""Number"" : 42 }")]
        [InlineData("$.$type", @"{ ""$type"" : ""derivedClass"", ""Number"" : 42, ""$type"" : ""derivedClass""}")]
        [InlineData("$.$id", @"{ ""$type"" : ""derivedClass"", ""Number"" : 42, ""$id"" : ""referenceId""}")]
        [InlineData("$.$values", @"{ ""$type"" : ""derivedClass"", ""Number"" : 42, ""$values"" : [] }")]
        [InlineData("$", @"{ ""$type"" : ""invalidDiscriminator"", ""Number"" : 42 }")]
        [InlineData("$", @"{ ""$type"" : 0, ""Number"" : 42 }")]
        [InlineData("$.$type", @"{ ""$type"" : false, ""Number"" : 42 }")]
        [InlineData("$.$type", @"{ ""$type"" : {}, ""Number"" : 42 }")]
        [InlineData("$.$type", @"{ ""$type"" : [], ""Number"" : 42 }")]
        [InlineData("$.$id", @"{ ""$id"" : ""1"", ""Number"" : 42 }")]
        [InlineData("$.$ref", @"{ ""$ref"" : ""1"" }")]
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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicInterface>.Instance);

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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicInterface>.Instance);
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
                        ExpectedJson: @"{ ""Number"" : 42, ""String"" : ""str"" }",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        new DerivedClass_NoTypeDiscriminator.DerivedClass(),
                        ExpectedSerializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: @"{ ""$type"" : ""derivedClass"", ""Number"" : 42, ""String"" : ""str"" }",
                        ExpectedRoundtripValue: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" });

                    yield return new TestData(
                        new DerivedClass_TypeDiscriminator.DerivedClass(),
                        ExpectedSerializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedStruct_NoTypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: @"{ ""Number"" : 42, ""String"" : ""str"" }",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedStruct_TypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: @"{ ""$type"" : ""derivedStruct"", ""Number"" : 42, ""String"" : ""str"" }",
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
                        ExpectedJson: @"{ ""Number"" : 42 }",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        new DerivedClass_NoTypeDiscriminator.DerivedClass { Number = 42 },
                        ExpectedJson: @"{ ""Number"" : 42 }",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: @"{ ""$type"" : ""derivedClass"", ""Number"" : 42, ""String"" : ""str"" }",
                        ExpectedRoundtripValue: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" });

                    yield return new TestData(
                        new DerivedClass_TypeDiscriminator.DerivedClass { Number = 42, String = "str", ExtraProperty = "extra" },
                        ExpectedJson: @"{ ""$type"" : ""derivedClass"", ""Number"" : 42, ""String"" : ""str"" }",
                        ExpectedRoundtripValue: new DerivedClass_TypeDiscriminator { Number = 42, String = "str" });

                    yield return new TestData(
                        Value: new DerivedStruct_NoTypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: @"{ ""Number"" : 42, ""String"" : ""str"" }",
                        ExpectedDeserializationException: typeof(NotSupportedException));

                    yield return new TestData(
                        Value: new DerivedStruct_TypeDiscriminator { Number = 42, String = "str" },
                        ExpectedJson: @"{ ""Number"" : 42 }",
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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicList>.Instance);

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

            await TestMultiContextDeserialization(inputs, equalityComparer: PolymorphicEqualityComparer<PolymorphicList>.Instance);
        }

        [Fact]
        public async Task PolymorphicList_UnrecognizedTypeDiscriminators_ShouldSucceedDeserialization()
        {
            string json = @"{ ""$type"" : ""invalidTypeDiscriminator"", ""$values"" : [42,42,42] }";
            PolymorphicList result = await Serializer.DeserializeWrapper<PolymorphicList>(json);
            Assert.IsType<PolymorphicList>(result);
            Assert.Equal(Enumerable.Repeat(42, 3), result);
        }

        [Theory]
        [InlineData("$.UnsupportedProperty", @"{ ""$type"" : ""derivedList"", ""UnsupportedProperty"" : 42 }")]
        [InlineData("$.UnsupportedProperty", @"{ ""$type"" : ""derivedList"", ""$values"" : [], ""UnsupportedProperty"" : 42 }")]
        [InlineData("$.$id", @"{ ""$id"" : 42, ""$values"" : [] }")]
        [InlineData("$.$ref", @"{ ""$ref"" : 42 }")]
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
                    ExpectedJson: @"{ ""$type"" : ""baseList"", ""$values"" : [42]}",
                    ExpectedRoundtripValue:  new PolymorphicList { 42 });

                yield return new TestData(
                    Value: new DerivedList1 { 42 },
                    ExpectedJson: @"{ ""$type"" : ""derivedList"", ""$values"" : [42]}",
                    ExpectedRoundtripValue: new DerivedList1 { 42 });

                yield return new TestData(
                    Value: new DerivedList2 { 42 },
                    ExpectedJson: @"{ ""$type"" : ""baseList"", ""$values"" : [42]}",
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
                @"[ [1,2,3],
                    { ""$type"":""list"" , ""$values"":[1,2,3] },
                    { ""$type"":""queue"", ""$values"":[1,2,3] },
                    { ""$type"":""set""  , ""$values"":[1,2,3] }]";

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
                @"[ [1,2,3],
                    { ""$type"":""list"" , ""$values"":[1,2,3] },
                    { ""$type"":""queue"", ""$values"":[1,2,3] },
                    { ""$type"":""set""  , ""$values"":[1,2,3] }]";

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
                equalityComparer: PolymorphicEqualityComparer<PolymorphicDictionary>.Instance);

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

            await TestMultiContextDeserialization(inputs, equalityComparer: PolymorphicEqualityComparer<PolymorphicDictionary>.Instance);
        }

        [Fact]
        public async Task PolymorphicDictionary_UnrecognizedTypeDiscriminators_ShouldSucceedDeserialization()
        {
            string json = @"{ ""$type"" : ""invalidTypeDiscriminator"", ""key"" : 42 }";
            PolymorphicDictionary result = await Serializer.DeserializeWrapper<PolymorphicDictionary>(json);
            Assert.IsType<PolymorphicDictionary>(result);
            Assert.Equal(new PolymorphicDictionary { ["key"] = 42 }, result);
        }

        [Theory]
        [InlineData("$.$ref", @"{ ""$type"" : ""derivedList"", ""UserProperty"" : 42, ""$ref"" : ""42"" }")]
        [InlineData("$.$type", @"{ ""$type"" : ""derivedList"", ""UserProperty"" : 42, ""$type"" : ""derivedDictionary"" }")]
        [InlineData("$.$type", @"{ ""UserProperty"" : 42, ""$type"" : ""derivedDictionary"" }")]
        [InlineData("$.$values", @"{ ""$type"" : ""derivedDictionary"", ""$values"" : [] }")]
        [InlineData("$.$id", @"{ ""$id"" : 42, ""UserProperty"" : 42 }")]
        [InlineData("$.$ref", @"{ ""$ref"" : 42 }")]
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
                    ExpectedJson: @"{ ""$type"" : ""baseDictionary"", ""key1"" : 42, ""key2"" : -1 }",
                    ExpectedRoundtripValue: new PolymorphicDictionary { ["key1"] = 42, ["key2"] = -1 });

                yield return new TestData(
                    Value: new DerivedDictionary1 { ["key1"] = 42, ["key2"] = -1 },
                    ExpectedJson: @"{ ""$type"" : ""derivedDictionary"", ""key1"" : 42, ""key2"" : -1 }",
                    ExpectedRoundtripValue: new DerivedDictionary1 { ["key1"] = 42, ["key2"] = -1 });

                yield return new TestData(
                    Value: new DerivedDictionary2 { ["key1"] = 42, ["key2"] = -1 },
                    ExpectedJson: @"{ ""$type"" : ""baseDictionary"", ""key1"" : 42, ""key2"" : -1 }",
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
                @"[ [ { ""Key"":0, ""Value"":0 } ],
                    { ""$type"" : ""dictionary"", ""42"" : false },
                    { ""$type"" : ""sortedDictionary"", ""0"" : 1, ""1"" : 42 },
                    { ""$type"" : ""readOnlyDictionary"" } ]";

            string actualJson = await Serializer.SerializeWrapper(values, s_optionsWithPolymorphicDictionaryInterface);

            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Fact]
        public async Task PolymorphicDictionaryInterface_Deserialization()
        {
            string json =
                @"[ [ { ""Key"":0, ""Value"":0 } ],
                    { ""$type"" : ""dictionary"", ""42"" : false },
                    { ""$type"" : ""sortedDictionary"", ""0"" : 1, ""1"" : 42 },
                    { ""$type"" : ""readOnlyDictionary"" } ]";

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
        [InlineData(0, @"{""$type"":""zero""}")]
        [InlineData(1, @"{""$type"":""succ"", ""value"":{""$type"":""zero""}}")]
        [InlineData(3, @"{""$type"":""succ"", ""value"":{""$type"":""succ"",""value"":{""$type"":""succ"",""value"":{""$type"":""zero""}}}}")]
        public async Task Peano_Serialization(int size, string expectedJson)
        {
            Peano peano = Peano.FromInteger(size);
            await TestMultiContextSerialization(peano, expectedJson);
        }

        [Theory]
        [InlineData(0, @"{""$type"":""zero""}")]
        [InlineData(1, @"{""$type"":""succ"", ""value"":{""$type"":""zero""}}")]
        [InlineData(3, @"{""$type"":""succ"", ""value"":{""$type"":""succ"",""value"":{""$type"":""succ"",""value"":{""$type"":""zero""}}}}")]
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
                yield return WrapArgs(new Leaf(), @"{""$type"":""leaf""}");
                yield return WrapArgs(
                    new Node(-1,
                        new Leaf(),
                        new Leaf()),
                    @"{""$type"":""node"",""value"":-1,""left"":{""$type"":""leaf""},""right"":{""$type"":""leaf""}}");

                yield return WrapArgs(
                    new Node(12,
                        new Leaf(),
                        new Node(24,
                            new Leaf(),
                            new Leaf())),
                    @"{""$type"":""node"", ""value"":12,
                            ""left"":{""$type"":""leaf""},
                            ""right"":{""$type"":""node"", ""value"":24,
                                      ""left"":{""$type"":""leaf""},
                                      ""right"":{""$type"":""leaf""}}}");

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
            Assert.Equal(expectedValue, actualValue, PolymorphicEqualityComparer<PolymorphicClass>.Instance);
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
            Assert.Equal(expectedValue, result[0], PolymorphicEqualityComparer<PolymorphicClass>.Instance);
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
            Assert.Equal(expectedValues, result, PolymorphicEqualityComparer<PolymorphicClass>.Instance);
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
            Assert.Equal(expectedValue, actualValue, PolymorphicEqualityComparer<PolymorphicClassWithCustomTypeDiscriminator>.Instance);
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
            Assert.Equal(expectedValue, result[0], PolymorphicEqualityComparer<PolymorphicClassWithCustomTypeDiscriminator>.Instance);
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
            Assert.Equal(expectedValues, result, PolymorphicEqualityComparer<PolymorphicClassWithCustomTypeDiscriminator>.Instance);
        }

        [Theory]
        [MemberData(nameof(Get_ReferencePreservation_TestData_Boxed))]
        public async Task ReferencePreservation_AllowOutOfOrderMetadata_SingleValue_Deserialization(PolymorphicClass expectedValue, Func<string, string> jsonTemplate)
        {
            string json = jsonTemplate("1"); // root values have reference id "1"
            PolymorphicClass actualValue = await Serializer.DeserializeWrapper<PolymorphicClass>(json, s_jsonSerializerOptionsPreserveRefsAndAllowReadAhead);
            Assert.Equal(expectedValue, actualValue, PolymorphicEqualityComparer<PolymorphicClass>.Instance);
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
            Assert.Equal(expectedValue, result[0], PolymorphicEqualityComparer<PolymorphicClass>.Instance);
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
            Assert.Equal(expectedValues, result, PolymorphicEqualityComparer<PolymorphicClass>.Instance);
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
        [InlineData("$[1].$ref", """[{ "$id" : "1" }, { "NonMetadataProperty": [1,2,3], "$ref" : "1" }]""")]
        [InlineData("$[1].NonMetadataProperty", """[{ "$id" : "1" }, { "$ref" : "1", "NonMetadataProperty": [1,2,3] }]""")]
        [InlineData("$[1].$ref", """[{ "$id" : "1" }, { "$type": "derivedClass1", "$ref" : "1" }]""")]
        [InlineData("$[1].$type", """[{ "$id" : "1" }, { "$ref" : "1", "$type": "derivedClass1" }]""")]
        [InlineData("$[1].$id", """[{ "$id" : "1" }, { "$ref" : "1", "$id": "1" }]""")]
        [InlineData("$[1].$ref", """[{ "$id" : "1" }, { "$id": "1", "$ref" : "1" }]""")]
        [InlineData("$[1].$ref", """[{ "$id" : "1" }, { "$values": [1, 2, 3], "$ref" : "1" }]""")]
        [InlineData("$[1].$values", """[{ "$id" : "1" }, { "$ref" : "1", "$values": [1, 2, 3] }]""")]
        [InlineData("$[0].NonMetadataProperty", """[{ "$type" : "derivedCollection", "$values": [42,42,42], "$id" : "1", "NonMetadataProperty": {}}, { "$ref" : "1" }]""")]
        [InlineData("$[0].$values", """[{ "$type" : "derivedCollection", "$id" : "1", "NonMetadataProperty": {}, "$values": [42,42,42]}, { "$ref" : "1" }]""")]
        [InlineData("$[1].$ref", """[{ "$type" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$type" : "derivedCollection", "$ref" : "1" }]""")]
        [InlineData("$[1].$values", """[{ "$type" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$ref" : "1", "$values" : [1,2,3] }]""")]
        [InlineData("$[1].$ref", """[{ "$type" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$values" : [1,2,3], "$ref" : "1" }]""")]
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
        [InlineData("$[1].$ref", """[{ "$id" : "1" }, { "NonMetadataProperty": [1,2,3], "$ref" : "1" }]""")]
        [InlineData("$[1].NonMetadataProperty", """[{ "$id" : "1" }, { "$ref" : "1", "NonMetadataProperty": [1,2,3] }]""")]
        [InlineData("$[1].$ref", """[{ "$id" : "1" }, { "case": "derivedClass", "$ref" : "1" }]""")]
        [InlineData("$[1].case", """[{ "$id" : "1" }, { "$ref" : "1", "case": "derivedClass" }]""")]
        [InlineData("$[1].$id", """[{ "$id" : "1" }, { "$ref" : "1", "$id": "1" }]""")]
        [InlineData("$[1].$ref", """[{ "$id" : "1" }, { "$id": "1", "$ref" : "1" }]""")]
        [InlineData("$[1].$ref", """[{ "$id" : "1" }, { "$values": [1, 2, 3], "$ref" : "1" }]""")]
        [InlineData("$[1].$values", """[{ "$id" : "1" }, { "$ref" : "1", "$values": [1, 2, 3] }]""")]
        [InlineData("$[0].NonMetadataProperty", """[{ "case" : "derivedCollection", "$values": [42,42,42], "$id" : "1", "NonMetadataProperty": {}}, { "$ref" : "1" }]""")]
        [InlineData("$[0].$values", """[{ "case" : "derivedCollection", "$id" : "1", "NonMetadataProperty": {}, "$values": [42,42,42]}, { "$ref" : "1" }]""")]
        [InlineData("$[1].$ref", """[{ "case" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "case" : "derivedCollection", "$ref" : "1" }]""")]
        [InlineData("$[1].$values", """[{ "case" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$ref" : "1", "$values" : [1,2,3] }]""")]
        [InlineData("$[1].$ref", """[{ "case" : "derivedCollection", "$id" : "1", "$values": [42,42,42]}, { "$values" : [1,2,3], "$ref" : "1" }]""")]
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

        [Fact]
        public async Task PolymorphicClassWithNullDerivedTypeAttribute_ThrowsInvalidOperationException()
        {
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
            string json = @"{""$type"":""derivedInterface""}";
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.DeserializeWrapper<PolymorphicInterfaceWithInterfaceDerivedType>(json));
        }

        [Fact]
        public async Task PolymorphicInterfaceWithInterfaceDerivedType_FallbackToNearestAncestor_Serialization()
        {
            PolymorphicInterfaceWithInterfaceDerivedType value = new PolymorphicInterfaceWithInterfaceDerivedType.DerivedInterface.ImplementingClass();
            string expectedJson = @"{""$type"":""derivedInterface""}";
            string actualJson = await Serializer.SerializeWrapper(value, PolymorphicInterfaceWithInterfaceDerivedType_OptionsWithFallbackToNearestAncestor);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Fact]
        public async Task PolymorphicInterfaceWithInterfaceDerivedType_FallbackToNearestAncestor_Deserialization_ThrowsNotSupportedException()
        {
            string json = @"{""$type"":""derivedInterface""}";
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
        public async Task PolymorphicGenericClass_ThrowsInvalidOperationException()
        {
            PolymorphicGenericClass<int> value = new PolymorphicGenericClass<int>.DerivedClass();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value));
        }

        [JsonDerivedType(typeof(PolymorphicGenericClass<>.DerivedClass))]
        public class PolymorphicGenericClass<T>
        {
            public class DerivedClass : PolymorphicGenericClass<T>
            {
            }
        }

        [Fact]
        public async Task PolymorphicDerivedGenericClass_ThrowsInvalidOperationException()
        {
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

        [Fact]
        public async Task PolymorphicClass_CustomConverter_TypeDiscriminator_Serialization_ThrowsNotSupportedException()
        {
            PolymorphicClass_CustomConverter_TypeDiscriminator value = new PolymorphicClass_CustomConverter_TypeDiscriminator.DerivedClass();
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(value));
        }

        [Fact]
        public async Task PolymorphicClass_CustomConverter_TypeDiscriminator_Deserialization_ThrowsNotSupportedException()
        {
            string json = @"{ ""$type"" : ""derivedClass"" }";
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
            string expectedJson = @"{ ""Number"" : 42 }";
            string actualJson = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Fact]
        public async Task PolymorphicClass_CustomConverter_NoTypeDiscriminator_Deserialization()
        {
            string json = @"{ ""Number"" : 42 }";
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

            string expectedJson = @"{ ""$type"" : ""derivedClass"", ""Number"" : 42 }";
            string actualJson = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
        }

        [Theory]
        [InlineData(@"")]
        [InlineData(@" ")]
        [InlineData(@"\t")]
        [InlineData(@"\r\n")]
        [InlineData(@"{ ""lol"" : true }")]
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
        public class PolymorphicEqualityComparer<TBaseType> : IEqualityComparer<TBaseType>
            where TBaseType : class
        {
            public static PolymorphicEqualityComparer<TBaseType> Instance { get; } = new();

            public bool Equals(TBaseType? left, TBaseType? right)
            {
                if (left is null || right is null)
                {
                    return left is null == right is null;
                }

                Type runtimeType = left.GetType();
                if (runtimeType != right.GetType())
                {
                    return false;
                }

                EqualityComparer<object> objComparer = EqualityComparer<object>.Default;

                // Runtime type is enumerable; use enumerable sequence comparison
                if (left is IEnumerable leftColl)
                {
                    IEnumerable rightColl = (IEnumerable)right;
                    return leftColl.Cast<object>().SequenceEqual(rightColl.Cast<object>(), objComparer);
                }

                // Runtime is regular POCO; use property structural comparison
                foreach (var propInfo in runtimeType.GetProperties())
                {
                    if (!objComparer.Equals(propInfo.GetValue(left), propInfo.GetValue(right)))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(TBaseType _) => throw new NotImplementedException();
        }

        public class CustomPolymorphismResolver : DefaultJsonTypeInfoResolver
        {
            private List<JsonDerivedType> _jsonDerivedTypes = new();

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

            public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);
                if (jsonTypeInfo.Type == BaseType)
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
